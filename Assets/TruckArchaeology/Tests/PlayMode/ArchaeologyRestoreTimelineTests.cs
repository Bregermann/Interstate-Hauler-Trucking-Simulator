#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Focused original-runtime scheduling and assembly reproduction. These cases
// deliberately do NOT represent Editor Digger persistence as player disk proof.
public sealed class ArchaeologyRestoreTimelineTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static Type Runtime(string n)=>Type.GetType("LWS."+n+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static void Set(object o,string n,object v){if(o.GetType().GetField(n,Any) is FieldInfo f)f.SetValue(o,v);else o.GetType().GetProperty(n,Any).SetValue(o,v);}
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);
    sealed class Scope:IDisposable
    {
        readonly IDisposable prefs;readonly InputSettings old,scoped;readonly GameObject isolation;
        public Component match;public readonly string folder;
        public Scope()
        {
            prefs=(IDisposable)Runtime("TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ArchaeologyRestore"+Guid.NewGuid().ToString("N")});
            old=InputSystem.settings;scoped=UnityEngine.Object.Instantiate(old);scoped.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;scoped.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=scoped;
            isolation=new GameObject("Actual Archaeology restore regression input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("TruckSports.SportsProbeInputIsolation"));
            folder=Path.GetFullPath("Logs/TruckSports/Impossible07/ArchaeologyRestoreTimeline/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        }
        public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckArchaeology/Scenes/TruckArchaeology.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("TruckArchaeology.ArchaeologyMatch"));Assert.That(match,Is.Not.Null);Call(match,"AllBots");Set(Get(match,"setup"),"count",1);
        }
        public void Trace(string detail){File.AppendAllText(Path.Combine(folder,"actual-timeline.txt"),"frame="+Time.frameCount+" scale="+Time.timeScale.ToString("R")+" "+detail+"\n",new UTF8Encoding(false));TestContext.WriteLine(detail);}
        public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=old;UnityEngine.Object.DestroyImmediate(scoped);prefs.Dispose();}
    }
    static IEnumerator Until(Func<bool> condition,float seconds,string message)
    {float until=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<until)yield return null;Assert.That(condition(),Is.True,message);}
    static Rigidbody[] Rig(Component match)=>((Array)Get(match,"trucks")).Cast<Component>().Take((int)Get(Get(match,"setup"),"count")).SelectMany(t=>t.GetComponentsInChildren<Rigidbody>(true)).ToArray();
    static void Observe(Scope s,Rigidbody[] bodies,object[] saved,string phase)
    {
        var truck=((Array)Get(s.match,"trucks")).GetValue(0);var motor=Get(truck,"tractorMotor");var vc=Get(motor,"vehicle");
        s.Trace(phase+" NWH.IsInitialized="+Get(vc,"IsInitialized")+" live="+Get(s.match,"Live")+" paused="+Get(s.match,"Paused"));
        for(int i=0;i<bodies.Length;i++){var b=bodies[i];var p=(Vector3)Get(saved[i],"position");var q=(Quaternion)Get(saved[i],"rotation");s.Trace(phase+" id="+Get(saved[i],"id")+" body="+b.position.ToString("F9")+" transform="+b.transform.position.ToString("F9")+" saved="+p.ToString("F9")+" posError="+Vector3.Distance(b.position,p).ToString("R")+" angle="+Quaternion.Angle(b.rotation,q).ToString("R")+" vel="+b.linearVelocity.ToString("F9")+" expectedVel="+((Vector3)Get(saved[i],"velocity")).ToString("F9")+" angular="+b.angularVelocity.ToString("F9")+" expectedAngular="+((Vector3)Get(saved[i],"angular")).ToString("F9")+" kinematic="+b.isKinematic+" expected="+Get(saved[i],"kinematic")+" interpolation="+b.interpolation);}
    }
    [UnityTest,Timeout(70000)]public IEnumerator BackwardClockInvalidatesOnlyTransientFuturePlansThenActualAiCutsWithinOriginalSixSeconds()
    {
        using(var s=new Scope())
        {
            yield return s.Load();Assert.That((bool)Call(s.match,"StartMatch"),Is.True);var earth=Get(s.match,"earth");
            yield return Until(()=>(int)Get(earth,"ActualEdits")>=4,25,"Original AI creates real edits before checkpoint clock sample");float checkpoint=(float)Get(s.match,"Clock");
            yield return Until(()=>(float)Get(s.match,"Clock")>=checkpoint+20&&(bool)Get(earth,"Quiescent"),30,"Ordinary original1x simulation advances actual future clock");Call(s.match,"Pause",0,false);yield return null;yield return null;
            float future=(float)Get(s.match,"Clock");int cuts=(int)Get(earth,"ActualEdits");s.Trace("BEFORE "+Call(s.match,"AiWellTelemetry",0)+" nextStroke="+Get(s.match,"nextStroke")+" nextExposure="+Get(s.match,"nextExposure"));
            Assert.That((float)Get(s.match,"nextStroke"),Is.GreaterThan(checkpoint+10));
            // Controlled scheduling regression only: no terrain/rules/poses are
            // restored here. Actual disk+voxel restoration is the Windows probe.
            Set(s.match,"Clock",checkpoint);Call(s.match,"ResetRestoredWorkScheduling");
            Assert.That((float)Get(s.match,"nextStroke"),Is.EqualTo(checkpoint));Assert.That((float)Get(s.match,"nextExposure"),Is.EqualTo(checkpoint));
            var wells=(Array)Get(s.match,"aiWells");foreach(var well in wells){Assert.That((bool)Get(well,"Active"),Is.False);Assert.That((bool)Call(well,"Due",checkpoint),Is.True);Assert.That((int)Get(well,"Artifact"),Is.EqualTo(-1));}
            Assert.That((int)Get(earth,"ActualEdits"),Is.EqualTo(cuts));Assert.That((int)Get(Get(s.match,"ScoreJournal"),"Count"),Is.Zero);Call(s.match,"Resume");
            yield return Until(()=>(int)Get(earth,"ActualEdits")>cuts,6,"Actual original AI resumes accepted Digger edits within unchanged6real-second gate");
            Assert.That((float)Get(s.match,"Clock"),Is.LessThan(future));s.Trace("AFTER actualEdits="+Get(earth,"ActualEdits")+" "+Call(s.match,"AiWellTelemetry",0));
        }
    }
    [UnityTest,Timeout(35000)]public IEnumerator FreshScenePausedAssemblyRestoreRetainsActualCapturedWorldBodiesAcrossVendorStartup()
    {
        using(var s=new Scope())
        {
            yield return s.Load();Set(Get(s.match,"setup"),"count",4);Assert.That((bool)Call(s.match,"StartMatch"),Is.True);
            yield return Until(()=>(float)Get(s.match,"Clock")>=3.12f,10,"Original physical NWH assembly reaches actual checkpoint");Call(s.match,"Pause",0,false);yield return null;yield return null;
            var bodies=Rig(s.match);Assert.That(bodies.Length,Is.EqualTo(8),"Four actual original NWH root+Cab assemblies, not one representative rig.");var capture=s.match.GetType().GetMethod("CaptureBody",Any);var saved=bodies.Select((body,i)=>capture.Invoke(s.match,new object[]{"actual.original.body."+i,body})).ToArray();Observe(s,bodies,saved,"NATURAL_CHECKPOINT");
            Call(s.match,"SetupScreen");yield return s.Load();Set(Get(s.match,"setup"),"count",4);Assert.That((bool)Call(s.match,"StartMatch"),Is.True);Call(s.match,"Pause",0,false);
            var fresh=Rig(s.match);Assert.That(fresh.Length,Is.EqualTo(saved.Length));Observe(s,fresh,saved,"FRESH_BEFORE_APPLY");
            var apply=s.match.GetType().GetMethod("ApplyBody",Any);for(int i=0;i<fresh.Length;i++)apply.Invoke(null,new[]{(object)fresh[i],saved[i]});Observe(s,fresh,saved,"IMMEDIATE_AFTER_ORIGINAL_APPLY");
            yield return null;Observe(s,fresh,saved,"AFTER_FRAME_ONE");yield return null;Observe(s,fresh,saved,"AFTER_FRAME_TWO");
            for(int i=0;i<fresh.Length;i++)
            {
                Assert.That(Vector3.Distance(fresh[i].position,(Vector3)Get(saved[i],"position")),Is.LessThan(.00001f),"Actual original body world pose survives paused native/vendor startup: "+Get(saved[i],"id"));
                Assert.That(Vector3.Distance(fresh[i].transform.position,(Vector3)Get(saved[i],"position")),Is.LessThan(.00001f),"Paused visible Transform must publish the SAME saved Rigidbody world pose: "+Get(saved[i],"id"));
                Assert.That(Quaternion.Angle(fresh[i].transform.rotation,(Quaternion)Get(saved[i],"rotation")),Is.LessThan(.01f));
                Assert.That(Quaternion.Angle(fresh[i].rotation,(Quaternion)Get(saved[i],"rotation")),Is.LessThan(.01f));Assert.That(fresh[i].isKinematic,Is.EqualTo((bool)Get(saved[i],"kinematic")));
                if(!fresh[i].isKinematic){Assert.That(Vector3.Distance(fresh[i].linearVelocity,(Vector3)Get(saved[i],"velocity")),Is.LessThan(.0001f));Assert.That(Vector3.Distance(fresh[i].angularVelocity,(Vector3)Get(saved[i],"angular")),Is.LessThan(.0001f));}
            }
            s.Trace("CONTROLLED native original ApplyBody path using captured actual checkpoint, no Disk/Digger restoration claim and no fixture SyncTransforms or explicit vendor Initialize.");
        }
    }
}
#endif
