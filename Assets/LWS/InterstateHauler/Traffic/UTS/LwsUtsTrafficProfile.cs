using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(menuName = "Interstate Hauler/Traffic/UTS Traffic Profile", fileName = "IH_UtsTrafficProfile")]
    public sealed class LwsUtsTrafficProfile : ScriptableObject
    {
        [SerializeField] private bool trafficEnabled = true;
        [SerializeField] private LwsTrafficSpawnPolicy spawnPolicy = new LwsTrafficSpawnPolicy();
        [SerializeField] private GameObject[] trafficPrefabs;

        public bool TrafficEnabled => trafficEnabled;
        public LwsTrafficSpawnPolicy SpawnPolicy => spawnPolicy;
        public int PrefabCount => CountPrefabs(trafficPrefabs);

        public GameObject[] GetTrafficPrefabsCopy()
        {
            if (trafficPrefabs == null || trafficPrefabs.Length == 0)
            {
                return new GameObject[0];
            }

            var copy = new GameObject[trafficPrefabs.Length];
            for (int i = 0; i < trafficPrefabs.Length; i++)
            {
                copy[i] = trafficPrefabs[i];
            }

            return copy;
        }

        public LwsTrafficSpawnPolicy CreateSpawnPolicyCopy()
        {
            LwsTrafficSpawnPolicy source = spawnPolicy ?? new LwsTrafficSpawnPolicy();
            return new LwsTrafficSpawnPolicy
            {
                densityTier = trafficEnabled ? source.densityTier : LwsTrafficDensityTier.Off,
                maxActiveVehicles = source.maxActiveVehicles,
                spawnIntervalSeconds = source.spawnIntervalSeconds,
                minimumPlayerSpawnDistanceMeters = source.minimumPlayerSpawnDistanceMeters,
                maximumPlayerSpawnDistanceMeters = source.maximumPlayerSpawnDistanceMeters,
                despawnDistanceMeters = source.despawnDistanceMeters,
                despawnNearLaneEndMeters = source.despawnNearLaneEndMeters,
                targetCruiseSpeedScale = source.targetCruiseSpeedScale,
                maximumTrafficSpeedMetersPerSecond = source.maximumTrafficSpeedMetersPerSecond,
                includeRampTraffic = source.includeRampTraffic,
                includeTurnaroundTraffic = source.includeTurnaroundTraffic,
                autoResolveEditorPrefabs = source.autoResolveEditorPrefabs,
                showDebugPanel = source.showDebugPanel
            };
        }

        public bool ValidateProfile(out string message)
        {
            if (!trafficEnabled)
            {
                message = $"{name} is disabled.";
                return false;
            }

            if (spawnPolicy == null)
            {
                message = $"{name} has no traffic spawn policy.";
                return false;
            }

            if (!spawnPolicy.Validate(out message))
            {
                return false;
            }

            if (spawnPolicy.maxActiveVehicles <= 0)
            {
                message = $"{name} must allow at least one active validation traffic vehicle.";
                return false;
            }

            if (CountPrefabs(trafficPrefabs) == 0)
            {
                message = $"{name} has no UTS validation traffic prefab references.";
                return false;
            }

            message = $"{name} is valid.";
            return true;
        }

        private static int CountPrefabs(GameObject[] prefabs)
        {
            int count = 0;
            if (prefabs == null)
            {
                return count;
            }

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
