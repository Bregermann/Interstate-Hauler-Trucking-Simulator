using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiWorldExit : MonoBehaviour
    {
        public const float ArrivalExclusionRadiusMeters = 30f;
        public string destinationScene;
        public Transform arrival;
        private float nextAttempt;
        public static bool CorrectDirection(Vector3 velocity, Vector3 exitForward) => velocity.sqrMagnitude > .25f &&
            Vector3.Dot(velocity.normalized, exitForward.normalized) > .4f;
        public static bool CorrectDirection(Vector3 velocity, Vector3 exitForward, Vector3 vehicleForward)
        {
            // An inbound tractor can roll backwards downhill through the boundary. That is not an outbound drive.
            Vector3 heading = Vector3.ProjectOnPlane(vehicleForward, Vector3.up);
            Vector3 outbound = Vector3.ProjectOnPlane(exitForward, Vector3.up);
            return CorrectDirection(velocity, exitForward) && Vector3.Dot(heading.normalized, outbound.normalized) > .4f;
        }
        public static bool IsSafeArrival(Vector3 point, Vector3 exitPosition)
        {
            Vector3 horizontal = point - exitPosition; horizontal.y = 0;
            return horizontal.sqrMagnitude >= ArrivalExclusionRadiusMeters * ArrivalExclusionRadiusMeters;
        }
        private void OnTriggerEnter(Collider other)
        {
            var host = TruckTaxiBootstrap.Instance;
            if (host?.Player == null || TruckTaxiWorldTravel.IsBusy || Time.unscaledTime < nextAttempt ||
                other.GetComponentInParent<LwsPlayerTruck>() != host.Player) return;
            var body = host.Player.GetComponent<Rigidbody>();
            if (body == null || !CorrectDirection(body.linearVelocity, transform.forward, body.rotation * Vector3.forward)) return;
            nextAttempt = Time.unscaledTime + 3;
            TruckTaxiWorldTravel.TryTravel(host, destinationScene, false);
        }
    }
}
