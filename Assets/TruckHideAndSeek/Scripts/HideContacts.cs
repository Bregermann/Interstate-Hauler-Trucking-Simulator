using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckHideAndSeek
{
 public sealed class HideContacts:MonoBehaviour
 {
  public HideMatch match;public SportsEventTruck truck;
  void OnCollisionEnter(Collision c){Tag(c);}void OnCollisionStay(Collision c){Tag(c);}
  void Tag(Collision c){if(c.collider.isTrigger||!truck.Powered)return;var other=c.collider.GetComponentInParent<SportsEventTruck>();if(other==null||other.session!=match)return;for(int i=0;i<c.contactCount;i++){var p=c.GetContact(i);if(p.thisCollider.isTrigger||p.otherCollider.isTrigger||p.thisCollider.attachedRigidbody!=truck.body||p.otherCollider.attachedRigidbody!=other.body)continue;match.Contact(truck.slot,other.slot);return;}}
 }
}
