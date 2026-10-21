using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckFrogger
{
 public sealed class FrogContacts:MonoBehaviour{public FrogMatch match;public SportsEventTruck truck;void OnCollisionEnter(Collision c){match.SolidContact(truck.slot,c);}}
}
