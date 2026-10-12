using UnityEngine;
namespace LWS.TruckHockey {
// Planar sports contact body, deliberately separate from Hauler road/wheel dynamics.
public sealed class HockeyTruck:MonoBehaviour {
 public HockeyMatch match;public Rigidbody body;public int side;public float acceleration=18,maxSpeed=8.2f,reverseSpeed=5,turnDegrees=160,lateralGrip=3.5f,rollingDrag=.65f,brakeDrag=8,recoverCooldown=2;
 [System.NonSerialized]public float throttle,steer;[System.NonSerialized]public bool brake;float nextRecover,contactAudioTime;
 public int PuckContacts{get;private set;}
 public void Control(float gas,float turn,bool braking){throttle=Mathf.Clamp(gas,-1,1);steer=Mathf.Clamp(turn,-1,1);brake=braking;}
 public void ClearInput(){throttle=steer=0;brake=false;}
 public void ResetBody(Vector3 p,Quaternion rotation){body.position=p;body.rotation=rotation;if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}ClearInput();if(match!=null&&match.puckContact!=null)match.puckContact.ForgetTruckContact(side);nextRecover=0;}
 void FixedUpdate(){if(match==null||!match.rules.Live||match.MenuOpen){ClearInput();return;}float dt=Time.fixedDeltaTime;var v=body.linearVelocity;var forward=transform.forward;float speed=Vector3.Dot(v,forward);float steeringFactor=Mathf.Clamp01(Mathf.Abs(speed)/1.6f);float reverse=speed<-.15f?-1:1;body.MoveRotation(body.rotation*Quaternion.Euler(0,steer*turnDegrees*steeringFactor*reverse*dt,0));
 body.AddForce(forward*throttle*acceleration,ForceMode.Acceleration);Vector3 lateral=transform.right*Vector3.Dot(v,transform.right);body.AddForce(-lateral*lateralGrip-v*(brake?brakeDrag:rollingDrag),ForceMode.Acceleration);float cap=throttle<0?reverseSpeed:maxSpeed;body.linearVelocity=Vector3.ClampMagnitude(v,cap);}
 public bool Recover(){if(!match.rules.Live||match.MenuOpen||Time.time<nextRecover)return false;if(!match.SafeRecover(side,out var p))return false;ResetBody(p,match.SpawnRotation(side));nextRecover=Time.time+recoverCooldown;match.Notify(match.PlayerName(side)+" recovered in their own half");return true;}
 void OnCollisionEnter(Collision c){if(!match.rules.Live||match.MenuOpen)return;if(c.rigidbody==match.puck){PuckContacts++;match.feedback.Pulse(match.players[side].pad,.12f,.1f);}if(Time.time>=contactAudioTime&&c.rigidbody!=match.puck){contactAudioTime=Time.time+.12f;if(c.rigidbody==null||side==0)match.feedback.Play(3,c.relativeVelocity.magnitude);}}
}
}
