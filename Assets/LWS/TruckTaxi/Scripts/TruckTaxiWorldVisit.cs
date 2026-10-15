using System;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public static class TruckTaxiWorldCatalog
    {
        public const string Demo = TruckTaxiMainMenu.VibinSceneName;
        public const string Edenborough = TruckTaxiEdenboroughLayout.SceneName;
        public static readonly string[] Scenes = { Demo, Edenborough };
        public static bool Contains(string scene) => scene == Demo || scene == Edenborough;
        public static string Label(string scene) => scene == Demo ? "Demo City" : scene == Edenborough ? "Edenborough" : "Unknown region";
        public static string Other(string scene) => scene == Demo ? Edenborough : scene == Edenborough ? Demo : null;
        public static string WorldId(string scene) => scene == Edenborough ? TruckTaxiEdenboroughLayout.WorldId :
            scene == Demo ? LwsWorldResumeCatalog.TruckTaxiWorldId : null;
    }

    // Bounded optional career semantics in the existing Vibin participant, not a storage system.
    [Serializable]
    public sealed class TruckTaxiWorldVisit
    {
        public string scene;
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public double gameSeconds;
        public float clockRate = 60;
        public TruckTaxiEnvironmentSnapshot environment;
        public TruckTaxiExtremeSaveState extremes;
        public TruckTaxiPersistentWorkArea workArea;
        public LwsSaveParticipantState weather, roadCondition;
        public static bool Valid(TruckTaxiWorldVisit value) => value != null && TruckTaxiWorldCatalog.Contains(value.scene) &&
            float.IsFinite(value.position.x) && float.IsFinite(value.position.y) && float.IsFinite(value.position.z) &&
            float.IsFinite(value.rotation.x) && float.IsFinite(value.rotation.y) && float.IsFinite(value.rotation.z) && float.IsFinite(value.rotation.w) &&
            Mathf.Abs(Quaternion.Dot(value.rotation, value.rotation) - 1) < .01f && double.IsFinite(value.gameSeconds) && value.gameSeconds >= 0 &&
            float.IsFinite(value.clockRate) && value.clockRate >= 0 && TruckTaxiEnvironmentSnapshot.Valid(value.environment) &&
            TruckTaxiExtremeSaveState.Valid(value.extremes) &&
            (value.weather == null || value.weather.participantId == "lws.weather.semantic") &&
            (value.roadCondition == null || value.roadCondition.participantId == "lws.road-condition.semantic");
        public static bool ValidVisits(TruckTaxiWorldVisit[] visits)
        {
            if (visits == null) return true;
            if (visits.Length > 2) return false;
            for (int i = 0; i < visits.Length; i++)
            {
                if (!Valid(visits[i])) return false;
                for (int j = 0; j < i; j++) if (visits[j].scene == visits[i].scene) return false;
            }
            return true;
        }
        public static void AdvanceSnow(TruckTaxiEnvironmentSnapshot environment, double elapsedGameSeconds, float clockRate,
            TruckTaxiEnvironmentSettings settings, TruckTaxiWeatherCondition? observedCondition = null, float temperatureC = 0)
        {
            var snow = environment?.snow;
            if (snow == null || settings == null || environment.frozen || !double.IsFinite(elapsedGameSeconds) ||
                !float.IsFinite(clockRate) || elapsedGameSeconds <= 0 || clockRate <= 0) return;
            float seconds = (float)Math.Min(86400, elapsedGameSeconds / clockRate);
            string name = environment.forecast?.automatic == false ? environment.forecast.manualCondition.ToString() :
                observedCondition?.ToString() ?? environment.forecast?.manualCondition.ToString() ?? "Clear";
            float rate = name == "Blizzard" ? settings.blizzardMetersPerSecond : name == "HeavySnow" ? settings.heavySnowMetersPerSecond :
                name == "LightSnow" || name == "Snow" ? settings.lightSnowMetersPerSecond : -settings.snowMeltMetersPerSecond *
                TruckTaxiSnow.MeltRateMultiplier(temperatureC, name == "Rain" || name == "HeavyRain" || name == "Thunderstorm");
            float amount = rate * seconds;
            snow.backgroundDepth = Mathf.Clamp(snow.backgroundDepth + amount, 0, snow.maximumDepth);
            for (int i = 0; i < snow.depths.Length; i++) snow.depths[i] = Mathf.Clamp(snow.depths[i] + amount, 0, snow.maximumDepth);
        }
    }
}
