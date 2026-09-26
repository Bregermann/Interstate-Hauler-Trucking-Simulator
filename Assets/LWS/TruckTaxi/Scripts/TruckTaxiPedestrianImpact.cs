using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public interface ITruckTaxiPedestrianRagdoll { void ActivateRagdoll(); }

    [Serializable]
    public sealed class TruckTaxiPedestrianImpactSettings
    {
        [Min(.1f), Tooltip("Relative speed in m/s required for a pedestrian knockdown.")]
        public float minimumRagdollImpactSpeed=2;
        [Min(0), Tooltip("Impulse in N s per m/s of relative impact speed. Higher launches harder.")]
        public float impactForceMultiplier=40;
        [Min(0), Tooltip("Maximum total launch impulse, shared across the whole ragdoll.")]
        public float maximumRagdollImpulse=900;
        [Min(0), Tooltip("Upward N s at an 8 m/s or faster hit; reduced for slower bumps.")]
        public float upwardImpulse=70;
        [Min(0), Tooltip("Capped tumble impulse applied to the bone nearest the contact.")]
        public float angularImpulse=2;
        [Range(1,60)] public float ragdollLifetime=15;
        public Vector3 CalculateImpulse(Vector3 relativeVelocity)
        {
            if(!Finite(relativeVelocity)) return Vector3.zero;
            float speed=Mathf.Min(relativeVelocity.magnitude,100);
            var direction=Vector3.ProjectOnPlane(relativeVelocity,Vector3.up);
            if(direction.sqrMagnitude<.0001f) direction=relativeVelocity;
            var impulse=direction.normalized*speed*Mathf.Clamp(impactForceMultiplier,0,200)+
                Vector3.up*Mathf.Clamp(upwardImpulse,0,300)*Mathf.Clamp01(speed/8);
            return Vector3.ClampMagnitude(impulse,Mathf.Clamp(maximumRagdollImpulse,0,1800));
        }
        public static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    public readonly struct TruckTaxiPedestrianImpact
    {
        public readonly string PedestrianId, PassengerId, RideId;
        public readonly float Speed;
        public readonly Vector3 Impulse, Position;
        public TruckTaxiPedestrianImpact(string id,float speed,Vector3 impulse,Vector3 position,string passenger,string ride)
        { PedestrianId=id; Speed=speed; Impulse=impulse; Position=position; PassengerId=passenger; RideId=ride; }
    }
}
