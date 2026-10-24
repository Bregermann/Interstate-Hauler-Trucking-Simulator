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

// Prepared follow-on fixture only. Explicit synthetic finished state is used
// to exercise the actual saved UI, never to claim physical race acceptance.
public sealed class SeriesResultsLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) { if (owner.GetType().GetField(name, Any) is FieldInfo field) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value); }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static Rect Bounds(RectTransform rect) { var c = new Vector3[4]; rect.GetWorldCorners(c); return Rect.MinMaxRect(c.Min(v => v.x), c.Min(v => v.y), c.Max(v => v.x), c.Max(v => v.y)); }
    [UnityTest, Timeout(300000)]
    public IEnumerator SyntheticSeriesBoardsPreserveTiebreaksAndEightCooperativeIdentities()
    {
        foreach (string scene in new[] { "TruckBaseJumping", "TruckBogSnorkelling", "TruckMattressRacing", "TruckTerrierRacing", "TruckWifeCarrying", "TruckSkyTow" }) yield return Inspect(scene);
        yield return Inspect("TruckWifeCarrying", true); yield return Inspect("TruckSkyTow", true);
    }
    static IEnumerator Inspect(string scene, bool variant = false)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticSeriesResultBindings" });
        var isolation = new GameObject("Explicit synthetic series results fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup"); Set(setup, "count", Get(match, "MaximumPlayers")); Call(match, "AllBots");
            foreach (object player in ((Array)Get(setup, "players")).Cast<object>().Take((int)Get(setup, "count"))) Set(player, "name", "Shared Name");
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0; int count = (int)Get(setup, "count");
            bool wife = scene == "TruckWifeCarrying", sky = scene == "TruckSkyTow", jump = scene == "TruckBaseJumping", terrier = scene == "TruckTerrierRacing";
            object owner = jump || terrier ? (object)match : Get(match, sky ? "rules" : "series");
            int[] totals = (int[])Get(owner, jump ? "totals" : terrier ? "wins" : sky ? "score" : "total");
            Array.Clear(totals, 0, totals.Length);
            if (jump)
            {
                totals[0] = totals[1] = 8; totals[2] = 3; Set(match, "Winner", 1); Set(match, "Round", 3);
                ((int[])Get(match, "totalErrors"))[0] = 2000; ((int[])Get(match, "totalErrors"))[1] = 1000;
                ((int[])Get(match, "totalTimes"))[0] = 30000; ((int[])Get(match, "totalTimes"))[1] = 40000; ((int[])Get(match, "totalErrors"))[2] = 5000; ((int[])Get(match, "totalTimes"))[2] = 45000;
                var final = (Array)Get(match, "roundResults"); var order = (int[])Get(match, "order");
                for (int i = 0; i < count; i++)
                {
                    order[i] = i; var landing = final.GetValue(i); Set(landing, "valid", i < 2); Set(landing, "inside", i == 1); Set(landing, "errorMillimetres", i == 1 ? 730 : 1450); Set(landing, "timeMilliseconds", i == 1 ? 4000 : 4500); Set(landing, "points", i == 1 ? 4 : i == 0 ? 3 : 0); final.SetValue(landing, i);
                }
            }
            else if (terrier) { totals[0] = 4; totals[1] = 5; Set(match, "MatchWinner", 1); Set(match, "Heat", 9); }
            else if (sky)
            {
                for (int i = 0; i < count; i++)
                {
                    int hits = i == 1 || variant && i == 0 ? 5 : i == 0 ? 3 : 0;
                    for (int ring = 0; ring < hits; ring++) Assert.That(Call(owner, "Ring", i, ring, true, false, false), Is.True);
                    Assert.That(Call(owner, "Finish", i), Is.True);
                }
                Assert.That(Get(owner, "Complete"), Is.True);
            }
            else
            {
                int teams = wife ? 4 : count; bool placementTies = (bool)Get(owner, "placementTies"); Call(owner, "Reset", teams, placementTies);
                for (int race = 0; race < 3; race++)
                {
                    Call(owner, "BeginRound"); var times = new float[teams]; for (int i = 0; i < teams; i++) times[i] = i + 1;
                    times[0] = race == 0 ? 1 : 2; times[1] = race == 0 ? 3 : 1; times[2] = race == 0 ? 2 : 3;
                    Assert.That(Call(owner, "Resolve", times), Is.True);
                }
                if ((int)Get(owner, "winner") < 0)
                {
                    Call(owner, "BeginRound"); var times = Enumerable.Repeat(float.PositiveInfinity, teams).ToArray(); times[0] = 2; times[1] = 1; Assert.That(Call(owner, "Resolve", times), Is.True);
                }
                Assert.That(Get(owner, "winner"), Is.EqualTo(1), "Original series bookkeeping chooses the winner, including its real placement-history/tiebreak policy.");
                if (wife) { Set(match, "Swapped", variant); var names = (string[])Get(match, "teams"); for (int i = 0; i < 4; i++) names[i] = "Team " + (char)('A' + i); }
            }
            Call(match, "EndMatch"); Time.timeScale = 0; var before = (int[])totals.Clone(); yield return new WaitForSecondsRealtime(.22f); yield return null;
            var board = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation")); Assert.That(board, Is.Not.Null); Call(board, "CompleteAnimations"); Canvas.ForceUpdateCanvases();
            Assert.That(Get(board, "Displaying"), Is.True); var snapshot = Get(board, "Snapshot"); var entries = ((IEnumerable)Get(snapshot, "Rows")).Cast<object>().ToArray(); Assert.That(entries.Length, Is.EqualTo(count)); Assert.That(entries.Select(entry => Get(entry, "ParticipantId")).Distinct().Count(), Is.EqualTo(count));
            var views = ((Array)Get(board, "rows")).Cast<object>().Where(view => ((Component)Get(view, "rect")).gameObject.activeSelf).ToArray();
            foreach (var entry in entries)
            {
                int slot = (int)Get(entry, "Slot"), team = wife ? slot % 4 : slot; bool winner = sky ? ((bool[])Get(owner, "winners"))[slot] : team == 1;
                Assert.That(Get(entry, "Place"), Is.EqualTo(Call(match, "ResultPlace", team))); Assert.That(Get(entry, "Winner"), Is.EqualTo(winner)); Assert.That(Get(entry, "Measurement"), Is.EqualTo((double)totals[team]));
                Assert.That(Get(entry, "DisplayName"), Is.EqualTo("Shared Name")); Assert.That(Get(entry, "Unit"), Is.EqualTo(wife ? "TEAM PTS" : terrier ? "WINS" : "PTS"));
                string detail = (string)Get(entry, "Detail");
                if (wife) { StringAssert.Contains("Team " + (char)('A' + team), detail); StringAssert.Contains((int)Call(match, "DriverRole", team) == slot ? "DRIVER" : "PASSENGER", detail); }
                if (jump) { StringAssert.Contains("VALID ERROR", detail); StringAssert.Contains("SEC", detail); StringAssert.Contains("FINAL", detail); if (slot < 2) { StringAssert.Contains("BAND", detail); StringAssert.Contains(slot == 1 ? "4 PTS" : "3 PTS", detail); } else StringAssert.Contains("INVALID · 0 PTS", detail); }
                if (scene == "TruckMattressRacing") StringAssert.Contains("FINAL RACE", detail);
            }
            Assert.That(entries.Count(entry => (bool)Get(entry, "Winner")), Is.EqualTo(wife || sky && variant ? 2 : 1));
            var menus = (Component)Get(match, "menus"); var page = ((GameObject[])Get(menus, "pages"))[6]; Assert.That(page.transform.Find("Results readability backing"), Is.Not.Null);
            Action<Camera> inspect = camera =>
            {
                Canvas.ForceUpdateCanvases(); var canvas = Bounds(menus.GetComponent<RectTransform>());
                foreach (string field in new[] { "heading", "summary" }) { var label = Get(board, field); Call(label, "ForceMeshUpdate", false, false); Assert.That(Get(label, "isTextOverflowing"), Is.False, scene + " caption"); }
                for (int i = 0; i < views.Length; i++)
                {
                    var view = views[i]; var row = Bounds((RectTransform)Get(view, "rect")); Assert.That(row.xMin, Is.GreaterThanOrEqualTo(canvas.xMin)); Assert.That(row.xMax, Is.LessThanOrEqualTo(canvas.xMax)); Assert.That(row.yMin, Is.GreaterThanOrEqualTo(canvas.yMin)); Assert.That(row.yMax, Is.LessThanOrEqualTo(canvas.yMax));
                    foreach (var action in page.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>()) Assert.That(row.Overlaps(Bounds(action.GetComponent<RectTransform>())), Is.False);
                    foreach (string heading in new[] { "Title", "Subtitle" }) Assert.That(row.Overlaps(Bounds(page.transform.Find(heading).GetComponent<RectTransform>())), Is.False);
                    foreach (string field in new[] { "name", "value", "detail" }) { var label = Get(view, field); Call(label, "ForceMeshUpdate", false, false); Assert.That(Get(label, "isTextOverflowing"), Is.False, scene + " / " + field); }
                    for (int j = i + 1; j < views.Length; j++) Assert.That(row.Overlaps(Bounds((RectTransform)Get(views[j], "rect"))), Is.False);
                }
            };
            var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) }) save.Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/SeriesResults/" + scene + (variant ? "-variant" : "") + "-" + size.y + ".png", inspect });
            CollectionAssert.AreEqual(before, totals, "Rendering must not mutate series totals or team scores.");
            ((Behaviour)match).enabled = true; Time.timeScale = 1; Call(match, "SetupScreen"); Assert.That(Get(board, "Snapshot"), Is.Null); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; Assert.That(Get(board, "Snapshot"), Is.Null);
            TestContext.WriteLine("SYNTHETIC_SERIES_RESULTS " + scene + " count=" + count + " variant=" + variant + ": saved Heat/DOTween UI, explicit synthetic winners/totals/landing data. No physical race or human acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
