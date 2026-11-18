#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    public sealed class PaddleChaosHazardsPlayModeTests
    {
        static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);

        [UnityTest]
        public IEnumerator AuthoredChaosTimberUsesNativeBuoyancyTethersAndModeSafeReset()
        {
            var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation")
                .Invoke(null, new object[] { "PaddleChaosTimber" });
            float scale = Time.timeScale, fixedStep = Time.fixedDeltaTime; Component match = null;
            try
            {
                Time.timeScale = 1;
                var weather = AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset", Runtime("LWS.TruckSports.SportsWeatherPresets"));
                bool selected = false;
                foreach (var entry in (Array)Field(weather, "entries")) if ((string)Field(entry, "displayName") == "Clear")
                { Call(weather, "Select", (string)Field(entry, "id")); selected = true; break; }
                Assert.That(selected, Is.True);
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/PaddlePals.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.PaddlePalsMatch"), FindObjectsSortMode.None)[0];
                var hazard = match.GetComponent(Runtime("LWS.TruckWaterSports.PaddleChaosHazards"));
                Assert.That(hazard, Is.Not.Null, "Run the explicit saved-scene Chaos installer before this acceptance check.");
                var logs = (Rigidbody[])Field(hazard, "logs"); var saved = new Vector3[logs.Length];
                Assert.That(logs.Length, Is.EqualTo(4));
                for (int i = 0; i < logs.Length; i++) saved[i] = logs[i].position;
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); Call(match, "NormalizeSetup"); Call(match, "AllBots");
                match.GetType().GetField("option").SetValue(match, 2);
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                string firstMatch = (string)Property(match, "MatchId");
                bool wet = false, tension = false; float movement = 0;
                Time.timeScale = 4; float deadline = Time.realtimeSinceStartup + 20;
                while ((float)Property(match, "Clock") < 16 && Time.realtimeSinceStartup < deadline)
                {
                    foreach (var body in logs)
                    {
                        var joint = body.GetComponent<SpringJoint>(); Assert.That(joint, Is.Not.Null); Assert.That(joint.connectedBody, Is.Null);
                        tension |= joint.currentForce.sqrMagnitude > 1;
                        var floating = body.GetComponent(Runtime("LWS.TruckSports.SportsFloatingBody"));
                        wet |= (uint)Property(floating, "WetTriangles") > 0;
                    }
                    for (int i = 0; i < logs.Length; i++) movement = Mathf.Max(movement, Vector3.Distance(logs[i].position, saved[i]));
                    yield return null;
                }
                Assert.That((float)Property(match, "Clock"), Is.GreaterThanOrEqualTo(16));
                Assert.That(wet && tension, Is.True, "Real Archimeds wet hulls and native tether forces are required.");
                Assert.That(movement, Is.GreaterThan(.2f), "The authored current must move actual dynamic timber.");
                int contacts = (int)Property(hazard, "ActualCanoeContacts");

                Time.timeScale = 1; Call(match, "SetupScreen");
                match.GetType().GetField("option").SetValue(match, 0); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                yield return new WaitForFixedUpdate(); yield return null;
                Assert.That(((Transform)Field(hazard, "equipment")).gameObject.activeSelf, Is.False, "Default Sprint must not gain Chaos equipment.");
                foreach (var body in logs) Assert.That(body.isKinematic, Is.True);

                Call(match, "SetupScreen"); match.GetType().GetField("option").SetValue(match, 2);
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return new WaitForFixedUpdate(); yield return null;
                Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(firstMatch));
                Assert.That((int)Property(hazard, "ActualCanoeContacts"), Is.Zero);
                for (int i = 0; i < logs.Length; i++) Assert.That(Vector3.Distance(logs[i].position, saved[i]), Is.LessThan(.5f), "Ordinary rematch restores saved physical equipment.");
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(fixedStep));
                TestContext.WriteLine("PADDLE_CHAOS_TIMBER software_ai_only=true wet_hulls=true native_tether_force=true movement=" + movement + " actual_contact_events=" + contacts + "; no contacts, outcomes or scores were injected; full Chaos match/human acceptance is separate.");
            }
            finally { if (match != null) Call(match, "SetupScreen"); Time.timeScale = scale; lease.Dispose(); }
        }
    }
}
#endif
