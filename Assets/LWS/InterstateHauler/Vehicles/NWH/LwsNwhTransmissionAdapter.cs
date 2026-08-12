using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsNwhTransmissionAdapter : MonoBehaviour, ILwsTruckTransmission
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private LwsTransmissionMode lwsMode = LwsTransmissionMode.SimpleHPattern;

        private LwsTransmissionState _lastState;

        public LwsTransmissionState CurrentState => CaptureState();

        private void Reset()
        {
            vehicleController = GetComponent<VehicleController>();
        }

        private void Awake()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }
        }

        public void ApplyGearIntent(LwsTruckGearIntent gearIntent)
        {
            _lastState.physicalGate = gearIntent.physicalGate;
            _lastState.requestedRange = gearIntent.range;
            _lastState.engagedRange = gearIntent.range;
            _lastState.requestedSplitter = gearIntent.splitter;
            _lastState.engagedSplitter = gearIntent.splitter;

            if (vehicleController == null)
            {
                return;
            }

            TransmissionComponent transmission = vehicleController.powertrain.transmission;

            if (gearIntent.neutralRequested)
            {
                transmission.ShiftInto(0);
                return;
            }

            if (gearIntent.reverseRequested)
            {
                transmission.ShiftInto(-1);
                return;
            }

            if (gearIntent.requestedLogicalGear >= 1 && gearIntent.requestedLogicalGear <= 8)
            {
                transmission.ShiftInto(gearIntent.requestedLogicalGear);
            }
        }

        public LwsTransmissionState CaptureState()
        {
            if (vehicleController == null)
            {
                return _lastState;
            }

            TransmissionComponent transmission = vehicleController.powertrain.transmission;
            ClutchComponent clutch = vehicleController.powertrain.clutch;

            _lastState.mode = lwsMode;
            _lastState.nwhGear = transmission.Gear;
            _lastState.logicalGear = transmission.Gear == 0
                ? Lws18SpeedGearId.Neutral
                : transmission.Gear < 0 ? Lws18SpeedGearId.Reverse1 : _lastState.logicalGear;
            _lastState.displayLabel = transmission.GearName;
            _lastState.clutchInput = clutch.clutchInput;
            _lastState.neutral = transmission.Gear == 0;
            _lastState.reverse = transmission.Gear < 0;
            _lastState.engineStalled = vehicleController.powertrain.engine.IsStalled;
            return _lastState;
        }

        public void RestoreState(LwsTransmissionState state)
        {
            _lastState = state;
            if (vehicleController == null)
            {
                return;
            }

            vehicleController.powertrain.transmission.ShiftInto(state.nwhGear, true);
        }
    }
}
