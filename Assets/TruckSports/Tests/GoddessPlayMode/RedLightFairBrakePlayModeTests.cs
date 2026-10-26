#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RedLightFairBrakePlayModeTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static Behaviour Match() => UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckRedLightGreenLight.RedLightMatch")).Cast<Behaviour>().Single();
    static object Player(Behaviour match) => ((Array)Get(Get(match, "setup"), "players")).GetValue(0);
    static void Source(object player, string value) => Set(player, "source", Enum.Parse(player.GetType().GetField("source").FieldType, value));
    static void Intents(Behaviour match, Vector2 command, bool accelerator, bool brake, string label)
    {
        var args = new object[] { 0, command, false, false }; CallIntent(match, args);
        Assert.That((bool)args[2], Is.EqualTo(accelerator), label + " accelerator"); Assert.That((bool)args[3], Is.EqualTo(brake), label + " brake");
    }
    static void CallIntent(Behaviour match, object[] args) => match.GetType().GetMethod("ReadDriverIntent", Any).Invoke(match, args);
    static IEnumerator Keys(Keyboard keyboard, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); keyboard.MakeCurrent(); yield return null; yield return null;
    }
    static IEnumerator Pad(Gamepad pad, GamepadState state)
    {
        InputSystem.QueueStateEvent(pad, state); yield return null; yield return null;
    }
    static GameObject Isolate()
    {
        var isolation = new GameObject("Red Light fair brake software-input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation")); return isolation;
    }
    static InputSettings InputClone(InputSettings original)
    {
        var temporary = UnityEngine.Object.Instantiate(original); temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = temporary; return temporary;
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator AssignedSourcesKeepIndependentIntentsBeforeSignedAxisCancellation()
    {
        var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "RedLightFairBrakeSources" });
        var original = InputSystem.settings; InputSettings temporary = null; Keyboard keyboard = null; Gamepad pad = null; Behaviour match = null; GameObject isolation = null;
        try
        {
            temporary = InputClone(original); isolation = Isolate(); keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard); pad = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(pad);
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = Match(); Call(match, "AllBots"); Set(Get(match, "setup"), "count", 1); var player = Player(match);
            // Input-boundary fixture only. Existing simulation stays disabled;
            // explicit released-state cases test the original source gate.
            match.enabled = false; Source(player, "Keyboard"); Set(player, "forward", Key.W); Set(player, "back", Key.S); Set(player, "released", true);
            yield return Keys(keyboard, Key.W, Key.S);
            Assert.That(((Vector2)Call(match, "ReadDrive", 0)).y, Is.Zero, "The established signed keyboard drive cancels both keys.");
            Intents(match, Vector2.zero, true, true, "both configured keyboard keys");
            yield return Keys(keyboard, Key.S); Intents(match, Vector2.zero, false, true, "keyboard brake only");
            Set(player, "forward", Key.UpArrow); Set(player, "back", Key.DownArrow);
            yield return Keys(keyboard, Key.UpArrow, Key.DownArrow); Intents(match, Vector2.zero, true, true, "configured alternate keyboard keys");
            Set(player, "released", false); Intents(match, Vector2.zero, false, false, "held keys before neutral release gate"); Set(player, "released", true);

            Source(player, "WholeController"); Set(player, "pad", pad); yield return Pad(pad, new GamepadState { rightTrigger = .9f, leftTrigger = .9f });
            Assert.That(((Vector2)Call(match, "ReadDrive", 0)).y, Is.Zero, "The established signed trigger drive cancels both pedals.");
            Intents(match, Vector2.zero, true, true, "independent whole-pad throttle and brake");
            yield return Pad(pad, new GamepadState { leftTrigger = .9f }); Intents(match, Vector2.zero, false, true, "whole-pad service brake");
            yield return Pad(pad, new GamepadState { rightTrigger = .9f }.WithButton(GamepadButton.South)); Intents(match, Vector2.zero, true, true, "whole-pad accelerator plus ACTION handbrake");
            Set(player, "released", false); Intents(match, Vector2.zero, false, false, "held pad before neutral release gate"); Set(player, "released", true);
            Set(player, "pad", null); Intents(match, Vector2.zero, false, false, "unavailable assigned device"); Set(player, "pad", pad);

            Source(player, "LeftStick"); yield return Pad(pad, new GamepadState { leftStick = Vector2.up, rightStick = Vector2.down, rightTrigger = 1 });
            Intents(match, Vector2.zero, true, false, "left-stick owner ignores other owner's ACTION trigger");
            yield return Pad(pad, new GamepadState { leftStick = Vector2.up, leftTrigger = 1 }); Intents(match, Vector2.zero, true, true, "left-stick accelerator plus assigned ACTION brake");
            Source(player, "RightStick"); yield return Pad(pad, new GamepadState { rightStick = Vector2.down, leftStick = Vector2.up, leftTrigger = 1 });
            Intents(match, Vector2.zero, false, true, "right-stick signed service brake");
            yield return Pad(pad, new GamepadState { rightStick = Vector2.up, rightTrigger = 1 }); Intents(match, Vector2.zero, true, true, "right-stick accelerator plus assigned ACTION brake");

            Source(player, "AI"); Intents(match, Vector2.up, true, false, "actual AI drive command path"); Intents(match, Vector2.down, false, true, "actual AI braking command path");
            var rules = Get(match, "rules"); Call(rules, "Reset", 1, 14); for (int step = 0; step < 2000 && Get(rules, "Phase").ToString() != "Red"; step++) Call(rules, "Advance", .01f);
            var bot = (Vector2)Call(match, "Bot", 0); Assert.That(bot.y, Is.LessThan(0)); Intents(match, bot, false, true, "existing AI chooses ordinary braking on RED");
            TestContext.WriteLine("Software source-boundary cases cover configured keys, independent RT/LT, both shared sticks, assigned ACTION, neutral-release/device gates and the unchanged AI command path. No physical device or driving acceptance claimed by this fixture.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { Call(match, "SetupScreen"); match.enabled = true; }
            InputSystem.settings = original; if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary); if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose();
        }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator SnowBrakeRetainsResidualMotionAndCancelledThrottleStillExplodesOnce()
    {
        var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "RedLightFairBrakeSnow" });
        var original = InputSystem.settings; InputSettings temporary = null; Keyboard keyboard = null; Behaviour match = null; GameObject isolation = null;
        try
        {
            temporary = InputClone(original); isolation = Isolate(); keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard);
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = Match(); Call(match, "AllBots"); Set(Get(match, "setup"), "count", 1); var player = Player(match); Source(player, "Keyboard");
            Set(player, "forward", Key.W); Set(player, "back", Key.S); Set(player, "left", Key.A); Set(player, "right", Key.D);
            var catalog = Get(Get(match, "atmosphere"), "presets"); var snow = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Snow"); Call(catalog, "Select", Get(snow, "id"));
            yield return Keys(keyboard); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null; yield return null;
            var rules = Get(match, "rules"); float deadline = Time.realtimeSinceStartup + 30;
            while (Get(rules, "Phase").ToString() != "LaughWarning" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get(rules, "Phase").ToString(), Is.EqualTo("LaughWarning"));
            yield return Keys(keyboard, Key.S);
            while (Get(rules, "Phase").ToString() != "Red" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get(rules, "Phase").ToString(), Is.EqualTo("Red"));
            Assert.That((string)Get(Get(match, "atmosphere"), "PresetId"), Is.EqualTo((string)Get(snow, "id")));
            var truck = ((Array)Get(match, "trucks")).GetValue(0); var body = (Rigidbody)Get(truck, "body"); var motor = Get(truck, "tractorMotor");
            // Explicit sliding-velocity fixture followed by actual NWH/PhysX,
            // existing Snow grip and normal keyboard braking. No position edit.
            Call(motor, "AddAssemblyVelocity", new Vector3(3, 0, 8)); float speed = body.linearVelocity.magnitude; Assert.That(speed, Is.GreaterThan(5));
            var start = body.position;
            for (int step = 0; step < 10; step++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(((Array)Get(rules, "outcomes")).GetValue(0).ToString(), Is.EqualTo("Racing"), "Active brake-only sliding must remain safe.");
                Assert.That(((bool[])Get(match, "lastBraking"))[0], Is.True); Assert.That(((bool[])Get(match, "lastAccelerating"))[0], Is.False);
            }
            float slideDistance = Vector3.ProjectOnPlane(body.position - start, Vector3.up).magnitude;
            Assert.That(slideDistance, Is.GreaterThan(.05f), "The fixture must really slide, not only compare invented velocity values.");
            var vehicleInput = Get(Get(motor, "vehicle"), "input"); var states = Get(vehicleInput, "states"); Assert.That((float)Get(states, "throttle"), Is.Zero); Assert.That((float)Get(states, "handbrake"), Is.EqualTo(1));
            Assert.That((float)Get(vehicleInput, "InputSwappedThrottle"), Is.Zero, "NWH must not turn brake-only RED input into powered reverse.");
            Assert.That((int)Get(match, "Explosions"), Is.Zero); Assert.That(Get(rules, "Phase").ToString(), Is.EqualTo("Red"));
            yield return Keys(keyboard, Key.W, Key.S);
            Assert.That(((Vector2)Call(match, "ReadDrive", 0)).y, Is.Zero, "Both keys cancel normal drive while their independent intent remains visible.");
            deadline = Time.realtimeSinceStartup + 2;
            while ((int)Get(match, "Explosions") == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((int)Get(match, "Explosions"), Is.EqualTo(1)); Assert.That(((Array)Get(rules, "outcomes")).GetValue(0).ToString(), Is.EqualTo("Eliminated"));
            Assert.That((bool)Get(((Array)Get(match, "fragments")).GetValue(0), "Bursting"), Is.True);
            Assert.That((float)Get(match, "explosionRadius"), Is.EqualTo(20)); Assert.That((float)Get(match, "explosionVelocity"), Is.EqualTo(12));
            yield return new WaitForFixedUpdate(); Assert.That((int)Get(match, "Explosions"), Is.EqualTo(1));
            Call(match, "SetupScreen"); yield return Keys(keyboard); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            Assert.That(((bool[])Get(match, "lastAccelerating"))[0], Is.False); Assert.That(((bool[])Get(match, "lastBraking"))[0], Is.False); Assert.That((int)Get(match, "Explosions"), Is.Zero);
            TestContext.WriteLine("Actual Snow/NWH brake-only fixture retained a controlled initial sliding velocity=" + speed + ", moved=" + slideDistance + "; raw keyboard throttle+brake cancellation then caused exactly one original destruction event; rematch cleared intent. No physical hardware or listening acceptance.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); InputSystem.settings = original; if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary); if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose();
        }
    }
}
#endif
