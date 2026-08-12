using NWH.VehiclePhysics2.Input;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsNwhVehicleInputProvider : VehicleInputProviderBase
    {
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [SerializeField] private bool validationGearMappingEnabled;

        private ILwsVehicleInputSource _inputSource;

        public void SetInputSource(ILwsVehicleInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public void SetValidationGearMappingEnabled(bool enabled)
        {
            validationGearMappingEnabled = enabled;
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
        }

        public override float Steering()
        {
            ResolveSource();
            return _inputSource?.ReadContinuousInput().steering ?? 0f;
        }

        public override float Throttle()
        {
            ResolveSource();
            return _inputSource?.ReadContinuousInput().throttle ?? 0f;
        }

        public override float Brakes()
        {
            ResolveSource();
            return _inputSource?.ReadContinuousInput().brake ?? 0f;
        }

        public override float Handbrake()
        {
            ResolveSource();
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
            return IsPressed(_inputSource?.ReadCommandFrame().ignition ?? LwsMomentaryIntent.None);
        }

        public override bool Horn()
        {
            ResolveSource();
            return IsActive(_inputSource?.ReadCommandFrame().horn ?? LwsMomentaryIntent.None);
        }

        public override bool LowBeamLights()
        {
            ResolveSource();
            return IsPressed(_inputSource?.ReadCommandFrame().lowBeamLights ?? LwsMomentaryIntent.None);
        }

        public override bool HighBeamLights()
        {
            ResolveSource();
            return IsPressed(_inputSource?.ReadCommandFrame().highBeamLights ?? LwsMomentaryIntent.None);
        }

        public override bool HazardLights()
        {
            ResolveSource();
            return IsPressed(_inputSource?.ReadCommandFrame().hazardLights ?? LwsMomentaryIntent.None);
        }

        public override bool LeftBlinker()
        {
            ResolveSource();
            return IsPressed(_inputSource?.ReadCommandFrame().leftIndicator ?? LwsMomentaryIntent.None);
        }

        public override bool RightBlinker()
        {
            ResolveSource();
            return IsPressed(_inputSource?.ReadCommandFrame().rightIndicator ?? LwsMomentaryIntent.None);
        }

        public override bool CruiseControl()
        {
            ResolveSource();
            return IsPressed(_inputSource?.ReadCommandFrame().cruiseControl ?? LwsMomentaryIntent.None);
        }

        public override bool TrailerAttachDetach()
        {
            ResolveSource();
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
