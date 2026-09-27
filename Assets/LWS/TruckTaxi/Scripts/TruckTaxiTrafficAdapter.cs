using System.Collections;
using System.Collections.Generic;
using System;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiTrafficAdapter : MonoBehaviour
    {
        public GameObject[] trafficPrefabs;
        public LwsTrafficLaneDefinition[] cityLanes;
        public int maximumVehicles = 12;
        [Min(0)] public int maximumDedicatedVehicles = 8;
        [Min(10)] public float maximumDedicatedSpawnDistance = 100;
        [Tooltip("Optional Taxi-only override. Unassigned or disabled preserves the original baseline setup.")]
        public TruckTaxiPopulationProfile densityProfile;
        public bool useDensityOverride = true;
        [Header("Taxi traffic behaviour")]
        public bool useTaxiBoulevardLanes = true;
        public AudioClip trafficHorn;
        [Header("Traffic simulation bubbles")]
        [Min(1)] public int maxFullTraffic = 40;
        [Min(0)] public int maxVisibleTraffic = 40;
        [Min(10)] public float fullTrafficRadius = 175;
        [Min(20)] public float visibleTrafficRadius = 350;
        [Min(0)] public float trafficHysteresis = 35;
        [Min(0)] public float forwardPreloadSeconds = 3;
        [Min(1)] public int maximumPoolChangesPerPass = 8;
        public Func<Vector3, bool> RegionAvailable { get; private set; } = _ => true;
        private readonly List<Vector3[]> runtimePathPoints = new List<Vector3[]>();
        private TruckTaxiIntersection[] intersections = System.Array.Empty<TruckTaxiIntersection>();
        public int RuntimeLaneCount => validLanes.Count;
        public int LaneChanges { get; private set; }
        private readonly List<LaneReservation> laneReservations = new List<LaneReservation>();
        private struct LaneReservation { public int lane; public Vector3 position; public float until; }
        private readonly LwsUtsTrafficApi api = new LwsUtsTrafficApi();
        private readonly List<Component> paths = new List<Component>();
        private readonly List<LwsTrafficLaneDefinition> validLanes = new List<LwsTrafficLaneDefinition>();
        private readonly List<GameObject> vehicles = new List<GameObject>();
        private readonly Dictionary<string, GameObject> vehiclesById = new Dictionary<string, GameObject>();
        private readonly HashSet<GameObject> dedicatedVehicles = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, TruckTaxiPopulationPresentation> presentations = new Dictionary<GameObject, TruckTaxiPopulationPresentation>();
        private readonly List<TruckTaxiTrafficPooledActor> pool = new List<TruckTaxiTrafficPooledActor>();
        private readonly List<TruckTaxiTrafficPopulation.Car> candidates = new List<TruckTaxiTrafficPopulation.Car>();
        private readonly HashSet<TruckTaxiTrafficPopulation.Car> selectedFull = new HashSet<TruckTaxiTrafficPopulation.Car>();
        private readonly HashSet<TruckTaxiTrafficPopulation.Car> selectedVisible = new HashSet<TruckTaxiTrafficPopulation.Car>();
        private TruckTaxiTrafficPopulation population;
        private int spawnedThisSecond, despawnedThisSecond;
        private float rateWindow;
        private readonly LwsTrafficSpawnPolicy policy = new LwsTrafficSpawnPolicy {
            targetCruiseSpeedScale = 0.7f, maximumTrafficSpeedMetersPerSecond = 12, autoResolveEditorPrefabs = false };
        private float nextMaintenance;
        private int serial;
        private int spawnCursor;
        private bool initialized;
        private bool baselineValidation;
        public bool IsBaselineValidation => baselineValidation;
        public int LogicalCount => population?.Count ?? 0;
        public int FullPhysicsCount { get; private set; }
        public int PooledCount => pool.Count;
        public int SpawnsPerSecond { get; private set; }
        public int DespawnsPerSecond { get; private set; }
        public void SetRegionAvailability(Func<Vector3, bool> predicate)
        {
            RegionAvailable = predicate ?? (_ => true);
            nextMaintenance = 0;
            if (initialized) EvictUnavailable();
        }
        public int BaselineActiveCount { get; private set; }
        public int TargetCount => DensityEnabled ? (baselineValidation ?
            TruckTaxiPopulationProfile.ScaleTarget(densityProfile.measuredTrafficBaseline > 0 ? densityProfile.measuredTrafficBaseline : BaselineActiveCount, 1, densityProfile.maximumActiveTraffic)
            : densityProfile.TrafficTarget(BaselineActiveCount)) : maximumVehicles;
        public int SpawnAttempts { get; private set; }
        public int RejectedSpawnAttempts { get; private set; }
        public bool DensityEnabled => useDensityOverride && densityProfile != null;
        public IReadOnlyList<GameObject> Vehicles => vehicles;
        public IReadOnlyDictionary<string, GameObject> ActiveVehicles => vehiclesById;
        public bool TryResolveVehicle(string stableId, out GameObject vehicle)
        {
            vehicle = null;
            if (string.IsNullOrEmpty(stableId) || !vehiclesById.TryGetValue(stableId, out var found) || found == null || !found.activeInHierarchy) return false;
            vehicle = found;
            return true;
        }
        public bool TryGetVehicleAi(string stableId, out Component ai)
        {
            ai = null;
            if (!TryResolveVehicle(stableId, out var vehicle)) return false;
            foreach (var component in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
                if (component.GetType().Name == "CarAIController") { ai = component; return true; }
            return false;
        }
        public bool ReleaseDedicatedVehicle(string stableId)
        {
            if (!TryResolveVehicle(stableId, out var vehicle) || !dedicatedVehicles.Contains(vehicle)) return false;
            if (!PoolVehicle(vehicle)) return false;
            return true;
        }
        public bool TrySpawnDedicatedVehicle(Vector3 near, out GameObject vehicle, out string stableId) =>
            TrySpawnDedicatedVehicle(null, near, out vehicle, out stableId);
        public bool TrySpawnDedicatedVehicle(string stableId, Vector3 near, out GameObject vehicle)
        {
            vehicle = null;
            if (string.IsNullOrWhiteSpace(stableId)) return false;
            return TrySpawnDedicatedVehicle(stableId, near, out vehicle, out _);
        }
        private bool TrySpawnDedicatedVehicle(string requestedId, Vector3 near, out GameObject vehicle, out string stableId)
        {
            vehicle = null; stableId = null;
            if (requestedId != null && vehiclesById.ContainsKey(requestedId)) return false;
            if (!initialized || !Ready || trafficPrefabs == null || trafficPrefabs.Length == 0 ||
                dedicatedVehicles.Count >= maximumDedicatedVehicles || FullPhysicsCount >= maxFullTraffic) return false;
            int bestLane = -1, bestPoint = -1;
            float bestDistance = maximumDedicatedSpawnDistance * maximumDedicatedSpawnDistance;
            for (int laneIndex = 0; laneIndex < validLanes.Count; laneIndex++)
            {
                var lane = validLanes[laneIndex];
                if (!lane.spawnEnabled) continue;
                for (int point = 2; point < lane.centerline.Length - 2; point++)
                {
                    Vector3 position = lane.centerline[point];
                    float distance = (position - near).sqrMagnitude;
                    if (distance >= bestDistance || !RegionAvailable(position) || !HasSpawnClearance(position, 14, 20)) continue;
                    bestDistance = distance; bestLane = laneIndex; bestPoint = point;
                }
            }
            if (bestLane < 0) return false;
            int prefabIndex = spawnCursor++ % trafficPrefabs.Length;
            vehicle = AcquireVehicle(prefabIndex, bestLane, bestPoint, requestedId ?? "taxi.traffic." + (serial + 1),
                true, validLanes[bestLane].centerline[bestPoint]);
            if (vehicle == null) return false;
            stableId = RegisterVehicle(vehicle, bestLane, requestedId, prefabIndex);
            dedicatedVehicles.Add(vehicle);
            FullPhysicsCount++;
            return true;
        }
        public int ActiveCount => vehicles.Count;
        public int PresentationCount => presentations.Count;
        public int ReducedShadowCount { get; private set; }
        public bool Ready => paths.Count > 0 && api.IsAvailable;
        public bool ConfigureDensity(TruckTaxiPopulationProfile profile, bool applyOverride = true)
        {
            if (initialized) { Debug.LogWarning("Configure Taxi traffic density before Initialize; live traffic was left untouched.", this); return false; }
            densityProfile = profile; useDensityOverride = applyOverride; baselineValidation = false; return true;
        }
        public bool ConfigureBaselineForValidation(TruckTaxiPopulationProfile profile)
        {
            if (!ConfigureDensity(profile)) return false;
            baselineValidation = true; return true;
        }
        public void Initialize()
        {
            if (initialized) return;
            if (!api.IsAvailable) { Debug.LogError(api.AvailabilitySummary,this); return; }
            intersections = GetComponentsInChildren<TruckTaxiIntersection>();
            if (intersections.Length == 0) intersections = FindObjectsByType<TruckTaxiIntersection>(FindObjectsSortMode.None);
            if (trafficHorn == null) trafficHorn = Resources.Load<AudioClip>("NWH Vehicle Physics 2/Defaults/Sound/Horn");
            foreach (var lane in useTaxiBoulevardLanes ? TruckTaxiTrafficLaneNetwork.ExpandTaxiBoulevards(cityLanes) : cityLanes ?? System.Array.Empty<LwsTrafficLaneDefinition>())
            {
                if (lane == null || lane.centerline == null || lane.centerline.Length < 5) continue;
                var root = new GameObject(lane.laneId); root.transform.SetParent(transform,false);
                var path = api.CreatePath(root,lane,trafficPrefabs,policy,out string message);
                if (path == null) { Debug.LogError(message,this); Destroy(root); continue; }
                paths.Add(path); validLanes.Add(lane);
                var countMethod = path.GetType().GetMethod("getPointsTotal");
                var pointMethod = path.GetType().GetMethod("getNextPoint");
                int count = (int)countMethod.Invoke(path, new object[] { 0 });
                var points = new Vector3[count];
                for (int p = 0; p < count; p++) points[p] = (Vector3)pointMethod.Invoke(path, new object[] { 0, p });
                runtimePathPoints.Add(points);
            }
            // Measure the old startup behavior before enabling any density override.
            for (int i=0;i<maximumVehicles;i++) TrySpawnTraffic(maximumVehicles, false);
            BaselineActiveCount = ActiveCount; initialized = true;
            for (int i = vehicles.Count - 1; i >= 0; i--) PoolVehicle(vehicles[i]);
            population = new TruckTaxiTrafficPopulation(validLanes, TargetCount, trafficPrefabs?.Length ?? 0, serial);
            serial += population.Count;
            nextMaintenance = 0;
            if (DensityEnabled) Debug.Log($"TAXI TRAFFIC: original cap {maximumVehicles}, observed startup {BaselineActiveCount}, multiplier {(baselineValidation ? 1 : densityProfile.trafficDensityMultiplier)}, target/cap {TargetCount}/{densityProfile.maximumActiveTraffic}.", this);
        }
        public bool SpawnTraffic()
        {
            if (!initialized || population == null) return false;
            var observer = PresentationObserver();
            for (int i = 0; i < population.Count; i++)
            {
                var car = population.Cars[i];
                if (car.Actor != null || !RegionAvailable(population.Position(car))) continue;
                if ((population.Position(car) - observer).sqrMagnitude > visibleTrafficRadius * visibleTrafficRadius) continue;
                if (Materialize(car, Vector3.Distance(population.Position(car), observer) <= fullTrafficRadius)) return true;
            }
            return false;
        }
        private bool TrySpawnTraffic(int limit, bool dense)
        {
            for (int i = vehicles.Count - 1; i >= 0; i--)
                if (vehicles[i] == null)
                {
                    if (!ReferenceEquals(vehicles[i], null)) ForgetVehicle(vehicles[i]);
                    vehicles.RemoveAt(i);
                }
            if (vehicles.Count - dedicatedVehicles.Count >= limit || !Ready || trafficPrefabs == null || trafficPrefabs.Length == 0) return false;
            SpawnAttempts++;
            int cursor = spawnCursor++;
            int index = cursor % paths.Count;
            var lane = validLanes[index];
            if (!lane.spawnEnabled) { RejectedSpawnAttempts++; return false; }
            // Vary points within each lane, avoiding a common-factor cycle that never visits most road nodes.
            int point = dense ? DensePointIndex(cursor, paths.Count, lane.centerline.Length)
                : 2 + (cursor * 23) % (lane.centerline.Length - 4);
            Vector3 position = lane.centerline[point];
            if (!RegionAvailable(position)) { RejectedSpawnAttempts++; return false; }
            if (dense && !densityProfile.AllowsSpawn(position, transform.position)) { RejectedSpawnAttempts++; return false; }
            float clearance = dense ? Mathf.Max(1, densityProfile.trafficSpawnClearance) : 14;
            float playerClearance = dense ? Mathf.Max(1, densityProfile.playerTrafficSpawnClearance) : 20;
            if (!HasSpawnClearance(position, clearance, playerClearance)) { RejectedSpawnAttempts++; return false; }
            var vehicle = api.SpawnVehicle(trafficPrefabs[cursor % trafficPrefabs.Length],paths[index],lane,point,transform,policy,out string message);
            if (vehicle == null) { RejectedSpawnAttempts++; Debug.LogWarning(message,this); return false; }
            RegisterVehicle(vehicle, index, null, cursor % trafficPrefabs.Length);
            return true;
        }
        private bool HasSpawnClearance(Vector3 position, float vehicleClearance, float playerClearance)
        {
            foreach (var car in vehicles)
                if (car != null && (car.transform.position-position).sqrMagnitude < vehicleClearance*vehicleClearance) return false;
            var player = TruckTaxiBootstrap.Instance?.Player;
            return player == null || (player.transform.position-position).sqrMagnitude >= playerClearance*playerClearance;
        }
        private string RegisterVehicle(GameObject vehicle, int lane, string requestedId = null, int prefabIndex = 0)
        {
            // Runtime-added UTS components need their Start lifecycle before AI calls Move.
            foreach (var component in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component.GetType().Name == "CarMove") component.enabled = true;
                if (component.GetType().Name == "CarAIController")
                    component.enabled = false;
            }
            var target = vehicle.GetComponent<TruckTaxiImpactTarget>() ?? vehicle.AddComponent<TruckTaxiImpactTarget>();
            target.kind = TaxiImpactKind.Traffic;
            if (requestedId == null)
            {
                do { requestedId = "taxi.traffic."+(++serial); } while (vehiclesById.ContainsKey(requestedId));
            }
            target.targetId = requestedId;
            vehicles.Add(vehicle);
            vehiclesById.Add(target.targetId, vehicle);
            if (vehicle.GetComponent<TruckTaxiRoadRage>() == null) vehicle.AddComponent<TruckTaxiRoadRage>();
            var behaviour = vehicle.GetComponent<TruckTaxiTrafficBehaviour>() ?? vehicle.AddComponent<TruckTaxiTrafficBehaviour>();
            behaviour.Initialize(this, lane, target.targetId, trafficHorn);
            var pooled = vehicle.GetComponent<TruckTaxiTrafficPooledActor>();
            if (pooled == null) { pooled = vehicle.AddComponent<TruckTaxiTrafficPooledActor>(); pooled.Configure(prefabIndex); }
            pooled.SetCruiseSpeed(api.ResolveTrafficSpeed(validLanes[lane], policy));
            pooled.SetFullPhysics(true);
            StartCoroutine(EnableInitializedAi(pooled, pooled.Generation));
            var presentation = new TruckTaxiPopulationPresentation(vehicle, false);
            presentations.Add(vehicle, presentation);
            presentation.Refresh(DensityEnabled ? densityProfile : null, PresentationObserver());
            return target.targetId;
        }
        private IEnumerator EnableInitializedAi(TruckTaxiTrafficPooledActor actor, int generation)
        {
            yield return null;
            if (actor != null) actor.EnableAi(generation);
        }
        public float LaneSpeed(int lane) => lane >= 0 && lane < validLanes.Count ? validLanes[lane].speedLimitMph * .44704f : 10;

        public float SignalStoppingDistance(Vector3 front, Vector3 forward, float reach)
        {
            float distance = float.PositiveInfinity;
            foreach (var intersection in intersections)
                if (intersection != null && intersection.TryGetStopDistance(front, forward, reach, out float candidate))
                    distance = Mathf.Min(distance, candidate);
            return distance;
        }

        public bool TryChangeLane(TruckTaxiTrafficBehaviour driver, bool emergency)
        {
            if (driver == null || driver.ChangingLane || driver.LaneIndex < 0 || driver.LaneIndex >= validLanes.Count) return false;
            Vector3 position = driver.transform.position, forward = driver.transform.forward;
            // No lateral manoeuvres on junction approaches, curves or unregistered lanes.
            var graph = TruckTaxiBootstrap.Instance?.roadGraph?.Graph;
            if(graph!=null)
                foreach(var node in graph.nodes)
                {
                    if (node == null || (position-node.position).sqrMagnitude >= 28*28) continue;
                    int degree = 0;
                    foreach (var edge in graph.edges)
                        if (edge != null && (edge.fromNodeId == node.nodeId || edge.toNodeId == node.nodeId)) degree++;
                    if (degree > 2) return false;
                }
            foreach (var intersection in intersections)
                if (intersection != null && Vector3.Distance(position, intersection.transform.position) < 35) return false;
            for (int candidate = 0; candidate < validLanes.Count; candidate++)
            {
                if (!TruckTaxiTrafficLaneNetwork.AreAdjacent(validLanes[driver.LaneIndex], validLanes[candidate])) continue;
                var points = runtimePathPoints[candidate];
                int closest = -1; float distance = 8 * 8;
                for (int p = 2; p < points.Length - 3; p++)
                {
                    float sqr = (points[p] - position).sqrMagnitude;
                    if (sqr >= distance || Vector3.Dot((points[p + 1] - points[p]).normalized, forward) < .97f ||
                        !TruckTaxiTrafficLaneNetwork.IsStraight(points[p - 1], points[p], points[p + 1])) continue;
                    closest = p; distance = sqr;
                }
                if (closest < 0) continue;
                Vector3 tangent = (points[closest + 1] - points[closest]).normalized;
                Vector3 lanePosition = points[closest] + tangent * Vector3.Dot(position - points[closest], tangent);
                float lateral = Vector3.Distance(lanePosition, position);
                if (lateral < 2.5f || lateral > 5) continue;
                float rearGap = Mathf.Max(12, driver.Speed * (driver.Personality >= TruckTaxiDriverPersonality.Aggressive ? 1.3f : 1.8f));
                float ahead = Mathf.Max(15, driver.Speed * 1.6f);
                if (!driver.GapClear(lanePosition, tangent, ahead, rearGap)) continue;
                bool reserved=false;
                for(int r=laneReservations.Count-1;r>=0;r--)
                {
                    if(laneReservations[r].until<Time.time) { laneReservations.RemoveAt(r); continue; }
                    if(laneReservations[r].lane==candidate && Vector3.Distance(laneReservations[r].position,lanePosition)<ahead+rearGap) reserved=true;
                }
                if(reserved) continue;
                int target = closest;
                while (target < points.Length - 3 && Vector3.Dot(points[target] - position, forward) < ahead) target++;
                if (Vector3.Dot(points[target] - position, forward) < 10 ||
                    !TruckTaxiTrafficLaneNetwork.IsStraight(points[target - 1], points[target], points[target + 1])) continue;
                if (driver.SwitchLane(paths[candidate], target - 1, candidate))
                { LaneChanges++; laneReservations.Add(new LaneReservation { lane=candidate,position=lanePosition,until=Time.time+4 }); return true; }
            }
            return false;
        }
        public static int DensePointIndex(int cursor, int laneCount, int pointCount)
        {
            int available = Mathf.Max(1, pointCount - 4);
            int stride = Mathf.Min(23, available);
            while (GreatestCommonDivisor(stride, available) != 1) stride--;
            return 2 + (int)(((long)Mathf.Max(0, cursor) / Mathf.Max(1, laneCount) * stride) % available);
        }
        private static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0) { int next = a % b; a = b; b = next; }
            return a;
        }
        private void Update()
        {
            if (!Ready || population == null) return;
            EvictUnavailable();
            float delta = Mathf.Min(.5f, Time.deltaTime);
            for (int i = 0; i < population.Count; i++)
            {
                var car = population.Cars[i];
                population.Advance(car, delta);
                if (car.Actor != null && !car.FullPhysics)
                {
                    var actor = car.Actor.GetComponent<TruckTaxiTrafficPooledActor>();
                    if (actor != null)
                    {
                        Vector3 destination = population.Position(car);
                        Vector3 smooth = Vector3.MoveTowards(car.Actor.transform.position, destination, car.Speed * delta * 1.5f);
                        actor.MoveReduced(smooth, population.Forward(car));
                    }
                }
            }
            if (Time.time - rateWindow >= 1)
            {
                SpawnsPerSecond = spawnedThisSecond; DespawnsPerSecond = despawnedThisSecond;
                spawnedThisSecond = despawnedThisSecond = 0; rateWindow = Time.time;
            }
            if (Time.time < nextMaintenance) return;
            nextMaintenance = Time.time + (DensityEnabled ? Mathf.Max(.1f, densityProfile.maintenanceInterval) : .5f);
            MaintainPopulation();
        }

        private void MaintainPopulation()
        {
            for (int i = vehicles.Count - 1; i >= 0; i--)
                if (vehicles[i] == null)
                {
                    if (!ReferenceEquals(vehicles[i], null)) ForgetVehicle(vehicles[i]);
                    vehicles.RemoveAt(i);
                }
            dedicatedVehicles.RemoveWhere(vehicle => vehicle == null);
            FullPhysicsCount = 0;
            foreach (var vehicle in vehicles)
                if (vehicle != null && vehicle.GetComponent<TruckTaxiTrafficPooledActor>()?.FullPhysics == true)
                    FullPhysicsCount++;
            Vector3 observer = PresentationObserver();
            var player = TruckTaxiBootstrap.Instance?.Player;
            Vector3 forward = player != null ? player.transform.forward : transform.forward;
            float speed = player != null ? player.GetComponent<Rigidbody>()?.linearVelocity.magnitude ?? 0 : 0;
            float preload = speed * forwardPreloadSeconds;
            Camera camera = Camera.main;
            candidates.Clear(); selectedFull.Clear(); selectedVisible.Clear();
            for (int i = 0; i < population.Count; i++)
            {
                var car = population.Cars[i];
                if (car.Actor == null) { car.Actor = null; car.FullPhysics = false; }
                else
                {
                    var pooled = car.Actor.GetComponent<TruckTaxiTrafficPooledActor>();
                    car.MissionReserved = pooled != null && pooled.MissionReserved;
                    if (car.FullPhysics && pooled != null)
                    {
                        var behaviour = car.Actor.GetComponent<TruckTaxiTrafficBehaviour>();
                        if (behaviour != null && behaviour.LaneIndex >= 0 && behaviour.LaneIndex < validLanes.Count)
                            car.Lane = behaviour.LaneIndex;
                    }
                }
                Vector3 position = car.FullPhysics && car.Actor != null ? car.Actor.transform.position : population.Position(car);
                if (!RegionAvailable(position) && !car.MissionReserved) continue;
                candidates.Add(car);
            }
            candidates.Sort((a, b) => Priority(a, observer, forward, preload, camera)
                .CompareTo(Priority(b, observer, forward, preload, camera)));
            int fullBudget = Mathf.Max(0, maxFullTraffic - dedicatedVehicles.Count);
            int visibleBudget = Mathf.Max(0, maxVisibleTraffic);
            foreach (var car in candidates)
            {
                Vector3 position = car.FullPhysics && car.Actor != null ? car.Actor.transform.position : population.Position(car);
                Vector3 offset = position - observer;
                float distance = offset.magnitude;
                float ahead = Vector3.Dot(offset, forward);
                float effective = ahead > 0 ? Mathf.Max(0, distance - preload) : distance;
                if (car.MissionReserved)
                {
                    selectedFull.Add(car);
                    fullBudget = Mathf.Max(0, fullBudget - 1);
                }
                else if (fullBudget > 0 && TruckTaxiTrafficPopulation.WantFull(effective, car.FullPhysics, fullTrafficRadius, trafficHysteresis))
                {
                    selectedFull.Add(car); fullBudget--;
                }
                else if (visibleBudget > 0 && TruckTaxiTrafficPopulation.WantVisible(effective, car.Actor != null, visibleTrafficRadius, trafficHysteresis))
                {
                    selectedVisible.Add(car); visibleBudget--;
                }
            }
            int changes = 0;
            for (int i = 0; i < population.Count; i++)
            {
                var car = population.Cars[i];
                bool full = selectedFull.Contains(car), visible = selectedVisible.Contains(car);
                if (car.Actor == null)
                {
                    if ((full || visible) && changes < maximumPoolChangesPerPass && Materialize(car, full)) changes++;
                    continue;
                }
                var actor = car.Actor.GetComponent<TruckTaxiTrafficPooledActor>();
                if (actor == null) continue;
                if (!full && !visible && !car.MissionReserved)
                {
                    if (changes < maximumPoolChangesPerPass)
                    {
                        if (car.FullPhysics) population.Capture(car, car.Actor.transform.position, actor.Body != null ? actor.Body.linearVelocity.magnitude : car.Speed);
                        if (PoolVehicle(car.Actor)) { car.Actor = null; car.FullPhysics = false; changes++; }
                    }
                }
                else if (full && !car.FullPhysics)
                {
                    if (FullPhysicsCount >= maxFullTraffic) continue;
                    population.Capture(car, car.Actor.transform.position, car.Speed);
                    actor.SynchronizePath(paths[car.Lane], car.Point);
                    actor.SetFullPhysics(true); car.FullPhysics = true; FullPhysicsCount++;
                    StartCoroutine(EnableInitializedAi(actor, actor.Generation));
                }
                else if (!full && car.FullPhysics && !car.MissionReserved)
                {
                    population.Capture(car, car.Actor.transform.position, actor.Body != null ? actor.Body.linearVelocity.magnitude : car.Speed);
                    actor.SetFullPhysics(false); car.FullPhysics = false; FullPhysicsCount = Mathf.Max(0, FullPhysicsCount - 1);
                }
                if (car.Actor != null && presentations.TryGetValue(car.Actor, out var presentation))
                    presentation.Refresh(DensityEnabled ? densityProfile : null, observer);
            }
            ReducedShadowCount = 0;
            foreach (var presentation in presentations.Values) if (presentation.ShadowsReduced) ReducedShadowCount++;
        }

        private float Priority(TruckTaxiTrafficPopulation.Car car, Vector3 observer, Vector3 forward, float preload, Camera camera)
        {
            Vector3 position = car.FullPhysics && car.Actor != null ? car.Actor.transform.position : population.Position(car);
            Vector3 offset = position - observer;
            float distance = offset.magnitude;
            float ahead = Vector3.Dot(offset, forward);
            float score = Mathf.Max(0, distance - (ahead > 0 ? preload : 0));
            if (camera != null)
            {
                Vector3 viewport = camera.WorldToViewportPoint(position);
                if (viewport.z > 0 && viewport.x >= -.1f && viewport.x <= 1.1f &&
                    viewport.y >= -.1f && viewport.y <= 1.1f) score *= .65f;
            }
            return score;
        }

        private bool Materialize(TruckTaxiTrafficPopulation.Car car, bool full)
        {
            if (!TruckTaxiTrafficPopulation.CanMaterialize(vehicles.Count, FullPhysicsCount,
                maxFullTraffic, maxVisibleTraffic, full)) return false;
            Vector3 position = population.Position(car);
            if (!RegionAvailable(position) || !HasSpawnClearance(position, 10, 16)) return false;
            var vehicle = AcquireVehicle(car.VehicleType, car.Lane, car.Point, car.Id, full, position);
            if (vehicle == null) return false;
            RegisterVehicle(vehicle, car.Lane, car.Id, car.VehicleType);
            var actor = vehicle.GetComponent<TruckTaxiTrafficPooledActor>();
            actor.SetFullPhysics(full);
            car.Actor = vehicle; car.FullPhysics = full;
            if (full) FullPhysicsCount++;
            spawnedThisSecond++;
            return true;
        }

        private GameObject AcquireVehicle(int prefabIndex, int lane, int point, string stableId, bool full, Vector3 position)
        {
            if (!RegionAvailable(position)) return null;
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                var actor = pool[i];
                if (actor == null) { pool.RemoveAt(i); continue; }
                if (actor.PrefabIndex != prefabIndex) continue;
                pool.RemoveAt(i);
                actor.Place(paths[lane], point, position, stableId, full);
                return actor.gameObject;
            }
            var vehicle = api.SpawnVehicle(trafficPrefabs[prefabIndex], paths[lane], validLanes[lane], point,
                transform, policy, out string message);
            if (vehicle == null) Debug.LogWarning(message, this);
            else vehicle.transform.position = position;
            return vehicle;
        }

        private void EvictUnavailable()
        {
            if (population != null)
                for (int i = 0; i < population.Count; i++)
                {
                    var car = population.Cars[i];
                    if (car.Actor == null) continue;
                    var actor = car.Actor.GetComponent<TruckTaxiTrafficPooledActor>();
                    if (actor == null || actor.MissionReserved || RegionAvailable(car.Actor.transform.position)) continue;
                    if (car.FullPhysics)
                    {
                        var driver = car.Actor.GetComponent<TruckTaxiTrafficBehaviour>();
                        if (driver != null && driver.LaneIndex >= 0 && driver.LaneIndex < validLanes.Count)
                            car.Lane = driver.LaneIndex;
                        population.Capture(car, car.Actor.transform.position,
                            actor.Body != null ? actor.Body.linearVelocity.magnitude : car.Speed);
                    }
                    if (PoolVehicle(car.Actor)) { car.Actor = null; car.FullPhysics = false; }
                }
            for (int i = vehicles.Count - 1; i >= 0; i--)
            {
                var vehicle = vehicles[i];
                if (vehicle == null || !dedicatedVehicles.Contains(vehicle) || RegionAvailable(vehicle.transform.position)) continue;
                PoolVehicle(vehicle);
            }
        }

        private bool PoolVehicle(GameObject vehicle)
        {
            if (vehicle == null) return false;
            var actor = vehicle.GetComponent<TruckTaxiTrafficPooledActor>();
            if (actor == null || actor.MissionReserved) return false;
            if (presentations.TryGetValue(vehicle, out var presentation)) presentation.Restore();
            ForgetVehicle(vehicle);
            vehicles.Remove(vehicle);
            if (actor.FullPhysics) FullPhysicsCount = Mathf.Max(0, FullPhysicsCount - 1);
            actor.PutAway();
            pool.Add(actor);
            despawnedThisSecond++;
            return true;
        }

        public void ResetTraffic()
        {
            foreach (var vehicle in vehicles)
                if (vehicle != null && vehicle.GetComponent<TruckTaxiTrafficPooledActor>()?.MissionReserved == true)
                { Debug.LogWarning("Traffic reset deferred while a mission target is assigned.", this); return; }
            for (int i = vehicles.Count - 1; i >= 0; i--) PoolVehicle(vehicles[i]);
            population = new TruckTaxiTrafficPopulation(validLanes, TargetCount, trafficPrefabs?.Length ?? 0, serial);
            serial += population.Count;
            FullPhysicsCount = 0;
            nextMaintenance = 0;
        }
        private Vector3 PresentationObserver()
        {
            var player = TruckTaxiBootstrap.Instance?.Player;
            return player != null ? player.transform.position : transform.position;
        }
        private void ForgetVehicle(GameObject vehicle)
        {
            presentations.Remove(vehicle);
            dedicatedVehicles.Remove(vehicle);
            var target = vehicle != null ? vehicle.GetComponent<TruckTaxiImpactTarget>() : null;
            if (target != null) vehiclesById.Remove(target.targetId);
            else
            {
                string key = null;
                foreach (var pair in vehiclesById) if (ReferenceEquals(pair.Value, vehicle)) { key = pair.Key; break; }
                if (key != null) vehiclesById.Remove(key);
            }
        }
        private void OnDisable()
        {
            foreach (var presentation in presentations.Values) presentation.Restore();
        }
    }
}
