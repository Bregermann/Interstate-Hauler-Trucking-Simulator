using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LwsSceneStreamerAdapter))]
    public sealed class LwsWorldStreamingCoordinator : MonoBehaviour
    {
        [SerializeField] private LwsWorldStreamingManifest manifest;
        [SerializeField] private LwsSceneStreamerAdapter sceneStreamerAdapter;
        [SerializeField] private bool configureOnStart = true;
        [SerializeField] private bool updateEveryFrame = true;
        [SerializeField] private bool fallbackToTransformWhenNoPlayer = true;
        [SerializeField] private Vector3 fallbackHeading = Vector3.forward;
        [SerializeField] private float trailerLookupIntervalSeconds = 1.0f;
        [SerializeField] private bool ensureFloatingOriginCoordinator = true;
        [SerializeField] private LwsFloatingOriginCoordinator floatingOriginCoordinator;

        private ILwsWorldStreamingService _streamingService;
        private ILwsWorldOriginService _originService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private ILwsVehicleRuntimeService _vehicleRuntimeService;
        private string _cachedTrailerId = string.Empty;
        private Transform _cachedTrailerTransform;
        private float _nextTrailerLookupTime;

        public LwsWorldStreamingManifest Manifest => manifest;
        public ILwsWorldStreamingService StreamingService => _streamingService;
        public string LastConfigurationMessage { get; private set; } = "Not configured.";

        private void Reset()
        {
            sceneStreamerAdapter = GetComponent<LwsSceneStreamerAdapter>();
        }

        private void Awake()
        {
            if (sceneStreamerAdapter == null)
            {
                sceneStreamerAdapter = GetComponent<LwsSceneStreamerAdapter>();
            }

            if (ensureFloatingOriginCoordinator)
            {
                EnsureFloatingOriginCoordinator();
            }
        }

        private void Start()
        {
            ResolveServices();
            if (configureOnStart)
            {
                ConfigureStreaming();
            }
        }

        private void Update()
        {
            if (!updateEveryFrame || _streamingService == null)
            {
                return;
            }

            _streamingService.UpdateStreamingAnchor(BuildAnchorState());
            _streamingService.Tick(Time.deltaTime);
        }

        public void Configure(LwsWorldStreamingManifest newManifest)
        {
            manifest = newManifest;
            ConfigureStreaming();
        }

        public void ConfigureStreaming()
        {
            ResolveServices();
            if (_streamingService == null)
            {
                LastConfigurationMessage = "LWS world streaming service is not registered.";
                Debug.LogWarning(LastConfigurationMessage, this);
                return;
            }

            if (sceneStreamerAdapter == null)
            {
                sceneStreamerAdapter = GetComponent<LwsSceneStreamerAdapter>();
            }

            LwsWorldStreamingValidationResult result = _streamingService.Configure(manifest, sceneStreamerAdapter);
            LastConfigurationMessage = result.Summary;
            if (!result.IsValid)
            {
                Debug.LogWarning($"World streaming manifest validation failed: {result.Summary}", this);
                return;
            }

            _streamingService.UpdateStreamingAnchor(BuildAnchorState());
            _streamingService.Tick(float.MaxValue);
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _streamingService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _vehicleRuntimeService);
        }

        private LwsWorldStreamingAnchorState BuildAnchorState()
        {
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck != null)
            {
                Vector3 heading = truck.transform.forward.sqrMagnitude > 0.0001f ? truck.transform.forward : Vector3.forward;
                LwsVehicleTelemetry telemetry = _vehicleRuntimeService != null ? _vehicleRuntimeService.LastTelemetry : truck.LastTelemetry;
                bool hasTrailer = telemetry.trailerAttached;
                Vector3 trailerPosition = hasTrailer
                    ? ResolveTrailerPosition(telemetry.trailerId, truck.transform.position - heading * 18f)
                    : truck.transform.position;
                LwsWorldPositionD tractorGlobal = _originService != null
                    ? _originService.LocalToGlobal(truck.transform.position)
                    : LwsWorldPositionD.FromVector3(truck.transform.position);
                LwsWorldPositionD trailerGlobal = _originService != null
                    ? _originService.LocalToGlobal(trailerPosition)
                    : LwsWorldPositionD.FromVector3(trailerPosition);

                return new LwsWorldStreamingAnchorState(
                    truck.transform.position,
                    tractorGlobal,
                    heading,
                    Mathf.Abs(telemetry.signedSpeedMetersPerSecond),
                    hasTrailer,
                    trailerPosition,
                    trailerGlobal);
            }

            Vector3 fallback = fallbackHeading.sqrMagnitude > 0.0001f ? fallbackHeading.normalized : Vector3.forward;
            Vector3 fallbackLocal = fallbackToTransformWhenNoPlayer ? transform.position : Vector3.zero;
            LwsWorldPositionD fallbackGlobal = _originService != null
                ? _originService.LocalToGlobal(fallbackLocal)
                : LwsWorldPositionD.FromVector3(fallbackLocal);
            return new LwsWorldStreamingAnchorState(
                fallbackLocal,
                fallbackGlobal,
                fallback,
                0f,
                false,
                Vector3.zero,
                LwsWorldPositionD.Zero);
        }

        private Vector3 ResolveTrailerPosition(string trailerId, Vector3 fallback)
        {
            if (string.IsNullOrWhiteSpace(trailerId))
            {
                _cachedTrailerId = string.Empty;
                _cachedTrailerTransform = null;
                return fallback;
            }

            bool cacheMatches = string.Equals(_cachedTrailerId, trailerId, System.StringComparison.OrdinalIgnoreCase) &&
                                _cachedTrailerTransform != null;
            if (cacheMatches)
            {
                return _cachedTrailerTransform.position;
            }

            if (Time.unscaledTime < _nextTrailerLookupTime)
            {
                return fallback;
            }

            _nextTrailerLookupTime = Time.unscaledTime + Mathf.Max(0.25f, trailerLookupIntervalSeconds);
            LwsVehicleIdentity[] identities = FindObjectsByType<LwsVehicleIdentity>(FindObjectsSortMode.None);
            for (int i = 0; i < identities.Length; i++)
            {
                LwsVehicleIdentity identity = identities[i];
                if (identity != null &&
                    identity.Role == LwsVehicleRole.Trailer &&
                    string.Equals(identity.VehicleId, trailerId, System.StringComparison.OrdinalIgnoreCase))
                {
                    _cachedTrailerId = trailerId;
                    _cachedTrailerTransform = identity.transform;
                    return _cachedTrailerTransform.position;
                }
            }

            _cachedTrailerId = string.Empty;
            _cachedTrailerTransform = null;
            return fallback;
        }

        private void EnsureFloatingOriginCoordinator()
        {
            if (floatingOriginCoordinator == null)
            {
                floatingOriginCoordinator = GetComponent<LwsFloatingOriginCoordinator>();
            }

            if (floatingOriginCoordinator == null)
            {
                floatingOriginCoordinator = gameObject.AddComponent<LwsFloatingOriginCoordinator>();
            }
        }
    }
}
