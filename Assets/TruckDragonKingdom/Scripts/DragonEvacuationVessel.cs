using System;
using System.Collections.Generic;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // Mode-owned secure/boarding semantics over an authored solid moving hull.
    // A proximity trigger never counts as extraction. The truck must drive onto
    // the deck, settle on its NWH wheels, secure, and remain aboard at departure.
    [DefaultExecutionOrder(100)]
    public sealed class DragonEvacuationVessel : MonoBehaviour, IDragonWorldResettable
    {
        public Rigidbody hull;
        public BoxCollider parkingZone;
        public Collider[] supportingDeck = Array.Empty<Collider>();
        public Transform[] departureWaypoints = Array.Empty<Transform>();
        [Min(0)] public float departureNoticeSeconds = 8;
        [Min(.1f)] public float sailingSpeed = 4;
        [Min(.1f)] public float acceleration = .8f;
        [Min(0)] public float turningDegreesPerSecond = 8;
        public bool BoardingOpen { get; private set; } = true;
        public bool DepartureRequested { get; private set; }
        public bool Sailing { get; private set; }
        public bool Paused { get; private set; }
        public float SecondsUntilDeparture { get; private set; }
        public float DistanceSailed { get; private set; }
        public int SecuredCount => secured.Count;
        public event Action<SportsEventTruck> TruckSecured;
        public event Action Departing;

        readonly Dictionary<SportsEventTruck, float> dwell = new Dictionary<SportsEventTruck, float>();
        readonly Dictionary<SportsEventTruck, DragonTruckCarryBinding> secured = new Dictionary<SportsEventTruck, DragonTruckCarryBinding>();
        readonly HashSet<Collider> deck = new HashSet<Collider>();
        Vector3 dockPosition, lastPosition;
        Quaternion dockRotation;
        Vector3[] path;
        int target;
        float speed;
        bool initialized;

        void Awake() => Initialize();
        void Initialize()
        {
            if (initialized) return;
            if (hull == null || parkingZone == null || supportingDeck.Length == 0 || departureWaypoints.Length == 0)
                throw new InvalidOperationException("Evacuation vessel requires its hull, solid supporting deck, parking zone and real departure route.");
            if (!hull.isKinematic) throw new InvalidOperationException("Evacuation hull must use the authored kinematic navigation adapter.");
            if ((hull.transform.lossyScale - Vector3.one).sqrMagnitude > .0001f)
                throw new InvalidOperationException("Evacuation hull must have unit world scale; size its authored deck modules instead.");
            foreach (var collider in supportingDeck)
            {
                if (collider == null || collider.isTrigger || collider.attachedRigidbody != hull)
                    throw new InvalidOperationException("Every boarding deck surface must be solid and belong to the vessel hull.");
                deck.Add(collider);
            }
            path = new Vector3[departureWaypoints.Length];
            for (int i = 0; i < path.Length; i++)
            {
                if (departureWaypoints[i] == null || departureWaypoints[i].IsChildOf(hull.transform))
                    throw new InvalidOperationException("Vessel departure waypoints must remain in the world, outside the moving hull.");
                path[i] = departureWaypoints[i].position;
            }
            dockPosition = lastPosition = hull.position; dockRotation = hull.rotation;
            initialized = true;
        }

        public bool IsSecured(SportsEventTruck truck) => truck != null && secured.ContainsKey(truck);
        public bool ReleaseTruck(SportsEventTruck truck, bool reset = false)
        {
            if (truck == null || !secured.TryGetValue(truck, out var binding)) return false;
            binding.Release(truck.body != null ? hull.GetPointVelocity(truck.body.worldCenterOfMass) : Vector3.zero, hull.angularVelocity, reset);
            secured.Remove(truck); dwell.Remove(truck); return true;
        }

        public bool ObserveBoarding(SportsEventTruck truck, float deltaTime, float requiredSeconds, float maximumRelativeSpeed)
        {
            Initialize();
            if (truck == null || truck.body == null) return false;
            if (IsSecured(truck)) return true;
            if (!BoardingOpen || !PhysicallyParked(truck, maximumRelativeSpeed))
            { dwell.Remove(truck); return false; }
            dwell.TryGetValue(truck, out float elapsed);
            elapsed += Mathf.Clamp(deltaTime, 0, .1f); dwell[truck] = elapsed;
            if (elapsed < Mathf.Max(.1f, requiredSeconds)) return false;
            Secure(truck); return true;
        }

        // A driving host may brake on valid supporting geometry before the
        // strict velocity/dwell gate is satisfied. Position alone cannot hold
        // a truck straddling the ramp or the edge of the deck indefinitely.
        public bool HasSupportedParkingFootprint(SportsEventTruck truck)
        {
            if (truck == null || truck.body == null || truck.shape == null || !truck.shape.enabled || truck.body.isKinematic || truck.wheels == null
                || Vector3.Dot(truck.transform.up, hull.transform.up) < .85f) return false;
            var b = truck.shape.bounds;
            var half = parkingZone.size * .5f;
            // Check the entire truck footprint, not just its centre or trigger.
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var p = parkingZone.transform.InverseTransformPoint(new Vector3(b.center.x + b.extents.x * x, b.center.y, b.center.z + b.extents.z * z)) - parkingZone.center;
                if (Mathf.Abs(p.x) > half.x || Mathf.Abs(p.y) > half.y || Mathf.Abs(p.z) > half.z) return false;
            }
            int contacts = 0;
            foreach (var wheel in truck.wheels)
                if (wheel != null && wheel.enabled && wheel.IsGrounded && deck.Contains(wheel.HitCollider)) contacts++;
            if (contacts < Mathf.Min(3, truck.wheels.Length) || contacts == 0) return false;
            return true;
        }

        bool PhysicallyParked(SportsEventTruck truck, float maximumRelativeSpeed)
        {
            return HasSupportedParkingFootprint(truck)
                && (truck.body.linearVelocity - hull.GetPointVelocity(truck.body.worldCenterOfMass)).magnitude <= Mathf.Max(.05f, maximumRelativeSpeed)
                && truck.body.angularVelocity.magnitude < .35f;
        }

        void Secure(SportsEventTruck truck)
        {
            var binding = new DragonTruckCarryBinding(truck, new Pose(hull.position, hull.rotation));
            secured.Add(truck, binding); dwell.Remove(truck); TruckSecured?.Invoke(truck);
        }

        // Match host decides when all required crews have boarded or a clearly
        // announced competitive departure window should begin. No auto-award.
        public bool RequestDeparture()
        {
            Initialize();
            if (DepartureRequested || secured.Count == 0) return false;
            DepartureRequested = true; SecondsUntilDeparture = Mathf.Max(1, departureNoticeSeconds); return true;
        }
        public void SetPaused(bool paused) => Paused = paused;
        public bool HasExtracted(SportsEventTruck truck, float minimumDepartureDistance)
        {
            if (!Sailing || truck == null || truck.body == null || !secured.TryGetValue(truck, out var binding)
                || DistanceSailed < Mathf.Max(1, minimumDepartureDistance)
                || Vector3.Distance(hull.position, dockPosition) < Mathf.Max(1, minimumDepartureDistance)) return false;
            return binding.RemainsBound(new Pose(hull.position, hull.rotation));
        }

        void FixedUpdate()
        {
            if (!initialized || Paused) return;
            DistanceSailed += Sailing ? Vector3.Distance(lastPosition, hull.position) : 0;
            lastPosition = hull.position;
            if (DepartureRequested && !Sailing)
            {
                SecondsUntilDeparture = Mathf.Max(0, SecondsUntilDeparture - Time.fixedDeltaTime);
                if (SecondsUntilDeparture <= 0) { BoardingOpen = false; Sailing = true; dwell.Clear(); Departing?.Invoke(); }
            }
            if (!Sailing || target >= path.Length) return;
            var delta = path[target] - hull.position;
            if (delta.magnitude < .25f) { target++; speed = 0; return; }
            speed = Mathf.MoveTowards(speed, sailingSpeed, acceleration * Time.fixedDeltaTime);
            var nextPosition = Vector3.MoveTowards(hull.position, path[target], speed * Time.fixedDeltaTime);
            var horizontal = Vector3.ProjectOnPlane(delta, Vector3.up);
            var desiredRotation = horizontal.sqrMagnitude > .01f ? Quaternion.LookRotation(horizontal, Vector3.up) : hull.rotation;
            var nextRotation = Quaternion.RotateTowards(hull.rotation, desiredRotation, turningDegreesPerSecond * Time.fixedDeltaTime);
            hull.MovePosition(nextPosition); hull.MoveRotation(nextRotation);
            foreach (var binding in secured.Values)
                binding.Follow(new Pose(nextPosition, nextRotation));
        }

        void ReleaseAll(bool resetting, bool restorePower = true)
        {
            foreach (var binding in secured.Values)
            {
                var velocity = binding.Truck != null && binding.Truck.body != null ? hull.GetPointVelocity(binding.Truck.body.worldCenterOfMass) : Vector3.zero;
                if (restorePower) binding.Release(velocity, hull.angularVelocity, resetting);
                else binding.ReleaseForTeardown(velocity, hull.angularVelocity, resetting);
            }
            secured.Clear(); dwell.Clear();
        }
        public void ResetDragonMatch(int seed)
        {
            Initialize(); ReleaseAll(true);
            hull.position = lastPosition = dockPosition; hull.rotation = dockRotation;
            target = 0; speed = DistanceSailed = SecondsUntilDeparture = 0;
            BoardingOpen = true; Paused = Sailing = DepartureRequested = false;
        }
        void OnDisable() { if (initialized) ReleaseAll(false, false); }
    }
}
