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
    public sealed class WhitewaterSurvivalRoutePlayModeTests
    {
        static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
        static float Clock(object match) => (float)Property(match, "Clock");

        [UnityTest]
        public IEnumerator NaturalLowerSurvivalForkPassesExistingCurrentThenResetsWithoutChangingOtherModes()
        {
            var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation")
                .Invoke(null, new object[] { "WhitewaterLowerFork" });
            float scale = Time.timeScale, fixedStep = Time.fixedDeltaTime; Component match = null;
            try
            {
                Time.timeScale = 1;
                var weather = AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset", Runtime("LWS.TruckSports.SportsWeatherPresets"));
                bool selected = false;
                foreach (var entry in (Array)Field(weather, "entries")) if ((string)Field(entry, "displayName") == "Clear")
                { Call(weather, "Select", (string)Field(entry, "id")); selected = true; break; }
                Assert.That(selected, Is.True);
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.WhitewaterWipeoutMatch"), FindObjectsSortMode.None)[0];
                var fork = Field(match, "survivalFork"); Assert.That(fork, Is.Not.Null, "Install and save the explicit lower survival route first.");
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1); Call(match, "NormalizeSetup"); Call(match, "AllBots");
                match.GetType().GetField("option").SetValue(match, 3);
                float authoredDuration = (float)Field(match, "matchDuration");
                Assert.That((bool)Call(match, "StartMatch"), Is.True); string first = (string)Property(match, "MatchId");
                var vessels = (Array)Field(match, "vessels"); var vessel = vessels.GetValue(0); var body = (Rigidbody)Field(vessel, "body");
                var center = (Vector3)Field(fork, "center"); var right = (Vector3)Field(fork, "right");
                var course = Field(match, "course"); var field = Field(course, "whirlpoolField");
                Assert.That((bool)Property(field, "Active"), Is.True);
                Vector3 narrowPoint = center + right * (float)Field(fork, "narrowOffset");
                Assert.That(((Vector3)Call(field, "SampleCurrent", narrowPoint, Clock(match))).magnitude, Is.GreaterThan(.05f), "The dangerous notch must use the existing real whirlpool field.");
                bool wet = false, reached = false; int passageCount = 0;
                Time.timeScale = 4; float deadline = Time.realtimeSinceStartup + 100;
                while (!(bool)Property(match, "Finished") && Clock(match) <= authoredDuration + 5 && Time.realtimeSinceStartup < deadline)
                {
                    wet |= (uint)Property(Field(vessel, "flotation"), "WetTriangles") > 0;
                    reached |= Vector3.ProjectOnPlane(body.position - center, Vector3.up).magnitude < 30;
                    passageCount = ((int[])Field(fork, "NarrowPassages"))[0] + ((int[])Field(fork, "OpenChannelPassages"))[0];
                    yield return null;
                }
                Time.timeScale = 1;
                Assert.That((bool)Property(match, "Finished"), Is.True, "The unchanged natural survival round must resolve.");
                Assert.That((float)Field(match, "matchDuration"), Is.EqualTo(authoredDuration));
                Assert.That(wet && reached, Is.True, "AI must actually descend the existing river to the new reach.");
                Assert.That(passageCount, Is.GreaterThan(0), "Actual hull passage, not a waypoint/score injection, must traverse a new branch.");
                Assert.That((bool)Call(match, "ResultHasOutcome", 0), Is.True);
                Assert.That(Property(Field(match, "menus"), "Screen").ToString(), Is.EqualTo("Result"));
                TestContext.WriteLine("WHITEWATER_SURVIVAL_FORK software_ai_only=true actual_passages=" + passageCount +
                    " clock=" + Clock(match) + " final_position=" + body.position + "; original full duration/current/buoyancy/capsize rules retained; no poses/gates/scores/outcomes injected.");

                Call(match, "SetupScreen"); yield return null; Assert.That((bool)Call(match, "StartMatch"), Is.True);
                yield return new WaitForFixedUpdate(); yield return null;
                Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(first));
                Assert.That((int[])Field(fork, "NarrowPassages"), Is.All.EqualTo(0));
                Assert.That((int[])Field(fork, "OpenChannelPassages"), Is.All.EqualTo(0));
                Time.timeScale = 4; yield return new WaitForSeconds(4); Time.timeScale = 1;
                Assert.That(Clock(match), Is.GreaterThan(3));
                Call(match, "SetupScreen"); match.GetType().GetField("option").SetValue(match, 0);
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return new WaitForFixedUpdate(); yield return null;
                Assert.That(((Transform)Field(fork, "equipment")).gameObject.activeSelf, Is.False, "Race must retain its original open geometry.");
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(fixedStep));
            }
            finally { if (match != null) Call(match, "SetupScreen"); Time.timeScale = scale; lease.Dispose(); }
        }
    }
}
#endif
