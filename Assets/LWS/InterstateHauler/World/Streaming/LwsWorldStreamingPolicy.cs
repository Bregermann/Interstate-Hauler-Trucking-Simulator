using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(
        fileName = "IH_WorldStreamingPolicy",
        menuName = "Interstate Hauler/World Streaming/Streaming Policy")]
    public sealed class LwsWorldStreamingPolicy : ScriptableObject
    {
        [Header("Distances")]
        public float loadAheadDistanceMeters = 1500f;
        public float keepBehindDistanceMeters = 800f;
        public float preloadMarginMeters = 250f;
        public float unloadDistanceMeters = 2100f;
        public float unloadHysteresisMeters = 300f;
        public float tractorSafetyMarginMeters = 140f;
        public float trailerSafetyMarginMeters = 180f;

        [Header("Scheduling")]
        public int maximumConcurrentLoads = 2;
        public int minimumLoadedNeighborCount = 1;
        public float evaluationIntervalSeconds = 0.25f;

        [Header("Quality Scaling")]
        public float ultraOptionalSceneryScale = 1.2f;
        public float highOptionalSceneryScale = 1.0f;
        public float mediumOptionalSceneryScale = 0.85f;
        public float lowOptionalSceneryScale = 0.7f;
        public float steamDeckOptionalSceneryScale = 0.65f;

        public void Clamp()
        {
            loadAheadDistanceMeters = Mathf.Max(100f, loadAheadDistanceMeters);
            keepBehindDistanceMeters = Mathf.Max(100f, keepBehindDistanceMeters);
            preloadMarginMeters = Mathf.Max(0f, preloadMarginMeters);
            unloadDistanceMeters = Mathf.Max(loadAheadDistanceMeters + 100f, unloadDistanceMeters);
            unloadHysteresisMeters = Mathf.Max(0f, unloadHysteresisMeters);
            tractorSafetyMarginMeters = Mathf.Max(0f, tractorSafetyMarginMeters);
            trailerSafetyMarginMeters = Mathf.Max(0f, trailerSafetyMarginMeters);
            maximumConcurrentLoads = Mathf.Clamp(maximumConcurrentLoads, 1, 8);
            minimumLoadedNeighborCount = Mathf.Clamp(minimumLoadedNeighborCount, 0, 4);
            evaluationIntervalSeconds = Mathf.Clamp(evaluationIntervalSeconds, 0.05f, 2f);
        }

        public bool Validate(out string message)
        {
            if (loadAheadDistanceMeters <= 0f || keepBehindDistanceMeters <= 0f || unloadDistanceMeters <= 0f)
            {
                message = "Streaming policy distances must be positive.";
                return false;
            }

            if (unloadDistanceMeters <= loadAheadDistanceMeters)
            {
                message = "Unload distance must be greater than load-ahead distance.";
                return false;
            }

            if (maximumConcurrentLoads <= 0)
            {
                message = "Maximum concurrent loads must be positive.";
                return false;
            }

            message = "Streaming policy is valid.";
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Clamp();
        }
#endif
    }
}
