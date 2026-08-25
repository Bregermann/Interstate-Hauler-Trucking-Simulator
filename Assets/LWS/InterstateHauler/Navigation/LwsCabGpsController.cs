using UnityEngine;
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(180)]
    [DisallowMultipleComponent]
    public sealed class LwsCabGpsController : MonoBehaviour, ILwsNavigationRoutePresenter
    {
        public const string DefaultGpsAnchorId = "IH_CabAnchor_GpsMount";

        [SerializeField] private LwsGpsVoicePack voicePack;
        [SerializeField] private bool createPhysicalScreen = true;
        [SerializeField] private string gpsAnchorId = DefaultGpsAnchorId;
        [SerializeField] private Vector3 localScreenPosition = Vector3.zero;
        [SerializeField] private Vector3 localScreenEulerAngles = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector2 screenSize = new Vector2(640f, 400f);
        [SerializeField] private float screenScale = 0.00042f;
        [SerializeField] private float mapMetersVisible = 2600f;
        [SerializeField, Range(0.2f, 0.8f)] private float playerViewportY = 0.38f;
        [SerializeField] private bool addGraphicRaycaster;
        [SerializeField] private float displayRefreshIntervalSeconds = 0.15f;
        [SerializeField] private Color dayPanelColor = new Color(0.02f, 0.04f, 0.045f, 1f);
        [SerializeField] private Color nightPanelColor = new Color(0.005f, 0.012f, 0.018f, 1f);

        private ILwsNavigationService _navigationService;
        private ILwsRoadGraphService _roadGraphService;
        private ILwsGpsVoiceGuidanceService _voiceService;
        private ILwsWeatherService _weatherService;
        private ILwsWorldOriginService _originService;
        private ILwsCameraPresentationService _cameraPresentationService;
        private Canvas _canvas;
        private Image _panelImage;
        private LwsSemanticGpsMapGraphic _mapGraphic;
        private Text _instructionText;
        private Text _distanceText;
        private Text _roadText;
        private LwsRouteResult _presentedRoute;
        private float _nextRefreshTime;
        private AudioSource _voiceAudioSource;
        private Transform _gpsMount;
        private bool _placementDiagnosticLogged;

        public string PresenterId => "lws.cab.gps";
        public string GpsAnchorId => gpsAnchorId;
        public bool PhysicalGpsBound => _canvas != null;
        public bool FallbackPhysicalScreenVisible => _canvas != null && _canvas.gameObject.activeSelf;
        public Canvas PhysicalCanvas => _canvas;
        public Transform GpsMount => _gpsMount;
        public LwsSemanticGpsMapGraphic SemanticMapGraphic => _mapGraphic;
        public bool RouteRendered => _presentedRoute != null && _presentedRoute.succeeded;
        public Vector3 LocalScreenPosition => localScreenPosition;
        public Vector3 LocalScreenEulerAngles => localScreenEulerAngles;
        public Vector2 ScreenSize => screenSize;
        public float ScreenScale => screenScale;
        public Vector2 ApproximatePhysicalSizeMeters => screenSize * screenScale;

        private void Start()
        {
            ResolveServices();
            if (createPhysicalScreen)
            {
                BindPhysicalScreen();
            }

            ConfigureVoice();
            _navigationService?.SetPresenter(this);
        }

        private void OnDestroy()
        {
            if (_navigationService != null)
            {
                _navigationService.SetPresenter(null);
            }

            _cameraPresentationService?.SetCabGpsActive(false);
        }

        private void Update()
        {
            if (_navigationService == null)
            {
                ResolveServices();
            }

            _cameraPresentationService?.SetCabGpsActive(_canvas != null && _canvas.gameObject.activeInHierarchy);
            _navigationService?.UpdateVehiclePose(ResolveGlobalPosition(), transform.forward, Time.deltaTime);
            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, displayRefreshIntervalSeconds);
            RefreshDisplay();
        }

        public bool PresentRoute(LwsRouteResult route)
        {
            _presentedRoute = route;
            RefreshDisplay();
            return _mapGraphic != null;
        }

        public void ClearRoute()
        {
            _presentedRoute = null;
            RefreshDisplay();
        }

        public void BindPhysicalScreen()
        {
            if (_canvas != null)
            {
                return;
            }

            Transform parent = ResolveGpsMount();
            var root = new GameObject("IH Physical Cab GPS Screen");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localScreenPosition;
            root.transform.localRotation = Quaternion.Euler(localScreenEulerAngles);
            root.transform.localScale = Vector3.one * screenScale;

            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.pixelPerfect = false;
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = screenSize;

            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            if (addGraphicRaycaster)
            {
                root.AddComponent<GraphicRaycaster>();
            }

            GameObject panel = CreateUiChild(root.transform, "Screen Panel", new Vector2(0f, 0f), screenSize);
            _panelImage = panel.AddComponent<Image>();
            _panelImage.color = dayPanelColor;

            GameObject map = CreateUiChild(panel.transform, "Map", new Vector2(0f, 28f), new Vector2(screenSize.x - 28f, screenSize.y - 96f));
            _mapGraphic = map.AddComponent<LwsSemanticGpsMapGraphic>();
            _mapGraphic.raycastTarget = false;

            _instructionText = CreateText(panel.transform, "Instruction", new Vector2(0f, -92f), new Vector2(screenSize.x - 24f, 34f), 24, TextAnchor.MiddleLeft);
            _distanceText = CreateText(panel.transform, "Distance", new Vector2(screenSize.x * 0.29f, -122f), new Vector2(screenSize.x * 0.38f, 28f), 20, TextAnchor.MiddleRight);
            _roadText = CreateText(panel.transform, "Road", new Vector2(-screenSize.x * 0.17f, -122f), new Vector2(screenSize.x * 0.56f, 28f), 18, TextAnchor.MiddleLeft);
            _cameraPresentationService?.SetCabGpsActive(true);
            LogPlacementDiagnostic(parent, root.transform);
            RefreshDisplay();
        }

        public void SetFallbackPhysicalScreenVisible(bool visible)
        {
            if (_canvas == null)
            {
                return;
            }

            _canvas.gameObject.SetActive(visible);
            _cameraPresentationService?.SetCabGpsActive(visible);
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _navigationService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _roadGraphService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _voiceService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _weatherService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _cameraPresentationService);
        }

        private void ConfigureVoice()
        {
            if (_voiceService == null)
            {
                return;
            }

            if (_voiceAudioSource == null)
            {
                _voiceAudioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            }

            if (voicePack == null)
            {
#if UNITY_EDITOR
                voicePack = UnityEditor.AssetDatabase.LoadAssetAtPath<LwsGpsVoicePack>(
                    "Assets/LWS/InterstateHauler/Navigation/Data/IH_GpsVoicePack_Default.asset");
#endif
            }

            if (voicePack == null)
            {
                voicePack = Resources.Load<LwsGpsVoicePack>("IH_GpsVoicePack_Default");
            }

            _voiceService.Configure(voicePack, _voiceAudioSource);
        }

        private void RefreshDisplay()
        {
            if (_canvas == null)
            {
                return;
            }

            LwsNavigationRuntimeState state = _navigationService != null ? _navigationService.RuntimeState : null;
            LwsRouteResult route = _navigationService != null && _navigationService.CurrentRoute != null ? _navigationService.CurrentRoute : _presentedRoute;
            bool active = state != null && state.routeActive && route != null && route.succeeded;
            if (_mapGraphic != null)
            {
                Vector3 globalPosition = ResolveGlobalPosition();
                _mapGraphic.SetMapData(
                    _roadGraphService?.ActiveGraph,
                    route,
                    LwsWorldPositionD.FromVector3(globalPosition),
                    transform.forward,
                    true,
                    mapMetersVisible,
                    Vector2.zero,
                    playerViewportY);
            }

            if (_instructionText != null)
            {
                _instructionText.text = active ? state.nextInstructionText : "No active route";
            }

            if (_distanceText != null)
            {
                _distanceText.text = active ? FormatDistance(state.distanceToNextManeuverMeters) : "--";
            }

            if (_roadText != null)
            {
                _roadText.text = active ? $"{state.currentRoadDisplayName} | {FormatDistance(state.distanceRemainingMeters)} left" : "GPS ready";
            }

            ApplyWeatherTheme();
        }

        private Transform ResolveGpsMount()
        {
            LwsCabAccessoryAnchorRegistry anchors = GetComponent<LwsCabAccessoryAnchorRegistry>();
            if (anchors != null && anchors.TryGetAnchor(gpsAnchorId, out LwsCabAccessoryAnchor anchor))
            {
                _gpsMount = anchor.transform;
                return _gpsMount;
            }

            _gpsMount = FindChildRecursive(transform, "Cab") ?? transform;
            return _gpsMount;
        }

        private void ApplyWeatherTheme()
        {
            if (_panelImage == null)
            {
                return;
            }

            float daylight = _weatherService != null ? _weatherService.CurrentSnapshot.Daylight01 : 1f;
            _panelImage.color = Color.Lerp(nightPanelColor, dayPanelColor, Mathf.Clamp01(daylight));
        }

        private Vector3 ResolveGlobalPosition()
        {
            return _originService != null
                ? _originService.LocalToGlobal(transform.position).ToVector3()
                : transform.position;
        }

        private static string FormatDistance(float meters)
        {
            if (meters >= 1609.344f)
            {
                return $"{meters / 1609.344f:0.0} mi";
            }

            return $"{Mathf.Max(0f, meters) * 3.28084f:0} ft";
        }

        private static GameObject CreateUiChild(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return go;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor)
        {
            GameObject go = CreateUiChild(parent, name, anchoredPosition, size);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = new Color(0.82f, 0.96f, 1f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private void LogPlacementDiagnostic(Transform parent, Transform screen)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_placementDiagnosticLogged)
            {
                return;
            }

            _placementDiagnosticLogged = true;
            Vector2 physicalSize = ApproximatePhysicalSizeMeters;
            Debug.Log(
                $"[IH Cab GPS] Bound world-space GPS to {parent.name}; local pos {screen.localPosition}; local euler {screen.localEulerAngles}; scale {screenScale:0.00000}; approx size {physicalSize.x:0.00}m x {physicalSize.y:0.00}m.",
                this);
#endif
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform mount = _gpsMount != null ? _gpsMount : FindChildRecursive(transform, gpsAnchorId);
            if (mount == null)
            {
                return;
            }

            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.85f);
            Gizmos.matrix = Matrix4x4.TRS(
                mount.TransformPoint(localScreenPosition),
                mount.rotation * Quaternion.Euler(localScreenEulerAngles),
                Vector3.one * Mathf.Max(0.0001f, screenScale));
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(screenSize.x, screenSize.y, 1f));
            UnityEditor.Handles.Label(mount.TransformPoint(localScreenPosition), "IH_CabAnchor_GpsMount");
        }
#endif
    }
}
