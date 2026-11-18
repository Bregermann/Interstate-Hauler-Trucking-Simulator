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
    public sealed class RingMovingAperturePlayModeTests
    {
        static Type Runtime => Type.GetType("LWS.TruckExtremeHauling.HitchRingTossMatch, Assembly-CSharp", true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
        static bool Swept(float priorRingX, float currentRingX, Vector3 a, Vector3 b, float radius, float bodyRadius)
        {
            object[] args = { Matrix4x4.TRS(Vector3.right * priorRingX, Quaternion.identity, Vector3.one).inverse, Matrix4x4.TRS(Vector3.right * currentRingX, Quaternion.identity, Vector3.one).inverse, a, b, radius, bodyRadius, 0f, 0f };
            return (bool)Runtime.GetMethod("TryMovingRingCenterCrossing").Invoke(null, args);
        }

        [Test]
        public void MovingApertureUsesTheActualCrossingFrameAndRejectsAnEarlierClosedOpening()
        {
            Assert.That(Swept(-2, 2, Vector3.back, Vector3.forward, 1, .2f), Is.True, "The real opening is centered over the body halfway through this opposing motion.");
            Assert.That(Swept(-4, 0, Vector3.back, Vector3.forward * 3, 1, .2f), Is.False, "At the actual plane-crossing quarter-step, the moving opening is still three metres sideways. Its later centered pose cannot fabricate a passage.");
            Assert.That(Swept(-2, 2, Vector3.back, Vector3.forward, 1, 1.1f), Is.False, "The complete projected load must fit; a center crossing alone is insufficient.");
            Assert.That(Swept(-2, 2, Vector3.back, Vector3.back * .1f, 1, .2f), Is.False, "An aperture moving beside a load cannot score without a real plane crossing.");
        }

        [UnityTest]
        public IEnumerator NaturalMovingRingRoundClearsPhysicalMovingFrameAndRematchesWithOriginalNativeLoad()
        {
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10RingMovingAperture" }))
            {
                object match = null; float originalScale = Time.timeScale;
                try
                {
                    EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity", new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null; yield return null;
                    match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                    match.GetType().GetField("option").SetValue(match, 2);
                    var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                    Call(match, "NormalizeSetup"); Call(match, "AllBots");
                    var weather = Field(match, "presentationWeather");
                    if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                    var load = ((Array)Field(match, "trailers")).GetValue(0); var body = (Rigidbody)Field(load, "Body"); int bodyId = body.GetInstanceID();
                    var rings = (Array)Field(match, "rings"); var ring = (Component)rings.GetValue(0);
                    Vector3[] origins = rings.Cast<Component>().Select(value => value.transform.position).ToArray();
                    Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    var ringBody = ring.GetComponent<Rigidbody>();
                    Assert.That(ringBody, Is.Not.Null); Assert.That(ringBody.isKinematic, Is.True);
                    var frames = ring.GetComponentsInChildren<Collider>(true).Where(shape => shape.enabled && !shape.isTrigger).ToArray();
                    Assert.That(frames.Length, Is.GreaterThanOrEqualTo(12), "The moving aperture must carry its actual solid frame, not just a score marker.");
                    Assert.That(frames.All(shape => shape.attachedRigidbody == ringBody), Is.True);
                    Time.timeScale = 3;
                    float deadline = Time.realtimeSinceStartup + 75, nextObservation = 0;
                    float minRingX = ringBody.position.x, maxRingX = minRingX; bool releasedObserved = false;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                    {
                        minRingX = Mathf.Min(minRingX, ringBody.position.x); maxRingX = Mathf.Max(maxRingX, ringBody.position.x);
                        releasedObserved |= (bool)Call(match, "TrailerReleased", 0);
                        if (Time.realtimeSinceStartup >= nextObservation)
                        {
                            nextObservation = Time.realtimeSinceStartup + 6;
                            Debug.Log("HH10_RING_MOVING_LIVE " + Call(match, "MovingRingDiagnostic", 0));
                        }
                        yield return null;
                    }
                    string state = Call(match, "MovingRingDiagnostic", 0) + " actualRingTravel=" + (maxRingX - minRingX).ToString("F2");
                    Assert.That((bool)Property(match, "Finished"), Is.True, state);
                    Assert.That(maxRingX - minRingX, Is.GreaterThan(5), "Actual kinematic frame motion must be observed. " + state);
                    Assert.That(releasedObserved, Is.True, "AI must physically release its native trailer. " + state);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.GreaterThan(0), "A natural whole-load moving-aperture passage is required; timer-only misses cannot pass. " + state);
                    Assert.That((float)Call(match, "MaximumMovingPassageSpeed", 0), Is.GreaterThan(.1f), "At least one qualifying full passage must finish while its physical aperture is actually moving. " + state);
                    Assert.That((int)Call(match, "AttemptsResolved", 0), Is.EqualTo((int)Field(match, "throwsPerPlayer")), state);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0) + (int)Call(match, "Misses", 0), Is.EqualTo((int)Field(match, "throwsPerPlayer")), "Each real throw resolves exactly once. " + state);
                    Assert.That((bool)Call(match, "ResultWinner", 0), Is.True, state);
                    Assert.That(body.GetInstanceID(), Is.EqualTo(bodyId));
                    string matchId = (string)Property(match, "MatchId");
                    Call(match, "SetupScreen"); yield return null; yield return null;
                    Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(matchId));
                    Assert.That((int)Call(match, "AttemptsResolved", 0), Is.Zero);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.Zero);
                    Assert.That((double)Call(match, "ResultScore", 0), Is.Zero);
                    Assert.That((float)Call(match, "MaximumMovingPassageSpeed", 0), Is.Zero);
                    for (int index = 0; index < rings.Length; index++) Assert.That(Vector3.Distance(((Component)rings.GetValue(index)).transform.position, origins[index]), Is.LessThan(.01f), "Rematch restores the original target, not a late moving pose.");
                    Assert.That(body.GetInstanceID(), Is.EqualTo(bodyId));
                    Debug.Log("HH10_RING_MOVING_RESULT " + state + " rematch=fresh");
                }
                finally
                {
                    Time.timeScale = originalScale;
                    if (match is UnityEngine.Object owner && owner != null) Call(match, "SetupScreen");
                }
            }
        }
    }
}
#endif
