using UnityEngine;
using LWS.TruckSports;
using LWS.TruckTaxi;
namespace LWS.TruckFareFight {
 public sealed class FareFightContact:MonoBehaviour {
  public FareFightMatch match;public SportsEventTruck truck;public Vector3 Incoming{get;private set;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  public int PedestrianContacts{get;private set;}public string LastPedestrianEvent{get;private set;}
#endif
  void FixedUpdate(){Incoming=truck.body.linearVelocity;}
  void OnCollisionEnter(Collision collision){Report(collision);}
  void Report(Collision collision){
   if(match==null||!match.Live||collision.collider.isTrigger)return;
   var pedestrian=collision.collider.GetComponentInParent<TruckTaxiPedestrian>();
   if(pedestrian!=null){
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    PedestrianContacts++;LastPedestrianEvent="contact intro="+match.Intro+" ragdoll="+pedestrian.IsRagdoll+" incoming="+Incoming+" relative="+collision.relativeVelocity;
#endif
    if(match.Intro>0||pedestrian.IsRagdoll)return;var contact=collision.contactCount>0?collision.GetContact(0).point:pedestrian.transform.position;var toward=Vector3.ProjectOnPlane(contact-truck.body.worldCenterOfMass,Vector3.up);if(Vector3.Dot(Incoming,toward.normalized)<=.1f)return;var relative=Incoming.normalized*collision.relativeVelocity.magnitude;if(pedestrian.TryStrike(relative,contact)){pedestrian.MarkHitEventSent();match.feedback.PlayAt(1,contact,relative.magnitude);}return;}
   var opponent=collision.rigidbody!=null?collision.rigidbody.GetComponentInParent<FareFightContact>():null;
   if(opponent==null||opponent.match!=match||opponent.truck.slot==truck.slot)return;
   float normalSpeed=0;Vector3 point=truck.body.worldCenterOfMass;
   for(int i=0;i<collision.contactCount;i++){var contact=collision.GetContact(i);float closing=Mathf.Abs(Vector3.Dot(Incoming-opponent.Incoming,contact.normal));if(closing>normalSpeed){normalSpeed=closing;point=contact.point;}}
   match.Contact(truck.slot,opponent.truck.slot,normalSpeed,point);
  }
 }
}
