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

// Prepared only. Synthetic final values validate saved UI, not real matches.
public sealed class LegacyRoundResultsLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) { if (owner.GetType().GetField(name, Any) is FieldInfo field) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value); }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static Rect Bounds(RectTransform rect) { var c = new Vector3[4]; rect.GetWorldCorners(c); return Rect.MinMaxRect(c.Min(v => v.x), c.Min(v => v.y), c.Max(v => v.x), c.Max(v => v.y)); }
    static Rect GlyphBounds(object label)
    {
        var info = Get(label, "textInfo"); var characters = (Array)Get(info, "characterInfo"); var transform = ((Component)label).transform;
        Vector2 low = Vector2.positiveInfinity, high = Vector2.negativeInfinity; bool visible = false;
        foreach (object character in characters.Cast<object>().Take((int)Get(info, "characterCount")))
        {
            if (!(bool)Get(character, "isVisible")) continue;
            Vector2 a = transform.TransformPoint((Vector3)Get(character, "bottomLeft")), b = transform.TransformPoint((Vector3)Get(character, "topRight"));
            low = Vector2.Min(low, Vector2.Min(a, b)); high = Vector2.Max(high, Vector2.Max(a, b)); visible = true;
        }
        Assert.That(visible, Is.True, "A named participant and actual result must render visible glyphs.");
        return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
    }
    [UnityTest, Timeout(240000)]
    public IEnumerator SyntheticLegacyResultsPreserveOriginalWinnersRecordsAndStatistics()
    {
        foreach (string scene in new[] { "TruckBeerPong", "TruckBungeeDemolition", "TruckCurling", "TruckDanceOff", "TruckDemolitionDerby", "TruckFareFight", "TruckMusicalParking", "TruckSnake" }) yield return Inspect(scene);
        yield return Inspect("TruckBeerPong", true);
        yield return Inspect("TruckBungeeDemolition", true);
        yield return Inspect("TruckSnake", wide: true);
        yield return Inspect("TruckDemolitionDerby", wide: true);
    }

    static IEnumerator Inspect(string scene, bool solo = false, bool wide = false)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticLegacyRoundResults" });
        var isolation = new GameObject("Explicit synthetic final values; real saved results UI"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        bool pong = scene == "TruckBeerPong", bungee = scene == "TruckBungeeDemolition", curling = scene == "TruckCurling", dance = scene == "TruckDanceOff";
        bool derby = scene == "TruckDemolitionDerby", fare = scene == "TruckFareFight", parking = scene == "TruckMusicalParking", snake = scene == "TruckSnake";
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup");
            if (pong) { Set(match, "option", solo ? 1 : 0); Set(match, "teamA", 2); Set(match, "teamB", 2); }
            if (curling) { Set(match, "teamA", 4); Set(match, "teamB", 4); }
            Set(setup, "count", solo ? 1 : Get(match, "MaximumPlayers")); Call(match, "AllBots");
            int count = (int)Get(setup, "count");
            foreach (object player in ((Array)Get(setup, "players")).Cast<object>().Take(count)) Set(player, "name", wide ? new string('W', 40) : "Shared Name");
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0;
            object owner = pong || derby || fare || snake ? Get(match, "rules") : (object)match;
            Array original; double?[] expected = new double?[count]; int[] places = new int[count]; bool[] winners = new bool[count]; string unit = "PTS";
            string recordKey = null; int recordValue = 0;
            if (pong)
            {
                var hits = (int[])Get(owner, "hits"); hits[0] = solo ? 6 : 4; hits[1] = 10; original = hits;
                var shots = (int[])Get(owner, "shots"); shots[0] = 12; shots[1] = 15; Set(owner, "Winner", solo ? -1 : 1);
                unit = solo ? "CUPS HIT" : "TEAM CUPS HIT";
                for (int i = 0; i < count; i++) { int team = solo || i < 2 ? 0 : 1; expected[i] = hits[team]; winners[i] = !solo && team == 1; places[i] = solo ? 0 : winners[i] ? 1 : 2; }
            }
            else if (bungee)
            {
                var totals = (int[])Get(match, "total"); original = totals; Set(match, "Winner", solo ? 0 : 2); Set(match, "CompletedRounds", 3);
                for (int i = 0; i < count; i++) { totals[i] = solo ? 12 : new[] { 6, 12, 12, 3 }[i]; expected[i] = totals[i]; winners[i] = i == (solo ? 0 : 2); places[i] = solo ? 1 : new[] { 3, 2, 1, 4 }[i]; }
                if (solo) { recordKey = "TruckBungeeDemolition.PersonalBest"; recordValue = 54321; }
            }
            else if (curling)
            {
                var totals = (int[])Get(match, "scores"); totals[0] = 4; totals[1] = 6; original = totals; Set(match, "EndsScored", 8); unit = "TEAM PTS";
                for (int i = 0; i < count; i++) { expected[i] = totals[i < 4 ? 0 : 1]; winners[i] = i >= 4; places[i] = winners[i] ? 1 : 2; }
            }
            else if (dance)
            {
                var totals = (int[])Get(match, "total"); original = totals; Set(match, "Round", 3);
                for (int i = 0; i < count; i++) { totals[i] = new[] { 400, 900, 900, 200 }[i]; expected[i] = totals[i]; winners[i] = i == 1 || i == 2; places[i] = new[] { 3, 1, 1, 4 }[i]; }
            }
            else if (derby)
            {
                var wins = (int[])Get(owner, "wins"); original = wins; Set(owner, "Winner", 1); Set(owner, "Heat", 6); unit = "WINS";
                var alive = (bool[])Get(owner, "alive"); var reasons = (string[])Get(owner, "reasons");
                for (int i = 0; i < count; i++) { wins[i] = i == 1 ? 3 : 0; expected[i] = wins[i]; winners[i] = alive[i] = i == 1; places[i] = winners[i] ? 1 : 2; reasons[i] = "PHYSICAL CRASH"; }
            }
            else if (fare || snake)
            {
                var scores = (int[])Get(owner, "score"); original = scores; var flags = (bool[])Get(owner, "winners"); unit = fare ? "FARES" : "PTS";
                for (int i = 0; i < count; i++) { scores[i] = (fare ? new[] { 5, 6, 1, 0 } : new[] { 1000, 2000, 2000, 500 })[i]; expected[i] = scores[i]; winners[i] = flags[i] = i == 1 || snake && i == 2; places[i] = (fare ? new[] { 2, 1, 3, 4 } : new[] { 3, 1, 1, 4 })[i]; }
                if (snake)
                {
                    var attached = (int[])Get(owner, "attached"); var distance = (float[])Get(owner, "distance"); var survived = (float[])Get(owner, "survived");
                    for (int i = 0; i < count; i++) { attached[i] = i == 1 ? 10 : 1; distance[i] = 350 + i * 10; survived[i] = 75; }
                    recordKey = "TruckSnake.Best." + Get(((Array)Get(setup, "players")).GetValue(1), "participantId"); recordValue = 91234;
                }
            }
            else
            {
                var active = (bool[])Get(match, "active"); var secured = (int[])Get(match, "secured"); original = active; Set(match, "Winner", 3); Set(match, "Round", 7); Set(match, "TotalCaptures", 28); unit = "";
                for (int i = 0; i < count; i++) { active[i] = winners[i] = i == 3; secured[i] = i == 3 ? 0 : -1; places[i] = winners[i] ? 1 : 0; }
            }
            if (recordKey != null) Runtime("SportsPrefs").GetMethod("SetInt").Invoke(null, new object[] { recordKey, recordValue });
            var before = original.Cast<object>().ToArray(); Call(match, "EndMatch"); Time.timeScale = 0; yield return new WaitForSecondsRealtime(.22f); yield return null;
            var board = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation")); Assert.That(board, Is.Not.Null); Call(board, "CompleteAnimations"); Canvas.ForceUpdateCanvases();
            Assert.That(Get(board, "Displaying"), Is.True); var snapshot = Get(board, "Snapshot"); var entries = ((IEnumerable)Get(snapshot, "Rows")).Cast<object>().ToArray();
            Assert.That(entries.Length, Is.EqualTo(count)); Assert.That(entries.Select(entry => Get(entry, "ParticipantId")).Distinct().Count(), Is.EqualTo(count));
            foreach (var entry in entries)
            {
                int slot = (int)Get(entry, "Slot"); Assert.That(Get(entry, "Place"), Is.EqualTo(places[slot])); Assert.That(Get(entry, "Winner"), Is.EqualTo(winners[slot]));
                Assert.That(Get(entry, "Measurement"), Is.EqualTo(expected[slot])); Assert.That(Get(entry, "Unit"), Is.EqualTo(unit)); Assert.That(Get(entry, "DisplayName"), Is.EqualTo(wide ? new string('W', 40) : "Shared Name"));
                if (parking) Assert.That(Get(entry, "Value"), Is.EqualTo(winners[slot] ? "LAST SPACE" : "ELIMINATED"));
                if (dance) StringAssert.Contains("FINAL SONG", (string)Get(entry, "Detail"));
                if (snake && slot == 1) { StringAssert.Contains("10 ATTACHED · x10", (string)Get(entry, "Detail")); StringAssert.Contains("BEST 91234", (string)Get(entry, "Detail")); }
            }
            if (bungee && solo) StringAssert.Contains("PERSONAL BEST 54321 PTS", (string)Get(snapshot, "Summary"));
            if (recordKey != null) Assert.That(Runtime("SportsPrefs").GetMethod("GetInt").Invoke(null, new object[] { recordKey, 0 }), Is.EqualTo(recordValue), "Results must retain the original record without writing it.");
            var menus = (Component)Get(match, "menus"); var page = ((GameObject[])Get(menus, "pages"))[6]; Assert.That(page.transform.Find("Results readability backing"), Is.Not.Null);
            var views = ((Array)Get(board, "rows")).Cast<object>().Where(view => ((Component)Get(view, "rect")).gameObject.activeSelf).ToArray();
            Action<Camera> inspect = camera =>
            {
                Canvas.ForceUpdateCanvases(); var canvas = Bounds(menus.GetComponent<RectTransform>());
                foreach (string field in new[] { "heading", "summary" }) { var label = Get(board, field); Call(label, "ForceMeshUpdate", false, false); Assert.That(Get(label, "isTextOverflowing"), Is.False, scene + " caption"); }
                for (int i = 0; i < views.Length; i++)
                {
                    var view = views[i]; int rank = (int)Get(entries[i], "Place");
                    Assert.That(Get(Get(view, "place"), "text"), Is.EqualTo(rank > 0 ? "#" + rank : "—"), "Unranked practice/draw entries must not invent an elimination; explicit result values retain the actual outcome.");
                    var row = Bounds((RectTransform)Get(view, "rect")); Assert.That(row.xMin, Is.GreaterThanOrEqualTo(canvas.xMin)); Assert.That(row.xMax, Is.LessThanOrEqualTo(canvas.xMax)); Assert.That(row.yMin, Is.GreaterThanOrEqualTo(canvas.yMin)); Assert.That(row.yMax, Is.LessThanOrEqualTo(canvas.yMax));
                    foreach (var action in page.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>()) Assert.That(row.Overlaps(Bounds(action.GetComponent<RectTransform>())), Is.False);
                    foreach (string heading in new[] { "Title", "Subtitle" }) Assert.That(row.Overlaps(Bounds(page.transform.Find(heading).GetComponent<RectTransform>())), Is.False);
                    foreach (string field in new[] { "name", "value", "detail" }) { var label = Get(view, field); Call(label, "ForceMeshUpdate", false, false); Assert.That(Get(label, "isTextOverflowing"), Is.False, scene + " / " + field); }
                    Assert.That(GlyphBounds(Get(view, "name")).Overlaps(GlyphBounds(Get(view, "value"))), Is.False, "Actual rendered name glyphs must remain clear of the actual result, including long TEAM units.");
                    Assert.That((float)Get(Get(view, "name"), "fontSize"), Is.GreaterThanOrEqualTo(14f - .01f), "Long names must not become illegibly small to fit.");
                    if (wide) Assert.That(Get(Get(view, "name"), "text"), Is.EqualTo(new string('W', 40)), "Bounded display must preserve the complete authored name text.");
                    for (int j = i + 1; j < views.Length; j++) Assert.That(row.Overlaps(Bounds((RectTransform)Get(views[j], "rect"))), Is.False);
                }
            };
            var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                string path = "Logs/TruckSports/PlaytestRepair/LegacyRoundResults/" + scene + (solo ? "-solo" : wide ? "-wide-name" : "") + "-" + size.y + ".png";
                try { save.Invoke(null, new object[] { match, size.x, size.y, path, inspect }); }
                catch (TargetInvocationException)
                {
                    save.Invoke(null, new object[] { match, size.x, size.y, path.Replace(".png", "-assertion-failure.png"), null });
                    throw;
                }
            }
            CollectionAssert.AreEqual(before, original.Cast<object>().ToArray(), "Rendering must not alter the original points, team scores or survivor flags.");
            ((Behaviour)match).enabled = true; Time.timeScale = 1; Call(match, "SetupScreen"); Assert.That(Get(board, "Snapshot"), Is.Null); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; Assert.That(Get(board, "Snapshot"), Is.Null);
            TestContext.WriteLine("SYNTHETIC_LEGACY_ROUND_RESULTS " + scene + " count=" + count + ": explicit synthetic values; actual saved UI/identities/original winners/records/metrics/reset. Not physical-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
