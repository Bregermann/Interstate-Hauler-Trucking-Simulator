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

public sealed class AirAttemptScoreLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
    [UnityTest,Timeout(360000)]public IEnumerator DockRealWaterDistanceNeverCreatesPoints()=>Inspect("TruckDockDiving",300);
    [UnityTest,Timeout(360000)]public IEnumerator SwingRealFirstContactUsesOriginalZonePoints()=>Inspect("TruckGiantSwing",300);
    static IEnumerator Inspect(string scene,float budget)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"AirActualAwards"});var isolation=new GameObject("Explicit original jump and pendulum outcome acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));var setup=Get(match,"setup");Set(setup,"count",4);Call(match,"AllBots");foreach(var player in ((Array)Get(setup,"players")).Cast<object>())Set(player,"name","Same Driver Name");Assert.That(Call(match,"StartMatch"),Is.True);
            Time.timeScale=4;var journal=Get(match,"ScoreJournal");Assert.That(Get(journal,"Count"),Is.Zero);float deadline=Time.realtimeSinceStartup+budget;
            while((int)Get(journal,"Count")==0&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That((int)Get(journal,"Count"),Is.GreaterThan(0),"An actual original physical landing must precede feedback.");((Behaviour)match).enabled=false;Time.timeScale=0;var receipt=Get(journal,"Latest");int slot=(int)Get(receipt,"Slot"),count=(int)Get(journal,"Count");Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));Assert.That(Get(receipt,"IsTeam"),Is.False);
            if(scene=="TruckDockDiving")
            {
                Assert.That(Get(receipt,"Kind").ToString(),Is.EqualTo("Outcome"));Assert.That(Get(receipt,"Unit"),Is.EqualTo("FT"));Assert.That(Get(receipt,"OutcomeValue"),Is.EqualTo((int)Get(match,"LastInches")/12d));Assert.That(Get(receipt,"Delta"),Is.EqualTo(0d));Assert.That(Get(receipt,"Total"),Is.EqualTo(0d));Assert.That(Get(match,"locked"),Is.True);Assert.That(Get(match,"Locks"),Is.GreaterThan(0));
            }
            else
            {
                Assert.That(Get(receipt,"Kind").ToString(),Is.EqualTo("Points"));Assert.That(Get(receipt,"Delta"),Is.EqualTo((double)(int)Get(match,"LockedScore")));Assert.That(Get(receipt,"Total"),Is.EqualTo((double)((int[])Get(match,"points"))[slot]));Assert.That((double)Get(receipt,"Delta"),Is.GreaterThan(0));Assert.That(Get(match,"Locks"),Is.GreaterThan(0));
            }
            var presenter=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation"));Assert.That(presenter,Is.Not.Null);Time.timeScale=1;deadline=Time.realtimeSinceStartup+25;while((Get(presenter,"LastPresented")==null||(string)Get(Get(presenter,"LastPresented"),"EventId")!=(string)Get(receipt,"EventId"))&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(Get(presenter,"LastPresented"),Is.SameAs(receipt));
            var animator=(Animator)Get(presenter,"notificationAnimator");float entrance=animator.runtimeAnimatorController.animationClips.Single(p=>p.name=="Notification_In").length;yield return new WaitForSeconds(entrance+.03f);Time.timeScale=0;yield return null;Assert.That(((CanvasGroup)Get(presenter,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(presenter,"notification")).gameObject.activeInHierarchy,Is.True);Assert.That(Get(journal,"Count"),Is.EqualTo(count));
            var scoreCards=UnityEngine.Object.FindAnyObjectByType(Runtime("SportsBroadcastScores"));var cards=(Array)Get(scoreCards,"cards");
            for(int cardSlot=0;cardSlot<(int)Get(setup,"count");cardSlot++)
            {
                object[] args={cardSlot,null,null};Assert.That(match.GetType().GetMethod("TryReadScoreCard").Invoke(match,args),Is.True);
                int expected=scene=="TruckDockDiving"?((int[])Get(match,"best"))[cardSlot]/12:((int[])Get(match,"points"))[cardSlot];Assert.That(args[1],Is.EqualTo(expected.ToString()));
                var card=cards.GetValue(cardSlot);Assert.That(Get(Get(card,"value"),"text"),Is.EqualTo(args[1]));Assert.That(Get(Get(card,"detail"),"text"),Is.EqualTo(args[2]));Call(Get(card,"detail"),"ForceMeshUpdate",false,false);Assert.That(Get(Get(card,"detail"),"isTextOverflowing"),Is.False);
            }
            var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/AirAttemptAwards/"+scene+"-"+size.y+".png",null});
            string previous=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(previous));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(presenter,"Presented"),Is.Zero);TestContext.WriteLine("ACTUAL_AIR_AWARD "+scene+" original physical outcome, exact units/total, duplicate names, visible Heat and fresh rematch. Early lifecycle only; not full Windows/human/device acceptance.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
}
#endif
