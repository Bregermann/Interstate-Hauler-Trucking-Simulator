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

public sealed class AttemptResultsLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) { if (owner.GetType().GetField(name, Any) is FieldInfo field) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value); }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static Rect Bounds(RectTransform rect) { var c = new Vector3[4]; rect.GetWorldCorners(c); return Rect.MinMaxRect(c.Min(v => v.x), c.Min(v => v.y), c.Max(v => v.x), c.Max(v => v.y)); }
    [UnityTest, Timeout(240000)]
    public IEnumerator SyntheticSixAttemptBoardsPreserveWinnersMeasurementsAndReset()
    {
        foreach (string scene in new[] { "TruckDockDiving", "TruckFieldGoal", "TruckTargetLaunch", "TruckSkeeBall", "TruckGiantSwing", "TruckTrailerPlug" }) yield return Inspect(scene);
    }
    [UnityTest, Timeout(240000)]
    public IEnumerator SyntheticSoloBoardsPreserveExistingPersonalRecords()
    {
        foreach (string scene in new[] { "TruckDockDiving", "TruckFieldGoal", "TruckTargetLaunch", "TruckSkeeBall", "TruckGiantSwing" }) yield return Inspect(scene, true);
    }
    static IEnumerator Inspect(string scene, bool solo = false)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticAttemptResultBindings" });
        var isolation = new GameObject("Explicit synthetic rule values and actual saved results fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); if(scene=="TruckSkeeBall")Set(match,"option",1);if(scene=="TruckFieldGoal")Set(match,"option",solo?1:0);var setup = Get(match, "setup"); Set(setup, "count", solo ? 1 : Get(match, "MaximumPlayers")); Call(match, "AllBots");
            foreach (object player in ((Array)Get(setup, "players")).Cast<object>().Take((int)Get(setup, "count"))) Set(player, "name", "Shared Name");
            string recordKey = scene=="TruckDockDiving"?"TruckDockDiving.SoloBest.Inches.v1":scene=="TruckFieldGoal"?(string)Get(match,"BestKey"):scene=="TruckTargetLaunch"?"TruckTargetLaunch.Best":scene=="TruckSkeeBall"?"TruckSkeeBall.Best":"TruckGiantSwing.Best.v1";
            int record = scene=="TruckDockDiving"?1699:scene=="TruckFieldGoal"?4:scene=="TruckTargetLaunch"?400:scene=="TruckSkeeBall"?450:250;
            string recordText = scene=="TruckDockDiving"?"141′ 7″":record + (scene=="TruckFieldGoal"?" GOOD":" PTS");
            if(solo)Runtime("SportsPrefs").GetMethod("SetInt").Invoke(null,new object[]{recordKey,record});
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0; int count = (int)Get(setup, "count"); Assert.That(count,Is.EqualTo(solo ? 1 : Get(match,"MaximumPlayers")),"Exercise maximum roster with real competitive/hot-seat configuration."); var rules = scene=="TruckDockDiving"||scene=="TruckGiantSwing"?(object)match:Get(match,"rules");
            string metricField = scene=="TruckDockDiving"?"best":scene=="TruckFieldGoal"?"goals":scene=="TruckGiantSwing"?"points":scene=="TruckTrailerPlug"?"score":"totals";
            var metrics=(int[])Get(rules,metricField);
            for(int i=0;i<count;i++)metrics[i]=i<2?100:0;
            if(scene=="TruckTrailerPlug") { var winners=(bool[])Get(rules,"winners");winners[0]=winners[1]=true;Set(rules,"Complete",true); }
            else Set(rules,"Winner",solo?0:1);
            if(scene=="TruckDockDiving")
            {
                metrics[0]=metrics[1]=1200;var history=(int[,])Get(match,"history");
                for(int i=0;i<count;i++){history[i,0]=metrics[i];history[i,1]=Math.Max(0,metrics[i]-24);history[i,2]=0;}
                Set(match,"extra",!solo);((int[])Get(match,"extraScores"))[1]=60;
            }
            else
            {
                var attempts=(int[])Get(rules,"attempts");for(int i=0;i<count;i++)attempts[i]=scene=="TruckSkeeBall"?9:scene=="TruckFieldGoal"||scene=="TruckTargetLaunch"?5:3;
                if(scene=="TruckFieldGoal"){metrics[0]=3;metrics[1]=4;}
                if(scene=="TruckTargetLaunch"){metrics[0]=250;metrics[1]=300;Set(rules,"Round",5);}
                if(scene=="TruckSkeeBall"){metrics[0]=300;metrics[1]=400;((int[])Get(rules,"last"))[0]=50;((int[])Get(rules,"last"))[1]=100;}
                if(scene=="TruckGiantSwing"){metrics[1]=150;Set(match,"Round",solo?3:4);Set(match,"tieRound",solo?0:1);}
            }
            Call(match, "EndMatch"); Time.timeScale = 0; var before = metrics.Cast<object>().ToArray(); yield return new WaitForSecondsRealtime(.22f); yield return null;
            var board = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation")); Assert.That(board, Is.Not.Null); Call(board, "CompleteAnimations"); Canvas.ForceUpdateCanvases();
            Assert.That(Get(board, "Displaying"), Is.True); var snapshot = Get(board, "Snapshot"); var entries = ((IEnumerable)Get(snapshot, "Rows")).Cast<object>().ToArray(); Assert.That(entries.Length, Is.EqualTo(count)); Assert.That(entries.Select(entry => Get(entry, "ParticipantId")).Distinct().Count(), Is.EqualTo(count));
            var views = ((Array)Get(board, "rows")).Cast<object>().Where(view => ((Component)Get(view, "rect")).gameObject.activeSelf).ToArray();
            for (int i = 0; i < entries.Length; i++)
            {
                int slot=(int)Get(entries[i],"Slot"),place=(int)Call(match,"ResultPlace",slot);
                bool winner=scene=="TruckTrailerPlug"?((bool[])Get(rules,"winners"))[slot]:(int)Get(rules,"Winner")==slot;
                double value=scene=="TruckDockDiving"?metrics[slot]/12d:metrics[slot];
                Assert.That(Get(entries[i],"Place"),Is.EqualTo(place));Assert.That(Get(entries[i],"Winner"),Is.EqualTo(winner));Assert.That(Get(entries[i],"Measurement"),Is.EqualTo(value));
                Assert.That(Get(entries[i],"Unit"),Is.EqualTo(scene=="TruckDockDiving"?"FT":scene=="TruckFieldGoal"?"GOOD":"PTS"));
                Assert.That(Get(Get(views[i],"value"),"text"),Is.EqualTo(Get(entries[i],"Value")));
            }
            if(scene=="TruckDockDiving"&&!solo) Assert.That(Get(entries[0],"Slot"),Is.EqualTo(1),"Existing extra-jump winner takes precedence over equal best distances.");
            if(scene=="TruckDockDiving"&&!solo)Assert.That((string)Get(snapshot,"Summary"),Does.Contain("WINNING EXTRA JUMP 5′ 0″"),"The actual deciding extra jump explains equal main-round distances.");
            if(scene=="TruckTrailerPlug") Assert.That(entries.Count(entry=>(bool)Get(entry,"Winner")),Is.EqualTo(2),"Every original shared quarry winner remains highlighted.");
            if(solo){
                Assert.That((string)Get(snapshot,"Summary"),Does.Contain("RECORD " + recordText),"Preserve the existing solo record with its original units.");
                Assert.That(Convert.ToDouble(Get(match,"ResultPersonalBest")),Is.EqualTo(record));
                Assert.That(Runtime("SportsPrefs").GetMethod("GetInt").Invoke(null,new object[]{recordKey,-1}),Is.EqualTo(record),"Results rendering cannot overwrite stored records.");
            }
            var menus = (Component)Get(match, "menus"); var page = ((GameObject[])Get(menus, "pages"))[6];
            var readability = page.transform.Find("Results readability backing"); Assert.That(readability, Is.Not.Null, "Busy stadium/environment text must remain behind the results.");
            var image = readability.GetComponent(Type.GetType("UnityEngine.UI.Image, UnityEngine.UI", true)); Assert.That(((Color)Get(image,"color")).a, Is.GreaterThanOrEqualTo(.9f)); Assert.That(Get(image,"raycastTarget"), Is.False); Assert.That(readability.GetSiblingIndex(),Is.Zero);
            Action<Camera> inspect = camera =>
            {
                Canvas.ForceUpdateCanvases(); var canvas = Bounds(menus.GetComponent<RectTransform>());
                foreach(string field in new[]{"heading","summary"}){var label=Get(board,field);Call(label,"ForceMeshUpdate",false,false);Assert.That(Get(label,"isTextOverflowing"),Is.False,"Record and match captions must fit: "+scene);}
                for (int i = 0; i < views.Length; i++)
                {
                    var view = views[i]; var row = Bounds((RectTransform)Get(view, "rect")); Assert.That(row.xMin, Is.GreaterThanOrEqualTo(canvas.xMin)); Assert.That(row.xMax, Is.LessThanOrEqualTo(canvas.xMax)); Assert.That(row.yMin, Is.GreaterThanOrEqualTo(canvas.yMin)); Assert.That(row.yMax, Is.LessThanOrEqualTo(canvas.yMax));
                    foreach (var action in page.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>()) Assert.That(row.Overlaps(Bounds(action.GetComponent<RectTransform>())), Is.False, "Results must not cover existing replay/rematch controls.");
                    foreach (string heading in new[] { "Title", "Subtitle" }) Assert.That(row.Overlaps(Bounds(page.transform.Find(heading).GetComponent<RectTransform>())), Is.False);
                    foreach (string field in new[] { "name", "value", "detail" }) { var label = Get(view, field); Call(label, "ForceMeshUpdate", false, false); Assert.That(Get(label, "isTextOverflowing"), Is.False, "Actual result text must fit: " + scene + " / " + field); }
                    for (int j = i + 1; j < views.Length; j++) Assert.That(row.Overlaps(Bounds((RectTransform)Get(views[j], "rect"))), Is.False);
                }
            };
            var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080) }) save.Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/AttemptResults/" + scene + (solo?"-solo":"") + "-" + size.y + ".png", inspect });
            CollectionAssert.AreEqual(before, metrics.Cast<object>().ToArray(), "Rendering and counting values cannot change actual rule data.");
            ((Behaviour)match).enabled = true; Time.timeScale = 1; Call(match, "SetupScreen"); Assert.That(Get(board, "Snapshot"), Is.Null); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; Assert.That(Get(board, "Snapshot"), Is.Null); if(solo)Assert.That(Convert.ToDouble(Get(match,"ResultPersonalBest")),Is.EqualTo(record),"Rematch must preserve the existing stored record.");
            TestContext.WriteLine("SYNTHETIC_ATTEMPT_RESULTS " + scene + " count=" + count + " solo=" + solo + ": explicit synthetic rule values, actual saved Heat/DOTween result board; original ranks/ties/metrics/IDs, controlled720/1080 bounds/TMP and rematch. Not physical-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
