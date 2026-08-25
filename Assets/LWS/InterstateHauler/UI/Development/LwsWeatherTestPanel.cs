using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    public readonly struct LwsWeatherTestWeatherButton
    {
        public LwsWeatherTestWeatherButton(string label, string presetId)
        {
            Label = label ?? string.Empty;
            PresetId = presetId ?? string.Empty;
        }

        public string Label { get; }
        public string PresetId { get; }
    }

    public readonly struct LwsWeatherTestTimeButton
    {
        public LwsWeatherTestTimeButton(string label, float hour)
        {
            Label = label ?? string.Empty;
            Hour = hour;
        }

        public string Label { get; }
        public float Hour { get; }
    }

    public readonly struct LwsWeatherTestRoadButton
    {
        public LwsWeatherTestRoadButton(string label, LwsRoadConditionOverrideMode mode)
        {
            Label = label ?? string.Empty;
            Mode = mode;
        }

        public string Label { get; }
        public LwsRoadConditionOverrideMode Mode { get; }
    }

    [DefaultExecutionOrder(725)]
    [DisallowMultipleComponent]
    public sealed class LwsWeatherTestPanel : MonoBehaviour
    {
        public const string CanvasObjectName = "F2 Weather Test Canvas";
        public const string PanelObjectName = "F2 Weather Test Panel";
        public const string ToggleHotkeyName = "F2";
        public const float ButtonPreferredHeight = 60f;
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private static readonly Color DimmerColor = new Color(0f, 0f, 0f, 0.52f);
        private static readonly Color PanelColor = new Color(0.028f, 0.034f, 0.034f, 0.98f);
        private static readonly Color SectionColor = new Color(0.045f, 0.058f, 0.057f, 0.96f);
        private static readonly Color StatusColor = new Color(0.035f, 0.044f, 0.044f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.11f, 0.18f, 0.19f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.16f, 0.27f, 0.28f, 1f);
        private static readonly Color ButtonPressedColor = new Color(0.08f, 0.13f, 0.14f, 1f);
        private static readonly Color TextColor = new Color(0.92f, 0.96f, 0.94f, 1f);
        private static readonly Color MutedTextColor = new Color(0.7f, 0.78f, 0.78f, 1f);
        private static readonly Color ValueTextColor = new Color(0.72f, 0.96f, 0.9f, 1f);
        private static Font _uiFont;

        private static readonly LwsWeatherTestWeatherButton[] WeatherButtons =
        {
            new LwsWeatherTestWeatherButton("CLEAR", LwsWeatherPresetCatalog.ClearId),
            new LwsWeatherTestWeatherButton("PARTLY CLOUDY", LwsWeatherPresetCatalog.PartlyCloudyId),
            new LwsWeatherTestWeatherButton("CLOUDY", LwsWeatherPresetCatalog.CloudyId),
            new LwsWeatherTestWeatherButton("OVERCAST", LwsWeatherPresetCatalog.OvercastId),
            new LwsWeatherTestWeatherButton("LIGHT RAIN", LwsWeatherPresetCatalog.LightRainId),
            new LwsWeatherTestWeatherButton("HEAVY RAIN", LwsWeatherPresetCatalog.HeavyRainId),
            new LwsWeatherTestWeatherButton("STORM", LwsWeatherPresetCatalog.ThunderstormId),
            new LwsWeatherTestWeatherButton("FOG", LwsWeatherPresetCatalog.FogId),
            new LwsWeatherTestWeatherButton("LIGHT SNOW", LwsWeatherPresetCatalog.LightSnowId),
            new LwsWeatherTestWeatherButton("HEAVY SNOW", LwsWeatherPresetCatalog.HeavySnowId)
        };

        private static readonly LwsWeatherTestTimeButton[] TimeButtons =
        {
            new LwsWeatherTestTimeButton("DAWN", 6f),
            new LwsWeatherTestTimeButton("MORNING", 9f),
            new LwsWeatherTestTimeButton("NOON", 12f),
            new LwsWeatherTestTimeButton("EVENING", 18f),
            new LwsWeatherTestTimeButton("DUSK", 20f),
            new LwsWeatherTestTimeButton("MIDNIGHT", 0f)
        };

        private static readonly LwsWeatherTestRoadButton[] RoadButtons =
        {
            new LwsWeatherTestRoadButton("DRY ROAD", LwsRoadConditionOverrideMode.ForceDry),
            new LwsWeatherTestRoadButton("WET ROAD", LwsRoadConditionOverrideMode.ForceWet),
            new LwsWeatherTestRoadButton("PUDDLED ROAD", LwsRoadConditionOverrideMode.ForceStandingWater),
            new LwsWeatherTestRoadButton("SNOWY ROAD", LwsRoadConditionOverrideMode.ForceSnow),
            new LwsWeatherTestRoadButton("ICY ROAD", LwsRoadConditionOverrideMode.ForceIce)
        };

        private readonly Dictionary<string, Text> _statusValues = new Dictionary<string, Text>(StringComparer.Ordinal);
        private readonly Dictionary<LwsKeyboardGamepadTruckInputSource, bool> _suppressedDrivingSources = new Dictionary<LwsKeyboardGamepadTruckInputSource, bool>();

        private LwsDevelopmentUiRoot _ownerRoot;
        private LwsServiceRegistry _registry;
        private ILwsWeatherService _weatherService;
        private ILwsGameClockService _gameClockService;
        private ILwsCameraPresentationService _cameraPresentationService;
        private ILwsRoadConditionService _roadConditionService;
        private Canvas _canvas;
        private CanvasScaler _canvasScaler;
        private GraphicRaycaster _graphicRaycaster;
        private GameObject _dimmer;
        private GameObject _panel;
        private RectTransform _panelRect;
        private Text _lastActionText;
        private bool _cursorCaptured;
        private bool _previousCursorVisible;
        private CursorLockMode _previousCursorLock;
        private float _nextStatusRefreshTime;
        private string _lastActionMessage = "Ready. Press F2 or CLOSE to hide this panel.";

        public static IReadOnlyList<LwsWeatherTestWeatherButton> WeatherButtonDefinitions => WeatherButtons;
        public static IReadOnlyList<LwsWeatherTestTimeButton> TimeButtonDefinitions => TimeButtons;
        public static IReadOnlyList<LwsWeatherTestRoadButton> RoadButtonDefinitions => RoadButtons;
        public bool Visible => _panel != null && _panel.activeSelf;
        public Canvas Canvas => _canvas;
        public CanvasScaler CanvasScaler => _canvasScaler;
        public GraphicRaycaster GraphicRaycaster => _graphicRaycaster;
        public RectTransform PanelRect => _panelRect;
        public string LastActionMessage => _lastActionMessage;

        public void Bind(LwsDevelopmentUiRoot ownerRoot, LwsServiceRegistry registry)
        {
            _ownerRoot = ownerRoot;
            _registry = registry;
            BuildUi();
            ResolveServices();
        }

        public void Show()
        {
            BuildUi();
            ResolveServices();
            if (Visible)
            {
                RefreshStatus();
                return;
            }

            _ownerRoot?.HideControlCenter();
            _ownerRoot?.HideBigMap();
            _dimmer.SetActive(true);
            _panel.SetActive(true);
            _dimmer.transform.SetAsLastSibling();
            _panel.transform.SetAsLastSibling();
            SuppressDrivingInputSources(true);
            CaptureCursor();
            RefreshStatus();
        }

        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            if (_dimmer != null)
            {
                _dimmer.SetActive(false);
            }

            SuppressDrivingInputSources(false);
            RestoreCursorIfClear();
        }

        public void Toggle()
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void OnDisable()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            if (_dimmer != null)
            {
                _dimmer.SetActive(false);
            }

            SuppressDrivingInputSources(false);
            RestoreCursorIfClear();
        }

        private void Update()
        {
            ResolveServices();
            if (WasF2Pressed())
            {
                Toggle();
            }

            if (!Visible)
            {
                return;
            }

            SuppressDrivingInputSources(true);
            if (Time.unscaledTime >= _nextStatusRefreshTime)
            {
                _nextStatusRefreshTime = Time.unscaledTime + 0.25f;
                RefreshStatus();
            }
        }

        private void BuildUi()
        {
            if (_canvas != null)
            {
                return;
            }

            RectTransform canvasRect = CreateRect(transform, CanvasObjectName, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _canvas = canvasRect.gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 6800;
            _canvasScaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = ReferenceResolution;
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0.5f;
            _graphicRaycaster = canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            _graphicRaycaster.enabled = true;

            _dimmer = CreatePanel(canvasRect, "F2 Weather Test Dimmer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, DimmerColor);
            _panel = CreatePanel(canvasRect, PanelObjectName, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero, PanelColor);
            _panelRect = _panel.GetComponent<RectTransform>();
            VerticalLayoutGroup panelLayout = _panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(34, 34, 26, 26);
            panelLayout.spacing = 16f;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandHeight = false;

            BuildHeader(_panel.transform);
            BuildBody(_panel.transform);
            BuildFooter(_panel.transform);
            _dimmer.SetActive(false);
            _panel.SetActive(false);
        }

        private void BuildHeader(Transform parent)
        {
            RectTransform header = CreateRect(parent, "Panel Header", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
            VerticalLayoutGroup layout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            CreateLayoutText(header, "Title Interstate Hauler", "INTERSTATE HAULER", 34, FontStyle.Bold, TextAnchor.MiddleCenter, 38f, TextColor);
            CreateLayoutText(header, "Title Weather Test Panel", "WEATHER TEST PANEL", 28, FontStyle.Bold, TextAnchor.MiddleCenter, 32f, MutedTextColor);
        }

        private void BuildBody(Transform parent)
        {
            RectTransform body = CreateRect(parent, "Panel Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            HorizontalLayoutGroup bodyLayout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 26f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandHeight = true;

            RectTransform controlsColumn = CreateSectionColumn(body, "Weather Time Road Controls", 3f);
            BuildWeatherSection(controlsColumn);
            BuildTimeSection(controlsColumn);
            BuildRoadSection(controlsColumn);

            RectTransform statusColumn = CreateSectionColumn(body, "Weather Road Status", 2f);
            BuildStatusSection(statusColumn);
        }

        private void BuildFooter(Transform parent)
        {
            RectTransform footer = CreateRect(parent, "Panel Footer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;
            HorizontalLayoutGroup layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;

            _lastActionText = CreateLayoutText(footer, "Last Action", _lastActionMessage, 18, FontStyle.Normal, TextAnchor.MiddleLeft, 62f, MutedTextColor);
            LayoutElement messageLayout = _lastActionText.gameObject.GetComponent<LayoutElement>();
            messageLayout.flexibleWidth = 1f;
            messageLayout.preferredWidth = 900f;

            Button close = CreateLayoutButton(footer, "Close F2 Weather Test Panel", "CLOSE", Hide, 220f);
            LayoutElement closeLayout = close.gameObject.GetComponent<LayoutElement>();
            closeLayout.preferredWidth = 220f;
            closeLayout.flexibleWidth = 0f;
        }

        private void BuildWeatherSection(RectTransform parent)
        {
            RectTransform content = CreateTitledSection(parent, "WEATHER", 386f);
            for (int i = 0; i < WeatherButtons.Length; i += 2)
            {
                RectTransform row = CreateButtonRow(content, $"Weather Row {i / 2}");
                int left = i;
                int right = i + 1;
                CreateLayoutButton(row, $"Weather {WeatherButtons[left].Label}", WeatherButtons[left].Label, () => RequestWeather(WeatherButtons[left].PresetId));
                CreateLayoutButton(row, $"Weather {WeatherButtons[right].Label}", WeatherButtons[right].Label, () => RequestWeather(WeatherButtons[right].PresetId));
            }
        }

        private void BuildTimeSection(RectTransform parent)
        {
            RectTransform content = CreateTitledSection(parent, "TIME OF DAY", 168f);
            for (int i = 0; i < TimeButtons.Length; i += 3)
            {
                RectTransform row = CreateButtonRow(content, $"Time Row {i / 3}");
                int first = i;
                int second = i + 1;
                int third = i + 2;
                CreateLayoutButton(row, $"Time {TimeButtons[first].Label}", TimeButtons[first].Label, () => SetTimeOfDay(TimeButtons[first].Hour));
                CreateLayoutButton(row, $"Time {TimeButtons[second].Label}", TimeButtons[second].Label, () => SetTimeOfDay(TimeButtons[second].Hour));
                CreateLayoutButton(row, $"Time {TimeButtons[third].Label}", TimeButtons[third].Label, () => SetTimeOfDay(TimeButtons[third].Hour));
            }
        }

        private void BuildRoadSection(RectTransform parent)
        {
            RectTransform content = CreateTitledSection(parent, "ROAD CONDITION TESTING", 102f);
            RectTransform row = CreateButtonRow(content, "Road Condition Row");
            for (int i = 0; i < RoadButtons.Length; i++)
            {
                LwsWeatherTestRoadButton definition = RoadButtons[i];
                CreateLayoutButton(row, $"Road {definition.Label}", definition.Label, () => ForceRoadCondition(definition.Mode));
            }
        }

        private void BuildStatusSection(RectTransform parent)
        {
            RectTransform content = CreateTitledSection(parent, "STATUS", 0f, true);
            AddStatusLine(content, "CurrentWeather", "CURRENT WEATHER");
            AddStatusLine(content, "CurrentTime", "CURRENT TIME");
            AddStatusLine(content, "RoadCondition", "ROAD CONDITION");
            AddStatusLine(content, "Camera", "CAMERA");
            AddStatusSpacer(content);
            AddStatusLine(content, "WeatherMaker", "WEATHER MAKER");
            AddStatusLine(content, "WeatherMakerProfile", "CURRENT PROFILE");
            AddStatusLine(content, "Precipitation", "PRECIPITATION");
            AddStatusLine(content, "Intensity", "INTENSITY");
            AddStatusSpacer(content);
            AddStatusLine(content, "Weatherade", "WEATHERADE");
            AddStatusLine(content, "RainCoverage", "RAIN COVERAGE");
            AddStatusLine(content, "RainWetness", "RAIN WETNESS");
            AddStatusLine(content, "Puddles", "PUDDLES");
            AddStatusLine(content, "SnowCoverage", "SNOW COVERAGE");
            AddStatusLine(content, "SnowAmount", "SNOW AMOUNT");
            AddStatusLine(content, "CompatibleRoads", "COMPATIBLE ROAD RENDERERS");
            AddStatusLine(content, "IncompatibleRoads", "INCOMPATIBLE ROAD RENDERERS");
            AddStatusLine(content, "CoverageUpdate", "LAST COVERAGE UPDATE");
        }

        private RectTransform CreateSectionColumn(RectTransform parent, string name, float flexibleWidth)
        {
            RectTransform column = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            LayoutElement layoutElement = column.gameObject.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = flexibleWidth;
            layoutElement.flexibleHeight = 1f;
            VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        private RectTransform CreateTitledSection(RectTransform parent, string title, float preferredHeight, bool flexibleHeight = false)
        {
            GameObject sectionObject = CreatePanel(parent, $"Section {title}", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, flexibleHeight ? StatusColor : SectionColor);
            RectTransform section = sectionObject.GetComponent<RectTransform>();
            LayoutElement sectionLayoutElement = sectionObject.AddComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                sectionLayoutElement.preferredHeight = preferredHeight;
            }

            sectionLayoutElement.flexibleHeight = flexibleHeight ? 1f : 0f;
            VerticalLayoutGroup layout = sectionObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 14);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            CreateLayoutText(section, $"Header {title}", title, 22, FontStyle.Bold, TextAnchor.MiddleLeft, 28f, TextColor);
            return section;
        }

        private static RectTransform CreateButtonRow(RectTransform parent, string name)
        {
            RectTransform row = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = ButtonPreferredHeight;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private void AddStatusLine(RectTransform parent, string key, string label)
        {
            RectTransform row = CreateRect(parent, $"Status {label}", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 29f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;

            Text labelText = CreateLayoutText(row, $"Label {label}", $"{label}:", 16, FontStyle.Bold, TextAnchor.MiddleLeft, 29f, MutedTextColor);
            LayoutElement labelLayout = labelText.gameObject.GetComponent<LayoutElement>();
            labelLayout.preferredWidth = 220f;
            labelLayout.flexibleWidth = 0f;

            Text valueText = CreateLayoutText(row, $"Value {label}", "--", 17, FontStyle.Bold, TextAnchor.MiddleLeft, 29f, ValueTextColor);
            valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            valueText.verticalOverflow = VerticalWrapMode.Truncate;
            _statusValues[key] = valueText;
        }

        private static void AddStatusSpacer(RectTransform parent)
        {
            RectTransform spacer = CreateRect(parent, "Status Spacer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 6f;
        }

        private static Button CreateLayoutButton(Transform parent, string name, string label, Action onClick, float preferredWidth = 0f)
        {
            RectTransform rect = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHoverColor;
            colors.selectedColor = ButtonHoverColor;
            colors.pressedColor = ButtonPressedColor;
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());

            Text text = CreateText(rect, "Label", label, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = ButtonPreferredHeight;
            if (preferredWidth > 0f)
            {
                layoutElement.preferredWidth = preferredWidth;
            }

            layoutElement.flexibleWidth = preferredWidth > 0f ? 0f : 1f;
            return button;
        }

        private static Text CreateLayoutText(Transform parent, string name, string text, int size, FontStyle style, TextAnchor alignment, float preferredHeight, Color color)
        {
            Text label = CreateText(parent, name, text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, size, style, alignment, color);
            LayoutElement layoutElement = label.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 1f;
            return label;
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

            _registry.TryGet(out _weatherService);
            _registry.TryGet(out _gameClockService);
            _registry.TryGet(out _cameraPresentationService);
            _registry.TryGet(out _roadConditionService);
        }

        private void RequestWeather(string presetId)
        {
            ResolveServices();
            LwsServiceResult result = _weatherService != null
                ? _weatherService.RequestWeather(presetId, 0f, true)
                : LwsServiceResult.Failure("Weather service is missing.");
            _lastActionMessage = result.Message;
            RefreshStatus();
        }

        private void SetTimeOfDay(float hours)
        {
            ResolveServices();
            if (_gameClockService != null)
            {
                _gameClockService.SetTimeOfDayHours(hours);
                _weatherService?.SetTimeOfDayHours(_gameClockService.CurrentSnapshot.timeOfDayHours);
                _lastActionMessage = $"Game clock set to {_gameClockService.CurrentSnapshot.ClockText}.";
            }
            else if (_weatherService != null)
            {
                _weatherService.SetTimeOfDayHours(hours);
                _lastActionMessage = $"Weather time set to {hours:0.0}h.";
            }
            else
            {
                _lastActionMessage = "Game clock and weather services are missing.";
            }

            RefreshStatus();
        }

        private void ForceRoadCondition(LwsRoadConditionOverrideMode mode)
        {
            ResolveServices();
            if (_roadConditionService == null)
            {
                _lastActionMessage = "Road condition service is missing.";
                RefreshStatus();
                return;
            }

            _roadConditionService.ForceCondition(mode);
            _lastActionMessage = $"Road condition forced to {FormatRoadMode(mode)}.";
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            ResolveServices();
            LwsWeatherSnapshot weather = _weatherService != null ? _weatherService.CurrentSnapshot : LwsWeatherSnapshot.Clear;
            LwsRoadConditionSnapshot road = _roadConditionService != null ? _roadConditionService.CurrentSnapshot : default;
            ILwsWeatherRuntimeAdapter weatherAdapter = _weatherService?.ActiveAdapter;
            ILwsWeatherRuntimeDiagnostics weatherDiagnostics = weatherAdapter as ILwsWeatherRuntimeDiagnostics;
            LwsWeatheradeAdapter weatherade = FindFirstObjectByType<LwsWeatheradeAdapter>();

            SetStatus("CurrentWeather", _weatherService != null ? $"{weather.condition} ({weather.weatherPresetId})" : "MISSING");
            SetStatus("CurrentTime", _gameClockService != null ? _gameClockService.CurrentSnapshot.ClockText : $"{weather.timeOfDayHours:00.00}h");
            SetStatus("RoadCondition", _roadConditionService != null ? $"{road.MajorGameplayState} / {road.condition} / {_roadConditionService.Mode}" : "MISSING");
            SetStatus("Camera", FormatCameraMode());
            SetStatus("WeatherMaker", ResolveWeatherMakerStatus(weatherAdapter, weatherDiagnostics));
            SetStatus("WeatherMakerProfile", weatherDiagnostics != null ? SafeStatus(weatherDiagnostics.LastResolvedWeatherMakerProfile) : "--");
            SetStatus("Precipitation", weatherDiagnostics != null ? SafeStatus(weatherDiagnostics.PrecipitationDiagnostic) : $"{weather.precipitationType}");
            SetStatus("Intensity", $"{weather.precipitationIntensity01:0.00}");
            SetStatus("Weatherade", weatherade != null ? (weatherade.IsAvailable ? "READY" : "NOT READY") : "NOT READY");
            SetStatus("RainCoverage", weatherade != null ? ResolveCoverageActivity(weatherade.RainCoverageDiagnostic) : "UNKNOWN");
            SetStatus("RainWetness", _roadConditionService != null ? $"{road.wetness01:0.00}" : "--");
            SetStatus("Puddles", _roadConditionService != null ? $"{road.standingWater01:0.00}" : "--");
            SetStatus("SnowCoverage", weatherade != null ? ResolveCoverageActivity(weatherade.SnowCoverageDiagnostic) : "UNKNOWN");
            SetStatus("SnowAmount", _roadConditionService != null ? $"{Mathf.Max(road.snowDepth01, road.packedSnow01):0.00}" : "--");
            SetStatus("CompatibleRoads", weatherade != null ? weatherade.BoundWeatheradeSurfaceCount.ToString() : "0");
            SetStatus("IncompatibleRoads", weatherade != null ? weatherade.IncompatibleSurfaceMaterialCount.ToString() : "0");
            SetStatus("CoverageUpdate", weatherade != null ? ResolveCoverageUpdateStatus(weatherade.LastVendorApplyDiagnostic) : "UNKNOWN");

            if (_lastActionText != null)
            {
                _lastActionText.text = _lastActionMessage;
            }
        }

        private void SetStatus(string key, string value)
        {
            if (_statusValues.TryGetValue(key, out Text text) && text != null)
            {
                text.text = value ?? "--";
            }
        }

        private void SuppressDrivingInputSources(bool suppress)
        {
            LwsKeyboardGamepadTruckInputSource[] sources = FindObjectsByType<LwsKeyboardGamepadTruckInputSource>(FindObjectsSortMode.None);
            if (suppress)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    LwsKeyboardGamepadTruckInputSource source = sources[i];
                    if (source == null || _suppressedDrivingSources.ContainsKey(source))
                    {
                        continue;
                    }

                    _suppressedDrivingSources[source] = source.DrivingInputSuppressed;
                    source.SetDrivingInputSuppressed(true);
                }

                return;
            }

            foreach (KeyValuePair<LwsKeyboardGamepadTruckInputSource, bool> pair in _suppressedDrivingSources)
            {
                if (pair.Key != null)
                {
                    pair.Key.SetDrivingInputSuppressed(pair.Value);
                }
            }

            _suppressedDrivingSources.Clear();
        }

        private void CaptureCursor()
        {
            if (_ownerRoot != null)
            {
                _ownerRoot.CaptureDevelopmentCursor();
                return;
            }

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
            if (_ownerRoot != null)
            {
                _ownerRoot.RestoreDevelopmentCursorIfClear();
                return;
            }

            if (!_cursorCaptured)
            {
                return;
            }

            Cursor.visible = _previousCursorVisible;
            Cursor.lockState = _previousCursorLock;
            _cursorCaptured = false;
        }

        public static string ResolveCoverageActivity(string diagnostic)
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
            {
                return "UNKNOWN";
            }

            return diagnostic.IndexOf("inactive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   diagnostic.IndexOf("not applied", StringComparison.OrdinalIgnoreCase) >= 0
                ? "INACTIVE"
                : "ACTIVE";
        }

        public static string ResolveCoverageUpdateStatus(string diagnostic)
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
            {
                return "UNKNOWN";
            }

            if (diagnostic.IndexOf("invoked", StringComparison.OrdinalIgnoreCase) >= 0 ||
                diagnostic.IndexOf("applied", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "SUCCESS";
            }

            if (diagnostic.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                diagnostic.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "FAILURE";
            }

            return "UNKNOWN";
        }

        private static string ResolveWeatherMakerStatus(ILwsWeatherRuntimeAdapter adapter, ILwsWeatherRuntimeDiagnostics diagnostics)
        {
            if (diagnostics != null)
            {
                return diagnostics.WeatherMakerRuntimeExists && diagnostics.WeatherMakerInstanceResolved ? "READY" : "NOT READY";
            }

            if (adapter != null)
            {
                return adapter.WeatherMakerAvailable ? "READY" : "NOT READY";
            }

            return "NOT READY";
        }

        private string FormatCameraMode()
        {
            if (_cameraPresentationService == null)
            {
                return "UNKNOWN";
            }

            return string.IsNullOrWhiteSpace(_cameraPresentationService.CurrentCameraName)
                ? _cameraPresentationService.CurrentMode.ToString()
                : $"{_cameraPresentationService.CurrentMode} / {_cameraPresentationService.CurrentCameraName}";
        }

        private static string FormatRoadMode(LwsRoadConditionOverrideMode mode)
        {
            switch (mode)
            {
                case LwsRoadConditionOverrideMode.ForceDry:
                    return "Dry Road";
                case LwsRoadConditionOverrideMode.ForceWet:
                    return "Wet Road";
                case LwsRoadConditionOverrideMode.ForceStandingWater:
                    return "Puddled Road";
                case LwsRoadConditionOverrideMode.ForceSnow:
                    return "Snowy Road";
                case LwsRoadConditionOverrideMode.ForceIce:
                    return "Icy Road";
                default:
                    return mode.ToString();
            }
        }

        private static string SafeStatus(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "--" : value;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
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

        private static Text CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Text label = rect.gameObject.AddComponent<Text>();
            Font font = ResolveUiFont();
            if (font != null)
            {
                label.font = font;
            }

            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = color;
            label.text = text ?? string.Empty;
            label.raycastTarget = false;
            return label;
        }

        private static Font ResolveUiFont()
        {
            if (_uiFont != null)
            {
                return _uiFont;
            }

            try
            {
                _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (Exception ex)
            {
                LwsDevelopmentUiDiagnostics.LogFailure("F2 Weather Test Panel font resolve", ex);
            }

            return _uiFont;
        }

        public static bool WasF2Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.f2Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F2);
#endif
        }
    }
}

