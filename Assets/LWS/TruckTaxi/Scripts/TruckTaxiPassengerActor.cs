using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiPassengerActor : MonoBehaviour
    {
        public bool fallbackModel;
        public bool specialAnimationFallback;
        public Rigidbody[] ragdollBodies;
        private Animator animator;
        private TruckTaxiAnimatorProfile animationProfile;
        private float animationStart;
        private bool moving;
        public void Bind(PassengerProfile passenger)
        {
            animator=GetComponentInChildren<Animator>(); animationProfile=passenger.animatorProfile;
            if(animator!=null) { animator.applyRootMotion=false; if(animationProfile?.controller!=null) animator.runtimeAnimatorController=animationProfile.controller; }
            foreach(var body in GetComponentsInChildren<Rigidbody>()) body.isKinematic=true;
            foreach(var collider in GetComponentsInChildren<Collider>()) collider.enabled=false;
            Animate(TruckTaxiPassengerAnimation.Idle_Normal);
        }
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
