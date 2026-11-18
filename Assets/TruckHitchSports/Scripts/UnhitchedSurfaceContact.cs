using UnityEngine;
namespace LWS.TruckHitchSports
{
    // Wheel support remains NWH-owned. This only distinguishes an actual roof/
    // chassis landing from free flight when native wheels no longer touch.
    public sealed class UnhitchedSurfaceContact : MonoBehaviour
    {
        Transform tractor;
        float supportedAt = float.NegativeInfinity;
        Rigidbody supportBody;
        public bool Supported => Time.fixedTime - supportedAt <= Time.fixedDeltaTime * 1.5f;
        public bool SupportedOutside(Rigidbody platform) => Supported && supportBody != platform;
        public void Configure(Transform owner) { tractor = owner; supportedAt = float.NegativeInfinity; supportBody = null; }
        void OnCollisionEnter(Collision collision) => Observe(collision);
        void OnCollisionStay(Collision collision) => Observe(collision);
        void Observe(Collision collision)
        {
            if (tractor == null || collision.collider.transform.IsChildOf(tractor)) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (Vector3.Dot(collision.GetContact(i).normal, Vector3.up) > .35f) { supportedAt = Time.fixedTime; supportBody = collision.rigidbody; return; }
        }
    }
}
