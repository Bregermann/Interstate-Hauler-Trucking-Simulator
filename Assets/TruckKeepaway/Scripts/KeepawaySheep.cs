using UnityEngine;
namespace LWS.TruckKeepaway {
public sealed class KeepawaySheep:MonoBehaviour {
 public KeepawayChaos arena;public int index;public Rigidbody body;public bool Queued{get;private set;}public bool Active{get;private set;}public float ReturnAt{get;private set;}Vector3 destination;
 public void Dormant(){Active=Queued=false;ReturnAt=float.PositiveInfinity;gameObject.SetActive(false);}
 public void ResetSheep(Vector3 p){Queued=false;Active=true;gameObject.SetActive(true);body.position=p;body.rotation=Quaternion.Euler(0,(index*137)%360,0);if(!body.isKinematic)body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;destination=p;}
 public void Queue(){if(!Active||Queued||!arena.match.Live)return;Queued=true;arena.Queue(this);}
 public void Detonated(float time){Active=false;Queued=false;ReturnAt=time+9+index%4;gameObject.SetActive(false);}
 public void Step(float time,float dt){if(!Active){if(time>=ReturnAt&&arena.SafeSheepSpawn(index,out var p))ResetSheep(p);return;}if(Queued)return;if((destination-body.position).sqrMagnitude<1||!arena.Reachable(destination))destination=arena.Wander(index,time);var delta=Vector3.ProjectOnPlane(destination-body.position,Vector3.up);if(delta.sqrMagnitude<.1f)return;var next=body.position+delta.normalized*1.1f*dt;next.y=.65f;body.MovePosition(next);body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(delta),dt*95));}
 void OnCollisionEnter(Collision c){if(c.rigidbody!=null&&c.rigidbody.GetComponent<LWS.TruckSports.SportsEventTruck>()!=null)Queue();}
}
}
