using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckPinataPanic
{
    public sealed class PinataCandy : MonoBehaviour
    {
        public PinataMatch match;
        public Rigidbody body;
        public PinataCandyKind kind;
        public int id;
        public ParticleSystem pickupBurst;
        public int Generation { get; private set; } = -1;
        public bool Released { get; private set; }
        public int CollectedBy { get; private set; } = -1;
        public void Release(int generation, Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            Generation = generation; CollectedBy = -1; Released = true;
            gameObject.SetActive(true); body.isKinematic = false; body.detectCollisions = true;
            body.position = position; body.rotation = rotation; transform.SetPositionAndRotation(position, rotation);
            body.linearVelocity = velocity; body.angularVelocity = new Vector3(id % 3 - 1, .7f, (id + 1) % 3 - 1) * 3;
        }
        public void Retire()
        {
            Released = false; if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true; body.detectCollisions = false; gameObject.SetActive(false);
        }
        void OnCollisionEnter(Collision contact)
        {
            if (!Released || match == null || !match.CanCollect || contact.rigidbody == null) return;
            var truck = contact.rigidbody.GetComponentInParent<SportsEventTruck>();
            if (truck == null || truck.session != match || truck.slot < 0 || truck.slot >= match.setup.count || truck.body.isKinematic || !truck.Powered) return;
            if (match.Collect(this, truck)) { CollectedBy = truck.slot; Retire(); }
        }
    }
}
