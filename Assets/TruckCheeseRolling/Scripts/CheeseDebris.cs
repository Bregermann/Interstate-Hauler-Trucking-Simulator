using UnityEngine;
namespace LWS.TruckCheeseRolling {
public sealed class CheeseDebris:MonoBehaviour {
 public Rigidbody body;float remaining;
 public void Emit(Vector3 p){gameObject.SetActive(true);body.isKinematic=false;body.position=p;body.linearVelocity=body.angularVelocity=Vector3.zero;remaining=2.5f;}
 void FixedUpdate(){remaining-=Time.fixedDeltaTime;if(remaining<=0){body.isKinematic=true;gameObject.SetActive(false);}}
}
}
