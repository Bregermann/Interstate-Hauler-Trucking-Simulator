using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TaxiRequestType { FastDelivery, Shortcut, RamTraffic, HitPedestrian, PropertyDamage, Offroad,
        SmoothRide, NoCollisions, MaximumChaos, ScenicRoute, NearMiss }
    public enum TaxiEventType { Collision, TrafficRam, PedestrianHit, PropDamage, Shortcut, ScenicPoint, NearMiss, HardLanding }
    public enum TaxiRequestState { Active, Succeeded, Failed }
    [System.Flags]
    public enum TaxiRequestBehavior { None=0, Impact=1, Offroad=2, HardAcceleration=4 }

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
        [Tooltip("Additional behavior this request requires; used to exclude contradictory active requests.")]
        public TaxiRequestBehavior requiredBehavior;
        [Tooltip("Additional behavior this request forbids.")]
        public TaxiRequestBehavior forbiddenBehavior;
        public TaxiRequestBehavior RequiredBehavior => requiredBehavior |
            (requestType==TaxiRequestType.HitPedestrian || requestType==TaxiRequestType.RamTraffic || requestType==TaxiRequestType.PropertyDamage
                ? TaxiRequestBehavior.Impact : requestType==TaxiRequestType.Offroad ? TaxiRequestBehavior.Offroad : TaxiRequestBehavior.None);
        public TaxiRequestBehavior ForbiddenBehavior => forbiddenBehavior |
            (requestType==TaxiRequestType.NoCollisions || requestType==TaxiRequestType.SmoothRide ? TaxiRequestBehavior.Impact : TaxiRequestBehavior.None);
        public bool IsCompatibleWith(PassengerRequestDefinition other) => other!=null &&
            (RequiredBehavior & other.ForbiddenBehavior)==0 && (other.RequiredBehavior & ForbiddenBehavior)==0;
    }
}
