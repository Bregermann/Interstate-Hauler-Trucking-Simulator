using UnityEngine;

namespace LWS.TruckTaxi
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TruckTaxiPedestrian : MonoBehaviour
    {
        public float respawnDelay = 12;
        private Rigidbody body;
        private Renderer[] visuals;
        private Collider[] colliders;
        private Behaviour vendorMovement;
        private float waiting;
        private bool struck;
        private Vector3 spawn;
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            foreach (var component in GetComponents<Behaviour>())
                if (component.GetType().Name == "Passersby") vendorMovement = component;
            spawn = transform.position;
            visuals = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
        }
        private void FixedUpdate()
        {
            if (waiting > 0) { waiting -= Time.fixedDeltaTime; if (struck && waiting <= 0) ResetPedestrian(); return; }
        }
        public bool Struck()
        {
            if (struck) return false;
            struck = true; waiting = respawnDelay;
            if (vendorMovement != null) vendorMovement.enabled = false;
            foreach (var r in visuals) r.enabled = false;
            foreach (var c in colliders) c.enabled = false;
            return true;
        }
        public void ResetPedestrian()
        {
            struck = false; waiting = 0;
            body.position = spawn;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            if (vendorMovement != null) vendorMovement.enabled = true;
            foreach (var r in visuals) r.enabled = true;
            foreach (var c in colliders) c.enabled = true;
        }
    }
}
