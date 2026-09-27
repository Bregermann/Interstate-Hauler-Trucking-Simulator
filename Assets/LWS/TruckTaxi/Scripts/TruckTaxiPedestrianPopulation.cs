using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LWS.TruckTaxi
{
    // UTS owns walking, path following, avoidance and animation; LWS adds scoring and respawn.
    public sealed class TruckTaxiPedestrianPopulation : MonoBehaviour
    {
        public Component[] peoplePaths;
        public TruckTaxiPedestrianArea[] pedestrianAreas;
        public TruckTaxiIntersection[] signalIntersections;
        public LWS.InterstateHauler.LwsTrafficLaneDefinition[] roadLanes;
        public int maximumPeople = 24;
        [Min(1)] public int maxFullPedestrians = 100;
        [Min(1)] public int maxVisiblePedestrians = 180;
        [Min(1)] public float fullPhysicsRadius = 85;
        [Min(1)] public float visibleRadius = 170;
        [Min(0)] public float populationHysteresis = 20;
        [Range(5, 10)] public float offscreenGraceSeconds = 8;
        [Min(25)] public float graphRebuildDistance = 120;
        [Tooltip("Use clothed WobblePeople visuals; disable for the original UTS pedestrian models.")]
        public bool useWobblePeople = true;
        [Tooltip("Optional Taxi-only override. Disable before Initialize/ResetPopulation to measure the unmodified UTS baseline.")]
        public TruckTaxiPopulationProfile densityProfile;
        public bool useDensityOverride = true;
        private TruckTaxiPedestrianImpactSettings settings=new TruckTaxiPedestrianImpactSettings();
        private readonly List<TruckTaxiPedestrian> people = new List<TruckTaxiPedestrian>();
        private readonly List<Component> activePaths = new List<Component>();
        private readonly Dictionary<Component, TruckTaxiPedestrianArea> pathAreas = new Dictionary<Component, TruckTaxiPedestrianArea>();
        private readonly Dictionary<TruckTaxiPedestrian,Component> origins=new Dictionary<TruckTaxiPedestrian,Component>();
        private readonly Dictionary<Component, PathAccess> bindings = new Dictionary<Component, PathAccess>();
        private readonly List<TruckTaxiPedestrian> fallen = new List<TruckTaxiPedestrian>();
        private readonly Dictionary<TruckTaxiPedestrian, TruckTaxiPopulationPresentation> presentations = new Dictionary<TruckTaxiPedestrian, TruckTaxiPopulationPresentation>();
        private readonly Dictionary<TruckTaxiPedestrian, float> visibleUntil = new Dictionary<TruckTaxiPedestrian, float>();
        private readonly Dictionary<TruckTaxiPedestrian, float> interactionUntil = new Dictionary<TruckTaxiPedestrian, float>();
        private readonly Dictionary<TruckTaxiPedestrian, WalkAgent> walkers = new Dictionary<TruckTaxiPedestrian, WalkAgent>();
        private readonly List<WalkAgent> walkerSchedule = new List<WalkAgent>();
        private readonly Dictionary<TruckTaxiIntersection, int> activeCrossings = new Dictionary<TruckTaxiIntersection, int>();
        private readonly Stack<int> freeRows = new Stack<int>();
        private readonly TruckTaxiPedestrianPool pool = new TruckTaxiPedestrianPool();
        private readonly TruckTaxiPedestrianLogicalPopulation logical = new TruckTaxiPedestrianLogicalPopulation();
        private readonly Dictionary<TruckTaxiPedestrian, TruckTaxiPedestrianLogicalPopulation.Record> assigned =
            new Dictionary<TruckTaxiPedestrian, TruckTaxiPedestrianLogicalPopulation.Record>();
        private Func<Vector3, bool> regionAvailable = _ => true;
        private bool graphDirty;
        private Vector3 graphOrigin;
        private int logicalCursor;
        private int spawnedInWindow, despawnedInWindow;
        private float rateWindowStart;
        private TruckTaxiPedestrianWalkGraph walkGraph;
        private Component runtimePath;
        private Vector3[,] runtimePoints;
        private int nextRow;
        private bool initialized;
        private bool appliedWobbleStyle;
        private bool baselineValidation;
        private float nextMaintenance;
        private int pathCursor;
        private int serial;
        private int walkerCursor;
        public int BaselineActiveCount { get; private set; }
        public bool IsBaselineValidation => baselineValidation;
        public int TargetCount => DensityEnabled ? (baselineValidation ?
            TruckTaxiPopulationProfile.ScaleTarget(densityProfile.measuredPedestrianBaseline > 0 ? densityProfile.measuredPedestrianBaseline : BaselineActiveCount, 1, densityProfile.maximumActivePedestrians)
            : densityProfile.PedestrianTarget(BaselineActiveCount)) : maximumPeople;
        public bool DensityEnabled => useDensityOverride && densityProfile != null;
        public int RagdollCount => fallen.Count;
        public int RetiredCount { get; private set; }
        public int RejectedSpawnAttempts { get; private set; }
        public bool Ready => initialized && bindings.Count > 0;
        public int ActiveCount => people.Count;
        public int LogicalCount => logical.Count > 0 ? logical.Count : TargetCount;
        public int PooledCount => pool.Count;
        public int FullPhysicsCount { get { int count = 0; foreach (var ped in people) if (ped != null && (ped.IsRagdoll || ped.IsFullPhysics)) count++; return count; } }
        public float SpawnsPerSecond { get; private set; }
        public float DespawnsPerSecond { get; private set; }
        public int PresentationCount => presentations.Count;
        public int GraphNodeCount => walkGraph?.Count ?? 0;
        public int GraphWalkableSpawnCount => walkGraph?.WalkableSpawnCount ?? 0;
        public int GraphRoadSegmentCount => walkGraph?.RoadSegmentCount ?? 0;
        public int GraphWalkerCount => walkers.Count;
        public int LegalCrossingCount(TruckTaxiIntersection intersection) =>
            intersection != null && activeCrossings.TryGetValue(intersection, out int count) ? count : 0;
        public int ReducedShadowCount { get; private set; }
        public IReadOnlyList<TruckTaxiPedestrian> People => people;
        public void PinForInteraction(TruckTaxiPedestrian pedestrian, float seconds = 10)
        {
            if (pedestrian != null && people.Contains(pedestrian))
                interactionUntil[pedestrian] = Mathf.Max(Time.time + Mathf.Clamp(seconds, 0, 30),
                    interactionUntil.TryGetValue(pedestrian, out float current) ? current : 0);
        }

        public static bool KeepLive(float distanceSquared, float radius, float hysteresis,
            float now, float visibleUntil, bool interacting)
        {
            if (interacting || now < visibleUntil) return true;
            float outer = Mathf.Max(0, radius) + Mathf.Max(0, hysteresis);
            return distanceSquared <= outer * outer;
        }
        public bool ShowColliders { get; set; }
        public void SetRegionAvailability(Func<Vector3, bool> availability)
        { regionAvailable = availability ?? (_ => true); if (initialized) NotifyRegionChanged(); }
        public void RegisterLogicalRegion(string id, Bounds bounds)
        {
            logical.RegisterRegion(id, bounds);
            if (initialized) NotifyRegionChanged();
        }
        public void NotifyRegionChanged()
        {
            for (int i = people.Count - 1; i >= 0; i--)
                if (people[i] != null && !regionAvailable(people[i].transform.position)) Retire(people[i]);
            graphDirty = true;
            nextMaintenance = Mathf.Min(nextMaintenance, Time.time + .1f);
        }
        public void SetWobblePeopleVisible(bool visible)
        {
            useWobblePeople = visible;
            appliedWobbleStyle = visible;
            foreach (var ped in people)
                if (ped != null) ped.SetWobbleVisible(visible);
        }
        public bool ConfigureDensity(TruckTaxiPopulationProfile profile, bool applyOverride = true)
        {
            if (initialized) { Debug.LogWarning("Configure Taxi pedestrian density before Initialize; live population was left untouched.", this); return false; }
            densityProfile = profile; useDensityOverride = applyOverride; baselineValidation = false; return true;
        }
        public bool ConfigureBaselineForValidation(TruckTaxiPopulationProfile profile)
        {
            if (!ConfigureDensity(profile)) return false;
            baselineValidation = true; return true;
        }
        public void Initialize(TruckTaxiPedestrianImpactSettings configuration=null)
        {
            settings=configuration ?? settings;
            if (initialized) return;
            bindings.Clear();
            activePaths.Clear(); pathAreas.Clear();
            foreach (var area in pedestrianAreas ?? System.Array.Empty<TruckTaxiPedestrianArea>())
            {
                if (area == null) continue;
                foreach (var path in area.walkingPaths ?? System.Array.Empty<Component>())
                {
                    if (path == null || !path.transform.IsChildOf(area.transform) || pathAreas.ContainsKey(path)) continue;
                    pathAreas.Add(path, area); activePaths.Add(path);
                }
            }
            if (activePaths.Count == 0)
                foreach (var path in peoplePaths ?? System.Array.Empty<Component>()) if (path != null && !activePaths.Contains(path)) activePaths.Add(path);
            foreach (var intersection in signalIntersections ?? System.Array.Empty<TruckTaxiIntersection>())
                if (intersection != null && intersection.IsConfigured && !activePaths.Contains(intersection.crosswalkPath))
                    activePaths.Add(intersection.crosswalkPath);
            foreach (var path in activePaths)
            {
                if (path == null || bindings.ContainsKey(path)) continue;
                var access = new PathAccess(path);
                if (!access.Valid) { Debug.LogError("TAXI PEDESTRIANS: required public UTS PeopleWalkPath API missing on " + path.name, this); continue; }
                if (pathAreas.TryGetValue(path, out var area) && !access.AllPointsAllowed(area, roadLanes))
                { Debug.LogError("TAXI PEDESTRIANS: unsafe walk path rejected: " + path.name, path); continue; }
                bindings.Add(path, access);
                access.EnsureParent();
                if (regionAvailable(path.transform.position) && regionAvailable(access.StartPosition))
                    access.SpawnBatch(1);
                Bind(path, int.MaxValue, false);
            }
            BaselineActiveCount = ActiveCount;
            appliedWobbleStyle = useWobblePeople;
            initialized = true;
            logical.Resize(TargetCount, transform.position);
            if (DensityEnabled)
            {
                ClearPeople();
                BuildWalkGraph();
                if (walkGraph != null && walkGraph.HasRoadBounds && walkGraph.WalkableSpawnCount > 0 && TargetCount > 0)
                {
                    CreateRuntimePath();
                    for (int i = 0; i < Mathf.Min(24, Mathf.Min(TargetCount, maxVisiblePedestrians)); i++)
                        if (!SpawnGraphOne()) break;
                }
                else
                {
                    Debug.LogWarning("TAXI PEDESTRIANS: no grounded walk graph nodes; retaining only authored UTS baseline.", this);
                    walkGraph = null;
                }
                Debug.Log($"TAXI PEDESTRIANS: UTS baseline {BaselineActiveCount}, graph nodes {GraphNodeCount}, target {TargetCount}, initial {ActiveCount}.", this);
            }
            nextMaintenance = Time.time + .5f;
            rateWindowStart = Time.time;
        }
        private void BuildWalkGraph()
        {
            Vector3 observer = PresentationObserver();
            graphOrigin = observer;
            walkGraph = new TruckTaxiPedestrianWalkGraph(roadLanes, pedestrianAreas, peoplePaths,
                signalIntersections, TruckTaxiBootstrap.Instance?.roadGraph?.Graph,
                Array.FindAll(FindObjectsByType<TruckTaxiWorldAnchor>(FindObjectsSortMode.None),
                    anchor => anchor != null && anchor.gameObject.scene.isLoaded),
                Array.FindAll(FindObjectsByType<TruckTaxiSurface>(FindObjectsSortMode.None),
                    surface => surface != null && surface.gameObject.scene.isLoaded),
                densityProfile.spawnRadius > 0 ? Mathf.Min(densityProfile.spawnRadius,
                    visibleRadius + populationHysteresis + 50) : visibleRadius + populationHysteresis + 50, observer);
            graphDirty = false;
        }
        private void Bind(Component path, int limit, bool dense, int graphNode = -1, Transform newChild = null)
        {
            var parent=bindings[path].Parent;
            if(parent==null) return;
            if (newChild != null) { BindChild(path, limit, dense, graphNode, newChild); return; }
            foreach(Transform child in parent.transform) BindChild(path, limit, dense, graphNode, child);
        }
        private void BindChild(Component path, int limit, bool dense, int graphNode, Transform child)
        {
            if(child.GetComponent<TruckTaxiPedestrian>()!=null) return;
            Vector3 spawnPosition = graphNode >= 0 ? walkGraph[graphNode] : child.position;
            if (people.Count >= limit || !regionAvailable(spawnPosition) ||
                (graphNode < 0 && pathAreas.TryGetValue(path, out var area) && !area.Allows(spawnPosition, roadLanes)) ||
                (dense && !CanSpawnAt(spawnPosition)))
            { child.gameObject.SetActive(false); Destroy(child.gameObject); RejectedSpawnAttempts++; return; }
            if (graphNode >= 0) child.position = spawnPosition;
            child.gameObject.layer=0;
            if(child.GetComponent<Rigidbody>()==null) child.gameObject.AddComponent<Rigidbody>();
            var ped=child.gameObject.AddComponent<TruckTaxiPedestrian>();
            ped.settings=settings;
            ped.ConfigureTractor(TruckTaxiBootstrap.Instance?.Player?.GetComponent<Rigidbody>());
            ped.SetVisualBinding(TruckTaxiWobbleVisual.BindPedestrian(child, null, null, 1.7f, serial), useWobblePeople);
            child.gameObject.AddComponent<TruckTaxiAiPedestrianImpact>();
            child.gameObject.AddComponent<TruckTaxiPedestrianGesture>();
            var target=child.gameObject.AddComponent<TruckTaxiImpactTarget>();
            target.kind=TaxiImpactKind.Pedestrian; target.targetId="taxi.pedestrian."+serial++;
            people.Add(ped);
            origins.Add(ped,path); ped.Expired+=Retire; ped.Ragdolled+=OnRagdolled;
            var presentation = new TruckTaxiPopulationPresentation(ped.gameObject, true);
            presentations.Add(ped, presentation);
            presentation.Refresh(DensityEnabled ? densityProfile : null, PresentationObserver());
            if (graphNode >= 0) BindGraphWalker(ped, graphNode);
            if (graphNode >= 0) ped.transform.SetParent(transform, true);
            if (DensityEnabled && (FullPhysicsCount > maxFullPedestrians ||
                (ped.transform.position - PresentationObserver()).sqrMagnitude > fullPhysicsRadius * fullPhysicsRadius))
                ped.SetFullPhysics(false);
        }
        public void SpawnOne()
        {
            TrySpawnOne();
        }
        private bool TrySpawnOne()
        {
            if (walkGraph != null) return SpawnGraphOne();
            if (DensityEnabled) return false;
            if (!Ready || people.Count >= Mathf.Min(TargetCount, maxVisiblePedestrians) || activePaths.Count == 0) return false;
            for (int attempt = 0; attempt < activePaths.Count; attempt++)
            {
                var path = activePaths[pathCursor++ % activePaths.Count];
                if (path == null || !bindings.TryGetValue(path, out var access)) continue;
                if (pathAreas.TryGetValue(path, out var area) && !area.Allows(access.StartPosition, roadLanes)) { RejectedSpawnAttempts++; continue; }
                if (DensityEnabled && !CanSpawnAt(access.StartPosition)) { RejectedSpawnAttempts++; continue; }
                int before = people.Count;
                access.SpawnSingle(); Bind(path, TargetCount, DensityEnabled);
                if (people.Count > before) return true;
            }
            return false;
        }
        private bool CanSpawnAt(Vector3 position)
        {
            if (!regionAvailable(position) || !densityProfile.AllowsSpawn(position, PresentationObserver())) return false;
            Vector3 delta = position - PresentationObserver(); delta.y = 0;
            if (delta.sqrMagnitude > visibleRadius * visibleRadius) return false;
            if (Physics.CheckCapsule(position + Vector3.up * .65f, position + Vector3.up * 1.55f,
                .35f, ~0, QueryTriggerInteraction.Ignore)) return false;
            float clearance = Mathf.Max(.25f, densityProfile.pedestrianSpawnClearance);
            foreach (var ped in people)
                if (ped != null && (ped.transform.position - position).sqrMagnitude < clearance * clearance) return false;
            var player = TruckTaxiBootstrap.Instance?.Player;
            return player == null || (player.transform.position - position).sqrMagnitude >= 25;
        }
        private void CreateRuntimePath()
        {
            var source = activePaths[0];
            var go = new GameObject("UTS dynamic pedestrian waypoints");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            runtimePath = go.AddComponent(source.GetType());
            var type = source.GetType();
            int capacity = TargetCount;
            runtimePoints = new Vector3[capacity, 4];
            type.GetField("points").SetValue(runtimePath, runtimePoints);
            var lengths = new int[capacity];
            var forwards = new bool[capacity];
            for (int i = 0; i < capacity; i++) { lengths[i] = 4; forwards[i] = true; }
            type.GetField("pointLength").SetValue(runtimePath, lengths);
            type.GetField("_forward").SetValue(runtimePath, forwards);
            type.GetField("numberOfWays").SetValue(runtimePath, capacity);
            type.GetField("loopPath").SetValue(runtimePath, true);
            type.GetField("disableLineDraw").SetValue(runtimePath, true);
            foreach (var flag in new[] { "_ignorePeople", "_ignoreCar", "_ignoreBicycle" })
                type.GetField(flag, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(runtimePath, false);
            go.SetActive(true);
        }
        private bool SpawnGraphOne()
        {
            if (walkGraph == null || runtimePath == null || people.Count >= Mathf.Min(TargetCount, maxVisiblePedestrians) || bindings.Count == 0) return false;
            TruckTaxiPedestrianLogicalPopulation.Record record = null;
            for (int i = 0; i < logical.Count; i++)
            {
                var candidate = logical.Records[logicalCursor++ % logical.Count];
                if (candidate.Actor == null && logical.RegionNear(candidate, PresentationObserver(), visibleRadius) &&
                    logical.RegionAvailable(candidate, regionAvailable)) { record = candidate; break; }
            }
            if (record == null) return false;
            for (int attempt = 0; attempt < Mathf.Min(walkGraph.WalkableSpawnCount, 256); attempt++)
            {
                int node = walkGraph.NextSpawnNode();
                if (node < 0 || !logical.Contains(record, walkGraph[node]) || !CanSpawnAt(walkGraph[node]))
                { RejectedSpawnAttempts++; continue; }
                var source = activePaths[pathCursor++ % activePaths.Count];
                if (source == null || !bindings.TryGetValue(source, out var access) ||
                    !regionAvailable(access.StartPosition)) continue;
                var parent = access.Parent;
                if (parent == null) continue;
                string id = "taxi.pedestrian." + record.Id;
                var reused = pool.Acquire(walkGraph[node], Quaternion.identity, id, false);
                if (reused != null)
                {
                    RegisterGraphActor(reused, source, node, record);
                    return true;
                }
                int before = people.Count;
                int childCount = parent.transform.childCount;
                access.SpawnSingle();
                if (parent.transform.childCount > childCount)
                    Bind(source, TargetCount, true, node, parent.transform.GetChild(parent.transform.childCount - 1));
                if (people.Count > before)
                {
                    var ped = people[people.Count - 1];
                    var target = ped.GetComponent<TruckTaxiImpactTarget>(); if (target != null) target.targetId = id;
                    record.Actor = ped; record.Position = walkGraph[node]; assigned[ped] = record;
                    spawnedInWindow++;
                    return true;
                }
            }
            return false;
        }
        private void RegisterGraphActor(TruckTaxiPedestrian ped, Component source, int node,
            TruckTaxiPedestrianLogicalPopulation.Record record)
        {
            ped.settings = settings;
            ped.Expired += Retire; ped.Ragdolled += OnRagdolled;
            ped.SetWobbleVisible(useWobblePeople);
            people.Add(ped); origins[ped] = source;
            record.Actor = ped; record.Position = walkGraph[node]; assigned[ped] = record;
            var presentation = new TruckTaxiPopulationPresentation(ped.gameObject, true);
            presentations[ped] = presentation;
            presentation.Refresh(densityProfile, PresentationObserver());
            BindGraphWalker(ped, node);
            UpdatePhysicsBudget(PresentationObserver());
            spawnedInWindow++;
        }
        private void OnRagdolled(TruckTaxiPedestrian pedestrian) => UpdatePhysicsBudget(PresentationObserver());
        private void BindGraphWalker(TruckTaxiPedestrian ped, int node)
        {
            int row = freeRows.Count > 0 ? freeRows.Pop() : nextRow++;
            if (row >= runtimePoints.GetLength(0)) return;
            var move = ped.GetComponent("MovePath");
            var actor = ped.GetComponent("Passersby");
            if (move == null || actor == null) return;
            var walker = new WalkAgent(ped, move, actor, row, node);
            walkers.Add(ped, walker);
            walkerSchedule.Add(walker);
            SetWaypoint(walker, node);
            PlanNextWalk(walker);
        }
        private void SetWaypoint(WalkAgent walker, int node)
        {
            var position = walkGraph[node];
            for (int i = 0; i < 4; i++) runtimePoints[walker.Row, i] = position;
            walker.MoveType.GetField("walkPath").SetValue(walker.Move, runtimePath);
            walker.MoveType.GetField("w").SetValue(walker.Move, walker.Row);
            walker.MoveType.GetField("forward").SetValue(walker.Move, true);
            walker.MoveType.GetField("loop").SetValue(walker.Move, true);
            walker.MoveType.GetField("targetPoint").SetValue(walker.Move, 1);
            walker.MoveType.GetField("targetPointsTotal").SetValue(walker.Move, 2);
            walker.MoveType.GetField("finishPos").SetValue(walker.Move, position);
            walker.MoveType.GetField("randXFinish").SetValue(walker.Move, 0f);
            walker.MoveType.GetField("randZFinish").SetValue(walker.Move, 0f);
            walker.NextNode = node;
        }
        private void PlanNextWalk(WalkAgent walker)
        {
            bool plannedCrossing = Random.value < .35f && walkGraph.PickCrossingJourney(walker.CurrentNode, walker.Route);
            int destination = plannedCrossing ? -1 : walkGraph.PickDestination(walker.CurrentNode, Random.value < .4f ? 45 : 180);
            if ((!plannedCrossing && (destination < 0 || !walkGraph.Route(walker.CurrentNode, destination, walker.Route))) || walker.Route.Count == 0)
            {
                walker.IdleUntil = Time.time + Random.Range(1f, 3f);
                walker.SetState(false);
                return;
            }
            walker.SetState(true);
            walker.RouteIndex = 0;
            int first = walker.Route[0];
            if (walkGraph.TryCrossingEntry(walker.CurrentNode, first, out var crossing))
            {
                if (!crossing.PedestrianMayCross)
                {
                    walker.RouteIndex = -1;
                    walker.WaitingFor = crossing;
                    walker.SetState(false);
                    SetWaypoint(walker, walker.CurrentNode);
                    return;
                }
                BeginCrossing(walker, crossing);
            }
            SetWaypoint(walker, first);
        }
        private void UpdateWalker(WalkAgent walker)
        {
            if (walker.Pedestrian == null || walker.Pedestrian.IsRagdoll) return;
            if (walker.IdleUntil > Time.time) return;
            if (walker.IdleUntil > 0)
            {
                walker.IdleUntil = 0;
                PlanNextWalk(walker);
                return;
            }
            Vector3 delta = walker.Pedestrian.transform.position - walkGraph[walker.NextNode];
            delta.y = 0;
            if (delta.sqrMagnitude > 1.5f * 1.5f) return;
            walker.CurrentNode = walker.NextNode;
            if (walker.Crossing != null && walkGraph.IsOppositeCurb(walker.CurrentNode, walker.EntryCurb, walker.Crossing))
                EndCrossing(walker);
            if (++walker.RouteIndex < walker.Route.Count)
            {
                int next = walker.Route[walker.RouteIndex];
                if (walker.Crossing == null && walkGraph.TryCrossingEntry(walker.CurrentNode, next, out var crossing))
                {
                    if (!crossing.PedestrianMayCross)
                    {
                        walker.RouteIndex--;
                        walker.WaitingFor = crossing;
                        walker.SetState(false);
                        SetWaypoint(walker, walker.CurrentNode);
                        return;
                    }
                    BeginCrossing(walker, crossing);
                }
                if (walker.WaitingFor != null)
                {
                    walker.WaitingFor = null;
                    walker.SetState(true);
                }
                SetWaypoint(walker, next);
                return;
            }
            SetWaypoint(walker, walker.CurrentNode);
            walker.IdleUntil = Time.time + Random.Range(1f, 6f);
            walker.SetState(false);
        }
        private void BeginCrossing(WalkAgent walker, TruckTaxiIntersection crossing)
        {
            walker.WaitingFor = null;
            walker.Crossing = crossing;
            walker.EntryCurb = walker.CurrentNode;
            walker.SetState(true);
            walker.SetInsideCrosswalk(true);
            activeCrossings.TryGetValue(crossing, out int count);
            activeCrossings[crossing] = count + 1;
        }
        private void EndCrossing(WalkAgent walker)
        {
            var crossing = walker.Crossing;
            walker.Crossing = null;
            walker.SetInsideCrosswalk(false);
            if (crossing == null || !activeCrossings.TryGetValue(crossing, out int count)) return;
            if (count <= 1) activeCrossings.Remove(crossing);
            else activeCrossings[crossing] = count - 1;
        }
        private sealed class WalkAgent
        {
            public readonly TruckTaxiPedestrian Pedestrian;
            public readonly Component Move;
            public readonly Component Actor;
            public readonly System.Type MoveType;
            public readonly System.Type ActorType;
            private readonly PropertyInfo lastState, animationState, inside, red;
            private readonly object walking, idle;
            public readonly int Row;
            public readonly List<int> Route = new List<int>();
            public int CurrentNode;
            public int NextNode;
            public int RouteIndex;
            public float IdleUntil;
            public float NextCheckAt;
            public TruckTaxiIntersection WaitingFor;
            public TruckTaxiIntersection Crossing;
            public int EntryCurb;
            public WalkAgent(TruckTaxiPedestrian pedestrian, Component move, Component actor, int row, int node)
            {
                Pedestrian = pedestrian; Move = move; Actor = actor; MoveType = move.GetType(); ActorType = actor.GetType();
                lastState = ActorType.GetProperty("LastState"); animationState = ActorType.GetProperty("ANIMATION_STATE");
                inside = ActorType.GetProperty("INSIDE"); red = ActorType.GetProperty("RED");
                walking = System.Enum.Parse(lastState.PropertyType, "walk"); idle = System.Enum.Parse(lastState.PropertyType, "idle1");
                Row = row; CurrentNode = node; NextNode = node;
            }
            public void SetState(bool moving)
            {
                object value = moving ? walking : idle;
                lastState.SetValue(Actor, value); animationState.SetValue(Actor, value);
            }
            public void SetInsideCrosswalk(bool value)
            {
                inside?.SetValue(Actor, value);
                red?.SetValue(Actor, false);
            }
        }
        public void ResetPopulation()
        {
            ClearPeople();
            pool.Clear();
            if (runtimePath != null) Destroy(runtimePath.gameObject);
            runtimePath = null; runtimePoints = null; walkGraph = null;
            initialized = false; pathCursor = 0; Initialize();
        }
        private void ClearPeople()
        {
            foreach(var ped in people)
                if(ped!=null) { ped.Expired-=Retire; ped.Ragdolled-=OnRagdolled; ped.gameObject.SetActive(false); Destroy(ped.gameObject); }
            foreach (var record in logical.Records) record.Actor = null;
            assigned.Clear();
            people.Clear(); origins.Clear(); fallen.Clear(); presentations.Clear(); visibleUntil.Clear(); interactionUntil.Clear(); walkers.Clear();
            walkerSchedule.Clear(); activeCrossings.Clear(); walkerCursor = 0; freeRows.Clear(); nextRow = 0;
        }
        private void Retire(TruckTaxiPedestrian ped)
        {
            if(!origins.TryGetValue(ped,out var path)) return;
            ped.Expired-=Retire; ped.Ragdolled-=OnRagdolled; people.Remove(ped); origins.Remove(ped);
            if (presentations.TryGetValue(ped, out var presentation)) presentation.Restore();
            presentations.Remove(ped);
            visibleUntil.Remove(ped); interactionUntil.Remove(ped);
            if (assigned.TryGetValue(ped, out var record))
            { record.Position = ped.transform.position; record.Actor = null; assigned.Remove(ped); }
            if (walkers.TryGetValue(ped, out var walker))
            { EndCrossing(walker); freeRows.Push(walker.Row); walkers.Remove(ped); walkerSchedule.Remove(walker); }
            fallen.Remove(ped); RetiredCount++;
            if (DensityEnabled) { ped.transform.SetParent(transform, true); pool.Release(ped); }
            else { ped.gameObject.SetActive(false); Destroy(ped.gameObject); }
            despawnedInWindow++;
            // Dense replacement is deferred to the bounded maintenance pass, including simultaneous mass impacts.
            if(!isActiveAndEnabled || path==null || people.Count>=TargetCount || DensityEnabled) return;
            bindings[path].SpawnSingle(); Bind(path, TargetCount, false);
        }
        private void Update()
        {
            if (DensityEnabled)
                foreach (var ped in people)
                    if (ped != null && !ped.IsFullPhysics && !ped.IsRagdoll)
                        ped.TickReducedMovement(((Time.frameCount + ped.GetInstanceID()) & 1) == 0);
            if (Ready && walkGraph != null) TickWalkers();
            if (!Ready || Time.time < nextMaintenance) return;
            float elapsed = Time.time - rateWindowStart;
            if (elapsed >= 1f)
            {
                SpawnsPerSecond = spawnedInWindow / elapsed;
                DespawnsPerSecond = despawnedInWindow / elapsed;
                spawnedInWindow = despawnedInWindow = 0; rateWindowStart = Time.time;
            }
            if (appliedWobbleStyle != useWobblePeople) SetWobblePeopleVisible(useWobblePeople);
            nextMaintenance = Time.time + (DensityEnabled ? Mathf.Max(.1f, densityProfile.maintenanceInterval) : .5f);
            if (!DensityEnabled)
            {
                foreach (var presentation in presentations.Values) presentation.Restore();
                ReducedShadowCount = 0; return;
            }
            Vector3 graphOffset = PresentationObserver() - graphOrigin; graphOffset.y = 0;
            if (graphOffset.sqrMagnitude >= graphRebuildDistance * graphRebuildDistance) graphDirty = true;
            Vector3 observer = PresentationObserver();
            var camera = Camera.main;
            foreach (var ped in people)
            {
                if (ped == null) continue;
                Vector3 toPed = ped.transform.position - observer; toPed.y = 0;
                bool near = toPed.sqrMagnitude < 35 * 35;
                var player = TruckTaxiBootstrap.Instance?.Player;
                bool forward = player != null && toPed.sqrMagnitude < 100 * 100 &&
                    Vector3.Dot(player.transform.forward, toPed.normalized) > .6f;
                bool onCamera = false;
                float cameraRadius = visibleRadius + populationHysteresis + 30;
                if (camera != null && toPed.sqrMagnitude < cameraRadius * cameraRadius)
                {
                    Vector3 screen = camera.WorldToViewportPoint(ped.transform.position + Vector3.up);
                    onCamera = screen.z > 0 && screen.x > -.05f && screen.x < 1.05f &&
                        screen.y > -.05f && screen.y < 1.05f;
                }
                if (near || forward || onCamera) visibleUntil[ped] = Time.time + offscreenGraceSeconds;
            }
            if (graphDirty && !HasVisibilityPins())
            {
                for (int i = people.Count - 1; i >= 0; i--)
                    if (people[i] != null && !people[i].IsRagdoll) Retire(people[i]);
                foreach (var walker in walkerSchedule) if (walker.Crossing != null) EndCrossing(walker);
                walkers.Clear(); walkerSchedule.Clear(); activeCrossings.Clear(); freeRows.Clear(); nextRow = 0;
                BuildWalkGraph();
                if (runtimePath == null && walkGraph.WalkableSpawnCount > 0) CreateRuntimePath();
            }
            ReducedShadowCount = 0;
            for (int i = people.Count - 1; i >= 0; i--)
            {
                var ped = people[i];
                if (ped == null)
                {
                    if (!ReferenceEquals(ped, null)) { origins.Remove(ped); presentations.Remove(ped); }
                    people.RemoveAt(i); continue;
                }
                Vector3 offset = ped.transform.position - observer; offset.y = 0;
                float until = visibleUntil.TryGetValue(ped, out float grace) ? grace : 0;
                bool interacting = interactionUntil.TryGetValue(ped, out float interaction) && Time.time < interaction;
                if (!ped.IsRagdoll && ((people.Count > maxVisiblePedestrians && !interacting && Time.time >= until) ||
                    ped.transform.position.y < -5 ||
                    !KeepLive(offset.sqrMagnitude, visibleRadius, populationHysteresis, Time.time, until, interacting) ||
                    !regionAvailable(ped.transform.position) ||
                    (assigned.TryGetValue(ped, out var slot) && !logical.Contains(slot, ped.transform.position) &&
                        !interacting && Time.time >= until)))
                { Retire(ped); continue; }
                if (ped.IsRagdoll && !fallen.Contains(ped)) fallen.Add(ped);
                if (presentations.TryGetValue(ped, out var presentation))
                {
                    presentation.Refresh(densityProfile, observer);
                    if (presentation.ShadowsReduced) ReducedShadowCount++;
                }
            }
            fallen.RemoveAll(p => p == null || !p.IsRagdoll);
            while (fallen.Count > Mathf.Min(Mathf.Max(1, maxFullPedestrians),
                Mathf.Max(1, densityProfile.maximumActiveRagdolls)))
                Retire(fallen[0]);
            UpdatePhysicsBudget(observer);
            for (int i = 0; i < Mathf.Max(1, densityProfile.maximumSpawnsPerPass) && people.Count < Mathf.Min(TargetCount, maxVisiblePedestrians); i++)
                if (!TrySpawnOne()) break;
        }

        private bool HasVisibilityPins()
        {
            foreach (var ped in people)
                if (ped != null && ((visibleUntil.TryGetValue(ped, out float grace) && Time.time < grace) ||
                    (interactionUntil.TryGetValue(ped, out float interaction) && Time.time < interaction))) return true;
            return false;
        }
        private void UpdatePhysicsBudget(Vector3 observer)
        {
            int full = 0;
            foreach (var ped in people) if (ped != null && ped.IsRagdoll) full++;
            var candidates = new List<TruckTaxiPedestrian>(people.Count);
            foreach (var ped in people) if (ped != null && !ped.IsRagdoll) candidates.Add(ped);
            candidates.Sort((a, b) => (a.transform.position - observer).sqrMagnitude.CompareTo(
                (b.transform.position - observer).sqrMagnitude));
            foreach (var ped in candidates)
            {
                Vector3 delta = ped.transform.position - observer; delta.y = 0;
                bool shouldBeFull = full < maxFullPedestrians && delta.sqrMagnitude <= fullPhysicsRadius * fullPhysicsRadius;
                ped.SetFullPhysics(shouldBeFull);
                if (shouldBeFull) full++;
            }
        }
        private void TickWalkers()
        {
            int count = Mathf.Min(48, walkerSchedule.Count);
            Vector3 observer = PresentationObserver();
            for (int i = 0; i < count; i++)
            {
                if (walkerCursor >= walkerSchedule.Count) walkerCursor = 0;
                var walker = walkerSchedule[walkerCursor++];
                if (walker.Pedestrian == null || walker.Pedestrian.IsRagdoll)
                { if (walker.Crossing != null) EndCrossing(walker); continue; }
                if (Time.time < walker.NextCheckAt) continue;
                float distance = (walker.Pedestrian.transform.position - observer).sqrMagnitude;
                walker.NextCheckAt = Time.time + (distance < 65 * 65 ? .1f : distance < 180 * 180 ? .5f : 1.5f);
                UpdateWalker(walker);
            }
        }
        private Vector3 PresentationObserver()
        {
            var player = TruckTaxiBootstrap.Instance?.Player;
            return player != null ? player.transform.position : transform.position;
        }
        private void OnDisable()
        {
            foreach (var presentation in presentations.Values) presentation.Restore();
        }
        // UTS is in Assembly-CSharp. Cache its existing public API once per authored path, never reflect per frame.
        private sealed class PathAccess
        {
            private readonly Component path;
            private readonly MethodInfo batch, single;
            private readonly FieldInfo parent, density, points;
            private readonly float authoredDensity;
            private readonly object[] singleArgs = { 0, true };
            public bool Valid => batch != null && single != null && parent != null && density != null && points != null;
            public GameObject Parent => parent.GetValue(path) as GameObject;
            public void EnsureParent()
            {
                if (Parent != null) return;
                var container = new GameObject("walkingObjects");
                container.transform.SetParent(path.transform, false);
                parent.SetValue(path, container);
            }
            public Vector3 StartPosition => points.GetValue(path) is Vector3[,] value && value.GetLength(1) > 1 ? value[0, 1] : path.transform.position;
            public bool AllPointsAllowed(TruckTaxiPedestrianArea area, LWS.InterstateHauler.LwsTrafficLaneDefinition[] lanes)
            {
                if (!(points.GetValue(path) is Vector3[,] value)) return false;
                for (int way = 0; way < value.GetLength(0); way++)
                    for (int index = 1; index < value.GetLength(1)-1; index++)
                        if (!area.Allows(value[way,index], lanes)) return false;
                return true;
            }
            public PathAccess(Component value)
            {
                path = value; var type = path.GetType();
                batch = type.GetMethod("SpawnPeople"); single = type.GetMethod("SpawnOnePeople");
                parent = type.GetField("par"); density = type.GetField("Density"); points = type.GetField("points");
                authoredDensity = density != null ? (float)density.GetValue(path) : 0;
            }
            public void SpawnBatch(float multiplier)
            {
                try { density.SetValue(path, authoredDensity * multiplier); batch.Invoke(path, null); }
                finally { density.SetValue(path, authoredDensity); }
            }
            public void SpawnSingle() => single.Invoke(path, singleArgs);
        }
        public void DebugRagdoll(Vector3 origin,Camera camera,bool allVisible)
        {
            TruckTaxiPedestrian nearest=null; float distance=float.PositiveInfinity;
            foreach(var ped in people)
            {
                if(ped==null || ped.IsRagdoll) continue;
                float d=(ped.transform.position-origin).sqrMagnitude;
                if(d<distance) { distance=d; nearest=ped; }
                if(!allVisible || camera==null) continue;
                var point=camera.WorldToViewportPoint(ped.transform.position+Vector3.up);
                if(point.z>0 && point.x>=0 && point.x<=1 && point.y>=0 && point.y<=1)
                    ped.TryStrike((ped.transform.position-origin).normalized*8,ped.transform.position+Vector3.up);
            }
            if(!allVisible && nearest!=null)
                nearest.TryStrike((nearest.transform.position-origin).normalized*8,nearest.transform.position+Vector3.up);
        }
        private void OnDrawGizmos()
        {
            if(!ShowColliders) return;
            Gizmos.color=Color.cyan;
            foreach(var ped in people) if(ped!=null)
                foreach(var collider in ped.GetComponentsInChildren<Collider>()) if(collider.enabled)
                    Gizmos.DrawWireCube(collider.bounds.center,collider.bounds.size);
        }
    }
}
