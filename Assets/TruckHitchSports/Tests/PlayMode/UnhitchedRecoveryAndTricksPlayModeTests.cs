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

public sealed class UnhitchedRecoveryAndTricksPlayModeTests
{
    static object Read(object value, string name)
    {
        for (var type = value.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) return field.GetValue(value);
        }
        throw new MissingFieldException(value.GetType().FullName, name);
    }
    static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
    static Type Runtime => Type.GetType("LWS.TruckHitchSports.UnhitchedMatch, Assembly-CSharp", true);
    static IDisposable Preferences(string key) => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { key });
    static void Clear(object match)
    {
        var weather = Read(match, "presentationWeather");
        if (weather != null && Read(weather, "presets") != null) Call(Read(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
    }
    static void Queue(Gamepad pad, float steer = 0, float effort = 0, bool action = false)
    {
        var state = new GamepadState { leftStick = new Vector2(steer, 0), rightTrigger = Mathf.Max(0, effort), leftTrigger = Mathf.Max(0, -effort) };
        if (action) state = state.WithButton(GamepadButton.South);
        InputSystem.QueueStateEvent(pad, state);
    }
    static string State(object match) => Call(match, "PlatformDiagnostic", 0) + " " + Call(match, "RecoveryDiagnostic", 0) + " " + Call(match, "TrickDiagnostic", 0);

    [UnityTest]
    public IEnumerator HumanActuallyFallsGroundHopsAndRemountsSameMovingNativeDeck()
    {
        using (Preferences("Alpha10UnhitchedPhysicalRecovery"))
        {
            var background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var pad = InputSystem.AddDevice<Gamepad>(); object match = null;
            float deadline = Time.realtimeSinceStartup + 95;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/Unhitched.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                var setup = Read(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1); match.GetType().GetField("option").SetValue(match, 0);
                var player = ((Array)Read(setup, "players")).GetValue(0); var source = player.GetType().GetField("source");
                source.SetValue(player, Enum.Parse(source.FieldType, "WholeController")); player.GetType().GetField("pad").SetValue(player, pad);
                Call(match, "NormalizeSetup"); Queue(pad); yield return null; yield return null; yield return null;
                Clear(match); Assert.That((bool)Property(match, "DevicesReady"), Is.True); Assert.That((bool)Call(match, "StartMatch"), Is.True); Time.timeScale = 2;
                var truck = (Component)((Array)Read(match, "trucks")).GetValue(0); var body = (Rigidbody)Read(truck, "body");
                var platform = ((Array)Read(match, "trailers")).GetValue(0); var deck = (Rigidbody)Read(platform, "Body"); int deckId = deck.GetInstanceID();
                var surface = ((Collider[])Read(match, "deckSurfaces"))[0];
                while (((float[])Read(match, "supportedRideSeconds"))[0] < 2.5f && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) { Queue(pad); yield return null; }
                Assert.That(((float[])Read(match, "supportedRideSeconds"))[0], Is.GreaterThanOrEqualTo(2.5f), State(match));
                // Brake firmly while the native deck is still rolling on the
                // flat run-up. Beyond the ramp it can stop directly under the truck.
                while (!((bool[])Read(match, "recovering"))[0] && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) { Queue(pad, 0, -.95f); yield return null; }
                Assert.That(((bool[])Read(match, "recovering"))[0], Is.True, "A real unsupported fall must enter recovery rather than teleporting/resetting. " + State(match));
                float nextPress = 0, pressUntil = 0, nextObservation = 0; string lastLive = State(match);
                while (((int[])Read(match, "recoveries"))[0] == 0 && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    if (Time.time >= nextObservation) { nextObservation = Time.time + 2; lastLive = State(match); Debug.Log("HH10_UNHITCHED_RECOVERY_LIVE " + lastLive); }
                    bool grounded = (bool)Property(truck, "Grounded"); float steering = 0, effort = 0;
                    if (grounded)
                    {
                        Vector3 target = (Vector3)Call(match, "RecoveryAimPoint", 0);
                        Vector3 local = truck.transform.InverseTransformPoint(new Vector3(target.x, body.position.y, target.z));
                        steering = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f, -1, 1);
                        float desired = Mathf.Min(16, deck.linearVelocity.magnitude + 4);
                        effort = Mathf.Clamp((desired - Vector3.Dot(body.linearVelocity, truck.transform.forward)) * .35f, -.7f, .8f);
                        if (Time.time >= nextPress && (bool)Call(match, "RecoveryHopPrediction", 0)) { pressUntil = Time.realtimeSinceStartup + .18f; nextPress = Time.time + 1.5f; }
                    }
                    Queue(pad, steering, effort, Time.realtimeSinceStartup < pressUntil); yield return null;
                }
                Queue(pad);
                Assert.That(((int[])Read(match, "falls"))[0], Is.GreaterThan(0), State(match));
                Assert.That(((int[])Read(match, "recoveryHops"))[0], Is.GreaterThan(0), "Only an actual grounded ACTION impulse can certify the remount-hop input. " + State(match) + " lastLive=[" + lastLive + "]");
                Assert.That(((int[])Read(match, "recoveries"))[0], Is.GreaterThan(0), "Actual stable supported wheel contact on the same native deck is required. " + State(match) + " lastLive=[" + lastLive + "]");
                Assert.That((bool)Call(match, "OwnDeckWheelSupport", 0), Is.True, "Remount requires a native wheel contact on the designated load-bearing deck top, not trailer chassis or arena contact. " + State(match));
                Assert.That(deck.GetInstanceID(), Is.EqualTo(deckId));
                Assert.That(Property(((Array)Read(match, "couplers")).GetValue(0), "Attached"), Is.Null, "Recovery must not create a hidden hitch.");
                string recovered = State(match);
                Call(match, "SetupScreen"); Queue(pad); yield return null; yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That(((bool[])Read(match, "recovering")).Any(value => value), Is.False);
                Assert.That(((int[])Read(match, "recoveries")).Any(value => value > 0), Is.False);
                Assert.That(((int[])Read(match, "recoveryHops")).Any(value => value > 0), Is.False);
                Debug.Log("HH10_UNHITCHED_ACTUAL_RECOVERY " + recovered + " rematch=fresh");
            }
            finally
            {
                Time.timeScale = 1;
                if (match is Component component && component != null) Call(match, "SetupScreen");
                InputSystem.RemoveDevice(pad); InputSystem.settings.backgroundBehavior = background;
            }
        }
    }

    [UnityTest]
    public IEnumerator NaturalTrickChallengeBanksActualLandedNetQuarterTurnsAndRematches()
    {
        using (Preferences("Alpha10UnhitchedNetTricks"))
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/Unhitched.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
            var setup = Read(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1); match.GetType().GetField("option").SetValue(match, 3);
            Call(match, "NormalizeSetup"); Call(match, "AllBots"); Clear(match); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            try
            {
                Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 75, nextObservation = 0; string lastLive = State(match);
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    if (Time.time >= nextObservation) { nextObservation = Time.time + 2; lastLive = State(match); Debug.Log("HH10_UNHITCHED_TRICK_LIVE " + lastLive); }
                    yield return null;
                }
                string state = State(match) + " lastLive=[" + lastLive + "] netQuarterTurns=" + ((int[])Read(match, "quarterTurns"))[0] + " landedAir=" + ((float[])Read(match, "landedAirtime"))[0];
                Assert.That((bool)Property(match, "Finished"), Is.True, state);
                Assert.That(((int[])Read(match, "quarterTurns"))[0], Is.GreaterThan(0), "Ground turns and opposite-direction wiggles cannot substitute for a landed net90degree airborne turn. " + state);
                Assert.That(((int[])Read(match, "cleanLandings"))[0], Is.GreaterThan(0), state);
                Assert.That(((double[])Read(match, "Scores"))[0], Is.GreaterThan(0), state);
                Assert.That((bool)Call(match, "ResultEligible", 0), Is.True, state);
                Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That(((int[])Read(match, "quarterTurns")).Any(value => value > 0), Is.False);
                Assert.That(((int[])Read(match, "bestCombo")).Any(value => value > 0), Is.False);
                Debug.Log("HH10_UNHITCHED_ACTUAL_NET_TRICKS " + state + " rematch=fresh");
            }
            finally { Time.timeScale = 1; if (match is Component component && component != null) Call(match, "SetupScreen"); }
        }
    }
}
#endif
