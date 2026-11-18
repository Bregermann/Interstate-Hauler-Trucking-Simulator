#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TrebuchetDownrangePlayModeTests
{
    static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(value);
    static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method).Invoke(value, args);
    static Type Runtime => Type.GetType("LWS.TruckHitchSports.TrebuchetMatch, Assembly-CSharp", true);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10TrebuchetDownrange" });
    [UnityTest] public IEnumerator NaturalReleasePreservesRealDownrangeFlightAndRestoresContacts() => Natural(0);
    [UnityTest] public IEnumerator NaturalCastleThrowPhysicallyStrikesReachableMasonry() => Natural(2);

    // Read-only observations of the same native bodies/controller used by the
    // ordinary match. No inputs, settings, state transitions or poses are changed.
    static string LiveSample(object match, object engine, Component truck, Component cargo, HitchNaturalImpactProbe[] tractorContacts, HitchNaturalImpactProbe loadContacts)
    {
        var tractor = (Rigidbody)Field(truck, "body"); var load = (Rigidbody)Field(cargo, "Body");
        var vehicle = Field(Field(truck, "tractorMotor"), "vehicle");
        var powertrain = Field(vehicle, "powertrain"); var nativeEngine = Field(powertrain, "engine"); var transmission = Field(powertrain, "transmission");
        var wheels = ((Array)Field(truck, "wheels")).Cast<object>().ToArray();
        string contacts = string.Join(" | ", tractorContacts.Select(probe => probe.gameObject.name + ":" + probe.LastContact));
        return "age=" + ((float)Property(match, "Elapsed")).ToString("F1") + " tractor=" + tractor.position.ToString("F2") + " velocity=" + tractor.linearVelocity.ToString("F2") + " heading=" + (tractor.rotation * Vector3.forward).ToString("F2") + " gear=" + Property(transmission, "Gear") + " engine=" + Property(nativeEngine, "IsRunning") + " stalled=" + Property(nativeEngine, "IsStalled") + " wheelGrounded=" + wheels.Count(wheel => (bool)Property(wheel, "IsGrounded")) + "/" + wheels.Length + " cargoCOM=" + load.worldCenterOfMass.ToString("F2") + " cargoV=" + load.linearVelocity.ToString("F2") + " cargoGrounded=" + Property(cargo, "WheelContact") + " canLoad=" + Call(engine, "CanLoad", cargo) + " " + Call(match, "TrebuchetLoadingDiagnostic", 0) + " " + Call(match, "ReleaseDecisionDiagnostic", 0) + " tractorContacts=[" + contacts + "] cargoContact=[" + loadContacts.LastContact + "]";
    }

    IEnumerator Natural(int mode)
    {
        using (Preferences())
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
            var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
            match.GetType().GetField("option").SetValue(match, mode); Call(match, "NormalizeSetup"); Call(match, "AllBots");
            var weather = Field(match, "presentationWeather");
            if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
            var engine = ((Array)Field(match, "engines")).GetValue(0);
            var trailer = (Component)((Array)Field(match, "trailers")).GetValue(0); var body = (Rigidbody)Field(trailer, "Body");
            var target = ((Transform[])Field(match, "targets"))[0];
            if (mode == 2) Assert.That(target.position.z, Is.LessThan(90), "Use the explicit short-range siege target authoring, preserving the original physical blocks.");
            var observer = body.gameObject.AddComponent<TrebuchetReleaseContactObserver>(); observer.Released = () => (bool)Property(engine, "Released");
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
            var truck = (Component)((Array)Field(match, "trucks")).GetValue(0);
            var tractorContacts = truck.GetComponentsInChildren<Rigidbody>(true).Select(part => part.gameObject.AddComponent<HitchNaturalImpactProbe>()).ToArray();
            var loadContacts = body.gameObject.AddComponent<HitchNaturalImpactProbe>();
            float nextSample = 10; int samples = 0; string lastLive = "not sampled";
            try
            {
                Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 70;
                float airborneDownrange = 0; bool released = false; Vector3 start = Vector3.zero, direction = Vector3.forward;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    float age = (float)Property(match, "Elapsed");
                    if (age >= nextSample && samples < 18)
                    {
                        lastLive = LiveSample(match, engine, truck, trailer, tractorContacts, loadContacts);
                        Debug.Log("HH10_TREBUCHET_DOWNRANGE_LIVE mode=" + mode + " " + lastLive);
                        samples++; nextSample += 10;
                    }
                    if ((bool)Property(engine, "Released"))
                    {
                        if (!released) { released = true; start = (Vector3)Property(engine, "ReleaseWorldPosition"); direction = Vector3.ProjectOnPlane(target.position - start, Vector3.up).normalized; }
                        if (observer.Contacts == 0) airborneDownrange = Mathf.Max(airborneDownrange, Vector3.Dot(body.worldCenterOfMass - start, direction));
                    }
                    yield return null;
                }
                string state = (string)Call(engine, "ReleaseDiagnostic") + " airborneDownrange=" + airborneDownrange.ToString("F2") + " finalCOM=" + body.worldCenterOfMass.ToString("F2") + " contacts=" + observer.Summary + " lastLive=[" + lastLive + "]";
                Debug.Log("HH10_TREBUCHET_RELEASE_CLEARANCE " + state);
                Assert.That((bool)Property(match, "Finished"), Is.True, state);
                Assert.That(released, Is.True, state);
                Assert.That(Vector3.Dot((Vector3)Property(engine, "ReleaseWorldVelocity"), direction), Is.GreaterThan(1), "Observe the actual pre-release velocity, not a projected score. " + state);
                Assert.That(airborneDownrange, Is.GreaterThan(8), "A physical forward flight must precede first external impact. " + state);
                Assert.That(observer.Contacts, Is.GreaterThan(0), "All ordinary world/target contacts must remain active. " + state);
                Assert.That((int)Property(engine, "ActiveClearancePairs"), Is.Zero, "Release-only contact filtering must be restored. " + state);
                if (mode == 2) Assert.That(((int[])Field(match, "targetsHit"))[0], Is.GreaterThan(0), "Only actual trailer/masonry collisions can certify demolition. " + state);
                Assert.That(((double[])Field(match, "Scores"))[0], Is.GreaterThan(0), state);
                Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((int)Property(engine, "ActiveClearancePairs"), Is.Zero);
                Assert.That((Vector3)Property(engine, "ReleaseWorldVelocity"), Is.EqualTo(Vector3.zero));
                Call(match, "SetupScreen");
            }
            finally { Time.timeScale = 1; }
        }
    }
}

public sealed class TrebuchetReleaseContactObserver : MonoBehaviour
{
    public Func<bool> Released;
    public int Contacts { get; private set; }
    public string Summary { get; private set; } = "none";
    void OnCollisionEnter(Collision collision)
    {
        if (Released == null || !Released()) return;
        Contacts++;
        if (Contacts > 3) return;
        var body = GetComponent<Rigidbody>(); var contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
        string sample = "#" + Contacts + " other=" + collision.collider.name + " relative=" + collision.relativeVelocity.ToString("F2") + " resultingV=" + body.linearVelocity.ToString("F2") + " COM=" + body.worldCenterOfMass.ToString("F2") + " normal=" + contact.normal.ToString("F2");
        Summary = Contacts == 1 ? sample : Summary + " | " + sample;
    }
}
#endif
