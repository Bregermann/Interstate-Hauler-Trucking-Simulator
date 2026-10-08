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
        [Tooltip("Offset from the authored seat for the second rider's center perch.")]
        public Vector3 secondarySeatOffset = new Vector3(-.42f,.12f,-.02f);
        public Vector3 secondaryEulerOffset;
        [Header("Optional authored sleeper rig")]
        public TruckTaxiCabAnchorId primaryCabAnchor = TruckTaxiCabAnchorId.FrontPassengerSeat;
        public TruckTaxiCabAnchorId secondaryCabAnchor = TruckTaxiCabAnchorId.SleeperLeft;
        public bool secondaryRequiresSleeperAnchor = true;
        [Min(.1f)] public float approachSpeed = 2.5f;
        [Min(.1f)] public float boardingDistance = 2;
        [Header("Oversized boarding; restored on every exit path")]
        [Range(0,800)] public float additionalMassKg;
        [Range(0,2000)] public float boardingImpulse = 150;
        public Vector3 boardingImpulseOffset = new Vector3(.6f,0,0);
        public AudioClip boardingAudio;

        public static void PlaceActor(Transform actor, Transform truck, TruckTaxiSeatProfile profile,
            bool secondary, Vector3 standingWorldScale)
        {
            if (!TryPlaceActor(actor, truck, profile, secondary, standingWorldScale) && actor != null)
                Debug.LogWarning("Authored sleeper cab has no compatible free occupant anchor.", actor);
        }

        public static bool TryPlaceActor(Transform actor, Transform truck, TruckTaxiSeatProfile profile,
            bool secondary, Vector3 standingWorldScale, TruckTaxiCabOccupantKind? occupantKind = null)
        {
            if (!CanPlaceActor(actor, truck, profile, secondary, occupantKind)) return false;
            var rig = truck.GetComponent<TruckTaxiCabOccupantRig>();
            // Every extra occupant uses certified capacity, including a fare's authored pair.
            if (secondary)
                return rig != null && rig.TrySeatSecondary(actor,
                    profile != null ? profile.seatType : TruckTaxiSeatType.StandardPassenger, standingWorldScale);
            if (rig != null && rig.HasSleeperCapability)
            {
                var type = profile != null ? profile.seatType : TruckTaxiSeatType.StandardPassenger;
                var kind = occupantKind ?? TruckTaxiCabOccupantKind.Passenger;
                var preferred = secondary ? profile != null ? profile.secondaryCabAnchor : TruckTaxiCabAnchorId.SleeperLeft :
                    profile != null ? profile.primaryCabAnchor : TruckTaxiCabAnchorId.FrontPassengerSeat;
                return rig.TrySeat(actor, kind, preferred, type, standingWorldScale,
                    secondary && (profile == null || profile.secondaryRequiresSleeperAnchor));
            }
            Transform mount = profile != null && !string.IsNullOrWhiteSpace(profile.mountPath)
                ? truck.Find(profile.mountPath) : null;
            if (mount == null) mount = truck;
            actor.SetParent(mount, false);
            actor.localPosition = (profile != null ? profile.localPosition : new Vector3(.5f,1f,0f)) +
                (secondary ? profile != null ? profile.secondarySeatOffset : new Vector3(-.42f,.12f,-.02f) : Vector3.zero);
            actor.localRotation = Quaternion.Euler((profile != null ? profile.localEulerAngles : Vector3.zero) +
                (secondary && profile != null ? profile.secondaryEulerOffset : Vector3.zero));
            Vector3 parentScale = mount.lossyScale;
            float ratio = secondary ? .84f : .88f;
            Vector3 authoredScale = profile != null ? profile.localScale : Vector3.one;
            actor.localScale = new Vector3(
                standingWorldScale.x * ratio * Mathf.Clamp01(authoredScale.x) / Mathf.Max(Mathf.Abs(parentScale.x), .0001f),
                standingWorldScale.y * ratio * Mathf.Clamp01(authoredScale.y) / Mathf.Max(Mathf.Abs(parentScale.y), .0001f),
                standingWorldScale.z * ratio * Mathf.Clamp01(authoredScale.z) / Mathf.Max(Mathf.Abs(parentScale.z), .0001f));
            return true;
        }

        public static bool CanPlaceActor(Transform actor, Transform truck, TruckTaxiSeatProfile profile,
            bool secondary, TruckTaxiCabOccupantKind? occupantKind = null)
        {
            if (actor == null || truck == null) return false;
            var rig = truck.GetComponent<TruckTaxiCabOccupantRig>();
            var type = profile != null ? profile.seatType : TruckTaxiSeatType.StandardPassenger;
            if (secondary) return rig != null && rig.HasSecondarySeat(type, actor);
            if (rig != null && rig.HasSleeperCapability)
                return rig.HasSeat(actor, occupantKind ?? TruckTaxiCabOccupantKind.Passenger,
                    profile != null ? profile.primaryCabAnchor : TruckTaxiCabAnchorId.FrontPassengerSeat, type);
            var mount = profile != null && !string.IsNullOrWhiteSpace(profile.mountPath) ? truck.Find(profile.mountPath) : null;
            if (mount == null) mount = truck;
            Vector3 position = mount.TransformPoint(profile != null ? profile.localPosition : new Vector3(.5f, 1, 0));
            var incoming = actor.GetComponent<TruckTaxiPassengerActor>();
            float radius = (incoming != null ? incoming.StandingRadius : .55f) * .88f;
            if (profile != null) radius *= Mathf.Max(Mathf.Clamp01(profile.localScale.x), Mathf.Clamp01(profile.localScale.z));
            foreach (var seated in truck.GetComponentsInChildren<TruckTaxiPassengerActor>(true))
                if (seated.transform != actor && Vector3.Distance(position, seated.transform.position) <
                    radius + TruckTaxiCabOccupantRig.SeatedRadius(seated)) return false;
            return true;
        }
    }
}
