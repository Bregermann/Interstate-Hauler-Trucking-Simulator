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

public sealed class FinishOutcomeLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(210000)]
    public IEnumerator ActualLegalSignalRaceFinishPublishesItsRecordedMeasurement() { yield return ActualFinish("TruckRedLightGreenLight", "finishTimes"); }
    [UnityTest, Timeout(210000)]
    public IEnumerator ActualMountainEscapePublishesItsRecordedMeasurement() { yield return ActualFinish("TruckAvalancheEscape", "finishTime"); }
    static IEnumerator ActualFinish(string scene, string timesField)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "ActualFinishOutcome" });
        var isolation = new GameObject("Actual NWH finish measurement fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup"); Set(setup, "count", scene == "TruckAvalancheEscape" ? 12 : 1); Call(match, "AllBots");
            foreach (var player in ((Array)Get(setup, "players")).Cast<object>().Take((int)Get(setup, "count"))) { var difficulty = player.GetType().GetField("difficulty"); difficulty.SetValue(player, Enum.Parse(difficulty.FieldType, "Hard")); }
            if (scene == "TruckAvalancheEscape") Set(match, "option", 0);
            var catalog = Get(Get(match, "atmosphere"), "presets"); var entries = ((Array)Get(catalog, "entries")).Cast<object>();
            Call(catalog, "Select", Get(entries.Single(entry => (string)Get(entry, "displayName") == "Clear"), "id")); Assert.That(Call(match, "StartMatch"), Is.True); Time.timeScale = 3;
            var journal = Get(match, "ScoreJournal"); float until = Time.realtimeSinceStartup + 150;
            while ((int)Get(journal, "Count") == 0 && !(bool)Get(match, "Finished") && Time.realtimeSinceStartup < until) yield return new WaitForFixedUpdate();
            var activeRules = Get(match, "rules");
            int finishers = scene == "TruckAvalancheEscape"
                ? ((bool[])Get(activeRules, "finished")).Take((int)Get(setup, "count")).Count(value => value)
                : ((Array)Get(activeRules, "outcomes")).Cast<object>().Take((int)Get(setup, "count")).Count(value => value.ToString() == "Finished");
            Assert.That(finishers, Is.GreaterThan(0), "At least one actual AI/NWH route must reach its accepted whole-truck finish; no finish/clock/position is injected. Individual avalanche competitors can legitimately be buried.");
            Assert.That(Get(journal, "Count"), Is.EqualTo(finishers), "Exactly one measurement per genuine finish; eliminated drivers receive none.");
            var receipt = Get(journal, "Latest"); int slot = (int)Get(receipt, "Slot"); float time = ((float[])Get(Get(match, "rules"), timesField))[slot];
            Assert.That(Get(receipt, "Kind").ToString(), Is.EqualTo("Outcome")); Assert.That(Get(receipt, "OutcomeValue"), Is.EqualTo((double)time));
            Assert.That(Get(receipt, "Delta"), Is.EqualTo(0d)); Assert.That(Get(receipt, "Total"), Is.EqualTo(0d)); Assert.That(Get(receipt, "Unit"), Is.EqualTo("SEC"));
            ((Behaviour)match).enabled = false; Time.timeScale = 1; var presenter = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation")); Assert.That(presenter, Is.Not.Null);
            until = Time.realtimeSinceStartup + 10; while (Get(presenter, "LastPresented") == null && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(Get(presenter, "LastPresented"), Is.Not.Null); string message = (string)Get(Get(presenter, "notification"), "notificationText");
            Assert.That(message, Does.Contain("SEC").And.Not.Contain("+0").And.Not.Contain("PTS").And.Not.Contain("TOTAL"));
            Assert.That(((float[])Get(Get(match, "rules"), timesField))[slot], Is.EqualTo(time));
            ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return null;
            Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero); Assert.That(Get(presenter, "Presented"), Is.Zero);
            TestContext.WriteLine("ACTUAL_FINISH_OUTCOME " + scene + " actual AI/NWH route at3x game time; recorded seconds=" + time + "; no score, crossing, clock or position injection; rematch cleared.");
        }
        finally { if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } Time.timeScale = 1; UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
    [UnityTest, Timeout(60000)]
    public IEnumerator SyntheticMixedReceiptsKeepMeasurementsDistinctFromPointChanges()
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticOutcomeFormat" });
        var isolation = new GameObject("Explicit presentation-only mixed receipt fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); Call(match, "AllBots"); Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false;
            var before = ((int[])Get(Get(match, "rules"), "scores")).ToArray(); object tier = Enum.Parse(Runtime("SportsScoreTier"), "Major");
            Assert.That(Call(match, "ConfirmScore", "synthetic:one", 0, 1d, 1d, "MILESTONE", tier, "SEC", null, false, false), Is.True);
            Assert.That(Call(match, "ConfirmOutcome", "synthetic:finish", 0, 42.125d, "MILESTONE", "SEC", tier, false, false), Is.True);
            Assert.That(Call(match, "ConfirmScore", "synthetic:two", 0, 2d, 3d, "MILESTONE", tier, "SEC", null, false, false), Is.True);
            var presenter = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation")); var observed = new List<string>(); object prior = null; float until = Time.realtimeSinceStartup + 20;
            while (observed.Count < 3 && Time.realtimeSinceStartup < until)
            {
                var current = Get(presenter, "LastPresented"); if (current != null && current != prior) { prior = current; observed.Add((string)Get(Get(presenter, "notification"), "notificationText")); } yield return null;
            }
            Assert.That(observed.Count, Is.EqualTo(3)); Assert.That(observed[0], Does.Contain("+1").And.Contain("TOTAL"));
            Assert.That(observed[1], Does.Contain("42.125").And.Contain("SEC").And.Not.Contain("+0").And.Not.Contain("TOTAL").And.Not.Contain("PTS"));
            Assert.That(observed[2], Does.Contain("+2").And.Contain("3 TOTAL")); CollectionAssert.AreEqual(before, (int[])Get(Get(match, "rules"), "scores"), "This explicitly synthetic presentation fixture never awards game points.");
            TestContext.WriteLine("SYNTHETIC_OUTCOME_FORMAT same recipient/reason/tier/unit; point1, measured42.125, point2 remain distinct and ordered; game points unchanged.");
        }
        finally { if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } Time.timeScale = 1; UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
