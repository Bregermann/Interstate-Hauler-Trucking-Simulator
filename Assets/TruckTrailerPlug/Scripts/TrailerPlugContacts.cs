using UnityEngine;
namespace LWS.TruckTrailerPlug {
    public sealed class TrailerPlugContacts : MonoBehaviour
    {
        public TrailerPlugRig rig;
        void OnCollisionEnter(Collision collision) { rig.GroundContact(collision); }
        void OnCollisionStay(Collision collision) { rig.GroundContact(collision); }
    }
}
