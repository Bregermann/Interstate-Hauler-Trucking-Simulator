using System;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckMoonMadness
{
    public sealed class MoonFlag : MonoBehaviour
    {
        public MoonMatch match; public int id; public Rigidbody body; public Collider shape;
        public Transform banner;
        public SpringJoint Joint { get; private set; }
        public float HeldSeconds { get; private set; }
        public int PhysicalCollections { get; private set; }
        readonly float[] contacts = new float[8];
        Collider[] ignored; SportsEventTruck previousOwner;
        public bool Touching(int owner) => contacts[owner] > 0 && match.Clock - contacts[owner] < .16f;
        public void ResetFlag(Vector3 position)
        {
            Release(); RestoreContacts(); Array.Clear(contacts, 0, 8);
            body.gameObject.SetActive(true); body.isKinematic = false; body.position = position; body.rotation = Quaternion.identity;
            body.linearVelocity = body.angularVelocity = Vector3.zero; HeldSeconds = 0; PhysicalCollections = 0;
        }
        public bool Collect(int owner)
        {
            if (!match.Live || !Touching(owner) || !match.Rules.Collect(id, owner, true)) return false;
            RestoreContacts(); var truck = match.trucks[owner]; previousOwner = truck;
            ignored = truck.GetComponentsInChildren<Collider>(true);
            foreach (var collider in ignored) Physics.IgnoreCollision(shape, collider, true);
            Joint = gameObject.AddComponent<SpringJoint>(); Joint.connectedBody = truck.body; Joint.autoConfigureConnectedAnchor = false;
            Joint.anchor = Vector3.zero; Joint.connectedAnchor = truck.transform.InverseTransformPoint(body.position); Joint.spring = 4500; Joint.damper = 180;
            Joint.minDistance = .1f; Joint.maxDistance = .35f; Joint.tolerance = .025f; Joint.enableCollision = false;
            Joint.breakForce = 6000; Joint.breakTorque = 6000; HeldSeconds = 0; PhysicalCollections++; return true;
        }
        public void Release()
        {
            if (Joint != null)
            {
                // Destroy is deferred. Disconnecting a local anchor from its truck would
                // turn it into a WORLD anchor for intervening physics steps and fire the flag.
                // Retire its force/constraint interval first, retaining the original body reference.
                var retiring=Joint;Joint=null;retiring.spring=retiring.damper=0;retiring.minDistance=0;retiring.maxDistance=1000000;
                Destroy(retiring);
            }
            HeldSeconds = 0;
        }
        public void Step(float dt)
        {
            if (!body.isKinematic && body.useGravity) body.AddForce(Physics.gravity * (1f / 6 - 1), ForceMode.Acceleration);
            if (Joint != null) { HeldSeconds += dt; Joint.connectedAnchor = Vector3.MoveTowards(Joint.connectedAnchor,new Vector3(0,2.5f,.3f),dt*2.5f); }
            else if (ignored != null && previousOwner != null && Vector3.Distance(body.position, previousOwner.body.position) > 6) RestoreContacts();
        }
        public void RestoreContacts()
        {
            if (ignored != null) foreach (var collider in ignored) if (collider != null && shape != null) Physics.IgnoreCollision(shape, collider, false);
            ignored = null; previousOwner = null;
        }
        void Contact(Collision collision)
        {
            var truck = collision.collider.GetComponentInParent<SportsEventTruck>();
            if (truck != null && truck.session == match && truck.slot >= 0 && truck.slot < 8) contacts[truck.slot] = match.Clock;
        }
        void OnCollisionEnter(Collision collision) => Contact(collision);
        void OnCollisionStay(Collision collision) => Contact(collision);
        void OnJointBreak(float force) { Joint = null; if (match != null && match.Live) match.DropFlag(id, "PHYSICAL CARGO JOINT BROKE"); }
        void OnDestroy() { RestoreContacts(); }
    }

}
