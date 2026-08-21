using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsUtsTrafficDebugPanel : MonoBehaviour
    {
        [SerializeField] private LwsUtsHighwayTrafficController controller;
        [SerializeField] private bool visible;
        [SerializeField] private float refreshIntervalSeconds = 0.25f;

        private float _nextRefreshTime;
        private LwsTrafficRuntimeStats _cachedStats;
        private string _cachedAvailability = string.Empty;
        private bool _cachedInitialized;
        private bool _cachedGraphAvailable;
        private int _cachedPrefabCount;
        private int _cachedMaxActive;
        private LwsTrafficDemandSnapshot _cachedDemand;
        private LwsTrafficDensityTier _cachedDensity;
        private string _cachedLastSpawnResult = string.Empty;
        private string _cachedLastError = string.Empty;
        private string _manualResult = string.Empty;

        public void SetController(LwsUtsHighwayTrafficController value)
        {
            controller = value;
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<LwsUtsHighwayTrafficController>();
            }
        }

        private void Update()
        {
            if (!visible || controller == null || Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshIntervalSeconds);
            _cachedStats = controller.Stats;
            _cachedAvailability = controller.UtsAvailability;
            _cachedInitialized = controller.Initialized;
            _cachedGraphAvailable = controller.GraphAvailable;
            _cachedPrefabCount = controller.SpawnablePrefabCount;
            _cachedMaxActive = controller.MaxActiveVehicles;
            _cachedDemand = controller.DemandSnapshot;
            _cachedDensity = controller.DensityTier;
            _cachedLastSpawnResult = controller.LastSpawnResult;
            _cachedLastError = controller.LastError;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(420f, 460f, 460f, 300f), "TRAFFIC STATUS", GUI.skin.window);
            GUILayout.Label($"Initialized: {_cachedInitialized}");
            GUILayout.Label($"UTS Available: {_cachedAvailability}");
            GUILayout.Label($"Graph Available: {_cachedGraphAvailable}");
            GUILayout.Label($"Generated Lanes: {_cachedStats.Lanes}");
            GUILayout.Label($"Spawnable Prefabs: {_cachedPrefabCount}");
            GUILayout.Label($"Active Vehicles: {_cachedStats.ActiveVehicles}");
            GUILayout.Label($"Max Active: {_cachedMaxActive}");
            GUILayout.Label(_cachedDemand.Valid
                ? $"Demand: {_cachedDemand.TrafficPeriod} target {_cachedDemand.SmoothedTargetActive}, nearby {_cachedDemand.NearbyActual}, interval {_cachedDemand.SpawnIntervalSeconds:0.00}s"
                : "Demand: unavailable");
            GUILayout.Label($"Density: {_cachedDensity}");
            GUILayout.Label($"Last Spawn Result: {_cachedLastSpawnResult}");
            GUILayout.Label($"Last Error: {_cachedLastError}");
            GUILayout.Label($"Total Spawned: {_cachedStats.TotalSpawned}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SPAWN TEST TRAFFIC NOW"))
            {
                if (controller != null && controller.TrySpawnOneForValidation(out string message))
                {
                    _manualResult = message;
                }
                else
                {
                    _manualResult = controller != null ? controller.LastSpawnResult : "No traffic controller assigned.";
                }
            }

            if (GUILayout.Button("FILL TRAFFIC TO MAX"))
            {
                int spawned = controller != null ? controller.FillTrafficToMaxForValidation() : 0;
                _manualResult = controller != null ? $"{spawned} vehicle(s) spawned. {controller.LastSpawnResult}" : "No traffic controller assigned.";
            }

            GUILayout.EndHorizontal();
            GUILayout.Label($"Manual Test: {_manualResult}");
            GUILayout.EndArea();
        }
#endif
    }
}
