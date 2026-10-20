using UnityEngine;
namespace LWS.TruckSeesaw
{
    public sealed class SeesawContacts : MonoBehaviour
    {
        public SeesawMatch match; public int slot;
        void OnCollisionEnter(Collision collision)
        {
            var other=collision.rigidbody!=null?collision.rigidbody.GetComponentInParent<SeesawContacts>():null;
            if(other!=null&&other.match==match&&collision.contactCount>0)match.BodyContact(slot,other.slot,collision.GetContact(0).normal);
            if(collision.contactCount>0)match.QuarryContact(slot,collision);
        }
    }
}
