using UnityEngine;
namespace LWS.TruckSubmarine {
 public sealed class SubmarineGripper:MonoBehaviour {
  public SubmarineDiver diver;
  void OnTriggerEnter(Collider other)=>Observe(other);
  void OnTriggerStay(Collider other)=>Observe(other);
  void Observe(Collider other){var cargo=other.attachedRigidbody!=null?other.attachedRigidbody.GetComponent<SubmarineCargo>():null;if(cargo!=null&&cargo.match==diver.match&&!cargo.Banked&&cargo.Owner<0)diver.ObserveContact(cargo);}
 }
}
