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
    public sealed class RoadTrainObstaclePlayModeTests
    {
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method).Invoke(value, args);
        static Type Runtime => Type.GetType("LWS.TruckExtremeHauling.RoadTrainWreckMatch, Assembly-CSharp", true);
        static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10RoadTrainObstacle" });

        [UnityTest]
        public IEnumerator NaturalObstacleRaceRequiresConnectedTailGatesThenSettledDeliveryAndResets()
        {
            using (Preferences())
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/RoadTrainWreck.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                var course = Field(match, "obstacleCourse"); Assert.That(course, Is.Not.Null, "Apply the explicit Road Train obstacle upgrade first.");
                var content = (GameObject)Field(course, "Content"); var props = (Rigidbody[])Field(course, "MovableRoadblocks");
                Assert.That(props.Length, Is.EqualTo(48), "The twelve visible gates use real movable production props.");
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                match.GetType().GetField("option").SetValue(match, 1); Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather");
                if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                Assert.That((int)Call(match, "ObstacleGateCount", 0), Is.EqualTo(3));
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That(content.activeInHierarchy, Is.True);
                Assert.That(props.All(b => !b.isKinematic && b.GetComponent<Collider>() != null && b.GetComponentsInChildren<Renderer>(true).Length > 0), Is.True);
                try
                {
                    Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 75; float nextSample = 12;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                    {
                        if (Time.timeSinceLevelLoad >= nextSample) { nextSample += 12; Debug.Log("ROAD_TRAIN_OBSTACLE_ACTUAL " + Call(match, "TrainDiagnostic", 0)); }
                        yield return null;
                    }
                    string state = (string)Call(match, "TrainDiagnostic", 0);
                    Assert.That((bool)Property(match, "Finished"), Is.True, state);
                    Assert.That((int)Call(match, "ObstacleGatesPassed", 0), Is.EqualTo(3), "Every gate requires actual swept passage by the connected last trailer. " + state);
                    Assert.That((int)Call(match, "DeliveredTrailerCount", 0), Is.EqualTo((int)Property(match, "TrailersPerDriver")), state);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.EqualTo(1), "Only one real stopped delivery awards points. " + state);
                    Call(match, "SetupScreen"); match.GetType().GetField("option").SetValue(match, 0); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That(content.activeInHierarchy, Is.False, "Obstacle geometry must not affect the already-passing default or Maximum paths.");
                    Call(match, "SetupScreen"); match.GetType().GetField("option").SetValue(match, 1); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That((int)Call(match, "ObstacleGatesPassed", 0), Is.Zero); Assert.That((int)Call(match, "DeliveredTrailerCount", 0), Is.Zero);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.Zero);
                    yield return new WaitForSeconds(3.6f);
                    Assert.That((int)Call(match, "ConnectedTrailerCount", 0), Is.EqualTo((int)Property(match, "TrailersPerDriver")), (string)Call(match, "TrainDiagnostic", 0));
                    Call(match, "SetupScreen");
                }
                finally { Time.timeScale = 1; }
            }
        }
    }
}
#endif
