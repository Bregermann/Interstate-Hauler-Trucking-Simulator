using System;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class DeadAirVehicleAdapter : MonoBehaviour
    {
        [SerializeField] private LwsNwhVehicleAdapter lwsVehicleAdapter;
        [SerializeField] private LwsNwh18SpeedTransmissionAdapter nwhTransmissionAdapter;
        [SerializeField] private Lws18SpeedTransmissionController transmissionController;
        [SerializeField] private LwsKeyboardGamepadTruckInputSource sharedKeyboardGamepadInputSource;
        [SerializeField] private LwsNwhVehicleInputProvider nwhInputProvider;
        [SerializeField] private LwsNwhTrailerCouplingAdapter trailerCouplingAdapter;
        [SerializeField] private LwsNwhTruckControlAdapter truckControlAdapter;
        [SerializeField] private LwsTruckControlController truckControlController;
        [SerializeField] private DeadAirBasicAutomaticInputSource basicAutomaticInputSource;
        [SerializeField] private Rigidbody fallbackRigidbody;
        [SerializeField] private float movingThresholdMph = 1f;

        private bool _hornActive;
        private bool _wasHornActive;
        private bool _deadAirDrivingInputLocked;
        private GameObject _deadAirDeliveryTrailer;
        private ILwsVehicleInputService _inputService;
        private DeadAirTrailerConnectionStatus _trailerConnectionStatus = DeadAirTrailerConnectionStatus.NotFound;

        public event Action HornStarted;
        public event Action HornStopped;
        public DeadAirVehicleSnapshot CurrentSnapshot { get; private set; }
        public bool CameraCycleSuppressed => sharedKeyboardGamepadInputSource != null && sharedKeyboardGamepadInputSource.CameraCycleSuppressed ||
                                             basicAutomaticInputSource != null && !basicAutomaticInputSource.CameraCycleAllowed;
        public bool TrailerAttachedForDeadAir => trailerCouplingAdapter != null && trailerCouplingAdapter.IsTrailerAttached;
        public bool TrailerConsideredConnectedForDeadAir => TrailerAttachedForDeadAir;
        public bool DeadAirDrivingInputLocked => _deadAirDrivingInputLocked;
        public Lws18SpeedTransmissionController TransmissionController => transmissionController;
        public bool TransmissionAvailable => transmissionController != null;
        public bool TransmissionAutomatic => transmissionController != null && transmissionController.AutomaticModeActive;
        public bool InputSourceAvailable => sharedKeyboardGamepadInputSource != null || basicAutomaticInputSource != null;
        public bool SharedInputSourceAvailable => sharedKeyboardGamepadInputSource != null;
        public bool InputServiceAvailable
        {
            get
            {
                ResolveInputService();
                return _inputService != null;
            }
        }

        public bool NwhVehicleAvailable => ResolveVehicleController() != null;
        public bool NwhInputProviderAvailable => nwhInputProvider != null;
        public DeadAirTrailerConnectionStatus TrailerConnectionStatus => TrailerAttachedForDeadAir
            ? DeadAirTrailerConnectionStatus.Connected
            : _trailerConnectionStatus;
        public string CurrentTrailerId => trailerCouplingAdapter != null && !string.IsNullOrWhiteSpace(trailerCouplingAdapter.CurrentTrailerId)
            ? trailerCouplingAdapter.CurrentTrailerId
            : _deadAirDeliveryTrailer != null ? _deadAirDeliveryTrailer.name : string.Empty;

        private void Reset()
        {
            ResolveReferences(false);
        }

        private void Awake()
        {
            ResolveReferences(false);
        }

        private void Update()
        {
            CurrentSnapshot = CaptureSnapshot();
            if (CurrentSnapshot.hornActive != _wasHornActive)
            {
                if (CurrentSnapshot.hornActive)
                {
                    HornStarted?.Invoke();
                }
                else
                {
                    HornStopped?.Invoke();
                }

                _wasHornActive = CurrentSnapshot.hornActive;
            }
        }

        public bool ConfigureDeadAirTruckRig(Lws18SpeedTransmissionDefinition transmissionDefinition, out string message)
        {
            ResolveReferences(true);
            ConfigurePlayerTruckIdentity();
            VehicleController vehicleController = ResolveVehicleController();

            if (transmissionController == null)
            {
                message = "Dead Air player truck transmission controller is missing.";
                return false;
            }

            transmissionController.SetDefinition(transmissionDefinition);
            transmissionController.SetMode(Lws18SpeedTransmissionController.DevelopmentDefaultMode);

            if (sharedKeyboardGamepadInputSource != null)
            {
                sharedKeyboardGamepadInputSource.enabled = true;
                sharedKeyboardGamepadInputSource.SetDrivingInputSuppressed(false);
                sharedKeyboardGamepadInputSource.SetCameraCycleSuppressed(true);
                sharedKeyboardGamepadInputSource.ConfigureTransmissionController(transmissionController);
                transmissionController.SetFallbackInputSource(sharedKeyboardGamepadInputSource);
            }

            ResolveInputService();
            if (_inputService != null && sharedKeyboardGamepadInputSource != null)
            {
                LwsServiceResult result = _inputService.SetInputSource(sharedKeyboardGamepadInputSource, LwsVehicleInputOwner.KeyboardMouse, true);
                if (!result.Succeeded)
                {
                    Debug.LogWarning(result.Message, this);
                }
            }

            ILwsVehicleInputSource drivingInputSource = _inputService != null
                ? (ILwsVehicleInputSource)_inputService
                : sharedKeyboardGamepadInputSource;

            if (nwhInputProvider != null)
            {
                nwhInputProvider.enabled = true;
                nwhInputProvider.SetVehicleController(vehicleController);
                nwhInputProvider.SetInputSource(drivingInputSource);
                nwhInputProvider.SetTransmissionController(transmissionController);
                nwhInputProvider.SetTruckControlController(truckControlController);
                nwhInputProvider.SetValidationGearMappingEnabled(false);
            }

            if (truckControlAdapter != null)
            {
                truckControlAdapter.enabled = true;
            }

            if (truckControlController != null)
            {
                truckControlController.enabled = true;
            }

            if (basicAutomaticInputSource != null)
            {
                basicAutomaticInputSource.enabled = false;
                basicAutomaticInputSource.SetDrivingInputSuppressed(false);
                basicAutomaticInputSource.SetCameraCycleAllowed(false);
                basicAutomaticInputSource.SetTransmissionController(transmissionController);
            }

            _deadAirDrivingInputLocked = false;
            string automaticMessage;
            bool automaticSet = sharedKeyboardGamepadInputSource != null
                ? sharedKeyboardGamepadInputSource.TrySetInputMode(LwsTruckInputMode.BasicAutomatic, out automaticMessage)
                : transmissionController.TrySetAutomaticMode(out automaticMessage);

            if (!automaticSet)
            {
                Debug.LogWarning(automaticMessage, this);
            }

            if (vehicleController == null)
            {
                message = "Dead Air player truck NWH VehicleController is missing.";
                return false;
            }

            if (nwhInputProvider == null)
            {
                message = "Dead Air player truck NWH input provider is missing.";
                return false;
            }

            message = automaticSet
                ? "Dead Air player truck uses LWS Basic Automatic input through the NWH input provider."
                : automaticMessage;
            return automaticSet;
        }

        public bool EnableBasicAutomatic()
        {
            bool configured = ConfigureDeadAirTruckRig(null, out string message);
            if (!configured && !string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning(message, this);
            }

            return configured;
        }

        public void SetDeadAirCameraCycleSuppressed(bool suppressed)
        {
            ResolveReferences(false);
            if (sharedKeyboardGamepadInputSource != null)
            {
                sharedKeyboardGamepadInputSource.SetCameraCycleSuppressed(suppressed);
            }

            if (basicAutomaticInputSource != null)
            {
                basicAutomaticInputSource.SetCameraCycleAllowed(!suppressed);
            }
        }

        public void ConfigureDeadAirTrailerState(bool consideredConnected, GameObject deliveryTrailer)
        {
            ConfigureDeadAirTrailerStatus(TrailerAttachedForDeadAir
                ? DeadAirTrailerConnectionStatus.Connected
                : deliveryTrailer != null ? DeadAirTrailerConnectionStatus.Positioned : DeadAirTrailerConnectionStatus.NotFound,
                deliveryTrailer);
        }

        public void ConfigureDeadAirTrailerStatus(DeadAirTrailerConnectionStatus status, GameObject deliveryTrailer)
        {
            if (deliveryTrailer != null)
            {
                _deadAirDeliveryTrailer = deliveryTrailer;
            }

            _trailerConnectionStatus = TrailerAttachedForDeadAir
                ? DeadAirTrailerConnectionStatus.Connected
                : status == DeadAirTrailerConnectionStatus.Connected ? DeadAirTrailerConnectionStatus.Connecting : status;
        }

        public void RequestTrailerAttachDetach()
        {
            ResolveReferences(false);
            trailerCouplingAdapter?.RequestAttachDetach();
        }

        public void SetDeadAirDrivingInputLocked(bool locked)
        {
            ResolveReferences(false);
            _deadAirDrivingInputLocked = locked;
            if (sharedKeyboardGamepadInputSource != null)
            {
                sharedKeyboardGamepadInputSource.SetDrivingInputSuppressed(locked);
            }

            if (basicAutomaticInputSource != null)
            {
                basicAutomaticInputSource.SetDrivingInputSuppressed(locked);
            }
        }

        public DeadAirVehicleSnapshot CaptureSnapshot()
        {
            ResolveReferences(false);
            VehicleController vehicleController = ResolveVehicleController();
            bool nwhReady = lwsVehicleAdapter != null && lwsVehicleAdapter.IsReady;
            if (nwhReady)
            {
                LwsVehicleTelemetry telemetry = lwsVehicleAdapter.ReadTelemetry();
                LwsTransmissionDisplayState transmission = transmissionController != null ? transmissionController.DisplayState : default;
                _hornActive = ResolveHornActive();
                return new DeadAirVehicleSnapshot
                {
                    available = true,
                    truckFound = true,
                    inputSourceFound = InputSourceAvailable,
                    inputServiceFound = InputServiceAvailable,
                    transmissionFound = transmissionController != null,
                    transmissionAutomatic = transmissionController != null && transmissionController.AutomaticModeActive,
                    nwhVehicleFound = vehicleController != null,
                    throttleInput = telemetry.throttleInput,
                    brakeInput = telemetry.brakeInput,
                    steeringInput = telemetry.steeringInput,
                    speedMph = Mathf.Abs(telemetry.speedMetersPerSecond) * 2.23693629f,
                    signedSpeedMph = telemetry.signedSpeedMetersPerSecond * 2.23693629f,
                    position = telemetry.worldPosition,
                    forward = transform.forward,
                    moving = Mathf.Abs(telemetry.signedSpeedMetersPerSecond) * 2.23693629f >= movingThresholdMph,
                    reverse = telemetry.reverse || telemetry.signedSpeedMetersPerSecond < -0.25f,
                    hornActive = _hornActive,
                    transmissionMode = transmissionController != null ? transmission.mode.ToString() : "MISSING",
                    trailerStatus = TrailerConnectionStatus,
                    trailerConnected = TrailerAttachedForDeadAir
                };
            }

            Vector3 velocity = fallbackRigidbody != null ? fallbackRigidbody.linearVelocity : Vector3.zero;
            float signed = Vector3.Dot(velocity, transform.forward);
            _hornActive = ResolveHornActive();
            return new DeadAirVehicleSnapshot
            {
                available = fallbackRigidbody != null,
                truckFound = this != null,
                inputSourceFound = InputSourceAvailable,
                inputServiceFound = InputServiceAvailable,
                transmissionFound = transmissionController != null,
                transmissionAutomatic = transmissionController != null && transmissionController.AutomaticModeActive,
                nwhVehicleFound = vehicleController != null,
                throttleInput = vehicleController != null ? vehicleController.input.Throttle : 0f,
                brakeInput = vehicleController != null ? vehicleController.input.Brakes : 0f,
                steeringInput = vehicleController != null ? vehicleController.input.Steering : 0f,
                speedMph = velocity.magnitude * 2.23693629f,
                signedSpeedMph = signed * 2.23693629f,
                position = transform.position,
                forward = transform.forward,
                moving = velocity.magnitude * 2.23693629f >= movingThresholdMph,
                reverse = signed < -0.25f,
                hornActive = _hornActive,
                transmissionMode = transmissionController != null ? transmissionController.DisplayState.mode.ToString() : "MISSING",
                trailerStatus = TrailerConnectionStatus,
                trailerConnected = TrailerAttachedForDeadAir
            };
        }

        private bool ResolveHornActive()
        {
            if (sharedKeyboardGamepadInputSource != null)
            {
                LwsVehicleCommandFrame commands = sharedKeyboardGamepadInputSource.LastCommands;
                return LwsVehicleCommandFrameUtility.IsActive(commands.horn) ||
                       LwsVehicleCommandFrameUtility.IsActive(commands.airHorn);
            }

            if (basicAutomaticInputSource != null)
            {
                return basicAutomaticInputSource.HornHeld;
            }

            return false;
        }

        private void ResolveReferences(bool createMissing)
        {
            if (lwsVehicleAdapter == null) lwsVehicleAdapter = ResolveComponent<LwsNwhVehicleAdapter>(createMissing);
            if (nwhTransmissionAdapter == null) nwhTransmissionAdapter = ResolveComponent<LwsNwh18SpeedTransmissionAdapter>(createMissing);
            if (transmissionController == null) transmissionController = ResolveComponent<Lws18SpeedTransmissionController>(createMissing);
            if (sharedKeyboardGamepadInputSource == null) sharedKeyboardGamepadInputSource = ResolveComponent<LwsKeyboardGamepadTruckInputSource>(createMissing);
            if (nwhInputProvider == null) nwhInputProvider = ResolveComponent<LwsNwhVehicleInputProvider>(createMissing);
            if (trailerCouplingAdapter == null) trailerCouplingAdapter = ResolveComponent<LwsNwhTrailerCouplingAdapter>(createMissing);
            if (truckControlAdapter == null) truckControlAdapter = ResolveComponent<LwsNwhTruckControlAdapter>(createMissing);
            if (truckControlController == null) truckControlController = ResolveComponent<LwsTruckControlController>(createMissing);
            if (basicAutomaticInputSource == null) basicAutomaticInputSource = GetComponent<DeadAirBasicAutomaticInputSource>();
            if (fallbackRigidbody == null) fallbackRigidbody = GetComponent<Rigidbody>();
            ResolveInputService();
        }

        private T ResolveComponent<T>(bool createMissing) where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = GetComponentInChildren<T>(true);
            }

            if (component == null && createMissing)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private VehicleController ResolveVehicleController()
        {
            VehicleController vehicleController = null;
            if (lwsVehicleAdapter != null)
            {
                vehicleController = lwsVehicleAdapter.VehicleController;
            }

            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            if (vehicleController == null)
            {
                vehicleController = GetComponentInChildren<VehicleController>(true);
            }

            return vehicleController;
        }

        private void ResolveInputService()
        {
            if (_inputService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
        }

        private void ConfigurePlayerTruckIdentity()
        {
            LwsVehicleIdentity identity = GetComponent<LwsVehicleIdentity>();
            if (identity == null)
            {
                identity = gameObject.AddComponent<LwsVehicleIdentity>();
            }

            identity.Configure(
                "deadair.player.truck",
                "deadair.truck.player",
                LwsVehicleRole.PlayerTractor,
                "Dead Air Player Truck",
                true);
        }
    }
}