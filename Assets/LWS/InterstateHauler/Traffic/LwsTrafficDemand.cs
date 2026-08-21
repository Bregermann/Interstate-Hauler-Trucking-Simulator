using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsTrafficDemandProfileId
    {
        GenericInterstate,
        RuralInterstate,
        SuburbanInterstate,
        UrbanInterstate,
        SmallTown,
        UrbanArterial
    }

    public enum LwsTrafficPeriod
    {
        VeryLightNight,
        Building,
        AmPeak,
        Daytime,
        PmPeak,
        Evening,
        LateNight
    }

    [Serializable]
    public struct LwsTrafficDemandSample
    {
        [Range(0f, 24f)] public float hour;
        [Range(0f, 1f)] public float demand01;

        public LwsTrafficDemandSample(float hour, float demand01)
        {
            this.hour = Mathf.Approximately(hour, 24f) ? 24f : LwsGameClockUtility.NormalizeHours(hour);
            this.demand01 = Mathf.Clamp01(demand01);
        }
    }

    [Serializable]
    public sealed class LwsTrafficDemandProfile
    {
        public LwsTrafficDemandProfileId profileId = LwsTrafficDemandProfileId.GenericInterstate;
        public string displayName = "Generic Interstate";
        public int minimumActiveVehicles = 3;
        public int maximumActiveVehicles = 24;
        public int overnightNearbyTarget = 2;
        public int daytimeNearbyTarget = 4;
        public int peakNearbyTarget = 6;
        public float nearbyRadiusMeters = 750f;
        public float nightSpawnIntervalSeconds = 3.0f;
        public float daytimeSpawnIntervalSeconds = 1.35f;
        public float peakSpawnIntervalSeconds = 0.75f;
        public float targetSmoothingVehiclesPerSecond = 1.5f;
        public float weatherDemandMultiplier = 1f;
        public LwsTrafficDemandSample[] hourlySamples =
        {
            new LwsTrafficDemandSample(0f, 0.18f),
            new LwsTrafficDemandSample(3f, 0.12f),
            new LwsTrafficDemandSample(5f, 0.22f),
            new LwsTrafficDemandSample(6.5f, 0.58f),
            new LwsTrafficDemandSample(8f, 0.86f),
            new LwsTrafficDemandSample(9f, 0.70f),
            new LwsTrafficDemandSample(12f, 0.52f),
            new LwsTrafficDemandSample(15.5f, 0.58f),
            new LwsTrafficDemandSample(17f, 1.00f),
            new LwsTrafficDemandSample(19f, 0.68f),
            new LwsTrafficDemandSample(22f, 0.30f),
            new LwsTrafficDemandSample(24f, 0.18f)
        };

        public bool Validate(out string message)
        {
            if (maximumActiveVehicles < 1 || minimumActiveVehicles < 0 || minimumActiveVehicles > maximumActiveVehicles)
            {
                message = "Traffic demand active vehicle range is invalid.";
                return false;
            }

            if (overnightNearbyTarget < 0 || daytimeNearbyTarget < overnightNearbyTarget || peakNearbyTarget < daytimeNearbyTarget)
            {
                message = "Traffic demand nearby targets should increase from overnight to daytime to peak.";
                return false;
            }

            if (peakNearbyTarget > maximumActiveVehicles)
            {
                message = "Traffic demand peak nearby target cannot exceed maximum active vehicles.";
                return false;
            }

            if (nearbyRadiusMeters < 100f)
            {
                message = "Traffic demand nearby radius is too small for interstate validation.";
                return false;
            }

            if (hourlySamples == null || hourlySamples.Length < 2)
            {
                message = "Traffic demand requires at least two hourly samples.";
                return false;
            }

            message = "Traffic demand profile is valid.";
            return true;
        }

        public float EvaluateDemand01(float timeOfDayHours)
        {
            if (hourlySamples == null || hourlySamples.Length == 0)
            {
                return 0f;
            }

            LwsTrafficDemandSample[] samples = hourlySamples;

            float hour = LwsGameClockUtility.NormalizeHours(timeOfDayHours);
            for (int i = 0; i < samples.Length - 1; i++)
            {
                LwsTrafficDemandSample current = samples[i];
                LwsTrafficDemandSample next = samples[i + 1];
                if (hour < current.hour || hour > next.hour)
                {
                    continue;
                }

                float t = Mathf.InverseLerp(current.hour, next.hour, hour);
                return Mathf.Clamp01(Mathf.Lerp(current.demand01, next.demand01, t) * Mathf.Max(0f, weatherDemandMultiplier));
            }

            LwsTrafficDemandSample last = samples[samples.Length - 1];
            LwsTrafficDemandSample first = samples[0];
            float wrappedEnd = last.hour >= first.hour ? first.hour + 24f : first.hour;
            float wrappedHour = hour < last.hour ? hour + 24f : hour;
            float wrappedT = Mathf.InverseLerp(last.hour, wrappedEnd, wrappedHour);
            return Mathf.Clamp01(Mathf.Lerp(last.demand01, first.demand01, wrappedT) * Mathf.Max(0f, weatherDemandMultiplier));
        }

        public int EvaluateTargetActiveVehicles(float timeOfDayHours)
        {
            float demand = EvaluateDemand01(timeOfDayHours);
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minimumActiveVehicles, maximumActiveVehicles, demand)), minimumActiveVehicles, maximumActiveVehicles);
        }

        public int EvaluateMinimumNearby(float timeOfDayHours)
        {
            float demand = EvaluateDemand01(timeOfDayHours);
            if (demand >= 0.78f)
            {
                return Mathf.Clamp(peakNearbyTarget, 0, maximumActiveVehicles);
            }

            if (demand >= 0.42f)
            {
                return Mathf.Clamp(daytimeNearbyTarget, 0, maximumActiveVehicles);
            }

            return Mathf.Clamp(overnightNearbyTarget, 0, maximumActiveVehicles);
        }

        public float EvaluateSpawnInterval(float timeOfDayHours)
        {
            float demand = EvaluateDemand01(timeOfDayHours);
            float interval = demand >= 0.78f
                ? Mathf.Lerp(daytimeSpawnIntervalSeconds, peakSpawnIntervalSeconds, Mathf.InverseLerp(0.78f, 1f, demand))
                : Mathf.Lerp(nightSpawnIntervalSeconds, daytimeSpawnIntervalSeconds, Mathf.InverseLerp(0f, 0.78f, demand));
            return Mathf.Max(0.25f, interval);
        }

        public LwsTrafficPeriod ClassifyPeriod(float timeOfDayHours)
        {
            float hour = LwsGameClockUtility.NormalizeHours(timeOfDayHours);
            if (hour >= 0f && hour < 5f)
            {
                return LwsTrafficPeriod.VeryLightNight;
            }

            if (hour >= 5f && hour < 6.5f)
            {
                return LwsTrafficPeriod.Building;
            }

            if (hour >= 6.5f && hour < 9f)
            {
                return LwsTrafficPeriod.AmPeak;
            }

            if (hour >= 9f && hour < 15.5f)
            {
                return LwsTrafficPeriod.Daytime;
            }

            if (hour >= 15.5f && hour < 19f)
            {
                return LwsTrafficPeriod.PmPeak;
            }

            if (hour >= 19f && hour < 22f)
            {
                return LwsTrafficPeriod.Evening;
            }

            return LwsTrafficPeriod.LateNight;
        }

        public static LwsTrafficDemandProfile CreateGenericInterstate()
        {
            return new LwsTrafficDemandProfile();
        }
    }

    public readonly struct LwsTrafficDemandSnapshot
    {
        public LwsTrafficDemandSnapshot(
            bool valid,
            LwsTrafficDemandProfileId profileId,
            string displayName,
            float timeOfDayHours,
            LwsTrafficPeriod trafficPeriod,
            float demandMultiplier,
            int targetActive,
            int smoothedTargetActive,
            int minimumNearby,
            int nearbyActual,
            int sameDirection,
            int oppositeDirection,
            int spawnDeficit,
            float spawnIntervalSeconds,
            float lastSpawnSeconds,
            float lastCleanupSeconds,
            int maximumActive)
        {
            Valid = valid;
            ProfileId = profileId;
            DisplayName = displayName ?? string.Empty;
            TimeOfDayHours = timeOfDayHours;
            TrafficPeriod = trafficPeriod;
            DemandMultiplier = demandMultiplier;
            TargetActive = targetActive;
            SmoothedTargetActive = smoothedTargetActive;
            MinimumNearby = minimumNearby;
            NearbyActual = nearbyActual;
            SameDirection = sameDirection;
            OppositeDirection = oppositeDirection;
            SpawnDeficit = spawnDeficit;
            SpawnIntervalSeconds = spawnIntervalSeconds;
            LastSpawnSeconds = lastSpawnSeconds;
            LastCleanupSeconds = lastCleanupSeconds;
            MaximumActive = maximumActive;
        }

        public bool Valid { get; }
        public LwsTrafficDemandProfileId ProfileId { get; }
        public string DisplayName { get; }
        public float TimeOfDayHours { get; }
        public LwsTrafficPeriod TrafficPeriod { get; }
        public float DemandMultiplier { get; }
        public int TargetActive { get; }
        public int SmoothedTargetActive { get; }
        public int MinimumNearby { get; }
        public int NearbyActual { get; }
        public int SameDirection { get; }
        public int OppositeDirection { get; }
        public int SpawnDeficit { get; }
        public float SpawnIntervalSeconds { get; }
        public float LastSpawnSeconds { get; }
        public float LastCleanupSeconds { get; }
        public int MaximumActive { get; }

        public static LwsTrafficDemandSnapshot Empty => new LwsTrafficDemandSnapshot(
            false,
            LwsTrafficDemandProfileId.GenericInterstate,
            "None",
            0f,
            LwsTrafficPeriod.Daytime,
            0f,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0f,
            0f,
            0f,
            0);
    }

    public interface ILwsTrafficDemandService : ILwsService
    {
        LwsTrafficDemandProfile ActiveProfile { get; }
        LwsTrafficDemandSnapshot CurrentSnapshot { get; }
        string LastMessage { get; }
        void Configure(LwsTrafficDemandProfile profile);
        LwsTrafficDemandSnapshot EvaluateDemand(
            LwsGameClockSnapshot clock,
            int activeVehicles,
            int nearbyVehicles,
            int sameDirectionVehicles,
            int oppositeDirectionVehicles,
            float deltaTimeSeconds,
            float lastSpawnSeconds,
            float lastCleanupSeconds);
    }

    public sealed class LwsTrafficDemandService : ILwsTrafficDemandService
    {
        private float _smoothedTarget = -1f;

        public string ServiceId => "lws.traffic.demand";
        public LwsTrafficDemandProfile ActiveProfile { get; private set; } = LwsTrafficDemandProfile.CreateGenericInterstate();
        public LwsTrafficDemandSnapshot CurrentSnapshot { get; private set; } = LwsTrafficDemandSnapshot.Empty;
        public string LastMessage { get; private set; } = "Not initialized.";

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            Configure(ActiveProfile);
            return LwsServiceResult.Success("LWS traffic demand service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            CurrentSnapshot = LwsTrafficDemandSnapshot.Empty;
            LastMessage = "LWS traffic demand service shut down.";
            return LwsServiceResult.Success(LastMessage);
        }

        public void Configure(LwsTrafficDemandProfile profile)
        {
            ActiveProfile = profile ?? LwsTrafficDemandProfile.CreateGenericInterstate();
            if (ActiveProfile.hourlySamples != null)
            {
                Array.Sort(ActiveProfile.hourlySamples, (a, b) => a.hour.CompareTo(b.hour));
            }

            if (!ActiveProfile.Validate(out string message))
            {
                ActiveProfile = LwsTrafficDemandProfile.CreateGenericInterstate();
                ActiveProfile.Validate(out message);
            }

            _smoothedTarget = -1f;
            LastMessage = message;
        }

        public LwsTrafficDemandSnapshot EvaluateDemand(
            LwsGameClockSnapshot clock,
            int activeVehicles,
            int nearbyVehicles,
            int sameDirectionVehicles,
            int oppositeDirectionVehicles,
            float deltaTimeSeconds,
            float lastSpawnSeconds,
            float lastCleanupSeconds)
        {
            LwsTrafficDemandProfile profile = ActiveProfile ?? LwsTrafficDemandProfile.CreateGenericInterstate();
            float timeOfDay = LwsGameClockUtility.NormalizeHours(clock.timeOfDayHours);
            float demand = profile.EvaluateDemand01(timeOfDay);
            int target = profile.EvaluateTargetActiveVehicles(timeOfDay);
            int minimumNearby = profile.EvaluateMinimumNearby(timeOfDay);
            float spawnInterval = profile.EvaluateSpawnInterval(timeOfDay);

            if (_smoothedTarget < 0f || deltaTimeSeconds <= 0f)
            {
                _smoothedTarget = target;
            }
            else
            {
                float rate = Mathf.Max(0.1f, profile.targetSmoothingVehiclesPerSecond);
                _smoothedTarget = Mathf.MoveTowards(_smoothedTarget, target, rate * Mathf.Max(0f, deltaTimeSeconds));
            }

            int smoothedTarget = Mathf.Clamp(Mathf.RoundToInt(_smoothedTarget), profile.minimumActiveVehicles, profile.maximumActiveVehicles);
            int effectiveTarget = Mathf.Max(smoothedTarget, minimumNearby);
            int activeDeficit = Mathf.Max(0, effectiveTarget - activeVehicles);
            int nearbyDeficit = Mathf.Max(0, minimumNearby - nearbyVehicles);
            int spawnDeficit = Mathf.Max(activeDeficit, nearbyDeficit);
            CurrentSnapshot = new LwsTrafficDemandSnapshot(
                true,
                profile.profileId,
                profile.displayName,
                timeOfDay,
                profile.ClassifyPeriod(timeOfDay),
                demand,
                target,
                effectiveTarget,
                minimumNearby,
                nearbyVehicles,
                sameDirectionVehicles,
                oppositeDirectionVehicles,
                spawnDeficit,
                spawnInterval,
                lastSpawnSeconds,
                lastCleanupSeconds,
                profile.maximumActiveVehicles);
            LastMessage = $"Traffic demand {profile.displayName}: target {effectiveTarget}, nearby {nearbyVehicles}.";
            return CurrentSnapshot;
        }
    }
}
