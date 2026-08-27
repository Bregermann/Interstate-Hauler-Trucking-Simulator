using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(185)]
    [DisallowMultipleComponent]
    public sealed class LwsCompassNavigatorProAdapter : MonoBehaviour, ILwsNavigationRoutePresenter
    {
        private const string CompassProductName = "Compass Navigator Pro 4";
        private const string CompassPackageVersion = "6.0.2";
        private const string CompassNamespace = "CompassNavigatorPro";
        private const string CompassControllerTypeName = "CompassNavigatorPro.CompassPro";
        private const string CompassPoiTypeName = "CompassNavigatorPro.CompassProPOI";
        private const string CompassNavMeshRouteTypeName = "CompassNavigatorPro.CompassProNavMeshRoute";
        private const string CompassPrefabResourcePath = "CNPro/Prefabs/CompassNavigatorPro";
        private const string CompassPoiPrefabResourcePath = "CNPro/Prefabs/CompassPOI";
        private const float RouteRefreshIntervalSeconds = 0.2f;
        private const float CabCompassMinimumVisibleRectPixels = 1f;
        private const float CabCompassFallbackVisibleElementSize = 100f;

        [SerializeField] private bool createHudPresentation = true;
        [SerializeField] private bool createCabPresentation = true;
        [SerializeField] private bool createDestinationPoi = true;
        [SerializeField] private bool createRoadPois = true;
        [SerializeField] private int maximumRoadPoiCount = 10;
        [SerializeField] private float hudMiniMapSize = 0.28f;
        [SerializeField] private float cabMiniMapSize = 0.34f;
        [SerializeField] private float miniMapCaptureMeters = 2600f;
        [SerializeField] private Color routeColor = new Color(0.0f, 0.72f, 1f, 1f);
        [SerializeField] private Color destinationColor = new Color(1f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color roadPoiColor = new Color(0.72f, 0.92f, 1f, 1f);

        private readonly List<Vector3> _convertedRoutePoints = new List<Vector3>();
        private readonly Dictionary<string, Component> _roadPois = new Dictionary<string, Component>(StringComparer.Ordinal);

        private ILwsNavigationService _navigationService;
        private ILwsRoadGraphService _roadGraphService;
        private ILwsWorldOriginService _originService;
        private ILwsCameraPresentationService _cameraPresentationService;
        private LwsCabGpsController _cabGpsController;
        private Type _compassType;
        private Type _poiType;
        private Type _navMeshRouteType;
        private GameObject _hudRoot;
        private GameObject _cabRoot;
        private GameObject _poiRoot;
        private Component _hudCompass;
        private Component _cabCompass;
        private Component _destinationPoi;
        private LwsRouteResult _lastRoute;
        private LwsRoadGraph _lastGraph;
        private long _lastRouteOriginVersion = long.MinValue;
        private long _lastGraphOriginVersion = long.MinValue;
        private int _lastRouteSignature;
        private int _lastGraphSignature;
        private float _nextRefreshTime;
        private bool _registeredAsPresenter;
        private bool _lastRequestedHudVisible = true;
        private string _lastError = string.Empty;
        private string _lastRouteSummary = "No route applied.";

        public string PresenterId => "lws.compass.navigator.pro4";
        public bool VendorDetected => _compassType != null && _poiType != null;
        public string ProductName => CompassProductName;
        public string PackageVersion => CompassPackageVersion;
        public string VendorPath => "Assets/Plugins/Kronnect/CompassNavigatorPro";
        public string VendorNamespace => CompassNamespace;
        public string MainVendorController => CompassControllerTypeName;
        public string VendorPrefabPath => "Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab";
        public string PoiSystem => CompassPoiTypeName;
        public bool NavMeshRouteHelperAvailable => _navMeshRouteType != null;
        public string NavMeshRouteStatus => NavMeshRouteHelperAvailable
            ? "Available for ordinary Unity NavMesh paths; not used as LWS truck route authority."
            : "Not detected.";
        public bool HudPresentationReady => _hudCompass != null;
        public bool CabPresentationReady => _cabCompass != null;
        public bool VendorRuntimeReady => HudPresentationReady || CabPresentationReady;
        public bool HudMinimapVisible { get; private set; }
        public bool CabGpsActive => CabPresentationReady && _cabRoot != null && _cabRoot.activeInHierarchy;
        public bool FullMapSupported => HasWritableProperty(_hudCompass, "miniMapFullScreenState") || HasWritableProperty(_cabCompass, "miniMapFullScreenState");
        public bool FullMapVisible => GetBoolProperty(_hudCompass, "miniMapFullScreenState");
        public bool VendorRoutePresentationActive { get; private set; }
        public int AppliedRoutePointCount { get; private set; }
        public int VendorPoiCount => _roadPois.Count + (_destinationPoi != null ? 1 : 0);
        public string LastError => _lastError;
        public string LastRouteSummary => _lastRouteSummary;
        public string VendorComponentsActuallyUsed => "CompassPro, CompassProPOI, CompassPro route/minimap APIs";

        private void Awake()
        {
            ResolveVendorApi();
        }

        private void Start()
        {
            ResolveServices();
            EnsureRuntime();
            RegisterPresenter();
            PresentRouteIfAvailable(true);
        }

        private void OnDestroy()
        {
            if (_registeredAsPresenter && _navigationService != null)
            {
                _navigationService.SetPresenter(null);
            }
        }

        private void Update()
        {
            ResolveServices();
            EnsureRuntime();
            RegisterPresenter();
            UpdateCompassTargets();
            ApplyCameraPresentationPolicy();

            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + RouteRefreshIntervalSeconds;
            PresentRouteIfAvailable(false);
            SyncRoadPoisIfNeeded();
        }

        public bool PresentRoute(LwsRouteResult route)
        {
            _lastRoute = route;
            return ApplyRoute(route);
        }

        public void ClearRoute()
        {
            _lastRoute = null;
            VendorRoutePresentationActive = false;
            AppliedRoutePointCount = 0;
            _lastRouteSummary = "Compass route cleared.";
            InvokeNoArgs(_hudCompass, "ClearRoute");
            InvokeNoArgs(_cabCompass, "ClearRoute");
            if (_destinationPoi != null)
            {
                Destroy(_destinationPoi.gameObject);
                _destinationPoi = null;
            }
        }

        public void SetHudMinimapVisible(bool visible)
        {
            _lastRequestedHudVisible = visible;
            if (_hudCompass == null)
            {
                HudMinimapVisible = false;
                return;
            }

            if (FullMapVisible)
            {
                HudMinimapVisible = false;
                return;
            }

            HudMinimapVisible = visible;
            SetProperty(_hudCompass, "showMiniMap", HudMinimapVisible);
        }

        public bool SetFullMapVisible(bool visible)
        {
            if (_hudCompass == null || !FullMapSupported)
            {
                return false;
            }

            SetProperty(_hudCompass, "showMiniMap", true);
            bool applied = SetProperty(_hudCompass, "miniMapFullScreenState", visible);
            if (applied && visible)
            {
                HudMinimapVisible = false;
            }
            else if (applied)
            {
                SetHudMinimapVisible(_lastRequestedHudVisible);
            }

            return applied;
        }

        public string BuildDiagnosticsSummary()
        {
            if (!VendorDetected)
            {
                return $"not detected / {_lastError}";
            }

            return $"{PackageVersion} / HUD {(HudPresentationReady ? "ready" : "missing")} / CAB {(CabPresentationReady ? "ready" : "missing")} / route {AppliedRoutePointCount} pts / POIs {VendorPoiCount}";
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsServiceRegistry registry = LwsApplicationBootstrap.Instance.Registry;
            registry.TryGet(out _navigationService);
            registry.TryGet(out _roadGraphService);
            registry.TryGet(out _originService);
            registry.TryGet(out _cameraPresentationService);
            if (_cabGpsController == null)
            {
                _cabGpsController = GetComponent<LwsCabGpsController>() ?? FindFirstObjectByType<LwsCabGpsController>();
            }
        }

        private void ResolveVendorApi()
        {
            _compassType = ResolveType(CompassControllerTypeName);
            _poiType = ResolveType(CompassPoiTypeName);
            _navMeshRouteType = ResolveType(CompassNavMeshRouteTypeName);
            _lastError = _compassType == null || _poiType == null ? "Compass Navigator Pro 4 runtime types were not found." : string.Empty;
        }

        private void EnsureRuntime()
        {
            if (!VendorDetected)
            {
                ResolveVendorApi();
                if (!VendorDetected)
                {
                    return;
                }
            }

            if (createHudPresentation && _hudCompass == null)
            {
                _hudRoot = CreateCompassInstance("IH Compass Navigator Pro HUD", null, false);
                _hudCompass = FindCompassComponent(_hudRoot);
                ConfigureCompass(_hudCompass, false);
                SetHudMinimapVisible(_cameraPresentationService == null || _cameraPresentationService.ShouldShowHudMinimap);
            }

            if (createCabPresentation && _cabCompass == null)
            {
                EnsureCabGpsController();
                if (_cabGpsController != null && _cabGpsController.GpsMount != null)
                {
                    Transform physicalScreen = _cabGpsController.PhysicalScreenTransform;
                    Transform cabParent = physicalScreen != null && physicalScreen.parent != null ? physicalScreen.parent : _cabGpsController.GpsMount;
                    Vector3 localPosition = physicalScreen != null ? physicalScreen.localPosition : _cabGpsController.LocalScreenPosition;
                    Quaternion localRotation = physicalScreen != null ? physicalScreen.localRotation : Quaternion.Euler(_cabGpsController.LocalScreenEulerAngles);
                    Vector3 localScale = physicalScreen != null ? physicalScreen.localScale : Vector3.one * _cabGpsController.ScreenScale;

                    DisableLegacyCabMapGraphic(_cabGpsController);
                    _cabRoot = CreateCompassInstance("IH Cab GPS Compass Navigator Pro", cabParent, true);
                    _cabRoot.transform.localPosition = localPosition;
                    _cabRoot.transform.localRotation = localRotation;
                    _cabRoot.transform.localScale = localScale;
                    RectTransform rect = _cabRoot.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.sizeDelta = _cabGpsController.ScreenSize;
                    }

                    _cabCompass = FindCompassComponent(_cabRoot);
                    ConfigureCompass(_cabCompass, true);
                    FitCabCompassToPhysicalScreen(_cabRoot, _cabGpsController.ScreenSize);
                    _cameraPresentationService?.SetCabGpsActive(true);
                }
            }

            EnsurePoiRoot();
            UpdateCompassTargets();
        }

        private void EnsureCabGpsController()
        {
            if (_cabGpsController == null)
            {
                _cabGpsController = GetComponent<LwsCabGpsController>() ?? FindFirstObjectByType<LwsCabGpsController>();
            }

            _cabGpsController?.BindPhysicalScreen();
        }

        private void RegisterPresenter()
        {
            if (_navigationService == null || _registeredAsPresenter)
            {
                return;
            }

            _navigationService.SetPresenter(this);
            _registeredAsPresenter = true;
        }

        private void PresentRouteIfAvailable(bool force)
        {
            LwsRouteResult route = _navigationService != null ? _navigationService.CurrentRoute : _lastRoute;
            if (route == null)
            {
                return;
            }

            int signature = BuildRouteSignature(route);
            long originVersion = _originService != null ? _originService.OriginVersion : 0;
            if (!force && ReferenceEquals(route, _lastRoute) && signature == _lastRouteSignature && originVersion == _lastRouteOriginVersion)
            {
                return;
            }

            _lastRoute = route;
            _lastRouteSignature = signature;
            _lastRouteOriginVersion = originVersion;
            ApplyRoute(route);
        }

        private bool ApplyRoute(LwsRouteResult route)
        {
            if (!VendorRuntimeReady)
            {
                _lastError = "Compass Navigator Pro runtime is not ready.";
                return false;
            }

            if (route == null || !route.succeeded || route.waypoints == null || route.waypoints.Count < 2)
            {
                ClearRoute();
                return false;
            }

            _convertedRoutePoints.Clear();
            for (int i = 0; i < route.waypoints.Count; i++)
            {
                _convertedRoutePoints.Add(GlobalToLocal(route.waypoints[i]));
            }

            bool hudApplied = InvokeSetRoute(_hudCompass, _convertedRoutePoints);
            bool cabApplied = InvokeSetRoute(_cabCompass, _convertedRoutePoints);
            VendorRoutePresentationActive = hudApplied || cabApplied;
            AppliedRoutePointCount = VendorRoutePresentationActive ? _convertedRoutePoints.Count : 0;
            if (VendorRoutePresentationActive)
            {
                _lastError = string.Empty;
                _lastRouteSummary = $"{AppliedRoutePointCount} LWS route points applied to Compass Navigator Pro.";
                SyncDestinationPoi(route);
            }
            else
            {
                _lastError = "CompassPro.SetRoute(IList<Vector3>) was not available.";
            }

            return VendorRoutePresentationActive;
        }

        private void SyncDestinationPoi(LwsRouteResult route)
        {
            if (!createDestinationPoi || route == null || route.waypoints == null || route.waypoints.Count == 0)
            {
                return;
            }

            Component poi = _destinationPoi;
            if (poi == null)
            {
                poi = CreatePoi("IH Compass Destination POI");
                _destinationPoi = poi;
            }

            if (poi == null)
            {
                return;
            }

            poi.transform.position = GlobalToLocal(route.waypoints[route.waypoints.Count - 1]);
            ConfigurePoi(poi, "lws.destination.active", string.IsNullOrWhiteSpace(route.destinationId) ? "Destination" : route.destinationId, destinationColor, 1.25f);
            RegisterPoiWithCompasses(poi);
        }

        private void SyncRoadPoisIfNeeded()
        {
            if (!createRoadPois || _roadGraphService == null || _roadGraphService.ActiveGraph == null)
            {
                return;
            }

            LwsRoadGraph graph = _roadGraphService.ActiveGraph;
            int signature = BuildGraphSignature(graph);
            long originVersion = _originService != null ? _originService.OriginVersion : 0;
            if (ReferenceEquals(graph, _lastGraph) && signature == _lastGraphSignature && originVersion == _lastGraphOriginVersion)
            {
                return;
            }

            _lastGraph = graph;
            _lastGraphSignature = signature;
            _lastGraphOriginVersion = originVersion;
            int created = 0;
            if (graph.edges == null)
            {
                return;
            }

            foreach (LwsRoadEdge edge in graph.edges)
            {
                if (edge == null || edge.samples == null || edge.samples.Count == 0 || created >= maximumRoadPoiCount)
                {
                    continue;
                }

                string id = string.IsNullOrWhiteSpace(edge.edgeId) ? $"road.poi.{created}" : $"lws.road.{edge.edgeId}";
                if (!_roadPois.TryGetValue(id, out Component poi) || poi == null)
                {
                    poi = CreatePoi($"IH Compass Road POI {created + 1:00}");
                    if (poi == null)
                    {
                        continue;
                    }

                    _roadPois[id] = poi;
                }

                LwsRoadSample sample = edge.samples[edge.samples.Count / 2];
                poi.transform.position = GlobalToLocal(sample.position);
                ConfigurePoi(poi, id, LwsRoadDisplayNames.GetRoadDisplayName(edge.roadId, edge.segmentId), roadPoiColor, 0.8f);
                RegisterPoiWithCompasses(poi);
                created++;
            }
        }

        private GameObject CreateCompassInstance(string name, Transform parent, bool worldSpace)
        {
            GameObject prefab = Resources.Load<GameObject>(CompassPrefabResourcePath);
            GameObject instance = prefab != null ? Instantiate(prefab) : new GameObject(name);
            instance.name = name;
            instance.transform.SetParent(parent != null ? parent : transform, false);

            Canvas canvas = instance.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = worldSpace ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = false;
                canvas.sortingOrder = worldSpace ? 80 : 6200;
            }

            CanvasScaler scaler = instance.GetComponent<CanvasScaler>();
            if (scaler != null && worldSpace)
            {
                scaler.dynamicPixelsPerUnit = 18f;
            }

            return instance;
        }

        private Component CreatePoi(string name)
        {
            EnsurePoiRoot();
            GameObject prefab = Resources.Load<GameObject>(CompassPoiPrefabResourcePath);
            GameObject poiObject = prefab != null ? Instantiate(prefab) : new GameObject(name);
            poiObject.name = name;
            poiObject.transform.SetParent(_poiRoot != null ? _poiRoot.transform : transform, false);
            Component poi = _poiType != null ? poiObject.GetComponent(_poiType) : null;
            if (poi == null && _poiType != null)
            {
                poi = poiObject.AddComponent(_poiType);
            }

            return poi;
        }

        private void EnsurePoiRoot()
        {
            if (_poiRoot != null)
            {
                return;
            }

            _poiRoot = new GameObject("IH Compass Navigator Pro POIs");
            _poiRoot.transform.SetParent(transform, false);
        }

        private Component FindCompassComponent(GameObject root)
        {
            return root != null && _compassType != null ? root.GetComponentInChildren(_compassType, true) : null;
        }

        private void ConfigureCompass(Component compass, bool cab)
        {
            if (compass == null)
            {
                return;
            }

            SetProperty(compass, "compassGroup", cab ? 2 : 1);
            SetProperty(compass, "cameraMain", ResolveCamera());
            SetProperty(compass, "follow", transform);
            SetProperty(compass, "viewportRect", new Rect(0f, 0f, 1f, 1f));
            SetProperty(compass, "showCompassBar", false);
            SetProperty(compass, "showMiniMap", true);
            SetEnumProperty(compass, "miniMapLocation", cab ? "MiddleCenter" : "BottomRight");
            SetEnumProperty(compass, "miniMapPositionAndSize", cab ? "UserDefined" : "ControlledByCompassNavigatorPro");
            SetEnumProperty(compass, "miniMapOrientation", "Follow");
            SetEnumProperty(compass, "miniMapStyle", "SciFi2");
            SetEnumProperty(compass, "miniMapContents", "TopDownWorldView");
            SetEnumProperty(compass, "miniMapCameraMode", "Orthographic");
            SetEnumProperty(compass, "miniMapCameraSnapshotFrequency", "Continuous");
            SetProperty(compass, "miniMapKeepStraight", false);
            SetProperty(compass, "miniMapSize", cab ? cabMiniMapSize : hudMiniMapSize);
            SetProperty(compass, "miniMapLocationOffset", cab ? Vector2.zero : new Vector2(-28f, 28f));
            SetProperty(compass, "miniMapCaptureSize", miniMapCaptureMeters);
            SetProperty(compass, "miniMapCameraHeightVSFollow", Mathf.Max(150f, miniMapCaptureMeters * 0.5f));
            SetProperty(compass, "miniMapCameraDepth", Mathf.Max(300f, miniMapCaptureMeters));
            SetProperty(compass, "miniMapShowMaximizeButton", !cab);
            SetProperty(compass, "miniMapShowZoomInOutButtons", !cab);
            SetProperty(compass, "miniMapShowPOIs", true);
            SetProperty(compass, "miniMapShowPlayerIcon", true);
            SetProperty(compass, "routeShowOnMiniMap", true);
            SetProperty(compass, "routeShowOnCompassBar", false);
            SetProperty(compass, "routeCueShowOnMiniMap", true);
            SetProperty(compass, "routeShowInWorld", false);
            SetProperty(compass, "routeColor", routeColor);
            SetProperty(compass, "routeWidth", cab ? 6f : 5f);
            SetProperty(compass, "routeAutoProgress", true);
            SetProperty(compass, "routeAutoAdvance", true);
            SetProperty(compass, "routeWaypointTitle", "Route");
        }

        private void ConfigurePoi(Component poi, string stableId, string title, Color tint, float iconScale)
        {
            if (poi == null)
            {
                return;
            }

            SetProperty(poi, "StableId", stableId);
            SetField(poi, "title", title);
            SetEnumField(poi, "titleVisibility", "Always");
            SetEnumField(poi, "visibility", "AlwaysHidden");
            SetEnumField(poi, "miniMapVisibility", "AlwaysVisible");
            SetField(poi, "showOnScreenIndicator", false);
            SetField(poi, "showOffScreenIndicator", false);
            SetField(poi, "canBeVisited", false);
            SetField(poi, "iconScale", iconScale);
            SetField(poi, "miniMapIconScale", iconScale);
            SetField(poi, "iconShowDistance", true);
            SetField(poi, "miniMapClampPosition", true);
            SetField(poi, "tintColor", tint);
        }

        private void RegisterPoiWithCompasses(Component poi)
        {
            if (poi == null)
            {
                return;
            }

            InvokeRegisterPoi(_hudCompass, poi);
            InvokeRegisterPoi(_cabCompass, poi);
        }

        private void UpdateCompassTargets()
        {
            Camera camera = ResolveCamera();
            if (_hudCompass != null)
            {
                SetProperty(_hudCompass, "cameraMain", camera);
                SetProperty(_hudCompass, "follow", transform);
            }

            if (_cabCompass != null)
            {
                SetProperty(_cabCompass, "cameraMain", camera);
                SetProperty(_cabCompass, "follow", transform);
            }
        }

        private void ApplyCameraPresentationPolicy()
        {
            if (_cameraPresentationService == null)
            {
                return;
            }

            if (!FullMapVisible)
            {
                SetHudMinimapVisible(_cameraPresentationService.ShouldShowHudMinimap);
            }

            if (CabPresentationReady)
            {
                _cameraPresentationService.SetCabGpsActive(true);
            }
        }

        private Vector3 GlobalToLocal(Vector3 globalPosition)
        {
            return _originService != null ? _originService.GlobalToLocal(LwsWorldPositionD.FromVector3(globalPosition)) : globalPosition;
        }

        private Camera ResolveCamera()
        {
            Camera main = Camera.main;
            return main != null ? main : FindFirstObjectByType<Camera>();
        }

        private static int BuildRouteSignature(LwsRouteResult route)
        {
            if (route == null || route.waypoints == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = route.succeeded ? 17 : 23;
                hash = hash * 31 + route.waypoints.Count;
                for (int i = 0; i < route.waypoints.Count; i++)
                {
                    Vector3 point = route.waypoints[i];
                    hash = hash * 31 + Mathf.RoundToInt(point.x * 10f);
                    hash = hash * 31 + Mathf.RoundToInt(point.y * 10f);
                    hash = hash * 31 + Mathf.RoundToInt(point.z * 10f);
                }

                return hash;
            }
        }

        private static int BuildGraphSignature(LwsRoadGraph graph)
        {
            if (graph == null || graph.edges == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = graph.edges.Count;
                for (int i = 0; i < graph.edges.Count; i++)
                {
                    LwsRoadEdge edge = graph.edges[i];
                    hash = hash * 31 + (edge != null && edge.edgeId != null ? edge.edgeId.GetHashCode() : 0);
                    hash = hash * 31 + (edge != null && edge.samples != null ? edge.samples.Count : 0);
                }

                return hash;
            }
        }


        private static void FitCabCompassToPhysicalScreen(GameObject root, Vector2 screenSize)
        {
            if (root == null)
            {
                return;
            }

            Vector2 safeScreenSize = SanitizeCabScreenSize(screenSize);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = safeScreenSize;
                rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, safeScreenSize.x);
                rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, safeScreenSize.y);
            }

            RectTransform miniMapRoot = FindRectTransformRecursive(root.transform, "MiniMap Root");
            if (miniMapRoot != null)
            {
                FitCabChildRectToScreen(miniMapRoot, safeScreenSize);
            }

            RectTransform miniMap = FindRectTransformRecursive(root.transform, "MiniMap");
            if (miniMap != null)
            {
                FitCabChildRectToScreen(miniMap, safeScreenSize);
            }

            RectTransform miniMapMask = FindRectTransformRecursive(root.transform, "MiniMapMask");
            if (miniMapMask != null)
            {
                FitCabChildRectToScreen(miniMapMask, safeScreenSize);
            }

            if (rootRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }

            Canvas.ForceUpdateCanvases();
            ValidateCabCompassVisibilityOnce(root, safeScreenSize);
        }

        private static Vector2 SanitizeCabScreenSize(Vector2 screenSize)
        {
            return new Vector2(
                Mathf.Max(CabCompassFallbackVisibleElementSize, screenSize.x),
                Mathf.Max(CabCompassFallbackVisibleElementSize, screenSize.y));
        }

        private static void FitCabChildRectToScreen(RectTransform rect, Vector2 screenSize)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = screenSize;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenSize.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, screenSize.y);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static void ValidateCabCompassVisibilityOnce(GameObject root, Vector2 screenSize)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (!IsVisibleCabCompassElement(rect))
                {
                    continue;
                }

                if (HasInvertedAnchors(rect) ||
                    rect.rect.width <= CabCompassMinimumVisibleRectPixels ||
                    rect.rect.height <= CabCompassMinimumVisibleRectPixels ||
                    IsEffectivelyZeroScale(rect.localScale))
                {
                    Vector2 repairSize = IsFullScreenCabCompassElement(rect)
                        ? screenSize
                        : new Vector2(CabCompassFallbackVisibleElementSize, CabCompassFallbackVisibleElementSize);
                    FitCabChildRectToScreen(rect, repairSize);
                    Debug.LogError(
                        $"Cab Compass GPS presentation RectTransform collapsed and was repaired: {BuildHierarchyPath(rect.transform)}",
                        rect);
                }
            }
