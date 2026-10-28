using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckMechanicalBull
{
    public sealed class MechanicalBullRider : MonoBehaviour
    {
        public SportsEventTruck truck;
        public MechanicalBullMatch match;
        public int slot;
        public Vector3 spawn, waiting;
        public Rigidbody[] attachedBodies;
        Vector3 originalCOM;
        bool[] attachedKinematic, attachedCollisions;
        float originalDamping, noContact, elapsed;
        bool cached;
        public bool DeckContact { get; private set; }
        public bool Fell { get; private set; }
        public Vector2 LastLean { get; private set; }
        public int BalanceSteps { get; private set; }
        void Awake() { Cache(); Stop(); }
        void Cache()
        {
            if (cached) return;
            originalCOM = truck.body.centerOfMass; originalDamping = truck.body.angularDamping;
            attachedBodies = attachedBodies ?? System.Array.Empty<Rigidbody>(); attachedKinematic = new bool[attachedBodies.Length]; attachedCollisions = new bool[attachedBodies.Length];
            for (int i = 0; i < attachedBodies.Length; i++) { attachedKinematic[i] = attachedBodies[i].isKinematic; attachedCollisions[i] = attachedBodies[i].detectCollisions; }
            cached = true;
        }
        public void Prepare(bool active)
        {
            Cache();
            noContact = elapsed = 0; Fell = DeckContact = false; LastLean = Vector2.zero; BalanceSteps = 0;
            truck.body.centerOfMass = originalCOM; truck.body.angularDamping = originalDamping;
            if (active)
            {
                for (int i = 0; i < attachedBodies.Length; i++) { attachedBodies[i].isKinematic = attachedKinematic[i]; attachedBodies[i].detectCollisions = attachedCollisions[i]; }
                truck.Prepare(spawn, Quaternion.identity, true); truck.tractorMotor?.Configure(); truck.body.WakeUp();
            }
            else { truck.Park(waiting); truck.tractorMotor?.ResetAttachedBodies(); foreach (var body in attachedBodies) { body.isKinematic = true; body.detectCollisions = false; } }
        }
        public Vector2 BalanceAI(float skill)
        {
            var localUp = Quaternion.Inverse(truck.body.rotation) * Vector3.up;
            var rate = Quaternion.Inverse(truck.body.rotation) * truck.body.angularVelocity;
            return Vector2.ClampMagnitude(new Vector2(localUp.x * 2.4f + rate.z * .38f, localUp.z * 2.4f - rate.x * .38f) * skill, 1);
        }
        public void Step(Vector2 lean, bool brace, float dt, bool riding)
        {
            if (truck.body.isKinematic) return;
            elapsed += dt; LastLean = Vector2.ClampMagnitude(lean, 1); BalanceSteps++;
            truck.TickDrive(Vector2.zero);
            // NWH owns tires/suspension/contact. These are the sport's bounded
            // Mattress-style balance impulses, not a second drive controller.
            var body = truck.body;
            body.centerOfMass = originalCOM + new Vector3(LastLean.x * .32f, 0, LastLean.y * .38f);
            body.angularDamping = brace ? 1.8f : originalDamping;
            body.AddTorque(body.rotation * (Vector3.right * LastLean.y * 1.7f - Vector3.forward * LastLean.x * 1.7f), ForceMode.Acceleration);
            DeckContact = false;
            foreach (var wheel in truck.wheels)
                if (wheel != null && wheel.enabled && wheel.IsGrounded && wheel.HitCollider != null && wheel.HitCollider == match.machine.deck) DeckContact = true;
            noContact = DeckContact ? 0 : noContact + dt;
            if (!riding) return;
            Vector3 point = body.worldCenterOfMass;
            Fell = point.y < match.machine.TopCenter.y - 1.2f || match.machine.Outside(point, .8f) && noContact > .35f ||
                noContact > 1.5f && Vector3.Dot(body.rotation * Vector3.up, Vector3.up) < .35f;
        }
        public void Stop()
        {
            Cache();
            truck.Power(false); truck.StopSound();
            if (!truck.body.isKinematic) truck.body.linearVelocity = truck.body.angularVelocity = Vector3.zero;
            truck.body.isKinematic = true;
            foreach (var body in attachedBodies) { if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero; body.isKinematic = true; body.detectCollisions = false; }
            if (cached) { truck.body.centerOfMass = originalCOM; truck.body.angularDamping = originalDamping; }
        }
        public void EndRide(bool fell)
        {
            if (!fell) { Stop(); return; }
            // A real fall keeps gravity/collisions until the next turn rather
            // than freezing the losing truck in mid-air at the score boundary.
            truck.Power(false); truck.StopSound();
        }
    }
}
