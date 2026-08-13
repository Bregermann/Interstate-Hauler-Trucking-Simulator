using UnityEngine;
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(180)]
    [DisallowMultipleComponent]
    public sealed class LwsCabGpsController : MonoBehaviour, ILwsNavigationRoutePresenter
    {
        [SerializeField] private LwsGpsVoicePack voicePack;
        [SerializeField] private bool createPhysicalScreen = true;
        [SerializeField] private Vector3 localScreenPosition = new Vector3(0.24f, 0.48f, 0.95f);
        [SerializeField] private Vector3 localScreenEulerAngles = new Vector3(62f, -8f, 0f);
        [SerializeField] private Vector2 screenSize = new Vector2(460f, 270f);
        [SerializeField] private float screenScale = 0.00105f;
        [SerializeField] private float displayRefreshIntervalSeconds = 0.15f;

        private ILwsNavigationService _navigationService;
        private ILwsGpsVoiceGuidanceService _voiceService;
        private Canvas _canvas;
        private LwsGpsMapGraphic _mapGraphic;
        private Text _instructionText;
        private Text _distanceText;
        private Text _roadText;
        private LwsRouteResult _presentedRoute;
        private float _nextRefreshTime;
        private AudioSource _voiceAudioSource;

        public string PresenterId => "lws.cab.gps";
        public bool PhysicalGpsBound => _canvas != null;
        public bool RouteRendered => _presentedRoute != null && _presentedRoute.succeeded;

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
        }

        private void Update()
        {
            if (_navigationService == null)
            {
                ResolveServices();
            }

            _navigationService?.UpdateVehiclePose(transform.position, transform.forward, Time.deltaTime);
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

            Transform parent = FindChildRecursive(transform, "Cab") ?? transform;
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
            root.AddComponent<GraphicRaycaster>();

            GameObject panel = CreateUiChild(root.transform, "Screen Panel", new Vector2(0f, 0f), screenSize);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.04f, 0.045f, 1f);

            GameObject map = CreateUiChild(panel.transform, "Map", new Vector2(0f, 28f), new Vector2(screenSize.x - 28f, screenSize.y - 96f));
            _mapGraphic = map.AddComponent<LwsGpsMapGraphic>();
            _mapGraphic.raycastTarget = false;

            _instructionText = CreateText(panel.transform, "Instruction", new Vector2(0f, -92f), new Vector2(screenSize.x - 24f, 34f), 24, TextAnchor.MiddleLeft);
            _distanceText = CreateText(panel.transform, "Distance", new Vector2(screenSize.x * 0.29f, -122f), new Vector2(screenSize.x * 0.38f, 28f), 20, TextAnchor.MiddleRight);
            _roadText = CreateText(panel.transform, "Road", new Vector2(-screenSize.x * 0.17f, -122f), new Vector2(screenSize.x * 0.56f, 28f), 18, TextAnchor.MiddleLeft);
            RefreshDisplay();
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _navigationService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _voiceService);
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
            bool active = state != null && state.routeActive && _presentedRoute != null && _presentedRoute.succeeded;
            if (_mapGraphic != null)
            {
                _mapGraphic.SetRoute(active ? _presentedRoute.waypoints : null, transform.position, transform.forward);
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = new Color(0.82f, 0.96f, 1f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
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
    }
}
