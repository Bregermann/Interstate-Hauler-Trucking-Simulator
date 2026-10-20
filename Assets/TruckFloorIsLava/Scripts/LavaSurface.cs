using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckFloorIsLava
{
    public sealed class LavaSurface : MonoBehaviour
    {
        public LavaMatch match;
        void OnTriggerEnter(Collider other)
        {
            var truck=other.attachedRigidbody!=null?other.attachedRigidbody.GetComponentInParent<SportsEventTruck>():null;
            if(truck!=null)match.MarkLavaContact(truck);
        }
    }
}
