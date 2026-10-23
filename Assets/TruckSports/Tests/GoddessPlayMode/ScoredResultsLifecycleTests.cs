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

public sealed class ScoredResultsLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) { if (owner.GetType().GetField(name, Any) is FieldInfo field) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value); }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static Rect Bounds(RectTransform rect) { var c = new Vector3[4]; rect.GetWorldCorners(c); return Rect.MinMaxRect(c.Min(v => v.x), c.Min(v => v.y), c.Max(v => v.x), c.Max(v => v.y)); }
    [UnityTest, Timeout(240000)]
    public IEnumerator SyntheticEightSportBindingsPreserveTheirOwnRanksMetricsAndReset()
    {
        foreach (string scene in new[] { "TruckWhackAMole", "TruckMagnetMayhem", "TruckSheepHerding", "TruckSeesaw", "TruckKingOfTheHill", "TruckFloorIsLava", "TruckHideAndSeek", "TruckHungryHaulers" }) yield return Inspect(scene);
    }
    static IEnumerator Inspect(string scene)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticScoredResultBindings" });
        var isolation = new GameObject("Explicit synthetic rule values and actual saved results fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup"); Set(setup, "count", Get(match, "MaximumPlayers")); Call(match, "AllBots");
            foreach (object player in ((Array)Get(setup, "players")).Cast<object>().Take((int)Get(setup, "count"))) Set(player, "name", "Shared Name");
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0; int count = (int)Get(setup, "count"); var rules = Get(match, "rules");
            string metricField = scene == "TruckKingOfTheHill" ? "control" : new[] { "TruckWhackAMole", "TruckSeesaw", "TruckFloorIsLava" }.Contains(scene) ? "scores" : "points";
            var metrics = (Array)Get(rules, metricField); Type element = metrics.GetType().GetElementType();
            for (int i = 0; i < count; i++) metrics.SetValue(Convert.ChangeType(i < 2 ? 10 : 0, element), i);
            if (scene == "TruckSheepHerding") { metrics.SetValue(1, 0); metrics.SetValue(1, 1); ((float[])Get(rules, "lastCompletion"))[0] = 20; ((float[])Get(rules, "lastCompletion"))[1] = 10; }
            if (scene == "TruckKingOfTheHill") { metrics.SetValue(12.345f, 0); metrics.SetValue(12.345f, 1); Set(rules, "Winner", 0); }
            if (scene == "TruckFloorIsLava") { metrics.SetValue(10, 0); metrics.SetValue(20, 1); ((float[])Get(rules, "survived"))[0] = 60; ((float[])Get(rules, "survived"))[1] = 20; Set(rules, "Winner", 0); }
            if (scene == "TruckMagnetMayhem" || scene == "TruckWhackAMole") metrics.SetValue(Convert.ChangeType(-5, element), count - 1);
            Call(match, "EndMatch"); Time.timeScale = 0; var before = metrics.Cast<object>().ToArray(); yield return null; yield return null;
            var board = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation")); Assert.That(board, Is.Not.Null); Call(board, "CompleteAnimations"); Canvas.ForceUpdateCanvases();
            Assert.That(Get(board, "Displaying"), Is.True); var snapshot = Get(board, "Snapshot"); var entries = ((IEnumerable)Get(snapshot, "Rows")).Cast<object>().ToArray(); Assert.That(entries.Length, Is.EqualTo(count)); Assert.That(entries.Select(entry => Get(entry, "ParticipantId")).Distinct().Count(), Is.EqualTo(count));
            var views = ((Array)Get(board, "rows")).Cast<object>().Where(view => ((Component)Get(view, "rect")).gameObject.activeSelf).ToArray();
            for (int i = 0; i < entries.Length; i++)
            {
                int slot = (int)Get(entries[i], "Slot"), place = (int)Call(rules, "Place", slot);
                Assert.That(Get(entries[i], "Place"), Is.EqualTo(place)); Assert.That(Get(entries[i], "Measurement"), Is.EqualTo(Convert.ToDouble(metrics.GetValue(slot))));
                Assert.That(Get(entries[i], "Winner"), Is.EqualTo(scene == "TruckFloorIsLava" || scene == "TruckKingOfTheHill" ? (int)Get(rules, "Winner") == slot : place == 1));
                Assert.That(Get(entries[i], "Unit"), Is.EqualTo(scene == "TruckSheepHerding" ? "SHEEP" : scene == "TruckKingOfTheHill" ? "SEC" : "PTS"));
                Assert.That(Get(Get(views[i], "value"), "text"), Is.EqualTo(Get(entries[i], "Value")));
            }
            if (scene == "TruckSheepHerding") Assert.That(Get(entries[0], "Slot"), Is.EqualTo(1), "Original delivery-time tiebreak, not a new point-only rank.");
            if (scene == "TruckFloorIsLava") Assert.That(Get(entries[0], "Slot"), Is.EqualTo(0), "Original survival ranking wins even when another truck has more points.");
            var menus = (Component)Get(match, "menus"); var page = ((GameObject[])Get(menus, "pages"))[6];
            Action<Camera> inspect = camera =>
            {
                Canvas.ForceUpdateCanvases(); var canvas = Bounds(menus.GetComponent<RectTransform>());
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
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080) }) save.Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/ScoredResults/" + scene + "-" + size.y + ".png", inspect });
            CollectionAssert.AreEqual(before, metrics.Cast<object>().ToArray(), "Rendering and counting values cannot change actual rule data.");
            ((Behaviour)match).enabled = true; Time.timeScale = 1; Call(match, "SetupScreen"); Assert.That(Get(board, "Snapshot"), Is.Null); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; Assert.That(Get(board, "Snapshot"), Is.Null);
            TestContext.WriteLine("SYNTHETIC_SCORED_RESULTS " + scene + " count=" + count + ": explicit synthetic rule values, actual saved Heat/DOTween result board; original ranks/ties/metrics/IDs, controlled720/1080 bounds/TMP and rematch. Not physical-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
