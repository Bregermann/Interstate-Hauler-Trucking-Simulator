using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public class LwsWeatherMakerAdapter : MonoBehaviour, ILwsWeatherRuntimeAdapter
    {
        private const string WeatherMakerPrefabPath = "Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab";
        private const string WeatherMakerScriptTypeName = "DigitalRuby.WeatherMaker.WeatherMakerScript";
        private const string WeatherMakerProfileTypeName = "DigitalRuby.WeatherMaker.WeatherMakerProfileScript";
        private const string WeatherMakerPerformanceProfileTypeName = "DigitalRuby.WeatherMaker.WeatherMakerPerformanceProfileScript";
        private const string DayNightManagerTypeName = "DigitalRuby.WeatherMaker.WeatherMakerDayNightCycleManagerScript";

        [SerializeField] private bool instantiateWeatherMakerIfMissing = true;
        [SerializeField] private bool bindMainCamera = true;
        [SerializeField] private bool applyQualityOnStart = true;
        [SerializeField] private LwsRenderQualityTier defaultQualityTier = LwsRenderQualityTier.High;
        [SerializeField] private float cameraRefreshIntervalSeconds = 0.75f;

        private ILwsWeatherService _weatherService;
        private object _weatherMakerInstance;
        private Type _weatherMakerScriptType;
        private Type _weatherMakerProfileType;
        private Type _weatherMakerPerformanceProfileType;
        private Type _dayNightManagerType;
        private float _nextCameraRefreshTime;
        private bool _attached;

        public bool WeatherMakerAvailable { get; private set; }
        public bool WeatherCameraBound { get; private set; }
        public string ActiveCameraName { get; private set; } = "None";
        public string AdapterStatus { get; private set; } = "Not initialized.";
        public string LastAppliedWeatherMakerProfile { get; private set; } = "None";

        private void OnEnable()
        {
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

            _attached = false;
        }

        private void Update()
        {
            if (_weatherService == null)
            {
                ResolveService();
            }

            AttachToService();
            _weatherService?.Tick(Time.deltaTime);

            if (Time.unscaledTime >= _nextCameraRefreshTime)
            {
                _nextCameraRefreshTime = Time.unscaledTime + Mathf.Max(0.25f, cameraRefreshIntervalSeconds);
                RefreshCameraBinding();
            }
        }

        public bool ApplyWeatherPreset(LwsWeatherPreset preset, float transitionSeconds, bool instant)
        {
            if (!EnsureWeatherMakerRuntime(true))
            {
                return false;
            }

            if (!ResolveWeatherMakerTypes())
            {
                return false;
            }

            object profile = LoadWeatherMakerResource(_weatherMakerProfileType, preset.weatherMakerProfileName, preset.weatherMakerProfilePath);
            if (profile == null)
            {
                AdapterStatus = $"Weather Maker profile not found: {preset.weatherMakerProfileName}";
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
                    return false;
                }

                raise.Invoke(_weatherMakerInstance, new[] { oldProfile, profile, instant ? 0.001f : Mathf.Max(0.001f, transitionSeconds), -1f, true, null });
                SetMember(_weatherMakerInstance, "LastLocalProfile", profile);
                LastAppliedWeatherMakerProfile = preset.weatherMakerProfileName;
                AdapterStatus = $"Applied Weather Maker profile {preset.weatherMakerProfileName}.";
                return true;
            }
            catch (Exception ex)
            {
                AdapterStatus = $"Weather Maker weather request failed: {ex.GetType().Name}: {ex.Message}";
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
                SetMember(dayNight, "TimeOfDay", LwsWeatherSnapshot.NormalizeHours(hours) * 3600f);
                AdapterStatus = $"Set Weather Maker time to {LwsWeatherSnapshot.NormalizeHours(hours):0.00}h.";
                return true;
            }
            catch (Exception ex)
            {
                AdapterStatus = $"Weather Maker time request failed: {ex.GetType().Name}: {ex.Message}";
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

            _weatherMakerInstance = GetStaticProperty(_weatherMakerScriptType, "Instance");
            if (_weatherMakerInstance != null)
            {
                WeatherMakerAvailable = true;
                AdapterStatus = "Weather Maker runtime is available.";
                return true;
            }

            if (!instantiateWeatherMakerIfMissing)
            {
                WeatherMakerAvailable = false;
                AdapterStatus = "Weather Maker runtime is missing and auto-instantiation is disabled.";
                return false;
            }

#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeatherMakerPrefabPath);
            if (prefab == null)
            {
                WeatherMakerAvailable = false;
                AdapterStatus = $"{WeatherMakerPrefabPath} could not be loaded.";
                if (logFailures)
                {
                    Debug.LogWarning(AdapterStatus, this);
                }

                return false;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = "IH Weather Maker Runtime";
            _weatherMakerInstance = GetComponentInChildrenOfType(instance, _weatherMakerScriptType);
            if (_weatherMakerInstance != null)
            {
                SetMember(_weatherMakerInstance, "IsPermanent", true);
                WeatherMakerAvailable = true;
                AdapterStatus = "Instantiated Weather Maker prefab for validation.";
                RefreshCameraBinding();
                return true;
            }
#endif

            WeatherMakerAvailable = false;
            AdapterStatus = "Weather Maker runtime could not be created outside the Unity Editor.";
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
                return false;
            }

            dayNight = GetStaticProperty(_dayNightManagerType, "Instance");
            if (dayNight == null)
            {
                AdapterStatus = "Weather Maker day/night manager instance was not found.";
                return false;
            }

            return true;
        }

        private void RefreshCameraBinding()
        {
            WeatherCameraBound = false;
            ActiveCameraName = "None";
            if (!bindMainCamera || !EnsureWeatherMakerRuntime(false))
            {
                return;
            }

            Camera main = Camera.main;
            if (main == null)
            {
                AdapterStatus = "Camera.main was not found for Weather Maker binding.";
                return;
            }

            if (main.targetTexture != null || main.name.IndexOf("mirror", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AdapterStatus = $"Skipped Weather Maker binding for non-gameplay camera {main.name}.";
                return;
            }

            object allowCameras = GetMember(_weatherMakerInstance, "AllowCameras");
            if (allowCameras is IList list)
            {
                list.Clear();
                list.Add(main);
                WeatherCameraBound = true;
                ActiveCameraName = main.name;
                AdapterStatus = $"Weather Maker camera bound to {main.name}.";
            }
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

        private static object GetStaticProperty(Type type, string propertyName)
        {
            return type?.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
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

    [Obsolete("Use LwsWeatherMakerAdapter for Prompt 012 atmospheric weather. Weatherade remains deferred to Prompt 013.")]
    public sealed class LwsWeatherMakerWeatheradeAdapter : LwsWeatherMakerAdapter
    {
    }
}
