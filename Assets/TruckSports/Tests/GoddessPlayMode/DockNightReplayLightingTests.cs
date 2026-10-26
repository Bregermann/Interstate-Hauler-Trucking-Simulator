#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Original physical flights and recorded-geometry lighting, without supplied scores.
public sealed class DockNightReplayLightingTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value){if(owner.GetType().GetField(name,Any) is FieldInfo field)field.SetValue(owner,value);else owner.GetType().GetProperty(name,Any).SetValue(owner,value);}
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(x=>x)).Invoke(owner,args);
    [UnityTest,Timeout(150000)]
    public IEnumerator ActualNightDockReplayKeepsExistingBeamsOnTheRecordedTruck() => VerifyVenue(true);
    [UnityTest,Timeout(150000)]
    public IEnumerator ActualNightSwingReplayKeepsExistingBeamsOnTheRecordedTruck() => VerifyVenue(false);
    IEnumerator VerifyVenue(bool dock)
    {
        string scene=dock ? "TruckDockDiving" : "TruckGiantSwing";
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"NightReplayLighting"+scene});
        var isolation=new GameObject("Explicit original Dock night replay lighting reproduction");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null,new object[]{Enum.Parse(Runtime("SportsTimeChoice"),"Night")});
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType(dock ? "LWS.TruckDockDiving.DockMatch, Assembly-CSharp" : "LWS.TruckGiantSwing.SwingMatch, Assembly-CSharp",true));Set(Get(match,"setup"),"count",1);Call(match,"AllBots");
            var catalog=Get(Get(match,dock ? "presentationWeather" : "weatherField"),"presets");var clear=((Array)Get(catalog,"entries")).Cast<object>().Single(entry=>(string)Get(entry,"displayName")=="Clear");Call(catalog,"Select",Get(clear,"id"));Assert.That(Call(match,"StartMatch"),Is.True);var replay=(Component)Get(match,"replay");float deadline=Time.realtimeSinceStartup+65;
            while(!(bool)Get(replay,"Playing")&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Get(replay,"Playing"),Is.True,"Original AI approach, physical launch/water contact and original recording must reach replay; no supplied result or fabricated replay.");
            Time.timeScale=0;if(dock)Call(replay,"Rewind");else Call(replay,"Seek",0f);yield return null;yield return null;
            var truck=(Component)Get(match,"truck");var ghost=dock ? (Transform)Get(replay,"ghost") : ((Transform[])Get(replay,"ghosts"))[0];var lighting=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsNightHeadlights"));
            var beams=((Array)Get(lighting,"beams")).Cast<object>().Where(beam=>(Transform)Get(beam,"target")==truck.transform).ToArray();Assert.That(beams.Length,Is.GreaterThan(0),"Exercise the saved original vehicle beam bindings.");
            Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,"Logs/TruckSports/PlaytestRepair/NightReplayLighting/"+scene+"-night-recorded-truck-720.png",null});
            var anchor=dock ? truck.transform : ((Transform[])Get(replay,"sources"))[0];
            foreach(var beam in beams){var light=(Light)Get(beam,"light");var local=(Vector3)Get(beam,"position");var expected=ghost.TransformPoint(anchor.InverseTransformPoint(truck.transform.TransformPoint(local)));float distance=Vector3.Distance(light.transform.position,expected);TestContext.WriteLine("ORIGINAL_NIGHT_REPLAY beam="+light.name+" enabled="+light.enabled+" recordedPoseDistance="+distance+" recordedAnchor="+anchor.name);Assert.That(light.enabled,Is.True);Assert.That(distance,Is.LessThan(.1f),"Existing beam must follow the actual recorded root/visual-child frame, preserving its authored scale and offset.");Assert.That(Quaternion.Angle(light.transform.rotation,ghost.rotation*Quaternion.Inverse(anchor.rotation)*truck.transform.rotation*(Quaternion)Get(beam,"rotation")),Is.LessThan(.1f));}
            yield return VerifyLifecycle(match,replay,truck,ghost,lighting,beams,dock,scene,catalog);
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
    static IEnumerator VerifyLifecycle(Component match,Component replay,Component truck,Transform ghost,Component lighting,object[] beams,bool dock,string scene,object catalog)
    {
        var originalPoints=(int[])((int[])Get(match,dock ? "best" : "points")).Clone();
        var anchor=dock ? truck.transform : ((Transform[])Get(replay,"sources"))[0];
        int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");var ids=beams.Select(b=>((Light)Get(b,"light")).GetInstanceID()).ToArray();
        Assert.That(ghost.GetComponentsInChildren<Light>(true),Is.Empty,"Recorded hierarchies must not gain light components.");
        Call(match,"Pause",0,false);yield return new WaitForSecondsRealtime(.3f);Assert.That(Get(match,"Paused"),Is.True);
        foreach(var beam in beams)Assert.That(Vector3.Distance(((Light)Get(beam,"light")).transform.position,ghost.TransformPoint(anchor.InverseTransformPoint(truck.transform.TransformPoint((Vector3)Get(beam,"position"))))),Is.LessThan(.1f));
        Call(match,"Resume");Time.timeScale=0;if(dock)Call(replay,"Rewind");else Call(replay,"Seek",0f);yield return null;yield return null;
        Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5).Invoke(null,new object[]{match,1920,1080,"Logs/TruckSports/PlaytestRepair/NightReplayLighting/"+scene+"-night-rewound-truck-1080.png",null});
        Assert.That((int[])Get(match,dock ? "best" : "points"),Is.EqualTo(originalPoints));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));
        Call(replay,dock ? "Stop" : "End");yield return new WaitForSecondsRealtime(.3f);
        foreach(var beam in beams)Assert.That(Vector3.Distance(((Light)Get(beam,"light")).transform.position,truck.transform.TransformPoint((Vector3)Get(beam,"position"))),Is.LessThan(.1f));
        Call(match,"SetupScreen");yield return new WaitForSecondsRealtime(.3f);Assert.That(beams.All(b=>!((Light)Get(b,"light")).enabled),Is.True,"Setup retires prior playback lighting.");
        string before=(string)Get(match,"MatchId");Assert.That(Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.3f);Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(before));Assert.That(Get(replay,"Playing"),Is.False);Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);
        foreach(var beam in beams)Assert.That(Vector3.Distance(((Light)Get(beam,"light")).transform.position,truck.transform.TransformPoint((Vector3)Get(beam,"position"))),Is.LessThan(.1f));
        Call(match,"SetupScreen");Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null,new object[]{Enum.Parse(Runtime("SportsTimeChoice"),"Day")});Assert.That(Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.3f);
        Assert.That(beams.All(b=>!((Light)Get(b,"light")).enabled),Is.True,"Day rematch must not retain night replay beams.");Assert.That(beams.Select(b=>((Light)Get(b,"light")).GetInstanceID()),Is.EqualTo(ids));
        TestContext.WriteLine(scene+" original physical recording, pause/rewind,720p/1080p controlled frames, no score mutation, replay exit/live fallback, fresh rematch, Day off and original light identities passed. Not human or Windows acceptance.");
    }
}
#endif
