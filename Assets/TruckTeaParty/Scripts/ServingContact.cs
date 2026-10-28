using UnityEngine;
namespace LWS.TruckTeaParty {
 public sealed class ServingContact:MonoBehaviour {public ServingArm arm;void OnTriggerEnter(Collider c)=>Observe(c);void OnTriggerStay(Collider c)=>Observe(c);void Observe(Collider c){var vessel=c.attachedRigidbody!=null?c.attachedRigidbody.GetComponent<ServingVessel>():null;if(vessel!=null&&vessel.session==arm.truck.session)arm.Observe(vessel);}}
}
