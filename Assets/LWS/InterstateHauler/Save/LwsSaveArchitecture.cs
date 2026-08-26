using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsSaveParticipantState
    {
        public string participantId;
        public int payloadVersion;
        public string payloadJson;
    }

    [Serializable]
    public sealed class LwsSaveSnapshot
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string gameVersion = LwsSaveSchema.DefaultGameVersion;
        public string profileId;
        public string profileDisplayName;
        public LwsSaveSlotType slotType = LwsSaveSlotType.Manual;
        public int slotNumber;
        public int vendorSlotNumber;
        public string sceneName;
        public long capturedUtcTicks;
        public List<LwsSaveParticipantState> participants = new List<LwsSaveParticipantState>();
    }

    public readonly struct LwsSaveOperationResult
    {
        public bool Succeeded { get; }
        public string Message { get; }

        private LwsSaveOperationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        public static LwsSaveOperationResult Success(string message = "")
        {
            return new LwsSaveOperationResult(true, message);
        }

        public static LwsSaveOperationResult Failure(string message)
        {
            return new LwsSaveOperationResult(false, string.IsNullOrWhiteSpace(message) ? "Save operation failed." : message);
        }
    }

    public interface ILwsSaveParticipant
    {
        string ParticipantId { get; }
        int PayloadVersion { get; }
        LwsSaveParticipantState CaptureState();
        LwsSaveOperationResult RestoreState(LwsSaveParticipantState state);
        LwsSaveOperationResult ClearState();
        LwsSaveOperationResult ValidateParticipant();
    }

    public interface ILwsSaveService : ILwsService
    {
        IReadOnlyList<ILwsSaveParticipant> Participants { get; }
        LwsPixelCrushersSaveAdapter Adapter { get; }
        LwsSaveDiagnostics Diagnostics { get; }
        LwsSaveLoadCoordinator LoadCoordinator { get; }
        LwsSaveRecoveryOffer RecoveryOffer { get; }
        LwsAutosaveConfiguration AutosaveConfiguration { get; }
        IReadOnlyList<LwsSaveProfileMetadata> Profiles { get; }
        LwsSaveProfileMetadata ActiveProfile { get; }
        string ActiveProfileId { get; }
        int ManualSlotCount { get; }
        bool IsSaving { get; }
        bool IsLoading { get; }
        bool PendingAutosave { get; }
        string LastFailure { get; }
        LwsSaveLoadCoordinatorPhase LoadPhase { get; }
        bool CanSave { get; }
        bool CanLoad { get; }
        event Action<LwsSaveOperationResult, LwsManualSaveSlotMetadata> SaveCompleted;
        event Action<LwsSaveOperationResult, LwsManualSaveSlotMetadata> LoadCompleted;
        event Action<LwsSaveOperationResult, string, int> DeleteCompleted;
        LwsSaveOperationResult RegisterParticipant(ILwsSaveParticipant participant);
        LwsSaveOperationResult UnregisterParticipant(string participantId);
        LwsSaveOperationResult ValidateParticipants();
        LwsSaveSnapshot CaptureSnapshot(string profileId);
        LwsSaveOperationResult RestoreSnapshot(LwsSaveSnapshot snapshot);
        LwsSaveOperationResult ClearAllParticipants();
        LwsSaveOperationResult CreateProfile(string displayName, out LwsSaveProfileMetadata profile);
        LwsSaveOperationResult SelectProfile(string profileId);
        LwsSaveOperationResult RenameProfile(string profileId, string displayName);
        LwsSaveOperationResult DeleteProfile(string profileId);
        IReadOnlyList<LwsManualSaveSlotMetadata> GetManualSlots(string profileId = null);
        LwsManualSaveSlotMetadata GetSlotMetadata(string profileId, int manualSlotNumber);
        LwsManualSaveSlotMetadata GetAutosaveSlotMetadata(string profileId = null);
        int MapToVendorSlot(string profileId, LwsSaveSlotType slotType, int slotNumber);
        bool HasSave(string profileId, int manualSlotNumber);
        bool HasAutosave(string profileId = null);
        LwsSaveOperationResult Save(string profileId, int manualSlotNumber, bool overwrite);
        LwsSaveOperationResult SaveAutosave(string profileId = null, string reason = null);
        LwsSaveOperationResult RequestAutosave(string reason);
        LwsSaveOperationResult Load(string profileId, int manualSlotNumber);
        LwsSaveOperationResult LoadAutosave(string profileId = null);
        LwsSaveOperationResult LoadRecoveryBackup();
        void DismissRecoveryOffer();
        LwsSaveOperationResult Delete(string profileId, int manualSlotNumber);
        LwsSaveOperationResult DeleteAutosave(string profileId = null);
        void TickAutosave(float deltaTimeSeconds);
        string BuildDiagnosticsReport();
    }

    public sealed class LwsSaveService : ILwsSaveService
    {
        private const string DefaultProfileId = "profile.development.driver";
        private const string DefaultProfileName = "Development Driver";

        private readonly List<ILwsSaveParticipant> _participants = new List<ILwsSaveParticipant>();
        private readonly HashSet<string> _participantIds = new HashSet<string>(StringComparer.Ordinal);

        private LwsServiceRegistry _registry;
        private LwsSaveProfileDirectory _directory = new LwsSaveProfileDirectory();
        private string _lastSaveMessage = "--";
        private string _lastLoadMessage = "--";
        private string _lastDeleteMessage = "--";
        private string _pendingProfileId;
        private LwsSaveSlotType _pendingSlotType = LwsSaveSlotType.Manual;
        private int _pendingSlotNumber;
        private int _pendingVendorSlotNumber;
        private readonly LwsAutosaveConfiguration _autosaveConfiguration = new LwsAutosaveConfiguration();
        private float _autosaveElapsedSeconds;
        private float _lastAutosaveRealtime = -9999f;
        private string _pendingAutosaveReason = string.Empty;
        private LwsAutosaveRuntimeDriver _autosaveDriver;

        public string ServiceId => "lws.save";
        public IReadOnlyList<ILwsSaveParticipant> Participants => _participants;
        public LwsPixelCrushersSaveAdapter Adapter { get; private set; }
        public LwsSaveDiagnostics Diagnostics { get; private set; } = LwsSaveDiagnostics.Empty;
        public LwsSaveLoadCoordinator LoadCoordinator { get; private set; }
        public LwsSaveRecoveryOffer RecoveryOffer { get; private set; } = LwsSaveRecoveryOffer.None;
        public LwsAutosaveConfiguration AutosaveConfiguration => _autosaveConfiguration;
        public IReadOnlyList<LwsSaveProfileMetadata> Profiles
        {
            get
            {
                EnsureDefaultProfile();
                return _directory.Profiles;
            }
        }

        public LwsSaveProfileMetadata ActiveProfile
        {
            get
            {
                EnsureDefaultProfile();
                return _directory.FindProfile(_directory.selectedProfileId);
            }
        }

        public string ActiveProfileId => ActiveProfile != null ? ActiveProfile.stableProfileId : string.Empty;
        public int ManualSlotCount => LwsSaveSchema.ManualSlotCount;
        public bool IsSaving { get; private set; }
        public bool IsLoading { get; private set; }
        public bool PendingAutosave { get; private set; }
        public string LastFailure { get; private set; } = string.Empty;
        public LwsSaveLoadCoordinatorPhase LoadPhase => LoadCoordinator != null ? LoadCoordinator.Phase : LwsSaveLoadCoordinatorPhase.Idle;
        public bool CanSave => IsOperationSafe(out _);
        public bool CanLoad => Adapter != null && Adapter.ReadyForRuntimeSaves && ActiveProfile != null && !IsSaving && !IsLoading && (LoadCoordinator == null || !LoadCoordinator.IsLoadActive);

        public event Action<LwsSaveOperationResult, LwsManualSaveSlotMetadata> SaveCompleted;
        public event Action<LwsSaveOperationResult, LwsManualSaveSlotMetadata> LoadCompleted;
        public event Action<LwsSaveOperationResult, string, int> DeleteCompleted;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            _registry = context.Registry;
            Adapter = new LwsPixelCrushersSaveAdapter();
            LoadCoordinator = new LwsSaveLoadCoordinator(this, () => _registry);
            RegisterSemanticParticipants();

            LwsSaveOperationResult validation = ValidateParticipants();
            if (!validation.Succeeded)
            {
                return LwsServiceResult.Failure(validation.Message);
            }

            LwsSaveOperationResult adapterValidation = Adapter.Initialize(this);
            if (adapterValidation.Succeeded)
            {
                LwsSaveOperationResult directoryLoad = Adapter.LoadProfileDirectory(out LwsSaveProfileDirectory loadedDirectory);
                if (directoryLoad.Succeeded && loadedDirectory != null)
                {
                    _directory = loadedDirectory;
                }
            }

            EnsureDefaultProfile();
            EnsureAutosaveRuntimeDriver();
            RefreshDiagnostics("Initialize", adapterValidation);
            return adapterValidation.Succeeded
                ? LwsServiceResult.Success("LWS persistence facade initialized with Pixel Crushers Save System as the save authority.")
                : LwsServiceResult.Failure(adapterValidation.Message);
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _participants.Clear();
            _participantIds.Clear();
            if (_autosaveDriver != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_autosaveDriver.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_autosaveDriver.gameObject);
                }

                _autosaveDriver = null;
            }

            _registry = null;
            _directory = new LwsSaveProfileDirectory();
            Diagnostics = LwsSaveDiagnostics.Empty;
            IsSaving = false;
            IsLoading = false;
            PendingAutosave = false;
            LastFailure = string.Empty;
            LoadCoordinator = null;
            RecoveryOffer = LwsSaveRecoveryOffer.None;
            return LwsServiceResult.Success("LWS save facade shut down.");
        }

        public LwsSaveOperationResult RegisterParticipant(ILwsSaveParticipant participant)
        {
            if (participant == null)
            {
                return LwsSaveOperationResult.Failure("Cannot register a null save participant.");
            }

            if (string.IsNullOrWhiteSpace(participant.ParticipantId))
            {
                return LwsSaveOperationResult.Failure("Save participant ID must be stable and non-empty.");
            }

            if (!_participantIds.Add(participant.ParticipantId))
            {
                return LwsSaveOperationResult.Failure($"Duplicate save participant ID: {participant.ParticipantId}");
            }

            _participants.Add(participant);
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult UnregisterParticipant(string participantId)
        {
            int index = _participants.FindIndex(p => p.ParticipantId == participantId);
            if (index < 0)
            {
                return LwsSaveOperationResult.Failure($"Save participant not found: {participantId}");
            }

            _participantIds.Remove(participantId);
            _participants.RemoveAt(index);
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipants()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ILwsSaveParticipant participant in _participants)
            {
                if (participant == null)
                {
                    return LwsSaveOperationResult.Failure("Null save participant registered.");
                }

                if (string.IsNullOrWhiteSpace(participant.ParticipantId))
                {
                    return LwsSaveOperationResult.Failure("Save participant has an empty ID.");
                }

                if (!seen.Add(participant.ParticipantId))
                {
                    return LwsSaveOperationResult.Failure($"Duplicate save participant ID: {participant.ParticipantId}");
                }

                LwsSaveOperationResult participantResult = participant.ValidateParticipant();
                if (!participantResult.Succeeded)
                {
                    return participantResult;
                }
            }

            return LwsSaveOperationResult.Success("Save participants are valid.");
        }

        public LwsSaveSnapshot CaptureSnapshot(string profileId)
        {
            string resolvedProfileId = string.IsNullOrWhiteSpace(profileId)
                ? (string.IsNullOrWhiteSpace(_pendingProfileId) ? ActiveProfileId : _pendingProfileId)
                : profileId;
            LwsSaveProfileMetadata profile = _directory.FindProfile(resolvedProfileId) ?? ActiveProfile;
            string sceneName = SceneManager.GetActiveScene().name;
            return new LwsSaveSnapshot
            {
                schemaVersion = LwsSaveSchema.CurrentVersion,
                gameVersion = LwsSaveSchema.ResolveGameVersion(),
                profileId = profile != null ? profile.stableProfileId : resolvedProfileId ?? string.Empty,
                profileDisplayName = profile != null ? profile.DisplayNameOrFallback : string.Empty,
                slotType = _pendingSlotType,
                slotNumber = _pendingSlotNumber,
                vendorSlotNumber = _pendingVendorSlotNumber,
                sceneName = sceneName ?? string.Empty,
                capturedUtcTicks = DateTime.UtcNow.Ticks,
                participants = _participants.Select(p => p.CaptureState()).ToList()
            };
        }

        public LwsSaveOperationResult RestoreSnapshot(LwsSaveSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return LwsSaveOperationResult.Failure("Cannot restore a null save snapshot.");
            }

            if (!string.IsNullOrWhiteSpace(snapshot.profileId))
            {
                SelectProfile(snapshot.profileId);
            }

            if (snapshot.participants == null)
            {
                return LwsSaveOperationResult.Failure("Save snapshot has no participant list.");
            }

            foreach (LwsSaveParticipantState payload in snapshot.participants
                         .Where(p => p != null && !string.IsNullOrWhiteSpace(p.participantId))
                         .OrderBy(p => GetRestoreOrder(p.participantId)))
            {
                ILwsSaveParticipant participant = _participants.FirstOrDefault(p => p.ParticipantId == payload.participantId);
                if (participant == null)
                {
                    continue;
                }

                LwsSaveOperationResult result = participant.RestoreState(payload);
                if (!result.Succeeded)
                {
                    return result;
                }
            }

            return LwsSaveOperationResult.Success("LWS semantic save payload restored by Pixel Crushers saver.");
        }

        public LwsSaveOperationResult ClearAllParticipants()
        {
            foreach (ILwsSaveParticipant participant in _participants)
            {
                LwsSaveOperationResult result = participant.ClearState();
                if (!result.Succeeded)
                {
                    return result;
                }
            }

            return LwsSaveOperationResult.Success("Save participants cleared.");
        }

        public LwsSaveOperationResult CreateProfile(string displayName, out LwsSaveProfileMetadata profile)
        {
            EnsureDefaultProfile();
            profile = null;
            string cleanName = string.IsNullOrWhiteSpace(displayName) ? "Driver" : displayName.Trim();
            long now = DateTime.UtcNow.Ticks;
            int profileIndex = _directory.AllocateProfileIndex();
            profile = new LwsSaveProfileMetadata
            {
                stableProfileId = $"profile.{Guid.NewGuid():N}",
                displayName = cleanName,
                profileIndex = profileIndex,
                createdUtcTicks = now,
                lastPlayedUtcTicks = now,
                lastUsedManualSlot = 1,
                schemaVersion = LwsSaveSchema.CurrentVersion,
                gameVersion = LwsSaveSchema.ResolveGameVersion()
            };

            _directory.profiles.Add(profile);
            _directory.selectedProfileId = profile.stableProfileId;
            EnsureProfileSlots(profile);
            LwsSaveOperationResult result = PersistDirectory("CreateProfile");
            RefreshDiagnostics("Profile", result);
            return result.Succeeded
                ? LwsSaveOperationResult.Success($"Created save profile '{profile.DisplayNameOrFallback}'.")
                : result;
        }

        public LwsSaveOperationResult SelectProfile(string profileId)
        {
            EnsureDefaultProfile();
            LwsSaveProfileMetadata profile = _directory.FindProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure($"Save profile not found: {profileId}");
            }

            _directory.selectedProfileId = profile.stableProfileId;
            profile.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            EnsureProfileSlots(profile);
            LwsSaveOperationResult result = PersistDirectory("SelectProfile");
            RefreshDiagnostics("Profile", result);
            return result.Succeeded
                ? LwsSaveOperationResult.Success($"Selected save profile '{profile.DisplayNameOrFallback}'.")
                : result;
        }

        public LwsSaveOperationResult RenameProfile(string profileId, string displayName)
        {
            LwsSaveProfileMetadata profile = _directory.FindProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure($"Save profile not found: {profileId}");
            }

            profile.displayName = string.IsNullOrWhiteSpace(displayName) ? profile.DisplayNameOrFallback : displayName.Trim();
            profile.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            LwsSaveOperationResult result = PersistDirectory("RenameProfile");
            RefreshDiagnostics("Profile", result);
            return result.Succeeded
                ? LwsSaveOperationResult.Success($"Renamed save profile to '{profile.DisplayNameOrFallback}'.")
                : result;
        }

        public LwsSaveOperationResult DeleteProfile(string profileId)
        {
            LwsSaveProfileMetadata profile = _directory.FindProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure($"Save profile not found: {profileId}");
            }

            foreach (int vendorSlot in LwsSaveSchema.EnumerateReservedVendorSlots(profile.profileIndex))
            {
                if (Adapter != null && Adapter.ReadyForRuntimeSaves)
                {
                    Adapter.DeleteSlot(vendorSlot);
                }
            }

            _directory.RemoveProfile(profileId);
            EnsureDefaultProfile();
            LwsSaveOperationResult result = PersistDirectory("DeleteProfile");
            RefreshDiagnostics("Delete", result);
            return result.Succeeded
                ? LwsSaveOperationResult.Success($"Deleted save profile '{profile.DisplayNameOrFallback}'.")
                : result;
        }

        public IReadOnlyList<LwsManualSaveSlotMetadata> GetManualSlots(string profileId = null)
        {
            EnsureDefaultProfile();
            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            return _directory.GetManualSlotsForProfile(profile);
        }

        public LwsManualSaveSlotMetadata GetSlotMetadata(string profileId, int manualSlotNumber)
        {
            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            if (profile == null)
            {
                return null;
            }

            return _directory.GetOrCreateManualSlot(profile, Mathf.Clamp(manualSlotNumber, 1, LwsSaveSchema.ManualSlotCount));
        }

        public LwsManualSaveSlotMetadata GetAutosaveSlotMetadata(string profileId = null)
        {
            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            return profile != null ? _directory.GetOrCreateAutosaveSlot(profile) : null;
        }
        public int MapToVendorSlot(string profileId, LwsSaveSlotType slotType, int slotNumber)
        {
            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            int profileIndex = profile != null ? profile.profileIndex : 0;
            return LwsSaveSchema.MapToVendorSlot(profileIndex, slotType, slotNumber);
        }

        public bool HasSave(string profileId, int manualSlotNumber)
        {
            LwsManualSaveSlotMetadata slot = GetSlotMetadata(profileId, manualSlotNumber);
            if (slot == null)
            {
                return false;
            }

            bool vendorHasSave = Adapter != null && Adapter.ReadyForRuntimeSaves && Adapter.HasSaveInSlot(slot.vendorSlotNumber);
            return slot.occupied || vendorHasSave;
        }

        public bool HasAutosave(string profileId = null)
        {
            LwsManualSaveSlotMetadata slot = GetAutosaveSlotMetadata(profileId);
            if (slot == null)
            {
                return false;
            }

            bool vendorHasSave = Adapter != null && Adapter.ReadyForRuntimeSaves && Adapter.HasSaveInSlot(slot.vendorSlotNumber);
            return slot.occupied || vendorHasSave;
        }
        public LwsSaveOperationResult Save(string profileId, int manualSlotNumber, bool overwrite)
        {
            if (!IsOperationSafe(out string safeMessage))
            {
                LwsSaveOperationResult unsafeResult = LwsSaveOperationResult.Failure(safeMessage);
                RefreshDiagnostics("Save", unsafeResult);
                return unsafeResult;
            }

            LwsSaveOperationResult validation = ValidateParticipants();
            if (!validation.Succeeded)
            {
                RefreshDiagnostics("Save", validation);
                return validation;
            }

            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure("No active save profile is available.");
            }

            LwsManualSaveSlotMetadata slot = GetSlotMetadata(profile.stableProfileId, manualSlotNumber);
            if (slot == null)
            {
                return LwsSaveOperationResult.Failure("Manual save slot is not available.");
            }

            if (slot.occupied && !overwrite)
            {
                return LwsSaveOperationResult.Failure($"{slot.SlotLabel} already contains a save. Confirm overwrite first.");
            }

            LwsSaveOperationResult backup = PrepareBackupBeforeOverwrite(profile, slot, LwsSaveLoadSlotKind.ManualPrimary);
            if (!backup.Succeeded)
            {
                RefreshDiagnostics("Save", backup);
                SaveCompleted?.Invoke(backup, slot);
                return backup;
            }

            SetPendingContext(profile, LwsSaveSlotType.Manual, slot.slotNumber, slot.vendorSlotNumber);
            IsSaving = true;
            LwsSaveOperationResult result;
            try
            {
                result = Adapter != null
                    ? Adapter.SaveToSlotImmediate(slot.vendorSlotNumber)
                    : LwsSaveOperationResult.Failure("Pixel Crushers save adapter is not available.");
            }
            finally
            {
                IsSaving = false;
                ClearPendingContext();
            }

            if (result.Succeeded)
            {
                MarkSlotSaved(profile, slot, true);
                DismissRecoveryOffer();
                PendingAutosave = false;
                LwsSaveOperationResult directoryResult = PersistDirectory("Save");
                if (!directoryResult.Succeeded)
                {
                    result = directoryResult;
                }
            }

            LastFailure = result.Succeeded ? string.Empty : result.Message;
            RefreshDiagnostics("Save", result);
            SaveCompleted?.Invoke(result, slot);
            return result;
        }
        public LwsSaveOperationResult SaveAutosave(string profileId = null, string reason = null)
        {
            if (!_autosaveConfiguration.autosaveEnabled)
            {
                return LwsSaveOperationResult.Success("Autosave is disabled.");
            }

            if (!IsOperationSafe(out string safeMessage))
            {
                PendingAutosave = true;
                _pendingAutosaveReason = string.IsNullOrWhiteSpace(reason) ? "unsafe state" : reason;
                LwsSaveOperationResult deferred = LwsSaveOperationResult.Success($"Autosave deferred: {safeMessage}");
                RefreshDiagnostics("Autosave", deferred);
                return deferred;
            }

            LwsSaveOperationResult validation = ValidateParticipants();
            if (!validation.Succeeded)
            {
                RefreshDiagnostics("Autosave", validation);
                return validation;
            }

            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure("No active save profile is available for autosave.");
            }

            LwsManualSaveSlotMetadata slot = _directory.GetOrCreateAutosaveSlot(profile);
            if (slot == null)
            {
                return LwsSaveOperationResult.Failure("Autosave slot metadata is not available.");
            }

            LwsSaveOperationResult backup = PrepareBackupBeforeOverwrite(profile, slot, LwsSaveLoadSlotKind.AutosavePrimary);
            if (!backup.Succeeded)
            {
                RefreshDiagnostics("Autosave", backup);
                SaveCompleted?.Invoke(backup, slot);
                return backup;
            }

            SetPendingContext(profile, LwsSaveSlotType.Autosave, slot.slotNumber, slot.vendorSlotNumber);
            IsSaving = true;
            LwsSaveOperationResult result;
            try
            {
                result = Adapter != null
                    ? Adapter.SaveToSlotImmediate(slot.vendorSlotNumber)
                    : LwsSaveOperationResult.Failure("Pixel Crushers save adapter is not available.");
            }
            finally
            {
                IsSaving = false;
                ClearPendingContext();
            }

            if (result.Succeeded)
            {
                MarkSlotSaved(profile, slot, false);
                DismissRecoveryOffer();
                PendingAutosave = false;
                _pendingAutosaveReason = string.Empty;
                _autosaveElapsedSeconds = 0f;
                _lastAutosaveRealtime = Time.realtimeSinceStartup;
                LwsSaveOperationResult directoryResult = PersistDirectory("Autosave");
                if (!directoryResult.Succeeded)
                {
                    result = directoryResult;
                }
            }

            LastFailure = result.Succeeded ? string.Empty : result.Message;
            RefreshDiagnostics("Autosave", result);
            SaveCompleted?.Invoke(result, slot);
            return result;
        }

        public LwsSaveOperationResult RequestAutosave(string reason)
        {
            if (!_autosaveConfiguration.autosaveEnabled)
            {
                return LwsSaveOperationResult.Success("Autosave request ignored because autosave is disabled.");
            }

            _autosaveConfiguration.Sanitize();
            if (!IsOperationSafe(out string safeMessage))
            {
                PendingAutosave = true;
                _pendingAutosaveReason = string.IsNullOrWhiteSpace(reason) ? "requested" : reason;
                return LwsSaveOperationResult.Success($"Autosave pending: {safeMessage}");
            }

            float now = Time.realtimeSinceStartup;
            if (now - _lastAutosaveRealtime < _autosaveConfiguration.minimumTimeBetweenAutosavesSeconds)
            {
                PendingAutosave = true;
                _pendingAutosaveReason = string.IsNullOrWhiteSpace(reason) ? "minimum interval" : reason;
                return LwsSaveOperationResult.Success("Autosave pending until minimum interval elapses.");
            }

            return SaveAutosave(ActiveProfileId, reason);
        }
        public LwsSaveOperationResult Load(string profileId, int manualSlotNumber)
        {
            if (IsSaving || IsLoading || (LoadCoordinator != null && LoadCoordinator.IsLoadActive))
            {
                return LwsSaveOperationResult.Failure("A save/load operation is already in progress.");
            }

            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure("No active save profile is available.");
            }

            LwsManualSaveSlotMetadata slot = GetSlotMetadata(profile.stableProfileId, manualSlotNumber);
            if (slot == null || !HasSave(profile.stableProfileId, manualSlotNumber))
            {
                return LwsSaveOperationResult.Failure($"No saved game is available in manual slot {manualSlotNumber}.");
            }

            LwsLoadApplicationContext context = null;
            LwsSaveOperationResult preRead = LoadCoordinator != null
                ? LoadCoordinator.PreReadVendorSlot(profile, slot.vendorSlotNumber, LwsSaveLoadSlotKind.ManualPrimary, out context)
                : LwsSaveOperationResult.Failure("LWS load coordinator is not available.");
            if (!preRead.Succeeded)
            {
                LwsSaveOperationResult recovery = PrepareRecoveryOffer(profile, slot, LwsSaveLoadSlotKind.ManualPrimary, LwsSaveLoadSlotKind.ManualBackup, preRead.Message);
                RefreshDiagnostics("Load", preRead);
                LoadCompleted?.Invoke(preRead, slot);
                return recovery.Succeeded && RecoveryOffer.available ? LwsSaveOperationResult.Failure(RecoveryOffer.displayMessage) : preRead;
            }

            IsLoading = true;
            LwsSaveOperationResult result;
            try
            {
                result = LoadCoordinator.LoadPreparedVendorSlot(context);
            }
            finally
            {
                IsLoading = false;
            }

            if (result.Succeeded)
            {
                _directory.selectedProfileId = profile.stableProfileId;
                profile.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
                profile.lastUsedManualSlot = slot.slotNumber;
                DismissRecoveryOffer();
                PersistDirectory("Load");
            }
            else
            {
                PrepareRecoveryOffer(profile, slot, LwsSaveLoadSlotKind.ManualPrimary, LwsSaveLoadSlotKind.ManualBackup, result.Message);
            }

            LastFailure = result.Succeeded ? string.Empty : result.Message;
            RefreshDiagnostics("Load", result);
            LoadCompleted?.Invoke(result, slot);
            return result;
        }

        public LwsSaveOperationResult LoadAutosave(string profileId = null)
        {
            if (IsSaving || IsLoading || (LoadCoordinator != null && LoadCoordinator.IsLoadActive))
            {
                return LwsSaveOperationResult.Failure("A save/load operation is already in progress.");
            }

            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure("No active save profile is available.");
            }

            LwsManualSaveSlotMetadata slot = _directory.GetOrCreateAutosaveSlot(profile);
            if (slot == null || !HasAutosave(profile.stableProfileId))
            {
                return LwsSaveOperationResult.Failure("No autosave is available for the active profile.");
            }

            LwsLoadApplicationContext context = null;
            LwsSaveOperationResult preRead = LoadCoordinator != null
                ? LoadCoordinator.PreReadVendorSlot(profile, slot.vendorSlotNumber, LwsSaveLoadSlotKind.AutosavePrimary, out context)
                : LwsSaveOperationResult.Failure("LWS load coordinator is not available.");
            if (!preRead.Succeeded)
            {
                LwsSaveOperationResult recovery = PrepareRecoveryOffer(profile, slot, LwsSaveLoadSlotKind.AutosavePrimary, LwsSaveLoadSlotKind.AutosaveBackup, preRead.Message);
                RefreshDiagnostics("Load", preRead);
                LoadCompleted?.Invoke(preRead, slot);
                return recovery.Succeeded && RecoveryOffer.available ? LwsSaveOperationResult.Failure(RecoveryOffer.displayMessage) : preRead;
            }

            IsLoading = true;
            LwsSaveOperationResult result;
            try
            {
                result = LoadCoordinator.LoadPreparedVendorSlot(context);
            }
            finally
            {
                IsLoading = false;
            }

            if (result.Succeeded)
            {
                _directory.selectedProfileId = profile.stableProfileId;
                profile.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
                DismissRecoveryOffer();
                PersistDirectory("LoadAutosave");
            }
            else
            {
                PrepareRecoveryOffer(profile, slot, LwsSaveLoadSlotKind.AutosavePrimary, LwsSaveLoadSlotKind.AutosaveBackup, result.Message);
            }

            LastFailure = result.Succeeded ? string.Empty : result.Message;
            RefreshDiagnostics("Load", result);
            LoadCompleted?.Invoke(result, slot);
            return result;
        }

        public LwsSaveOperationResult LoadRecoveryBackup()
        {
            if (RecoveryOffer == null || !RecoveryOffer.available)
            {
                return LwsSaveOperationResult.Failure("No recovery backup is currently available.");
            }

            if (IsSaving || IsLoading || (LoadCoordinator != null && LoadCoordinator.IsLoadActive))
            {
                return LwsSaveOperationResult.Failure("A save/load operation is already in progress.");
            }

            LwsSaveRecoveryOffer offer = RecoveryOffer;
            LwsSaveProfileMetadata profile = ResolveProfile(offer.profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure("Recovery backup profile is not available.");
            }

            LwsLoadApplicationContext context = null;
            LwsSaveOperationResult preRead = LoadCoordinator != null
                ? LoadCoordinator.PreReadVendorSlot(profile, offer.backupVendorSlotNumber, offer.backupSlotKind, out context)
                : LwsSaveOperationResult.Failure("LWS load coordinator is not available.");
            if (!preRead.Succeeded)
            {
                RecoveryOffer.failureMessage = preRead.Message;
                RefreshDiagnostics("Load", preRead);
                return preRead;
            }

            IsLoading = true;
            LwsSaveOperationResult result;
            try
            {
                result = LoadCoordinator.LoadPreparedVendorSlot(context);
            }
            finally
            {
                IsLoading = false;
            }

            if (result.Succeeded)
            {
                _directory.selectedProfileId = profile.stableProfileId;
                profile.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
                DismissRecoveryOffer();
                PersistDirectory("LoadRecovery");
            }

            LastFailure = result.Succeeded ? string.Empty : result.Message;
            RefreshDiagnostics("Load", result);
            return result;
        }

        public void DismissRecoveryOffer()
        {
            RecoveryOffer = LwsSaveRecoveryOffer.None;
        }
        public LwsSaveOperationResult Delete(string profileId, int manualSlotNumber)
        {
            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure("No active save profile is available.");
            }

            LwsManualSaveSlotMetadata slot = GetSlotMetadata(profile.stableProfileId, manualSlotNumber);
            if (slot == null)
            {
                return LwsSaveOperationResult.Failure($"Manual save slot {manualSlotNumber} is not available.");
            }

            LwsSaveOperationResult result = Adapter != null && Adapter.ReadyForRuntimeSaves
                ? Adapter.DeleteSlot(slot.vendorSlotNumber)
                : LwsSaveOperationResult.Success("Slot metadata cleared; Pixel Crushers runtime storage is not active in Edit Mode.");
            if (result.Succeeded && slot.backupVendorSlotNumber > 0 && Adapter != null && Adapter.ReadyForRuntimeSaves)
            {
                Adapter.DeleteSlot(slot.backupVendorSlotNumber);
            }

            if (result.Succeeded)
            {
                ClearSlotMetadata(slot, true);
                LwsManualSaveSlotMetadata backupSlot = _directory.GetOrCreateManualBackupSlot(profile, slot.slotNumber);
                ClearSlotMetadata(backupSlot, true);
                LwsSaveOperationResult directoryResult = PersistDirectory("Delete");
                if (!directoryResult.Succeeded)
                {
                    result = directoryResult;
                }
            }

            LastFailure = result.Succeeded ? string.Empty : result.Message;
            RefreshDiagnostics("Delete", result);
            DeleteCompleted?.Invoke(result, profile.stableProfileId, manualSlotNumber);
            return result;
        }

        public LwsSaveOperationResult DeleteAutosave(string profileId = null)
        {
            LwsSaveProfileMetadata profile = ResolveProfile(profileId);
            if (profile == null)
            {
                return LwsSaveOperationResult.Failure("No active save profile is available.");
            }

            LwsManualSaveSlotMetadata slot = _directory.GetOrCreateAutosaveSlot(profile);
            if (slot == null)
            {
                return LwsSaveOperationResult.Failure("Autosave slot metadata is not available.");
            }

            LwsSaveOperationResult result = Adapter != null && Adapter.ReadyForRuntimeSaves
                ? Adapter.DeleteSlot(slot.vendorSlotNumber)
                : LwsSaveOperationResult.Success("Autosave metadata cleared; Pixel Crushers runtime storage is not active in Edit Mode.");
            if (result.Succeeded && slot.backupVendorSlotNumber > 0 && Adapter != null && Adapter.ReadyForRuntimeSaves)
            {
                Adapter.DeleteSlot(slot.backupVendorSlotNumber);
            }

            if (result.Succeeded)
            {
                ClearSlotMetadata(slot, true);
                LwsManualSaveSlotMetadata backupSlot = _directory.GetOrCreateAutosaveBackupSlot(profile);
                ClearSlotMetadata(backupSlot, true);
                LwsSaveOperationResult directoryResult = PersistDirectory("DeleteAutosave");
                if (!directoryResult.Succeeded)
                {
                    result = directoryResult;
                }
            }

            LastFailure = result.Succeeded ? string.Empty : result.Message;
            RefreshDiagnostics("Delete", result);
            DeleteCompleted?.Invoke(result, profile.stableProfileId, slot.slotNumber);
            return result;
        }

        public void TickAutosave(float deltaTimeSeconds)
        {
            if (!Application.isPlaying || !_autosaveConfiguration.autosaveEnabled)
            {
                return;
            }

            _autosaveConfiguration.Sanitize();
            _autosaveElapsedSeconds += Mathf.Max(0f, deltaTimeSeconds);
            bool intervalDue = _autosaveElapsedSeconds >= _autosaveConfiguration.autosaveIntervalSeconds;
            if (!intervalDue && !PendingAutosave)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now - _lastAutosaveRealtime < _autosaveConfiguration.minimumTimeBetweenAutosavesSeconds)
            {
                PendingAutosave = true;
                return;
            }

            if (!IsOperationSafe(out string safeMessage))
            {
                PendingAutosave = true;
                _pendingAutosaveReason = safeMessage;
                return;
            }

            SaveAutosave(ActiveProfileId, string.IsNullOrWhiteSpace(_pendingAutosaveReason) ? "periodic" : _pendingAutosaveReason);
        }
        public string BuildDiagnosticsReport()
        {
            LwsWorldPositionD global = LwsWorldPositionD.Zero;
            if (_registry != null && _registry.TryGet(out ILwsWorldOriginService origin))
            {
                global = origin.PlayerGlobalPosition;
            }

            LwsGameClockSnapshot clock = default;
            ILwsGameClockService clockService = null;
            bool clockAvailable = _registry != null && _registry.TryGet(out clockService);
            if (clockAvailable && clockService != null)
            {
                clock = clockService.CurrentSnapshot;
            }

            string weather = "missing";
            if (_registry != null && _registry.TryGet(out ILwsWeatherService weatherService))
            {
                weather = weatherService.CurrentSnapshot.weatherPresetId;
            }

            string road = "missing";
            if (_registry != null && _registry.TryGet(out ILwsRoadConditionService roadService))
            {
                road = $"{roadService.CurrentSnapshot.MajorGameplayState} / {roadService.Mode}";
            }

            string route = "inactive";
            if (_registry != null && _registry.TryGet(out ILwsNavigationService navigationService) && navigationService.RuntimeState != null)
            {
                route = navigationService.RuntimeState.routeActive
                    ? $"{navigationService.RuntimeState.destinationId} / {navigationService.RuntimeState.distanceRemainingMeters:0} m"
                    : navigationService.RuntimeState.status.ToString();
            }

            return
                "SAVE / PERSISTENCE DIAGNOSTICS\n" +
                "Save framework authority: PIXEL CRUSHERS\n" +
                $"Pixel Crushers available: {(Adapter != null && Adapter.PixelCrushersAvailable ? "YES" : "NO")}\n" +
                $"Pixel Crushers Common version: {LwsPixelCrushersSaveAdapter.KnownCommonPackageVersion}\n" +
                $"Dialogue System version: {LwsPixelCrushersSaveAdapter.KnownDialogueSystemVersion}\n" +
                $"LWS adapter: {(Adapter != null ? Adapter.Status : "missing")}\n" +
                $"Serializer: {Diagnostics.activeSerializer}\n" +
                $"Storer: {Diagnostics.activeStorer}\n" +
                $"Profiles: {Profiles.Count}\n" +
                $"Active profile: {(ActiveProfile != null ? ActiveProfile.DisplayNameOrFallback : "none")}\n" +
                $"Current save-state providers: {Participants.Count}\n" +
                $"Current global player position: {global}\n" +
                $"Current clock: {(clockAvailable ? $"{clock.DateText} {clock.ClockText} {clock.timeScale:0.##}x paused:{clock.paused}" : "missing")}\n" +
                $"Current weather: {weather}\n" +
                $"Current road condition: {road}\n" +
                $"Current route: {route}\n" +
                $"Last save operation: {Diagnostics.LastSaveMessage}\n" +
                $"Last load operation: {Diagnostics.LastLoadMessage}\n" +
                $"Last delete operation: {Diagnostics.LastDeleteMessage}\n" +
                $"Last vendor error: {Diagnostics.LastVendorError}";
        }

        private void RegisterSemanticParticipants()
        {
            RegisterParticipant(new LwsWorldResumeSaveParticipant(() => _registry));
            RegisterParticipant(new LwsGlobalPositionSaveParticipant(() => _registry));
            RegisterParticipant(new LwsPlayerTruckSaveParticipant(() => _registry));
            RegisterParticipant(new LwsGameClockSaveParticipant(() => _registry));
            RegisterParticipant(new LwsWeatherSaveParticipant(() => _registry));
            RegisterParticipant(new LwsRoadConditionSaveParticipant(() => _registry));
            RegisterParticipant(new LwsActiveJobSaveParticipant(() => _registry));
            RegisterParticipant(new LwsNavigationSaveParticipant(() => _registry));
        }

        private void EnsureDefaultProfile()
        {
            _directory = _directory ?? new LwsSaveProfileDirectory();
            _directory.EnsureValid();
            if (_directory.profiles.Count == 0)
            {
                long now = DateTime.UtcNow.Ticks;
                var profile = new LwsSaveProfileMetadata
                {
                    stableProfileId = DefaultProfileId,
                    displayName = DefaultProfileName,
                    profileIndex = 0,
                    createdUtcTicks = now,
                    lastPlayedUtcTicks = now,
                    lastUsedManualSlot = 1,
                    schemaVersion = LwsSaveSchema.CurrentVersion,
                    gameVersion = LwsSaveSchema.ResolveGameVersion()
                };
                _directory.profiles.Add(profile);
                _directory.selectedProfileId = profile.stableProfileId;
                _directory.nextProfileIndex = Mathf.Max(_directory.nextProfileIndex, 1);
            }

            foreach (LwsSaveProfileMetadata profile in _directory.profiles)
            {
                EnsureProfileSlots(profile);
            }

            if (_directory.FindProfile(_directory.selectedProfileId) == null)
            {
                _directory.selectedProfileId = _directory.profiles[0].stableProfileId;
            }
        }

        private LwsSaveProfileMetadata ResolveProfile(string profileId)
        {
            EnsureDefaultProfile();
            return string.IsNullOrWhiteSpace(profileId) ? ActiveProfile : _directory.FindProfile(profileId);
        }

        private void EnsureProfileSlots(LwsSaveProfileMetadata profile)
        {
            if (profile == null)
            {
                return;
            }

            for (int i = 1; i <= LwsSaveSchema.ManualSlotCount; i++)
            {
                _directory.GetOrCreateManualSlot(profile, i);
                _directory.GetOrCreateManualBackupSlot(profile, i);
            }

            _directory.GetOrCreateAutosaveSlot(profile);
            _directory.GetOrCreateAutosaveBackupSlot(profile);
        }

        private LwsSaveOperationResult PersistDirectory(string operation)
        {
            _directory.EnsureValid();
            return Adapter != null
                ? Adapter.StoreProfileDirectory(_directory)
                : LwsSaveOperationResult.Failure($"Cannot persist profile directory during {operation}; Pixel Crushers adapter is missing.");
        }

        private void MarkSlotSaved(LwsSaveProfileMetadata profile, LwsManualSaveSlotMetadata slot, bool markManualAsLastUsed)
        {
            long now = DateTime.UtcNow.Ticks;
            profile.lastPlayedUtcTicks = now;
            if (markManualAsLastUsed && slot.slotType == LwsSaveSlotType.Manual)
            {
                profile.lastUsedManualSlot = slot.slotNumber;
            }

            slot.occupied = true;
            slot.savedUtcTicks = now;
            slot.schemaVersion = LwsSaveSchema.CurrentVersion;
            slot.gameVersion = LwsSaveSchema.ResolveGameVersion();
            slot.sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            slot.worldLabel = ResolveWorldLabel();
            slot.truckDefinitionId = ResolveTruckDefinitionId();
            slot.routeDestinationId = ResolveRouteDestinationId();
            slot.weatherPresetId = ResolveWeatherPresetId();
        }

        private void ClearSlotMetadata(LwsManualSaveSlotMetadata slot, bool clearBackupFields = false)
        {
            if (slot == null)
            {
                return;
            }

            slot.occupied = false;
            slot.savedUtcTicks = 0;
            slot.playtimeSeconds = 0d;
            slot.sceneName = string.Empty;
            slot.worldLabel = string.Empty;
            slot.truckDefinitionId = string.Empty;
            slot.routeDestinationId = string.Empty;
            slot.weatherPresetId = string.Empty;
            if (clearBackupFields)
            {
                slot.backupExists = false;
                slot.backupSavedUtcTicks = 0;
            }
        }

        private string ResolveWorldLabel()
        {
            if (_registry != null && _registry.TryGet(out ILwsWorldStreamingService streaming) && !string.IsNullOrWhiteSpace(streaming.WorldId))
            {
                return streaming.WorldId;
            }

            return SceneManager.GetActiveScene().name ?? string.Empty;
        }

        private string ResolveTruckDefinitionId()
        {
            return _registry != null && _registry.TryGet(out ILwsPlayerVehicleService vehicle) && vehicle.ActiveTruck != null
                ? vehicle.ActiveTruck.DefinitionId
                : string.Empty;
        }

        private string ResolveRouteDestinationId()
        {
            return _registry != null && _registry.TryGet(out ILwsNavigationService navigation) && navigation.RuntimeState != null
                ? navigation.RuntimeState.destinationId ?? string.Empty
                : string.Empty;
        }

        private string ResolveWeatherPresetId()
        {
            return _registry != null && _registry.TryGet(out ILwsWeatherService weather)
                ? weather.CurrentSnapshot.weatherPresetId ?? string.Empty
                : string.Empty;
        }

        private LwsSaveOperationResult PrepareBackupBeforeOverwrite(LwsSaveProfileMetadata profile, LwsManualSaveSlotMetadata slot, LwsSaveLoadSlotKind primaryKind)
        {
            if (profile == null || slot == null)
            {
                return LwsSaveOperationResult.Failure("Cannot prepare a backup without profile and slot metadata.");
            }

            bool hasExistingPrimary = slot.occupied || (Adapter != null && Adapter.ReadyForRuntimeSaves && Adapter.HasSaveInSlot(slot.vendorSlotNumber));
            if (!hasExistingPrimary)
            {
                return LwsSaveOperationResult.Success("No existing primary save to back up.");
            }

            if (slot.backupVendorSlotNumber <= 0)
            {
                return LwsSaveOperationResult.Failure($"{slot.SlotLabel} has no reserved backup vendor slot.");
            }

            if (Adapter == null || !Adapter.ReadyForRuntimeSaves)
            {
                return LwsSaveOperationResult.Failure("Pixel Crushers adapter must be ready before preserving a backup.");
            }

            if (!Adapter.HasSaveInSlot(slot.vendorSlotNumber))
            {
                return LwsSaveOperationResult.Success("Primary slot metadata was occupied, but Pixel Crushers has no primary data to preserve.");
            }

            LwsSaveOperationResult copy = Adapter.CopySavedGameDataSlot(slot.vendorSlotNumber, slot.backupVendorSlotNumber);
            if (!copy.Succeeded)
            {
                return copy;
            }

            long backupTicks = slot.savedUtcTicks > 0 ? slot.savedUtcTicks : DateTime.UtcNow.Ticks;
            slot.backupExists = true;
            slot.backupSavedUtcTicks = backupTicks;

            LwsManualSaveSlotMetadata backupSlot = primaryKind == LwsSaveLoadSlotKind.AutosavePrimary
                ? _directory.GetOrCreateAutosaveBackupSlot(profile)
                : _directory.GetOrCreateManualBackupSlot(profile, slot.slotNumber);
            if (backupSlot != null)
            {
                backupSlot.occupied = true;
                backupSlot.savedUtcTicks = backupTicks;
                backupSlot.schemaVersion = slot.schemaVersion;
                backupSlot.gameVersion = slot.gameVersion;
                backupSlot.sceneName = slot.sceneName;
                backupSlot.worldLabel = slot.worldLabel;
                backupSlot.truckDefinitionId = slot.truckDefinitionId;
                backupSlot.routeDestinationId = slot.routeDestinationId;
                backupSlot.weatherPresetId = slot.weatherPresetId;
            }

            return LwsSaveOperationResult.Success($"Previous {slot.SlotLabel} preserved in hidden Pixel Crushers backup slot.");
        }

        private LwsSaveOperationResult PrepareRecoveryOffer(
            LwsSaveProfileMetadata profile,
            LwsManualSaveSlotMetadata primarySlot,
            LwsSaveLoadSlotKind primaryKind,
            LwsSaveLoadSlotKind backupKind,
            string failureMessage)
        {
            RecoveryOffer = LwsSaveRecoveryOffer.None;
            if (profile == null || primarySlot == null || primarySlot.backupVendorSlotNumber <= 0)
            {
                return LwsSaveOperationResult.Failure("No recovery backup metadata is available.");
            }

            if (Adapter == null || !Adapter.ReadyForRuntimeSaves || !Adapter.HasSaveInSlot(primarySlot.backupVendorSlotNumber))
            {
                return LwsSaveOperationResult.Failure("No Pixel Crushers backup save is available for recovery.");
            }

            LwsLoadApplicationContext backupContext = null;
            LwsSaveOperationResult validation = LoadCoordinator != null
                ? LoadCoordinator.PreReadVendorSlot(profile, primarySlot.backupVendorSlotNumber, backupKind, out backupContext, false)
                : LwsSaveOperationResult.Failure("LWS load coordinator is not available for backup validation.");
            if (LoadCoordinator != null)
            {
                LoadCoordinator.ResetAfterFailure();
            }

            if (!validation.Succeeded)
            {
                return validation;
            }

            long backupTicks = primarySlot.backupSavedUtcTicks > 0
                ? primarySlot.backupSavedUtcTicks
                : backupContext != null && backupContext.Snapshot != null ? backupContext.Snapshot.capturedUtcTicks : DateTime.UtcNow.Ticks;
            string timestamp = FormatUtc(backupTicks);
            string displayMessage = $"SAVE COULD NOT BE LOADED. A BACKUP FROM {timestamp} IS AVAILABLE.";
            RecoveryOffer = new LwsSaveRecoveryOffer
            {
                available = true,
                profileId = profile.stableProfileId,
                primarySlotNumber = primarySlot.slotNumber,
                primarySlotKind = primaryKind,
                primaryVendorSlotNumber = primarySlot.vendorSlotNumber,
                backupSlotKind = backupKind,
                backupVendorSlotNumber = primarySlot.backupVendorSlotNumber,
                backupTimestampUtcTicks = backupTicks,
                failureMessage = failureMessage ?? string.Empty,
                displayMessage = displayMessage
            };
            return LwsSaveOperationResult.Success(displayMessage);
        }

        private bool IsOperationSafe(out string message)
        {
            if (IsSaving)
            {
                message = "PLEASE WAIT... save already in progress.";
                return false;
            }

            if (IsLoading || (LoadCoordinator != null && LoadCoordinator.IsLoadActive))
            {
                message = "PLEASE WAIT... load already in progress.";
                return false;
            }

            if (Adapter == null || !Adapter.ReadyForRuntimeSaves)
            {
                message = "PLEASE WAIT... Pixel Crushers runtime storage is not ready.";
                return false;
            }

            if (ActiveProfile == null)
            {
                message = "PLEASE WAIT... no active save profile is selected.";
                return false;
            }

            if (_registry != null && _registry.TryGet(out ILwsWorldOriginService originService) && originService.ShiftInProgress)
            {
                message = "PLEASE WAIT... world origin shift in progress.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private void SetPendingContext(LwsSaveProfileMetadata profile, LwsSaveSlotType slotType, int slotNumber, int vendorSlotNumber)
        {
            _pendingProfileId = profile != null ? profile.stableProfileId : string.Empty;
            _pendingSlotType = slotType;
            _pendingSlotNumber = slotNumber;
            _pendingVendorSlotNumber = vendorSlotNumber;
        }

        private void EnsureAutosaveRuntimeDriver()
        {
            if (!Application.isPlaying || _autosaveDriver != null)
            {
                return;
            }

            LwsAutosaveRuntimeDriver[] existing = UnityEngine.Object.FindObjectsByType<LwsAutosaveRuntimeDriver>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0)
            {
                _autosaveDriver = existing[0];
                for (int i = 1; i < existing.Length; i++)
                {
                    if (existing[i] != null)
                    {
                        UnityEngine.Object.Destroy(existing[i].gameObject);
                    }
                }
            }
            else
            {
                GameObject driverObject = new GameObject("IH Autosave Runtime Driver");
                _autosaveDriver = driverObject.AddComponent<LwsAutosaveRuntimeDriver>();
                UnityEngine.Object.DontDestroyOnLoad(driverObject);
            }

            _autosaveDriver.Bind(this);
        }

        private static int GetRestoreOrder(string participantId)
        {
            if (string.Equals(participantId, LwsSaveSchema.WorldResumeContextParticipantId, StringComparison.Ordinal))
            {
                return 0;
            }

            switch (participantId)
            {
                case "lws.world.global-position":
                    return 10;
                case "lws.vehicle.player-truck":
                    return 20;
                case "vehicle.transmission.player":
                    return 30;
                case "lws.game-clock":
                    return 40;
                case "lws.weather.semantic":
                    return 50;
                case "lws.road-condition.semantic":
                    return 60;
                case "lws.job.active":
                    return 65;
                case "lws.navigation.destination-intent":
                    return 70;
                default:
                    return 100;
            }
        }

        private static string FormatUtc(long utcTicks)
        {
            if (utcTicks <= 0)
            {
                return "unknown time";
            }

            return new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        private void RefreshDiagnostics(string operation, LwsSaveOperationResult result)
        {
            if (string.Equals(operation, "Save", StringComparison.OrdinalIgnoreCase) || string.Equals(operation, "Autosave", StringComparison.OrdinalIgnoreCase))
            {
                _lastSaveMessage = result.Message;
            }
            else if (string.Equals(operation, "Load", StringComparison.OrdinalIgnoreCase))
            {
                _lastLoadMessage = result.Message;
            }
            else if (string.Equals(operation, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                _lastDeleteMessage = result.Message;
            }

            LwsSaveDiagnostics diagnostics = LwsSaveDiagnostics.FromState(
                Adapter,
                Participants.Count,
                Profiles.Count,
                ActiveProfile != null ? ActiveProfile.DisplayNameOrFallback : "none",
                operation,
                result);
            diagnostics.lastSaveMessage = _lastSaveMessage;
            diagnostics.lastLoadMessage = _lastLoadMessage;
            diagnostics.lastDeleteMessage = _lastDeleteMessage;
            Diagnostics = diagnostics;
        }

        private void ClearPendingContext()
        {
            _pendingProfileId = string.Empty;
            _pendingSlotType = LwsSaveSlotType.Manual;
            _pendingSlotNumber = 0;
            _pendingVendorSlotNumber = 0;
        }
    }

    public sealed class LwsPixelCrushersSaveAdapter
    {
        public const string SaveSystemTypeName = "PixelCrushers.SaveSystem";
        public const string SavedGameDataTypeName = "PixelCrushers.SavedGameData";
        public const string SavedGameDataStorerTypeName = "PixelCrushers.SavedGameDataStorer";
        public const string DiskSavedGameDataStorerTypeName = "PixelCrushers.DiskSavedGameDataStorer";
        public const string PlayerPrefsSavedGameDataStorerTypeName = "PixelCrushers.PlayerPrefsSavedGameDataStorer";
        public const string JsonDataSerializerTypeName = "PixelCrushers.JsonDataSerializer";
        public const string BinaryDataSerializerTypeName = "PixelCrushers.BinaryDataSerializer";
        public const string SaverTypeName = "PixelCrushers.Saver";
        public const string DialogueSystemSaverTypeName = "PixelCrushers.DialogueSystem.DialogueSystemSaver";
        public const string GameSaverTypeName = "PixelCrushers.DialogueSystem.GameSaver";
        public const string SemanticSaverTypeName = "LWS.InterstateHauler.LwsPixelCrushersSemanticSaver";
        public const string KnownCommonPackageVersion = "1.10.73";
        public const string KnownDialogueSystemVersion = "2.2.73.2";
        public const string KnownPackageVersion = KnownDialogueSystemVersion;
        public const string RootPath = "Assets/Plugins/Pixel Crushers";
        public const string AssetName = "Pixel Crushers Common Save System / Dialogue System";

        private ILwsSaveService _saveService;
        private Type _saveSystemType;
        private Type _savedGameDataType;
        private Type _savedGameDataStorerType;
        private Type _diskStorerType;
        private Type _jsonSerializerType;
        private Type _semanticSaverType;
        private MethodInfo _saveToSlotImmediateMethod;
        private MethodInfo _loadFromSlotMethod;
        private MethodInfo _hasSavedGameInSlotMethod;
        private MethodInfo _deleteSavedGameInSlotMethod;
        private MethodInfo _serializeMethod;
        private MethodInfo _deserializeGenericMethod;
        private MethodInfo _setDataMethod;
        private MethodInfo _getDataMethod;
        private MethodInfo _storeSavedGameDataMethod;
        private MethodInfo _retrieveSavedGameDataMethod;
        private MethodInfo _hasDataInSlotMethod;
        private PropertyInfo _saveSystemInstanceProperty;
        private PropertyInfo _saveSystemStorerProperty;
        private PropertyInfo _saveCurrentSceneProperty;
        private PropertyInfo _maxSaveSlotProperty;
        private PropertyInfo _versionProperty;
        private PropertyInfo _savedGameVersionProperty;
        private PropertyInfo _savedGameSceneNameProperty;
        private FieldInfo _storerField;
        private FieldInfo _serializerField;
        private Component _activeStorer;
        private Component _activeSerializer;
        private MonoBehaviour _semanticSaver;

        public bool PixelCrushersAvailable => _saveSystemType != null && _savedGameDataType != null;
        public bool Initialized { get; private set; }
        public bool RuntimeBound { get; private set; }
        public bool ReadyForRuntimeSaves => Initialized && RuntimeBound && _activeStorer != null && _semanticSaver != null;
        public string Status { get; private set; } = "Not initialized.";
        public string LastVendorError { get; private set; } = string.Empty;
        public string ActiveStorerTypeName { get; private set; } = "unknown";
        public string ActiveSerializerTypeName { get; private set; } = "unknown";

        public LwsSaveOperationResult Initialize(ILwsSaveService saveService)
        {
            _saveService = saveService;
            try
            {
                ResolveTypesAndMethods();
                if (_saveSystemType == null || _savedGameDataType == null || _savedGameDataStorerType == null)
                {
                    return Fail("PIXEL CRUSHERS SAVE SYSTEM UNAVAILABLE: SaveSystem, SavedGameData, or SavedGameDataStorer type was not found.");
                }

                Initialized = true;
                if (!Application.isPlaying)
                {
                    Status = "Pixel Crushers Save System types resolved; runtime binding deferred until Play Mode.";
                    return LwsSaveOperationResult.Success(Status);
                }

                return BindRuntimeAuthority();
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers adapter initialization failed: {ex.Message}");
            }
        }

        public LwsSaveOperationResult SaveToSlotImmediate(int vendorSlotNumber)
        {
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            try
            {
                _saveToSlotImmediateMethod.Invoke(null, new object[] { vendorSlotNumber });
                return LwsSaveOperationResult.Success($"Saved through Pixel Crushers slot {vendorSlotNumber}.");
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers SaveToSlotImmediate failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers SaveToSlotImmediate failed: {ex.Message}");
            }
        }

        public LwsSaveOperationResult LoadFromSlot(int vendorSlotNumber)
        {
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            if (!HasSaveInSlot(vendorSlotNumber))
            {
                return LwsSaveOperationResult.Failure($"Pixel Crushers slot {vendorSlotNumber} does not contain saved data.");
            }

            try
            {
                _loadFromSlotMethod.Invoke(null, new object[] { vendorSlotNumber });
                return LwsSaveOperationResult.Success($"Loaded through Pixel Crushers slot {vendorSlotNumber}.");
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers LoadFromSlot failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers LoadFromSlot failed: {ex.Message}");
            }
        }

        public bool HasSaveInSlot(int vendorSlotNumber)
        {
            if (!Application.isPlaying || !ReadyForRuntimeSaves || _hasSavedGameInSlotMethod == null)
            {
                return false;
            }

            try
            {
                return (bool)_hasSavedGameInSlotMethod.Invoke(null, new object[] { vendorSlotNumber });
            }
            catch (Exception ex)
            {
                LastVendorError = ex.Message;
                return false;
            }
        }

        public LwsSaveOperationResult DeleteSlot(int vendorSlotNumber)
        {
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            try
            {
                _deleteSavedGameInSlotMethod.Invoke(null, new object[] { vendorSlotNumber });
                return LwsSaveOperationResult.Success($"Deleted Pixel Crushers slot {vendorSlotNumber}.");
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers DeleteSavedGameInSlot failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers DeleteSavedGameInSlot failed: {ex.Message}");
            }
        }

        public LwsSaveOperationResult TryReadSemanticSnapshot(
            int vendorSlotNumber,
            out LwsSaveSnapshot snapshot,
            out int vendorVersion,
            out string vendorSceneName)
        {
            snapshot = null;
            vendorVersion = 0;
            vendorSceneName = string.Empty;
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            try
            {
                bool hasData = _hasDataInSlotMethod != null && (bool)_hasDataInSlotMethod.Invoke(_activeStorer, new object[] { vendorSlotNumber });
                if (!hasData)
                {
                    return LwsSaveOperationResult.Failure($"Pixel Crushers slot {vendorSlotNumber} does not contain saved data.");
                }

                object savedGameData = _retrieveSavedGameDataMethod.Invoke(_activeStorer, new object[] { vendorSlotNumber });
                if (savedGameData == null)
                {
                    return LwsSaveOperationResult.Failure($"Pixel Crushers slot {vendorSlotNumber} returned no SavedGameData.");
                }

                vendorVersion = GetSavedGameVersion(savedGameData);
                vendorSceneName = GetSavedGameSceneName(savedGameData);
                string payload = _getDataMethod.Invoke(savedGameData, new object[] { LwsSaveSchema.SemanticSnapshotRecordKey }) as string;
                if (string.IsNullOrWhiteSpace(payload))
                {
                    return LwsSaveOperationResult.Failure($"Pixel Crushers slot {vendorSlotNumber} has no LWS semantic snapshot record.");
                }

                snapshot = DeserializeWithPixelCrushers<LwsSaveSnapshot>(payload);
                return snapshot != null
                    ? LwsSaveOperationResult.Success($"Pixel Crushers slot {vendorSlotNumber} pre-read completed.")
                    : LwsSaveOperationResult.Failure($"Pixel Crushers slot {vendorSlotNumber} semantic snapshot could not be deserialized.");
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers SavedGameData pre-read failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers SavedGameData pre-read failed: {ex.Message}");
            }
        }

        public LwsSaveOperationResult CopySavedGameDataSlot(int sourceVendorSlotNumber, int backupVendorSlotNumber)
        {
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            try
            {
                bool hasData = _hasDataInSlotMethod != null && (bool)_hasDataInSlotMethod.Invoke(_activeStorer, new object[] { sourceVendorSlotNumber });
                if (!hasData)
                {
                    return LwsSaveOperationResult.Success($"Pixel Crushers source slot {sourceVendorSlotNumber} is empty; no backup needed.");
                }

                object savedGameData = _retrieveSavedGameDataMethod.Invoke(_activeStorer, new object[] { sourceVendorSlotNumber });
                if (savedGameData == null)
                {
                    return LwsSaveOperationResult.Failure($"Pixel Crushers source slot {sourceVendorSlotNumber} returned no data for backup.");
                }

                _storeSavedGameDataMethod.Invoke(_activeStorer, new object[] { backupVendorSlotNumber, savedGameData });
                return LwsSaveOperationResult.Success($"Pixel Crushers slot {sourceVendorSlotNumber} backed up to hidden slot {backupVendorSlotNumber}.");
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers backup slot copy failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers backup slot copy failed: {ex.Message}");
            }
        }
        public LwsSaveOperationResult StoreProfileDirectory(LwsSaveProfileDirectory directory)
        {
            if (!Application.isPlaying)
            {
                return LwsSaveOperationResult.Success("Profile directory updated in memory; Pixel Crushers runtime storage is inactive in Edit Mode.");
            }

            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            if (directory == null)
            {
                return LwsSaveOperationResult.Failure("Cannot store a null save profile directory.");
            }

            try
            {
                directory.EnsureValid();
                object savedGameData = Activator.CreateInstance(_savedGameDataType);
                SetSavedGameVersion(savedGameData, LwsSaveSchema.CurrentVersion);
                SetSavedGameSceneName(savedGameData, SceneManager.GetActiveScene().name ?? string.Empty);
                string json = SerializeWithPixelCrushers(directory);
                _setDataMethod.Invoke(savedGameData, new object[] { LwsSaveSchema.ProfileDirectoryRecordKey, -1, json });
                _storeSavedGameDataMethod.Invoke(_activeStorer, new object[] { LwsSaveSchema.ProfileDirectoryVendorSlot, savedGameData });
                return LwsSaveOperationResult.Success("Profile directory stored through Pixel Crushers SavedGameDataStorer.");
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers profile directory store failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers profile directory store failed: {ex.Message}");
            }
        }

        public LwsSaveOperationResult LoadProfileDirectory(out LwsSaveProfileDirectory directory)
        {
            directory = null;
            if (!Application.isPlaying)
            {
                return LwsSaveOperationResult.Success("Profile directory runtime load deferred until Play Mode.");
            }

            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            try
            {
                bool hasDirectory = (bool)_hasDataInSlotMethod.Invoke(_activeStorer, new object[] { LwsSaveSchema.ProfileDirectoryVendorSlot });
                if (!hasDirectory)
                {
                    return LwsSaveOperationResult.Success("No Pixel Crushers profile directory save exists yet.");
                }

                object savedGameData = _retrieveSavedGameDataMethod.Invoke(_activeStorer, new object[] { LwsSaveSchema.ProfileDirectoryVendorSlot });
                if (savedGameData == null)
                {
                    return LwsSaveOperationResult.Success("Pixel Crushers profile directory slot is empty.");
                }

                string payload = _getDataMethod.Invoke(savedGameData, new object[] { LwsSaveSchema.ProfileDirectoryRecordKey }) as string;
                if (string.IsNullOrWhiteSpace(payload))
                {
                    return LwsSaveOperationResult.Success("Pixel Crushers profile directory record is empty.");
                }

                directory = DeserializeWithPixelCrushers<LwsSaveProfileDirectory>(payload);
                directory?.EnsureValid();
                return LwsSaveOperationResult.Success("Profile directory loaded through Pixel Crushers SavedGameDataStorer.");
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers profile directory load failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers profile directory load failed: {ex.Message}");
            }
        }

        private void ResolveTypesAndMethods()
        {
            _saveSystemType = FindType(SaveSystemTypeName);
            _savedGameDataType = FindType(SavedGameDataTypeName);
            _savedGameDataStorerType = FindType(SavedGameDataStorerTypeName);
            _diskStorerType = FindType(DiskSavedGameDataStorerTypeName);
            _jsonSerializerType = FindType(JsonDataSerializerTypeName);
            _semanticSaverType = FindType(SemanticSaverTypeName);

            if (_saveSystemType == null || _savedGameDataType == null)
            {
                return;
            }

            _saveSystemInstanceProperty = _saveSystemType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            _saveSystemStorerProperty = _saveSystemType.GetProperty("storer", BindingFlags.Public | BindingFlags.Static);
            _saveCurrentSceneProperty = _saveSystemType.GetProperty("saveCurrentScene", BindingFlags.Public | BindingFlags.Static);
            _maxSaveSlotProperty = _saveSystemType.GetProperty("maxSaveSlot", BindingFlags.Public | BindingFlags.Static);
            _versionProperty = _saveSystemType.GetProperty("version", BindingFlags.Public | BindingFlags.Static);
            _storerField = _saveSystemType.GetField("m_storer", BindingFlags.NonPublic | BindingFlags.Static);
            _serializerField = _saveSystemType.GetField("m_serializer", BindingFlags.NonPublic | BindingFlags.Static);
            _saveToSlotImmediateMethod = _saveSystemType.GetMethod("SaveToSlotImmediate", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            _loadFromSlotMethod = _saveSystemType.GetMethod("LoadFromSlot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            _hasSavedGameInSlotMethod = _saveSystemType.GetMethod("HasSavedGameInSlot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            _deleteSavedGameInSlotMethod = _saveSystemType.GetMethod("DeleteSavedGameInSlot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            _serializeMethod = _saveSystemType.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(object) }, null);
            _deserializeGenericMethod = _saveSystemType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Deserialize" && m.IsGenericMethodDefinition);

            _savedGameVersionProperty = _savedGameDataType.GetProperty("version", BindingFlags.Public | BindingFlags.Instance);
            _savedGameSceneNameProperty = _savedGameDataType.GetProperty("sceneName", BindingFlags.Public | BindingFlags.Instance);
            _setDataMethod = _savedGameDataType.GetMethod("SetData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(int), typeof(string) }, null);
            _getDataMethod = _savedGameDataType.GetMethod("GetData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);

            if (_savedGameDataStorerType != null)
            {
                _storeSavedGameDataMethod = _savedGameDataStorerType.GetMethod("StoreSavedGameData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int), _savedGameDataType }, null);
                _retrieveSavedGameDataMethod = _savedGameDataStorerType.GetMethod("RetrieveSavedGameData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
                _hasDataInSlotMethod = _savedGameDataStorerType.GetMethod("HasDataInSlot", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            }
        }

        private LwsSaveOperationResult BindRuntimeAuthority()
        {
            if (_semanticSaverType == null)
            {
                return Fail($"LWS Pixel Crushers semantic saver bridge is missing: {SemanticSaverTypeName}");
            }

            object saveSystem = _saveSystemInstanceProperty?.GetValue(null, null);
            Component saveSystemComponent = saveSystem as Component;
            if (saveSystemComponent == null)
            {
                return Fail("Pixel Crushers SaveSystem runtime instance is not a Unity component.");
            }

            GameObject saveSystemObject = saveSystemComponent.gameObject;
            _versionProperty?.SetValue(null, LwsSaveSchema.CurrentVersion, null);
            _maxSaveSlotProperty?.SetValue(null, Mathf.Max(99999, LwsSaveSchema.FirstProfileSlotBase + 999 * LwsSaveSchema.SlotsPerProfile), null);
            _saveCurrentSceneProperty?.SetValue(null, false, null);

            _activeSerializer = EnsureComponent(saveSystemObject, _jsonSerializerType) as Component;
            if (_activeSerializer != null)
            {
                _serializerField?.SetValue(null, _activeSerializer);
                ActiveSerializerTypeName = _activeSerializer.GetType().FullName;
            }

            _activeStorer = EnsureComponent(saveSystemObject, _diskStorerType) as Component;
            if (_activeStorer == null)
            {
                object fallback = _saveSystemStorerProperty?.GetValue(null, null);
                _activeStorer = fallback as Component;
            }

            if (_activeStorer == null)
            {
                return Fail("Pixel Crushers SavedGameDataStorer could not be created or found.");
            }

            _storerField?.SetValue(null, _activeStorer);
            ActiveStorerTypeName = _activeStorer.GetType().FullName;

            _semanticSaver = EnsureSemanticSaver(saveSystemObject);
            if (_semanticSaver == null)
            {
                return Fail("LWS Pixel Crushers semantic saver bridge could not be attached to the Save System object.");
            }

            RuntimeBound = true;
            Status = $"Pixel Crushers runtime bound using {ActiveSerializerTypeName} and {ActiveStorerTypeName}.";
            return LwsSaveOperationResult.Success(Status);
        }

        private LwsSaveOperationResult EnsureRuntimeReady()
        {
            if (!Initialized)
            {
                return LwsSaveOperationResult.Failure("Pixel Crushers adapter is not initialized.");
            }

            if (!Application.isPlaying)
            {
                return LwsSaveOperationResult.Failure("Pixel Crushers save/load is only available in Play Mode.");
            }

            if (!RuntimeBound || _activeStorer == null || _semanticSaver == null)
            {
                return BindRuntimeAuthority();
            }

            return LwsSaveOperationResult.Success();
        }

        private Component EnsureComponent(GameObject owner, Type type)
        {
            if (owner == null || type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return null;
            }

            Component existing = owner.GetComponent(type);
            return existing != null ? existing : owner.AddComponent(type);
        }

        private MonoBehaviour EnsureSemanticSaver(GameObject owner)
        {
            Component component = owner.GetComponent(_semanticSaverType) ?? owner.AddComponent(_semanticSaverType);
            var behaviour = component as MonoBehaviour;
            MethodInfo bind = _semanticSaverType.GetMethod("Bind", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(ILwsSaveService) }, null);
            bind?.Invoke(component, new object[] { _saveService });
            return behaviour;
        }

        private string SerializeWithPixelCrushers(object data)
        {
            return _serializeMethod != null
                ? _serializeMethod.Invoke(null, new[] { data }) as string
                : JsonUtility.ToJson(data);
        }

        private T DeserializeWithPixelCrushers<T>(string data) where T : class
        {
            if (_deserializeGenericMethod != null)
            {
                MethodInfo method = _deserializeGenericMethod.MakeGenericMethod(typeof(T));
                return method.Invoke(null, new object[] { data, null }) as T;
            }

            return JsonUtility.FromJson<T>(data);
        }

        private void SetSavedGameVersion(object savedGameData, int version)
        {
            _savedGameVersionProperty?.SetValue(savedGameData, version, null);
        }

        private void SetSavedGameSceneName(object savedGameData, string sceneName)
        {
            _savedGameSceneNameProperty?.SetValue(savedGameData, sceneName ?? string.Empty, null);
        }

        private int GetSavedGameVersion(object savedGameData)
        {
            object value = _savedGameVersionProperty?.GetValue(savedGameData, null);
            return value is int intValue ? intValue : 0;
        }

        private string GetSavedGameSceneName(object savedGameData)
        {
            return _savedGameSceneNameProperty?.GetValue(savedGameData, null) as string ?? string.Empty;
        }


        private LwsSaveOperationResult Fail(string message)
        {
            LastVendorError = message;
            Status = message;
            return LwsSaveOperationResult.Failure(message);
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class LwsSaveDiagnostics
    {
        public bool pixelCrushersAvailable;
        public string pixelCrushersCommonVersion;
        public string pixelCrushersVersion;
        public string dialogueSystemVersion;
        public string adapterStatus;
        public string activeSerializer;
        public string activeStorer;
        public int providerCount;
        public int profileCount;
        public string activeProfile;
        public string lastOperation;
        public bool lastOperationSucceeded;
        public string lastSaveMessage;
        public string lastLoadMessage;
        public string lastDeleteMessage;
        public string lastVendorError;

        public string LastSaveMessage => string.IsNullOrWhiteSpace(lastSaveMessage) ? "--" : lastSaveMessage;
        public string LastLoadMessage => string.IsNullOrWhiteSpace(lastLoadMessage) ? "--" : lastLoadMessage;
        public string LastDeleteMessage => string.IsNullOrWhiteSpace(lastDeleteMessage) ? "--" : lastDeleteMessage;
        public string LastVendorError => string.IsNullOrWhiteSpace(lastVendorError) ? "--" : lastVendorError;

        public static LwsSaveDiagnostics Empty => new LwsSaveDiagnostics
        {
            pixelCrushersAvailable = false,
            pixelCrushersCommonVersion = LwsPixelCrushersSaveAdapter.KnownCommonPackageVersion,
            pixelCrushersVersion = LwsPixelCrushersSaveAdapter.KnownPackageVersion,
            dialogueSystemVersion = LwsPixelCrushersSaveAdapter.KnownDialogueSystemVersion,
            adapterStatus = "Not initialized.",
            activeSerializer = "unknown",
            activeStorer = "unknown",
            providerCount = 0,
            profileCount = 0,
            activeProfile = "none",
            lastOperation = "None",
            lastOperationSucceeded = false,
            lastSaveMessage = "--",
            lastLoadMessage = "--",
            lastDeleteMessage = "--",
            lastVendorError = "--"
        };

        public static LwsSaveDiagnostics FromState(
            LwsPixelCrushersSaveAdapter adapter,
            int providerCount,
            int profileCount,
            string activeProfile,
            string operation,
            LwsSaveOperationResult result)
        {
            return new LwsSaveDiagnostics
            {
                pixelCrushersAvailable = adapter != null && adapter.PixelCrushersAvailable,
                pixelCrushersCommonVersion = LwsPixelCrushersSaveAdapter.KnownCommonPackageVersion,
                pixelCrushersVersion = LwsPixelCrushersSaveAdapter.KnownPackageVersion,
                dialogueSystemVersion = LwsPixelCrushersSaveAdapter.KnownDialogueSystemVersion,
                adapterStatus = adapter != null ? adapter.Status : "missing",
                activeSerializer = adapter != null ? adapter.ActiveSerializerTypeName : "unknown",
                activeStorer = adapter != null ? adapter.ActiveStorerTypeName : "unknown",
                providerCount = providerCount,
                profileCount = profileCount,
                activeProfile = activeProfile ?? "none",
                lastOperation = operation ?? "None",
                lastOperationSucceeded = result.Succeeded,
                lastVendorError = adapter != null ? adapter.LastVendorError : string.Empty
            };
        }
    }

    public sealed class LwsGlobalPositionSaveParticipant : ILwsSaveParticipant
    {
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private LwsGlobalPositionSavePayload _lastRestored;

        public LwsGlobalPositionSaveParticipant(Func<LwsServiceRegistry> registryProvider)
        {
            _registryProvider = registryProvider;
        }

        public string ParticipantId => "lws.world.global-position";
        public int PayloadVersion => 1;
        public LwsGlobalPositionSavePayload LastRestored => _lastRestored;

        public LwsSaveParticipantState CaptureState()
        {
            LwsGlobalPositionSavePayload payload = CapturePayload();
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

            _lastRestored = JsonUtility.FromJson<LwsGlobalPositionSavePayload>(state.payloadJson);
            if (!_lastRestored.IsValid)
            {
                return LwsSaveOperationResult.Failure("Global position payload is invalid.");
            }

            if (TryGetService(out ILwsWorldOriginService originService))
            {
                var savedGlobal = new LwsWorldPositionD(_lastRestored.globalX, _lastRestored.globalY, _lastRestored.globalZ);
                originService.UpdatePlayerLocalPosition(originService.GlobalToLocal(savedGlobal));
            }

            return LwsSaveOperationResult.Success("Global double-precision player position payload restored.");
        }

        public LwsSaveOperationResult ClearState()
        {
            _lastRestored = default;
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return LwsSaveOperationResult.Success("Global-position save participant uses LwsWorldPositionD doubles.");
        }

        private LwsGlobalPositionSavePayload CapturePayload()
        {
            LwsWorldPositionD global = LwsWorldPositionD.Zero;
            Vector3 local = Vector3.zero;
            long originVersion = 0;
            long originShiftCount = 0;
            if (TryGetService(out ILwsWorldOriginService originService))
            {
                global = originService.PlayerGlobalPosition;
                local = originService.PlayerLocalPosition;
                originVersion = originService.OriginVersion;
                originShiftCount = originService.ShiftCount;
            }

            Quaternion rotation = Quaternion.identity;
            if (TryGetService(out ILwsPlayerVehicleService vehicleService) && vehicleService.ActiveTruck != null)
            {
                rotation = vehicleService.ActiveTruck.transform.rotation;
            }

            return new LwsGlobalPositionSavePayload
            {
                globalX = global.x,
                globalY = global.y,
                globalZ = global.z,
                localX = local.x,
                localY = local.y,
                localZ = local.z,
                rotationX = rotation.x,
                rotationY = rotation.y,
                rotationZ = rotation.z,
                rotationW = rotation.w,
                originVersion = originVersion,
                originShiftCount = originShiftCount,
                capturedUtcTicks = DateTime.UtcNow.Ticks
            };
        }

        private bool TryGetService<T>(out T service) where T : class, ILwsService
        {
            service = null;
            LwsServiceRegistry registry = _registryProvider?.Invoke();
            return registry != null && registry.TryGet(out service);
        }
    }

    [Serializable]
    public struct LwsGlobalPositionSavePayload
    {
        public double globalX;
        public double globalY;
        public double globalZ;
        public float localX;
        public float localY;
        public float localZ;
        public float rotationX;
        public float rotationY;
        public float rotationZ;
        public float rotationW;
        public long originVersion;
        public long originShiftCount;
        public long capturedUtcTicks;

        public bool IsValid => !double.IsNaN(globalX) &&
                               !double.IsNaN(globalY) &&
                               !double.IsNaN(globalZ) &&
                               Math.Abs(rotationW) > 0.000001f;
    }

    public sealed class LwsPlayerTruckSaveParticipant : ILwsSaveParticipant
    {
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private LwsPlayerTruckSavePayload _lastRestored;

        public LwsPlayerTruckSaveParticipant(Func<LwsServiceRegistry> registryProvider)
        {
            _registryProvider = registryProvider;
        }

        public string ParticipantId => "lws.vehicle.player-truck";
        public int PayloadVersion => 1;
        public LwsPlayerTruckSavePayload LastRestored => _lastRestored;

        public LwsSaveParticipantState CaptureState()
        {
            LwsPlayerTruckState truckState = default;
            bool hasGlobalPosition = false;
            LwsWorldPositionD global = LwsWorldPositionD.Zero;
            if (TryGetService(out ILwsPlayerVehicleService vehicleService) && vehicleService.ActiveTruck != null)
            {
                truckState = vehicleService.ActiveTruck.CaptureState();
                if (TryGetService(out ILwsWorldOriginService originService))
                {
                    global = originService.LocalToGlobal(vehicleService.ActiveTruck.transform.position);
                    hasGlobalPosition = true;
                }
            }

            var payload = new LwsPlayerTruckSavePayload
            {
                schemaVersion = LwsSaveSchema.CurrentVersion,
                state = truckState,
                hasGlobalPosition = hasGlobalPosition,
                globalX = global.x,
                globalY = global.y,
                globalZ = global.z,
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

            _lastRestored = JsonUtility.FromJson<LwsPlayerTruckSavePayload>(state.payloadJson);
            if (TryGetService(out ILwsPlayerVehicleService vehicleService) && vehicleService.ActiveTruck != null)
            {
                LwsPlayerTruck truck = vehicleService.ActiveTruck;
                LwsPlayerTruckState restored = _lastRestored.state;
                Quaternion rotation = NormalizeRotation(restored.pose.rotation);
                Vector3 localPosition = restored.pose.position;
                if (_lastRestored.hasGlobalPosition && TryGetService(out ILwsWorldOriginService originService))
                {
                    localPosition = originService.GlobalToLocal(new LwsWorldPositionD(_lastRestored.globalX, _lastRestored.globalY, _lastRestored.globalZ));
                    originService.UpdatePlayerLocalPosition(localPosition);
                }

                truck.transform.SetPositionAndRotation(localPosition, rotation);

                Rigidbody rb = null;
                if (truck.NwhAdapter != null && truck.NwhAdapter.VehicleController != null)
                {
                    rb = truck.NwhAdapter.VehicleController.vehicleRigidbody;
                }

                if (rb == null)
                {
                    rb = truck.GetComponent<Rigidbody>();
                }

                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }

                RestoreAttachedTrailerPose(restored.trailerAttachment);

                if (truck.TransmissionController != null)
                {
                    truck.TransmissionController.RestoreState(restored.transmissionState);
                }

                Physics.SyncTransforms();
                return LwsSaveOperationResult.Success("Player truck semantic state restored to the active truck.");
            }

            return LwsSaveOperationResult.Success("Player truck semantic state captured for deferred restore; active truck is not available yet.");
        }

        public LwsSaveOperationResult ClearState()
        {
            _lastRestored = null;
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return LwsSaveOperationResult.Success("Player truck save participant captures identity, global pose, diagnostic motion, trailer attachment, and transmission state shape.");
        }

        private static void RestoreAttachedTrailerPose(LwsTrailerAttachmentState attachment)
        {
            if (!attachment.attached || string.IsNullOrWhiteSpace(attachment.trailerId))
            {
                return;
            }

            foreach (LwsVehicleIdentity identity in UnityEngine.Object.FindObjectsByType<LwsVehicleIdentity>(FindObjectsSortMode.None))
            {
                if (identity == null || !string.Equals(identity.VehicleId, attachment.trailerId, StringComparison.Ordinal))
                {
                    continue;
                }

                identity.transform.SetPositionAndRotation(attachment.trailerPose.position, NormalizeRotation(attachment.trailerPose.rotation));
                foreach (Rigidbody rb in identity.GetComponentsInChildren<Rigidbody>(true))
                {
                    if (rb == null)
                    {
                        continue;
                    }

                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }

                return;
            }
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
            if (magnitude <= 0.000001f)
            {
                return Quaternion.identity;
            }

            return new Quaternion(rotation.x / magnitude, rotation.y / magnitude, rotation.z / magnitude, rotation.w / magnitude);
        }
    }

    [Serializable]
    public sealed class LwsPlayerTruckSavePayload
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public LwsPlayerTruckState state;
        public bool hasGlobalPosition;
        public double globalX;
        public double globalY;
        public double globalZ;
        public long capturedUtcTicks;
    }

    public sealed class LwsGameClockSaveParticipant : ILwsSaveParticipant
    {
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private LwsGameClockSavePayload _lastRestored;

        public LwsGameClockSaveParticipant(Func<LwsServiceRegistry> registryProvider)
        {
            _registryProvider = registryProvider;
        }

        public string ParticipantId => "lws.game-clock";
        public int PayloadVersion => 1;
        public LwsGameClockSavePayload LastRestored => _lastRestored;

        public LwsSaveParticipantState CaptureState()
        {
            LwsGameClockSavePayload payload = default;
            if (TryGetClock(out ILwsGameClockService clockService))
            {
                LwsGameClockSnapshot snapshot = clockService.CurrentSnapshot;
                payload = new LwsGameClockSavePayload
                {
                    year = snapshot.year,
                    month = snapshot.month,
                    day = snapshot.day,
                    hour = snapshot.hour,
                    minute = snapshot.minute,
                    second = snapshot.second,
                    timeScale = snapshot.timeScale,
                    paused = snapshot.paused,
                    totalGameSeconds = snapshot.totalGameSeconds,
                    versionTicks = snapshot.versionTicks
                };
            }

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

            _lastRestored = JsonUtility.FromJson<LwsGameClockSavePayload>(state.payloadJson);
            if (TryGetClock(out ILwsGameClockService clockService))
            {
                clockService.SetDateTime(new LwsGameDateTime(_lastRestored.year, _lastRestored.month, _lastRestored.day, _lastRestored.hour, _lastRestored.minute, _lastRestored.second));
                clockService.SetTimeScale(_lastRestored.timeScale);
                clockService.SetPaused(_lastRestored.paused);
            }

            return LwsSaveOperationResult.Success("Game clock semantic payload restored.");
        }

        public LwsSaveOperationResult ClearState()
        {
            _lastRestored = default;
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return LwsSaveOperationResult.Success("Game-clock save participant captures LWS semantic clock state.");
        }

        private bool TryGetClock(out ILwsGameClockService clockService)
        {
            clockService = null;
            LwsServiceRegistry registry = _registryProvider?.Invoke();
            return registry != null && registry.TryGet(out clockService);
        }
    }

    [Serializable]
    public struct LwsGameClockSavePayload
    {
        public int year;
        public int month;
        public int day;
        public int hour;
        public int minute;
        public float second;
        public float timeScale;
        public bool paused;
        public double totalGameSeconds;
        public long versionTicks;
    }

    public sealed class LwsWeatherSaveParticipant : ILwsSaveParticipant
    {
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private LwsWeatherSavePayload _lastRestored;

        public LwsWeatherSaveParticipant(Func<LwsServiceRegistry> registryProvider)
        {
            _registryProvider = registryProvider;
        }

        public string ParticipantId => "lws.weather.semantic";
        public int PayloadVersion => 1;
        public LwsWeatherSavePayload LastRestored => _lastRestored;

        public LwsSaveParticipantState CaptureState()
        {
            LwsWeatherSnapshot snapshot = LwsWeatherSnapshot.Clear;
            if (TryGetWeather(out ILwsWeatherService weatherService))
            {
                snapshot = weatherService.CurrentSnapshot;
            }

            var payload = new LwsWeatherSavePayload
            {
                weatherPresetId = snapshot.weatherPresetId,
                condition = snapshot.condition,
                precipitationType = snapshot.precipitationType,
                precipitationIntensity01 = snapshot.precipitationIntensity01,
                cloudCover01 = snapshot.cloudCover01,
                fogIntensity01 = snapshot.fogIntensity01,
                windSpeedMetersPerSecond = snapshot.windSpeedMetersPerSecond,
                windDirectionX = snapshot.windDirectionWorld.x,
                windDirectionY = snapshot.windDirectionWorld.y,
                windDirectionZ = snapshot.windDirectionWorld.z,
                lightningActive = snapshot.lightningActive,
                stormIntensity01 = snapshot.stormIntensity01,
                ambientTemperatureC = snapshot.ambientTemperatureC,
                visibilityMeters = snapshot.visibilityMeters,
                timeOfDayHours = snapshot.timeOfDayHours,
                worldTimeTicks = snapshot.worldTimeTicks
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

            _lastRestored = JsonUtility.FromJson<LwsWeatherSavePayload>(state.payloadJson);
            if (TryGetWeather(out ILwsWeatherService weatherService))
            {
                weatherService.SetState(new LwsWeatherState
                {
                    weatherId = _lastRestored.weatherPresetId,
                    precipitationType = _lastRestored.precipitationType,
                    precipitationIntensity = _lastRestored.precipitationIntensity01,
                    temperatureCelsius = _lastRestored.ambientTemperatureC,
                    wetness = 0f,
                    snowAmount = 0f,
                    windVelocity = new Vector3(_lastRestored.windDirectionX, _lastRestored.windDirectionY, _lastRestored.windDirectionZ).normalized * Mathf.Max(0f, _lastRestored.windSpeedMetersPerSecond),
                    visibilityMeters = _lastRestored.visibilityMeters,
                    worldTimeTicks = _lastRestored.worldTimeTicks
                });
                weatherService.SetTimeOfDayHours(_lastRestored.timeOfDayHours);
            }

            return LwsSaveOperationResult.Success("Weather semantic payload restored.");
        }

        public LwsSaveOperationResult ClearState()
        {
            _lastRestored = default;
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return LwsSaveOperationResult.Success("Weather save participant captures LWS semantic weather state.");
        }

        private bool TryGetWeather(out ILwsWeatherService weatherService)
        {
            weatherService = null;
            LwsServiceRegistry registry = _registryProvider?.Invoke();
            return registry != null && registry.TryGet(out weatherService);
        }
    }

    [Serializable]
    public struct LwsWeatherSavePayload
    {
        public string weatherPresetId;
        public LwsWeatherCondition condition;
        public LwsPrecipitationType precipitationType;
        public float precipitationIntensity01;
        public float cloudCover01;
        public float fogIntensity01;
        public float windSpeedMetersPerSecond;
        public float windDirectionX;
        public float windDirectionY;
        public float windDirectionZ;
        public bool lightningActive;
        public float stormIntensity01;
        public float ambientTemperatureC;
        public float visibilityMeters;
        public float timeOfDayHours;
        public long worldTimeTicks;
    }

    public sealed class LwsRoadConditionSaveParticipant : ILwsSaveParticipant
    {
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private LwsRoadConditionSavePayload _lastRestored;

        public LwsRoadConditionSaveParticipant(Func<LwsServiceRegistry> registryProvider)
        {
            _registryProvider = registryProvider;
        }

        public string ParticipantId => "lws.road-condition.semantic";
        public int PayloadVersion => 1;
        public LwsRoadConditionSavePayload LastRestored => _lastRestored;

        public LwsSaveParticipantState CaptureState()
        {
            var payload = new LwsRoadConditionSavePayload
            {
                schemaVersion = LwsSaveSchema.CurrentVersion,
                capturedUtcTicks = DateTime.UtcNow.Ticks
            };

            if (TryGetService(out ILwsRoadConditionService roadService))
            {
                payload.mode = roadService.Mode;
                payload.currentRoadKey = roadService.CurrentRoadKey;
                payload.snapshot = roadService.CurrentSnapshot;
                payload.accumulationSpeedMultiplier = roadService.AccumulationSpeedMultiplier;
                payload.temperatureOverrideEnabled = roadService.TemperatureOverrideEnabled;
                payload.surfaceTemperatureOverrideC = roadService.SurfaceTemperatureOverrideC;
            }

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

            _lastRestored = JsonUtility.FromJson<LwsRoadConditionSavePayload>(state.payloadJson);
            if (TryGetService(out ILwsRoadConditionService roadService))
            {
                roadService.SetAccumulationSpeedMultiplier(_lastRestored.accumulationSpeedMultiplier <= 0f ? 1f : _lastRestored.accumulationSpeedMultiplier);
                roadService.SetSurfaceTemperatureOverride(_lastRestored.temperatureOverrideEnabled, _lastRestored.surfaceTemperatureOverrideC);
                if (_lastRestored.mode == LwsRoadConditionOverrideMode.AutoFromWeather)
                {
                    roadService.SetAutoFromWeather();
                }
                else
                {
                    roadService.ForceCondition(_lastRestored.mode);
                }
            }

            return LwsSaveOperationResult.Success("Road condition semantic intent restored.");
        }

        public LwsSaveOperationResult ClearState()
        {
            _lastRestored = null;
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return LwsSaveOperationResult.Success("Road-condition save participant captures condition, override mode, and physical grip semantics.");
        }

        private bool TryGetService<T>(out T service) where T : class, ILwsService
        {
            service = null;
            LwsServiceRegistry registry = _registryProvider?.Invoke();
            return registry != null && registry.TryGet(out service);
        }
    }

    [Serializable]
    public sealed class LwsRoadConditionSavePayload
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public LwsRoadConditionOverrideMode mode;
        public string currentRoadKey;
        public LwsRoadConditionSnapshot snapshot;
        public float accumulationSpeedMultiplier = 1f;
        public bool temperatureOverrideEnabled;
        public float surfaceTemperatureOverrideC;
        public long capturedUtcTicks;
    }

    public sealed class LwsNavigationSaveParticipant : ILwsSaveParticipant
    {
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private LwsNavigationSavePayload _lastRestored;

        public LwsNavigationSaveParticipant(Func<LwsServiceRegistry> registryProvider)
        {
            _registryProvider = registryProvider;
        }

        public string ParticipantId => "lws.navigation.destination-intent";
        public int PayloadVersion => 1;
        public LwsNavigationSavePayload LastRestored => _lastRestored;

        public LwsSaveParticipantState CaptureState()
        {
            var payload = new LwsNavigationSavePayload
            {
                schemaVersion = LwsSaveSchema.CurrentVersion,
                capturedUtcTicks = DateTime.UtcNow.Ticks
            };

            if (TryGetService(out ILwsNavigationService navigationService))
            {
                LwsNavigationRuntimeState runtime = navigationService.RuntimeState;
                LwsRouteResult route = navigationService.CurrentRoute;
                payload.routeActive = runtime != null && runtime.routeActive;
                payload.routeId = runtime != null ? runtime.routeId : string.Empty;
                payload.destinationId = runtime != null ? runtime.destinationId : string.Empty;
                payload.currentRoadId = runtime != null ? runtime.currentRoadId : string.Empty;
                payload.currentEdgeId = runtime != null ? runtime.currentEdgeId : string.Empty;
                payload.currentRoadDisplayName = runtime != null ? runtime.currentRoadDisplayName : string.Empty;
                payload.distanceRemainingMeters = runtime != null ? runtime.distanceRemainingMeters : 0f;
                payload.playerPosition = runtime != null ? runtime.playerPosition : ResolveOriginPosition();
                payload.playerForward = runtime != null ? runtime.playerForward : Vector3.forward;

                if (route != null)
                {
                    payload.originNodeId = route.originNodeId;
                    payload.destinationNodeId = route.destinationNodeId;
                    payload.destinationId = string.IsNullOrWhiteSpace(route.destinationId) ? payload.destinationId : route.destinationId;
                    payload.destinationWorldPosition = route.waypoints != null && route.waypoints.Count > 0
                        ? route.waypoints[route.waypoints.Count - 1]
                        : payload.playerPosition;
                }
            }

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

            _lastRestored = JsonUtility.FromJson<LwsNavigationSavePayload>(state.payloadJson);
            if (!TryGetService(out ILwsNavigationService navigationService))
            {
                return LwsSaveOperationResult.Success("Navigation save payload stored for deferred restore; navigation service is not available.");
            }

            if (!_lastRestored.routeActive)
            {
                navigationService.ClearRoute();
                return LwsSaveOperationResult.Success("Inactive navigation state restored.");
            }

            if (!TryGetService(out ILwsRoadGraphService roadGraphService) || roadGraphService.ActiveGraph == null)
            {
                return LwsSaveOperationResult.Success("Navigation destination intent restored; route will recalculate when a road graph is available.");
            }

            var request = new LwsRouteRequest
            {
                requestId = $"restore.{DateTime.UtcNow:yyyyMMddHHmmss}",
                originNodeId = _lastRestored.originNodeId,
                destinationNodeId = _lastRestored.destinationNodeId,
                destinationId = string.IsNullOrWhiteSpace(_lastRestored.destinationId) ? "restored.destination" : _lastRestored.destinationId,
                useOriginWorldPosition = true,
                originWorldPosition = _lastRestored.playerPosition,
                useDestinationWorldPosition = _lastRestored.destinationWorldPosition != Vector3.zero,
                destinationWorldPosition = _lastRestored.destinationWorldPosition,
                truckRouteRequired = true
            };
            LwsRouteResult result = navigationService.RequestRoute(request, roadGraphService.ActiveGraph);
            return result != null && result.succeeded
                ? LwsSaveOperationResult.Success("Navigation destination intent restored and route recalculated.")
                : LwsSaveOperationResult.Success("Navigation destination intent restored; route recalculation is pending a compatible road graph.");
        }

        public LwsSaveOperationResult ClearState()
        {
            _lastRestored = null;
            if (TryGetService(out ILwsNavigationService navigationService))
            {
                navigationService.ClearRoute();
            }

            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return LwsSaveOperationResult.Success("Navigation save participant captures destination intent, not duplicate route authority.");
        }

        private Vector3 ResolveOriginPosition()
        {
            return TryGetService(out ILwsWorldOriginService originService) ? originService.PlayerGlobalPosition.ToVector3() : Vector3.zero;
        }

        private bool TryGetService<T>(out T service) where T : class, ILwsService
        {
            service = null;
            LwsServiceRegistry registry = _registryProvider?.Invoke();
            return registry != null && registry.TryGet(out service);
        }
    }

    [Serializable]
    public sealed class LwsNavigationSavePayload
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public bool routeActive;
        public string routeId;
        public string destinationId;
        public string originNodeId;
        public string destinationNodeId;
        public Vector3 destinationWorldPosition;
        public Vector3 playerPosition;
        public Vector3 playerForward;
        public string currentRoadId;
        public string currentEdgeId;
        public string currentRoadDisplayName;
        public float distanceRemainingMeters;
        public long capturedUtcTicks;
    }
}
