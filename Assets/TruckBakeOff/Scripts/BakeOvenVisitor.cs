using UnityEngine;using LWS.TruckSports;using LWS.TruckTeaParty;
namespace LWS.TruckBakeOff {
 public sealed class BakeOvenVisitor:MonoBehaviour {public BakeOven oven;void OnTriggerStay(Collider c){var truck=c.attachedRigidbody!=null?c.attachedRigidbody.GetComponentInParent<SportsEventTruck>():null;oven.ObserveVisit(truck);}}
}
