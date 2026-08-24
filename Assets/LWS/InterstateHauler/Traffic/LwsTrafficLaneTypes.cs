using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsTrafficVehicleKind
    {
        Unknown,
        PassengerCar,
        Bus,
        ServiceVehicle,
        BoxTruck,
        Motorcycle
    }

    public enum LwsTrafficDensityTier
    {
        Off,
        Sparse,
        Normal,
        Dense
    }

    [Serializable]
    public sealed class LwsTrafficSpawnPolicy
    {
        public LwsTrafficDensityTier densityTier = LwsTrafficDensityTier.Sparse;
        public int maxActiveVehicles = 8;
        public float spawnIntervalSeconds = 4f;
        public float minimumPlayerSpawnDistanceMeters = 140f;
        public float maximumPlayerSpawnDistanceMeters = 650f;
        public float despawnDistanceMeters = 850f;
        public float despawnNearLaneEndMeters = 90f;
        public float offRoadCleanupDistanceMeters = 22f;
        public float offRoadCleanupGraceSeconds = 3f;
        public float safetyFloorMeters = -12f;
        public float targetCruiseSpeedScale = 0.72f;
        public float maximumTrafficSpeedMetersPerSecond = 22f;
        public bool includeRampTraffic = true;
        public bool includeTurnaroundTraffic;
        public bool autoResolveEditorPrefabs = true;
        public bool showDebugPanel = true;

        public bool Validate(out string message)
        {
            if (maxActiveVehicles < 0)
            {
                message = "Traffic max active vehicles cannot be negative.";
                return false;
            }

            if (spawnIntervalSeconds <= 0f)
            {
                message = "Traffic spawn interval must be greater than zero.";
                return false;
            }

            if (minimumPlayerSpawnDistanceMeters < 0f || maximumPlayerSpawnDistanceMeters < minimumPlayerSpawnDistanceMeters)
            {
                message = "Traffic player spawn distance range is invalid.";
                return false;
            }

            if (despawnDistanceMeters < maximumPlayerSpawnDistanceMeters)
            {
                message = "Traffic despawn distance should be greater than or equal to the maximum spawn distance.";
                return false;
            }

            if (offRoadCleanupDistanceMeters <= 0f || offRoadCleanupGraceSeconds < 0f)
            {
                message = "Traffic off-road cleanup settings are invalid.";
                return false;
            }

            message = "Traffic spawn policy is valid.";
            return true;
        }
    }

    [Serializable]
    public sealed class LwsTrafficLaneDefinition
    {
        public string laneId;
        public string roadId;
        public string segmentId;
        public string edgeId;
        public int laneIndex;
        public LwsRoadClass roadClass;
        public LwsRoadDirection direction;
        public float speedLimitMph;
        public float laneWidthMeters;
        public float laneCenterOffsetMeters;
        public float lengthMeters;
        public bool spawnEnabled = true;
        public Vector3[] centerline = Array.Empty<Vector3>();

        public Vector3 StartPosition => centerline != null && centerline.Length > 0 ? centerline[0] : Vector3.zero;
        public Vector3 EndPosition => centerline != null && centerline.Length > 0 ? centerline[centerline.Length - 1] : Vector3.zero;

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(laneId))
            {
                message = "Traffic lane has no stable lane ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(roadId) || string.IsNullOrWhiteSpace(segmentId))
            {
                message = $"{laneId} is missing road or segment metadata.";
                return false;
            }

            if (laneIndex < 0)
            {
                message = $"{laneId} has an invalid lane index.";
                return false;
            }

            if (centerline == null || centerline.Length < 3)
            {
                message = $"{laneId} has too few centerline points for UTS path following.";
                return false;
            }

            if (laneWidthMeters <= 0f || speedLimitMph <= 0f || lengthMeters <= 0f)
            {
                message = $"{laneId} has invalid width, speed, or length metadata.";
                return false;
            }

            message = $"{laneId} is valid.";
            return true;
        }
    }

    public readonly struct LwsTrafficRuntimeStats
    {
        public LwsTrafficRuntimeStats(int lanes, int activeVehicles, int totalSpawned, string lastMessage)
        {
            Lanes = lanes;
            ActiveVehicles = activeVehicles;
            TotalSpawned = totalSpawned;
            LastMessage = lastMessage ?? string.Empty;
        }

        public int Lanes { get; }
        public int ActiveVehicles { get; }
        public int TotalSpawned { get; }
        public string LastMessage { get; }
    }
}
