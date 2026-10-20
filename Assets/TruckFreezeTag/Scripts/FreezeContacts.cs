using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckFreezeTag
{
 public sealed class FreezeContacts:MonoBehaviour
 {
  public FreezeMatch match;public SportsEventTruck truck;
  void OnCollisionEnter(Collision collision){var other=collision.collider.GetComponentInParent<SportsEventTruck>();if(other==null||other.session!=match)return;float incoming=0;for(int i=0;i<collision.contactCount;i++)incoming=Mathf.Max(incoming,Mathf.Abs(Vector3.Dot(collision.relativeVelocity,collision.GetContact(i).normal)));match.Contact(truck.slot,other.slot,incoming);}
 }
}
