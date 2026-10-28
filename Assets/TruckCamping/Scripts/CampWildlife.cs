using UnityEngine;
using UnityEngine.AI;
namespace LWS.TruckCamping
{
    // Habitat semantics only. Unity Navigation owns path following; the actual
    // rig's Animator or explicitly bound bones own movement presentation.
    public sealed class CampWildlife : MonoBehaviour
    {
        public CampingWilderness world;
        public NavMeshAgent agent;
        public CampLivingArt living;
        public Animator animator;
        public Transform[] route;
        public string idleState = "Idle", walkState = "Walk";
        public float pace = 1.4f, waitSeconds = 4;
        public int BoneFrames => living != null ? living.AnimatedFrames : frames;
        public int Arrivals { get; private set; }
        int target, frames;
        float wait, nextLook;
        string state;
        void Start()
        {
            if (route == null || route.Length < 2 || agent == null || !agent.isOnNavMesh) throw new System.InvalidOperationException("Animated wildlife requires its actual shared forest NavMesh and authored habitat route.");
            agent.speed = pace; agent.acceleration = 3; agent.angularSpeed = 90; agent.autoRepath = true;
            agent.SetDestination(route[0].position); wait = waitSeconds;
            if (animator != null) { animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
        }
        void Update()
        {
            if (Time.deltaTime <= 0 || agent == null || !agent.isOnNavMesh) return;
            bool live = world.session != null && world.session.Live;
            agent.isStopped = !live; if (!live) return;
            frames++;
            bool nearTruck = false;
            foreach (var explorer in world.session.explorers)
                if (explorer.gameObject.activeInHierarchy && Vector3.Distance(explorer.truck.body.position, transform.position) < 11) { nearTruck = true; break; }
            if (nearTruck && Time.time > nextLook)
            {
                nextLook = Time.time + 7;
                target = (target + 1) % route.Length;
                agent.SetDestination(route[target].position); wait = 0;
            }
            if (!agent.pathPending && agent.remainingDistance < 1.2f)
            {
                wait -= Time.deltaTime;
                if (wait <= 0) { Arrivals++; target = (target + 1) % route.Length; agent.SetDestination(route[target].position); wait = waitSeconds; }
            }
            bool moving = agent.velocity.sqrMagnitude > .04f;
            if (living != null) {living.LocomotionSpeed=agent.velocity.magnitude;living.Request(moving ? CampPose.Walk : CampPose.Look);}
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                string desired = moving ? walkState : idleState;
                if (state != desired) { animator.CrossFadeInFixedTime(desired, .25f); state = desired; }
            }
        }
    }
}
