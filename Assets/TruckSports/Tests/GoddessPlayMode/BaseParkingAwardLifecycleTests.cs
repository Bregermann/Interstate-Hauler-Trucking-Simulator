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

public sealed class BaseParkingAwardLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v)).Invoke(owner, args);
    static Rect Bounds(RectTransform rect) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y); }
    static Rect Pixels(RectTransform rect, Camera camera) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); var low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]); var high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]); return Rect.MinMaxRect(low.x, low.y, high.x, high.y); }
    static Component Match() => (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));
    static Component[] Panels(Component match) => ((Component)Get(match, "menus")).GetComponentsInChildren(Runtime("SportsScorePresentation"), true).Cast<Component>().OrderBy(p => (int)Get(p, "viewport")).ToArray();
    static IDisposable Preferences(string name)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { name });
        Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        return scope;
    }
    static GameObject IsolateInputs()
    {
        var owner = new GameObject("Base and Parking award source isolation");
        UnityEngine.Object.DontDestroyOnLoad(owner); owner.AddComponent(Runtime("SportsProbeInputIsolation")); return owner;
    }
    static Array Prepare(Component match, bool showAI)
    {
        Call(match, "AllBots"); Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { showAI });
        var players = (Array)Get(Get(match, "setup"), "players");
        for (int i = 0; i < (int)Get(Get(match, "setup"), "count"); i++)
        {
            var player = players.GetValue(i); Set(player, "difficulty", Enum.Parse(Get(player, "difficulty").GetType(), "Hard"));
            var identity = Runtime("SportsParticipantRegistry").GetMethod("Default").Invoke(null, new object[] { i });
            Runtime("SportsParticipantRegistry").GetMethod("Rename").Invoke(null, new object[] { Get(identity, "id"), "Same Award Name", Get(player, "color"), true });
        }
        Call(match, "RefreshParticipants", false); return players;
    }
    static void Identity(object receipt, Component match, Array players)
    {
        int slot = (int)Get(receipt, "Slot"); Assert.That(slot, Is.InRange(0, (int)Get(Get(match, "setup"), "count") - 1));
        Assert.That(Get(receipt, "MatchId"), Is.EqualTo(Get(match, "MatchId")));
        Assert.That(Get(receipt, "ParticipantId"), Is.EqualTo(Get(players.GetValue(slot), "participantId")));
        Assert.That(Get(receipt, "DisplayName"), Is.EqualTo("Same Award Name"));
        Assert.That(Get(receipt, "Color"), Is.EqualTo(Get(players.GetValue(slot), "color")));
        Assert.That(Get(receipt, "IsTeam"), Is.False);
    }
    static void Capture(Component match, string name, Action<Camera, int, int> inspect = null)
    {
        var save = Runtime("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5);
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
        {
            Action<Camera> check = camera => inspect?.Invoke(camera, size.x, size.y);
            save.Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/BaseParkingAwards/" + name + "-" + size.y + ".png", check });
        }
    }
    public sealed class Receipts { public readonly List<object> items = new List<object>(); public void Record(object receipt) => items.Add(receipt); }

    [UnityTest, Timeout(420000)]
    public IEnumerator BaseActualLandingsBankExactPointsIntoOnlyTheirOwnFourViewPresenters()
    {
        var prefs = Preferences("BaseOriginalPlacementAwards"); var isolation = IsolateInputs(); Component match = null;
        EventInfo signal = null; Delegate subscription = null; var receipts = new Receipts();
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = Match(); var players = Prepare(match, true); Set(match, "option", 0);
            signal = match.GetType().GetEvent("ScoreConfirmed"); subscription = Delegate.CreateDelegate(signal.EventHandlerType, receipts, typeof(Receipts).GetMethod("Record")); signal.AddEventHandler(match, subscription);
            Assert.That(Call(match, "StartMatch"), Is.True); Time.timeScale = 4; float deadline = Time.realtimeSinceStartup + 300;
            while ((int)Get(match, "RoundLocks") == 0 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(Get(match, "RoundLocks"), Is.EqualTo(1), "The original physical four-truck round must bank before any points are inspected.");
            ((Behaviour)match).enabled = false; Time.timeScale = 0;
            Assert.That(Get(match, "LandingLocks"), Is.EqualTo(4)); Assert.That(Get(match, "Phase").ToString(), Is.EqualTo("RoundResult"));
            var totals = (int[])((int[])Get(match, "totals")).Clone(); var sums = new double[4]; var jumpers = (Array)Get(match, "jumpers"); var ids = new HashSet<string>();
            Assert.That(receipts.items.Count, Is.GreaterThan(0), "At least one genuine safe landing is required; no fixture awards substitute for it.");
            foreach (var receipt in receipts.items)
            {
                Identity(receipt, match, players); int slot = (int)Get(receipt, "Slot"); var landing = Get(jumpers.GetValue(slot), "Landing");
                Assert.That(ids.Add((string)Get(receipt, "EventId")), Is.True); Assert.That(Get(landing, "valid"), Is.True);
                Assert.That(Get(receipt, "Kind").ToString(), Is.EqualTo("Points")); Assert.That(Get(receipt, "Delta"), Is.EqualTo((double)(int)Get(landing, "points")));
                Assert.That(Get(receipt, "Total"), Is.EqualTo((double)totals[slot])); Assert.That(Get(receipt, "Unit"), Is.EqualTo("PTS"));
                Assert.That(Get(receipt, "RoundEnded"), Is.True); Assert.That(Get(receipt, "MatchEnded"), Is.False);
                Assert.That(Get(receipt, "Tier").ToString(), Is.EqualTo((int)Get(landing, "placement") == 1 ? "RoundWinner" : "Major")); sums[slot] += (double)Get(receipt, "Delta");
            }
            for (int i = 0; i < 4; i++) Assert.That(sums[i], Is.EqualTo(totals[i]), "Every original positive first-round point is represented once; invalid landings remain zero.");
            int count = receipts.items.Count; Call(match, "FinishRound"); Assert.That(receipts.items.Count, Is.EqualTo(count)); Assert.That((int[])Get(match, "totals"), Is.EqualTo(totals));
            var panels = Panels(match); Assert.That(panels.Length, Is.EqualTo(4)); Assert.That(((Camera[])Get(match, "cameras")).Count(c => c.enabled), Is.EqualTo(4));
            Time.timeScale = 1; deadline = Time.realtimeSinceStartup + 20;
            while (panels.Any(p => sums[(int)Get(p, "viewport")] > 0 && Get(p, "LastPresented") == null) && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = 0; Canvas.ForceUpdateCanvases(); var viewPanels = (Array)Get(Get(match, "menus"), "viewPanels");
            foreach (var panel in panels)
            {
                int view = (int)Get(panel, "viewport"); Assert.That(Get(panel, "ViewportOwner"), Is.EqualTo(view)); var shown = Get(panel, "LastPresented");
                if (sums[view] == 0) Assert.That(shown, Is.Null);
                else { Assert.That(shown, Is.Not.Null); Assert.That(Get(shown, "Slot"), Is.EqualTo(view), "A viewport may never animate another participant's award."); }
                var oldCard = ((Component)viewPanels.GetValue(view)).transform.Find("Compact owned viewport score") as RectTransform;
                Assert.That(oldCard, Is.Not.Null); Assert.That(oldCard.gameObject.activeInHierarchy, Is.True);
                Assert.That(Bounds((RectTransform)panel.transform).Overlaps(Bounds(oldCard)), Is.False, "New notification leaves the approved owned scorecard in place.");
            }
            Capture(match, "base-actual-four-AI-round");
            Call(match, "Pause", 0, false); yield return new WaitForSecondsRealtime(.12f); Assert.That(receipts.items.Count, Is.EqualTo(count)); Call(match, "Resume");
            string previous = (string)Get(match, "MatchId"); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; yield return null;
            Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(previous)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero); foreach (var panel in panels) Assert.That(Get(panel, "LastPresented"), Is.Null);
            // Separate controlled bounded-failure lifecycle: invoke the original
            // failure path, never inject a successful landing, rank or point.
            receipts.items.Clear(); Call(match, "Simulate", 4.1f); foreach (var jumper in jumpers) Call(jumper, "Fail", "CONTROLLED BOUNDED FAILURE"); Call(match, "Simulate", 0f);
            Assert.That(Get(match, "RoundLocks"), Is.EqualTo(1)); Assert.That((int[])Get(match, "totals"), Is.EqualTo(new int[4])); Assert.That(receipts.items, Is.Empty); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero);
            TestContext.WriteLine("BASE_ACTUAL_AWARDS receipts=" + count + " totals=" + string.Join(",", totals) + "; original physical AI landing/bank, duplicate guard, owned four-AI viewport notifications, preserved cards, pause/rematch. Controlled all-failure path separately proves zero awards. No human/device acceptance.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { if (subscription != null) signal.RemoveEventHandler(match, subscription); ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); }
            UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }

    [UnityTest, Timeout(600000)]
    public IEnumerator ParkingActualCapturesAfterEmptyReplayPublishOnlyRealSurvivalAndChampionOutcomes()
    {
        var prefs = Preferences("ParkingOriginalCaptureAwards"); var isolation = IsolateInputs(); Component match = null;
        EventInfo signal = null; Delegate subscription = null; var receipts = new Receipts();
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = Match(); var players = Prepare(match, false);
            signal = match.GetType().GetEvent("ScoreConfirmed"); subscription = Delegate.CreateDelegate(signal.EventHandlerType, receipts, typeof(Receipts).GetMethod("Record")); signal.AddEventHandler(match, subscription);
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0;
            // Explicit empty-round state-transition fixture. No physical capture
            // or successful outcome is injected; the original rules resolve it.
            Call(match, "Simulate", 3f); Assert.That(Get(match, "Phase").ToString(), Is.EqualTo("Music")); Call(match, "Horn"); Call(match, "Resolve");
            Assert.That(Get(match, "ReplayRound"), Is.True); Assert.That(Get(match, "Remaining"), Is.EqualTo(8)); Assert.That(Get(match, "Winner"), Is.EqualTo(-1)); Assert.That(Get(match, "EliminatedMask"), Is.Zero);
            Call(match, "Resolve"); Assert.That(receipts.items, Is.Empty); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero);
            Call(match, "Simulate", 2.6f); Assert.That(Get(match, "Round"), Is.EqualTo(1), "An empty replay retains the same original round identity."); Assert.That(Get(match, "ReplayRound"), Is.False);
            ((Behaviour)match).enabled = true; Time.timeScale = 4; float deadline = Time.realtimeSinceStartup + 480;
            while (receipts.items.Count == 0 && !(bool)Get(match, "Finished") && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(receipts.items.Count, Is.GreaterThan(0), "Original upright, supported, whole-body AI captures must produce the first outcome.");
            int beforePause = receipts.items.Count; Call(match, "Pause", 0, false); float age = (float)Get(match, "Age"); yield return new WaitForSecondsRealtime(.12f);
            Assert.That(Get(match, "Age"), Is.EqualTo(age)); Assert.That(receipts.items.Count, Is.EqualTo(beforePause)); Call(match, "Resume"); Time.timeScale = 4;
            while (!(bool)Get(match, "Finished") && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(Get(match, "Finished"), Is.True, "A real original eight-AI match must reach its sole survivor without injected capture/winner state."); Time.timeScale = 0;
            int winner = (int)Get(match, "Winner"); Assert.That(winner, Is.InRange(0, 7)); Assert.That(Get(match, "Remaining"), Is.EqualTo(1));
            var ids = new HashSet<string>(); var captures = new HashSet<string>(); var survived = new Dictionary<string, List<object>>(); int champions = 0;
            foreach (var receipt in receipts.items)
            {
                Identity(receipt, match, players); string id = (string)Get(receipt, "EventId"); Assert.That(ids.Add(id), Is.True);
                Assert.That(Get(receipt, "Kind").ToString(), Is.EqualTo("Outcome")); Assert.That(Get(receipt, "Delta"), Is.EqualTo(0d)); Assert.That(Get(receipt, "Total"), Is.EqualTo(0d));
                string[] key = id.Split(':'); Assert.That(key.Length, Is.EqualTo(4)); int slot = (int)Get(receipt, "Slot"); Assert.That(key[3], Is.EqualTo(slot.ToString()));
                if (key[2] == "capture")
                {
                    captures.Add(id); Assert.That(Get(receipt, "Unit"), Is.EqualTo("SEC")); Assert.That((double)Get(receipt, "OutcomeValue"), Is.InRange(1d, 29d));
                    Assert.That(Get(receipt, "Reason"), Is.EqualTo("BAY SECURED")); Assert.That(Get(receipt, "Tier").ToString(), Is.EqualTo("Major")); Assert.That(Get(receipt, "RoundEnded"), Is.False);
                }
                else
                {
                    Assert.That(key[2], Is.EqualTo("survival")); Assert.That(captures.Contains("round:" + key[1] + ":capture:" + slot), Is.True, "Only a real committed bay owner may survive."); Assert.That(Get(receipt, "RoundEnded"), Is.True);
                    if ((bool)Get(receipt, "MatchEnded"))
                    {
                        champions++; Assert.That(slot, Is.EqualTo(winner)); Assert.That(Get(receipt, "Tier").ToString(), Is.EqualTo("MatchChampion")); Assert.That(Get(receipt, "Unit"), Is.EqualTo("ROUNDS")); Assert.That(Get(receipt, "OutcomeValue"), Is.EqualTo((double)(int)Get(match, "Round")));
                    }
                    else
                    {
                        Assert.That(Get(receipt, "Tier").ToString(), Is.EqualTo("RoundWinner")); Assert.That(Get(receipt, "Unit"), Is.EqualTo("TRUCKS LEFT"));
                        if (!survived.TryGetValue(key[1], out var round)) survived[key[1]] = round = new List<object>(); round.Add(receipt);
                    }
                }
            }
            foreach (var round in survived.Values) foreach (var receipt in round) Assert.That(Get(receipt, "OutcomeValue"), Is.EqualTo((double)round.Count), "Survival announces the real committed survivor count, never points.");
            Assert.That(captures.Count, Is.EqualTo(Get(match, "TotalCaptures"))); Assert.That(champions, Is.EqualTo(1)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(receipts.items.Count));
            int count = receipts.items.Count; Call(match, "Resolve"); var secured = (int[])Get(match, "secured"); Assert.That(Call(match, "Secure", winner, secured[winner]), Is.False); Assert.That(receipts.items.Count, Is.EqualTo(count));
            var panels = Panels(match); Assert.That(panels.Length, Is.EqualTo(1)); Assert.That(Get(panels[0], "viewport"), Is.EqualTo(-1)); Time.timeScale = 1; deadline = Time.realtimeSinceStartup + 60;
            while ((Get(panels[0], "LastPresented") == null || !(bool)Get(Get(panels[0], "LastPresented"), "MatchEnded")) && Time.realtimeSinceStartup < deadline) yield return null;
            var shown = Get(panels[0], "LastPresented"); Assert.That(shown, Is.Not.Null); Assert.That(Get(shown, "MatchEnded"), Is.True); Assert.That(Get(shown, "Slot"), Is.EqualTo(winner));
            // LastPresented is assigned when Heat starts In, before its panel
            // opens. Let the actual authored entrance finish before freezing.
            var awardAnimator = (Animator)Get(panels[0], "notificationAnimator");
            yield return new WaitForSecondsRealtime(awardAnimator.runtimeAnimatorController.animationClips.Single(clip => clip.name == "Notification_In").length + .08f); yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(Get(match, "Finished"), Is.True); Assert.That(Get(panels[0], "LastPresented"), Is.SameAs(shown));
            var notice = (Component)Get(panels[0], "notification"); var message = (Component)Get(notice, "textObj"); Call(message, "ForceMeshUpdate", false, false); Canvas.ForceUpdateCanvases();
            Assert.That(notice.gameObject.activeInHierarchy && message.gameObject.activeInHierarchy, Is.True); Assert.That(message.GetComponent<CanvasRenderer>().GetInheritedAlpha(), Is.GreaterThan(.95f)); Assert.That(((Color)Get(message, "color")).a, Is.GreaterThan(.95f));
            StringAssert.Contains((string)Get(shown, "Reason"), (string)Get(message, "text")); StringAssert.Contains((string)Get(shown, "DisplayName"), (string)Get(message, "text")); Assert.That(Get(message, "isTextOverflowing"), Is.False);
            Time.timeScale = 0; Capture(match, "parking-actual-champion", (ui, width, height) =>
            {
                // The Editor overlay has its own Canvas scale. Measure pixels
                // only after the controlled capture installs the requested size.
                Canvas.ForceUpdateCanvases(); Call(message, "ForceMeshUpdate", false, false);
                var messageRect = Pixels((RectTransform)Get(message, "rectTransform"), ui); var noticeRect = Pixels((RectTransform)notice.transform, ui);
                float scale = Mathf.Min(width / 1920f, height / 1080f); var authored = (Vector2)Get(panels[0], "resultSize");
                Assert.That(noticeRect.width, Is.EqualTo(authored.x * scale).Within(2)); Assert.That(noticeRect.height, Is.EqualTo(authored.y * scale).Within(2));
                Assert.That(messageRect.width, Is.GreaterThan(100)); Assert.That(messageRect.height, Is.GreaterThan(10));
                Assert.That(noticeRect.width, Is.GreaterThan(600)); Assert.That(noticeRect.height, Is.GreaterThan(50));
                Assert.That(noticeRect.xMin, Is.GreaterThanOrEqualTo(-1)); Assert.That(noticeRect.yMin, Is.GreaterThanOrEqualTo(-1));
                Assert.That(noticeRect.xMax, Is.LessThanOrEqualTo(width + 1)); Assert.That(noticeRect.yMax, Is.LessThanOrEqualTo(height + 1));
                Assert.That(messageRect.xMin, Is.GreaterThanOrEqualTo(noticeRect.xMin)); Assert.That(messageRect.xMax, Is.LessThanOrEqualTo(noticeRect.xMax));
                Assert.That(messageRect.yMin, Is.GreaterThanOrEqualTo(noticeRect.yMin)); Assert.That(messageRect.yMax, Is.LessThanOrEqualTo(noticeRect.yMax));
                Assert.That(Get(message, "isTextOverflowing"), Is.False); StringAssert.Contains((string)Get(shown, "DisplayName"), (string)Get(message, "text"));
                TestContext.WriteLine("PARKING_ACTUAL_CHAMPION_PIXELS " + width + "x" + height + " notice=" + noticeRect + " text=" + messageRect + " referenceScale=" + scale);
            });
            string previous = (string)Get(match, "MatchId"); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; yield return null;
            Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(previous)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero); Assert.That(Get(match, "TotalCaptures"), Is.Zero); Assert.That(Get(panels[0], "LastPresented"), Is.Null);
            TestContext.WriteLine("PARKING_ACTUAL_OUTCOMES receipts=" + count + " captures=" + captures.Count + " champion=" + winner + "; controlled empty replay followed by original full physical AI match, pause, unique capture/survival, no fabricated points, saved Heat champion and rematch. No injected successful captures/poses/forces/winner; no human/device acceptance.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { if (subscription != null) signal.RemoveEventHandler(match, subscription); ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); }
            UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }
}
#endif
