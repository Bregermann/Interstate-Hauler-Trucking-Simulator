using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckSnake {
 public sealed class SnakeTruckContact:MonoBehaviour {
  public SnakeMatch match;public SportsEventTruck truck;Vector3 incoming;public string LastImpact{get;private set;}="none";public string LastSolid{get;private set;}="none";public Vector3 LastNormal{get;private set;}public float LastNormalSpeed{get;private set;}
  void OnCollisionStay(Collision collision){if(collision.contactCount>0){LastSolid=collision.collider.name;LastNormal=collision.GetContact(0).normal;LastNormalSpeed=Mathf.Abs(Vector3.Dot(collision.relativeVelocity,LastNormal));}}
  void FixedUpdate(){incoming=truck.body.linearVelocity;}
  void OnCollisionEnter(Collision collision){OnCollisionStay(collision);if(!match.Live||match.Intro>0||!match.rules.Playing(truck.slot)||collision.collider.isTrigger)return;float speed=0;for(int i=0;i<collision.contactCount;i++)speed=Mathf.Max(speed,Mathf.Max(0,-Vector3.Dot(incoming-(collision.rigidbody!=null?collision.rigidbody.linearVelocity:Vector3.zero),collision.GetContact(i).normal)));
   var convoy=match.convoys[truck.slot];if(convoy.IsOwn(collision.collider,out int segment)){convoy.TractorContact(segment,speed);return;}
   if(collision.rigidbody!=null)return;
   Vector3 relative=incoming-(collision.rigidbody!=null?collision.rigidbody.linearVelocity:Vector3.zero);float qualifying=0;Vector3 impactNormal=default;for(int i=0;i<collision.contactCount;i++){var normal=collision.GetContact(i).normal;if(SnakeSolidImpact.Qualifies(relative,truck.transform.forward,normal,out float measured)&&measured>qualifying){qualifying=measured;impactNormal=normal;}}
   if(qualifying>0){LastImpact="object="+collision.collider.name+" normal="+impactNormal.ToString("F3")+" normalSpeed="+qualifying.ToString("F3")+" incoming="+relative.ToString("F3")+" position="+truck.body.position.ToString("F3")+" bounds="+collision.collider.bounds;Debug.Log("SNAKE_SOLID_IMPACT slot="+truck.slot+" "+LastImpact);match.Fail(truck.slot,"MAJOR IMPACT · "+collision.collider.name);}
  }
 }
}
