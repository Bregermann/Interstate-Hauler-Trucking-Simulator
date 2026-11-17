using System;
using UnityEngine;

namespace LWS.TruckExtremeHauling
{
    public sealed class PhysicalCableCar : MonoBehaviour
    {
        public Rigidbody load;
        public Rigidbody trolley;
        public SpringJoint cable;
        public LineRenderer line;
        public Vector3 routeEnd;
        public float acceleration = 6, maxSpeed = 18;
        public bool Released { get; private set; }
        float spring, damper;
        Vector3 trolleyStart;
        void Awake() { if (cable != null) { spring = cable.spring; damper = cable.damper; } if (trolley != null) trolleyStart = trolley.transform.position; }
        public void Drive(float throttle, float brake)
        {
            if (Released || load == null || trolley == null) return;
            Vector3 direction = (routeEnd - trolley.position).normalized;
            if (Vector3.Dot(trolley.linearVelocity, direction) < maxSpeed)
                trolley.AddForce(direction * throttle * acceleration, ForceMode.Acceleration);
            trolley.AddForce(-trolley.linearVelocity * brake * 3, ForceMode.Acceleration);
        }
        public void Release() { if (Released) return; Released = true; if (cable != null) cable.connectedBody = null; if (cable != null) { cable.spring = 0; cable.damper = 0; } }
        public void ResetCable() { Released = false; if (trolley != null) { trolley.interpolation=RigidbodyInterpolation.None;trolley.position = trolleyStart;trolley.transform.position=trolleyStart;trolley.interpolation=RigidbodyInterpolation.Interpolate; trolley.linearVelocity = trolley.angularVelocity = Vector3.zero; } if (cable != null) { cable.connectedBody = trolley; cable.spring = spring; cable.damper = damper; } }
        void LateUpdate() { if (line == null || load == null || trolley == null) return; line.enabled = !Released; line.positionCount = 2; line.SetPosition(0, trolley.position); line.SetPosition(1, load.worldCenterOfMass); }
    }
}
