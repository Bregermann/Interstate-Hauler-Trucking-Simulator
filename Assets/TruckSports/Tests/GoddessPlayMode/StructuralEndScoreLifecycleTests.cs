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

// Real autonomous scoring, no injected damage, final positions, clocks or points.
public sealed class StructuralEndScoreLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name) => owner.GetType().GetField(name,Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value) => owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] values) => owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==values.Length).Invoke(owner,values);
    [UnityTest,Timeout(240000)] public IEnumerator ActualStructuralCollapsePublishesItsOriginalModuleBudgetOnce() => Inspect("TruckBungeeDemolition",180);
    [UnityTest,Timeout(720000)] public IEnumerator ActualSettledCurlingEndCreditsOnlyItsTeam() => Inspect("TruckCurling",600);
    [UnityTest,Timeout(240000)] public IEnumerator ActualDerbySurvivorPublishesOneHeatWin() => Inspect("TruckDemolitionDerby",180);
    static IEnumerator Inspect(string scene,float budget)
    {
        var preferences=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"StructuralEndAwardContacts"});
        var isolation=new GameObject("Explicit autonomous actual-award fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));var setup=Get(match,"setup");
            if(scene=="TruckCurling"){Set(match,"option",0);Set(match,"teamA",1);Set(match,"teamB",1);}
            Set(setup,"count",scene=="TruckCurling"?2:4);Call(match,"AllBots");
            foreach(var player in ((Array)Get(setup,"players")).Cast<object>())Set(player,"name","Same Driver Name");
            Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=4;var journal=Get(match,"ScoreJournal");Assert.That(Get(journal,"Count"),Is.Zero);
            float deadline=Time.realtimeSinceStartup+budget,next=0;
            while((int)Get(journal,"Count")==0&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+15;TestContext.WriteLine("ACTUAL_AWARD_PROGRESS "+scene+" "+Get(match,"Header")+" "+Get(match,"Status"));}
                yield return new WaitForFixedUpdate();
            }
            Assert.That((int)Get(journal,"Count"),Is.GreaterThan(0),"Natural original NWH/PhysX/Core-Collapse gameplay must commit a genuine award.");
            ((Behaviour)match).enabled=false;Time.timeScale=0;var receipt=Get(journal,"Latest");int count=(int)Get(journal,"Count");
            Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));Assert.That((double)Get(receipt,"Delta"),Is.GreaterThan(0));
            if(scene=="TruckBungeeDemolition")
            {
                int slot=(int)Get(receipt,"Slot");var bay=((Array)Get(match,"bays")).GetValue(slot);
                Assert.That(Get(receipt,"Total"),Is.EqualTo((double)(((int[])Get(match,"total"))[slot]+(int)Get(bay,"Score"))));
                var module=((Array)Get(bay,"modules")).Cast<object>().First(item=>(bool)Get(item,"awarded"));
                int before=(int)Get(bay,"Score");Call(bay,"Award",module);Assert.That(Get(bay,"Score"),Is.EqualTo(before));Assert.That(Get(journal,"Count"),Is.EqualTo(count));
                Assert.That(Get(receipt,"IsTeam"),Is.False);
            }
            else if(scene=="TruckCurling")
            {
                var totals=(int[])Get(match,"scores");int team=(string)Get(receipt,"RecipientId")=="team:blue"?0:1;
                Assert.That(Get(receipt,"IsTeam"),Is.True);Assert.That(Get(receipt,"ParticipantId"),Is.Null);
                Assert.That(Get(receipt,"Total"),Is.EqualTo((double)totals[team]));Assert.That(Get(receipt,"Delta"),Is.EqualTo((double)totals[team]));
                Assert.That(Get(receipt,"Tier").ToString(),Is.EqualTo("RoundWinner"));
            }
            else
            {
                var rules=Get(match,"rules");int slot=(int)Get(receipt,"Slot");Assert.That(Get(rules,"HeatWinner"),Is.EqualTo(slot));
                Assert.That(Get(receipt,"Delta"),Is.EqualTo(1d));Assert.That(Get(receipt,"Total"),Is.EqualTo((double)((int[])Get(rules,"wins"))[slot]));Assert.That(Get(receipt,"IsTeam"),Is.False);
            }
            var presenter=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation"));Assert.That(presenter,Is.Not.Null);Time.timeScale=1;deadline=Time.realtimeSinceStartup+15;
            while((Get(presenter,"LastPresented")==null||(string)Get(Get(presenter,"LastPresented"),"EventId")!=(string)Get(receipt,"EventId"))&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Get(presenter,"LastPresented"),Is.SameAs(receipt));
            var animator=(Animator)Get(presenter,"notificationAnimator");float entrance=animator.runtimeAnimatorController.animationClips.Single(clip=>clip.name=="Notification_In").length;
            yield return new WaitForSeconds(entrance+.03f);Time.timeScale=0;yield return null;
            Assert.That(((CanvasGroup)Get(presenter,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(presenter,"notification")).gameObject.activeInHierarchy,Is.True);
            Assert.That(Get(journal,"Count"),Is.EqualTo(count),"Presentation cannot add points or receipts.");
            var save=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/StructuralEndAwards/"+scene+"-"+size.y+".png",null});
            string old=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;
            Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(old));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(presenter,"Presented"),Is.Zero);
            TestContext.WriteLine("ACTUAL_STRUCTURAL_END_AWARD "+scene+" original autonomous contact/settled result, original totals, duplicate names, readable720/1080 and fresh rematch. Not a complete-match, human/device/listening or Windows claim.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);preferences.Dispose();}
    }
}
#endif
