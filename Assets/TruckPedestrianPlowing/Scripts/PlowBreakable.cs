using UnityEngine;
using Assets.CoreCollapse.Scripts.Structural;
using LWS.TruckSports;
namespace LWS.TruckPedestrianPlowing {
 // Core-Collapse owns damage/support; the sport adds bounded, reusable fragment lifecycle.
 public sealed class PlowBreakable:Collapser {
  public Rigidbody intactBody;public Transform model;public Rigidbody[] fragments;
  Vector3[] localPositions;Quaternion[] localRotations;Transform[] parents;bool broken;
  void Awake(){localPositions=new Vector3[fragments.Length];localRotations=new Quaternion[fragments.Length];parents=new Transform[fragments.Length];for(int i=0;i<fragments.Length;i++){parents[i]=fragments[i].transform.parent;localPositions[i]=fragments[i].transform.localPosition;localRotations[i]=fragments[i].transform.localRotation;}}
  public void ResetPiece(){currentHp=startingHp;broken=false;model.gameObject.SetActive(true);intactBody.detectCollisions=true;for(int i=0;i<fragments.Length;i++){var b=fragments[i];b.isKinematic=true;b.transform.SetParent(parents[i],false);b.transform.SetLocalPositionAndRotation(localPositions[i],localRotations[i]);b.gameObject.SetActive(false);}}
  protected override void Collapse(){if(broken)return;base.Collapse();broken=true;var velocity=intactBody.linearVelocity;var angular=intactBody.angularVelocity;model.gameObject.SetActive(false);intactBody.detectCollisions=false;if(!intactBody.isKinematic)intactBody.linearVelocity=intactBody.angularVelocity=Vector3.zero;intactBody.Sleep();foreach(var b in fragments){b.transform.SetParent(null,true);b.gameObject.SetActive(true);b.isKinematic=false;b.linearVelocity=Vector3.ClampMagnitude(velocity,12);b.angularVelocity=Vector3.ClampMagnitude(angular,6);}}
  void OnCollisionEnter(Collision c){if(broken||c.rigidbody==null||c.rigidbody.GetComponent<SportsEventTruck>()==null||c.relativeVelocity.magnitude<4)return;ReceiveDamage(Mathf.Min(60,c.relativeVelocity.magnitude*5));}
 }
}
