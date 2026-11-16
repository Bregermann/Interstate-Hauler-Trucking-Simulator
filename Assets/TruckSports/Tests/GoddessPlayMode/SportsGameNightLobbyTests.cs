#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Software-controller integration through the real saved front end and sport setup.
// This does not certify physical hardware or complete-match gameplay.
public sealed class SportsGameNightLobbyTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static Type T(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v)).Invoke(owner, args);
    static object Static(string type, string method, params object[] args) => T(type).GetMethods(Any).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(null, args);
    static IEnumerator Pad(Gamepad pad, GamepadState state)
    { InputSystem.QueueStateEvent(pad, state); yield return null; yield return null; }
    static IEnumerator Press(Gamepad pad, GamepadButton button)
    { yield return Pad(pad, new GamepadState().WithButton(button)); yield return Pad(pad, new GamepadState()); }
    static IEnumerator Move(Gamepad pad, Vector2 direction)
    { yield return Pad(pad, new GamepadState { leftStick = direction }); yield return Pad(pad, new GamepadState()); }
    IDisposable preferences;
    Gamepad one, two;
    Component night;
    GameObject telemetry;
    Gamepad[] four;
    [UnityTest, Timeout(120000)]
    public IEnumerator TwoControllersCreateProfileReadyAndKeepOwnershipAcrossSportReturn()
    {
        preferences = (IDisposable)Static("SportsPrefs", "IsolateValidation", "FoundationLobby" + Guid.NewGuid().ToString("N"));
        Static("SportsParticipantRegistry", "ForgetCache");
        one = InputSystem.AddDevice<Gamepad>("Foundation host"); two = InputSystem.AddDevice<Gamepad>("Foundation second player");
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        night = UnityEngine.Object.FindAnyObjectByType(T("SportsGameNightRuntime")) as Component;
        if (night == null) night = new GameObject("Foundation game-night integration").AddComponent(T("SportsGameNightRuntime"));
        yield return null; yield return null;
        var front = UnityEngine.Object.FindAnyObjectByType(T("SportsFrontEnd"));
        Call(night, "Open", front, one); yield return null; yield return null;
        yield return Press(one, GamepadButton.South); // Select existing profile.
        yield return Press(two, GamepadButton.South); // Join, not select on the same press.
        var state = Get(night, "State"); var seats = (IList)Get(state, "Seats");
        Assert.That(Get(seats[0], "ProfileConfirmed"), Is.EqualTo(true));
        Assert.That(Get(seats[1], "ProfileConfirmed"), Is.EqualTo(false));
        yield return Press(two, GamepadButton.West); // Actual lobby new-profile action.
        Assert.That(T("SportsControllerKeyboard").GetProperty("OwnerPad").GetValue(null), Is.SameAs(two));
        yield return Move(two, Vector2.down); yield return Move(two, Vector2.down); // a
        yield return Press(two, GamepadButton.South);
        yield return Move(two, Vector2.down); yield return Move(two, Vector2.right); yield return Move(two, Vector2.right); // c
        yield return Press(two, GamepadButton.South);
        yield return Move(two, Vector2.up); yield return Move(two, Vector2.up); // e
        yield return Press(two, GamepadButton.South);
        yield return Press(two, GamepadButton.Start); yield return null; yield return null;
        string secondId = (string)Get(seats[1], "ProfileId");
        Assert.That((string)Get(Call(night, "Profile", 1), "displayName"), Is.EqualTo("ace"));
        Static("SportsParticipantRegistry", "ForgetCache");
        Assert.That(Static("SportsParticipantRegistry", "Find", secondId), Is.Not.Null, "Confirmed controller-created profile survives a preferences reload.");
        yield return Press(one, GamepadButton.South); yield return Press(two, GamepadButton.South);
        Assert.That(Get(state, "AllReady"), Is.EqualTo(true));
        yield return Press(two, GamepadButton.Start);
        Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Lobby"), "Player 2 cannot select the next sport.");
        yield return Press(one, GamepadButton.Start);
        Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Selection"));
        Static("SportsNavigation", "LoadSport", "TruckMusicalParking");
        yield return null; yield return null; yield return null; yield return null;
        var session = UnityEngine.Object.FindAnyObjectByType(T("SportsEventSession"));
        Assert.That(session, Is.Not.Null); Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Configuration"));
        var players = (Array)Get(Get(session, "setup"), "players");
        Assert.That(Get(players.GetValue(0), "pad"), Is.SameAs(one));
        Assert.That(Get(players.GetValue(1), "pad"), Is.SameAs(two));
        Assert.That(Get(players.GetValue(1), "participantId"), Is.EqualTo(secondId));
        Assert.That(Get(players.GetValue(2), "source").ToString(), Is.EqualTo("AI"));
        Static("SportsGameNightRuntime", "ConfigurationEdited", session);
        Static("SportsNavigation", "FrontEnd", true);
        yield return null; yield return null; yield return null; yield return null;
        Assert.That(Call(night, "Pad", 0), Is.SameAs(one)); Assert.That(Call(night, "Pad", 1), Is.SameAs(two));
        Assert.That(Get(seats[1], "ProfileId"), Is.EqualTo(secondId));
        Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Lobby"));
        Assert.That(Get(state, "AllReady"), Is.EqualTo(false), "Returning requires an explicit fresh ready check.");
    }
    [UnityTest, Timeout(240000)]
    public IEnumerator MixedHumanAiMatchCompletesRematchesAndReturnsWithoutAiProfileAwards()
    {
        preferences = (IDisposable)Static("SportsPrefs", "IsolateValidation", "FoundationMatch" + Guid.NewGuid().ToString("N"));
        Static("SportsParticipantRegistry", "ForgetCache");
        telemetry = new GameObject("Foundation real telemetry observer"); UnityEngine.Object.DontDestroyOnLoad(telemetry); telemetry.AddComponent(T("SportsSessionTelemetry"));
        one = InputSystem.AddDevice<Gamepad>("Foundation match host"); two = InputSystem.AddDevice<Gamepad>("Foundation match second");
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        night = UnityEngine.Object.FindAnyObjectByType(T("SportsGameNightRuntime")) as Component;
        if (night == null) night = new GameObject("Foundation match integration").AddComponent(T("SportsGameNightRuntime"));
        yield return null; yield return null;
        Call(night, "Open", UnityEngine.Object.FindAnyObjectByType(T("SportsFrontEnd")), one);
        Call(night, "Join", two);
        string firstId = (string)Get(Static("SportsParticipantRegistry", "Default", 0), "id");
        string secondId = (string)Get(Static("SportsParticipantRegistry", "Default", 1), "id");
        Call(night, "SelectProfile", 0, firstId); Call(night, "SelectProfile", 1, secondId);
        var state = Get(night, "State"); Call(state, "SetReady", 0, true); Call(state, "SetReady", 1, true); Call(night, "SelectSport");
        Static("SportsNavigation", "LoadSport", "TruckDanceOff");
        yield return null; yield return null; yield return null; yield return null;
        var session = UnityEngine.Object.FindAnyObjectByType(T("SportsEventSession"));
        Get(session, "setup").GetType().GetField("count").SetValue(Get(session, "setup"), 4);
        session.GetType().GetField("option", Any).SetValue(session, 0); Call(session, "Save");
        Assert.That(Call(session, "StartMatch"), Is.EqualTo(false), "Editing settings requires every human to ready again.");
        yield return Press(one, GamepadButton.Start); yield return Press(two, GamepadButton.Start);
        Assert.That(Call(session, "StartMatch"), Is.EqualTo(true)); yield return null; yield return null;
        string firstMatch = (string)Get(session, "MatchId");
        Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Playing"));
        // Deliberately idle human inputs are valid misses. The existing AI and
        // DSP-driven song judge must produce the complete unmodified result.
        double deadline = Time.realtimeSinceStartupAsDouble + Convert.ToDouble(Get(Get(session, "Song"), "duration")) + 25;
        while (!(bool)Get(session, "Finished") && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That(Get(session, "Finished"), Is.EqualTo(true), "Actual song and result lifecycle must complete.");
        yield return null; yield return null;
        Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Results"));
        var totals = (int[])Get(session, "total"); Assert.That(Math.Max(totals[2], totals[3]), Is.GreaterThan(Math.Max(totals[0], totals[1])));
        var points = (IDictionary)Get(state, "Points"); Assert.That(points[firstId], Is.EqualTo(0)); Assert.That(points[secondId], Is.EqualTo(0));
        Assert.That(points.Count, Is.EqualTo(2), "AI victory must not create a human-profile session score entry.");
        InputSystem.DisableDevice(two); yield return null;
        InputSystem.EnableDevice(two); yield return null;
        Call(Get(session, "menus"), "Action", "rematch"); yield return null;
        Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Configuration"));
        Assert.That(Get(session, "Finished"), Is.EqualTo(false)); Assert.That(Get(state, "AllReady"), Is.EqualTo(false));
        yield return Press(one, GamepadButton.Start); yield return Press(two, GamepadButton.Start);
        Assert.That(Call(session, "StartMatch"), Is.EqualTo(true)); yield return null; yield return null;
        Assert.That(Get(session, "MatchId"), Is.Not.EqualTo(firstMatch));
        Static("SportsNavigation", "FrontEnd", true);
        yield return null; yield return null; yield return null; yield return null;
        Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Lobby"));
        Assert.That(Call(night, "Pad", 0), Is.SameAs(one)); Assert.That(Call(night, "Pad", 1), Is.SameAs(two));
        Assert.That(points[firstId], Is.EqualTo(0)); Assert.That(points[secondId], Is.EqualTo(0));
        Static("SportsParticipantRegistry", "ForgetCache");
        var firstStats = Get(Static("SportsParticipantRegistry", "Find", firstId), "statistics");
        Assert.That(Get(firstStats, "matches"), Is.EqualTo(1)); Assert.That(Get(firstStats, "losses"), Is.EqualTo(1)); Assert.That(Get(firstStats, "winnerPoints"), Is.EqualTo(0));
        var savedSessions = (IList)Get(firstStats, "sessionIds"); Assert.That(savedSessions.Count, Is.EqualTo(1));
        Call(night, "EndNight"); yield return null;
        yield return Press(one, GamepadButton.West); // Final-screen controller export.
        var report = Get(night, "Report"); var contents = Get(report, "Contents");
        Assert.That(Get(contents, "completedMatches"), Is.EqualTo(1)); Assert.That(Get(contents, "interruptedMatches"), Is.EqualTo(1));
        Assert.That(Get(contents, "status"), Is.EqualTo("completed")); Assert.That(Get(report, "SaveError"), Is.Null);
        var events = (IEnumerable)Get(contents, "events");
        Assert.That(events.Cast<object>().Any(e => (string)Get(e, "kind") == "device_changed"), Is.True);
        string jsonPath = (string)Get(report, "JsonPath"), summaryPath = (string)Get(report, "SummaryPath");
        Assert.That(File.Exists(jsonPath), Is.True); Assert.That(File.ReadAllText(jsonPath), Does.Contain(firstMatch));
        Assert.That(File.ReadAllText(summaryPath), Does.Contain("Completed matches: 1"));
        string stream = (string)T("SportsSessionTelemetry").GetProperty("CurrentFile").GetValue(null);
        using (var source = new FileStream(stream, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(source)) Assert.That(reader.ReadToEnd(), Does.Contain("game_night_ended"));
    }
    [UnityTest, Timeout(300000)]
    public IEnumerator FourControllersKeepProfilesAndReadyGatesAcrossAllSevenLegacySessions()
    {
        preferences = (IDisposable)Static("SportsPrefs", "IsolateValidation", "FoundationLegacy" + Guid.NewGuid().ToString("N"));
        Static("SportsParticipantRegistry", "ForgetCache");
        four = Enumerable.Range(0, 4).Select(i => InputSystem.AddDevice<Gamepad>("Legacy driver " + (i + 1))).ToArray();
        one = four[0]; two = four[1];
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        night = UnityEngine.Object.FindAnyObjectByType(T("SportsGameNightRuntime")) as Component;
        if (night == null) night = new GameObject("Legacy four-controller integration").AddComponent(T("SportsGameNightRuntime"));
        yield return null; yield return null;
        Call(night, "Open", UnityEngine.Object.FindAnyObjectByType(T("SportsFrontEnd")), one);
        for (int i = 1; i < 4; i++) Call(night, "Join", four[i]);
        string[] ids = Enumerable.Range(0, 4).Select(i => (string)Get(Static("SportsParticipantRegistry", "Default", i), "id")).ToArray();
        for (int i = 0; i < 4; i++) Call(night, "SelectProfile", i, ids[i]);
        var state = Get(night, "State");
        foreach (string scene in new[] { "TruckBowling", "TruckCornhole", "TruckCheeseRolling", "TruckHockey", "TruckAirHockey", "TruckWallball", "TruckBilliards" })
        {
            for (int i = 0; i < 4; i++) Call(state, "SetReady", i, true);
            Call(night, "SelectSport"); Static("SportsNavigation", "LoadSport", scene);
            yield return null; yield return null; yield return null; yield return null;
            var binding = Get(night, "binding"); Assert.That(binding, Is.Not.Null, scene);
            var owner = (Component)Get(binding, "Owner"); Assert.That(owner.gameObject.scene.name, Is.EqualTo(scene));
            string[] humans = (string[])Call(binding, "HumanProfileIds"); int expected = scene == "TruckAirHockey" || scene == "TruckBilliards" ? 2 : 4;
            Assert.That(humans.Length, Is.EqualTo(expected), scene + " supported human capacity");
            Assert.That(humans.Distinct().Count(), Is.EqualTo(expected));
            var roster = (Array)Get(binding, "Players");
            for (int i = 0; i < roster.Length; i++)
            {
                var player = roster.GetValue(i); string id = (string)Get(player, "participantId"); int seat = Array.IndexOf(ids, id);
                if (seat >= 0 && humans.Contains(id)) Assert.That(Call(binding, "Controller", i), Is.SameAs(four[seat]), scene + " source ownership");
            }
            Call(state, "SetReady", 0, false);
            var start = owner.GetType().GetMethod("StartMatch", Any);
            object[] startArgs = start.GetParameters().Length == 0 ? Array.Empty<object>() : new object[] { false };
            Assert.That(start.Invoke(owner, startArgs), Is.EqualTo(false), scene + " blocks an unready start");
            Call(state, "SetReady", 0, true);
            Assert.That(start.Invoke(owner, startArgs), Is.EqualTo(true), scene + " starts with the assigned real devices");
            yield return null; yield return null; yield return null;
            Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Playing"), scene + " match identity/lifecycle");
            Assert.That(Get(binding, "MatchId"), Is.Not.Null.And.Not.Empty);
            if (scene == "TruckHockey")
            {
                InputSystem.RemoveDevice(four[3]); yield return null; yield return null;
                four[3] = InputSystem.AddDevice<Gamepad>("Replacement fourth driver");
                yield return Press(four[3], GamepadButton.Start);
                Assert.That(Call(night, "Pad", 3), Is.SameAs(four[3]));
                Assert.That(Get(Call(night, "Profile", 3), "id"), Is.EqualTo(ids[3]), "Replacement preserves the actual driver identity.");
            }
            Static("SportsNavigation", "FrontEnd", true);
            yield return null; yield return null; yield return null; yield return null;
            Assert.That(Get(state, "Phase").ToString(), Is.EqualTo("Lobby"));
            for (int i = 0; i < 4; i++) Assert.That(Call(night, "Pad", i), Is.SameAs(four[i]));
            Assert.That(((IDictionary)Get(state, "Points")).Values.Cast<int>().All(p => p == 0), Is.True, "Interrupted starts never award points.");
        }
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Static("SportsControllerKeyboard", "Cancel");
        if (one != null && one.added) InputSystem.QueueStateEvent(one, new GamepadState());
        if (two != null && two.added) InputSystem.QueueStateEvent(two, new GamepadState());
        yield return null; yield return null; yield return null;
        if (night != null) UnityEngine.Object.Destroy(night.gameObject);
        if (telemetry != null) UnityEngine.Object.Destroy(telemetry);
        if (one != null && one.added) InputSystem.RemoveDevice(one);
        if (two != null && two.added) InputSystem.RemoveDevice(two);
        if (four != null) foreach (var pad in four) if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
        four = null;
        yield return null;
        Static("SportsParticipantRegistry", "ForgetCache"); preferences?.Dispose(); preferences = null;
    }
}
#endif
