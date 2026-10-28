using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckCamping
{
    // A real spring-supported platform. Trucks retain NWH dynamic rigidbodies;
    // no actor pose is copied from the hammock or animated by a second writer.
    public sealed class CampHammock : MonoBehaviour
    {
        public CampingWilderness wilderness;
        public Rigidbody platform;
        public BoxCollider deck;
        public Transform[] anchors;
        public LineRenderer[] ropes;
        public Transform approach;
        public float swaySeconds;
        public int ContactSteps { get; private set; }
        SpringJoint[] ropeSprings;
        Rigidbody ropePlatform;
        public bool Supports(SportsEventTruck truck)
        {
            int contacts = 0;
            foreach (var wheel in truck.wheels) if (wheel.IsGrounded && wheel.HitCollider == deck) contacts++;
            if (contacts >= 2) ContactSteps++;
            return contacts >= 2 && truck.body.linearVelocity.magnitude < .8f;
        }
        public void Tick(float dt)
        {
            swaySeconds += dt;
            platform.AddTorque(Vector3.forward * Mathf.Sin(swaySeconds * .8f) * 700, ForceMode.Force);
        }
        void LateUpdate()
        {
            if (anchors == null || ropes == null) return;
            if (ropePlatform != platform || ropeSprings == null)
            { ropePlatform = platform; ropeSprings = platform.GetComponents<SpringJoint>(); }
            for (int i = 0; i < ropes.Length; i++)
            {
                if (i >= ropeSprings.Length || ropeSprings[i] == null) continue;
                // The physical spring anchor is already in this scaled deck's
                // local space. World-sized offsets were being scaled again,
                // stretching the visible ropes across the entire clearing.
                var a = anchors[i].position; var b = platform.transform.TransformPoint(ropeSprings[i].anchor);
                ropes[i].SetPosition(0, a); ropes[i].SetPosition(1, (a + b) * .5f + Vector3.down * .18f); ropes[i].SetPosition(2, b);
            }
        }
    }
}
