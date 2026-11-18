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
    public sealed class CargoMountainRecoveryPlayModeTests
    {
        static Type Runtime(string name) => Type.GetType("LWS.TruckExtremeHauling." + name + ", Assembly-CSharp", true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);

        [UnityTest]
        public IEnumerator NaturalMountainAIWinchesHitchesDescendsAndDeliversSameNativeCargoThenRematches()
        {
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10CargoMountainRecovery" }))
            {
                object match = null; float originalScale = Time.timeScale;
                try
                {
                    EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity", new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null; yield return null;
                    match = UnityEngine.Object.FindObjectsByType(Runtime("CargoCrisisMatch"), FindObjectsSortMode.None).Single();
                    var sites = Field(match, "emergencySites");
                    Assert.That(sites, Is.Not.Null, "Use the existing saved emergency adapter.");
                    var mountain = Field(sites, "mountainRecovery");
                    Assert.That(mountain, Is.Not.Null, "Run the explicit saved mountain author first; the test never constructs an incident.");
                    Assert.That((int)Property(match, "OptionCount"), Is.GreaterThanOrEqualTo(8), "Integrate the optional option7 label/count hook before testing.");
                    var environment = (GameObject)Field(mountain, "environment");
                    Assert.That(environment.activeSelf, Is.False, "An unselected mountain incident must not alter ordinary Cargo Crisis.");
                    match.GetType().GetField("option").SetValue(match, 7);
                    var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                    Call(match, "NormalizeSetup"); Call(match, "AllBots");
                    var weather = Field(match, "presentationWeather");
                    if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                    var load = ((Array)Field(mountain, "loads")).GetValue(0); var body = (Rigidbody)Field(load, "Body"); int bodyId = body.GetInstanceID();
                    Assert.That(Vector3.Dot(((Component)load).transform.up, Vector3.up), Is.LessThan(.6f), "The incident begins with a genuinely tipped native load.");
                    var depot = (Transform)((Array)Field(mountain, "depots")).GetValue(0);
                    Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That((bool)Property(sites, "MountainActive"), Is.True);
                    var truck = (Component)((Array)Field(match, "trucks")).GetValue(0);
                    var coupler = truck.GetComponent(Type.GetType("LWS.TruckHitchSports.HitchCoupler, Assembly-CSharp", true));
                    var cable = (SpringJoint)((Array)Field(match, "winches")).GetValue(0);
                    Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 80, nextObservation = 0;
                    bool loadedCableSeen = false, elevatedNativeHitchSeen = false, actualDepotSupportSeen = false;
                    float highestTowed = float.NegativeInfinity, lowestTowed = float.PositiveInfinity;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                    {
                        loadedCableSeen |= cable.connectedBody == body && cable.currentForce.magnitude > 100;
                        if (Property(coupler, "Attached") == load)
                        {
                            elevatedNativeHitchSeen |= body.position.y > 25;
                            highestTowed = Mathf.Max(highestTowed, body.position.y); lowestTowed = Mathf.Min(lowestTowed, body.position.y);
                            actualDepotSupportSeen |= (bool)Property(load, "WheelContact") && Vector3.ProjectOnPlane(body.worldCenterOfMass - depot.position, Vector3.up).magnitude < 8 && body.linearVelocity.magnitude < 1;
                        }
                        if (Time.realtimeSinceStartup >= nextObservation)
                        {
                            nextObservation = Time.realtimeSinceStartup + 6;
                            Debug.Log("HH10_CARGO_MOUNTAIN_LIVE stage=" + Call(match, "RecoveryPhase", 0) + " cargo=" + body.worldCenterOfMass.ToString("F2") + " winchedMetres=" + ((float[])Field(sites, "CableTowMetres"))[0] + " righted=" + ((bool[])Field(sites, "MountainRecovered"))[0] + " " + Call(sites, "DiagnosticState", 0));
                        }
                        yield return null;
                    }
                    string state = "stage=" + Call(match, "RecoveryPhase", 0) + " cargo=" + body.worldCenterOfMass.ToString("F2") + " nativeTowHeight=" + highestTowed + ".." + lowestTowed + " " + Call(sites, "DiagnosticState", 0);
                    Assert.That((bool)Property(match, "Finished"), Is.True, state);
                    Assert.That(loadedCableSeen, Is.True, "A genuine loaded cable must perform the rescue. " + state);
                    Assert.That(((float[])Field(sites, "CableTowMetres"))[0], Is.GreaterThan(.5f), state);
                    Assert.That(((bool[])Field(sites, "MountainRecovered"))[0], Is.True, "Real elevated upright native wheel support must follow actual winching. " + state);
                    Assert.That(elevatedNativeHitchSeen, Is.True, "The original high-terrace load must physically couple to the tractor. " + state);
                    Assert.That(highestTowed - lowestTowed, Is.GreaterThan(8), "The native hitched load must actually traverse the descending mountain road. " + state);
                    Assert.That(actualDepotSupportSeen, Is.True, "A fall to the old valley floor or a cable-only arrival cannot count as supported lower-depot delivery. " + state);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.EqualTo(1), state);
                    Assert.That((bool)Call(match, "ResultWinner", 0), Is.True, state);
                    Assert.That(body.GetInstanceID(), Is.EqualTo(bodyId));
                    Call(match, "SetupScreen"); yield return null; yield return null;
                    Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That(((bool[])Field(sites, "MountainRecovered")).Any(value => value), Is.False);
                    Assert.That(((float[])Field(sites, "CableTowMetres")).Any(value => value != 0), Is.False);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.Zero);
                    Assert.That(body.GetInstanceID(), Is.EqualTo(bodyId));
                    Debug.Log("HH10_CARGO_MOUNTAIN_RESULT " + state + " rematch=fresh");
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
