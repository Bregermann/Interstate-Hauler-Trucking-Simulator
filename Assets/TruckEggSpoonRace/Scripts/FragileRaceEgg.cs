using UnityEngine;
namespace LWS.TruckEggSpoonRace
{
    public sealed class FragileRaceEgg : MonoBehaviour
    {
        public EggSpoonRacer racer;
        public Rigidbody body;
        public Collider shell;
        public Renderer model;
        public ParticleSystem chips, yolk;
        public float breakSpeed = 4.5f;
        public bool Intact { get; private set; } = true;
        public bool Shattered { get; private set; }
        public int PhysicalContacts { get; private set; }
        public float GreatestImpact { get; private set; }
        public float SpoonContactAge { get; private set; }
        public bool Carried { get { var p = racer.spoon.InverseTransformPoint(body.position) - EggSpoonRacer.BowlCenter; return Intact && !Shattered && new Vector2(p.x, p.z).magnitude < 2.35f && p.y > .2f && p.y < 3.3f; } }
        public void Place(Vector3 position, Quaternion rotation, bool frozen)
        {
            body.isKinematic = false; body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.position = position; body.rotation = rotation; shell.enabled = true; body.detectCollisions = true; model.enabled = true;
            body.isKinematic = frozen; Intact = true; Shattered = false; PhysicalContacts = 0; GreatestImpact = SpoonContactAge = 0;
            foreach (var fx in new[] { chips, yolk }) if (fx != null) fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void Release() { body.isKinematic = false; body.WakeUp(); }
        public void Observe(float dt)
        {
            SpoonContactAge = Mathf.Max(0, SpoonContactAge - dt);
            if (!Intact || Shattered) return;
            var p = racer.spoon.InverseTransformPoint(body.position) - EggSpoonRacer.BowlCenter;
            if (p.y < -.8f || new Vector2(p.x, p.z).magnitude > 3.0f && SpoonContactAge <= 0) Lose();
        }
        void Lose() { if (!Intact) return; Intact = false; racer.Spilled(); }
        void OnCollisionStay(Collision contact) { if (contact.collider == racer.bowlCollider) SpoonContactAge = .12f; }
        void OnCollisionEnter(Collision contact)
        {
            if (racer.match == null || !racer.match.Live || !racer.match.Racing || Shattered) return;
            PhysicalContacts++; GreatestImpact = Mathf.Max(GreatestImpact, contact.relativeVelocity.magnitude);
            if (contact.collider == racer.bowlCollider) SpoonContactAge = .12f;
            if (contact.relativeVelocity.magnitude < breakSpeed && (Intact || contact.collider == racer.bowlCollider)) return;
            Lose(); Shattered = true; model.enabled = false; shell.enabled = false; body.isKinematic = true;
            Vector3 point = contact.contactCount > 0 ? contact.GetContact(0).point : body.position;
            foreach (var fx in new[] { chips, yolk }) if (fx != null) { fx.transform.position = point; fx.Play(); }
            racer.match.feedback.Play(3, racer.slot, true);
        }
        public void Stop()
        {
            if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            foreach (var fx in new[] { chips, yolk }) if (fx != null) fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
