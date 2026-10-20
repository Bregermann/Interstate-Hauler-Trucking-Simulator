using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckVolleyball
{
 public sealed class VolleyBallContact:MonoBehaviour
 {
  public VolleyMatch match;
  void OnCollisionEnter(Collision hit){if(match==null||!match.Live||match.Countdown>0||!match.rules.Live)return;var truck=hit.collider.GetComponentInParent<SportsEventTruck>();if(truck!=null&&truck.session==match){match.Strike(truck,hit);return;}if(hit.collider.gameObject.name=="Physical woven volleyball net")match.MarkNet();if(hit.collider.gameObject.name=="Continuous volleyball sand court")match.Landing();}
 }
}
