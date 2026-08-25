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
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (LwsApplicationBootstrap.Instance != null)
            {
                Object.Destroy(LwsApplicationBootstrap.Instance.gameObject);
            }

            foreach (LwsPersistencePauseMenu menu in Object.FindObjectsByType<LwsPersistencePauseMenu>(FindObjectsSortMode.None))
            {
                Object.Destroy(menu.gameObject);
            }

            foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (transform != null && transform.gameObject.name == "Save System")
                {
                    Object.Destroy(transform.gameObject);
                }
            }

            LwsApplicationBootstrap.ResetForTests();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PixelCrushersManualSlotRoundTripsSemanticPayloads()
        {
            GameObject bootstrapObject = new GameObject("pixel-crushers-save-bootstrap");
            LwsApplicationBootstrap bootstrap = bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsSaveService saveService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWorldOriginService originService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsGameClockService clockService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWeatherService weatherService));
            Assert.IsTrue(saveService.Adapter.ReadyForRuntimeSaves, saveService.Diagnostics.adapterStatus);

            string profileId = saveService.ActiveProfileId;
            saveService.Delete(profileId, 1);
            yield return null;

            originService.UpdatePlayerLocalPosition(new Vector3(12.5f, 1.25f, 6400f));
            clockService.SetDateTime(new LwsGameDateTime(2026, 6, 2, 14, 35, 12f));
            clockService.SetTimeScale(6f);
            weatherService.RequestWeather(LwsWeatherPresetCatalog.FogId, 0f, true);

            LwsSaveOperationResult save = saveService.Save(profileId, 1, false);
            Assert.IsTrue(save.Succeeded, save.Message);
            Assert.IsTrue(saveService.HasSave(profileId, 1));

            originService.UpdatePlayerLocalPosition(Vector3.zero);
            clockService.SetDateTime(new LwsGameDateTime(2026, 6, 3, 3, 10, 0f));
            clockService.SetTimeScale(1f);
            weatherService.RequestWeather(LwsWeatherPresetCatalog.ClearId, 0f, true);

            LwsSaveOperationResult load = saveService.Load(profileId, 1);
            Assert.IsTrue(load.Succeeded, load.Message);
            yield return null;

            LwsGlobalPositionSaveParticipant globalProvider =
                saveService.Participants.OfType<LwsGlobalPositionSaveParticipant>().FirstOrDefault();
            Assert.IsNotNull(globalProvider);
            Assert.That(globalProvider.LastRestored.globalZ, Is.EqualTo(6400d).Within(0.001d));
            Assert.AreEqual(14, clockService.CurrentSnapshot.hour);
            Assert.AreEqual(LwsWeatherPresetCatalog.FogId, weatherService.CurrentSnapshot.weatherPresetId);

            LwsSaveOperationResult delete = saveService.Delete(profileId, 1);
            Assert.IsTrue(delete.Succeeded, delete.Message);
            Assert.IsFalse(saveService.GetSlotMetadata(profileId, 1).occupied);
        }

        [UnityTest]
        public IEnumerator PersistencePauseMenuIsAccessibleAndSuppressesFallbackDrivingInput()
        {
            GameObject inputObject = new GameObject("keyboard-gamepad-input-source");
            LwsKeyboardGamepadTruckInputSource inputSource = inputObject.AddComponent<LwsKeyboardGamepadTruckInputSource>();
            inputSource.SetDrivingInputSuppressed(false);

            GameObject bootstrapObject = new GameObject("persistence-menu-bootstrap");
            LwsApplicationBootstrap bootstrap = bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsPersistenceMenuService menuService));
            Assert.IsFalse(menuService.IsOpen);

            menuService.Show();
            yield return null;

            Assert.IsTrue(menuService.IsOpen);
            Assert.IsTrue(inputSource.DrivingInputSuppressed);
            Assert.AreEqual(0f, Time.timeScale);

            menuService.Hide();
            yield return null;

            Assert.IsFalse(menuService.IsOpen);
            Assert.IsFalse(inputSource.DrivingInputSuppressed);
            Assert.AreEqual(1f, Time.timeScale);
            Object.Destroy(inputObject);
        }
    }
}
