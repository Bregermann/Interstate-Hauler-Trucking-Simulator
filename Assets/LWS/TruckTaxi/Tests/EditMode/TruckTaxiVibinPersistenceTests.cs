using System;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiPersistence")]
    public sealed class TruckTaxiVibinPersistenceTests
    {
        private TruckTaxiEnvironmentSettings settings;
        [SetUp] public void SetUp() { settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>(); }
        [TearDown] public void TearDown()
        { UnityEngine.Object.DestroyImmediate(settings); TruckTaxiVibinPersistence.ClearContinueRequest(); }

        private TruckTaxiVibinSavePayload Payload()
        {
            var needs = new TruckTaxiDriverNeedsState(settings, 17);
            Assert.IsTrue(needs.CreateNewVibinInventory());
            return new TruckTaxiVibinSavePayload { regionId = "taxi.town01", session = new TruckTaxiPersistentState(), needs = needs.CapturePersistentState() };
        }
        private static LwsSaveParticipantState State(string id, object payload) => new LwsSaveParticipantState
        { participantId = id, payloadVersion = 1, payloadJson = JsonUtility.ToJson(payload) };
        private LwsSaveSnapshot Snapshot()
        {
            var snapshot = new LwsSaveSnapshot { profileId = "profile.test", capturedUtcTicks = 123 };
            snapshot.participants.Add(State(TruckTaxiVibinSaveParticipant.Id, Payload()));
            snapshot.participants.Add(State(LwsSaveSchema.WorldResumeContextParticipantId, new LwsWorldResumeContextPayload
            {
                stableWorldId = LwsWorldResumeCatalog.TruckTaxiWorldId, authoredSceneName = "TruckTaxi",
                hasGlobalPosition = true, rotationW = 1
            }));
            snapshot.participants.Add(State("lws.world.global-position", new LwsGlobalPositionSavePayload { rotationW = 1 }));
            snapshot.participants.Add(State("lws.vehicle.player-truck", new LwsPlayerTruckSavePayload
            { hasGlobalPosition = true, state = new LwsPlayerTruckState { pose = new LwsSerializablePose { rotation = Quaternion.identity } } }));
            snapshot.participants.Add(State("lws.game-clock", new LwsGameClockSavePayload
            { year = 2026, month = 9, day = 30, hour = 12, timeScale = 10 }));
            return snapshot;
        }
        private LwsSaveOperationResult Validate(LwsSaveSnapshot snapshot) =>
            TruckTaxiVibinSaveParticipant.ValidateSnapshot(snapshot, settings, out _, out _);

        [Test] public void RequiredCanonicalProvidersAndTaxiPayloadPassDetachedValidation()
        { var result = Validate(Snapshot()); Assert.IsTrue(result.Succeeded, result.Message); }

        [TestCase(-1f, 80f)]
        [TestCase(81f, 80f)]
        [TestCase(1f, 0f)]
        [TestCase(float.NaN, 80f)]
        public void InvalidSavedFuelIsRejected(float amount, float capacity)
        {
            var payload = Payload(); payload.hasFuelState = true;
            payload.fuelLiters = amount; payload.fuelCapacityLiters = capacity;
            Assert.IsFalse(TruckTaxiVibinSaveParticipant.ValidatePayload(payload, settings).Succeeded);
        }

        [Test] public void OptionalFuelPreservesOlderSavesAndExactNewQuantity()
        {
            var payload = Payload();
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.ValidatePayload(payload, settings).Succeeded);
            payload.hasFuelState = true; payload.fuelLiters = 34.4f; payload.fuelCapacityLiters = 80;
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.Read(State(TruckTaxiVibinSaveParticipant.Id, payload), out var copy).Succeeded);
            Assert.IsTrue(copy.hasFuelState); Assert.AreEqual(34.4f, copy.fuelLiters);
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.ValidatePayload(copy, settings).Succeeded);
        }

        [Test] public void PayloadRoundTripPreservesExactInventoryAndDoesNotGrantOnRestore()
        {
            var payload = Payload();
            payload.needs.Inventory[0].Count = 3;
            string before = JsonUtility.ToJson(payload);
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.ValidatePayload(payload, settings).Succeeded);
            Assert.AreEqual(before, JsonUtility.ToJson(payload), "Detached validation must not mutate the caller's snapshot.");
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.Read(State(TruckTaxiVibinSaveParticipant.Id, payload), out var copy).Succeeded);
            var restored = new TruckTaxiDriverNeedsState(settings, 22);
            Assert.IsTrue(restored.RestorePersistentState(copy.needs, out _));
            Assert.IsFalse(restored.CreateNewVibinInventory(), "Load must never issue another starter grant.");
            Assert.AreEqual(3, restored.CapturePersistentState().Inventory[0].Count);
            Assert.AreEqual(before, JsonUtility.ToJson(copy));
        }

        [Test] public void StarterGrantIsExactlyTenAndOncePerNewState()
        {
            var state = new TruckTaxiDriverNeedsState(settings, 17);
            Assert.IsTrue(state.CreateNewVibinInventory()); Assert.IsFalse(state.CreateNewVibinInventory());
            foreach (var entry in state.CapturePersistentState().Inventory) Assert.AreEqual(10, entry.Count);
        }

        [TestCase(TruckTaxiVibinSaveParticipant.Id)]
        [TestCase(LwsSaveSchema.WorldResumeContextParticipantId)]
        [TestCase("lws.world.global-position")]
        [TestCase("lws.vehicle.player-truck")]
        [TestCase("lws.game-clock")]
        public void MissingRequiredProviderFailsBeforeAnyApply(string id)
        { var snapshot = Snapshot(); snapshot.participants.RemoveAll(p => p.participantId == id); Assert.IsFalse(Validate(snapshot).Succeeded); }

        [Test] public void DuplicateParticipantAndFutureVersionFail()
        {
            var snapshot = Snapshot(); snapshot.participants.Add(snapshot.participants[0]);
            Assert.IsFalse(Validate(snapshot).Succeeded);
            snapshot = Snapshot(); snapshot.participants[0].payloadVersion = 2;
            Assert.IsFalse(Validate(snapshot).Succeeded);
        }

        [Test] public void InvalidNeedsNeverInvokesRestoreCallback()
        {
            int applies = 0;
            var payload = Payload(); payload.needs.Inventory[0].Count = -1;
            var participant = new TruckTaxiVibinSaveParticipant(() => payload,
                _ => { applies++; return LwsSaveOperationResult.Success(); },
                value => TruckTaxiVibinSaveParticipant.ValidatePayload(value, settings));
            Assert.IsFalse(participant.RestoreState(State(TruckTaxiVibinSaveParticipant.Id, payload)).Succeeded);
            Assert.AreEqual(0, applies);
        }

        [Test] public void MalformedPayloadAndInvalidClockFail()
        {
            var snapshot = Snapshot(); snapshot.participants[0].payloadJson = "{invalid";
            Assert.IsFalse(Validate(snapshot).Succeeded);
            snapshot = Snapshot(); snapshot.participants[4] = State("lws.game-clock", new LwsGameClockSavePayload
            { year = 2026, month = 2, day = 31, timeScale = 10 });
            Assert.IsFalse(Validate(snapshot).Succeeded);
        }

        [Test] public void CrossWorldAndInconsistentTruckPositionFail()
        {
            var snapshot = Snapshot();
            var resume = JsonUtility.FromJson<LwsWorldResumeContextPayload>(snapshot.participants[1].payloadJson);
            resume.stableWorldId = LwsSaveSchema.DefaultStableWorldId;
            snapshot.participants[1] = State(LwsSaveSchema.WorldResumeContextParticipantId, resume);
            Assert.IsFalse(Validate(snapshot).Succeeded);
            snapshot = Snapshot();
            var truck = JsonUtility.FromJson<LwsPlayerTruckSavePayload>(snapshot.participants[3].payloadJson);
            truck.globalX = 100;
            snapshot.participants[3] = State("lws.vehicle.player-truck", truck);
            Assert.IsFalse(Validate(snapshot).Succeeded);
        }

        [TestCase(TruckTaxiState.DrivingToPickup)]
        [TestCase(TruckTaxiState.PassengerBoarding)]
        [TestCase(TruckTaxiState.DrivingToDestination)]
        [TestCase(TruckTaxiState.PassengerExiting)]
        [TestCase(TruckTaxiState.AppreciationSequence)]
        public void ActiveFareIsNotSaveable(TruckTaxiState state)
        { Assert.IsTrue(TruckTaxiVibinPersistence.HasUnsafeFare(state)); Assert.AreEqual("FINISH CURRENT RIDE BEFORE SAVING.", TruckTaxiVibinPersistence.FinishRideMessage); }

        [TestCase(TruckTaxiState.Available)]
        [TestCase(TruckTaxiState.RideOffered)]
        [TestCase(TruckTaxiState.RideComplete)]
        public void IdleAndCompletedFareMaySave(TruckTaxiState state)
        { Assert.IsFalse(TruckTaxiVibinPersistence.HasUnsafeFare(state)); }

        [Test] public void ChangedSlotBetweenInspectionAndApplyDoesNotMatch()
        {
            var first = Snapshot(); var second = JsonUtility.FromJson<LwsSaveSnapshot>(JsonUtility.ToJson(first));
            Assert.IsTrue(TruckTaxiVibinPersistence.SameSnapshot(first, second));
            second.participants[0].payloadJson = "{}";
            Assert.IsFalse(TruckTaxiVibinPersistence.SameSnapshot(first, second));
        }

        [Test] public void ContinueRequestContainsOnlyMetadataAndRejectsInvalidSlots()
        {
            Assert.IsFalse(TruckTaxiVibinPersistence.SetContinueRequest("profile.test", 0));
            Assert.IsFalse(TruckTaxiVibinPersistence.HasContinueRequest);
            Assert.IsTrue(TruckTaxiVibinPersistence.SetContinueRequest("profile.test", 2));
            Assert.IsTrue(TruckTaxiVibinPersistence.HasContinueRequest);
            Assert.AreEqual(3, typeof(TruckTaxiVibinContinueRequest).GetProperties().Length);
            TruckTaxiVibinPersistence.ClearContinueRequest(); Assert.IsFalse(TruckTaxiVibinPersistence.HasContinueRequest);
        }

        [Test] public void CareerFacadeIsNeverReplacedByTaxiDiscovery()
        {
            var registry = new LwsServiceRegistry(); var career = new LwsSaveService();
            registry.Register<ILwsSaveService>(career);
            Assert.IsFalse(TruckTaxiVibinPersistence.GetOrRegisterSaveService(registry, out _).Succeeded);
            Assert.AreSame(career, registry.GetRequired<ILwsSaveService>());
            Assert.IsNull(TruckTaxiVibinPersistence.FindLatest(career));
        }

        [Test] public void BootstrapScopeGuardPermitsOnlyVibin()
        {
            Assert.IsFalse(TruckTaxiVibinPersistence.IsVibinScope(null));
            Assert.IsFalse(TruckTaxiVibinPersistence.IsVibinScope(new LwsSaveService()));
            Assert.IsFalse(TruckTaxiVibinPersistence.IsVibinScope(new LwsSaveService("another-mode")));
            Assert.IsTrue(TruckTaxiVibinPersistence.IsVibinScope(new LwsSaveService(TruckTaxiVibinPersistence.ScopeId)));
        }

        [Test] public void ScopedRegistryUsesCanonicalProvidersOnceWithoutFreightJobs()
        {
            var registry = new LwsServiceRegistry();
            var first = TruckTaxiVibinPersistence.GetOrRegisterSaveService(registry, out var saves);
            Assert.IsTrue(first.Succeeded, first.Message);
            try
            {
                Assert.IsTrue(TruckTaxiVibinPersistence.GetOrRegisterSaveService(registry, out var same).Succeeded);
                Assert.AreSame(saves, same);
                Assert.AreEqual(7, saves.Participants.Count);
                var ids = new System.Collections.Generic.HashSet<string>();
                foreach (var provider in saves.Participants) Assert.IsTrue(ids.Add(provider.ParticipantId));
                Assert.IsFalse(ids.Contains(LwsSaveSchema.ActiveJobParticipantId));
                CollectionAssert.IsSubsetOf(new[] { LwsSaveSchema.WorldResumeContextParticipantId,
                    "lws.world.global-position", "lws.vehicle.player-truck", "lws.game-clock" }, ids);
                Assert.IsTrue(saves.ValidateParticipants().Succeeded);
            }
            finally { registry.ShutdownAll(); }
        }

        [Test] public void OccupiedMetadataWithoutVendorSlotDoesNotAdvertiseASave()
        {
            var stale = new LwsManualSaveSlotMetadata { occupied = true, slotNumber = 1, savedUtcTicks = DateTime.UtcNow.Ticks };
            Assert.AreEqual("SLOT 1 - EMPTY", TruckTaxiMainMenu.FormatSlotLabel(stale, "SLOT 1", false));
            Assert.AreEqual("AUTOSAVE - EMPTY", TruckTaxiMainMenu.FormatSlotLabel(null, "AUTOSAVE", true));
        }

        [Test] public void OccupiedDirectoryAloneCannotEnableContinueDiscovery()
        {
            var registry = new LwsServiceRegistry();
            Assert.IsTrue(TruckTaxiVibinPersistence.GetOrRegisterSaveService(registry, out var saves).Succeeded);
            try
            {
                var slot = saves.GetSlotMetadata(saves.ActiveProfileId, 1);
                slot.occupied = true;
                Assert.IsFalse(TruckTaxiVibinPersistence.ExistsInVendorStore(saves, slot), "EditMode has no ready vendor runtime storage.");
                Assert.IsNull(TruckTaxiVibinPersistence.FindLatest(saves));
            }
            finally { registry.ShutdownAll(); }
        }

        [Test] public void ExistingSlotLabelUsesRealTimestampAndToleratesBadMetadataTicks()
        {
            var slot = new LwsManualSaveSlotMetadata { savedUtcTicks = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc).Ticks };
            string expected = "SLOT 2\n" + new DateTime(slot.savedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            Assert.AreEqual(expected, TruckTaxiMainMenu.FormatSlotLabel(slot, "SLOT 2", true));
            slot.savedUtcTicks = long.MaxValue;
            Assert.AreEqual("SLOT 2\nSAVED", TruckTaxiMainMenu.FormatSlotLabel(slot, "SLOT 2", true));
        }

        [Test] public void UninitializedPersistenceCannotGrantANewRunOrResume()
        {
            var owner = new GameObject("Vibin persistence not ready");
            try
            {
                var persistence = owner.AddComponent<TruckTaxiVibinPersistence>();
                Assert.IsTrue(persistence.ResumeBlocked); Assert.IsFalse(persistence.HasActiveRun);
                Assert.IsFalse(persistence.NewGame().Succeeded);
                Assert.IsFalse(persistence.NewGame().Succeeded);
                Assert.IsTrue(persistence.ResumeBlocked);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test] public void BootstrapRejectsResumeBeforePersistenceAndWorldAreReady()
        {
            float previousTimeScale = Time.timeScale;
            var owner = new GameObject("Vibin startup resume guard");
            try
            {
                var bootstrap = owner.AddComponent<TruckTaxiBootstrap>();
                bootstrap.SetPaused(false);
                Assert.IsTrue(bootstrap.Paused); Assert.IsFalse(bootstrap.CanResume);
                Assert.IsNotEmpty(bootstrap.ResumeBlockReason);
                bootstrap.StartShift();
                Assert.IsTrue(bootstrap.Paused);
                Assert.AreEqual(0, Time.timeScale);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); Time.timeScale = previousTimeScale; }
        }

        [Test] public void SelectedProfileCannotOwnAnotherRunsManualSaveOrAutosave()
        {
            var registry = new LwsServiceRegistry();
            Assert.IsTrue(TruckTaxiVibinPersistence.GetOrRegisterSaveService(registry, out var saves).Succeeded);
            var owner = new GameObject("Vibin profile ownership");
            try
            {
                var persistence = owner.AddComponent<TruckTaxiVibinPersistence>();
                string runProfile = saves.ActiveProfileId;
                typeof(TruckTaxiVibinPersistence).GetProperty(nameof(TruckTaxiVibinPersistence.SaveService)).SetValue(persistence, saves);
                typeof(TruckTaxiVibinPersistence).GetProperty(nameof(TruckTaxiVibinPersistence.RunProfileId)).SetValue(persistence, runProfile);
                var method = typeof(TruckTaxiVibinPersistence).GetMethod("SaveBlockReason",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                saves.AdditionalSaveBlockReason = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), persistence, method);
                Assert.IsTrue(saves.CreateProfile("Different run", out var other).Succeeded);
                Assert.IsTrue(saves.SelectProfile(other.stableProfileId).Succeeded);
                Assert.AreEqual(runProfile, persistence.RunProfileId);
                Assert.AreEqual(TruckTaxiVibinPersistence.ProfileMismatchMessage, saves.AdditionalSaveBlockReason());
                Assert.AreEqual(TruckTaxiVibinPersistence.ProfileMismatchMessage, persistence.Save(1, true).Message);
                Assert.IsFalse(saves.Save(other.stableProfileId, 1, true).Succeeded);
                saves.RequestAutosave("selection changed");
                Assert.IsTrue(saves.PendingAutosave);
                Assert.IsFalse(saves.GetSlotMetadata(other.stableProfileId, 1).occupied);
                Assert.IsFalse(saves.GetAutosaveSlotMetadata(other.stableProfileId).occupied);
                Assert.IsNull(TruckTaxiVibinPersistence.ProfileWriteBlockReason(runProfile, runProfile));
                Assert.IsNotNull(TruckTaxiVibinPersistence.ProfileWriteBlockReason(null, other.stableProfileId));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); registry.ShutdownAll(); }
        }

        [Test] public void ContinueFailureAndRetryKeepMetadataUntilExplicitCancellation()
        {
            TruckTaxiVibinPersistence.SetContinueRequest("profile.retry", 2);
            var owner = new GameObject("Vibin retry request");
            try
            {
                var persistence = owner.AddComponent<TruckTaxiVibinPersistence>();
                Assert.IsFalse(persistence.ConsumeContinueRequest().Succeeded);
                Assert.IsTrue(TruckTaxiVibinPersistence.HasContinueRequest);
                Assert.AreEqual("profile.retry", TruckTaxiVibinPersistence.PendingContinueRequest.Value.ProfileId);
                Assert.AreEqual(2, TruckTaxiVibinPersistence.PendingContinueRequest.Value.Slot);
                Assert.IsFalse(persistence.RetryContinueRequest().Succeeded);
                Assert.IsTrue(TruckTaxiVibinPersistence.HasContinueRequest);
                TruckTaxiVibinPersistence.ClearContinueRequest();
                Assert.IsFalse(TruckTaxiVibinPersistence.HasContinueRequest);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test] public void OnlyMatchingTerminalSuccessClearsContinueMetadata()
        {
            TruckTaxiVibinPersistence.SetContinueRequest("profile.retry", 2);
            var complete = typeof(TruckTaxiVibinPersistence).GetMethod("CompleteContinueRequest",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            complete.Invoke(null, new object[] { "profile.other", 2, false });
            Assert.IsTrue(TruckTaxiVibinPersistence.HasContinueRequest);
            complete.Invoke(null, new object[] { "profile.retry", 1, false });
            Assert.IsTrue(TruckTaxiVibinPersistence.HasContinueRequest);
            complete.Invoke(null, new object[] { "profile.retry", 2, false });
            Assert.IsFalse(TruckTaxiVibinPersistence.HasContinueRequest);
        }

        [Test] public void RepeatedWorkAreaPreflightDoesNotMutateOrDoubleRebaseSource()
        {
            var source = new TruckTaxiPersistentState();
            source.workArea.centerX = 10; source.workArea.centerY = 20; source.workArea.centerZ = 30;
            var resume = new LwsWorldResumeContextPayload { savedGlobalX = 1000, savedGlobalY = 20, savedGlobalZ = 3000, localY = 20 };
            Func<LwsWorldPositionD, Vector3> toLocal = position => new Vector3((float)(position.x - 500), (float)position.y, (float)(position.z - 1500));
            Assert.IsTrue(TruckTaxiVibinPersistence.TryRebaseWorkArea(source, resume, toLocal, out var first));
            Assert.IsTrue(TruckTaxiVibinPersistence.TryRebaseWorkArea(source, resume, toLocal, out var second));
            Assert.AreEqual(10, source.workArea.centerX); Assert.AreEqual(30, source.workArea.centerZ);
            Assert.AreEqual(510, first.workArea.centerX); Assert.AreEqual(1530, first.workArea.centerZ);
            Assert.AreEqual(first.workArea.centerX, second.workArea.centerX);
            Assert.AreEqual(first.workArea.centerZ, second.workArea.centerZ);
            Assert.AreNotSame(source.workArea, first.workArea);
        }

        [Test] public void ValidHalfTurnQuaternionPassesSnapshotPreflight()
        {
            var snapshot = Snapshot();
            var resume = JsonUtility.FromJson<LwsWorldResumeContextPayload>(snapshot.participants[1].payloadJson);
            resume.rotationY = 1; resume.rotationW = 0;
            snapshot.participants[1] = State(LwsSaveSchema.WorldResumeContextParticipantId, resume);
            snapshot.participants[2] = State("lws.world.global-position", new LwsGlobalPositionSavePayload { rotationY = 1, rotationW = 0 });
            var truck = JsonUtility.FromJson<LwsPlayerTruckSavePayload>(snapshot.participants[3].payloadJson);
            truck.state.pose.rotation = new Quaternion(0, 1, 0, 0);
            snapshot.participants[3] = State("lws.vehicle.player-truck", truck);
            var result = Validate(snapshot);
            Assert.IsTrue(result.Succeeded, result.Message);
        }
    }
}
