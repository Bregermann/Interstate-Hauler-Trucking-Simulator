#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RocketPrecisionLandingPlayModeTests
{
    static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
    static Type Runtime => Type.GetType("LWS.TruckHitchSports.TrailerblazersMatch, Assembly-CSharp", true);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10RocketPrecisionLanding" });
    static int PadWheels(object truck, Collider pad)
    {
        int count = 0;
        foreach (var wheel in (Array)Field(truck, "wheels"))
            if ((bool)Property(wheel, "IsGrounded") && (Collider)Property(wheel, "HitCollider") == pad && Vector3.Dot((Vector3)Property(wheel, "HitNormal"), pad.transform.up) > .8f) count++;
        return count;
    }
    static string State(object match, Rigidbody tractor, object truck, Collider pad)
        => Call(match, "FlightDiagnostic", 0) + " tractorCOM=" + tractor.worldCenterOfMass.ToString("F2") + " velocity=" + tractor.linearVelocity.ToString("F2") + " up=" + Vector3.Dot(tractor.transform.up, Vector3.up).ToString("F2") + " pad=" + pad.bounds + " padWheels=" + PadWheels(truck, pad) + " guidedSeconds=" + Call(match, "GuidedDescentSeconds", 0) + " landingError=" + Call(match, "LandingErrorMetres", 0);

    [UnityTest]
    public IEnumerator NaturalPrecisionLaunchStagesGuidesAndSettlesOnAssignedPhysicalPadThenRematches()
    {
        using (Preferences())
        {
            object match = null;
            float previousScale = Time.timeScale;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/Trailerblazers.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                match.GetType().GetField("option").SetValue(match, 2);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather");
                if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                var truck = ((Array)Field(match, "trucks")).GetValue(0);
                var tractor = (Rigidbody)Field(truck, "body");
                var stage = ((Array)Field(match, "trailers")).GetValue(0);
                var booster = (Rigidbody)Field(stage, "Body"); int boosterId = booster.GetInstanceID();
                var padTransform = (Transform)((Array)Field(match, "landingPads")).GetValue(0);
                var pad = padTransform.GetComponent<Collider>();
                Assert.That(pad, Is.Not.Null, "The assigned saved pad must be an actual load-bearing surface.");
                Assert.That(pad.isTrigger, Is.False);
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Time.timeScale = 3;
                float deadline = Time.realtimeSinceStartup + 75, nextObservation = 0;
                float onPadSince = -1, longestSettledPadSeconds = 0;
                bool physicalAirborne = false, releasedInAir = false;
                string lastLive = State(match, tractor, truck, pad);
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    if ((float)Call(match, "PeakAltitude", 0) > 30 && !(bool)Property(truck, "Grounded")) physicalAirborne = true;
                    if ((int)Call(match, "StageReleaseCount", 0) == 1 && !(bool)Property(truck, "Grounded")) releasedInAir = true;
                    Vector3 local = pad.transform.InverseTransformPoint(tractor.worldCenterOfMass);
                    Vector3 half = pad.GetComponent<BoxCollider>() != null ? pad.GetComponent<BoxCollider>().size * .5f : Vector3.one * .5f;
                    bool inside = Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z;
                    bool settledOnPad = inside && PadWheels(truck, pad) >= 2 && tractor.linearVelocity.magnitude < 2 && Vector3.Dot(tractor.transform.up, Vector3.up) > .65f;
                    if (!settledOnPad) onPadSince = -1;
                    else
                    {
                        if (onPadSince < 0) onPadSince = Time.time;
                        longestSettledPadSeconds = Mathf.Max(longestSettledPadSeconds, Time.time - onPadSince);
                    }
                    if (Time.realtimeSinceStartup >= nextObservation)
                    {
                        nextObservation = Time.realtimeSinceStartup + 8;
                        lastLive = State(match, tractor, truck, pad);
                        Debug.Log("HH10_ROCKET_PRECISION_LIVE " + lastLive);
                    }
                    yield return null;
                }
                string observed = State(match, tractor, truck, pad) + " live=[" + lastLive + "] settledPadSeconds=" + longestSettledPadSeconds;
                Assert.That((bool)Property(match, "Finished"), Is.True, observed);
                Assert.That(physicalAirborne, Is.True, "A natural rocket launch must actually leave native support above 30 m. " + observed);
                Assert.That(releasedInAir, Is.True, "The original attached booster must physically separate during flight. " + observed);
                Assert.That((int)Call(match, "StageReleaseCount", 0), Is.EqualTo(1), observed);
                Assert.That((float)Call(match, "GuidedDescentSeconds", 0), Is.GreaterThan(1), "The precision descent force must run during the actual airborne descent. " + observed);
                Assert.That(longestSettledPadSeconds, Is.GreaterThan(.5f), "A quiet apron landing or watchdog result cannot substitute for native tyre support on the assigned pad. " + observed);
                Assert.That((bool)Call(match, "LandedControlled", 0), Is.True, observed);
                Assert.That((bool)Call(match, "ResultWinner", 0), Is.True, observed);
                Assert.That(booster.GetInstanceID(), Is.EqualTo(boosterId));
                Assert.That(((Component)stage).gameObject.activeSelf, Is.True, "The physically separated stage must remain in the venue.");
                float error = (float)Call(match, "LandingErrorMetres", 0);
                Assert.That(error, Is.EqualTo(Vector3.ProjectOnPlane(tractor.position - padTransform.position, Vector3.up).magnitude).Within(.2f), "Reported precision must come from the actual resting position. " + observed);
                Assert.That(((double[])Field(match, "Scores"))[0], Is.GreaterThan(100), "Precision must contribute measured pad accuracy beyond the generic controlled-landing bonus. " + observed);
                Call(match, "SetupScreen"); yield return null; yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((int)Call(match, "StageReleaseCount", 0), Is.Zero);
                Assert.That((float)Call(match, "GuidedDescentSeconds", 0), Is.Zero);
                Assert.That((float)Call(match, "LandingErrorMetres", 0), Is.Zero);
                Assert.That((bool)Call(match, "LandedControlled", 0), Is.False);
                Assert.That(((double[])Field(match, "Scores"))[0], Is.Zero);
                Assert.That(booster.GetInstanceID(), Is.EqualTo(boosterId));
                Debug.Log("HH10_ROCKET_PRECISION_RESULT " + observed + " rematch=fresh");
            }
            finally
            {
                Time.timeScale = previousScale;
                if (match is UnityEngine.Object owner && owner != null) Call(match, "SetupScreen");
            }
        }
    }
}
#endif
