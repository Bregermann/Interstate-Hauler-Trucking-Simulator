using UnityEngine;
namespace LWS.TruckHockey {
// PhysX is the sole contact impulse authority. This controller only applies drive, grip and braking.
public sealed class HockeyTruck:MonoBehaviour {
 public HockeyMatch match;public Rigidbody body;public int side;public float acceleration=18,maxSpeed=8.2f,reverseSpeed=5,turnDegrees=160,lateralGrip=3.5f,rollingDrag=.65f,brakeDrag=8,recoverCooldown=2;
 [System.NonSerialized]public float throttle,steer;[System.NonSerialized]public bool brake;float nextRecover,contactAudioTime;bool opposing;public bool BrakingOpposite{get;private set;}public int PuckContacts{get;private set;}
 public void Control(float gas,float turn,bool braking){throttle=Mathf.Clamp(gas,-1,1);steer=Mathf.Clamp(turn,-1,1);brake=braking;}
 public void ClearInput(){throttle=steer=0;brake=opposing=BrakingOpposite=false;}
 public void ResetBody(Vector3 p,Quaternion rotation){body.position=p;body.rotation=rotation;if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}ClearInput();if(match!=null&&match.puckContact!=null)match.puckContact.ForgetTruckContact(side);}
 void FixedUpdate(){if(match==null||!match.rules.Live||match.MenuOpen){ClearInput();return;}float dt=Time.fixedDeltaTime;var v=body.linearVelocity;var forward=transform.forward;float speed=Vector3.Dot(v,forward);bool requested=Mathf.Abs(throttle)>.08f&&speed*throttle<0;
 if(!requested||Mathf.Abs(speed)<.12f)opposing=false;else if(Mathf.Abs(speed)>.3f)opposing=true;BrakingOpposite=opposing;
 float steeringFactor=Mathf.Clamp01(Mathf.Abs(speed)/1.6f);float reverse=speed<-.15f?-1:1;body.MoveRotation(body.rotation*Quaternion.Euler(0,steer*turnDegrees*steeringFactor*reverse*dt,0));
 if(!opposing&&!brake)body.AddForce(forward*throttle*acceleration,ForceMode.Acceleration);Vector3 lateral=transform.right*Vector3.Dot(v,transform.right);body.AddForce(-lateral*lateralGrip-v*(opposing||brake?brakeDrag:rollingDrag),ForceMode.Acceleration);float cap=speed<-.12f?reverseSpeed:maxSpeed;body.linearVelocity=Vector3.ClampMagnitude(v,cap);}
 public bool Recover(bool fromPause=false){if(!match.rules.Live&&!(fromPause&&match.rules.Phase==HockeyPhase.Paused))return false;if(match.MenuOpen&&!fromPause||Time.unscaledTime<nextRecover)return false;if(!match.SafeRecover(side,out var p))return false;ResetBody(p,match.SpawnRotation(side));match.ClearRecoveryHistory();nextRecover=Time.unscaledTime+recoverCooldown;match.Notify(match.PlayerName(side)+" recovered in their own half");return true;}
 void OnCollisionEnter(Collision c){if(!match.rules.Live||match.MenuOpen)return;float strength=c.relativeVelocity.magnitude;if(c.rigidbody==match.puck)PuckContacts++;if(strength>.5f&&Time.time>=contactAudioTime){contactAudioTime=Time.time+.12f;match.feedback.Pulse(match.players[side].controller?match.players[side].pad:null,Mathf.Clamp(strength*.017f,.025f,.16f),.08f);var other=c.rigidbody!=null?c.rigidbody.GetComponent<HockeyTruck>():null;if(c.rigidbody==null||c.rigidbody==match.puck||(other!=null&&side<other.side)){if(c.rigidbody!=match.puck)match.feedback.Play(3,strength);match.contacts?.Impact(c.GetContact(0).point,strength,match.players[side].color);}}}
}
}