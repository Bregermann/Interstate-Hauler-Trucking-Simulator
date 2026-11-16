using UnityEngine;

namespace LWS.TruckDragonKingdom.Mayhem
{
    // One lease per physical drop. Trigger callbacks never invent proximity credit.
    public sealed class DragonMayhemGold : MonoBehaviour
    {
        public Rigidbody body;
        public Collider solid;
        public DragonMayhemDirector Owner { get; private set; }
        public int Lease { get; private set; }
        public bool Collected { get; private set; }
        public float Age { get; private set; }
        public void Release(DragonMayhemDirector owner, int lease, Vector3 point, Vector3 velocity)
        {
            Owner = owner; Lease = lease; Collected = false; Age = 0;
            transform.SetParent(owner.RuntimeRoot, false); transform.SetPositionAndRotation(point, Quaternion.identity);
            gameObject.SetActive(true); body.isKinematic = false; body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; body.position = point; body.linearVelocity = velocity; body.angularVelocity = new Vector3(3,5,2);
        }
        public void Advance(float dt) { Age += dt; if (Age > Owner.configuration.goldLifetime) Retire(); }
        public void Retire() { if(!body.isKinematic)body.linearVelocity = body.angularVelocity = Vector3.zero; body.collisionDetectionMode=CollisionDetectionMode.Discrete; body.isKinematic = true; gameObject.SetActive(false); }
        void OnTriggerEnter(Collider other) => Contact(other);
        void OnTriggerStay(Collider other) => Contact(other);
        void Contact(Collider other)
        {
            if (Owner == null || Collected || Age < .65f || !Owner.Active || !Owner.adapter.TryParticipant(other, out int slot)) return;
            if (!Owner.TryCollect(this, slot)) return;
            Collected = true; Retire();
        }
    }
}
