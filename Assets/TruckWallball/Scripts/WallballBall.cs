using UnityEngine;
namespace LWS.TruckWallball {
[DefaultExecutionOrder(-50)]public sealed class WallballBall:MonoBehaviour {
 public WallballMatch match;public Rigidbody body;public SphereCollider shape;public float gravity=12,radius=.45f,maxSpeed=15,wallBounce=.9f,floorBounce=.88f;
 public Renderer visual;public TrailRenderer trail;public Vector3 BeforeImpact{get;private set;}public int FloorContacts{get;private set;}public int WallContacts{get;private set;}public int TruckContacts{get;private set;}
 bool floorTouch,wallTouch;float soundAfter;
 public void ClearContacts(){floorTouch=wallTouch=false;BeforeImpact=Vector3.zero;if(trail!=null)trail.Clear();}
 void FixedUpdate(){if(!match.rules.Active||match.MenuOpen||body.isKinematic)return;body.linearVelocity=Vector3.ClampMagnitude(body.linearVelocity,maxSpeed);BeforeImpact=body.linearVelocity;body.AddForce(Vector3.down*gravity,ForceMode.Acceleration);}
 void Contact(Collision c){if(!match.rules.Active||match.MenuOpen||c.contactCount==0)return;
  if(c.collider==match.floor){if(!floorTouch&&BeforeImpact.y<-.2f){FloorContacts++;match.Floor();Sound(2,c.relativeVelocity.magnitude);}floorTouch=true;}
  else if(c.collider==match.wall){if(!wallTouch){WallContacts++;var point=c.GetContact(0).point;match.Wall(Mathf.Abs(point.x)<=match.halfWidth-.2f&&point.y>=.55f&&point.y<=7.5f);Sound(1,c.relativeVelocity.magnitude);}wallTouch=true;}
  else if(c.rigidbody!=null){for(int i=0;i<2;i++)if(c.rigidbody==match.trucks[i].body){if(match.Strike(i,c.GetContact(0).normal,c.relativeVelocity)){TruckContacts++;Sound(5,c.relativeVelocity.magnitude);}break;}}
 }
 void OnCollisionEnter(Collision c){Contact(c);}void OnCollisionStay(Collision c){if(c.collider==match.floor||c.collider==match.wall)Contact(c);}
 void OnCollisionExit(Collision c){if(c.collider==match.floor)floorTouch=false;if(c.collider==match.wall)wallTouch=false;}
 void Sound(int kind,float strength){if(Time.time<soundAfter)return;soundAfter=Time.time+.045f;match.feedback.Play(kind,Mathf.Max(1,strength));}
}
}
