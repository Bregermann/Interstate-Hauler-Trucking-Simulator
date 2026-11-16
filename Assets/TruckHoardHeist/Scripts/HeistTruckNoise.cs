using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckHoardHeist
{
    public sealed class HeistTruckNoise : MonoBehaviour
    {
        public HoardHeistMatch match;
        public SportsEventTruck truck;
        void OnCollisionEnter(Collision collision)
        {
            if (match == null || truck == null || !match.IsDriving(truck.slot) || collision.contactCount == 0) return;
            float speed = collision.impulse.magnitude / Mathf.Max(1, truck.body.mass);
            if (speed > .2f) match.ReportCollisionNoise(truck.slot, collision.GetContact(0).point, speed, truck.body.GetInstanceID(), collision.rigidbody != null ? collision.rigidbody.GetInstanceID() : collision.collider.GetInstanceID(), "TRUCK IMPACT");
        }
    }
}
