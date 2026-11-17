#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class HitchCorePhysicsPlayModeTests
{
    static Type TypeOf(string name) => Type.GetType("LWS.TruckHitchSports." + name + ", Assembly-CSharp", true);
    static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
    static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10HitchPhysics" });

    [UnityTest]
    public IEnumerator NativeHitchLocksThenReleasesActualMomentumAcrossRematches()
    {
        using (Preferences())
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            object match = UnityEngine.Object.FindObjectsByType(TypeOf("HighwireMatch"), FindObjectsSortMode.None).Single();
            Call(match, "AllBots");
            for (int round = 0; round < 2; round++)
            {
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                var couplers = (Array)Field(match, "couplers"); var trailers = (Array)Field(match, "trailers");
                yield return new WaitForSeconds(.8f);
                for (int i = 0; i < 4; i++)
                {
                    var coupler = couplers.GetValue(i); var trailer = trailers.GetValue(i);
                    Assert.That(Property(coupler, "Attached"), Is.SameAs(trailer), "Saved native kingpins must physically latch during countdown. round=" + round + " slot=" + i + " " + Call(coupler, "DiagnosticState", trailer) + " matchAge=" + Property(match, "Elapsed"));
                    var anchor = (Transform)Property(coupler, "Anchor"); var pin = (Transform)Property(trailer, "Kingpin");
                    Assert.That(Vector3.Distance(anchor.position, pin.position), Is.LessThan(.3f));
                    var body = (Rigidbody)Field(trailer, "Body"); var before = body.linearVelocity; var spin = body.angularVelocity;
                    Assert.That(Call(coupler, "Release"), Is.SameAs(trailer));
                    Assert.That(body.linearVelocity, Is.EqualTo(before)); Assert.That(body.angularVelocity, Is.EqualTo(spin));
                    Assert.That(Property(coupler, "Attached"), Is.Null);
                }
                Call(match, "SetupScreen"); yield return null;
            }
        }
    }

    [UnityTest]
    public IEnumerator UnalignedNativeTrailerCannotTeleportIntoCoupling()
    {
        using (Preferences())
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/HitchAndRun.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            object match = UnityEngine.Object.FindObjectsByType(TypeOf("HitchRunMatch"), FindObjectsSortMode.None).Single();
            Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            yield return new WaitForSeconds(.4f);
            var coupler = ((Array)Field(match, "couplers")).GetValue(0); var trailer = ((Array)Field(match, "trailers")).GetValue(0);
            var body = (Rigidbody)Field(trailer, "Body"); Vector3 before = body.position;
            Assert.That((bool)Call(coupler, "TryCouple", trailer), Is.False);
            Assert.That(body.position, Is.EqualTo(before), "A rejected hitch request must not relocate the trailer.");
            Assert.That(Property(coupler, "LastReason").ToString(), Does.Contain("kingpin"));
            Call(match, "SetupScreen");
        }
    }

    [UnityTest]
    public IEnumerator SavedRocketBoostersHavePhysicalSuspensionClearance()
    {
        using (Preferences())
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/Trailerblazers.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var apron = GameObject.Find("Industrial competition apron").GetComponent<Collider>();
            var casings = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("Mounted physical booster casing ", StringComparison.Ordinal)).ToArray();
            Assert.That(casings.Length, Is.EqualTo(16));
            foreach (var casing in casings)
                foreach (var collider in casing.GetComponentsInChildren<Collider>(true))
                    Assert.That(collider.bounds.min.y - apron.bounds.max.y, Is.GreaterThanOrEqualTo(1.39f), casing.name + " must not begin in the floor, including nozzle contact and suspension settlement margin.");
        }
    }

    [UnityTest]
    public IEnumerator NaturalRocketAIStagesClimbsAndCompletesControlledLanding()
    {
        using (Preferences())
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/Trailerblazers.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            object match = UnityEngine.Object.FindObjectsByType(TypeOf("TrailerblazersMatch"), FindObjectsSortMode.None).Single();
            object setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
            Call(match, "NormalizeSetup"); Call(match, "AllBots");
            var weather = Field(match, "presentationWeather");
            if (weather != null)
            {
                var catalog = Field(weather, "presets");
                if (catalog != null) Call(catalog, "Select", "9aad5ff5dfa09ce468205f676714f891");
            }
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
            if (weather != null && Field(weather, "presets") == null) Call(weather, "Configure", 1234, 1, 0);
            try
            {
                Time.timeScale = 3; // Accelerated genuine physics; no result/score injection.
                float deadline = Time.realtimeSinceStartup + 55;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) yield return null;
                string observed = " age=" + Property(match, "Elapsed") + " peak=" + Call(match, "PeakAltitude", 0) + " air=" + Call(match, "AirborneSeconds", 0) + " releases=" + Call(match, "StageReleaseCount", 0);
                Assert.That((bool)Property(match, "Finished"), Is.True, "Natural AI rocket round must resolve." + observed);
                Assert.That((float)Call(match, "PeakAltitude", 0), Is.GreaterThan(30), "Actual body must climb." + observed);
                Assert.That((float)Call(match, "AirborneSeconds", 0), Is.GreaterThan(2), "Actual body must fly." + observed);
                Assert.That((int)Call(match, "StageReleaseCount", 0), Is.EqualTo(1), "One actual native stage separation." + observed);
                Assert.That((bool)Call(match, "LandedControlled", 0), Is.True, "The tractor must finish upright and quiet on real support, not through the flight watchdog." + observed);
                Assert.That((bool)Call(match, "ResultWinner", 0), Is.True, "Only the genuine resolved result can award victory.");
                var coupler = ((Array)Field(match, "couplers")).GetValue(0);
                Assert.That(Property(coupler, "Attached"), Is.Null);
                var trailer = (Component)((Array)Field(match, "trailers")).GetValue(0);
                Assert.That(trailer.gameObject.activeSelf, Is.True, "The separated booster remains a physical scene object.");
            }
            finally { Time.timeScale = 1; }
            Call(match, "SetupScreen");
        }
    }
}
#endif