#endif
        }

        private static bool IsVisibleCabCompassElement(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy || !IsVisibleByCanvasGroups(rect.transform))
            {
                return false;
            }

            if (IsFullScreenCabCompassElement(rect))
            {
                return true;
            }

            Canvas canvas = rect.GetComponent<Canvas>();
            if (canvas != null && canvas.enabled)
            {
                return true;
            }

            Graphic graphic = rect.GetComponent<Graphic>();
            return graphic != null && graphic.enabled && graphic.color.a > 0.01f;
        }

        private static bool IsFullScreenCabCompassElement(RectTransform rect)
        {
            if (rect == null)
            {
                return false;
            }

            return string.Equals(rect.name, "MiniMap Root", StringComparison.Ordinal) ||
                   string.Equals(rect.name, "MiniMap", StringComparison.Ordinal) ||
                   string.Equals(rect.name, "MiniMapMask", StringComparison.Ordinal);
        }

        private static bool IsVisibleByCanvasGroups(Transform transform)
        {
            while (transform != null)
            {
                CanvasGroup canvasGroup = transform.GetComponent<CanvasGroup>();
                if (canvasGroup != null && canvasGroup.alpha <= 0.01f)
                {
                    return false;
                }

                transform = transform.parent;
            }

            return true;
        }

        private static bool HasInvertedAnchors(RectTransform rect)
        {
            return rect.anchorMin.x > rect.anchorMax.x || rect.anchorMin.y > rect.anchorMax.y;
        }

        private static bool IsEffectivelyZeroScale(Vector3 scale)
        {
            return Mathf.Abs(scale.x) <= 0.0001f || Mathf.Abs(scale.y) <= 0.0001f || Mathf.Abs(scale.z) <= 0.0001f;
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }

        private static RectTransform FindRectTransformRecursive(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, objectName, StringComparison.Ordinal))
            {
                return root as RectTransform;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform result = FindRectTransformRecursive(root.GetChild(i), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
        private static void DisableLegacyCabMapGraphic(LwsCabGpsController cabGps)
        {
            if (cabGps == null)
            {
                return;
            }

            LwsSemanticGpsMapGraphic[] legacyMaps = cabGps.GetComponentsInChildren<LwsSemanticGpsMapGraphic>(true);
            for (int i = 0; i < legacyMaps.Length; i++)
            {
                if (legacyMaps[i] != null)
                {
                    legacyMaps[i].gameObject.SetActive(false);
                }
            }

            cabGps.SetFallbackPhysicalScreenVisible(false);
        }

        private static bool InvokeSetRoute(Component compass, List<Vector3> routePoints)
        {
            if (compass == null)
            {
                return false;
            }

            MethodInfo method = compass.GetType().GetMethod("SetRoute", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                return false;
            }

            method.Invoke(compass, new object[] { routePoints });
            return true;
        }

        private static void InvokeRegisterPoi(Component compass, Component poi)
        {
            if (compass == null || poi == null)
            {
                return;
            }

            MethodInfo method = compass.GetType().GetMethod("POIRegister", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(compass, new object[] { poi });
        }

        private static void InvokeNoArgs(Component component, string methodName)
        {
            MethodInfo method = component != null ? component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public) : null;
            method?.Invoke(component, null);
        }

        private static bool SetProperty(object target, string propertyName, object value)
        {
            if (target == null)
            {
                return false;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            property.SetValue(target, value);
            return true;
        }

        private static bool SetField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return false;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                return false;
            }

            field.SetValue(target, value);
            return true;
        }

        private static bool SetEnumProperty(object target, string propertyName, string enumName)
        {
            if (target == null)
            {
                return false;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
            {
                return false;
            }

            object value = Enum.Parse(property.PropertyType, enumName);
            property.SetValue(target, value);
            return true;
        }

        private static bool SetEnumField(object target, string fieldName, string enumName)
        {
            if (target == null)
            {
                return false;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null || !field.FieldType.IsEnum)
            {
                return false;
            }

            object value = Enum.Parse(field.FieldType, enumName);
            field.SetValue(target, value);
            return true;
        }

        private static bool GetBoolProperty(object target, string propertyName)
        {
            PropertyInfo property = target != null
                ? target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                : null;
            return property != null && property.PropertyType == typeof(bool) && (bool)property.GetValue(target);
        }

        private static bool HasWritableProperty(object target, string propertyName)
        {
            PropertyInfo property = target != null
                ? target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                : null;
            return property != null && property.CanWrite;
        }

        private static Type ResolveType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return Type.GetType($"{fullName}, Assembly-CSharp-firstpass") ??
                   Type.GetType($"{fullName}, Assembly-CSharp");
        }
    }
}
