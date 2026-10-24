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

public sealed class RingKickScoreLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);
    static Rect Bounds(RectTransform rect){var points=new Vector3[4];rect.GetWorldCorners(points);return Rect.MinMaxRect(points[0].x,points[0].y,points[2].x,points[2].y);}
    [UnityTest,Timeout(360000)]public IEnumerator OriginalSkeeRingContactUsesItsActualSimultaneousLaneOwner()=>Inspect(true,false);
    [UnityTest,Timeout(360000)]public IEnumerator OriginalSkeeHotSeatSwitchNeverShowsThePreviousDriverAward()=>Inspect(true,true);
    [UnityTest,Timeout(360000)]public IEnumerator OriginalFieldGoalCrossingAwardsOneOriginalPoint()=>Inspect(false,false);
    static IEnumerator Inspect(bool skee,bool hotSeat)
    {
        var preferences=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"RingKickOriginalAwards"});var isolation=new GameObject("Original real ring and football contact acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            string scene=skee?"TruckSkeeBall":"TruckFieldGoal";EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Set(match,"option",hotSeat?1:0);var setup=Get(match,"setup");Set(setup,"count",hotSeat?8:4);Call(match,"AllBots");
            foreach(var player in ((Array)Get(setup,"players")).Cast<object>()){Set(player,"name","Same Arcade Driver");Set(player,"difficulty",Enum.Parse(Get(player,"difficulty").GetType(),"Hard"));}
            Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});if(!skee){var presets=Get(match,"weatherPresets");var clear=((Array)Get(presets,"entries")).Cast<object>().Single(p=>(string)Get(p,"displayName")=="Clear");Call(presets,"Select",Get(clear,"id"));}
            Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=4;var journal=Get(match,"ScoreJournal");float deadline=Time.realtimeSinceStartup+240;
            while(Time.realtimeSinceStartup<deadline&&!(bool)Get(match,"Finished"))
            {
                if((int)Get(journal,"Count")>0&&(!hotSeat||(int)Get(Get(journal,"Latest"),"Slot")>0))break;
                yield return new WaitForFixedUpdate();
            }
            Assert.That((int)Get(journal,"Count"),Is.GreaterThan(0),"Only a genuine original opening/football crossing can create an award.");var receipt=Get(journal,"Latest");int owner=(int)Get(receipt,"Slot");if(hotSeat)Assert.That(owner,Is.GreaterThan(0));((Behaviour)match).enabled=false;Time.timeScale=0;
            var rules=Get(match,"rules");int total=((int[])Get(rules,skee?"totals":"goals"))[owner];Assert.That(Get(receipt,"Total"),Is.EqualTo((double)total));double delta=(double)Get(receipt,"Delta");Assert.That(Get(receipt,"Unit"),Is.EqualTo("PTS"));Assert.That(Get(receipt,"IsTeam"),Is.False);Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId")));
            if(skee){Assert.That(new[]{10d,20d,30d,40d,50d,100d},Does.Contain(delta));Assert.That(Get(journal,"Count"),Is.EqualTo(Get(match,"GenuineEntries")));}
            else{Assert.That(delta,Is.EqualTo(1d));Assert.That(Get(journal,"Count"),Is.EqualTo(((int[])Get(rules,"goals")).Sum()));Assert.That(Get(rules,"AttemptLocked"),Is.True);}
            var presenters=((Component)Get(match,"menus")).GetComponentsInChildren(Runtime("SportsScorePresentation"),true).Cast<Component>().ToArray();Assert.That(presenters.Length,Is.EqualTo(skee?4:1));Time.timeScale=1;deadline=Time.realtimeSinceStartup+20;Component credited=null;
            while(credited==null&&Time.realtimeSinceStartup<deadline){credited=presenters.FirstOrDefault(p=>Get(p,"LastPresented") is object latest&&(int)Get(latest,"Slot")==owner);if(credited==null)yield return null;}
            Assert.That(credited,Is.Not.Null);var animator=(Animator)Get(credited,"notificationAnimator");yield return new WaitForSeconds(animator.runtimeAnimatorController.animationClips.Single(p=>p.name=="Notification_In").length+.03f);Time.timeScale=0;yield return null;
            Assert.That(((CanvasGroup)Get(credited,"visibility")).alpha,Is.EqualTo(1));Assert.That(((Component)Get(credited,"notification")).gameObject.activeInHierarchy,Is.True);
            if(skee)
            {
                var texts=(Array)Get(Get(match,"menus"),"viewTexts");foreach(var p in presenters){int view=(int)Get(p,"viewport");var latest=Get(p,"LastPresented");object[] args={view,0,null,null};bool valid=(bool)match.GetType().GetMethod("TryReadViewportCard").Invoke(match,args);if(latest!=null){Assert.That(valid,Is.True);Assert.That(Get(latest,"Slot"),Is.EqualTo(args[1]));}if(valid){var original=(Component)texts.GetValue(view);Assert.That(original.gameObject.activeInHierarchy,Is.True);Assert.That(Bounds((RectTransform)p.transform).Overlaps(Bounds((RectTransform)original.transform)),Is.False,"Ring feedback must not cover the original four-line controls/aftertouch HUD.");}}
            }
            else Assert.That(Bounds((RectTransform)credited.transform).yMax,Is.LessThan(Screen.height*.35f),"Keep the new award in the lower margin, clear of the original stadium scoreboard.");
            var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/RingKickAwards/"+scene+(hotSeat?"-hotseat":"-standard")+"-"+size.y+".png",null});
            string previous=(string)Get(match,"MatchId");((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(previous));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(presenters.All(p=>Get(p,"LastPresented")==null),Is.True);
            TestContext.WriteLine("ORIGINAL_PHYSICAL_RING_KICK "+scene+" hotSeat="+hotSeat+" slot="+owner+" earned="+delta+" total="+total+" physical original rule commit, saved Heat, ownership, duplicate names, retained HUD and rematch. First-award lifecycle only; full Windows/hardware acceptance separate.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);preferences.Dispose();}
    }
}
#endif
