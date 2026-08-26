using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    public interface ILwsPersistenceMenuService : ILwsService
    {
        bool IsOpen { get; }
        LwsPersistencePauseMenu RuntimeRoot { get; }
        void EnsureRuntime();
        void Show();
        void ShowPauseMenu();
        void ShowSaveLoad();
        void Hide();
        void Toggle();
        void ToggleSaveLoad();
    }

    public sealed class LwsPersistenceMenuService : ILwsPersistenceMenuService
    {
        private LwsServiceRegistry _registry;
        private LwsPersistencePauseMenu _runtimeRoot;

        public string ServiceId => "lws.ui.persistence-menu";
        public bool IsOpen => _runtimeRoot != null && _runtimeRoot.IsOpen;
        public LwsPersistencePauseMenu RuntimeRoot => _runtimeRoot;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            _registry = context.Registry;
            if (Application.isPlaying)
            {
                EnsureRuntime();
            }

            return LwsServiceResult.Success("LWS persistence pause menu service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            if (_runtimeRoot != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_runtimeRoot.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_runtimeRoot.gameObject);
                }

                _runtimeRoot = null;
            }

            _registry = null;
            return LwsServiceResult.Success("LWS persistence pause menu service shut down.");
        }

        public void EnsureRuntime()
        {
            if (_runtimeRoot != null)
            {
                return;
            }

            LwsPersistencePauseMenu[] existing = UnityEngine.Object.FindObjectsByType<LwsPersistencePauseMenu>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0)
            {
                _runtimeRoot = existing[0];
                for (int i = 1; i < existing.Length; i++)
                {
                    UnityEngine.Object.Destroy(existing[i].gameObject);
                }
            }
            else
            {
                GameObject root = new GameObject("IH Persistence Pause Menu");
                _runtimeRoot = root.AddComponent<LwsPersistencePauseMenu>();
            }

            UnityEngine.Object.DontDestroyOnLoad(_runtimeRoot.gameObject);
            _runtimeRoot.Bind(this, _registry);
        }

        public void Show()
        {
            ShowPauseMenu();
        }

        public void ShowPauseMenu()
        {
            EnsureRuntime();
            _runtimeRoot?.ShowPauseMenu();
        }

        public void ShowSaveLoad()
        {
            EnsureRuntime();
            _runtimeRoot?.ShowSaveLoad();
        }

        public void Hide()
        {
            _runtimeRoot?.Hide();
        }

        public void Toggle()
        {
            EnsureRuntime();
            if (_runtimeRoot != null)
            {
                _runtimeRoot.Toggle();
            }
        }

        public void ToggleSaveLoad()
        {
            EnsureRuntime();
            if (_runtimeRoot != null)
            {
                _runtimeRoot.ToggleSaveLoad();
            }
        }
    }

    public enum LwsPersistenceMenuView
    {
        Main,
        SaveLoad,
        Profiles,
        Confirm
    }

    public enum LwsPersistenceMenuOpenContext
    {
        PauseMenu,
        DirectSaveLoad
    }

    [DefaultExecutionOrder(-45)]
    [DisallowMultipleComponent]
    public sealed class LwsPersistencePauseMenu : MonoBehaviour
    {
        public const string CanvasObjectName = "F3 Save Load Canvas";
        public const string DirectSaveHotkeyName = "F3";
        public const float ButtonPreferredHeight = 60f;
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.52f);
        private static readonly Color PanelColor = new Color(0.028f, 0.034f, 0.034f, 0.98f);
        private static readonly Color RowColor = new Color(0.045f, 0.058f, 0.057f, 0.96f);
        private static readonly Color ButtonColor = new Color(0.11f, 0.18f, 0.19f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.16f, 0.27f, 0.28f, 1f);
        private static readonly Color ButtonPressedColor = new Color(0.08f, 0.13f, 0.14f, 1f);
        private static readonly Color ButtonDisabledColor = new Color(0.08f, 0.08f, 0.08f, 0.72f);
        private static readonly Color TextColor = new Color(0.92f, 0.96f, 0.94f, 1f);
        private static readonly Color MutedTextColor = new Color(0.7f, 0.78f, 0.78f, 1f);
        private static Font _uiFont;

        private readonly Dictionary<LwsKeyboardGamepadTruckInputSource, bool> _suppressedDrivingSources = new Dictionary<LwsKeyboardGamepadTruckInputSource, bool>();
        private LwsPersistenceMenuService _service;
        private LwsServiceRegistry _registry;
        private ILwsSaveService _saveService;
        private ILwsVehicleInputService _inputService;
        private ILwsGameplayStateService _gameplayStateService;
        private ILwsDevelopmentUiService _developmentUiService;
        private Canvas _canvas;
        private RectTransform _panel;
        private RectTransform _contentRoot;
        private Text _titleText;
        private Text _statusText;
        private InputField _profileNameInput;
        private LwsPersistenceMenuView _view = LwsPersistenceMenuView.Main;
        private LwsPersistenceMenuView _profileReturnView = LwsPersistenceMenuView.Main;
        private LwsPersistenceMenuOpenContext _openContext = LwsPersistenceMenuOpenContext.PauseMenu;
        private string _lastStatus = "Ready.";
        private string _pendingConfirmTitle;
        private string _pendingConfirmBody;
        private Action _pendingConfirmAction;
        private LwsPersistenceMenuView _confirmCancelView = LwsPersistenceMenuView.Main;
        private float _previousTimeScale = 1f;
        private bool _pauseSnapshotTaken;
        private bool _previousCursorVisible;
        private CursorLockMode _previousCursorLockMode;

        public bool IsOpen => _canvas != null && _canvas.gameObject.activeSelf;
        public LwsPersistenceMenuView CurrentView => _view;
        public LwsPersistenceMenuOpenContext OpenContext => _openContext;

        public void Bind(LwsPersistenceMenuService service, LwsServiceRegistry registry)
        {
            _service = service;
            _registry = registry;
            ResolveServices();
            BuildIfNeeded();
            HideImmediate();
        }

        public void Show()
        {
            ShowPauseMenu();
        }

        public void ShowPauseMenu()
        {
            ShowInternal(LwsPersistenceMenuView.Main, LwsPersistenceMenuOpenContext.PauseMenu);
        }

        public void ShowSaveLoad()
        {
            LwsPersistenceMenuOpenContext context = IsOpen && _openContext == LwsPersistenceMenuOpenContext.PauseMenu
                ? LwsPersistenceMenuOpenContext.PauseMenu
                : LwsPersistenceMenuOpenContext.DirectSaveLoad;
            ShowInternal(LwsPersistenceMenuView.SaveLoad, context);
        }

        private void ShowInternal(LwsPersistenceMenuView view, LwsPersistenceMenuOpenContext context)
        {
            BuildIfNeeded();
            ResolveServices();
            if (_canvas == null)
            {
                return;
            }

            CloseDevelopmentOverlays();
            if (!IsOpen)
            {
                _pauseSnapshotTaken = true;
                _previousTimeScale = Time.timeScale;
                _previousCursorVisible = Cursor.visible;
                _previousCursorLockMode = Cursor.lockState;
            }

            if (context == LwsPersistenceMenuOpenContext.PauseMenu && (_gameplayStateService == null || _gameplayStateService.CurrentState != LwsGameplayState.Paused))
            {
                _gameplayStateService?.Pause("Escape pause menu opened.");
            }

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SuppressDrivingInputSources(true);
            _openContext = context;
            _view = view;
            _canvas.gameObject.SetActive(true);
            RebuildView();
        }

        public void Hide()
        {
            if (_canvas == null || !IsOpen)
            {
                return;
            }

            HideImmediate();
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                ShowPauseMenu();
            }
        }

        public void ToggleSaveLoad()
        {
            if (IsOpen && _view == LwsPersistenceMenuView.SaveLoad && _openContext == LwsPersistenceMenuOpenContext.DirectSaveLoad)
            {
                Hide();
                return;
            }

            ShowSaveLoad();
        }

        private void Update()
        {
            ResolveServices();
            if (WasDirectSavePressed())
            {
                ToggleSaveLoad();
                return;
            }

            if (WasPausePressed())
            {
                if (IsOpen)
                {
                    Hide();
                }
                else
                {
                    ShowPauseMenu();
                }
            }
        }

        private void OnDestroy()
        {
            SuppressDrivingInputSources(false);
            if (_pauseSnapshotTaken)
            {
                Time.timeScale = _previousTimeScale;
                Cursor.visible = _previousCursorVisible;
                Cursor.lockState = _previousCursorLockMode;
            }
        }

        private void HideImmediate()
        {
            bool wasOpen = IsOpen;
            LwsPersistenceMenuOpenContext closingContext = _openContext;
            SuppressDrivingInputSources(false);
            if (_pauseSnapshotTaken)
            {
                Time.timeScale = _previousTimeScale;
                Cursor.visible = _previousCursorVisible;
                Cursor.lockState = _previousCursorLockMode;
                _pauseSnapshotTaken = false;
            }

            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }

            if (wasOpen && closingContext == LwsPersistenceMenuOpenContext.PauseMenu && _gameplayStateService != null && _gameplayStateService.CurrentState == LwsGameplayState.Paused)
            {
                _gameplayStateService.Resume("Escape pause menu closed.");
            }
        }

        private bool WasPausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
            {
                return true;
            }
