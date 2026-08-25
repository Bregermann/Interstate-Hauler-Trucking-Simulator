using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LwsRoadGraphProvider))]
    public sealed class LwsFiftyMileHighwayValidationController : MonoBehaviour
    {
        [SerializeField] private LwsRoadGraphProvider roadGraphProvider;
        [SerializeField] private LwsWorldStreamingManifest manifest;
        [SerializeField] private LwsUtsTrafficProfile validationTrafficProfile;
        [SerializeField] private GameObject[] validationTrafficPrefabs;
        [SerializeField] private LwsPlayerTruckSpawner playerTruckSpawner;
        [SerializeField] private bool registerOnStart = true;
        [SerializeField] private bool requestGpsRouteOnStart = true;
        [SerializeField] private bool addDevelopmentPanels = true;
        [SerializeField] private bool automaticWeatherCycle = true;
        [SerializeField] private bool fillTrafficOnStart = true;
        [SerializeField] private float weatherTransitionSeconds = 15f;

        private readonly HashSet<int> _reportedCheckpoints = new HashSet<int>();
        private ILwsWorldOriginService _originService;
        private ILwsWorldStreamingService _streamingService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private ILwsNavigationService _navigationService;
        private ILwsWeatherService _weatherService;
        private ILwsRoadConditionService _roadConditionService;
        private ILwsTrafficService _trafficService;
        private LwsUtsHighwayTrafficController _trafficController;
        private LwsRoadGraph _graph;
        private bool _streamingEventsSubscribed;
        private bool _routeRequested;
        private bool _completionLogged;
        private string _activeWeatherPreset = string.Empty;
        private double _maxLocalDistanceMeters;
        private double _maxOriginShiftDurationMs;
        private int _streamingFailureCount;

        public double CurrentGlobalDistanceMeters { get; private set; }
        public double CurrentMile => LwsFiftyMileHighwayModel.MetersToMile(CurrentGlobalDistanceMeters);
        public double DistanceRemainingMeters => Math.Max(0d, LwsFiftyMileHighwayModel.TotalLengthMeters - CurrentGlobalDistanceMeters);
        public string CurrentChunkId => LwsFiftyMileHighwayModel.GetChunkId(LwsFiftyMileHighwayModel.GetChunkIndexForMeters(CurrentGlobalDistanceMeters));
        public int ChunkLoadCount { get; private set; }
        public int ChunkUnloadCount { get; private set; }
        public int StreamingFailureCount => _streamingFailureCount;
        public int WeatherChangeCount { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public string LastReport { get; private set; } = "50-mile validation not started.";
        public string ActiveWeatherPreset => _activeWeatherPreset;
        public bool AutomaticWeatherCycle => automaticWeatherCycle;
        public double MaxLocalDistanceMeters => _maxLocalDistanceMeters;
        public double MaxOriginShiftDurationMs => _maxOriginShiftDurationMs;
        public double MetersRoadAheadAvailable => CalculateMetersRoadAheadAvailable();
        public LwsRoadGraph ActiveGraph => _graph;
        public LwsUtsHighwayTrafficController TrafficController => _trafficController;
        public ILwsWorldStreamingService StreamingService => _streamingService;
        public ILwsWorldOriginService OriginService => _originService;
        public ILwsNavigationService NavigationService => _navigationService;
        public ILwsWeatherService WeatherService => _weatherService;
        public ILwsRoadConditionService RoadConditionService => _roadConditionService;
        public ILwsTrafficService TrafficService => _trafficService;

        private void Reset()
        {
            roadGraphProvider = GetComponent<LwsRoadGraphProvider>();
        }

        private void Start()
        {
            ResolveLocalReferences();
            ResolveServices();

            if (registerOnStart)
            {
                RegisterValidationWorld();
            }
        }

        private void Update()
        {
            ResolveServices();
            UpdateGlobalProgress();
            UpdateNavigationPose();
            UpdateWeatherCycle();
            UpdateCheckpoints();
            UpdateCompletionReport();

            if (_originService != null)
            {
                _maxLocalDistanceMeters = Math.Max(_maxLocalDistanceMeters, _originService.PlayerLocalPosition.magnitude);
                _maxOriginShiftDurationMs = Math.Max(_maxOriginShiftDurationMs, _originService.LastShiftDurationMilliseconds);
            }
        }

        private void OnDestroy()
        {
            if (_streamingService != null && _streamingEventsSubscribed)
            {
                _streamingService.ChunkLoaded -= HandleChunkLoaded;
                _streamingService.ChunkUnloaded -= HandleChunkUnloaded;
                _streamingService.ChunkFailed -= HandleChunkFailed;
                _streamingEventsSubscribed = false;
            }
        }

        public void RegisterValidationWorld()
        {
            ResolveLocalReferences();
            ResolveServices();
            EnsureRoadGraphProvider();
            _graph = LwsFiftyMileHighwayModel.CreateRoadGraph();
            roadGraphProvider.SetGraph(_graph, true);
            EnsureValidationTruckSpawned();

            if (addDevelopmentPanels)
            {
                EnsureComponent<LwsNavigationDebugPanel>();
                EnsureComponent<LwsGpsSettingsPanel>();
                EnsureComponent<LwsWeatherMakerAdapter>();
                EnsureComponent<LwsWeatherDebugPanel>();
                EnsureComponent<LwsRoadConditionRuntimeController>();
                EnsureComponent<LwsWeatheradeAdapter>();
                EnsureComponent<LwsNwhRoadConditionAdapter>();
                EnsureComponent<LwsRoadConditionDebugPanel>();
                EnsureComponent<LwsFiftyMileHighwayDebugPanel>();
            }
            else
            {
                EnsureComponent<LwsWeatherMakerAdapter>();
                EnsureComponent<LwsRoadConditionRuntimeController>();
                EnsureComponent<LwsWeatheradeAdapter>();
                EnsureComponent<LwsNwhRoadConditionAdapter>();
            }

            _trafficController = EnsureComponent<LwsUtsHighwayTrafficController>();
            _trafficController.ConfigureValidationProfile(validationTrafficProfile, validationTrafficPrefabs);
            if (playerTruckSpawner != null && playerTruckSpawner.SpawnedTruck != null)
            {
                _trafficController.SetPlayerOverride(playerTruckSpawner.SpawnedTruck.transform);
            }

            _trafficController.InitializeFromGraph(roadGraphProvider, _graph);
            if (fillTrafficOnStart)
            {
                _trafficController.FillTrafficToMaxForValidation();
            }

            if (addDevelopmentPanels)
            {
                EnsureComponent<LwsUtsTrafficDebugPanel>();
            }

            if (requestGpsRouteOnStart)
            {
                RequestGpsRoute();
            }

            UpdateWeatherCycle(true);
            LastReport = "50-mile validation world registered.";
        }

        public bool RequestGpsRoute()
        {
            ResolveServices();
            if (_navigationService == null || _graph == null)
            {
                LastError = "GPS route cannot start because navigation service or 50-mile graph is missing.";
                return false;
            }

            LwsRouteResult result = _navigationService.SetDestination(
                LwsFiftyMileHighwayModel.EastboundDestinationPosition,
                LwsFiftyMileHighwayModel.EastboundStartPosition,
                _graph);
            _routeRequested = result != null && result.succeeded;
            LastReport = _routeRequested
                ? $"GPS route active: {result.distanceMeters / LwsFiftyMileHighwayModel.MetersPerMile:0.00} mi."
                : result != null ? result.message : "GPS route request failed.";
            if (!_routeRequested)
            {
                LastError = LastReport;
            }

            return _routeRequested;
        }

        public void SetAutomaticWeatherCycle(bool enabled)
        {
            automaticWeatherCycle = enabled;
            LastReport = enabled
                ? "50-mile automatic weather cycle enabled."
                : "50-mile automatic weather cycle disabled.";
        }

        public bool TeleportToMile(double mile)
        {
            ResolveServices();
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                LastError = "Cannot teleport: active player truck is missing.";
                return false;
            }

            double clampedMile = Math.Max(0d, Math.Min(LwsFiftyMileHighwayModel.TotalMiles, mile));
            double globalDistance = LwsFiftyMileHighwayModel.MileToMeters(clampedMile);
            Vector3 globalTruckPosition = LwsFiftyMileHighwayModel.EastboundLanePosition(globalDistance, 0) + Vector3.up * 0.65f;
            double grid = 500d;
            double offsetZ = Math.Floor(globalDistance / grid) * grid;
            LwsWorldPositionD targetOffset = new LwsWorldPositionD(0d, 0d, offsetZ);

            if (_originService == null || !_originService.SetOriginOffsetForValidation(targetOffset, $"50-mile validation teleport to Mile {clampedMile:0.##}.", out _))
            {
                LastError = _originService != null ? _originService.LastError : "Cannot teleport: world origin service is missing.";
                return false;
            }

            Vector3 localTruckPosition = _originService.GlobalToLocal(LwsWorldPositionD.FromVector3(globalTruckPosition));
            Quaternion truckRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            MoveBody(truck.gameObject, localTruckPosition, truckRotation);

            GameObject trailer = ResolveValidationTrailer();
            if (trailer != null)
            {
                MoveBody(trailer, localTruckPosition - Vector3.forward * 13f, truckRotation);
            }

            _originService.UpdatePlayerLocalPosition(localTruckPosition);
            _streamingService?.UpdateStreamingAnchor(new LwsWorldStreamingAnchorState(
                localTruckPosition,
                LwsWorldPositionD.FromVector3(globalTruckPosition),
                Vector3.forward,
                0f,
                trailer != null,
                trailer != null ? trailer.transform.position : localTruckPosition,
                trailer != null ? _originService.LocalToGlobal(trailer.transform.position) : LwsWorldPositionD.FromVector3(globalTruckPosition)));
            _streamingService?.ReloadCurrentNeighborhood();
            UpdateGlobalProgress();
            UpdateWeatherCycle(true);
            if (!_routeRequested)
            {
                RequestGpsRoute();
            }

            LastReport = $"Teleported to Mile {clampedMile:0.##}; requested streaming neighborhood for {CurrentChunkId}.";
            return true;
        }

        private void ResolveLocalReferences()
        {
            if (roadGraphProvider == null)
            {
                roadGraphProvider = GetComponent<LwsRoadGraphProvider>();
            }

            if (_trafficController == null)
            {
                _trafficController = GetComponent<LwsUtsHighwayTrafficController>();
            }
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _streamingService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _navigationService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _weatherService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _roadConditionService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _trafficService);

            if (_streamingService != null && !_streamingEventsSubscribed)
            {
                _streamingService.ChunkLoaded += HandleChunkLoaded;
                _streamingService.ChunkUnloaded += HandleChunkUnloaded;
                _streamingService.ChunkFailed += HandleChunkFailed;
                _streamingEventsSubscribed = true;
            }
        }

        private void EnsureRoadGraphProvider()
        {
            if (roadGraphProvider == null)
            {
                roadGraphProvider = GetComponent<LwsRoadGraphProvider>();
            }

            if (roadGraphProvider == null)
            {
                roadGraphProvider = gameObject.AddComponent<LwsRoadGraphProvider>();
            }
        }

        private void EnsureValidationTruckSpawned()
        {
            if (playerTruckSpawner != null && playerTruckSpawner.SpawnedTruck == null)
            {
                playerTruckSpawner.SpawnValidationRig();
            }
        }

        private T EnsureComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private void UpdateGlobalProgress()
        {
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            Vector3 local = truck != null ? truck.transform.position : transform.position;
            LwsWorldPositionD global = _originService != null
                ? _originService.LocalToGlobal(local)
                : LwsWorldPositionD.FromVector3(local);
            CurrentGlobalDistanceMeters = Math.Max(0d, Math.Min(LwsFiftyMileHighwayModel.TotalLengthMeters, global.z));
        }

        private void UpdateNavigationPose()
        {
            if (_navigationService == null)
            {
                return;
            }

            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                return;
            }

            Vector3 global = _originService != null
                ? _originService.LocalToGlobal(truck.transform.position).ToVector3()
                : truck.transform.position;
            _navigationService.UpdateVehiclePose(global, truck.transform.forward, Time.deltaTime);
        }

        private void UpdateWeatherCycle(bool force = false)
        {
            if (!automaticWeatherCycle || _weatherService == null)
            {
                return;
            }

            string presetId = LwsFiftyMileHighwayModel.GetWeatherPresetForMile(CurrentMile);
            if (!force && string.Equals(_activeWeatherPreset, presetId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            LwsServiceResult result = _weatherService.RequestWeather(presetId, force ? 0f : weatherTransitionSeconds, force);
            if (!result.Succeeded)
            {
                LastError = result.Message;
                return;
            }

            _activeWeatherPreset = presetId;
            WeatherChangeCount++;
        }

        private void UpdateCheckpoints()
        {
            for (int mile = 0; mile <= 50; mile += 5)
            {
                if (CurrentMile + 0.005d < mile || !_reportedCheckpoints.Add(mile))
                {
                    continue;
                }

                string message = $"[IH 50-Mile Test] Reached Mile {mile} | Origin Shifts: {(_originService != null ? _originService.ShiftCount : 0)} | Current Chunk: {CurrentChunkId}";
                LastReport = message;
                Debug.Log(message, this);
            }
        }

        private void UpdateCompletionReport()
        {
            if (_completionLogged || CurrentMile < LwsFiftyMileHighwayModel.TotalMiles - 0.02d)
            {
                return;
            }

            _completionLogged = true;
            string report = BuildCertificationReport();
            LastReport = report;
            Debug.Log(report, this);
        }

        public string BuildCertificationReportForValidation()
        {
            return BuildCertificationReport();
        }

        private string BuildCertificationReport()
        {
            LwsTrafficRuntimeStats traffic = _trafficController != null ? _trafficController.Stats : default;
            long originShifts = _originService != null ? _originService.ShiftCount : 0;
            long originVersion = _originService != null ? _originService.OriginVersion : 0;
            string gpsStatus = _navigationService != null && _navigationService.RuntimeState.routeActive ? "PASS" : "FAIL";
            string streamingError = _streamingService != null && !string.IsNullOrWhiteSpace(_streamingService.LastError)
                ? _streamingService.LastError
                : "None";
            string runtimeErrors = ResolveCertificationRuntimeErrors(streamingError);

            return
                "50-MILE CERTIFICATION COMPLETE\n" +
                $"Distance: {LwsFiftyMileHighwayModel.TotalMiles:0.00} mi / {LwsFiftyMileHighwayModel.TotalLengthMeters:0.0} m\n" +
                $"Origin Shifts: {originShifts}\n" +
                $"Origin Version: {originVersion}\n" +
                $"Maximum Local Distance: {_maxLocalDistanceMeters:0.0} m\n" +
                $"Largest Shift Duration: {_maxOriginShiftDurationMs:0.000} ms\n" +
                $"Chunk Loads: {ChunkLoadCount}\n" +
                $"Chunk Unloads: {ChunkUnloadCount}\n" +
                $"Meters Road Ahead: {MetersRoadAheadAvailable:0.0} m\n" +
                $"Traffic Spawned: {traffic.TotalSpawned}\n" +
                $"Weather Transitions: {WeatherChangeCount}\n" +
                $"GPS Status: {gpsStatus}\n" +
                $"Streaming Failures: {_streamingFailureCount}\n" +
                $"Streaming Last Error: {streamingError}\n" +
                $"Runtime Errors: {runtimeErrors}";
        }

        private string ResolveCertificationRuntimeErrors(string streamingError)
        {
            var errors = new List<string>();
            if (!string.IsNullOrWhiteSpace(LastError))
            {
                errors.Add($"50-mile: {LastError}");
            }

            if (!string.IsNullOrWhiteSpace(streamingError) && !string.Equals(streamingError, "None", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"streaming: {streamingError}");
            }

            if (_originService != null && !string.IsNullOrWhiteSpace(_originService.LastError))
            {
                errors.Add($"origin: {_originService.LastError}");
            }

            return errors.Count == 0 ? "None" : string.Join(" | ", errors);
        }

        private double CalculateMetersRoadAheadAvailable()
        {
            if (_streamingService == null || _streamingService.ActiveManifest == null)
            {
                return 0d;
            }

            double farthestLoadedEnd = CurrentGlobalDistanceMeters;
            IReadOnlyList<LwsWorldChunkRuntimeState> states = _streamingService.ChunkStates;
            for (int i = 0; i < states.Count; i++)
            {
                LwsWorldChunkRuntimeState state = states[i];
                if (state == null || !state.IsLoaded ||
                    !_streamingService.TryGetChunkDefinition(state.chunkId, out LwsWorldChunkDefinition definition))
                {
                    continue;
                }

                double chunkEnd = definition.WorldBounds.max.z;
                if (chunkEnd >= CurrentGlobalDistanceMeters)
                {
                    farthestLoadedEnd = Math.Max(farthestLoadedEnd, chunkEnd);
                }
            }

            return Math.Max(0d, Math.Min(LwsFiftyMileHighwayModel.TotalLengthMeters, farthestLoadedEnd) - CurrentGlobalDistanceMeters);
        }

        private GameObject ResolveValidationTrailer()
        {
            if (playerTruckSpawner != null && playerTruckSpawner.SpawnedTrailer != null)
            {
                return playerTruckSpawner.SpawnedTrailer;
            }

            LwsVehicleIdentity[] identities = FindObjectsByType<LwsVehicleIdentity>(FindObjectsSortMode.None);
            for (int i = 0; i < identities.Length; i++)
            {
                if (identities[i] != null && identities[i].Role == LwsVehicleRole.Trailer)
                {
                    return identities[i].gameObject;
                }
            }

            return null;
        }

        private static void MoveBody(GameObject owner, Vector3 position, Quaternion rotation)
        {
            if (owner == null)
            {
                return;
            }

            owner.transform.SetPositionAndRotation(position, rotation);
            Rigidbody rb = owner.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = owner.GetComponentInChildren<Rigidbody>();
            }

            if (rb == null)
            {
                return;
            }

            rb.position = position;
            rb.rotation = rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        private void HandleChunkLoaded(LwsWorldChunkEvent chunkEvent)
        {
            ChunkLoadCount++;
        }

        private void HandleChunkUnloaded(LwsWorldChunkEvent chunkEvent)
        {
            ChunkUnloadCount++;
        }

        private void HandleChunkFailed(LwsWorldChunkEvent chunkEvent)
        {
            _streamingFailureCount++;
            LastError = chunkEvent.Message;
        }
    }
}
