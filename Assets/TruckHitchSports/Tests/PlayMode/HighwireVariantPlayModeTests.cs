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

public sealed class HighwireVariantPlayModeTests
{
    static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(value);
    static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method).Invoke(value, args);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10HighwireVariants" });
    static Type Runtime => Type.GetType("LWS.TruckHitchSports.HighwireMatch, Assembly-CSharp", true);
    static void Load() => EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity", new LoadSceneParameters(LoadSceneMode.Single));

    [UnityTest]
    public IEnumerator AlternateSupportsSwitchWithoutChangingDefaultGeometryOrStarts()
    {
        using (Preferences())
        {
            Load(); yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
            var originals = (GameObject[])Field(match, "originalRoadway");
            Assert.That(originals.Length, Is.EqualTo(24), "Run the explicit additive variant installer first.");
            var poses = originals.Select(g => g.transform.localToWorldMatrix).ToArray();
            var trucks = (Array)Field(match, "trucks");
            var spawn = Enumerable.Range(0, trucks.Length).Select(i => ((Component)trucks.GetValue(i)).transform.position).ToArray();
            var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
            Call(match, "AllBots");
            foreach (int mode in new[] { 0, 1, 3, 0 })
            {
                match.GetType().GetField("option").SetValue(match, mode); Call(match, "NormalizeSetup");
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                for (int i = 0; i < originals.Length; i++)
                {
                    Assert.That(originals[i].activeInHierarchy, Is.EqualTo(mode == 0));
                    Assert.That(originals[i].transform.localToWorldMatrix, Is.EqualTo(poses[i]), "Variant authoring may mask the original support, never move it.");
                }
                for (int i = 0; i < trucks.Length; i++) Assert.That(Vector3.Distance(((Component)trucks.GetValue(i)).transform.position, spawn[i]), Is.LessThan(.1f));
                if (mode != 0)
                {
                    var courses = (Array)Field(match, "courseVariants");
                    object active = courses.Cast<object>().Single(c => (int)Field(c, "Mode") == mode);
                    Assert.That(((Component)active).gameObject.activeInHierarchy, Is.True);
                    Physics.SyncTransforms();
                    foreach (var point in (Vector3[])Field(active, "Points"))
                    {
                        Assert.That(Physics.Raycast(point + Vector3.up * 3, Vector3.down, out var hit, 5, 1, QueryTriggerInteraction.Ignore), Is.True, "Every real checkpoint requires visible physical support.");
                        Assert.That(Mathf.Abs(hit.point.y - point.y), Is.LessThan(.3f));
                    }
                }
                Call(match, "SetupScreen");
                yield return null;
            }
        }
    }

    // Sample while the native simulation is live; EndMatch deliberately parks
    // the motors, so terminal gear/velocity alone cannot diagnose a failed run.
    static string LiveSample(object match, Component truck, Component cargo, HitchNaturalImpactProbe[] tractorContacts, HitchNaturalImpactProbe loadContacts)
    {
        var tractor = (Rigidbody)Field(truck, "body"); var load = (Rigidbody)Field(cargo, "Body");
        var vehicle = Field(Field(truck, "tractorMotor"), "vehicle"); var powertrain = Field(vehicle, "powertrain");
        var engine = Field(powertrain, "engine"); var coupler = ((Array)Field(match, "couplers")).GetValue(0);
        var wheels = ((Array)Field(truck, "wheels")).Cast<object>().ToArray();
        string contacts = string.Join(" | ", tractorContacts.Select(probe => probe.gameObject.name + ":" + probe.LastContact));
        return "age=" + ((float)Property(match, "Elapsed")).ToString("F1") + " " + Call(match, "VariantDiagnostic", 0) + " cargoV=" + load.linearVelocity.ToString("F2") + " tractorUp=" + Vector3.Dot(tractor.rotation * Vector3.up, Vector3.up).ToString("F2") + " cargoUp=" + Vector3.Dot(load.rotation * Vector3.up, Vector3.up).ToString("F2") + " tractorGrounded=" + wheels.Count(wheel => (bool)Property(wheel, "IsGrounded")) + "/" + wheels.Length + " cargoGrounded=" + Property(cargo, "WheelContact") + " engine=" + Property(engine, "IsRunning") + " stalled=" + Property(engine, "IsStalled") + " " + Call(coupler, "DiagnosticState", cargo) + " tractorContacts=[" + contacts + "] cargoContact=[" + loadContacts.LastContact + "]";
    }

    [UnityTest]
    public IEnumerator NaturalTechnicalRigActuallyReversesAndFinishesThenResets()
    {
        using (Preferences())
        {
            Load(); yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
            var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
            match.GetType().GetField("option").SetValue(match, 3); Call(match, "NormalizeSetup"); Call(match, "AllBots");
            Assert.That(((Array)Field(match, "courseVariants")).Length, Is.EqualTo(2), "Explicit additive variant authoring required.");
            var weather = Field(match, "presentationWeather");
            if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
            var truck = (Component)((Array)Field(match, "trucks")).GetValue(0);
            var cargo = (Component)((Array)Field(match, "trailers")).GetValue(0);
            var load = (Rigidbody)Field(cargo, "Body");
            var tractorContacts = truck.GetComponentsInChildren<Rigidbody>(true).Select(part => part.gameObject.AddComponent<HitchNaturalImpactProbe>()).ToArray();
            var loadContacts = load.gameObject.AddComponent<HitchNaturalImpactProbe>();
            string lastLive = "not sampled"; float nextSample = 5; int samples = 0;
            try
            {
                Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 100;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    float age = (float)Property(match, "Elapsed");
                    if (age >= nextSample && samples < 48)
                    {
                        lastLive = LiveSample(match, truck, cargo, tractorContacts, loadContacts);
                        Debug.Log("HH10_HIGHWIRE_TECHNICAL_LIVE " + lastLive); samples++; nextSample += 5;
                    }
                    yield return null;
                }
                string state = (string)Call(match, "VariantDiagnostic", 0) + " age=" + Property(match, "Elapsed") + " failed=" + ((bool[])Field(match, "failed"))[0] + " lastLive=[" + lastLive + "]";
                Assert.That((bool)Property(match, "Finished"), Is.True, state);
                Assert.That((int)Call(match, "ReversingGatesPassed", 0), Is.EqualTo(1), "Both real bodies must traverse the marked section backward. " + state);
                Assert.That((bool)Call(match, "ResultEligible", 0), Is.True, "A physical complete crossing is required; timeout/fall cannot pass. " + state);
                Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((int)Call(match, "ReversingGatesPassed", 0), Is.Zero);
                Call(match, "SetupScreen");
            }
            finally { Time.timeScale = 1; }
        }
    }
}
#endif
