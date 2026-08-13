using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsInterstateCorridorPlayModeTests
    {
        private const string CorridorScenePath = "Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LwsApplicationBootstrap.ResetForTests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (LwsApplicationBootstrap.Instance != null)
            {
                Object.Destroy(LwsApplicationBootstrap.Instance.gameObject);
            }

            LwsApplicationBootstrap.ResetForTests();
            yield return null;
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator InterstateCorridorValidationSceneLoadsAndRegistersRoadGraph()
        {
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                CorridorScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));

            while (!operation.isDone)
            {
                yield return null;
            }

            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }

            LwsInterstateCorridorSceneMarker marker = Object.FindFirstObjectByType<LwsInterstateCorridorSceneMarker>();
            LwsInterstateCorridorRuntimeBuilder builder = Object.FindFirstObjectByType<LwsInterstateCorridorRuntimeBuilder>();
            LwsRoadGraphProvider provider = Object.FindFirstObjectByType<LwsRoadGraphProvider>();

            Assert.IsNotNull(marker);
            Assert.IsNotNull(builder);
            Assert.IsNotNull(provider);
            Assert.IsNotNull(Object.FindFirstObjectByType<LwsPlayerTruckSpawner>());
            Assert.IsNotNull(Object.FindFirstObjectByType<LwsApplicationBootstrap>());
            Assert.IsNotNull(builder.LastGraph);
            Assert.IsTrue(builder.LastGraph.Validate().IsValid, builder.LastGraph.Validate().Summary);
            Assert.GreaterOrEqual(Object.FindObjectsByType<LwsRoadSurface>(FindObjectsSortMode.None).Length, 3);
            Assert.IsTrue(provider.TryFindNearestRoad(new Vector3(12.8f, 0.55f, 500f), out LwsRoadLookupResult result));
            Assert.AreEqual(LwsRoadClass.Interstate, result.RoadClass);

            LwsUtsHighwayTrafficController traffic = Object.FindFirstObjectByType<LwsUtsHighwayTrafficController>();
            Assert.IsNotNull(traffic);
            Assert.IsNotNull(GameObject.Find("IH UTS Highway Traffic Runtime"));

            for (int i = 0; i < 80 && !traffic.Initialized; i++)
            {
                yield return null;
            }

            Assert.IsTrue(traffic.UtsAvailable, traffic.UtsTypeReport);
            Assert.IsTrue(traffic.GraphAvailable, traffic.LastError);
            Assert.IsTrue(traffic.Initialized, traffic.LastError);
            Assert.Greater(traffic.GeneratedLaneCount, 0);
            Assert.Greater(traffic.SpawnablePrefabCount, 0);

            bool spawned = traffic.TrySpawnOneForValidation(out string spawnMessage);
            Assert.IsTrue(spawned, spawnMessage);
            yield return null;

            Assert.GreaterOrEqual(traffic.Stats.ActiveVehicles, 1, traffic.LastSpawnResult);
            LwsApplicationBootstrap bootstrap = Object.FindFirstObjectByType<LwsApplicationBootstrap>();
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsTrafficService trafficService));
            Assert.GreaterOrEqual(trafficService.ActiveTrafficVehicles.Count, 1);
        }
#endif
    }
}
