#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RaceEndScoreLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
    public sealed class Receipts { public readonly List<object> items=new List<object>(); public void Record(object value)=>items.Add(value); }
    [UnityTest,Timeout(420000)]public IEnumerator MinefieldActualSupportedRestAwardsOnlyOriginalHeatLeaders()=>Inspect("TruckMinefield");
    [UnityTest,Timeout(420000)]public IEnumerator MattressActualFinishesPublishOnlyOriginalPlacementPoints()=>Inspect("TruckMattressRacing");
    [UnityTest,Timeout(420000)]public IEnumerator TerrierActualLegalFinishPublishesOnlyOriginalHeatWin()=>Inspect("TruckTerrierRacing");
    static IEnumerator Inspect(string scene)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"RaceEndActualScores"});var isolation=new GameObject("Original physical race-score acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);Component match=null;Delegate subscription=null;var receipts=new Receipts();EventInfo signal=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));var setup=Get(match,"setup");Call(match,"AllBots");Set(match,"option",scene=="TruckMinefield"?0:1);Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
            var players=(Array)Get(setup,"players");for(int i=0;i<(int)Get(setup,"count");i++){var player=players.GetValue(i);var identity=Runtime("SportsParticipantRegistry").GetMethod("Default").Invoke(null,new object[]{i});Runtime("SportsParticipantRegistry").GetMethod("Rename").Invoke(null,new object[]{Get(identity,"id"),"Same Racing Name",Get(player,"color"),true});Set(player,"difficulty",Enum.Parse(Get(player,"difficulty").GetType(),"Hard"));}Call(match,"RefreshParticipants",false);Assert.That(players.Cast<object>().Take((int)Get(setup,"count")).All(p=>(string)Get(p,"name")=="Same Racing Name"),Is.True,"Use the real Your Crew rename flow before testing duplicate identities.");
            signal=match.GetType().GetEvent("ScoreConfirmed");subscription=Delegate.CreateDelegate(signal.EventHandlerType,receipts,typeof(Receipts).GetMethod("Record"));signal.AddEventHandler(match,subscription);
            Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=4;var journal=Get(match,"ScoreJournal");Assert.That(Get(journal,"Count"),Is.Zero);float deadline=Time.realtimeSinceStartup+300;
            while(receipts.items.Count==0&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(receipts.items.Count,Is.GreaterThan(0),"Only original actual supported rest/finish/race resolution may produce a receipt.");((Behaviour)match).enabled=false;Time.timeScale=0;
            int competitors=(int)Get(setup,"count");var totals=scene=="TruckMattressRacing"?(int[])Get(Get(match,"series"),"total"):(int[])Get(match,scene=="TruckMinefield"?"points":"wins");totals=(int[])totals.Clone();var accumulated=new double[competitors];var ids=new HashSet<string>();var identities=new HashSet<string>();bool mile=false;
            foreach(var receipt in receipts.items)
            {
                int slot=(int)Get(receipt,"Slot");Assert.That(slot,Is.InRange(0,competitors-1));Assert.That(ids.Add((string)Get(receipt,"EventId")),Is.True);Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));Assert.That(Get(receipt,"DisplayName"),Is.EqualTo("Same Racing Name"));Assert.That(Get(receipt,"IsTeam"),Is.False);Assert.That(identities.Add((string)Get(receipt,"ParticipantId")),Is.True,"First resolved heat/race awards each actual participant once, regardless of duplicate names.");
                var racers=(Array)Get(match,"racers");var racer=racers.GetValue(slot);
                if(Get(receipt,"Kind").ToString()=="Outcome")
                {
                    mile=true;Assert.That(scene,Is.EqualTo("TruckMinefield"));Assert.That(Get(racer,"Phase").ToString(),Is.EqualTo("DrivenFinish"));Assert.That(Get(receipt,"OutcomeValue"),Is.EqualTo((double)(float)Get(racer,"FinishTime")));Assert.That(Get(receipt,"Unit"),Is.EqualTo("SEC"));Assert.That(Get(receipt,"Delta"),Is.EqualTo(0d));Assert.That(Get(receipt,"Total"),Is.EqualTo(0d));
                }
                else
                {
                    double delta=(double)Get(receipt,"Delta");Assert.That(delta,Is.GreaterThan(0));accumulated[slot]+=delta;Assert.That(Get(receipt,"Total"),Is.EqualTo((double)totals[slot]));
                    if(scene=="TruckMinefield"){Assert.That(delta,Is.EqualTo(1));Assert.That((int)Get(match,"LastLeaders")&(1<<slot),Is.Not.Zero);Assert.That((int)Get(match,"HeatLocks"),Is.GreaterThan(0));}
                    if(scene=="TruckMattressRacing"){int rank=((int[])Get(Get(match,"series"),"ranks"))[slot];Assert.That(Get(racer,"Phase").ToString(),Is.EqualTo("Finished"));Assert.That(delta,Is.EqualTo(competitors-rank+1));}
                    if(scene=="TruckTerrierRacing"){Assert.That(delta,Is.EqualTo(1));Assert.That(Get(receipt,"Unit"),Is.EqualTo("WIN"));Assert.That(slot,Is.EqualTo(Get(match,"HeatWinner")));Assert.That(Get(racer,"Finished"),Is.True);}
                }
            }
            if(!mile)for(int i=0;i<competitors;i++)Assert.That(accumulated[i],Is.EqualTo(totals[i]),"Every first original point is represented exactly once; no invented points for DNF/reruns.");Assert.That(Get(journal,"Count"),Is.EqualTo(receipts.items.Count));
            var panel=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation"));Assert.That(panel,Is.Not.Null);Assert.That(Get(panel,"viewport"),Is.EqualTo(-1));Time.timeScale=1;deadline=Time.realtimeSinceStartup+25;while(Get(panel,"LastPresented")==null&&Time.realtimeSinceStartup<deadline)yield return null;var shown=Get(panel,"LastPresented");Assert.That(receipts.items,Does.Contain(shown));
            var animator=(Animator)Get(panel,"notificationAnimator");yield return new WaitForSeconds(animator.runtimeAnimatorController.animationClips.Single(x=>x.name=="Notification_In").length+.03f);Time.timeScale=0;yield return null;Assert.That(((CanvasGroup)Get(panel,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(panel,"notification")).gameObject.activeInHierarchy,Is.True);
            if(scene=="TruckMattressRacing"){var cardPresenter=UnityEngine.Object.FindAnyObjectByType(Runtime("SportsBroadcastScores"));var cards=(Array)Get(cardPresenter,"cards");for(int i=0;i<competitors;i++){object[] args={i,null,null};Assert.That(match.GetType().GetMethod("TryReadScoreCard").Invoke(match,args),Is.True);Assert.That(args[1],Is.EqualTo(totals[i].ToString()));var card=cards.GetValue(i);Assert.That(Get(Get(card,"value"),"text"),Is.EqualTo(args[1]),"Rendered card must use its own original total despite duplicate names.");Assert.That(Get(Get(card,"detail"),"text"),Is.EqualTo(args[2]));}}
            var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/RaceEndAwards/"+scene+"-"+size.y+".png",null});
            int count=receipts.items.Count;Call(match,"Pause",0,false);yield return new WaitForSecondsRealtime(.1f);Assert.That(receipts.items.Count,Is.EqualTo(count));Call(match,"Resume");string previous=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(previous));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(panel,"LastPresented"),Is.Null);
            TestContext.WriteLine("ORIGINAL_RACE_END_SCORE "+scene+" actual first physical race/heat resolution, receipts="+count+", original points="+string.Join(",",totals)+", stable duplicate-name identity, saved Heat, pause and rematch. No injected scores, finishes, poses or forces. Full Windows/human/device acceptance separate.");
        }
        finally{Time.timeScale=1;if(match!=null){if(subscription!=null)signal.RemoveEventHandler(match,subscription);((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
