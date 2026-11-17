using System.Collections.Generic;using UnityEngine;
namespace LWS.TruckWaterSports {
// Coupling requires a native collider contact, never just a projected target.
public sealed class BlackwaterSupply:MonoBehaviour {
 readonly Dictionary<WaterVessel,float> contacts=new Dictionary<WaterVessel,float>(4);
 public bool Touching(WaterVessel vessel)=>contacts.TryGetValue(vessel,out float when)&&Time.fixedTime-when<.12f;
 public void ResetContacts()=>contacts.Clear();
 void Observe(Collision collision){var vessel=collision.rigidbody!=null?collision.rigidbody.GetComponent<WaterVessel>():null;if(vessel!=null)contacts[vessel]=Time.fixedTime;}
 void OnCollisionEnter(Collision collision)=>Observe(collision);
 void OnCollisionStay(Collision collision)=>Observe(collision);
 void OnDisable()=>contacts.Clear();
}
}
