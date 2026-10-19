using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckFareFight {
 public sealed class FareFightContact:MonoBehaviour {
  public FareFightMatch match;public SportsEventTruck truck;public Vector3 Incoming{get;private set;}
  void FixedUpdate(){Incoming=truck.body.linearVelocity;}
  void OnCollisionEnter(Collision collision){Report(collision);}
  void Report(Collision collision){
   if(match==null||!match.Live||collision.collider.isTrigger)return;
   var opponent=collision.rigidbody!=null?collision.rigidbody.GetComponentInParent<FareFightContact>():null;
   if(opponent==null||opponent.match!=match||opponent.truck.slot==truck.slot)return;
   float normalSpeed=0;Vector3 point=truck.body.worldCenterOfMass;
   for(int i=0;i<collision.contactCount;i++){var contact=collision.GetContact(i);float closing=Mathf.Abs(Vector3.Dot(Incoming-opponent.Incoming,contact.normal));if(closing>normalSpeed){normalSpeed=closing;point=contact.point;}}
   match.Contact(truck.slot,opponent.truck.slot,normalSpeed,point);
  }
 }
}
