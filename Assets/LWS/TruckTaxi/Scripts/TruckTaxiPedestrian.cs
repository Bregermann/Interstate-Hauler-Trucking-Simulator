using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiPedestrian : MonoBehaviour
    {
        public TruckTaxiPedestrianImpactSettings settings=new TruckTaxiPedestrianImpactSettings();
        private Rigidbody walkingBody;
        private Rigidbody[] ragdollBodies;
        private Rigidbody[] activeRagdollBodies;
        private Transform[] ragdollTransforms;
        private Vector3[] restPositions;
        private Quaternion[] restRotations;
        private Collider[] colliders;
        private readonly HashSet<Collider> walkingColliders = new HashSet<Collider>();
        private Joint[] joints;
        private readonly List<Behaviour> movement=new List<Behaviour>();
        private readonly List<bool> movementEnabled=new List<bool>();
        private Animator animator;
        private bool animatorEnabled;
        private ITruckTaxiPedestrianRagdoll vendorRagdoll;
        private TruckTaxiWobbleVisual.Binding visualBinding;
        private Collider[] tractorColliders = Array.Empty<Collider>();
        private float expiresAt;
        private bool retiring;
        private static PhysicsMaterial impactMaterial;
        public bool IsRagdoll { get; private set; }
        public bool HitEventSent { get; private set; }
        public Vector3 LastImpulse { get; private set; }
        public float LastImpactSpeed { get; private set; }
        public bool HasJointedRagdoll => vendorRagdoll!=null && ragdollBodies.Length>1;
        public bool CanReuse
        {
            get
            {
                if(walkingBody==null || GetComponent<CapsuleCollider>()==null) return false;
                foreach(var body in ragdollBodies) if(body==null) return false;
                foreach(var joint in joints) if(joint==null) return false;
                return true;
            }
        }
        public bool IsFullPhysics { get; private set; } = true;
        public IReadOnlyList<Rigidbody> RagdollBodies => activeRagdollBodies ?? ragdollBodies;
        public string PedestrianId => GetComponent<TruckTaxiImpactTarget>()?.targetId;
        public event Action<TruckTaxiPedestrian> Expired;
        public event Action<TruckTaxiPedestrian> Ragdolled;
        public void SetVisualBinding(TruckTaxiWobbleVisual.Binding binding, bool wobbleVisible)
        {
            visualBinding = binding;
            if (!IsRagdoll) visualBinding?.SetWobbleVisible(wobbleVisible);
        }
        public void SetWobbleVisible(bool visible)
        {
            if (!IsRagdoll) visualBinding?.SetWobbleVisible(visible);
        }
        private void Awake()
        {
            // Do not RequireComponent: UTS legitimately destroys this body on activation.
            walkingBody=GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>(); animator=GetComponent<Animator>();
            foreach(var component in GetComponents<Behaviour>())
            {
                if(component is ITruckTaxiPedestrianRagdoll adapter) vendorRagdoll=adapter;
                var name=component.GetType().Name;
                if(name=="Passersby" || name=="MovePath" || name=="PeopleController" || name=="NavMeshAgent")
                { movement.Add(component); movementEnabled.Add(component.enabled); }
            }
            var bones=new List<Rigidbody>();
            foreach(var body in GetComponentsInChildren<Rigidbody>(true)) if(body!=walkingBody) bones.Add(body);
            ragdollBodies=bones.ToArray(); colliders=GetComponentsInChildren<Collider>(true);
            joints=GetComponentsInChildren<Joint>(true);
            ragdollTransforms=new Transform[ragdollBodies.Length];
            restPositions=new Vector3[ragdollBodies.Length]; restRotations=new Quaternion[ragdollBodies.Length];
            for(int i=0;i<ragdollBodies.Length;i++)
            { ragdollTransforms[i]=ragdollBodies[i].transform; restPositions[i]=ragdollTransforms[i].localPosition; restRotations[i]=ragdollTransforms[i].localRotation; }
            animatorEnabled=animator!=null && animator.enabled;
            walkingBody.mass=12;
            walkingBody.maxDepenetrationVelocity=6;
            if (impactMaterial == null)
            {
                impactMaterial = new PhysicsMaterial("Taxi pedestrian low-friction contact") {
                    dynamicFriction = .05f, staticFriction = .05f, bounciness = 0,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
            }
            foreach(var collider in colliders)
            {
                if(collider.GetComponentInParent<Rigidbody>(true)==walkingBody) walkingColliders.Add(collider);
                collider.gameObject.layer=gameObject.layer;
                if (!collider.isTrigger) collider.sharedMaterial=impactMaterial;
                // Only the root solid walking capsule participates before the hit.
                collider.enabled=!collider.isTrigger && walkingColliders.Contains(collider);
            }
            foreach(var body in ragdollBodies) body.isKinematic=true;
        }
        public void ConfigureTractor(Rigidbody body)
        {
            if (body == null) return;
            var candidates = body.GetComponentsInChildren<Collider>();
            var solids = new List<Collider>(candidates.Length);
            foreach (var candidate in candidates)
                if (candidate != null && !candidate.isTrigger) solids.Add(candidate);
            tractorColliders = solids.ToArray();
            IgnoreTractorCollisions(false);
            var walkingCapsule = GetComponent<CapsuleCollider>();
            if (walkingCapsule == null) return;
            var detector = new GameObject("Tractor pedestrian hit trigger");
            detector.layer = gameObject.layer;
            detector.transform.SetParent(transform, false);
            var trigger = detector.AddComponent<CapsuleCollider>();
            trigger.center = walkingCapsule.center;
            trigger.radius = walkingCapsule.radius + .3f;
            trigger.height = walkingCapsule.height + .4f;
            trigger.direction = walkingCapsule.direction;
            trigger.isTrigger = true;
            walkingColliders.Add(trigger);
            detector.AddComponent<TruckTaxiPedestrianTractorTrigger>().Initialize(this, body);
            colliders=GetComponentsInChildren<Collider>(true);
        }
        private void IgnoreTractorCollisions(bool ragdoll)
        {
            foreach (var pedestrianCollider in colliders)
            {
                if (pedestrianCollider == null || pedestrianCollider.isTrigger ||
                    walkingColliders.Contains(pedestrianCollider) == ragdoll) continue;
                foreach (var tractorCollider in tractorColliders)
                    if (tractorCollider != null) Physics.IgnoreCollision(pedestrianCollider, tractorCollider, true);
            }
        }
        public bool TryStrike(Vector3 relativeVelocity,Vector3 contact)
        {
            if(IsRagdoll || retiring || !TruckTaxiPedestrianImpactSettings.Finite(relativeVelocity) ||
                !TruckTaxiPedestrianImpactSettings.Finite(contact) || relativeVelocity.magnitude<Mathf.Max(.1f,settings.minimumRagdollImpactSpeed)) return false;
            IsRagdoll=true; LastImpactSpeed=relativeVelocity.magnitude;
            // The overlay is rigid; reveal UTS skinned renderers before its jointed ragdoll takes over.
            visualBinding?.SetWobbleVisible(false);
            LastImpulse=settings.CalculateImpulse(relativeVelocity);
            expiresAt=Time.time+Mathf.Clamp(settings.ragdollLifetime,1,60);
            foreach(var component in movement) if(component!=null) component.enabled=false;
            if(animator!=null) animator.enabled=false;
            if(HasJointedRagdoll)
            {
                foreach(var collider in colliders) if(collider!=null) collider.enabled=!collider.isTrigger && !walkingColliders.Contains(collider);
                IgnoreTractorCollisions(true);
                // UTS NPCStats.EnablePhysics destroys the walking body/capsule. Keep its
                // jointed rig but switch bodies here so cleanup can restore this instance.
                if(walkingBody!=null)
                {
                    if(!walkingBody.isKinematic) { walkingBody.linearVelocity=Vector3.zero; walkingBody.angularVelocity=Vector3.zero; }
                    walkingBody.isKinematic=true;
                }
            }
            else
            {
                Debug.LogWarning("PEDESTRIAN RAGDOLL NOT CONFIGURED: "+PedestrianId+"; using physical body fallback.",this);
                activeRagdollBodies=new[]{walkingBody};
                walkingBody.constraints=RigidbodyConstraints.None;
                walkingBody.isKinematic=false;
                foreach(var collider in colliders) if(collider!=null && !collider.isTrigger) collider.enabled=true;
            }
            var bodies=activeRagdollBodies ?? ragdollBodies;
            float mass=12f/bodies.Length;
            Rigidbody closest=null; float distance=float.PositiveInfinity;
            foreach(var body in bodies)
            {
                body.mass=mass; body.useGravity=true; body.isKinematic=false;
                body.maxDepenetrationVelocity=3; body.maxAngularVelocity=12; body.maxLinearVelocity=30;
                body.solverIterations=8; body.solverVelocityIterations=3;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero;
                body.WakeUp(); body.AddForce(LastImpulse/bodies.Length,ForceMode.Impulse);
                float d=(body.worldCenterOfMass-contact).sqrMagnitude;
                if(d<distance) { closest=body; distance=d; }
            }
            var axis=Vector3.Cross(Vector3.up,relativeVelocity.normalized);
            closest?.AddTorque(axis*Mathf.Clamp(settings.angularImpulse,0,5),ForceMode.Impulse);
            Ragdolled?.Invoke(this);
            return true;
        }
        public void MarkHitEventSent() => HitEventSent=true;
        public void SetFullPhysics(bool full)
        {
            if(IsRagdoll || IsFullPhysics==full) return;
            IsFullPhysics=full;
            if(full)
                for(int i=0;i<movement.Count;i++)
                    if(movement[i]!=null) movement[i].enabled=movementEnabled[i];
            if(walkingBody!=null)
            {
                if(!full && !walkingBody.isKinematic) { walkingBody.linearVelocity=Vector3.zero; walkingBody.angularVelocity=Vector3.zero; }
                walkingBody.isKinematic=!full;
            }
            foreach(var collider in colliders)
                if(collider!=null && walkingColliders.Contains(collider)) collider.enabled=full;
        }
        public void TickReducedMovement(bool updateThisFrame)
        {
            if(IsRagdoll || IsFullPhysics) return;
            for(int i=0;i<movement.Count;i++)
                if(movement[i]!=null && (movement[i].GetType().Name=="Passersby" ||
                    movement[i].GetType().Name=="PeopleController"))
                    movement[i].enabled=movementEnabled[i] && updateThisFrame;
        }
        public void PrepareForPool()
        {
            if(IsRagdoll) RestoreRagdoll();
            foreach(var component in movement) if(component!=null) component.enabled=false;
            if(animator!=null) animator.enabled=false;
            foreach(var collider in colliders) if(collider!=null) collider.enabled=false;
            if(walkingBody!=null)
            {
                if(!walkingBody.isKinematic) { walkingBody.linearVelocity=Vector3.zero; walkingBody.angularVelocity=Vector3.zero; }
                walkingBody.isKinematic=true;
            }
            foreach(var body in ragdollBodies)
                if(body!=null)
                {
                    if(!body.isKinematic) { body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero; }
                    body.isKinematic=true;
                }
            IsFullPhysics=false;
            gameObject.SetActive(false);
        }
        public void Reactivate(Vector3 position, Quaternion rotation, string id, bool fullPhysics)
        {
            RestoreRagdoll();
            ResetUtsController();
            transform.SetPositionAndRotation(position,rotation);
            var target=GetComponent<TruckTaxiImpactTarget>(); if(target!=null) target.targetId=id;
            HitEventSent=false; LastImpulse=Vector3.zero; LastImpactSpeed=0; retiring=false; expiresAt=0;
            gameObject.SetActive(true);
            for(int i=0;i<movement.Count;i++) if(movement[i]!=null) movement[i].enabled=movementEnabled[i];
            if(animator!=null) animator.enabled=animatorEnabled;
            IsFullPhysics=!fullPhysics;
            SetFullPhysics(fullPhysics);
        }
        private void RestoreRagdoll()
        {
            activeRagdollBodies=null;
            for(int i=0;i<ragdollBodies.Length;i++)
            {
                var body=ragdollBodies[i]; if(body==null) continue;
                if(!body.isKinematic) { body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero; }
                body.isKinematic=true;
                ragdollTransforms[i].localPosition=restPositions[i]; ragdollTransforms[i].localRotation=restRotations[i];
            }
            if(walkingBody!=null)
            {
                if(!walkingBody.isKinematic) { walkingBody.linearVelocity=Vector3.zero; walkingBody.angularVelocity=Vector3.zero; }
                walkingBody.isKinematic=true; walkingBody.constraints=RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            foreach(var collider in colliders)
                if(collider!=null) collider.enabled=collider.isTrigger
                    ? collider.GetComponent<TruckTaxiPedestrianTractorTrigger>()!=null
                    : walkingColliders.Contains(collider);
            IsRagdoll=false;
        }
        private void ResetUtsController()
        {
            const BindingFlags flags=BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            foreach(var component in movement)
            {
                if(component==null) continue;
                var type=component.GetType();
                if(type.Name=="Passersby")
                {
                    foreach(var name in new[]{"nearPassersby","nearSemaphore","nearCar","nearPlayer"})
                        type.GetField(name,flags)?.SetValue(component,null);
                    foreach(var name in new[]{"redSemaphore","insideSemaphore","tempStop"})
                        type.GetField(name,flags)?.SetValue(component,false);
                    type.GetField("timeToWalk",flags)?.SetValue(component,true);
                    type.GetField("curMoveSpeed",flags)?.SetValue(component,0f);
                    type.GetField("moveSpeed",flags)?.SetValue(component,0f);
                }
                else if(type.Name=="PeopleController")
                {
                    type.GetField("target",flags)?.SetValue(component,null);
                    type.GetField("timer",flags)?.SetValue(component,0f);
                }
            }
        }
        private void Update() { if(IsRagdoll && Time.time>=expiresAt) ResetPedestrian(); }
        public void ResetPedestrian()
        {
            if(retiring) return;
            retiring=true;
            if(Expired!=null) Expired(this); else Destroy(gameObject);
        }
    }

    // The walking capsule still collides with AI traffic. Only the canonical tractor uses this trigger.
    public sealed class TruckTaxiPedestrianTractorTrigger : MonoBehaviour
    {
        private TruckTaxiPedestrian pedestrian;
        private Rigidbody tractor;
        public void Initialize(TruckTaxiPedestrian value, Rigidbody body) { pedestrian = value; tractor = body; }
        private void OnTriggerEnter(Collider other) { Strike(other); }
        private void OnTriggerStay(Collider other) { Strike(other); }
        private void Strike(Collider other)
        {
            if (pedestrian == null || pedestrian.IsRagdoll || tractor == null || other.isTrigger ||
                !other.transform.IsChildOf(tractor.transform)) return;
            var host = TruckTaxiBootstrap.Instance;
            if (host == null || host.Session == null) return;
            var walking = pedestrian.GetComponent<Rigidbody>();
            Vector3 relative = tractor.linearVelocity - (walking != null ? walking.linearVelocity : Vector3.zero);
            Vector3 toward = Vector3.ProjectOnPlane(pedestrian.transform.position - tractor.worldCenterOfMass, Vector3.up);
            if (Vector3.Dot(tractor.linearVelocity, toward.normalized) <= .1f) return;
            Vector3 contact = pedestrian.transform.position + Vector3.up;
            if (!pedestrian.TryStrike(relative, contact)) return;
            host.Session.RecordPedestrianHit(pedestrian.PedestrianId, pedestrian.LastImpactSpeed, pedestrian.LastImpulse, contact);
            pedestrian.MarkHitEventSent();
        }
    }
}
