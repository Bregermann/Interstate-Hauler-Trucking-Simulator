using System;
using UnityEngine;

namespace LWS.TruckExtremeHauling
{
    public sealed class OrbitalFreightThrusters : MonoBehaviour
    {
        public Rigidbody body;
        public float acceleration = 4, rotationAcceleration = 2, fuel = 100;
        public float FuelUsed { get; private set; }
        public void ResetFuel() { fuel = 100; FuelUsed = 0; }
        public void Apply(Vector3 translation, Vector3 rotation, bool brake, float dt)
        {
            if (body == null || body.isKinematic || dt <= 0 || fuel <= 0) return;
            translation = Vector3.ClampMagnitude(translation, 1);
            rotation = Vector3.ClampMagnitude(rotation, 1);
            float expenditure = (translation.magnitude + rotation.magnitude * .5f + (brake ? .7f : 0)) * dt;
            float fraction = Mathf.Min(1, fuel / Mathf.Max(expenditure, .0001f));
            fuel -= expenditure * fraction; FuelUsed += expenditure * fraction;
            body.AddRelativeForce(translation * (acceleration * fraction), ForceMode.Acceleration);
            body.AddRelativeTorque(rotation * (rotationAcceleration * fraction), ForceMode.Acceleration);
            if (brake)
            {
                body.AddForce(-body.linearVelocity * Mathf.Min(2, 1 / dt), ForceMode.Acceleration);
                body.AddTorque(-body.angularVelocity * Mathf.Min(2, 1 / dt), ForceMode.Acceleration);
            }
        }
    }
}
