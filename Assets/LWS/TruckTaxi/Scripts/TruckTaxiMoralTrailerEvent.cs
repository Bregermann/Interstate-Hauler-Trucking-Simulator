using System;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiMoralDestination
    {
        public string objectiveId, displayName;
        public Transform center;
        [Range(3,50)] public float radius=20;
        [Range(.5f,5)] public float maximumHeightDifference=3;
        public bool IsValid => TruckTaxiCareerProfileData.ValidId(objectiveId) && center!=null &&
            TruckTaxiTownState.InRange(radius,3,50) && TruckTaxiTownState.InRange(maximumHeightDifference,.5f,5) &&
            TruckTaxiMoralArrivalPolicy.Finite(center.position);
    }

    // References existing scene content only. This component never spawns or restores physical trailers.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiMoralTrailerEvent : MonoBehaviour
    {
        public TruckTaxiMoralChoiceDefinition definition;
        public LwsTrailerGameplayIdentity trailer;
        public TruckTaxiMoralDestination[] destinations=Array.Empty<TruckTaxiMoralDestination>();
        [Range(1,10)] public float stationarySeconds=2;
        [Range(.05f,.5f)] public float maximumSpeed=.2f;
        public bool ValidateAuthored(out string reason)
        {
            reason="Missing authored choice or existing trailer identity.";
            if(definition==null || trailer==null) return false;
            if(!definition.Validate(out reason) || !trailer.Validate(out reason)) return false;
            reason="Authored trailer ID, destinations or stationary limits do not match.";
            if(trailer.StableTrailerId!=definition.trailerId || destinations==null || destinations.Length!=2 ||
                !TruckTaxiTownState.InRange(stationarySeconds,1,10) || !TruckTaxiTownState.InRange(maximumSpeed,.05f,.5f)) return false;
            foreach(var destination in destinations) if(destination==null || !destination.IsValid) return false;
            if(destinations[0].objectiveId==destinations[1].objectiveId) return false;
            foreach(var option in definition.options) if(FindDestination(option.completionObjectiveId)==null) return false;
            reason=null; return true;
        }
        public bool ValidatePhysical(out string reason)
        {
            if(!ValidateAuthored(out reason)) return false;
            reason="Existing trailer needs its own NWH VehicleController and dynamic Rigidbody; no substitute will be created.";
            var body=trailer.GetComponent<Rigidbody>();
            if(trailer.GetComponent<VehicleController>()==null || body==null || body.isKinematic || !body.detectCollisions) return false;
            reason=null; return true;
        }
        public TruckTaxiMoralDestination FindDestination(string objectiveId) => destinations==null ? null :
            Array.Find(destinations,d=>d!=null && d.objectiveId==objectiveId);

        public static bool TryResolve(TruckTaxiMoralTrailerEvent[] authored,string eventId,out TruckTaxiMoralTrailerEvent result,out string reason)
        {
            result=null; reason="Authored trailer event is unavailable. Existing world content must be restored by its owner.";
            foreach(var item in authored ?? Array.Empty<TruckTaxiMoralTrailerEvent>())
            {
                if(item==null || item.definition==null || item.definition.eventId!=eventId) continue;
                if(result!=null) { result=null; reason="Duplicate authored event ID; context is ambiguous."; return false; }
                result=item;
            }
            if(result==null || !result.isActiveAndEnabled || !result.trailer || !result.trailer.isActiveAndEnabled) return false;
            return result.ValidateAuthored(out reason);
        }
        public static bool TryResolveAccepted(TruckTaxiMoralTrailerEvent[] authored,TruckTaxiAcceptedMoralChoice accepted,
            out TruckTaxiMoralTrailerEvent result,out TruckTaxiMoralDestination destination,out string reason)
        {
            result=null; destination=null; reason="No accepted authored trailer objective.";
            if(accepted==null || accepted.completed || !accepted.IsValid) return false;
            if(!TryResolve(authored,accepted.eventId,out result,out reason)) return false;
            var definition=result.definition;
            reason="Saved choice no longer matches the authored town, trailer or completion objective.";
            if(definition.trailerId!=accepted.trailerId || definition.town.initialState.townId!=accepted.townId) return false;
            var option=Array.Find(definition.options,o=>o.choiceId==accepted.choiceId);
            if(option==null || option.completionObjectiveId!=accepted.completionObjectiveId) return false;
            destination=result.FindDestination(accepted.completionObjectiveId);
            if(destination==null || !destination.center.gameObject.activeInHierarchy) return false;
            reason=null; return true;
        }
    }
}
