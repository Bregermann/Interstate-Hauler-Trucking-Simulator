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
    static void DrivePadTowardVelocity(Gamepad pad, Component truck, Rigidbody body, Vector3 wanted, bool action = false)
    {
        Vector3 local = truck.transform.InverseTransformDirection(wanted);
        float angle = Mathf.Atan2(local.x, Mathf.Max(.1f, local.z));
        float steer = Mathf.Clamp(angle * 1.4f - body.angularVelocity.y * .35f, -.6f, .6f);
        float requested = Mathf.Max(0, Vector3.Dot(wanted, truck.transform.forward));
        float effort = Mathf.Clamp((requested - Vector3.Dot(body.linearVelocity, truck.transform.forward)) * .5f, -.8f, .8f);
        Queue(pad, steer, effort, action);
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
                var shoulders = (Array)Read(match, "recoveryShoulders");
                Assert.That(shoulders.Length, Is.EqualTo(4), "Run the explicit saved Unhitched recovery-shoulder authoring first; no test-time scenery generation.");
                var shoulder = shoulders.GetValue(0); Assert.That(shoulder, Is.Not.Null);
                while (((float[])Read(match, "supportedRideSeconds"))[0] < 2 && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) { Queue(pad); yield return null; }
                Assert.That(((float[])Read(match, "supportedRideSeconds"))[0], Is.GreaterThanOrEqualTo(2), State(match));
                // A forward, gently steered departure uses the actual lower
                // road. It does not ask NWH to reverse at world-forward speed.
                float nextObservation = 0; string lastLive = State(match); bool roadContactSeen = false;
                while ((!((bool[])Read(match, "recovering"))[0] || !(bool)Call(shoulder, "HasNativeWheelContact", truck)) && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    Vector3 point = (Vector3)Call(shoulder, "PointAtForwardPosition", body.position.z + 18, true);
                    Vector3 direction = Vector3.ProjectOnPlane(point - body.position, Vector3.up).normalized;
                    float pace = Mathf.Clamp(Vector3.Dot(deck.linearVelocity, Vector3.forward) + 1, 6, 12);
                    DrivePadTowardVelocity(pad, truck, body, direction * pace);
                    if (Time.time >= nextObservation) { nextObservation = Time.time + 2; lastLive = State(match); Debug.Log("HH10_UNHITCHED_SHOULDER_DEPARTURE " + lastLive); }
                    yield return null;
                }
                roadContactSeen = (bool)Call(shoulder, "HasNativeWheelContact", truck);
                Assert.That(((bool[])Read(match, "recovering"))[0], Is.True, "Only an actual unsupported fall opens recovery. " + State(match));
                Assert.That(roadContactSeen, Is.True, "The native tyres must physically land on the authored recovery road. " + State(match));
                float pressUntil = 0, nextPress = 0;
                while (((int[])Read(match, "recoveries"))[0] == 0 && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    if (Time.time >= nextObservation) { nextObservation = Time.time + 2; lastLive = State(match); Debug.Log("HH10_UNHITCHED_RECOVERY_LIVE " + lastLive); }
                    bool grounded = (bool)Property(truck, "Grounded"), onDeck = (bool)Call(match, "OwnDeckWheelSupport", 0);
                    if (onDeck) Queue(pad);
                    else if (grounded)
                    {
                        object[] prediction = { 0, Vector3.zero };
                        bool valid = (bool)match.GetType().GetMethod("TryRecoveryLaunchVelocity").Invoke(match, prediction);
                        Vector3 wanted = valid ? (Vector3)prediction[1] : Vector3.forward * Mathf.Max(0, deck.linearVelocity.z);
                        if (Time.time >= nextPress && (bool)Call(match, "RecoveryHopPrediction", 0)) { pressUntil = Time.realtimeSinceStartup + .18f; nextPress = Time.time + 1.5f; }
                        DrivePadTowardVelocity(pad, truck, body, wanted, Time.realtimeSinceStartup < pressUntil);
                    }
                    else Queue(pad, Mathf.Clamp(-body.angularVelocity.y * .5f, -.6f, .6f), 0, Time.realtimeSinceStartup < pressUntil);
                    yield return null;
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
