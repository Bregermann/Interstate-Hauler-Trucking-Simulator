#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Digger.Modules.Core.Sources;
using LWS.TruckArchaeology;
public sealed class ArchaeologyPlayModeTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly InputSettings original,temporary;readonly GameObject isolation;public Behaviour match;
        public ArchaeologyRules Rules=>(ArchaeologyRules)Get(match,"Rules");public Component[] Finds=>((Array)Get(match,"artifacts")).Cast<Component>().ToArray();public object Earth=>Get(match,"earth");
        public Fixture(){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ArchaeologyNative07"});original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;isolation=new GameObject("Archaeology original input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
        public IEnumerator Load(){Assert.That(File.Exists("Assets/TruckArchaeology/Scenes/TruckArchaeology.unity"),Is.True,"Lead must author actual Digger terrain after private dependency intake.");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckArchaeology/Scenes/TruckArchaeology.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckArchaeology.ArchaeologyMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;}
        public void Dispose(){Time.timeScale=1;if(match!=null){match.enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(temporary);prefs.Dispose();}
    }
    static IEnumerator Until(Fixture f,Func<bool> test,float seconds,string label){float deadline=Time.realtimeSinceStartup+seconds;while(!test()&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(test(),Is.True,label+" clock="+Get(f.match,"Clock")+" edits="+Get(f.Earth,"ActualEdits"));}
    static bool Surface(object earth,Vector3 p,out RaycastHit hit){object[] args={p,default(RaycastHit)};bool ok=(bool)earth.GetType().GetMethod("Surface").Invoke(earth,args);hit=(RaycastHit)args[1];return ok;}
    static void Capture(Fixture f,string name){string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07Archaeology");Directory.CreateDirectory(folder);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6);foreach(int w in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,w,w==1280?720:1080,Path.Combine(folder,name+"-"+w+"-controlled.png"),null});}
    [UnityTest,Timeout(150000)]public IEnumerator ActualDiggerCutsVisibleTerrainAndRebuildsRealColliderWithoutHidingArtifactOrScoring()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();Assert.That((bool)Get(f.Earth,"Ready"),Is.True);Assert.That((bool)Call(f.match,"Operate",0),Is.True,"Actual owner scanner range must discover the first physical plot.");f.match.enabled=false;
            var artifact=f.Finds[0];Vector3 p=(Vector3)Get(artifact,"seedPosition");var renderers=artifact.GetComponentsInChildren<Renderer>(true);Assert.That(renderers.Length,Is.GreaterThan(10));Assert.That(Surface(f.Earth,p,out RaycastHit before),Is.True);float initial=before.point.y;
            for(int i=0;i<8;i++){Assert.That(Surface(f.Earth,p,out RaycastHit surface),Is.True);Assert.That((bool)Call(f.Earth,"Cut",surface.point-Vector3.up*.35f,1.7f,1f),Is.True);yield return null;yield return null;}
            Physics.SyncTransforms();Assert.That(Surface(f.Earth,p,out RaycastHit after),Is.True);TestContext.WriteLine("ACTUAL_DIGGER colliderBefore="+before.collider.name+" y="+initial+" colliderAfter="+after.collider.name+" y="+after.point.y+" DiggerVersion="+DiggerSystem.DiggerVersion);
            Assert.That(after.point.y,Is.LessThan(initial-1));Assert.That(after.collider.GetComponentInParent<DiggerSystem>(),Is.Not.Null,"Real vendor voxel collider, not a fake hole or hidden artifact.");Assert.That(renderers.All(r=>r.enabled&&r.gameObject.activeInHierarchy),Is.True);Assert.That(f.Rules.Scores.Sum(),Is.Zero);Assert.That(f.Rules.Delivered,Is.Zero);Capture(f,"controlled-actual-vendor-voxel-collider-cut");
        }
    }
    [UnityTest,Timeout(720000)]public IEnumerator NaturalFourAiScanExcavatePreservePhysicallyLoadDeliverAndFinishActualSession()=>RunNaturalSession();
    // The game is untimed: three real finds per crew, then actual gallery/results.
    // R33's original600-real-second failure is retained; this additional case
    // covers the entire configured session rather than relabeling that result.
    [UnityTest,Timeout(1700000)]public IEnumerator CompleteUntimedFourAiAllTwelveActualFindsPreservedDeliveredUnloadedResultsPauseAndRematch()=>RunNaturalSession(1500,true);
    IEnumerator RunNaturalSession(float realBudget=600,bool fullLifecycle=false)
    {
        using(var f=new Fixture())
        {
            yield return f.Load();
            // IsolateValidation namespaces persistent preferences; it does not
            // erase a preceding controlled SOLO fixture's saved count. This
            // original full-session contract explicitly exercises FOUR crews.
            Call(f.match,"SetupScreen");var actualSetup=Get(f.match,"setup");actualSetup.GetType().GetField("count").SetValue(actualSetup,4);Call(f.match,"NormalizeSetup");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True,"Four actual AI crews must start through the original setup boundary");
            Assert.That((int)Get(actualSetup,"count"),Is.EqualTo(4),"Persisted solo validation preferences must not shrink the full twelve-find contract");Assert.That(f.Rules.Players,Is.EqualTo(4));Assert.That((bool)Get(f.match,"AllAI"),Is.True);Assert.That(f.Finds.Count(a=>a.gameObject.activeInHierarchy),Is.EqualTo(12));
            Debug.Log("ARCHAEOLOGY_FULL_SESSION_SETUP count="+Get(actualSetup,"count")+" option="+Get(f.match,"option")+" rulePlayers="+f.Rules.Players+" sources="+string.Join(",",((Array)Get(actualSetup,"players")).Cast<object>().Take(4).Select(player=>Get(player,"source")))+" activeOriginalFinds="+f.Finds.Count(a=>a.gameObject.activeInHierarchy));
            string evidenceDirectory=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ArchaeologyFullFourAiNative/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(evidenceDirectory);string evidencePath=Path.Combine(evidenceDirectory,"original-four-AI-full-session-owner-tool-clearance.txt");var snapshot=typeof(ArchaeologyExcavationDiagnostics).GetMethod("Snapshot",BindingFlags.Static|BindingFlags.NonPublic);var utf8=new System.Text.UTF8Encoding(false);File.WriteAllText(evidencePath,"Original four-AI full"+realBudget.ToString("0")+"-real-second observation budget for an UNTIMED objective session; read-only native owner/tool/workpoint/clearance evidence.\ncount="+Get(actualSetup,"count")+" option="+Get(f.match,"option")+" rulePlayers="+f.Rules.Players+" activeFinds="+f.Finds.Count(a=>a.gameObject.activeInHierarchy)+"\n",utf8);Debug.Log("ARCHAEOLOGY_FULL_SESSION_EVIDENCE "+evidencePath);
            var bodies=f.Finds.Select(a=>(Rigidbody)Get(a,"body")).ToArray();float deadline=Time.realtimeSinceStartup+realBudget,next=0;bool captured=false,pausedOnce=false;bool[] actualDynamicRelease=new bool[12],actualCargo=new bool[12];
            while(!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.realtimeSinceStartup>=next){Debug.Log("ARCHAEOLOGY_NATURAL clock="+Get(f.match,"Clock")+" stages="+string.Join(",",f.Rules.Stages)+" scores="+string.Join(",",f.Rules.Scores)+" edits="+Get(f.Earth,"ActualEdits")+" loads="+Get(f.match,"PhysicalLoads")+" unloads="+Get(f.match,"PhysicalUnloads"));string observed=(string)snapshot.Invoke(null,new object[]{f.match});File.AppendAllText(evidencePath,"SAMPLE\n"+observed+"\n",utf8);Debug.Log(observed);next=Time.realtimeSinceStartup+10;}
                if(fullLifecycle)
                {
                    for(int id=0;id<12;id++){actualDynamicRelease[id]|=!bodies[id].isKinematic;actualCargo[id]|=f.Rules.Stages[id]==ArtifactStage.Cargo;}
                    if(!pausedOnce&&f.Rules.Delivered>0)
                    {
                        Call(f.match,"Pause",0,false);yield return null;yield return null;Vector3[] pausedPositions=bodies.Select(body=>body.position).ToArray();Quaternion[] pausedRotations=bodies.Select(body=>body.rotation).ToArray();float pausedClock=(float)Get(f.match,"Clock");int pausedEdits=(int)Get(f.Earth,"ActualEdits");yield return new WaitForSecondsRealtime(.25f);CollectionAssert.AreEqual(pausedPositions,bodies.Select(body=>body.position).ToArray());CollectionAssert.AreEqual(pausedRotations,bodies.Select(body=>body.rotation).ToArray());Assert.That((float)Get(f.match,"Clock"),Is.EqualTo(pausedClock));Assert.That((int)Get(f.Earth,"ActualEdits"),Is.EqualTo(pausedEdits));Assert.That((bool)Get(f.match,"Paused"),Is.True);Call(f.match,"Resume");Assert.That((bool)Get(f.match,"Live"),Is.True);Assert.That(Time.timeScale,Is.EqualTo(1));pausedOnce=true;File.AppendAllText(evidencePath,"ACTUAL_MID_SESSION_PAUSE_RESUME clock="+pausedClock+" edits="+pausedEdits+" all12actualbodyposes/quaternions/clock/edits exact\n",utf8);
                    }
                }
                if(!captured&&f.Rules.Delivered>0){Capture(f,"natural-first-physical-research-reception");captured=true;}yield return null;
            }
            string finalObservation=(string)snapshot.Invoke(null,new object[]{f.match});File.AppendAllText(evidencePath,"FINAL_BEFORE_ORIGINAL_ASSERTIONS\n"+finalObservation+"\n",utf8);Debug.Log(finalObservation);
            Assert.That((bool)Get(f.match,"Finished"),Is.True,"Original AI must finish actual excavation/precision/physical crane/delivery without pose or result injection.");Assert.That(f.Rules.Delivered,Is.EqualTo(12));Assert.That((int)Get(f.Earth,"ActualEdits"),Is.GreaterThan(12));Assert.That((int)Get(f.match,"ActualScans"),Is.EqualTo(12));Assert.That((int)Get(f.match,"PhysicalLoads"),Is.EqualTo(12));Assert.That((int)Get(f.match,"PhysicalUnloads"),Is.EqualTo(12));Assert.That(f.Rules.Scores.Sum(),Is.GreaterThan(0));Assert.That(f.Rules.Preservation.Any(p=>p<1),Is.True,"Real physical work has actual preservation consequences.");Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(12));CollectionAssert.AreEqual(bodies,f.Finds.Select(a=>(Rigidbody)Get(a,"body")).ToArray());Capture(f,"natural-full-preserved-exhibit-results");
            if(fullLifecycle)
            {
                Assert.That(pausedOnce,Is.True);Assert.That(actualDynamicRelease.All(value=>value),Is.True,"All twelve original bodies must genuinely become dynamic before actual crane recovery");Assert.That(actualCargo.All(value=>value),Is.True,"All twelve must pass real cargo stage; delivered counters alone cannot substitute");Assert.That(f.Rules.Complete,Is.True);Assert.That(Get(Get(f.match,"menus"),"Screen").ToString(),Is.EqualTo("Result"));Assert.That(((Array)Get(f.match,"equipment")).Cast<Component>().All(eq=>Get(Get(eq,"hoist"),"Stage").ToString()=="Idle"),Is.True,"Results wait until all physical exhibit unloading finishes");
                for(int owner=0;owner<4;owner++){int expected=f.Rules.Cooperative?Enumerable.Range(0,12).Where(id=>f.Rules.Owners[id]==owner).Sum(id=>(int)Math.Round(f.Rules.Preservation[id]*(int)Get(f.Finds[id],"value"),MidpointRounding.AwayFromZero)):Enumerable.Range(owner*3,3).Sum(id=>(int)Math.Round(f.Rules.Preservation[id]*(int)Get(f.Finds[id],"value"),MidpointRounding.AwayFromZero));Assert.That(f.Rules.Scores[owner],Is.EqualTo(expected),"Only exact physically received preserved values score, once per real artifact");}
                File.AppendAllText(evidencePath,"ACTUAL_COMPLETE_UNTIMED_SESSION clock="+Get(f.match,"Clock")+" realBudget="+realBudget+" delivered=12 loads=12 unloads=12 actualDynamic=12 actualCargo=12 scoreJournal=12 originalBodiesRetained\n",utf8);
                Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True,"Original rematch start, no forced state");Assert.That((float)Get(f.match,"Clock"),Is.Zero);Assert.That((int)Get(f.match,"PhysicalLoads"),Is.Zero);Assert.That((int)Get(f.match,"PhysicalUnloads"),Is.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);yield return null;yield return null;Assert.That(f.Rules.Players,Is.EqualTo(4));Assert.That(f.Rules.Delivered,Is.Zero);Assert.That(f.Rules.Scores.Sum(),Is.Zero);Assert.That(f.Rules.Stages.All(stage=>stage==ArtifactStage.Buried),Is.True);Assert.That((int)Get(f.Earth,"ActualEdits"),Is.Zero);CollectionAssert.AreEqual(bodies,f.Finds.Select(a=>(Rigidbody)Get(a,"body")).ToArray());
                foreach(var find in f.Finds){var body=(Rigidbody)Get(find,"body");Assert.That(body.isKinematic,Is.True);Assert.That(Vector3.Distance(body.position,(Vector3)Get(find,"seedPosition")),Is.LessThan(1e-5f));Assert.That((Joint)Get(find,"CargoJoint")==null,Is.True);}
                for(int owner=0;owner<4;owner++){Vector3 plot=(Vector3)Call(f.match,"Plot",owner);Assert.That(Surface(f.Earth,plot,out RaycastHit restoredSurface),Is.True);Assert.That(restoredSurface.point.y,Is.EqualTo(7).Within(.03f),"Supported actual Digger reset restores equal original competitive mounds");}
                TestContext.WriteLine("ARCHAEOLOGY_COMPLETE_UNTIMED_ALL12 "+evidencePath+"; full1500real budget follows measured~368sim first cycle and three configured finds/crew; original600case/failure retained, live1x unchanged. No Windows disk/human/device/performance acceptance inferred.");
            }
        }
    }
    [UnityTest,Timeout(120000)]public IEnumerator ActualEditedTerrainAndOriginalBodiesPauseExactlyThenResetToEqualCompetitivePlots()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var bodyIds=f.Finds.Select(a=>((Rigidbody)Get(a,"body")).GetInstanceID()).ToArray();var sample=(Vector3)Get(f.Finds[0],"seedPosition");Assert.That(Surface(f.Earth,sample,out RaycastHit before),Is.True);float originalY=before.point.y;
            yield return Until(f,()=>(int)Get(f.Earth,"ActualEdits")>4,60,"Actual original AI must excavate before pause");Call(f.match,"Pause",0,false);yield return null;yield return null;
            var poses=f.Finds.Select(a=>((Rigidbody)Get(a,"body")).position).ToArray();float clock=(float)Get(f.match,"Clock");var terrain=(Terrain)Get(f.Earth,"terrain");var holes=terrain.terrainData.GetHoles(0,0,terrain.terrainData.holesResolution,terrain.terrainData.holesResolution);int edits=(int)Get(f.Earth,"ActualEdits");yield return new WaitForSecondsRealtime(.25f);
            CollectionAssert.AreEqual(poses,f.Finds.Select(a=>((Rigidbody)Get(a,"body")).position).ToArray());CollectionAssert.AreEqual(holes,terrain.terrainData.GetHoles(0,0,terrain.terrainData.holesResolution,terrain.terrainData.holesResolution));Assert.That((float)Get(f.match,"Clock"),Is.EqualTo(clock));Assert.That((int)Get(f.Earth,"ActualEdits"),Is.EqualTo(edits));
            var error=Assert.Throws<TargetInvocationException>(()=>Call(f.match,"CaptureWorkshopJson"));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("Editor"),"Digger Editor-no-op persistence must never be represented as actual disk acceptance.");
            Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);yield return null;yield return null;Assert.That(Surface(f.Earth,sample,out RaycastHit reset),Is.True);Assert.That(reset.point.y,Is.EqualTo(originalY).Within(.02f));Assert.That(f.Rules.Scores.Sum(),Is.Zero);Assert.That(f.Rules.Delivered,Is.Zero);CollectionAssert.AreEqual(bodyIds,f.Finds.Select(a=>((Rigidbody)Get(a,"body")).GetInstanceID()).ToArray());
            for(int i=0;i<4;i++){var plot=(Vector3)Call(f.match,"Plot",i);Assert.That(Surface(f.Earth,plot,out RaycastHit top),Is.True);Assert.That(top.point.y,Is.EqualTo(7).Within(.03f),"All four competition mounds return to the same original terrain height.");}
        }
    }
    static void Source(object match,int i,string source){var p=((Array)Get(Get(match,"setup"),"players")).GetValue(i);var field=p.GetType().GetField("source");field.SetValue(p,Enum.Parse(field.FieldType,source));}
    static void Owners(object match,int count)
    {
        Key[] forward={Key.W,Key.UpArrow,Key.I,Key.T},left={Key.A,Key.LeftArrow,Key.J,Key.F},back={Key.S,Key.DownArrow,Key.K,Key.G},right={Key.D,Key.RightArrow,Key.L,Key.H};
        var players=(Array)Get(Get(match,"setup"),"players");Get(match,"setup").GetType().GetField("count").SetValue(Get(match,"setup"),count);
        for(int i=0;i<count;i++){Source(match,i,"Keyboard");var p=players.GetValue(i);p.GetType().GetField("forward").SetValue(p,forward[i]);p.GetType().GetField("left").SetValue(p,left[i]);p.GetType().GetField("back").SetValue(p,back[i]);p.GetType().GetField("right").SetValue(p,right[i]);}match.GetType().GetField("actionKeys").SetValue(match,new[]{Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.Numpad0,Key.Digit0,Key.PageUp});
    }
    [UnityTest,Timeout(150000)]public IEnumerator OneThroughFourAssignedOwnersRetainOriginalNwhDrivingPrivateViewsAndPhysicalToolSelection()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try
            {
                for(int count=1;count<=4;count++)
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Call(f.match,"SetupScreen");Owners(f.match,count);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Call(Get(f.match,"drivingViews"),"Select",1);yield return null;yield return null;
                    var rally=Get(Get(f.match,"drivingViews"),"rally");Assert.That((int)Get(rally,"Humans"),Is.EqualTo(count));CollectionAssert.AreEqual(Enumerable.Range(0,count).ToArray(),((int[])Get(rally,"owners")).Take(count).ToArray());var truck=((Array)Get(f.match,"trucks")).GetValue(0);var body=(Rigidbody)Get(truck,"body");Vector3 p=body.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();float target=(float)Get(f.match,"Clock")+2,deadline=Time.realtimeSinceStartup+8;while((float)Get(f.match,"Clock")<target&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Get(f.match,"Clock"),Is.GreaterThanOrEqualTo(target));Assert.That(Vector3.Distance(body.position,p),Is.GreaterThan(.5f));Assert.That(((Vector2[])Get(f.match,"commands"))[0].y,Is.GreaterThan(.9f));
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();yield return null;var select=f.match.GetType().GetMethod("SelectTool");Assert.That((bool)select.Invoke(f.match,new object[]{0,Enum.ToObject(select.GetParameters()[1].ParameterType,4)}),Is.True);Assert.That(((Array)Get(f.match,"Tools")).GetValue(0).ToString(),Is.EqualTo("Brush"));Capture(f,count+"-assigned-original-NWH-views");
                }
            }finally{InputSystem.RemoveDevice(keyboard);}
        }
    }
}
#endif
