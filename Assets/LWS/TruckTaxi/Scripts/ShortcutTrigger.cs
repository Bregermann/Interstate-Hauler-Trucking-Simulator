using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ShortcutTrigger : MonoBehaviour
    {
        public string shortcutId, displayName;
        public int difficulty = 1;
        public int bonusScore = 300;
        public bool requireDirection;
        public float minimumSpeed;
        public bool scenicPoint;
        [TextArea] public string passengerDialogue;
        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
        private void OnTriggerEnter(Collider other)
        {
            if(!isActiveAndEnabled || scenicPoint) return;
            var player = other.GetComponentInParent<LwsPlayerTruck>();
            if (player == null || TruckTaxiBootstrap.Instance == null || player != TruckTaxiBootstrap.Instance.Player) return;
            if (!occupants.Add(other) || occupants.Count != 1) return;
            var body = player.GetComponent<Rigidbody>();
            if (body == null || body.linearVelocity.magnitude < minimumSpeed) return;
            if (requireDirection && Vector3.Dot(body.linearVelocity, transform.forward) <= 0) return;
            TruckTaxiBootstrap.Instance.Session?.RecordEvent(scenicPoint ? TaxiEventType.ScenicPoint : TaxiEventType.Shortcut,
                shortcutId,0,bonusScore,passengerDialogue);
        }
        private void OnTriggerExit(Collider other) { occupants.Remove(other); }
        private void OnDisable() { occupants.Clear(); }
    }
}
