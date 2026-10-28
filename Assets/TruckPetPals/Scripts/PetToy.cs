using UnityEngine;
namespace LWS.TruckPetPals
{
    public sealed class PetToy : MonoBehaviour
    {
        public string id;
        public Rigidbody body;
        public Vector3 home;
        public Quaternion homeRotation;
        public AudioSource squeak;
        public bool Carried => grip != null;
        public int Throws { get; private set; }
        public int PhysicalReturns { get; private set; }
        FixedJoint grip;
        int carrier = -1;
        public void ResetToy()
        {
            Release(-1, false); body.isKinematic = false; body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true; body.position = home; body.rotation = homeRotation; body.isKinematic = false; carrier = -1; Throws = PhysicalReturns = 0;
        }
        public bool Throw(Vector3 origin, Vector3 forward)
        {
            if (Carried || Vector3.Distance(origin, body.position) > 12) return false;
            body.AddForce(forward.normalized * 8 + Vector3.up * 2, ForceMode.VelocityChange); Throws++;
            if (squeak != null && squeak.clip != null) squeak.Play(); return true;
        }
        public bool Retrieve(Rigidbody pet, int owner)
        {
            if (Carried || pet == null || Vector3.Distance(pet.position, body.position) > 2 || body.linearVelocity.magnitude > 3) return false;
            grip = gameObject.AddComponent<FixedJoint>(); grip.connectedBody = pet; grip.breakForce = 500; grip.breakTorque = 300; grip.enableCollision = false; carrier = owner; return true;
        }
        public bool Release(int owner, bool returned)
        {
            if (grip == null || owner >= 0 && owner != carrier) return false;
            grip.connectedBody = null; Destroy(grip); grip = null; carrier = -1; if (returned) PhysicalReturns++; return true;
        }
    }
}
