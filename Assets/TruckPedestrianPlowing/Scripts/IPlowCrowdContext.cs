using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckPedestrianPlowing {
 // Environment/scoring remains owned by the sport; character navigation,
 // reactions, authored animation and Taxi ragdoll lifecycle stay shared.
 public interface IPlowCrowdContext {
  PlowSettings CrowdSettings {get;}
  int ThreatCount {get;}
  Rigidbody ThreatBody(int index);
  Collider[] ClimbSurfaces {get;}
  bool TryRoute(Vector3 from,Vector3 desired,out Vector3 destination);
  Vector3 RandomDestination(Vector3 from,float distance);
  bool NearImpact(Vector3 position);
  bool NearView(Vector3 position,float distance);
  bool SafeToRetire(Vector3 position);
  void Reaction(PlowPedestrian pedestrian);
  void Contact(PlowPedestrian pedestrian,Collision collision);
 }
}
