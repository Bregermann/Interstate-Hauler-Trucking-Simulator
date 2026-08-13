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
        public int PrefabCount => TryGetTrafficPrefabsCopy(out GameObject[] prefabs, out _) ? prefabs.Length : 0;
        public int PrefabSlotCount => trafficPrefabs != null ? trafficPrefabs.Length : 0;

        public GameObject[] GetTrafficPrefabsCopy()
        {
            return TryGetTrafficPrefabsCopy(out GameObject[] prefabs, out _) ? prefabs : new GameObject[0];
        }

        public bool TryGetTrafficPrefabsCopy(out GameObject[] prefabs, out string message)
        {
            if (trafficPrefabs == null || trafficPrefabs.Length == 0)
            {
                prefabs = new GameObject[0];
                message = $"{name} has no UTS validation traffic prefab references.";
                return false;
            }

            var validPrefabs = new System.Collections.Generic.List<GameObject>(trafficPrefabs.Length);
            for (int i = 0; i < trafficPrefabs.Length; i++)
            {
                GameObject prefab;
                try
                {
                    prefab = trafficPrefabs[i];
                    if (prefab == null)
                    {
                        prefabs = new GameObject[0];
                        message = $"Traffic profile invalid: traffic prefab slot {i} is missing.";
                        return false;
                    }

                    _ = prefab.name;
                }
                catch (MissingReferenceException)
                {
                    prefabs = new GameObject[0];
                    message = $"Traffic profile invalid: traffic prefab slot {i} references a missing or destroyed prefab.";
                    return false;
                }

                validPrefabs.Add(prefab);
            }

            prefabs = validPrefabs.ToArray();
            message = $"{name} has {prefabs.Length} valid UTS validation traffic prefab references.";
            return true;
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

            if (!TryGetTrafficPrefabsCopy(out _, out message))
            {
                return false;
            }

            message = $"{name} is valid.";
            return true;
        }
    }
}
