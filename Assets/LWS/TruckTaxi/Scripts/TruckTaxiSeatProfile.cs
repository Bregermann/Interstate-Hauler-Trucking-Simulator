using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiSeatType { StandardPassenger, OversizedPassenger, TinyPassenger, DashboardPassenger, CompanionPassenger, QuadrupedPassenger, RobotPassenger, SpecialCustom }
    [CreateAssetMenu(menuName = "Truck Taxi/Passengers/Seat")]
    public sealed class TruckTaxiSeatProfile : ScriptableObject
    {
        public TruckTaxiSeatType seatType;
        [Tooltip("Path relative to the player tractor. Empty uses the tractor root.")]
        public string mountPath = "Cab";
        public Vector3 localPosition = new Vector3(.48f,.05f,.05f);
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;
        [Min(.1f)] public float approachSpeed = 2.5f;
        [Min(.1f)] public float boardingDistance = 2;
        [Header("Oversized boarding; restored on every exit path")]
        [Range(0,800)] public float additionalMassKg;
        [Range(0,2000)] public float boardingImpulse = 150;
        public Vector3 boardingImpulseOffset = new Vector3(.6f,0,0);
        public AudioClip boardingAudio;
    }
}
