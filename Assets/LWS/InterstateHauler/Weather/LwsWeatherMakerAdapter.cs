using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public class LwsWeatherMakerAdapter : MonoBehaviour, ILwsWeatherRuntimeAdapter, ILwsWeatherRuntimeDiagnostics
    {
        public const string DefaultWeatherMakerPrefabPath = "Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab";
        private const string WeatherMakerScriptTypeName = "DigitalRuby.WeatherMaker.WeatherMakerScript";
        private const string WeatherMakerProfileTypeName = "DigitalRuby.WeatherMaker.WeatherMakerProfileScript";
        private const string WeatherMakerPerformanceProfileTypeName = "DigitalRuby.WeatherMaker.WeatherMakerPerformanceProfileScript";
        private const string DayNightManagerTypeName = "DigitalRuby.WeatherMaker.WeatherMakerDayNightCycleManagerScript";
        private const string PrecipitationManagerTypeName = "DigitalRuby.WeatherMaker.WeatherMakerPrecipitationManagerScript";
        private const string PrecipitationTypeName = "DigitalRuby.WeatherMaker.WeatherMakerPrecipitationType";
        private const string FullScreenCloudsTypeName = "DigitalRuby.WeatherMaker.WeatherMakerFullScreenCloudsScript";
        private const string FullScreenFogTypeName = "DigitalRuby.WeatherMaker.WeatherMakerFullScreenFogScript";

        [SerializeField] private GameObject weatherMakerPrefab;
        [SerializeField] private bool instantiateWeatherMakerIfMissing = true;
        [SerializeField] private bool bindMainCamera = true;
        [SerializeField] private bool suppressNonWeatherMakerDirectionalLights = true;
        [SerializeField] private bool applyQualityOnStart = true;
        [SerializeField] private LwsRenderQualityTier defaultQualityTier = LwsRenderQualityTier.High;
        [SerializeField] private float cameraRefreshIntervalSeconds = 0.75f;
        [SerializeField, Min(0.25f), Tooltip("Refresh diagnostic text at this unscaled interval. Weather and clock updates are not throttled.")]
        private float diagnosticsRefreshIntervalSeconds = 1f;
        [SerializeField] private string runtimeInstanceName = "IH Weather Maker Runtime";

        private ILwsWeatherService _weatherService;
        private ILwsGameClockService _gameClockService;
        private ILwsCameraPresentationService _cameraPresentationService;
        private object _weatherMakerInstance;
        private object _dayNightManagerInstance;
        private Type _weatherMakerScriptType;
        private Type _weatherMakerProfileType;
        private Type _weatherMakerPerformanceProfileType;
        private Type _dayNightManagerType;
        private Type _precipitationManagerType;
        private Type _precipitationType;
        private Type _fullScreenCloudsType;
        private Type _fullScreenFogType;
        private float _nextCameraRefreshTime;
        private float _nextVisualDiagnosticsTime;
        private Component _managerCacheRoot;
        private readonly Dictionary<Type, CachedManager> _managerCache = new Dictionary<Type, CachedManager>();
        private struct CachedManager
        {
            public Component component;
            public float retryAfter;
        }
        private bool _attached;
        private bool _directionalLightsSuppressed;
        private bool _gameClockSlaveConfigured;
        private bool _cameraPresentationSubscribed;
        private long _lastAppliedClockVersion = long.MinValue;

        public bool WeatherMakerAvailable { get; private set; }
        public bool WeatherCameraBound { get; private set; }
        public string ActiveCameraName { get; private set; } = "None";
        public string AdapterStatus { get; private set; } = "Not initialized.";
        public string LastAppliedWeatherMakerProfile { get; private set; } = "None";
        public bool WeatherMakerPrefabConfigured => weatherMakerPrefab != null;
        public bool WeatherMakerRuntimeExists => WeatherMakerAvailable;
        public bool WeatherMakerInstanceResolved { get; private set; }
        public int WeatherMakerInstanceCount { get; private set; }
        public bool DayNightManagerAvailable { get; private set; }
        public string RuntimeInstanceName { get; private set; } = "None";
        public bool ActiveCameraAllowed { get; private set; }
        public string LastRequestedLwsPresetId { get; private set; } = "None";
        public string LastRequestedWeatherMakerProfile { get; private set; } = "None";
        public string LastResolvedWeatherMakerProfile { get; private set; } = "None";
        public bool LastWeatherMakerApplySucceeded { get; private set; }
        public float WeatherMakerTimeOfDayHours { get; private set; } = 12f;
        public string LastApplySummary { get; private set; } = "None";
        public string PrecipitationDiagnostic { get; private set; } = "None";
        public string CloudCoverDiagnostic { get; private set; } = "None";
        public string FogDiagnostic { get; private set; } = "None";
        public string LastRuntimeError { get; private set; } = string.Empty;
        public bool GameClockSlaved => _gameClockService != null && _gameClockSlaveConfigured;
        public Transform WeatherMakerRuntimeRoot => (_weatherMakerInstance as Component)?.transform.root;

        public void ConfigureWeatherMakerPrefab(GameObject prefab)
        {
            if (prefab != null)
            {
                weatherMakerPrefab = prefab;
            }
        }

        public void RefreshCameraBindingForValidation()
        {
            RefreshCameraBinding();
        }

        private void OnEnable()
        {
            InvalidateManagerCache();
            ResolveService();
            EnsureWeatherMakerRuntime(false);
            if (applyQualityOnStart)
            {
                ApplyQualityTier(defaultQualityTier);
            }

            AttachToService();
        }

        private void OnDisable()
        {
            if (_weatherService != null)
            {
                _weatherService.DetachAdapter(this);
            }

            UnsubscribeCameraPresentation();
            InvalidateManagerCache();
            _attached = false;
        }

        private void Update()
        {
            if (_weatherService == null || _gameClockService == null)
            {
                ResolveService();
            }

            AttachToService();
            if (_gameClockService != null)
            {
                if (!_gameClockSlaveConfigured)
                {
                    _weatherService?.SetTimeScale(0f);
                    ApplyTimeScale(0f);
                    _gameClockSlaveConfigured = true;
                }

                if (LwsGameClockCoordinator.ActiveInstance == null)
                {
                    _gameClockService.Tick(Time.unscaledDeltaTime);
                }

                LwsGameClockSnapshot clock = _gameClockService.CurrentSnapshot;
                if (_weatherService != null && _lastAppliedClockVersion != clock.versionTicks)
                {
                    _lastAppliedClockVersion = clock.versionTicks;
                    _weatherService.SetTimeOfDayHours(clock.timeOfDayHours);
                }

                _weatherService?.Tick(Time.deltaTime);
            }
            else
            {
                _gameClockSlaveConfigured = false;
                _weatherService?.Tick(Time.deltaTime);
            }

            if (Time.unscaledTime >= _nextCameraRefreshTime)
            {
                _nextCameraRefreshTime = Time.unscaledTime + Mathf.Max(0.25f, cameraRefreshIntervalSeconds);
                RefreshCameraBinding();
            }

            RefreshWeatherMakerVisualDiagnostics();
        }

        public bool ApplyWeatherPreset(LwsWeatherPreset preset, float transitionSeconds, bool instant)
        {
            LastRequestedLwsPresetId = preset.presetId;
            LastRequestedWeatherMakerProfile = preset.weatherMakerProfileName;
            LastResolvedWeatherMakerProfile = "None";
            LastWeatherMakerApplySucceeded = false;
            LastRuntimeError = string.Empty;

            if (!EnsureWeatherMakerRuntime(true))
            {
                LastRuntimeError = AdapterStatus;
                return false;
            }

            if (!ResolveWeatherMakerTypes())
            {
                LastRuntimeError = AdapterStatus;
                return false;
            }

            object profile = LoadWeatherMakerResource(_weatherMakerProfileType, preset.weatherMakerProfileName, preset.weatherMakerProfilePath);
            if (profile == null)
            {
                AdapterStatus = $"Weather Maker profile not found: {preset.weatherMakerProfileName}";
                LastRuntimeError = AdapterStatus;
                return false;
            }

            try
            {
                object oldProfile = GetMember(_weatherMakerInstance, "LastLocalProfile");
                MethodInfo raise = FindMethod(
                    _weatherMakerScriptType,
                    "RaiseWeatherProfileChanged",
                    _weatherMakerProfileType,
                    _weatherMakerProfileType,
                    typeof(float),
                    typeof(float),
                    typeof(bool),
                    typeof(string[]));
                if (raise == null)
                {
                    AdapterStatus = "WeatherMakerScript.RaiseWeatherProfileChanged API was not found.";
                    LastRuntimeError = AdapterStatus;
                    return false;
                }

                raise.Invoke(_weatherMakerInstance, new[] { oldProfile, profile, instant ? 0.001f : Mathf.Max(0.001f, transitionSeconds), -1f, true, null });
                SetMember(_weatherMakerInstance, "LastLocalProfile", profile);
                string visualHint = ApplySemanticWeatherToWeatherMakerRuntime(preset, transitionSeconds, instant);
                RefreshWeatherMakerVisualDiagnostics(true);
                LastResolvedWeatherMakerProfile = GetUnityObjectName(profile);
                LastAppliedWeatherMakerProfile = LastResolvedWeatherMakerProfile;
                LastWeatherMakerApplySucceeded = true;
                string applyMode = instant ? "instant" : $"{transitionSeconds:0.0}s";
                LastApplySummary = $"{preset.displayName} -> {LastResolvedWeatherMakerProfile} ({applyMode})";
                AdapterStatus = $"Applied Weather Maker profile {LastResolvedWeatherMakerProfile}. {visualHint}";
                return true;
            }
            catch (Exception ex)
            {
                AdapterStatus = $"Weather Maker weather request failed: {ex.GetType().Name}: {ex.Message}";
                LastRuntimeError = AdapterStatus;
                Debug.LogWarning(AdapterStatus, this);
                return false;
            }
        }

        public bool ApplyTimeOfDayHours(float hours)
        {
            if (!ResolveDayNightManager(out object dayNight))
            {
                return false;
            }

            try
            {
                float normalized = LwsWeatherSnapshot.NormalizeHours(hours);
                SetMember(dayNight, "TimeOfDay", normalized * 3600f);
                WeatherMakerTimeOfDayHours = normalized;
                AdapterStatus = $"Set Weather Maker time to {normalized:0.00}h.";
                return true;
            }
            catch (Exception ex)
            {
                AdapterStatus = $"Weather Maker time request failed: {ex.GetType().Name}: {ex.Message}";
                LastRuntimeError = AdapterStatus;
                Debug.LogWarning(AdapterStatus, this);
                return false;
            }
        }

        public bool ApplyTimeScale(float timeScale)
        {
            if (!ResolveDayNightManager(out object dayNight))
            {
                return false;
            }

            try
            {
                float speed = Mathf.Max(0f, timeScale);
                SetMember(dayNight, "Speed", speed);
                SetMember(dayNight, "NightSpeed", speed);
                AdapterStatus = $"Set Weather Maker time speed to {speed:0.##}x.";
                return true;
            }
            catch (Exception ex)
            {
                AdapterStatus = $"Weather Maker time-speed request failed: {ex.GetType().Name}: {ex.Message}";
                LastRuntimeError = AdapterStatus;
                Debug.LogWarning(AdapterStatus, this);
                return false;
            }
        }

        public bool ApplyQualityTier(LwsRenderQualityTier tier)
        {
            if (!EnsureWeatherMakerRuntime(true) || !ResolveWeatherMakerTypes())
            {
                return false;
            }

            string profileName = GetPerformanceProfileName(tier);
            object profile = LoadWeatherMakerResource(_weatherMakerPerformanceProfileType, profileName, null);
            if (profile == null)
            {
                AdapterStatus = $"Weather Maker performance profile not found: {profileName}";
                LastRuntimeError = AdapterStatus;
                return false;
            }

            SetMember(_weatherMakerInstance, "PerformanceProfile", profile);
            AdapterStatus = $"Applied Weather Maker performance profile {profileName}.";
            return true;
        }

        private void ResolveService()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _weatherService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _gameClockService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _cameraPresentationService);
            SubscribeCameraPresentation();
        }

        private void AttachToService()
        {
            if (_attached || _weatherService == null)
            {
                return;
            }

            _attached = _weatherService.AttachAdapter(this);
        }

        private bool EnsureWeatherMakerRuntime(bool logFailures)
        {
            if (!ResolveWeatherMakerTypes())
            {
                return false;
            }

            if (IsAlive(_weatherMakerInstance) || RefreshWeatherMakerRuntimeFromScene())
            {
                WeatherMakerAvailable = true;
                WeatherMakerInstanceResolved = true;
                AdapterStatus = "Weather Maker runtime is available.";
                SuppressCompetingDirectionalLights();
                return true;
            }

            if (!instantiateWeatherMakerIfMissing)
            {
                WeatherMakerAvailable = false;
                WeatherMakerInstanceResolved = false;
                AdapterStatus = "Weather Maker runtime is missing and auto-instantiation is disabled.";
                LastRuntimeError = AdapterStatus;
                return false;
            }

            GameObject prefab = ResolveWeatherMakerPrefab();
            if (prefab == null)
            {
                WeatherMakerAvailable = false;
                WeatherMakerInstanceResolved = false;
                AdapterStatus = $"Weather Maker prefab reference missing. Expected {DefaultWeatherMakerPrefabPath}.";
                LastRuntimeError = AdapterStatus;
                if (logFailures)
                {
                    Debug.LogWarning(AdapterStatus, this);
                }

                return false;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = string.IsNullOrWhiteSpace(runtimeInstanceName) ? "IH Weather Maker Runtime" : runtimeInstanceName;
            _weatherMakerInstance = GetComponentInChildrenOfType(instance, _weatherMakerScriptType);
            RefreshWeatherMakerRuntimeFromScene();
            if (IsAlive(_weatherMakerInstance))
            {
                // Preserve the prefab's lifetime policy: Taxi weather is scene-scoped,
                // while the normal Interstate prefab is authored as permanent.
                SetMember(_weatherMakerInstance, "AutoFindMainCamera", false);
                WeatherMakerAvailable = true;
                WeatherMakerInstanceResolved = true;
                AdapterStatus = "Instantiated Weather Maker prefab for validation/runtime presentation.";
                RefreshDayNightManagerDiagnostic();
                SuppressCompetingDirectionalLights();
                RefreshCameraBinding();
                return true;
            }

            WeatherMakerAvailable = false;
            WeatherMakerInstanceResolved = false;
            AdapterStatus = "Weather Maker prefab instantiated, but WeatherMakerScript was not found in the instance.";
            LastRuntimeError = AdapterStatus;
            if (logFailures)
            {
                Debug.LogWarning(AdapterStatus, this);
            }

            return false;
        }

        private bool ResolveWeatherMakerTypes()
        {
            _weatherMakerScriptType ??= ResolveType(WeatherMakerScriptTypeName);
            _weatherMakerProfileType ??= ResolveType(WeatherMakerProfileTypeName);
            _weatherMakerPerformanceProfileType ??= ResolveType(WeatherMakerPerformanceProfileTypeName);
            _dayNightManagerType ??= ResolveType(DayNightManagerTypeName);

            bool resolved = _weatherMakerScriptType != null && _weatherMakerProfileType != null;
            if (!resolved)
            {
                WeatherMakerAvailable = false;
                AdapterStatus = "Weather Maker runtime types were not found.";
            }

            return resolved;
        }

        private bool ResolveDayNightManager(out object dayNight)
        {
            dayNight = null;
            if (!EnsureWeatherMakerRuntime(true))
            {
                return false;
            }

            _dayNightManagerType ??= ResolveType(DayNightManagerTypeName);
            if (_dayNightManagerType == null)
            {
                AdapterStatus = "WeatherMakerDayNightCycleManagerScript type was not found.";
                DayNightManagerAvailable = false;
                LastRuntimeError = AdapterStatus;
                return false;
            }

            dayNight = FindRuntimeManager(_dayNightManagerType);
            _dayNightManagerInstance = dayNight;
            DayNightManagerAvailable = IsAlive(dayNight);
            if (!DayNightManagerAvailable)
            {
                AdapterStatus = "Weather Maker day/night manager instance was not found.";
                LastRuntimeError = AdapterStatus;
                return false;
            }

            RefreshWeatherMakerTimeDiagnostic(dayNight);
            return true;
        }

        private void RefreshCameraBinding()
        {
            WeatherCameraBound = false;
            ActiveCameraAllowed = false;
            ActiveCameraName = "None";
            if (!bindMainCamera || !EnsureWeatherMakerRuntime(false))
            {
                return;
            }

            Camera camera = FindActiveGameplayCamera();
            if (camera == null)
            {
                AdapterStatus = "No supported gameplay camera was found for Weather Maker binding.";
                LastRuntimeError = AdapterStatus;
                return;
            }

            object allowCameras = GetMember(_weatherMakerInstance, "AllowCameras");
            if (allowCameras is IList list)
            {
                list.Clear();
                list.Add(camera);
                ClearListMember(_weatherMakerInstance, "AllowCamerasNames");
                ClearListMember(_weatherMakerInstance, "AllowCamerasNamesPartial");
                SetMember(_weatherMakerInstance, "AutoFindMainCamera", false);
                ClearWeatherMakerCameraIgnoreCache();
                WeatherCameraBound = true;
                ActiveCameraAllowed = list.Contains(camera);
                ActiveCameraName = camera.name;
                AdapterStatus = $"Weather Maker camera bound to {camera.name}.";
            }
            else
            {
                AdapterStatus = "WeatherMakerScript.AllowCameras was not available.";
                LastRuntimeError = AdapterStatus;
            }
        }

        private GameObject ResolveWeatherMakerPrefab()
        {
            if (weatherMakerPrefab != null)
            {
                return weatherMakerPrefab;
            }

#if UNITY_EDITOR
            weatherMakerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultWeatherMakerPrefabPath);
#endif
            return weatherMakerPrefab;
        }

        private bool RefreshWeatherMakerRuntimeFromScene()
        {
            object found = FindSceneComponent(_weatherMakerScriptType, out int instanceCount);
            if (IsAlive(found) || !IsAlive(_weatherMakerInstance)) _weatherMakerInstance = found;
            WeatherMakerInstanceCount = instanceCount;
            WeatherMakerInstanceResolved = IsAlive(_weatherMakerInstance);
            WeatherMakerAvailable = WeatherMakerInstanceResolved;
            RuntimeInstanceName = GetRuntimeRootName(_weatherMakerInstance);
            return WeatherMakerInstanceResolved;
        }

        private void RefreshDayNightManagerDiagnostic()
        {
            _dayNightManagerType ??= ResolveType(DayNightManagerTypeName);
            _dayNightManagerInstance = FindRuntimeManager(_dayNightManagerType);
            DayNightManagerAvailable = IsAlive(_dayNightManagerInstance);
            if (DayNightManagerAvailable)
            {
                RefreshWeatherMakerTimeDiagnostic(_dayNightManagerInstance);
            }
        }

        private void RefreshWeatherMakerTimeDiagnostic(object dayNight)
        {
            object seconds = GetMember(dayNight, "TimeOfDay");
            if (seconds is float timeSeconds)
            {
                WeatherMakerTimeOfDayHours = LwsWeatherSnapshot.NormalizeHours(timeSeconds / 3600f);
            }
        }

        private void SuppressCompetingDirectionalLights()
        {
            if (_directionalLightsSuppressed || !suppressNonWeatherMakerDirectionalLights || !IsAlive(_weatherMakerInstance))
            {
                return;
            }

            Transform weatherRoot = (_weatherMakerInstance as Component)?.transform.root;
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null || light.type != LightType.Directional || !light.enabled)
                {
                    continue;
                }

                if (weatherRoot != null && light.transform.IsChildOf(weatherRoot))
                {
                    continue;
                }

                light.enabled = false;
            }

            _directionalLightsSuppressed = true;
        }

        private Camera FindActiveGameplayCamera()
        {
            Camera presentationCamera = FindCameraFromPresentationService();
            if (presentationCamera != null)
            {
                return presentationCamera;
            }

            Camera main = Camera.main;
            if (IsGameplayCamera(main))
            {
                return main;
            }

            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (IsGameplayCamera(cameras[i]))
                {
                    return cameras[i];
                }
            }

            return null;
        }

        private Camera FindCameraFromPresentationService()
        {
            if (_cameraPresentationService == null ||
                string.IsNullOrWhiteSpace(_cameraPresentationService.CurrentCameraName) ||
                string.Equals(_cameraPresentationService.CurrentCameraName, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (IsGameplayCamera(camera) &&
                    string.Equals(camera.name, _cameraPresentationService.CurrentCameraName, StringComparison.Ordinal))
                {
                    return camera;
                }
            }

            return null;
        }

        private static bool IsGameplayCamera(Camera camera)
        {
            if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy || camera.targetTexture != null)
            {
                return false;
            }

            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
            {
                return false;
            }

            string cameraName = camera.name;
            return cameraName.IndexOf("mirror", StringComparison.OrdinalIgnoreCase) < 0 &&
                   cameraName.IndexOf("rendertexture", StringComparison.OrdinalIgnoreCase) < 0 &&
                   cameraName.IndexOf("reflection", StringComparison.OrdinalIgnoreCase) < 0 &&
                   cameraName.IndexOf("depth", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private object FindSceneComponent(Type componentType, out int count)
        {
            count = 0;
            if (componentType == null)
            {
                return null;
            }

            Component first = null;
            // Discovery is only needed for runtime acquisition or an uncached manager.
            // Restrict it to the requested type, never every component in the project.
            UnityEngine.Object[] components = Resources.FindObjectsOfTypeAll(componentType);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i] as Component;
                if (component == null)
                {
                    continue;
                }

                GameObject componentObject = component.gameObject;
                // OnEnable runs during scene activation, before scene.isLoaded is true.
                // Reject assets (invalid scene), not the runtime we just instantiated.
                if (componentObject == null || !componentObject.scene.IsValid())
                {
                    continue;
                }

                count++;
                if (first == null || componentObject.activeInHierarchy)
                {
                    first = component;
                }
            }

            return first;
        }

        private void InvalidateManagerCache()
        {
            _managerCache.Clear();
            _managerCacheRoot = null;
            _dayNightManagerInstance = null;
            _nextVisualDiagnosticsTime = 0f;
        }

        private Component FindRuntimeManager(Type componentType)
        {
            Component root = _weatherMakerInstance as Component;
            if (root == null || componentType == null) return null;
            if (_managerCacheRoot != root)
            {
                InvalidateManagerCache();
                _managerCacheRoot = root;
            }

            float now = Time.unscaledTime;
            if (_managerCache.TryGetValue(componentType, out CachedManager cached))
            {
                if (cached.component != null) return cached.component;
                // Retry destroyed references immediately, but missing optional managers
                // at most once a second, including while the game clock is paused.
                if (ReferenceEquals(cached.component, null) && now < cached.retryAfter) return null;
            }

            Component found = root.GetComponentInChildren(componentType, true);
            // Support existing authored setups whose managers are outside the prefab root.
            if (found == null) found = FindSceneComponent(componentType, out _) as Component;
            _managerCache[componentType] = new CachedManager { component = found, retryAfter = now + 1f };
            return found;
        }

        private string ApplySemanticWeatherToWeatherMakerRuntime(LwsWeatherPreset preset, float transitionSeconds, bool instant)
        {
            string precipitation = ApplySemanticPrecipitationToWeatherMaker(preset, transitionSeconds, instant);
            string fog = ApplySemanticFogToWeatherMaker(preset, transitionSeconds, instant);
            return $"{precipitation} {fog}".Trim();
        }

        private string ApplySemanticPrecipitationToWeatherMaker(LwsWeatherPreset preset, float transitionSeconds, bool instant)
        {
            _precipitationManagerType ??= ResolveType(PrecipitationManagerTypeName);
            _precipitationType ??= ResolveType(PrecipitationTypeName);
            object manager = FindRuntimeManager(_precipitationManagerType);
            if (manager == null || _precipitationType == null)
            {
                return "Precipitation manager diagnostics unavailable.";
            }

            try
            {
                string precipitationName = preset.precipitationType switch
                {
                    LwsPrecipitationType.Rain => "Rain",
                    LwsPrecipitationType.Snow => "Snow",
                    LwsPrecipitationType.Mixed => "Sleet",
                    _ => "None"
                };

                float visualDuration = instant ? 0.001f : Mathf.Clamp(transitionSeconds, 0.25f, 4f);
                SetMember(manager, "PrecipitationChangeDelay", 0f);
                SetMember(manager, "PrecipitationChangeDuration", visualDuration);
                SetMember(manager, "PrecipitationIntensity", Mathf.Clamp01(preset.precipitationIntensity01));
                SetMember(manager, "Precipitation", Enum.Parse(_precipitationType, precipitationName));

                if (instant)
                {
                    SetPrecipitationScriptIntensity(GetMember(manager, "RainScript"), precipitationName == "Rain" ? preset.precipitationIntensity01 : 0f);
                    SetPrecipitationScriptIntensity(GetMember(manager, "SnowScript"), precipitationName == "Snow" ? preset.precipitationIntensity01 : 0f);
                    SetPrecipitationScriptIntensity(GetMember(manager, "SleetScript"), precipitationName == "Sleet" ? preset.precipitationIntensity01 : 0f);
                }

                return $"Precipitation {precipitationName} {preset.precipitationIntensity01:0.00}.";
            }
            catch (Exception ex)
            {
                string message = $"Weather Maker precipitation hint failed: {ex.GetType().Name}: {ex.Message}";
                LastRuntimeError = message;
                Debug.LogWarning(message, this);
                return message;
            }
        }

        private string ApplySemanticFogToWeatherMaker(LwsWeatherPreset preset, float transitionSeconds, bool instant)
        {
            _fullScreenFogType ??= ResolveType(FullScreenFogTypeName);
            try
            {
                object fogScript = FindRuntimeManager(_fullScreenFogType);
                object fogProfile = GetMember(fogScript, "FogProfile");
                if (fogProfile == null)
                {
                    return "Fog diagnostics unavailable.";
                }

                float fog01 = Mathf.Clamp01(preset.fogIntensity01);
                float density = fog01 <= 0.001f ? 0f : Mathf.Lerp(0.0006f, 0.018f, fog01);
                SetMember(fogProfile, "MaxFogFactor", fog01 > 0.5f ? 0.78f : 0.45f);
                // The vendor tween captures its destination. A direct density write
                // after profile application is overwritten on the next tween tick.
                MethodInfo showFog = FindMethod(_fullScreenFogType, "ShowFogAnimated",
                    fogProfile.GetType(), typeof(float), typeof(float), typeof(float?), typeof(string));
                if (showFog == null) throw new MissingMethodException("Weather Maker ShowFogAnimated API was not found.");
                showFog.Invoke(fogScript, new object[] { fogProfile, 0f,
                    instant ? 0.001f : Mathf.Max(0.001f, transitionSeconds), (float?)density, null });
                return $"Fog {fog01:0.00}.";
            }
            catch (Exception ex)
            {
                string message = $"Weather Maker fog hint failed: {ex.GetType().Name}: {ex.Message}";
                LastRuntimeError = message;
                Debug.LogWarning(message, this);
                return message;
            }
        }

        private static void SetPrecipitationScriptIntensity(object script, float intensity)
        {
            if (script == null)
            {
                return;
            }

            SetMember(script, "ExternalIntensityMultiplier", 1f);
            SetMember(script, "Intensity", Mathf.Clamp01(intensity));
        }

        private void RefreshWeatherMakerVisualDiagnostics(bool force = false)
        {
            float now = Time.unscaledTime;
            if (!force && now < _nextVisualDiagnosticsTime) return;
            _nextVisualDiagnosticsTime = now + Mathf.Max(0.25f, diagnosticsRefreshIntervalSeconds);
            if (!WeatherMakerAvailable)
            {
                PrecipitationDiagnostic = "Weather Maker unavailable.";
                CloudCoverDiagnostic = "Weather Maker unavailable.";
                FogDiagnostic = "Weather Maker unavailable.";
                return;
            }

            _precipitationManagerType ??= ResolveType(PrecipitationManagerTypeName);
            _fullScreenCloudsType ??= ResolveType(FullScreenCloudsTypeName);
            _fullScreenFogType ??= ResolveType(FullScreenFogTypeName);

            try
            {
                RefreshDayNightManagerDiagnostic();
                object precipitationManager = FindRuntimeManager(_precipitationManagerType);
                if (precipitationManager != null)
                {
                    float rain = ReadFloatMember(precipitationManager, "RainIntensity");
                    float snow = ReadFloatMember(precipitationManager, "SnowIntensity");
                    float target = ReadFloatMember(precipitationManager, "PrecipitationIntensity");
                    string precipitation = ReadMemberName(precipitationManager, "Precipitation");
                    PrecipitationDiagnostic = $"{precipitation} target {target:0.00}, rain {rain:0.00}, snow {snow:0.00}";
                }
                else
                {
                    PrecipitationDiagnostic = "Weather Maker precipitation manager missing.";
                }

                object cloudScript = FindRuntimeManager(_fullScreenCloudsType);
                object cloudProfile = GetMember(cloudScript, "CloudProfile");
                CloudCoverDiagnostic = cloudProfile != null
                    ? $"{GetUnityObjectName(cloudProfile)} cover {ReadFloatMember(cloudProfile, "CloudCoverTotal"):0.00}"
                    : "Weather Maker cloud profile missing.";

                object fogScript = FindRuntimeManager(_fullScreenFogType);
                object fogProfile = GetMember(fogScript, "FogProfile");
                FogDiagnostic = fogProfile != null
                    ? $"{GetUnityObjectName(fogProfile)} density {ReadFloatMember(fogProfile, "FogDensity"):0.0000}, max {ReadFloatMember(fogProfile, "MaxFogFactor"):0.00}"
                    : "Weather Maker fog profile missing.";
            }
            catch (Exception ex)
            {
                string message = $"Weather Maker visual diagnostics failed: {ex.GetType().Name}: {ex.Message}";
                LastRuntimeError = message;
                PrecipitationDiagnostic = message;
                CloudCoverDiagnostic = "Weather Maker diagnostics unavailable.";
                FogDiagnostic = "Weather Maker diagnostics unavailable.";
            }
        }

        private void SubscribeCameraPresentation()
        {
            if (_cameraPresentationSubscribed || _cameraPresentationService == null)
            {
                return;
            }

            _cameraPresentationService.CameraModeChanged += OnCameraModeChanged;
            _cameraPresentationSubscribed = true;
        }

        private void UnsubscribeCameraPresentation()
        {
            if (!_cameraPresentationSubscribed || _cameraPresentationService == null)
            {
                return;
            }

            _cameraPresentationService.CameraModeChanged -= OnCameraModeChanged;
            _cameraPresentationSubscribed = false;
        }

        private void OnCameraModeChanged(LwsVehicleCameraMode mode)
        {
            _nextCameraRefreshTime = 0f;
            RefreshCameraBinding();
        }

        private object LoadWeatherMakerResource(Type resourceType, string resourceName, string assetPath)
        {
            if (_weatherMakerInstance == null || resourceType == null || string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            MethodInfo loadResource = _weatherMakerScriptType.GetMethod("LoadResource", BindingFlags.Instance | BindingFlags.Public);
            if (loadResource != null && loadResource.IsGenericMethodDefinition)
            {
                object result = loadResource.MakeGenericMethod(resourceType).Invoke(_weatherMakerInstance, new object[] { resourceName });
                if (result != null)
                {
                    return result;
                }
            }

#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath(assetPath, resourceType);
            }

            string[] guids = AssetDatabase.FindAssets($"{resourceName} t:{resourceType.Name}", new[] { "Assets/WeatherMaker/Prefab/Profiles" });
            if (guids != null && guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), resourceType);
            }
