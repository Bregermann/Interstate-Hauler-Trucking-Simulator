using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiStorePoint : MonoBehaviour
    {
        public string stableId = "TT_STORE_";
        public string displayName = "Truck stop store";
        public TruckTaxiRideLocation location;
        [Min(2)] public float stopRadius = 18;
        [Min(.5f)] public float verticalTolerance = 3;
        public Vector3 Position => location != null ? location.StopPosition : transform.position;
        public float InteractionRadius => Mathf.Max(18, stopRadius);
        public bool CanUse(Vector3 position, float speed, float maximumSpeed)
        {
            Vector3 difference = position - Position;
            return isActiveAndEnabled && !string.IsNullOrWhiteSpace(stableId) &&
                float.IsFinite(speed) && speed >= 0 && speed <= Mathf.Max(TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond, maximumSpeed) &&
                Mathf.Abs(difference.y) <= verticalTolerance &&
                Vector3.ProjectOnPlane(difference, Vector3.up).sqrMagnitude <= InteractionRadius * InteractionRadius;
        }
        private void OnDrawGizmosSelected()
        { Gizmos.color = new Color(.1f, .8f, .55f); Gizmos.DrawWireSphere(Position, InteractionRadius); }
    }
}
