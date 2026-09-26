using LWS.TruckTaxi;
using UnityEngine;

// Keep UTS's rig activation API, but let the player collision observer own qualification.
// The original Car-tag trigger bypasses speed thresholds, scoring and lifetime ownership.
public sealed class TruckTaxiUtsRagdollAdapter : NPCStats, ITruckTaxiPedestrianRagdoll
{
    private void OnTriggerEnter(Collider other) { }
    public void ActivateRagdoll() => EnablePhysics();
}
