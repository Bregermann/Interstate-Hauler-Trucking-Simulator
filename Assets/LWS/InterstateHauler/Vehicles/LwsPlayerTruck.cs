using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LwsVehicleIdentity))]
    public sealed class LwsPlayerTruck : MonoBehaviour
    {
        [SerializeField] private LwsVehicleIdentity identity;
        [SerializeField] private LwsTruckDefinition definition;
        [SerializeField] private LwsNwhVehicleAdapter nwhAdapter;
        [SerializeField] private LwsNwhTrailerCouplingAdapter couplingAdapter;
        [SerializeField] private bool registerWithBootstrap = true;

        private ILwsPlayerVehicleService _playerVehicleService;
        private ILwsVehicleRuntimeService _runtimeService;
        private bool _readyPublished;

        public string VehicleId => identity != null ? identity.VehicleId : string.Empty;
        public string DefinitionId => definition != null ? definition.StableId : identity != null ? identity.DefinitionId : string.Empty;
        public LwsTruckDefinition Definition => definition;
        public LwsNwhVehicleAdapter NwhAdapter => nwhAdapter;
        public LwsNwhTrailerCouplingAdapter CouplingAdapter => couplingAdapter;
        public bool IsReady => nwhAdapter != null && nwhAdapter.VehicleController != null;
        public LwsVehicleTelemetry LastTelemetry { get; private set; }

        private void Reset()
        {
            ResolveLocalReferences();
        }

        private void Awake()
        {
            ResolveLocalReferences();
        }

        private void Start()
        {
            ResolveServices();
            if (_playerVehicleService != null)
            {
                LwsServiceResult result = _playerVehicleService.RegisterActiveTruck(this);
                if (!result.Succeeded)
                {
                    Debug.LogError(result.Message, this);
                }
            }

            PublishReadyIfPossible();
        }

        private void OnDestroy()
        {
            _playerVehicleService?.ClearActiveTruck(this);
        }

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            LastTelemetry = nwhAdapter.ReadTelemetry();
            _runtimeService?.PublishTelemetry(LastTelemetry);
            _playerVehicleService?.PublishEngineState(this, LastTelemetry.engineRunning);

            if (couplingAdapter != null)
            {
                _playerVehicleService?.PublishTrailerState(this, couplingAdapter.CurrentState);
            }

            PublishReadyIfPossible();
        }

        public void Configure(LwsTruckDefinition truckDefinition, string vehicleId)
        {
            definition = truckDefinition;
            ResolveLocalReferences();
            if (identity != null && truckDefinition != null)
            {
                identity.Configure(
                    vehicleId,
                    truckDefinition.StableId,
                    LwsVehicleRole.PlayerTractor,
                    truckDefinition.DisplayName,
                    true);
            }
        }

        public LwsPlayerTruckState CaptureState()
        {
            LwsVehicleTelemetry telemetry = nwhAdapter != null ? nwhAdapter.ReadTelemetry() : LastTelemetry;
            VehicleController vehicleController = nwhAdapter != null ? nwhAdapter.VehicleController : null;
            Rigidbody rb = vehicleController != null ? vehicleController.vehicleRigidbody : GetComponent<Rigidbody>();

            return new LwsPlayerTruckState
            {
                vehicleId = telemetry.vehicleId,
                truckDefinitionId = telemetry.definitionId,
                pose = LwsSerializablePose.FromTransform(transform),
                linearVelocity = rb != null ? rb.linearVelocity : Vector3.zero,
                angularVelocity = rb != null ? rb.angularVelocity : Vector3.zero,
                engineRunning = telemetry.engineRunning,
                nwhGear = telemetry.currentGear,
                nwhGearName = telemetry.currentGearName,
                trailerAttachment = couplingAdapter != null ? couplingAdapter.CurrentState : default
            };
        }

        private void ResolveLocalReferences()
        {
            if (identity == null)
            {
                identity = GetComponent<LwsVehicleIdentity>();
            }

            if (nwhAdapter == null)
            {
                nwhAdapter = GetComponent<LwsNwhVehicleAdapter>();
            }

            if (couplingAdapter == null)
            {
                couplingAdapter = GetComponent<LwsNwhTrailerCouplingAdapter>();
            }
        }

        private void ResolveServices()
        {
            if (!registerWithBootstrap || LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _runtimeService);
        }

        private void PublishReadyIfPossible()
        {
            if (_readyPublished || !IsReady)
            {
                return;
            }

            _readyPublished = true;
            _playerVehicleService?.PublishTruckReady(this);
        }
    }
}
