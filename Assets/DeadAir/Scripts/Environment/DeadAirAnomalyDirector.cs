using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-180)]
    [DisallowMultipleComponent]
    public sealed class DeadAirAnomalyDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class AnomalyEffect
        {
            public string effectId = "DA_ANOMALY";
            public DeadAirAnomalyKind kind = DeadAirAnomalyKind.None;
            public string weatherPresetId = LwsWeatherPresetCatalog.FogId;
            public float transitionSeconds = 4f;
            public bool instant;
            [Range(0f, 1f)] public float fogDensity01 = 0.35f;
            public float timeOfDayHours = 23.4f;
            [TextArea] public string gpsInstructionOverride;
            public DeadAirGpsArrow gpsArrow = DeadAirGpsArrow.Straight;
        }

        private readonly HashSet<string> _activeEffects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ILwsWeatherService _weatherService;

        public IReadOnlyCollection<string> ActiveEffects => _activeEffects;
        public string LastAppliedEffect { get; private set; } = string.Empty;

        public void ApplyEffect(AnomalyEffect effect)
        {
            if (effect == null || string.IsNullOrWhiteSpace(effect.effectId))
            {
                return;
            }

            ResolveServices();
            _activeEffects.Add(effect.effectId);
            LastAppliedEffect = effect.effectId;

            switch (effect.kind)
            {
                case DeadAirAnomalyKind.WeatherPreset:
                    _weatherService?.RequestWeather(effect.weatherPresetId, effect.transitionSeconds, effect.instant);
                    break;
                case DeadAirAnomalyKind.Fog:
                    RenderSettings.fog = true;
                    RenderSettings.fogDensity = Mathf.Lerp(0.005f, 0.09f, Mathf.Clamp01(effect.fogDensity01));
                    break;
                case DeadAirAnomalyKind.TimeOfDay:
                    _weatherService?.SetTimeOfDayHours(effect.timeOfDayHours);
                    break;
                case DeadAirAnomalyKind.GpsCorruption:
                    DeadAirGameManager.Instance?.GpsDirector?.SetGpsInstruction(effect.gpsInstructionOverride, effect.gpsArrow, 805f, true);
                    DeadAirGameManager.Instance?.GpsDirector?.SetCorrupted(true);
                    break;
                case DeadAirAnomalyKind.Lightning:
                case DeadAirAnomalyKind.DashboardOverride:
                case DeadAirAnomalyKind.AudioStatic:
                case DeadAirAnomalyKind.TrafficHint:
                    Debug.Log($"[Dead Air] Anomaly hook applied: {effect.kind} ({effect.effectId}).", this);
                    break;
            }
        }

        public void ClearAllAnomalies()
        {
            _activeEffects.Clear();
            LastAppliedEffect = string.Empty;
            RenderSettings.fog = false;
            DeadAirGameManager.Instance?.GpsDirector?.ResetGps();
        }

        private void ResolveServices()
        {
            if (_weatherService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _weatherService);
        }
    }
}
