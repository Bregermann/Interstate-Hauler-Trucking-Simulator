using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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
        private readonly Dictionary<TruckTaxiPedestrian, WalkAgent> walkers = new Dictionary<TruckTaxiPedestrian, WalkAgent>();
        private readonly List<WalkAgent> walkerSchedule = new List<WalkAgent>();
        private readonly Dictionary<TruckTaxiIntersection, int> activeCrossings = new Dictionary<TruckTaxiIntersection, int>();
        private readonly Stack<int> freeRows = new Stack<int>();
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
        public int PresentationCount => presentations.Count;
        public int GraphNodeCount => walkGraph?.Count ?? 0;
        public int GraphWalkableSpawnCount => walkGraph?.WalkableSpawnCount ?? 0;
        public int GraphRoadSegmentCount => walkGraph?.RoadSegmentCount ?? 0;
        public int GraphWalkerCount => walkers.Count;
        public int LegalCrossingCount(TruckTaxiIntersection intersection) =>
            intersection != null && activeCrossings.TryGetValue(intersection, out int count) ? count : 0;
        public int ReducedShadowCount { get; private set; }
        public IReadOnlyList<TruckTaxiPedestrian> People => people;
        public bool ShowColliders { get; set; }
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
                access.SpawnBatch(1);
                Bind(path, int.MaxValue, false);
            }
            BaselineActiveCount = ActiveCount;
            appliedWobbleStyle = useWobblePeople;
            initialized = true;
            if (DensityEnabled)
            {
                ClearPeople();
                walkGraph = new TruckTaxiPedestrianWalkGraph(roadLanes, pedestrianAreas, peoplePaths,
                    signalIntersections, TruckTaxiBootstrap.Instance?.roadGraph?.Graph,
                    System.Array.FindAll(FindObjectsByType<TruckTaxiWorldAnchor>(FindObjectsSortMode.None),
                        anchor => anchor != null && anchor.gameObject.scene == gameObject.scene),
                    System.Array.FindAll(FindObjectsByType<TruckTaxiSurface>(FindObjectsSortMode.None),
                        surface => surface != null && surface.gameObject.scene == gameObject.scene),
                    densityProfile.spawnRadius, transform.position);
                if (walkGraph != null && walkGraph.HasRoadBounds && walkGraph.WalkableSpawnCount > 0 && TargetCount > 0)
                {
                    CreateRuntimePath();
                    for (int i = 0; i < Mathf.Min(24, TargetCount); i++)
                        if (!SpawnGraphOne()) break;
                }
                else
                {
                    Debug.LogWarning("TAXI PEDESTRIANS: no grounded walk graph nodes; retaining authored UTS paths.", this);
                    walkGraph = null;
                    float ratio = BaselineActiveCount > 0 ? (float)TargetCount / BaselineActiveCount : 0;
                    if (ratio > 0)
                        foreach (var entry in bindings)
                        {
                            entry.Value.SpawnBatch(ratio);
                            Bind(entry.Key, TargetCount, true);
                        }
                }
                Debug.Log($"TAXI PEDESTRIANS: UTS baseline {BaselineActiveCount}, graph nodes {GraphNodeCount}, target {TargetCount}, initial {ActiveCount}.", this);
            }
            nextMaintenance = Time.time + .5f;
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
            if (people.Count >= limit || (graphNode < 0 && pathAreas.TryGetValue(path, out var area) && !area.Allows(spawnPosition, roadLanes)) ||
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
            origins.Add(ped,path); ped.Expired+=Retire;
            var presentation = new TruckTaxiPopulationPresentation(ped.gameObject, true);
            presentations.Add(ped, presentation);
            presentation.Refresh(DensityEnabled ? densityProfile : null, PresentationObserver());
            if (graphNode >= 0) BindGraphWalker(ped, graphNode);
        }
        public void SpawnOne()
        {
            TrySpawnOne();
        }
        private bool TrySpawnOne()
        {
            if (walkGraph != null) return SpawnGraphOne();
            if (!Ready || people.Count >= TargetCount || activePaths.Count == 0) return false;
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
            if (!densityProfile.AllowsSpawn(position, transform.position)) return false;
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
            if (walkGraph == null || runtimePath == null || people.Count >= TargetCount || bindings.Count == 0) return false;
            for (int attempt = 0; attempt < Mathf.Min(walkGraph.Count, 32); attempt++)
            {
                int node = walkGraph.NextSpawnNode();
                if (node < 0 || !CanSpawnAt(walkGraph[node])) { RejectedSpawnAttempts++; continue; }
                var source = activePaths[pathCursor++ % activePaths.Count];
                if (source == null || !bindings.TryGetValue(source, out var access)) continue;
                int before = people.Count;
                var parent = access.Parent;
                if (parent == null) continue;
                int childCount = parent.transform.childCount;
                access.SpawnSingle();
                if (parent.transform.childCount > childCount)
                    Bind(source, TargetCount, true, node, parent.transform.GetChild(parent.transform.childCount - 1));
                if (people.Count > before) return true;
            }
            return false;
        }
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
            if (runtimePath != null) Destroy(runtimePath.gameObject);
            runtimePath = null; runtimePoints = null; walkGraph = null;
            initialized = false; pathCursor = 0; Initialize();
        }
        private void ClearPeople()
        {
            foreach(var ped in people)
                if(ped!=null) { ped.Expired-=Retire; ped.gameObject.SetActive(false); Destroy(ped.gameObject); }
            people.Clear(); origins.Clear(); fallen.Clear(); presentations.Clear(); walkers.Clear();
            walkerSchedule.Clear(); activeCrossings.Clear(); walkerCursor = 0; freeRows.Clear(); nextRow = 0;
        }
        private void Retire(TruckTaxiPedestrian ped)
        {
            if(!origins.TryGetValue(ped,out var path)) return;
            ped.Expired-=Retire; people.Remove(ped); origins.Remove(ped);
            presentations.Remove(ped);
            if (walkers.TryGetValue(ped, out var walker))
            { EndCrossing(walker); freeRows.Push(walker.Row); walkers.Remove(ped); walkerSchedule.Remove(walker); }
            fallen.Remove(ped); RetiredCount++;
            ped.gameObject.SetActive(false); Destroy(ped.gameObject);
            // Dense replacement is deferred to the bounded maintenance pass, including simultaneous mass impacts.
            if(!isActiveAndEnabled || path==null || people.Count>=TargetCount || DensityEnabled) return;
            bindings[path].SpawnSingle(); Bind(path, TargetCount, false);
        }
        private void Update()
        {
            if (Ready && walkGraph != null) TickWalkers();
            if (!Ready || Time.time < nextMaintenance) return;
            if (appliedWobbleStyle != useWobblePeople) SetWobblePeopleVisible(useWobblePeople);
            nextMaintenance = Time.time + (DensityEnabled ? Mathf.Max(.1f, densityProfile.maintenanceInterval) : .5f);
            if (!DensityEnabled)
            {
                foreach (var presentation in presentations.Values) presentation.Restore();
                ReducedShadowCount = 0; return;
            }
            Vector3 observer = PresentationObserver(); ReducedShadowCount = 0;
            for (int i = people.Count - 1; i >= 0; i--)
            {
                var ped = people[i];
                if (ped == null)
                {
                    if (!ReferenceEquals(ped, null)) { origins.Remove(ped); presentations.Remove(ped); }
                    people.RemoveAt(i); continue;
                }
                if (people.Count > TargetCount || ped.transform.position.y < -5 || !densityProfile.AllowsPresence(ped.transform.position, transform.position))
                { Retire(ped); continue; }
                if (ped.IsRagdoll && !fallen.Contains(ped)) fallen.Add(ped);
                if (presentations.TryGetValue(ped, out var presentation))
                {
                    presentation.Refresh(densityProfile, observer);
                    if (presentation.ShadowsReduced) ReducedShadowCount++;
                }
            }
            fallen.RemoveAll(p => p == null || !p.IsRagdoll);
            while (fallen.Count > Mathf.Max(1, densityProfile.maximumActiveRagdolls))
                Retire(fallen[0]);
            for (int i = 0; i < Mathf.Max(1, densityProfile.maximumSpawnsPerPass) && people.Count < TargetCount; i++)
                if (!TrySpawnOne()) break;
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
