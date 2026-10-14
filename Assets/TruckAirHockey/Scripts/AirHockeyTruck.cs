using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckAirHockey {
public sealed class AirHockeyTruck:MonoBehaviour {
 public AirHockeyMatch match;public Rigidbody body;public BoxCollider shape;public int slot;
 public float acceleration=24,maxSpeed=9,reverseSpeed=6,turnDegrees=205,lateralGrip=6,rollingDrag=.85f,brakeDrag=9;
 public float throttle,steer;bool opposing;public bool BrakingOpposite{get;private set;}public int PuckContacts{get;private set;}
 public void Control(Vector2 v){steer=Mathf.Clamp(v.x,-1,1);throttle=Mathf.Clamp(v.y,-1,1);}public void Stop(){throttle=steer=0;opposing=false;}
 public void ResetBody(Vector3 p,Quaternion rotation){body.position=p;body.rotation=rotation;if(!body.isKinematic){body.linearVelocity=body.angularVelocity=Vector3.zero;}Stop();}
 void FixedUpdate(){if(!match.rules.Live||match.MenuOpen){Stop();return;}BrakingOpposite=SportsPlanarDrive.Step(body,throttle,steer,false,acceleration,maxSpeed,reverseSpeed,turnDegrees,lateralGrip,rollingDrag,brakeDrag,11,ref opposing);Constrain();}
 // Rotation can extend an oblong body through a center wall. Project the entire proposed body,
 // including MoveRotation's next pose, into its half; the truck-only wall backs up this constraint.
 public void Constrain(){var q=body.rotation;float yaw=steer*turnDegrees*Mathf.Clamp01(Mathf.Abs(Vector3.Dot(body.linearVelocity,q*Vector3.forward))/1.6f)*(Vector3.Dot(body.linearVelocity,q*Vector3.forward)<-.15f?-1:1)*Time.fixedDeltaTime;var next=q*Quaternion.Euler(0,yaw,0);var right=next*Vector3.right;var forward=next*Vector3.forward;float ex=Mathf.Abs(right.x)*shape.size.x*.5f+Mathf.Abs(forward.x)*shape.size.z*.5f+.03f;float ez=Mathf.Abs(right.z)*shape.size.x*.5f+Mathf.Abs(forward.z)*shape.size.z*.5f+.03f;
  var currentRight=q*Vector3.right;var currentForward=q*Vector3.forward;
  ex=Mathf.Max(ex,Mathf.Abs(currentRight.x)*shape.size.x*.5f+Mathf.Abs(currentForward.x)*shape.size.z*.5f+.03f);
  ez=Mathf.Max(ez,Mathf.Abs(currentRight.z)*shape.size.x*.5f+Mathf.Abs(currentForward.z)*shape.size.z*.5f+.03f);
  var p=body.position;var old=p;var v=body.linearVelocity;float low=slot==0?-match.goalLine+ex:.12f+ex,high=slot==0?-.12f-ex:match.goalLine-ex;
  p.x=Mathf.Clamp(p.x,low,high);p.z=Mathf.Clamp(p.z,-match.halfWidth+ez,match.halfWidth-ez);p=Round(p,q,next);
  // PhysX integrates translation after FixedUpdate. Bound that next movement too;
  // the .03 inset covers the pending drive acceleration within this fixed step.
  float dt=Time.fixedDeltaTime;
  v.x=Mathf.Clamp(v.x,(low-p.x)/dt,(high-p.x)/dt);
  v.z=Mathf.Clamp(v.z,(-match.halfWidth+ez-p.z)/dt,(match.halfWidth-ez-p.z)/dt);
  var future=Round(p+v*dt,q,next);v=(future-p)/dt;
  if(p!=old)body.position=p;body.linearVelocity=v;
 }
 Vector3 Round(Vector3 p,Quaternion current,Quaternion next){for(int iteration=0;iteration<6;iteration++)for(int pose=0;pose<2;pose++){var q=pose==0?current:next;var right=q*Vector3.right*shape.size.x*.5f;var forward=q*Vector3.forward*shape.size.z*.5f;for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){var corner=p+right*x+forward*z;var safe=AirHockeyArena.Project(corner,match.goalLine,match.halfWidth,match.cornerRadius,.18f);p+=safe-corner;}}return p;}
 void OnCollisionEnter(Collision c){if(!match.rules.Live||match.MenuOpen)return;if(c.rigidbody==match.puck){PuckContacts++;match.LastTouch=slot;}float strength=Mathf.Abs(Vector3.Dot(c.relativeVelocity,c.GetContact(0).normal));match.feedback.Pulse(match.players[slot].controller?match.players[slot].pad:null,Mathf.Clamp(strength*.012f,0,.14f),.08f);if(c.rigidbody!=match.puck)match.feedback.Play(3,strength);}
}
}
