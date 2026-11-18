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
        public Collider[] LeafColliders = System.Array.Empty<Collider>();
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
            // Existing saved gates predate explicit leaf references. Cache all
            // solid compound shapes; installed art is never a collision proxy.
            if (LeafColliders == null || LeafColliders.Length == 0) LeafColliders = Body.GetComponentsInChildren<Collider>(true);
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
            // SmoothStep's maximum derivative is1.5. Resume from the current
            // held position at the authored cycle's normal mechanical rate;
            // assigning the old absolute cycle phase after a hold could sweep
            // nearly15m through the road in a single physics step.
            float phaseSeconds = Mathf.Max(8, CycleSeconds) * (desired < Opening ? .2f : .15f);
            float requested = Mathf.MoveTowards(Opening, desired, 1.5f / phaseSeconds * Time.fixedDeltaTime);
            OccupancyHeld = requested < Opening && Opening > .001f && RigOccupiesClosingSweep();
            if (OccupancyHeld) requested = Opening;
            Opening = requested;
            Body.MovePosition(Vector3.Lerp(ClosedPosition, OpenPosition, Opening));
            if (Signal != null) { Signal.text = Blocking ? "WAIT · CROSSING CLOSED" : "GO · CLEAR THE WHOLE LOAD"; Signal.color = Blocking ? new Color(1, .36f, .1f) : new Color(.45f, 1, .7f); }
        }
        bool RigOccupiesClosingSweep()
        {
            if (rigColliders == null || LeafColliders == null) return false;
            Vector3 remainingTravel = ClosedPosition - Body.position;
            const float clearance = .35f;
            foreach (var leaf in LeafColliders)
            {
                if (leaf == null || !leaf.enabled || leaf.isTrigger || !leaf.gameObject.activeInHierarchy || leaf.attachedRigidbody != Body) continue;
                // Translation-only kinematic leaf: the union is its complete
                // actual closing envelope, including offset/compound shapes.
                // A theoretical plane at the root misses such real geometry.
                Bounds sweptLeaf = leaf.bounds;
                var closedLeaf = sweptLeaf; closedLeaf.center += remainingTravel;
                sweptLeaf.Encapsulate(closedLeaf); sweptLeaf.Expand(clearance * 2);
                foreach (var rig in rigColliders)
                foreach (var shape in rig)
                {
                    if (shape == null || !shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy) continue;
                    Bounds sweptRig = shape.bounds;
                    if (shape.attachedRigidbody != null)
                    {
                        var next = sweptRig;
                        next.center += shape.attachedRigidbody.GetPointVelocity(next.center) * Time.fixedDeltaTime;
                        sweptRig.Encapsulate(next);
                    }
                    if (sweptLeaf.Intersects(sweptRig)) return true;
                }
            }
            return false;
        }

    }
}
