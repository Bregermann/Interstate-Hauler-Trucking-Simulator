using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiStorePoint : MonoBehaviour
    {
        public string stableId = "TT_STORE_";
        public string displayName = "Truck stop store";
        public TruckTaxiRideLocation location;
        [Min(2)] public float stopRadius = 12;
        [Min(.5f)] public float verticalTolerance = 3;
        public Vector3 Position => location != null ? location.StopPosition : transform.position;
        public bool CanUse(Vector3 position, float speed, float maximumSpeed)
        {
            Vector3 difference = position - Position;
            return isActiveAndEnabled && !string.IsNullOrWhiteSpace(stableId) &&
                float.IsFinite(speed) && speed >= 0 && speed <= maximumSpeed &&
                Mathf.Abs(difference.y) <= verticalTolerance &&
                Vector3.ProjectOnPlane(difference, Vector3.up).sqrMagnitude <= stopRadius * stopRadius;
        }
        private void OnDrawGizmosSelected()
        { Gizmos.color = new Color(.1f, .8f, .55f); Gizmos.DrawWireSphere(Position, stopRadius); }
    }
}
