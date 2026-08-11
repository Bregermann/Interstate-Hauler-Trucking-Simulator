using System;
using System.Reflection;
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

        public static LwsWeatherState Clear => new LwsWeatherState
        {
            weatherId = "clear",
            precipitationType = LwsPrecipitationType.None,
            visibilityMeters = 20000f
        };
    }

    public interface ILwsWeatherCoordinator : ILwsService
    {
        LwsWeatherState CurrentState { get; }
        void SetState(LwsWeatherState state);
    }

    public sealed class LwsWeatherCoordinator : ILwsWeatherCoordinator
    {
        public string ServiceId => "lws.weather";
        public LwsWeatherState CurrentState { get; private set; }

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            CurrentState = LwsWeatherState.Clear;
            return LwsServiceResult.Success("LWS weather coordinator initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            CurrentState = LwsWeatherState.Clear;
            return LwsServiceResult.Success("LWS weather coordinator shut down.");
        }

        public void SetState(LwsWeatherState state)
        {
            CurrentState = state;
        }
    }

    [DisallowMultipleComponent]
    public sealed class LwsWeatherMakerWeatheradeAdapter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour weatherMakerBehaviour;
        [SerializeField] private MonoBehaviour rainCoverageBehaviour;
        [SerializeField] private MonoBehaviour snowCoverageBehaviour;

        public bool CanReachWeatherMaker()
        {
            return weatherMakerBehaviour != null || ResolveWeatherMakerInstance() != null;
        }

        public bool CanReachWeatherade()
        {
            return rainCoverageBehaviour != null || snowCoverageBehaviour != null;
        }

        public void ApplyWeatherState(LwsWeatherState state)
        {
            // Prompt 002 only defines the ownership bridge. Prompt 003+ will bind
            // concrete Weather Maker profiles and Weatherade coverage values after
            // render-pipeline setup is complete.
            object weatherMaker = weatherMakerBehaviour != null ? weatherMakerBehaviour : ResolveWeatherMakerInstance();
            MethodInfo cloneProfiles = weatherMaker?.GetType().GetMethod("CloneProfiles", BindingFlags.Instance | BindingFlags.Public);
            if (cloneProfiles != null)
            {
                // Presence check only. No profile mutation in Prompt 002.
            }
        }

        private static object ResolveWeatherMakerInstance()
        {
            Type type = Type.GetType("DigitalRuby.WeatherMaker.WeatherMakerScript, Assembly-CSharp-firstpass")
                ?? Type.GetType("DigitalRuby.WeatherMaker.WeatherMakerScript, Assembly-CSharp");
            PropertyInfo instance = type?.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            return instance?.GetValue(null);
        }
    }
}
