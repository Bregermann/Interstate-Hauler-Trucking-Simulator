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

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class DominoCreativePlacementPlayModeTests
    {
        static object Read(object instance, string field) => instance.GetType().GetField(field).GetValue(instance);
        static object Call(object instance, string method, params object[] values)
            => instance.GetType().GetMethod(method).Invoke(instance, values);

        [UnityTest]
        public IEnumerator AssignedPadMovesAnUprightTrailerBeyondTheOriginalLaneAndOntoTheWorkshopRamp()
        {
            var matchType = Type.GetType("LWS.TruckExtremeHauling.DominoDerbyMatch, Assembly-CSharp", true);
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            using var isolated = (IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,
                new object[] { "Alpha10DominoWorkshop" });
            var background = InputSystem.settings.backgroundBehavior;
            var pad = InputSystem.AddDevice<Gamepad>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            UnityEngine.Object match = null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/DominoDerby.unity",
                    new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                yield return null;
                match = UnityEngine.Object.FindObjectsByType(matchType, FindObjectsSortMode.None).Single();
                var setup = Read(match, "setup");
                setup.GetType().GetField("count").SetValue(setup, 1);
                matchType.GetField("option").SetValue(match, 3);
                var player = ((Array)Read(setup, "players")).GetValue(0);
                var source = player.GetType().GetField("source");
                source.SetValue(player, Enum.Parse(source.FieldType, "WholeController"));
                player.GetType().GetField("pad").SetValue(player, pad);
                Call(match, "NormalizeSetup");
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null;
                yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Time.timeScale = 2;

                var workshop = (GameObject)Read(match, "creativeWorkshop");
                Assert.That(workshop != null && workshop.activeInHierarchy, Is.True);
                var trailer = ((Array)Read(match, "trailers")).GetValue(0);
                var body = (Rigidbody)Read(trailer, "Body");
                var origin = body.position;
                var target = origin + new Vector3(14, 0, 21);
                bool enteredBranch = false;
                float deadline = Time.realtimeSinceStartup + 18;
                while (Time.realtimeSinceStartup < deadline)
                {
                    Vector3 distance = target - body.position;
                    if (Mathf.Abs(distance.x) < .18f && Mathf.Abs(distance.z) < .18f) break;
                    // Pull out of the tight original line sideways before
                    // travelling toward the ramp between the standing trailers.
                    enteredBranch |= Mathf.Abs(distance.x) < .25f;
                    float drive = enteredBranch ? Mathf.Clamp(distance.z, -1, 1) : 0;
                    InputSystem.QueueStateEvent(pad, new GamepadState
                    {
                        leftStick = new Vector2(Mathf.Clamp(distance.x, -1, 1), 0),
                        rightTrigger = Mathf.Max(0, drive),
                        leftTrigger = Mathf.Max(0, -drive)
                    });
                    yield return null;
                }
                InputSystem.QueueStateEvent(pad, new GamepadState());
                Assert.That(body.position.x - origin.x, Is.GreaterThan(12), "Creative controls must reach the branch lane.");
                Assert.That(body.position.z - origin.z, Is.GreaterThan(18), "Creative controls must reach the ramp.");
                Assert.That(body.position.y - origin.y, Is.GreaterThan(.35f), "The trailer must stand on real sloped support.");
                Assert.That(Vector3.Dot(body.rotation * Vector3.forward, Vector3.up), Is.GreaterThan(.98f));
                Assert.That(body.isKinematic, Is.True, "Setup still locks upright pieces before the horn.");
                Assert.That((double)Call(match, "ResultScore", 0), Is.Zero, "Placement must never score.");

                Call(match, "SetupScreen");
                matchType.GetField("option").SetValue(match, 0);
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That(workshop.activeSelf, Is.False);
                Assert.That(Vector3.Distance(body.position, origin), Is.LessThan(.15f), "The next mode restores the authored lane.");
            }
            finally
            {
                if (match != null) Call(match, "SetupScreen");
                InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = background;
                Time.timeScale = 1;
            }
        }
    }
}
#endif
