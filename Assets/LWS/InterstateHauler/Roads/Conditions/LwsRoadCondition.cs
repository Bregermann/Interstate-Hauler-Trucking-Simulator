using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsRoadConditionType
    {
        Dry,
        Damp,
        Wet,
        StandingWater,
        LightSnow,
        PackedSnow,
        Ice,
        Mixed
    }

    public enum LwsRoadConditionOverrideMode
    {
        AutoFromWeather,
        ForceDry,
        ForceWet,
        ForceStandingWater,
        ForceSnow,
        ForcePackedSnow,
        ForceIce
    }

    [Serializable]
    public struct LwsRoadConditionGripProfile
    {
        public LwsRoadConditionType condition;
        [Range(0f, 1.25f)] public float longitudinalGripMultiplier01;
        [Range(0f, 1.25f)] public float lateralGripMultiplier01;
        [Range(0f, 1.25f)] public float brakingGripMultiplier01;
        [Range(0.5f, 3f)] public float rollingResistanceMultiplier;

        public static LwsRoadConditionGripProfile Create(
            LwsRoadConditionType condition,
            float longitudinal,
            float lateral,
            float braking,
            float rollingResistance)
        {
            return new LwsRoadConditionGripProfile
            {
                condition = condition,
                longitudinalGripMultiplier01 = Mathf.Clamp(longitudinal, 0f, 1.25f),
                lateralGripMultiplier01 = Mathf.Clamp(lateral, 0f, 1.25f),
                brakingGripMultiplier01 = Mathf.Clamp(braking, 0f, 1.25f),
                rollingResistanceMultiplier = Mathf.Clamp(rollingResistance, 0.5f, 3f)
            };
        }
    }

    [Serializable]
    public sealed class LwsRoadConditionProfile
    {
        public string profileId = "road_condition.default";
        public string displayName = "Default Road Condition Physics";
        public float wettingRatePerSecond = 0.004f;
        public float heavyRainMultiplier = 2.4f;
        public float standingWaterBuildRatePerSecond = 0.0015f;
        public float standingWaterDrainRatePerSecond = 0.0008f;
        public float dryingRatePerSecond = 0.0012f;
        public float daylightDryingMultiplier = 0.75f;
        public float windDryingPerMeterSecond = 0.015f;
        public float snowAccumulationRatePerSecond = 0.0025f;
        public float heavySnowMultiplier = 2.2f;
        public float snowCompactionRatePerSecond = 0.0007f;
        public float snowMeltRatePerSecond = 0.0018f;
        public float iceFormationRatePerSecond = 0.0012f;
        public float iceMeltRatePerSecond = 0.0025f;
        public float freezingTemperatureC = 0f;
        public float snowAccumulationMaxTemperatureC = 1.5f;
        public float meltTemperatureC = 1.5f;
        public float surfaceTemperatureFollowRateCPerSecond = 2.5f;
        [Range(0f, 1f)] public float standingWaterWetnessThreshold = 0.7f;
        [Range(0f, 1f)] public float snowPackThreshold = 0.55f;
        public float hydroplaningSpeedMetersPerSecond = 24f;
        public List<LwsRoadConditionGripProfile> gripProfiles = new List<LwsRoadConditionGripProfile>();

        public static LwsRoadConditionProfile Default()
        {
            return new LwsRoadConditionProfile
            {
                gripProfiles = new List<LwsRoadConditionGripProfile>
                {
                    LwsRoadConditionGripProfile.Create(LwsRoadConditionType.Dry, 1.00f, 1.00f, 1.00f, 1.00f),
                    LwsRoadConditionGripProfile.Create(LwsRoadConditionType.Damp, 0.92f, 0.95f, 0.90f, 1.03f),
                    LwsRoadConditionGripProfile.Create(LwsRoadConditionType.Wet, 0.72f, 0.78f, 0.68f, 1.08f),
                    LwsRoadConditionGripProfile.Create(LwsRoadConditionType.StandingWater, 0.50f, 0.58f, 0.45f, 1.18f),
                    LwsRoadConditionGripProfile.Create(LwsRoadConditionType.LightSnow, 0.42f, 0.50f, 0.38f, 1.22f),
                    LwsRoadConditionGripProfile.Create(LwsRoadConditionType.PackedSnow, 0.30f, 0.36f, 0.27f, 1.28f),
                    LwsRoadConditionGripProfile.Create(LwsRoadConditionType.Ice, 0.14f, 0.18f, 0.12f, 1.10f),
                    LwsRoadConditionGripProfile.Create(LwsRoadConditionType.Mixed, 0.32f, 0.40f, 0.30f, 1.24f)
                }
            };
        }

        public LwsRoadConditionGripProfile GetGripProfile(LwsRoadConditionType condition)
        {
            if (gripProfiles != null)
            {
                for (int i = 0; i < gripProfiles.Count; i++)
                {
                    if (gripProfiles[i].condition == condition)
                    {
                        return gripProfiles[i];
                    }
                }
            }

            return LwsRoadConditionGripProfile.Create(LwsRoadConditionType.Dry, 1f, 1f, 1f, 1f);
        }

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                message = "Road condition profile ID is required.";
                return false;
            }

            if (wettingRatePerSecond < 0f || dryingRatePerSecond < 0f || snowAccumulationRatePerSecond < 0f ||
                snowMeltRatePerSecond < 0f || iceFormationRatePerSecond < 0f || iceMeltRatePerSecond < 0f)
            {
                message = "Road condition accumulation rates must be non-negative.";
                return false;
            }

            if (meltTemperatureC < freezingTemperatureC)
            {
                message = "Road condition melt threshold should be at or above the freezing threshold.";
                return false;
            }

            LwsRoadConditionGripProfile dry = GetGripProfile(LwsRoadConditionType.Dry);
            LwsRoadConditionGripProfile wet = GetGripProfile(LwsRoadConditionType.Wet);
            LwsRoadConditionGripProfile snow = GetGripProfile(LwsRoadConditionType.LightSnow);
            LwsRoadConditionGripProfile ice = GetGripProfile(LwsRoadConditionType.Ice);
            if (!(wet.longitudinalGripMultiplier01 < dry.longitudinalGripMultiplier01 &&
                  snow.longitudinalGripMultiplier01 < wet.longitudinalGripMultiplier01 &&
                  ice.longitudinalGripMultiplier01 < snow.longitudinalGripMultiplier01))
            {
                message = "Grip hierarchy must be Dry > Wet > Snow > Ice.";
                return false;
            }

            message = $"Road condition profile {profileId} is valid.";
            return true;
        }
    }

    [Serializable]
    public struct LwsRoadConditionSnapshot
    {
        public string roadId;
        public string segmentId;
        public string edgeId;
        public LwsRoadSurfaceType baseSurfaceType;
        public LwsRoadConditionType condition;
        [Range(0f, 1f)] public float wetness01;
        [Range(0f, 1f)] public float standingWater01;
        [Range(0f, 1f)] public float snowDepth01;
        [Range(0f, 1f)] public float packedSnow01;
        [Range(0f, 1f)] public float ice01;
        public float surfaceTemperatureC;
        [Range(0f, 1.25f)] public float longitudinalGripMultiplier01;
        [Range(0f, 1.25f)] public float lateralGripMultiplier01;
        [Range(0f, 1.25f)] public float brakingGripMultiplier01;
        [Range(0.5f, 3f)] public float rollingResistanceMultiplier;
        [Range(0f, 1f)] public float hydroplaningRisk01;
        public float vehicleSpeedMetersPerSecond;
        public bool transitioning;
        public bool forced;
        public string lastWeatherPresetId;
        public long updatedAtTicks;
        public long versionTicks;

        public string StateKey => string.IsNullOrWhiteSpace(edgeId)
            ? (string.IsNullOrWhiteSpace(roadId) ? "offroad.default" : roadId)
            : edgeId;

        public string MajorGameplayState
        {
            get
            {
                switch (condition)
                {
                    case LwsRoadConditionType.Dry:
                    case LwsRoadConditionType.Damp:
                        return "DRY";
                    case LwsRoadConditionType.Wet:
                    case LwsRoadConditionType.StandingWater:
                        return "WET";
                    case LwsRoadConditionType.LightSnow:
                    case LwsRoadConditionType.PackedSnow:
                        return "SNOW";
                    case LwsRoadConditionType.Ice:
                    case LwsRoadConditionType.Mixed:
                        return "ICE";
                    default:
                        return "DRY";
                }
            }
        }

        public static LwsRoadConditionSnapshot CreateDry(
            string roadId,
            string segmentId,
            string edgeId,
            LwsRoadSurfaceType baseSurfaceType,
            float surfaceTemperatureC,
            LwsRoadConditionProfile profile)
        {
            var snapshot = new LwsRoadConditionSnapshot
            {
                roadId = string.IsNullOrWhiteSpace(roadId) ? "offroad.default" : roadId,
                segmentId = segmentId ?? string.Empty,
                edgeId = string.IsNullOrWhiteSpace(edgeId) ? "offroad.default" : edgeId,
                baseSurfaceType = baseSurfaceType,
                condition = LwsRoadConditionType.Dry,
                surfaceTemperatureC = surfaceTemperatureC,
                lastWeatherPresetId = LwsWeatherPresetCatalog.ClearId,
                updatedAtTicks = DateTime.UtcNow.Ticks,
                versionTicks = DateTime.UtcNow.Ticks
            };

            return LwsRoadConditionSimulation.ApplyGripProfile(snapshot, profile ?? LwsRoadConditionProfile.Default());
        }

        public void Clamp()
        {
            roadId = string.IsNullOrWhiteSpace(roadId) ? "offroad.default" : roadId;
            segmentId = segmentId ?? string.Empty;
            edgeId = string.IsNullOrWhiteSpace(edgeId) ? roadId : edgeId;
            wetness01 = Mathf.Clamp01(wetness01);
            standingWater01 = Mathf.Clamp01(standingWater01);
            snowDepth01 = Mathf.Clamp01(snowDepth01);
            packedSnow01 = Mathf.Clamp01(packedSnow01);
            ice01 = Mathf.Clamp01(ice01);
            longitudinalGripMultiplier01 = Mathf.Clamp(longitudinalGripMultiplier01, 0f, 1.25f);
            lateralGripMultiplier01 = Mathf.Clamp(lateralGripMultiplier01, 0f, 1.25f);
            brakingGripMultiplier01 = Mathf.Clamp(brakingGripMultiplier01, 0f, 1.25f);
            rollingResistanceMultiplier = rollingResistanceMultiplier <= 0f ? 1f : Mathf.Clamp(rollingResistanceMultiplier, 0.5f, 3f);
            hydroplaningRisk01 = Mathf.Clamp01(hydroplaningRisk01);
            vehicleSpeedMetersPerSecond = Mathf.Max(0f, vehicleSpeedMetersPerSecond);
            lastWeatherPresetId = string.IsNullOrWhiteSpace(lastWeatherPresetId) ? LwsWeatherPresetCatalog.ClearId : lastWeatherPresetId;
            updatedAtTicks = updatedAtTicks == 0 ? DateTime.UtcNow.Ticks : updatedAtTicks;
            versionTicks = versionTicks == 0 ? updatedAtTicks : versionTicks;
        }
    }

    [Serializable]
    public struct LwsRoadConditionRuntimeState
    {
        public string roadId;
        public string segmentId;
        public string edgeId;
        public LwsRoadSurfaceType baseSurfaceType;
        public float wetness01;
        public float standingWater01;
        public float snowDepth01;
        public float packedSnow01;
        public float ice01;
        public float surfaceTemperatureC;
        public long updatedAtTicks;

        public static LwsRoadConditionRuntimeState FromSnapshot(LwsRoadConditionSnapshot snapshot)
        {
            return new LwsRoadConditionRuntimeState
            {
                roadId = snapshot.roadId,
                segmentId = snapshot.segmentId,
                edgeId = snapshot.edgeId,
                baseSurfaceType = snapshot.baseSurfaceType,
                wetness01 = snapshot.wetness01,
                standingWater01 = snapshot.standingWater01,
                snowDepth01 = snapshot.snowDepth01,
                packedSnow01 = snapshot.packedSnow01,
                ice01 = snapshot.ice01,
                surfaceTemperatureC = snapshot.surfaceTemperatureC,
                updatedAtTicks = snapshot.updatedAtTicks
            };
        }
    }

    public interface ILwsRoadConditionVisualAdapter
    {
        string AdapterId { get; }
        bool IsAvailable { get; }
        string Status { get; }
        void ApplyRoadCondition(LwsRoadConditionSnapshot snapshot);
        void ApplyQualityTier(LwsRenderQualityTier tier);
    }

    public interface ILwsRoadConditionPhysicsAdapter
    {
        string AdapterId { get; }
        bool IsAvailable { get; }
        string Status { get; }
        int BoundWheelCount { get; }
        void ApplyRoadCondition(LwsRoadConditionSnapshot snapshot);
        void RestoreDryBaseline();
    }

    public interface ILwsRoadConditionService : ILwsService
    {
        LwsRoadConditionSnapshot CurrentSnapshot { get; }
        LwsRoadConditionProfile Profile { get; }
        LwsRoadConditionOverrideMode Mode { get; }
        float AccumulationSpeedMultiplier { get; }
        bool TemperatureOverrideEnabled { get; }
        float SurfaceTemperatureOverrideC { get; }
        string CurrentRoadKey { get; }

        event Action<LwsRoadConditionSnapshot, LwsRoadConditionSnapshot> RoadConditionChanged;
        event Action<LwsRoadConditionSnapshot> RoadWetnessChanged;
        event Action<LwsRoadConditionSnapshot> SnowAccumulationStarted;
        event Action<LwsRoadConditionSnapshot> SnowAccumulationStopped;
        event Action<LwsRoadConditionSnapshot> IceFormed;
        event Action<LwsRoadConditionSnapshot> IceCleared;
        event Action<LwsRoadConditionSnapshot> HydroplaningRiskChanged;

        void SetProfile(LwsRoadConditionProfile profile);
        void UpdatePlayerRoad(Vector3 worldPosition, Vector3 forward);
        void SetVehicleSpeedMetersPerSecond(float speedMetersPerSecond);
        void Tick(float deltaTimeSeconds);
        bool TryGetRoadCondition(string edgeOrRoadId, out LwsRoadConditionSnapshot snapshot);
        void SetAutoFromWeather();
        void ForceCondition(LwsRoadConditionOverrideMode mode);
        void ResetCurrentRoad();
        void SetAccumulationSpeedMultiplier(float multiplier);
        void SetSurfaceTemperatureOverride(bool enabled, float temperatureC);
        bool AttachVisualAdapter(ILwsRoadConditionVisualAdapter adapter);
        void DetachVisualAdapter(ILwsRoadConditionVisualAdapter adapter);
        bool AttachPhysicsAdapter(ILwsRoadConditionPhysicsAdapter adapter);
        void DetachPhysicsAdapter(ILwsRoadConditionPhysicsAdapter adapter);
    }

    public static class LwsRoadConditionSimulation
    {
        public static LwsRoadConditionSnapshot StepAuto(
            LwsRoadConditionSnapshot previous,
            LwsWeatherSnapshot weather,
            LwsRoadConditionProfile profile,
            float deltaTimeSeconds,
            float vehicleSpeedMetersPerSecond)
        {
            profile ??= LwsRoadConditionProfile.Default();
            deltaTimeSeconds = Mathf.Clamp(deltaTimeSeconds, 0f, 10f);

            LwsRoadConditionSnapshot next = previous;
            next.forced = false;
            next.vehicleSpeedMetersPerSecond = Mathf.Max(0f, vehicleSpeedMetersPerSecond);
            next.lastWeatherPresetId = string.IsNullOrWhiteSpace(weather.weatherPresetId)
                ? LwsWeatherPresetCatalog.ClearId
                : weather.weatherPresetId;

            float targetSurfaceTemperature = ComputeTargetSurfaceTemperature(weather);
            if (Mathf.Approximately(next.surfaceTemperatureC, 0f) && previous.versionTicks == 0)
            {
                next.surfaceTemperatureC = targetSurfaceTemperature;
            }
            else
            {
                next.surfaceTemperatureC = Mathf.MoveTowards(
                    next.surfaceTemperatureC,
                    targetSurfaceTemperature,
                    Mathf.Max(0.01f, profile.surfaceTemperatureFollowRateCPerSecond) * deltaTimeSeconds);
            }

            bool raining = weather.precipitationType == LwsPrecipitationType.Rain || weather.precipitationType == LwsPrecipitationType.Mixed;
            bool snowing = weather.precipitationType == LwsPrecipitationType.Snow || weather.precipitationType == LwsPrecipitationType.Mixed;
            float intensity = Mathf.Clamp01(weather.precipitationIntensity01);

            if (raining && intensity > 0f)
            {
                float rainFactor = Mathf.Lerp(1f, Mathf.Max(1f, profile.heavyRainMultiplier), intensity);
                next.wetness01 += profile.wettingRatePerSecond * intensity * rainFactor * deltaTimeSeconds;
                if (next.wetness01 >= profile.standingWaterWetnessThreshold && intensity > 0.55f)
                {
                    next.standingWater01 += profile.standingWaterBuildRatePerSecond * intensity * rainFactor * deltaTimeSeconds;
                }
            }
            else
            {
                float dryingRate = ComputeDryingRate(weather, profile);
                next.wetness01 -= dryingRate * deltaTimeSeconds;
                next.standingWater01 -= Mathf.Max(profile.standingWaterDrainRatePerSecond, dryingRate * 0.35f) * deltaTimeSeconds;
            }

            if (snowing && intensity > 0f)
            {
                if (next.surfaceTemperatureC <= profile.snowAccumulationMaxTemperatureC)
                {
                    float snowFactor = Mathf.Lerp(1f, Mathf.Max(1f, profile.heavySnowMultiplier), intensity);
                    next.snowDepth01 += profile.snowAccumulationRatePerSecond * intensity * snowFactor * deltaTimeSeconds;
                }
                else
                {
                    next.wetness01 += profile.wettingRatePerSecond * 0.45f * intensity * deltaTimeSeconds;
                }
            }

            if (next.snowDepth01 > profile.snowPackThreshold)
            {
                float compact = profile.snowCompactionRatePerSecond * (next.snowDepth01 - profile.snowPackThreshold + 0.1f) * deltaTimeSeconds;
                compact = Mathf.Min(compact, next.snowDepth01);
                next.snowDepth01 -= compact * 0.45f;
                next.packedSnow01 += compact;
            }

            if (next.surfaceTemperatureC > profile.meltTemperatureC)
            {
                float warmth = Mathf.Clamp(next.surfaceTemperatureC - profile.meltTemperatureC, 0f, 15f);
                float meltRate = profile.snowMeltRatePerSecond * (1f + warmth * 0.25f + weather.daylight01 * 0.5f);
                float snowMelt = Mathf.Min(next.snowDepth01, meltRate * deltaTimeSeconds);
                float packedMelt = Mathf.Min(next.packedSnow01, meltRate * 0.55f * deltaTimeSeconds);
                next.snowDepth01 -= snowMelt;
                next.packedSnow01 -= packedMelt;
                next.wetness01 += (snowMelt + packedMelt) * 0.65f;
            }

            float moisture = next.wetness01 + next.standingWater01 + next.snowDepth01 * 0.25f + next.packedSnow01 * 0.35f;
            if (next.surfaceTemperatureC <= profile.freezingTemperatureC && moisture > 0.05f)
            {
                float coldFactor = Mathf.Clamp01((profile.freezingTemperatureC - next.surfaceTemperatureC + 1f) / 8f);
                float freeze = profile.iceFormationRatePerSecond * moisture * (0.35f + coldFactor) * deltaTimeSeconds;
                next.ice01 += freeze;
                next.wetness01 -= freeze * 0.55f;
                next.standingWater01 -= freeze * 0.35f;
            }
            else if (next.surfaceTemperatureC > profile.meltTemperatureC)
            {
                float warmth = Mathf.Clamp(next.surfaceTemperatureC - profile.meltTemperatureC, 0f, 15f);
                float melt = Mathf.Min(next.ice01, profile.iceMeltRatePerSecond * (1f + warmth * 0.35f) * deltaTimeSeconds);
                next.ice01 -= melt;
                next.wetness01 += melt * 0.8f;
            }

            next.condition = ResolveCondition(next);
            return FinalizeSnapshot(next, profile);
        }

        public static LwsRoadConditionSnapshot BuildForcedSnapshot(
            LwsRoadConditionSnapshot previous,
            LwsRoadConditionOverrideMode mode,
            LwsRoadConditionProfile profile)
        {
            profile ??= LwsRoadConditionProfile.Default();
            LwsRoadConditionSnapshot next = previous;
            next.forced = true;
            next.transitioning = true;

            switch (mode)
            {
                case LwsRoadConditionOverrideMode.ForceWet:
                    next.condition = LwsRoadConditionType.Wet;
                    next.wetness01 = 0.8f;
                    next.standingWater01 = 0f;
                    next.snowDepth01 = 0f;
                    next.packedSnow01 = 0f;
                    next.ice01 = 0f;
                    break;
                case LwsRoadConditionOverrideMode.ForceStandingWater:
                    next.condition = LwsRoadConditionType.StandingWater;
                    next.wetness01 = 1f;
                    next.standingWater01 = 0.75f;
                    next.snowDepth01 = 0f;
                    next.packedSnow01 = 0f;
                    next.ice01 = 0f;
                    break;
                case LwsRoadConditionOverrideMode.ForceSnow:
                    next.condition = LwsRoadConditionType.LightSnow;
                    next.wetness01 = 0.2f;
                    next.standingWater01 = 0f;
                    next.snowDepth01 = 0.7f;
                    next.packedSnow01 = 0.15f;
                    next.ice01 = 0f;
                    break;
                case LwsRoadConditionOverrideMode.ForcePackedSnow:
                    next.condition = LwsRoadConditionType.PackedSnow;
                    next.wetness01 = 0.1f;
                    next.standingWater01 = 0f;
                    next.snowDepth01 = 0.25f;
                    next.packedSnow01 = 0.75f;
                    next.ice01 = 0.05f;
                    break;
                case LwsRoadConditionOverrideMode.ForceIce:
                    next.condition = LwsRoadConditionType.Ice;
                    next.wetness01 = 0.1f;
                    next.standingWater01 = 0.05f;
                    next.snowDepth01 = 0f;
                    next.packedSnow01 = 0.05f;
                    next.ice01 = 0.8f;
                    break;
                case LwsRoadConditionOverrideMode.ForceDry:
                default:
                    next.condition = LwsRoadConditionType.Dry;
                    next.wetness01 = 0f;
                    next.standingWater01 = 0f;
                    next.snowDepth01 = 0f;
                    next.packedSnow01 = 0f;
                    next.ice01 = 0f;
                    next.hydroplaningRisk01 = 0f;
                    break;
            }

            return FinalizeSnapshot(next, profile);
        }

        public static LwsRoadConditionSnapshot ApplyGripProfile(
            LwsRoadConditionSnapshot snapshot,
            LwsRoadConditionProfile profile)
        {
            profile ??= LwsRoadConditionProfile.Default();
            LwsRoadConditionGripProfile grip = profile.GetGripProfile(snapshot.condition);
            float hydroLoss = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(snapshot.hydroplaningRisk01));
            snapshot.longitudinalGripMultiplier01 = Mathf.Clamp(grip.longitudinalGripMultiplier01 * hydroLoss, 0f, 1.25f);
            snapshot.lateralGripMultiplier01 = Mathf.Clamp(grip.lateralGripMultiplier01 * hydroLoss, 0f, 1.25f);
            snapshot.brakingGripMultiplier01 = Mathf.Clamp(grip.brakingGripMultiplier01 * hydroLoss, 0f, 1.25f);
            snapshot.rollingResistanceMultiplier = grip.rollingResistanceMultiplier;
            return snapshot;
        }

        public static LwsRoadConditionType ResolveCondition(LwsRoadConditionSnapshot snapshot)
        {
            if (snapshot.ice01 >= 0.2f && (snapshot.snowDepth01 + snapshot.packedSnow01) >= 0.15f)
            {
                return LwsRoadConditionType.Mixed;
            }

            if (snapshot.ice01 >= 0.18f)
            {
                return LwsRoadConditionType.Ice;
            }

            if (snapshot.packedSnow01 >= 0.4f)
            {
                return LwsRoadConditionType.PackedSnow;
            }

            if (snapshot.snowDepth01 >= 0.08f)
            {
                return LwsRoadConditionType.LightSnow;
            }

            if (snapshot.standingWater01 >= 0.1f)
            {
                return LwsRoadConditionType.StandingWater;
            }

            if (snapshot.wetness01 >= 0.45f)
            {
                return LwsRoadConditionType.Wet;
            }

            if (snapshot.wetness01 >= 0.05f)
            {
                return LwsRoadConditionType.Damp;
            }

            return LwsRoadConditionType.Dry;
        }

        private static LwsRoadConditionSnapshot FinalizeSnapshot(
            LwsRoadConditionSnapshot snapshot,
            LwsRoadConditionProfile profile)
        {
            snapshot.Clamp();
            snapshot.condition = ResolveCondition(snapshot);
            snapshot.hydroplaningRisk01 = ComputeHydroplaningRisk(snapshot, profile ?? LwsRoadConditionProfile.Default());
            snapshot = ApplyGripProfile(snapshot, profile);
            snapshot.updatedAtTicks = DateTime.UtcNow.Ticks;
            snapshot.versionTicks++;
            snapshot.Clamp();
            return snapshot;
        }

        private static float ComputeTargetSurfaceTemperature(LwsWeatherSnapshot weather)
        {
            float daylightWarming = Mathf.Lerp(-0.8f, 2.0f, Mathf.Clamp01(weather.daylight01));
            float cloudCooling = Mathf.Clamp01(weather.cloudCover01) * 0.65f;
            return weather.ambientTemperatureC + daylightWarming - cloudCooling;
        }

        private static float ComputeDryingRate(LwsWeatherSnapshot weather, LwsRoadConditionProfile profile)
        {
            float daylight = Mathf.Clamp01(weather.daylight01) * profile.daylightDryingMultiplier;
            float wind = Mathf.Clamp(weather.windSpeedMetersPerSecond, 0f, 30f) * profile.windDryingPerMeterSecond;
            float warm = Mathf.Max(0f, weather.ambientTemperatureC - profile.meltTemperatureC) * 0.015f;
            return profile.dryingRatePerSecond * (1f + daylight + wind + warm);
        }

        private static float ComputeHydroplaningRisk(LwsRoadConditionSnapshot snapshot, LwsRoadConditionProfile profile)
        {
            float speedRisk = Mathf.InverseLerp(
                Mathf.Max(1f, profile.hydroplaningSpeedMetersPerSecond),
                Mathf.Max(2f, profile.hydroplaningSpeedMetersPerSecond * 1.6f),
                snapshot.vehicleSpeedMetersPerSecond);
            return Mathf.Clamp01(snapshot.standingWater01 * speedRisk * (0.5f + snapshot.wetness01 * 0.5f));
        }
    }

    public sealed class LwsRoadConditionCoordinator : ILwsRoadConditionService
    {
        private static readonly string OffRoadKey = "offroad.default";
        private static LwsRoadConditionCoordinator _activeService;

        private readonly Dictionary<string, LwsRoadConditionSnapshot> _roadStates =
            new Dictionary<string, LwsRoadConditionSnapshot>(StringComparer.OrdinalIgnoreCase);

        private readonly List<ILwsRoadConditionVisualAdapter> _visualAdapters =
            new List<ILwsRoadConditionVisualAdapter>();

        private readonly List<ILwsRoadConditionPhysicsAdapter> _physicsAdapters =
            new List<ILwsRoadConditionPhysicsAdapter>();

        private ILwsWeatherService _weatherService;
        private ILwsRoadGraphService _roadGraphService;
        private ILwsNavigationService _navigationService;
        private LwsRoadConditionSnapshot _current;
        private float _vehicleSpeedMetersPerSecond;

        public string ServiceId => "lws.road.condition";
        public LwsRoadConditionSnapshot CurrentSnapshot => _current;
        public LwsRoadConditionProfile Profile { get; private set; } = LwsRoadConditionProfile.Default();
        public LwsRoadConditionOverrideMode Mode { get; private set; } = LwsRoadConditionOverrideMode.AutoFromWeather;
        public float AccumulationSpeedMultiplier { get; private set; } = 1f;
        public bool TemperatureOverrideEnabled { get; private set; }
        public float SurfaceTemperatureOverrideC { get; private set; }
        public string CurrentRoadKey { get; private set; } = OffRoadKey;

        public event Action<LwsRoadConditionSnapshot, LwsRoadConditionSnapshot> RoadConditionChanged;
        public event Action<LwsRoadConditionSnapshot> RoadWetnessChanged;
        public event Action<LwsRoadConditionSnapshot> SnowAccumulationStarted;
        public event Action<LwsRoadConditionSnapshot> SnowAccumulationStopped;
        public event Action<LwsRoadConditionSnapshot> IceFormed;
        public event Action<LwsRoadConditionSnapshot> IceCleared;
        public event Action<LwsRoadConditionSnapshot> HydroplaningRiskChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            if (_activeService != null && _activeService != this)
            {
                return LwsServiceResult.Failure("Only one LWS road condition service may be initialized at a time.");
            }

            _activeService = this;
            context.Registry.TryGet(out _weatherService);
            context.Registry.TryGet(out _roadGraphService);
            context.Registry.TryGet(out _navigationService);

            Profile = LwsRoadConditionProfile.Default();
            _roadStates.Clear();
            _current = LwsRoadConditionSnapshot.CreateDry(
                OffRoadKey,
                string.Empty,
                OffRoadKey,
                LwsRoadSurfaceType.AsphaltInterstate,
                (_weatherService?.CurrentSnapshot ?? LwsWeatherSnapshot.Clear).ambientTemperatureC,
                Profile);
            _roadStates[OffRoadKey] = _current;
            return LwsServiceResult.Success("LWS road condition service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            for (int i = 0; i < _physicsAdapters.Count; i++)
            {
                _physicsAdapters[i]?.RestoreDryBaseline();
            }

            _visualAdapters.Clear();
            _physicsAdapters.Clear();
            _roadStates.Clear();
            _weatherService = null;
            _roadGraphService = null;
            _navigationService = null;
            _current = default;
            if (_activeService == this)
            {
                _activeService = null;
            }

            return LwsServiceResult.Success("LWS road condition service shut down.");
        }

        public void SetProfile(LwsRoadConditionProfile profile)
        {
            if (profile == null || !profile.Validate(out _))
            {
                return;
            }

            Profile = profile;
            LwsRoadConditionSnapshot previous = _current;
            _current = LwsRoadConditionSimulation.ApplyGripProfile(_current, Profile);
            _roadStates[_current.StateKey] = _current;
            PublishAndApply(previous, _current, true);
        }

        public void UpdatePlayerRoad(Vector3 worldPosition, Vector3 forward)
        {
            RoadContext context = ResolveRoadContext(worldPosition, forward);
            string key = string.IsNullOrWhiteSpace(context.edgeId) ? OffRoadKey : context.edgeId;
            if (!_roadStates.TryGetValue(key, out LwsRoadConditionSnapshot snapshot))
            {
                snapshot = LwsRoadConditionSnapshot.CreateDry(
                    context.roadId,
                    context.segmentId,
                    key,
                    context.surfaceType,
                    _current.surfaceTemperatureC == 0f ? (_weatherService?.CurrentSnapshot ?? LwsWeatherSnapshot.Clear).ambientTemperatureC : _current.surfaceTemperatureC,
                    Profile);
                _roadStates[key] = snapshot;
            }

            if (!string.Equals(CurrentRoadKey, key, StringComparison.OrdinalIgnoreCase))
            {
                LwsRoadConditionSnapshot previous = _current;
                CurrentRoadKey = key;
                _current = snapshot;
                PublishAndApply(previous, _current, true);
            }
        }

        public void SetVehicleSpeedMetersPerSecond(float speedMetersPerSecond)
        {
            _vehicleSpeedMetersPerSecond = Mathf.Max(0f, speedMetersPerSecond);
        }

        public void Tick(float deltaTimeSeconds)
        {
            deltaTimeSeconds = Mathf.Max(0f, deltaTimeSeconds) * AccumulationSpeedMultiplier;
            if (deltaTimeSeconds <= 0f)
            {
                return;
            }

            LwsRoadConditionSnapshot previous = _current;
            LwsRoadConditionSnapshot next;
            if (Mode == LwsRoadConditionOverrideMode.AutoFromWeather)
            {
                LwsWeatherSnapshot weather = _weatherService?.CurrentSnapshot ?? LwsWeatherSnapshot.Clear;
                if (TemperatureOverrideEnabled)
                {
                    weather.ambientTemperatureC = SurfaceTemperatureOverrideC;
                    weather.ClampAndRefreshDerived();
                }

                next = LwsRoadConditionSimulation.StepAuto(previous, weather, Profile, deltaTimeSeconds, _vehicleSpeedMetersPerSecond);
            }
            else
            {
                next = LwsRoadConditionSimulation.BuildForcedSnapshot(previous, Mode, Profile);
                if (TemperatureOverrideEnabled)
                {
                    next.surfaceTemperatureC = SurfaceTemperatureOverrideC;
                    next = LwsRoadConditionSimulation.ApplyGripProfile(next, Profile);
                }
            }

            _roadStates[next.StateKey] = next;
            _current = next;
            PublishAndApply(previous, next, HasMeaningfulChange(previous, next));
        }

        public bool TryGetRoadCondition(string edgeOrRoadId, out LwsRoadConditionSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(edgeOrRoadId))
            {
                snapshot = default;
                return false;
            }

            if (_roadStates.TryGetValue(edgeOrRoadId, out snapshot))
            {
                return true;
            }

            foreach (KeyValuePair<string, LwsRoadConditionSnapshot> pair in _roadStates)
            {
                if (string.Equals(pair.Value.roadId, edgeOrRoadId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pair.Value.segmentId, edgeOrRoadId, StringComparison.OrdinalIgnoreCase))
                {
                    snapshot = pair.Value;
                    return true;
                }
            }

            snapshot = default;
            return false;
        }

        public void SetAutoFromWeather()
        {
            Mode = LwsRoadConditionOverrideMode.AutoFromWeather;
            LwsRoadConditionSnapshot previous = _current;
            _current.forced = false;
            PublishAndApply(previous, _current, true);
        }

        public void ForceCondition(LwsRoadConditionOverrideMode mode)
        {
            if (mode == LwsRoadConditionOverrideMode.AutoFromWeather)
            {
                SetAutoFromWeather();
                return;
            }

            Mode = mode;
            LwsRoadConditionSnapshot previous = _current;
            _current = LwsRoadConditionSimulation.BuildForcedSnapshot(_current, mode, Profile);
            _roadStates[_current.StateKey] = _current;
            PublishAndApply(previous, _current, true);
        }

        public void ResetCurrentRoad()
        {
            LwsRoadConditionSnapshot previous = _current;
            _current = LwsRoadConditionSnapshot.CreateDry(
                previous.roadId,
                previous.segmentId,
                previous.edgeId,
                previous.baseSurfaceType,
                (_weatherService?.CurrentSnapshot ?? LwsWeatherSnapshot.Clear).ambientTemperatureC,
                Profile);
            _roadStates[_current.StateKey] = _current;
            for (int i = 0; i < _physicsAdapters.Count; i++)
            {
                _physicsAdapters[i]?.RestoreDryBaseline();
            }

            PublishAndApply(previous, _current, true);
        }

        public void SetAccumulationSpeedMultiplier(float multiplier)
        {
            AccumulationSpeedMultiplier = Mathf.Clamp(multiplier, 0.01f, 120f);
        }

        public void SetSurfaceTemperatureOverride(bool enabled, float temperatureC)
        {
            TemperatureOverrideEnabled = enabled;
            SurfaceTemperatureOverrideC = temperatureC;
        }

        public bool AttachVisualAdapter(ILwsRoadConditionVisualAdapter adapter)
        {
            if (adapter == null || _visualAdapters.Contains(adapter))
            {
                return false;
            }

            _visualAdapters.Add(adapter);
            adapter.ApplyRoadCondition(_current);
            return true;
        }

        public void DetachVisualAdapter(ILwsRoadConditionVisualAdapter adapter)
        {
            _visualAdapters.Remove(adapter);
        }

        public bool AttachPhysicsAdapter(ILwsRoadConditionPhysicsAdapter adapter)
        {
            if (adapter == null || _physicsAdapters.Contains(adapter))
            {
                return false;
            }

            _physicsAdapters.Add(adapter);
            adapter.ApplyRoadCondition(_current);
            return true;
        }

        public void DetachPhysicsAdapter(ILwsRoadConditionPhysicsAdapter adapter)
        {
            if (_physicsAdapters.Remove(adapter))
            {
                adapter.RestoreDryBaseline();
            }
        }

        private void PublishAndApply(
            LwsRoadConditionSnapshot previous,
            LwsRoadConditionSnapshot next,
            bool applyAdapters)
        {
            if (applyAdapters)
            {
                for (int i = 0; i < _visualAdapters.Count; i++)
                {
                    _visualAdapters[i]?.ApplyRoadCondition(next);
                }

                for (int i = 0; i < _physicsAdapters.Count; i++)
                {
                    _physicsAdapters[i]?.ApplyRoadCondition(next);
                }
            }

            if (previous.condition != next.condition || !string.Equals(previous.StateKey, next.StateKey, StringComparison.OrdinalIgnoreCase))
            {
                RoadConditionChanged?.Invoke(previous, next);
            }

            if (Mathf.Abs(previous.wetness01 - next.wetness01) >= 0.05f)
            {
                RoadWetnessChanged?.Invoke(next);
            }

            if (previous.snowDepth01 <= 0.02f && next.snowDepth01 > 0.02f)
            {
                SnowAccumulationStarted?.Invoke(next);
            }
            else if (previous.snowDepth01 > 0.02f && next.snowDepth01 <= 0.02f)
            {
                SnowAccumulationStopped?.Invoke(next);
            }

            if (previous.ice01 <= 0.05f && next.ice01 > 0.05f)
            {
                IceFormed?.Invoke(next);
            }
            else if (previous.ice01 > 0.05f && next.ice01 <= 0.05f)
            {
                IceCleared?.Invoke(next);
            }

            if (Mathf.Abs(previous.hydroplaningRisk01 - next.hydroplaningRisk01) >= 0.1f)
            {
                HydroplaningRiskChanged?.Invoke(next);
            }
        }

        private bool HasMeaningfulChange(LwsRoadConditionSnapshot previous, LwsRoadConditionSnapshot next)
        {
            return previous.condition != next.condition ||
                   Mathf.Abs(previous.wetness01 - next.wetness01) >= 0.005f ||
                   Mathf.Abs(previous.standingWater01 - next.standingWater01) >= 0.005f ||
                   Mathf.Abs(previous.snowDepth01 - next.snowDepth01) >= 0.005f ||
                   Mathf.Abs(previous.packedSnow01 - next.packedSnow01) >= 0.005f ||
                   Mathf.Abs(previous.ice01 - next.ice01) >= 0.005f ||
                   Mathf.Abs(previous.hydroplaningRisk01 - next.hydroplaningRisk01) >= 0.02f ||
                   previous.forced != next.forced ||
                   !string.Equals(previous.StateKey, next.StateKey, StringComparison.OrdinalIgnoreCase);
        }

        private RoadContext ResolveRoadContext(Vector3 worldPosition, Vector3 forward)
        {
            LwsNavigationRuntimeState navigationState = _navigationService?.RuntimeState;
            if (navigationState != null && !string.IsNullOrWhiteSpace(navigationState.currentEdgeId) &&
                TryFindEdge(navigationState.currentEdgeId, out LwsRoadEdge navEdge))
            {
                return new RoadContext(navEdge.roadId, navEdge.segmentId, navEdge.edgeId, navEdge.surfaceType);
            }

            if (_roadGraphService != null && _roadGraphService.TryFindNearestRoad(worldPosition, 75f, out LwsRoadLookupResult lookup) && lookup.Found)
            {
                return new RoadContext(lookup.RoadId, lookup.SegmentId, lookup.EdgeId, lookup.SurfaceType);
            }

            return new RoadContext(OffRoadKey, string.Empty, OffRoadKey, LwsRoadSurfaceType.AsphaltInterstate);
        }

        private bool TryFindEdge(string edgeId, out LwsRoadEdge edge)
        {
            edge = null;
            LwsRoadGraph graph = _roadGraphService?.ActiveGraph;
            if (graph == null || graph.edges == null || string.IsNullOrWhiteSpace(edgeId))
            {
                return false;
            }

            for (int i = 0; i < graph.edges.Count; i++)
            {
                if (graph.edges[i] != null && string.Equals(graph.edges[i].edgeId, edgeId, StringComparison.OrdinalIgnoreCase))
                {
                    edge = graph.edges[i];
                    return true;
                }
            }

            return false;
        }

        private readonly struct RoadContext
        {
            public RoadContext(string roadId, string segmentId, string edgeId, LwsRoadSurfaceType surfaceType)
            {
                this.roadId = string.IsNullOrWhiteSpace(roadId) ? OffRoadKey : roadId;
                this.segmentId = segmentId ?? string.Empty;
                this.edgeId = string.IsNullOrWhiteSpace(edgeId) ? OffRoadKey : edgeId;
                this.surfaceType = surfaceType == LwsRoadSurfaceType.Unknown ? LwsRoadSurfaceType.AsphaltInterstate : surfaceType;
            }

            public readonly string roadId;
            public readonly string segmentId;
            public readonly string edgeId;
            public readonly LwsRoadSurfaceType surfaceType;
        }
    }
}
