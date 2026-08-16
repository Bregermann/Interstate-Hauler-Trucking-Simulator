using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsPrecipitationType
    {
        None,
        Rain,
        Snow,
        Mixed
    }

    public enum LwsWeatherCondition
    {
        Clear,
        PartlyCloudy,
        Cloudy,
        Overcast,
        LightRain,
        HeavyRain,
        Thunderstorm,
        LightSnow,
        HeavySnow,
        Fog
    }

    public interface ILwsWeatherState
    {
        string WeatherId { get; }
        LwsPrecipitationType PrecipitationType { get; }
        float PrecipitationIntensity { get; }
        float TemperatureCelsius { get; }
        float Wetness { get; }
        float SnowAmount { get; }
        Vector3 WindVelocity { get; }
        float VisibilityMeters { get; }
        long WorldTimeTicks { get; }
    }

    [Serializable]
    public struct LwsWeatherState : ILwsWeatherState
    {
        public string weatherId;
        public LwsPrecipitationType precipitationType;
        public float precipitationIntensity;
        public float temperatureCelsius;
        public float wetness;
        public float snowAmount;
        public Vector3 windVelocity;
        public float visibilityMeters;
        public long worldTimeTicks;

        public string WeatherId => weatherId;
        public LwsPrecipitationType PrecipitationType => precipitationType;
        public float PrecipitationIntensity => precipitationIntensity;
        public float TemperatureCelsius => temperatureCelsius;
        public float Wetness => wetness;
        public float SnowAmount => snowAmount;
        public Vector3 WindVelocity => windVelocity;
        public float VisibilityMeters => visibilityMeters;
        public long WorldTimeTicks => worldTimeTicks;

        public static LwsWeatherState Clear => LwsWeatherSnapshot.Clear.ToLegacyState();
    }

    [Serializable]
    public struct LwsWeatherPreset
    {
        public string presetId;
        public string displayName;
        public LwsWeatherCondition condition;
        public LwsPrecipitationType precipitationType;
        [Range(0f, 1f)] public float precipitationIntensity01;
        [Range(0f, 1f)] public float cloudCover01;
        [Range(0f, 1f)] public float fogIntensity01;
        public float windSpeedMetersPerSecond;
        public Vector3 windDirectionWorld;
        public bool lightningActive;
        [Range(0f, 1f)] public float stormIntensity01;
        public float ambientTemperatureC;
        public float visibilityMeters;
        public string weatherMakerProfileName;
        public string weatherMakerProfilePath;
        public float defaultTransitionSeconds;
        public string notes;

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                message = "Weather preset ID is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                message = $"Weather preset {presetId} is missing a display name.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(weatherMakerProfileName))
            {
                message = $"Weather preset {presetId} is missing a Weather Maker profile name.";
                return false;
            }

            if (precipitationType == LwsPrecipitationType.None && precipitationIntensity01 > 0.001f)
            {
                message = $"Weather preset {presetId} has precipitation intensity but no precipitation type.";
                return false;
            }

            if (precipitationType != LwsPrecipitationType.None && precipitationIntensity01 <= 0.001f)
            {
                message = $"Weather preset {presetId} has precipitation type {precipitationType} but no intensity.";
                return false;
            }

            message = $"Weather preset {presetId} is valid.";
            return true;
        }

        public LwsWeatherSnapshot ToSnapshot(float timeOfDayHours)
        {
            return LwsWeatherSnapshot.FromPreset(this, timeOfDayHours);
        }
    }

    [Serializable]
    public struct LwsWeatherSnapshot : ILwsWeatherState
    {
        public string weatherPresetId;
        public LwsWeatherCondition condition;
        public LwsPrecipitationType precipitationType;
        [Range(0f, 1f)] public float precipitationIntensity01;
        [Range(0f, 1f)] public float cloudCover01;
        [Range(0f, 1f)] public float fogIntensity01;
        public float windSpeedMetersPerSecond;
        public Vector3 windDirectionWorld;
        public Vector3 windVelocity;
        public bool lightningActive;
        [Range(0f, 1f)] public float stormIntensity01;
        public float ambientTemperatureC;
        public float visibilityMeters;
        public float timeOfDayHours;
        [Range(0f, 1f)] public float daylight01;
        public bool isDay;
        public bool isNight;
        public bool transitioning;
        [Range(0f, 1f)] public float transitionProgress01;
        public string transitionTargetPresetId;
        public long worldTimeTicks;

        public string WeatherId => weatherPresetId;
        public LwsPrecipitationType PrecipitationType => precipitationType;
        public float PrecipitationIntensity => precipitationIntensity01;
        public float TemperatureCelsius => ambientTemperatureC;
        public float Wetness => 0f;
        public float SnowAmount => 0f;
        public Vector3 WindVelocity => windVelocity;
        public float VisibilityMeters => visibilityMeters;
        public long WorldTimeTicks => worldTimeTicks;
        public float Daylight01 => daylight01;
        public bool IsDay => isDay;
        public bool IsNight => isNight;

        public static LwsWeatherSnapshot Clear => new LwsWeatherSnapshot
        {
            weatherPresetId = LwsWeatherPresetCatalog.ClearId,
            condition = LwsWeatherCondition.Clear,
            precipitationType = LwsPrecipitationType.None,
            precipitationIntensity01 = 0f,
            cloudCover01 = 0.05f,
            fogIntensity01 = 0f,
            windSpeedMetersPerSecond = 2f,
            windDirectionWorld = Vector3.forward,
            windVelocity = Vector3.forward * 2f,
            lightningActive = false,
            stormIntensity01 = 0f,
            ambientTemperatureC = 18f,
            visibilityMeters = 24000f,
            timeOfDayHours = 12f,
            daylight01 = 1f,
            isDay = true,
            isNight = false,
            transitioning = false,
            transitionProgress01 = 1f,
            transitionTargetPresetId = LwsWeatherPresetCatalog.ClearId,
            worldTimeTicks = DateTime.UtcNow.Ticks
        };

        public static LwsWeatherSnapshot FromPreset(LwsWeatherPreset preset, float timeOfDayHours)
        {
            Vector3 direction = preset.windDirectionWorld.sqrMagnitude > 0.0001f
                ? preset.windDirectionWorld.normalized
                : Vector3.forward;

            var snapshot = new LwsWeatherSnapshot
            {
                weatherPresetId = preset.presetId,
                condition = preset.condition,
                precipitationType = preset.precipitationType,
                precipitationIntensity01 = preset.precipitationIntensity01,
                cloudCover01 = preset.cloudCover01,
                fogIntensity01 = preset.fogIntensity01,
                windSpeedMetersPerSecond = Mathf.Max(0f, preset.windSpeedMetersPerSecond),
                windDirectionWorld = direction,
                windVelocity = direction * Mathf.Max(0f, preset.windSpeedMetersPerSecond),
                lightningActive = preset.lightningActive,
                stormIntensity01 = preset.stormIntensity01,
                ambientTemperatureC = preset.ambientTemperatureC,
                visibilityMeters = Mathf.Max(20f, preset.visibilityMeters),
                timeOfDayHours = timeOfDayHours,
                transitionTargetPresetId = preset.presetId,
                transitionProgress01 = 1f,
                worldTimeTicks = DateTime.UtcNow.Ticks
            };

            snapshot.ClampAndRefreshDerived();
            return snapshot;
        }

        public void ClampAndRefreshDerived()
        {
            weatherPresetId = string.IsNullOrWhiteSpace(weatherPresetId) ? LwsWeatherPresetCatalog.ClearId : weatherPresetId;
            precipitationIntensity01 = Mathf.Clamp01(precipitationIntensity01);
            cloudCover01 = Mathf.Clamp01(cloudCover01);
            fogIntensity01 = Mathf.Clamp01(fogIntensity01);
            stormIntensity01 = Mathf.Clamp01(stormIntensity01);
            windSpeedMetersPerSecond = Mathf.Max(0f, windSpeedMetersPerSecond);
            visibilityMeters = Mathf.Max(20f, visibilityMeters);
            timeOfDayHours = NormalizeHours(timeOfDayHours);
            daylight01 = ComputeDaylight01(timeOfDayHours);
            isDay = daylight01 > 0.2f;
            isNight = daylight01 < 0.15f;
            windDirectionWorld = windDirectionWorld.sqrMagnitude > 0.0001f ? windDirectionWorld.normalized : Vector3.forward;
            windVelocity = windDirectionWorld * windSpeedMetersPerSecond;
            transitionProgress01 = Mathf.Clamp01(transitionProgress01);
            transitionTargetPresetId = string.IsNullOrWhiteSpace(transitionTargetPresetId) ? weatherPresetId : transitionTargetPresetId;
            worldTimeTicks = worldTimeTicks == 0 ? DateTime.UtcNow.Ticks : worldTimeTicks;
        }

        public LwsWeatherState ToLegacyState()
        {
            return new LwsWeatherState
            {
                weatherId = weatherPresetId,
                precipitationType = precipitationType,
                precipitationIntensity = precipitationIntensity01,
                temperatureCelsius = ambientTemperatureC,
                wetness = 0f,
                snowAmount = 0f,
                windVelocity = windVelocity,
                visibilityMeters = visibilityMeters,
                worldTimeTicks = worldTimeTicks
            };
        }

        public static LwsWeatherSnapshot Lerp(LwsWeatherSnapshot from, LwsWeatherSnapshot to, float progress01)
        {
            progress01 = Mathf.Clamp01(progress01);
            LwsWeatherSnapshot result = progress01 >= 0.5f ? to : from;
            result.weatherPresetId = progress01 >= 0.999f ? to.weatherPresetId : from.weatherPresetId;
            result.condition = progress01 >= 0.5f ? to.condition : from.condition;
            result.precipitationType = progress01 >= 0.5f ? to.precipitationType : from.precipitationType;
            result.precipitationIntensity01 = Mathf.Lerp(from.precipitationIntensity01, to.precipitationIntensity01, progress01);
            result.cloudCover01 = Mathf.Lerp(from.cloudCover01, to.cloudCover01, progress01);
            result.fogIntensity01 = Mathf.Lerp(from.fogIntensity01, to.fogIntensity01, progress01);
            result.windSpeedMetersPerSecond = Mathf.Lerp(from.windSpeedMetersPerSecond, to.windSpeedMetersPerSecond, progress01);
            result.windDirectionWorld = Vector3.Slerp(from.windDirectionWorld, to.windDirectionWorld, progress01);
            result.lightningActive = progress01 >= 0.85f && to.lightningActive;
            result.stormIntensity01 = Mathf.Lerp(from.stormIntensity01, to.stormIntensity01, progress01);
            result.ambientTemperatureC = Mathf.Lerp(from.ambientTemperatureC, to.ambientTemperatureC, progress01);
            result.visibilityMeters = Mathf.Lerp(from.visibilityMeters, to.visibilityMeters, progress01);
            result.timeOfDayHours = to.timeOfDayHours;
            result.transitioning = progress01 < 0.999f;
            result.transitionProgress01 = progress01;
            result.transitionTargetPresetId = to.weatherPresetId;
            result.worldTimeTicks = DateTime.UtcNow.Ticks;
            result.ClampAndRefreshDerived();
            return result;
        }

        public static float NormalizeHours(float hours)
        {
            if (float.IsNaN(hours) || float.IsInfinity(hours))
            {
                return 12f;
            }

            hours %= 24f;
            return hours < 0f ? hours + 24f : hours;
        }

        public static float ComputeDaylight01(float timeOfDayHours)
        {
            float hour = NormalizeHours(timeOfDayHours);
            if (hour >= 6f && hour <= 12f)
            {
                return Mathf.Lerp(0.15f, 1f, (hour - 6f) / 6f);
            }

            if (hour > 12f && hour <= 18f)
            {
                return Mathf.Lerp(1f, 0.15f, (hour - 12f) / 6f);
            }

            if (hour > 18f && hour <= 20f)
            {
                return Mathf.Lerp(0.15f, 0f, (hour - 18f) / 2f);
            }

            if (hour >= 4f && hour < 6f)
            {
                return Mathf.Lerp(0f, 0.15f, (hour - 4f) / 2f);
            }

            return 0f;
        }
    }

    public interface ILwsWeatherRuntimeAdapter
    {
        bool WeatherMakerAvailable { get; }
        bool WeatherCameraBound { get; }
        string ActiveCameraName { get; }
        string AdapterStatus { get; }
        bool ApplyWeatherPreset(LwsWeatherPreset preset, float transitionSeconds, bool instant);
        bool ApplyTimeOfDayHours(float hours);
        bool ApplyTimeScale(float timeScale);
        bool ApplyQualityTier(LwsRenderQualityTier tier);
    }

    public interface ILwsWeatherService : ILwsService
    {
        LwsWeatherSnapshot CurrentSnapshot { get; }
        LwsWeatherState CurrentState { get; }
        IReadOnlyList<LwsWeatherPreset> Presets { get; }
        string LastRequest { get; }
        string LastError { get; }
        float TimeScale { get; }
        bool WeatherMakerAvailable { get; }
        ILwsWeatherRuntimeAdapter ActiveAdapter { get; }

        event Action<LwsWeatherSnapshot, LwsWeatherSnapshot, float> WeatherTransitionStarted;
        event Action<LwsWeatherSnapshot> WeatherChanged;
        event Action<LwsWeatherSnapshot> WeatherTransitionCompleted;
        event Action<LwsWeatherSnapshot> PrecipitationStarted;
        event Action<LwsWeatherSnapshot> PrecipitationStopped;
        event Action<LwsWeatherSnapshot> DayNightChanged;

        bool TryGetPreset(string presetId, out LwsWeatherPreset preset);
        LwsServiceResult RequestWeather(string presetId, float transitionSeconds = -1f, bool instant = false);
        LwsServiceResult RequestWeather(LwsWeatherPreset preset, float transitionSeconds = -1f, bool instant = false);
        void SetTimeOfDayHours(float hours);
        void SetTimeScale(float timeScale);
        void Tick(float deltaTime);
        bool AttachAdapter(ILwsWeatherRuntimeAdapter adapter);
        void DetachAdapter(ILwsWeatherRuntimeAdapter adapter);
        void SetState(LwsWeatherState state);
    }

    public interface ILwsWeatherCoordinator : ILwsWeatherService
    {
    }

    public sealed class LwsWeatherCoordinator : ILwsWeatherCoordinator
    {
        private static LwsWeatherCoordinator _activeService;

        private readonly List<LwsWeatherPreset> _presets = new List<LwsWeatherPreset>();
        private readonly Dictionary<string, LwsWeatherPreset> _byId = new Dictionary<string, LwsWeatherPreset>(StringComparer.OrdinalIgnoreCase);

        private LwsWeatherSnapshot _current;
        private LwsWeatherSnapshot _transitionStart;
        private LwsWeatherSnapshot _transitionTarget;
        private float _transitionElapsed;
        private float _transitionDuration;
        private bool _wasNight;

        public string ServiceId => "lws.weather";
        public LwsWeatherSnapshot CurrentSnapshot => _current;
        public LwsWeatherState CurrentState => _current.ToLegacyState();
        public IReadOnlyList<LwsWeatherPreset> Presets => _presets;
        public string LastRequest { get; private set; } = "None";
        public string LastError { get; private set; } = string.Empty;
        public float TimeScale { get; private set; } = 1f;
        public bool WeatherMakerAvailable => ActiveAdapter != null && ActiveAdapter.WeatherMakerAvailable;
        public ILwsWeatherRuntimeAdapter ActiveAdapter { get; private set; }

        public event Action<LwsWeatherSnapshot, LwsWeatherSnapshot, float> WeatherTransitionStarted;
        public event Action<LwsWeatherSnapshot> WeatherChanged;
        public event Action<LwsWeatherSnapshot> WeatherTransitionCompleted;
        public event Action<LwsWeatherSnapshot> PrecipitationStarted;
        public event Action<LwsWeatherSnapshot> PrecipitationStopped;
        public event Action<LwsWeatherSnapshot> DayNightChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            if (_activeService != null && _activeService != this)
            {
                return LwsServiceResult.Failure("Only one LWS weather service may be initialized at a time.");
            }

            _activeService = this;
            _presets.Clear();
            _byId.Clear();
            foreach (LwsWeatherPreset preset in LwsWeatherPresetCatalog.CreateDefaultPresets())
            {
                RegisterPreset(preset);
            }

            _current = LwsWeatherSnapshot.Clear;
            _transitionStart = _current;
            _transitionTarget = _current;
            _wasNight = _current.IsNight;
            LastRequest = "Initialized with Clear.";
            LastError = string.Empty;
            return LwsServiceResult.Success("LWS weather service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            ActiveAdapter = null;
            _presets.Clear();
            _byId.Clear();
            _current = LwsWeatherSnapshot.Clear;
            _transitionStart = _current;
            _transitionTarget = _current;
            LastRequest = "Shutdown.";
            LastError = string.Empty;
            if (_activeService == this)
            {
                _activeService = null;
            }

            return LwsServiceResult.Success("LWS weather service shut down.");
        }

        public bool TryGetPreset(string presetId, out LwsWeatherPreset preset)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                preset = default;
                return false;
            }

            return _byId.TryGetValue(presetId, out preset);
        }

        public LwsServiceResult RequestWeather(string presetId, float transitionSeconds = -1f, bool instant = false)
        {
            if (!TryGetPreset(presetId, out LwsWeatherPreset preset))
            {
                LastError = $"Unknown weather preset ID: {presetId}";
                return LwsServiceResult.Failure(LastError);
            }

            return RequestWeather(preset, transitionSeconds, instant);
        }

        public LwsServiceResult RequestWeather(LwsWeatherPreset preset, float transitionSeconds = -1f, bool instant = false)
        {
            if (!preset.Validate(out string validationMessage))
            {
                LastError = validationMessage;
                return LwsServiceResult.Failure(validationMessage);
            }

            float duration = instant ? 0f : (transitionSeconds >= 0f ? transitionSeconds : preset.defaultTransitionSeconds);
            duration = Mathf.Max(0f, duration);
            LastRequest = $"{preset.presetId} ({(instant ? "instant" : $"{duration:0.0}s")})";
            LastError = string.Empty;

            bool adapterSucceeded = ActiveAdapter == null || ActiveAdapter.ApplyWeatherPreset(preset, duration, instant);
            if (!adapterSucceeded && ActiveAdapter != null)
            {
                LastError = ActiveAdapter.AdapterStatus;
            }

            LwsWeatherSnapshot target = preset.ToSnapshot(_current.timeOfDayHours);
            if (duration <= 0.001f)
            {
                bool wasPrecipitating = IsPrecipitating(_current);
                _current = target;
                _current.transitioning = false;
                _current.transitionProgress01 = 1f;
                WeatherChanged?.Invoke(_current);
                PublishPrecipitationEvents(wasPrecipitating, _current);
                PublishDayNightEvent();
                return adapterSucceeded
                    ? LwsServiceResult.Success($"Weather changed to {preset.displayName}.")
                    : LwsServiceResult.Failure(LastError);
            }

            _transitionStart = _current;
            _transitionTarget = target;
            _transitionElapsed = 0f;
            _transitionDuration = duration;
            _current.transitioning = true;
            _current.transitionProgress01 = 0f;
            _current.transitionTargetPresetId = target.weatherPresetId;
            WeatherTransitionStarted?.Invoke(_transitionStart, _transitionTarget, duration);
            return adapterSucceeded
                ? LwsServiceResult.Success($"Weather transition started: {preset.displayName}.")
                : LwsServiceResult.Failure(LastError);
        }

        public void SetTimeOfDayHours(float hours)
        {
            bool wasNight = _current.IsNight;
            _current.timeOfDayHours = LwsWeatherSnapshot.NormalizeHours(hours);
            _current.ClampAndRefreshDerived();
            _transitionStart.timeOfDayHours = _current.timeOfDayHours;
            _transitionTarget.timeOfDayHours = _current.timeOfDayHours;
            _transitionStart.ClampAndRefreshDerived();
            _transitionTarget.ClampAndRefreshDerived();
            ActiveAdapter?.ApplyTimeOfDayHours(_current.timeOfDayHours);
            WeatherChanged?.Invoke(_current);
            if (wasNight != _current.IsNight)
            {
                DayNightChanged?.Invoke(_current);
            }
        }

        public void SetTimeScale(float timeScale)
        {
            TimeScale = Mathf.Max(0f, timeScale);
            ActiveAdapter?.ApplyTimeScale(TimeScale);
        }

        public void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);
            AdvanceTime(deltaTime);

            if (!_current.transitioning || _transitionDuration <= 0.001f)
            {
                PublishDayNightEvent();
                return;
            }

            bool wasPrecipitating = IsPrecipitating(_current);
            _transitionElapsed += deltaTime;
            float progress = _transitionElapsed / _transitionDuration;
            _current = LwsWeatherSnapshot.Lerp(_transitionStart, _transitionTarget, progress);
            WeatherChanged?.Invoke(_current);
            PublishPrecipitationEvents(wasPrecipitating, _current);

            if (progress >= 1f)
            {
                _current = _transitionTarget;
                _current.transitioning = false;
                _current.transitionProgress01 = 1f;
                _current.ClampAndRefreshDerived();
                WeatherChanged?.Invoke(_current);
                WeatherTransitionCompleted?.Invoke(_current);
            }

            PublishDayNightEvent();
        }

        public bool AttachAdapter(ILwsWeatherRuntimeAdapter adapter)
        {
            if (adapter == null)
            {
                return false;
            }

            if (ActiveAdapter != null && !ReferenceEquals(ActiveAdapter, adapter))
            {
                LastError = "Duplicate weather runtime adapter rejected.";
                return false;
            }

            ActiveAdapter = adapter;
            adapter.ApplyTimeOfDayHours(_current.timeOfDayHours);
            adapter.ApplyTimeScale(TimeScale);
            if (TryGetPreset(_current.weatherPresetId, out LwsWeatherPreset preset))
            {
                adapter.ApplyWeatherPreset(preset, 0f, true);
            }

            return true;
        }

        public void DetachAdapter(ILwsWeatherRuntimeAdapter adapter)
        {
            if (ReferenceEquals(ActiveAdapter, adapter))
            {
                ActiveAdapter = null;
            }
        }

        public void SetState(LwsWeatherState state)
        {
            LwsWeatherSnapshot snapshot = _current;
            snapshot.weatherPresetId = string.IsNullOrWhiteSpace(state.weatherId) ? LwsWeatherPresetCatalog.ClearId : state.weatherId;
            snapshot.precipitationType = state.precipitationType;
            snapshot.precipitationIntensity01 = state.precipitationIntensity;
            snapshot.ambientTemperatureC = state.temperatureCelsius;
            snapshot.windVelocity = state.windVelocity;
            snapshot.windSpeedMetersPerSecond = state.windVelocity.magnitude;
            snapshot.windDirectionWorld = state.windVelocity.sqrMagnitude > 0.0001f ? state.windVelocity.normalized : Vector3.forward;
            snapshot.visibilityMeters = state.visibilityMeters;
            snapshot.worldTimeTicks = state.worldTimeTicks;
            snapshot.transitioning = false;
            snapshot.transitionProgress01 = 1f;
            snapshot.ClampAndRefreshDerived();
            _current = snapshot;
            WeatherChanged?.Invoke(_current);
        }

        private void RegisterPreset(LwsWeatherPreset preset)
        {
            if (!preset.Validate(out _))
            {
                return;
            }

            _presets.Add(preset);
            _byId[preset.presetId] = preset;
        }

        private void AdvanceTime(float deltaTime)
        {
            if (TimeScale <= 0f || deltaTime <= 0f)
            {
                return;
            }

            _current.timeOfDayHours = LwsWeatherSnapshot.NormalizeHours(_current.timeOfDayHours + (deltaTime * TimeScale / 3600f));
            _current.ClampAndRefreshDerived();
            _transitionStart.timeOfDayHours = _current.timeOfDayHours;
            _transitionTarget.timeOfDayHours = _current.timeOfDayHours;
            _transitionStart.ClampAndRefreshDerived();
            _transitionTarget.ClampAndRefreshDerived();
        }

        private void PublishPrecipitationEvents(bool wasPrecipitating, LwsWeatherSnapshot current)
        {
            bool isPrecipitating = IsPrecipitating(current);
            if (!wasPrecipitating && isPrecipitating)
            {
                PrecipitationStarted?.Invoke(current);
            }
            else if (wasPrecipitating && !isPrecipitating)
            {
                PrecipitationStopped?.Invoke(current);
            }
        }

        private void PublishDayNightEvent()
        {
            if (_wasNight != _current.IsNight)
            {
                _wasNight = _current.IsNight;
                DayNightChanged?.Invoke(_current);
            }
        }

        private static bool IsPrecipitating(LwsWeatherSnapshot snapshot)
        {
            return snapshot.precipitationType != LwsPrecipitationType.None && snapshot.precipitationIntensity01 > 0.001f;
        }
    }

    public static class LwsWeatherPresetCatalog
    {
        public const string ClearId = "clear";
        public const string PartlyCloudyId = "partly_cloudy";
        public const string CloudyId = "cloudy";
        public const string OvercastId = "overcast";
        public const string LightRainId = "light_rain";
        public const string HeavyRainId = "heavy_rain";
        public const string ThunderstormId = "thunderstorm";
        public const string LightSnowId = "light_snow";
        public const string HeavySnowId = "heavy_snow";
        public const string FogId = "fog";

        public static IReadOnlyList<LwsWeatherPreset> CreateDefaultPresets()
        {
            return new[]
            {
                Create(ClearId, "Clear", LwsWeatherCondition.Clear, LwsPrecipitationType.None, 0f, 0.05f, 0f, 2f, 18f, 24000f, false, 0f, "WeatherMakerProfile_Clear", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_Clear.asset"),
                Create(PartlyCloudyId, "Partly Cloudy", LwsWeatherCondition.PartlyCloudy, LwsPrecipitationType.None, 0f, 0.35f, 0.02f, 3f, 18f, 22000f, false, 0f, "WeatherMakerProfile_LightCloudsScattered", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_LightCloudsScattered.asset"),
                Create(CloudyId, "Cloudy", LwsWeatherCondition.Cloudy, LwsPrecipitationType.None, 0f, 0.62f, 0.05f, 4f, 17f, 18000f, false, 0f, "WeatherMakerProfile_MediumHeavyClouds", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_MediumHeavyClouds.asset"),
                Create(OvercastId, "Overcast", LwsWeatherCondition.Overcast, LwsPrecipitationType.None, 0f, 0.9f, 0.12f, 5f, 16f, 14000f, false, 0f, "WeatherMakerProfile_OvercastClouds", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_OvercastClouds.asset"),
                Create(LightRainId, "Light Rain", LwsWeatherCondition.LightRain, LwsPrecipitationType.Rain, 0.28f, 0.75f, 0.18f, 6f, 14f, 9000f, false, 0.15f, "WeatherMakerProfile_LightRain", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_LightRain.asset"),
                Create(HeavyRainId, "Heavy Rain", LwsWeatherCondition.HeavyRain, LwsPrecipitationType.Rain, 0.82f, 0.95f, 0.32f, 9f, 13f, 5500f, false, 0.35f, "WeatherMakerProfile_HeavyRain", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_HeavyRain.asset"),
                Create(ThunderstormId, "Thunderstorm", LwsWeatherCondition.Thunderstorm, LwsPrecipitationType.Rain, 0.95f, 1f, 0.45f, 14f, 13f, 4200f, true, 1f, "WeatherMakerProfile_Storm", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_Storm.asset"),
                Create(LightSnowId, "Light Snow", LwsWeatherCondition.LightSnow, LwsPrecipitationType.Snow, 0.32f, 0.72f, 0.18f, 4f, -2f, 9000f, false, 0.15f, "WeatherMakerProfile_LightSnow", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Snow/WeatherMakerProfile_LightSnow.asset"),
                Create(HeavySnowId, "Heavy Snow", LwsWeatherCondition.HeavySnow, LwsPrecipitationType.Snow, 0.82f, 0.95f, 0.36f, 8f, -5f, 3600f, false, 0.5f, "WeatherMakerProfile_HeavySnow", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Snow/WeatherMakerProfile_HeavySnow.asset"),
                Create(FogId, "Fog", LwsWeatherCondition.Fog, LwsPrecipitationType.None, 0f, 0.5f, 0.88f, 2f, 11f, 800f, false, 0f, "WeatherMakerProfile_MediumFog", "Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Fog/WeatherMakerProfile_MediumFog.asset")
            };
        }

        public static bool TryGetBuiltInPreset(string presetId, out LwsWeatherPreset preset)
        {
            IReadOnlyList<LwsWeatherPreset> presets = CreateDefaultPresets();
            for (int i = 0; i < presets.Count; i++)
            {
                if (string.Equals(presets[i].presetId, presetId, StringComparison.OrdinalIgnoreCase))
                {
                    preset = presets[i];
                    return true;
                }
            }

            preset = default;
            return false;
        }

        private static LwsWeatherPreset Create(
            string presetId,
            string displayName,
            LwsWeatherCondition condition,
            LwsPrecipitationType precipitationType,
            float precipitationIntensity,
            float cloudCover,
            float fogIntensity,
            float windSpeed,
            float temperatureC,
            float visibilityMeters,
            bool lightning,
            float stormIntensity,
            string profileName,
            string profilePath)
        {
            return new LwsWeatherPreset
            {
                presetId = presetId,
                displayName = displayName,
                condition = condition,
                precipitationType = precipitationType,
                precipitationIntensity01 = precipitationIntensity,
                cloudCover01 = cloudCover,
                fogIntensity01 = fogIntensity,
                windSpeedMetersPerSecond = windSpeed,
                windDirectionWorld = new Vector3(0.25f, 0f, 1f).normalized,
                lightningActive = lightning,
                stormIntensity01 = stormIntensity,
                ambientTemperatureC = temperatureC,
                visibilityMeters = visibilityMeters,
                weatherMakerProfileName = profileName,
                weatherMakerProfilePath = profilePath,
                defaultTransitionSeconds = 10f,
                notes = "Prompt 012 validation preset. Atmospheric only; no road traction or accumulation changes."
            };
        }
    }
}
