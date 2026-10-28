using UnityEngine;
namespace LWS.TruckSubmarine {
 public sealed class SubmarineBuoy:MonoBehaviour {
  public SubmarineMatch match;public BoxCollider reception;public Transform lightCrown;
  void OnTriggerEnter(Collider other)=>Receive(other);
  void OnTriggerStay(Collider other)=>Receive(other);
  void Receive(Collider other){var cargo=other.attachedRigidbody!=null?other.attachedRigidbody.GetComponent<SubmarineCargo>():null;if(cargo==null||cargo.match!=match||cargo.Owner<0||cargo.Banked||!match.Live)return;int slot=cargo.Owner;if(!reception.bounds.Contains(cargo.body.worldCenterOfMass))return;match.Deliver(cargo,slot);}
 }
}
