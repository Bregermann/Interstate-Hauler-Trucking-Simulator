using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckPedestrianPlowing {
 // Captured before simulation; collision callbacks are resolved together at the next fixed boundary.
 [DefaultExecutionOrder(100)]public sealed class PlowTruckContact:MonoBehaviour {
  public SportsEventTruck truck;public Vector3 position,velocity,angular;public float stepStart;
  void FixedUpdate(){position=truck.body.worldCenterOfMass;velocity=truck.body.linearVelocity;angular=truck.body.angularVelocity;stepStart=Time.fixedTime-Time.fixedDeltaTime;}
 }
}
