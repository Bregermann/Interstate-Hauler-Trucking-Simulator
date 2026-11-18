#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RocketRollOwnershipPlayModeTests
{
    static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method).Invoke(value, args);
    static Type Runtime => Type.GetType("LWS.TruckHitchSports.TrailerblazersMatch, Assembly-CSharp", true);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10RocketRoll" });
    static void Load() => EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/Trailerblazers.unity", new LoadSceneParameters(LoadSceneMode.Single));
    static void SetSource(object player, string source, Gamepad pad)
    {
        var field = player.GetType().GetField("source");
        field.SetValue(player, Enum.Parse(field.FieldType, source)); player.GetType().GetField("pad").SetValue(player, pad);
    }

    [UnityTest]
    public IEnumerator WholeSharedAndKeyboardRollKeepIndependentOwnershipAndHorn()
    {
        using (Preferences())
        {
            var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var whole = InputSystem.AddDevice<Gamepad>(); var shared = InputSystem.AddDevice<Gamepad>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            object match = null;
            try
            {
                Load(); yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4);
                var players = (Array)Field(setup, "players");
                SetSource(players.GetValue(0), "WholeController", whole); SetSource(players.GetValue(1), "LeftStick", shared); SetSource(players.GetValue(2), "RightStick", shared); SetSource(players.GetValue(3), "Keyboard", null);
                Call(match, "NormalizeSetup");
                yield return null;yield return null;yield return null;
                object[] validation={null};bool valid=(bool)match.GetType().GetMethod("ValidateSources").Invoke(match,validation);
                Assert.That(valid,Is.True,"The ordinary assigned-source roster must be valid: "+validation[0]);
                Assert.That((bool)Property(match,"DevicesReady"),Is.True,"Whole/shared/keyboard software devices must be enabled before entering gameplay: "+whole.enabled+"/"+shared.enabled+"/"+keyboard.enabled);
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                yield return null; yield return null;
                var rollKey = (Key)Call(match, "RollKey", 3, true);
                Assert.That(rollKey, Is.Not.EqualTo(Key.None));
                InputSystem.QueueStateEvent(whole, new GamepadState().WithButton(GamepadButton.RightShoulder));
                InputSystem.QueueStateEvent(shared, new GamepadState().WithButton(GamepadButton.DpadRight).WithButton(GamepadButton.West));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(rollKey));
                keyboard.MakeCurrent(); // This fixture owns the one global keyboard used by keyboard bindings.
                yield return null; yield return null;
                Assert.That((float)Call(match, "ReadRollInput", 0), Is.EqualTo(1));
                Assert.That((float)Call(match, "ReadRollInput", 1), Is.EqualTo(1));
                Assert.That((float)Call(match, "ReadRollInput", 2), Is.EqualTo(-1));
                Assert.That(keyboard[rollKey].isPressed, Is.True, "Virtual keyboard event must have reached the assigned key: "+rollKey);
                Assert.That(Keyboard.current, Is.SameAs(keyboard), "Keyboard bindings use the currently active keyboard.");
                Assert.That((float)Call(match, "ReadRollInput", 3), Is.EqualTo(1));
                InputSystem.QueueStateEvent(whole, new GamepadState().WithButton(GamepadButton.LeftStick));
                InputSystem.QueueStateEvent(shared, new GamepadState().WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.RightShoulder));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null; yield return null;
                for (int slot = 0; slot < 4; slot++) Assert.That((float)Call(match, "ReadRollInput", slot), Is.Zero, "Horn controls cannot roll another source's rig.");
                Call(match, "Pause", 0, false);
                Assert.That((float)Call(match, "ReadRollInput", 0), Is.Zero, "Modal pause suppresses auxiliary flight input.");
                Call(match, "SetupScreen");
            }
            finally
            {
                Time.timeScale = 1;
                if (match is Component component && component != null) Call(match, "SetupScreen");
                InputSystem.RemoveDevice(whole); InputSystem.RemoveDevice(shared); InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;
            }
        }
    }

    [UnityTest]
    public IEnumerator NaturalNormalFlightShowsCanopyOnlyWithActualAppliedBrake()
    {
        using (Preferences())
        {
            Load(); yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
            var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
            match.GetType().GetField("option").SetValue(match, 0); Call(match, "NormalizeSetup"); Call(match, "AllBots");
            var player = ((Array)Field(setup, "players")).GetValue(0); var difficulty = player.GetType().GetField("difficulty"); difficulty.SetValue(player, Enum.Parse(difficulty.FieldType, "Normal"));
            var weather = Field(match, "presentationWeather");
            if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
            var presentations = (Array)Field(match, "flightPresentation");
            Assert.That(presentations.Length, Is.GreaterThan(0), "Apply the explicit saved-scene presentation upgrade first.");
            object presentation = presentations.GetValue(0);
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
            try
            {
                Time.timeScale = 3; bool forceObserved = false, canopyObserved = false;
                float deadline = Time.realtimeSinceStartup + 70;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    bool braking = (float)Call(match, "AppliedAirBrakeNewtons", 0) > 0;
                    forceObserved |= braking;
                    if (braking && (float)Property(presentation, "Opening") > .2f)
                    {
                        canopyObserved |= ((Transform)Field(presentation, "Canopy")).gameObject.activeInHierarchy && ((LineRenderer[])Field(presentation, "Harness")).All(line => line.enabled);
                    }
                    yield return null;
                }
                string state = (string)Call(match, "FlightDiagnostic", 0);
                Assert.That((bool)Property(match, "Finished"), Is.True, state);
                Assert.That(forceObserved && canopyObserved, Is.True, "A real descent brake must deploy the existing mesh and harness. " + state);
                Assert.That((int)Call(match, "StageReleaseCount", 0), Is.EqualTo(1), state);
                Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((float)Property(presentation, "Opening"), Is.Zero, "Rematch clears deployment presentation.");
                Assert.That((ParticleSystem)Field(presentation, "Crash"), Is.Not.Null);
                Assert.That(((ParticleSystem)Field(presentation, "Crash")).particleCount, Is.Zero);
                Call(match, "SetupScreen");
            }
            finally { Time.timeScale = 1; }
        }
    }
}
#endif
