using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckExtremeHauling
{
    public sealed class PlinkoLandingTray : MonoBehaviour
    {
        readonly Dictionary<Rigidbody,float> contact = new Dictionary<Rigidbody,float>();
        void OnCollisionStay(Collision collision) { if (collision.rigidbody != null) contact[collision.rigidbody] = Time.fixedTime; }
        void OnCollisionExit(Collision collision) { if (collision.rigidbody != null) contact.Remove(collision.rigidbody); }
        public bool Supports(Rigidbody body) => body != null && contact.TryGetValue(body,out float last) && Time.fixedTime - last <= Time.fixedDeltaTime * 2.5f;
        public void ResetContacts() => contact.Clear();
    }
}
