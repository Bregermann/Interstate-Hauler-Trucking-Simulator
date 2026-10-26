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

public sealed class WifePairScoreLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
    static Rect Bounds(RectTransform rect){var p=new Vector3[4];rect.GetWorldCorners(p);return Rect.MinMaxRect(p[0].x,p[0].y,p[2].x,p[2].y);}
    public sealed class Receipts { public readonly List<object> items=new List<object>(); public void Record(object receipt)=>items.Add(receipt); }
    [UnityTest,Timeout(450000)]public IEnumerator ActualAutomaticRoleSwapKeepsOriginalPairPointsWithBothMembers()=>Inspect(false);
    [UnityTest,Timeout(450000)]public IEnumerator ActualManualRoleSwapAndFourAICamerasKeepSharedPairAwardsReadable()=>Inspect(true);
    static IEnumerator Inspect(bool split)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"WifeOriginalPairAwards"});Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);var isolation=new GameObject("Original physical cooperative pair scoring acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;EventInfo signal=null;Delegate subscription=null;var observed=new Receipts();
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Call(match,"AllBots");Set(match,"option",split?0:1);Set(match,"weather",1);Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{split});var setup=Get(match,"setup");var players=(Array)Get(setup,"players");foreach(var player in players.Cast<object>())Set(player,"difficulty",Enum.Parse(Get(player,"difficulty").GetType(),"Hard"));var names=(string[])Get(match,"teams");for(int i=0;i<4;i++)names[i]="Same Pair";
            var weather=Get(Get(match,"course"),"weather");var presets=Get(weather,"presets");if(presets!=null){var clear=((Array)Get(presets,"entries")).Cast<object>().FirstOrDefault(p=>(string)Get(p,"displayName")=="Clear");if(clear!=null)Call(presets,"Select",Get(clear,"id"));}
            signal=match.GetType().GetEvent("ScoreConfirmed");subscription=Delegate.CreateDelegate(signal.EventHandlerType,observed,typeof(Receipts).GetMethod("Record"));signal.AddEventHandler(match,subscription);Assert.That(Call(match,"StartMatch"),Is.True);Assert.That(names.All(n=>n=="Same Pair"),Is.True);string[] originalIds=Enumerable.Range(0,4).Select(i=>(string)Call(match,"PairScoreId",i)).ToArray();Assert.That(originalIds.Distinct().Count(),Is.EqualTo(4));
            for(int i=0;i<4;i++){StringAssert.Contains((string)Get(players.GetValue(i),"participantId"),originalIds[i]);StringAssert.Contains((string)Get(players.GetValue(i+4),"participantId"),originalIds[i]);}
            Time.timeScale=4;float deadline=Time.realtimeSinceStartup+360;
            while(observed.items.Count==0&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(observed.items.Count,Is.GreaterThan(0),"Original physical carried finishes must precede pair points.");
            if(split){Assert.That(Get(match,"Phase").ToString(),Is.EqualTo("RaceResult"));Call(match,"ExtraAction","swaproles");}
            else while(!(bool)Get(match,"Swapped")&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(Get(match,"Swapped"),Is.True);Assert.That(Call(match,"DriverRole",0),Is.EqualTo(4));Assert.That(Call(match,"PassengerRole",0),Is.EqualTo(0));((Behaviour)match).enabled=false;Time.timeScale=0;var series=Get(match,"series");var totals=(int[])((int[])Get(series,"total")).Clone();var sums=new double[4];var ids=new HashSet<string>();
            foreach(var receipt in observed.items)
            {
                string team=(string)Get(receipt,"TeamId");int pair=Array.IndexOf(originalIds,team);Assert.That(pair,Is.InRange(0,3));Assert.That(ids.Add((string)Get(receipt,"EventId")),Is.True);Assert.That(Get(receipt,"IsTeam"),Is.True);Assert.That(Get(receipt,"ParticipantId"),Is.Null);Assert.That(Get(receipt,"Slot"),Is.EqualTo(-1));Assert.That(Get(receipt,"RecipientId"),Is.EqualTo("team:"+team));Assert.That(Get(receipt,"DisplayName"),Is.EqualTo("Same Pair"));Assert.That(Get(receipt,"Color"),Is.EqualTo(Get(players.GetValue(pair),"color")));Assert.That(Get(receipt,"Unit"),Is.EqualTo("TEAM PTS"));Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));double delta=(double)Get(receipt,"Delta");Assert.That(delta,Is.InRange(1d,4d));sums[pair]+=delta;Assert.That(Get(receipt,"Total"),Is.EqualTo(sums[pair]));
            }
            for(int i=0;i<4;i++){Assert.That(Call(match,"PairScoreId",i),Is.EqualTo(originalIds[i]));Assert.That(sums[i],Is.EqualTo(totals[i]));object[] args={i,null,null};Assert.That(match.GetType().GetMethod("TryReadScoreCard").Invoke(match,args),Is.True);Assert.That(args[1],Is.EqualTo(totals[i].ToString()));}
            Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(observed.items.Count));var panels=((Component)Get(match,"menus")).GetComponentsInChildren(Runtime("SportsScorePresentation"),true).Cast<Component>().ToArray();Assert.That(panels.Length,Is.EqualTo(1));var panel=panels[0];Assert.That(Get(panel,"viewport"),Is.EqualTo(-1));Time.timeScale=1;deadline=Time.realtimeSinceStartup+25;while(Get(panel,"LastPresented")==null&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(observed.items,Does.Contain(Get(panel,"LastPresented")));var animator=(Animator)Get(panel,"notificationAnimator");yield return new WaitForSeconds(animator.runtimeAnimatorController.animationClips.Single(x=>x.name=="Notification_In").length+.03f);Time.timeScale=0;yield return null;Assert.That(((CanvasGroup)Get(panel,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(panel,"notification")).gameObject.activeInHierarchy,Is.True);
            var cameras=(Camera[])Get(match,"cameras");Assert.That(cameras.Length,Is.EqualTo(split?4:1));if(split){var viewPanels=(Array)Get(Get(match,"menus"),"viewPanels");for(int i=0;i<4;i++){var card=((Component)viewPanels.GetValue(i)).transform.Find("Compact owned viewport score") as RectTransform;Assert.That(card,Is.Not.Null);Assert.That(card.gameObject.activeInHierarchy,Is.True);Assert.That(Bounds((RectTransform)panel.transform).Overlaps(Bounds(card)),Is.False,"Shared pair award must clear existing owned viewport cards.");}}
            else{var cards=(Array)Get(UnityEngine.Object.FindAnyObjectByType(Runtime("SportsBroadcastScores")),"cards");for(int i=0;i<4;i++)Assert.That(Get(Get(cards.GetValue(i),"value"),"text"),Is.EqualTo(totals[i].ToString()),"Duplicate pair names must not copy another team's total.");}
            var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/WifePairAwards/actual-"+(split?"manual-four-AI-views":"automatic-shared")+"-"+size.y+".png",null});
            int count=observed.items.Count;Call(match,"Pause",0,false);float clock=(float)Get(match,"Clock");yield return new WaitForSecondsRealtime(.12f);Assert.That(Get(match,"Clock"),Is.EqualTo(clock));Assert.That(observed.items.Count,Is.EqualTo(count));Call(match,"Resume");string old=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;yield return null;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(old));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(panel,"LastPresented"),Is.Null);TestContext.WriteLine("ACTUAL_WIFE_PAIR_SCORES splitAI="+split+" receipts="+count+" totals="+string.Join(",",totals)+"; original physical carried finishes, both member IDs, actual legal role swap, duplicate team names, saved Heat, viewport-card clearance, pause and rematch. No injected score/finish/poses/forces; AI cameras are not human hardware testing.");
        }
        finally{Time.timeScale=1;if(match!=null){if(subscription!=null)signal.RemoveEventHandler(match,subscription);((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
