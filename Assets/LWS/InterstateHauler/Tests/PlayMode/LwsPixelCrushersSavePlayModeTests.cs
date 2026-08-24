using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsPixelCrushersSavePlayModeTests
    {
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
        public IEnumerator PixelCrushersProofSaveRoundTripsSemanticPayloads()
        {
            GameObject bootstrapObject = new GameObject("pixel-crushers-save-bootstrap");
            LwsApplicationBootstrap bootstrap = bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsSaveService saveService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWorldOriginService originService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsGameClockService clockService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWeatherService weatherService));

            saveService.DeleteSlot(LwsSaveService.DevelopmentTestSlot);
            originService.UpdatePlayerLocalPosition(new Vector3(12.5f, 1.25f, 6400f));
            clockService.SetDateTime(new LwsGameDateTime(2026, 6, 2, 14, 35, 12f));
            clockService.SetTimeScale(6f);
            weatherService.RequestWeather(LwsWeatherPresetCatalog.FogId, 0f, true);

            LwsSaveOperationResult save = saveService.SaveTestState();
            Assert.IsTrue(save.Succeeded, save.Message);

            originService.UpdatePlayerLocalPosition(Vector3.zero);
            clockService.SetDateTime(new LwsGameDateTime(2026, 6, 3, 3, 10, 0f));
            clockService.SetTimeScale(1f);
            weatherService.RequestWeather(LwsWeatherPresetCatalog.ClearId, 0f, true);

            LwsSaveOperationResult load = saveService.LoadTestState();
            Assert.IsTrue(load.Succeeded, load.Message);

            LwsGlobalPositionSaveParticipant globalProvider =
                saveService.Participants.OfType<LwsGlobalPositionSaveParticipant>().FirstOrDefault();
            Assert.IsNotNull(globalProvider);
            Assert.That(globalProvider.LastRestored.globalZ, Is.EqualTo(6400d).Within(0.001d));
            Assert.IsTrue(saveService.Diagnostics.validationVariableRoundTripped);
            Assert.AreEqual(14, clockService.CurrentSnapshot.hour);
            Assert.AreEqual(LwsWeatherPresetCatalog.FogId, weatherService.CurrentSnapshot.weatherPresetId);

            saveService.DeleteSlot(LwsSaveService.DevelopmentTestSlot);
        }
    }
}
