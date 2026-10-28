#if UNITY_EDITOR
using System;using UnityEngine;
namespace LWS.TruckBakeOff.Tests {
 // Records real callbacks only. Never registers a pickup or drives any body.
 public sealed class BakeWristContactDiagnosticObserver:MonoBehaviour {
  public int triggers,vessels,collisions;public string last="none";
  void OnTriggerEnter(Collider other)=>Observe(other,"enter");
  void OnTriggerStay(Collider other)=>Observe(other,"stay");
  void Observe(Collider other,string kind){triggers++;var body=other.attachedRigidbody;var type=Type.GetType("LWS.TruckTeaParty.ServingVessel, Assembly-CSharp",true);bool vessel=body!=null&&body.GetComponent(type)!=null;if(vessel)vessels++;last=kind+" "+other.name+" rb="+(body!=null?body.name:"none")+" vessel="+vessel;}
  void OnCollisionEnter(Collision c){collisions++;last="collision "+c.collider.name;}
  void OnCollisionStay(Collision c){collisions++;last="rest "+c.collider.name;}
 }
}
#endif
