using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Taxi policy only: LWS owns elapsed game time and weather, Weather Maker owns visuals.
    [DefaultExecutionOrder(80), DisallowMultipleComponent]
    public sealed class TruckTaxiEnvironmentCoordinator : MonoBehaviour
    {
        public TruckTaxiEnvironmentSettings settings;
        public LwsWeatherMakerAdapter weatherAdapter;
        [Tooltip("Only explicitly authored city lights. Never include traffic signals or Weather Maker sun/moon.")]
        public Light[] nightLights = Array.Empty<Light>();
        public ILwsGameClockService Clock { get; private set; }
        public ILwsWeatherService Weather { get; private set; }
        public TruckTaxiDayPeriod Period { get; private set; }
        public bool Frozen { get; private set; }
        public bool AutomaticWeather { get; private set; }
        public string LastDiagnostic { get; private set; } = "Not initialized";
        public Transform WeatherAudioRoot { get; private set; }
        public event Action<Transform> WeatherAudioRootAvailable;
        public event Action<TruckTaxiDialogueCategory> EnvironmentEvent;
        private TruckTaxiBootstrap host;
        private System.Random random;
        private double nextWeatherAt;
        private LwsGameClockSnapshot previousClock;
        private LwsWeatherState previousWeather;
        private float previousWeatherScale;
        private bool initialized, audioRootChecked, audioRoutingRegistered;
        private bool[] lightStates;
        private string lastWeatherId;
        private UnityEngine.Rendering.RenderPipelineAsset previousRenderPipeline;
        private Material previousSkybox;
        private bool adapterWasEnabled;
        public bool IsInitialized => initialized;
        public string CurrentTaxiWeatherId { get; private set; } = LwsWeatherPresetCatalog.ClearId;
        public TruckTaxiSnow Snow { get; private set; }

        public bool Initialize(TruckTaxiBootstrap owner)
        {
            if (initialized) return true;
            if (owner == null || settings == null || LwsApplicationBootstrap.Instance?.Registry == null) return false;
            host = owner;
            LwsApplicationBootstrap.Instance.Registry.TryGet<ILwsGameClockService>(out var clock);
            LwsApplicationBootstrap.Instance.Registry.TryGet<ILwsWeatherService>(out var weather);
            if (clock == null || weather == null || weatherAdapter == null)
            {
                LastDiagnostic = "Taxi environment requires the existing LWS clock/weather services and a configured Weather Maker adapter.";
                Debug.LogError(LastDiagnostic, this); return false;
            }
            Clock = clock; Weather = weather; previousClock = clock.CurrentSnapshot;
            previousWeather = weather.CurrentState; previousWeatherScale = weather.TimeScale;
            previousRenderPipeline = QualitySettings.renderPipeline;
            if (settings.weatherRenderPipeline != null) QualitySettings.renderPipeline = settings.weatherRenderPipeline;
            previousSkybox = RenderSettings.skybox;
            // The installed vendor sky renderer deliberately leaves assigned Unity skyboxes
            // alone. Taxi uses Weather Maker's generated sky, including at night.
            RenderSettings.skybox = null;
            adapterWasEnabled = weatherAdapter.enabled; weatherAdapter.enabled = true;
            random = new System.Random(71923); AutomaticWeather = settings.automaticWeather;
            Frozen = !settings.timeProgressionEnabled;
            clock.SetTimeScale(Mathf.Max(0, settings.timeScale));
            clock.SetTimeOfDayHours(settings.startingTime);
            clock.SetPaused(Frozen || owner.Paused);
            // Existing adapter drives the sole clock if no LwsGameClockCoordinator is present.
            weather.SetTimeScale(0);
            Period = TruckTaxiEnvironmentSettings.PeriodAt(clock.CurrentSnapshot.timeOfDayHours);
            clock.ClockChanged += OnClockChanged;
            weather.WeatherTransitionCompleted += OnWeatherCompleted;
            lightStates = new bool[nightLights.Length];
            for (int i = 0; i < nightLights.Length; i++) if (nightLights[i] != null) lightStates[i] = nightLights[i].enabled;
            initialized = true;
            ForceWeather(settings.startingWeather, false);
            Snow = GetComponent<TruckTaxiSnow>() ?? gameObject.AddComponent<TruckTaxiSnow>();
            Snow.Initialize(owner, this, settings);
            RefreshNightLights();
            return true;
        }
        private void Update()
        {
            if (!initialized) return;
            SetSessionPaused(host.Paused);
            if (!audioRootChecked && weatherAdapter.WeatherMakerRuntimeExists)
            {
                // Resolve the adapter's known runtime object once; audio routing is owned by the Taxi audio integration.
                var root = weatherAdapter.WeatherMakerRuntimeRoot;
                if (root != null)
                {
                    WeatherAudioRoot = root; audioRootChecked = true;
                    WeatherAudioRootAvailable?.Invoke(WeatherAudioRoot);
                }
            }
            if (!audioRoutingRegistered && WeatherAudioRoot != null && TruckTaxiAudioController.Instance != null)
            {
                // The existing audio controller keeps this registered subtree current for dynamic rain/thunder sources.
                TruckTaxiAudioController.Instance.RouteWorldTree(WeatherAudioRoot); audioRoutingRegistered = true;
            }
            if (AutomaticWeather && !Clock.CurrentSnapshot.paused && Clock.CurrentSnapshot.totalGameSeconds >= nextWeatherAt) NextWeather();
            if (!Clock.CurrentSnapshot.paused) Snow?.Tick(Time.deltaTime, CurrentTaxiWeatherId);
        }
        public void SetSessionPaused(bool paused) { if (initialized) Clock.SetPaused(paused || Frozen); }
        public void SetFrozen(bool frozen) { Frozen = frozen; SetSessionPaused(host != null && host.Paused); }
        public void SetTime(float hours) { Clock?.SetTimeOfDayHours(hours); }
        public void SetTimeScale(float scale) { if (float.IsFinite(scale)) Clock?.SetTimeScale(Mathf.Clamp(scale, 0, 600)); }
        public void AdvanceHour() { Clock?.AddHours(1); }
        public void SetAutomaticWeather(bool automatic) { AutomaticWeather = automatic; if (initialized) ScheduleNext(); }
        public bool ForceWeather(string presetId, bool turnOffAutomatic = true)
        {
            if (!initialized || !IsSupportedWeather(presetId)) return false;
            if (turnOffAutomatic) AutomaticWeather = false;
            var result = presetId == TruckTaxiSnow.BlizzardId
                ? Weather.RequestWeather(CreateBlizzardPreset(), Mathf.Max(5, settings.weatherTransitionSeconds))
                : Weather.RequestWeather(presetId, Mathf.Max(5, settings.weatherTransitionSeconds));
            LastDiagnostic = result.Message;
            if (result.Succeeded) CurrentTaxiWeatherId = presetId;
            ScheduleNext();
            if (!result.Succeeded) Debug.LogWarning("Taxi weather request: " + LastDiagnostic, this);
            return result.Succeeded;
        }
        public void NextWeather()
        {
            if (!initialized) return;
            string target = PickNextWeather(settings.weatherWeights, CurrentTaxiWeatherId, (float)random.NextDouble());
            ForceWeather(target, false);
        }
        public static bool IsSupportedWeather(string id) => id == LwsWeatherPresetCatalog.ClearId || id == LwsWeatherPresetCatalog.PartlyCloudyId ||
            id == LwsWeatherPresetCatalog.CloudyId || id == LwsWeatherPresetCatalog.OvercastId || id == LwsWeatherPresetCatalog.LightRainId ||
            id == LwsWeatherPresetCatalog.HeavyRainId || id == LwsWeatherPresetCatalog.ThunderstormId || id == LwsWeatherPresetCatalog.FogId ||
            id == LwsWeatherPresetCatalog.LightSnowId || id == LwsWeatherPresetCatalog.HeavySnowId || id == TruckTaxiSnow.BlizzardId;
        public static LwsWeatherPreset CreateBlizzardPreset()
        {
            if (!LwsWeatherPresetCatalog.TryGetBuiltInPreset(LwsWeatherPresetCatalog.HeavySnowId, out var preset))
                throw new InvalidOperationException("Installed LWS heavy-snow preset is required for Taxi blizzard.");
            preset.presetId = TruckTaxiSnow.BlizzardId;
            preset.displayName = "Blizzard";
            preset.precipitationIntensity01 = 1;
            preset.fogIntensity01 = .75f;
            preset.windSpeedMetersPerSecond = 18;
            preset.visibilityMeters = 400;
            return preset;
        }
        public static string PickNextWeather(TruckTaxiWeatherWeight[] weights, string current, float roll)
        {
            float total = 0;
            if (weights != null) foreach (var choice in weights)
                if (choice != null && IsSupportedWeather(choice.presetId) && choice.presetId != current && float.IsFinite(choice.weight)) total += Mathf.Max(0, choice.weight);
            if (total <= 0) return IsSupportedWeather(current) ? current : LwsWeatherPresetCatalog.ClearId;
            float remaining = Mathf.Clamp01(roll) * total;
            string last = current;
            foreach (var choice in weights)
            {
                if (choice == null || !IsSupportedWeather(choice.presetId) || choice.presetId == current || !float.IsFinite(choice.weight) || choice.weight <= 0) continue;
                last = choice.presetId; remaining -= choice.weight;
                if (remaining <= 0) return last;
            }
            return last;
        }
        private void ScheduleNext()
        {
            float minimum = Mathf.Max(.25f, settings.weatherDurationGameHours.x);
            float duration = Mathf.Lerp(minimum, Mathf.Max(minimum, settings.weatherDurationGameHours.y), (float)random.NextDouble());
            nextWeatherAt = Clock.CurrentSnapshot.totalGameSeconds + duration * 3600;
        }
        private void OnClockChanged(LwsGameClockSnapshot snapshot)
        {
            var next = TruckTaxiEnvironmentSettings.PeriodAt(snapshot.timeOfDayHours);
            if (Period == next) return;
            Period = next; RefreshNightLights();
            if (next == TruckTaxiDayPeriod.Sunset) Emit(TruckTaxiDialogueCategory.SunsetReaction);
            else if (next == TruckTaxiDayPeriod.Night) Emit(TruckTaxiDialogueCategory.NightReaction);
        }
        private void OnWeatherCompleted(LwsWeatherSnapshot snapshot)
        {
            if (lastWeatherId == snapshot.weatherPresetId) return;
            lastWeatherId = snapshot.weatherPresetId;
            Emit(TruckTaxiDialogueCategory.WeatherChanged);
            if (snapshot.weatherPresetId == TruckTaxiSnow.BlizzardId) Emit(TruckTaxiDialogueCategory.BlizzardReaction);
            else if (snapshot.weatherPresetId == LwsWeatherPresetCatalog.LightSnowId ||
                snapshot.weatherPresetId == LwsWeatherPresetCatalog.HeavySnowId) Emit(TruckTaxiDialogueCategory.SnowReaction);
            else if (snapshot.condition == LwsWeatherCondition.Thunderstorm) Emit(TruckTaxiDialogueCategory.StormReaction);
            else if (snapshot.precipitationType == LwsPrecipitationType.Rain) Emit(TruckTaxiDialogueCategory.RainReaction);
            else if (snapshot.condition == LwsWeatherCondition.Fog) Emit(TruckTaxiDialogueCategory.FogReaction);
        }
        private void Emit(TruckTaxiDialogueCategory category)
        {
            EnvironmentEvent?.Invoke(category);
            if (host.Session?.HasPassenger == true) host.Passengers?.Dialogue?.Speak(host.Session.Passenger, category, host.Session,
                settings.ReactionFor(category, host.Session.Passenger), 12);
        }
        private void RefreshNightLights()
        {
            bool dark = Period == TruckTaxiDayPeriod.Night || Period == TruckTaxiDayPeriod.LateNight || Period == TruckTaxiDayPeriod.Dawn;
            foreach (var light in nightLights) if (light != null) light.enabled = dark;
        }
        private void OnDestroy()
        {
            if (!initialized) return;
            Clock.ClockChanged -= OnClockChanged; Weather.WeatherTransitionCompleted -= OnWeatherCompleted;
            Clock.SetDateTime(new LwsGameDateTime(previousClock.year, previousClock.month, previousClock.day, previousClock.hour, previousClock.minute, previousClock.second));
            Clock.SetTimeScale(previousClock.timeScale); Clock.SetPaused(previousClock.paused);
            Weather.SetState(previousWeather); Weather.SetTimeScale(previousWeatherScale);
            if (weatherAdapter != null) weatherAdapter.enabled = adapterWasEnabled;
            if (QualitySettings.renderPipeline == settings.weatherRenderPipeline) QualitySettings.renderPipeline = previousRenderPipeline;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene() == gameObject.scene && RenderSettings.skybox == null)
                RenderSettings.skybox = previousSkybox;
            for (int i = 0; i < nightLights.Length; i++) if (nightLights[i] != null) nightLights[i].enabled = lightStates[i];
        }
    }
}
