using UnityEngine;
using LWS.TruckTaxi;
namespace LWS.TruckTerrierRacing {
public sealed class TerrierRunner:MonoBehaviour {
 public TerrierCrowd crowd;public TruckTaxiPedestrian pedestrian;public Passersby walker;public MovePath movement;public int slot;public CapsuleCollider detector;
 void Awake(){pedestrian.Expired+=Expired;pedestrian.Ragdolled+=Ragdolled;}
 void Expired(TruckTaxiPedestrian p){p.PrepareForPool();p.gameObject.SetActive(false);}
 void Ragdolled(TruckTaxiPedestrian p){crowd.Hits++;foreach(var collider in p.GetComponentsInChildren<Collider>(true))foreach(var racer in crowd.match.racers)foreach(var truckCollider in racer.truck.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(collider,truckCollider,true);}
 void OnTriggerEnter(Collider other){if(!crowd.match.Live||crowd.match.Phase!=TerrierPhase.Race||pedestrian.IsRagdoll)return;var racer=other.GetComponentInParent<TerrierRacer>();if(racer==null||!racer.Alive)return;var point=other.ClosestPoint(transform.position+Vector3.up*.7f);pedestrian.TryStrike(racer.truck.body.GetPointVelocity(point),point);}
 void OnDestroy(){if(pedestrian!=null){pedestrian.Expired-=Expired;pedestrian.Ragdolled-=Ragdolled;}}
}
}
