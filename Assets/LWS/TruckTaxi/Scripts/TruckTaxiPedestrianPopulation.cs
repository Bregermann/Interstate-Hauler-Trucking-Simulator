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
        private bool initialized;
        private bool appliedWobbleStyle;
        private bool baselineValidation;
        private float nextMaintenance;
        private int pathCursor;
        private int serial;
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
                // UTS distributes its batch along the authored sidewalk loops. Do not stack 144 people at four entrances.
                float ratio = BaselineActiveCount > 0 ? (float)TargetCount / BaselineActiveCount : 0;
                if (!Mathf.Approximately(ratio, 1))
                {
                    ClearPeople();
                    if (ratio > 0)
                        foreach (var entry in bindings)
                        {
                            entry.Value.SpawnBatch(ratio);
                            Bind(entry.Key, TargetCount, true);
                        }
                }
                Debug.Log($"TAXI PEDESTRIANS: original cap {maximumPeople}, observed UTS startup {BaselineActiveCount}, multiplier {(baselineValidation ? 1 : densityProfile.pedestrianDensityMultiplier)}, target/cap {TargetCount}/{densityProfile.maximumActivePedestrians}, spawned {ActiveCount}.", this);
            }
            nextMaintenance = Time.time + .5f;
        }
        private void Bind(Component path, int limit, bool dense)
        {
            var parent=bindings[path].Parent;
            if(parent==null) return;
            foreach(Transform child in parent.transform)
            {
                if(child.GetComponent<TruckTaxiPedestrian>()!=null) continue;
                if (people.Count >= limit || (pathAreas.TryGetValue(path, out var area) && !area.Allows(child.position, roadLanes)) ||
                    (dense && !densityProfile.AllowsSpawn(child.position, transform.position)))
                { child.gameObject.SetActive(false); Destroy(child.gameObject); RejectedSpawnAttempts++; continue; }
                child.gameObject.layer=0;
                if(child.GetComponent<Rigidbody>()==null) child.gameObject.AddComponent<Rigidbody>();
                var ped=child.gameObject.AddComponent<TruckTaxiPedestrian>();
                ped.settings=settings;
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
            }
        }
        public void SpawnOne()
        {
            TrySpawnOne();
        }
        private bool TrySpawnOne()
        {
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
            float clearance = Mathf.Max(.25f, densityProfile.pedestrianSpawnClearance);
            foreach (var ped in people)
                if (ped != null && (ped.transform.position - position).sqrMagnitude < clearance * clearance) return false;
            var player = TruckTaxiBootstrap.Instance?.Player;
            return player == null || (player.transform.position - position).sqrMagnitude >= 25;
        }
        public void ResetPopulation()
        {
            ClearPeople(); initialized = false; pathCursor = 0; Initialize();
        }
        private void ClearPeople()
        {
            foreach(var ped in people)
                if(ped!=null) { ped.Expired-=Retire; ped.gameObject.SetActive(false); Destroy(ped.gameObject); }
            people.Clear(); origins.Clear(); fallen.Clear(); presentations.Clear();
        }
        private void Retire(TruckTaxiPedestrian ped)
        {
            if(!origins.TryGetValue(ped,out var path)) return;
            ped.Expired-=Retire; people.Remove(ped); origins.Remove(ped);
            presentations.Remove(ped);
            fallen.Remove(ped); RetiredCount++;
            ped.gameObject.SetActive(false); Destroy(ped.gameObject);
            // Dense replacement is deferred to the bounded maintenance pass, including simultaneous mass impacts.
            if(!isActiveAndEnabled || path==null || people.Count>=TargetCount || DensityEnabled) return;
            bindings[path].SpawnSingle(); Bind(path, TargetCount, false);
        }
        private void Update()
        {
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
