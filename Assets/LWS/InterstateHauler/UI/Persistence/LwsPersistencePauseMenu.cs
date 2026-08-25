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
        void Hide();
        void Toggle();
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
            EnsureRuntime();
            _runtimeRoot?.Show();
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
    }

    public enum LwsPersistenceMenuView
    {
        Main,
        SaveLoad,
        Profiles,
        Confirm
    }

    [DefaultExecutionOrder(-45)]
    [DisallowMultipleComponent]
    public sealed class LwsPersistencePauseMenu : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0.01f, 0.015f, 0.018f, 0.74f);
        private static readonly Color PanelColor = new Color(0.05f, 0.065f, 0.07f, 0.96f);
        private static readonly Color RowColor = new Color(0.08f, 0.105f, 0.11f, 0.92f);
        private static readonly Color ButtonColor = new Color(0.13f, 0.18f, 0.19f, 1f);
        private static readonly Color ButtonDisabledColor = new Color(0.08f, 0.08f, 0.08f, 0.72f);
        private static readonly Color TextColor = new Color(0.88f, 0.94f, 0.93f, 1f);
        private static readonly Color MutedTextColor = new Color(0.62f, 0.72f, 0.72f, 1f);
        private static Font _uiFont;

        private readonly Dictionary<LwsKeyboardGamepadTruckInputSource, bool> _suppressedDrivingSources = new Dictionary<LwsKeyboardGamepadTruckInputSource, bool>();
        private LwsPersistenceMenuService _service;
        private LwsServiceRegistry _registry;
        private ILwsSaveService _saveService;
        private ILwsVehicleInputService _inputService;
        private Canvas _canvas;
        private RectTransform _panel;
        private RectTransform _contentRoot;
        private Text _titleText;
        private Text _statusText;
        private InputField _profileNameInput;
        private LwsPersistenceMenuView _view = LwsPersistenceMenuView.Main;
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
            BuildIfNeeded();
            ResolveServices();
            if (_canvas == null || IsOpen)
            {
                return;
            }

            _pauseSnapshotTaken = true;
            _previousTimeScale = Time.timeScale;
            _previousCursorVisible = Cursor.visible;
            _previousCursorLockMode = Cursor.lockState;
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SuppressDrivingInputSources(true);
            _view = LwsPersistenceMenuView.Main;
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
                Show();
            }
        }

        private void Update()
        {
            ResolveServices();
            if (WasPausePressed())
            {
                Toggle();
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
        }

        private void BuildIfNeeded()
        {
            if (_canvas != null)
            {
                return;
            }

            RectTransform canvasRect = CreateRect(transform, "Persistence Canvas", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _canvas = canvasRect.gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 6900;
            CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            Image backdrop = canvasRect.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;

            _panel = CreateRect(canvasRect, "Pause Save Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -390f), new Vector2(520f, 390f));
            _panel.gameObject.AddComponent<Image>().color = PanelColor;
            _titleText = CreateText(_panel, "Title", "INTERSTATE HAULER", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -58f), new Vector2(-28f, -14f), 28, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor);
            _statusText = CreateText(_panel, "Status", "Ready.", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 18f), new Vector2(-28f, 58f), 16, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
            _contentRoot = CreateRect(_panel, "Content", Vector2.zero, Vector2.one, new Vector2(28f, 74f), new Vector2(-28f, -78f));
            VerticalLayoutGroup layout = _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;
            ContentSizeFitter fitter = _contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

            _statusText.text = _saveService.IsSaving ? "SAVING..." : _saveService.IsLoading ? "LOADING..." : _lastStatus;
        }

        private void BuildMainView()
        {
            SetHeader("PAUSE MENU");
            AddInfo("Current profile", _saveService.ActiveProfile != null ? _saveService.ActiveProfile.DisplayNameOrFallback : "none");
            AddInfo("Save authority", "PIXEL CRUSHERS");
            AddInfo("Storage path", _saveService.Diagnostics.activeStorer);
            AddButton("RESUME", Hide, true);
            AddButton("SAVE / LOAD", () => SetView(LwsPersistenceMenuView.SaveLoad), true);
            AddButton("PROFILES", () => SetView(LwsPersistenceMenuView.Profiles), true);
            AddButton("PRINT SAVE DIAGNOSTICS", () =>
            {
                Debug.Log(_saveService.BuildDiagnosticsReport());
                _lastStatus = "Save diagnostics printed to Console.";
                RebuildView();
            }, true);
        }

        private void BuildSaveLoadView()
        {
            SetHeader("SAVE / LOAD");
            LwsSaveProfileMetadata profile = _saveService.ActiveProfile;
            AddInfo("Current profile", profile != null ? profile.DisplayNameOrFallback : "none");
            AddInfo("Busy", _saveService.IsSaving ? "SAVING..." : _saveService.IsLoading ? "LOADING..." : "READY");

            foreach (LwsManualSaveSlotMetadata slot in _saveService.GetManualSlots())
            {
                AddSlotRow(slot);
            }

            AddButton("BACK", () => SetView(LwsPersistenceMenuView.Main), true);
        }

        private void BuildProfilesView()
        {
            SetHeader("PROFILES");
            AddInfo("Active", _saveService.ActiveProfile != null ? _saveService.ActiveProfile.DisplayNameOrFallback : "none");
            _profileNameInput = AddInput("Profile Name", _saveService.ActiveProfile != null ? _saveService.ActiveProfile.DisplayNameOrFallback : "Driver");
            AddButton("CREATE PROFILE", () =>
            {
                LwsSaveOperationResult result = _saveService.CreateProfile(_profileNameInput.text, out _);
                _lastStatus = result.Message;
                RebuildView();
            }, true);

            foreach (LwsSaveProfileMetadata profile in _saveService.Profiles)
            {
                AddProfileRow(profile);
            }

            AddButton("BACK", () => SetView(LwsPersistenceMenuView.Main), true);
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

        private void AddSlotRow(LwsManualSaveSlotMetadata slot)
        {
            RectTransform row = AddRow($"Slot {slot.slotNumber}", 74f);
            string detail = slot.occupied
                ? $"{slot.SlotLabel}: {FormatUtc(slot.savedUtcTicks)} | {SafeLabel(slot.sceneName, "unknown scene")} | truck {SafeLabel(slot.truckDefinitionId, "unknown")}"
                : $"{slot.SlotLabel}: EMPTY";
            CreateText(row, "Slot Detail", detail, new Vector2(0f, 0f), new Vector2(0.52f, 1f), new Vector2(14f, 8f), new Vector2(-8f, -8f), 15, FontStyle.Normal, TextAnchor.MiddleLeft, TextColor);
            CreateButton(row, "Save", slot.occupied ? "OVERWRITE" : "SAVE", new Vector2(0.53f, 0.12f), new Vector2(0.68f, 0.88f), Vector2.zero, Vector2.zero, () =>
            {
                if (slot.occupied)
                {
                    Confirm($"OVERWRITE SLOT {slot.slotNumber}?", "Existing saved game data in this manual slot will be replaced.", () => RunSave(slot.slotNumber, true), LwsPersistenceMenuView.SaveLoad);
                }
                else
                {
                    RunSave(slot.slotNumber, false);
                }
            }, !_saveService.IsSaving && !_saveService.IsLoading);
            CreateButton(row, "Load", "LOAD", new Vector2(0.69f, 0.12f), new Vector2(0.84f, 0.88f), Vector2.zero, Vector2.zero, () => RunLoad(slot.slotNumber), slot.occupied && !_saveService.IsSaving && !_saveService.IsLoading);
            CreateButton(row, "Delete", "DELETE", new Vector2(0.85f, 0.12f), new Vector2(1f, 0.88f), Vector2.zero, new Vector2(-10f, 0f), () =>
            {
                Confirm($"DELETE SLOT {slot.slotNumber}?", "This cannot be undone.", () => RunDelete(slot.slotNumber), LwsPersistenceMenuView.SaveLoad);
            }, slot.occupied && !_saveService.IsSaving && !_saveService.IsLoading);
        }

        private void AddProfileRow(LwsSaveProfileMetadata profile)
        {
            RectTransform row = AddRow($"Profile {profile.profileIndex}", 64f);
            bool active = _saveService.ActiveProfile != null && string.Equals(_saveService.ActiveProfile.stableProfileId, profile.stableProfileId, StringComparison.Ordinal);
            string label = $"{profile.DisplayNameOrFallback} | id {profile.stableProfileId}";
            CreateText(row, "Profile Detail", label, new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(14f, 8f), new Vector2(-8f, -8f), 14, active ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleLeft, active ? TextColor : MutedTextColor);
            CreateButton(row, "Select", active ? "ACTIVE" : "SELECT", new Vector2(0.50f, 0.12f), new Vector2(0.65f, 0.88f), Vector2.zero, Vector2.zero, () =>
            {
                LwsSaveOperationResult result = _saveService.SelectProfile(profile.stableProfileId);
                _lastStatus = result.Message;
                RebuildView();
            }, !active);
            CreateButton(row, "Rename", "RENAME", new Vector2(0.66f, 0.12f), new Vector2(0.81f, 0.88f), Vector2.zero, Vector2.zero, () =>
            {
                LwsSaveOperationResult result = _saveService.RenameProfile(profile.stableProfileId, _profileNameInput != null ? _profileNameInput.text : profile.DisplayNameOrFallback);
                _lastStatus = result.Message;
                RebuildView();
            }, true);
            CreateButton(row, "Delete", "DELETE", new Vector2(0.82f, 0.12f), new Vector2(1f, 0.88f), Vector2.zero, new Vector2(-10f, 0f), () =>
            {
                Confirm($"DELETE PROFILE {profile.DisplayNameOrFallback}?", "This removes only this profile and its reserved Pixel Crushers save slots.", () =>
                {
                    LwsSaveOperationResult result = _saveService.DeleteProfile(profile.stableProfileId);
                    _lastStatus = result.Message;
                    SetView(LwsPersistenceMenuView.Profiles);
                }, LwsPersistenceMenuView.Profiles);
            }, _saveService.Profiles.Count > 1);
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
            RectTransform row = AddRow($"Info {label}", 34f);
            CreateText(row, "Label", $"{label}: {value}", Vector2.zero, Vector2.one, new Vector2(12f, 2f), new Vector2(-12f, -2f), 15, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
        }

        private void AddButton(string label, Action action, bool interactable)
        {
            RectTransform row = AddRow($"Button Row {label}", 46f);
            CreateButton(row, label, label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, action, interactable);
        }

        private InputField AddInput(string placeholder, string value)
        {
            RectTransform row = AddRow($"Input {placeholder}", 48f);
            Image image = row.gameObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.025f, 0.035f, 0.038f, 1f);
            }

            InputField input = row.gameObject.AddComponent<InputField>();
            Text text = CreateText(row, "Text", value ?? string.Empty, Vector2.zero, Vector2.one, new Vector2(14f, 4f), new Vector2(-14f, -4f), 16, FontStyle.Normal, TextAnchor.MiddleLeft, TextColor);
            Text placeholderText = CreateText(row, "Placeholder", placeholder, Vector2.zero, Vector2.one, new Vector2(14f, 4f), new Vector2(-14f, -4f), 16, FontStyle.Italic, TextAnchor.MiddleLeft, MutedTextColor);
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
            label.text = text ?? string.Empty;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Action onClick, bool interactable)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = interactable ? ButtonColor : ButtonDisabledColor;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            CreateText(rect, "Label", label, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f), 14, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
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

        private static string SafeLabel(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
