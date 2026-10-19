using UnityEngine;
namespace LWS.TruckFareFight {
 public sealed class FareFightJug:MonoBehaviour {
  public FareFightActions actions;public int owner;public Rigidbody body;public Collider shape;
  bool live;float deadline;
  public void ResetJug(){live=false;body.isKinematic=true;shape.enabled=false;gameObject.SetActive(false);}
  public void Launch(Vector3 position,Vector3 velocity){live=true;gameObject.SetActive(true);body.position=position;body.rotation=Quaternion.identity;shape.enabled=true;body.isKinematic=false;body.linearVelocity=Vector3.ClampMagnitude(velocity,45);body.angularVelocity=new Vector3(3,4,2);deadline=Time.time+3;}
  void FixedUpdate(){if(live&&Time.time>=deadline)Detonate();}
  void OnCollisionEnter(Collision collision){if(live&&!collision.collider.isTrigger)Detonate();}
  void Detonate(){if(!live)return;live=false;var point=body.position;ResetJug();actions.Blast(point,owner);}
 }
}
