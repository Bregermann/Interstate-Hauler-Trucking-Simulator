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

// Original autonomous NWH/PhysX outcomes; no injected scoring, hits or finishes.
public sealed class StuntScoreLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
    [UnityTest,Timeout(300000)]public IEnumerator LimboOriginalBarClearPublishesOnce()=>Inspect("TruckLimbo",240);
    [UnityTest,Timeout(300000)]public IEnumerator SkyTowPhysicalRingKeepsOriginalBonus()=>Inspect("TruckSkyTow",240);
    [UnityTest,Timeout(420000)]public IEnumerator TrailerPlugBanksOriginalDropOnlyAfterResolution()=>Inspect("TruckTrailerPlug",360);
    static IEnumerator Inspect(string scene,float budget)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"StuntActualAwards"});
        var isolation=new GameObject("Explicit original physical stunt award acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));var setup=Get(match,"setup");Set(setup,"count",4);Set(match,"option",0);Call(match,"AllBots");
            var catalog=Get(match,scene=="TruckLimbo"?"weatherPresets":"presets");var forecasts=((IEnumerable)Get(catalog,"entries")).Cast<object>();var clear=forecasts.First(p=>(string)Get(p,"displayName")=="Clear");Call(catalog,"Select",Get(clear,"id"));
            foreach(var player in ((Array)Get(setup,"players")).Cast<object>())Set(player,"name","Same Driver Name");
            Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=4;var journal=Get(match,"ScoreJournal");Assert.That(Get(journal,"Count"),Is.Zero);float deadline=Time.realtimeSinceStartup+budget,next=0;
            while((int)Get(journal,"Count")==0&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+15;TestContext.WriteLine("ACTUAL_STUNT_PROGRESS "+scene+" "+Get(match,"Header")+" "+Get(match,"Status"));}
                yield return new WaitForFixedUpdate();
            }
            Assert.That((int)Get(journal,"Count"),Is.GreaterThan(0),"A genuine original physical outcome must commit before notification acceptance.");
            ((Behaviour)match).enabled=false;Time.timeScale=0;var receipt=Get(journal,"Latest");int count=(int)Get(journal,"Count"),slot=(int)Get(receipt,"Slot");var rules=Get(match,"rules");var totals=(int[])Get(rules,scene=="TruckLimbo"?"points":"score");
            Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));Assert.That(Get(receipt,"IsTeam"),Is.False);Assert.That(Get(receipt,"Total"),Is.EqualTo((double)totals[slot]));Assert.That((double)Get(receipt,"Delta"),Is.GreaterThan(0));
            if(scene=="TruckLimbo"){Assert.That(((int[])Get(rules,"clears"))[slot],Is.GreaterThan(0));Assert.That(((bool[])Get(rules,"done"))[slot],Is.True);Assert.That(Get(match,"PhysicalClears"),Is.GreaterThan(0));}
            else if(scene=="TruckSkyTow"){Assert.That(((int[])Get(rules,"hits"))[slot],Is.GreaterThan(0));Assert.That(Get(match,"PhysicalRings"),Is.GreaterThan(0));}
            else{Assert.That(Get(rules,"Resolved"),Is.True);Assert.That(Get(receipt,"Delta"),Is.EqualTo((double)(int)Get(rules,"AttemptPoints")));Assert.That(Get(match,"PhysicalReleases"),Is.GreaterThan(0));}
            var presenter=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation"));Assert.That(presenter,Is.Not.Null);Time.timeScale=1;deadline=Time.realtimeSinceStartup+35;
            while((Get(presenter,"LastPresented")==null||(string)Get(Get(presenter,"LastPresented"),"EventId")!=(string)Get(receipt,"EventId"))&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Get(presenter,"LastPresented"),Is.SameAs(receipt));var animator=(Animator)Get(presenter,"notificationAnimator");float entrance=animator.runtimeAnimatorController.animationClips.Single(p=>p.name=="Notification_In").length;yield return new WaitForSeconds(entrance+.03f);Time.timeScale=0;yield return null;
            Assert.That(((CanvasGroup)Get(presenter,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(presenter,"notification")).gameObject.activeInHierarchy,Is.True);Assert.That(Get(journal,"Count"),Is.EqualTo(count));
            var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/StuntAwards/"+scene+"-"+size.y+".png",null});
            string previous=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(previous));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(presenter,"Presented"),Is.Zero);
            TestContext.WriteLine("ACTUAL_STUNT_AWARD "+scene+" original physical outcome, stable slot/total, visible Heat notification and rematch. Early native lifecycle only, not a complete Windows/human/device claim.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
}
#endif
