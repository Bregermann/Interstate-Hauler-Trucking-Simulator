using UnityEngine;
using DG.Tweening;
using LWS.TruckPedestrianPlowing;
namespace LWS.TruckTrailerPlug
{
    // Mode-specific entrance over the shared pedestrian/Taxi ragdoll. Once out,
    // the existing navigation and reaction controller owns movement again.
    public sealed class TrailerPlugEmergence : MonoBehaviour
    {
        public bool Active { get; private set; }
        Tween entrance;
        PlowPedestrian pedestrian;
        Rigidbody walkingBody;
        readonly Collider[] clearance=new Collider[64];
        public void Begin(PlowPedestrian actor, Vector3 destination)
        {
            Cancel(); pedestrian = actor; Active = true; actor.agent.enabled = false;
            var body = actor.GetComponent<Rigidbody>(); walkingBody=body; body.isKinematic = true;
            // A cosmetic entrance must never act as an infinite-mass piston
            // underneath a physically settling trailer.
            body.detectCollisions=false;
            body.position = destination - Vector3.up * 2.8f; transform.position = body.position;
            if (actor.animator != null) actor.animator.CrossFade("run", .12f);
            entrance = transform.DOMove(destination, 1.1f).SetEase(Ease.OutQuad).SetUpdate(UpdateType.Fixed)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable).OnComplete(() =>
                {
                    Active = false; entrance = null;
                    if (actor == null || actor.Claimed || !actor.gameObject.activeInHierarchy) return;
                    Vector3 clear=destination;bool found=false;
                    for(int attempt=0;attempt<10;attempt++){
                        clear=destination+(attempt==0?Vector3.zero:Vector3.ProjectOnPlane(destination,Vector3.up).normalized*attempt*.65f);
                        if(!UnityEngine.AI.NavMesh.SamplePosition(clear,out var nav,.7f,UnityEngine.AI.NavMesh.AllAreas))continue;clear=nav.position;
                        int count=Physics.OverlapCapsuleNonAlloc(clear+Vector3.up*.4f,clear+Vector3.up*1.6f,.33f,clearance,~0,QueryTriggerInteraction.Ignore);
                        bool blocked=count==clearance.Length;for(int i=0;i<count;i++)if(!clearance[i].transform.IsChildOf(actor.transform)&&clearance[i].bounds.max.y>clear.y+.35f){blocked=true;break;}
                        if(!blocked){found=true;break;}
                    }
                    if(!found){actor.ragdoll.PrepareForPool();return;}
                    body.position=clear;actor.transform.position=clear;body.detectCollisions=true;
                    actor.agent.enabled = true; actor.agent.Warp(clear); actor.agent.isStopped = false;
                });
        }
        public void Cancel() { entrance?.Kill(); entrance = null; Active = false;if(walkingBody!=null)walkingBody.detectCollisions=true; }
        void OnDisable() { Cancel(); }
        void OnDestroy() { Cancel(); }
    }
}
