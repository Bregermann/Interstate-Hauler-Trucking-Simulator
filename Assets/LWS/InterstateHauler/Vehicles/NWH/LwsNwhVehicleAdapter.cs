using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsNwhVehicleAdapter : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private TrailerHitchModuleWrapper trailerHitch;

        public VehicleController VehicleController => vehicleController;

        private void Reset()
        {
            vehicleController = GetComponent<VehicleController>();
            trailerHitch = GetComponentInChildren<TrailerHitchModuleWrapper>();
        }

        private void Awake()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }
        }

        public LwsVehicleTelemetry ReadTelemetry()
        {
            if (vehicleController == null)
            {
                return default;
            }

            var powertrain = vehicleController.powertrain;
            var engine = powertrain.engine;
            var transmission = powertrain.transmission;
            var clutch = powertrain.clutch;

            return new LwsVehicleTelemetry
            {
                speedMetersPerSecond = vehicleController.Speed,
                engineRpm = engine.OutputRPM,
                currentGear = transmission.Gear,
                currentGearName = transmission.GearName,
                clutchInput = clutch.clutchInput,
                engineRunning = engine.IsRunning,
                engineStalled = engine.IsStalled,
                trailerAttached = trailerHitch != null && trailerHitch.module != null && trailerHitch.module.attached
            };
        }
    }
}
