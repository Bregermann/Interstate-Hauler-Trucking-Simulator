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

// Autonomous original contacts and possession; no injected scores or final outcomes.
public sealed class CargoPossessionScoreLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name) => owner.GetType().GetField(name,Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value) => owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] values) => owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==values.Length).Invoke(owner,values);
    [UnityTest,Timeout(420000)] public IEnumerator ActualWholeCargoDeliveryUsesConfiguredTeamName() => Inspect("TruckBuzkashi",360);
    [UnityTest,Timeout(300000)] public IEnumerator ActualDodgeballRoundCreditsOnlySurvivingTeam() => Inspect("TruckTrampolineDodgeball",240);
    [UnityTest,Timeout(300000)] public IEnumerator ArenaPossessionCelebratesOriginalSeconds() => Inspect("TruckKeepaway",240);
    [UnityTest,Timeout(300000)] public IEnumerator CabPossessionCelebratesOriginalSeconds() => Inspect("TruckKeepawayFirstPerson",240);
    static IEnumerator Inspect(string scene,float budget)
    {
        var preferences=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CargoPossessionActualAwards"});
        var isolation=new GameObject("Explicit autonomous cargo and possession fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match=null;
        try
        {
            string folder=scene=="TruckKeepawayFirstPerson"?"TruckKeepaway":scene;
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+folder+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));var setup=Get(match,"setup");
            bool buz=scene=="TruckBuzkashi",dodge=scene=="TruckTrampolineDodgeball";
            Set(setup,"count",buz?6:4);
            if(dodge){Set(match,"teamA",2);Set(match,"teamB",2);}
            if(buz)Set(match,"teams",new[]{"Cargo Crew Alpha","Cargo Crew Omega"});
            Call(match,"AllBots");
            foreach(var player in ((Array)Get(setup,"players")).Cast<object>())Set(player,"name","Same Driver Name");
            Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=4;var journal=Get(match,"ScoreJournal");Assert.That(Get(journal,"Count"),Is.Zero);
            float deadline=Time.realtimeSinceStartup+budget,next=0;
            while((int)Get(journal,"Count")==0&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+15;TestContext.WriteLine("ACTUAL_CARGO_PROGRESS "+scene+" "+Get(match,"Header")+" "+Get(match,"Status"));}
                yield return new WaitForFixedUpdate();
            }
            Assert.That((int)Get(journal,"Count"),Is.GreaterThan(0),"Original autonomous gameplay must commit a real delivery, surviving round or possession milestone.");
            ((Behaviour)match).enabled=false;Time.timeScale=0;var receipt=Get(journal,"Latest");int count=(int)Get(journal,"Count");
            Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));
            if(buz||dodge)
            {
                int team=(string)Get(receipt,"RecipientId")=="team:blue"?0:1;
                Assert.That(Get(receipt,"RecipientId"),Is.EqualTo(team==0?"team:blue":"team:orange"));
                Assert.That(Get(receipt,"IsTeam"),Is.True);Assert.That(Get(receipt,"ParticipantId"),Is.Null);Assert.That(Get(receipt,"Delta"),Is.EqualTo(1d));
                var totals=buz?(int[])Get(match,"score"):(int[])Get(Get(match,"rules"),"wins");Assert.That(Get(receipt,"Total"),Is.EqualTo((double)totals[team]));
                Assert.That(Get(receipt,"DisplayName"),Is.EqualTo(buz?((string[])Get(match,"teams"))[team]:team==0?"BLUE TEAM":"ORANGE TEAM"));
                if(buz){Assert.That(Get(match,"Deliveries"),Is.GreaterThan(0));Assert.That(totals.Sum(),Is.EqualTo((int)Get(match,"Deliveries")));}
                else Assert.That(Get(Get(match,"rules"),"RoundWinner"),Is.EqualTo(team));
            }
            else
            {
                int slot=(int)Get(receipt,"Slot");var seconds=(float[])Get(match,"possession");
                Assert.That(Get(receipt,"IsTeam"),Is.False);Assert.That(Get(receipt,"Unit"),Is.EqualTo("SEC"));
                Assert.That(Get(receipt,"Delta"),Is.EqualTo(10d));Assert.That(Get(receipt,"Total"),Is.EqualTo((double)seconds[slot]));Assert.That(seconds[slot],Is.InRange(10f,10.1f));
                Assert.That(Get(match,"Carrier"),Is.EqualTo(slot));Assert.That(Get(match,"Pickups"),Is.GreaterThan(0));
            }
            var presenter=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation"));Assert.That(presenter,Is.Not.Null);Time.timeScale=1;deadline=Time.realtimeSinceStartup+15;
            while((Get(presenter,"LastPresented")==null||(string)Get(Get(presenter,"LastPresented"),"EventId")!=(string)Get(receipt,"EventId"))&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Get(presenter,"LastPresented"),Is.SameAs(receipt));
            var animator=(Animator)Get(presenter,"notificationAnimator");float entrance=animator.runtimeAnimatorController.animationClips.Single(clip=>clip.name=="Notification_In").length;
            yield return new WaitForSeconds(entrance+.03f);Time.timeScale=0;yield return null;
            Assert.That(((CanvasGroup)Get(presenter,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(presenter,"notification")).gameObject.activeInHierarchy,Is.True);
            Assert.That(Get(journal,"Count"),Is.EqualTo(count),"Presentation cannot add points or receipts.");
            if(dodge)
            {
                var scores=UnityEngine.Object.FindAnyObjectByType(Runtime("SportsBroadcastScores"));var cards=(Array)Get(scores,"cards");var rules=Get(match,"rules");
                for(int slot=0;slot<(int)Get(setup,"count");slot++)
                {
                    int team=(int)Call(rules,"Team",slot);object[] values={slot,null,null};
                    Assert.That(match.GetType().GetMethod("TryReadScoreCard").Invoke(match,values),Is.True,"Typed card data must not depend on duplicate display names.");
                    Assert.That(values[1],Is.EqualTo(((int[])Get(rules,"wins"))[team].ToString()));
                    Assert.That((string)values[2],Does.StartWith(team==0?"BLUE TEAM":"ORANGE TEAM"));
                    var card=cards.GetValue(slot);Assert.That(Get(Get(card,"value"),"text"),Is.EqualTo(values[1]));Assert.That(Get(Get(card,"detail"),"text"),Is.EqualTo(values[2]));
                    var caption=Get(card,"detail");Call(caption,"ForceMeshUpdate",false,false);Assert.That(Get(caption,"isTextOverflowing"),Is.False,"Actual per-slot team/status caption must fit its authored card.");
                }
            }
            var save=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CargoPossessionAwards/"+scene+"-"+size.y+".png",null});
            string old=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;
            Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(old));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(presenter,"Presented"),Is.Zero);
            TestContext.WriteLine("ACTUAL_CARGO_POSSESSION_AWARD "+scene+" original autonomous contacts, original totals, duplicate names, readable720/1080 and clean rematch. Early lifecycle check; no full-match, human/device/listening or Windows claim.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);preferences.Dispose();}
    }
}
#endif
