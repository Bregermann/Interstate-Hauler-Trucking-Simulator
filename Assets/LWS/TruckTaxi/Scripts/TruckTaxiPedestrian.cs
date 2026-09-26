using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiPedestrian : MonoBehaviour
    {
        public TruckTaxiPedestrianImpactSettings settings=new TruckTaxiPedestrianImpactSettings();
        private Rigidbody walkingBody;
        private Rigidbody[] ragdollBodies;
        private Collider[] colliders;
        private readonly List<Behaviour> movement=new List<Behaviour>();
        private Animator animator;
        private ITruckTaxiPedestrianRagdoll vendorRagdoll;
        private float expiresAt;
        private bool retiring;
        public bool IsRagdoll { get; private set; }
        public bool HitEventSent { get; private set; }
        public Vector3 LastImpulse { get; private set; }
        public float LastImpactSpeed { get; private set; }
        public bool HasJointedRagdoll => vendorRagdoll!=null && ragdollBodies.Length>1;
        public IReadOnlyList<Rigidbody> RagdollBodies => ragdollBodies;
        public string PedestrianId => GetComponent<TruckTaxiImpactTarget>()?.targetId;
        public event Action<TruckTaxiPedestrian> Expired;
        private void Awake()
        {
            // Do not RequireComponent: UTS legitimately destroys this body on activation.
            walkingBody=GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>(); animator=GetComponent<Animator>();
            foreach(var component in GetComponents<Behaviour>())
            {
                if(component is ITruckTaxiPedestrianRagdoll adapter) vendorRagdoll=adapter;
                var name=component.GetType().Name;
                if(name=="Passersby" || name=="MovePath" || name=="PeopleController" || name=="NavMeshAgent") movement.Add(component);
            }
            var bones=new List<Rigidbody>();
            foreach(var body in GetComponentsInChildren<Rigidbody>(true)) if(body!=walkingBody) bones.Add(body);
            ragdollBodies=bones.ToArray(); colliders=GetComponentsInChildren<Collider>(true);
            walkingBody.mass=70;
            foreach(var collider in colliders)
            {
                collider.gameObject.layer=gameObject.layer;
                // Only the root solid walking capsule participates before the hit.
                collider.enabled=!collider.isTrigger && collider.attachedRigidbody==walkingBody;
            }
            foreach(var body in ragdollBodies) body.isKinematic=true;
        }
        public bool TryStrike(Vector3 relativeVelocity,Vector3 contact)
        {
            if(IsRagdoll || retiring || !TruckTaxiPedestrianImpactSettings.Finite(relativeVelocity) ||
                !TruckTaxiPedestrianImpactSettings.Finite(contact) || relativeVelocity.magnitude<Mathf.Max(.1f,settings.minimumRagdollImpactSpeed)) return false;
            IsRagdoll=true; LastImpactSpeed=relativeVelocity.magnitude;
            LastImpulse=settings.CalculateImpulse(relativeVelocity);
            expiresAt=Time.time+Mathf.Clamp(settings.ragdollLifetime,1,60);
            foreach(var component in movement) if(component!=null) component.enabled=false;
            if(animator!=null) animator.enabled=false;
            if(HasJointedRagdoll)
            {
                foreach(var collider in colliders) if(collider!=null) collider.enabled=!collider.isTrigger && collider.attachedRigidbody!=walkingBody;
                // NPCStats owns the actual ragdoll switch and removes the walking body/capsule.
                vendorRagdoll.ActivateRagdoll();
            }
            else
            {
                Debug.LogWarning("PEDESTRIAN RAGDOLL NOT CONFIGURED: "+PedestrianId+"; using physical body fallback.",this);
                ragdollBodies=new[]{walkingBody};
                walkingBody.constraints=RigidbodyConstraints.None;
                walkingBody.isKinematic=false;
                foreach(var collider in colliders) if(collider!=null && !collider.isTrigger) collider.enabled=true;
            }
            float mass=70f/ragdollBodies.Length;
            Rigidbody closest=null; float distance=float.PositiveInfinity;
            foreach(var body in ragdollBodies)
            {
                body.mass=mass; body.useGravity=true; body.isKinematic=false;
                body.maxDepenetrationVelocity=3; body.maxAngularVelocity=12; body.maxLinearVelocity=30;
                body.solverIterations=8; body.solverVelocityIterations=3;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero;
                body.WakeUp(); body.AddForce(LastImpulse/ragdollBodies.Length,ForceMode.Impulse);
                float d=(body.worldCenterOfMass-contact).sqrMagnitude;
                if(d<distance) { closest=body; distance=d; }
            }
            var axis=Vector3.Cross(Vector3.up,relativeVelocity.normalized);
            closest?.AddTorque(axis*Mathf.Clamp(settings.angularImpulse,0,5),ForceMode.Impulse);
            return true;
        }
        public void MarkHitEventSent() => HitEventSent=true;
        private void Update() { if(IsRagdoll && Time.time>=expiresAt) ResetPedestrian(); }
        public void ResetPedestrian()
        {
            if(retiring) return;
            retiring=true;
            // UTS activation destroys its walking body. Recreate through its spawn path,
            // rather than trying to pool an irreversibly changed vendor NPCStats instance.
            if(Expired!=null) Expired(this); else Destroy(gameObject);
        }
    }
}
