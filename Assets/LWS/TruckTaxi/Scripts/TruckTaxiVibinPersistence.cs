using System;
using System.Collections;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    public readonly struct TruckTaxiVibinContinueRequest
    {
        public string ProfileId { get; }
        public int Slot { get; }
        public bool Autosave { get; }
        public TruckTaxiVibinContinueRequest(string profileId, int slot, bool autosave)
        { ProfileId = profileId; Slot = slot; Autosave = autosave; }
    }

    [DisallowMultipleComponent]
    public sealed partial class TruckTaxiVibinPersistence : MonoBehaviour
    {
        public const string ScopeId = "truck-taxi";
        public const string FinishRideMessage = "FINISH CURRENT RIDE BEFORE SAVING.";
        public const string NoSaveMessage = "NO VIBIN' SAVE";
        public const string ProfileMismatchMessage = "Load the selected profile before saving; it does not own this Vibin run.";
        private static TruckTaxiVibinContinueRequest? continueRequest;
        private static readonly Func<LwsLoadApplicationContext, LwsSaveOperationResult> UnboundLoadGate =
            _ => LwsSaveOperationResult.Failure("Vibin gameplay is not initialized.");
        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld regional;
        private LwsServiceRegistry registry;
        private ILwsWorldOriginService origin;
        private TruckTaxiVibinSaveParticipant participant;
        private Rigidbody body;
        private bool profileReady, newGameStarted, boundaryPending, permitApply, applied, loadFailed, loadAttempted;
        private int completedRides;
        private long purchases, services;
        private string region;
        private LwsLoadApplicationContext prepared;
        private Vector3 preparedPosition;
        private LwsSaveOperationResult restoreResult;

        public LwsSaveService SaveService { get; private set; }
        public bool Initialized => participant != null;
        public bool IsBusy { get; private set; }
        public string Status { get; private set; } = "Vibin persistence is not initialized.";
        public LwsSaveOperationResult LastResult { get; private set; }
        public event Action<LwsSaveOperationResult> OperationCompleted;
        public event Action<LwsSaveOperationResult> LoadCompleted;
        public IReadOnlyList<LwsSaveProfileMetadata> Profiles => SaveService?.Profiles ?? Array.Empty<LwsSaveProfileMetadata>();
        public LwsSaveRecoveryOffer RecoveryOffer => SaveService?.RecoveryOffer;
        public LwsManualSaveSlotMetadata Latest => FindLatest(SaveService);
        public bool HasSave => Latest != null;
        public string RunProfileId { get; private set; }
        public bool HasActiveRun => profileReady && !string.IsNullOrEmpty(RunProfileId);
        public bool ResumeBlocked => !Initialized || !HasActiveRun || IsBusy || loadFailed;
        public string ResumeBlockReason => ResumeBlocked ? Status : string.Empty;
        public static bool HasContinueRequest => continueRequest.HasValue;
        public static TruckTaxiVibinContinueRequest? PendingContinueRequest => continueRequest;
        public static bool IsVibinScope(ILwsSaveService saves) => saves is LwsSaveService scoped && scoped.ProfileScopeId == ScopeId;

        // Menu discovery may register this same scoped facade before the Taxi scene exists.
        // It never constructs an independent registry or replaces an Interstate career facade.
        public static LwsSaveOperationResult GetOrRegisterSaveService(LwsServiceRegistry services, out LwsSaveService saves)
        {
            saves = null;
            if (TruckTaxiRestartHarness.Requested && !TruckTaxiRestartHarness.Configured)
                return LwsSaveOperationResult.Failure("Isolated restart validation is not configured: " + TruckTaxiRestartHarness.Failure);
            if (services == null) return LwsSaveOperationResult.Failure("LWS services are unavailable.");
            if (services.TryGet(out ILwsSaveService existing))
            {
                saves = existing as LwsSaveService;
                if (!IsVibinScope(saves))
                { saves = null; return LwsSaveOperationResult.Failure("A different save scope already owns this registry."); }
            }
            else
            {
                // Career defaults include a freight-job participant that cannot initialize in
                // the Taxi sandbox. Reuse the applicable canonical providers, once each.
                saves = new LwsSaveService(ScopeId, false);
                saves.AdditionalSaveBlockReason = () => "Vibin gameplay is not initialized.";
                services.Register<ILwsSaveService>(saves);
            }
            var providers = RegisterCanonicalProviders(saves, services);
            if (!providers.Succeeded) return providers;
            var result = services.InitializeRegistered<ILwsSaveService>();
            if (result.Succeeded && saves.LoadCoordinator.AdditionalWorldReadiness == null)
                saves.LoadCoordinator.AdditionalWorldReadiness = UnboundLoadGate;
            return result.Succeeded ? LwsSaveOperationResult.Success() : LwsSaveOperationResult.Failure(result.Message);
        }

        private static LwsSaveOperationResult RegisterCanonicalProviders(LwsSaveService saves, LwsServiceRegistry services)
        {
            ILwsSaveParticipant[] canonical = {
                new LwsWorldResumeSaveParticipant(() => services), new LwsGlobalPositionSaveParticipant(() => services),
                new LwsPlayerTruckSaveParticipant(() => services), new LwsGameClockSaveParticipant(() => services),
                new LwsWeatherSaveParticipant(() => services), new LwsRoadConditionSaveParticipant(() => services),
                new LwsNavigationSaveParticipant(() => services)
            };
            foreach (var provider in canonical)
            {
                ILwsSaveParticipant found = null;
                foreach (var existing in saves.Participants) if (existing.ParticipantId == provider.ParticipantId) found = existing;
                if (found != null)
                {
                    if (found.GetType() != provider.GetType()) return LwsSaveOperationResult.Failure("A different provider owns a canonical Vibin save ID.");
                    continue;
                }
                var result = saves.RegisterParticipant(provider);
                if (!result.Succeeded) return result;
            }
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult Initialize(TruckTaxiBootstrap bootstrap)
        {
            if (Initialized) return host == bootstrap ? LwsSaveOperationResult.Success() : Fail("Persistence already has an owner.");
            if (bootstrap == null || bootstrap.Session == null || bootstrap.DriverNeeds?.State == null ||
                bootstrap.Environment?.settings == null || bootstrap.Player == null)
                return Fail("Initialize Vibin persistence after session, needs, clock and player services.");
            host = bootstrap;
            regional = host.GetComponent<TruckTaxiRegionalWorld>();
            registry = LwsApplicationBootstrap.Instance?.Registry;
            if (regional == null || registry == null || !registry.TryGet(out origin)) return Fail("Regional world/origin services are unavailable.");
            if (!registry.TryGet(out ILwsPlayerVehicleService vehicles) || vehicles.ActiveTruck != host.Player ||
                !registry.TryGet(out ILwsGameClockService clock) || !ReferenceEquals(clock, host.Environment.Clock))
                return Fail("Vibin must use the registry's canonical truck and game clock.");
            body = host.Player.GetComponent<Rigidbody>();
            if (body == null) return Fail("Canonical player rigidbody is unavailable.");
            var setup = GetOrRegisterSaveService(registry, out var saves);
            if (!setup.Succeeded) return Fail(setup.Message);
            SaveService = saves;
            foreach (var existing in saves.Participants)
                if (existing.ParticipantId == TruckTaxiVibinSaveParticipant.Id) return Fail("A Vibin participant is already registered.");
            if (saves.LoadCoordinator.AdditionalSnapshotValidation != null ||
                saves.LoadCoordinator.AdditionalWorldReadiness != null && saves.LoadCoordinator.AdditionalWorldReadiness != UnboundLoadGate ||
                saves.LoadCoordinator.ResumeOriginOffset != null) return Fail("Scoped load hooks already have an owner.");
            participant = new TruckTaxiVibinSaveParticipant(Capture, Restore, ValidatePayload);
            saves.RegisterParticipant(participant);
            saves.AdditionalSaveBlockReason = SaveBlockReason;
            saves.LoadCoordinator.AdditionalSnapshotValidation = ValidateSnapshot;
            saves.LoadCoordinator.AdditionalWorldReadiness = WorldReady;
            saves.LoadCoordinator.ResumeOriginOffset = PreserveOrigin;
            host.Session.Changed += SessionChanged;
            regional.AvailabilityChanged += RegionChanged;
            host.DriverNeeds.DriverNeedsEvent += NeedsBoundary;
            host.DriverNeeds.SleepCompleted += SleepBoundary;
            if (host.Fuel != null) host.Fuel.Rescued += ServiceBoundary;
            RememberCounters();
            region = regional.ResolveRegion(host.Player.transform.position);
            Status = "Vibin persistence ready; select New Game or Load.";
            return LwsSaveOperationResult.Success(Status);
        }

        public IReadOnlyList<LwsManualSaveSlotMetadata> GetManualSlots(string profileId = null) =>
            SaveService?.GetManualSlots(profileId) ?? Array.Empty<LwsManualSaveSlotMetadata>();
        public LwsManualSaveSlotMetadata GetAutosaveSlot(string profileId = null) => SaveService?.GetAutosaveSlotMetadata(profileId);

        public static LwsManualSaveSlotMetadata FindLatest(ILwsSaveService saves)
        {
            LwsManualSaveSlotMetadata latest = null;
            if (!IsVibinScope(saves)) return null;
            foreach (var profile in saves.Profiles)
            {
                foreach (var slot in saves.GetManualSlots(profile.stableProfileId))
                    if (ExistsInVendorStore(saves, slot) && (latest == null || slot.savedUtcTicks > latest.savedUtcTicks)) latest = slot;
                var automatic = saves.GetAutosaveSlotMetadata(profile.stableProfileId);
                if (ExistsInVendorStore(saves, automatic) && (latest == null || automatic.savedUtcTicks > latest.savedUtcTicks)) latest = automatic;
            }
            return latest;
        }
        public static bool ExistsInVendorStore(ILwsSaveService saves, LwsManualSaveSlotMetadata slot) =>
            IsVibinScope(saves) && slot != null && saves.Adapter?.ReadyForRuntimeSaves == true &&
            (slot.slotType == LwsSaveSlotType.Manual || slot.slotType == LwsSaveSlotType.Autosave) &&
            saves.Adapter.HasSaveInSlot(slot.vendorSlotNumber);

        public static bool SetContinueRequest(string profileId, int slot, bool autosave = false)
        {
            if (string.IsNullOrWhiteSpace(profileId) || (!autosave && (slot < 1 || slot > LwsSaveSchema.ManualSlotCount))) return false;
            continueRequest = new TruckTaxiVibinContinueRequest(profileId, autosave ? LwsSaveSchema.AutosaveSlotNumber : slot, autosave);
            return true;
        }
        public static void ClearContinueRequest() => continueRequest = null;
        public LwsSaveOperationResult ConsumeContinueRequest()
        {
            if (!continueRequest.HasValue) return Fail(NoSaveMessage);
            var request = continueRequest.Value;
            return request.Autosave ? LoadAutosave(request.ProfileId) : Load(request.ProfileId, request.Slot);
        }
        public LwsSaveOperationResult RetryContinueRequest() => ConsumeContinueRequest();
        private static void CompleteContinueRequest(string profileId, int slot, bool autosave)
        {
            if (!continueRequest.HasValue) return;
            var request = continueRequest.Value;
            if (request.ProfileId == profileId && request.Slot == slot && request.Autosave == autosave)
                continueRequest = null;
        }

        // New Game is called on a fresh Taxi bootstrap. Never grant starter stock during load
        // or initialize, and never turn an existing restored run into a new profile accidentally.
        public LwsSaveOperationResult NewGame(string displayName = "VIBIN'")
        {
            if (!Initialized || IsBusy || profileReady || newGameStarted || loadAttempted || HasContinueRequest)
                return Fail("Start New Game from a fresh Taxi scene with no pending Continue request.");
            string blocked = UnsafeGameplayReason();
            if (blocked != null) return Fail(blocked);
            var session = host.Session.CapturePersistentState();
            var needs = host.DriverNeeds.CapturePersistentState();
            if (session.shiftEarningsCents != 0 || session.purchaseChargesCents != 0 || session.serviceChargesCents != 0 ||
                session.passengerSelection.offerIndex != 0 || needs.StarterInventoryGranted)
                return Fail("Start New Game before gameplay or an inventory restore.");
            var result = SaveService.CreateProfile(displayName, out var profile);
            if (!result.Succeeded) return Complete(result);
            if (!host.DriverNeeds.CreateNewVibinInventory()) return Fail("Starter inventory was already initialized; no second grant was made.");
            var selected = SaveService.SelectProfile(profile.stableProfileId);
            if (!selected.Succeeded) return Complete(selected);
            RunProfileId = profile.stableProfileId;
            if (!host.Session.BindCareerRun(RunProfileId, out string careerError) ||
                !host.Session.TryInitializeCareer("Driver", host.Player.DefinitionId, out careerError))
                return Fail("Career initialization failed: " + careerError);
            newGameStarted = profileReady = true;
            host.Session.StartShift();
            RememberCounters();
            boundaryPending = true;
            return Complete(LwsSaveOperationResult.Success("New Vibin profile created."));
        }

        public LwsSaveOperationResult Save(int slot = 1, bool overwrite = false)
        {
            string blocked = SaveBlockReason();
            if (blocked != null) return Fail(blocked);
            if (slot < 1 || slot > LwsSaveSchema.ManualSlotCount) return Fail("Invalid manual slot.");
            return Complete(SaveService.Save(RunProfileId, slot, overwrite));
        }
        public LwsSaveOperationResult RequestAutosave(string reason = "Vibin boundary") => Initialized && profileReady
            ? SaveService.RequestAutosave(reason) : LwsSaveOperationResult.Failure("No active Vibin run.");
        public LwsSaveOperationResult SaveBeforeLeaving()
        {
            string blocked = SaveBlockReason();
            if (blocked != null) return Fail(blocked);
            // Scene teardown cannot leave a throttled autosave waiting for a later frame.
            return Complete(SaveService.SaveAutosave(RunProfileId, "Return to main menu"));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveAtApplicationBoundary("Application suspended");
        }
        private void SaveAtApplicationBoundary(string reason)
        {
            if (!HasActiveRun || ResumeBlocked || IsBusy || SaveService == null) return;
            if (SaveBlockReason() == null)
                Complete(SaveService.SaveAutosave(RunProfileId, reason));
            else RequestAutosave(reason);
            // Unsupported in-flight semantics remain deferred, never force-written on suspend.
        }
        public LwsSaveOperationResult Load(string profileId, int slot = 1) => BeginLoad(profileId, slot, false, false);
        public LwsSaveOperationResult LoadAutosave(string profileId = null) => BeginLoad(profileId ?? SaveService?.ActiveProfileId, 1, true, false);
        public LwsSaveOperationResult LoadLatest()
        {
            var latest = Latest;
            return latest == null ? Fail(NoSaveMessage) : BeginLoad(latest.stableProfileId, latest.slotNumber, latest.slotType == LwsSaveSlotType.Autosave, false);
        }
        // Invoke only after the player explicitly confirms the facade's RecoveryOffer.
        public LwsSaveOperationResult LoadRecoveryBackup()
        {
            var offer = RecoveryOffer;
            return offer == null || !offer.available ? Fail("No confirmed recovery backup is available.") :
                BeginLoad(offer.profileId, offer.primarySlotNumber, offer.primarySlotKind == LwsSaveLoadSlotKind.AutosavePrimary, true);
        }

        private LwsSaveOperationResult BeginLoad(string profileId, int slot, bool autosave, bool backup)
        {
            if (IsBusy) return LwsSaveOperationResult.Failure("Vibin load preparation is already active.");
            if (!Initialized || !isActiveAndEnabled || !Application.isPlaying) return Fail("Vibin loading is not ready.");
            loadAttempted = true;
            host.SetPaused(true);
            string blocked = UnsafeGameplayReason(true);
            if (blocked != null) return Fail(blocked);
            if (!autosave && (slot < 1 || slot > LwsSaveSchema.ManualSlotCount)) return Fail("Invalid manual slot.");
            LwsSaveProfileMetadata profile = null;
            foreach (var candidate in Profiles) if (candidate.stableProfileId == profileId) profile = candidate;
            if (profile == null) return Fail(NoSaveMessage);
            var metadata = autosave ? SaveService.GetAutosaveSlotMetadata(profileId) : SaveService.GetSlotMetadata(profileId, slot);
            if (metadata == null || !backup && !ExistsInVendorStore(SaveService, metadata)) return Fail(NoSaveMessage);
            int vendorSlot = backup ? RecoveryOffer.backupVendorSlotNumber : metadata.vendorSlotNumber;
            var kind = backup ? RecoveryOffer.backupSlotKind : autosave ? LwsSaveLoadSlotKind.AutosavePrimary : LwsSaveLoadSlotKind.ManualPrimary;
            loadFailed = true;
            var inspect = SaveService.LoadCoordinator.InspectVendorSlot(profile, vendorSlot, kind, out var context);
            if (!inspect.Succeeded)
            {
                // Re-enter the facade's own preread failure path to offer its validated backup.
                // The world-readiness gate remains closed, so a changed slot cannot apply here.
                InvokeFacade(profileId, slot, autosave, backup);
                return Fail(inspect.Message);
            }
            IsBusy = true;
            Status = "Preparing saved Vibin region.";
            StartCoroutine(LoadRoutine(context, profileId, slot, autosave, backup));
            return LwsSaveOperationResult.Success("Vibin load preparation started.");
        }

        private IEnumerator LoadRoutine(LwsLoadApplicationContext context, string profileId, int slot, bool autosave, bool backup)
        {
            var outcome = LwsSaveOperationResult.Failure("Vibin load preparation was interrupted.");
            try
            {
                var position = origin.GlobalToLocal(context.ResumeContext.SavedGlobalPosition);
                yield return regional.PrepareInitialWorld(position);
                Physics.SyncTransforms();
                if (!regional.InitialWorldReady || !regional.IsPositionAvailable(position) ||
                    !TruckTaxiRecoverySpace.TryGroundedPose(body, position, context.ResumeContext.SavedRotation, .3f, body, null, out preparedPosition))
                { outcome = LwsSaveOperationResult.Failure("Saved location has no ready ground and clearance. Load was not applied."); yield break; }
                prepared = context;
                permitApply = true;
                applied = false;
                restoreResult = LwsSaveOperationResult.Failure("Vibin semantic restore did not acknowledge application.");
                var result = InvokeFacade(profileId, slot, autosave, backup);
                if (!result.Succeeded) { outcome = result; yield break; }
                float deadline = Time.realtimeSinceStartup + 10;
                while (!applied && Time.realtimeSinceStartup < deadline) yield return null;
                if (!applied || !restoreResult.Succeeded) { outcome = restoreResult; yield break; }
                Physics.SyncTransforms();
                if (Vector3.SqrMagnitude(body.position - preparedPosition) > .01f ||
                    Quaternion.Angle(body.rotation, prepared.ResumeContext.SavedRotation) > 1f ||
                    !regional.IsPositionAvailable(body.position) ||
                    !TruckTaxiRecoverySpace.HasClearance(body, body.position, body.rotation, body, null))
                {
                    outcome = LwsSaveOperationResult.Failure("Canonical load did not retain the prepared safe pose. Stay paused and retry or return to menu.");
                    yield break;
                }
                RunProfileId = profileId;
                profileReady = true;
                loadFailed = false;
                boundaryPending = false;
                RememberCounters();
                region = regional.ResolveRegion(host.Player.transform.position);
                host.WorldLife?.ReturnToTruck();
                host.ComponentDamage?.ReapplyPhysicalState();
                host.GPS?.RequestPresentationRefresh();
                CompleteContinueRequest(profileId, slot, autosave);
                outcome = LwsSaveOperationResult.Success("Vibin save loaded. Ready to drive.");
            }
            finally
            {
                permitApply = false;
                prepared = null;
                IsBusy = false;
                // Release this transaction's pause only after the facade and world are ready.
                // Failures deliberately keep the recovery UI and controls locked.
                if (host != null) host.SetPaused(!outcome.Succeeded);
                Complete(outcome);
                LoadCompleted?.Invoke(outcome);
            }
        }
        private LwsSaveOperationResult InvokeFacade(string profileId, int slot, bool autosave, bool backup) => backup
            ? SaveService.LoadRecoveryBackup() : autosave ? SaveService.LoadAutosave(profileId) : SaveService.Load(profileId, slot);

        private TruckTaxiVibinSavePayload Capture() => new TruckTaxiVibinSavePayload
        {
            regionId = regional.ResolveRegion(host.Player.transform.position),
            session = host.Session.CapturePersistentState(), needs = host.DriverNeeds.CapturePersistentState(),
            damage = host.ComponentDamage?.Capture(),
            environment = host.Environment.Capture(),
            extremes = host.Extremes?.Capture(origin.LocalToGlobal),
            hasFuelState = host.Fuel?.Fuel != null,
            fuelLiters = host.Fuel?.Fuel?.amount ?? 0,
            fuelCapacityLiters = host.Fuel?.Fuel?.capacity ?? 0,
            worldVisits = worldVisits.ToArray()
        };
        private LwsSaveOperationResult ValidatePayload(TruckTaxiVibinSavePayload payload)
        {
            var result = TruckTaxiVibinSaveParticipant.ValidatePayload(payload, host.Environment.settings);
            if (!result.Succeeded) return result;
            if (!host.Environment.CanRestore(payload.environment))
                return LwsSaveOperationResult.Failure("Saved environment does not match the authored snow layout.");
            if (payload.extremes != null && (host.Extremes == null || !host.Extremes.CanRestore(payload.extremes)))
                return LwsSaveOperationResult.Failure("Saved ambient events do not match the authored world.");
            if (host.ComponentDamage != null && !TruckTaxiComponentDamage.ValidSnapshot(payload.damage,
                host.Player.VehicleId, host.ComponentDamage.Parts))
                return LwsSaveOperationResult.Failure("Invalid component damage record or truck ownership.");
            foreach (var candidate in regional.regions) if (candidate != null && candidate.population && candidate.id == payload.regionId) return result;
            return LwsSaveOperationResult.Failure("Saved Vibin region is not in this world.");
        }
        private LwsSaveOperationResult ValidateSnapshot(LwsSaveSnapshot snapshot)
        {
            var result = TruckTaxiVibinSaveParticipant.ValidateSnapshot(snapshot, host.Environment.settings, out var payload, out var resume);
            if (!result.Succeeded) return result;
            if (payload.session.ownedProfile != null && payload.session.ownedProfile.runProfileId != snapshot.profileId)
                return LwsSaveOperationResult.Failure("Saved Career belongs to another run profile.");
            result = ValidatePayload(payload);
            if (!result.Succeeded) return result;
            if (!string.Equals(SceneManager.GetActiveScene().name, resume.authoredSceneName, StringComparison.OrdinalIgnoreCase))
                return LwsSaveOperationResult.Failure("Enter the Taxi gameplay scene before loading this profile.");
            var local = origin.GlobalToLocal(resume.SavedGlobalPosition);
            if (!float.IsFinite(local.x) || !float.IsFinite(local.y) || !float.IsFinite(local.z) || regional.ResolveRegion(local) != payload.regionId)
                return LwsSaveOperationResult.Failure("Saved pose no longer belongs to its authored region.");
            if (!TryRebaseWorkArea(payload.session, resume, origin.GlobalToLocal, out _))
                return LwsSaveOperationResult.Failure("Saved work area cannot be rebased to this origin.");
            return LwsSaveOperationResult.Success();
        }
        private LwsSaveOperationResult WorldReady(LwsLoadApplicationContext context)
        {
            if (!permitApply || prepared == null || prepared.VendorSlotNumber != context.VendorSlotNumber ||
                !SameSnapshot(prepared.Snapshot, context.Snapshot) || !regional.IsPositionAvailable(preparedPosition) ||
                !TruckTaxiRecoverySpace.HasClearance(body, preparedPosition, context.ResumeContext.SavedRotation, body, null))
                return LwsSaveOperationResult.Failure("Load must complete Vibin regional preparation before applying data.");
            context.CalculatedLocalPosition = preparedPosition;
            return LwsSaveOperationResult.Success();
        }
        private LwsWorldPositionD PreserveOrigin(LwsWorldPositionD _) => origin.CurrentOriginOffset;
        private LwsSaveOperationResult Restore(TruckTaxiVibinSavePayload payload)
        {
            if (!permitApply || prepared == null || !IsBusy) return LwsSaveOperationResult.Failure("Vibin restore was not prepared.");
            applied = true;
            if (!TryRebaseWorkArea(payload.session, prepared.ResumeContext, origin.GlobalToLocal, out var session))
                return restoreResult = LwsSaveOperationResult.Failure("Saved work area cannot be rebased to this origin.");
            if (!host.DriverNeeds.RestorePersistentState(payload.needs, out _) ||
                !host.Session.TryRestoreCareerRun(prepared.Snapshot.profileId, session, out _))
                return restoreResult = LwsSaveOperationResult.Failure("Vibin semantic state could not be restored.");
            if (!host.Session.HasCareerIdentity &&
                !host.Session.TryInitializeCareer("Driver", host.Player.DefinitionId, out _))
                return restoreResult = LwsSaveOperationResult.Failure("Legacy Career identity could not be initialized.");
            if (host.ComponentDamage != null && !host.ComponentDamage.Restore(payload.damage))
                return restoreResult = LwsSaveOperationResult.Failure("Vibin component damage could not be restored.");
            if (payload.hasFuelState && host.Fuel?.Fuel != null)
                host.Fuel.Fuel.amount = Mathf.Min(payload.fuelLiters, host.Fuel.Fuel.capacity);
            if (host.Extremes != null && !host.Extremes.Restore(payload.extremes, origin.GlobalToLocal))
                return restoreResult = LwsSaveOperationResult.Failure("Vibin ambient events could not be restored.");
            if (!host.Environment.Restore(payload.environment))
                return restoreResult = LwsSaveOperationResult.Failure("Vibin environment could not be restored.");
            worldVisits.Clear();
            if (payload.worldVisits != null) worldVisits.AddRange(payload.worldVisits);
            // The existing truck provider/load coordinator alone own final vehicle placement.
            host.Environment.Clock.SetPaused(true);
            return restoreResult = LwsSaveOperationResult.Success();
        }

        public static bool TryRebaseWorkArea(TruckTaxiPersistentState source, LwsWorldResumeContextPayload resume,
            Func<LwsWorldPositionD, Vector3> toLocal, out TruckTaxiPersistentState rebased)
        {
            rebased = null;
            if (source?.workArea == null || resume == null || toLocal == null) return false;
            rebased = source.Copy();
            var area = rebased.workArea;
            var center = toLocal(new LwsWorldPositionD(
                resume.savedGlobalX - resume.localX + area.centerX,
                resume.savedGlobalY - resume.localY + area.centerY,
                resume.savedGlobalZ - resume.localZ + area.centerZ));
            area.centerX = center.x; area.centerY = center.y; area.centerZ = center.z;
            return TruckTaxiSession.ValidatePersistentState(rebased, out _);
        }

        public static bool SameSnapshot(LwsSaveSnapshot a, LwsSaveSnapshot b)
        {
            if (a == null || b == null || a.profileId != b.profileId || a.capturedUtcTicks != b.capturedUtcTicks ||
                a.participants == null || b.participants == null || a.participants.Count != b.participants.Count) return false;
            for (int i = 0; i < a.participants.Count; i++)
            {
                var x = a.participants[i]; var y = b.participants[i];
                if (x == null || y == null || x.participantId != y.participantId || x.payloadVersion != y.payloadVersion || x.payloadJson != y.payloadJson) return false;
            }
            return true;
        }

        public static bool HasUnsafeFare(TruckTaxiState state) => state != TruckTaxiState.Inactive && state != TruckTaxiState.Available && state != TruckTaxiState.RideOffered &&
            state != TruckTaxiState.RideComplete && state != TruckTaxiState.RideFailed && state != TruckTaxiState.PassengerEjected;
        private string SaveBlockReason()
        {
            string ownership = ProfileWriteBlockReason(RunProfileId, SaveService?.ActiveProfileId);
            if (ownership != null) return ownership;
            if (!Initialized || !profileReady) return "No active Vibin run.";
            if (IsBusy) return "Vibin load preparation is active.";
            if (TruckTaxiWorldTravel.IsBusy) return "Regional travel is active.";
            if (loadFailed) return "Resolve the failed load before saving over any slot.";
            string blocked = UnsafeGameplayReason();
            if (blocked != null) return blocked;
            if (!TruckTaxiRecoverySpace.TryGroundedPose(body, body.position, body.rotation, .3f, body, null, out var grounded) ||
                Mathf.Abs(grounded.y - body.position.y) > 3f)
                return "Park on clear, supported ground before saving.";
            return null;
        }
        public static string ProfileWriteBlockReason(string runProfileId, string selectedProfileId)
        {
            if (string.IsNullOrEmpty(runProfileId)) return "No active Vibin run.";
            return string.Equals(runProfileId, selectedProfileId, StringComparison.Ordinal) ? null : ProfileMismatchMessage;
        }
        private string UnsafeGameplayReason(bool loading = false)
        {
            if (host == null || !host.Ready || host.Player == null || host.DriverNeeds?.State == null || !loading && !regional.InitialWorldReady)
                return "Vibin world is not ready.";
            if (host.Session.HasStolenPassenger) return "Deliver the stolen passenger before saving or loading.";
            if (HasUnsafeFare(host.Session.State) || host.Session.HasPassenger) return FinishRideMessage;
            if (host.Kaiju?.IsCapturingPlayer == true) return "Vehicle capture/recovery is in progress. Try again after release.";
            if (host.Fuel?.IsRescuing == true || host.Roadside?.IsRecovering == true || host.FastTravel?.IsTravelling == true)
                return "Vehicle relocation is in progress. Try again when recovery/travel completes.";
            if (host.Races?.IsActive == true || host.Session.RaceEventActive ||
                host.CustomContent?.IsPlaying == true || host.Companions?.IsBusy == true || host.Companions?.ActivePrivateStop != null ||
                host.DriverNeeds.JugActive || host.DriverNeeds.RoadPepChallenge != null || host.DriverNeeds.IsSleeping)
                return "Finish the current interaction or vehicle transition before saving or loading.";
            if (origin.ShiftInProgress || !loading && (!regional.IsPositionAvailable(host.Player.transform.position) ||
                string.IsNullOrEmpty(regional.ResolveRegion(host.Player.transform.position)))) return "Vibin region is not ready.";
            if (host.Player.CaptureState().trailerAttachment.attached) return "Detach the trailer before saving or loading Vibin.";
            return null;
        }
        private void RememberCounters()
        { completedRides = host.Session.CompletedRides; purchases = host.Session.PurchaseChargesCents; services = host.Session.ServiceChargesCents; }
        private void SessionChanged()
        {
            if (host.Session.CompletedRides != completedRides || host.Session.PurchaseChargesCents != purchases || host.Session.ServiceChargesCents != services)
                boundaryPending = true;
            RememberCounters();
        }
        private void RegionChanged()
        {
            string next = regional.ResolveRegion(host.Player.transform.position);
            if (!string.IsNullOrEmpty(next) && next != region) { region = next; boundaryPending = true; }
        }
        private void NeedsBoundary(TruckTaxiDriverNeedEvent e)
        { if (e == TruckTaxiDriverNeedEvent.CabCleaned || e == TruckTaxiDriverNeedEvent.BathroomStop) boundaryPending = true; }
        private void SleepBoundary(int _) => boundaryPending = true;
        private void ServiceBoundary() => boundaryPending = true;
        private void LateUpdate()
        {
            // Charge notifications precede inventory/service mutations; capture after that frame's transaction.
            if (!boundaryPending || !profileReady || IsBusy) return;
            boundaryPending = false;
            SaveService.RequestAutosave("Vibin gameplay boundary");
        }
        private LwsSaveOperationResult Fail(string message) => Complete(LwsSaveOperationResult.Failure(message));
        private LwsSaveOperationResult Complete(LwsSaveOperationResult result)
        { LastResult = result; Status = result.Message; OperationCompleted?.Invoke(result); return result; }
        private void OnDestroy()
        {
            StopAllCoroutines();
            if (!Initialized) return;
            host.Session.Changed -= SessionChanged;
            regional.AvailabilityChanged -= RegionChanged;
            host.DriverNeeds.DriverNeedsEvent -= NeedsBoundary;
            host.DriverNeeds.SleepCompleted -= SleepBoundary;
            if (host.Fuel != null) host.Fuel.Rescued -= ServiceBoundary;
            SaveService.UnregisterParticipant(participant.ParticipantId);
            SaveService.AdditionalSaveBlockReason = () => "Vibin gameplay is not initialized.";
            var coordinator = SaveService.LoadCoordinator;
            if (coordinator != null)
            {
                coordinator.AdditionalSnapshotValidation = null;
                coordinator.AdditionalWorldReadiness = UnboundLoadGate;
                coordinator.ResumeOriginOffset = null;
            }
            participant = null;
        }
    }
}
