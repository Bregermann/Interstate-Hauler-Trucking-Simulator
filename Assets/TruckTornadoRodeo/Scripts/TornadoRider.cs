using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckTornadoRodeo
{
    public sealed class TornadoRider : MonoBehaviour
    {
        public TornadoMatch match;
        public SportsEventTruck truck;
        public int slot;
        public readonly SportsLimitedAftertouch correction = new SportsLimitedAftertouch();
        public float FlightSeconds { get; private set; }
        public float AirborneSeconds { get; private set; }
        public bool Flaring => match.rules.Clock < flareUntil && match.rules.airborne[slot];
        public int NativeDebrisContacts { get; private set; }
        public float MaximumAppliedAcceleration { get; private set; }
        public int NativeLandingSamples { get; private set; }
        public int NativeGroundImpacts { get; private set; }
        readonly bool[] nearby = new bool[32], hit = new bool[32];
        readonly float[] closest = new float[32];
        readonly float[] recentDescent = new float[5];
        int descentCursor;
        Vector3 previous, pitchAxis, rollAxis;
        Quaternion previousRotation;
        float flareUntil, nextFlare, supported, previousDownSpeed, impactDownSpeed;
        bool touchedDown, uncontrolledGroundContact;
        float nativeImpact;
        public void ResetRider()
        {
            FlightSeconds = AirborneSeconds = MaximumAppliedAcceleration = 0; NativeDebrisContacts = NativeLandingSamples = NativeGroundImpacts = 0;
            flareUntil = nextFlare = supported = previousDownSpeed = impactDownSpeed = nativeImpact = 0; touchedDown = uncontrolledGroundContact = false;
            previous = truck.body.position; previousRotation = truck.body.rotation;
            System.Array.Clear(recentDescent, 0, recentDescent.Length); descentCursor = 0;
            correction.Reset(7, Vector2.zero); ClearEncounters();
        }
        void ClearEncounters() { System.Array.Clear(nearby, 0, nearby.Length); System.Array.Clear(hit, 0, hit.Length); for (int i = 0; i < closest.Length; i++) closest[i] = float.PositiveInfinity; }
        public bool Flare()
        {
            if (!match.Live || !match.rules.Live || !match.rules.airborne[slot] || match.rules.Clock < nextFlare) return false;
            flareUntil = match.rules.Clock + 7; nextFlare = match.rules.Clock + 8; return true;
        }
        public void Step(Vector2 command, bool action, float dt)
        {
            var body = truck.body; if (match.rules.retired[slot]) return;
            float height = body.worldCenterOfMass.y - match.field.Center.y;
            if (match.rules.BeginFlight(slot, height, truck.Grounded))
            {
                FlightSeconds = 0; touchedDown = uncontrolledGroundContact = false; supported = nativeImpact = 0; correction.Reset(7, command); ClearEncounters();
                System.Array.Clear(recentDescent, 0, recentDescent.Length); descentCursor = 0;
                var forward = Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up).normalized; if (forward.sqrMagnitude < .5f) forward = Vector3.forward;
                pitchAxis = Vector3.Cross(Vector3.up, forward); rollAxis = forward; previousRotation = body.rotation; previous = body.position;
            }
            if (action) Flare();
            float response = Flaring ? .25f : 1;
            var acceleration = match.field.Acceleration(body.worldCenterOfMass, body.linearVelocity) * response;
            MaximumAppliedAcceleration = Mathf.Max(MaximumAppliedAcceleration, acceleration.magnitude); body.AddForce(acceleration, ForceMode.Acceleration);
            if (!match.rules.airborne[slot]) truck.TickDrive(command);
            else
            {
                FlightSeconds += dt; AirborneSeconds += dt; truck.TickDrive(Vector2.zero);
                float before = correction.Spent; correction.Tick(body, command, dt, 1.8f); float authority = Mathf.Clamp01((correction.Spent - before) / Mathf.Max(.0001f, dt * 1.8f));
                Vector3 control = pitchAxis * (correction.RangeArmed ? -command.y * 2 : 0) + rollAxis * (-command.x * 2);
                if (body.angularVelocity.magnitude < 7) body.AddTorque(control * authority, ForceMode.Acceleration);
                float strength = TornadoFieldModel.Strength(Vector3.ProjectOnPlane(body.position - match.field.Center, Vector3.up).magnitude);
                if (!Flaring && body.angularVelocity.magnitude < 6) body.AddTorque(new Vector3(Mathf.Sin(match.rules.Clock * 1.3f + slot) * .13f, .55f, Mathf.Cos(match.rules.Clock * .9f + slot) * .13f) * strength, ForceMode.Acceleration);
                if (Flaring)
                {
                    var radial = Vector3.ProjectOnPlane(body.position - match.field.Center, Vector3.up).normalized; if (radial.sqrMagnitude < .1f) radial = Vector3.forward;
                    var flare = radial * 9 - Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up) * .5f + Vector3.up * Mathf.Clamp(-body.linearVelocity.y * .9f, 0, 12);
                    var flareForce = Vector3.ClampMagnitude(flare, 20); body.AddForce(flareForce, ForceMode.Acceleration); MaximumAppliedAcceleration = Mathf.Max(MaximumAppliedAcceleration, (acceleration + flareForce).magnitude);
                    var upright = Quaternion.FromToRotation(truck.transform.up, Vector3.up); upright.ToAngleAxis(out float angle, out Vector3 axis); if (angle > 180) angle -= 360;
                    if (float.IsFinite(axis.x)) body.AddTorque(Vector3.ClampMagnitude(axis * angle * .08f - body.angularVelocity * 2, 5), ForceMode.Acceleration);
                }
                int delta = match.rules.ObserveHeight(slot, height); if (delta > 0) match.Award(slot, delta, "ALTITUDE", SportsScoreTier.Small);
                var rotation = body.rotation * Quaternion.Inverse(previousRotation); rotation.ToAngleAxis(out float degrees, out Vector3 turnAxis); if (degrees > 180) degrees -= 360;
                if (float.IsFinite(turnAxis.x)) { delta = match.rules.ObserveRotation(slot, Vector3.Dot(turnAxis * degrees, pitchAxis), Vector3.Dot(turnAxis * degrees, rollAxis), turnAxis.y * degrees); if (delta > 0) match.Award(slot, delta, "ACTUAL FULL ROTATION", SportsScoreTier.Major); }
                foreach (var ring in match.rings) if (ring.WholeTruckPassed(truck, previous, previousRotation, body.position, body.rotation)) { delta = match.rules.PassRing(slot, match.rules.flights[slot], ring.id); if (delta > 0) { match.Award(slot, delta, "AERIAL RING", SportsScoreTier.Major); match.presentation?.Ring(ring.transform.position); } }
                recentDescent[descentCursor++ % recentDescent.Length] = Mathf.Max(0, -body.linearVelocity.y);
                ObserveDebris(); ObserveLanding(dt);
            }
            previousDownSpeed = Mathf.Max(0, -body.linearVelocity.y); previous = body.position; previousRotation = body.rotation;
            if (body.position.y < -25 || Vector3.ProjectOnPlane(body.position - match.field.Center, Vector3.up).magnitude > 260)
            {
                match.rules.Retire(slot); truck.Power(false); body.isKinematic = true; match.Retired(slot);
            }
        }
        void ObserveLanding(float dt)
        {
            var body = truck.body;
            if (!truck.Grounded) { supported = 0; touchedDown = false; return; }
            NativeLandingSamples++;
            if (!touchedDown) { impactDownSpeed = previousDownSpeed; foreach (float speed in recentDescent) impactDownSpeed = Mathf.Max(impactDownSpeed, speed); touchedDown = true; }
            supported += dt;
            if (supported < .8f) return;
            int flight = match.rules.flights[slot]; int delta = match.rules.Land(slot, flight, true, uncontrolledGroundContact ? 0 : Vector3.Dot(truck.transform.up, Vector3.up), Mathf.Max(impactDownSpeed, nativeImpact), Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude, supported);
            if (delta > 0) { match.Award(slot, delta, "CONTROLLED NATIVE LANDING", SportsScoreTier.Major); match.presentation?.Landing(body.position); }
            FlightSeconds = 0; flareUntil = 0; supported = 0;
        }
        void ObserveDebris()
        {
            foreach (var debris in match.debris)
            {
                int id = debris.id; if (id < 0 || id >= nearby.Length || hit[id]) continue;
                float separation = Vector3.Distance(truck.shape.ClosestPoint(debris.body.worldCenterOfMass), debris.body.worldCenterOfMass) - debris.hazardRadius;
                float relativeSpeed = (debris.body.linearVelocity - truck.body.linearVelocity).magnitude;
                if (separation < 6 && relativeSpeed > 3) { nearby[id] = true; closest[id] = Mathf.Min(closest[id], separation); }
                if (nearby[id] && separation > closest[id] + 2 && closest[id] > .4f)
                {
                    int delta = match.rules.Dodge(slot, match.rules.flights[slot], id); nearby[id] = false;
                    if (delta > 0) match.Award(slot, delta, "PHYSICAL DEBRIS AVOIDED", SportsScoreTier.Small);
                }
            }
        }
        public void DebrisContact(int id, float speed, Vector3 point)
        {
            if (!match.Live || !match.rules.Live || id < 0 || id >= hit.Length) return;
            hit[id] = true; nearby[id] = false; NativeDebrisContacts++;
            if (speed >= 2) { match.feedback.PlayAt(2, point, speed); match.Pulse(slot, Mathf.Clamp01(speed / 25) * .4f, .15f); match.presentation?.Contact(point); }
        }
        void OnCollisionEnter(Collision collision)
        {
            if (match == null || !match.Live || !match.rules.airborne[slot] || collision.collider != match.ground) return;
            NativeGroundImpacts++; nativeImpact = Mathf.Max(nativeImpact, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, Vector3.up)));
            uncontrolledGroundContact |= nativeImpact > 12 || Vector3.Dot(transform.up, Vector3.up) < .6f;
        }
    }
}
