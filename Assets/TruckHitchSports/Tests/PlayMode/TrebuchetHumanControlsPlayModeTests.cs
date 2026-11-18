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

public sealed class TrebuchetHumanControlsPlayModeTests
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
    static Type Runtime => Type.GetType("LWS.TruckHitchSports.TrebuchetMatch, Assembly-CSharp", true);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10TrebuchetHumanControls" });
    static void Queue(Gamepad pad, float steering = 0, float effort = 0, bool action = false)
    {
        var state = new GamepadState { leftStick = new Vector2(steering, 0), rightTrigger = Mathf.Max(0, effort), leftTrigger = Mathf.Max(0, -effort) };
        if (action) state = state.WithButton(GamepadButton.South);
        InputSystem.QueueStateEvent(pad, state);
    }
    static void DriveToThroughPad(Gamepad pad, Rigidbody tractor, Transform transform, Vector3 target, float desiredSpeed)
    {
        Vector3 local = transform.InverseTransformPoint(new Vector3(target.x, tractor.position.y, target.z));
        float steering = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f, -1, 1);
        float forwardSpeed = Vector3.Dot(tractor.linearVelocity, transform.forward);
        Queue(pad, steering, Mathf.Clamp((desiredSpeed - forwardSpeed) * .65f, -.8f, .8f));
    }
    static string State(object match, object engine, Rigidbody tractor, Rigidbody cargo) => "age=" + Property(match, "Elapsed") + " tractor=" + tractor.position.ToString("F2") + " cargoCOM=" + cargo.worldCenterOfMass.ToString("F2") + " cargoV=" + cargo.linearVelocity.ToString("F2") + " " + Call(match, "TrebuchetLoadingDiagnostic", 0) + " " + Call(match, "ReleaseDecisionDiagnostic", 0);

    [UnityTest]
    public IEnumerator AssignedHumanGamepadLoadsDrivesClearAimsSwingsAndReleasesPhysicalCargo()
    {
        using (Preferences())
        {
            var previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var pad = InputSystem.AddDevice<Gamepad>(); object match = null;
            float deadline = Time.realtimeSinceStartup + 95;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                var setup = Read(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                match.GetType().GetField("option").SetValue(match, 0);
                var player = ((Array)Read(setup, "players")).GetValue(0); var source = player.GetType().GetField("source");
                source.SetValue(player, Enum.Parse(source.FieldType, "WholeController")); player.GetType().GetField("pad").SetValue(player, pad);
                Call(match, "NormalizeSetup");
                Queue(pad); yield return null; yield return null; yield return null;
                object[] validation = { null };
                Assert.That((bool)match.GetType().GetMethod("ValidateSources").Invoke(match, validation), Is.True, "Ordinary assigned player must validate: " + validation[0]);
                Assert.That((bool)Property(match, "DevicesReady"), Is.True);
                var weather = Read(match, "presentationWeather");
                if (weather != null && Read(weather, "presets") != null) Call(Read(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                var truck = (Component)((Array)Read(match, "trucks")).GetValue(0);
                var tractor = (Rigidbody)Read(truck, "body");
                var cargo = ((Array)Read(match, "trailers")).GetValue(0); var load = (Rigidbody)Read(cargo, "Body"); int cargoId = load.GetInstanceID();
                var engine = ((Array)Read(match, "engines")).GetValue(0); var arm = (Rigidbody)Read(engine, "Arm");
                var cradle = (BoxCollider)Read(engine, "LoadingCradle"); var bridges = (Rigidbody[])Read(engine, "LoadingBridges");
                Assert.That(bridges.Length, Is.EqualTo(2), "Run the explicit real bridge actuator installation first.");
                var bridgeStarts = bridges.Select(body => body.position).ToArray();
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Time.timeScale = 2;
                while ((float)Property(match, "Elapsed") < 3 && Time.realtimeSinceStartup < deadline) { Queue(pad); yield return null; }

                // This is an input-driven test driver, not an AI-source switch:
                // its only writes are real gamepad events consumed by the game.
                while (!(bool)Call(engine, "CanLoad", cargo) && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    DriveToThroughPad(pad, tractor, truck.transform, new Vector3(cradle.bounds.center.x, tractor.position.y, tractor.position.z + 12), 1.8f);
                    yield return null;
                }
                Assert.That((bool)Call(engine, "CanLoad", cargo), Is.True, State(match, engine, tractor, load));
                while (tractor.linearVelocity.magnitude > .25f && Time.realtimeSinceStartup < deadline) { Queue(pad, 0, -1); yield return null; }
                Queue(pad); yield return null; yield return null;
                Queue(pad, action: true); yield return null; yield return null;
                Queue(pad); yield return null; yield return null;
                Assert.That(Property(engine, "Load"), Is.SameAs(cargo), "Only actual ACTION in the physically loaded cradle may lock the sling. " + State(match, engine, tractor, load));
                Assert.That((bool)Property(engine, "SlingIntact"), Is.True);

                Vector3 lockedTractorPosition = tractor.position; Quaternion lockedArmRotation = arm.rotation;
                float originalAssist = ((float[])Read(match, "assistPower"))[0]; bool nativeInputObserved = false;
                Vector3 exit = cradle.bounds.center + Vector3.right * 20 + Vector3.forward * 30;
                while (!(bool)Property(engine, "PreparingToSwing") && !(bool)Property(engine, "LoadingBridgesReady") && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    DriveToThroughPad(pad, tractor, truck.transform, exit, 2.5f);
                    yield return null;
                    var vehicle = Read(Read(truck, "tractorMotor"), "vehicle"); var states = Read(Read(vehicle, "input"), "states");
                    nativeInputObserved |= Convert.ToSingle(Read(states, "throttleRaw")) > .15f || Mathf.Abs(Convert.ToSingle(Read(states, "steeringRaw"))) > .15f;
                    Assert.That(Quaternion.Angle(arm.rotation, lockedArmRotation), Is.LessThan(.5f), "Driving clear must not aim an arm still surrounded by the loading roads.");
                    Assert.That(((float[])Read(match, "assistPower"))[0], Is.EqualTo(originalAssist).Within(.001f), "Driving throttle must not also alter winch assist.");
                }
                Queue(pad);
                Assert.That(nativeInputObserved, Is.True, "Assigned post-lock driving input must reach the native tractor.");
                Assert.That(Vector3.Distance(tractor.position, lockedTractorPosition), Is.GreaterThan(4), "The old permanent ParkAI bug would leave this human tractor beside the cradle. " + State(match, engine, tractor, load));
                while (!(bool)Property(engine, "LoadingBridgesReady") && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) { Queue(pad); yield return null; }
                Assert.That((bool)Property(engine, "LoadingBridgesReady"), Is.True, State(match, engine, tractor, load));
                Assert.That((bool)Property(engine, "Swinging"), Is.False, "Retraction for human aim cannot automatically fire.");
                Assert.That(arm.isKinematic, Is.True, "The mechanical lock holds until the human's next ACTION.");
                for (int i = 0; i < bridges.Length; i++) Assert.That(Vector3.Distance(bridges[i].position, bridgeStarts[i]), Is.GreaterThan(5.9f));

                float aimUntil = Time.time + .5f;
                while (Time.time < aimUntil && Time.realtimeSinceStartup < deadline) { Queue(pad, -.65f, .7f); yield return null; }
                Queue(pad); yield return null; yield return null;
                Assert.That(((float[])Read(match, "assistPower"))[0], Is.GreaterThan(originalAssist + .05f));
                Assert.That(Quaternion.Angle(arm.rotation, lockedArmRotation), Is.GreaterThan(1));
                Assert.That((bool)Property(engine, "Swinging"), Is.False);
                Queue(pad, action: true); yield return null; yield return null;
                Queue(pad); yield return null; yield return null;
                Assert.That((bool)Property(engine, "Swinging"), Is.True, "A fresh assigned ACTION must unlock the real hinge. " + State(match, engine, tractor, load));
                Assert.That(arm.isKinematic, Is.False);
                while (!(load.linearVelocity.y > 1 && Vector3.Dot(load.linearVelocity, Vector3.forward) > .1f) && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) { Queue(pad); yield return null; }
                Assert.That(load.linearVelocity.y, Is.GreaterThan(1), "Release must be observed during real rising motion. " + State(match, engine, tractor, load));
                Queue(pad, action: true); yield return null; yield return null;
                Queue(pad); yield return null; yield return null;
                Assert.That((bool)Property(engine, "Released"), Is.True, "Only the assigned release ACTION may drop the physical sling. " + State(match, engine, tractor, load));
                Assert.That(((Vector3)Property(engine, "ReleaseWorldVelocity")).y, Is.GreaterThan(0));
                Assert.That(load.GetInstanceID(), Is.EqualTo(cargoId));
                Assert.That(load.isKinematic, Is.False);
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) { Queue(pad); yield return null; }
                Assert.That((bool)Property(match, "Finished"), Is.True, "The real projectile must resolve through ordinary gameplay. " + State(match, engine, tractor, load));
                string completedRound = State(match, engine, tractor, load);

                Call(match, "SetupScreen"); Queue(pad); yield return null; yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((bool)Property(engine, "LoadingBridgesReady"), Is.False);
                Assert.That((bool)Property(engine, "PreparingToSwing"), Is.False);
                Assert.That((bool)Property(engine, "Swinging"), Is.False);
                Assert.That((bool)Property(engine, "Released"), Is.False);
                Assert.That(Property(engine, "Load"), Is.Null);
                for (int i = 0; i < bridges.Length; i++) Assert.That(Vector3.Distance(bridges[i].position, bridgeStarts[i]), Is.LessThan(.05f));
                Assert.That(Read(player, "pad"), Is.SameAs(pad));
                Assert.That(Read(player, "source").ToString(), Is.EqualTo("WholeController"));
                Debug.Log("HH10_TREBUCHET_HUMAN_INPUT_PASS " + completedRound + " rematch=fresh");
            }
            finally
            {
                Time.timeScale = 1;
                if (match is Component component && component != null) Call(match, "SetupScreen");
                InputSystem.RemoveDevice(pad); InputSystem.settings.backgroundBehavior = previousBackground;
            }
        }
    }
}
#endif
