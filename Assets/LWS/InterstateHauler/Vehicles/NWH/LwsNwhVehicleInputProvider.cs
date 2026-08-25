using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Input;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsNwhVehicleInputProvider : VehicleInputProviderBase
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [SerializeField] private LwsTruckControlController truckControlController;
        [SerializeField] private Lws18SpeedTransmissionController transmissionController;
        [SerializeField] private bool validationGearMappingEnabled;
        [SerializeField] private bool translateSemanticInputForNwhReverse = true;
        [SerializeField] private bool logDriveInputDiagnostics = true;

        private ILwsVehicleInputSource _inputSource;
        private ILwsVehicleInputService _inputService;
        private bool _truckControlLookupAttempted;
        private bool _forwardDiagnosticLogged;
        private bool _reverseDiagnosticLogged;

        public LwsVehicleContinuousInput LastSemanticContinuousInput { get; private set; }
        public LwsVehicleContinuousInput LastNwhContinuousInput { get; private set; }

        public void SetInputSource(ILwsVehicleInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public void SetValidationGearMappingEnabled(bool enabled)
        {
            validationGearMappingEnabled = enabled;
        }

        public void SetTruckControlController(LwsTruckControlController controller)
        {
            truckControlController = controller;
            _truckControlLookupAttempted = controller != null;
        }

        public void SetTransmissionController(Lws18SpeedTransmissionController controller)
        {
            transmissionController = controller;
        }

        public void SetVehicleController(VehicleController controller)
        {
            vehicleController = controller;
        }

        public override void Awake()
        {
            base.Awake();
            ResolveSource();
        }

        private void ResolveSource()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            if (transmissionController == null)
            {
                transmissionController = GetComponent<Lws18SpeedTransmissionController>();
            }

            if (_inputSource == null && inputSourceBehaviour != null)
            {
                _inputSource = inputSourceBehaviour as ILwsVehicleInputSource;
            }

            if (_inputSource == null)
            {
                ResolveInputService();
                if (_inputService != null && _inputService.HasActiveSource)
                {
                    _inputSource = _inputService;
                }
            }

            if (truckControlController == null && !_truckControlLookupAttempted)
            {
                truckControlController = GetComponent<LwsTruckControlController>();
                if (truckControlController == null)
                {
                    truckControlController = FindFirstObjectByType<LwsTruckControlController>();
                }

                _truckControlLookupAttempted = true;
            }
        }

        public override float Steering()
        {
            if (!isActiveAndEnabled)
            {
                return 0f;
            }

            return ReadNwhContinuousInput().steering;
        }

        public override float Throttle()
        {
            if (!isActiveAndEnabled)
            {
                return 0f;
            }

            LwsVehicleContinuousInput input = ReadNwhContinuousInput();
            return input.throttle;
        }

        public override float Brakes()
        {
            if (!isActiveAndEnabled)
            {
                return 0f;
            }

            LwsVehicleContinuousInput input = ReadNwhContinuousInput();
            return input.brake;
        }

        public override float Handbrake()
        {
            if (!isActiveAndEnabled)
            {
                return 0f;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.CurrentState.parkingBrakeOn ? 1f : 0f;
            }

            return _inputSource?.ReadContinuousInput().parkingBrake ?? 0f;
        }

        public override float Clutch()
        {
            if (!isActiveAndEnabled)
            {
                return 0f;
            }

            return ReadNwhContinuousInput().clutch;
        }

        public override bool EngineStartStop()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return false;
            }

            return IsPressed(_inputSource?.ReadCommandFrame().ignition ?? LwsMomentaryIntent.None);
        }

        public override bool Horn()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                LwsTruckControlState state = truckControlController.CurrentState;
                return state.hornActive || state.airHornActive;
            }

            return IsActive(_inputSource?.ReadCommandFrame().horn ?? LwsMomentaryIntent.None);
        }

        public override bool LowBeamLights()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.LowBeamLights);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().lowBeamLights ?? LwsMomentaryIntent.None);
        }

        public override bool HighBeamLights()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.HighBeamLights);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().highBeamLights ?? LwsMomentaryIntent.None);
        }

        public override bool HazardLights()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.HazardLights);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().hazardLights ?? LwsMomentaryIntent.None);
        }

        public override bool LeftBlinker()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.LeftBlinker);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().leftIndicator ?? LwsMomentaryIntent.None);
        }

        public override bool RightBlinker()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.RightBlinker);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().rightIndicator ?? LwsMomentaryIntent.None);
        }

        public override bool CruiseControl()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return false;
            }

            return IsPressed(_inputSource?.ReadCommandFrame().cruiseControl ?? LwsMomentaryIntent.None);
        }

        public override bool TrailerAttachDetach()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            ResolveSource();
            if (truckControlController != null)
            {
                return false;
            }

            return IsPressed(_inputSource?.ReadCommandFrame().trailerAttachDetach ?? LwsMomentaryIntent.None);
        }

        public override int ShiftInto()
        {
            if (!isActiveAndEnabled)
            {
                return -999;
            }

            ResolveSource();
            if (_inputSource == null || !validationGearMappingEnabled)
            {
                return -999;
            }

            LwsTruckGearIntent intent = _inputSource.ReadGearIntent();
            if (intent.neutralRequested)
            {
                return 0;
            }

            if (intent.reverseRequested)
            {
                return -1;
            }

            // Validation-only path retained for tests; Truck18Speed owns production shifting.
            return intent.requestedLogicalGear >= 1 && intent.requestedLogicalGear <= 8
                ? intent.requestedLogicalGear
                : -999;
        }

        public static LwsVehicleContinuousInput TranslateSemanticInputForNwh(
            LwsVehicleContinuousInput semanticInput,
            bool swapThrottleBrakeInReverse,
            bool reverseDriveState)
        {
            return LwsNwhInputAxisTranslator.TranslateSemanticInputForNwh(
                semanticInput,
                swapThrottleBrakeInReverse,
                reverseDriveState);
        }

        private LwsVehicleContinuousInput ReadNwhContinuousInput()
        {
            ResolveSource();
            LwsVehicleContinuousInput semanticInput = _inputSource?.ReadContinuousInput() ?? default;
            if (truckControlController != null)
            {
                LwsTruckControlState state = truckControlController.CurrentState;
                semanticInput.throttle = Mathf.Max(semanticInput.throttle, state.cruiseThrottleOutput);
                semanticInput.brake = Mathf.Max(semanticInput.brake, state.cruiseBrakeOutput);
            }

            bool reverseDriveState = IsNwhReverseDriveState();
            bool swapInReverse = vehicleController == null ||
                                 vehicleController.input == null ||
                                 vehicleController.input.swapInputInReverse;
            LwsVehicleContinuousInput nwhInput = LwsNwhInputAxisTranslator.TranslateSemanticInputForNwh(
                semanticInput,
                translateSemanticInputForNwhReverse && swapInReverse,
                reverseDriveState);

            LastSemanticContinuousInput = semanticInput;
            LastNwhContinuousInput = nwhInput;
            LogInputDiagnosticIfNeeded(semanticInput, nwhInput, reverseDriveState);
            return nwhInput;
        }

        private bool IsNwhReverseDriveState()
        {
            if (transmissionController != null)
            {
                LwsTransmissionDisplayState state = transmissionController.DisplayState;
                if (transmissionController.AutomaticModeActive && state.automaticSelector == LwsAutomaticTransmissionSelector.Reverse)
                {
                    return true;
                }

                if (state.nwhGear < 0)
                {
                    return true;
                }
            }

            return vehicleController != null &&
                   vehicleController.powertrain.transmission != null &&
                   vehicleController.powertrain.transmission.Gear < 0;
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

        private void LogInputDiagnosticIfNeeded(
            LwsVehicleContinuousInput semanticInput,
            LwsVehicleContinuousInput nwhInput,
            bool reverseDriveState)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!logDriveInputDiagnostics)
            {
                return;
            }

            bool forwardDrive = !reverseDriveState && semanticInput.throttle > 0.05f;
            bool reverseDrive = reverseDriveState && semanticInput.throttle > 0.05f;
            if ((!forwardDrive || _forwardDiagnosticLogged) && (!reverseDrive || _reverseDiagnosticLogged))
            {
                return;
            }

            if (forwardDrive)
            {
                _forwardDiagnosticLogged = true;
            }

            if (reverseDrive)
            {
                _reverseDiagnosticLogged = true;
            }

            int nwhGear = vehicleController != null && vehicleController.powertrain.transmission != null
                ? vehicleController.powertrain.transmission.Gear
                : 0;
            float ratio = vehicleController != null && vehicleController.powertrain.transmission != null
                ? vehicleController.powertrain.transmission.currentGearRatio
                : 0f;
            float speed = vehicleController != null ? vehicleController.SpeedSigned : 0f;
            string selector = transmissionController != null
                ? transmissionController.AutomaticSelector.ToString()
                : "Unknown";
            Debug.Log(
                $"[IH Truck Input] Semantic THR {semanticInput.throttle:0.00} BRK {semanticInput.brake:0.00} STR {semanticInput.steering:0.00} -> NWH THR {nwhInput.throttle:0.00} BRK {nwhInput.brake:0.00}; selector {selector}; NWH gear {nwhGear}; ratio {ratio:0.000}; speed {speed:0.00} m/s.",
                this);
#endif
        }

        private static bool IsPressed(LwsMomentaryIntent intent)
        {
            return intent == LwsMomentaryIntent.Pressed;
        }

        private static bool IsActive(LwsMomentaryIntent intent)
        {
            return intent == LwsMomentaryIntent.Pressed || intent == LwsMomentaryIntent.Held;
        }
    }
}
