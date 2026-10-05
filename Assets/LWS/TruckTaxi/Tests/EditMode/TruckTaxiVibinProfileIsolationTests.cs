using System;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;

namespace LWS.TruckTaxi.Tests.EditMode
{
    [Category("TaxiPersistence")]
    public sealed class TruckTaxiVibinProfileIsolationTests
    {
        private const string OwnedId = "profile.test.vibin";
        private const string ForeignId = "profile.test.career";
        private LwsSaveService saves;
        private LwsSaveProfileDirectory directory;
        private MutationSpy[] participants;

        [SetUp]
        public void SetUp()
        {
            // Never initialize: no vendor adapter, runtime driver, registry, or disk storage.
            saves = new LwsSaveService(TruckTaxiVibinPersistence.ScopeId, includeDefaultParticipants: false);
            Assert.IsNull(saves.Adapter);
            Assert.IsEmpty(saves.Participants);
            directory = new LwsSaveProfileDirectory { selectedProfileId = OwnedId, nextProfileIndex = 2 };
            directory.profiles.Add(Profile(OwnedId, TruckTaxiVibinPersistence.ScopeId, 0));
            directory.profiles.Add(Profile(ForeignId, string.Empty, 1));
            // Seed the shared directory directly so a real foreign entry exists without CreateProfile writes.
            var field = typeof(LwsSaveService).GetField("_directory", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(saves, directory);
            Assert.AreEqual(OwnedId, saves.ActiveProfileId);
            participants = new[]
            {
                new MutationSpy("lws.vehicle.player-truck"),
                new MutationSpy(TruckTaxiVibinSaveParticipant.Id)
            };
            foreach (var participant in participants)
            {
                var result = saves.RegisterParticipant(participant);
                Assert.IsTrue(result.Succeeded, result.Message);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("profile.test.absent")]
        [TestCase("PROFILE.TEST.VIBIN")]
        [TestCase(ForeignId)]
        public void RestoreSnapshot_MissingOrForeignProfileRejectsBeforeAnyParticipantMutation(string profileId)
        {
            Assert.IsNotNull(directory.FindProfile(ForeignId), "The foreign profile must exist, not merely be unknown.");
            var result = saves.RestoreSnapshot(Snapshot(profileId));
            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains("gameplay scope", result.Message);
            Assert.AreEqual(OwnedId, saves.ActiveProfileId);
            AssertParticipantsUntouched();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(LwsSaveSchema.CurrentVersion + 1)]
        public void RestoreSnapshot_UnsupportedSchemaRejectsBeforeSelectionOrParticipantMutation(int version)
        {
            var snapshot = Snapshot(OwnedId); snapshot.schemaVersion = version;
            var result = saves.RestoreSnapshot(snapshot);
            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains("schema", result.Message);
            Assert.Zero(directory.FindProfile(OwnedId).lastPlayedUtcTicks);
            AssertParticipantsUntouched();
        }

        [Test]
        public void RestoreSnapshot_ProfileSelectionFailureStopsBeforeParticipantMutation()
        {
            var result = saves.RestoreSnapshot(Snapshot(OwnedId));
            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains("adapter is missing", result.Message);
            Assert.AreEqual(OwnedId, saves.ActiveProfileId);
            AssertParticipantsUntouched();
        }

        [TestCase(false, null)]
        [TestCase(true, null)]
        [TestCase(true, "")]
        [TestCase(true, "TRUCK-TAXI")]
        [TestCase(true, "truck-taxi ")]
        [TestCase(true, "another-gameplay-scope")]
        public void InspectVendorSlot_NullOrForeignProfileRejectsBeforeVendorRead(bool hasProfile, string scope)
        {
            int registryReads = 0, snapshotValidations = 0;
            var coordinator = new LwsSaveLoadCoordinator(saves, () => { registryReads++; return null; });
            coordinator.AdditionalSnapshotValidation = _ =>
            {
                snapshotValidations++;
                return LwsSaveOperationResult.Success();
            };
            // Reuse the owned ID to prove metadata scope is checked, not just the ID or slot number.
            var profile = hasProfile ? Profile(OwnedId, scope, 0) : null;
            var result = default(LwsSaveOperationResult);
            LwsLoadApplicationContext context = null;
            // Scope rejection must precede even the unavailable-adapter check.
            Assert.DoesNotThrow(() => result = coordinator.InspectVendorSlot(profile, LwsSaveSchema.FirstProfileSlotBase,
                LwsSaveLoadSlotKind.ManualPrimary, out context));
            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains("scope", result.Message);
            Assert.IsNull(context); Assert.IsNull(coordinator.CurrentContext);
            Assert.AreEqual(LwsSaveLoadCoordinatorPhase.Idle, coordinator.Phase);
            Assert.Zero(registryReads); Assert.Zero(snapshotValidations);
            Assert.AreEqual(OwnedId, saves.ActiveProfileId);
            AssertParticipantsUntouched();
        }

        [Test]
        public void InspectVendorSlot_UnknownProfileInOwnedScopeRejectsBeforeVendorRead()
        {
            var coordinator = new LwsSaveLoadCoordinator(saves, () => null);
            var unknown = Profile("profile.test.absent", TruckTaxiVibinPersistence.ScopeId, 2);
            var result = coordinator.InspectVendorSlot(unknown, LwsSaveSchema.FirstProfileSlotBase,
                LwsSaveLoadSlotKind.ManualPrimary, out var context);
            Assert.IsFalse(result.Succeeded); StringAssert.Contains("scope", result.Message);
            Assert.IsNull(context); Assert.IsNull(coordinator.CurrentContext);
            AssertParticipantsUntouched();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InspectVendorSlot_NullServiceRejectsWithoutAccessingRegistryOrVendor(bool hasProfile)
        {
            int registryReads = 0;
            var coordinator = new LwsSaveLoadCoordinator(null, () => { registryReads++; return null; });
            var profile = hasProfile ? directory.FindProfile(OwnedId) : null;
            var result = default(LwsSaveOperationResult);
            LwsLoadApplicationContext context = null;
            Assert.DoesNotThrow(() => result = coordinator.InspectVendorSlot(profile, LwsSaveSchema.FirstProfileSlotBase,
                LwsSaveLoadSlotKind.ManualPrimary, out context));
            Assert.IsFalse(result.Succeeded); Assert.IsNotEmpty(result.Message);
            Assert.IsNull(context); Assert.IsNull(coordinator.CurrentContext);
            Assert.AreEqual(LwsSaveLoadCoordinatorPhase.Idle, coordinator.Phase);
            Assert.Zero(registryReads); AssertParticipantsUntouched();
        }

        [Test]
        public void InspectVendorSlot_KnownOwnedProfileReachesMissingAdapterGuardWithoutVendorAccess()
        {
            var coordinator = new LwsSaveLoadCoordinator(saves, () => null);
            var result = coordinator.InspectVendorSlot(directory.FindProfile(OwnedId), LwsSaveSchema.FirstProfileSlotBase,
                LwsSaveLoadSlotKind.ManualPrimary, out var context);
            Assert.IsFalse(result.Succeeded); StringAssert.Contains("adapter", result.Message);
            Assert.IsNull(context); Assert.IsNull(coordinator.CurrentContext);
            AssertParticipantsUntouched();
        }

        [TestCase(0f, 0f, 0f, 1f)]
        [TestCase(1f, 0f, 0f, 0f)]
        [TestCase(0f, 1f, 0f, 0f)]
        [TestCase(0f, 0f, 1f, 0f)]
        [TestCase(-1f, 0f, 0f, 0f)]
        [TestCase(.5f, .5f, .5f, .5f)]
        [TestCase(.00001f, 0f, 0f, 0f)]
        [TestCase(.0000006f, .0000006f, .0000006f, .0000006f)]
        public void GlobalPositionPayload_FiniteQuaternionAboveSquaredCutoffIsValidIncludingHalfTurns(
            float x, float y, float z, float w)
        {
            var payload = GlobalPayload(x, y, z, w);
            Assert.IsTrue(payload.IsValid, "A zero W component is valid when the full quaternion has sufficient magnitude.");
        }

        [TestCase(0f, 0f, 0f, 0f)]
        [TestCase(.0000001f, 0f, 0f, 0f)]
        [TestCase(0f, 0f, 0f, .0000001f)]
        [TestCase(.0000001f, .0000001f, .0000001f, .0000001f)]
        public void GlobalPositionPayload_ZeroOrSubthresholdQuaternionIsInvalid(float x, float y, float z, float w)
        {
            Assert.IsFalse(GlobalPayload(x, y, z, w).IsValid);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void GlobalPositionPayload_RejectsNonfiniteValueInEveryQuaternionComponent(float nonfinite)
        {
            for (int component = 0; component < 4; component++)
            {
                var payload = GlobalPayload(0, 0, 0, 1);
                switch (component)
                {
                    case 0: payload.rotationX = nonfinite; break;
                    case 1: payload.rotationY = nonfinite; break;
                    case 2: payload.rotationZ = nonfinite; break;
                    case 3: payload.rotationW = nonfinite; break;
                }
                Assert.IsFalse(payload.IsValid, "Nonfinite quaternion component " + component);
            }
        }

        private static LwsGlobalPositionSavePayload GlobalPayload(float x, float y, float z, float w) =>
            new LwsGlobalPositionSavePayload
            { globalX = 100, globalY = 5, globalZ = -200, rotationX = x, rotationY = y, rotationZ = z, rotationW = w };

        private LwsSaveSnapshot Snapshot(string profileId)
        {
            var snapshot = new LwsSaveSnapshot { profileId = profileId, schemaVersion = LwsSaveSchema.CurrentVersion };
            foreach (var participant in participants)
                snapshot.participants.Add(new LwsSaveParticipantState
                {
                    participantId = participant.ParticipantId, payloadVersion = participant.PayloadVersion,
                    payloadJson = "{\"value\":99}"
                });
            return snapshot;
        }

        private void AssertParticipantsUntouched()
        {
            Assert.IsNull(saves.Adapter, "The fixture must never initialize vendor persistence.");
            foreach (var participant in participants)
            {
                Assert.Zero(participant.RestoreCalls, participant.ParticipantId);
                Assert.Zero(participant.CaptureCalls, participant.ParticipantId);
                Assert.Zero(participant.ClearCalls, participant.ParticipantId);
                Assert.AreEqual(17, participant.Value, participant.ParticipantId);
            }
        }

        private static LwsSaveProfileMetadata Profile(string id, string scope, int index) => new LwsSaveProfileMetadata
        { stableProfileId = id, profileScopeId = scope, profileIndex = index, displayName = "Isolation test" };

        private sealed class MutationSpy : ILwsSaveParticipant
        {
            public string ParticipantId { get; }
            public int PayloadVersion => 1;
            public int RestoreCalls { get; private set; }
            public int CaptureCalls { get; private set; }
            public int ClearCalls { get; private set; }
            public int Value { get; private set; } = 17;

            public MutationSpy(string id) { ParticipantId = id; }
            public LwsSaveParticipantState CaptureState()
            {
                CaptureCalls++;
                throw new InvalidOperationException("Profile guard tests must not capture a save.");
            }
            public LwsSaveOperationResult RestoreState(LwsSaveParticipantState state)
            {
                RestoreCalls++; Value = 99;
                return LwsSaveOperationResult.Success();
            }
            public LwsSaveOperationResult ClearState()
            {
                ClearCalls++; Value = 0;
                return LwsSaveOperationResult.Success();
            }
            public LwsSaveOperationResult ValidateParticipant() => LwsSaveOperationResult.Success();
        }
    }
}
