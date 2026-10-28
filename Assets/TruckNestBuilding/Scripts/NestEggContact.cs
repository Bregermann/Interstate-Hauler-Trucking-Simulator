using UnityEngine;
namespace LWS.TruckNestBuilding
{
    public sealed class NestEggContact : MonoBehaviour
    {
        public NestFamily family;
        void RecordContact(Collision contact)
        {
            var lining = family.site.slots[9];
            if (lining != null && contact.collider == lining.shape && family.site.Rooted(lining)) family.LiningContact();
        }
        void OnCollisionStay(Collision contact){RecordContact(contact);}
        void OnCollisionEnter(Collision contact) { if (family.EggPresent && contact.relativeVelocity.magnitude > 4) family.LoseEgg();RecordContact(contact); }
        void OnCollisionExit(Collision contact){family.LiningContactEnded(contact.collider);}
    }
}
