using System;
using LWS.TruckCamping;
using UnityEngine;
using UnityEngine.AI;

namespace LWS.TruckDragonKingdom.Ambience
{
    public sealed class DragonAmbientWalker : MonoBehaviour
    {
        public string regionId, sourceAsset;
        public bool villager;
        public NavMeshAgent agent;
        public Animator animator;
        public CampLivingArt living;
        public Transform[] route=Array.Empty<Transform>();
        public Renderer[] visuals=Array.Empty<Renderer>();
        public string idleState="Idle",walkState="Walk";
        public float pace=1.1f,waitSeconds=3;
        public Vector3 authoredPosition;
        public Quaternion authoredRotation=Quaternion.identity;
        public int Arrivals { get; private set; }
        public bool Active { get; private set; }
        int target;float wait;string current;
        readonly RaycastHit[] obstacles=new RaycastHit[16];
        Quaternion[] frozenRotations;
        Vector3[] frozenPositions;
        public void ValidateBindings()
        {
            if(agent==null||route.Length<3||visuals.Length==0||string.IsNullOrWhiteSpace(sourceAsset)||animator==null&&living==null)throw new InvalidOperationException("An actual animated rig and physically validated habitat route are required.");
            if(animator!=null&&(animator.avatar==null||!animator.avatar.isValid||animator.runtimeAnimatorController==null||animator.runtimeAnimatorController.animationClips.Length==0))throw new InvalidOperationException("Actual compatible villager/fauna animation required.");
        }
        public void ResetForMatch(int seed)
        {
            SetActivity(false,false);agent.enabled=true;
            if(!agent.Warp(authoredPosition)||!agent.isOnNavMesh)throw new InvalidOperationException("Authored ambient start no longer rests on its real bounded NavMesh: "+name);
            transform.rotation=authoredRotation;target=0;wait=waitSeconds;Arrivals=0;current=null;
            agent.speed=pace;agent.acceleration=2;agent.angularSpeed=120;agent.stoppingDistance=.25f;agent.autoRepath=true;
            agent.SetDestination(route[target].position);agent.isStopped=true;
        }
        public void SetActivity(bool live,bool visible)
        {
            Active=live&&visible;
            if(agent!=null&&agent.enabled&&agent.isOnNavMesh)agent.isStopped=!Active;
            if(animator!=null)animator.enabled=Active;
            if(living!=null&&living.enabled!=Active)
            {
                if(Active)living.enabled=true;
                else FreezeLivingPose();
            }
            foreach(var renderer in visuals)if(renderer!=null)renderer.enabled=visible;
        }
        void FreezeLivingPose()
        {
            // The existing component restores its bind pose in OnDisable.
            // Preserve the observed pose around that lifecycle callback so
            // pausing/culling freezes actual articulation instead of snapping.
            var bones=living.bones;
            if(frozenRotations==null||frozenRotations.Length!=bones.Length){frozenRotations=new Quaternion[bones.Length];frozenPositions=new Vector3[bones.Length];}
            for(int i=0;i<bones.Length;i++){frozenRotations[i]=bones[i].localRotation;frozenPositions[i]=bones[i].localPosition;}
            Vector3 position=living.model.localPosition;Quaternion rotation=living.model.localRotation;
            living.enabled=false;
            for(int i=0;i<bones.Length;i++){bones[i].localRotation=frozenRotations[i];bones[i].localPosition=frozenPositions[i];}
            living.model.SetLocalPositionAndRotation(position,rotation);
        }
        void Update()
        {
            if(!Active||agent==null||!agent.isOnNavMesh||Time.deltaTime<=0)return;
            // Navigation owns steering. A native swept-body check yields to
            // newly moving trucks/props instead of clipping through them.
            Vector3 velocity=agent.desiredVelocity;
            bool blocked=false;
            if(velocity.sqrMagnitude>.01f)
            {
                Vector3 foot=transform.position+Vector3.up*(agent.radius+.12f);
                int count=Physics.CapsuleCastNonAlloc(foot,foot+Vector3.up*Mathf.Max(0,agent.height-agent.radius*2-.12f),agent.radius,velocity.normalized,obstacles,Mathf.Max(.8f,velocity.magnitude*Time.deltaTime+.3f),~0,QueryTriggerInteraction.Ignore);
                blocked=count==obstacles.Length;for(int i=0;i<count;i++)if(obstacles[i].collider!=null&&!obstacles[i].collider.transform.IsChildOf(transform)&&obstacles[i].normal.y<.7f)blocked=true;
            }
            agent.isStopped=blocked;
            if(!agent.pathPending&&agent.remainingDistance<.6f)
            {
                agent.isStopped=true;wait-=Time.deltaTime;
                if(wait<=0){Arrivals++;target=(target+1)%route.Length;agent.SetDestination(route[target].position);wait=waitSeconds;agent.isStopped=blocked;}
            }
            bool moving=!agent.isStopped&&agent.velocity.sqrMagnitude>.025f;
            if(living!=null){living.LocomotionSpeed=moving?agent.velocity.magnitude:0;living.Request(moving?CampPose.Walk:CampPose.Look);}
            if(animator!=null&&living==null)
            {
                string desired=moving?walkState:idleState;
                if(current!=desired){animator.CrossFadeInFixedTime(desired,.25f);current=desired;}
                animator.speed=moving?Mathf.Clamp(agent.velocity.magnitude/Mathf.Max(.5f,pace),.35f,1.4f):1;
            }
        }
    }
}
