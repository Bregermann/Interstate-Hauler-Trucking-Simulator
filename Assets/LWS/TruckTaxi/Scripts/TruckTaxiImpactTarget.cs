using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TaxiImpactKind { Traffic, Pedestrian, Property }
    public sealed class TruckTaxiImpactTarget : MonoBehaviour
    {
        public TaxiImpactKind kind;
        public string targetId;
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
                if (Damaged) return false;
                Damaged = true;
                if (body != null) body.isKinematic = false;
            }
            return true;
        }
        public void ResetTarget()
        {
            Damaged = false;
            if (body != null) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            transform.SetPositionAndRotation(home, rotation);
            if (kind == TaxiImpactKind.Pedestrian) GetComponent<TruckTaxiPedestrian>()?.ResetPedestrian();
        }
    }
}
