using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TaxiImpactKind { Traffic, Pedestrian, Property }
    public sealed class TruckTaxiImpactTarget : MonoBehaviour
    {
        public TaxiImpactKind kind;
        public string targetId;
        public bool IsTrafficVehicle => kind == TaxiImpactKind.Traffic;
        public static TruckTaxiImpactTarget FromCollider(Collider collider) => collider != null ?
            collider.GetComponentInParent<TruckTaxiImpactTarget>() ??
            collider.attachedRigidbody?.GetComponent<TruckTaxiImpactTarget>() : null;
        private Vector3 home;
        private Quaternion rotation;
        private Rigidbody body;
        public bool Damaged { get; private set; }
        private void Awake() { home = transform.position; rotation = transform.rotation; body = GetComponent<Rigidbody>(); }
        public bool Hit()
        {
            // Pedestrians require measured impact context through the collision observer.
            if (kind == TaxiImpactKind.Pedestrian) return false;
            if (kind == TaxiImpactKind.Property)
            {
                var prop = GetComponent<TruckTaxiBreakawayProp>();
                if (prop != null)
                {
                    if (!prop.TryReceiveHit()) return false;
                    return MarkPropertyDamage();
                }
                if (!MarkPropertyDamage()) return false;
                if (body != null) body.isKinematic = false;
            }
            return true;
        }
        // The return value is scoring eligibility, not whether a previously damaged prop lost durability.
        public bool Hit(Rigidbody source, Vector3 contact, float speed)
        {
            if (kind != TaxiImpactKind.Property) return Hit();
            var prop = GetComponent<TruckTaxiBreakawayProp>();
            if (prop == null) return Hit();
            return prop.TryReceiveImpact(source, contact, speed) && MarkPropertyDamage();
        }
        internal bool MarkPropertyDamage()
        {
            if (kind != TaxiImpactKind.Property || Damaged) return false;
            Damaged = true; return true;
        }
        public void ResetTarget()
        {
            var prop = GetComponent<TruckTaxiBreakawayProp>();
            if (prop != null) { prop.ResetProp(); Damaged = false; return; }
            Damaged = false;
            if (body != null) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            transform.SetPositionAndRotation(home, rotation);
            if (kind == TaxiImpactKind.Pedestrian) GetComponent<TruckTaxiPedestrian>()?.ResetPedestrian();
        }
    }
}
