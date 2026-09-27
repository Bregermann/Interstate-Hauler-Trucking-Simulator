using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LWS.InterstateHauler;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiRegion
    {
        public string id, displayName, sceneName;
        public Bounds bounds;
        public bool population = true;
        public float Distance(Vector3 position)
        { position.y = bounds.center.y; return Vector3.Distance(position, bounds.ClosestPoint(position)); }
    }

    // Taxi metadata and anchor binding; existing LWS policy and Scene Streamer own scheduling/loading.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRegionalWorld : MonoBehaviour
    {
        public TruckTaxiRegion[] regions = Array.Empty<TruckTaxiRegion>();
        public LwsWorldStreamingManifest streamingManifest;
        [Min(100)] public float preloadDistance = 450;
        [Min(1)] public float forwardPreloadSeconds = 14;
        [Min(100)] public float unloadDistance = 850;
        [Min(.1f)] public float policyInterval = .5f;
        public bool InitialWorldReady { get; private set; }
        public string CurrentRegion { get; private set; } = "Preparing world";
        public event Action AvailabilityChanged;
        public string LastFailure { get; private set; }
        private LwsSceneStreamerAdapter adapter;
        private ILwsWorldStreamingService streaming;
        private LwsWorldStreamingManifest runtimeManifest;
        private LwsWorldStreamingPolicy runtimePolicy;
        private Transform player;
        private Rigidbody body;
        private float nextPolicy, nextSample, frameSeconds;
        private ProfilerRecorder mainThread, physics;
        private int activeBodies;
        private Vector3? preparationPosition;
        private readonly StringBuilder diagnostics = new StringBuilder();

        public string Diagnostics
        {
            get
            {
                diagnostics.Clear().Append("REGION: ").Append(CurrentRegion).Append("\nLOADED: ");
                foreach (var region in regions) if (Loaded(region)) diagnostics.Append(region.displayName).Append("; ");
                diagnostics.Append("\nPRELOAD / PENDING: ").Append(adapter != null ? adapter.PendingOperationCount : 0)
                    .Append("  SPEED: ").Append(body != null ? (body.linearVelocity.magnitude * 2.23694f).ToString("F0") : "0").Append(" MPH")
                    .Append("\nFPS: ").Append((1 / Mathf.Max(.001f, frameSeconds)).ToString("F1"))
                    .Append("  MAIN: ").Append(MainThreadMs.ToString("F1")).Append(" ms  PHYSICS: ").Append(PhysicsMs.ToString("F1"))
                    .Append(" ms\nACTIVE RIGIDBODIES: ").Append(activeBodies);
                if (!string.IsNullOrEmpty(LastFailure)) diagnostics.Append("\n").Append(LastFailure);
                return diagnostics.ToString();
            }
        }
        public double MainThreadMs => mainThread.Valid ? mainThread.LastValue / 1000000d : 0;
        public double PhysicsMs => physics.Valid ? physics.LastValue / 1000000d : 0;
        public int ActiveBodies => activeBodies;
        public int LoadedCount { get { int count = 0; foreach (var r in regions) if (Loaded(r)) count++; return count; } }
        private void Awake()
        {
            adapter = GetComponent<LwsSceneStreamerAdapter>() ?? gameObject.AddComponent<LwsSceneStreamerAdapter>();
            adapter.SceneLoaded += Changed;
            adapter.SceneUnloaded += Changed;
            adapter.SceneOperationFailed += Failed;
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            physics = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", 1);
        }
        public IEnumerator PrepareInitialWorld(Vector3 position)
        {
            LastFailure = null;
            if (!ConfigureStreaming()) yield break;
            preparationPosition = position;
            Refresh(position, Vector3.zero);
            float deadline = Time.realtimeSinceStartup + 60;
            while (!IsPositionAvailable(position) && string.IsNullOrEmpty(LastFailure) && Time.realtimeSinceStartup < deadline)
            {
                Refresh(position, Vector3.zero);
                yield return null;
            }
            InitialWorldReady = IsPositionAvailable(position);
            preparationPosition = null;
            if (!InitialWorldReady) Debug.LogError("Truck Taxi world preparation failed: " + (LastFailure ?? "region load timeout"), this);
        }
        public void BindPlayer(Transform target)
        { player = target; body = target != null ? target.GetComponent<Rigidbody>() : null; }
        public string ResolveRegion(Vector3 position)
        {
            foreach (var region in regions)
                if (region.population && region.Distance(position) <= .01f) return region.id;
            return string.Empty;
        }
        public bool IsPositionAvailable(Vector3 position)
        {
            foreach (var region in regions)
                if (region.population && region.Distance(position) <= .01f && Loaded(region)) return true;
            return regions.Length == 0;
        }
        public bool Loaded(TruckTaxiRegion region) => SceneManager.GetSceneByName(region.sceneName).isLoaded;
        public void RegisterMapMetadata(TruckTaxiMapMarkers markers)
        {
            if (markers == null) return;
            foreach (var region in regions)
                markers.RegisterRegionalPoint(region.id, region.displayName, "Region", region.bounds.center, TruckTaxiMapMarkerType.ScenicStop);
            markers.RegisterRegionalPoint("taxi.river", "Regional River", "River", new Vector3(2000,0,200), TruckTaxiMapMarkerType.ScenicStop);
            markers.RegisterRegionalPoint("taxi.bridge", "River Highway Bridge", "Bridge", new Vector3(2000,8,0), TruckTaxiMapMarkerType.ScenicStop);
            foreach (var stop in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
                if (stop.gameObject.scene == gameObject.scene)
                    markers.RegisterRegionalPoint(stop.locationId, stop.locationName, stop.locationType.ToString(), stop.StopPosition,
                        stop.locationType == TaxiLocationType.Transit ? TruckTaxiMapMarkerType.SpecialEvent : TruckTaxiMapMarkerType.FoodStop, true);
            foreach (var service in TruckTaxiServicePoint.Points)
                if (service != null && service.gameObject.scene == gameObject.scene)
                    markers.RegisterRegionalPoint(service.stableId, service.displayName, service.capabilities.ToString(), service.Position, TruckTaxiMapMarkerType.FoodStop, true);
        }
        private void Update()
        {
            frameSeconds = Mathf.Lerp(frameSeconds, Time.unscaledDeltaTime, .05f);
            if (player == null) return;
            if (Time.unscaledTime >= nextPolicy)
            {
                nextPolicy = Time.unscaledTime + policyInterval;
                Refresh(player.position, body != null ? body.linearVelocity : Vector3.zero);
            }
            // Diagnostics scan is development-only and bounded to once per five seconds, never per frame.
            if (Debug.isDebugBuild && Time.unscaledTime >= nextSample)
            {
                nextSample = Time.unscaledTime + 5;
                activeBodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length;
            }
        }
        public void Refresh(Vector3 position, Vector3 velocity)
        {
            if (streaming == null) return;
            CurrentRegion = ResolveRegion(position);
            runtimePolicy.loadAheadDistanceMeters = preloadDistance + Mathf.Min(1200, velocity.magnitude * forwardPreloadSeconds);
            runtimePolicy.unloadDistanceMeters = Mathf.Max(unloadDistance, runtimePolicy.loadAheadDistanceMeters + 200);
            Vector3 anchor = preparationPosition ?? position;
            Vector3 heading = velocity.sqrMagnitude > 1 ? velocity.normalized : player != null ? player.forward : Vector3.right;
            // During service/tow preparation protect the old player region until the move is committed.
            streaming.UpdateStreamingAnchor(new LwsWorldStreamingAnchorState(anchor, heading, velocity.magnitude,
                preparationPosition.HasValue && player != null, player != null ? player.position : anchor));
            streaming.Tick(policyInterval);
        }
        private bool ConfigureStreaming()
        {
            if (streaming != null) return true;
            if (streamingManifest == null || LwsApplicationBootstrap.Instance == null ||
                !LwsApplicationBootstrap.Instance.Registry.TryGet(out streaming))
            { LastFailure = "Regional streaming manifest or registered LWS streaming service is unavailable."; return false; }
            runtimeManifest = Instantiate(streamingManifest);
            runtimePolicy = Instantiate(streamingManifest.policy);
            runtimeManifest.policy = runtimePolicy;
            var validation = streaming.Configure(runtimeManifest, adapter);
            if (!validation.IsValid) { LastFailure = validation.Summary; streaming = null; return false; }
            return true;
        }
        private void Changed(string sceneName)
        {
            foreach (var region in regions)
                if (region.sceneName == sceneName) { AvailabilityChanged?.Invoke(); break; }
        }
        private void Failed(string sceneName, string reason) => LastFailure = sceneName + ": " + reason;
        private void OnDestroy()
        {
            if (adapter != null)
            { adapter.SceneLoaded -= Changed; adapter.SceneUnloaded -= Changed; adapter.SceneOperationFailed -= Failed; }
            mainThread.Dispose(); physics.Dispose();
            if (runtimeManifest != null) Destroy(runtimeManifest);
            if (runtimePolicy != null) Destroy(runtimePolicy);
        }
    }
}
