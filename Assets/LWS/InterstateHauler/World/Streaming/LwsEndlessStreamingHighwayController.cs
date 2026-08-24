using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(125)]
    [DisallowMultipleComponent]
    public sealed class LwsEndlessStreamingHighwayController : MonoBehaviour
    {
        private const string RuntimeRootName = "IH Endless Streaming Highway Runtime";
        private static readonly int[] MileageMilestones = { 10, 25, 50, 100, 250, 500 };

        [SerializeField] private bool endlessValidationEnabled = true;
        [SerializeField] private int behindSegments = LwsEndlessHighwayModel.DefaultBehindSegments;
        [SerializeField] private int aheadSegments = LwsEndlessHighwayModel.DefaultAheadSegments;
        [SerializeField] private int physicalChunkPoolSize = LwsEndlessHighwayModel.DefaultPhysicalChunkPoolSize;
        [SerializeField] private float roadAheadTargetMeters = LwsEndlessHighwayModel.DefaultRoadAheadTargetMeters;
        [SerializeField] private float emergencyRoadAheadThresholdMeters = LwsEndlessHighwayModel.DefaultRoadAheadEmergencyThresholdMeters;
        [SerializeField] private bool createLaneDebugLines = true;
        [SerializeField] private LwsRoadGraphProvider roadGraphProvider;

        private readonly List<SlotRuntime> _slots = new List<SlotRuntime>();
        private readonly List<LwsEndlessHighwaySlotSnapshot> _slotSnapshots = new List<LwsEndlessHighwaySlotSnapshot>();
        private ILwsWorldOriginService _originService;
        private ILwsNavigationService _navigationService;
        private LwsUtsHighwayTrafficController _trafficController;
        private Transform _runtimeRoot;
        private Material _asphaltMaterial;
        private int _currentLogicalSegmentIndex = -1;
        private int _lastRegisteredFirstSegmentIndex = -1;
        private int _nextMilestoneIndex;
        private long _lastAppliedOriginVersion = long.MinValue;
        private bool _initialSlotAssignmentComplete;
        private bool _unsafeLogged;

        public bool EndlessValidationEnabled => endlessValidationEnabled;
        public double LogicalSegmentLengthMeters => LwsEndlessHighwayModel.SegmentLengthMeters;
        public int PhysicalChunkPoolSize => Mathf.Max(MinimumPoolSize, physicalChunkPoolSize);
        public int CurrentLogicalSegmentIndex => Mathf.Max(0, _currentLogicalSegmentIndex);
        public string CurrentLogicalSegmentId => LwsEndlessHighwayModel.FormatSegmentId(CurrentLogicalSegmentIndex);
        public int HighestSegmentGenerated { get; private set; }
        public int LowestSegmentRetained { get; private set; }
        public float MetersOfRoadAvailableAhead { get; private set; }
        public float RoadAheadTargetMeters => roadAheadTargetMeters;
        public float EmergencyRoadAheadThresholdMeters => emergencyRoadAheadThresholdMeters;
        public bool RoadAheadUnsafe { get; private set; }
        public int ChunksRecycled { get; private set; }
        public int ChunkLoadFailures { get; private set; }
        public string LastStatus { get; private set; } = "Not initialized.";
        public string LastError { get; private set; } = string.Empty;
        public LwsRoadGraph LastGraph { get; private set; }
        public IReadOnlyList<LwsEndlessHighwaySlotSnapshot> SlotSnapshots => _slotSnapshots;

        private int MinimumPoolSize => Mathf.Max(3, Mathf.Max(0, behindSegments) + 1 + Mathf.Max(1, aheadSegments));

        private void Start()
        {
            ResolveServices();
            EnsureRuntimePrepared();
            RefreshNow();
        }

        private void Update()
        {
            if (!endlessValidationEnabled)
            {
                return;
            }

            ResolveServices();
            EnsureRuntimePrepared();
            RefreshForPlayerGlobalZ(ResolvePlayerGlobalPosition().z, false);
            LogMileageMilestoneIfNeeded(ResolvePlayerGlobalPosition().z);
        }

        private void OnDestroy()
        {
            ClearRuntimePool();
        }

        public void Configure(LwsRoadGraphProvider provider, LwsUtsHighwayTrafficController trafficController = null)
        {
            if (provider != null)
            {
                roadGraphProvider = provider;
            }

            if (trafficController != null)
            {
                _trafficController = trafficController;
            }
        }

        public void BindTrafficController(LwsUtsHighwayTrafficController trafficController)
        {
            _trafficController = trafficController;
        }

        public void RefreshNow()
        {
            ResolveServices();
            EnsureRuntimePrepared();
            RefreshForPlayerGlobalZ(ResolvePlayerGlobalPosition().z, true);
        }

        public void RefreshForPlayerGlobalZ(double playerGlobalZ, bool forceRebuild)
        {
            if (!endlessValidationEnabled)
            {
                return;
            }

            try
            {
                int current = LwsEndlessHighwayModel.GetSegmentIndex(playerGlobalZ);
                int first = Math.Max(0, current - Mathf.Max(0, behindSegments));
                int poolCount = PhysicalChunkPoolSize;
                bool segmentChanged = forceRebuild || current != _currentLogicalSegmentIndex || first != _lastRegisteredFirstSegmentIndex || _slots.Count != poolCount;
                _currentLogicalSegmentIndex = current;
                LowestSegmentRetained = first;
                HighestSegmentGenerated = first + poolCount - 1;
                MetersOfRoadAvailableAhead = LwsEndlessHighwayModel.CalculateMetersOfRoadAhead(playerGlobalZ, HighestSegmentGenerated);
                RoadAheadUnsafe = LwsEndlessHighwayModel.IsRoadAheadUnsafe(MetersOfRoadAvailableAhead, emergencyRoadAheadThresholdMeters);

                EnsureSlotCount(poolCount);
                AssignSlotWindow(first, segmentChanged);
                ApplySlotLocalPositions();

                if (segmentChanged)
                {
                    RegisterGraphWindow(first, poolCount);
                }

                if (RoadAheadUnsafe)
                {
                    if (!_unsafeLogged)
                    {
                        _unsafeLogged = true;
                        Debug.LogError($"STREAMING ROAD AHEAD UNSAFE: only {MetersOfRoadAvailableAhead:0} m ahead. Retaining current endless validation pool and attempting recovery.", this);
                    }

                    RegisterGraphWindow(first, poolCount);
                }
                else
                {
                    _unsafeLogged = false;
                }

                LastError = string.Empty;
                LastStatus = $"Endless highway active: {CurrentLogicalSegmentId}, road ahead {MetersOfRoadAvailableAhead:0} m.";
            }
            catch (Exception ex)
            {
                ChunkLoadFailures++;
                LastError = $"{ex.GetType().Name}: {ex.Message}";
                LastStatus = "Endless highway recovery failed.";
                if (!_unsafeLogged)
                {
                    _unsafeLogged = true;
                    Debug.LogError($"STREAMING ROAD AHEAD UNSAFE: {LastError}", this);
                }
            }
        }

        public bool TryRequestDestinationMilesAhead(float milesAhead, out string message)
        {
            ResolveServices();
            if (_navigationService == null)
            {
                message = "Navigation service is missing.";
                return false;
            }

            float miles = Mathf.Max(0.25f, milesAhead);
            LwsWorldPositionD player = ResolvePlayerGlobalPosition();
            double destinationZ = player.z + miles * LwsEndlessHighwayModel.MetersPerMile;
            int first = Math.Max(0, LwsEndlessHighwayModel.GetSegmentIndex(player.z) - 1);
            int destinationSegment = LwsEndlessHighwayModel.GetSegmentIndex(destinationZ);
            int count = Mathf.Clamp(destinationSegment - first + 2, 2, 64);
            LwsRoadGraph routeGraph = LwsEndlessHighwayModel.BuildRoadGraph(first, count);
            Vector3 origin = player.ToVector3();
            Vector3 destination = new Vector3(LwsEndlessHighwayModel.CarriagewayOffsetMeters, LwsEndlessHighwayModel.RoadSurfaceY, (float)destinationZ);
            LwsRouteResult result = _navigationService.SetDestination(destination, origin, routeGraph);
            if (result != null && result.succeeded)
            {
                message = $"Endless highway route set {miles:0.#} mi ahead.";
                return true;
            }

            message = result?.message ?? "Endless highway route request failed.";
            return false;
        }

        public static Mesh CreateStraightRoadRibbonMesh(float lengthMeters, float widthMeters)
        {
            var samples = new[]
            {
                new Vector3(0f, LwsEndlessHighwayModel.RoadSurfaceY, 0f),
                new Vector3(0f, LwsEndlessHighwayModel.RoadSurfaceY, Mathf.Max(1f, lengthMeters))
            };

            return CreateRibbonMesh(samples, Mathf.Max(1f, widthMeters));
        }

        private void EnsureRuntimePrepared()
        {
            if (roadGraphProvider == null)
            {
                roadGraphProvider = GetComponent<LwsRoadGraphProvider>();
                if (roadGraphProvider == null)
                {
                    roadGraphProvider = gameObject.AddComponent<LwsRoadGraphProvider>();
                }
            }

            if (_runtimeRoot == null)
            {
                GameObject existing = GameObject.Find(RuntimeRootName);
                GameObject root = existing != null ? existing : new GameObject(RuntimeRootName);
                root.transform.SetParent(null, false);
                _runtimeRoot = root.transform;
            }

            if (_asphaltMaterial == null)
            {
                _asphaltMaterial = CreateRuntimeMaterial("IH Endless Streamed Asphalt", new Color(0.06f, 0.062f, 0.058f, 1f));
            }
        }

        private void EnsureSlotCount(int poolCount)
        {
            poolCount = Mathf.Max(MinimumPoolSize, poolCount);
            while (_slots.Count < poolCount)
            {
                _slots.Add(CreateSlot(_slots.Count));
            }

            while (_slots.Count > poolCount)
            {
                SlotRuntime slot = _slots[_slots.Count - 1];
                if (slot.Root != null)
                {
                    Destroy(slot.Root);
                }

                _slots.RemoveAt(_slots.Count - 1);
            }
        }

        private SlotRuntime CreateSlot(int slotIndex)
        {
            var root = new GameObject($"POOL_CHUNK_{slotIndex:00}_UNASSIGNED");
            root.transform.SetParent(_runtimeRoot, false);
            CreateRoadRibbon(root.transform, "NB Road", LwsEndlessHighwayModel.CarriagewayOffsetMeters, LwsRoadDirection.Northbound);
            CreateRoadRibbon(root.transform, "SB Road", -LwsEndlessHighwayModel.CarriagewayOffsetMeters, LwsRoadDirection.Southbound);
            LwsInterstateRoadsideBuilder.BuildStraightPairedInterstate(
                root.transform,
                $"IH_ENDLESS_SLOT_{slotIndex:00}",
                0f,
                (float)LwsEndlessHighwayModel.SegmentLengthMeters);
            return new SlotRuntime(slotIndex, root);
        }

        private void AssignSlotWindow(int firstSegmentIndex, bool countRecycle)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                int segmentIndex = firstSegmentIndex + i;
                SlotRuntime slot = _slots[i];
                if (slot.LogicalSegmentIndex != segmentIndex && _initialSlotAssignmentComplete && countRecycle)
                {
                    ChunksRecycled++;
                }

                slot.LogicalSegmentIndex = segmentIndex;
                slot.LogicalSegmentId = LwsEndlessHighwayModel.FormatSegmentId(segmentIndex);
                slot.GlobalSegmentStartZ = LwsEndlessHighwayModel.GetSegmentStartZ(segmentIndex);
                if (slot.Root != null)
                {
                    slot.Root.name = $"POOL_CHUNK_{slot.PhysicalSlotIndex:00}_{slot.LogicalSegmentId}";
                }
            }

            _initialSlotAssignmentComplete = true;
        }

        private void ApplySlotLocalPositions()
        {
            LwsWorldPositionD origin = _originService != null ? _originService.CurrentOriginOffset : LwsWorldPositionD.Zero;
            long version = _originService != null ? _originService.OriginVersion : 0;
            _slotSnapshots.Clear();
            for (int i = 0; i < _slots.Count; i++)
            {
                SlotRuntime slot = _slots[i];
                Vector3 local = LwsEndlessHighwayModel.CalculateLocalSegmentPosition(slot.LogicalSegmentIndex, origin);
                if (slot.Root != null)
                {
                    slot.Root.transform.position = local;
                }

                _slotSnapshots.Add(new LwsEndlessHighwaySlotSnapshot(
                    slot.PhysicalSlotIndex,
                    slot.LogicalSegmentIndex,
                    slot.LogicalSegmentId,
                    slot.GlobalSegmentStartZ,
                    local));
            }

            _lastAppliedOriginVersion = version;
        }

        private void RegisterGraphWindow(int firstSegmentIndex, int poolCount)
        {
            LastGraph = LwsEndlessHighwayModel.BuildRoadGraph(firstSegmentIndex, poolCount);
            roadGraphProvider.SetGraph(LastGraph, true);
            _lastRegisteredFirstSegmentIndex = firstSegmentIndex;
            _trafficController?.ForceRebuildFromGraph(roadGraphProvider, LastGraph, "Endless highway segment window recycled.");
        }

        private void CreateRoadRibbon(Transform parent, string name, float xOffset, LwsRoadDirection direction)
        {
            Mesh mesh = CreateStraightRoadRibbonMesh((float)LwsEndlessHighwayModel.SegmentLengthMeters, LwsEndlessHighwayModel.CarriagewayWidthMeters);
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(xOffset, 0f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = _asphaltMaterial;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure("IH_ENDLESS_TEST", "IH_ENDLESS_TEST_PRESENTATION", LwsRoadSurfaceType.AsphaltInterstate, "Endless validation interstate presentation");

            if (createLaneDebugLines)
            {
                CreateLaneDebugLines(go.transform, direction);
            }
        }

        private void CreateLaneDebugLines(Transform parent, LwsRoadDirection direction)
        {
            Material material = CreateRuntimeMaterial(
                direction == LwsRoadDirection.Southbound ? "IH Endless Lane Yellow" : "IH Endless Lane Cyan",
                direction == LwsRoadDirection.Southbound ? new Color(1f, 0.78f, 0.12f, 1f) : new Color(0.15f, 0.75f, 1f, 1f));

            for (int lane = 0; lane < 2; lane++)
            {
                float offset = ((2 - 1) * -0.5f + lane) * LwsEndlessHighwayModel.LaneWidthMeters;
                GameObject lineObject = new GameObject($"Lane {lane + 1} Center Debug");
                lineObject.transform.SetParent(parent, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.widthMultiplier = 0.14f;
                line.positionCount = 2;
                line.useWorldSpace = false;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.SetPosition(0, new Vector3(offset, 0.07f, 0f));
                line.SetPosition(1, new Vector3(offset, 0.07f, (float)LwsEndlessHighwayModel.SegmentLengthMeters));
            }
        }

        private LwsWorldPositionD ResolvePlayerGlobalPosition()
        {
            if (_originService != null)
            {
                return _originService.PlayerGlobalPosition;
            }

            LwsPlayerTruck truck = FindFirstObjectByType<LwsPlayerTruck>();
            return truck != null ? LwsWorldPositionD.FromVector3(truck.transform.position) : LwsWorldPositionD.FromVector3(transform.position);
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance != null &&
                LwsApplicationBootstrap.Instance.Registry != null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _navigationService);
            }

            if (_trafficController == null)
            {
                _trafficController = FindFirstObjectByType<LwsUtsHighwayTrafficController>();
            }

            if (_lastAppliedOriginVersion != long.MinValue &&
                _originService != null &&
                _originService.OriginVersion != _lastAppliedOriginVersion)
            {
                ApplySlotLocalPositions();
            }
        }

        private void LogMileageMilestoneIfNeeded(double playerGlobalZ)
        {
            if (_nextMilestoneIndex >= MileageMilestones.Length)
            {
                return;
            }

            double miles = playerGlobalZ / LwsEndlessHighwayModel.MetersPerMile;
            if (miles < MileageMilestones[_nextMilestoneIndex])
            {
                return;
            }

            Debug.Log($"Endless highway validation milestone reached: {MileageMilestones[_nextMilestoneIndex]} miles.", this);
            _nextMilestoneIndex++;
        }

        private void ClearRuntimePool()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Root != null)
                {
                    Destroy(_slots[i].Root);
                }
            }

            _slots.Clear();
            _slotSnapshots.Clear();
        }

        private static Mesh CreateRibbonMesh(IReadOnlyList<Vector3> samples, float width)
        {
            int count = samples.Count;
            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            var triangles = new int[(count - 1) * 6];
            float distance = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 forward = i < count - 1 ? samples[i + 1] - samples[i] : samples[i] - samples[i - 1];
                forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                vertices[i * 2] = samples[i] - right * width * 0.5f;
                vertices[i * 2 + 1] = samples[i] + right * width * 0.5f;
                if (i > 0)
                {
                    distance += Vector3.Distance(samples[i - 1], samples[i]);
                }

                uvs[i * 2] = new Vector2(0f, distance / 10f);
                uvs[i * 2 + 1] = new Vector2(1f, distance / 10f);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int vi = i * 2;
                int ti = i * 6;
                triangles[ti] = vi;
                triangles[ti + 1] = vi + 2;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + 2;
                triangles[ti + 5] = vi + 3;
            }

            var mesh = new Mesh { name = "IH Endless Highway Ribbon Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateRuntimeMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private sealed class SlotRuntime
        {
            public SlotRuntime(int physicalSlotIndex, GameObject root)
            {
                PhysicalSlotIndex = physicalSlotIndex;
                Root = root;
                LogicalSegmentIndex = -1;
                LogicalSegmentId = string.Empty;
            }

            public int PhysicalSlotIndex { get; }
            public GameObject Root { get; }
            public int LogicalSegmentIndex { get; set; }
            public string LogicalSegmentId { get; set; }
            public double GlobalSegmentStartZ { get; set; }
        }
    }
}
