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

        static string LiveObservation(Component match, object vessel, Rigidbody body, RiverForkContactObservation contacts)
        {
            int gate = ((int[])Field(match, "Gates"))[0];
            var course = Field(match, "course");
            Vector3 target = (Vector3)Call(course, "Point", gate);
            Vector3 prior = (Vector3)Call(course, "Point", gate - 1);
            Vector3 downstream = Vector3.ProjectOnPlane(target - prior, Vector3.up).normalized;
            Vector3 delta = body.position - target;
            return "clock=" + Property(match, "Clock") + " gate=" + gate + " misses=" + ((int[])Field(match, "MissedGates"))[0] +
                " capsizes=" + Property(vessel, "Capsizes") + " capsized=" + Property(vessel, "Capsized") +
                " position=" + body.position.ToString("F3") + " velocity=" + body.linearVelocity.ToString("F3") +
                " angular=" + body.angularVelocity.ToString("F3") + " target=" + target.ToString("F3") +
                " gateDistance=" + delta.magnitude.ToString("F3") + " downstream=" + Vector3.Dot(delta, downstream).ToString("F3") +
                " nearestSegment=" + Call(course, "Nearest", body.position) + " strokes=" + Property(vessel, "Strokes") +
                " waterSupported=" + Property(vessel, "WaterSupported") + " contacts=" + contacts.Describe();
        }

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
                var contacts = body.gameObject.AddComponent<RiverForkContactObservation>();
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
                float nextObservation = 0; int lastGate = -1; string lastLive = "not sampled";
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    int actualGate = ((int[])Field(match, "Gates"))[0];
                    float clock = (float)Property(match, "Clock");
                    if (clock >= nextObservation || actualGate != lastGate)
                    {
                        lastLive = LiveObservation(match, vessel, body, contacts);
                        Debug.Log("RIVER_FORK_ACTUAL_SAMPLE " + lastLive);
                        nextObservation = clock + 15; lastGate = actualGate;
                    }
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
                string observation = "clock=" + Property(match, "Clock") + " position=" + body.position + " crossed=" + sawCrossing + " lateral=" + crossingLateral + " logs_moved=" + logTravel + " final={" + LiveObservation(match, vessel, body, contacts) + "} last_live={" + lastLive + "}";
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

    // Observation only: real PhysX callbacks, no contact/force changes.
    public sealed class RiverForkContactObservation : MonoBehaviour
    {
        string other = "none"; float seenAt = -10, separation, impact;
        void OnCollisionStay(Collision collision)
        {
            if (collision.contactCount == 0) return;
            var point = collision.GetContact(0);
            var collider = collision.collider;
            other = collider.name + "/" + (collider.transform.parent != null ? collider.transform.parent.name : "root");
            seenAt = Time.fixedTime; separation = point.separation; impact = collision.relativeVelocity.magnitude;
        }
        public string Describe() => Time.fixedTime - seenAt < .2f ? other + " separation=" + separation.ToString("F4") + " relativeSpeed=" + impact.ToString("F3") : "none_recent";
    }
}
#endif
