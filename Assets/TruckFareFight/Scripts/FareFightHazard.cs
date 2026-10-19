using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckFareFight {
 public enum FareFightHazardKind {Oil,Banana,Sheep}
 public sealed class FareFightHazard:MonoBehaviour {
  public FareFightHazards host;public FareFightHazardKind kind;readonly float[] next=new float[4];bool detonated;
  public void ResetHazard(Vector3 point,bool active){System.Array.Clear(next,0,4);detonated=false;transform.position=point+Vector3.up*.05f;transform.rotation=Quaternion.Euler(0,point.x*13+point.z*7,0);gameObject.SetActive(active);}
  void OnTriggerEnter(Collider other){Hit(other);}
  void OnCollisionEnter(Collision collision){Hit(collision.collider);}
  void Hit(Collider collider){var truck=collider.GetComponentInParent<SportsEventTruck>();if(truck==null||truck.session!=host.match||!host.match.Live||host.match.Intro>0)return;int slot=truck.slot;if(Time.time<next[slot]||detonated)return;next[slot]=Time.time+5;if(kind==FareFightHazardKind.Sheep){detonated=true;host.match.interactions.Blast(transform.position,slot,10);gameObject.SetActive(false);}else host.Slip(slot,transform.position);}
 }
}
