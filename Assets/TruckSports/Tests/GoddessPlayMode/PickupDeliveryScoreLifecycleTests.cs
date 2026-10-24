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

// Prepared actual-contact fixtures. No score/clock/winner injection.
public sealed class PickupDeliveryScoreLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);
    static Rect Bounds(RectTransform rect) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); return Rect.MinMaxRect(corners.Min(v=>v.x),corners.Min(v=>v.y),corners.Max(v=>v.x),corners.Max(v=>v.y)); }

    [UnityTest, Timeout(240000)]
    public IEnumerator PhysicalCupAwardsKeepTeamAndPracticeRecipientsSeparate()
    {
        yield return Inspect("TruckBeerPong", false);
        yield return Inspect("TruckBeerPong", true);
    }
    [UnityTest, Timeout(240000)]
    public IEnumerator PhysicalFareDeliveryUsesTheActualScorerAfterAtomicResolution() => Inspect("TruckFareFight", false);
    [UnityTest, Timeout(720000)]
    public IEnumerator FinalPhysicalFareDeliveryKeepsItsWinnerReceiptOnResults() => Inspect("TruckFareFight", false, true);
    [UnityTest, Timeout(180000)]
    public IEnumerator PhysicalTrailerPickupReportsItsCommittedPointsOnce() => Inspect("TruckSnake", false);

    static IEnumerator Inspect(string scene, bool practice, bool fullMatch = false)
    {
        var preferences = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ActualPickupDeliveryAwards"});
        var isolation = new GameObject("Explicit actual-contact award fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));
            var setup=Get(match,"setup");
            if(scene=="TruckBeerPong") { Set(match,"option",practice?1:0); Set(match,"teamA",2); Set(match,"teamB",2); }
            if(scene=="TruckFareFight") Set(match,"option",0); // Existing selectable first-to-three; no injected score or reduced target.
            if(fullMatch) {
                // Reproduce an existing recorded physical Windows full-match seed,
                // with the same explicit Clear selection as FareFightProbe.
                Set(match,"validationSeed",1632681558);
                var catalog=Get(match,"presets"); var clear=((Array)Get(catalog,"entries")).Cast<object>().Single(entry=>(string)Get(entry,"displayName")=="Clear"); Call(catalog,"Select",Get(clear,"id"));
                Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null,new[]{Enum.Parse(Runtime("SportsTimeChoice"),"Day")});
            }
            Set(setup,"count",practice?1:4); Call(match,"AllBots");
            var players=((Array)Get(setup,"players")).Cast<object>().ToArray();
            foreach(var player in players) Set(player,"name","Same Driver Name");
            Assert.That(Call(match,"StartMatch"),Is.True); Time.timeScale=4;
            var journal=Get(match,"ScoreJournal"); var rules=Get(match,"rules");
            Assert.That(Get(journal,"Count"),Is.Zero);
            float deadline=Time.realtimeSinceStartup+(fullMatch?600:150), reportAt=0;
            if(fullMatch) { Assert.That(Get(setup,"count"),Is.EqualTo(4)); Assert.That(Get(rules,"Count"),Is.EqualTo(4)); TestContext.WriteLine("FARE_AWARD_SCENARIO seed=1632681558 Clear/Day first-to-three; existing FareFightProbe full-match600s watchdog; actual4AI expected."); }
            while((fullMatch?!(bool)Get(match,"Finished"):(int)Get(journal,"Count")==0) && Time.realtimeSinceStartup<deadline) {
                if(fullMatch && Time.realtimeSinceStartup>=reportAt) { reportAt=Time.realtimeSinceStartup+15; TestContext.WriteLine("FARE_AWARD_PROGRESS clock="+Get(rules,"Clock")+" phase="+Get(rules,"Phase")+" scores="+string.Join(",",(int[])Get(rules,"score"))+" deliveries="+Get(rules,"Deliveries")+" receipts="+Get(journal,"Count")); }
                yield return new WaitForFixedUpdate();
            }
            Assert.That((int)Get(journal,"Count"),Is.GreaterThan(0),"Natural autonomous NWH/PhysX gameplay must produce a real scoring contact.");
            if(fullMatch) Assert.That(Get(match,"Finished"),Is.True,"The original selectable first-to-three match must finish through actual AI driving and trigger contacts.");
            ((Behaviour)match).enabled=false; Time.timeScale=0;
            var receipt=Get(journal,"Latest"); int count=(int)Get(journal,"Count");
            Assert.That(Get(receipt,"MatchId"),Is.EqualTo(Get(match,"MatchId"))); Assert.That((double)Get(receipt,"Delta"),Is.GreaterThan(0));
            int[] totals=((int[])Get(rules,scene=="TruckBeerPong"?"hits":"score")).ToArray();
            if(scene=="TruckBeerPong")
            {
                int team=(int)Get(rules,"ActiveTeam"); Assert.That(count,Is.EqualTo(totals.Sum()));
                Assert.That(Get(receipt,"Total"),Is.EqualTo((double)totals[team])); Assert.That(Get(receipt,"Delta"),Is.EqualTo(1d)); Assert.That(Get(receipt,"IsTeam"),Is.EqualTo(!practice));
                if(practice) Assert.That(Get(receipt,"ParticipantId"),Is.EqualTo(Get(players[0],"participantId")));
                else { Assert.That(Get(receipt,"RecipientId"),Is.EqualTo(team==0?"team:blue":"team:orange")); Assert.That(Get(receipt,"ParticipantId"),Is.Null); }
                // Same already-claimed physical cup cannot publish twice.
                Call(match,"Resolve",Get(rules,"LastCup"),"DUPLICATE CONTACT FIXTURE"); Assert.That(Get(journal,"Count"),Is.EqualTo(count));
            }
            else
            {
                int slot=(int)Get(receipt,"Slot"); Assert.That(Get(receipt,"IsTeam"),Is.False); Assert.That(Get(receipt,"ParticipantId"),Is.EqualTo(Get(players[slot],"participantId")));
                Assert.That(Get(receipt,"Total"),Is.LessThanOrEqualTo((double)totals[slot]));
                Assert.That(count,Is.EqualTo((int)Get(scene=="TruckSnake"?(object)match:rules,scene=="TruckSnake"?"PhysicalPickups":"Deliveries")));
                if(scene=="TruckFareFight") { Assert.That(Get(receipt,"Total"),Is.EqualTo((double)totals[slot])); Assert.That(Get(receipt,"Delta"),Is.EqualTo(1d)); Assert.That(Get(receipt,"Reason"),Is.EqualTo("PASSENGER DELIVERED")); }
                if(fullMatch) { Assert.That(((bool[])Get(rules,"winners"))[slot],Is.True); Assert.That(Get(receipt,"Tier").ToString(),Is.EqualTo("MatchChampion")); }
            }
            // Explicit lifecycle fixture: open results after the real award.
            // This is separate from a complete-match acceptance claim.
            if(!fullMatch) Call(match,"EndMatch"); Time.timeScale=1;
            var presenter=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation")); Assert.That(presenter,Is.Not.Null);
            deadline=Time.realtimeSinceStartup+15;
            while((Get(presenter,"LastPresented")==null || (string)Get(Get(presenter,"LastPresented"),"EventId")!=(string)Get(receipt,"EventId")) && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(Get(presenter,"LastPresented"),Is.SameAs(receipt)); Assert.That(((CanvasGroup)Get(presenter,"visibility")).alpha,Is.EqualTo(1));
            // Keep original real-contact assertions; wait for the existing Heat/results
            // transitions rather than photographing their first transparent frame.
            if(scene=="TruckSnake") {
                var animator=(Animator)Get(presenter,"notificationAnimator");
                var clip=animator.runtimeAnimatorController.animationClips.Single(item=>item.name=="Notification_In");
                Assert.That(clip.length+.03f,Is.LessThan((float)Get(Get(presenter,"notification"),"minimizeAfter")));
                yield return new WaitForSeconds(clip.length+.03f); Time.timeScale=0;
                // Hold only the controlled short notification while the existing
                // unscaled results-row tweens finish. No production cadence edit.
            }
            yield return new WaitForSecondsRealtime(1.05f); Time.timeScale=0; yield return null;
            Assert.That(((Component)Get(presenter,"notification")).gameObject.activeInHierarchy,Is.True,"The actual committed notification must still be visibly expanded.");
            if(scene=="TruckBeerPong" || scene=="TruckFareFight") StringAssert.Contains("</size> "+(scene=="TruckBeerPong"?"CUP":"FARE")+" ",(string)Get(Get(presenter,"notification"),"notificationText"));
            var resultBoard=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation"));
            Assert.That(((CanvasGroup)Get(resultBoard,"visibility")).alpha,Is.GreaterThanOrEqualTo(.99f));
            foreach(var view in ((Array)Get(resultBoard,"rows")).Cast<object>())
                if(((Component)Get(view,"rect")).gameObject.activeSelf) Assert.That(((CanvasGroup)Get(view,"visibility")).alpha,Is.GreaterThanOrEqualTo(.99f));
            CollectionAssert.AreEqual(totals,(int[])Get(rules,scene=="TruckBeerPong"?"hits":"score"),"Presentation cannot award points or change the rules.");
            var menus=(Component)Get(match,"menus"); var page=((GameObject[])Get(menus,"pages"))[6];
            Action<Camera> inspect=camera=>{
                Canvas.ForceUpdateCanvases(); var panel=Bounds(presenter.GetComponent<RectTransform>());
                foreach(string label in new[]{"Title","Subtitle"}) Assert.That(panel.Overlaps(Bounds(page.transform.Find(label).GetComponent<RectTransform>())),Is.False);
                foreach(var action in page.GetComponentsInChildren(Runtime("SportsMenuAction"),true).Cast<Component>()) Assert.That(panel.Overlaps(Bounds(action.GetComponent<RectTransform>())),Is.False);
            };
            var save=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}) save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/PickupDeliveryAwards/"+scene+(fullMatch?"-full-match":practice?"-practice":"-competition")+"-"+size.y+".png",inspect});
            string oldMatch=(string)Get(match,"MatchId"); ((Behaviour)match).enabled=true; Call(match,"SetupScreen"); Assert.That(Call(match,"StartMatch"),Is.True); yield return null;
            Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(oldMatch)); Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero); Assert.That(Get(presenter,"Presented"),Is.Zero);
            TestContext.WriteLine("ACTUAL_PICKUP_DELIVERY "+scene+" practice="+practice+" fullMatch="+fullMatch+"; natural scoring contact, original totals, stable identity, one receipt, "+(fullMatch?"actual first-to-three match completion":"explicit early-results lifecycle")+", controlled720/1080, fresh rematch. No human/device/Windows/performance claim.");
        }
        finally { Time.timeScale=1; if(match!=null) { ((Behaviour)match).enabled=true; Call(match,"SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); preferences.Dispose(); }
    }
}
#endif
