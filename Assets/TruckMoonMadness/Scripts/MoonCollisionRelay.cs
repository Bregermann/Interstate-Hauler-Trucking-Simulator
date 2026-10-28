using UnityEngine;
namespace LWS.TruckMoonMadness
{
    // The original NWH cab is a separate dynamic body; it reports to the same owner.
    public sealed class MoonCollisionRelay : MonoBehaviour
    {
        public MoonTruck owner; public Rigidbody contactBody;
        void OnCollisionEnter(Collision collision) { if(owner!=null)owner.ObserveCollision(collision,contactBody); }
    }
}
