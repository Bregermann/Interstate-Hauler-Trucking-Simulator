using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiWorldExit : MonoBehaviour
    {
        public string destinationScene;
        public Transform arrival;
        private float nextAttempt;
        public static bool CorrectDirection(Vector3 velocity, Vector3 exitForward) => velocity.sqrMagnitude > .25f &&
            Vector3.Dot(velocity.normalized, exitForward.normalized) > .4f;
        private void OnTriggerEnter(Collider other)
        {
            var host = TruckTaxiBootstrap.Instance;
            if (host?.Player == null || TruckTaxiWorldTravel.IsBusy || Time.unscaledTime < nextAttempt ||
                other.GetComponentInParent<LwsPlayerTruck>() != host.Player) return;
            var body = host.Player.GetComponent<Rigidbody>();
            if (body == null || !CorrectDirection(body.linearVelocity, transform.forward)) return;
            nextAttempt = Time.unscaledTime + 3;
            TruckTaxiWorldTravel.TryTravel(host, destinationScene, false);
        }
    }
}
