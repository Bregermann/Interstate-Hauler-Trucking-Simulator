using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsGameplayStateEditModeTests
    {
        private const string SaveArchitecturePath = "Assets/LWS/InterstateHauler/Save/LwsSaveArchitecture.cs";
        private const string SaveProfileTypesPath = "Assets/LWS/InterstateHauler/Save/LwsSaveProfileTypes.cs";
        private const string SaveLoadCoordinatorPath = "Assets/LWS/InterstateHauler/Save/LwsSaveLoadCoordinator.cs";
        private const string GameplayStatePath = "Assets/LWS/InterstateHauler/Gameplay/LwsGameplayState.cs";
        private const string BootstrapPath = "Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs";

        [Test]
        public void InitialStateIsDeterministicAndReadOnly()
        {
            var service = CreateInitializedService();

            Assert.AreEqual(LwsGameplayState.Initializing, service.CurrentState);
            Assert.AreEqual(LwsGameplayState.Initializing, service.PreviousState);
            Assert.IsFalse(service.AllowsDrivingInput);
            Assert.IsFalse(typeof(ILwsGameplayStateService).GetProperty(nameof(ILwsGameplayStateService.CurrentState)).CanWrite);
        }

        [Test]
        public void ValidTransitionRecordsPreviousStateReasonAndEventOnce()
        {
            var service = CreateInitializedService();
            int eventCount = 0;
            LwsGameplayStateChangedEvent lastEvent = default;
            service.StateChanged += evt =>
            {
                eventCount++;
                lastEvent = evt;
            };

            LwsGameplayStateTransitionResult result = service.EnterFreeDrive("services ready");
            LwsGameplayStateTransitionResult duplicate = service.EnterFreeDrive("still ready");

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.IsTrue(result.Changed);
            Assert.AreEqual(LwsGameplayState.Initializing, result.From);
            Assert.AreEqual(LwsGameplayState.FreeDrive, result.To);
            Assert.AreEqual(LwsGameplayState.Initializing, service.PreviousState);
            Assert.AreEqual("services ready", service.LastTransitionReason);
            Assert.IsTrue(service.AllowsDrivingInput);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(LwsGameplayState.FreeDrive, lastEvent.CurrentState);
            Assert.IsTrue(lastEvent.AllowsDrivingInput);
            Assert.IsTrue(duplicate.Succeeded, duplicate.Message);
            Assert.IsFalse(duplicate.Changed);
            Assert.AreEqual(1, eventCount, "No-op same-state requests must not publish duplicate transition events.");
        }

        [Test]
        public void InvalidTransitionIsRejectedWithoutChangingState()
        {
            var service = CreateInitializedService();
            LogAssert.Expect(LogType.Warning, "Rejected gameplay state transition Initializing -> Paused: Initializing may only enter FreeDrive, LoadingWorld, or RecoveryError. Reason: invalid test");

            LwsGameplayStateTransitionResult result = service.TryTransitionTo(LwsGameplayState.Paused, "invalid test");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(LwsGameplayState.Initializing, service.CurrentState);
            Assert.IsFalse(service.AllowsDrivingInput);
        }

        [Test]
        public void PauseReturnsToPreviousGameplayState()
        {
            var service = CreateInitializedService();
            Assert.IsTrue(service.EnterFreeDrive("ready").Succeeded);
            Assert.IsTrue(service.Pause("pause requested").Succeeded);

            Assert.AreEqual(LwsGameplayState.Paused, service.CurrentState);
            Assert.AreEqual(LwsGameplayState.FreeDrive, service.PauseReturnState);
            Assert.IsFalse(service.AllowsDrivingInput);

            LwsGameplayStateTransitionResult resume = service.Resume("resume requested");

            Assert.IsTrue(resume.Succeeded, resume.Message);
            Assert.AreEqual(LwsGameplayState.FreeDrive, service.CurrentState);
            Assert.AreEqual(LwsGameplayState.Paused, service.PreviousState);
            Assert.IsTrue(service.AllowsDrivingInput);
        }

        [Test]
        public void CurrentDrivingPermissionsMatchCanonicalVocabulary()
        {
            Assert.IsFalse(LwsGameplayStateRules.AllowsDrivingInput(LwsGameplayState.Initializing));
            Assert.IsFalse(LwsGameplayStateRules.AllowsDrivingInput(LwsGameplayState.LoadingWorld));
            Assert.IsTrue(LwsGameplayStateRules.AllowsDrivingInput(LwsGameplayState.FreeDrive));
            Assert.IsFalse(LwsGameplayStateRules.AllowsDrivingInput(LwsGameplayState.Paused));
            Assert.IsFalse(LwsGameplayStateRules.AllowsDrivingInput(LwsGameplayState.Transitioning));
            Assert.IsFalse(LwsGameplayStateRules.AllowsDrivingInput(LwsGameplayState.RecoveryError));
        }

        [Test]
        public void FutureStatesAreReservedVocabularyOnly()
        {
            LwsGameplayState[] reserved =
            {
                LwsGameplayState.AtDepot,
                LwsGameplayState.JobSelection,
                LwsGameplayState.TrailerPickup,
                LwsGameplayState.HaulActive,
                LwsGameplayState.Delivery,
                LwsGameplayState.DeliveryResults
            };

            Assert.IsTrue(reserved.All(LwsGameplayStateRules.IsFutureReservedState));
            Assert.IsFalse(reserved.Any(LwsGameplayStateRules.IsCurrentRuntimeState));
        }

        [Test]
        public void BootstrapRegistersExactlyOneGameplayStateService()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();

            int count = registry.Registrations.Count(r => r.Service is ILwsGameplayStateService);
            Assert.AreEqual(1, count);
            Assert.IsTrue(registry.TryGet(out ILwsGameplayStateService service));
            Assert.AreEqual(LwsGameplayState.Initializing, service.CurrentState);
        }

        [Test]
        public void Prompt017LoadCoordinatorMapsToMacroStatesWithoutReplacingPixelCrushers()
        {
            string coordinator = File.ReadAllText(SaveLoadCoordinatorPath);

            StringAssert.Contains("EnterLoadingWorld", coordinator);
            StringAssert.Contains("EnterFreeDrive", coordinator);
            StringAssert.Contains("EnterRecoveryError", coordinator);
            StringAssert.Contains("LwsSaveLoadCoordinatorPhase.Complete", coordinator);
            StringAssert.Contains("LwsSaveLoadCoordinatorPhase.Failed", coordinator);
            StringAssert.Contains("PreReadVendorSlot(profile, primarySlot.backupVendorSlotNumber, backupKind, out backupContext, false)", File.ReadAllText(SaveArchitecturePath));
            Assert.IsFalse(coordinator.Contains("StoreSavedGameData"), "Prompt 019 must not turn the load coordinator into a storage backend.");
        }

        [Test]
        public void GameplayStateIsNotPersistedAsCareerAuthority()
        {
            string saveSources = File.ReadAllText(SaveArchitecturePath) + "\n" +
                                 File.ReadAllText(SaveProfileTypesPath) + "\n" +
                                 File.ReadAllText(SaveLoadCoordinatorPath);
            string gameplayStateSource = File.ReadAllText(GameplayStatePath);

            Assert.IsFalse(saveSources.Contains("LwsGameplayStateSavePayload"));
            Assert.IsFalse(saveSources.Contains("lws.gameplay.state"), "Gameplay state service ID must not become a semantic save participant ID.");
            Assert.IsFalse(gameplayStateSource.Contains("ILwsSaveParticipant"));
            Assert.IsFalse(gameplayStateSource.Contains("JsonUtility.ToJson"));
        }

        [Test]
        public void PixelCrushersAndInputAuthoritiesRemainUnchanged()
        {
            string saveSource = File.ReadAllText(SaveArchitecturePath);
            string bootstrapSource = File.ReadAllText(BootstrapPath);

            StringAssert.Contains("LwsPixelCrushersSaveAdapter", saveSource);
            StringAssert.Contains("LwsPixelCrushersSemanticSaver", saveSource);
            StringAssert.Contains("registry.Register<ILwsVehicleInputService>(new LwsVehicleInputService())", bootstrapSource);
            Assert.IsFalse(saveSource.Contains("interface ILwsSaveStorage"));
            Assert.IsFalse(saveSource.Contains("LwsInMemorySaveStorage"));
        }

        private static LwsGameplayStateService CreateInitializedService()
        {
            var service = new LwsGameplayStateService();
            LwsServiceResult result = service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            Assert.IsTrue(result.Succeeded, result.Message);
            return service;
        }
    }
}