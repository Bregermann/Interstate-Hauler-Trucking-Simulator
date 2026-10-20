using UnityEngine;
namespace LWS.TruckHotPotato
{
    public sealed class HotPotatoContact : MonoBehaviour
    {
        public HotPotatoMatch match;
        public int slot;
        void OnCollisionEnter(Collision contact) { Report(contact); }
        void OnCollisionStay(Collision contact) { Report(contact); }
        void Report(Collision contact)
        {
            if (match == null || !match.Live || match.Countdown > 0 || contact.rigidbody == null) return;
            var other = contact.rigidbody.GetComponentInParent<HotPotatoContact>();
            if (other != null && other.match == match) match.rules.Contact(slot, other.slot, Time.fixedDeltaTime);
        }
    }
}
