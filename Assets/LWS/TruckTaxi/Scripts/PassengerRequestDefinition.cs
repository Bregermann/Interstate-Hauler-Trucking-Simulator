using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TaxiRequestType { FastDelivery, Shortcut, RamTraffic, HitPedestrian, PropertyDamage, Offroad,
        SmoothRide, NoCollisions, MaximumChaos, ScenicRoute, NearMiss }
    public enum TaxiEventType { Collision, TrafficRam, PedestrianHit, PropDamage, Shortcut, ScenicPoint, NearMiss, HardLanding }
    public enum TaxiRequestState { Active, Succeeded, Failed }

    [CreateAssetMenu(menuName = "Truck Taxi/Passenger Request")]
    public sealed class PassengerRequestDefinition : ScriptableObject
    {
        public TaxiRequestType requestType;
        [TextArea] public string description;
        [Min(1)] public float timer = 90;
        [Min(0.1f)] public float target = 1;
        public string targetId;
        public long bonusMoneyCents = 400;
        public int bonusScore = 100;
        public float ratingModifier = 0.2f;
        [TextArea] public string dialogue;
        [Min(0)] public float cooldown = 30;
        public bool allowMultipleInstances;
        [Tooltip("Smooth rides fail above this acceleration/braking magnitude (m/s squared).")]
        public float maximumAcceleration = 5;
    }
}
