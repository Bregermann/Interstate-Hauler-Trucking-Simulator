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

public sealed class WallballPresentationLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v)).Invoke(owner, args);
    static Rect Pixels(RectTransform rect, Camera ui) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); var a = RectTransformUtility.WorldToScreenPoint(ui, corners[0]); var b = RectTransformUtility.WorldToScreenPoint(ui, corners[2]); return Rect.MinMaxRect(a.x, a.y, b.x, b.y); }
    public sealed class Receipts { public readonly List<object> items = new List<object>(); public void Record(object receipt) => items.Add(receipt); }
    static IEnumerator Load()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWallball/Scenes/TruckWallball.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
    }
    static void Capture(Component menus, Camera camera, string name, Action<Camera, int, int> inspect = null)
    {
        var save = Runtime("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 6);
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
        {
            Action<Camera> check = ui => inspect?.Invoke(ui, size.x, size.y);
            save.Invoke(null, new object[] { menus.GetComponent<Canvas>(), new[] { camera }, size.x, size.y, "Logs/TruckSports/PlaytestRepair/WallballTeamPresentation/" + name + "-" + size.y + ".png", check });
        }
    }

    [UnityTest, Timeout(480000)]
    public IEnumerator ActualFourVersusFourRalliesPublishOnlyOriginalTeamPointsAndSharedRankedResults()
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "WallballOriginalTeamPresentation" });
        Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        var isolation = new GameObject("Wallball actual physical team presentation acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null; EventInfo signal = null; Delegate subscription = null; var receipts = new Receipts();
        try
        {
            yield return Load();
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckWallball.WallballMatch, Assembly-CSharp", true));
            Set(match, "blueSize", 4); Set(match, "orangeSize", 4);
            var mode = Type.GetType("LWS.TruckWallball.WallballMode, Assembly-CSharp", true); Call(match, "Configure", Enum.Parse(mode, "AIVsAI"));
            var players = (Array)Get(match, "players");
            for (int i = 0; i < 8; i++)
            {
                var player = players.GetValue(i); Set(player, "difficulty", Enum.Parse(Get(player, "difficulty").GetType(), "Hard"));
                var identity = Runtime("SportsParticipantRegistry").GetMethod("Default").Invoke(null, new object[] { i });
                Runtime("SportsParticipantRegistry").GetMethod("Rename").Invoke(null, new object[] { Get(identity, "id"), "Same Name <crew>", Get(player, "color"), true });
            }
            Call(match, "RefreshParticipants", false);
            var menus = (Component)Get(match, "menus"); var panel = menus.GetComponentsInChildren(Runtime("SportsScorePresentation"), true).Cast<Component>().Single();
            var board = menus.GetComponentsInChildren(Runtime("SportsResultsPresentation"), true).Cast<Component>().Single(); var source = Get(board, "source");
            Assert.That(Get(panel, "session"), Is.Null); Assert.That(Get(panel, "alternateSession"), Is.SameAs(match)); Assert.That(Get(panel, "viewport"), Is.EqualTo(-1));
            Assert.That(Get(source, "wallball"), Is.SameAs(match)); Assert.That(Get(source, "session"), Is.Null); Assert.That(Get(source, "cornhole"), Is.Null);
            var oldActions = ((GameObject[])Get(menus, "pages"))[8].GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>().ToArray();
            Assert.That(oldActions.Select(a => (string)Get(a, "action")), Is.EquivalentTo(new[] { "rematch", "setup", "front" }));
            var visual = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckWallball.WallballPresentation, Assembly-CSharp", true)); var camera = (Camera)Get(visual, "cameraTarget");
            signal = match.GetType().GetEvent("ScoreConfirmed"); subscription = Delegate.CreateDelegate(signal.EventHandlerType, receipts, typeof(Receipts).GetMethod("Record")); signal.AddEventHandler(match, subscription);
            Assert.That(Call(match, "StartMatch", false), Is.True); Time.timeScale = 4; var rules = Get(match, "rules"); float deadline = Time.realtimeSinceStartup + 340;
            while (receipts.items.Count == 0 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(receipts.items.Count, Is.GreaterThan(0), "A real original AI rally must resolve before presentation is inspected.");
            ((Behaviour)match).enabled = false; Time.timeScale = 0; int firstCount = receipts.items.Count; int[] firstScore = (int[])((int[])Get(rules, "Score")).Clone();
            Call(match, "ResolvePoint"); Assert.That(receipts.items.Count, Is.EqualTo(firstCount)); Assert.That((int[])Get(rules, "Score"), Is.EqualTo(firstScore));
            object[] incomplete = { null }; Assert.That(source.GetType().GetMethod("TryCapture").Invoke(source, incomplete), Is.False, "A point is not an invented final result.");
            Call(match, "Pause", false); var position = ((Rigidbody)Get(Get(match, "ball"), "body")).position; yield return new WaitForSecondsRealtime(.12f);
            Assert.That(receipts.items.Count, Is.EqualTo(firstCount)); Assert.That(((Rigidbody)Get(Get(match, "ball"), "body")).position, Is.EqualTo(position)); Call(match, "Resume");
            Time.timeScale = 1; float showDeadline = Time.realtimeSinceStartup + 10;
            while (Get(panel, "LastPresented") == null && Time.realtimeSinceStartup < showDeadline) yield return null;
            Assert.That(receipts.items, Does.Contain(Get(panel, "LastPresented"))); yield return new WaitForSecondsRealtime(1);
            Time.timeScale = 0; Capture(menus, camera, "actual-first-team-point");
            ((Behaviour)match).enabled = true; Time.timeScale = 4;
            while (!(bool)Get(rules, "Active") && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(Get(rules, "Active"), Is.True); int pointsBeforeReplay = (int)Get(rules, "Points"); int receiptsBeforeReplay = receipts.items.Count;
            Call(match, "Replay", "CONTROLLED NEUTRAL REPLAY"); Assert.That(Get(rules, "Points"), Is.EqualTo(pointsBeforeReplay)); Assert.That(receipts.items.Count, Is.EqualTo(receiptsBeforeReplay));
            while (Get(rules, "Phase").ToString() != "Finished" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get(rules, "Phase").ToString(), Is.EqualTo("Finished"), "The original physical 4v4 match must finish without injected points, shots or poses.");
            Assert.That(Get(rules, "Shots"), Is.GreaterThan(0)); Assert.That(Get(Get(match, "ball"), "TruckContacts"), Is.GreaterThan(0)); Assert.That(Get(rules, "LegalWalls"), Is.GreaterThan(0));
            Assert.That(Time.timeScale, Is.Zero, "Preserve Wallball's original frozen-results flow.");
            var totals = new double[2]; var ids = new HashSet<string>(); int champions = 0;
            foreach (var receipt in receipts.items)
            {
                string teamId = (string)Get(receipt, "TeamId"); int team = teamId == "blue" ? 0 : teamId == "orange" ? 1 : -1; Assert.That(team, Is.InRange(0, 1));
                Assert.That(ids.Add((string)Get(receipt, "EventId")), Is.True); Assert.That(Get(receipt, "IsTeam"), Is.True); Assert.That(Get(receipt, "ParticipantId"), Is.Null); Assert.That(Get(receipt, "Slot"), Is.EqualTo(-1));
                Assert.That(Get(receipt, "RecipientId"), Is.EqualTo("team:" + teamId)); Assert.That(Get(receipt, "MatchId"), Is.EqualTo(Get(match, "MatchId"))); Assert.That(Get(receipt, "DisplayName"), Is.EqualTo(Call(match, "TeamName", team)));
                Assert.That(Get(receipt, "Delta"), Is.EqualTo(1d)); totals[team]++; Assert.That(Get(receipt, "Total"), Is.EqualTo(totals[team])); Assert.That(Get(receipt, "Unit"), Is.EqualTo("TEAM PTS")); Assert.That(Get(receipt, "RoundEnded"), Is.True);
                Assert.That((string)Get(receipt, "Reason"), Is.Not.Empty); Assert.That(Get(receipt, "Kind").ToString(), Is.EqualTo("Points"));
                if ((bool)Get(receipt, "MatchEnded")) { champions++; Assert.That(team, Is.EqualTo(Get(rules, "Winner"))); Assert.That(Get(receipt, "Tier").ToString(), Is.EqualTo("MatchChampion")); }
                else Assert.That(Get(receipt, "Tier").ToString(), Is.EqualTo("Major"));
            }
            var scores = (int[])((int[])Get(rules, "Score")).Clone(); for (int team = 0; team < 2; team++) Assert.That(totals[team], Is.EqualTo(scores[team]));
            Assert.That(champions, Is.EqualTo(1)); Assert.That(receipts.items.Count, Is.EqualTo(Get(rules, "Points"))); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(receipts.items.Count));
            int finalCount = receipts.items.Count; Call(match, "ResolvePoint"); Assert.That(receipts.items.Count, Is.EqualTo(finalCount));
            // Do not unpause the match to make a final notification pass.
            showDeadline = Time.realtimeSinceStartup + 5;
            while ((Get(panel, "LastPresented") == null || !(bool)Get(Get(panel, "LastPresented"), "MatchEnded")) && Time.realtimeSinceStartup < showDeadline) yield return null;
            Assert.That(Get(panel, "LastPresented"), Is.Not.Null); Assert.That(Get(Get(panel, "LastPresented"), "MatchEnded"), Is.True, "The original final team point must display while the original result pause remains active.");
            var awardAnimator = (Animator)Get(panel, "notificationAnimator");
            yield return new WaitForSecondsRealtime(awardAnimator.runtimeAnimatorController.animationClips.Single(clip => clip.name == "Notification_In").length + .08f); yield return null;
            Assert.That(Time.timeScale, Is.Zero, "Heat's authored unscaled entrance must finish without unpausing original Wallball results.");
            var notice = (Component)Get(panel, "notification"); var message = (Component)Get(notice, "textObj"); Call(message, "ForceMeshUpdate", false, false); Canvas.ForceUpdateCanvases();
            Assert.That(notice.gameObject.activeInHierarchy && message.gameObject.activeInHierarchy, Is.True); Assert.That(message.GetComponent<CanvasRenderer>().GetInheritedAlpha(), Is.GreaterThan(.95f)); Assert.That(((Color)Get(message, "color")).a, Is.GreaterThan(.95f));
            StringAssert.Contains((string)Get(Get(panel, "LastPresented"), "Reason"), (string)Get(message, "text")); Assert.That(Get(message, "isTextOverflowing"), Is.False);
            var noticeRect = ((RectTransform)notice.transform).rect; var messageRect = ((RectTransform)Get(message, "rectTransform")).rect; Assert.That(noticeRect.width, Is.GreaterThan(600)); Assert.That(noticeRect.height, Is.GreaterThan(50)); Assert.That(messageRect.width, Is.GreaterThan(100)); Assert.That(messageRect.height, Is.GreaterThan(10));
            Call(board, "CompleteAnimations"); Canvas.ForceUpdateCanvases();
            Assert.That(Get(board, "Displaying"), Is.True); Assert.That(Get(Get(source, "OriginalResult"), "enabled"), Is.False); Assert.That(Get(menus, "Screen").ToString(), Is.EqualTo("Result"));
            var snapshot = Get(board, "Snapshot"); var rows = ((IEnumerable)Get(snapshot, "Rows")).Cast<object>().ToArray(); Assert.That(rows.Length, Is.EqualTo(8)); Assert.That(rows.Select(r => (string)Get(r, "ParticipantId")).Distinct().Count(), Is.EqualTo(8));
            foreach (var row in rows)
            {
                int slot = (int)Get(row, "Slot"); int team = (int)Call(match, "Team", slot); bool winner = team == (int)Get(rules, "Winner");
                Assert.That(Get(row, "ParticipantId"), Is.EqualTo(Get(players.GetValue(slot), "participantId"))); Assert.That(Get(row, "DisplayName"), Is.EqualTo("Same Name <crew>"));
                Assert.That(Get(row, "Color"), Is.EqualTo(Get(players.GetValue(slot), "color"))); Assert.That(Get(row, "IsAI"), Is.True); Assert.That(Get(row, "Measurement"), Is.EqualTo((double)scores[team])); Assert.That(Get(row, "Unit"), Is.EqualTo("TEAM PTS"));
                Assert.That(Get(row, "Place"), Is.EqualTo(winner ? 1 : 2)); Assert.That(Get(row, "Winner"), Is.EqualTo(winner)); StringAssert.Contains("SHARED TEAM TOTAL", (string)Get(row, "Detail"));
            }
            var rowViews = (Array)Get(board, "rows"); var title = (RectTransform)Get(menus, "resultTitle"); var subtitle = ((GameObject[])Get(menus, "pages"))[8].transform.Find("Subtitle") as RectTransform;
            Capture(menus, camera, "actual-four-versus-four-results", (ui, width, height) =>
            {
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < rows.Length; i++)
                {
                    var view = rowViews.GetValue(i); var bounds = Pixels((RectTransform)Get(view, "rect"), ui);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(-2)); Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(-2)); Assert.That(bounds.xMax, Is.LessThanOrEqualTo(width + 2)); Assert.That(bounds.yMax, Is.LessThanOrEqualTo(height + 2));
                    foreach (var action in oldActions) Assert.That(bounds.Overlaps(Pixels((RectTransform)action.transform, ui)), Is.False, "Real ranked rows preserve all original result controls.");
                    Assert.That(bounds.Overlaps(Pixels(title, ui)), Is.False); Assert.That(bounds.Overlaps(Pixels(subtitle, ui)), Is.False);
                    foreach (string text in new[] { "value", "detail", "place" }) { var label = Get(view, text); Call(label, "ForceMeshUpdate", false, false); Assert.That(Get(label, "isTextOverflowing"), Is.False); }
                    Assert.That(Get(Get(view, "value"), "text"), Is.EqualTo(Get(rows[i], "Value"))); StringAssert.DoesNotContain("<crew>", (string)Get(Get(view, "name"), "text"));
                }
                Assert.That(Pixels((RectTransform)panel.transform, ui).Overlaps(Pixels(title, ui)), Is.False, "Final score receipt must clear the retained original result title.");
            });
            Assert.That((int[])Get(rules, "Score"), Is.EqualTo(scores)); Assert.That(receipts.items.Count, Is.EqualTo(finalCount), "Repeated render/capture never awards points.");
            string previousMatch = (string)Get(match, "MatchId"); int opener = (int)Get(rules, "Server");
            // Follow the original menu action, including its existing fresh-confirm gate.
            yield return null; yield return null; Call(menus, "Action", "rematch"); yield return null; yield return null;
            Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(previousMatch)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero); Assert.That((int[])Get(rules, "Score"), Is.EqualTo(new int[2])); Assert.That(Get(panel, "LastPresented"), Is.Null); Assert.That(Get(board, "Snapshot"), Is.Null); Assert.That(Get(Get(source, "OriginalResult"), "enabled"), Is.True);
            Assert.That(Get(rules, "Server"), Is.EqualTo(1), "First opener0 alternates to1 through the original rematch action, independent of the prior winner/server.");
            TestContext.WriteLine("ACTUAL_WALLBALL_TEAM_PRESENTATION score=" + string.Join(",", scores) + " receipts=" + finalCount + " finalServer=" + opener + "; original physical4v4AI match, team-only receipts, exact score reconstruction, pause, explicit neutral replay, frozen-result final receipt, eight stable participant rows, all original actions and rematch. No injected points/poses/forces, human/device or Windows acceptance.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { if (subscription != null) signal.RemoveEventHandler(match, subscription); ((Behaviour)match).enabled = true; Call(match, "ChangeSetup"); }
            UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }
}
#endif
