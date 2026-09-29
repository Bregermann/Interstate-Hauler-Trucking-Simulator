using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Kaiju Configuration")]
    public sealed class TruckTaxiKaijuConfig : ScriptableObject
    {
        [Header("Natural game-time cadence")]
        [Min(.1f)] public float sightingMinDays = 1;
        [Min(.1f)] public float sightingMaxDays = 3;
        [Min(.1f)] public float attackMinDays = 3;
        [Min(.1f)] public float attackMaxDays = 7;
        [Min(.1f)] public float maximumSightingDryDays = 3;
        [Header("Presentation")]
        [Min(100)] public float minimumSpawnMeters = 500;
        [Min(100)] public float maximumSpawnMeters = 1200;
        [Min(100)] public float distantLodMeters = 650;
        [Min(1)] public float creatureHeightMeters = 68;
        [Min(5)] public float sightingSeconds = 100;
        [Min(5)] public float attackSeconds = 175;
        [Min(1)] public float walkMetersPerSecond = 4;
        [Header("Bounded interactions")]
        [Min(0)] public float interactionRadiusMeters = 65;
        [Range(0, 12)] public int maximumPropsPerEvent = 4;
        [Range(0, 8)] public int maximumDebris = 3;
        [Min(1)] public float debrisLifetimeSeconds = 12;
        [Min(1)] public float playerWarningSeconds = 7;
        [Min(5)] public float playerCaptureRadiusMeters = 28;
        public float CaptureWarningSeconds => float.IsFinite(playerWarningSeconds) ? Mathf.Max(3, playerWarningSeconds) : 7;
        public float CaptureRadiusMeters => float.IsFinite(playerCaptureRadiusMeters) ? Mathf.Clamp(playerCaptureRadiusMeters, 5, 50) : 28;
        public AudioClip roar;
        public AudioClip stomp;
        public AudioClip fire;

        private void OnValidate()
        {
            sightingMaxDays = Mathf.Max(sightingMinDays, sightingMaxDays);
            attackMaxDays = Mathf.Max(attackMinDays, attackMaxDays);
            maximumSightingDryDays = Mathf.Max(sightingMinDays, maximumSightingDryDays);
            maximumSpawnMeters = Mathf.Max(minimumSpawnMeters, maximumSpawnMeters);
        }
    }
}
