using UnityEngine;
namespace LWS.TruckLimbo {
public sealed class LimboBar:MonoBehaviour {
 public LimboRacer racer;public Rigidbody body;public Transform[] ledges;public float Plane=>origin==Vector3.zero?transform.position.z:origin.z;Vector3 origin;Quaternion rotation;
 public void Prepare(float clearance){if(origin==Vector3.zero){origin=transform.position;rotation=transform.rotation;}body.isKinematic=true;body.position=new Vector3(origin.x,clearance+.075f,origin.z);body.rotation=rotation;foreach(var ledge in ledges){var p=ledge.position;p.y=clearance-.06f;ledge.position=p;}body.isKinematic=false;body.linearVelocity=body.angularVelocity=Vector3.zero;body.WakeUp();}
 void OnCollisionEnter(Collision c){var source=c.collider.GetComponentInParent<LimboRacer>();if(source==racer&&c.collider.attachedRigidbody==source.truck.body)racer.ContactBar(c);}
}
}
