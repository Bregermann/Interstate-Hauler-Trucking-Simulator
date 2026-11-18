using System;
using UnityEngine;

namespace LWS.TruckExtremeHauling
{
    public sealed class OrbitalFreightThrusters : MonoBehaviour
    {
        public Rigidbody body;
        public float acceleration = 4, rotationAcceleration = 2, fuel = 100;
        public float FuelUsed { get; private set; }
        Rigidbody capturedPayload;
        Rigidbody[] tractorBodies;
        public void SetCapturedPayload(Rigidbody payload)
        {
            capturedPayload=payload;
            if(payload!=null&&tractorBodies==null)tractorBodies=GetComponentsInChildren<Rigidbody>(true);
        }
        Vector3 InertiaTimes(Vector3 angularAcceleration,Vector3 center)
        {
            Vector3 result=BodyInertia(capturedPayload,angularAcceleration,center);
            foreach(var part in tractorBodies)if(part!=null&&!part.isKinematic)result+=BodyInertia(part,angularAcceleration,center);
            return result;
        }
        static Vector3 BodyInertia(Rigidbody part,Vector3 acceleration,Vector3 center)
        {
            Quaternion axes=part.rotation*part.inertiaTensorRotation;
            Vector3 intrinsic=axes*Vector3.Scale(Quaternion.Inverse(axes)*acceleration,part.inertiaTensor);
            Vector3 offset=part.worldCenterOfMass-center;
            return intrinsic+part.mass*(offset.sqrMagnitude*acceleration-Vector3.Dot(offset,acceleration)*offset);
        }
        void ApplyCapturedRotation(Vector3 localRotation,Vector3 worldForce,float fraction)
        {
            // Native docking makes a rigid assembly. Translational thrusters are
            // mounted on the tractor, away from the loaded assembly's center of
            // mass. Its RCS must cancel that real force lever, not chase the spin
            // after the cargo has swung the cab into a dock.
            float mass=capturedPayload.mass;Vector3 center=capturedPayload.worldCenterOfMass*mass;
            foreach(var part in tractorBodies)if(part!=null&&!part.isKinematic){mass+=part.mass;center+=part.worldCenterOfMass*part.mass;}
            center/=Mathf.Max(1,mass);
            Vector3 acceleration=body.rotation*localRotation*(rotationAcceleration*.175f*fraction);
            Vector3 angularMomentum=InertiaTimes(body.angularVelocity,center);
            Vector3 torque=InertiaTimes(acceleration,center)+Vector3.Cross(body.angularVelocity,angularMomentum)
                -Vector3.Cross(body.worldCenterOfMass-center,worldForce);
            body.AddTorque(torque,ForceMode.Force);
        }
        public void ResetFuel() { fuel = 100; FuelUsed = 0; capturedPayload=null; }
        public void Apply(Vector3 translation, Vector3 rotation, bool brake, float dt)
        {
            if (body == null || body.isKinematic || dt <= 0 || fuel <= 0) return;
            translation = Vector3.ClampMagnitude(translation, 1);
            rotation = Vector3.ClampMagnitude(rotation, 1);
            float expenditure = (translation.magnitude + rotation.magnitude * .5f + (brake ? .7f : 0)) * dt;
            float fraction = Mathf.Min(1, fuel / Mathf.Max(expenditure, .0001f));
            fuel -= expenditure * fraction; FuelUsed += expenditure * fraction;
            body.AddRelativeForce(translation * (acceleration * fraction), ForceMode.Acceleration);
            if(capturedPayload!=null&&!capturedPayload.isKinematic)
                ApplyCapturedRotation(rotation,body.rotation*translation*(acceleration*fraction*body.mass),fraction);
            else body.AddRelativeTorque(rotation * (rotationAcceleration * fraction), ForceMode.Acceleration);
            if (brake)
            {
                body.AddForce(-body.linearVelocity * Mathf.Min(2, 1 / dt), ForceMode.Acceleration);
                body.AddTorque(-body.angularVelocity * Mathf.Min(2, 1 / dt), ForceMode.Acceleration);
            }
        }
    }
}
