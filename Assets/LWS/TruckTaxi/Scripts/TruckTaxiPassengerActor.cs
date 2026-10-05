using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiActorLifecycle { OutsideWaiting, Boarding, Seated, Exiting, WalkingToContact, SketchyInteraction, Returning }

    public sealed class TruckTaxiPassengerActor : MonoBehaviour
    {
        public bool fallbackModel;
        public bool specialAnimationFallback;
        public Rigidbody[] ragdollBodies;
        private Animator animator;
        private TruckTaxiAnimatorProfile animationProfile;
        private float animationStart;
        private bool moving;
        private Rigidbody pickupTruck;
        private CapsuleCollider pickupSensor;
        private Rigidbody tumbleBody;
        private readonly List<Collider> ignoredTruckColliders = new List<Collider>();
        private readonly Collider[] waitingOverlaps = new Collider[24];
        public float MinimumWaitingImpactMetersPerSecond = 1.34f;
        public bool IsTumbling { get; private set; }
        public Vector3 StandingWorldScale { get; private set; }
        public float StandingRadius { get; private set; } = .55f;
        public float StandingHeight { get; private set; } = 1.8f;
        public float StandingGroundOffset { get; private set; }
        public Vector3 GroundPosition => transform.position - Vector3.up * StandingGroundOffset;
        public TruckTaxiActorLifecycle Lifecycle { get; private set; } = TruckTaxiActorLifecycle.OutsideWaiting;
        public event Action<TruckTaxiPassengerActor,Vector3> WaitingTruckHit;
        public void Bind(PassengerProfile passenger)
        {
            RestoreTruckCollisions();
            IsTumbling=false;
            pickupTruck=null;
            StandingWorldScale=transform.lossyScale;
            CaptureStandingDimensions();
            animator=GetComponentInChildren<Animator>(); animationProfile=passenger?.animatorProfile;
            if(animator!=null) { animator.applyRootMotion=false; if(animationProfile?.controller!=null) animator.runtimeAnimatorController=animationProfile.controller; }
            foreach(var body in GetComponentsInChildren<Rigidbody>())
            {
                if(!body.isKinematic)
                {
                    body.linearVelocity=Vector3.zero;
                    body.angularVelocity=Vector3.zero;
                }
                body.isKinematic=true;
            }
            foreach(var collider in GetComponentsInChildren<Collider>()) collider.enabled=false;
            if(pickupSensor!=null) pickupSensor.isTrigger=true;
            if(animator!=null) animator.enabled=true;
            Animate(TruckTaxiPassengerAnimation.Idle_Normal);
        }
        private void CaptureStandingDimensions()
        {
            bool found = false;
            Bounds bounds = default;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) return;
            Vector3 offset = bounds.center - transform.position;
            StandingRadius = Mathf.Max(.35f, new Vector2(Mathf.Abs(offset.x) + bounds.extents.x,
                Mathf.Abs(offset.z) + bounds.extents.z).magnitude + .05f);
            StandingGroundOffset = Mathf.Max(0, transform.position.y - bounds.min.y);
            StandingHeight = Mathf.Max(.7f, bounds.max.y - transform.position.y + StandingGroundOffset + .1f);
        }

        public void SetLifecycle(TruckTaxiActorLifecycle value) => Lifecycle = value;

        public void PrepareForWorld(Transform truck)
        {
            gameObject.SetActive(false);
            truck?.GetComponent<TruckTaxiCabOccupantRig>()?.Release(transform);
            DisarmWaitingTruckHit();
            EndLightTumble(transform.position);
            transform.SetParent(null, true);
            transform.localScale = StandingWorldScale;
            ResetStandingPhysics();
            Lifecycle = TruckTaxiActorLifecycle.Exiting;
        }

        public bool ShowInWorld(Vector3 ground, Transform truck)
        {
            if (!TruckTaxiActorWorldPlacement.Clear(ground, StandingRadius, StandingHeight, truck, transform)) return false;
            transform.SetPositionAndRotation(ground + Vector3.up * StandingGroundOffset, Quaternion.identity);
            var sway = GetComponentInChildren<TruckTaxiWobbleWalkSway>(true);
            if (sway != null) sway.enabled = true;
            Lifecycle = TruckTaxiActorLifecycle.OutsideWaiting;
            gameObject.SetActive(true);
            ArmWaitingTruckHit(truck.GetComponent<Rigidbody>());
            Animate(TruckTaxiPassengerAnimation.Idle_Normal);
            return true;
        }

        public void PrepareForSeat()
        {
            gameObject.SetActive(false);
            DisarmWaitingTruckHit();
            ResetStandingPhysics();
            Lifecycle = TruckTaxiActorLifecycle.Boarding;
        }

        public void CompleteSeating()
        {
            var sway = GetComponentInChildren<TruckTaxiWobbleWalkSway>(true);
            if (sway != null) sway.enabled = false;
            Lifecycle = TruckTaxiActorLifecycle.Seated;
            gameObject.SetActive(true);
            Animate(TruckTaxiPassengerAnimation.BoardAbstraction);
        }

        private void ResetStandingPhysics()
        {
            RestoreTruckCollisions();
            IsTumbling = false;
            foreach (var body in GetComponentsInChildren<Rigidbody>(true))
            {
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = true;
            }
            // Standing actors use the fitted trigger, never live ragdoll colliders.
            foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            if (pickupSensor != null) pickupSensor.isTrigger = true;
            if (animator != null) animator.enabled = true;
        }
        public void ArmWaitingTruckHit(Rigidbody truck)
        {
            pickupTruck=truck;
            pickupSensor=GetComponent<CapsuleCollider>();
            if(pickupSensor==null) pickupSensor=gameObject.AddComponent<CapsuleCollider>();
            FitWaitingSensor();
            pickupSensor.isTrigger=true;
            pickupSensor.enabled=true;
        }
        private void FitWaitingSensor()
        {
            bool found=false;
            Bounds visible=default;
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if(!found) { visible=renderer.bounds; found=true; }
                else visible.Encapsulate(renderer.bounds);
            }
            if(!found) visible=new Bounds(transform.position+Vector3.up*.85f,new Vector3(.7f,1.7f,.7f));
            var scale=transform.lossyScale;
            pickupSensor.center=transform.InverseTransformPoint(visible.center);
            pickupSensor.radius=Mathf.Clamp(Mathf.Max(visible.size.x/Mathf.Max(Mathf.Abs(scale.x),.001f),
                visible.size.z/Mathf.Max(Mathf.Abs(scale.z),.001f))*.42f,.23f,.65f);
            pickupSensor.height=Mathf.Max(pickupSensor.radius*2f,visible.size.y/Mathf.Max(Mathf.Abs(scale.y),.001f)*.95f);
        }
        public void DisarmWaitingTruckHit()
        {
            pickupTruck=null;
            if(pickupSensor!=null) pickupSensor.enabled=false;
        }
        private void OnTriggerEnter(Collider other)
        {
            CheckWaitingImpact(other);
        }
        private void OnTriggerStay(Collider other) { CheckWaitingImpact(other); }
        private void FixedUpdate()
        {
            if(pickupTruck==null || IsTumbling || pickupSensor==null || !pickupSensor.enabled) return;
            Vector3 center=transform.TransformPoint(pickupSensor.center);
            float scaleY=Mathf.Abs(transform.lossyScale.y);
            float radius=pickupSensor.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z));
            float half=Mathf.Max(0,pickupSensor.height*scaleY*.5f-radius);
            int count=Physics.OverlapCapsuleNonAlloc(center+transform.up*half,center-transform.up*half,
                radius,waitingOverlaps,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++) CheckWaitingImpact(waitingOverlaps[i]);
        }
        private void CheckWaitingImpact(Collider other)
        {
            if(pickupTruck==null || IsTumbling || other==null || !other.transform.IsChildOf(pickupTruck.transform)) return;
            Vector3 point=pickupSensor.ClosestPoint(other.bounds.center);
            Vector3 otherVelocity=other.attachedRigidbody!=null && other.attachedRigidbody!=pickupTruck ?
                other.attachedRigidbody.GetPointVelocity(point) : Vector3.zero;
            Vector3 relative=pickupTruck.GetPointVelocity(point)-otherVelocity;
            if(relative.sqrMagnitude<MinimumWaitingImpactMetersPerSecond*MinimumWaitingImpactMetersPerSecond) return;
            WaitingTruckHit?.Invoke(this,relative);
        }
        public void BeginLightTumble(Vector3 truckVelocity)
        {
            if(IsTumbling) return;
            IsTumbling=true;
            var truck=pickupTruck;
            DisarmWaitingTruckHit();
            if(animator!=null) animator.enabled=false;
            tumbleBody=GetComponent<Rigidbody>();
            if(tumbleBody==null) tumbleBody=gameObject.AddComponent<Rigidbody>();
            tumbleBody.mass=65f;
            tumbleBody.isKinematic=false;
            tumbleBody.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            if(pickupSensor!=null) { pickupSensor.isTrigger=false; pickupSensor.enabled=true; }
            if(truck!=null && pickupSensor!=null)
                foreach(var collider in truck.GetComponentsInChildren<Collider>())
                {
                    if(collider==null || !collider.enabled) continue;
                    Physics.IgnoreCollision(pickupSensor,collider,true);
                    ignoredTruckColliders.Add(collider);
                }
            tumbleBody.linearVelocity=Vector3.ClampMagnitude(truckVelocity*.25f+Vector3.up*1.5f,4f);
            tumbleBody.angularVelocity=new Vector3(0,1.5f,1f);
        }
        public void EndLightTumble(Vector3 standingPosition)
        {
            if(tumbleBody!=null)
            {
                if (!tumbleBody.isKinematic) { tumbleBody.linearVelocity=Vector3.zero; tumbleBody.angularVelocity=Vector3.zero; }
                tumbleBody.isKinematic=true;
            }
            RestoreTruckCollisions();
            if(pickupSensor!=null) pickupSensor.enabled=false;
            transform.SetPositionAndRotation(standingPosition,Quaternion.identity);
            if(animator!=null) animator.enabled=true;
            IsTumbling=false;
            Animate(TruckTaxiPassengerAnimation.Idle_Normal);
        }
        private void RestoreTruckCollisions()
        {
            if(pickupSensor!=null)
                foreach(var collider in ignoredTruckColliders)
                    if(collider!=null) Physics.IgnoreCollision(pickupSensor,collider,false);
            ignoredTruckColliders.Clear();
        }
        private void OnDestroy() { RestoreTruckCollisions(); }
        public void Animate(TruckTaxiPassengerAnimation action)
        {
            moving=action==TruckTaxiPassengerAnimation.ApproachVehicle || action==TruckTaxiPassengerAnimation.Walk_Normal;
            animationStart=Time.time;
            if(animator==null || animator.runtimeAnimatorController==null) return;
            string state=animationProfile!=null ? animationProfile.StateFor(action) : "Idle_Normal";
            int hash=Animator.StringToHash(state);
            if(animator.HasState(0,hash)) animator.CrossFade(hash,.12f);
        }
        public void Eject(Vector3 velocity,Transform truck)
        {
            transform.SetParent(null,true);
            if(animator!=null) animator.enabled=false;
            var allBodies=ragdollBodies;
            if(allBodies==null || allBodies.Length==0)
            {
                var body=GetComponent<Rigidbody>(); if(body==null) body=gameObject.AddComponent<Rigidbody>(); body.mass=65;
                var capsule=GetComponent<CapsuleCollider>(); if(capsule==null) capsule=gameObject.AddComponent<CapsuleCollider>();
                  capsule.height=1.5f; capsule.radius=.3f; capsule.center=Vector3.up*.8f;
                  capsule.isTrigger=false;
                allBodies=new[]{body};
            }
            var own=GetComponentsInChildren<Collider>(); var vehicle=truck.GetComponentsInChildren<Collider>();
            foreach(var collider in own)
            {
                collider.enabled=true;
                foreach(var other in vehicle) Physics.IgnoreCollision(collider,other,true);
            }
            foreach(var body in allBodies)
            {
                if(body==null) continue;
                body.isKinematic=false; body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                body.linearVelocity=Vector3.ClampMagnitude(velocity,22); body.angularVelocity=new Vector3(0,1,2);
            }
            Destroy(gameObject,12);
        }
        private void Update()
        {
            var body=GetComponent<Rigidbody>();
            if(!specialAnimationFallback || (body!=null && !body.isKinematic)) return;
            // Intentional special-creature fallback, not a substitute humanoid rig.
            if(moving && transform.childCount>0)
                transform.GetChild(0).localRotation=Quaternion.Euler(0,0,Mathf.Sin((Time.time-animationStart)*8)*4);
        }
    }
}
