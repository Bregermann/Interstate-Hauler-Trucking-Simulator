using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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
        public int schemaVersion = 1;
        public string profileId;
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

    public interface ILwsSaveStorage
    {
        string StorageId { get; }
        string Platform { get; }
        string PixelCrushersStorerTypeName { get; }
        int DevelopmentProofSlot { get; }
        bool UsesDirectFileAccess { get; }
        LwsSaveOperationResult ValidateStorage();
    }

    public interface ILwsSaveService : ILwsService
    {
        IReadOnlyList<ILwsSaveParticipant> Participants { get; }
        ILwsSaveStorage Storage { get; }
        LwsPixelCrushersSaveAdapter Adapter { get; }
        LwsSaveDiagnostics Diagnostics { get; }
        LwsSaveOperationResult RegisterParticipant(ILwsSaveParticipant participant);
        LwsSaveOperationResult UnregisterParticipant(string participantId);
        LwsSaveOperationResult ValidateParticipants();
        LwsSaveSnapshot CaptureSnapshot(string profileId);
        LwsSaveOperationResult RestoreSnapshot(LwsSaveSnapshot snapshot);
        LwsSaveOperationResult ClearAllParticipants();
        LwsSaveOperationResult SaveToSlot(int slotNumber);
        LwsSaveOperationResult LoadFromSlot(int slotNumber);
        bool HasSaveInSlot(int slotNumber);
        LwsSaveOperationResult DeleteSlot(int slotNumber);
        LwsSaveOperationResult SaveTestState();
        LwsSaveOperationResult LoadTestState();
        string BuildDiagnosticsReport();
    }

    public sealed class LwsSaveService : ILwsSaveService
    {
        public const int DevelopmentTestSlot = 16;

        private readonly List<ILwsSaveParticipant> _participants = new List<ILwsSaveParticipant>();
        private readonly HashSet<string> _participantIds = new HashSet<string>(StringComparer.Ordinal);

        private LwsServiceRegistry _registry;
        private LwsValidationSaveParticipant _validationParticipant;
        private string _lastSaveMessage = "--";
        private string _lastLoadMessage = "--";

        public string ServiceId => "lws.save";
        public IReadOnlyList<ILwsSaveParticipant> Participants => _participants;
        public ILwsSaveStorage Storage { get; private set; }
        public LwsPixelCrushersSaveAdapter Adapter { get; private set; }
        public LwsSaveDiagnostics Diagnostics { get; private set; } = LwsSaveDiagnostics.Empty;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            _registry = context.Registry;
            Storage = new LwsPcSaveStorage(DevelopmentTestSlot);
            Adapter = new LwsPixelCrushersSaveAdapter(Storage);

            RegisterParticipant(new LwsPlaceholderSaveParticipant("pixel-crushers.dialogue", 1));
            RegisterParticipant(new LwsPlaceholderSaveParticipant("compass.navigator", 1));
            RegisterParticipant(new LwsPlaceholderSaveParticipant("vehicle.truck", 1));
            RegisterParticipant(new LwsPlaceholderSaveParticipant("jobs.state", 1));
            RegisterSemanticParticipants();

            LwsSaveOperationResult validation = ValidateParticipants();
            if (!validation.Succeeded)
            {
                return LwsServiceResult.Failure(validation.Message);
            }

            LwsSaveOperationResult storageValidation = Storage.ValidateStorage();
            if (!storageValidation.Succeeded)
            {
                return LwsServiceResult.Failure(storageValidation.Message);
            }

            LwsSaveOperationResult adapterValidation = Adapter.Initialize();
            RefreshDiagnostics("Initialize", adapterValidation);
            return adapterValidation.Succeeded
                ? LwsServiceResult.Success("LWS save service initialized with Pixel Crushers Save System.")
                : LwsServiceResult.Failure(adapterValidation.Message);
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _participants.Clear();
            _participantIds.Clear();
            _registry = null;
            _validationParticipant = null;
            Diagnostics = LwsSaveDiagnostics.Empty;
            return LwsServiceResult.Success("LWS save service shut down.");
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

            return LwsSaveOperationResult.Success();
        }

        public LwsSaveSnapshot CaptureSnapshot(string profileId)
        {
            return new LwsSaveSnapshot
            {
                profileId = profileId ?? string.Empty,
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

            foreach (LwsSaveParticipantState payload in snapshot.participants)
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

            return LwsSaveOperationResult.Success();
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

            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult SaveToSlot(int slotNumber)
        {
            LwsSaveOperationResult validation = ValidateParticipants();
            if (!validation.Succeeded)
            {
                RefreshDiagnostics("Save", validation);
                return validation;
            }

            LwsSaveSnapshot snapshot = CaptureSnapshot("development");
            LwsSaveOperationResult result = Adapter.SaveToSlot(slotNumber, snapshot);
            RefreshDiagnostics("Save", result);
            return result;
        }

        public LwsSaveOperationResult LoadFromSlot(int slotNumber)
        {
            LwsSaveOperationResult result = Adapter.LoadFromSlot(slotNumber, _participants);
            RefreshDiagnostics("Load", result);
            return result;
        }

        public bool HasSaveInSlot(int slotNumber)
        {
            return Adapter != null && Adapter.HasSaveInSlot(slotNumber);
        }

        public LwsSaveOperationResult DeleteSlot(int slotNumber)
        {
            LwsSaveOperationResult result = Adapter.DeleteSlot(slotNumber);
            RefreshDiagnostics("Delete", result);
            return result;
        }

        public LwsSaveOperationResult SaveTestState()
        {
            _validationParticipant?.MarkSaved();
            return SaveToSlot(Storage != null ? Storage.DevelopmentProofSlot : DevelopmentTestSlot);
        }

        public LwsSaveOperationResult LoadTestState()
        {
            return LoadFromSlot(Storage != null ? Storage.DevelopmentProofSlot : DevelopmentTestSlot);
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
            if (clockAvailable)
            {
                clock = clockService.CurrentSnapshot;
            }

            string weather = "missing";
            if (_registry != null && _registry.TryGet(out ILwsWeatherService weatherService))
            {
                weather = weatherService.CurrentSnapshot.weatherPresetId;
            }

            return
                "SAVE / PERSISTENCE DIAGNOSTICS\n" +
                $"Pixel Crushers available: {(Adapter != null && Adapter.PixelCrushersAvailable ? "YES" : "NO")}\n" +
                $"Pixel Crushers version: {LwsPixelCrushersSaveAdapter.KnownPackageVersion}\n" +
                $"LWS adapter: {(Adapter != null ? Adapter.Status : "missing")}\n" +
                $"Storage: {(Storage != null ? $"{Storage.Platform} via {Storage.PixelCrushersStorerTypeName}" : "missing")}\n" +
                $"Current save-state providers: {Participants.Count}\n" +
                $"Current global player position: {global}\n" +
                $"Current clock: {(clockAvailable ? $"{clock.DateText} {clock.ClockText} {clock.timeScale:0.##}x paused:{clock.paused}" : "missing")}\n" +
                $"Current weather: {weather}\n" +
                $"Last save operation: {Diagnostics.LastSaveMessage}\n" +
                $"Last load operation: {Diagnostics.LastLoadMessage}\n" +
                $"Last vendor error: {Diagnostics.LastVendorError}";
        }

        private void RegisterSemanticParticipants()
        {
            RegisterParticipant(new LwsGlobalPositionSaveParticipant(() => _registry));
            RegisterParticipant(new LwsGameClockSaveParticipant(() => _registry));
            RegisterParticipant(new LwsWeatherSaveParticipant(() => _registry));
            _validationParticipant = new LwsValidationSaveParticipant();
            RegisterParticipant(_validationParticipant);
        }

        private void RefreshDiagnostics(string operation, LwsSaveOperationResult result)
        {
            if (string.Equals(operation, "Save", StringComparison.OrdinalIgnoreCase))
            {
                _lastSaveMessage = result.Message;
            }
            else if (string.Equals(operation, "Load", StringComparison.OrdinalIgnoreCase))
            {
                _lastLoadMessage = result.Message;
            }

            LwsSaveDiagnostics diagnostics = LwsSaveDiagnostics.FromState(
                Adapter,
                Storage,
                Participants.Count,
                operation,
                result,
                _validationParticipant != null ? _validationParticipant.RoundTripSucceeded : false);
            diagnostics.lastSaveMessage = _lastSaveMessage;
            diagnostics.lastLoadMessage = _lastLoadMessage;
            Diagnostics = diagnostics;
        }
    }

    public sealed class LwsPcSaveStorage : ILwsSaveStorage
    {
        public LwsPcSaveStorage(int developmentProofSlot)
        {
            DevelopmentProofSlot = developmentProofSlot;
        }

        public string StorageId => "lws.save.storage.pc";
        public string Platform => "PC / Windows / Steam";
        public string PixelCrushersStorerTypeName => LwsPixelCrushersSaveAdapter.DiskSavedGameDataStorerTypeName;
        public int DevelopmentProofSlot { get; }
        public bool UsesDirectFileAccess => false;

        public LwsSaveOperationResult ValidateStorage()
        {
            return DevelopmentProofSlot >= 0
                ? LwsSaveOperationResult.Success("PC save storage routes through Pixel Crushers storage.")
                : LwsSaveOperationResult.Failure("Development save slot must be non-negative.");
        }
    }

    public sealed class LwsPixelCrushersSaveAdapter
    {
        public const string SaveSystemTypeName = "PixelCrushers.SaveSystem";
        public const string SavedGameDataTypeName = "PixelCrushers.SavedGameData";
        public const string SavedGameDataStorerTypeName = "PixelCrushers.SavedGameDataStorer";
        public const string DiskSavedGameDataStorerTypeName = "PixelCrushers.DiskSavedGameDataStorer";
        public const string PlayerPrefsSavedGameDataStorerTypeName = "PixelCrushers.PlayerPrefsSavedGameDataStorer";
        public const string SaverTypeName = "PixelCrushers.Saver";
        public const string DialogueSystemSaverTypeName = "PixelCrushers.DialogueSystemSaver";
        public const string GameSaverTypeName = "PixelCrushers.DialogueSystem.GameSaver";
        public const string KnownPackageVersion = "2.2.73.2";
        public const string RootPath = "Assets/Plugins/Pixel Crushers";
        public const string AssetName = "Pixel Crushers Common Save System / Dialogue System";

        private readonly ILwsSaveStorage _storage;

        private Type _saveSystemType;
        private Type _savedGameDataType;
        private Type _diskStorerType;
        private MethodInfo _recordSavedGameDataMethod;
        private MethodInfo _applySavedGameDataMethod;
        private MethodInfo _setDataMethod;
        private MethodInfo _getDataMethod;
        private MethodInfo _storeSavedGameDataMethod;
        private MethodInfo _retrieveSavedGameDataMethod;
        private MethodInfo _hasDataInSlotMethod;
        private MethodInfo _deleteSavedGameDataMethod;
        private PropertyInfo _saveSystemInstanceProperty;
        private PropertyInfo _saveSystemStorerProperty;
        private PropertyInfo _saveCurrentSceneProperty;

        public LwsPixelCrushersSaveAdapter(ILwsSaveStorage storage)
        {
            _storage = storage;
        }

        public bool PixelCrushersAvailable => _saveSystemType != null && _savedGameDataType != null;
        public bool Initialized { get; private set; }
        public string Status { get; private set; } = "Not initialized.";
        public string LastVendorError { get; private set; } = string.Empty;
        public string ActiveStorerTypeName { get; private set; } = "unknown";

        public LwsSaveOperationResult Initialize()
        {
            try
            {
                _saveSystemType = FindType(SaveSystemTypeName);
                _savedGameDataType = FindType(SavedGameDataTypeName);
                _diskStorerType = FindType(DiskSavedGameDataStorerTypeName);
                if (_saveSystemType == null || _savedGameDataType == null)
                {
                    return Fail("PIXEL CRUSHERS SAVE SYSTEM UNAVAILABLE: PixelCrushers.SaveSystem or SavedGameData type was not found.");
                }

                _saveSystemInstanceProperty = _saveSystemType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                _saveSystemStorerProperty = _saveSystemType.GetProperty("storer", BindingFlags.Public | BindingFlags.Static);
                _saveCurrentSceneProperty = _saveSystemType.GetProperty("saveCurrentScene", BindingFlags.Public | BindingFlags.Static);
                _recordSavedGameDataMethod = _saveSystemType.GetMethod("RecordSavedGameData", BindingFlags.Public | BindingFlags.Static);
                _applySavedGameDataMethod = _saveSystemType.GetMethod("ApplySavedGameData", BindingFlags.Public | BindingFlags.Static, null, new[] { _savedGameDataType }, null);
                _setDataMethod = _savedGameDataType.GetMethod("SetData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(int), typeof(string) }, null);
                _getDataMethod = _savedGameDataType.GetMethod("GetData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);

                if (_saveSystemInstanceProperty == null ||
                    _saveSystemStorerProperty == null ||
                    _recordSavedGameDataMethod == null ||
                    _applySavedGameDataMethod == null ||
                    _setDataMethod == null ||
                    _getDataMethod == null)
                {
                    return Fail("PIXEL CRUSHERS SAVE SYSTEM UNAVAILABLE: required public SaveSystem/SavedGameData APIs were not found.");
                }

                Initialized = true;
                if (!Application.isPlaying)
                {
                    Status = "READY (Pixel Crushers APIs detected; runtime storer deferred until Play Mode).";
                    LastVendorError = string.Empty;
                    return LwsSaveOperationResult.Success(Status);
                }

                return BindRuntimeStorer();
            }
            catch (Exception ex)
            {
                return Fail($"PIXEL CRUSHERS SAVE SYSTEM UNAVAILABLE: {ex.GetType().Name}: {ex.Message}");
            }
        }

        public LwsSaveOperationResult SaveToSlot(int slotNumber, LwsSaveSnapshot snapshot)
        {
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            if (snapshot == null)
            {
                return Fail("Cannot save a null LWS save snapshot.");
            }

            try
            {
                object savedGameData = _recordSavedGameDataMethod.Invoke(null, null);
                if (savedGameData == null)
                {
                    return Fail("Pixel Crushers SaveSystem.RecordSavedGameData returned null.");
                }

                foreach (LwsSaveParticipantState participantState in snapshot.participants)
                {
                    if (participantState == null || string.IsNullOrWhiteSpace(participantState.participantId))
                    {
                        continue;
                    }

                    string participantJson = JsonUtility.ToJson(participantState);
                    _setDataMethod.Invoke(savedGameData, new object[] { participantState.participantId, -1, participantJson });
                }

                object storer = _saveSystemStorerProperty.GetValue(null, null);
                _storeSavedGameDataMethod.Invoke(storer, new[] { (object)slotNumber, savedGameData });
                Status = $"Saved slot {slotNumber} through Pixel Crushers.";
                LastVendorError = string.Empty;
                return LwsSaveOperationResult.Success(Status);
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers save failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers save failed: {ex.Message}");
            }
        }

        public LwsSaveOperationResult LoadFromSlot(int slotNumber, IReadOnlyList<ILwsSaveParticipant> participants)
        {
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            try
            {
                object storer = _saveSystemStorerProperty.GetValue(null, null);
                bool exists = (bool)_hasDataInSlotMethod.Invoke(storer, new object[] { slotNumber });
                if (!exists)
                {
                    return Fail($"Pixel Crushers save slot {slotNumber} does not exist.");
                }

                object savedGameData = _retrieveSavedGameDataMethod.Invoke(storer, new object[] { slotNumber });
                if (savedGameData == null)
                {
                    return Fail($"Pixel Crushers storer returned no data for slot {slotNumber}.");
                }

                _applySavedGameDataMethod.Invoke(null, new[] { savedGameData });
                foreach (ILwsSaveParticipant participant in participants ?? Array.Empty<ILwsSaveParticipant>())
                {
                    if (participant == null || string.IsNullOrWhiteSpace(participant.ParticipantId))
                    {
                        continue;
                    }

                    string json = _getDataMethod.Invoke(savedGameData, new object[] { participant.ParticipantId }) as string;
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        continue;
                    }

                    LwsSaveParticipantState state = JsonUtility.FromJson<LwsSaveParticipantState>(json);
                    if (state == null)
                    {
                        continue;
                    }

                    LwsSaveOperationResult restore = participant.RestoreState(state);
                    if (!restore.Succeeded)
                    {
                        return restore;
                    }
                }

                Status = $"Loaded slot {slotNumber} through Pixel Crushers.";
                LastVendorError = string.Empty;
                return LwsSaveOperationResult.Success(Status);
            }
            catch (TargetInvocationException ex)
            {
                return Fail($"Pixel Crushers load failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Pixel Crushers load failed: {ex.Message}");
            }
        }

        public bool HasSaveInSlot(int slotNumber)
        {
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return false;
            }

            object storer = _saveSystemStorerProperty.GetValue(null, null);
            return (bool)_hasDataInSlotMethod.Invoke(storer, new object[] { slotNumber });
        }

        public LwsSaveOperationResult DeleteSlot(int slotNumber)
        {
            LwsSaveOperationResult ready = EnsureRuntimeReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            object storer = _saveSystemStorerProperty.GetValue(null, null);
            _deleteSavedGameDataMethod.Invoke(storer, new object[] { slotNumber });
            Status = $"Deleted Pixel Crushers slot {slotNumber}.";
            return LwsSaveOperationResult.Success(Status);
        }

        public static Type FindType(string fullName)
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

        private void EnsureDiskStorer(Component saveSystemComponent)
        {
            if (_diskStorerType == null || saveSystemComponent == null || saveSystemComponent.gameObject == null)
            {
                return;
            }

            Component existing = saveSystemComponent.GetComponent(_diskStorerType);
            if (existing == null)
            {
                existing = saveSystemComponent.gameObject.AddComponent(_diskStorerType);
            }

            FieldInfo storerField = _saveSystemType.GetField("m_storer", BindingFlags.NonPublic | BindingFlags.Static);
            if (storerField != null && existing != null)
            {
                storerField.SetValue(null, existing);
            }
        }

        private LwsSaveOperationResult BindRuntimeStorer()
        {
            object saveSystem = _saveSystemInstanceProperty.GetValue(null, null);
            if (saveSystem is Component saveSystemComponent)
            {
                EnsureDiskStorer(saveSystemComponent);
            }

            object storer = _saveSystemStorerProperty.GetValue(null, null);
            if (storer == null)
            {
                return Fail("PIXEL CRUSHERS SAVE SYSTEM UNAVAILABLE: SaveSystem.storer returned null.");
            }

            Type storerType = storer.GetType();
            ActiveStorerTypeName = storerType.FullName;
            _storeSavedGameDataMethod = storerType.GetMethod("StoreSavedGameData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int), _savedGameDataType }, null);
            _retrieveSavedGameDataMethod = storerType.GetMethod("RetrieveSavedGameData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            _hasDataInSlotMethod = storerType.GetMethod("HasDataInSlot", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            _deleteSavedGameDataMethod = storerType.GetMethod("DeleteSavedGameData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);

            if (_storeSavedGameDataMethod == null ||
                _retrieveSavedGameDataMethod == null ||
                _hasDataInSlotMethod == null ||
                _deleteSavedGameDataMethod == null)
            {
                return Fail($"PIXEL CRUSHERS SAVE SYSTEM UNAVAILABLE: storer {ActiveStorerTypeName} is missing required storage APIs.");
            }

            _saveCurrentSceneProperty?.SetValue(null, false, null);
            Initialized = true;
            Status = $"READY ({ActiveStorerTypeName})";
            LastVendorError = string.Empty;
            return LwsSaveOperationResult.Success(Status);
        }

        private LwsSaveOperationResult EnsureReady()
        {
            if (!Initialized)
            {
                return Initialize();
            }

            return PixelCrushersAvailable
                ? LwsSaveOperationResult.Success(Status)
                : Fail("PIXEL CRUSHERS SAVE SYSTEM UNAVAILABLE");
        }

        private LwsSaveOperationResult EnsureRuntimeReady()
        {
            LwsSaveOperationResult ready = EnsureReady();
            if (!ready.Succeeded)
            {
                return ready;
            }

            if (!Application.isPlaying)
            {
                return Fail("Pixel Crushers save/load proof requires Play Mode so the runtime SaveSystem and storer can be created safely.");
            }

            return _storeSavedGameDataMethod != null &&
                   _retrieveSavedGameDataMethod != null &&
                   _hasDataInSlotMethod != null &&
                   _deleteSavedGameDataMethod != null
                ? LwsSaveOperationResult.Success(Status)
                : BindRuntimeStorer();
        }

        private LwsSaveOperationResult Fail(string message)
        {
            Initialized = false;
            LastVendorError = message;
            Status = message;
            return LwsSaveOperationResult.Failure(message);
        }
    }

    [Serializable]
    public sealed class LwsSaveDiagnostics
    {
        public bool pixelCrushersAvailable;
        public string pixelCrushersVersion;
        public string adapterStatus;
        public string storageStatus;
        public int providerCount;
        public string lastOperation;
        public bool lastOperationSucceeded;
        public string lastSaveMessage;
        public string lastLoadMessage;
        public string lastVendorError;
        public bool validationVariableRoundTripped;

        public string LastSaveMessage => string.IsNullOrWhiteSpace(lastSaveMessage) ? "--" : lastSaveMessage;
        public string LastLoadMessage => string.IsNullOrWhiteSpace(lastLoadMessage) ? "--" : lastLoadMessage;
        public string LastVendorError => string.IsNullOrWhiteSpace(lastVendorError) ? "--" : lastVendorError;

        public static LwsSaveDiagnostics Empty => new LwsSaveDiagnostics
        {
            pixelCrushersAvailable = false,
            pixelCrushersVersion = LwsPixelCrushersSaveAdapter.KnownPackageVersion,
            adapterStatus = "Not initialized.",
            storageStatus = "Not initialized.",
            providerCount = 0,
            lastOperation = "None",
            lastOperationSucceeded = false,
            lastSaveMessage = "--",
            lastLoadMessage = "--",
            lastVendorError = "--",
            validationVariableRoundTripped = false
        };

        public static LwsSaveDiagnostics FromState(
            LwsPixelCrushersSaveAdapter adapter,
            ILwsSaveStorage storage,
            int providerCount,
            string operation,
            LwsSaveOperationResult result,
            bool validationRoundTrip)
        {
            var diagnostics = new LwsSaveDiagnostics
            {
                pixelCrushersAvailable = adapter != null && adapter.PixelCrushersAvailable,
                pixelCrushersVersion = LwsPixelCrushersSaveAdapter.KnownPackageVersion,
                adapterStatus = adapter != null ? adapter.Status : "missing",
                storageStatus = storage != null ? $"{storage.Platform} / {storage.PixelCrushersStorerTypeName}" : "missing",
                providerCount = providerCount,
                lastOperation = operation ?? "None",
                lastOperationSucceeded = result.Succeeded,
                lastVendorError = adapter != null ? adapter.LastVendorError : string.Empty,
                validationVariableRoundTripped = validationRoundTrip
            };

            if (string.Equals(operation, "Save", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.lastSaveMessage = result.Message;
            }
            else if (string.Equals(operation, "Load", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.lastLoadMessage = result.Message;
            }

            return diagnostics;
        }
    }

    public sealed class LwsPlaceholderSaveParticipant : ILwsSaveParticipant
    {
        public LwsPlaceholderSaveParticipant(string participantId, int payloadVersion)
        {
            ParticipantId = participantId;
            PayloadVersion = payloadVersion;
        }

        public string ParticipantId { get; }
        public int PayloadVersion { get; }

        public LwsSaveParticipantState CaptureState()
        {
            return new LwsSaveParticipantState
            {
                participantId = ParticipantId,
                payloadVersion = PayloadVersion,
                payloadJson = "{}"
            };
        }

        public LwsSaveOperationResult RestoreState(LwsSaveParticipantState state)
        {
            if (state == null || state.participantId != ParticipantId)
            {
                return LwsSaveOperationResult.Failure($"Payload does not belong to {ParticipantId}.");
            }

            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ClearState()
        {
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return PayloadVersion > 0
                ? LwsSaveOperationResult.Success()
                : LwsSaveOperationResult.Failure($"Invalid payload version for {ParticipantId}.");
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
            return _lastRestored.IsValid
                ? LwsSaveOperationResult.Success("Global double-precision player position payload restored for Prompt 017 resume.")
                : LwsSaveOperationResult.Failure("Global position payload is invalid.");
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

    public sealed class LwsValidationSaveParticipant : ILwsSaveParticipant
    {
        private string _currentValue = "not-saved";
        private string _lastSavedValue = string.Empty;
        private string _lastLoadedValue = string.Empty;
        private int _saveCount;

        public string ParticipantId => "lws.validation.proof";
        public int PayloadVersion => 1;
        public bool RoundTripSucceeded => !string.IsNullOrEmpty(_lastSavedValue) && _lastSavedValue == _lastLoadedValue;
        public string CurrentValue => _currentValue;

        public void MarkSaved()
        {
            _saveCount++;
            _currentValue = $"validation-{_saveCount}-{DateTime.UtcNow.Ticks}";
            _lastSavedValue = _currentValue;
        }

        public LwsSaveParticipantState CaptureState()
        {
            return new LwsSaveParticipantState
            {
                participantId = ParticipantId,
                payloadVersion = PayloadVersion,
                payloadJson = JsonUtility.ToJson(new LwsValidationSavePayload { value = _currentValue, saveCount = _saveCount })
            };
        }

        public LwsSaveOperationResult RestoreState(LwsSaveParticipantState state)
        {
            if (state == null || state.participantId != ParticipantId)
            {
                return LwsSaveOperationResult.Failure($"Payload does not belong to {ParticipantId}.");
            }

            LwsValidationSavePayload payload = JsonUtility.FromJson<LwsValidationSavePayload>(state.payloadJson);
            _currentValue = payload.value;
            _lastLoadedValue = payload.value;
            _saveCount = payload.saveCount;
            return LwsSaveOperationResult.Success(RoundTripSucceeded ? "Validation variable round-tripped." : "Validation variable restored.");
        }

        public LwsSaveOperationResult ClearState()
        {
            _currentValue = "cleared";
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return LwsSaveOperationResult.Success("Validation save participant is ready.");
        }
    }

    [Serializable]
    public struct LwsValidationSavePayload
    {
        public string value;
        public int saveCount;
    }
}
