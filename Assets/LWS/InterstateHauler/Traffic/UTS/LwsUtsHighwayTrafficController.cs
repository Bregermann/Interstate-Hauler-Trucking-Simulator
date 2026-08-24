using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsUtsHighwayTrafficController : MonoBehaviour
    {
        private const string RuntimeRootName = "IH UTS Highway Traffic Runtime";
        private const string LanesRootName = "Lanes";
        private const string VehiclesRootName = "Vehicles";
        private const float FirstSpawnDelaySeconds = 1f;

        private static readonly string[] DefaultEditorTrafficPrefabPaths =
        {
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_1.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_3.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_5.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Jeep.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Taxi.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_1.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_2.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/City_bus.prefab"
        };

        [SerializeField] private LwsRoadGraphProvider roadGraphProvider;
        [SerializeField] private LwsUtsTrafficProfile validationTrafficProfile;
        [SerializeField] private GameObject[] trafficPrefabs;
        [SerializeField] private LwsTrafficSpawnPolicy spawnPolicy = new LwsTrafficSpawnPolicy();
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Transform playerOverride;
        [SerializeField] private int initializationRetryFrames = 30;
        [SerializeField] private float initializationRetryIntervalSeconds = 0.25f;

        private readonly List<LwsTrafficLaneDefinition> _lanes = new List<LwsTrafficLaneDefinition>();
        private readonly Dictionary<string, Component> _utsPaths = new Dictionary<string, Component>();
        private readonly List<GameObject> _activeVehicles = new List<GameObject>();
        private readonly LwsUtsTrafficApi _utsApi = new LwsUtsTrafficApi();
        private ILwsWorldOriginService _originService;
        private ILwsGameClockService _gameClockService;
        private ILwsTrafficDemandService _trafficDemandService;
        private Transform _trafficRoot;
        private Transform _lanesRoot;
        private Transform _vehiclesRoot;
        private Transform _playerTransform;
        private float _nextSpawnTime;
        private float _nextPlayerResolveTime;
        private float _nextInitializationRetryTime;
        private int _initializationAttempts;
        private int _laneCursor;
        private int _prefabCursor;
        private int _totalSpawned;
        private float _lastSpawnSeconds = -1f;
        private float _lastCleanupSeconds = -1f;
        private bool _initialized;
        private bool _graphAvailable;
        private bool _diagnosticsLogged;
        private bool _prefabSupportLogged;
        private bool _failureLogged;
        private bool _configurationFailedPermanently;
        private bool _originEventsSubscribed;
        private string _lastMessage = "Not initialized.";
        private string _lastSpawnResult = "No spawn attempted.";
        private string _lastError = string.Empty;

        public IReadOnlyList<LwsTrafficLaneDefinition> Lanes => _lanes;
        public LwsTrafficRuntimeStats Stats => new LwsTrafficRuntimeStats(_utsPaths.Count, _activeVehicles.Count, _totalSpawned, _lastMessage);
        public string UtsAvailability => _utsApi.AvailabilitySummary;
        public string UtsTypeReport => _utsApi.TypeAvailabilityReport;
        public bool Initialized => _initialized;
        public bool UtsAvailable => _utsApi.IsAvailable;
        public bool GraphAvailable => _graphAvailable;
        public int GeneratedLaneCount => _utsPaths.Count;
        public int LaneCandidateCount => _lanes.Count;
        public int SpawnablePrefabCount => CountPrefabs(trafficPrefabs);
        public int MaxActiveVehicles => Mathf.Max(spawnPolicy != null ? Mathf.Max(0, spawnPolicy.maxActiveVehicles) : 0, DemandSnapshot.Valid ? DemandSnapshot.MaximumActive : 0);
        public int TargetActiveVehicles => DemandSnapshot.Valid ? DemandSnapshot.SmoothedTargetActive : MaxActiveVehicles;
        public int MinimumNearbyTraffic => DemandSnapshot.Valid ? DemandSnapshot.MinimumNearby : 0;
        public int NearbyTrafficVehicles => DemandSnapshot.Valid ? DemandSnapshot.NearbyActual : 0;
        public float EffectiveSpawnIntervalSeconds => DemandSnapshot.Valid ? DemandSnapshot.SpawnIntervalSeconds : spawnPolicy != null ? spawnPolicy.spawnIntervalSeconds : 4f;
        public LwsTrafficDemandSnapshot DemandSnapshot { get; private set; } = LwsTrafficDemandSnapshot.Empty;
        public LwsTrafficDensityTier DensityTier => spawnPolicy != null ? spawnPolicy.densityTier : LwsTrafficDensityTier.Off;
        public string LastSpawnResult => _lastSpawnResult;
        public string LastError => _lastError;
        public string RuntimeHierarchyPath => _trafficRoot != null ? _trafficRoot.name : RuntimeRootName;

        private void Start()
        {
            ResolveOriginService();
            if (buildOnStart)
            {
                TryInitializeFromGraph(roadGraphProvider, roadGraphProvider != null ? roadGraphProvider.Graph : null, true);
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                TryRetryInitialization();
                return;
            }

            if (spawnPolicy == null || spawnPolicy.densityTier == LwsTrafficDensityTier.Off)
            {
                return;
            }

            ResolvePlayerTransformThrottled();
            PruneNullVehicles();
            DespawnDistantVehicles();
            RefreshTrafficDemand(Time.deltaTime);

            if (_activeVehicles.Count >= MaxActiveVehicles ||
                (_activeVehicles.Count >= TargetActiveVehicles && NearbyTrafficVehicles >= MinimumNearbyTraffic) ||
                Time.time < _nextSpawnTime)
            {
                return;
            }

            _nextSpawnTime = Time.time + Mathf.Max(0.25f, EffectiveSpawnIntervalSeconds);
            TrySpawnTrafficVehicle(out _);
        }

        private void OnDestroy()
        {
            if (_originService != null && _originEventsSubscribed)
            {
                _originService.OriginShiftCompleted -= HandleOriginShiftCompleted;
                _originEventsSubscribed = false;
            }

            UnregisterActiveTraffic();
        }

        public void SetTrafficPrefabsForValidation(GameObject[] prefabs)
        {
            if (prefabs != null && prefabs.Length > 0)
            {
                trafficPrefabs = prefabs;
            }
        }

        public void ConfigureValidationProfile(LwsUtsTrafficProfile profile, GameObject[] fallbackPrefabs)
        {
            validationTrafficProfile = profile;
            SetTrafficPrefabsForValidation(fallbackPrefabs);
        }

        public void InitializeFromGraph(LwsRoadGraphProvider provider, LwsRoadGraph graph)
        {
            TryInitializeFromGraph(provider, graph, true);
        }

        public bool ForceRebuildFromGraph(LwsRoadGraphProvider provider, LwsRoadGraph graph, string reason)
        {
            if (provider != null)
            {
                roadGraphProvider = provider;
            }

            DespawnAllActiveTraffic(string.IsNullOrWhiteSpace(reason) ? "Traffic graph rebuilt." : reason);
            _lanes.Clear();
            _utsPaths.Clear();
            if (_lanesRoot != null)
            {
                ClearChildren(_lanesRoot);
            }

            _initialized = false;
            _graphAvailable = false;
            _configurationFailedPermanently = false;
            _failureLogged = false;
            _initializationAttempts = 0;
            _laneCursor = 0;
            _lastMessage = string.IsNullOrWhiteSpace(reason) ? "Traffic graph rebuild requested." : reason;
            return TryInitializeFromGraph(roadGraphProvider, graph, false);
        }

        public bool TrySpawnOneForValidation(out string message)
        {
            if (!_initialized)
            {
                message = "UTS traffic is not initialized.";
                _lastSpawnResult = message;
                return false;
            }

            ResolvePlayerTransformThrottled();
            PruneNullVehicles();
            bool spawned = TrySpawnTrafficVehicle(out message);
            _lastSpawnResult = message;
            return spawned;
        }

        public int FillTrafficToMaxForValidation()
        {
            if (!_initialized)
            {
                _lastSpawnResult = "UTS traffic is not initialized.";
                return 0;
            }

            int spawned = 0;
            int maxAttempts = Mathf.Max(1, MaxActiveVehicles * 2);
            for (int i = 0; i < maxAttempts && _activeVehicles.Count < MaxActiveVehicles; i++)
            {
                if (TrySpawnTrafficVehicle(out _))
                {
                    spawned++;
                }
            }

            _lastSpawnResult = spawned > 0
                ? $"Filled traffic with {spawned} validation vehicle(s)."
                : "Fill traffic found no safe spawn points.";
            _nextSpawnTime = Time.time + Mathf.Max(0.25f, EffectiveSpawnIntervalSeconds);
            return spawned;
        }

        public void RequestDespawn(GameObject vehicle, string reason)
        {
            if (vehicle == null)
            {
                return;
            }

            LwsTrafficIdentity identity = vehicle.GetComponent<LwsTrafficIdentity>();
            if (identity != null)
            {
                TryUnregister(identity);
            }

            _activeVehicles.Remove(vehicle);
            _lastMessage = string.IsNullOrWhiteSpace(reason) ? $"Despawned {vehicle.name}." : reason;
            Destroy(vehicle);
        }

        public bool IsInsideActiveRoadNeighborhood(Vector3 globalPosition, LwsTrafficLaneDefinition preferredLane, float maxDistanceMeters)
        {
            float maxDistance = Mathf.Max(1f, maxDistanceMeters);
            if (preferredLane != null && DistanceToLane(preferredLane, globalPosition) <= maxDistance)
            {
                return true;
            }

            for (int i = 0; i < _lanes.Count; i++)
            {
                LwsTrafficLaneDefinition lane = _lanes[i];
                if (lane != null && DistanceToLane(lane, globalPosition) <= maxDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryInitializeFromGraph(LwsRoadGraphProvider provider, LwsRoadGraph graph, bool scheduleRetry)
        {
            if (_initialized)
            {
                return true;
            }

            if (provider != null)
            {
                roadGraphProvider = provider;
            }

            EnsureTrafficRoot();
            EnsureDebugPanel();
            LogUtsDiagnosticsOnce();
            if (!TryApplyConfiguredProfile(out string profileMessage))
            {
                return FailInitialization(profileMessage, false, true);
            }

            _initializationAttempts++;

            if (!_utsApi.IsAvailable)
            {
                return FailInitialization(_utsApi.AvailabilitySummary, scheduleRetry);
            }

            if (spawnPolicy == null)
            {
                spawnPolicy = new LwsTrafficSpawnPolicy();
            }

            if (!spawnPolicy.Validate(out string policyMessage))
            {
                return FailInitialization(policyMessage, scheduleRetry);
            }

            ResolveEditorPrefabsIfNeeded();
            trafficPrefabs = FilterSupportedTrafficPrefabs(trafficPrefabs, out string prefabSupportReport);
            LogPrefabSupportReportOnce(prefabSupportReport);
            if (trafficPrefabs == null || trafficPrefabs.Length == 0)
            {
                return FailInitialization(
                    "UTS traffic initialization failed: zero supported traffic prefab references configured.\n" + prefabSupportReport,
                    false,
                    true);
            }

            LwsRoadGraph sourceGraph = graph ?? (roadGraphProvider != null ? roadGraphProvider.Graph : null);
            _graphAvailable = sourceGraph != null && sourceGraph.edges != null && sourceGraph.edges.Count > 0;
            if (!_graphAvailable)
            {
                return FailInitialization("UTS traffic initialization failed: no LWS road graph available.", scheduleRetry);
            }

            _lanes.Clear();
            _utsPaths.Clear();
            ClearChildren(_lanesRoot);
            _lanes.AddRange(LwsTrafficLaneBuilder.BuildTrafficLanes(sourceGraph, spawnPolicy));
            if (_lanes.Count == 0)
            {
                return FailInitialization("UTS traffic initialization failed: zero spawn-enabled LWS traffic lanes were generated.", scheduleRetry);
            }

            for (int i = 0; i < _lanes.Count; i++)
            {
                LwsTrafficLaneDefinition lane = _lanes[i];
                if (!lane.Validate(out string laneMessage))
                {
                    Debug.LogWarning(laneMessage, this);
                    continue;
                }

                var pathObject = new GameObject(lane.laneId);
                pathObject.transform.SetParent(_lanesRoot, false);
                Component path = _utsApi.CreatePath(pathObject, lane, trafficPrefabs, spawnPolicy, GlobalToLocalForTraffic, out string message);
                if (path != null)
                {
                    _utsPaths[lane.laneId] = path;
                }

                _lastMessage = message;
            }

            if (_utsPaths.Count == 0)
            {
                return FailInitialization("UTS traffic initialization failed: all generated lane/path objects failed UTS path creation.", scheduleRetry);
            }

            _initialized = true;
            _lastError = string.Empty;
            _lastMessage = $"UTS highway traffic initialized. Lanes: {_utsPaths.Count}. Prefabs: {trafficPrefabs.Length}.";
            _lastSpawnResult = "Waiting for first scheduled spawn.";
            _nextSpawnTime = Time.time + FirstSpawnDelaySeconds;
            SeedMinimumTrafficPresence();
            Debug.Log(_lastMessage, this);
            return true;
        }

        private void TryRetryInitialization()
        {
            if (!buildOnStart || _initialized || _configurationFailedPermanently || _initializationAttempts >= Mathf.Max(1, initializationRetryFrames))
            {
                return;
            }

            if (Time.unscaledTime < _nextInitializationRetryTime)
            {
                return;
            }

            _nextInitializationRetryTime = Time.unscaledTime + Mathf.Max(0.05f, initializationRetryIntervalSeconds);
            TryInitializeFromGraph(roadGraphProvider, roadGraphProvider != null ? roadGraphProvider.Graph : null, true);
        }

        private bool FailInitialization(string message, bool scheduleRetry, bool permanent = false)
        {
            if (permanent)
            {
                _configurationFailedPermanently = true;
            }

            _lastError = string.IsNullOrWhiteSpace(message)
                ? "UTS traffic initialization failed for an unknown reason."
                : message;
            _lastMessage = _lastError;

            if (!_failureLogged || _initializationAttempts >= Mathf.Max(1, initializationRetryFrames))
            {
                Debug.LogError(_lastError, this);
                _failureLogged = true;
            }

            if (scheduleRetry && _initializationAttempts < Mathf.Max(1, initializationRetryFrames))
            {
                _nextInitializationRetryTime = Time.unscaledTime + Mathf.Max(0.05f, initializationRetryIntervalSeconds);
            }

            return false;
        }

        private bool TrySpawnTrafficVehicle(out string message)
        {
            message = string.Empty;
            if (_utsPaths.Count == 0)
            {
                message = "No generated UTS lanes are available.";
                _lastSpawnResult = message;
                return false;
            }

            if (trafficPrefabs == null || trafficPrefabs.Length == 0)
            {
                message = "No spawnable UTS traffic prefabs are configured.";
                _lastSpawnResult = message;
                return false;
            }

            for (int attempt = 0; attempt < _lanes.Count; attempt++)
            {
                LwsTrafficLaneDefinition lane = _lanes[_laneCursor % _lanes.Count];
                _laneCursor++;
                if (lane == null || !lane.spawnEnabled || !_utsPaths.TryGetValue(lane.laneId, out Component path))
                {
                    continue;
                }

                int spawnIndex = ChooseSpawnIndex(lane);
                if (!CanSpawnAt(lane.centerline[spawnIndex]))
                {
                    message = $"No safe spawn point found on {lane.laneId}; player exclusion is active.";
                    continue;
                }

                GameObject prefab = trafficPrefabs[_prefabCursor % trafficPrefabs.Length];
                _prefabCursor++;
                GameObject vehicle = _utsApi.SpawnVehicle(prefab, path, lane, spawnIndex, _vehiclesRoot, spawnPolicy, GlobalToLocalForTraffic, out message);
                _lastSpawnResult = message;
                _lastMessage = message;
                if (vehicle == null)
                {
                    continue;
                }

                _totalSpawned++;
                vehicle.name = $"Traffic_{_totalSpawned:000}_{prefab.name}";
                string trafficId = $"ih.traffic.validation.{_totalSpawned:0000}";
                LwsTrafficIdentity identity = vehicle.GetComponent<LwsTrafficIdentity>() ?? vehicle.AddComponent<LwsTrafficIdentity>();
                identity.Configure(trafficId, lane.laneId, prefab.name, ClassifyPrefab(prefab));
                LwsFloatingOriginRigidbodyParticipant originParticipant =
                    vehicle.GetComponent<LwsFloatingOriginRigidbodyParticipant>() ??
                    vehicle.AddComponent<LwsFloatingOriginRigidbodyParticipant>();
                originParticipant.Configure(trafficId, LwsFloatingOriginParticipantKind.TrafficVehicle, false);
                TryRegister(identity);

                LwsUtsTrafficVehicleRuntime runtime = vehicle.GetComponent<LwsUtsTrafficVehicleRuntime>() ?? vehicle.AddComponent<LwsUtsTrafficVehicleRuntime>();
                runtime.Configure(this, lane, spawnPolicy);
                _activeVehicles.Add(vehicle);
                _lastSpawnSeconds = Time.time;
                return true;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "No safe UTS traffic spawn lane was available.";
            }

            _lastSpawnResult = message;
            _lastMessage = message;
            return false;
        }

        private void SeedMinimumTrafficPresence()
        {
            RefreshTrafficDemand(0f);
            int target = DemandSnapshot.Valid ? DemandSnapshot.MinimumNearby : Mathf.Min(2, MaxActiveVehicles);
            target = Mathf.Clamp(target, 0, MaxActiveVehicles);
            int attempts = Mathf.Max(1, MaxActiveVehicles * 2);
            int spawned = 0;
            for (int i = 0; i < attempts && _activeVehicles.Count < target; i++)
            {
                if (TrySpawnTrafficVehicle(out _))
                {
                    spawned++;
                }
            }

            if (spawned > 0)
            {
                _lastSpawnResult = $"Seeded {spawned} traffic vehicle(s) for local demand.";
            }
        }

        private int ChooseSpawnIndex(LwsTrafficLaneDefinition lane)
        {
            if (_playerTransform == null)
            {
                return Mathf.Clamp(2 + (_totalSpawned * 3) % Mathf.Max(2, lane.centerline.Length - 4), 1, lane.centerline.Length - 2);
            }

            Vector3 playerGlobal = LocalToGlobalForTraffic(_playerTransform.position);
            int fallbackOffset = 3 + (_totalSpawned % 8);
            return LwsTrafficLaneBuilder.ChooseSpawnPointIndex(
                lane,
                playerGlobal,
                spawnPolicy.minimumPlayerSpawnDistanceMeters,
                spawnPolicy.maximumPlayerSpawnDistanceMeters,
                fallbackOffset);
        }

        private bool CanSpawnAt(Vector3 position)
        {
            if (_playerTransform == null)
            {
                return true;
            }

            return Vector3.Distance(LocalToGlobalForTraffic(_playerTransform.position), position) >= spawnPolicy.minimumPlayerSpawnDistanceMeters;
        }

        private void DespawnDistantVehicles()
        {
            if (_playerTransform == null)
            {
                return;
            }

            for (int i = _activeVehicles.Count - 1; i >= 0; i--)
            {
                GameObject vehicle = _activeVehicles[i];
                if (vehicle == null)
                {
                    _activeVehicles.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(
                    LocalToGlobalForTraffic(_playerTransform.position),
                    LocalToGlobalForTraffic(vehicle.transform.position));
                if (distance > spawnPolicy.despawnDistanceMeters)
                {
                    RequestDespawn(vehicle, $"Despawned distant traffic vehicle at {distance:0}m.");
                    _lastCleanupSeconds = Time.time;
                }
            }
        }

        private void RefreshTrafficDemand(float deltaTimeSeconds)
        {
            ResolveTrafficDemandServices();
            CountNearbyTraffic(out int nearby, out int sameDirection, out int oppositeDirection);
            if (_trafficDemandService == null)
            {
                DemandSnapshot = LwsTrafficDemandSnapshot.Empty;
                return;
            }

            LwsGameClockSnapshot clock = _gameClockService != null
                ? _gameClockService.CurrentSnapshot
                : LwsGameClockUtility.CreateSnapshot(new System.DateTime(2026, 6, 1, 8, 0, 0), LwsGameClockTuning.CreateValidationDefault(), 1f, false, 0d, 0);
            DemandSnapshot = _trafficDemandService.EvaluateDemand(
                clock,
                _activeVehicles.Count,
                nearby,
                sameDirection,
                oppositeDirection,
                deltaTimeSeconds,
                _lastSpawnSeconds,
                _lastCleanupSeconds);
        }

        private void CountNearbyTraffic(out int nearby, out int sameDirection, out int oppositeDirection)
        {
            nearby = 0;
            sameDirection = 0;
            oppositeDirection = 0;
            if (_playerTransform == null)
            {
                return;
            }

            float radius = DemandSnapshot.Valid && _trafficDemandService?.ActiveProfile != null
                ? _trafficDemandService.ActiveProfile.nearbyRadiusMeters
                : 750f;
            Vector3 playerGlobal = LocalToGlobalForTraffic(_playerTransform.position);
            Vector3 playerForward = _playerTransform.forward.sqrMagnitude > 0.0001f ? _playerTransform.forward.normalized : Vector3.forward;
            for (int i = 0; i < _activeVehicles.Count; i++)
            {
                GameObject vehicle = _activeVehicles[i];
                if (vehicle == null)
                {
                    continue;
                }

                Vector3 vehicleGlobal = LocalToGlobalForTraffic(vehicle.transform.position);
                if (Vector3.Distance(playerGlobal, vehicleGlobal) > radius)
                {
                    continue;
                }

                nearby++;
                Vector3 forward = vehicle.transform.forward.sqrMagnitude > 0.0001f ? vehicle.transform.forward.normalized : Vector3.forward;
                if (Vector3.Dot(playerForward, forward) >= 0f)
                {
                    sameDirection++;
                }
                else
                {
                    oppositeDirection++;
                }
            }
        }

        private void ResolvePlayerTransformThrottled()
        {
            if (_playerTransform != null)
            {
                return;
            }

            if (playerOverride != null)
            {
                _playerTransform = playerOverride;
                return;
            }

            if (Time.unscaledTime < _nextPlayerResolveTime)
            {
                return;
            }

            _nextPlayerResolveTime = Time.unscaledTime + 2f;
            LwsPlayerTruck playerTruck = FindFirstObjectByType<LwsPlayerTruck>();
            _playerTransform = playerTruck != null ? playerTruck.transform : null;
        }

        private bool TryApplyConfiguredProfile(out string message)
        {
            if (validationTrafficProfile == null)
            {
                message = "No UTS validation traffic profile is assigned.";
                return true;
            }

            if (!validationTrafficProfile.ValidateProfile(out message))
            {
                return false;
            }

            if (!validationTrafficProfile.TryGetTrafficPrefabsCopy(out GameObject[] prefabs, out message))
            {
                return false;
            }

            spawnPolicy = validationTrafficProfile.CreateSpawnPolicyCopy();
            trafficPrefabs = prefabs;
            return true;
        }

        private void EnsureTrafficRoot()
        {
            if (_trafficRoot == null)
            {
                GameObject existing = GameObject.Find(RuntimeRootName);
                GameObject root = existing != null ? existing : new GameObject(RuntimeRootName);
                root.transform.SetParent(null, false);
                _trafficRoot = root.transform;
            }

            _lanesRoot = EnsureChild(_trafficRoot, LanesRootName);
            _vehiclesRoot = EnsureChild(_trafficRoot, VehiclesRootName);
            LwsFloatingOriginTransformParticipant laneOriginParticipant =
                _lanesRoot.GetComponent<LwsFloatingOriginTransformParticipant>() ??
                _lanesRoot.gameObject.AddComponent<LwsFloatingOriginTransformParticipant>();
            laneOriginParticipant.Configure("traffic.lanes", LwsFloatingOriginParticipantKind.TrafficLaneRoot, false);
        }

        private void EnsureDebugPanel()
        {
            if (spawnPolicy != null && !spawnPolicy.showDebugPanel)
            {
                return;
            }

            LwsUtsTrafficDebugPanel panel = GetComponent<LwsUtsTrafficDebugPanel>();
            if (panel == null)
            {
                panel = gameObject.AddComponent<LwsUtsTrafficDebugPanel>();
            }

            panel.SetController(this);
        }

        private void LogUtsDiagnosticsOnce()
        {
            if (_diagnosticsLogged)
            {
                return;
            }

            _diagnosticsLogged = true;
            Debug.Log(_utsApi.TypeAvailabilityReport, this);
        }

        private void PruneNullVehicles()
        {
            for (int i = _activeVehicles.Count - 1; i >= 0; i--)
            {
                if (_activeVehicles[i] == null)
                {
                    _activeVehicles.RemoveAt(i);
                }
            }
        }

        private void UnregisterActiveTraffic()
        {
            for (int i = _activeVehicles.Count - 1; i >= 0; i--)
            {
                GameObject vehicle = _activeVehicles[i];
                if (vehicle == null)
                {
                    continue;
                }

                LwsTrafficIdentity identity = vehicle.GetComponent<LwsTrafficIdentity>();
                if (identity != null)
                {
                    TryUnregister(identity);
                }
            }

            _activeVehicles.Clear();
        }

        private void DespawnAllActiveTraffic(string reason)
        {
            for (int i = _activeVehicles.Count - 1; i >= 0; i--)
            {
                GameObject vehicle = _activeVehicles[i];
                if (vehicle == null)
                {
                    continue;
                }

                LwsTrafficIdentity identity = vehicle.GetComponent<LwsTrafficIdentity>();
                if (identity != null)
                {
                    TryUnregister(identity);
                }

                Destroy(vehicle);
            }

            _activeVehicles.Clear();
            _lastCleanupSeconds = Time.time;
            _lastMessage = reason ?? string.Empty;
        }

        private void TryRegister(LwsTrafficIdentity identity)
        {
            if (identity == null)
            {
                return;
            }

            LwsApplicationBootstrap bootstrap = LwsApplicationBootstrap.Instance;
            if (bootstrap != null &&
                bootstrap.Registry != null &&
                bootstrap.Registry.TryGet(out ILwsTrafficService trafficService))
            {
                LwsServiceResult result = trafficService.RegisterTrafficVehicle(identity);
                if (!result.Succeeded)
                {
                    Debug.LogWarning(result.Message, identity);
                }
            }
        }

        private void TryUnregister(LwsTrafficIdentity identity)
        {
            LwsApplicationBootstrap bootstrap = LwsApplicationBootstrap.Instance;
            if (bootstrap != null &&
                bootstrap.Registry != null &&
                bootstrap.Registry.TryGet(out ILwsTrafficService trafficService))
            {
                trafficService.UnregisterTrafficVehicle(identity);
            }
            else if (identity != null)
            {
                identity.MarkRegistered(false);
            }
        }

        private GameObject[] FilterSupportedTrafficPrefabs(GameObject[] prefabs, out string supportReport)
        {
            var supportLines = new List<string>();
            if (prefabs == null)
            {
                supportReport = "UTS traffic prefab support: no prefab array is configured.";
                return new GameObject[0];
            }

            var supported = new List<GameObject>();
            for (int i = 0; i < prefabs.Length; i++)
            {
                GameObject prefab = prefabs[i];
                if (!_utsApi.TryDescribePrefabSupport(prefab, out string message))
                {
                    supportLines.Add($"Slot {i}: {message}");
                    continue;
                }

                supportLines.Add($"Slot {i}: {message}");
                supported.Add(prefab);
            }

            supportReport = "UTS traffic prefab support:\n" + string.Join("\n", supportLines);
            return supported.ToArray();
        }

        private void LogPrefabSupportReportOnce(string supportReport)
        {
            if (_prefabSupportLogged || string.IsNullOrWhiteSpace(supportReport))
            {
                return;
            }

            _prefabSupportLogged = true;
            Debug.Log(supportReport, this);
        }

        private LwsTrafficVehicleKind ClassifyPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return LwsTrafficVehicleKind.Unknown;
            }

            string name = prefab.name.ToLowerInvariant();
            if (name.Contains("bus"))
            {
                return LwsTrafficVehicleKind.Bus;
            }

            if (name.Contains("truck"))
            {
                return LwsTrafficVehicleKind.BoxTruck;
            }

            if (name.Contains("bike") || name.Contains("scooter"))
            {
                return LwsTrafficVehicleKind.Motorcycle;
            }

            if (name.Contains("fire") || name.Contains("police") || name.Contains("ambulance"))
            {
                return LwsTrafficVehicleKind.ServiceVehicle;
            }

            return LwsTrafficVehicleKind.PassengerCar;
        }

        private void ResolveEditorPrefabsIfNeeded()
        {
#if UNITY_EDITOR
            if (!spawnPolicy.autoResolveEditorPrefabs || trafficPrefabs != null && trafficPrefabs.Length > 0)
            {
                return;
            }

            var prefabs = new List<GameObject>();
            for (int i = 0; i < DefaultEditorTrafficPrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultEditorTrafficPrefabPaths[i]);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }

            trafficPrefabs = prefabs.ToArray();
#endif
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var go = new GameObject(childName);
            child = go.transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
        }

        private static int CountPrefabs(GameObject[] prefabs)
        {
            int count = 0;
            if (prefabs == null)
            {
                return count;
            }

            for (int i = 0; i < prefabs.Length; i++)
            {
                try
                {
                    if (prefabs[i] != null)
                    {
                        count++;
                    }
                }
                catch (MissingReferenceException)
                {
                    return count;
                }
            }

            return count;
        }

        private static float DistanceToLane(LwsTrafficLaneDefinition lane, Vector3 point)
        {
            if (lane == null || lane.centerline == null || lane.centerline.Length == 0)
            {
                return float.MaxValue;
            }

            if (lane.centerline.Length == 1)
            {
                return Vector3.Distance(point, lane.centerline[0]);
            }

            float best = float.MaxValue;
            for (int i = 0; i < lane.centerline.Length - 1; i++)
            {
                Vector3 a = lane.centerline[i];
                Vector3 b = lane.centerline[i + 1];
                Vector3 ab = b - a;
                float denominator = ab.sqrMagnitude;
                float t = denominator > 0.0001f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / denominator) : 0f;
                Vector3 closest = a + ab * t;
                best = Mathf.Min(best, Vector3.Distance(point, closest));
            }

            return best;
        }

        public Vector3 GlobalToLocalForTraffic(Vector3 globalPosition)
        {
            ResolveOriginService();
            return _originService != null
                ? _originService.GlobalToLocal(LwsWorldPositionD.FromVector3(globalPosition))
                : globalPosition;
        }

        public Vector3 LocalToGlobalForTraffic(Vector3 localPosition)
        {
            ResolveOriginService();
            return _originService != null
                ? _originService.LocalToGlobal(localPosition).ToVector3()
                : localPosition;
        }

        private void ResolveOriginService()
        {
            if (_originService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _gameClockService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _trafficDemandService);
            if (_originService != null && !_originEventsSubscribed)
            {
                _originService.OriginShiftCompleted += HandleOriginShiftCompleted;
                _originEventsSubscribed = true;
            }
        }

        private void ResolveTrafficDemandServices()
        {
            if (_gameClockService != null && _trafficDemandService != null)
            {
                return;
            }

            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _gameClockService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _trafficDemandService);
        }

        private void HandleOriginShiftCompleted(LwsOriginShiftEvent shiftEvent)
        {
            int refreshed = 0;
            foreach (KeyValuePair<string, Component> pair in _utsPaths)
            {
                if (_utsApi.RefreshPathPointCache(pair.Value))
                {
                    refreshed++;
                }
            }

            _lastMessage = $"Floating-origin shift refreshed {refreshed} UTS path cache(s).";
        }
    }

    [DisallowMultipleComponent]
    public sealed class LwsUtsTrafficVehicleRuntime : MonoBehaviour
    {
        private LwsUtsHighwayTrafficController _controller;
        private LwsTrafficLaneDefinition _lane;
        private LwsTrafficSpawnPolicy _policy;
        private float _nextCheckTime;
        private float _outsideRoadSince = -1f;

        public void Configure(LwsUtsHighwayTrafficController controller, LwsTrafficLaneDefinition lane, LwsTrafficSpawnPolicy policy)
        {
            _controller = controller;
            _lane = lane;
            _policy = policy;
        }

        private void Update()
        {
            if (_controller == null || _lane == null || _policy == null || Time.time < _nextCheckTime)
            {
                return;
            }

            _nextCheckTime = Time.time + 0.5f;
            Vector3 globalPosition = _controller.LocalToGlobalForTraffic(transform.position);
            if (globalPosition.y < _policy.safetyFloorMeters)
            {
                _controller.RequestDespawn(gameObject, $"Recycled traffic vehicle below safety floor on {_lane.laneId}.");
                return;
            }

            if (!_controller.IsInsideActiveRoadNeighborhood(globalPosition, _lane, _policy.offRoadCleanupDistanceMeters))
            {
                if (_outsideRoadSince < 0f)
                {
                    _outsideRoadSince = Time.time;
                }

                if (Time.time - _outsideRoadSince >= _policy.offRoadCleanupGraceSeconds)
                {
                    _controller.RequestDespawn(gameObject, $"Recycled traffic vehicle outside active road neighborhood on {_lane.laneId}.");
                    return;
                }
            }
            else
            {
                _outsideRoadSince = -1f;
            }

            if (Vector3.Distance(globalPosition, _lane.EndPosition) <= Mathf.Max(20f, _policy.despawnNearLaneEndMeters))
            {
                _controller.RequestDespawn(gameObject, $"Despawned traffic vehicle near end of {_lane.laneId}.");
            }
        }
    }
}
