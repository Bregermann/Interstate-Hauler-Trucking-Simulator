#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CarnivalLeaderboardLifecycleTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static object At(object owner, string name, int index) => ((Array)Get(owner, name)).GetValue(index);
    static string Text(object owner, string field) => (string)Get(Get(owner, field), "text");
    static Component[] Components(Scene scene) => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).Where(component => component != null).ToArray();
    static Rect Pixels(RectTransform rect, Camera ui)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var points = corners.Select(corner => RectTransformUtility.WorldToScreenPoint(ui, corner)).ToArray();
        return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y), points.Max(point => point.x), points.Max(point => point.y));
    }
    static void Within(Rect actual, Rect parent)
    {
        Assert.That(actual.xMin, Is.GreaterThanOrEqualTo(parent.xMin - 2)); Assert.That(actual.xMax, Is.LessThanOrEqualTo(parent.xMax + 2));
        Assert.That(actual.yMin, Is.GreaterThanOrEqualTo(parent.yMin - 2)); Assert.That(actual.yMax, Is.LessThanOrEqualTo(parent.yMax + 2));
    }
    static void AssertRows(Component match, Component hud, int count)
    {
        Assert.That(Get(hud, "VisibleCards"), Is.EqualTo(count));
        var cards = (Array)Get(hud, "cards"); var ids = new System.Collections.Generic.HashSet<string>();
        for (int rank = 0; rank < count; rank++)
        {
            object row = Call(hud, "Row", rank); string id = (string)Get(row, "id"); Assert.That(ids.Add(id), Is.True, "Every scored competitor needs a distinct stable identity.");
            int participant = (int)Get(row, "participant"); var card = cards.GetValue(match.GetType().Name == "WifeMatch" ? participant % 4 : participant);
            Assert.That(Get(card, "id"), Is.EqualTo(id)); Assert.That(Get(card, "rank"), Is.EqualTo(rank));
            Assert.That(Text(card, "name"), Is.EqualTo(Get(row, "name"))); Assert.That(Text(card, "value"), Is.EqualTo(Get(row, "value"))); Assert.That(Text(card, "detail"), Is.EqualTo(Get(row, "detail")));
            var portrait = (UnityEngine.UI.Image)Get(card, "portrait"); Assert.That(portrait.sprite, Is.Not.Null); Assert.That(((UnityEngine.UI.Image)Get(card, "stripe")).color, Is.EqualTo(Get(row, "color")));
            var player = At(Get(match, "setup"), "players", participant);
            if (match.GetType().Name != "WifeMatch") Assert.That(Get(row, "name"), Is.EqualTo(Get(player, "name")));
            if (match.GetType().Name == "BaseJumpMatch")
            {
                Assert.That(Get(row, "place"), Is.EqualTo(Call(match, "ResultPlace", participant)));
                Assert.That(Get(row, "value"), Is.EqualTo(((int[])Get(match, "totals"))[participant] + " PTS"));
            }
            if (match.GetType().Name == "MattressMatch" && (int)Get(row, "group") < 2) Assert.That(Get(row, "place"), Is.EqualTo(Call(match, "RunningPlace", participant)));
            if (match.GetType().Name == "WifeMatch" && (int)Get(row, "group") < 2) Assert.That(Get(row, "place"), Is.EqualTo(Call(match, "RunningPlace", participant % 4)));
            if (match.GetType().Name == "AvalancheMatch") Assert.That(Get(row, "place"), Is.EqualTo(Call(Get(match, "rules"), "Place", participant)));
            if (match.GetType().Name == "TerrierMatch")
            {
                float confirmed = (float)Call(match, "ConfirmedHeatFinishSeconds", participant);
                if (float.IsFinite(confirmed)) Assert.That(Get(row, "seconds"), Is.EqualTo(confirmed));
                else Assert.That(Get(row, "seconds"), Is.EqualTo(Get(match, "HeatTime")));
            }
        }
    }
    static void Capture(Component match, Component hud, string scene, string stage)
    {
        var menus = (Component)Get(match, "menus"); var cameras = (Camera[])Get(match, "cameras");
        var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 6);
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
        {
            Action<Camera> inspect = ui =>
            {
                Canvas.ForceUpdateCanvases(); Rect screen = new Rect(0, 0, size.x, size.y);
                var table = (RectTransform)Get(hud, "table"); Rect tableBounds = Pixels(table, ui); Within(tableBounds, screen);
                var visible = ((Array)Get(hud, "cards")).Cast<object>().Where(card => ((RectTransform)Get(card, "root")).gameObject.activeInHierarchy).ToArray();
                var bounds = visible.Select(card => Pixels((RectTransform)Get(card, "root"), ui)).ToArray();
                for (int i = 0; i < visible.Length; i++)
                {
                    Within(bounds[i], tableBounds);
                    for (int other = 0; other < i; other++) Assert.That(bounds[i].Overlaps(bounds[other]), Is.False, "Ranked cards overlap after reorder.");
                    foreach (string field in new[] { "name", "place", "value", "detail" })
                    {
                        var text = (Component)Get(visible[i], field); Call(text, "ForceMeshUpdate", false, false);
                        Within(Pixels(text.GetComponent<RectTransform>(), ui), bounds[i]);
                        Assert.That(Get(text, "isTextOverflowing"), Is.False, scene + " " + field + ": " + Get(text, "text"));
                    }
                    var fields = new[] { "name", "place", "value", "detail" }.Select(field => Pixels(((Component)Get(visible[i], field)).GetComponent<RectTransform>(), ui)).ToArray();
                    for (int a = 0; a < fields.Length; a++) for (int b = a + 1; b < fields.Length; b++) Assert.That(fields[a].Overlaps(fields[b]), Is.False, "Fields overlap inside competitor card.");
                }
                Rect context = Pixels((RectTransform)Get(hud, "contextRoot"), ui); Within(context, screen); Assert.That(context.Overlaps(tableBounds), Is.False);
            };
            save.Invoke(null, new object[] { menus.GetComponent<Canvas>(), cameras, size.x, size.y, "Logs/TruckSports/PlaytestRepair/CarnivalLeaderboards/" + scene + "-" + stage + "-" + size.y + ".png", inspect });
        }
    }
    static void ProtectedViews(Component match, Component hud)
    {
        bool baseJump = match.GetType().Name == "BaseJumpMatch";
        var rigs = (Array)Get(baseJump ? (object)match : Get(match, "views"), "views");
        var original = (Camera[])Get(match, "cameras");
        var all = Components(match.gameObject.scene).OfType<Camera>().ToArray();
        var old = all.Select(camera => (camera, camera.enabled, camera.rect)).ToArray();
        try
        {
            for (int count = 2; count <= 4; count++)
            {
                foreach (var camera in all) camera.enabled = false;
                var cameras = new Camera[count];
                for (int i = 0; i < count; i++)
                {
                    cameras[i] = (Camera)Get(rigs.GetValue(i), "view"); cameras[i].enabled = true;
                    cameras[i].rect = count == 2 ? new Rect(i * .5f, 0, .5f, 1) : new Rect((i % 2) * .5f, i < 2 ? .5f : 0, .5f, .5f);
                }
                Set(match, "cameras", cameras); Call(hud, "LateUpdate");
                Assert.That(Get(hud, "Showing"), Is.False); Assert.That(Get(Get(hud, "policy"), "SuppressedGraphics"), Is.Zero);
                // The new presenter must be a complete no-op against existing split-view components.
                var originals = Components(match.gameObject.scene).Where(component => (component is UnityEngine.UI.Graphic || component is RectTransform) && !component.transform.IsChildOf((RectTransform)Get(hud, "root"))).ToArray();
                var before = originals.Select(component => EditorJsonUtility.ToJson(component)).ToArray(); Call(hud, "LateUpdate");
                for (int i = 0; i < originals.Length; i++) Assert.That(EditorJsonUtility.ToJson(originals[i]), Is.EqualTo(before[i]), "Protected split component changed: " + originals[i].name);
            }
        }
        finally { foreach (var saved in old) { saved.camera.enabled = saved.enabled; saved.camera.rect = saved.rect; } Set(match, "cameras", original); Call(hud, "LateUpdate"); }
    }

    [UnityTest, Timeout(180000)] public IEnumerator BaseAllFourActualBankedRoundPointsInBottomRightAndProtectedSplit() => Inspect("TruckBaseJumping", 4);
    [UnityTest, Timeout(180000)] public IEnumerator MattressAllEightNativeRacersAndProtectedSplit() => Inspect("TruckMattressRacing", 8);
    [UnityTest, Timeout(180000)] public IEnumerator WifeFourActualPairOwnersAndRouteProgress() => Inspect("TruckWifeCarrying", 4);
    [UnityTest, Timeout(180000)] public IEnumerator BogAllFourNativeRacersKeepAirAndRecovery() => Inspect("TruckBogSnorkelling", 4);
    [UnityTest, Timeout(180000)] public IEnumerator TerrierAllTwelveNativeRacersWithoutLegacyTruncation() => Inspect("TruckTerrierRacing", 12);
    [UnityTest, Timeout(180000)] public IEnumerator AvalancheAllTwelveUseAuthoritativeDistanceAndSnowDanger() => Inspect("TruckAvalancheEscape", 12);
    [UnityTest, Timeout(180000)] public IEnumerator RedLightAllTwelveKeepActualSignalsAndLegalResults() => Inspect("TruckRedLightGreenLight", 12);
    static IEnumerator Inspect(string mode, int count)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CarnivalLeaderboard" + Guid.NewGuid().ToString("N") });
        Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        Component match = null;
        try
        {
            Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { false });
            var scene = EditorSceneManager.LoadSceneInPlayMode("Assets/" + mode + "/Scenes/" + mode + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            var all = Components(scene); var hud = all.Single(component => component.GetType() == Runtime("SportsCarnivalLeaderboard")); match = (Component)Get(Get(hud, "source"), "match");
            int rosterCount = mode == "TruckWifeCarrying" ? 8 : count; Set(Get(match, "setup"), "count", rosterCount); Call(match, "AllBots"); Assert.That(Call(match, "StartMatch"), Is.True);
            var bodies = all.OfType<Rigidbody>().Where(body => body.GetComponent(Runtime("SportsEventTruck")) != null).ToArray(); var positions = bodies.Select(body => body.position).ToArray();
            Assert.That(bodies.Length, Is.GreaterThanOrEqualTo(rosterCount));
            float deadline = Time.realtimeSinceStartup + 100;
            if (mode == "TruckBaseJumping")
                while ((int)Get(match, "RoundLocks") == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            else
                while ((float)Get(match, "RaceStartSeconds") < 5 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Native gameplay did not advance to an observable physical run.");
            Assert.That(bodies.Where((body, i) => Vector3.Distance(body.position, positions[i]) > .1f).Any(), Is.True, "Observe actual native body travel before inspecting presentation.");
            Time.timeScale = 0; ((Behaviour)match).enabled = false; yield return null;
            // Gameplay can bank/advance immediately after the previous ten-Hz HUD sample.
            // Let its existing bounded cadence publish the frozen authoritative state, then
            // compare exact points and times; do not change production pacing for a fixture.
            var priorRow = Call(hud, "Row", 0); float priorSeconds = (float)Get(priorRow, "seconds"); string priorValue = (string)Get(priorRow, "value");
            yield return new WaitForSecondsRealtime(.12f);
            Call(hud, "LateUpdate"); Assert.That(Get(hud, "Showing"), Is.True); AssertRows(match, hud, count);
            var freshRow = Call(hud, "Row", 0); TestContext.WriteLine("LEADERBOARD_FROZEN_REFRESH mode="+mode+" priorValue="+priorValue+" priorSeconds="+priorSeconds.ToString("R")+" refreshedValue="+Get(freshRow,"value")+" refreshedSeconds="+((float)Get(freshRow,"seconds")).ToString("R")+" frozenRaceStartSeconds="+((float)Get(match,"RaceStartSeconds")).ToString("R"));
            foreach (var graphic in (UnityEngine.UI.Graphic[])Get(Get(hud, "policy"), "legacyGraphics")) Assert.That(graphic.enabled, Is.False, "Remove the original instruction panel as well as its text.");
            int receipts = (int)Get(Get(match, "ScoreJournal"), "Count"); string matchId = (string)Get(match, "MatchId");
            yield return new WaitForSecondsRealtime(.7f); AssertRows(match, hud, count); Capture(match, hud, mode, "actual-native-run");
            if (mode == "TruckBaseJumping" || mode == "TruckMattressRacing") { ProtectedViews(match, hud); yield return new WaitForSecondsRealtime(.4f); AssertRows(match, hud, count); }
            Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(receipts), "Reading and reordering must never award points.");
            Call(match, "Pause", 0, false); Call(hud, "LateUpdate"); Assert.That(Get(hud, "Showing"), Is.False); Assert.That(Get(Get(hud, "policy"), "SuppressedGraphics"), Is.Zero);
            Call(match, "Resume"); Time.timeScale = 0; Call(hud, "LateUpdate"); AssertRows(match, hud, count);
            Call(match, "SetupScreen"); Call(match, "StartMatch"); Time.timeScale = 0; Call(hud, "LateUpdate"); Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(matchId)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero); AssertRows(match, hud, count);
            TestContext.WriteLine("CARNIVAL_LEADERBOARD_NATIVE mode=" + mode + " visible=" + count + "; actual native body travel and banked Base round before frozen software-controlled720/1080 captures; stable identities, original points/time/progress, portrait/color, compact nonoverlap, pause/rematch, no read-time awards. Protected Base/Mattress2–4-view component no-op checks. Not human/device/full-race/visual approval.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); }
            prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }
}
#endif
