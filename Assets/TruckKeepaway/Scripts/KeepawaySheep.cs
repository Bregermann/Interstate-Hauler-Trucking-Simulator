using UnityEngine;
namespace LWS.TruckKeepaway {
public sealed class KeepawaySheep:MonoBehaviour {
 public KeepawayChaos arena;public int index;public Rigidbody body;public float rootToFeet=.6f;public bool Queued{get;private set;}public bool Active{get;private set;}public float ReturnAt{get;private set;}Vector3 destination;float restUntil;
 public void Dormant(){Active=Queued=false;ReturnAt=float.PositiveInfinity;gameObject.SetActive(false);}
 public void ResetSheep(Vector3 p){Queued=false;Active=true;gameObject.SetActive(true);p=Grounded(p);body.position=p;body.rotation=Quaternion.Euler(0,(index*137)%360,0);if(!body.isKinematic)body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;destination=p;restUntil=0;}
 public void Queue(){if(!Active||Queued||!arena.match.Live)return;Queued=true;arena.Queue(this);}
 public void Detonated(float time){Active=false;Queued=false;ReturnAt=time+9+index%4;gameObject.SetActive(false);}
 Vector3 Grounded(Vector3 p){var surface=arena!=null&&arena.ground!=null&&arena.ground.surface!=null?arena.ground.surface.GetComponent<Collider>():null;if(surface!=null&&surface.Raycast(new Ray(new Vector3(p.x,6,p.z),Vector3.down),out var hit,12))p.y=hit.point.y+rootToFeet;return p;}
 public void Step(float time,float dt){if(!Active){if(time>=ReturnAt&&arena.SafeSheepSpawn(index,out var p))ResetSheep(p);return;}if(Queued)return;bool hurry=false;for(int i=0;i<arena.match.setup.count;i++)if(arena.match.alive[i]&&(arena.match.trucks[i].body.position-body.position).sqrMagnitude<36)hurry=true;if(!hurry&&time<restUntil){body.MovePosition(Grounded(body.position));return;}if((destination-body.position).sqrMagnitude<1||!arena.Reachable(destination)){destination=arena.Wander(index,time+index*3);if(!hurry){restUntil=time+.8f+index%3*.35f;return;}}var delta=Vector3.ProjectOnPlane(destination-body.position,Vector3.up);if(delta.sqrMagnitude<.1f)return;var next=Grounded(body.position+delta.normalized*(hurry?2.6f:1.1f)*dt);body.MovePosition(next);body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(delta),dt*(hurry?170:95)));}
 void OnCollisionEnter(Collision c){if(c.rigidbody!=null&&c.rigidbody.GetComponent<LWS.TruckSports.SportsEventTruck>()!=null)Queue();}
}
}
