using UnityEngine;
using LWS.TruckHitchSports;

namespace LWS.TruckExtremeHauling
{
    // Observation only: the native trailer, its wheel contact and PhysX hull
    // contacts remain authoritative. No forces or Transform writes occur here.
    public sealed class RingAerialTrickMeter
    {
        readonly Rigidbody body;
        readonly UnhitchedSurfaceContact hull;
        Quaternion previousRotation;
        Vector3 pitchAxis, yawAxis, rollAxis;
        Vector3 netDegrees;
        bool airborne;
        float phaseSeconds, bankedSeconds, bankedTrickSeconds;
        int bankedTurns;
        public bool FreeFlight { get; private set; }
        public float RejectedSupportedDegrees { get; private set; }
        public Vector3 CurrentNetDegrees => netDegrees;
        public Vector3 FlightRollAxis => rollAxis;
        public int CompletedTurns => bankedTurns + (airborne && phaseSeconds >= .15f ? PhaseTurns() : 0);
        public float FreeFlightSeconds => bankedSeconds + (airborne ? phaseSeconds : 0);
        public float TrickFlightSeconds => bankedTrickSeconds + (airborne && phaseSeconds >= .15f && PhaseTurns() > 0 ? phaseSeconds : 0);
        public RingAerialTrickMeter(Rigidbody nativeBody, UnhitchedSurfaceContact contact)
        { body = nativeBody; hull = contact; Reset(); }
        public void Reset()
        {
            previousRotation = body.rotation; netDegrees = Vector3.zero;
            airborne = FreeFlight = false; phaseSeconds = bankedSeconds = bankedTrickSeconds = RejectedSupportedDegrees = 0; bankedTurns = 0;
        }
        int PhaseTurns() => Mathf.FloorToInt(Mathf.Abs(netDegrees.x) / 360) + Mathf.FloorToInt(Mathf.Abs(netDegrees.y) / 360) + Mathf.FloorToInt(Mathf.Abs(netDegrees.z) / 360);
        public void Sample(bool nativeWheelContact, float dt)
        {
            Quaternion current = body.rotation;
            FreeFlight = !nativeWheelContact && (hull == null || !hull.Supported);
            if (!FreeFlight)
            {
                if (airborne)
                {
                    bankedSeconds += phaseSeconds;
                    if (phaseSeconds >= .15f) { int turns = PhaseTurns(); bankedTurns += turns; if (turns > 0) bankedTrickSeconds += phaseSeconds; }
                    airborne = false; phaseSeconds = 0; netDegrees = Vector3.zero;
                }
                else RejectedSupportedDegrees += Quaternion.Angle(previousRotation, current);
                previousRotation = current; return;
            }
            if (!airborne)
            {
                airborne = true; phaseSeconds = 0; netDegrees = Vector3.zero;
                pitchAxis = current * Vector3.right; yawAxis = current * Vector3.up; rollAxis = current * Vector3.forward;
                previousRotation = current;
            }
            else
            {
                Quaternion change = current * Quaternion.Inverse(previousRotation);
                change.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180) angle -= 360;
                if (Mathf.Abs(angle) > .0001f && !float.IsNaN(axis.x) && !float.IsNaN(axis.y) && !float.IsNaN(axis.z))
                {
                    Vector3 actualRotation = axis * angle;
                    // Signed integration cancels rocking back and forth. Each
                    // axis needs its own complete turn; three partial wobbles
                    // cannot masquerade as a revolution.
                    netDegrees += new Vector3(Vector3.Dot(actualRotation, pitchAxis), Vector3.Dot(actualRotation, yawAxis), Vector3.Dot(actualRotation, rollAxis));
                }
            }
            phaseSeconds += dt; previousRotation = current;
        }
        public string Diagnostic() => "freeFlight=" + FreeFlight + " turns=" + CompletedTurns + " air=" + FreeFlightSeconds.ToString("F2") + "s trickAir=" + TrickFlightSeconds.ToString("F2") + "s takeoffNetPitchYawRoll=" + netDegrees.ToString("F1") + " supportedRotationRejected=" + RejectedSupportedDegrees.ToString("F1") + "deg";
    }
}
