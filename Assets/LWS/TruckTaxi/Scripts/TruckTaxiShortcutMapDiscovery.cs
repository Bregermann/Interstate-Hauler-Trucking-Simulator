using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Additive presentation-only observer on the existing shortcut collider. Does not award score.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiShortcutMapDiscovery : MonoBehaviour
    {
        private ShortcutTrigger shortcut;
        private TruckTaxiMapMarkers markers;
        public void Initialize(ShortcutTrigger value,TruckTaxiMapMarkers owner) { shortcut=value; markers=owner; }
        private void OnTriggerEnter(Collider other)
        {
            if(shortcut==null || !shortcut.isActiveAndEnabled || shortcut.scenicPoint || markers==null) return;
            var player=other.GetComponentInParent<LwsPlayerTruck>();
            if(player==null || player!=TruckTaxiBootstrap.Instance?.Player) return;
            var body=player.GetComponent<Rigidbody>();
            if(body==null || body.linearVelocity.magnitude<shortcut.minimumSpeed ||
                (shortcut.requireDirection && Vector3.Dot(body.linearVelocity,shortcut.transform.forward)<=0)) return;
            markers.DiscoverShortcut(shortcut.shortcutId);
        }
    }
}
