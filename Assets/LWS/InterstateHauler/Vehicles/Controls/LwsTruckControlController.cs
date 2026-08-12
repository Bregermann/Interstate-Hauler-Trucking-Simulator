using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(80)]
    [DisallowMultipleComponent]
    public sealed class LwsTruckControlController : MonoBehaviour
    {
        [SerializeField] private LwsTruckDefinition definition;
        [SerializeField] private LwsVehicleIdentity identity;
        [SerializeField] private LwsNwhVehicleAdapter vehicleAdapter;
        [SerializeField] private LwsNwhTruckControlAdapter nwhControlAdapter;
        [SerializeField] private LwsNwhVehicleInputProvider nwhInputProvider;
        [SerializeField] private LwsNwhTrailerCouplingAdapter trailerCoupling;
        [SerializeField] private Lws18SpeedTransmissionController transmissionController;
        [SerializeField] private LwsKeyboardGamepadTruckInputSource keyboardGamepadCommandSource;
        [SerializeField] private LwsPlayerGestureController gestureController;
        [SerializeField] private bool createKeyboardGamepadCommandSource = true;
        [SerializeField] private bool registerWithService = true;
        [SerializeField] private float cruiseSpeedStepMetersPerSecond = 0.44704f;

        private readonly HashSet<LwsTruckNativePulse> _nativePulses = new HashSet<LwsTruckNativePulse>();
        private ILwsVehicleInputService _inputService;
        private ILwsTruckControlService _truckControlService;
        private LwsTruckControlCapabilities _capabilities;
        private LwsTruckControlState _state;
        private bool _serviceRegistered;

        public LwsTruckControlState CurrentState => _state;
        public LwsTruckControlCapabilities Capabilities => _capabilities;
        public LwsPlayerGestureController GestureController => gestureController;
        public bool HasNativeCruiseControl => nwhControlAdapter != null && nwhControlAdapter.HasCruiseControl;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            _capabilities = definition != null
                ? definition.ControlCapabilities
                : LwsTruckControlCapabilities.NwhSemiDevelopmentDefault();
            _state = new LwsTruckControlState
            {
                vehicleId = identity != null ? identity.VehicleId : name,
                ignitionState = LwsIgnitionState.Off,
                wiperState = LwsWiperState.Off,
                turnSignal = LwsTurnSignalState.Off
            };
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (gestureController != null)
            {
                gestureController.GestureStarted += OnGestureStarted;
                gestureController.GestureEnded += OnGestureEnded;
            }
        }

        private void Start()
        {
            ResolveServices();
            RegisterWithServiceIfNeeded();
            if (nwhInputProvider != null)
            {
                nwhInputProvider.SetTruckControlController(this);
            }
        }

        private void OnDisable()
        {
            if (gestureController != null)
            {
                gestureController.GestureStarted -= OnGestureStarted;
                gestureController.GestureEnded -= OnGestureEnded;
            }

            if (_serviceRegistered && _truckControlService != null)
            {
                _truckControlService.ClearActiveController(this);
                _serviceRegistered = false;
            }
        }

        private void Update()
        {
            ResolveServices();
            RegisterWithServiceIfNeeded();

            LwsVehicleCommandFrame commands = ReadCombinedCommands();
            LwsVehicleContinuousInput continuous = _inputService != null
                ? _inputService.ReadContinuousInput()
                : default;
            ApplyCommandFrame(commands, continuous);
            SyncReadbackState(continuous);
            _truckControlService?.PublishState(this, _state);
        }

        public bool ConsumeNativePulse(LwsTruckNativePulse pulse)
        {
            if (!_nativePulses.Contains(pulse))
            {
                return false;
            }

            _nativePulses.Remove(pulse);
            return true;
        }

        public void ApplyCommandFrame(LwsVehicleCommandFrame commands, LwsVehicleContinuousInput continuous)
        {
            ApplyEngineCommands(commands);
            ApplyBrakeCommands(commands, continuous);
            ApplyLightCommands(commands);
            ApplyCabCommands(commands);
            ApplyDrivetrainCommands(commands, continuous);
            ApplyTrailerCommands(commands);
            ApplyCameraCommands(commands);
            ApplyGestureCommands(commands);
        }

        private void ApplyEngineCommands(LwsVehicleCommandFrame commands)
        {
            if (Pressed(commands.ignitionToggle) || Pressed(commands.ignition))
            {
                nwhControlAdapter?.ToggleIgnition();
                Publish("IgnitionToggle", string.Empty);
            }

            if (Pressed(commands.engineStart))
            {
                nwhControlAdapter?.StartEngine();
                Publish("EngineStart", string.Empty);
            }

            if (Pressed(commands.engineStop))
            {
                nwhControlAdapter?.StopEngine();
                Publish("EngineStop", string.Empty);
            }
        }

        private void ApplyBrakeCommands(LwsVehicleCommandFrame commands, LwsVehicleContinuousInput continuous)
        {
            if (Pressed(commands.parkingBrakeToggle) && _capabilities.supportsParkingBrake)
            {
                _state.parkingBrakeOn = !_state.parkingBrakeOn;
                Publish("ParkingBrake", _state.parkingBrakeOn.ToString());
            }

            _state.serviceBrakeInput = continuous.brake;
            _state.trailerBrakeHeld = Active(commands.trailerBrake) && _capabilities.supportsIndependentTrailerBrake;
        }

        private void ApplyLightCommands(LwsVehicleCommandFrame commands)
        {
            if (Pressed(commands.lowBeamLights) && _capabilities.supportsHeadlights)
            {
                _state.headlightsOn = !_state.headlightsOn;
                if (!_state.headlightsOn)
                {
                    _state.highBeamsOn = false;
                }

                Pulse(LwsTruckNativePulse.LowBeamLights);
                Publish("Headlights", _state.headlightsOn.ToString());
            }

            if (Pressed(commands.highBeamLights) && _capabilities.supportsHighBeams)
            {
                _state.highBeamsOn = !_state.highBeamsOn;
                if (_state.highBeamsOn)
                {
                    _state.headlightsOn = true;
                }

                Pulse(LwsTruckNativePulse.HighBeamLights);
                Publish("HighBeams", _state.highBeamsOn.ToString());
            }

            if (Pressed(commands.leftIndicator) && _capabilities.supportsTurnSignals)
            {
                ClearHazardsBeforeSignal();
                _state.turnSignal = _state.turnSignal == LwsTurnSignalState.Left ? LwsTurnSignalState.Off : LwsTurnSignalState.Left;
                Pulse(LwsTruckNativePulse.LeftBlinker);
                Publish("TurnSignal", _state.turnSignal.ToString());
            }

            if (Pressed(commands.rightIndicator) && _capabilities.supportsTurnSignals)
            {
                ClearHazardsBeforeSignal();
                _state.turnSignal = _state.turnSignal == LwsTurnSignalState.Right ? LwsTurnSignalState.Off : LwsTurnSignalState.Right;
                Pulse(LwsTruckNativePulse.RightBlinker);
                Publish("TurnSignal", _state.turnSignal.ToString());
            }

            if (Pressed(commands.hazardLights) && _capabilities.supportsHazards)
            {
                _state.hazardsOn = !_state.hazardsOn;
                _state.turnSignal = LwsTurnSignalState.Off;
                Pulse(LwsTruckNativePulse.HazardLights);
                Publish("Hazards", _state.hazardsOn.ToString());
            }
        }

        private void ApplyCabCommands(LwsVehicleCommandFrame commands)
        {
            if (Pressed(commands.wipers) || Pressed(commands.wiperIncrease))
            {
                _state.wiperState = NextWiperState(_state.wiperState);
                Publish("Wipers", _state.wiperState.ToString());
            }

            if (Pressed(commands.wiperDecrease))
            {
                _state.wiperState = PreviousWiperState(_state.wiperState);
                Publish("Wipers", _state.wiperState.ToString());
            }

            _state.hornActive = Active(commands.horn);
            _state.airHornActive = Active(commands.airHorn);
        }

        private void ApplyDrivetrainCommands(LwsVehicleCommandFrame commands, LwsVehicleContinuousInput continuous)
        {
            if (Pressed(commands.engineBrake) && _capabilities.supportsEngineBrake)
            {
                _state.engineBrakeLevel = _state.engineBrakeLevel > 0 ? 0 : Mathf.Max(1, _capabilities.engineBrakeLevels);
                Publish("EngineBrake", _state.engineBrakeLevel.ToString());
            }

            if (Pressed(commands.engineBrakeIncrease) && _capabilities.supportsEngineBrake)
            {
                _state.engineBrakeLevel = Mathf.Clamp(_state.engineBrakeLevel + 1, 0, _capabilities.engineBrakeLevels);
                Publish("EngineBrake", _state.engineBrakeLevel.ToString());
            }

            if (Pressed(commands.engineBrakeDecrease) && _capabilities.supportsEngineBrake)
            {
                _state.engineBrakeLevel = Mathf.Clamp(_state.engineBrakeLevel - 1, 0, _capabilities.engineBrakeLevels);
                Publish("EngineBrake", _state.engineBrakeLevel.ToString());
            }

            if (Pressed(commands.retarder) || Pressed(commands.retarderIncrease))
            {
                if (_capabilities.supportsRetarder)
                {
                    _state.retarderLevel = Mathf.Clamp(_state.retarderLevel + 1, 0, _capabilities.retarderLevels);
                    Publish("Retarder", _state.retarderLevel.ToString());
                }
            }

            if (Pressed(commands.retarderDecrease) && _capabilities.supportsRetarder)
            {
                _state.retarderLevel = Mathf.Clamp(_state.retarderLevel - 1, 0, _capabilities.retarderLevels);
                Publish("Retarder", _state.retarderLevel.ToString());
            }

            if (Pressed(commands.differentialLock) && _capabilities.supportsDifferentialLock)
            {
                _state.differentialLocked = !_state.differentialLocked;
                nwhControlAdapter?.SetDifferentialLocked(_state.differentialLocked);
                Publish("DifferentialLock", _state.differentialLocked.ToString());
            }

            ApplyCruiseCommands(commands, continuous);
        }

        private void ApplyCruiseCommands(LwsVehicleCommandFrame commands, LwsVehicleContinuousInput continuous)
        {
            if (_state.cruiseEnabled && (continuous.brake > 0.05f || continuous.clutch > 0.75f))
            {
                CancelCruise("BrakeOrClutch");
            }

            if (Pressed(commands.cruiseCancel))
            {
                CancelCruise("Cancel");
            }

            if ((Pressed(commands.cruiseControl) || Pressed(commands.cruiseSet)) && _capabilities.supportsCruiseControl)
            {
                if (_state.cruiseEnabled && Pressed(commands.cruiseControl))
                {
                    CancelCruise("ToggleOff");
                }
                else
                {
                    float target = CurrentSpeedMetersPerSecond();
                    _state.cruiseTargetSpeedMetersPerSecond = Mathf.Max(target, 1f);
                    _state.cruiseEnabled = true;
                    ApplyCruiseBackend(true);
                    Publish("CruiseSet", _state.cruiseTargetSpeedMetersPerSecond.ToString("0.0"));
                }
            }

            if (Pressed(commands.cruiseResume) && _capabilities.supportsCruiseControl && _state.cruiseTargetSpeedMetersPerSecond > 0.1f)
            {
                _state.cruiseEnabled = true;
                ApplyCruiseBackend(true);
                Publish("CruiseResume", _state.cruiseTargetSpeedMetersPerSecond.ToString("0.0"));
            }

            if (Pressed(commands.cruiseIncrease) && _capabilities.supportsCruiseControl)
            {
                _state.cruiseTargetSpeedMetersPerSecond = Mathf.Max(1f, _state.cruiseTargetSpeedMetersPerSecond + cruiseSpeedStepMetersPerSecond);
                ApplyCruiseBackend(_state.cruiseEnabled);
                Publish("CruiseIncrease", _state.cruiseTargetSpeedMetersPerSecond.ToString("0.0"));
            }

            if (Pressed(commands.cruiseDecrease) && _capabilities.supportsCruiseControl)
            {
                _state.cruiseTargetSpeedMetersPerSecond = Mathf.Max(0f, _state.cruiseTargetSpeedMetersPerSecond - cruiseSpeedStepMetersPerSecond);
                ApplyCruiseBackend(_state.cruiseEnabled && _state.cruiseTargetSpeedMetersPerSecond > 0.1f);
                Publish("CruiseDecrease", _state.cruiseTargetSpeedMetersPerSecond.ToString("0.0"));
            }

            UpdateLwsCruiseAssistOutput(continuous);
        }

        private void ApplyTrailerCommands(LwsVehicleCommandFrame commands)
        {
            if (Pressed(commands.trailerAttachDetach) && _capabilities.supportsTrailerAttachDetach)
            {
                trailerCoupling?.RequestAttachDetach();
                Publish("TrailerAttachDetach", string.Empty);
            }
        }

        private void ApplyCameraCommands(LwsVehicleCommandFrame commands)
        {
            _state.cameraCycleRequested = false;
            _state.lookResetRequested = false;
            if (Pressed(commands.cameraCycle) && _capabilities.supportsCameraCycle)
            {
                _state.cameraCycleRequested = true;
                nwhControlAdapter?.CycleCamera();
                Publish("CameraCycle", string.Empty);
            }

            if (Pressed(commands.lookReset) && _capabilities.supportsLookReset)
            {
                _state.lookResetRequested = true;
                nwhControlAdapter?.ResetLook();
                Publish("LookReset", string.Empty);
            }
        }

        private void ApplyGestureCommands(LwsVehicleCommandFrame commands)
        {
            if (!Pressed(commands.flipOffDriver) || !_capabilities.supportsFlipOffDriver || gestureController == null)
            {
                return;
            }

            bool accepted = gestureController.RequestFlipOff(out LwsDriverGestureEvent gestureEvent);
            _state.lastGesture = LwsTruckGestureType.FlipOffDriver;
            _state.lastGestureHadTarget = gestureEvent.hasValidTarget;
            _state.lastGestureTargetId = gestureEvent.targetVehicleId;
            _state.lastGestureTargetDistance = gestureEvent.targetDistanceMeters;
            Publish("FlipOffDriver", accepted ? "Accepted" : "Cooldown");
        }

        private void SyncReadbackState(LwsVehicleContinuousInput continuous)
        {
            _state.vehicleId = identity != null ? identity.VehicleId : name;
            _state.inputOwner = _inputService != null ? _inputService.ActiveOwner : LwsVehicleInputOwner.None;
            _state.inputSourceId = _inputService != null ? _inputService.ActiveSourceId : string.Empty;
            _state.ignitionState = nwhControlAdapter != null ? nwhControlAdapter.ReadIgnitionState() : _state.ignitionState;
            _state.engineRunning = nwhControlAdapter != null && nwhControlAdapter.ReadEngineRunning();
            _state.engineStalled = nwhControlAdapter != null && nwhControlAdapter.ReadEngineStalled();
            _state.headlightsOn = nwhControlAdapter != null ? nwhControlAdapter.ReadHeadlightsOn() : _state.headlightsOn;
            _state.highBeamsOn = nwhControlAdapter != null ? nwhControlAdapter.ReadHighBeamsOn() : _state.highBeamsOn;
            if (HasNativeCruiseControl)
            {
                _state.cruiseEnabled = nwhControlAdapter != null && nwhControlAdapter.ReadCruiseEnabled();
                if (_state.cruiseEnabled && nwhControlAdapter != null)
                {
                    _state.cruiseTargetSpeedMetersPerSecond = nwhControlAdapter.ReadCruiseTargetSpeed();
                }
            }

            _state.trailerAttached = trailerCoupling != null && trailerCoupling.IsTrailerAttached;
            _state.trailerId = trailerCoupling != null ? trailerCoupling.CurrentTrailerId : string.Empty;
            _state.gestureCooldownRemaining = gestureController != null ? gestureController.CooldownRemaining : 0f;
            _state.transmission = transmissionController != null ? transmissionController.DisplayState : default;
            _state.serviceBrakeInput = continuous.brake;
        }

        private LwsVehicleCommandFrame ReadCombinedCommands()
        {
            LwsVehicleCommandFrame combined = _inputService != null ? _inputService.ReadCommandFrame() : default;
            if (keyboardGamepadCommandSource != null)
            {
                combined = LwsVehicleCommandFrameUtility.Combine(combined, keyboardGamepadCommandSource.ReadCommandFrame());
            }

            return combined;
        }

        private void CancelCruise(string reason)
        {
            _state.cruiseEnabled = false;
            _state.cruiseThrottleOutput = 0f;
            _state.cruiseBrakeOutput = 0f;
            nwhControlAdapter?.CancelCruise();
            Publish("CruiseCancel", reason);
        }

        private void ApplyCruiseBackend(bool enabled)
        {
            if (HasNativeCruiseControl)
            {
                nwhControlAdapter.SetCruise(enabled, _state.cruiseTargetSpeedMetersPerSecond);
            }
        }

        private void UpdateLwsCruiseAssistOutput(LwsVehicleContinuousInput continuous)
        {
            if (!_capabilities.supportsCruiseControl || !_state.cruiseEnabled || HasNativeCruiseControl)
            {
                _state.cruiseThrottleOutput = 0f;
                _state.cruiseBrakeOutput = 0f;
                return;
            }

            if (continuous.throttle > 0.05f)
            {
                _state.cruiseThrottleOutput = 0f;
                _state.cruiseBrakeOutput = 0f;
                return;
            }

            float speedError = _state.cruiseTargetSpeedMetersPerSecond - CurrentSpeedMetersPerSecond();
            _state.cruiseThrottleOutput = Mathf.Clamp01(speedError * 0.18f);
            _state.cruiseBrakeOutput = speedError < -1.5f ? Mathf.Clamp01(-speedError * 0.08f) : 0f;
        }

        private void ClearHazardsBeforeSignal()
        {
            if (!_state.hazardsOn)
            {
                return;
            }

            _state.hazardsOn = false;
            Pulse(LwsTruckNativePulse.HazardLights);
        }

        private float CurrentSpeedMetersPerSecond()
        {
            if (vehicleAdapter == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, vehicleAdapter.ReadTelemetry().signedSpeedMetersPerSecond);
        }

        private void Pulse(LwsTruckNativePulse pulse)
        {
            _nativePulses.Add(pulse);
        }

        private void Publish(string controlName, string value)
        {
            _truckControlService?.PublishControlEvent(this, controlName, value);
        }

        private void OnGestureStarted(LwsDriverGestureEvent gestureEvent)
        {
            _truckControlService?.PublishDriverGesture(this, gestureEvent);
        }

        private void OnGestureEnded(LwsDriverGestureEvent gestureEvent)
        {
            Publish("GestureEnded", gestureEvent.gestureType.ToString());
        }

        private void ResolveReferences()
        {
            if (definition == null)
            {
                LwsPlayerTruck truck = GetComponent<LwsPlayerTruck>();
                definition = truck != null ? truck.Definition : null;
            }

            if (identity == null) identity = GetComponent<LwsVehicleIdentity>();
            if (vehicleAdapter == null) vehicleAdapter = GetComponent<LwsNwhVehicleAdapter>();
            if (nwhControlAdapter == null) nwhControlAdapter = GetComponent<LwsNwhTruckControlAdapter>();
            if (nwhInputProvider == null) nwhInputProvider = FindFirstObjectByType<LwsNwhVehicleInputProvider>();
            if (trailerCoupling == null) trailerCoupling = GetComponent<LwsNwhTrailerCouplingAdapter>();
            if (transmissionController == null) transmissionController = GetComponent<Lws18SpeedTransmissionController>();
            if (gestureController == null) gestureController = GetComponent<LwsPlayerGestureController>();

            if (keyboardGamepadCommandSource == null && createKeyboardGamepadCommandSource)
            {
                keyboardGamepadCommandSource = GetComponent<LwsKeyboardGamepadTruckInputSource>();
                if (keyboardGamepadCommandSource == null)
                {
                    keyboardGamepadCommandSource = gameObject.AddComponent<LwsKeyboardGamepadTruckInputSource>();
                }
            }
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            if (_inputService == null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
            }

            if (_truckControlService == null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _truckControlService);
            }
        }

        private void RegisterWithServiceIfNeeded()
        {
            if (!registerWithService || _serviceRegistered || _truckControlService == null)
            {
                return;
            }

            LwsServiceResult result = _truckControlService.RegisterActiveController(this);
            _serviceRegistered = result.Succeeded;
            if (!result.Succeeded)
            {
                Debug.LogError(result.Message, this);
            }
        }

        private static bool Pressed(LwsMomentaryIntent intent)
        {
            return LwsVehicleCommandFrameUtility.IsPressed(intent);
        }

        private static bool Active(LwsMomentaryIntent intent)
        {
            return LwsVehicleCommandFrameUtility.IsActive(intent);
        }

        private static LwsWiperState NextWiperState(LwsWiperState state)
        {
            return state == LwsWiperState.High ? LwsWiperState.Off : (LwsWiperState)((int)state + 1);
        }

        private static LwsWiperState PreviousWiperState(LwsWiperState state)
        {
            return state == LwsWiperState.Off ? LwsWiperState.High : (LwsWiperState)((int)state - 1);
        }
    }
}
