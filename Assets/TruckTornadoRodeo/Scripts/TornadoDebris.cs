using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckTornadoRodeo
{
    public sealed class TornadoDebris : MonoBehaviour
    {
        public TornadoMatch match;
        public Rigidbody body;
        public int id;
        public float hazardRadius = 1.5f;
        public bool animal;
        public Vector3 startPosition;
        public Quaternion startRotation = Quaternion.identity;
        public float MaximumHeight { get; private set; }
        public void ResetProp()
        {
            body.isKinematic = false; body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.position = startPosition; body.rotation = startRotation; transform.SetPositionAndRotation(startPosition, startRotation); MaximumHeight = 0; body.isKinematic = true;
        }
        public void Step()
        {
            match.field.Apply(body); MaximumHeight = Mathf.Max(MaximumHeight, body.position.y);
            float inside = TornadoFieldModel.Strength(Vector3.ProjectOnPlane(body.position - match.field.Center, Vector3.up).magnitude);
            if (body.angularVelocity.magnitude < 4) body.AddTorque(new Vector3(.12f, animal ? .12f : .35f, -.1f) * inside, ForceMode.Acceleration);
            if (animal) body.AddTorque(Vector3.ClampMagnitude(Vector3.Cross(transform.up, Vector3.up) * 2 - body.angularVelocity, 2), ForceMode.Acceleration);
        }
        public void Freeze() { if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero; body.isKinematic = true; }
        void OnCollisionEnter(Collision contact)
        {
            if (contact.rigidbody == null || match == null || !match.Live || !match.rules.Live) return;
            var truck = contact.rigidbody.GetComponentInParent<SportsEventTruck>();
            if (truck != null && truck.session == match && truck.slot >= 0 && truck.slot < match.setup.count)
                match.riders[truck.slot].DebrisContact(id, contact.relativeVelocity.magnitude, contact.contactCount > 0 ? contact.GetContact(0).point : body.position);
        }
    }
}
