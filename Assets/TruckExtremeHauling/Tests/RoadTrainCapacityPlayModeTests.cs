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
    public sealed class RoadTrainCapacityPlayModeTests
    {
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method).Invoke(value, args);
        static Type Runtime => Type.GetType("LWS.TruckExtremeHauling.RoadTrainWreckMatch, Assembly-CSharp", true);
        static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10RoadTrainCapacity" });
        static void Load() => EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/RoadTrainWreck.unity", new LoadSceneParameters(LoadSceneMode.Single));
        static void Configure(object match, int mode)
        {
            Field(match, "setup").GetType().GetField("count").SetValue(Field(match, "setup"), 1);
            match.GetType().GetField("option").SetValue(match, mode); Call(match, "NormalizeSetup"); Call(match, "AllBots");
        }

        [UnityTest]
        public IEnumerator DefaultThreeThenEightThenThreeUseActualNativeLinksAcrossRematches()
        {
            using (Preferences())
            {
                Load(); yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                Assert.That((int)Property(match, "TrailerCapacityPerDriver"), Is.GreaterThanOrEqualTo(8), "Apply the explicit additive capacity authoring first.");
                var trailers = (Array)Field(match, "trailers");
                foreach (int count in new[] { 3, 8, 3 })
                {
                    Configure(match, 0); match.GetType().GetField("trailersPerDriver").SetValue(match, count);
                    Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    yield return new WaitForSeconds(3.6f);
                    string state = (string)Call(match, "TrainDiagnostic", 0);
                    Assert.That((int)Call(match, "ConnectedTrailerCount", 0), Is.EqualTo(count), state);
                    int capacity = (int)Property(match, "TrailerCapacityPerDriver");
                    for (int piece = 0; piece < capacity; piece++)
                    {
                        var trailer = (Component)trailers.GetValue(piece);
                        Assert.That(trailer.gameObject.activeInHierarchy, Is.EqualTo(piece < count), "Authored spares cannot create invisible physics loads.");
                        if (piece < count)
                        {
                            Assert.That(((Rigidbody)Field(trailer, "Body")).mass, Is.GreaterThan(1000), "Each segment retains actual native trailer mass.");
                            Assert.That((bool)Property(Field(trailer, "Vehicle"), "IsInitialized"), Is.True, "Native axle/suspension setup must initialize for every active trailer.");
                        }
                    }
                    Call(match, "SetupScreen"); yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator NaturalMaximumChallengeDeliversSixRealTrailersAndResets()
        {
            using (Preferences())
            {
                Load(); yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                Assert.That((int)Property(match, "TrailerCapacityPerDriver"), Is.GreaterThanOrEqualTo(6), "Actual authored trailers are required for this length goal.");
                Configure(match, 2); match.GetType().GetField("maximumChallengeTrailers").SetValue(match, 6);
                var weather = Field(match, "presentationWeather");
                if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                try
                {
                    Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 70;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) yield return null;
                    string state = (string)Call(match, "TrainDiagnostic", 0);
                    Assert.That((bool)Property(match, "Finished"), Is.True, state);
                    Assert.That((int)Call(match, "DeliveredTrailerCount", 0), Is.EqualTo(6), "The full real connected train must settle at its depot. " + state);
                    Assert.That((int)Call(match, "ConnectedTrailerCount", 0), Is.EqualTo(6), state);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.EqualTo(1), "A delivery scores once, not per collider contact.");
                    Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That((int)Call(match, "DeliveredTrailerCount", 0), Is.Zero);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.Zero);
                    Call(match, "SetupScreen");
                }
                finally { Time.timeScale = 1; }
            }
        }
    }
}
#endif
