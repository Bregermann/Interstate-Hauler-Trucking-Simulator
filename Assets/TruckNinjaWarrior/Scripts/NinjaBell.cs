using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckNinjaWarrior
{
 public sealed class NinjaBell:MonoBehaviour
 {
  public NinjaMatch match;void OnCollisionEnter(Collision collision){var truck=collision.collider.GetComponentInParent<SportsEventTruck>();if(truck==null||collision.collider.isTrigger||collision.collider.attachedRigidbody!=truck.body)return;for(int i=0;i<collision.contactCount;i++){var contact=collision.GetContact(i);if(Vector3.Dot(contact.point-truck.body.position,truck.transform.forward)>2&&Mathf.Abs(Vector3.Dot(collision.relativeVelocity,contact.normal))>1){match.StrikeBell(truck.slot);break;}}}
 }
}
