using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public sealed class LwsWeatherDebugPanel : MonoBehaviour
    {
        private static readonly string[] CyclePresetIds =
        {
            LwsWeatherPresetCatalog.ClearId,
            LwsWeatherPresetCatalog.PartlyCloudyId,
            LwsWeatherPresetCatalog.CloudyId,
            LwsWeatherPresetCatalog.OvercastId,
            LwsWeatherPresetCatalog.LightRainId,
            LwsWeatherPresetCatalog.HeavyRainId,
            LwsWeatherPresetCatalog.ThunderstormId,
            LwsWeatherPresetCatalog.LightSnowId,
            LwsWeatherPresetCatalog.HeavySnowId,
            LwsWeatherPresetCatalog.FogId
        };

        [SerializeField] private bool showPanel;
        [SerializeField] private bool developmentOnly = true;
        [SerializeField] private float transitionSeconds = 10f;
        [SerializeField] private float refreshIntervalSeconds = 0.25f;

        private ILwsWeatherService _weatherService;
        private LwsWeatherSnapshot _snapshot;
        private string _adapterStatus = "No adapter.";
        private string _lastError = string.Empty;
        private string _activeCamera = "None";
        private string _runtimeInstance = "None";
        private string _requestedLwsPreset = "None";
        private string _requestedVendorProfile = "None";
        private string _resolvedVendorProfile = "None";
        private bool _weatherMakerAvailable;
        private bool _weatherMakerInstance;
        private int _weatherMakerInstanceCount;
        private bool _dayNightManagerAvailable;
        private bool _cameraBound;
        private bool _cameraAllowed;
        private bool _vendorApplySuccess;
        private float _weatherMakerTimeHours;
        private float _nextRefreshTime;
        private int _cycleIndex;

        private void Update()
        {
            if (_weatherService == null)
            {
                ResolveService();
            }

            if (_weatherService != null)
            {
                _weatherService.Tick(Time.deltaTime);
            }

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                RefreshCache();
            }
        }

        private void OnGUI()
        {
            if (!showPanel || _weatherService == null)
            {
                return;
            }

            if (developmentOnly && !Application.isEditor && !Debug.isDebugBuild)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 360f, 420f, 570f), GUI.skin.box);
            GUILayout.Label("IH Weather Validation");
            GUILayout.Label("LWS Semantic State");
            GUILayout.Label($"LWS Service: {_weatherService != null}");
            GUILayout.Label($"Current Preset: {_snapshot.weatherPresetId}");
            GUILayout.Label($"Requested LWS Preset: {_requestedLwsPreset}");
            GUILayout.Label($"Condition: {_snapshot.condition}");
            GUILayout.Label($"Precipitation: {_snapshot.precipitationType} {_snapshot.precipitationIntensity01:0.00}");
            GUILayout.Label($"Cloud/Fog: {_snapshot.cloudCover01:0.00} / {_snapshot.fogIntensity01:0.00}");
            GUILayout.Label($"Wind: {_snapshot.windSpeedMetersPerSecond:0.0} m/s");
            GUILayout.Label($"Lightning: {_snapshot.lightningActive}");
            GUILayout.Label($"Transition: {_snapshot.transitioning} {_snapshot.transitionProgress01:0.00}");
            GUILayout.Label($"Time: {_snapshot.timeOfDayHours:0.00}h  Daylight: {_snapshot.daylight01:0.00}");
            GUILayout.Label($"Day/Night: {(_snapshot.isNight ? "Night" : "Day")}");
            GUILayout.Label($"Time Scale: {_weatherService.TimeScale:0.##}x");
            GUILayout.Label($"Last Request: {_weatherService.LastRequest}");

            GUILayout.Space(4f);
            GUILayout.Label("Weather Maker Presentation");
            GUILayout.Label($"Weather Maker Runtime: {_weatherMakerAvailable}");
            GUILayout.Label($"Weather Maker Instance: {_weatherMakerInstance} ({_weatherMakerInstanceCount})");
            GUILayout.Label($"Runtime Instance: {_runtimeInstance}");
            GUILayout.Label($"Day/Night Manager: {_dayNightManagerAvailable}");
            GUILayout.Label($"Active Gameplay Camera: {_activeCamera}");
            GUILayout.Label($"Camera Bound: {_cameraBound}");
            GUILayout.Label($"Camera Allowed: {_cameraAllowed}");
            GUILayout.Label($"Requested Vendor Profile: {_requestedVendorProfile}");
            GUILayout.Label($"Resolved Vendor Profile: {_resolvedVendorProfile}");
            GUILayout.Label($"Vendor Apply Success: {_vendorApplySuccess}");
            GUILayout.Label($"Current Time: LWS {_snapshot.timeOfDayHours:0.00}h / WM {_weatherMakerTimeHours:0.00}h");
            if (!string.IsNullOrWhiteSpace(_lastError))
            {
                GUILayout.Label($"Last Error: {_lastError}");
            }
            GUILayout.Label($"Adapter: {_adapterStatus}");

            DrawWeatherButtons();
            DrawTimeButtons();
            GUILayout.EndArea();
        }

        private void DrawWeatherButtons()
        {
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            DrawWeatherButton("CLEAR", LwsWeatherPresetCatalog.ClearId);
            DrawWeatherButton("PARTLY", LwsWeatherPresetCatalog.PartlyCloudyId);
            DrawWeatherButton("CLOUDY", LwsWeatherPresetCatalog.CloudyId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawWeatherButton("OVERCAST", LwsWeatherPresetCatalog.OvercastId);
            DrawWeatherButton("LIGHT RAIN", LwsWeatherPresetCatalog.LightRainId);
            DrawWeatherButton("HEAVY RAIN", LwsWeatherPresetCatalog.HeavyRainId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawWeatherButton("STORM", LwsWeatherPresetCatalog.ThunderstormId);
            DrawWeatherButton("LIGHT SNOW", LwsWeatherPresetCatalog.LightSnowId);
            DrawWeatherButton("HEAVY SNOW", LwsWeatherPresetCatalog.HeavySnowId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawWeatherButton("FOG", LwsWeatherPresetCatalog.FogId);
            if (GUILayout.Button("CYCLE WEATHER"))
            {
                _cycleIndex = (_cycleIndex + 1) % CyclePresetIds.Length;
                Request(CyclePresetIds[_cycleIndex], false);
            }

            if (GUILayout.Button("INSTANT CLEAR"))
            {
                Request(LwsWeatherPresetCatalog.ClearId, true);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTimeButtons()
        {
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SUNRISE")) _weatherService.SetTimeOfDayHours(6f);
            if (GUILayout.Button("NOON")) _weatherService.SetTimeOfDayHours(12f);
            if (GUILayout.Button("SUNSET")) _weatherService.SetTimeOfDayHours(18f);
            if (GUILayout.Button("MIDNIGHT")) _weatherService.SetTimeOfDayHours(0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("PAUSE")) _weatherService.SetTimeScale(0f);
            if (GUILayout.Button("1x")) _weatherService.SetTimeScale(1f);
            if (GUILayout.Button("10x")) _weatherService.SetTimeScale(10f);
            if (GUILayout.Button("60x")) _weatherService.SetTimeScale(60f);
            GUILayout.EndHorizontal();
        }

        private void DrawWeatherButton(string label, string presetId)
        {
            if (GUILayout.Button(label))
            {
                Request(presetId, false);
            }
        }

        private void Request(string presetId, bool instant)
        {
            _weatherService?.RequestWeather(presetId, transitionSeconds, instant);
            RefreshCache();
        }

        private void ResolveService()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _weatherService);
            RefreshCache();
        }

        private void RefreshCache()
        {
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshIntervalSeconds);
            if (_weatherService == null)
            {
                return;
            }

            _snapshot = _weatherService.CurrentSnapshot;
            _weatherMakerAvailable = _weatherService.WeatherMakerAvailable;
            ILwsWeatherRuntimeAdapter adapter = _weatherService.ActiveAdapter;
            _adapterStatus = adapter != null ? adapter.AdapterStatus : "No adapter.";
            _activeCamera = adapter != null ? adapter.ActiveCameraName : "None";
            _cameraBound = adapter != null && adapter.WeatherCameraBound;
            _lastError = !string.IsNullOrWhiteSpace(_weatherService.LastError) ? _weatherService.LastError : string.Empty;

            if (adapter is ILwsWeatherRuntimeDiagnostics diagnostics)
            {
                _weatherMakerAvailable = diagnostics.WeatherMakerRuntimeExists;
                _weatherMakerInstance = diagnostics.WeatherMakerInstanceResolved;
                _weatherMakerInstanceCount = diagnostics.WeatherMakerInstanceCount;
                _dayNightManagerAvailable = diagnostics.DayNightManagerAvailable;
                _runtimeInstance = diagnostics.RuntimeInstanceName;
                _cameraAllowed = diagnostics.ActiveCameraAllowed;
                _requestedLwsPreset = diagnostics.LastRequestedLwsPresetId;
                _requestedVendorProfile = diagnostics.LastRequestedWeatherMakerProfile;
                _resolvedVendorProfile = diagnostics.LastResolvedWeatherMakerProfile;
                _vendorApplySuccess = diagnostics.LastWeatherMakerApplySucceeded;
                _weatherMakerTimeHours = diagnostics.WeatherMakerTimeOfDayHours;
                if (!string.IsNullOrWhiteSpace(diagnostics.LastRuntimeError))
                {
                    _lastError = diagnostics.LastRuntimeError;
                }
            }
            else
            {
                _weatherMakerInstance = false;
                _weatherMakerInstanceCount = 0;
                _dayNightManagerAvailable = false;
                _runtimeInstance = "None";
                _cameraAllowed = false;
                _requestedLwsPreset = _snapshot.weatherPresetId;
                _requestedVendorProfile = "Unknown";
                _resolvedVendorProfile = "Unknown";
                _vendorApplySuccess = false;
                _weatherMakerTimeHours = _snapshot.timeOfDayHours;
            }
        }
    }
}
