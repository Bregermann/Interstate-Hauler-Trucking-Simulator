using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public static class TruckTaxiVehicleObjectives
    {
        public static bool IsVehicleRequest(TaxiRequestType type) =>
            type >= TaxiRequestType.FollowVehicle && type <= TaxiRequestType.CollectDroppedObjects;

        public static bool QualifiesImpact(TaxiEventType type, string observedId, string assignedId,
            float speed, float minimumSpeed) =>
            type == TaxiEventType.TrafficRam && !string.IsNullOrEmpty(assignedId) &&
            observedId == assignedId && float.IsFinite(speed) && speed >= minimumSpeed;

        public static bool InFollowBand(float distance, float playerSpeed, float targetSpeed,
            float minimumDistance, float maximumDistance) =>
            distance >= minimumDistance && distance <= maximumDistance &&
            playerSpeed >= 1 && targetSpeed >= 1;

        public static bool IsBlocking(Vector3 targetPosition, Vector3 targetForward, float targetSpeed,
            Vector3 tractorPosition, float tractorSpeed, float previouslyMovingSpeed)
        {
            Vector3 forward = Vector3.ProjectOnPlane(targetForward, Vector3.up).normalized;
            Vector3 offset = Vector3.ProjectOnPlane(tractorPosition - targetPosition, Vector3.up);
            float ahead = Vector3.Dot(offset, forward);
            float lateral = Vector3.Cross(forward, offset).magnitude;
            return ahead >= 1 && ahead <= 13 && lateral <= 3.5f &&
                targetSpeed <= 0.8f && tractorSpeed <= 2 && previouslyMovingSpeed >= 2;
        }
    }

    // This component exists only on a vehicle assigned to a mission, never on ordinary traffic.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiMissionTarget : MonoBehaviour
    {
        public string StableId { get; private set; }
        public int QualifiedHits { get; private set; }
        public int RequiredHits { get; private set; }
        public void Assign(string stableId, int requiredHits)
        {
            StableId = stableId;
            RequiredHits = Mathf.Max(2, requiredHits);
            QualifiedHits = 0;
        }
        public bool ApplyQualifiedHit()
        {
            if (string.IsNullOrEmpty(StableId) || QualifiedHits >= RequiredHits) return false;
            QualifiedHits++;
            return QualifiedHits >= RequiredHits;
        }
        public void Clear()
        {
            StableId = null;
            QualifiedHits = 0;
            RequiredHits = 0;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiDroppedCollectible : MonoBehaviour
    {
        private TruckTaxiVehicleObjectiveCoordinator owner;
        private TaxiRequestProgress request;
        private TruckTaxiBootstrap host;
        private float expiresAt;
        private bool collected;
        public bool Collected => collected;
        public void Initialize(TruckTaxiVehicleObjectiveCoordinator coordinator, TruckTaxiBootstrap bootstrap,
            TaxiRequestProgress progress, float lifetime)
        {
            owner = coordinator;
            host = bootstrap;
            request = progress;
            expiresAt = Time.time + lifetime;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (collected || owner == null || host?.Player == null || other == null) return;
            var tractor = host.Player.transform;
            if (other.attachedRigidbody?.transform != tractor && !other.transform.IsChildOf(tractor)) return;
            collected = true;
            owner.Collect(request, this);
        }
        private void Update()
        {
            if (!collected && Time.time >= expiresAt)
            {
                collected = true;
                owner?.Expire(request, this);
            }
        }
    }

    [DefaultExecutionOrder(500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TruckTaxiBootstrap))]
    public sealed class TruckTaxiVehicleObjectiveCoordinator : MonoBehaviour
    {
        private sealed class Assignment
        {
            public TaxiRequestProgress request;
            public string vehicleId;
            public int serial;
            public GameObject vehicle;
            public Rigidbody vehicleBody;
            public Behaviour ai;
            public TruckTaxiMissionTarget missionTarget;
            public TruckTaxiMapMarker marker;
            public TruckTaxiMapMarker goalMarker;
            public Vector3 raceGoal;
            public readonly List<TruckTaxiDroppedCollectible> drops = new List<TruckTaxiDroppedCollectible>();
            public float previousTargetSpeed;
            public float lastMovingAt;
            public float blockedSeconds;
            public float lastImpactTime = -1;
        }

        [Min(3)] public float followMinimumDistance = 8;
        [Min(5)] public float followMaximumDistance = 35;
        [Min(0.1f)] public float blockHoldSeconds = 2;
        [Min(5)] public float collectibleLifetimeSeconds = 90;
        private readonly Dictionary<TaxiRequestProgress, Assignment> assignments = new Dictionary<TaxiRequestProgress, Assignment>();
        private readonly HashSet<string> reservedIds = new HashSet<string>();
        private readonly Dictionary<string, TruckTaxiRideLocation> raceLocations =
            new Dictionary<string, TruckTaxiRideLocation>(System.StringComparer.Ordinal);
        private readonly List<Assignment> expired = new List<Assignment>();
        private readonly List<Assignment> snapshot = new List<Assignment>();
        private readonly RaycastHit[] blockHits = new RaycastHit[16];
        private TruckTaxiBootstrap host;
        private TruckTaxiCollisionObserver collisionObserver;
        private Rigidbody tractorBody;
        private TruckTaxiMapMarkers markers;
        private System.Action previousRefresh;
        private int missionSerial;
        private readonly TruckTaxiVehicleObjectivesUtsRoute utsRoute =
            new TruckTaxiVehicleObjectivesUtsRoute();

        public string Status(TaxiRequestProgress request)
        {
            if (request == null || !assignments.TryGetValue(request, out var assignment))
                return request?.Description;
            string progress = request.ProgressText;
            if (request.Definition.requestType == TaxiRequestType.FollowVehicle)
                progress = request.Progress.ToString("0.0") + "/" +
                    request.Target.ToString("0") + "s";
            else if (request.Definition.requestType == TaxiRequestType.BlockVehicle)
                progress = assignment.blockedSeconds.ToString("0.0") + "/" +
                    blockHoldSeconds.ToString("0.0") + "s";
            else if (request.Definition.requestType == TaxiRequestType.DestroyVehicle &&
                assignment.missionTarget != null)
                progress = assignment.missionTarget.QualifiedHits + "/" +
                    assignment.missionTarget.RequiredHits + " impacts";
            string result = request.Description + " [" + assignment.vehicleId + "] " + progress;
            if (request.Definition.requestType == TaxiRequestType.ReachLocationBeforeVehicle)
                result += " (" + Mathf.RoundToInt(Vector3.ProjectOnPlane(
                    assignment.raceGoal - tractorBody.position, Vector3.up).magnitude) + "m to goal)";
            return result;
        }

        public void Initialize(TruckTaxiBootstrap bootstrap)
        {
            if (host != null || bootstrap?.Session == null || bootstrap.Player == null ||
                bootstrap.traffic == null) return;
            host = bootstrap;
            tractorBody = host.Player.GetComponent<Rigidbody>();
            collisionObserver = host.Player.GetComponent<TruckTaxiCollisionObserver>();
            markers = host.GPS?.MapMarkers;
            foreach (var location in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
                if (location.gameObject.scene == host.gameObject.scene &&
                    location.isActiveAndEnabled && !string.IsNullOrWhiteSpace(location.locationId) &&
                    !raceLocations.ContainsKey(location.locationId))
                    raceLocations.Add(location.locationId, location);
            previousRefresh = host.Session.RefreshObjectiveSupport;
            host.Session.RefreshObjectiveSupport = RefreshSupport;
            host.Session.CanResolveSpecificTarget = CanResolveSpecificTarget;
            host.Session.RequestCreated += OnRequestCreated;
            host.Session.RequestResolved += OnRequestResolved;
            host.Session.DrivingEvent += OnDrivingEvent;
            RefreshSupport();
        }

        private void OnDestroy()
        {
            if (host?.Session == null) return;
            host.Session.RequestCreated -= OnRequestCreated;
            host.Session.RequestResolved -= OnRequestResolved;
            host.Session.DrivingEvent -= OnDrivingEvent;
            if (host.Session.CanResolveSpecificTarget == CanResolveSpecificTarget)
                host.Session.CanResolveSpecificTarget = null;
            if (host.Session.RefreshObjectiveSupport == RefreshSupport)
                host.Session.RefreshObjectiveSupport = previousRefresh;
            foreach (var assignment in assignments.Values) Cleanup(assignment);
            assignments.Clear();
        }

        private void RefreshSupport()
        {
            previousRefresh?.Invoke();
            int eligible = 0;
            int raceEligible = 0;
            bool hasTargetIcon = markers?.Registry?.Find(TruckTaxiMapMarkerType.TargetVehicle)?.icon != null;
            if (host?.traffic?.Ready == true && tractorBody != null &&
                collisionObserver != null && hasTargetIcon)
                foreach (var pair in host.traffic.ActiveVehicles)
                    if (Usable(pair.Key, pair.Value))
                    {
                        eligible++;
                        if (TryRaceGoal(null, pair.Key, out _)) raceEligible++;
                    }
            var caps = host.Session.Capabilities;
            caps.Register(TruckTaxiObjectiveCapability.TargetVehicles, eligible);
            caps.Register(TruckTaxiObjectiveCapability.VehicleDestruction, eligible);
            bool hasCollectibleIcon = markers?.Registry?.Find(TruckTaxiMapMarkerType.Collectible)?.icon != null;
            caps.Register(TruckTaxiObjectiveCapability.Collectibles, eligible > 0 && hasCollectibleIcon ? eligible : 0);
            // A faster car on a fixed loop is not player-following pursuit.
            caps.Register(TruckTaxiObjectiveCapability.Pursuit, 0);
            caps.Register(TruckTaxiObjectiveCapability.VehicleRaceRoute, raceEligible);
        }

        private bool Usable(string id, GameObject vehicle) =>
            !string.IsNullOrEmpty(id) && vehicle != null && vehicle.activeInHierarchy &&
            !reservedIds.Contains(id) && vehicle.GetComponent<TruckTaxiImpactTarget>()?.targetId == id &&
            vehicle.GetComponent<Rigidbody>() != null &&
            host.traffic.TryGetVehicleAi(id, out var ai) && ai is Behaviour behaviour && behaviour.isActiveAndEnabled;

        private bool CanResolveSpecificTarget(PassengerRequestDefinition definition)
        {
            if (definition == null || !TruckTaxiVehicleObjectives.IsVehicleRequest(definition.requestType)) return false;
            return host.traffic.TryResolveVehicle(definition.targetId, out var vehicle) &&
                Usable(definition.targetId, vehicle) &&
                definition.requestType != TaxiRequestType.LoseVehicle &&
                (definition.requestType != TaxiRequestType.ReachLocationBeforeVehicle ||
                    TryRaceGoal(definition, definition.targetId, out _));
        }

        private bool TryRaceGoal(PassengerRequestDefinition definition, string vehicleId,
            out Vector3 goal)
        {
            goal = default;
            if (host?.RouteDistances == null ||
                markers?.Registry?.Find(TruckTaxiMapMarkerType.ActiveObjective)?.icon == null ||
                !host.traffic.TryGetVehicleAi(vehicleId, out var ai)) return false;
            Vector3? required = null;
            if (!string.IsNullOrWhiteSpace(definition?.goalLocationId))
            {
                if (!raceLocations.TryGetValue(definition.goalLocationId, out var location))
                    return false;
                required = location.StopPosition;
            }
            return utsRoute.TryUpcomingGoal(ai, tractorBody.position,
                host.RouteDistances, required, out goal);
        }

        private void OnRequestCreated(TaxiRequestProgress request)
        {
            if (!TruckTaxiVehicleObjectives.IsVehicleRequest(request.Definition.requestType)) return;
            GameObject selected = null;
            string id = request.Definition.targetId;
            Vector3 raceGoal = default;
            bool race = request.Definition.requestType == TaxiRequestType.ReachLocationBeforeVehicle;
            if (!string.IsNullOrEmpty(id))
            {
                host.traffic.TryResolveVehicle(id, out selected);
                if (!Usable(id, selected) ||
                    (race && !TryRaceGoal(request.Definition, id, out raceGoal))) selected = null;
            }
            else
            {
                float nearest = float.MaxValue;
                foreach (var pair in host.traffic.ActiveVehicles)
                {
                    if (!Usable(pair.Key, pair.Value)) continue;
                    Vector3 candidateGoal = default;
                    if (race && !TryRaceGoal(request.Definition, pair.Key, out candidateGoal))
                        continue;
                    float distance = (pair.Value.transform.position - tractorBody.position).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance; id = pair.Key; selected = pair.Value;
                    if (race) raceGoal = candidateGoal;
                }
            }
            if (selected == null)
            {
                host.Session.FailObjective(request, "Assigned vehicle is unavailable.");
                return;
            }
            host.traffic.TryGetVehicleAi(id, out var ai);
            var assignment = new Assignment {
                request = request, vehicleId = id, serial = ++missionSerial, vehicle = selected,
                vehicleBody = selected.GetComponent<Rigidbody>(), ai = ai as Behaviour,
                raceGoal = raceGoal
            };
            reservedIds.Add(id);
            assignments.Add(request, assignment);
            if (request.Definition.requestType == TaxiRequestType.DestroyVehicle)
            {
                assignment.missionTarget = selected.GetComponent<TruckTaxiMissionTarget>() ??
                    selected.AddComponent<TruckTaxiMissionTarget>();
                assignment.missionTarget.Assign(id, Mathf.Max(2, Mathf.CeilToInt(request.Target)));
            }
            AddTargetMarker(assignment);
            if (race) AddRaceGoalMarker(assignment);
            if (request.Definition.requestType == TaxiRequestType.CollectDroppedObjects)
            {
                if (request.Target > 16)
                    host.Session.FailObjective(request, "Too many drops requested for a bounded mission.");
                else SpawnDrops(assignment, Mathf.Max(1, Mathf.CeilToInt(request.Target)));
            }
        }

        private void AddTargetMarker(Assignment assignment)
        {
            if (markers == null) return;
            var root = new GameObject("Mission target marker");
            root.transform.SetParent(transform, false);
            var marker = root.AddComponent<TruckTaxiMapMarker>();
            marker.stableId = "taxi.mission." + assignment.serial + "." + assignment.vehicleId;
            marker.label = "TARGET " + assignment.vehicleId;
            marker.markerType = TruckTaxiMapMarkerType.TargetVehicle;
            marker.state = TruckTaxiMapMarkerState.Active;
            marker.source = assignment.vehicle.transform;
            if (!markers.Register(marker)) Destroy(root);
            else assignment.marker = marker;
        }

        private void AddRaceGoalMarker(Assignment assignment)
        {
            if (markers == null) return;
            var root = new GameObject("Shared race goal");
            root.transform.SetParent(transform, false);
            root.transform.position = assignment.raceGoal;
            var marker = root.AddComponent<TruckTaxiMapMarker>();
            marker.stableId = "taxi.race." + assignment.serial;
            marker.label = "RACE GOAL";
            marker.markerType = TruckTaxiMapMarkerType.ActiveObjective;
            marker.state = TruckTaxiMapMarkerState.Active;
            if (!markers.Register(marker)) Destroy(root);
            else assignment.goalMarker = marker;
        }

        private void SpawnDrops(Assignment assignment, int count)
        {
            Vector3 origin = assignment.vehicle.transform.position;
            for (int i = 0; i < count; i++)
            {
                var drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                drop.name = "Mission drop " + (i + 1);
                drop.transform.position = origin + Vector3.up * 2 +
                    assignment.vehicle.transform.right * ((i % 3 - 1) * 2.5f) -
                    assignment.vehicle.transform.forward * (3 + i * 2);
                drop.transform.localScale = Vector3.one * 0.8f;
                var body = drop.AddComponent<Rigidbody>();
                body.mass = 0.6f;
                body.AddForce((assignment.vehicle.transform.right * (i % 2 == 0 ? -1 : 1) +
                    Vector3.up) * 2, ForceMode.Impulse);
                var pickup = new GameObject("Tractor collection trigger");
                pickup.transform.SetParent(drop.transform, false);
                var trigger = pickup.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 3;
                var collectible = pickup.AddComponent<TruckTaxiDroppedCollectible>();
                collectible.Initialize(this, host, assignment.request, Mathf.Min(collectibleLifetimeSeconds,
                    assignment.request.Definition.timer));
                assignment.drops.Add(collectible);
                var marker = drop.AddComponent<TruckTaxiMapMarker>();
                marker.stableId = "taxi.drop." + assignment.serial + "." + i;
                marker.label = "DROPPED OBJECT " + (i + 1);
                marker.markerType = TruckTaxiMapMarkerType.Collectible;
                marker.state = TruckTaxiMapMarkerState.Active;
                marker.source = drop.transform;
                markers?.Register(marker);
            }
        }

        public void Collect(TaxiRequestProgress request, TruckTaxiDroppedCollectible collectible)
        {
            if (!assignments.TryGetValue(request, out var assignment) ||
                !assignment.drops.Contains(collectible)) return;
            host.Session.RecordObjectiveProgress(request, 1);
            Destroy(collectible.transform.parent.gameObject);
        }

        public void Expire(TaxiRequestProgress request, TruckTaxiDroppedCollectible collectible)
        {
            if (!assignments.TryGetValue(request, out var assignment) ||
                !assignment.drops.Contains(collectible)) return;
            host.Session.FailObjective(request, "Dropped object expired.");
            Destroy(collectible.transform.parent.gameObject);
        }

        private void OnDrivingEvent(TaxiEventType type)
        {
            if (type != TaxiEventType.TrafficRam || collisionObserver == null ||
                Time.time - collisionObserver.LastCollisionTime > Time.fixedDeltaTime + 0.01f) return;
            snapshot.Clear();
            snapshot.AddRange(assignments.Values);
            foreach (var assignment in snapshot)
            {
                var request = assignment.request;
                if (request.State != TaxiRequestState.Active ||
                    (request.Definition.requestType != TaxiRequestType.RamTargetVehicle &&
                     request.Definition.requestType != TaxiRequestType.DestroyVehicle) ||
                    assignment.lastImpactTime == collisionObserver.LastCollisionTime ||
                    !TruckTaxiVehicleObjectives.QualifiesImpact(type, collisionObserver.LastTarget,
                        assignment.vehicleId, collisionObserver.LastImpactSpeed,
                        host.Configuration.minimumImpactSpeed)) continue;
                assignment.lastImpactTime = collisionObserver.LastCollisionTime;
                if (request.Definition.requestType == TaxiRequestType.RamTargetVehicle)
                    host.Session.RecordObjectiveProgress(request, 1, true);
                else if (assignment.missionTarget != null)
                {
                    bool destroyed = assignment.missionTarget.ApplyQualifiedHit();
                    if (host.Session.RecordObjectiveProgress(request,
                        request.Target / assignment.missionTarget.RequiredHits, destroyed) &&
                        destroyed && assignment.vehicle != null)
                        Destroy(assignment.vehicle);
                }
            }
        }

        private void Update()
        {
            if (host == null || host.Paused || !host.Session.HasPassenger) return;
            expired.Clear();
            snapshot.Clear();
            snapshot.AddRange(assignments.Values);
            foreach (var assignment in snapshot)
            {
                if (assignment.request.State != TaxiRequestState.Active) continue;
                if (!host.traffic.TryResolveVehicle(assignment.vehicleId, out var current) ||
                    current != assignment.vehicle || assignment.ai == null ||
                    !assignment.ai.isActiveAndEnabled)
                {
                    expired.Add(assignment);
                    continue;
                }
                float targetSpeed = assignment.vehicleBody != null ?
                    assignment.vehicleBody.linearVelocity.magnitude : 0;
                float playerSpeed = tractorBody.linearVelocity.magnitude;
                if (assignment.request.Definition.requestType == TaxiRequestType.FollowVehicle)
                {
                    float distance = Vector3.ProjectOnPlane(assignment.vehicle.transform.position -
                        tractorBody.position, Vector3.up).magnitude;
                    if (TruckTaxiVehicleObjectives.InFollowBand(distance, playerSpeed, targetSpeed,
                        followMinimumDistance, followMaximumDistance))
                        host.Session.RecordObjectiveProgress(assignment.request, Time.deltaTime);
                }
                else if (assignment.request.Definition.requestType == TaxiRequestType.BlockVehicle)
                {
                    if (targetSpeed >= 2) assignment.lastMovingAt = Time.time;
                    if (TruckTaxiVehicleObjectives.IsBlocking(assignment.vehicle.transform.position,
                        assignment.vehicle.transform.forward, targetSpeed, tractorBody.position,
                        playerSpeed, assignment.previousTargetSpeed) &&
                        Time.time - assignment.lastMovingAt <= 3 && TractorObstructs(assignment))
                        assignment.blockedSeconds += Time.deltaTime;
                    else assignment.blockedSeconds = 0;
                    if (assignment.blockedSeconds >= blockHoldSeconds)
                        host.Session.RecordObjectiveProgress(assignment.request, 1, true);
                }
                else if (assignment.request.Definition.requestType ==
                    TaxiRequestType.ReachLocationBeforeVehicle)
                {
                    float targetDistance = Vector3.ProjectOnPlane(
                        assignment.vehicle.transform.position - assignment.raceGoal, Vector3.up).magnitude;
                    float playerDistance = Vector3.ProjectOnPlane(
                        tractorBody.position - assignment.raceGoal, Vector3.up).magnitude;
                    if (targetDistance <= 10)
                        host.Session.FailObjective(assignment.request, "The target reached the race goal first.");
                    else if (playerDistance <= 10)
                        host.Session.RecordObjectiveProgress(assignment.request, 1, true);
                }
                assignment.previousTargetSpeed = targetSpeed >= 2 ? targetSpeed :
                    Time.time - assignment.lastMovingAt <= 3 ? assignment.previousTargetSpeed : 0;
            }
            foreach (var assignment in expired)
                host.Session.FailObjective(assignment.request, "Mission target lost.");
        }

        private bool TractorObstructs(Assignment assignment)
        {
            Vector3 direction = Vector3.ProjectOnPlane(assignment.vehicle.transform.forward, Vector3.up).normalized;
            Vector3 origin = assignment.vehicle.transform.position + direction * 3 + Vector3.up;
            int count = Physics.RaycastNonAlloc(origin, direction, blockHits, 11, ~0,
                QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            bool tractor = false;
            for (int i = 0; i < count; i++)
            {
                var hit = blockHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(assignment.vehicle.transform))
                    continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                tractor = hit.rigidbody == tractorBody ||
                    hit.collider.transform.IsChildOf(host.Player.transform);
            }
            return tractor;
        }

        private void OnRequestResolved(TaxiRequestProgress request)
        {
            if (!assignments.TryGetValue(request, out var assignment)) return;
            assignments.Remove(request);
            reservedIds.Remove(assignment.vehicleId);
            Cleanup(assignment);
        }

        private void Cleanup(Assignment assignment)
        {
            assignment.missionTarget?.Clear();
            if (assignment.marker != null) Destroy(assignment.marker.gameObject);
            if (assignment.goalMarker != null) Destroy(assignment.goalMarker.gameObject);
            foreach (var drop in assignment.drops)
                if (drop != null && drop.transform.parent != null)
                    Destroy(drop.transform.parent.gameObject);
        }
    }
}
