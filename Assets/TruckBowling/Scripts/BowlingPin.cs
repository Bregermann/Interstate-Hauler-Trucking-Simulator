using UnityEngine;
namespace LWS.TruckBowling {
public sealed class BowlingPin:MonoBehaviour {
 public Rigidbody body;public BowlingLane lane;public int index;public bool credited;public float downTime;float audioAt;
 public bool Down=>Vector3.Dot(transform.up,Vector3.up)<.72f||transform.position.y<-.1f||Mathf.Abs(transform.position.x-lane.center)>lane.halfWidth+.3f||transform.position.z>lane.pinLine+5;
 public bool Quiet=>!gameObject.activeSelf||body.isKinematic||body.linearVelocity.sqrMagnitude<.025f&&body.angularVelocity.sqrMagnitude<.16f;
 void FixedUpdate(){if(!body.isKinematic&&gameObject.activeSelf)downTime=Down?downTime+Time.fixedDeltaTime:0;}
 void OnCollisionEnter(Collision c){if(lane==null||!lane.ScoringLive||Time.time<audioAt)return;audioAt=Time.time+.09f;lane.match.feedback.Play(2,c.relativeVelocity.magnitude);}
 public void Freeze(){if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=true;}
 public void Release(){downTime=0;body.isKinematic=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.WakeUp();}
}
}