#endif

            return null;
        }

        private void ClearWeatherMakerCameraIgnoreCache()
        {
            MethodInfo clear = _weatherMakerScriptType?.GetMethod("ClearShouldIgnoreCameraCache", BindingFlags.Static | BindingFlags.Public);
            clear?.Invoke(null, null);
        }

        private static void ClearListMember(object target, string memberName)
        {
            if (GetMember(target, memberName) is IList list)
            {
                list.Clear();
            }
        }

        private static bool IsAlive(object target)
        {
            if (target is UnityEngine.Object unityObject)
            {
                return unityObject != null;
            }

            return target != null;
        }

        private static string GetRuntimeRootName(object target)
        {
            if (target is Component component && component != null)
            {
                Transform root = component.transform.root;
                return root != null ? root.name : component.name;
            }

            return "None";
        }

        private static string GetUnityObjectName(object target)
        {
            if (target is UnityEngine.Object unityObject && unityObject != null)
            {
                return unityObject.name;
            }

            return target != null ? target.ToString() : "None";
        }

        private static string GetPerformanceProfileName(LwsRenderQualityTier tier)
        {
            return tier switch
            {
                LwsRenderQualityTier.Ultra => "WeatherMakerPerformanceProfile_Fantastic",
                LwsRenderQualityTier.High => "WeatherMakerPerformanceProfile_Beautiful",
                LwsRenderQualityTier.Medium => "WeatherMakerPerformanceProfile_Good",
                LwsRenderQualityTier.Low => "WeatherMakerPerformanceProfile_Fast",
                LwsRenderQualityTier.SteamDeck => "WeatherMakerPerformanceProfile_Fastest",
                _ => "WeatherMakerPerformanceProfile_Default"
            };
        }

        private static object GetComponentInChildrenOfType(GameObject root, Type componentType)
        {
            if (root == null || componentType == null)
            {
                return null;
            }

            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && componentType.IsInstanceOfType(components[i]))
                {
                    return components[i];
                }
            }

            return null;
        }

        private static Type ResolveType(string typeName)
        {
            Type type = Type.GetType(typeName) ?? Type.GetType($"{typeName}, Assembly-CSharp");
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static object GetMember(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                return field.GetValue(target);
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            return property != null && property.CanRead ? property.GetValue(target) : null;
        }

        private static string ReadMemberName(object target, string memberName)
        {
            object value = GetMember(target, memberName);
            return value != null ? value.ToString() : "None";
        }

        private static float ReadFloatMember(object target, string memberName)
        {
            object value = GetMember(target, memberName);
            return value switch
            {
                float f => f,
                double d => (float)d,
                int i => i,
                _ => 0f
            };
        }

        private static bool SetMember(object target, string memberName, object value)
        {
            if (target == null)
            {
                return false;
            }

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            return false;
        }

        private static MethodInfo FindMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            return type?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null, parameterTypes, null);
        }
    }

    [Obsolete("Use LwsWeatherMakerAdapter for atmospheric weather and LwsWeatheradeAdapter for road accumulation.")]
    public sealed class LwsWeatherMakerWeatheradeAdapter : LwsWeatherMakerAdapter
    {
    }
}
