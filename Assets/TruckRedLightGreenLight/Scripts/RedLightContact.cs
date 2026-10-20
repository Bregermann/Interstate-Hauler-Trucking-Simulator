using UnityEngine;
namespace LWS.TruckRedLightGreenLight
{
    public sealed class RedLightContact : MonoBehaviour
    {
        public RedLightMatch match; public int slot;
        void OnCollisionEnter(Collision collision) { Report(collision); }
        void OnCollisionStay(Collision collision) { Report(collision); }
        void Report(Collision collision)
        {
            var other = collision.rigidbody != null ? collision.rigidbody.GetComponentInParent<RedLightContact>() : null;
            if (other == null || other.match != match || other.slot == slot || !match.Live) return;
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < collision.contactCount; i++) normal += collision.GetContact(i).normal;
            normal = Vector3.ProjectOnPlane(normal.normalized, Vector3.up);
            if (normal.sqrMagnitude < .1f) return;
            float amount = Mathf.Min(30, collision.impulse.magnitude / match.trucks[slot].body.mass);
            match.rules.ExternalPush(slot, new Vector2(normal.x, normal.z) * amount);
        }
    }
}
