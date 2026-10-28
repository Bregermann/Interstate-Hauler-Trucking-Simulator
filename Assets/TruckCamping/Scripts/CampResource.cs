using UnityEngine;
namespace LWS.TruckCamping
{
    public sealed class CampResource : MonoBehaviour
    {
        public string id;
        public CampSupply kind;
        public Rigidbody body;
        public Collider physicalPickup;
        public Renderer[] art;
        public Vector3 home;
        public Quaternion homeRotation;
        public ParticleSystem gathered;
        public float Regrowth { get; private set; }
        public bool Available => Regrowth <= 0;
        public bool Gather(CampJournal owner, Vector3 collector, float actualSpeed)
        {
            if (!Available || owner == null || !float.IsFinite(actualSpeed) || actualSpeed > 1.5f || Vector3.Distance(collector, body.position) > 10) return false;
            if (!owner.Add(kind, 1)) return false;
            Regrowth = 180; Visible(false); if (gathered != null) gathered.Play(); return true;
        }
        void Visible(bool active) { foreach (var renderer in art) renderer.enabled = active; physicalPickup.enabled = active; body.isKinematic = !active; }
        public void Tick(float dt) { if (Regrowth <= 0) return; Regrowth = Mathf.Max(0, Regrowth - dt); if (Regrowth <= 0) Restore(0); }
        public void Restore(float remaining)
        {
            body.isKinematic = true; body.position = home; body.rotation = homeRotation;
            Regrowth = remaining; Visible(remaining <= 0);
            if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
        }
    }
}
