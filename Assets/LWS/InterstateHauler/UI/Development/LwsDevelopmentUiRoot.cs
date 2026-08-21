using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(700)]
    [DisallowMultipleComponent]
    public sealed class LwsDevelopmentUiRoot : MonoBehaviour
    {
        private delegate void TruckCommandMutator(ref LwsVehicleCommandFrame frame);

        private static readonly Color PanelColor = new Color(0.035f, 0.043f, 0.045f, 0.96f);
        private static readonly Color PanelAccentColor = new Color(0.06f, 0.085f, 0.09f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.12f, 0.17f, 0.18f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.16f, 0.23f, 0.24f, 1f);
        private static readonly Color SelectedButtonColor = new Color(0.12f, 0.34f, 0.24f, 1f);
        private static readonly Color DisabledButtonColor = new Color(0.08f, 0.09f, 0.09f, 0.72f);
        private static readonly Color TextColor = new Color(0.88f, 0.93f, 0.92f, 1f);
        private static readonly Color MutedTextColor = new Color(0.62f, 0.71f, 0.72f, 1f);
        private static readonly Vector2 MinimapPanelSize = new Vector2(304f, 304f);
        private const float MinimapPanelMarginPixels = 28f;
        private const float MinimapMetersVisible = 2600f;
        private const float RoadLookupRefreshSeconds = 0.5f;

        private LwsDevelopmentUiService _service;
        private LwsServiceRegistry _registry;
        private ILwsNavigationService _navigationService;
        private ILwsRoadGraphService _roadGraphService;
        private ILwsWorldOriginService _originService;
        private ILwsWorldStreamingService _streamingService;
        private ILwsVehicleInputService _inputService;
        private ILwsTruckControlService _truckControlService;
        private ILwsTruckDashboardService _dashboardService;
        private ILwsWeatherService _weatherService;
        private ILwsRoadConditionService _roadConditionService;
        private ILwsTrafficService _trafficService;
        private ILwsPlayerSettingsService _playerSettingsService;
        private ILwsWheelCalibrationService _wheelCalibrationService;
        private ILwsForceFeedbackService _forceFeedbackService;
        private ILwsPlayerVehicleService _playerVehicleService;

        private Canvas _canvas;
        private CanvasScaler _canvasScaler;
        private GameObject _controlCenterPanel;
        private GameObject _bigMapPanel;
        private GameObject _minimapPanel;
        private RectTransform _tabList;
        private RectTransform _contentRoot;
        private ScrollRect _scrollRect;
        private Text _minimapStatusText;
        private Text _bigMapStatusText;
        private Text _bigMapDetailText;
        private LwsSemanticGpsMapGraphic _minimapGraphic;
        private LwsSemanticGpsMapGraphic _bigMapGraphic;
        private LwsDevelopmentUiTab _activeTab = LwsDevelopmentUiTab.Overview;
        private float _nextContentRefreshTime;
        private float _nextMapRefreshTime;
        private float _fpsSmoothed;
        private float _bigMapMetersVisible = 6500f;
        private Vector2 _bigMapPanMeters;
        private bool _cursorCaptured;
        private bool _previousCursorVisible;
        private CursorLockMode _previousCursorLock;
        private bool _bigMapPausedTime;
        private float _previousTimeScale = 1f;
        private float _nextRoadLookupTime;
        private string _cachedRoadDisplayName = "ROAD: UNKNOWN";
        private string _cachedSpeedLimitText = string.Empty;
        private string _lastActionMessage = "Ready.";

        public static LwsDevelopmentUiRoot Instance { get; private set; }
        public bool ControlCenterVisible => _controlCenterPanel != null && _controlCenterPanel.activeSelf;
        public bool BigMapVisible => _bigMapPanel != null && _bigMapPanel.activeSelf;
        public LwsDevelopmentUiTab ActiveTab => _activeTab;
        public Canvas Canvas => _canvas;
        public CanvasScaler CanvasScaler => _canvasScaler;
        public ScrollRect ScrollRect => _scrollRect;
        public LwsSemanticGpsMapGraphic MinimapGraphic => _minimapGraphic;
        public LwsSemanticGpsMapGraphic BigMapGraphic => _bigMapGraphic;
        public RectTransform MinimapRect => _minimapPanel != null ? _minimapPanel.GetComponent<RectTransform>() : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUi();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            RestoreCursorIfClear();
            RestoreTimeScaleIfPaused();
        }

        private void Update()
        {
            ResolveServices();
            UpdateNavigationPose();
            UpdateFps();
            HandleKeyboardShortcuts();

            if (Time.unscaledTime >= _nextMapRefreshTime)
            {
                _nextMapRefreshTime = Time.unscaledTime + 0.08f;
                RefreshMaps();
            }

            if (ControlCenterVisible && Time.unscaledTime >= _nextContentRefreshTime)
            {
                _nextContentRefreshTime = Time.unscaledTime + 0.35f;
                RebuildActiveTab();
            }
        }

        public void BindService(LwsDevelopmentUiService service, LwsServiceRegistry registry)
        {
            _service = service;
            _registry = registry;
            ResolveServices();
        }

        public void ShowControlCenter()
        {
            if (_controlCenterPanel == null)
            {
                BuildUi();
            }

            _controlCenterPanel.SetActive(true);
            _nextContentRefreshTime = 0f;
            CaptureCursor();
        }

        public void HideControlCenter()
        {
            if (_controlCenterPanel != null)
            {
                _controlCenterPanel.SetActive(false);
            }

            RestoreCursorIfClear();
        }

        public void ToggleControlCenter()
        {
            if (BigMapVisible)
            {
                HideBigMap();
            }

            if (ControlCenterVisible)
            {
                HideControlCenter();
            }
            else
            {
                ShowControlCenter();
            }
        }

        public void OpenTab(LwsDevelopmentUiTab tab)
        {
            _activeTab = tab;
            ShowControlCenter();
            RebuildActiveTab();
        }

        public void ShowBigMap()
        {
            if (_bigMapPanel == null)
            {
                BuildUi();
            }

            _bigMapPanel.SetActive(true);
            _minimapPanel.SetActive(false);
            PauseForBigMap();
            CaptureCursor();
            RefreshMaps();
        }

        public void HideBigMap()
        {
            if (_bigMapPanel != null)
            {
                _bigMapPanel.SetActive(false);
            }

            if (_minimapPanel != null)
            {
                _minimapPanel.SetActive(true);
            }

            RestoreTimeScaleIfPaused();
            RestoreCursorIfClear();
        }

        public void ToggleBigMap()
        {
            if (BigMapVisible)
            {
                HideBigMap();
            }
            else
            {
                ShowBigMap();
            }
        }

        private void BuildUi()
        {
            if (_canvas != null)
            {
                return;
            }

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 6500;
            _canvas.pixelPerfect = false;
            _canvasScaler = gameObject.AddComponent<CanvasScaler>();
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            BuildMinimap();
            BuildBigMap();
            BuildControlCenter();
            _controlCenterPanel.SetActive(false);
            _bigMapPanel.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("IH Development UI EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystem);
        }

        private void BuildMinimap()
        {
            _minimapPanel = CreateFixedPanel(
                transform,
                "GPS Minimap",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                MinimapPanelSize,
                new Vector2(-MinimapPanelMarginPixels, MinimapPanelMarginPixels),
                new Color(0.02f, 0.03f, 0.032f, 0.92f));
            _minimapPanel.AddComponent<RectMask2D>();
            _minimapGraphic = CreateSemanticMapGraphic(_minimapPanel.transform, "Semantic Road Graph Map", Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -46f));
            _minimapGraphic.raycastTarget = false;
            CreateText(_minimapPanel.transform, "GPS Title", "GPS", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -38f), new Vector2(82f, -8f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            _minimapStatusText = CreateText(_minimapPanel.transform, "GPS Status", "NO ROUTE | ROAD: UNKNOWN", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(64f, -38f), new Vector2(-72f, -8f), 12, FontStyle.Normal, TextAnchor.MiddleLeft);
            CreateButton(_minimapPanel.transform, "Map Button", "MAP", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-66f, -38f), new Vector2(-8f, -8f), ShowBigMap);
        }

        private void BuildBigMap()
        {
            _bigMapPanel = CreatePanel(transform, "Full GPS Map", Vector2.zero, Vector2.one, new Vector2(40f, 40f), new Vector2(-40f, -40f), new Color(0.015f, 0.022f, 0.024f, 0.985f));
            CreateText(_bigMapPanel.transform, "Big Map Title", "GPS / FULL MAP", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -52f), new Vector2(320f, -12f), 21, FontStyle.Bold, TextAnchor.MiddleLeft);
            _bigMapStatusText = CreateText(_bigMapPanel.transform, "Big Map Status", "No route", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(330f, -52f), new Vector2(-560f, -12f), 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            CreateButton(_bigMapPanel.transform, "Close Big Map", "CLOSE", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-132f, -48f), new Vector2(-24f, -12f), HideBigMap);

            GameObject mapFrame = CreatePanel(_bigMapPanel.transform, "Full Map Frame", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 112f), new Vector2(-330f, -72f), new Color(0.01f, 0.018f, 0.02f, 1f));
            mapFrame.AddComponent<RectMask2D>();
            _bigMapGraphic = CreateSemanticMapGraphic(mapFrame.transform, "Full Semantic Road Graph Map", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _bigMapGraphic.raycastTarget = false;

            GameObject controls = CreatePanel(_bigMapPanel.transform, "Full Map Controls", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-306f, 112f), new Vector2(-24f, -72f), PanelAccentColor);
            _bigMapDetailText = CreateText(controls.transform, "Map Details", string.Empty, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -168f), new Vector2(-14f, -14f), 13, FontStyle.Normal, TextAnchor.UpperLeft);
            float y = -46f;
            CreateButton(controls.transform, "Center Player", "CENTER PLAYER", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, y), new Vector2(-14f, y + 34f), CenterFullMapOnPlayer);
            y -= 42f;
            CreateButton(controls.transform, "Center Route", "CENTER ROUTE", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, y), new Vector2(-14f, y + 34f), CenterFullMapOnRoute);
            y -= 42f;
            CreateButton(controls.transform, "Zoom In", "ZOOM +", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(14f, y), new Vector2(-6f, y + 34f), () => SetBigMapZoom(_bigMapMetersVisible * 0.72f));
            CreateButton(controls.transform, "Zoom Out", "ZOOM -", new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(6f, y), new Vector2(-14f, y + 34f), () => SetBigMapZoom(_bigMapMetersVisible * 1.28f));
            y -= 42f;
            CreateButton(controls.transform, "Zoom Reset", "RESET ZOOM", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, y), new Vector2(-14f, y + 34f), () => SetBigMapZoom(6500f));
            y -= 52f;
            CreateButton(controls.transform, "Pan North", "PAN N", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(84f, y), new Vector2(-84f, y + 32f), () => PanBigMap(new Vector2(0f, 300f)));
            y -= 36f;
            CreateButton(controls.transform, "Pan West", "PAN W", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(14f, y), new Vector2(-6f, y + 32f), () => PanBigMap(new Vector2(-300f, 0f)));
            CreateButton(controls.transform, "Pan East", "PAN E", new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(6f, y), new Vector2(-14f, y + 32f), () => PanBigMap(new Vector2(300f, 0f)));
            y -= 36f;
            CreateButton(controls.transform, "Pan South", "PAN S", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(84f, y), new Vector2(-84f, y + 32f), () => PanBigMap(new Vector2(0f, -300f)));
        }

        private void BuildControlCenter()
        {
            _controlCenterPanel = CreatePanel(transform, "Development Control Center", Vector2.zero, Vector2.one, new Vector2(28f, 28f), new Vector2(-28f, -28f), PanelColor);
            CreateText(_controlCenterPanel.transform, "Control Center Title", "INTERSTATE HAULER DEVELOPMENT CONTROL CENTER", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -52f), new Vector2(-150f, -12f), 21, FontStyle.Bold, TextAnchor.MiddleLeft);
            CreateButton(_controlCenterPanel.transform, "Close Control Center", "CLOSE", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-132f, -48f), new Vector2(-22f, -12f), HideControlCenter);

            GameObject tabs = CreatePanel(_controlCenterPanel.transform, "Tabs", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(20f, 20f), new Vector2(252f, -68f), PanelAccentColor);
            _tabList = tabs.GetComponent<RectTransform>();
            var tabLayout = tabs.AddComponent<VerticalLayoutGroup>();
            tabLayout.padding = new RectOffset(8, 8, 8, 8);
            tabLayout.spacing = 6f;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = false;

            foreach (LwsDevelopmentUiTabDefinition tab in LwsDevelopmentUiCatalog.Tabs)
            {
                Button button = CreateButton(tabs.transform, $"Tab {tab.Label}", tab.Label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, () => OpenTab(tab.Tab));
                LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 34f;
            }

            GameObject viewport = CreatePanel(_controlCenterPanel.transform, "Scroll Viewport", new Vector2(0f, 0f), Vector2.one, new Vector2(272f, 20f), new Vector2(-20f, -68f), new Color(0.025f, 0.034f, 0.036f, 0.95f));
            viewport.AddComponent<RectMask2D>();
            _scrollRect = viewport.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _contentRoot = CreateRect(viewport.transform, "Scroll Content", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            _contentRoot.pivot = new Vector2(0.5f, 1f);
            var contentLayout = _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(16, 16, 16, 16);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlHeight = false;
            _contentRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scrollRect.content = _contentRoot;
            _scrollRect.viewport = viewport.GetComponent<RectTransform>();
            RebuildActiveTab();
        }

        private void RebuildActiveTab()
        {
            if (_contentRoot == null)
            {
                return;
            }

            ClearChildren(_contentRoot);
            LwsDevelopmentUiCatalog.TryGet(_activeTab, out LwsDevelopmentUiTabDefinition definition);
            AddHeader(definition.Label);
            AddInfo("Last action", _lastActionMessage);

            switch (_activeTab)
            {
                case LwsDevelopmentUiTab.Overview:
                    BuildOverviewTab();
                    break;
                case LwsDevelopmentUiTab.Truck:
                    BuildTruckTab();
                    break;
                case LwsDevelopmentUiTab.Transmission:
                    BuildTransmissionTab();
                    break;
                case LwsDevelopmentUiTab.InputWheel:
                    BuildInputWheelTab();
                    break;
                case LwsDevelopmentUiTab.Traffic:
                    BuildTrafficTab();
                    break;
                case LwsDevelopmentUiTab.GpsNavigation:
                    BuildGpsTab();
                    break;
                case LwsDevelopmentUiTab.Weather:
                    BuildWeatherTab();
                    break;
                case LwsDevelopmentUiTab.RoadConditions:
                    BuildRoadConditionsTab();
                    break;
                case LwsDevelopmentUiTab.Streaming:
                    BuildStreamingTab();
                    break;
                case LwsDevelopmentUiTab.FloatingOrigin:
                    BuildFloatingOriginTab();
                    break;
                case LwsDevelopmentUiTab.FiftyMileTest:
                    BuildFiftyMileTab();
                    break;
                case LwsDevelopmentUiTab.Performance:
                    BuildPerformanceTab();
                    break;
                case LwsDevelopmentUiTab.Systems:
                    BuildSystemsTab();
                    break;
            }
        }

        private void BuildOverviewTab()
        {
            LwsNavigationRuntimeState nav = _navigationService?.RuntimeState;
            Lws18SpeedTransmissionController transmission = FindFirstObjectByType<Lws18SpeedTransmissionController>();
            AddInfo("Scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            AddInfo("Input owner", _inputService != null ? $"{_inputService.ActiveOwner} / {_inputService.ActiveSourceId}" : "missing");
            AddInfo("Truck", _truckControlService?.ActiveState.vehicleId ?? "none");
            AddInfo("Transmission", FormatTransmissionOverview(transmission));
            AddInfo("Gear", transmission != null ? transmission.DisplayState.displayLabel : "--");
            AddInfo("Route", nav != null && nav.routeActive ? $"{FormatDistance(nav.distanceRemainingMeters)} remaining" : "inactive");
            AddInfo("Road", $"{ResolveCurrentRoadText()} {ResolveSpeedLimitText()}");
            AddInfo("Weather", _weatherService != null ? _weatherService.CurrentSnapshot.weatherPresetId : "missing");
            AddInfo("Road condition", _roadConditionService != null ? _roadConditionService.CurrentSnapshot.MajorGameplayState : "missing");
            AddInfo("Origin", _originService != null ? $"{_originService.CurrentOriginOffset} v{_originService.OriginVersion}" : "missing");
            AddInfo("Streaming", _streamingService != null ? $"{_streamingService.WorldId} / {_streamingService.ActiveWorldChunkId}" : "missing");
            AddButtonRow(("OPEN GPS MAP", ShowBigMap), ("GPS TAB", () => OpenTab(LwsDevelopmentUiTab.GpsNavigation)));
        }

        private void BuildTruckTab()
        {
            LwsTruckControlState state = _truckControlService != null ? _truckControlService.ActiveState : default;
            AddInfo("Engine", $"{state.ignitionState} / running: {state.engineRunning}");
            AddInfo("Parking brake", state.parkingBrakeOn ? "ON" : "OFF");
            AddInfo("Lights", $"head: {state.headlightsOn}, high: {state.highBeamsOn}, hazards: {state.hazardsOn}, signal: {state.turnSignal}");
            AddInfo("Trailer", state.trailerAttached ? state.trailerId : "not attached");
            AddButtonRow(("START", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.engineStart = LwsMomentaryIntent.Pressed)),
                ("STOP", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.engineStop = LwsMomentaryIntent.Pressed)),
                ("PARK BRAKE", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.parkingBrakeToggle = LwsMomentaryIntent.Pressed)));
            AddButtonRow(("HEADLIGHTS", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.lowBeamLights = LwsMomentaryIntent.Pressed)),
                ("HIGH BEAMS", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.highBeamLights = LwsMomentaryIntent.Pressed)),
                ("HAZARDS", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.hazardLights = LwsMomentaryIntent.Pressed)));
            AddButtonRow(("SIGNAL LEFT", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.leftIndicator = LwsMomentaryIntent.Pressed)),
                ("SIGNAL RIGHT", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.rightIndicator = LwsMomentaryIntent.Pressed)),
                ("WIPERS", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.wipers = LwsMomentaryIntent.Pressed)));
            AddButtonRow(("HORN", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.horn = LwsMomentaryIntent.Pressed)),
                ("AIR HORN", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.airHorn = LwsMomentaryIntent.Pressed)),
                ("FLIP OFF", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.flipOffDriver = LwsMomentaryIntent.Pressed)));
            AddButtonRow(("TRAILER", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.trailerAttachDetach = LwsMomentaryIntent.Pressed)),
                ("CAMERA", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.cameraCycle = LwsMomentaryIntent.Pressed)),
                ("LOOK RESET", () => SendTruckCommand((ref LwsVehicleCommandFrame c) => c.lookReset = LwsMomentaryIntent.Pressed)));
        }

        private void BuildTransmissionTab()
        {
            Lws18SpeedTransmissionController transmission = FindFirstObjectByType<Lws18SpeedTransmissionController>();
            if (transmission == null)
            {
                AddInfo("Transmission", "controller not found");
                return;
            }

            AddInfo("Mode", transmission.DevelopmentAutomaticModeActive ? "AUTOMATIC" : "18-SPEED MANUAL");
            AddInfo("Selector", transmission.DevelopmentAutomaticModeActive
                ? Lws18SpeedTransmissionController.GetAutomaticSelectorLabel(transmission.AutomaticSelector)
                : "AUTOMATIC SELECTOR UNAVAILABLE IN MANUAL MODE");
            AddInfo("Display", transmission.DisplayState.displayLabel);
            AddInfo("Current Gear", transmission.DisplayState.displayLabel);
            AddInfo("Target Gear", transmission.DisplayState.automaticTargetLabel);
            AddInfo("RPM", $"{transmission.DisplayState.engineRpm:0}");
            AddInfo("Vehicle Speed", FormatSignedSpeed(transmission.DisplayState.signedSpeedMetersPerSecond));
            AddInfo("Range", $"{transmission.DisplayState.engagedRange} (requested {transmission.DisplayState.requestedRange})");
            AddInfo("Splitter", $"{transmission.DisplayState.engagedSplitter} (requested {transmission.DisplayState.requestedSplitter})");
            AddInfo("Shift state", transmission.DisplayState.shiftState.ToString());
            AddInfo("Last rejection", transmission.DisplayState.lastRejectionReason.ToString());
            AddTransmissionSelectorRow(transmission);
            AddButtonRow((transmission.DevelopmentAutomaticModeActive ? "SWITCH TO MANUAL" : "SWITCH TO AUTOMATIC", () =>
            {
                bool nextAutomatic = !transmission.DevelopmentAutomaticModeActive;
                transmission.TrySetDevelopmentAutomaticTestMode(nextAutomatic, out _lastActionMessage);
                RebuildActiveTab();
            }));
        }

        private void BuildInputWheelTab()
        {
            LwsWheelInputSource wheel = FindFirstObjectByType<LwsWheelInputSource>();
            LwsWheelInputFrame frame = wheel != null ? wheel.LastFrame : default;
            AddInfo("Input owner", _inputService != null ? $"{_inputService.ActiveOwner} / {_inputService.ActiveSourceId}" : "missing");
            AddInfo("Wheel", wheel != null ? $"{frame.connectionState} / {frame.deviceDisplayName}" : "not present");
            AddInfo("Analog", wheel != null ? $"steer {frame.analog.steering:0.00}, throttle {frame.analog.throttle:0.00}, brake {frame.analog.brake:0.00}, clutch {frame.analog.clutch:0.00}" : "--");
            AddInfo("Shifter", wheel != null ? $"{frame.shifter.activeGate} range {frame.shifter.range} splitter {frame.shifter.splitter}" : "--");
            AddInfo("FFB", _forceFeedbackService != null ? $"{_forceFeedbackService.Status.enabled} / {_forceFeedbackService.Status.statusMessage}" : "service missing");
            AddInfo("Calibration", _wheelCalibrationService != null ? _wheelCalibrationService.CurrentProfile.displayName : "missing");
            AddButtonRow(("REFRESH WHEEL", () =>
                {
                    bool selected = wheel != null && wheel.SelectPreferredDevice();
                    _lastActionMessage = selected ? "Wheel preferred device selected." : "No matching wheel device selected.";
                }),
                ("NEUTRALIZE", () =>
                {
                    wheel?.NeutralizeForDisconnect("Development control center neutralize.");
                    _inputService?.NeutralizeInput();
                    _lastActionMessage = "Wheel/input neutralized.";
                }));
        }

        private void BuildTrafficTab()
        {
            LwsUtsHighwayTrafficController traffic = FindFirstObjectByType<LwsUtsHighwayTrafficController>();
            AddInfo("Traffic service", _trafficService != null ? $"{_trafficService.ActiveTrafficVehicles.Count} registered vehicles" : "missing");
            AddInfo("UTS controller", traffic != null ? traffic.UtsAvailability : "not present");
            AddInfo("Runtime", traffic != null ? $"active {traffic.Stats.ActiveVehicles}, lanes {traffic.GeneratedLaneCount}, max {traffic.MaxActiveVehicles}" : "--");
            AddButtonRow(("SPAWN ONE", () =>
                {
                    if (traffic == null)
                    {
                        _lastActionMessage = "Traffic controller missing.";
                        return;
                    }

                    traffic.TrySpawnOneForValidation(out string message);
                    _lastActionMessage = message;
                }),
                ("FILL TO MAX", () =>
                {
                    int count = traffic != null ? traffic.FillTrafficToMaxForValidation() : 0;
                    _lastActionMessage = traffic != null ? $"Traffic fill requested: {count} active." : "Traffic controller missing.";
                }));
        }

        private void BuildGpsTab()
        {
            LwsNavigationRuntimeState state = _navigationService?.RuntimeState;
            LwsSemanticGpsMapGraphic map = _minimapGraphic;
            LwsRouteResult route = _navigationService?.CurrentRoute;
            LwsWorldPositionD playerGlobal = ResolvePlayerGlobalPosition();
            AddInfo("Route", state != null && state.routeActive ? $"{state.routeId} / {FormatDistance(state.distanceRemainingMeters)}" : "inactive");
            AddInfo("Next maneuver", state != null ? $"{LwsNavigationManeuverCatalog.GetDisplayName(state.nextManeuver)} / {FormatDistance(state.distanceToNextManeuverMeters)}" : "--");
            AddInfo("ETA", state != null ? FormatEta(state.estimatedTimeRemainingSeconds) : "--");
            AddInfo("Current road", $"{ResolveCurrentRoadText()} {ResolveSpeedLimitText()}");
            AddInfo("Road Graph Bound", map != null && map.GraphBound ? "YES" : "NO");
            AddInfo("Graph ID", map != null && !string.IsNullOrWhiteSpace(map.GraphId) ? map.GraphId : "--");
            AddInfo("Road Count", map != null ? map.RoadCount.ToString() : "0");
            AddInfo("Edge Count", map != null ? map.EdgeCount.ToString() : "0");
            AddInfo("Centerline Samples", map != null ? map.CenterlineSampleCount.ToString() : "0");
            AddInfo("Map Vertices", map != null ? map.MapVertexCount.ToString() : "0");
            AddInfo("Map Triangles", map != null ? map.MapTriangleCount.ToString() : "0");
            AddInfo("Route Active", route != null && route.succeeded ? "YES" : "NO");
            AddInfo("Route Points", map != null ? map.RoutePointCount.ToString() : "0");
            AddInfo("Player Global Position", playerGlobal.ToString());
            AddInfo("Map Zoom", $"{MinimapMetersVisible:0} m");
            AddInfo("Voice", _playerSettingsService != null && _playerSettingsService.GpsVoiceGuidanceEnabled ? "enabled" : "disabled");
            AddButtonRow(("START TEST ROUTE", () => RequestTestRoute(false)),
                ("ROUTE TO MILE 50", () => RequestTestRoute(true)),
                ("CLEAR ROUTE", () =>
                {
                    _navigationService?.ClearRoute();
                    _lastActionMessage = "GPS route cleared.";
                }));
            AddButtonRow(("RECALCULATE", RecalculateRoute),
                ("CENTER MAP", CenterFullMapOnPlayer),
                ("VOICE TOGGLE", () =>
                {
                    if (_playerSettingsService != null)
                    {
                        _playerSettingsService.SetGpsVoiceGuidanceEnabled(!_playerSettingsService.GpsVoiceGuidanceEnabled);
                        _lastActionMessage = $"GPS voice {(_playerSettingsService.GpsVoiceGuidanceEnabled ? "enabled" : "disabled")}.";
                    }
                }));
        }

        private void BuildWeatherTab()
        {
            LwsWeatherSnapshot weather = _weatherService != null ? _weatherService.CurrentSnapshot : LwsWeatherSnapshot.Clear;
            AddInfo("Preset", weather.weatherPresetId);
            AddInfo("Time", $"{weather.timeOfDayHours:0.00}h");
            AddInfo("Conditions", $"{weather.condition}, precip {weather.precipitationIntensity01:0.00}, fog {weather.fogIntensity01:0.00}");
            AddButtonRow(("CLEAR", () => RequestWeather(LwsWeatherPresetCatalog.ClearId)),
                ("RAIN", () => RequestWeather(LwsWeatherPresetCatalog.HeavyRainId)),
                ("SNOW", () => RequestWeather(LwsWeatherPresetCatalog.HeavySnowId)),
                ("FOG", () => RequestWeather(LwsWeatherPresetCatalog.FogId)));
            AddButtonRow(("NOON", () => SetWeatherTime(12f)),
                ("NIGHT", () => SetWeatherTime(22f)));
        }

        private void BuildRoadConditionsTab()
        {
            if (_roadConditionService == null)
            {
                AddInfo("Road conditions", "service missing");
                return;
            }

            LwsRoadConditionSnapshot state = _roadConditionService.CurrentSnapshot;
            AddInfo("Mode", _roadConditionService.Mode.ToString());
            AddInfo("Condition", $"{state.MajorGameplayState} / {state.condition}");
            AddInfo("Grip", $"long {state.longitudinalGripMultiplier01:0.00}, lateral {state.lateralGripMultiplier01:0.00}, brake {state.brakingGripMultiplier01:0.00}");
            AddButtonRow(("AUTO", () => _roadConditionService.SetAutoFromWeather()),
                ("DRY", () => _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceDry)),
                ("WET", () => _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceWet)),
                ("SNOW", () => _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceSnow)),
                ("ICE", () => _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceIce)));
            AddButtonRow(("RESET ROAD", () => _roadConditionService.ResetCurrentRoad()));
        }

        private void BuildStreamingTab()
        {
            if (_streamingService == null)
            {
                AddInfo("Streaming", "service missing");
                return;
            }

            AddInfo("World", _streamingService.WorldId);
            AddInfo("Active chunk", _streamingService.ActiveWorldChunkId);
            AddInfo("Frozen", _streamingService.IsFrozen ? "YES" : "NO");
            AddInfo("Chunks", $"{_streamingService.ChunkStates.Count}");
            AddInfo("Last load/unload", $"{_streamingService.LastLoadDurationSeconds:0.000}s / {_streamingService.LastUnloadDurationSeconds:0.000}s");
            AddButtonRow((_streamingService.IsFrozen ? "UNFREEZE" : "FREEZE", () => _streamingService.SetFrozen(!_streamingService.IsFrozen)),
                ("LOAD ALL", () => _streamingService.LoadAllChunks()),
                ("UNLOAD DISTANT", () => _streamingService.UnloadDistantChunks()),
                ("RELOAD NEAR", () => _streamingService.ReloadCurrentNeighborhood()));
        }

        private void BuildFloatingOriginTab()
        {
            if (_originService == null)
            {
                AddInfo("Floating origin", "service missing");
                return;
            }

            AddInfo("Enabled", _originService.Enabled ? "YES" : "NO");
            AddInfo("Offset", _originService.CurrentOriginOffset.ToString());
            AddInfo("Player global", _originService.PlayerGlobalPosition.ToString());
            AddInfo("Shifts", $"{_originService.ShiftCount}, last {_originService.LastShiftDurationMilliseconds:0.000}ms");
            AddInfo("Participants", $"{_originService.RegisteredParticipantCount}");
            AddButtonRow((_originService.Enabled ? "DISABLE" : "ENABLE", () => _originService.SetEnabled(!_originService.Enabled)),
                ("FORCE SHIFT", () =>
                {
                    _originService.ForceShiftNow(_originService.PlayerLocalPosition, "Development control center force shift.", out LwsOriginShiftEvent shiftEvent);
                    _lastActionMessage = shiftEvent.Message;
                }),
                ("RESET ORIGIN", () =>
                {
                    _originService.ResetValidationOrigin();
                    _lastActionMessage = "Validation origin reset.";
                }));
        }

        private void BuildFiftyMileTab()
        {
            LwsFiftyMileHighwayValidationController fifty = FindFirstObjectByType<LwsFiftyMileHighwayValidationController>();
            if (fifty == null)
            {
                AddInfo("50-mile controller", "not present in current scene");
                return;
            }

            AddInfo("Mile", $"{fifty.CurrentMile:0.00} / {LwsFiftyMileHighwayModel.TotalMiles:0.0}");
            AddInfo("Chunk", fifty.CurrentChunkId);
            AddInfo("Remaining", $"{fifty.DistanceRemainingMeters / LwsFiftyMileHighwayModel.MetersPerMile:0.00} mi");
            AddInfo("Weather cycle", fifty.AutomaticWeatherCycle ? "enabled" : "disabled");
            AddInfo("Report", fifty.LastReport);
            AddButtonRow(("ROUTE TO MILE 50", () => { _lastActionMessage = fifty.RequestGpsRoute() ? "50-mile GPS route started." : fifty.LastError; }),
                ("MILE 0", () => TeleportFifty(fifty, 0d)),
                ("MILE 25", () => TeleportFifty(fifty, 25d)),
                ("MILE 50", () => TeleportFifty(fifty, 50d)));
            AddButtonRow(("WEATHER CYCLE", () =>
            {
                fifty.SetAutomaticWeatherCycle(!fifty.AutomaticWeatherCycle);
                _lastActionMessage = $"50-mile weather cycle {(fifty.AutomaticWeatherCycle ? "enabled" : "disabled")}.";
            }));
        }

        private void BuildPerformanceTab()
        {
            AddInfo("FPS", _fpsSmoothed > 0f ? _fpsSmoothed.ToString("0.0") : "--");
            AddInfo("Frame ms", _fpsSmoothed > 0f ? (1000f / _fpsSmoothed).ToString("0.00") : "--");
            AddInfo("Route segments", _minimapGraphic != null ? (_minimapGraphic.HasRoutePresentation ? "cached" : "none") : "missing");
            AddInfo("Road graph cache", _minimapGraphic != null && _minimapGraphic.HasRoadPresentation ? "ready" : "empty");
            AddInfo("Map zoom", $"{_bigMapMetersVisible:0} m visible");
        }

        private void BuildSystemsTab()
        {
            if (_registry == null)
            {
                AddInfo("Registry", "missing");
                return;
            }

            foreach (LwsServiceRegistration registration in _registry.Registrations)
            {
                AddInfo(registration.ServiceId, registration.State.ToString());
            }
        }

        private void RefreshMaps()
        {
            LwsWorldPositionD playerGlobal = ResolvePlayerGlobalPosition();
            Vector3 playerGlobalVector = playerGlobal.ToVector3();
            Vector3 playerForward = ResolvePlayerForward();
            LwsRoadGraph graph = _roadGraphService?.ActiveGraph;
            LwsRouteResult route = _navigationService?.CurrentRoute;
            _minimapGraphic?.SetMapData(graph, route, playerGlobal, playerForward, true, MinimapMetersVisible, Vector2.zero, 0.4f);
            _bigMapGraphic?.SetMapData(graph, route, playerGlobal, playerForward, false, _bigMapMetersVisible, _bigMapPanMeters);
            RefreshRoadLookupCache(playerGlobalVector);

            LwsNavigationRuntimeState state = _navigationService?.RuntimeState;
            bool routeActive = state != null && state.routeActive;
            string status = ResolveMapStatus(graph, state, routeActive);
            if (_minimapStatusText != null)
            {
                _minimapStatusText.text = status;
            }

            if (_bigMapStatusText != null)
            {
                _bigMapStatusText.text = status;
            }

            if (_bigMapDetailText != null)
            {
                _bigMapDetailText.text = routeActive
                    ? $"Next: {state.nextInstructionText}\nManeuver: {LwsNavigationManeuverCatalog.GetDisplayName(state.nextManeuver)}\nDistance to maneuver: {FormatDistance(state.distanceToNextManeuverMeters)}\nRemaining: {FormatDistance(state.distanceRemainingMeters)}\nETA: {FormatEta(state.estimatedTimeRemainingSeconds)}\nRoad: {ResolveCurrentRoadText()}\nSpeed limit: {ResolveSpeedLimitText()}"
                    : $"No route active.\nGraph: {(graph != null ? graph.graphId : "missing")}\nRoads: {(_bigMapGraphic != null ? _bigMapGraphic.RoadCount : 0)}\nEdges: {(_bigMapGraphic != null ? _bigMapGraphic.EdgeCount : 0)}\nMap triangles: {(_bigMapGraphic != null ? _bigMapGraphic.BaseRoadTriangleCount : 0)}\nPlayer global: {playerGlobal}\nZoom: {_bigMapMetersVisible:0} m";
            }
        }

        private string ResolveMapStatus(LwsRoadGraph graph, LwsNavigationRuntimeState state, bool routeActive)
        {
            if (_roadGraphService == null || _originService == null)
            {
                return "GPS MAP UNAVAILABLE";
            }

            if (graph == null)
            {
                return "NO ROAD GRAPH";
            }

            if (_minimapGraphic != null && _minimapGraphic.GraphBound && _minimapGraphic.BaseRoadTriangleCount <= 0)
            {
                return "MAP GEOMETRY EMPTY";
            }

            return routeActive
                ? $"{FormatDistance(state.distanceRemainingMeters)} | {FormatEta(state.estimatedTimeRemainingSeconds)} | {ResolveCurrentRoadText()} {ResolveSpeedLimitText()}"
                : $"NO ROUTE | {ResolveCurrentRoadText()}";
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

            _registry.TryGet(out _navigationService);
            _registry.TryGet(out _roadGraphService);
            _registry.TryGet(out _originService);
            _registry.TryGet(out _streamingService);
            _registry.TryGet(out _inputService);
            _registry.TryGet(out _truckControlService);
            _registry.TryGet(out _dashboardService);
            _registry.TryGet(out _weatherService);
            _registry.TryGet(out _roadConditionService);
            _registry.TryGet(out _trafficService);
            _registry.TryGet(out _playerSettingsService);
            _registry.TryGet(out _wheelCalibrationService);
            _registry.TryGet(out _forceFeedbackService);
            _registry.TryGet(out _playerVehicleService);
        }

        private void UpdateNavigationPose()
        {
            if (_navigationService == null)
            {
                return;
            }

            _navigationService.UpdateVehiclePose(ResolvePlayerGlobalPosition().ToVector3(), ResolvePlayerForward(), Time.deltaTime);
        }

        private void SendTruckCommand(TruckCommandMutator mutator)
        {
            LwsTruckControlController controller = _truckControlService?.ActiveController ?? FindFirstObjectByType<LwsTruckControlController>();
            if (controller == null)
            {
                _lastActionMessage = "No active truck control controller.";
                return;
            }

            LwsVehicleCommandFrame frame = default;
            mutator(ref frame);
            LwsVehicleContinuousInput continuous = _inputService != null ? _inputService.ReadContinuousInput() : default;
            controller.ApplyCommandFrame(frame, continuous);
            _lastActionMessage = "Truck command sent through semantic LWS control frame.";
        }

        private void RequestAutomaticSelector(LwsAutomaticTransmissionSelector selector)
        {
            Lws18SpeedTransmissionController transmission = FindFirstObjectByType<Lws18SpeedTransmissionController>();
            if (transmission == null)
            {
                _lastActionMessage = "Transmission controller not found.";
                return;
            }

            transmission.TrySetAutomaticSelector(selector, out _lastActionMessage);
            RebuildActiveTab();
        }

        private void RequestTestRoute(bool forceFiftyMile)
        {
            LwsFiftyMileHighwayValidationController fifty = FindFirstObjectByType<LwsFiftyMileHighwayValidationController>();
            if (forceFiftyMile && fifty != null)
            {
                _lastActionMessage = fifty.RequestGpsRoute() ? "50-mile GPS route started." : fifty.LastError;
                return;
            }

            if (!forceFiftyMile && fifty != null)
            {
                _lastActionMessage = fifty.RequestGpsRoute() ? "50-mile GPS route started." : fifty.LastError;
                return;
            }

            if (_navigationService == null || _roadGraphService?.ActiveGraph == null)
            {
                _lastActionMessage = "Navigation service or active road graph is missing.";
                return;
            }

            Vector3 origin = ResolvePlayerGlobalPosition().ToVector3();
            if (!TryFindRouteDestination(_roadGraphService.ActiveGraph, origin, out Vector3 destination))
            {
                _lastActionMessage = "Active road graph has no valid route destination.";
                return;
            }

            LwsRouteResult result = _navigationService.SetDestination(destination, origin, _roadGraphService.ActiveGraph);
            _lastActionMessage = result != null && result.succeeded ? $"Test route started: {FormatDistance(result.distanceMeters)}." : result?.message ?? "Route failed.";
        }

        private void RecalculateRoute()
        {
            if (_navigationService == null)
            {
                _lastActionMessage = "Navigation service is missing.";
                return;
            }

            LwsRouteResult result = _navigationService.RecalculateRoute(ResolvePlayerGlobalPosition().ToVector3());
            _lastActionMessage = result != null && result.succeeded ? "Route recalculated." : result?.message ?? "Route recalculation failed.";
        }

        private static bool TryFindRouteDestination(LwsRoadGraph graph, Vector3 origin, out Vector3 destination)
        {
            destination = default;
            float bestSqr = -1f;
            if (graph?.nodes != null)
            {
                foreach (LwsRoadNode node in graph.nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    float sqr = (node.position - origin).sqrMagnitude;
                    if (sqr > bestSqr)
                    {
                        bestSqr = sqr;
                        destination = node.position;
                    }
                }
            }

            if (graph?.edges != null)
            {
                foreach (LwsRoadEdge edge in graph.edges)
                {
                    if (edge?.samples == null)
                    {
                        continue;
                    }

                    foreach (LwsRoadSample sample in edge.samples)
                    {
                        if (sample == null)
                        {
                            continue;
                        }

                        float sqr = (sample.position - origin).sqrMagnitude;
                        if (sqr > bestSqr)
                        {
                            bestSqr = sqr;
                            destination = sample.position;
                        }
                    }
                }
            }

            return bestSqr > 1f;
        }

        private void RequestWeather(string presetId)
        {
            LwsServiceResult result = _weatherService != null
                ? _weatherService.RequestWeather(presetId, 8f, false)
                : LwsServiceResult.Failure("Weather service is missing.");
            _lastActionMessage = result.Message;
        }

        private void SetWeatherTime(float hours)
        {
            if (_weatherService == null)
            {
                _lastActionMessage = "Weather service is missing.";
                return;
            }

            _weatherService.SetTimeOfDayHours(hours);
            _lastActionMessage = $"Weather time set to {hours:0.0}h.";
        }

        private void TeleportFifty(LwsFiftyMileHighwayValidationController fifty, double mile)
        {
            _lastActionMessage = fifty.TeleportToMile(mile) ? $"Teleported to Mile {mile:0.##}." : fifty.LastError;
        }

        private void CenterFullMapOnPlayer()
        {
            _bigMapPanMeters = Vector2.zero;
            RefreshMaps();
        }

        private void CenterFullMapOnRoute()
        {
            LwsRouteResult route = _navigationService?.CurrentRoute;
            if (route?.waypoints == null || route.waypoints.Count == 0)
            {
                _bigMapPanMeters = Vector2.zero;
                _lastActionMessage = "No active route to center.";
                return;
            }

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < route.waypoints.Count; i++)
            {
                sum += route.waypoints[i];
            }

            Vector3 center = sum / route.waypoints.Count;
            LwsWorldPositionD player = ResolvePlayerGlobalPosition();
            _bigMapPanMeters = new Vector2(center.x - (float)player.x, center.z - (float)player.z);
            _lastActionMessage = "Full map centered on active route.";
            RefreshMaps();
        }

        private void SetBigMapZoom(float metersVisible)
        {
            _bigMapMetersVisible = Mathf.Clamp(metersVisible, 250f, 80000f);
            RefreshMaps();
        }

        private void PanBigMap(Vector2 deltaMeters)
        {
            _bigMapPanMeters += deltaMeters;
            RefreshMaps();
        }

        private LwsWorldPositionD ResolvePlayerGlobalPosition()
        {
            Vector3 local = ResolvePlayerLocalPosition();
            return _originService != null ? _originService.LocalToGlobal(local) : LwsWorldPositionD.FromVector3(local);
        }

        private Vector3 ResolvePlayerLocalPosition()
        {
            LwsPlayerTruck truck = _playerVehicleService?.ActiveTruck ?? FindFirstObjectByType<LwsPlayerTruck>();
            if (truck != null)
            {
                return truck.transform.position;
            }

            return _originService != null ? _originService.PlayerLocalPosition : Vector3.zero;
        }

        private Vector3 ResolvePlayerForward()
        {
            LwsPlayerTruck truck = _playerVehicleService?.ActiveTruck ?? FindFirstObjectByType<LwsPlayerTruck>();
            if (truck != null)
            {
                return truck.transform.forward;
            }

            Vector3 forward = _navigationService?.RuntimeState?.playerForward ?? Vector3.forward;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private string ResolveSpeedLimitText()
        {
            return _cachedSpeedLimitText;
        }

        private string ResolveCurrentRoadText()
        {
            return string.IsNullOrWhiteSpace(_cachedRoadDisplayName) ? "ROAD: UNKNOWN" : _cachedRoadDisplayName;
        }

        private void RefreshRoadLookupCache(Vector3 playerGlobal)
        {
            if (Time.unscaledTime < _nextRoadLookupTime)
            {
                return;
            }

            _nextRoadLookupTime = Time.unscaledTime + RoadLookupRefreshSeconds;
            string navigationRoadName = _navigationService?.RuntimeState?.currentRoadDisplayName;
            _cachedRoadDisplayName = !string.IsNullOrWhiteSpace(navigationRoadName) ? $"ROAD: {navigationRoadName}" : "ROAD: UNKNOWN";
            _cachedSpeedLimitText = string.Empty;

            if (_roadGraphService != null && _roadGraphService.TryFindNearestRoad(playerGlobal, 120f, out LwsRoadLookupResult road) && road.Found)
            {
                string roadName = LwsRoadDisplayNames.GetRoadDisplayName(road.RoadId, road.SegmentId);
                _cachedRoadDisplayName = string.IsNullOrWhiteSpace(roadName) ? "ROAD: UNKNOWN" : $"ROAD: {roadName}";
                _cachedSpeedLimitText = road.SpeedLimitMph > 0f ? $"{road.SpeedLimitMph:0} mph" : string.Empty;
            }
        }

        private void HandleKeyboardShortcuts()
        {
            if (WasKeyPressed(KeyCode.F1))
            {
                ToggleControlCenter();
            }

            if (WasKeyPressed(KeyCode.M))
            {
                ToggleBigMap();
            }

            if (WasKeyPressed(KeyCode.Escape))
            {
                if (ControlCenterVisible)
                {
                    HideControlCenter();
                }
                else if (BigMapVisible)
                {
                    HideBigMap();
                }
            }

            if (ControlCenterVisible && _activeTab == LwsDevelopmentUiTab.Transmission)
            {
                if (WasKeyPressed(KeyCode.Alpha1))
                {
                    RequestAutomaticSelector(LwsAutomaticTransmissionSelector.Drive);
                }
                else if (WasKeyPressed(KeyCode.Alpha2))
                {
                    RequestAutomaticSelector(LwsAutomaticTransmissionSelector.Neutral);
                }
                else if (WasKeyPressed(KeyCode.Alpha3))
                {
                    RequestAutomaticSelector(LwsAutomaticTransmissionSelector.Reverse);
                }
            }
        }

        private static bool WasKeyPressed(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.F1: return keyboard.f1Key.wasPressedThisFrame;
                case KeyCode.M: return keyboard.mKey.wasPressedThisFrame;
                case KeyCode.Escape: return keyboard.escapeKey.wasPressedThisFrame;
                case KeyCode.Alpha1: return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
                case KeyCode.Alpha2: return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
                case KeyCode.Alpha3: return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(keyCode);
#endif
        }

        private void CaptureCursor()
        {
            if (!_cursorCaptured)
            {
                _previousCursorVisible = Cursor.visible;
                _previousCursorLock = Cursor.lockState;
                _cursorCaptured = true;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void RestoreCursorIfClear()
        {
            if (!_cursorCaptured || ControlCenterVisible || BigMapVisible)
            {
                return;
            }

            Cursor.visible = _previousCursorVisible;
            Cursor.lockState = _previousCursorLock;
            _cursorCaptured = false;
        }

        private void PauseForBigMap()
        {
            if (_bigMapPausedTime)
            {
                return;
            }

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _bigMapPausedTime = true;
        }

        private void RestoreTimeScaleIfPaused()
        {
            if (!_bigMapPausedTime)
            {
                return;
            }

            Time.timeScale = _previousTimeScale;
            _bigMapPausedTime = false;
        }

        private void UpdateFps()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
            {
                return;
            }

            float fps = 1f / dt;
            _fpsSmoothed = _fpsSmoothed <= 0f ? fps : Mathf.Lerp(_fpsSmoothed, fps, 0.08f);
        }

        private void AddHeader(string text)
        {
            Text label = CreateText(_contentRoot, $"Header {text}", text.ToUpperInvariant(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.color = TextColor;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        }

        private void AddInfo(string label, string value)
        {
            Text text = CreateText(_contentRoot, $"Info {label}", $"{label}: {value}", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 13, FontStyle.Normal, TextAnchor.MiddleLeft);
            text.color = MutedTextColor;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        }

        private void AddTransmissionSelectorRow(Lws18SpeedTransmissionController transmission)
        {
            RectTransform row = CreateRect(_contentRoot, "Automatic Selector Row", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            bool enabled = transmission != null && transmission.DevelopmentAutomaticModeActive;
            AddTransmissionSelectorButton(row, "D", LwsAutomaticTransmissionSelector.Drive, transmission?.AutomaticSelector ?? LwsAutomaticTransmissionSelector.Drive, enabled);
            AddTransmissionSelectorButton(row, "N", LwsAutomaticTransmissionSelector.Neutral, transmission?.AutomaticSelector ?? LwsAutomaticTransmissionSelector.Drive, enabled);
            AddTransmissionSelectorButton(row, "R", LwsAutomaticTransmissionSelector.Reverse, transmission?.AutomaticSelector ?? LwsAutomaticTransmissionSelector.Drive, enabled);
        }

        private void AddTransmissionSelectorButton(
            Transform parent,
            string label,
            LwsAutomaticTransmissionSelector selector,
            LwsAutomaticTransmissionSelector activeSelector,
            bool enabled)
        {
            bool selected = enabled && selector == activeSelector;
            Button button = CreateButton(
                parent,
                $"Automatic Selector {label}",
                label,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                () => RequestAutomaticSelector(selector));

            button.interactable = enabled;
            Color normal = selected ? SelectedButtonColor : enabled ? ButtonColor : DisabledButtonColor;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = normal;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = selected ? new Color(0.16f, 0.44f, 0.31f, 1f) : ButtonHoverColor;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = DisabledButtonColor;
            button.colors = colors;
        }

        private void AddButtonRow(params (string label, Action action)[] buttons)
        {
            RectTransform row = CreateRect(_contentRoot, "Button Row", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            foreach ((string label, Action action) in buttons)
            {
                CreateButton(row, $"Button {label}", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, () =>
                {
                    action?.Invoke();
                    RebuildActiveTab();
                });
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect.gameObject;
        }

        private static GameObject CreateFixedPanel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, Color color)
        {
            RectTransform rect = CreateFixedRect(parent, name, anchor, pivot, size, anchoredPosition);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect.gameObject;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static RectTransform CreateFixedRect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return rect;
        }

        private static LwsSemanticGpsMapGraphic CreateSemanticMapGraphic(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            if (rect.GetComponent<CanvasRenderer>() == null)
            {
                rect.gameObject.AddComponent<CanvasRenderer>();
            }

            LwsSemanticGpsMapGraphic graphic = rect.GetComponent<LwsSemanticGpsMapGraphic>();
            return graphic != null ? graphic : rect.gameObject.AddComponent<LwsSemanticGpsMapGraphic>();
        }

        private static Text CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int size, FontStyle style, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = TextColor;
            label.text = text ?? string.Empty;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Action onClick)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHoverColor;
            colors.pressedColor = new Color(0.08f, 0.13f, 0.14f, 1f);
            colors.selectedColor = ButtonHoverColor;
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());

            Text text = CreateText(rect, "Label", label, Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-4f, -2f), 12, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return button;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static string FormatDistance(float meters)
        {
            if (meters >= 1609.344f)
            {
                return $"{meters / 1609.344f:0.0} mi";
            }

            return $"{meters:0} m";
        }

        private static string FormatTransmissionOverview(Lws18SpeedTransmissionController transmission)
        {
            if (transmission == null)
            {
                return "missing";
            }

            LwsTransmissionDisplayState state = transmission.DisplayState;
            if (state.mode == LwsTransmissionMode.Automatic)
            {
                return $"AUTO {FormatAutomaticSelectorShort(state.automaticSelector)}";
            }

            return "18-SPEED MANUAL";
        }

        private static string FormatAutomaticSelectorShort(LwsAutomaticTransmissionSelector selector)
        {
            switch (selector)
            {
                case LwsAutomaticTransmissionSelector.Drive:
                    return "D";
                case LwsAutomaticTransmissionSelector.Neutral:
                    return "N";
                case LwsAutomaticTransmissionSelector.Reverse:
                    return "R";
                default:
                    return "D";
            }
        }

        private static string FormatSignedSpeed(float metersPerSecond)
        {
            return $"{metersPerSecond * 2.23693629f:0.0} mph ({metersPerSecond:0.00} m/s)";
        }

        private static string FormatEta(float seconds)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                return "--";
            }

            int minutes = Mathf.CeilToInt(seconds / 60f);
            return minutes >= 60 ? $"{minutes / 60}h {minutes % 60:00}m" : $"{minutes}m";
        }

    }
}
