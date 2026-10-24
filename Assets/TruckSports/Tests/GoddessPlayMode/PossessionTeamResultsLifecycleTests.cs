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
public sealed class PossessionTeamResultsLifecycleTests
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
    public IEnumerator SyntheticTeamTotalsKeepIndividualIdentitiesAndRealTeamAssignments()
    {
        foreach (string scene in new[] { "TruckBuzkashi", "TruckKeepaway", "TruckKeepawayFirstPerson", "TruckKickball", "TruckTrampolineDodgeball" }) yield return Inspect(scene);
        yield return Inspect("TruckKeepaway", true);
        yield return Inspect("TruckKickball", true);
        yield return Inspect("TruckTrampolineDodgeball", true);
        yield return Inspect("TruckBuzkashi", wide: true);
        yield return Inspect("TruckKickball", wide: true);
    }
    static IEnumerator Inspect(string scene, bool smaller = false, bool wide = false)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticPossessionTeamResults" });
        var isolation = new GameObject("Explicit synthetic final values; real saved results UI"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        bool keep = scene.StartsWith("TruckKeepaway"), kick = scene == "TruckKickball", dodge = scene == "TruckTrampolineDodgeball";
        try
        {
            string folder = keep ? "TruckKeepaway" : scene;
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + folder + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup");
            Set(setup, "count", keep && smaller ? 2 : Get(match, "MaximumPlayers"));
            if (kick) Set(match, "option", smaller ? 0 : 3);
            if (dodge) { Set(match, "teamA", smaller ? 1 : 4); Set(match, "teamB", smaller ? 3 : 4); }
            Call(match, "AllBots");
            int count = (int)Get(setup, "count");
            foreach (object player in ((Array)Get(setup, "players")).Cast<object>().Take(count)) Set(player, "name", wide ? new string('W', 40) : "Shared Name");
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0;
            Assert.That(count, Is.EqualTo(smaller ? keep ? 2 : kick ? 6 : 4 : keep ? 4 : kick ? 12 : dodge ? 8 : 6));
            object owner = keep || !kick && !dodge ? (object)match : Get(match, "rules");
            Array original;
            if (keep)
            {
                Set(match, "Winner", 1); Set(match, "Steals", 7); Set(match, "Pickups", 3); Set(match, "Blasts", 2);
                var possession = (float[])Get(match, "possession"); possession[0] = 58.5f; possession[1] = (float)Get(match, "Target"); possession[2] = possession[3] = 35;
                original = possession;
            }
            else if (kick)
            {
                var scores = (int[])Get(owner, "Scores"); scores[0] = 10; scores[1] = 8;
                Set(owner, "Inning", 3); Set(owner, "Phase", Enum.Parse(Get(owner, "Phase").GetType(), "Complete")); original = scores;
            }
            else if (dodge)
            {
                var wins = (int[])Get(owner, "wins"); wins[0] = 2; wins[1] = 3; Set(owner, "Winner", 1);
                var alive = (bool[])Get(owner, "alive"); for (int i = 0; i < count; i++) alive[i] = (int)Call(owner, "Team", i) == 1 && i % 2 == 1;
                original = wins;
            }
            else
            {
                var scores = (int[])Get(match, "score"); scores[0] = 4; scores[1] = 5; Set(match, "Winner", 1); Set(match, "Deliveries", 9); Set(match, "Breaks", 3);
                var teams = (string[])Get(match, "teams"); teams[0] = "Cargo Crew A"; teams[1] = "Cargo Crew B"; original = scores;
            }
            var before = original.Cast<object>().ToArray(); Call(match, "EndMatch"); Time.timeScale = 0; yield return new WaitForSecondsRealtime(.22f); yield return null;
            var board = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation")); Assert.That(board, Is.Not.Null); Call(board, "CompleteAnimations"); Canvas.ForceUpdateCanvases();
            Assert.That(Get(board, "Displaying"), Is.True); var snapshot = Get(board, "Snapshot"); var entries = ((IEnumerable)Get(snapshot, "Rows")).Cast<object>().ToArray();
            Assert.That(entries.Length, Is.EqualTo(count)); Assert.That(entries.Select(entry => Get(entry, "ParticipantId")).Distinct().Count(), Is.EqualTo(count));
            int winningMembers = 0;
            foreach (var entry in entries)
            {
                int slot = (int)Get(entry, "Slot"), team = keep ? -1 : kick || dodge ? (int)Call(owner, "Team", slot) : slot < 3 ? 0 : 1;
                bool winner = keep ? slot == 1 : team == (kick ? 0 : 1); if (winner) winningMembers++;
                double measurement = Convert.ToDouble(original.GetValue(keep ? slot : team)); string unit = keep ? "SEC" : kick ? "TEAM RUNS" : dodge ? "TEAM WINS" : "TEAM DELIVERIES";
                int place = keep ? slot == 1 ? 1 : slot == 0 ? 2 : 3 : winner ? 1 : 2;
                Assert.That(Get(entry, "Place"), Is.EqualTo(place)); Assert.That(Get(entry, "Winner"), Is.EqualTo(winner)); Assert.That(Get(entry, "Measurement"), Is.EqualTo(measurement)); Assert.That(Get(entry, "Unit"), Is.EqualTo(unit)); Assert.That(Get(entry, "DisplayName"), Is.EqualTo(wide ? new string('W', 40) : "Shared Name"));
                string detail = (string)Get(entry, "Detail");
                StringAssert.Contains(keep ? "CUMULATIVE POSSESSION" : kick ? team == 0 ? "BLUE BANDITS" : "ORANGE OUTLAWS" : dodge ? "TEAM " + (team + 1) : "Cargo Crew " + (team == 0 ? "A" : "B"), detail);
            }
            Assert.That(winningMembers, Is.EqualTo(keep ? 1 : kick ? count / 2 : dodge ? smaller ? 3 : 4 : 3));
            var menus = (Component)Get(match, "menus"); var page = ((GameObject[])Get(menus, "pages"))[6]; Assert.That(page.transform.Find("Results readability backing"), Is.Not.Null);
            var views = ((Array)Get(board, "rows")).Cast<object>().Where(view => ((Component)Get(view, "rect")).gameObject.activeSelf).ToArray();
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
                    Assert.That(GlyphBounds(Get(view, "name")).Overlaps(GlyphBounds(Get(view, "value"))), Is.False, "Actual rendered name glyphs must remain clear of the actual result, including long TEAM units.");
                    Assert.That((float)Get(Get(view, "name"), "fontSize"), Is.GreaterThanOrEqualTo(14f - .01f), "Long names must not become illegibly small to fit.");
                    if (wide) Assert.That(Get(Get(view, "name"), "text"), Is.EqualTo(new string('W', 40)), "Bounded display must preserve the complete authored name text.");
                    for (int j = i + 1; j < views.Length; j++) Assert.That(row.Overlaps(Bounds((RectTransform)Get(views[j], "rect"))), Is.False);
                }
            };
            var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                string path = "Logs/TruckSports/PlaytestRepair/PossessionTeamResults/" + scene + (smaller ? "-smaller" : wide ? "-wide-name" : "") + "-" + size.y + ".png";
                try { save.Invoke(null, new object[] { match, size.x, size.y, path, inspect }); }
                catch (TargetInvocationException)
                {
                    save.Invoke(null, new object[] { match, size.x, size.y, path.Replace(".png", "-assertion-failure.png"), null });
                    throw;
                }
            }
            CollectionAssert.AreEqual(before, original.Cast<object>().ToArray(), "Repeated team metrics and display animations must not award points or mutate possession.");
            ((Behaviour)match).enabled = true; Time.timeScale = 1; Call(match, "SetupScreen"); Assert.That(Get(board, "Snapshot"), Is.Null); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; Assert.That(Get(board, "Snapshot"), Is.Null);
            TestContext.WriteLine("SYNTHETIC_POSSESSION_TEAM_RESULTS " + scene + " count=" + count + ": explicit synthetic values; actual saved UI/identities/team assignments/metrics/reset. Not physical-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
