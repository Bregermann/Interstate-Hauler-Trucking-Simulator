using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public readonly struct TruckTaxiMoralPhysicalEvidence
    {
        public readonly bool Attached;
        public readonly string TowingVehicleId;
        public readonly LwsTrailerGameplayIdentity AttachedTrailer;
        public readonly Rigidbody TruckBody, TrailerBody;
        public TruckTaxiMoralPhysicalEvidence(bool attached,string towingVehicleId,LwsTrailerGameplayIdentity attachedTrailer,
            Rigidbody truckBody,Rigidbody trailerBody)
        { Attached=attached; TowingVehicleId=towingVehicleId; AttachedTrailer=attachedTrailer; TruckBody=truckBody; TrailerBody=trailerBody; }
    }

    public static class TruckTaxiMoralArrivalPolicy
    {
        public static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        private static bool LiveBody(Rigidbody body) => body!=null && body.gameObject.activeInHierarchy && !body.isKinematic &&
            body.detectCollisions && Finite(body.position) && Finite(body.linearVelocity) && Finite(body.angularVelocity);
        public static bool Matches(LwsTrailerGameplayIdentity expected,string towingId,TruckTaxiMoralPhysicalEvidence sample)
        {
            return expected!=null && expected.isActiveAndEnabled && expected.Validate(out _) &&
                !string.IsNullOrWhiteSpace(towingId) && sample.Attached && sample.TowingVehicleId==towingId &&
                sample.AttachedTrailer==expected && sample.TruckBody!=sample.TrailerBody &&
                sample.TrailerBody==expected.GetComponent<Rigidbody>() && LiveBody(sample.TruckBody) && LiveBody(sample.TrailerBody);
        }
        public static bool Stationary(TruckTaxiMoralPhysicalEvidence sample,float maximumSpeed) =>
            TruckTaxiTownState.InRange(maximumSpeed,.05f,.5f) && LiveBody(sample.TruckBody) && LiveBody(sample.TrailerBody) &&
            sample.TruckBody.linearVelocity.sqrMagnitude<=maximumSpeed*maximumSpeed &&
            sample.TrailerBody.linearVelocity.sqrMagnitude<=maximumSpeed*maximumSpeed &&
            sample.TruckBody.angularVelocity.sqrMagnitude<=.01f && sample.TrailerBody.angularVelocity.sqrMagnitude<=.01f;
        public static bool Arrived(TruckTaxiMoralDestination destination,TruckTaxiMoralPhysicalEvidence sample,float maximumSpeed)
        {
            if(destination==null || !destination.IsValid || !destination.center.gameObject.activeInHierarchy || !Stationary(sample,maximumSpeed)) return false;
            return Inside(sample.TruckBody.position,destination) && Inside(sample.TrailerBody.position,destination);
        }
        private static bool Inside(Vector3 position,TruckTaxiMoralDestination destination)
        {
            var delta=position-destination.center.position;
            return Mathf.Abs(delta.y)<=destination.maximumHeightDifference &&
                delta.x*delta.x+delta.z*delta.z<=destination.radius*destination.radius;
        }
    }

    // Only the live controller feeds physics ticks. No wall-clock jumps or paused-time credit.
    public sealed class TruckTaxiMoralArrivalGate
    {
        private string key;
        private float elapsed;
        public void Reset() { key=null; elapsed=0; }
        public void Observe(string contextKey,bool valid,float physicsSeconds,float requiredSeconds)
        {
            if(!valid || string.IsNullOrEmpty(contextKey) || !TruckTaxiTownState.InRange(physicsSeconds,0,.1f) ||
                !TruckTaxiTownState.InRange(requiredSeconds,1,10)) { Reset(); return; }
            if(key!=contextKey) { key=contextKey; elapsed=0; }
            elapsed=Mathf.Min(requiredSeconds,elapsed+physicsSeconds);
        }
        public bool IsReady(string contextKey,float requiredSeconds) => key==contextKey && !string.IsNullOrEmpty(key) &&
            TruckTaxiTownState.InRange(requiredSeconds,1,10) && elapsed>=requiredSeconds;
    }
}
