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
        public IEnumerator AutosaveUsesPixelCrushersSlotWithoutOverwritingManualSlot()
        {
            GameObject bootstrapObject = new GameObject("pixel-crushers-autosave-bootstrap");
            LwsApplicationBootstrap bootstrap = bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsSaveService saveService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWorldOriginService originService));
            Assert.IsTrue(saveService.Adapter.ReadyForRuntimeSaves, saveService.Diagnostics.adapterStatus);

            string profileId = saveService.ActiveProfileId;
            saveService.Delete(profileId, 1);
            saveService.DeleteAutosave(profileId);
            yield return null;

            originService.UpdatePlayerLocalPosition(new Vector3(10f, 0f, 25f));
            LwsSaveOperationResult manual = saveService.Save(profileId, 1, false);
            Assert.IsTrue(manual.Succeeded, manual.Message);
            int manualVendorSlot = saveService.GetSlotMetadata(profileId, 1).vendorSlotNumber;

            originService.UpdatePlayerLocalPosition(new Vector3(20f, 0f, 45f));
            LwsSaveOperationResult autosave = saveService.RequestAutosave("playmode test");
            Assert.IsTrue(autosave.Succeeded, autosave.Message);
            LwsManualSaveSlotMetadata autosaveSlot = saveService.GetAutosaveSlotMetadata(profileId);

            Assert.IsTrue(autosaveSlot.occupied);
            Assert.AreNotEqual(manualVendorSlot, autosaveSlot.vendorSlotNumber);
            Assert.IsTrue(saveService.HasSave(profileId, 1));
            Assert.IsTrue(saveService.HasAutosave(profileId));

            saveService.Delete(profileId, 1);
            saveService.DeleteAutosave(profileId);
        }

        [UnityTest]
        public IEnumerator SavedGameDataPreReadDoesNotApplyGameplayState()
        {
            GameObject bootstrapObject = new GameObject("pixel-crushers-preread-bootstrap");
            LwsApplicationBootstrap bootstrap = bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsSaveService saveService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWorldOriginService originService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsGameClockService clockService));
            Assert.IsTrue(saveService.Adapter.ReadyForRuntimeSaves, saveService.Diagnostics.adapterStatus);

            string profileId = saveService.ActiveProfileId;
            saveService.Delete(profileId, 2);
            yield return null;

            originService.UpdatePlayerLocalPosition(new Vector3(5f, 0f, 1500f));
            clockService.SetDateTime(new LwsGameDateTime(2026, 8, 25, 12, 30, 0f));
            LwsSaveOperationResult save = saveService.Save(profileId, 2, false);
            Assert.IsTrue(save.Succeeded, save.Message);

            clockService.SetDateTime(new LwsGameDateTime(2026, 8, 25, 2, 0, 0f));
            int hourBeforePreRead = clockService.CurrentSnapshot.hour;
            LwsManualSaveSlotMetadata slot = saveService.GetSlotMetadata(profileId, 2);
            LwsSaveOperationResult preRead = saveService.LoadCoordinator.PreReadVendorSlot(saveService.ActiveProfile, slot.vendorSlotNumber, LwsSaveLoadSlotKind.ManualPrimary, out LwsLoadApplicationContext context);

            Assert.IsTrue(preRead.Succeeded, preRead.Message);
            Assert.IsNotNull(context);
            Assert.AreEqual(hourBeforePreRead, clockService.CurrentSnapshot.hour);
            Assert.IsFalse(context.AppliedVendorLoad);

            saveService.Delete(profileId, 2);
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

            menuService.ShowPauseMenu();
            yield return null;

            Assert.IsTrue(menuService.IsOpen);
            Assert.IsTrue(inputSource.DrivingInputSuppressed);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsNotNull(menuService.RuntimeRoot);
            Assert.AreEqual(LwsPersistenceMenuView.Main, menuService.RuntimeRoot.CurrentView);
            Assert.AreEqual(LwsPersistenceMenuOpenContext.PauseMenu, menuService.RuntimeRoot.OpenContext);

            menuService.ShowSaveLoad();
            yield return null;

            Assert.IsTrue(menuService.IsOpen);
            Assert.IsTrue(inputSource.DrivingInputSuppressed);
            Assert.AreEqual(LwsPersistenceMenuView.SaveLoad, menuService.RuntimeRoot.CurrentView);
            Assert.AreEqual(LwsPersistenceMenuOpenContext.DirectSaveLoad, menuService.RuntimeRoot.OpenContext);

            menuService.Hide();
            yield return null;

            Assert.IsFalse(menuService.IsOpen);
            Assert.IsFalse(inputSource.DrivingInputSuppressed);
            Assert.AreEqual(1f, Time.timeScale);
            Object.Destroy(inputObject);
        }
    }
}