#endif
            return false;
        }

        private void ResolveServices()
        {
            if (_registry == null && LwsApplicationBootstrap.Instance != null)
            {
                _registry = LwsApplicationBootstrap.Instance.Registry;
            }

            if (_registry == null)
            {
                return;
            }

            _registry.TryGet(out _saveService);
            _registry.TryGet(out _inputService);
            _registry.TryGet(out _gameplayStateService);
            _registry.TryGet(out _developmentUiService);
        }

        private void BuildIfNeeded()
        {
            if (_canvas != null)
            {
                return;
            }

            RectTransform canvasRect = CreateRect(transform, CanvasObjectName, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _canvas = canvasRect.gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 6900;
            CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            Image backdrop = canvasRect.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            _panel = CreateRect(canvasRect, "Pause Save Panel", new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);
            _panel.gameObject.AddComponent<Image>().color = PanelColor;
            _titleText = CreateText(_panel, "Title", "INTERSTATE HAULER", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -96f), new Vector2(-34f, -20f), 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            _titleText.verticalOverflow = VerticalWrapMode.Overflow;
            _statusText = CreateText(_panel, "Status", "Ready.", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(34f, 22f), new Vector2(-34f, 86f), 18, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
            _contentRoot = CreateRect(_panel, "Content", Vector2.zero, Vector2.one, new Vector2(34f, 104f), new Vector2(-34f, -112f));
            VerticalLayoutGroup layout = _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 14f;
        }

        private void RebuildView()
        {
            if (_contentRoot == null)
            {
                return;
            }

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentRoot.GetChild(i).gameObject);
            }

            if (_saveService == null)
            {
                SetHeader("SAVE / LOAD");
                AddInfo("Save service", "missing");
                AddButton("RESUME", Hide, true);
                return;
            }

            switch (_view)
            {
                case LwsPersistenceMenuView.SaveLoad:
                    BuildSaveLoadView();
                    break;
                case LwsPersistenceMenuView.Profiles:
                    BuildProfilesView();
                    break;
                case LwsPersistenceMenuView.Confirm:
                    BuildConfirmView();
                    break;
                default:
                    BuildMainView();
                    break;
            }

            _statusText.text = IsPersistenceBusy() ? ResolveBusyLabel() : _lastStatus;
        }

        private void BuildMainView()
        {
            SetHeader("INTERSTATE HAULER\nPAUSED");
            AddInfo("Current profile", _saveService.ActiveProfile != null ? _saveService.ActiveProfile.DisplayNameOrFallback : "none");
            AddInfo("Save system", ResolveSaveSystemStatus());
            AddButton("RESUME", Hide, true);
            AddButton("SAVE / LOAD", () => SetView(LwsPersistenceMenuView.SaveLoad), true);
            AddButton("SETTINGS (COMING LATER)", null, false);
            AddButton("CONTROLS (COMING LATER)", null, false);
            AddButton("QUIT TO MAIN MENU (COMING LATER)", null, false);
            AddButton("QUIT GAME", QuitGame, true);
        }

        private void BuildSaveLoadView()
        {
            SetHeader("INTERSTATE HAULER\nSAVE / LOAD");
            LwsSaveProfileMetadata profile = _saveService.ActiveProfile;
            AddInfo("Current profile", profile != null ? profile.DisplayNameOrFallback : "none");
            AddInfo("Save system", ResolveSaveSystemStatus());
            AddInfo("Autosave", ResolveAutosaveStatus());
            if (!string.IsNullOrWhiteSpace(_saveService.LastFailure))
            {
                AddInfo("Last error", _saveService.LastFailure);
            }

            AddProfileActionRow();

            if (_saveService.RecoveryOffer != null && _saveService.RecoveryOffer.available)
            {
                AddRecoveryRow(_saveService.RecoveryOffer);
            }

            AddAutosaveRow(_saveService.GetAutosaveSlotMetadata());

            foreach (LwsManualSaveSlotMetadata slot in _saveService.GetManualSlots())
            {
                AddSlotRow(slot);
            }

            AddButton(_openContext == LwsPersistenceMenuOpenContext.DirectSaveLoad ? "CLOSE" : "BACK", BackFromSaveLoad, !IsPersistenceBusy());
        }
        private void BuildProfilesView()
        {
            SetHeader("INTERSTATE HAULER\nPROFILES");
            AddInfo("Active", _saveService.ActiveProfile != null ? _saveService.ActiveProfile.DisplayNameOrFallback : "none");
            _profileNameInput = AddInput("Profile Name", _saveService.ActiveProfile != null ? _saveService.ActiveProfile.DisplayNameOrFallback : "Driver");
            AddButton("CREATE PROFILE", () =>
            {
                LwsSaveOperationResult result = _saveService.CreateProfile(_profileNameInput.text, out _);
                _lastStatus = result.Message;
                RebuildView();
            }, !IsPersistenceBusy());

            foreach (LwsSaveProfileMetadata profile in _saveService.Profiles)
            {
                AddProfileRow(profile);
            }

            AddButton("BACK", () => SetView(_profileReturnView), !IsPersistenceBusy());
        }
        private void BuildConfirmView()
        {
            SetHeader(_pendingConfirmTitle ?? "CONFIRM");
            AddInfo("Confirm", _pendingConfirmBody ?? "This action requires confirmation.");
            AddButton("CONFIRM", () =>
            {
                Action action = _pendingConfirmAction;
                ClearConfirm();
                action?.Invoke();
            }, true);
            AddButton("CANCEL", () =>
            {
                LwsPersistenceMenuView cancelView = _confirmCancelView;
                ClearConfirm();
                SetView(cancelView);
            }, true);
        }

        private void AddAutosaveRow(LwsManualSaveSlotMetadata slot)
        {
            RectTransform row = AddRow("Autosave", 104f);
            bool occupied = slot != null && slot.occupied;
            string detail = occupied
                ? $"AUTOSAVE\nSaved: {FormatUtc(slot.savedUtcTicks)}\nLocation: {SafeLabel(slot.worldLabel, SafeLabel(slot.sceneName, "unknown world"))} | Playtime: {FormatDuration(slot.playtimeSeconds)}"
                : "AUTOSAVE\nNO AUTOSAVE";
            if (_saveService.PendingAutosave)
            {
                detail += "\nPending safe autosave.";
            }

            CreateText(row, "Autosave Detail", detail, new Vector2(0f, 0f), new Vector2(0.57f, 1f), new Vector2(18f, 8f), new Vector2(-8f, -8f), 18, FontStyle.Normal, TextAnchor.MiddleLeft, TextColor);
            CreateButton(row, "Load Autosave", "LOAD", new Vector2(0.58f, 0.14f), new Vector2(0.78f, 0.86f), Vector2.zero, Vector2.zero, RunLoadAutosave, occupied && !IsPersistenceBusy());
            CreateButton(row, "Delete Autosave", "DELETE", new Vector2(0.79f, 0.14f), new Vector2(1f, 0.86f), Vector2.zero, new Vector2(-10f, 0f), () =>
            {
                Confirm("DELETE AUTOSAVE?", "This removes the autosave and its hidden recovery backup.", RunDeleteAutosave, LwsPersistenceMenuView.SaveLoad);
            }, occupied && !IsPersistenceBusy());
        }

        private void AddRecoveryRow(LwsSaveRecoveryOffer offer)
        {
            RectTransform row = AddRow("Recovery Offer", 104f);
            string detail = offer != null && !string.IsNullOrWhiteSpace(offer.displayMessage)
                ? $"SAVE COULD NOT BE LOADED\n{offer.displayMessage}"
                : "SAVE COULD NOT BE LOADED\nA recovery backup is available.";
            CreateText(row, "Recovery Detail", detail, new Vector2(0f, 0f), new Vector2(0.56f, 1f), new Vector2(18f, 8f), new Vector2(-8f, -8f), 18, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor);
            CreateButton(row, "Load Backup", "LOAD BACKUP", new Vector2(0.57f, 0.14f), new Vector2(0.78f, 0.86f), Vector2.zero, Vector2.zero, RunLoadRecovery, !IsPersistenceBusy());
            CreateButton(row, "Cancel Backup", "CANCEL", new Vector2(0.79f, 0.14f), new Vector2(1f, 0.86f), Vector2.zero, new Vector2(-10f, 0f), () =>
            {
                _saveService.DismissRecoveryOffer();
                _lastStatus = "Recovery backup offer dismissed.";
                RebuildView();
            }, !IsPersistenceBusy());
        }

        private void AddSlotRow(LwsManualSaveSlotMetadata slot)
        {
            RectTransform row = AddRow($"Slot {slot.slotNumber}", 104f);
            string detail = slot.occupied
                ? $"{slot.SlotLabel}\nSaved: {FormatUtc(slot.savedUtcTicks)}\nLocation: {SafeLabel(slot.worldLabel, SafeLabel(slot.sceneName, "unknown world"))} | Playtime: {FormatDuration(slot.playtimeSeconds)}"
                : $"{slot.SlotLabel}\nEMPTY";
            CreateText(row, "Slot Detail", detail, new Vector2(0f, 0f), new Vector2(0.52f, 1f), new Vector2(18f, 8f), new Vector2(-8f, -8f), 18, FontStyle.Normal, TextAnchor.MiddleLeft, TextColor);
            CreateButton(row, "Save", slot.occupied ? "OVERWRITE" : "SAVE", new Vector2(0.53f, 0.14f), new Vector2(0.68f, 0.86f), Vector2.zero, Vector2.zero, () =>
            {
                if (slot.occupied)
                {
                    Confirm($"OVERWRITE SLOT {slot.slotNumber}?", "Existing saved game data in this manual slot will be backed up, then replaced.", () => RunSave(slot.slotNumber, true), LwsPersistenceMenuView.SaveLoad);
                }
                else
                {
                    RunSave(slot.slotNumber, false);
                }
            }, !IsPersistenceBusy());
            CreateButton(row, "Load", "LOAD", new Vector2(0.69f, 0.14f), new Vector2(0.84f, 0.86f), Vector2.zero, Vector2.zero, () => RunLoad(slot.slotNumber), slot.occupied && !_saveService.IsSaving && !_saveService.IsLoading);
            CreateButton(row, "Delete", "DELETE", new Vector2(0.85f, 0.14f), new Vector2(1f, 0.86f), Vector2.zero, new Vector2(-10f, 0f), () =>
            {
                Confirm($"DELETE SLOT {slot.slotNumber}?", "This deletes the manual save and its hidden recovery backup.", () => RunDelete(slot.slotNumber), LwsPersistenceMenuView.SaveLoad);
            }, slot.occupied && !IsPersistenceBusy());
        }

        private void AddProfileRow(LwsSaveProfileMetadata profile)
        {
            RectTransform row = AddRow($"Profile {profile.profileIndex}", 88f);
            bool active = _saveService.ActiveProfile != null && string.Equals(_saveService.ActiveProfile.stableProfileId, profile.stableProfileId, StringComparison.Ordinal);
            string label = $"{profile.DisplayNameOrFallback}\nLast played: {FormatUtc(profile.lastPlayedUtcTicks)} | Playtime: {FormatDuration(profile.totalPlaytimeSeconds)}";
            CreateText(row, "Profile Detail", label, new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(18f, 8f), new Vector2(-8f, -8f), 18, active ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleLeft, active ? TextColor : MutedTextColor);
            CreateButton(row, "Select", active ? "ACTIVE" : "SELECT", new Vector2(0.50f, 0.14f), new Vector2(0.65f, 0.86f), Vector2.zero, Vector2.zero, () =>
            {
                LwsSaveOperationResult result = _saveService.SelectProfile(profile.stableProfileId);
                _lastStatus = result.Message;
                RebuildView();
            }, !active && !IsPersistenceBusy());
            CreateButton(row, "Rename", "RENAME", new Vector2(0.66f, 0.14f), new Vector2(0.81f, 0.86f), Vector2.zero, Vector2.zero, () =>
            {
                LwsSaveOperationResult result = _saveService.RenameProfile(profile.stableProfileId, _profileNameInput != null ? _profileNameInput.text : profile.DisplayNameOrFallback);
                _lastStatus = result.Message;
                RebuildView();
            }, !IsPersistenceBusy());
            CreateButton(row, "Delete", "DELETE", new Vector2(0.82f, 0.14f), new Vector2(1f, 0.86f), Vector2.zero, new Vector2(-10f, 0f), () =>
            {
                Confirm($"DELETE PROFILE {profile.DisplayNameOrFallback}?", "This removes only this profile and its reserved Pixel Crushers save slots.", () =>
                {
                    LwsSaveOperationResult result = _saveService.DeleteProfile(profile.stableProfileId);
                    _lastStatus = result.Message;
                    SetView(LwsPersistenceMenuView.Profiles);
                }, LwsPersistenceMenuView.Profiles);
            }, _saveService.Profiles.Count > 1 && !IsPersistenceBusy());
        }
        private bool IsPersistenceBusy()
        {
            return _saveService != null &&
                   (_saveService.IsSaving ||
                    _saveService.IsLoading ||
                    (_saveService.LoadCoordinator != null && _saveService.LoadCoordinator.IsLoadActive));
        }

        private string ResolveBusyLabel()
        {
            if (_saveService == null)
            {
                return "MISSING";
            }

            if (_saveService.IsSaving)
            {
                return "SAVING...";
            }

            if (_saveService.LoadPhase == LwsSaveLoadCoordinatorPhase.LoadingTargetWorld ||
                _saveService.LoadPhase == LwsSaveLoadCoordinatorPhase.PreparingWorld ||
                _saveService.LoadPhase == LwsSaveLoadCoordinatorPhase.WaitingForWorldReady)
            {
                return "LOADING WORLD...";
            }

            if (_saveService.IsLoading || (_saveService.LoadCoordinator != null && _saveService.LoadCoordinator.IsLoadActive))
            {
                return "LOADING...";
            }

            return _saveService.PendingAutosave ? "AUTOSAVE PENDING" : "READY";
        }

        private string ResolveSaveSystemStatus()
        {
            if (_saveService == null)
            {
                return "MISSING";
            }

            string storer = string.IsNullOrWhiteSpace(_saveService.Diagnostics.activeStorer) ? "unknown storer" : _saveService.Diagnostics.activeStorer;
            return $"PIXEL CRUSHERS | {storer} | {ResolveBusyLabel()}";
        }

        private string ResolveAutosaveStatus()
        {
            if (_saveService == null)
            {
                return "UNKNOWN";
            }

            LwsAutosaveConfiguration configuration = _saveService.AutosaveConfiguration;
            string enabled = configuration != null && configuration.autosaveEnabled ? "ON" : "OFF";
            string pending = _saveService.PendingAutosave ? " | PENDING" : string.Empty;
            return $"{enabled}{pending}";
        }

        private void AddProfileActionRow()
        {
            RectTransform row = AddRow("Profile Actions", ButtonPreferredHeight);
            CreateButton(row, "Select Profile", "SELECT PROFILE", new Vector2(0f, 0.10f), new Vector2(0.24f, 0.90f), new Vector2(0f, 0f), new Vector2(-8f, 0f), () => SetProfilesView(LwsPersistenceMenuView.SaveLoad), !IsPersistenceBusy());
            CreateButton(row, "New Profile", "NEW PROFILE", new Vector2(0.25f, 0.10f), new Vector2(0.49f, 0.90f), new Vector2(0f, 0f), new Vector2(-8f, 0f), () => SetProfilesView(LwsPersistenceMenuView.SaveLoad), !IsPersistenceBusy());
            CreateButton(row, "Rename Profile", "RENAME PROFILE", new Vector2(0.50f, 0.10f), new Vector2(0.74f, 0.90f), new Vector2(0f, 0f), new Vector2(-8f, 0f), () => SetProfilesView(LwsPersistenceMenuView.SaveLoad), !IsPersistenceBusy());
            CreateButton(row, "Delete Profile", "DELETE PROFILE", new Vector2(0.75f, 0.10f), new Vector2(1f, 0.90f), new Vector2(0f, 0f), new Vector2(-10f, 0f), ConfirmDeleteActiveProfile, _saveService.Profiles.Count > 1 && !IsPersistenceBusy());
        }

        private void ConfirmDeleteActiveProfile()
        {
            LwsSaveProfileMetadata profile = _saveService.ActiveProfile;
            if (profile == null)
            {
                _lastStatus = "No active profile is selected.";
                RebuildView();
                return;
            }

            Confirm($"DELETE PROFILE {profile.DisplayNameOrFallback}?", "This removes this profile and its reserved Pixel Crushers save slots.", () =>
            {
                LwsSaveOperationResult result = _saveService.DeleteProfile(profile.stableProfileId);
                _lastStatus = result.Message;
                SetView(LwsPersistenceMenuView.SaveLoad);
            }, LwsPersistenceMenuView.SaveLoad);
        }

        private void SetProfilesView(LwsPersistenceMenuView returnView)
        {
            _profileReturnView = returnView;
            SetView(LwsPersistenceMenuView.Profiles);
        }

        private void BackFromSaveLoad()
        {
            if (_openContext == LwsPersistenceMenuOpenContext.DirectSaveLoad)
            {
                Hide();
                return;
            }

            SetView(LwsPersistenceMenuView.Main);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void RunSave(int slotNumber, bool overwrite)
        {
            LwsSaveOperationResult result = _saveService.Save(_saveService.ActiveProfileId, slotNumber, overwrite);
            _lastStatus = result.Message;
            SetView(LwsPersistenceMenuView.SaveLoad);
        }

        private void RunLoad(int slotNumber)
        {
            LwsSaveOperationResult result = _saveService.Load(_saveService.ActiveProfileId, slotNumber);
            _lastStatus = result.Message;
            SetView(LwsPersistenceMenuView.SaveLoad);
        }

        private void RunDelete(int slotNumber)
        {
            LwsSaveOperationResult result = _saveService.Delete(_saveService.ActiveProfileId, slotNumber);
            _lastStatus = result.Message;
            SetView(LwsPersistenceMenuView.SaveLoad);
        }

        private void RunLoadAutosave()
        {
            LwsSaveOperationResult result = _saveService.LoadAutosave(_saveService.ActiveProfileId);
            _lastStatus = result.Message;
            SetView(LwsPersistenceMenuView.SaveLoad);
        }

        private void RunDeleteAutosave()
        {
            LwsSaveOperationResult result = _saveService.DeleteAutosave(_saveService.ActiveProfileId);
            _lastStatus = result.Message;
            SetView(LwsPersistenceMenuView.SaveLoad);
        }

        private void RunLoadRecovery()
        {
            LwsSaveOperationResult result = _saveService.LoadRecoveryBackup();
            _lastStatus = result.Message;
            SetView(LwsPersistenceMenuView.SaveLoad);
        }

        private void Confirm(string title, string body, Action action, LwsPersistenceMenuView cancelView)
        {
            _pendingConfirmTitle = title;
            _pendingConfirmBody = body;
            _pendingConfirmAction = action;
            _confirmCancelView = cancelView;
            _view = LwsPersistenceMenuView.Confirm;
            RebuildView();
        }

        private void ClearConfirm()
        {
            _pendingConfirmTitle = null;
            _pendingConfirmBody = null;
            _pendingConfirmAction = null;
            _confirmCancelView = LwsPersistenceMenuView.Main;
        }

        private void SetView(LwsPersistenceMenuView view)
        {
            ClearConfirm();
            _view = view;
            RebuildView();
        }

        private void SetHeader(string title)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }
        }

        private void AddInfo(string label, string value)
        {
            RectTransform row = AddRow($"Info {label}", 42f);
            CreateText(row, "Label", $"{label}: {value}", Vector2.zero, Vector2.one, new Vector2(18f, 4f), new Vector2(-18f, -4f), 18, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
        }

        private void AddButton(string label, Action action, bool interactable)
        {
            RectTransform row = AddRow($"Button Row {label}", ButtonPreferredHeight);
            CreateButton(row, label, label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, action, interactable);
        }

        private InputField AddInput(string placeholder, string value)
        {
            RectTransform row = AddRow($"Input {placeholder}", 64f);
            Image image = row.gameObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.025f, 0.035f, 0.038f, 1f);
            }

            InputField input = row.gameObject.AddComponent<InputField>();
            Text text = CreateText(row, "Text", value ?? string.Empty, Vector2.zero, Vector2.one, new Vector2(18f, 6f), new Vector2(-18f, -6f), 22, FontStyle.Normal, TextAnchor.MiddleLeft, TextColor);
            Text placeholderText = CreateText(row, "Placeholder", placeholder, Vector2.zero, Vector2.one, new Vector2(18f, 6f), new Vector2(-18f, -6f), 22, FontStyle.Italic, TextAnchor.MiddleLeft, MutedTextColor);
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.text = value ?? string.Empty;
            input.targetGraphic = image;
            return input;
        }
        private RectTransform AddRow(string name, float height)
        {
            RectTransform row = CreateRect(_contentRoot, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<Image>().color = RowColor;
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            return row;
        }


        private void CloseDevelopmentOverlays()
        {
            ResolveServices();
            if (_developmentUiService == null || _developmentUiService.RuntimeRoot == null)
            {
                return;
            }

            LwsDevelopmentUiRoot root = _developmentUiService.RuntimeRoot;
            if (root.ControlCenterVisible)
            {
                root.HideControlCenter();
            }

            if (root.BigMapVisible)
            {
                root.HideBigMap();
            }

            if (root.WeatherTestPanelVisible)
            {
                root.HideWeatherTestPanel();
            }
        }
        private void SuppressDrivingInputSources(bool suppress)
        {
            if (suppress)
            {
                foreach (LwsKeyboardGamepadTruckInputSource source in FindObjectsByType<LwsKeyboardGamepadTruckInputSource>(FindObjectsSortMode.None))
                {
                    if (source == null || _suppressedDrivingSources.ContainsKey(source))
                    {
                        continue;
                    }

                    _suppressedDrivingSources[source] = source.DrivingInputSuppressed;
                    source.SetDrivingInputSuppressed(true);
                }

                return;
            }

            foreach (KeyValuePair<LwsKeyboardGamepadTruckInputSource, bool> pair in _suppressedDrivingSources.ToArray())
            {
                if (pair.Key != null)
                {
                    pair.Key.SetDrivingInputSuppressed(pair.Value);
                }
            }

            _suppressedDrivingSources.Clear();
        }

        private void EnsureEventSystem()
        {
            EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            EventSystem selected = null;
            for (int i = 0; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] == null || !eventSystems[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

#if ENABLE_INPUT_SYSTEM
                if (eventSystems[i].GetComponent<InputSystemUIInputModule>() != null)
                {
                    selected = eventSystems[i];
                    break;
                }
#endif
                selected ??= eventSystems[i];
            }

            if (selected == null)
            {
                GameObject eventSystemObject = new GameObject("IH Persistence Menu EventSystem");
                eventSystemObject.transform.SetParent(transform, false);
                selected = eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = selected.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = selected.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.enabled = true;
            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }

            foreach (BaseInputModule module in selected.GetComponents<BaseInputModule>())
            {
                module.enabled = module == inputModule;
            }
#else
            StandaloneInputModule inputModule = selected.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                inputModule = selected.gameObject.AddComponent<StandaloneInputModule>();
            }

            inputModule.enabled = true;
#endif
            EventSystem.current = selected;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = ResolveUiFont();
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.text = text ?? string.Empty;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Action onClick, bool interactable)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = interactable ? ButtonColor : ButtonDisabledColor;
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;
            ColorBlock colors = button.colors;
            colors.normalColor = interactable ? ButtonColor : ButtonDisabledColor;
            colors.highlightedColor = ButtonHoverColor;
            colors.pressedColor = ButtonPressedColor;
            colors.disabledColor = ButtonDisabledColor;
            colors.selectedColor = ButtonHoverColor;
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            CreateText(rect, "Label", label, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, interactable ? TextColor : MutedTextColor);
            return button;
        }

        private static Font ResolveUiFont()
        {
            if (_uiFont == null)
            {
                _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _uiFont;
        }

        private static string FormatUtc(long ticks)
        {
            if (ticks <= 0)
            {
                return "unknown time";
            }

            return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }


        private static string FormatDuration(double seconds)
        {
            if (seconds <= 0d || double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                return "00:00:00";
            }

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours >= 1d)
            {
                return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
            }

            return $"00:{span.Minutes:00}:{span.Seconds:00}";
        }

        private static bool WasDirectSavePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
            {
                return true;
            }
#endif
            return Input.GetKeyDown(KeyCode.F3);
        }
        private static string SafeLabel(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
