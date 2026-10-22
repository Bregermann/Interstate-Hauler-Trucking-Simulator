using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckSnake {
 public sealed class SnakePickup:MonoBehaviour {
  public SnakeMatch match;public bool Golden;public Transform cargoIcon;public ParticleSystem celebration;
  public bool Live{get;private set;}public float Age=>age;float age;
  public void Dormant(){Live=false;gameObject.SetActive(false);}
  public void Spawn(Vector3 position){Live=true;age=0;transform.position=position+Vector3.up*.15f;gameObject.SetActive(true);}
  public void Tick(float dt){if(!Live)return;age+=dt;cargoIcon.localPosition=Vector3.up*(1.9f+Mathf.Sin(age*2)*.18f);cargoIcon.localRotation=Quaternion.Euler(0,age*35,0);}
  void OnTriggerEnter(Collider collider){if(!Live)return;var truck=collider.GetComponentInParent<SportsEventTruck>();if(truck==null||truck.session!=match)return;match.Collect(this,truck.slot);}
 }
}
