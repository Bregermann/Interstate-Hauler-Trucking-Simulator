using UnityEngine;using UnityEngine.AI;
namespace LWS.TruckSheepHerding
{
 // Unity AI Navigation owns pathfinding/avoidance. This component supplies
 // sport-specific threat/flock destinations and synchronizes the actual body.
 public sealed class HerdSheep:MonoBehaviour
 {
  public HerdMatch match;public int id;public bool explosive;public Rigidbody body;public CapsuleCollider shape;public NavMeshAgent agent;public GameObject warning;public float rootToGround;public float ThinkAt{get;private set;}public Vector3 RequestedGoal{get;private set;}public int Repaths{get;private set;}
  public void ResetSheep(Vector3 point,Quaternion rotation)
  {
   agent.enabled=false;gameObject.SetActive(true);body.isKinematic=true;body.interpolation=RigidbodyInterpolation.None;
   if(!NavMesh.SamplePosition(point,out var hit,4,NavMesh.AllAreas))throw new System.InvalidOperationException("Sheep spawn must lie on the authored connected farm navigation: "+id);
   body.position=hit.position+Vector3.up*rootToGround;body.rotation=rotation;transform.SetPositionAndRotation(body.position,rotation);agent.updatePosition=agent.updateRotation=false;agent.enabled=true;if(!agent.Warp(hit.position))throw new System.InvalidOperationException("Sheep navigation warp failed: "+id);agent.isStopped=true;agent.ResetPath();body.interpolation=RigidbodyInterpolation.Interpolate;ThinkAt=0;Repaths=0;if(warning!=null)warning.SetActive(explosive);
  }
  public void Step(float dt)
  {
   if(!gameObject.activeSelf||!agent.enabled||!agent.isOnNavMesh)return;
   if(!match.Live||!match.rules.Live){agent.isStopped=true;return;}
   if(match.rules.owners[id]>=0){agent.isStopped=true;return;}
   agent.isStopped=false;
   if(match.rules.Clock>=ThinkAt)
   {
    ThinkAt=match.rules.Clock+.28f+(id%5)*.02f;RequestedGoal=match.ThreatGoal(this,out float speed);agent.speed=speed;
    if(NavMesh.SamplePosition(RequestedGoal,out var hit,4,NavMesh.AllAreas)){agent.SetDestination(hit.position);Repaths++;}
   }
   var next=agent.nextPosition+Vector3.up*rootToGround;var movement=Vector3.ProjectOnPlane(next-body.position,Vector3.up);body.MovePosition(next);if(movement.sqrMagnitude>.0001f)body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(movement),dt*170));
  }
  public void Stop(){if(agent.enabled&&agent.isOnNavMesh){agent.isStopped=true;agent.ResetPath();}}
  void OnCollisionEnter(Collision collision){if(!explosive||!match.Live)return;var truck=collision.rigidbody!=null?collision.rigidbody.GetComponent<LWS.TruckSports.SportsEventTruck>():null;if(truck!=null&&collision.relativeVelocity.magnitude>=3)match.QueueExplosion(id);}
 }
}
