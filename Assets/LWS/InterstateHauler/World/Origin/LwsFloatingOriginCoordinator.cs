using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(-75)]
    [DisallowMultipleComponent]
    public sealed class LwsFloatingOriginCoordinator : MonoBehaviour
    {
        public const string ValidationTuningAssetPath = "Assets/LWS/InterstateHauler/World/Origin/Data/IH_FloatingOrigin_Validation.asset";

        [SerializeField] private LwsFloatingOriginTuning tuning;
        [SerializeField] private bool configureOnStart = true;
        [SerializeField] private bool monitorPlayerTruck = true;
        [SerializeField] private bool fallbackToTransformWhenNoPlayer = true;
        [SerializeField] private bool addDebugPanel = true;
        [SerializeField] private float trailerLookupIntervalSeconds = 1f;

        private ILwsWorldOriginService _originService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private ILwsVehicleRuntimeService _vehicleRuntimeService;
        private Transform _cachedTrailerTransform;
        private string _cachedTrailerId = string.Empty;
        private float _nextTrailerLookupTime;

        public ILwsWorldOriginService OriginService => _originService;
        public LwsFloatingOriginTuning Tuning => tuning;
        public string LastStatus { get; private set; } = "Floating origin not configured.";

        private void Awake()
        {
            ResolveTuning();
        }

        private void Start()
        {
            ResolveServices();
            if (configureOnStart)
            {
                ConfigureOriginService();
            }

            if (addDebugPanel && tuning != null && tuning.addDebugPanel &&
                GetComponent<LwsFloatingOriginDebugPanel>() == null)
            {
                gameObject.AddComponent<LwsFloatingOriginDebugPanel>();
            }
        }

        private void Update()
        {
            if (!monitorPlayerTruck)
            {
                return;
            }

            ResolveServices();
            if (_originService == null)
            {
                return;
            }

            Vector3 playerLocal = ResolvePlayerLocalPosition();
            _originService.UpdatePlayerLocalPosition(playerLocal);
            EnsurePlayerParticipants();

            if (_originService.ShouldShift(playerLocal))
            {
                Vector3 delta = _originService.CalculateShiftDelta(playerLocal);
                _originService.QueueShift(new LwsOriginShiftRequest(delta, "Player exceeded floating-origin threshold."));
            }
        }

        private void FixedUpdate()
        {
            if (_originService != null && _originService.TryExecuteQueuedShift(out LwsOriginShiftEvent shiftEvent))
            {
                LastStatus = shiftEvent.Message;
                if (tuning != null && tuning.developmentLogging)
                {
                    Debug.Log(
                        $"Floating origin shifted by {shiftEvent.GlobalShiftDelta}; offset is now {shiftEvent.NewOriginOffset}. Participants: {shiftEvent.ParticipantsShifted}.",
                        this);
                }
            }
        }

        public void Configure(LwsFloatingOriginTuning newTuning)
        {
            tuning = newTuning;
            ConfigureOriginService();
        }

        private void ConfigureOriginService()
        {
            ResolveServices();
            if (_originService == null)
            {
                LastStatus = "ILwsWorldOriginService is not registered.";
                Debug.LogWarning(LastStatus, this);
                return;
            }

            ResolveTuning();
            _originService.Configure(tuning);
            LastStatus = tuning != null && tuning.Validate(out string message)
                ? message
                : "Floating-origin tuning is missing or invalid.";
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _vehicleRuntimeService);
        }

        private Vector3 ResolvePlayerLocalPosition()
        {
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck != null)
            {
                return truck.transform.position;
            }

            return fallbackToTransformWhenNoPlayer ? transform.position : Vector3.zero;
        }

        private void EnsurePlayerParticipants()
        {
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                return;
            }

            EnsureRigidbodyParticipant(
                truck.gameObject,
                string.IsNullOrWhiteSpace(truck.VehicleId) ? "player.truck" : truck.VehicleId,
                LwsFloatingOriginParticipantKind.PlayerTractor);

            LwsVehicleTelemetry telemetry = _vehicleRuntimeService != null
                ? _vehicleRuntimeService.LastTelemetry
                : truck.LastTelemetry;
            if (!telemetry.trailerAttached && string.IsNullOrWhiteSpace(telemetry.trailerId))
            {
                return;
            }

            Transform trailer = ResolveTrailerTransform(telemetry.trailerId);
            if (trailer != null)
            {
                EnsureRigidbodyParticipant(
                    trailer.gameObject,
                    string.IsNullOrWhiteSpace(telemetry.trailerId) ? "player.trailer" : telemetry.trailerId,
                    LwsFloatingOriginParticipantKind.PlayerTrailer);
            }
        }

        private Transform ResolveTrailerTransform(string trailerId)
        {
            if (string.IsNullOrWhiteSpace(trailerId))
            {
                return _cachedTrailerTransform;
            }

            bool cacheMatches = _cachedTrailerTransform != null &&
                                string.Equals(_cachedTrailerId, trailerId, System.StringComparison.OrdinalIgnoreCase);
            if (cacheMatches)
            {
                return _cachedTrailerTransform;
            }

            if (Time.unscaledTime < _nextTrailerLookupTime)
            {
                return _cachedTrailerTransform;
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
                    return _cachedTrailerTransform;
                }
            }

            return _cachedTrailerTransform;
        }

        private static void EnsureRigidbodyParticipant(
            GameObject owner,
            string participantId,
            LwsFloatingOriginParticipantKind kind)
        {
            if (owner == null)
            {
                return;
            }

            LwsFloatingOriginRigidbodyParticipant participant =
                owner.GetComponent<LwsFloatingOriginRigidbodyParticipant>() ??
                owner.AddComponent<LwsFloatingOriginRigidbodyParticipant>();
            participant.Configure(participantId, kind, false);
        }

        private void ResolveTuning()
        {
            if (tuning != null)
            {
                return;
            }

#if UNITY_EDITOR
            tuning = AssetDatabase.LoadAssetAtPath<LwsFloatingOriginTuning>(ValidationTuningAssetPath);
#endif
            if (tuning == null)
            {
                tuning = LwsFloatingOriginTuning.CreateRuntimeDefault();
            }
        }
    }
}
