using UnityEngine;
using LWS.TruckHitchSports;
namespace LWS.TruckHorrorSports
{
    /// <summary>Hauntings operate the native trailer brakes and mass distribution through supported adapters.</summary>
    public sealed class HauntedCargo : MonoBehaviour
    {
        public TrailerBody trailer;
        public HitchCoupler coupler;
        public Transform doors;
        public Transform secondDoor;
        public Light interior;
        public Rigidbody Body => trailer != null ? trailer.Body : null;
        public float Condition => trailer != null ? trailer.CargoCondition : 0;
        public int PhysicalSurges { get; private set; }
        public bool Exorcised { get; private set; }
        public bool Ready => couplingStarted && coupler.Attached == trailer;
        float age, containedUntil, nextSurge, recoverUntil, nextRecovery;
        public bool Recovering => age < recoverUntil;
        bool couplingStarted;
        Quaternion doorRotation;
        Quaternion secondRotation;
        bool originalPoseCaptured;
        public void ResetCargo()
        {
            if (coupler != null) coupler.Release();
            trailer.ResetForRound();
            foreach (var body in coupler.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
            age = containedUntil = recoverUntil = nextRecovery = 0; nextSurge = 18; PhysicalSurges = 0; couplingStarted = false; Exorcised = false;
            if (!originalPoseCaptured)
            { if (doors != null) doorRotation = doors.localRotation; if (secondDoor != null) secondRotation = secondDoor.localRotation; originalPoseCaptured = true; }
            if (doors != null) doors.localRotation = doorRotation;
            if (secondDoor != null) secondDoor.localRotation = secondRotation;
            // Native modules initialize on their normal first update while the saved aligned spawn is held.
            foreach (var body in coupler.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            foreach (var body in trailer.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        }
        public void Contain(float seconds) { containedUntil = age + seconds; trailer.SetBrakes(0); trailer.ShiftCargo(Vector3.zero); RequestRecovery(); }
        /// <summary>Player/AI counterplay: a bounded physical brace, never an upright pose write.</summary>
        public bool RequestRecovery()
        {
            if (!Ready || Body == null || Body.isKinematic || Condition <= 0 || age < nextRecovery || Vector3.Dot(Body.rotation * Vector3.up, Vector3.up) >= .65f) return false;
            recoverUntil = age + 5; nextRecovery = age + 12;
            // Reset the supernatural COM bias while bracing, preserving the authored native COM.
            containedUntil = Mathf.Max(containedUntil, recoverUntil); trailer.ShiftCargo(Vector3.zero);
            return true;
        }
        void ApplyRecoveryTorque()
        {
            if (!Recovering || Body.isKinematic) return;
            Vector3 up = Body.rotation * Vector3.up;
            float alignment = Vector3.Dot(up, Vector3.up);
            if (alignment > .985f) { recoverUntil = age; return; }
            Vector3 axis = Vector3.Cross(up, Vector3.up);
            if (axis.sqrMagnitude < .0001f) axis = Vector3.ProjectOnPlane(Body.rotation * Vector3.forward, Vector3.up);
            Vector3 tiltAngularVelocity = Vector3.ProjectOnPlane(Body.angularVelocity, Vector3.up);
            Vector3 restoring = axis.normalized * Mathf.Min(2.8f, Mathf.Acos(Mathf.Clamp(alignment, -1, 1)) * 3.2f);
            Body.AddTorque(Vector3.ClampMagnitude(restoring - tiltAngularVelocity * 2, 3.5f), ForceMode.Acceleration);
        }
        public void Exorcise() { Exorcised = true; Contain(20); }
        public void TickHaunting(float dt, int escalation, bool live, bool headlights, float unrest = 1)
        {
            if (!live || trailer == null || Body == null) return;
            age += dt;
            if (!couplingStarted)
            {
                couplingStarted = coupler.Attached == trailer || coupler.TryCouple(trailer);
                if (!couplingStarted) return;
                foreach (var body in coupler.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                foreach (var body in trailer.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
            }
            if (coupler.Attached != trailer) { coupler.GuideCoupling(trailer, dt); return; }
            bool active = !Exorcised && age > containedUntil && escalation > 0;
            float pulse = active ? Mathf.Max(0, Mathf.Sin(age * .8f)) : 0;
            trailer.SetBrakes(active && Mathf.Sin(age * .37f) > .88f ? .65f : 0);
            // Original native rear half-track is about .785m. A 1.05m lateral COM
            // shift put the mass outside its support polygon and left the hull on its side.
            // Keep threatened, real weight transfer without an unavoidable sustained rollover.
            trailer.ShiftCargo(active ? new Vector3(Mathf.Sin(age * 1.6f) * Mathf.Min(.3f, .12f * escalation), 0, Mathf.Sin(age * .43f) * .18f) : Vector3.zero);
            ApplyRecoveryTorque();
            if (active)
            {
                float aggression = Mathf.Clamp(unrest, 1, 2.5f);
                Body.AddForce(Vector3.ProjectOnPlane(Body.rotation * Vector3.right,Vector3.up).normalized * Mathf.Sin(age * 1.2f) * (headlights ? .3f : .5f) * escalation * aggression, ForceMode.Acceleration);
                if (age >= nextSurge) { nextSurge = age + 16 / aggression; PhysicalSurges++; Body.AddTorque(Vector3.up * (.35f * aggression), ForceMode.VelocityChange); }
            }
            if (doors != null) doors.localRotation = doorRotation * Quaternion.Euler(0, pulse * 72, 0);
            if (secondDoor != null) secondDoor.localRotation = secondRotation * Quaternion.Euler(0, -pulse * 72, 0);
            if (interior != null) { interior.enabled = active; interior.intensity = pulse * 4; }
        }
    }
}
