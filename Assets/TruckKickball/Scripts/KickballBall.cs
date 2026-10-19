using UnityEngine;

namespace LWS.TruckKickball
{
    public sealed class KickballBall : MonoBehaviour
    {
        public KickballMatch match;
        public Rigidbody body;
        public SphereCollider shape;
        public Vector3 PreviousPosition { get; private set; }
        public void Observe() { PreviousPosition = body.position; }
        void OnCollisionEnter(Collision collision) { Contact(collision); }
        void OnCollisionStay(Collision collision) { Contact(collision); }
        void Contact(Collision collision)
        {
            if (!match.Live) return;
            if (collision.rigidbody != null)
            {
                var agent = collision.rigidbody.GetComponent<KickballAgent>();
                if (agent != null && agent.match == match)
                    match.QueueBallContact(agent.truck.slot, collision.GetContact(0).point);
            }
            else if (match.rules.Phase == KickballPhase.Live && body.linearVelocity.y < .5f)
            {
                for (int i = 0; i < collision.contactCount; i++)
                    if (collision.GetContact(i).normal.y > .5f)
                    { match.ObserveGroundContact(body.position); break; }
            }
        }
    }
}
