using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class SiegeTarget : MonoBehaviour
    {
        public TrebuchetMatch Match;
        public int Lane;
        public bool Hit { get; private set; }
        Rigidbody body;
        Vector3 start;
        Quaternion rotation;
        void Awake() { body = GetComponent<Rigidbody>(); start = body.position; rotation = body.rotation; }
        public void ResetTarget() { Hit = false; body.position = start; body.rotation = rotation; body.linearVelocity = body.angularVelocity = Vector3.zero; }
        void OnCollisionEnter(Collision collision)
        {
            if (Hit || !Match.Live || collision.relativeVelocity.magnitude < 1.5f || collision.rigidbody == null) return;
            var trailer = collision.rigidbody.GetComponent<TrailerBody>();
            var previous = collision.rigidbody.GetComponent<SiegeTarget>();
            if (trailer == null && (previous == null || !previous.Hit)) return;
            Hit = true; Match.TargetHit(Lane);
        }
    }
}
