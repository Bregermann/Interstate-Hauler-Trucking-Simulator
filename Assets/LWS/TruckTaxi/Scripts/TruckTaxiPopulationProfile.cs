using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Population Density Profile")]
    public sealed class TruckTaxiPopulationProfile : ScriptableObject
    {
        [Header("Truck Taxi only - measured baseline multipliers")]
        [Min(0), Tooltip("Scales the actual initial UTS pedestrian count, not the old maximumPeople cap.")]
        public float pedestrianDensityMultiplier = 60;
        [Min(0), Tooltip("Scales the actual initial traffic count, not the theoretical lane capacity.")]
        public float trafficDensityMultiplier = 25;
        [Min(0), Tooltip("0 measures the native startup batch. Set only after recording a verified runtime baseline.")]
        public int measuredPedestrianBaseline;
        [Min(0), Tooltip("0 measures the original startup attempts. Set after recording a verified runtime baseline.")]
        public int measuredTrafficBaseline;
        [Min(0), Tooltip("Explicit live-instance safety cap. Lowering this can reduce the requested multiplier; diagnostics report that.")]
        public int maximumActivePedestrians = 720;
        [Min(0)] public int maximumActiveTraffic = 300;
        [Header("Authored city footprint (metres from population host)")]
        [Min(0), Tooltip("0 allows every authored spawn point. This is not a new navigation or spawning system.")]
        public float spawnRadius = 600;
        [Min(0), Tooltip("0 retains all valid pedestrians and the original traffic bounds. Must exceed spawn radius when enabled.")]
        public float despawnRadius = 700;
        [Min(.1f), Tooltip("Population bookkeeping only. UTS movement and AI update rates are unchanged.")]
        public float maintenanceInterval = .5f;
        [Min(1), Tooltip("Maximum new instances per maintenance pass, preventing a whole-city respawn spike.")]
        public int maximumSpawnsPerPass = 8;
        [Min(1), Tooltip("Maximum simultaneously simulated fallen pedestrians. Oldest observed ragdolls retire first and free their slots.")]
        public int maximumActiveRagdolls = 24;
        [Min(.25f)] public float pedestrianSpawnClearance = 1.5f;
        [Min(1)] public float trafficSpawnClearance = 14;
        [Min(1)] public float playerTrafficSpawnClearance = 20;

        [Header("Native presentation budgets (population and AI remain active)")]
        [Tooltip("Uses Unity culling and the prefab's existing LOD meshes. Disable to compare the original presentation cost at identical density.")]
        public bool optimizePresentation = true;
        [Tooltip("Stop offscreen Animator evaluation only where root motion is disabled. UTS still moves every pedestrian.")]
        public bool cullOffscreenAnimation = true;
        [Range(1, 3), Tooltip("Earlier transitions to existing lower-detail meshes. The final culling threshold is preserved, so visibility range is not shortened.")]
        public float lodTransitionMultiplier = 1.75f;
        [Min(0), Tooltip("Pedestrian shadows stop beyond this distance from the player; 0 preserves all authored shadows. Bodies remain visible and simulated.")]
        public float pedestrianShadowDistance = 60;
        [Min(0), Tooltip("Traffic shadows stop beyond this distance from the player; 0 preserves authored shadows.")]
        public float trafficShadowDistance = 100;
        [Min(0), Tooltip("Use two skinning weights per vertex on distant pedestrians; 0 preserves authored skinning quality.")]
        public float pedestrianReducedSkinningDistance = 50;
        [Tooltip("Avoid the extra previous-frame skinned buffer on ambient population. Does not disable ordinary object motion vectors.")]
        public bool disableSkinnedMotionVectors = true;
        [Min(0), Tooltip("Distance hysteresis prevents shadow/skinning changes from chattering at a threshold.")]
        public float presentationDistanceHysteresis = 8;

        public int PedestrianTarget(int observed) => ScaleTarget(measuredPedestrianBaseline > 0 ? measuredPedestrianBaseline : observed,
            pedestrianDensityMultiplier, maximumActivePedestrians);
        public int TrafficTarget(int observed) => ScaleTarget(measuredTrafficBaseline > 0 ? measuredTrafficBaseline : observed,
            trafficDensityMultiplier, maximumActiveTraffic);
        public static int ScaleTarget(int baseline, float multiplier, int cap)
        {
            if (baseline <= 0 || cap <= 0 || float.IsNaN(multiplier) || multiplier <= 0) return 0;
            return (int)System.Math.Min(cap, System.Math.Round((double)baseline * multiplier, System.MidpointRounding.AwayFromZero));
        }
        public bool AllowsSpawn(Vector3 point, Vector3 origin) => WithinRadius(point, origin, spawnRadius);
        public bool AllowsPresence(Vector3 point, Vector3 origin) => WithinRadius(point, origin,
            despawnRadius > 0 ? Mathf.Max(spawnRadius, despawnRadius) : 0);
        private static bool WithinRadius(Vector3 point, Vector3 origin, float radius)
        {
            point.y = origin.y;
            return radius <= 0 || (point - origin).sqrMagnitude <= radius * radius;
        }
    }
}
