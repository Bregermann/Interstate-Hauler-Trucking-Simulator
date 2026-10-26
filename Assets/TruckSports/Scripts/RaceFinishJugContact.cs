using UnityEngine;
namespace LWS.TruckSports
{
    // Receives native collisions in the isolated presentation scene only.
    public sealed class RaceFinishJugContact:MonoBehaviour
    {
        public RaceFinishJugPresentation owner;public int slot;
        void OnCollisionEnter(Collision collision){if(owner!=null)owner.NativeContact(slot,collision);}
    }
}
