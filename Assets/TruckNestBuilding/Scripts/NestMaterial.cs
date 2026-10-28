using UnityEngine;
namespace LWS.TruckNestBuilding
{
    public sealed class NestMaterial : MonoBehaviour
    {
        public string pieceId;
        public NestMaterialKind kind;
        public Rigidbody body;
        public Collider shape;
        public Vector3 home;
        public Vector3 pickupStand;
        public Quaternion homeRotation = Quaternion.identity;
        public int ReservedBy { get; private set; } = -1;
        public int LastOwner { get; set; } = -1;
        public NestBuildSite Site { get; private set; }
        public int Socket { get; private set; } = -1;
        public FixedJoint Interlock { get; private set; }
        public FixedJoint Grip { get; private set; }
        public bool Available => Site == null && Grip == null && ReservedBy < 0;
        readonly NestMaterial[] touches = new NestMaterial[8];
        readonly float[] ages = new float[8];
        NestBuildSite platform;
        float platformAge;
        public bool Reserve(int owner) { if (owner < 0 || owner > 3 || !Available) return false; ReservedBy = owner; return true; }
        public void CancelReservation(int owner) { if (Grip == null && ReservedBy == owner) ReservedBy = -1; }
        public void Grab(Rigidbody hook, int owner)
        {
            if (hook == null || ReservedBy != owner || Grip != null || Site != null || Vector3.Distance(hook.position, body.worldCenterOfMass) > .55f) return;
            Grip = gameObject.AddComponent<FixedJoint>(); Grip.connectedBody = hook; Grip.breakForce = 2500; Grip.breakTorque = 1800; Grip.enableCollision = false; LastOwner = owner;
        }
        public void Drop() { if (Grip != null) { Grip.connectedBody = null; Destroy(Grip); } Grip = null; ReservedBy = -1; body.WakeUp(); }
        public void Lock(NestBuildSite site, int socket, Rigidbody support)
        {
            if (Grip != null || Interlock != null) return;
            Interlock = gameObject.AddComponent<FixedJoint>(); Interlock.connectedBody = support; Interlock.breakForce = kind == NestMaterialKind.Branch ? 3500 : 1600; Interlock.breakTorque = 1500; Interlock.enableCollision = false;
            Site = site; Socket = socket; ReservedBy = -1;
        }
        public void Unlock()
        {
            if (Interlock != null) { Interlock.connectedBody = null; Destroy(Interlock); }
            Interlock = null; Site = null; Socket = -1;
        }
        public void ResetPiece()
        {
            Drop(); Unlock(); body.isKinematic = false; body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true; body.position = home; body.rotation = homeRotation; body.isKinematic = false; body.WakeUp(); LastOwner = -1; platformAge = 0;
            System.Array.Clear(ages, 0, ages.Length);
        }
        public void Tick(float dt)
        {
            // PhysX does not send Stay while the resting body sleeps. A real
            // collision pair remains present until Exit; its support cannot
            // expire before the unchanged1.2s settling interval. A geometric
            // guard also invalidates reset/disabled/destroyed collider pairs.
            if(platformAge>0&&(platform==null||!ContactPairPresent(platform.deck)))platformAge=0;
            for(int i=0;i<ages.Length;i++)if(ages[i]>0&&(touches[i]==null||!ContactPairPresent(touches[i].shape)))ages[i]=0;
        }
        bool ContactPairPresent(Collider other)
        {
            if(shape==null||!shape.enabled||!shape.gameObject.activeInHierarchy||other==null||!other.enabled||!other.gameObject.activeInHierarchy)return false;
            var bounds=shape.bounds;bounds.Expand((shape.contactOffset+other.contactOffset+.005f)*2);return bounds.Intersects(other.bounds);
        }
        public Rigidbody ContactSupport(NestBuildSite site, int socket)
        {
            if (platformAge > 0 && platform == site && ContactPairPresent(site.deck) && NestBuildSite.SupportAllowed(socket, -1)) return site.body;
            for (int i = 0; i < touches.Length; i++) if (ages[i] > 0 && touches[i] != null && ContactPairPresent(touches[i].shape) && touches[i].Site == site && NestBuildSite.SupportAllowed(socket, touches[i].Socket) && site.Rooted(touches[i])) return touches[i].body;
            return null;
        }
        void OnCollisionEnter(Collision contact){RecordContact(contact);}
        void OnCollisionStay(Collision contact){RecordContact(contact);}
        void RecordContact(Collision contact)
        {
            var site = contact.collider.GetComponentInParent<NestBuildSite>();
            if (site != null && contact.collider == site.deck) { platform = site; platformAge = .12f; }
            var material = contact.collider.GetComponentInParent<NestMaterial>(); if (material == null || material == this) return;
            for (int i = 0; i < touches.Length; i++) if (touches[i] == material || ages[i] <= 0) { touches[i] = material; ages[i] = .12f; break; }
        }
        void OnCollisionExit(Collision contact)
        {
            if(platform!=null&&contact.collider==platform.deck){platformAge=0;platform=null;}
            var material=contact.collider.GetComponentInParent<NestMaterial>();if(material==null)return;
            for(int i=0;i<touches.Length;i++)if(touches[i]==material){touches[i]=null;ages[i]=0;}
        }
        void OnJointBreak(float force)
        {
            if (Grip == null) ReservedBy = -1;
            if (Interlock == null) { Site = null; Socket = -1; }
        }
    }
}
