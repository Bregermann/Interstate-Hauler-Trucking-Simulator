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

public sealed class TargetRestScoreLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);
    static Rect Bounds(RectTransform rect){var p=new Vector3[4];rect.GetWorldCorners(p);return Rect.MinMaxRect(p[0].x,p[0].y,p[2].x,p[2].y);}
    public sealed class Receipts{public readonly List<object> items=new List<object>();public void Record(object receipt)=>items.Add(receipt);}
    [UnityTest,Timeout(360000)]public IEnumerator OriginalPhysicalFinalRestAwardsOnceInSharedView()=>Inspect(false);
    [UnityTest,Timeout(360000)]public IEnumerator OriginalPhysicalFinalRestKeepsFourAICardsClear()=>Inspect(true);
    static IEnumerator Inspect(bool split)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"OriginalTargetFinalRestAwards"});Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);var isolation=new GameObject("Original final supported rest score acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;EventInfo signal=null;Delegate subscription=null;var observed=new Receipts();
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Call(match,"AllBots");Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{split});var players=(Array)Get(Get(match,"setup"),"players");foreach(var p in players.Cast<object>())Set(p,"difficulty",Enum.Parse(Get(p,"difficulty").GetType(),"Hard"));Set(match,"seed",42761);
            signal=match.GetType().GetEvent("ScoreConfirmed");subscription=Delegate.CreateDelegate(signal.EventHandlerType,observed,typeof(Receipts).GetMethod("Record"));signal.AddEventHandler(match,subscription);Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=4;float deadline=Time.realtimeSinceStartup+240;
            while(observed.items.Count==0&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(observed.items.Count,Is.GreaterThan(0),"A genuine original supported final rest must precede any positive receipt.");((Behaviour)match).enabled=false;Time.timeScale=0;var rules=Get(match,"rules");var totals=(int[])((int[])Get(rules,"totals")).Clone();var sums=new double[4];var ids=new HashSet<string>();var racers=(Array)Get(match,"racers");int originalCount=(int)Get(Get(match,"ScoreJournal"),"Count");
            foreach(var receipt in observed.items)
            {
                int slot=(int)Get(receipt,"Slot");Assert.That(slot,Is.InRange(0,3));Assert.That(ids.Add((string)Get(receipt,"EventId")),Is.True);Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));Assert.That(Get(receipt,"ParticipantId"),Is.EqualTo(Get(players.GetValue(slot),"participantId")));Assert.That(Get(receipt,"IsTeam"),Is.False);Assert.That(Get(receipt,"Unit"),Is.EqualTo("PTS"));double delta=(double)Get(receipt,"Delta");Assert.That(new[]{10d,25d,50d,100d},Does.Contain(delta));sums[slot]+=delta;Assert.That(Get(receipt,"Total"),Is.EqualTo(sums[slot]));var racer=racers.GetValue(slot);Assert.That(Get(racer,"ResolutionReason"),Is.EqualTo("FINAL_REST"));Assert.That(Get(racer,"Supported"),Is.True);Assert.That((float)Get(racer,"StableTime"),Is.GreaterThanOrEqualTo((float)Get(racer,"stabilityDuration")));Assert.That(Call(match,"ScoreAt",Get(racer,"LandingPoint")),Is.EqualTo((int)delta));
                Call(match,"Lock",slot,(int)delta,Get(racer,"LandingPoint"));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(originalCount),"Repeated already-resolved callback cannot create another award.");
            }
            for(int i=0;i<4;i++){Assert.That(sums[i],Is.EqualTo(totals[i]),"Every actual first-round positive point is represented; zero misses create no invented points.");object[] args={i,null,null};Assert.That(match.GetType().GetMethod("TryReadScoreCard").Invoke(match,args),Is.True);int attempts=((int[])Get(rules,"attempts"))[i];if(attempts>0)StringAssert.EndsWith(attempts+(attempts==1?" LAUNCH":" LAUNCHES"),(string)args[2]);}Assert.That((int)Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(observed.items.Count));Assert.That(Get(match,"scoreFeedback"),Is.Not.Null,"Keep original DOTween target-ring feedback.");
            var panels=((Component)Get(match,"menus")).GetComponentsInChildren(Runtime("SportsScorePresentation"),true).Cast<Component>().ToArray();Assert.That(panels.Length,Is.EqualTo(1));var panel=panels[0];Assert.That(Get(panel,"viewport"),Is.EqualTo(-1));Time.timeScale=1;deadline=Time.realtimeSinceStartup+20;while(Get(panel,"LastPresented")==null&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(observed.items,Does.Contain(Get(panel,"LastPresented")));var animator=(Animator)Get(panel,"notificationAnimator");yield return new WaitForSeconds(animator.runtimeAnimatorController.animationClips.Single(x=>x.name=="Notification_In").length+.03f);Time.timeScale=0;yield return null;Assert.That(((CanvasGroup)Get(panel,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(panel,"notification")).gameObject.activeInHierarchy,Is.True);
            var cameras=(Camera[])Get(match,"cameras");Assert.That(cameras.Length,Is.EqualTo(split?4:1));if(split){var views=(Array)Get(Get(match,"menus"),"viewPanels");for(int i=0;i<4;i++){var card=((Component)views.GetValue(i)).transform.Find("Compact owned viewport score") as RectTransform;Assert.That(card,Is.Not.Null);Assert.That(Bounds((RectTransform)panel.transform).Overlaps(Bounds(card)),Is.False,"Shared final-rest feedback clears original owned viewport cards.");}}
            var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/TargetRestAwards/actual-"+(split?"four-AI-views":"shared")+"-"+size.y+".png",null});Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(originalCount));string old=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(old));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(panel,"LastPresented"),Is.Null);TestContext.WriteLine("ACTUAL_TARGET_REST splitAI="+split+" receipts="+originalCount+" totals="+string.Join(",",totals)+"; original actual supported settling, exact zone points, duplicate-callback guard, retained ring feedback, saved Heat and rematch. No injected score/pose/force/finish; controlled native rendering is separate from Windows or human hardware acceptance.");
        }
        finally{Time.timeScale=1;if(match!=null){if(subscription!=null)signal.RemoveEventHandler(match,subscription);((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
