using Assets.CoreCollapse.Scripts.Structural;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckPinataPanic
{
    [RequireComponent(typeof(Rigidbody), typeof(PinataSuspensionSupport))]
    public sealed class PinataShell : Collapser
    {
        public PinataMatch match;
        public Rigidbody body;
        public ConfigurableJoint suspension;
        public Transform intact;
        public Collider[] shellColliders;
        public Rigidbody[] fragments;
        public LineRenderer rope;
        public Vector3 restPosition = new Vector3(0, 5.2f, 0);
        [Tooltip("Full four-truck shell health. Smaller rosters use the same health per active competitor.")]
        public float maximumHealth = 2400;
        public bool Ruptured { get; private set; }
        public int AcceptedContacts { get; private set; }
        public float Health => Mathf.Max(0, currentHp);
        public float MaximumHealth => startingHp;
        readonly float[] nextHit = new float[4];
        Vector3[] fragmentPositions; Quaternion[] fragmentRotations;
        int hitSerial;
        public void ResetShell()
        {
            support = GetComponent<Support>(); support.ClearSupports();
            float fullHealth = float.IsFinite(maximumHealth) ? Mathf.Clamp(maximumHealth, 2000, 6000) : 2400;
            startingHp = fullHealth * Mathf.Clamp(match.setup.count, 1, 4) / 4f;
            currentHp = startingHp; Ruptured = false; AcceptedContacts = hitSerial = 0;
            System.Array.Clear(nextHit, 0, nextHit.Length);
            if (fragmentPositions == null)
            {
                fragmentPositions = new Vector3[fragments.Length]; fragmentRotations = new Quaternion[fragments.Length];
                for (int i = 0; i < fragments.Length; i++) { fragmentPositions[i] = fragments[i].transform.localPosition; fragmentRotations[i] = fragments[i].transform.localRotation; }
            }
            foreach (var fragment in fragments) { if (!fragment.isKinematic) fragment.linearVelocity = fragment.angularVelocity = Vector3.zero; fragment.isKinematic = true; fragment.gameObject.SetActive(false); }
            body.isKinematic = false; body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.position = restPosition; body.rotation = Quaternion.identity; transform.SetPositionAndRotation(body.position, body.rotation);
            intact.gameObject.SetActive(true); foreach (var collider in shellColliders) collider.enabled = true;
            DrawRope();
        }
        // The vendor's public damage API remains the only health mutation path.
        public override void ReceiveDamage(float damage)
        {
            if (Ruptured || match == null || !match.CanStrike || !float.IsFinite(damage) || damage <= 0) return;
            base.ReceiveDamage(damage);
        }
        void OnCollisionEnter(Collision contact)
        {
            if (match == null || !match.CanStrike || Ruptured || contact.contactCount == 0) return;
            var otherBody = contact.rigidbody;
            var truck = otherBody != null ? otherBody.GetComponentInParent<SportsEventTruck>() : null;
            if (truck == null || truck.session != match || truck.slot < 0 || truck.slot >= match.setup.count || !truck.Powered || truck.body.isKinematic) return;
            // Tangential glancing/resting contact is not damage. Unity provides
            // the real contact relative velocity; neither AI nor ACTION fakes it.
            float speed = Mathf.Abs(Vector3.Dot(contact.relativeVelocity, contact.GetContact(0).normal));
            if (speed < 2.5f || match.rules.Clock < nextHit[truck.slot]) return;
            nextHit[truck.slot] = match.rules.Clock + .5f;
            float before = Health; ReceiveDamage(Mathf.Clamp((speed - 1.5f) * 32, 25, 480));
            float lost = before - Health;
            if (lost <= 0) return;
            AcceptedContacts++; match.BankImpact(truck.slot, ++hitSerial, lost, contact.GetContact(0).point, speed);
        }
        protected override void Collapse()
        {
            if (Ruptured) return;
            Ruptured = true; // latch before vendor callbacks/reentrant contacts
            base.Collapse();
            intact.gameObject.SetActive(false); foreach (var collider in shellColliders) collider.enabled = false;
            for (int i = 0; i < fragments.Length; i++)
            {
                var fragment = fragments[i]; fragment.gameObject.SetActive(true);
                fragment.position = transform.TransformPoint(fragmentPositions[i]); fragment.rotation = transform.rotation * fragmentRotations[i];
                fragment.isKinematic = false;
                var outward = fragment.worldCenterOfMass - body.position;
                fragment.linearVelocity = body.linearVelocity + outward.normalized * 2 + Vector3.up * 2;
                fragment.angularVelocity = new Vector3(i % 3 - 1, .6f, (i + 1) % 3 - 1) * 2;
            }
            var origin = body.position; body.isKinematic = true; match.ReleaseCandy(origin);
        }
        public void Freeze()
        {
            if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            foreach (var fragment in fragments) { if (!fragment.isKinematic) fragment.linearVelocity = fragment.angularVelocity = Vector3.zero; fragment.isKinematic = true; }
        }
        void LateUpdate() => DrawRope();
        void DrawRope()
        {
            if (rope == null || suspension == null) return;
            rope.enabled = !Ruptured; rope.positionCount = 2;
            Vector3 anchor = suspension.connectedBody == null ? suspension.connectedAnchor : suspension.connectedBody.transform.TransformPoint(suspension.connectedAnchor);
            rope.SetPosition(0, anchor); rope.SetPosition(1, transform.TransformPoint(suspension.anchor));
        }
    }
}
