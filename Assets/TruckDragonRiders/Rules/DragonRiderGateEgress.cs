using System;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    // Only the short, straight flight intent after an already-scored passage.
    // Ordered race progress remains exclusively owned by DragonRiderRace.
    public sealed class DragonRiderGateEgress
    {
        Vector3 center,forward;float distance;
        public bool Pending {get;private set;}
        public void Reset(){Pending=false;}
        public void Arm(Vector3 gateCenter,Vector3 gateForward,float exitDistance)
        {
            if(!Finite(gateCenter)||!Finite(gateForward)||gateForward.sqrMagnitude<.99f||gateForward.sqrMagnitude>1.01f||!float.IsFinite(exitDistance)||exitDistance<1||exitDistance>150)throw new ArgumentException("Finite unit gate forward and bounded1–150m straight egress required.");
            center=gateCenter;forward=gateForward.normalized;distance=exitDistance;Pending=true;
        }
        public bool TryTarget(Vector3 actualTruckCenter,out Vector3 target)
        {
            if(!Finite(actualTruckCenter))throw new ArgumentException("Actual finite truck center required.");
            target=center+forward*distance;if(!Pending)return false;
            if(Vector3.Dot(actualTruckCenter-center,forward)>=distance){Pending=false;return false;}
            return true;
        }
        static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
    }
}
