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

// Original autonomous buoyancy, ordered checkpoints and water contacts; no injected outcome.
public sealed class WaterContestScoreLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);
    [UnityTest,Timeout(420000)] public IEnumerator OriginalBogPlacementCommitsActualPoints()=>Inspect("TruckBogSnorkelling",360);
    [UnityTest,Timeout(300000)] public IEnumerator OriginalLogWaterFallsCreditSoleSurvivor()=>Inspect("TruckLogRolling",240);
    static IEnumerator Inspect(string scene,float budget)
    {
        var preferences=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"WaterContestActualAwards"});
        var isolation=new GameObject("Explicit autonomous water-contest award fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));var setup=Get(match,"setup");Set(setup,"count",4);Set(match,"option",0);Call(match,"AllBots");
            foreach(var player in ((Array)Get(setup,"players")).Cast<object>())Set(player,"name","Same Driver Name");
            Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=4;var journal=Get(match,"ScoreJournal");Assert.That(Get(journal,"Count"),Is.Zero);float deadline=Time.realtimeSinceStartup+budget,next=0;
            while((int)Get(journal,"Count")==0&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+15;TestContext.WriteLine("ACTUAL_WATER_PROGRESS "+scene+" "+Get(match,"Header")+" "+Get(match,"Status"));}
                yield return new WaitForFixedUpdate();
            }
            Assert.That((int)Get(journal,"Count"),Is.GreaterThan(0),"Original autonomous gameplay must commit a real completed placement or surviving heat.");
            ((Behaviour)match).enabled=false;Time.timeScale=0;var receipt=Get(journal,"Latest");int count=(int)Get(journal,"Count"),slot=(int)Get(receipt,"Slot");
            Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));Assert.That(Get(receipt,"IsTeam"),Is.False);
            if(scene=="TruckBogSnorkelling")
            {
                var series=Get(match,"series");var totals=(int[])Get(series,"total");Assert.That(Get(receipt,"Total"),Is.EqualTo((double)totals[slot]));Assert.That(Get(receipt,"Delta"),Is.EqualTo((double)totals[slot]));
                Assert.That(Get(series,"locked"),Is.True);Assert.That(((int[])Get(series,"ranks"))[slot],Is.GreaterThan(0));Assert.That(float.IsInfinity(((float[])Get(match,"times"))[slot]),Is.False);
            }
            else
            {
                var rules=Get(match,"rules");Assert.That(Get(rules,"HeatWinner"),Is.EqualTo(slot));Assert.That(Get(receipt,"Delta"),Is.EqualTo(1d));Assert.That(Get(receipt,"Total"),Is.EqualTo((double)((int[])Get(rules,"wins"))[slot]));Assert.That(Get(match,"Falls"),Is.GreaterThan(0));
            }
            var presenter=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation"));Assert.That(presenter,Is.Not.Null);Time.timeScale=1;deadline=Time.realtimeSinceStartup+25;
            while((Get(presenter,"LastPresented")==null||(string)Get(Get(presenter,"LastPresented"),"EventId")!=(string)Get(receipt,"EventId"))&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Get(presenter,"LastPresented"),Is.SameAs(receipt));
            var animator=(Animator)Get(presenter,"notificationAnimator");float entrance=animator.runtimeAnimatorController.animationClips.Single(clip=>clip.name=="Notification_In").length;yield return new WaitForSeconds(entrance+.03f);Time.timeScale=0;yield return null;
            Assert.That(((CanvasGroup)Get(presenter,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(presenter,"notification")).gameObject.activeInHierarchy,Is.True);Assert.That(Get(journal,"Count"),Is.EqualTo(count));
            var scoreCards=UnityEngine.Object.FindAnyObjectByType(Runtime("SportsBroadcastScores"));var cards=(Array)Get(scoreCards,"cards");
            for(int cardSlot=0;cardSlot<(int)Get(setup,"count");cardSlot++)
            {
                object[] values={cardSlot,null,null};Assert.That(match.GetType().GetMethod("TryReadScoreCard").Invoke(match,values),Is.True,"Typed slot source must not parse duplicate display names.");
                int expected=scene=="TruckBogSnorkelling"?((int[])Get(Get(match,"series"),"total"))[cardSlot]:((int[])Get(Get(match,"rules"),"wins"))[cardSlot];Assert.That(values[1],Is.EqualTo(expected.ToString()));
                var card=cards.GetValue(cardSlot);Assert.That(Get(Get(card,"value"),"text"),Is.EqualTo(values[1]));Assert.That(Get(Get(card,"detail"),"text"),Is.EqualTo(values[2]));var caption=Get(card,"detail");Call(caption,"ForceMeshUpdate",false,false);Assert.That(Get(caption,"isTextOverflowing"),Is.False);
                if(scene=="TruckLogRolling")Assert.That(((string)values[2]).Contains("IN WATER"),Is.EqualTo(!((bool[])Get(Get(match,"rules"),"alive"))[cardSlot]));
            }
            var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/WaterContestAwards/"+scene+"-"+size.y+".png",null});
            string prior=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(prior));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(presenter,"Presented"),Is.Zero);
            TestContext.WriteLine("ACTUAL_WATER_CONTEST_AWARD "+scene+" original autonomous physical result, exact total, duplicate names, readable720/1080 and fresh rematch. Early lifecycle only; not full-match, human/device/listening or Windows acceptance.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);preferences.Dispose();}
    }
}
#endif
