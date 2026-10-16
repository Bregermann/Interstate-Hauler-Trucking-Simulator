using UnityEngine;
namespace LWS.TruckBowling {
public sealed class BowlingPin:MonoBehaviour {
 public Rigidbody body;public BowlingLane lane;public int index;public bool credited;public bool RegisteredDown{get;private set;}public Vector3 IncomingVelocity{get;private set;}public float downTime;float audioAt;
 public bool Down=>Vector3.Dot(transform.up,Vector3.up)<.72f||transform.position.y<-.1f||Mathf.Abs(transform.position.x-lane.center)>lane.halfWidth+.3f||transform.position.z>lane.pinLine+5;
 public bool Quiet=>!gameObject.activeSelf||body.isKinematic||body.linearVelocity.sqrMagnitude<.025f&&body.angularVelocity.sqrMagnitude<.16f;
 void FixedUpdate(){if(!body.isKinematic&&gameObject.activeSelf){IncomingVelocity=body.linearVelocity;if(lane.ScoringLive){downTime=Down?downTime+Time.fixedDeltaTime:0;if(downTime>=.3f)RegisteredDown=true;}}}
 void OnCollisionEnter(Collision c){if(lane==null||!lane.ScoringLive)return;var wall=c.collider.GetComponent<BowlingReboundWall>();if(wall!=null)wall.Contact(this,c);if(Time.time<audioAt)return;audioAt=Time.time+.09f;lane.match.feedback.Play(2,c.relativeVelocity.magnitude);}
 public void Freeze(){if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=true;}
 public void Release(){RegisteredDown=false;downTime=0;body.isKinematic=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.WakeUp();}
}
}