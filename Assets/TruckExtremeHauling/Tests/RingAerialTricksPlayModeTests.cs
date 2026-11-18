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
    public sealed class RingAerialTricksPlayModeTests
    {
        static Type Runtime => Type.GetType("LWS.TruckExtremeHauling.HitchRingTossMatch, Assembly-CSharp", true);
        static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
        [UnityTest]
        public IEnumerator NaturalTrickRoundScoresActualRingPassagesAndRejectsSupportedRotationAcrossRematch()
        {
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10RingAerialTricks" }))
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                match.GetType().GetField("option").SetValue(match, 3);
                Field(match, "setup").GetType().GetField("count").SetValue(Field(match, "setup"), 1);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather");
                if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                var trailer = ((Array)Field(match, "trailers")).GetValue(0); var body = (Rigidbody)Field(trailer, "Body"); int bodyId = body.GetInstanceID();
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                int supportedSamples = 0; bool priorGrounded = false; int priorTurns = 0, priorAttempt = -1; float priorAir = 0;
                try
                {
                    Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 75; float nextObservation = Time.realtimeSinceStartup;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                    {
                        var meter = ((Array)FieldNonPublic(match, "trickMeters")).GetValue(0);
                        bool released = (bool)Call(match, "TrailerReleased", 0);
                        int attempt = (int)Call(match, "AttemptsResolved", 0);
                        bool grounded = released && !(bool)Property(meter, "FreeFlight");
                        int turns = (int)Property(meter, "CompletedTurns"); float air = (float)Property(meter, "FreeFlightSeconds");
                        if (grounded && priorGrounded && attempt == priorAttempt)
                        {
                            supportedSamples++;
                            Assert.That(turns, Is.EqualTo(priorTurns), "Supported roof/chassis/wheel rotation must never accumulate a new aerial trick. " + Call(match, "TrickDiagnostic", 0));
                            Assert.That(air, Is.EqualTo(priorAir).Within(.0001f), "Ground sliding cannot be timed as free flight. " + Call(match, "TrickDiagnostic", 0));
                        }
                        if (Time.realtimeSinceStartup >= nextObservation) { nextObservation = Time.realtimeSinceStartup + 6; Debug.Log("HH10_RING_TRICK_PHYSICS " + Call(match, "TrickDiagnostic", 0) + " cargo=" + body.position.ToString("F2")); }
                        priorGrounded = grounded; priorTurns = turns; priorAir = air; priorAttempt = attempt;
                        yield return null;
                    }
                    string diagnostic = (string)Call(match, "TrickDiagnostic", 0);
                    int passed = (int)Call(match, "ObjectivesCompleted", 0), misses = (int)Call(match, "Misses", 0), qualifiedTurns = (int)Call(match, "PassedAerialTrickTurns", 0);
                    Assert.That((bool)Property(match, "Finished"), Is.True, diagnostic);
                    Assert.That((int)Call(match, "AttemptsResolved", 0), Is.EqualTo((int)Field(match, "throwsPerPlayer")), diagnostic);
                    Assert.That(passed, Is.GreaterThan(0), "A natural full aperture passage is required; timer-only misses are insufficient. " + diagnostic);
                    Assert.That(passed + misses, Is.EqualTo((int)Field(match, "throwsPerPlayer")), "One outcome per real physical throw.");
                    Assert.That(body.GetInstanceID(), Is.EqualTo(bodyId), "All attempts retain the original native physical trailer.");
                    Assert.That(supportedSamples, Is.GreaterThan(0), "This negative check requires actual observed supported post-release physics, not an unsampled claim.");
                    if (qualifiedTurns == 0) Assert.That((double)Call(match, "ResultScore", 0), Is.EqualTo(passed * 1000 - misses * 100).Within(.001), "A passage without a measured aerial turn earns no invented trick or flight bonus.");
                    Assert.That((bool)Call(match, "ResultWinner", 0), Is.True);
                    Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That((int)Call(match, "AerialTrickTurns", 0), Is.Zero); Assert.That((int)Call(match, "PassedAerialTrickTurns", 0), Is.Zero);
                    Assert.That((int)Call(match, "AttemptsResolved", 0), Is.Zero); Assert.That((double)Call(match, "ResultScore", 0), Is.Zero);
                    yield return new WaitForSeconds(3.6f);
                    Assert.That((bool)Property(match, "Running"), Is.True); Call(match, "SetupScreen");
                }
                finally { Time.timeScale = 1; }
            }
        }
        static object FieldNonPublic(object target, string name) => target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(target);
    }
}
#endif
