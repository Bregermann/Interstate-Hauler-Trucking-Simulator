#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class SkyhookSlalomPlayModeTests
    {
        static Type Runtime => Type.GetType("LWS.TruckExtremeHauling.SkyhookSprintMatch, Assembly-CSharp", true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
        [UnityTest]
        public IEnumerator NaturalSuspendedSlalomClearsFourWholeLoadGatesAndPhysicallyLandsSameCargoThenRematches()
        {
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10SkyhookSlalom" }))
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/SkyhookSprint.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                match.GetType().GetField("option").SetValue(match, 2);
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather");
                if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                var gates = (Transform[])Field(match, "slalomGates"); int count = (int)Field(match, "slalomGatesPerLane");
                Assert.That(count, Is.EqualTo(4)); Assert.That(gates.Length, Is.GreaterThanOrEqualTo(count));
                var car = ((Array)Field(match, "cableCars")).GetValue(0); var cargo = (Rigidbody)Field(car, "load"); int cargoId = cargo.GetInstanceID();
                var cable = (SpringJoint)Field(car, "cable"); var trailer = ((Array)Field(match, "trailers")).GetValue(0);
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                foreach (var gate in gates.Take(count))
                    Assert.That(gate.gameObject.activeInHierarchy && gate.GetComponentsInChildren<Collider>().Count(c => c.enabled && !c.isTrigger) == 4, Is.True, "Each opening must have four real physical frame edges.");
                float minimumX = cargo.worldCenterOfMass.x, maximumX = minimumX, peakTension = 0; int seenPassed = 0; bool cableAttachedSeen = false, releasedAfterGates = false, supportedLandingSeen = false;
                var goal = ((Transform[])Field(match, "objectives"))[0];
                try
                {
                    Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 75, nextObservation = Time.realtimeSinceStartup;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                    {
                        int passed = (int)Call(match, "SlalomGatesPassed", 0);
                        Assert.That(passed, Is.GreaterThanOrEqualTo(seenPassed).And.LessThanOrEqualTo(seenPassed + 1), "Normal physics must resolve each ordered opening individually.");
                        seenPassed = passed;
                        minimumX = Mathf.Min(minimumX, cargo.worldCenterOfMass.x); maximumX = Mathf.Max(maximumX, cargo.worldCenterOfMass.x);
                        if (cable.connectedBody == (Rigidbody)Field(car, "trolley")) { cableAttachedSeen = true; peakTension = Mathf.Max(peakTension, cable.currentForce.magnitude); }
                        if ((bool)Property(car, "Released"))
                        {
                            Assert.That(passed, Is.EqualTo(count), "Natural AI must keep its original physical cable until the full load clears every gate."); releasedAfterGates = true;
                            supportedLandingSeen |= (bool)Property(trailer, "WheelContact") && Vector3.Distance(cargo.worldCenterOfMass, goal.position) < 12 && cargo.linearVelocity.magnitude < 1 && Vector3.Dot(cargo.rotation * Vector3.up, Vector3.up) > .7f;
                        }
                        if (Time.realtimeSinceStartup >= nextObservation) { nextObservation = Time.realtimeSinceStartup + 6; Debug.Log("HH10_SKYHOOK_SLALOM_ACTUAL " + Call(match, "SlalomDiagnostic", 0)); }
                        yield return null;
                    }
                    string state = (string)Call(match, "SlalomDiagnostic", 0);
                    Assert.That((bool)Property(match, "Finished"), Is.True, state);
                    Assert.That((int)Call(match, "SlalomGatesPassed", 0), Is.EqualTo(4), "A natural full-gate traversal is required. " + state);
                    Assert.That((int)Call(match, "SlalomGatesMissed", 0), Is.Zero, state);
                    Assert.That(cableAttachedSeen && peakTension > 100, Is.True, "The original load must really hang from its tensioned physical cable. " + state);
                    Assert.That(maximumX - minimumX, Is.GreaterThan(.5f), "Alternating gates require actual lateral load movement. " + state);
                    Assert.That(releasedAfterGates || (bool)Property(car, "Released"), Is.True, state);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.EqualTo(1), "One real settled final delivery scores once. " + state);
                    Assert.That(supportedLandingSeen, Is.True, "Before result teardown the upright final cargo must have actual native wheel support at its destination. " + state);
                    Assert.That(cargo.linearVelocity.magnitude, Is.LessThan(1)); Assert.That(Vector3.Dot(cargo.rotation * Vector3.up, Vector3.up), Is.GreaterThan(.7f));
                    Assert.That(cargo.GetInstanceID(), Is.EqualTo(cargoId)); Assert.That((bool)Call(match, "ResultWinner", 0), Is.True);
                    Call(match, "SetupScreen"); yield return null; Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That((int)Call(match, "SlalomGatesPassed", 0), Is.Zero); Assert.That((int)Call(match, "SlalomGatesMissed", 0), Is.Zero);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.Zero); Assert.That((bool)Property(car, "Released"), Is.False);
                    yield return new WaitForSeconds(3.5f); Assert.That((bool)Property(match, "Running"), Is.True);
                    Assert.That(cable.connectedBody, Is.SameAs(Field(car, "trolley"))); Call(match, "SetupScreen");
                }
                finally { Time.timeScale = 1; }
            }
        }
    }
}
#endif
