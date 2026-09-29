using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRoadPepCollisionRelay : MonoBehaviour
    {
        private TruckTaxiDriverNeedsCoordinator owner;
        public void Initialize(TruckTaxiDriverNeedsCoordinator value) => owner = value;
        public void ClearOwner(TruckTaxiDriverNeedsCoordinator value)
        { if (owner == value) owner = null; }
        private void OnCollisionEnter(Collision collision) => owner?.ReportRoadPepCollision(collision.relativeVelocity.magnitude);
    }
}
