using UnityEngine;
namespace LWS.TruckHungryHaulers
{
 public sealed class HaulerMouth:MonoBehaviour
 {
  public HaulerScoop scoop;
  void OnTriggerStay(Collider other){var ball=other.GetComponentInParent<HaulerBall>();if(ball!=null)scoop.match.Capture(scoop.slot,ball);}
 }
}
