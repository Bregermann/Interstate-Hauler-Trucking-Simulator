using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiExtremeStage { Moving, Cleanup }

    public readonly struct TruckTaxiExtremeWeatherProfile
    {
        public TruckTaxiExtremeWeatherProfile(string weatherPresetId, float windMetersPerSecond,
            float visibilityMeters, float rainIntensity01)
        { WeatherPresetId = weatherPresetId; WindMetersPerSecond = windMetersPerSecond;
          VisibilityMeters = visibilityMeters; RainIntensity01 = rainIntensity01; }
        public string WeatherPresetId { get; }
        public float WindMetersPerSecond { get; }
        public float VisibilityMeters { get; }
        public float RainIntensity01 { get; }
    }

    public readonly struct TruckTaxiExtremeSnapshot
    {
        public TruckTaxiExtremeSnapshot(TruckTaxiExtremeKind kind, string zoneId, string regionId,
            TruckTaxiExtremeStage stage, DateTime endsAt, Vector3 position, float radiusMeters,
            bool loadedNearby, bool roadHazard)
        { Kind = kind; ZoneId = zoneId; RegionId = regionId; Stage = stage; EndsAt = endsAt;
          Position = position; RadiusMeters = radiusMeters; LoadedNearby = loadedNearby; RoadHazard = roadHazard; }
        public TruckTaxiExtremeKind Kind { get; }
        public string ZoneId { get; }
        public string RegionId { get; }
        public TruckTaxiExtremeStage Stage { get; }
        public DateTime EndsAt { get; }
        public Vector3 Position { get; }
        public float RadiusMeters { get; }
        public bool LoadedNearby { get; }
        // A visible warning / traversable debris, never a routing closure.
        public bool RoadHazard { get; }
    }

    public readonly struct TruckTaxiExtremeWarning
    {
        public TruckTaxiExtremeWarning(TruckTaxiExtremeKind kind, string zoneId, string text,
            DateTime issuedAt, DateTime expiresAt)
        { Kind = kind; ZoneId = zoneId; Text = text; IssuedAt = issuedAt; ExpiresAt = expiresAt; }
        public TruckTaxiExtremeKind Kind { get; }
        public string ZoneId { get; }
        public string Text { get; }
        public DateTime IssuedAt { get; }
        public DateTime ExpiresAt { get; }
    }

    public static class TruckTaxiExtremeRules
    {
        public static TimeSpan Duration(TruckTaxiExtremeKind kind)
        {
            switch (kind)
            {
                case TruckTaxiExtremeKind.Tornado: return TimeSpan.FromMinutes(20);
                case TruckTaxiExtremeKind.Hurricane: return TimeSpan.FromHours(6);
                case TruckTaxiExtremeKind.Earthquake: return TimeSpan.FromMinutes(2);
                default: return TimeSpan.FromMinutes(12);
            }
        }

        public static TimeSpan Cleanup(TruckTaxiExtremeKind kind) =>
            kind == TruckTaxiExtremeKind.Avalanche || kind == TruckTaxiExtremeKind.Mudslide
                ? TimeSpan.FromMinutes(30) : TimeSpan.Zero;

        public static float DailyChance(TruckTaxiExtremeKind kind)
        {
            switch (kind)
            {
                case TruckTaxiExtremeKind.Hurricane: return .002f;
                case TruckTaxiExtremeKind.Tornado: return .006f;
                case TruckTaxiExtremeKind.Earthquake: return .001f;
                default: return .004f;
            }
        }

        public static bool NaturalEligible(TruckTaxiExtremeKind kind, bool severeStorm,
            bool heavySnow, float continuousHeavyRainHours)
        {
            switch (kind)
            {
                case TruckTaxiExtremeKind.Tornado:
                case TruckTaxiExtremeKind.Hurricane: return severeStorm;
                case TruckTaxiExtremeKind.Avalanche: return heavySnow;
                case TruckTaxiExtremeKind.Mudslide: return continuousHeavyRainHours >= 2;
                default: return true; // Earthquakes are never inferred from weather.
            }
        }

        public static float CappedForce(float requestedNewtons, float maximumNewtons) =>
            Mathf.Clamp(float.IsFinite(requestedNewtons) ? requestedNewtons : 0, 0,
                Mathf.Clamp(maximumNewtons, 0, 5000));
    }
}
