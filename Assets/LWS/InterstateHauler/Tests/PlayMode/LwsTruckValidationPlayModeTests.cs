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
    public sealed class LwsTruckValidationPlayModeTests
    {
        private const string TruckValidationScenePath = "Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity";

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

        [UnityTest]
        public IEnumerator PlayerVehicleServiceRegistersWithBootstrap()
        {
            var go = new GameObject("vehicle-service-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsPlayerVehicleService playerVehicleService));
            Assert.IsFalse(playerVehicleService.HasActiveTruck);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator TruckValidationSceneLoadsInEditorPlayMode()
        {
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                TruckValidationScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;

            Assert.IsNotNull(Object.FindFirstObjectByType<LwsTruckValidationSceneMarker>());
            Assert.IsNotNull(Object.FindFirstObjectByType<LwsPlayerTruckSpawner>());
            Assert.IsNotNull(Object.FindFirstObjectByType<LwsApplicationBootstrap>());
        }
#endif
    }
}
