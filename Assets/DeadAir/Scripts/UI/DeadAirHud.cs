using System.Text;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir
{
    [DefaultExecutionOrder(500)]
    [DisallowMultipleComponent]
    public sealed class DeadAirHud : MonoBehaviour
    {
        [SerializeField] private bool buildRuntimeUi = true;
        [SerializeField] private bool showDebugOverlay = true;

        private Text _speedText;
        private Text _gpsText;
        private Text _subtitleText;
        private Text _debugText;
        private LwsSemanticGpsMapGraphic _mapGraphic;
        private DeadAirGameManager _manager;
        private DeadAirAudioDirector _audioDirector;
        private DeadAirGPSDirector _gpsDirector;
        private DeadAirDashboardMisinformationDirector _dashboardDirector;
        private DeadAirOffRoadFailureController _offRoadFailureController;
        private ILwsNavigationService _navigationService;
        private ILwsRoadGraphService _roadGraphService;
        private readonly StringBuilder _builder = new StringBuilder(512);

        private void Awake()
        {
            if (buildRuntimeUi)
            {
                BuildUi();
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            ResolveReferences();
            DeadAirVehicleSnapshot vehicle = _manager != null && _manager.VehicleAdapter != null
                ? _manager.VehicleAdapter.CurrentSnapshot
                : default;

            if (_speedText != null)
            {
                DeadAirDashboardMisinformationDirector.DashboardState dashboard = _dashboardDirector != null
                    ? _dashboardDirector.CurrentState
                    : default;
                string speedText = dashboard.active && !string.IsNullOrWhiteSpace(dashboard.overrideSpeedText)
                    ? dashboard.overrideSpeedText
                    : $"{vehicle.speedMph:0} MPH";
                string gearText = dashboard.active && !string.IsNullOrWhiteSpace(dashboard.overrideGearText)
                    ? dashboard.overrideGearText
                    : vehicle.transmissionMode;
                _speedText.text = vehicle.available
                    ? $"{speedText}  |  {gearText}"
                    : "TRUCK OFFLINE";
            }

            if (_gpsText != null && _gpsDirector != null)
            {
                DeadAirGpsState gps = _gpsDirector.CurrentState;
                _gpsText.text = gps.presentationMode == DeadAirGpsPresentationMode.SignalLost
                    ? "GPS SIGNAL LOST"
                    : $"{FormatArrow(gps.arrow)} {gps.instructionText}\n{FormatDistance(gps.distanceToInstructionMeters)}  |  {gps.currentRoad}";
            }

            UpdateSemanticMap(vehicle);

            if (_debugText != null)
            {
                _debugText.gameObject.SetActive(showDebugOverlay);
                if (showDebugOverlay)
                {
                    BuildDebugText(vehicle);
                }
            }
        }

        private void BuildDebugText(DeadAirVehicleSnapshot vehicle)
        {
            _builder.Length = 0;
            _builder.AppendLine($"DEAD AIR | {(_manager != null ? _manager.State.ToString() : "NO MANAGER")}");
            _builder.AppendLine($"Speed: {vehicle.signedSpeedMph:0.0} mph");
            _builder.AppendLine($"Trailer: {(vehicle.trailerConnected ? "CONNECTED" : "PENDING")}");
            if (_manager != null && _manager.StoryDirector != null)
            {
                _builder.AppendLine($"Beat: {_manager.StoryDirector.CurrentBeatId}");
                _builder.AppendLine($"Triggered: {_manager.StoryDirector.TriggeredBeatCount}");
            }

            if (_manager != null && _manager.EndingDirector != null)
            {
                _builder.AppendLine($"Ending: {_manager.EndingDirector.CurrentEnding}");
            }

            if (_offRoadFailureController != null && _offRoadFailureController.ShowRuntimeDebug)
            {
                DeadAirRoadBoundaryEvaluation road = _offRoadFailureController.LastEvaluation;
                _builder.AppendLine($"Road: tractor {(road.tractorValid ? "VALID" : "OFF")} / trailer {(road.trailerValid ? "VALID" : "OFF")}");
                _builder.AppendLine($"Void: {(road.entireRigOffRoad ? "GRACE" : "CLEAR")} {road.graceTimerSeconds:0.00}/{road.graceDurationSeconds:0.00}s");
            }

            if (_dashboardDirector != null && _dashboardDirector.CurrentState.active)
            {
                _builder.AppendLine($"Dash: {_dashboardDirector.CurrentState.eventKind} {_dashboardDirector.CurrentState.warningLampId}");
                if (!string.IsNullOrWhiteSpace(_dashboardDirector.CurrentState.overrideFuelText))
                {
                    _builder.AppendLine($"Fuel Lie: {_dashboardDirector.CurrentState.overrideFuelText}");
                }

                if (!string.IsNullOrWhiteSpace(_dashboardDirector.CurrentState.overrideClockText))
                {
                    _builder.AppendLine($"Clock Lie: {_dashboardDirector.CurrentState.overrideClockText}");
                }
            }

            _debugText.text = _builder.ToString();
        }

        private void OnSubtitleChanged(string speaker, string subtitle, DeadAirAudioChannel channel)
        {
            if (_subtitleText == null)
            {
                return;
            }

            _subtitleText.text = string.IsNullOrWhiteSpace(subtitle) ? string.Empty : $"{speaker}: {subtitle}";
            _subtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(_subtitleText.text));
        }

        private void OnSubtitleCleared()
        {
            if (_subtitleText != null)
            {
                _subtitleText.text = string.Empty;
                _subtitleText.gameObject.SetActive(false);
            }
        }

        private void BuildUi()
        {
            if (GetComponentInChildren<Canvas>() != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform canvasRect = CreateRect(transform, "Dead Air Canvas", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 7200;
            CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            _speedText = CreateText(canvasRect, "Speed", font, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 32f), new Vector2(420f, 70f), 24, TextAnchor.MiddleLeft);
            RectTransform mapPanel = CreateRect(canvasRect, "Dead Air Semantic GPS Panel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-344f, 32f), new Vector2(312f, 312f));
            Image mapPanelImage = mapPanel.gameObject.AddComponent<Image>();
            mapPanelImage.color = new Color(0.015f, 0.026f, 0.028f, 0.94f);
            mapPanel.gameObject.AddComponent<RectMask2D>();
            RectTransform mapRect = CreateRect(mapPanel, "Dead Air Semantic GPS Map", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = new Vector2(8f, 54f);
            mapRect.offsetMax = new Vector2(-8f, -8f);
            mapRect.gameObject.AddComponent<CanvasRenderer>();
            _mapGraphic = mapRect.gameObject.AddComponent<LwsSemanticGpsMapGraphic>();
            _mapGraphic.raycastTarget = false;
            _gpsText = CreateText(mapPanel, "Dead Air GPS", font, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -44f), new Vector2(292f, 42f), 13, TextAnchor.MiddleLeft);
            _subtitleText = CreateText(canvasRect, "Subtitles", font, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 138f), new Vector2(900f, 72f), 24, TextAnchor.MiddleCenter);
            _debugText = CreateText(canvasRect, "Debug", font, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-420f, -32f), new Vector2(390f, 180f), 14, TextAnchor.UpperLeft);
            _subtitleText.gameObject.SetActive(false);
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, Font font, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(parent, name, anchor, pivot, anchoredPosition, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void ResolveReferences()
        {
            if (_manager == null) _manager = DeadAirGameManager.Instance != null ? DeadAirGameManager.Instance : FindFirstObjectByType<DeadAirGameManager>();
            if (_audioDirector == null) _audioDirector = _manager != null ? _manager.AudioDirector : FindFirstObjectByType<DeadAirAudioDirector>();
            if (_gpsDirector == null) _gpsDirector = _manager != null ? _manager.GpsDirector : FindFirstObjectByType<DeadAirGPSDirector>();
            if (_dashboardDirector == null) _dashboardDirector = FindFirstObjectByType<DeadAirDashboardMisinformationDirector>();
            if (_offRoadFailureController == null) _offRoadFailureController = _manager != null ? _manager.OffRoadFailureController : FindFirstObjectByType<DeadAirOffRoadFailureController>();
            if (LwsApplicationBootstrap.Instance != null && LwsApplicationBootstrap.Instance.Registry != null)
            {
                if (_navigationService == null) LwsApplicationBootstrap.Instance.Registry.TryGet(out _navigationService);
                if (_roadGraphService == null) LwsApplicationBootstrap.Instance.Registry.TryGet(out _roadGraphService);
            }
        }

        private void Subscribe()
        {
            if (_audioDirector == null)
            {
                return;
            }

            _audioDirector.SubtitleChanged -= OnSubtitleChanged;
            _audioDirector.SubtitleCleared -= OnSubtitleCleared;
            _audioDirector.SubtitleChanged += OnSubtitleChanged;
            _audioDirector.SubtitleCleared += OnSubtitleCleared;
        }

        private void Unsubscribe()
        {
            if (_audioDirector == null)
            {
                return;
            }

            _audioDirector.SubtitleChanged -= OnSubtitleChanged;
            _audioDirector.SubtitleCleared -= OnSubtitleCleared;
        }

        private static string FormatDistance(float meters)
        {
            return meters >= 1609.344f ? $"{meters / 1609.344f:0.0} mi" : $"{meters:0} m";
        }

        private static string FormatArrow(DeadAirGpsArrow arrow)
        {
            return arrow == DeadAirGpsArrow.None ? "--" : arrow.ToString().ToUpperInvariant();
        }

        private void UpdateSemanticMap(DeadAirVehicleSnapshot vehicle)
        {
            if (_mapGraphic == null)
            {
                return;
            }

            LwsRoadGraph graph = _roadGraphService != null ? _roadGraphService.ActiveGraph : null;
            LwsRouteResult route = _navigationService != null ? _navigationService.CurrentRoute : null;
            Vector3 position = vehicle.available ? vehicle.position : Vector3.zero;
            Vector3 forward = vehicle.forward.sqrMagnitude > 0.0001f ? vehicle.forward : Vector3.forward;
            _mapGraphic.SetMapData(
                graph,
                route,
                LwsWorldPositionD.FromVector3(position),
                forward,
                true,
                2600f,
                Vector2.zero,
                0.42f);
        }
    }
}
