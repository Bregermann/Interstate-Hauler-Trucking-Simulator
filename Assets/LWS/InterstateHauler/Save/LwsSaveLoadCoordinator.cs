using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.InterstateHauler
{
    public enum LwsSaveLoadCoordinatorPhase
    {
        Idle,
        ReadingVendorSlot,
        ValidatingSave,
        PreparingGameplaySuppression,
        ResolvingTargetWorld,
        LoadingTargetWorld,
        EstablishingOrigin,
        PreparingWorld,
        WaitingForWorldReady,
        ApplyingVendorSave,
        RestoringPlayerVehicle,
        RestoringTrailer,
        RestoringWorldSemantics,
        RestoringNavigation,
        RegeneratingAmbientWorld,
        FinalizingPhysics,
        RebindingPresentation,
        Complete,
        Failed
    }

    public enum LwsSaveLoadSlotKind
    {
        ManualPrimary,
        AutosavePrimary,
        ManualBackup,
        AutosaveBackup
    }

    [Serializable]
    public sealed class LwsAutosaveConfiguration
    {
        public bool autosaveEnabled = true;
        public float autosaveIntervalSeconds = 300f;
        public float minimumTimeBetweenAutosavesSeconds = 60f;

        public void Sanitize()
        {
            autosaveIntervalSeconds = Mathf.Max(5f, autosaveIntervalSeconds);
            minimumTimeBetweenAutosavesSeconds = Mathf.Clamp(minimumTimeBetweenAutosavesSeconds, 0f, autosaveIntervalSeconds);
        }
    }

    [Serializable]
    public sealed class LwsSaveRecoveryOffer
    {
        public bool available;
        public string profileId;
        public int primarySlotNumber;
        public LwsSaveLoadSlotKind primarySlotKind;
        public int primaryVendorSlotNumber;
        public LwsSaveLoadSlotKind backupSlotKind;
        public int backupVendorSlotNumber;
        public long backupTimestampUtcTicks;
        public string failureMessage;
        public string displayMessage;

        public static LwsSaveRecoveryOffer None => new LwsSaveRecoveryOffer();
    }

    [Serializable]
    public sealed class LwsWorldResumeContextPayload
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string stableWorldId = LwsSaveSchema.DefaultStableWorldId;
        public string authoredSceneName;
        public int sceneBuildIndex;
        public bool hasGlobalPosition;
        public double savedGlobalX;
        public double savedGlobalY;
        public double savedGlobalZ;
        public float localX;
        public float localY;
        public float localZ;
        public float rotationX;
        public float rotationY;
        public float rotationZ;
        public float rotationW = 1f;
        public float headingDegrees;
        public long originVersion;
        public long originShiftCount;
        public long capturedUtcTicks;

        public LwsWorldPositionD SavedGlobalPosition => new LwsWorldPositionD(savedGlobalX, savedGlobalY, savedGlobalZ);

        public Quaternion SavedRotation
        {
            get
            {
                Quaternion rotation = new Quaternion(rotationX, rotationY, rotationZ, rotationW);
                float magnitude = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
                return magnitude <= 0.000001f
                    ? Quaternion.Euler(0f, headingDegrees, 0f)
                    : new Quaternion(rotation.x / magnitude, rotation.y / magnitude, rotation.z / magnitude, rotation.w / magnitude);
            }
        }

        public bool IsValid => schemaVersion > 0 &&
                               schemaVersion <= LwsSaveSchema.CurrentVersion &&
                               !string.IsNullOrWhiteSpace(stableWorldId) &&
                               !string.IsNullOrWhiteSpace(authoredSceneName) &&
                               hasGlobalPosition &&
                               !double.IsNaN(savedGlobalX) &&
                               !double.IsNaN(savedGlobalY) &&
                               !double.IsNaN(savedGlobalZ);
    }

    public sealed class LwsLoadApplicationContext
    {
        public string ProfileId { get; set; }
        public int VendorSlotNumber { get; set; }
        public LwsSaveLoadSlotKind SlotKind { get; set; }
        public LwsSaveSnapshot Snapshot { get; set; }
        public LwsWorldResumeContextPayload ResumeContext { get; set; }
        public LwsWorldResumeSceneBinding WorldBinding { get; set; }
        public int VendorSavedGameVersion { get; set; }
        public string VendorSavedGameSceneName { get; set; }
        public string FailureMessage { get; set; }
        public LwsWorldPositionD OriginOffsetEstablished { get; set; }
        public Vector3 CalculatedLocalPosition { get; set; }
        public Vector3 ReconstructedGlobalDifference { get; set; }
        public bool AppliedVendorLoad { get; set; }
        public bool WorldPrepared { get; set; }
    }

    public readonly struct LwsWorldResumeSceneBinding
    {
        public LwsWorldResumeSceneBinding(string stableWorldId, string sceneName, string scenePath, string label)
        {
            StableWorldId = stableWorldId ?? string.Empty;
            SceneName = sceneName ?? string.Empty;
            ScenePath = scenePath ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public string StableWorldId { get; }
        public string SceneName { get; }
        public string ScenePath { get; }
        public string Label { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(StableWorldId) && !string.IsNullOrWhiteSpace(SceneName);
    }
    public static class LwsWorldResumeCatalog
    {
        public const string InterstateCorridorValidationWorldId = "world.interstate-corridor-validation";
        public const string TruckValidationWorldId = "world.truck-validation";
        public const string StreamingHighwayValidationWorldId = "world.streaming-highway-validation";
        public const string FiftyMileValidationWorldId = "world.fifty-mile-floating-origin-validation";

        private static readonly LwsWorldResumeSceneBinding[] KnownScenes =
        {
            new LwsWorldResumeSceneBinding(
                InterstateCorridorValidationWorldId,
                "InterstateCorridorValidation",
                "Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity",
                "Interstate Corridor Validation"),
            new LwsWorldResumeSceneBinding(
                TruckValidationWorldId,
                "TruckValidation",
                "Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity",
                "Truck Validation"),
            new LwsWorldResumeSceneBinding(
                StreamingHighwayValidationWorldId,
                "StreamingHighwayValidation",
                "Assets/LWS/InterstateHauler/World/Streaming/Validation/StreamingHighwayValidation.unity",
                "Streaming Highway Validation"),
            new LwsWorldResumeSceneBinding(
                FiftyMileValidationWorldId,
                "IH_50MileFloatingOriginValidation",
                "Assets/LWS/InterstateHauler/World/Origin/Validation/IH_50MileFloatingOriginValidation.unity",
                "50-Mile Floating Origin Validation")
        };

        public static IReadOnlyList<LwsWorldResumeSceneBinding> Scenes => KnownScenes;

        public static string ResolveStableWorldId(string sceneName, string streamingWorldId)
        {
            if (!string.IsNullOrWhiteSpace(streamingWorldId))
            {
                return streamingWorldId.Trim();
            }

            for (int i = 0; i < KnownScenes.Length; i++)
            {
                if (string.Equals(KnownScenes[i].SceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return KnownScenes[i].StableWorldId;
                }
            }

            return LwsSaveSchema.DefaultStableWorldId;
        }

        public static bool TryResolve(string stableWorldId, string sceneName, out LwsWorldResumeSceneBinding binding)
        {
            for (int i = 0; i < KnownScenes.Length; i++)
            {
                LwsWorldResumeSceneBinding candidate = KnownScenes[i];
                if (!string.IsNullOrWhiteSpace(stableWorldId) &&
                    string.Equals(candidate.StableWorldId, stableWorldId, StringComparison.OrdinalIgnoreCase))
                {
                    binding = candidate;
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(sceneName) &&
                    string.Equals(candidate.SceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    binding = candidate;
                    return true;
                }
            }

            binding = default;
            return false;
        }
    }

    public sealed class LwsSaveLoadCoordinator
    {
        private readonly ILwsSaveService _saveService;
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private bool _suppressGameplayStateMapping;

        public LwsSaveLoadCoordinator(ILwsSaveService saveService, Func<LwsServiceRegistry> registryProvider)
        {
            _saveService = saveService;
            _registryProvider = registryProvider;
        }

        public LwsSaveLoadCoordinatorPhase Phase { get; private set; } = LwsSaveLoadCoordinatorPhase.Idle;
        public bool IsLoadActive => Phase != LwsSaveLoadCoordinatorPhase.Idle &&
                                    Phase != LwsSaveLoadCoordinatorPhase.Complete &&
                                    Phase != LwsSaveLoadCoordinatorPhase.Failed;
        public LwsLoadApplicationContext CurrentContext { get; private set; }
        public string LastFailure { get; private set; } = string.Empty;
        public LwsWorldResumeContextPayload LastResumeContext { get; private set; }

        public LwsSaveOperationResult PreReadVendorSlot(
            LwsSaveProfileMetadata profile,
            int vendorSlotNumber,
            LwsSaveLoadSlotKind slotKind,
            out LwsLoadApplicationContext context,
            bool mapGameplayState = true)
        {
            context = null;
            bool previousGameplayStateSuppression = _suppressGameplayStateMapping;
            _suppressGameplayStateMapping = !mapGameplayState;
            try
            {
                if (_saveService == null || _saveService.Adapter == null)
                {
                    return Fail("Pixel Crushers save adapter is not available for pre-read.");
                }

                SetPhase(LwsSaveLoadCoordinatorPhase.ReadingVendorSlot);
                LwsSaveOperationResult preRead = _saveService.Adapter.TryReadSemanticSnapshot(
                    vendorSlotNumber,
                    out LwsSaveSnapshot snapshot,
                    out int vendorVersion,
                    out string vendorSceneName);
                if (!preRead.Succeeded)
                {
                    return Fail(preRead.Message);
                }

                SetPhase(LwsSaveLoadCoordinatorPhase.ValidatingSave);
                LwsSaveOperationResult validation = ValidatePreRead(profile, snapshot, vendorSlotNumber, out LwsWorldResumeContextPayload resumeContext, out LwsWorldResumeSceneBinding binding);
                if (!validation.Succeeded)
                {
                    return Fail(validation.Message);
                }

                context = new LwsLoadApplicationContext
                {
                    ProfileId = profile != null ? profile.stableProfileId : snapshot.profileId,
                    VendorSlotNumber = vendorSlotNumber,
                    SlotKind = slotKind,
                    Snapshot = snapshot,
                    ResumeContext = resumeContext,
                    WorldBinding = binding,
                    VendorSavedGameVersion = vendorVersion,
                    VendorSavedGameSceneName = vendorSceneName ?? string.Empty
                };
                CurrentContext = context;
                LastResumeContext = resumeContext;
                LastFailure = string.Empty;
                return LwsSaveOperationResult.Success("Pixel Crushers SavedGameData pre-read completed without applying gameplay state.");
            }
            finally
            {
                _suppressGameplayStateMapping = previousGameplayStateSuppression;
            }
        }
        public LwsSaveOperationResult LoadPreparedVendorSlot(LwsLoadApplicationContext context)
        {
            if (context == null)
            {
                return Fail("Cannot load without a prepared load context.");
            }

            CurrentContext = context;
            try
            {
                SetPhase(LwsSaveLoadCoordinatorPhase.PreparingGameplaySuppression);
                SuppressDrivingInput();

                SetPhase(LwsSaveLoadCoordinatorPhase.ResolvingTargetWorld);
                if (!context.WorldBinding.IsValid)
                {
                    return Fail("Saved world identity did not resolve to an allowed runtime scene.");
                }

                SetPhase(LwsSaveLoadCoordinatorPhase.LoadingTargetWorld);
                LwsSaveOperationResult sceneResult = EnsureTargetSceneLoaded(context.WorldBinding);
                if (!sceneResult.Succeeded)
                {
                    return Fail(sceneResult.Message);
                }

                SetPhase(LwsSaveLoadCoordinatorPhase.EstablishingOrigin);
                LwsSaveOperationResult originResult = EstablishOriginForResume(context);
                if (!originResult.Succeeded)
                {
                    return Fail(originResult.Message);
                }

                SetPhase(LwsSaveLoadCoordinatorPhase.PreparingWorld);
                PrepareWorldForGlobalPosition(context);

                SetPhase(LwsSaveLoadCoordinatorPhase.WaitingForWorldReady);
                Physics.SyncTransforms();
                context.WorldPrepared = true;

                SetPhase(LwsSaveLoadCoordinatorPhase.ApplyingVendorSave);
                LwsSaveOperationResult load = _saveService.Adapter.LoadFromSlot(context.VendorSlotNumber);
                context.AppliedVendorLoad = load.Succeeded;
                if (!load.Succeeded)
                {
                    return Fail(load.Message);
                }

                SetPhase(LwsSaveLoadCoordinatorPhase.RestoringPlayerVehicle);
                FinalizeCanonicalTruck(context);

                SetPhase(LwsSaveLoadCoordinatorPhase.RestoringTrailer);
                FinalizePlayerTrailers();

                SetPhase(LwsSaveLoadCoordinatorPhase.RestoringWorldSemantics);
                Physics.SyncTransforms();

                SetPhase(LwsSaveLoadCoordinatorPhase.RestoringNavigation);
                RebindNavigationPresentation();

                SetPhase(LwsSaveLoadCoordinatorPhase.RegeneratingAmbientWorld);
                // UTS ambient vehicles are intentionally not saved. Existing runtime traffic controllers repopulate normally.

                SetPhase(LwsSaveLoadCoordinatorPhase.FinalizingPhysics);
                Physics.SyncTransforms();

                SetPhase(LwsSaveLoadCoordinatorPhase.RebindingPresentation);
                RebindRuntimePresentation();

                SetPhase(LwsSaveLoadCoordinatorPhase.Complete);
                LastFailure = string.Empty;
                return LwsSaveOperationResult.Success("Pixel Crushers save loaded after LWS world resume preparation.");
            }
            catch (Exception ex)
            {
                return Fail($"Save load coordinator failed: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                if (Phase != LwsSaveLoadCoordinatorPhase.Failed)
                {
                    CurrentContext = null;
                }
            }
        }

        public void ResetAfterFailure()
        {
            CurrentContext = null;
            LastFailure = string.Empty;
            SetPhase(LwsSaveLoadCoordinatorPhase.Idle);
        }

        private LwsSaveOperationResult ValidatePreRead(
            LwsSaveProfileMetadata profile,
            LwsSaveSnapshot snapshot,
            int vendorSlotNumber,
            out LwsWorldResumeContextPayload resumeContext,
            out LwsWorldResumeSceneBinding binding)
        {
            resumeContext = null;
            binding = default;
            if (snapshot == null)
            {
                return LwsSaveOperationResult.Failure($"Pixel Crushers slot {vendorSlotNumber} does not contain an LWS semantic snapshot.");
            }

            if (snapshot.schemaVersion <= 0 || snapshot.schemaVersion > LwsSaveSchema.CurrentVersion)
            {
                return LwsSaveOperationResult.Failure($"Saved game schema {snapshot.schemaVersion} is not supported by runtime schema {LwsSaveSchema.CurrentVersion}.");
            }

            if (profile != null && !string.Equals(profile.stableProfileId, snapshot.profileId, StringComparison.Ordinal))
            {
                return LwsSaveOperationResult.Failure("Saved game profile ownership does not match the selected profile.");
            }

            if (!TryExtractResumeContext(snapshot, out resumeContext) &&
                !TryBuildResumeContextFromGlobalPosition(snapshot, out resumeContext))
            {
                return LwsSaveOperationResult.Failure("Saved game does not contain enough world resume context.");
            }

            if (!resumeContext.IsValid)
            {
                return LwsSaveOperationResult.Failure("Saved world resume context is invalid or unsupported.");
            }

            if (!LwsWorldResumeCatalog.TryResolve(resumeContext.stableWorldId, resumeContext.authoredSceneName, out binding))
            {
                return LwsSaveOperationResult.Failure($"Saved world '{resumeContext.stableWorldId}' / scene '{resumeContext.authoredSceneName}' is not in the allowed LWS world resume catalog.");
            }

            return LwsSaveOperationResult.Success("Saved world resume context is valid.");
        }

        private static bool TryExtractResumeContext(LwsSaveSnapshot snapshot, out LwsWorldResumeContextPayload resumeContext)
        {
            resumeContext = null;
            LwsSaveParticipantState state = snapshot.participants?.FirstOrDefault(p => p != null && p.participantId == LwsSaveSchema.WorldResumeContextParticipantId);
            if (state == null || string.IsNullOrWhiteSpace(state.payloadJson))
            {
                return false;
            }

            try
            {
                resumeContext = JsonUtility.FromJson<LwsWorldResumeContextPayload>(state.payloadJson);
                return resumeContext != null;
            }
            catch
            {
                resumeContext = null;
                return false;
            }
        }
        private static bool TryBuildResumeContextFromGlobalPosition(LwsSaveSnapshot snapshot, out LwsWorldResumeContextPayload resumeContext)
        {
            resumeContext = null;
            LwsSaveParticipantState state = snapshot.participants?.FirstOrDefault(p => p != null && p.participantId == "lws.world.global-position");
            if (state == null || string.IsNullOrWhiteSpace(state.payloadJson))
            {
                return false;
            }

            try
            {
                LwsGlobalPositionSavePayload payload = JsonUtility.FromJson<LwsGlobalPositionSavePayload>(state.payloadJson);
                if (!payload.IsValid)
                {
                    return false;
                }

                string sceneName = string.IsNullOrWhiteSpace(snapshot.sceneName)
                    ? SceneManager.GetActiveScene().name
                    : snapshot.sceneName;
                string stableWorldId = LwsWorldResumeCatalog.ResolveStableWorldId(sceneName, string.Empty);
                Quaternion rotation = NormalizeRotation(new Quaternion(payload.rotationX, payload.rotationY, payload.rotationZ, payload.rotationW));
                resumeContext = new LwsWorldResumeContextPayload
                {
                    schemaVersion = LwsSaveSchema.CurrentVersion,
                    stableWorldId = stableWorldId,
                    authoredSceneName = sceneName,
                    sceneBuildIndex = -1,
                    hasGlobalPosition = true,
                    savedGlobalX = payload.globalX,
                    savedGlobalY = payload.globalY,
                    savedGlobalZ = payload.globalZ,
                    localX = payload.localX,
                    localY = payload.localY,
                    localZ = payload.localZ,
                    rotationX = rotation.x,
                    rotationY = rotation.y,
                    rotationZ = rotation.z,
                    rotationW = rotation.w,
                    headingDegrees = rotation.eulerAngles.y,
                    originVersion = payload.originVersion,
                    originShiftCount = payload.originShiftCount,
                    capturedUtcTicks = payload.capturedUtcTicks
                };
                return true;
            }
            catch
            {
                resumeContext = null;
                return false;
            }
        }

        private LwsSaveOperationResult EnsureTargetSceneLoaded(LwsWorldResumeSceneBinding binding)
        {
            if (!Application.isPlaying)
            {
                return LwsSaveOperationResult.Success("Scene load deferred; Play Mode is not active.");
            }

            Scene active = SceneManager.GetActiveScene();
            if (string.Equals(active.name, binding.SceneName, StringComparison.OrdinalIgnoreCase))
            {
                return LwsSaveOperationResult.Success($"Target scene already active: {binding.SceneName}.");
            }

            if (!Application.CanStreamedLevelBeLoaded(binding.SceneName))
            {
                return LwsSaveOperationResult.Failure($"Target scene '{binding.SceneName}' is not available to runtime scene loading. Add it to Build Settings before using mid-route resume.");
            }

            SceneManager.LoadScene(binding.SceneName, LoadSceneMode.Single);
            Scene loaded = SceneManager.GetActiveScene();
            return string.Equals(loaded.name, binding.SceneName, StringComparison.OrdinalIgnoreCase)
                ? LwsSaveOperationResult.Success($"Loaded target resume scene: {binding.SceneName}.")
                : LwsSaveOperationResult.Failure($"Requested scene '{binding.SceneName}' but active scene is '{loaded.name}'.");
        }

        private LwsSaveOperationResult EstablishOriginForResume(LwsLoadApplicationContext context)
        {
            if (!TryGetService(out ILwsWorldOriginService originService))
            {
                context.CalculatedLocalPosition = context.ResumeContext.SavedGlobalPosition.ToVector3();
                context.OriginOffsetEstablished = LwsWorldPositionD.Zero;
                return LwsSaveOperationResult.Success("Floating-origin service unavailable; using global position as local validation fallback.");
            }

            LwsWorldPositionD savedGlobal = context.ResumeContext.SavedGlobalPosition;
            LwsWorldPositionD targetOffset = CalculateResumeOriginOffset(savedGlobal, originService.ActiveTuning);
            if (!originService.SetOriginOffset(targetOffset, "Mid-route save resume origin preparation.", out LwsOriginShiftEvent shiftEvent))
            {
                return LwsSaveOperationResult.Failure("Floating-origin service rejected the saved resume origin offset.");
            }

            context.OriginOffsetEstablished = originService.CurrentOriginOffset;
            context.CalculatedLocalPosition = originService.GlobalToLocal(savedGlobal);
            LwsWorldPositionD reconstructed = originService.LocalToGlobal(context.CalculatedLocalPosition);
            context.ReconstructedGlobalDifference = new Vector3(
                (float)(reconstructed.x - savedGlobal.x),
                (float)(reconstructed.y - savedGlobal.y),
                (float)(reconstructed.z - savedGlobal.z));
            originService.UpdatePlayerLocalPosition(context.CalculatedLocalPosition);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                "LWS mid-route resume origin diagnostics\n" +
                $"Saved Global: {savedGlobal}\n" +
                $"Current Origin Offset: {originService.CurrentOriginOffset}\n" +
                $"Calculated Local: {context.CalculatedLocalPosition}\n" +
                $"Reconstructed Difference: {context.ReconstructedGlobalDifference}\n" +
                $"Origin Shift: {shiftEvent.Message}");
#endif
            return LwsSaveOperationResult.Success("Floating origin established before Pixel Crushers load application.");
        }

        private void PrepareWorldForGlobalPosition(LwsLoadApplicationContext context)
        {
            Vector3 heading = context.ResumeContext.SavedRotation * Vector3.forward;
            if (TryGetService(out ILwsWorldStreamingService streamingService) && streamingService.ActiveManifest != null)
            {
                streamingService.UpdateStreamingAnchor(new LwsWorldStreamingAnchorState(
                    context.CalculatedLocalPosition,
                    context.ResumeContext.SavedGlobalPosition,
                    heading,
                    0f,
                    false,
                    context.CalculatedLocalPosition,
                    context.ResumeContext.SavedGlobalPosition));
                streamingService.ReloadCurrentNeighborhood();
            }
        }

        private void FinalizeCanonicalTruck(LwsLoadApplicationContext context)
        {
            LwsPlayerTruck activeTruck = null;
            if (TryGetService(out ILwsPlayerVehicleService vehicleService))
            {
                activeTruck = vehicleService.ActiveTruck;
            }

            LwsPlayerTruck[] trucks = UnityEngine.Object.FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None);
            if (activeTruck == null && trucks != null && trucks.Length > 0)
            {
                activeTruck = trucks.FirstOrDefault(t => t != null);
                vehicleService?.RegisterActiveTruck(activeTruck);
            }

            if (trucks != null && activeTruck != null)
            {
                for (int i = 0; i < trucks.Length; i++)
                {
                    LwsPlayerTruck truck = trucks[i];
                    if (truck != null && truck != activeTruck)
                    {
                        UnityEngine.Object.Destroy(truck.gameObject);
                    }
                }
            }

            if (activeTruck == null)
            {
                return;
            }

            activeTruck.transform.SetPositionAndRotation(context.CalculatedLocalPosition, context.ResumeContext.SavedRotation);
            ZeroRigidbodies(activeTruck.gameObject);
            if (TryGetService(out ILwsWorldOriginService originService))
            {
                originService.UpdatePlayerLocalPosition(context.CalculatedLocalPosition);
                LwsWorldPositionD reconstructed = originService.LocalToGlobal(activeTruck.transform.position);
                context.ReconstructedGlobalDifference = new Vector3(
                    (float)(reconstructed.x - context.ResumeContext.savedGlobalX),
                    (float)(reconstructed.y - context.ResumeContext.savedGlobalY),
                    (float)(reconstructed.z - context.ResumeContext.savedGlobalZ));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log(
                    "LWS mid-route resume truck pose diagnostics\n" +
                    $"Actual Truck Local: {activeTruck.transform.position}\n" +
                    $"Global reconstructed from loaded truck: {reconstructed}\n" +
                    $"Difference: {context.ReconstructedGlobalDifference}",
                    activeTruck);
#endif
            }
        }

        private void FinalizePlayerTrailers()
        {
            LwsPlayerTruck truck = null;
            if (TryGetService(out ILwsPlayerVehicleService vehicleService))
            {
                truck = vehicleService.ActiveTruck;
            }

            if (truck == null || truck.CouplingAdapter == null || !truck.CouplingAdapter.CurrentState.attached)
            {
                return;
            }

            LwsTrailerAttachmentState state = truck.CouplingAdapter.CurrentState;
            if (string.IsNullOrWhiteSpace(state.trailerId))
            {
                return;
            }

            foreach (LwsVehicleIdentity identity in UnityEngine.Object.FindObjectsByType<LwsVehicleIdentity>(FindObjectsSortMode.None))
            {
                if (identity == null || !string.Equals(identity.VehicleId, state.trailerId, StringComparison.Ordinal))
                {
                    continue;
                }

                identity.transform.SetPositionAndRotation(state.trailerPose.position, NormalizeRotation(state.trailerPose.rotation));
                ZeroRigidbodies(identity.gameObject);
                break;
            }
        }

        private static void ZeroRigidbodies(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb == null)
                {
                    continue;
                }

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }

        private void RebindNavigationPresentation()
        {
            if (TryGetService(out ILwsNavigationService navigationService))
            {
                navigationService.PresentCurrentRoute();
            }
        }

        private void RebindRuntimePresentation()
        {
            if (TryGetService(out ILwsCameraPresentationService cameraService))
            {
                cameraService.SetCameraMode(cameraService.CurrentMode, cameraService.CurrentCameraName);
            }
        }

        private void SuppressDrivingInput()
        {
            if (TryGetService(out ILwsVehicleInputService inputService))
            {
                inputService.NeutralizeInput();
            }
        }

        private bool TryGetService<T>(out T service) where T : class, ILwsService
        {
            service = null;
            LwsServiceRegistry registry = _registryProvider?.Invoke();
            return registry != null && registry.TryGet(out service);
        }

        private LwsSaveOperationResult Fail(string message)
        {
            CurrentContext?.GetType();
            LastFailure = string.IsNullOrWhiteSpace(message) ? "Save load coordinator failed." : message;
            SetPhase(LwsSaveLoadCoordinatorPhase.Failed);
            if (CurrentContext != null)
            {
                CurrentContext.FailureMessage = LastFailure;
            }
            SuppressDrivingInput();
            return LwsSaveOperationResult.Failure(LastFailure);
        }

        private void SetPhase(LwsSaveLoadCoordinatorPhase phase)
        {
            Phase = phase;
            MapMacroGameplayState(phase);
        }

        private void MapMacroGameplayState(LwsSaveLoadCoordinatorPhase phase)
        {
            if (_suppressGameplayStateMapping || !TryGetService(out ILwsGameplayStateService gameplayStateService))
            {
                return;
            }

            switch (phase)
            {
                case LwsSaveLoadCoordinatorPhase.Complete:
                    gameplayStateService.EnterFreeDrive("LWS save/load coordinator completed.");
                    break;
                case LwsSaveLoadCoordinatorPhase.Failed:
                    gameplayStateService.EnterRecoveryError(string.IsNullOrWhiteSpace(LastFailure) ? "LWS save/load coordinator failed." : LastFailure);
                    break;
                case LwsSaveLoadCoordinatorPhase.Idle:
                    break;
                default:
                    gameplayStateService.EnterLoadingWorld($"LWS save/load coordinator phase: {phase}.");
                    break;
            }
        }

        private static LwsWorldPositionD CalculateResumeOriginOffset(LwsWorldPositionD savedGlobal, LwsFloatingOriginTuning tuning)
        {
            if (tuning == null || !tuning.floatingOriginEnabled)
            {
                return LwsWorldPositionD.Zero;
            }

            double grid = Math.Max(1d, tuning.shiftGridMeters);
            return new LwsWorldPositionD(
                tuning.shiftXAxis ? TruncateToGrid(savedGlobal.x, grid) : 0d,
                tuning.shiftYAxis ? TruncateToGrid(savedGlobal.y, grid) : 0d,
                tuning.shiftZAxis ? TruncateToGrid(savedGlobal.z, grid) : 0d);
        }

        private static double TruncateToGrid(double value, double grid)
        {
            if (Math.Abs(value) < grid)
            {
                return 0d;
            }

            return Math.Sign(value) * Math.Floor(Math.Abs(value) / grid) * grid;
        }

        private static Quaternion NormalizeRotation(Quaternion rotation)
        {
            float magnitude = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
            return magnitude <= 0.000001f
                ? Quaternion.identity
                : new Quaternion(rotation.x / magnitude, rotation.y / magnitude, rotation.z / magnitude, rotation.w / magnitude);
        }
    }

    public sealed class LwsWorldResumeSaveParticipant : ILwsSaveParticipant
    {
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private LwsWorldResumeContextPayload _lastRestored;

        public LwsWorldResumeSaveParticipant(Func<LwsServiceRegistry> registryProvider)
        {
            _registryProvider = registryProvider;
        }

        public string ParticipantId => LwsSaveSchema.WorldResumeContextParticipantId;
        public int PayloadVersion => 1;
        public LwsWorldResumeContextPayload LastRestored => _lastRestored;

        public LwsSaveParticipantState CaptureState()
        {
            string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            string streamingWorldId = string.Empty;
            LwsWorldPositionD global = LwsWorldPositionD.Zero;
            Vector3 local = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            long originVersion = 0;
            long originShiftCount = 0;

            if (TryGetService(out ILwsWorldStreamingService streamingService))
            {
                streamingWorldId = streamingService.WorldId;
            }

            if (TryGetService(out ILwsWorldOriginService originService))
            {
                global = originService.PlayerGlobalPosition;
                local = originService.PlayerLocalPosition;
                originVersion = originService.OriginVersion;
                originShiftCount = originService.ShiftCount;
            }

            if (TryGetService(out ILwsPlayerVehicleService vehicleService) && vehicleService.ActiveTruck != null)
            {
                rotation = NormalizeRotation(vehicleService.ActiveTruck.transform.rotation);
                if (TryGetService(out ILwsWorldOriginService truckOriginService))
                {
                    local = vehicleService.ActiveTruck.transform.position;
                    global = truckOriginService.LocalToGlobal(local);
                }
            }

            var payload = new LwsWorldResumeContextPayload
            {
                schemaVersion = LwsSaveSchema.CurrentVersion,
                stableWorldId = LwsWorldResumeCatalog.ResolveStableWorldId(sceneName, streamingWorldId),
                authoredSceneName = sceneName,
                sceneBuildIndex = SceneManager.GetActiveScene().buildIndex,
                hasGlobalPosition = true,
                savedGlobalX = global.x,
                savedGlobalY = global.y,
                savedGlobalZ = global.z,
                localX = local.x,
                localY = local.y,
                localZ = local.z,
                rotationX = rotation.x,
                rotationY = rotation.y,
                rotationZ = rotation.z,
                rotationW = rotation.w,
                headingDegrees = rotation.eulerAngles.y,
                originVersion = originVersion,
                originShiftCount = originShiftCount,
                capturedUtcTicks = DateTime.UtcNow.Ticks
            };

            return new LwsSaveParticipantState
            {
                participantId = ParticipantId,
                payloadVersion = PayloadVersion,
                payloadJson = JsonUtility.ToJson(payload)
            };
        }

        public LwsSaveOperationResult RestoreState(LwsSaveParticipantState state)
        {
            if (state == null || state.participantId != ParticipantId)
            {
                return LwsSaveOperationResult.Failure($"Payload does not belong to {ParticipantId}.");
            }

            _lastRestored = JsonUtility.FromJson<LwsWorldResumeContextPayload>(state.payloadJson);
            return _lastRestored != null && _lastRestored.IsValid
                ? LwsSaveOperationResult.Success("World resume context acknowledged; load coordinator owns scene/origin preparation.")
                : LwsSaveOperationResult.Failure("World resume context payload is invalid.");
        }

        public LwsSaveOperationResult ClearState()
        {
            _lastRestored = null;
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return LwsSaveOperationResult.Success("World resume context captures stable world identity and global player pose for Pixel Crushers pre-read.");
        }

        private bool TryGetService<T>(out T service) where T : class, ILwsService
        {
            service = null;
            LwsServiceRegistry registry = _registryProvider?.Invoke();
            return registry != null && registry.TryGet(out service);
        }

        private static Quaternion NormalizeRotation(Quaternion rotation)
        {
            float magnitude = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
            return magnitude <= 0.000001f
                ? Quaternion.identity
                : new Quaternion(rotation.x / magnitude, rotation.y / magnitude, rotation.z / magnitude, rotation.w / magnitude);
        }
    }

    internal sealed class LwsAutosaveRuntimeDriver : MonoBehaviour
    {
        private ILwsSaveService _saveService;

        public void Bind(ILwsSaveService saveService)
        {
            _saveService = saveService;
        }

        private void Update()
        {
            _saveService?.TickAutosave(Time.unscaledDeltaTime);
        }
    }
}