using UnityEngine;

namespace LWS.TruckSports
{
    // Additive observer on an existing collider. Never creates/changes a
    // collider, retires a Rigidbody or calls a scoring/elimination method.
    public sealed class SportsCarnivalLavaContact : MonoBehaviour
    {
        public SportsCarnivalLavaSurface presentation;
        void OnTriggerEnter(Collider other) => Contact(other.attachedRigidbody);
        void OnCollisionEnter(Collision collision) => Contact(collision.rigidbody);
        void Contact(Rigidbody body)
        {
            if (presentation != null && presentation.isActiveAndEnabled && body != null)
                presentation.ObserveImpact(body.worldCenterOfMass, body.GetInstanceID(), true);
        }
    }
}
