using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiBathroomPoint : MonoBehaviour
    {
        public string stableId;
        public string displayName = "Restroom";
        public TruckTaxiRideLocation location;
        public bool allowsJugDisposal = true;
        public bool allowsCabCleanup = true;
        [Min(2)] public float stopRadius = 11;
        [Min(.5f)] public float verticalTolerance = 3;
        public Vector3 Position => location != null ? location.StopPosition : transform.position;
        public bool Contains(Vector3 position) => Mathf.Abs(position.y - Position.y) <= verticalTolerance &&
            Vector3.ProjectOnPlane(position - Position, Vector3.up).sqrMagnitude <= stopRadius * stopRadius;
        public bool CanUse(Vector3 position, float speed, float maximumSpeed) => isActiveAndEnabled && location != null &&
            !string.IsNullOrWhiteSpace(stableId) && float.IsFinite(speed) && speed >= 0 && speed <= maximumSpeed && Contains(position);
        private void OnDrawGizmosSelected() { Gizmos.color = new Color(.25f, .8f, .8f); Gizmos.DrawWireSphere(Position, stopRadius); }
    }
}
