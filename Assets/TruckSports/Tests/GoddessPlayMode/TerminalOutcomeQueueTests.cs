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

// Explicit synthetic presentation receipts over the original eight-AI setup.
// No physical captures, scores or winners are manufactured as gameplay evidence.
public sealed class TerminalOutcomeQueueTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);
    Component match, presenter;
    IDisposable scope;
    GameObject isolation;
    object[] Queue => ((IEnumerable)Get(presenter, "pending")).Cast<object>().Select(item => Get(item, "latest")).ToArray();
    object Latest => Get(Get(match, "ScoreJournal"), "Latest");
    object Tier(string tier) => Enum.Parse(Runtime("SportsScoreTier"), tier);
    object Outcome(string id, int slot, string tier = "RoundWinner", bool terminal = false)
    {
        Assert.That(Call(match, "ConfirmOutcome", id, slot, terminal ? 7d : 3d, terminal ? "PARKING CHAMPION" : "ROUND SURVIVED", terminal ? "ROUNDS" : "TRUCKS LEFT", Tier(tier), true, terminal), Is.True);
        return Latest;
    }
    object Points(string id, int slot, double delta, double total, string reason)
    {
        Assert.That(Call(match, "ConfirmScore", id, slot, delta, total, reason, Tier("Small"), "PTS", null, false, false), Is.True);
        return Latest;
    }
    IEnumerator Begin(string label)
    {
        scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { label });
        Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        isolation = new GameObject("SYNTHETIC terminal outcome queue fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); Call(match, "AllBots");
        Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false;
        presenter = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation")); Assert.That(presenter, Is.Not.Null);
        Assert.That(Get(Get(match, "setup"), "count"), Is.EqualTo(8));
    }
    void Finish()
    {
        Time.timeScale = 1;
        if (match != null) Call(match, "SetupScreen");
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation);
        Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null); scope?.Dispose();
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator SyntheticPausedEightAiOutcomeBurstShowsChampionAfterOneExistingHoldAndKeepsPoints()
    {
        try
        {
            yield return Begin("TerminalOutcomeBurst");
            var current = Outcome("synthetic:currently-readable", 0, "Major");
            float until = Time.realtimeSinceStartup + 15;
            while (Get(presenter, "LastPresented") != current && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(Get(presenter, "LastPresented"), Is.SameAs(current));
            float release = (float)Get(presenter, "next"); int shownBeforePause = (int)Get(presenter, "Presented");
            int captures = (int)Get(match, "TotalCaptures"), winner = (int)Get(match, "Winner"), roundBefore = (int)Get(match, "Round");
            Call(match, "Pause", 0, false); Assert.That(Get(match, "Paused"), Is.True); Assert.That(Time.timeScale, Is.Zero);
            for (int round = 1; round <= 7; round++) for (int slot = 0; slot < 8; slot++) Outcome("synthetic:survived:" + round + ":" + slot, slot);
            var positive = Points("synthetic:earned-positive", 3, 5, 5, "EARNED POINTS");
            var negative = Points("synthetic:earned-negative", 3, -2, 3, "REAL PENALTY");
            var champion = Outcome("synthetic:champion", 7, "MatchChampion", true);
            Outcome("synthetic:late-old-survival", 6);
            Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(61), "All synthetic receipts remain recorded even when old measurements leave the presentation queue.");
            CollectionAssert.AreEqual(new[] { champion, positive, negative }, Queue, "Only nonterminal measurements are superseded; earned points are intact.");
            Assert.That(Get(presenter, "next"), Is.EqualTo(release), "Terminal priority does not cut short the currently readable award or restart its hold.");
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(Get(presenter, "Presented"), Is.EqualTo(shownBeforePause)); Assert.That(Get(presenter, "LastPresented"), Is.SameAs(current));
            Call(match, "Resume"); until = Time.realtimeSinceStartup + 30;
            while (Get(presenter, "LastPresented") != champion && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(Get(presenter, "LastPresented"), Is.SameAs(champion), "The next presentation after the existing readable hold must be the actual terminal receipt.");
            Assert.That(Get(presenter, "Presented"), Is.EqualTo(shownBeforePause + 1));
            Assert.That(Time.time, Is.GreaterThanOrEqualTo(release - .05f));
            Assert.That(Time.time, Is.LessThanOrEqualTo(release + Time.maximumDeltaTime * 2 + .05f), "One existing hold plus the LateUpdate/test-coroutine frame boundary, rather than56 old measurements, controls champion latency.");
            CollectionAssert.AreEqual(new[] { positive, negative }, Queue); Assert.That(Queue.Any(item => Get(item, "Kind").ToString() == "Outcome"), Is.False);
            Assert.That(Get(match, "TotalCaptures"), Is.EqualTo(captures)); Assert.That(Get(match, "Winner"), Is.EqualTo(winner)); Assert.That(Get(match, "Round"), Is.EqualTo(roundBefore));
            Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(61));
            Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True);
            var fresh = Outcome("synthetic:next-match-survival", 7);
            CollectionAssert.AreEqual(new[] { fresh }, Queue, "A fresh match resets terminal measurement suppression.");
            TestContext.WriteLine("SYNTHETIC_TERMINAL_OUTCOME_QUEUE 56 stale measurements superseded, complete61-receipt journal retained, champion follows one unchanged hold after pause, positive/negative points retained, new match resets. No actual gameplay score/capture/winner or human-device evidence.");
        }
        finally { Finish(); }
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator SyntheticTerminalOutcomeCannotJumpItsEarlierSameRecipientPositiveOrNegativePoints()
    {
        try
        {
            yield return Begin("TerminalOutcomePointGuard"); Call(match, "Pause", 0, false);
            var positive = Points("synthetic:positive", 7, 5, 5, "EARNED POINTS");
            var negative = Points("synthetic:negative", 7, -2, 3, "REAL PENALTY");
            Outcome("synthetic:survival-other", 0); Outcome("synthetic:survival-winner", 7);
            var champion = Outcome("synthetic:champion", 7, "MatchChampion", true);
            CollectionAssert.AreEqual(new[] { positive, negative, champion }, Queue, "The existing point commit-order barrier remains stronger than terminal outcome priority.");
            Outcome("synthetic:stale-after-champion", 7);
            var later = Points("synthetic:later-positive", 7, 1, 4, "EARNED POINTS");
            CollectionAssert.AreEqual(new[] { positive, negative, champion, later }, Queue, "A later point cannot merge back across this recipient's terminal measurement.");
            CollectionAssert.AreEqual(new[] { 5d, -2d, 1d }, Queue.Where(item => Get(item, "Kind").ToString() == "Points").Select(item => (double)Get(item, "Delta")).ToArray());
            Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(7));
            Assert.That(Get(match, "TotalCaptures"), Is.Zero); Assert.That(Get(match, "Winner"), Is.EqualTo(-1));
            TestContext.WriteLine("SYNTHETIC_TERMINAL_POINT_GUARD earned+5,-2,terminal,+1 commit order preserved while stale outcome measurements are superseded; original gameplay untouched.");
        }
        finally { Finish(); }
    }
}
#endif
