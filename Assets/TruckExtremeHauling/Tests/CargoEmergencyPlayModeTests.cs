#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class CargoEmergencyPlayModeTests
    {
        static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] arguments) => target.GetType().GetMethod(name).Invoke(target, arguments);

        [UnityTest]
        public IEnumerator NaturalIndustrialFireRecoveryPreservesCargoAndRematches() => RunIncident(4);
        [UnityTest]
        public IEnumerator NaturalFloodedLoadingRecoveryUsesBuoyancyCableRampAndNativeHitch() => RunIncident(5);

        static IEnumerator RunIncident(int option)
        {
            var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CargoEmergencyActual" + option });
            float oldScale = Time.timeScale, fixedStep = Time.fixedDeltaTime;
            Component match = null;
            try
            {
                Time.timeScale = 1;
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var instances = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckExtremeHauling.CargoCrisisMatch"), FindObjectsSortMode.None);
                Assert.That(instances.Length, Is.EqualTo(1)); match = (Component)instances[0];
                var sites = Field(match, "emergencySites"); Assert.That(sites, Is.Not.Null, "The saved scene must contain the authored emergency incidents.");
                var originalLoads = (Array)Field(match, "trailers"); var originalLoad = originalLoads.GetValue(0);
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                match.GetType().GetField("option").SetValue(match, option); Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather"); if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                string firstId = (string)Property(match, "MatchId");
                var selected = Property(sites, "Active"); Assert.That(selected, Is.Not.Null);
                var load = ((Array)Field(match, "trailers")).GetValue(0); Assert.That(load, Is.Not.SameAs(originalLoad));
                var body = (Rigidbody)Field(load, "Body"); Vector3 spawn = body.position;
                var support = (GameObject)Field(sites, "defaultSupport"); Assert.That(support.activeSelf, Is.EqualTo(option != 5));
                bool cableSeen = false; float peakCableForce = 0, nextObserve = 0;
                Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 90;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    var cable = ((SpringJoint[])Field(match, "winches"))[0];
                    if (cable != null && cable.connectedBody == body) { cableSeen = true; peakCableForce = Mathf.Max(peakCableForce, cable.currentForce.magnitude); }
                    float clock = (float)Field(match, "age");
                    if (clock >= nextObserve)
                    {
                        nextObserve = clock + 50;
                        var truck = ((Array)Field(match, "trucks")).GetValue(0); var tractor = (Rigidbody)Field(truck, "body");
                        Debug.Log("CARGO_EMERGENCY_ACTUAL option=" + option + " clock=" + clock + " stage=" + Call(match, "RecoveryPhase", 0)
                            + " cargo=" + body.position + " velocity=" + body.linearVelocity + " tractor=" + tractor.position
                            + " condition=" + Property(load, "CargoCondition") + " heat=" + ((float[])Field(sites, "HeatSeconds"))[0]
                            + " wet=" + ((float[])Field(sites, "ImmersedSeconds"))[0] + " towed=" + ((float[])Field(sites, "CableTowMetres"))[0]
                            + " extracted=" + ((bool[])Field(sites, "Extracted"))[0] + " cablePeak=" + peakCableForce + " " + Call(sites,"DiagnosticState",0));
                    }
                    yield return null;
                }
                Time.timeScale = 1;
                string outcome = "option=" + option + " stage=" + Call(match, "RecoveryPhase", 0) + " cargo=" + body.position
                    + " condition=" + Property(load, "CargoCondition") + " heat=" + ((float[])Field(sites, "HeatSeconds"))[0]
                    + " wet=" + ((float[])Field(sites, "ImmersedSeconds"))[0] + " towed=" + ((float[])Field(sites, "CableTowMetres"))[0] + " " + Call(sites,"DiagnosticState",0);
                Assert.That((bool)Property(match, "Finished"), Is.True, outcome);
                Assert.That(cableSeen && peakCableForce > 100, Is.True, "Real winch tension must recover the authored load. " + outcome);
                Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.EqualTo(1), "Only a native-hitched supported delivery qualifies. " + outcome);
                var truckComponent = (Component)((Array)Field(match, "trucks")).GetValue(0);
                var hitch = truckComponent.GetComponent(Runtime("LWS.TruckHitchSports.HitchCoupler"));
                Assert.That((int)Property(hitch, "SuccessfulHitches"), Is.GreaterThan(0), outcome);
                if (option == 4)
                {
                    Assert.That(((float[])Field(sites, "HeatSeconds"))[0], Is.GreaterThan(0), outcome);
                    Assert.That((float)Property(load, "CargoCondition"), Is.GreaterThan(0).And.LessThan(1), outcome);
                }
                else
                {
                    Assert.That(((float[])Field(sites, "ImmersedSeconds"))[0], Is.GreaterThan(0), outcome);
                    Assert.That(((float[])Field(sites, "CableTowMetres"))[0], Is.GreaterThanOrEqualTo(5), outcome);
                    Assert.That(((bool[])Field(sites, "Extracted"))[0], Is.True, outcome);
                    Assert.That((bool)Property(load, "WheelContact"), Is.True, outcome);
                }
                Call(match, "SetupScreen"); yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(firstId));
                Assert.That(((float[])Field(sites, "HeatSeconds"))[0], Is.Zero);
                Assert.That(((float[])Field(sites, "ImmersedSeconds"))[0], Is.Zero);
                Assert.That(((float[])Field(sites, "CableTowMetres"))[0], Is.Zero);
                Assert.That(((bool[])Field(sites, "Extracted"))[0], Is.False);
                Assert.That(Vector3.Distance(body.position, spawn), Is.LessThan(.02f), "Only round-boundary reset restores the original site.");
                Time.timeScale = 3; yield return new WaitForSeconds(4); Time.timeScale = 1;
                Assert.That((bool)Property(match, "Running"), Is.True);
                Call(match, "SetupScreen"); yield return null;
                match.GetType().GetField("option").SetValue(match, 0);
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That(Property(sites, "Active"), Is.Null);
                Assert.That(((Array)Field(match, "trailers")).GetValue(0), Is.SameAs(originalLoad));
                Assert.That(support.activeSelf, Is.True);
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(fixedStep));
                TestContext.WriteLine("CARGO_EMERGENCY_NATURAL software_ai_only=true " + outcome + " rematch=fresh baseline_selection=restored");
            }
            finally
            {
                if (match != null) Call(match, "SetupScreen"); Time.timeScale = oldScale; lease.Dispose();
            }
        }
    }
}
#endif
