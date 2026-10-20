using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckFloorIsLava
{
    public sealed class LavaContacts : MonoBehaviour
    {
        public LavaMatch match;public int slot;
        void OnCollisionEnter(Collision collision){var other=collision.rigidbody!=null?collision.rigidbody.GetComponentInParent<LavaContacts>():null;if(other!=null&&other.match==match&&collision.contactCount>0)match.BodyContact(slot,other.slot,collision.GetContact(0).normal);}
    }
}
