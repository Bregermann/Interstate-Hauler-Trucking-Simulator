using TMPro;
using UnityEngine;

namespace LWS.TruckHitchSports
{
    // An actual moving native PhysX obstacle, with an occupancy hold to avoid
    // closing through the trailer after its tractor has passed the signal.
    public sealed class HighwireMovingBarrier : MonoBehaviour
    {
        public HighwireMatch Match;
        public Rigidbody Body;
        public Vector3 ClosedPosition, OpenPosition, ApproachForward = Vector3.forward;
        public float RoadWidth = 6.5f, BarrierWidth = 7, CycleSeconds = 16, PhaseOffset;
        public TMP_Text Signal;
        public float Opening { get; private set; }
        public bool OccupancyHeld { get; private set; }
        Collider[][] rigColliders;
        public bool Blocking => Vector3.Distance(Body.position, ClosedPosition) < (RoadWidth + BarrierWidth) * .5f + .5f;

        public void ResetCycle()
        {
            Opening = 0; OccupancyHeld = false;
            // Native tractors include a separately simulated cab, and a trailer
            // root is its model origin rather than the rear-most cargo point.
            // Cache all actual shapes once per ordinary round, not every tick.
            if (Match != null)
            {
                rigColliders = new Collider[Match.trucks.Length + Match.trailers.Length][];
                int cursor = 0;
                foreach (var truck in Match.trucks) rigColliders[cursor++] = truck.GetComponentsInChildren<Collider>(true);
                foreach (var trailer in Match.trailers) rigColliders[cursor++] = trailer.GetComponentsInChildren<Collider>(true);
            }
            if (Body == null) return;
            Body.position = ClosedPosition; Body.transform.position = ClosedPosition;
            if (Signal != null) { Signal.text = "WAIT · CROSSING CLOSED"; Signal.color = new Color(1, .36f, .1f); }
        }
        public bool ShouldWait(Vector3 point)
        {
            if (!Blocking) return false;
            Vector3 delta = ClosedPosition - point;
            float ahead = Vector3.Dot(delta, ApproachForward);
            Vector3 right = Vector3.Cross(Vector3.up, ApproachForward);
            return ahead > 0 && ahead < 23 && Mathf.Abs(Vector3.Dot(delta, right)) < RoadWidth + 3;
        }
        void FixedUpdate()
        {
            if (Match == null || !Match.Live || Match.Elapsed < 3 || Body == null) return;
            float cycle = Mathf.Repeat((Match.Elapsed - 3 + PhaseOffset) / Mathf.Max(8, CycleSeconds), 1);
            float desired = cycle < .25f ? 0 : cycle < .4f ? Mathf.SmoothStep(0, 1, (cycle - .25f) / .15f) : cycle < .8f ? 1 : Mathf.SmoothStep(1, 0, (cycle - .8f) / .2f);
            OccupancyHeld = desired < Opening && Opening > .001f && RigOccupiesCrossing();
            if (OccupancyHeld) desired = Opening;
            Opening = desired;
            Body.MovePosition(Vector3.Lerp(ClosedPosition, OpenPosition, Opening));
            if (Signal != null) { Signal.text = Blocking ? "WAIT · CROSSING CLOSED" : "GO · CLEAR THE WHOLE LOAD"; Signal.color = Blocking ? new Color(1, .36f, .1f) : new Color(.45f, 1, .7f); }
        }
        bool RigOccupiesCrossing()
        {
            if (rigColliders == null) return false;
            Vector3 forward = Vector3.ProjectOnPlane(ApproachForward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            // The closing leaf crosses this finite roadway plane. Inflate only
            // by the actual leaf half-depth and a small physical clearance;
            // never assume that a root point means the full load has cleared.
            const float leafHalfDepth = .55f, clearance = .35f;
            foreach (var rig in rigColliders)
            foreach (var shape in rig)
            {
                if (shape == null || !shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy) continue;
                Bounds bounds = shape.bounds;
                Vector3 delta = bounds.center - ClosedPosition, extent = bounds.extents;
                float alongExtent = Mathf.Abs(forward.x) * extent.x + Mathf.Abs(forward.z) * extent.z;
                float sideExtent = Mathf.Abs(right.x) * extent.x + Mathf.Abs(right.z) * extent.z;
                if (Mathf.Abs(delta.y) <= extent.y + .65f + clearance &&
                    Mathf.Abs(Vector3.Dot(delta, forward)) <= alongExtent + leafHalfDepth + clearance &&
                    Mathf.Abs(Vector3.Dot(delta, right)) <= sideExtent + RoadWidth * .5f + clearance)
                    return true;
            }
            return false;
        }

    }
}
