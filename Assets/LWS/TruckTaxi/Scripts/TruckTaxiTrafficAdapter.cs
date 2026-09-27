using System.Collections;
using System.Collections.Generic;
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
        private readonly LwsTrafficSpawnPolicy policy = new LwsTrafficSpawnPolicy {
            targetCruiseSpeedScale = 0.7f, maximumTrafficSpeedMetersPerSecond = 12, autoResolveEditorPrefabs = false };
        private float nextMaintenance;
        private int serial;
        private int spawnCursor;
        private bool initialized;
        private bool baselineValidation;
        public bool IsBaselineValidation => baselineValidation;
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
            return TryResolveVehicle(stableId, out var vehicle) && dedicatedVehicles.Remove(vehicle);
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
                dedicatedVehicles.Count >= maximumDedicatedVehicles || vehicles.Count >= TargetCount + maximumDedicatedVehicles) return false;
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
                    if (distance >= bestDistance || !HasSpawnClearance(position, 14, 20)) continue;
                    bestDistance = distance; bestLane = laneIndex; bestPoint = point;
                }
            }
            if (bestLane < 0) return false;
            int prefabIndex = spawnCursor++ % trafficPrefabs.Length;
            vehicle = api.SpawnVehicle(trafficPrefabs[prefabIndex], paths[bestLane], validLanes[bestLane],
                bestPoint, transform, policy, out string message);
            if (vehicle == null) { Debug.LogWarning(message, this); return false; }
            stableId = RegisterVehicle(vehicle, bestLane, requestedId);
            dedicatedVehicles.Add(vehicle);
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
            if (DensityEnabled) Debug.Log($"TAXI TRAFFIC: original cap {maximumVehicles}, observed startup {BaselineActiveCount}, multiplier {(baselineValidation ? 1 : densityProfile.trafficDensityMultiplier)}, target/cap {TargetCount}/{densityProfile.maximumActiveTraffic}.", this);
        }
        public bool SpawnTraffic() => TrySpawnTraffic(TargetCount, DensityEnabled);
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
            // Vary points within each lane, avoiding a common-factor cycle that never visits most road nodes.
            int point = dense ? DensePointIndex(cursor, paths.Count, lane.centerline.Length)
                : 2 + (cursor * 23) % (lane.centerline.Length - 4);
            Vector3 position = lane.centerline[point];
            if (dense && !densityProfile.AllowsSpawn(position, transform.position)) { RejectedSpawnAttempts++; return false; }
            float clearance = dense ? Mathf.Max(1, densityProfile.trafficSpawnClearance) : 14;
            float playerClearance = dense ? Mathf.Max(1, densityProfile.playerTrafficSpawnClearance) : 20;
            if (!HasSpawnClearance(position, clearance, playerClearance)) { RejectedSpawnAttempts++; return false; }
            var vehicle = api.SpawnVehicle(trafficPrefabs[cursor % trafficPrefabs.Length],paths[index],lane,point,transform,policy,out string message);
            if (vehicle == null) { RejectedSpawnAttempts++; Debug.LogWarning(message,this); return false; }
            RegisterVehicle(vehicle, index);
            return true;
        }
        private bool HasSpawnClearance(Vector3 position, float vehicleClearance, float playerClearance)
        {
            foreach (var car in vehicles)
                if (car != null && (car.transform.position-position).sqrMagnitude < vehicleClearance*vehicleClearance) return false;
            var player = TruckTaxiBootstrap.Instance?.Player;
            return player == null || (player.transform.position-position).sqrMagnitude >= playerClearance*playerClearance;
        }
        private string RegisterVehicle(GameObject vehicle, int lane, string requestedId = null)
        {
            // Runtime-added UTS components need their Start lifecycle before AI calls Move.
            foreach (var component in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component.GetType().Name == "CarMove") component.enabled = true;
                if (component.GetType().Name == "CarAIController")
                {
                    component.enabled = false;
                    StartCoroutine(EnableInitializedAi(component));
                }
            }
            var target = vehicle.AddComponent<TruckTaxiImpactTarget>(); target.kind = TaxiImpactKind.Traffic;
            if (requestedId == null)
            {
                do { requestedId = "taxi.traffic."+(++serial); } while (vehiclesById.ContainsKey(requestedId));
            }
            target.targetId = requestedId;
            vehicles.Add(vehicle);
            vehiclesById.Add(target.targetId, vehicle);
            vehicle.AddComponent<TruckTaxiRoadRage>();
            vehicle.AddComponent<TruckTaxiTrafficBehaviour>().Initialize(this, lane, target.targetId, trafficHorn);
            var presentation = new TruckTaxiPopulationPresentation(vehicle, false);
            presentations.Add(vehicle, presentation);
            presentation.Refresh(DensityEnabled ? densityProfile : null, PresentationObserver());
            return target.targetId;
        }
        private IEnumerator EnableInitializedAi(Behaviour ai)
        {
            yield return null;
            if (ai != null) ai.enabled = true;
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
                foreach(var node in graph.nodes) if(Vector3.Distance(position,node.position)<28) return false;
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
            if (!Ready || Time.time < nextMaintenance) return;
            nextMaintenance = Time.time + (DensityEnabled ? Mathf.Max(.1f, densityProfile.maintenanceInterval) : 3);
            Vector3 observer = PresentationObserver(); ReducedShadowCount = 0;
            for (int i=vehicles.Count-1;i>=0;i--)
            {
                var car = vehicles[i];
                if (car == null) { if (!ReferenceEquals(car, null)) ForgetVehicle(car); vehicles.RemoveAt(i); continue; }
                bool outside = DensityEnabled && densityProfile.despawnRadius > 0
                    ? !densityProfile.AllowsPresence(car.transform.position, transform.position)
                    : Mathf.Abs(car.transform.position.x)>420 || Mathf.Abs(car.transform.position.z)>420;
                if (car.transform.position.y < -5 || (!dedicatedVehicles.Contains(car) && (outside || vehicles.Count - dedicatedVehicles.Count > TargetCount)))
                { car.SetActive(false); Destroy(car); vehicles.RemoveAt(i); ForgetVehicle(car); continue; }
                if (presentations.TryGetValue(car, out var presentation))
                {
                    presentation.Refresh(DensityEnabled ? densityProfile : null, observer);
                    if (presentation.ShadowsReduced) ReducedShadowCount++;
                }
            }
            int budget = DensityEnabled ? Mathf.Max(1, densityProfile.maximumSpawnsPerPass) : 1;
            int spawned = 0;
            for (int attempt = 0; attempt < budget * (DensityEnabled ? 8 : 1) && spawned < budget && ActiveCount - dedicatedVehicles.Count < TargetCount; attempt++)
                if (SpawnTraffic()) spawned++;
        }
        public void ResetTraffic()
        {
            foreach (var vehicle in vehicles) if (vehicle!=null) { vehicle.SetActive(false); Destroy(vehicle); }
            vehicles.Clear(); presentations.Clear(); vehiclesById.Clear(); dedicatedVehicles.Clear(); spawnCursor = 0;
            // Keep unique impact IDs across respawns. Dense refill is budgeted by the existing maintenance path.
            for (int i=0;i<Mathf.Min(maximumVehicles, TargetCount);i++) SpawnTraffic();
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
