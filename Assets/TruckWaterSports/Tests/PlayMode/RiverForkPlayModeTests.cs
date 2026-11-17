#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    public sealed class RiverForkPlayModeTests
    {
        static Type Runtime(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType(name) is Type type) return type;
            throw new InvalidOperationException("Missing runtime type " + name);
        }
        static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);

        [UnityTest]
        public IEnumerator AuthoredKayakSlalomAITraversesPhysicalForkFinishesAndRematches()
        {
            var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "RiverForkNatural" });
            float originalScale = Time.timeScale, fixedStep = Time.fixedDeltaTime;
            Component match = null;
            try
            {
                Time.timeScale = 1;
                var weather = AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset", Runtime("LWS.TruckSports.SportsWeatherPresets"));
                Assert.That(weather, Is.Not.Null);
                bool clear = false;
                foreach (var entry in (Array)Field(weather, "entries"))
                    if ((string)Field(entry, "displayName") == "Clear") { Call(weather, "Select", (string)Field(entry, "id")); clear = true; break; }
                Assert.That(clear, Is.True);
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var matches = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.WhitewaterWipeoutMatch"), FindObjectsSortMode.None);
                Assert.That(matches.Length, Is.EqualTo(1)); match = (Component)matches[0];
                var fork = match.GetComponent(Runtime("LWS.TruckWaterSports.RiverForkChallenge"));
                Assert.That(fork, Is.Not.Null, "Test the installed saved sector; no runtime authoring or replacement course.");
                match.GetType().GetField("option").SetValue(match, 1);
                Field(match, "setup").GetType().GetField("count").SetValue(Field(match, "setup"), 1);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var vessel = Call(match, "Vessel", 0); var body = (Rigidbody)Field(vessel, "body");
                var logs = (Rigidbody[])Field(fork, "logs");
                Assert.That(logs.Length, Is.EqualTo(2));
                var logStarts = new Vector3[logs.Length]; for (int i = 0; i < logs.Length; i++) logStarts[i] = logs[i].position;
                Vector3 center = (Vector3)Field(fork, "center"), forward = (Vector3)Field(fork, "forward"), right = (Vector3)Field(fork, "right");
                float offset = (float)Field(fork, "laneOffset");
                Vector3 previous = body.position; bool sawCrossing = false; float crossingLateral = 0, logTravel = 0;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                string firstId = (string)Property(match, "MatchId");
                Time.timeScale = 3;
                float deadline = Time.realtimeSinceStartup + 100;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    Vector3 now = body.position;
                    float before = Vector3.Dot(previous - center, forward), after = Vector3.Dot(now - center, forward);
                    if (!sawCrossing && before < 0 && after >= 0)
                    {
                        sawCrossing = true;
                        crossingLateral = Vector3.Dot(Vector3.Lerp(previous, now, -before / (after - before)) - center, right);
                    }
                    for (int i = 0; i < logs.Length; i++) logTravel = Mathf.Max(logTravel, Vector3.Distance(logs[i].position, logStarts[i]));
                    previous = now; yield return null;
                }
                Time.timeScale = 1;
                string observation = "clock=" + Property(match, "Clock") + " position=" + body.position + " crossed=" + sawCrossing + " lateral=" + crossingLateral + " logs_moved=" + logTravel;
                Assert.That((bool)Property(match, "Finished"), Is.True, observation);
                Assert.That(sawCrossing, Is.True, "The actual kayak must traverse the fork. " + observation);
                Assert.That(((bool[])Field(fork, "crossed"))[0], Is.True, observation);
                Assert.That(logTravel, Is.GreaterThan(.1f), "Real buoyant logs must respond to current/contacts. " + observation);
                Assert.That(((float[])Field(match, "FinishTimes"))[0], Is.GreaterThanOrEqualTo(0), "A DNF/timer-only outcome does not pass. " + observation);
                Assert.That((bool)Call(match, "ResultWinner", 0), Is.True, observation);
                float penalty = (float)Call(match, "RoutePenaltySeconds", 0);
                Assert.That(penalty, Is.EqualTo(((bool[])Field(fork, "missed"))[0] ? 3 : 0));
                float halfWidth = (float)Field(fork, crossingLateral < 0 ? "shortcutHalfWidth" : "safeHalfWidth");
                float gateError = Mathf.Abs(Mathf.Abs(crossingLateral) - offset);
                // Frame observations can straddle several fixed steps. Only
                // classify a clearance with a decisive margin from the edge.
                if (Mathf.Abs(gateError - halfWidth) > .3f)
                    Assert.That(((bool[])Field(fork, "missed"))[0], Is.EqualTo(gateError > halfWidth), observation);
                double measured = ((float[])Field(match, "FinishTimes"))[0] + (int)Property(vessel, "Capsizes") * 2 + (int)Property(vessel, "Collisions") * 2 + penalty;
                Assert.That((double)Call(match, "ResultScore", 0), Is.EqualTo(measured).Within(.01));

                Call(match, "SetupScreen"); yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(firstId));
                yield return null; yield return null;
                Assert.That(((bool[])Field(fork, "crossed"))[0], Is.False);
                Assert.That(((bool[])Field(fork, "missed"))[0], Is.False);
                Assert.That((float)Call(match, "RoutePenaltySeconds", 0), Is.Zero);
                Assert.That((bool)Property(match, "Live"), Is.True);
                for (int i = 0; i < logs.Length; i++)
                    Assert.That(Vector3.Distance(logs[i].position, logStarts[i]), Is.LessThan(2.5f), "Round reset restores the actual log payloads before new drift.");
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(fixedStep));
                TestContext.WriteLine("RIVER_FORK_NATURAL software_ai_only=true option=1 " + observation + " penalty_seconds=" + penalty + " rematch=fresh");
            }
            finally
            {
                if (match != null) Call(match, "SetupScreen");
                Time.timeScale = originalScale; lease.Dispose();
            }
        }
    }
}
#endif
