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
            LwsWeatherPresetCatalog.CloudyId,
            LwsWeatherPresetCatalog.LightRainId,
            LwsWeatherPresetCatalog.ThunderstormId,
            LwsWeatherPresetCatalog.LightSnowId,
            LwsWeatherPresetCatalog.FogId
        };

        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool developmentOnly = true;
        [SerializeField] private float transitionSeconds = 10f;
        [SerializeField] private float refreshIntervalSeconds = 0.25f;

        private ILwsWeatherService _weatherService;
        private LwsWeatherSnapshot _snapshot;
        private string _adapterStatus = "No adapter.";
        private string _activeCamera = "None";
        private bool _weatherMakerAvailable;
        private bool _cameraBound;
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

            GUILayout.BeginArea(new Rect(12f, 360f, 360f, 430f), GUI.skin.box);
            GUILayout.Label("IH Weather Validation");
            GUILayout.Label($"Initialized: {_weatherService != null}");
            GUILayout.Label($"Weather Maker Available: {_weatherMakerAvailable}");
            GUILayout.Label($"Current Preset: {_snapshot.weatherPresetId}");
            GUILayout.Label($"Condition: {_snapshot.condition}");
            GUILayout.Label($"Precipitation: {_snapshot.precipitationType} {_snapshot.precipitationIntensity01:0.00}");
            GUILayout.Label($"Cloud/Fog: {_snapshot.cloudCover01:0.00} / {_snapshot.fogIntensity01:0.00}");
            GUILayout.Label($"Wind: {_snapshot.windSpeedMetersPerSecond:0.0} m/s");
            GUILayout.Label($"Lightning: {_snapshot.lightningActive}");
            GUILayout.Label($"Transition: {_snapshot.transitioning} {_snapshot.transitionProgress01:0.00}");
            GUILayout.Label($"Time: {_snapshot.timeOfDayHours:0.00}h  Daylight: {_snapshot.daylight01:0.00}");
            GUILayout.Label($"Day/Night: {(_snapshot.isNight ? "Night" : "Day")}");
            GUILayout.Label($"Time Scale: {_weatherService.TimeScale:0.##}x");
            GUILayout.Label($"Active Camera: {_activeCamera}");
            GUILayout.Label($"Weather Camera Bound: {_cameraBound}");
            GUILayout.Label($"Last Request: {_weatherService.LastRequest}");
            if (!string.IsNullOrWhiteSpace(_weatherService.LastError))
            {
                GUILayout.Label($"Last Error: {_weatherService.LastError}");
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
            DrawWeatherButton("OVERCAST", LwsWeatherPresetCatalog.OvercastId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawWeatherButton("LIGHT RAIN", LwsWeatherPresetCatalog.LightRainId);
            DrawWeatherButton("HEAVY RAIN", LwsWeatherPresetCatalog.HeavyRainId);
            DrawWeatherButton("STORM", LwsWeatherPresetCatalog.ThunderstormId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawWeatherButton("LIGHT SNOW", LwsWeatherPresetCatalog.LightSnowId);
            DrawWeatherButton("HEAVY SNOW", LwsWeatherPresetCatalog.HeavySnowId);
            DrawWeatherButton("FOG", LwsWeatherPresetCatalog.FogId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
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
        }
    }
}
