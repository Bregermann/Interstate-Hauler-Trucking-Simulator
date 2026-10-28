#if UNITY_EDITOR
using UnityEngine;
namespace LWS.TruckDentist.Tests {
 // Test-only observer of real native callbacks; never drives a tool or tooth.
 public sealed class DentistContactDiagnosticObserver:MonoBehaviour {
  public int triggers,collisions;public string last="none";
  void OnTriggerEnter(Collider other){triggers++;last="enter "+other.name;}
  void OnTriggerStay(Collider other){triggers++;last="stay "+other.name;}
  void OnCollisionEnter(Collision collision){collisions++;last="collision "+collision.collider.name;}
  void OnCollisionStay(Collision collision){collisions++;last="rest "+collision.collider.name;}
 }
}
#endif
