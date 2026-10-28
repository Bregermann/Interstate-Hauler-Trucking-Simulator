using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckEggSpoonRace
{
    public sealed class EggSpoonRacer : MonoBehaviour
    {
        public EggSpoonMatch match;
        public SportsEventTruck truck;
        public int slot;
        public Transform spoon;
        public Transform gearBase;
        public static readonly Vector3 BowlCenter = new Vector3(0, 0, 5);
        public Vector3 BowlPosition => spoon.TransformPoint(BowlCenter);
        public Rigidbody spoonBody;
        public MeshCollider bowlCollider;
        public FragileRaceEgg egg;
        public Rigidbody[] originalAttachedBodies;
        public Vector3 localAnchor;
        public float spawnLift;
        public float TrimPitch { get; private set; }
        public float TrimRoll { get; private set; }
        public float ServiceSeconds { get; private set; }
        public bool AtStation { get; private set; }
        public Vector2 LastDrive { get; private set; }
        public int DriveSteps { get; private set; }
        Vector3 previous;
        float inverted;
        void Awake() { Stop(); }
        public void Prepare(bool active)
        {
            foreach (var body in originalAttachedBodies) { body.isKinematic = !active; body.detectCollisions = active; }
            truck.gameObject.SetActive(active); spoon.gameObject.SetActive(active); gearBase.gameObject.SetActive(active); egg.gameObject.SetActive(active);
            TrimPitch = TrimRoll = ServiceSeconds = inverted = 0; DriveSteps = 0;
            if (!active) { Stop(); return; }
            truck.Prepare(EggSpoonCourse.Point(slot, 0) + Vector3.up * spawnLift, Quaternion.identity, false);
            truck.body.isKinematic = true; truck.body.detectCollisions = true;
            foreach (var body in originalAttachedBodies) body.isKinematic = true;
            MoveSpoon(true); egg.Place(BowlPosition + spoon.up * 1.1f, spoon.rotation, true); previous = truck.body.position;
        }
        public void Release()
        {
            truck.body.isKinematic = false; foreach (var body in originalAttachedBodies) body.isKinematic = false;
            truck.Power(true); truck.tractorMotor.Configure(); egg.Release(); truck.body.WakeUp();
        }
        void MoveSpoon(bool snap = false)
        {
            var position = truck.body.position + truck.body.rotation * localAnchor;
            gearBase.SetPositionAndRotation(truck.body.position, truck.body.rotation);
            var rotation = truck.body.rotation * Quaternion.Euler(TrimPitch, 0, TrimRoll);
            if (snap) { spoonBody.position = position; spoonBody.rotation = rotation; spoon.SetPositionAndRotation(position, rotation); }
            else { spoonBody.MovePosition(position); spoonBody.MoveRotation(rotation); }
        }
        public Vector2 AI(out bool trim)
        {
            var rules = match.rules; float station = EggSpoonCourse.StationZ(rules.checkpoint[slot]);
            if (!rules.carrying[slot] && Vector3.ProjectOnPlane(truck.body.position - EggSpoonCourse.Point(slot, station), Vector3.up).magnitude < 4)
            { trim = true; return Vector2.zero; }
            var eggLocal = spoon.InverseTransformPoint(egg.body.position) - BowlCenter;
            var up = Quaternion.Inverse(truck.body.rotation) * Vector3.up;
            float wantPitch = Mathf.Clamp(-eggLocal.z * 9 + up.z * Mathf.Rad2Deg, -16, 16);
            float wantRoll = Mathf.Clamp(eggLocal.x * 9 - up.x * Mathf.Rad2Deg, -18, 18);
            trim = rules.carrying[slot] && (Mathf.Abs(wantPitch - TrimPitch) > 4 || Mathf.Abs(wantRoll - TrimRoll) > 4);
            if (trim) return Vector2.ClampMagnitude(new Vector2((wantRoll - TrimRoll) / 4, (wantPitch - TrimPitch) / 4), 1);
            bool reverse = !rules.carrying[slot] && truck.body.position.z > station + 2;
            float z = reverse ? Mathf.Max(station, truck.body.position.z - 9) : Mathf.Min(EggSpoonCourse.Finish + 6, truck.body.position.z + 9);
            Vector3 delta = EggSpoonCourse.Point(slot, z) - truck.body.position; delta.y = 0;
            var forward = truck.body.rotation * (reverse ? Vector3.back : Vector3.forward);
            float angle = Vector3.SignedAngle(forward, delta, Vector3.up);
            float desired = Mathf.Abs(angle) > 20 ? 1.5f : reverse ? 2 : 3.6f;
            float signedSpeed = Vector3.Dot(truck.body.linearVelocity, forward);
            float throttle = Mathf.Clamp((desired - signedSpeed) * .4f, -.35f, .55f);
            return new Vector2(Mathf.Clamp(angle / 26, -1, 1) * (reverse ? -1 : 1), throttle * (reverse ? -1 : 1));
        }
        public void Step(Vector2 axes, bool trim, float dt)
        {
            var rules = match.rules; if (rules.finished[slot] || rules.dnf[slot]) return;
            float stationZ = EggSpoonCourse.StationZ(rules.checkpoint[slot]);
            AtStation = Vector3.ProjectOnPlane(truck.body.position - EggSpoonCourse.Point(slot, stationZ), Vector3.up).magnitude < 4;
            bool stopped = truck.body.linearVelocity.magnitude < .8f && Vector3.Dot(truck.transform.up, Vector3.up) > .85f;
            if (!rules.carrying[slot] && AtStation && stopped && trim)
            {
                ServiceSeconds += dt;
                if (rules.Replace(slot, true, true, ServiceSeconds)) { TrimPitch = TrimRoll = 0; MoveSpoon(true); egg.Place(BowlPosition + spoon.up * 1.1f, spoon.rotation, false); ServiceSeconds = 0; match.Replaced(slot); }
            }
            else ServiceSeconds = 0;
            if (trim && rules.carrying[slot]) { TrimPitch = Mathf.Clamp(TrimPitch + axes.y * 14 * dt, -16, 16); TrimRoll = Mathf.Clamp(TrimRoll + axes.x * 14 * dt, -18, 18); }
            LastDrive = trim ? Vector2.zero : new Vector2(axes.x, Mathf.Clamp(axes.y, -.65f, .65f));
            truck.TickDrive(LastDrive);
            // Use NWH's owned handbrake input. A negative accelerator/brake
            // demand could trigger automatic DNR and reverse during servicing.
            if (trim) truck.tractorMotor.vehicle.input.states.handbrake = truck.tractorMotor.vehicle.input.states.handbrakeRaw = 1;
            MoveSpoon(); egg.Observe(dt); DriveSteps++;
            var position = truck.body.position;
            int gate = rules.checkpoint[slot]; if (egg.Carried && gate < EggSpoonRules.Gates && EggSpoonCourse.Crossed(previous, position, EggSpoonCourse.Gates[gate], slot))
                if (rules.Cross(slot, gate)) match.Secured(slot, gate + 1);
            if (egg.Carried && EggSpoonCourse.Crossed(previous, position, EggSpoonCourse.Finish, slot) && rules.Finish(slot, match.Clock)) { Stop(); match.Arrived(slot); }
            inverted = Vector3.Dot(truck.transform.up, Vector3.up) < .2f ? inverted + dt : 0;
            if (inverted > 25 || position.y < -12 || Mathf.Abs(position.x) > 95) { rules.Fail(slot); Stop(); }
            previous = position;
        }
        public void Spilled() { if (match.rules.Spill(slot)) { ServiceSeconds = 0; match.Spilled(slot); } }
        public void Stop()
        {
            truck.Power(false); truck.StopSound(); if (!truck.body.isKinematic) truck.body.linearVelocity = truck.body.angularVelocity = Vector3.zero; truck.body.isKinematic = true;
            foreach (var body in originalAttachedBodies) { if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero; body.isKinematic = true; }
            spoonBody.isKinematic = true; egg.Stop();
        }
    }
}
