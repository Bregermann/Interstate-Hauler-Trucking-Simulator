using NWH.VehiclePhysics2.Input;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsNwhVehicleInputProvider : VehicleInputProviderBase
    {
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [SerializeField] private LwsTruckControlController truckControlController;
        [SerializeField] private bool validationGearMappingEnabled;

        private ILwsVehicleInputSource _inputSource;
        private bool _truckControlLookupAttempted;

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

        public override void Awake()
        {
            base.Awake();
            ResolveSource();
        }

        private void ResolveSource()
        {
            if (_inputSource == null && inputSourceBehaviour != null)
            {
                _inputSource = inputSourceBehaviour as ILwsVehicleInputSource;
            }

            if (truckControlController == null && !_truckControlLookupAttempted)
            {
                truckControlController = FindFirstObjectByType<LwsTruckControlController>();
                _truckControlLookupAttempted = true;
            }
        }

        public override float Steering()
        {
            ResolveSource();
            return _inputSource?.ReadContinuousInput().steering ?? 0f;
        }

        public override float Throttle()
        {
            ResolveSource();
            float input = _inputSource?.ReadContinuousInput().throttle ?? 0f;
            return truckControlController != null ? Mathf.Max(input, truckControlController.CurrentState.cruiseThrottleOutput) : input;
        }

        public override float Brakes()
        {
            ResolveSource();
            float input = _inputSource?.ReadContinuousInput().brake ?? 0f;
            return truckControlController != null ? Mathf.Max(input, truckControlController.CurrentState.cruiseBrakeOutput) : input;
        }

        public override float Handbrake()
        {
            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.CurrentState.parkingBrakeOn ? 1f : 0f;
            }

            return _inputSource?.ReadContinuousInput().parkingBrake ?? 0f;
        }

        public override float Clutch()
        {
            ResolveSource();
            return _inputSource?.ReadContinuousInput().clutch ?? 0f;
        }

        public override bool EngineStartStop()
        {
            ResolveSource();
            if (truckControlController != null)
            {
                return false;
            }

            return IsPressed(_inputSource?.ReadCommandFrame().ignition ?? LwsMomentaryIntent.None);
        }

        public override bool Horn()
        {
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
            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.LowBeamLights);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().lowBeamLights ?? LwsMomentaryIntent.None);
        }

        public override bool HighBeamLights()
        {
            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.HighBeamLights);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().highBeamLights ?? LwsMomentaryIntent.None);
        }

        public override bool HazardLights()
        {
            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.HazardLights);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().hazardLights ?? LwsMomentaryIntent.None);
        }

        public override bool LeftBlinker()
        {
            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.LeftBlinker);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().leftIndicator ?? LwsMomentaryIntent.None);
        }

        public override bool RightBlinker()
        {
            ResolveSource();
            if (truckControlController != null)
            {
                return truckControlController.ConsumeNativePulse(LwsTruckNativePulse.RightBlinker);
            }

            return IsPressed(_inputSource?.ReadCommandFrame().rightIndicator ?? LwsMomentaryIntent.None);
        }

        public override bool CruiseControl()
        {
            ResolveSource();
            if (truckControlController != null)
            {
                return false;
            }

            return IsPressed(_inputSource?.ReadCommandFrame().cruiseControl ?? LwsMomentaryIntent.None);
        }

        public override bool TrailerAttachDetach()
        {
            ResolveSource();
            if (truckControlController != null)
            {
                return false;
            }

            return IsPressed(_inputSource?.ReadCommandFrame().trailerAttachDetach ?? LwsMomentaryIntent.None);
        }

        public override int ShiftInto()
        {
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
