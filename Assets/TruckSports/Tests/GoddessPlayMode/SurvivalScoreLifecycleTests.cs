#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SurvivalScoreLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);

    [UnityTest, Timeout(120000)]
    public IEnumerator RealGroundedHillOccupationPublishesMilestonesWithoutChangingFractionalControl()
    {
        var previous = InputSystem.settings; var settings = UnityEngine.Object.Instantiate(previous);
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = settings;
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "HillScoreNative" });
        var isolation = new GameObject("Explicit grounded hill score fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        var keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent(); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup"); Set(setup, "count", 2); Call(match, "AllBots"); Set(match, "option", 0);
            var players = (Array)Get(setup, "players"); var defaults = (Array)Type.GetType("LWS.TruckBowling.BowlingSetup, Assembly-CSharp", true).GetMethod("Defaults").Invoke(null, null);
            for (int i = 0; i < 2; i++)
            {
                var player = players.GetValue(i); Set(player, "source", Enum.Parse(player.GetType().GetField("source").FieldType, "Keyboard"));
                foreach (string key in new[] { "forward", "back", "left", "right" }) Set(player, key, Get(defaults.GetValue(i), key));
            }
            var weather = Get(match, "atmosphere"); var catalog = Get(weather, "presets"); var entries = ((Array)Get(catalog, "entries")).Cast<object>().ToArray();
            Call(catalog, "Select", Get(entries.Single(entry => (string)Get(entry, "displayName") == "Clear"), "id"));
            Assert.That(Call(match, "StartMatch"), Is.True);
            var trucks = ((Array)Get(match, "trucks")).Cast<Component>().ToArray(); var sites = (Vector3[])Get(match, "sites");
            // Explicit placement fixture, with real NWH wheels and terrain support.
            // No score, eligibility, grounding or rule-clock value is assigned.
            Call(trucks[0], "Prepare", sites[0] + Vector3.up * .65f, Quaternion.identity, true);
            Call(trucks[1], "Prepare", sites[0] + new Vector3(25, .65f, 0), Quaternion.identity, true); Physics.SyncTransforms();
            Time.timeScale = 4; var rules = Get(match, "rules"); var control = (float[])Get(rules, "control"); var journal = Get(match, "ScoreJournal");
            float deadline = Time.realtimeSinceStartup + 25;
            while (control[0] < 9 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That((bool)Get(Get(trucks[0], "tractorMotor"), "Grounded"), Is.True, "Actual NWH wheel support is required.");
            Assert.That(control[0], Is.GreaterThanOrEqualTo(9), "A stationary supported truck in the uncontested physical ring earns actual control; body=" + Get(trucks[0], "body"));
            Assert.That(Get(journal, "Count"), Is.EqualTo(0), "Fractional control must not flood the journal/UI on each physics step.");
            deadline = Time.realtimeSinceStartup + 5; while (control[0] < 10.1f && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(Get(journal, "Count"), Is.EqualTo(1)); var receipt = Get(journal, "Latest");
            Assert.That(Get(receipt, "Delta"), Is.EqualTo(10d)); Assert.That(Get(receipt, "Unit"), Is.EqualTo("SEC")); Assert.That(Get(receipt, "EventId"), Is.EqualTo("control:0:1"));
            Assert.That((double)Get(receipt, "Total"), Is.InRange(10d, (double)control[0]), "The event carries the actual fractional total; it does not round or rewrite the rules.");
            Call(match, "Pause", 0, false); float held = control[0]; yield return new WaitForSecondsRealtime(.15f);
            Assert.That(control[0], Is.EqualTo(held)); Assert.That(Get(journal, "Count"), Is.EqualTo(1)); Call(match, "Resume"); Time.timeScale = 4;
            deadline = Time.realtimeSinceStartup + 8; while (control[0] < 20.1f && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(Get(journal, "Count"), Is.EqualTo(2)); Assert.That(Get(Get(journal, "Latest"), "EventId"), Is.EqualTo("control:0:2"));
            string oldMatch = (string)Get(match, "MatchId"); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return null;
            Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(oldMatch)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero);
            Assert.That(((float[])Get(match, "notifiedControl")).All(value => value == 0), Is.True); Assert.That(control.All(value => value == 0), Is.True);
            TestContext.WriteLine("GROUNDED_HILL_MILESTONES actual NWH/terrain supported occupation; no assigned score or clock; two ten-second notifications, pause and fresh rematch.");
        }
        finally
        {
            if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1;
            if (keyboard.added) InputSystem.RemoveDevice(keyboard); UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); InputSystem.settings = previous; UnityEngine.Object.DestroyImmediate(settings);
        }
    }
}
#endif
