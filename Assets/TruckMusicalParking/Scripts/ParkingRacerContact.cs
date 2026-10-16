using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckMusicalParking {
// Only an incoming physical truck contact grants brief recovery grace; no extra impact impulse.
public sealed class ParkingRacerContact:MonoBehaviour {
 public ParkingMatch match;public int slot;
 void OnCollisionEnter(Collision collision){if(match==null||collision.rigidbody==null||collision.contactCount==0)return;var other=collision.rigidbody.GetComponent<SportsEventTruck>();if(other==null||other.slot==slot)return;float incoming=0;for(int i=0;i<collision.contactCount;i++)incoming=Mathf.Max(incoming,Vector3.Dot(other.body.linearVelocity,collision.GetContact(i).normal));match.CollisionRecovery(slot,incoming);}
}
}
