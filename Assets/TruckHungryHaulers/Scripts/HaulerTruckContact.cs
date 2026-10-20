using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckHungryHaulers
{
 public sealed class HaulerTruckContact:MonoBehaviour
 {
  public HaulerMatch match;public SportsEventTruck truck;
  void OnCollisionStay(Collision collision){match.CaptureImpact(truck.slot,collision);}
  void OnCollisionEnter(Collision collision){if(match.CaptureImpact(truck.slot,collision))return;var other=collision.collider.GetComponentInParent<SportsEventTruck>();if(other==null||other.session!=match)return;float speed=0;for(int i=0;i<collision.contactCount;i++)speed=Mathf.Max(speed,Mathf.Abs(Vector3.Dot(collision.relativeVelocity,collision.GetContact(i).normal)));if(speed>=4)match.Ram(truck.slot,other.slot,speed);}
 }
}
