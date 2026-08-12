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
        [SerializeField] private LwsVehicleIdentity identity;

        public VehicleController VehicleController => vehicleController;
        public bool IsReady => vehicleController != null;

        private void Reset()
        {
            vehicleController = GetComponent<VehicleController>();
            trailerHitch = GetComponentInChildren<TrailerHitchModuleWrapper>();
            identity = GetComponent<LwsVehicleIdentity>();
        }

        private void Awake()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            if (trailerHitch == null)
            {
                trailerHitch = GetComponentInChildren<TrailerHitchModuleWrapper>();
            }

            if (identity == null)
            {
                identity = GetComponent<LwsVehicleIdentity>();
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
            var input = vehicleController.input;
            var brakes = vehicleController.brakes;
            bool trailerAttached = trailerHitch != null && trailerHitch.module != null && trailerHitch.module.attached;
            string trailerId = ResolveAttachedTrailerId();

            return new LwsVehicleTelemetry
            {
                vehicleId = identity != null ? identity.VehicleId : string.Empty,
                definitionId = identity != null ? identity.DefinitionId : string.Empty,
                speedMetersPerSecond = vehicleController.Speed,
                signedSpeedMetersPerSecond = vehicleController.SpeedSigned,
                engineRpm = engine.OutputRPM,
                currentGear = transmission.Gear,
                currentGearName = transmission.GearName,
                neutral = transmission.Gear == 0,
                reverse = transmission.Gear < 0,
                throttleInput = input.Throttle,
                brakeInput = input.Brakes,
                clutchInput = clutch.clutchInput,
                steeringInput = input.Steering,
                parkingBrakeInput = input.Handbrake,
                serviceBrakeActive = brakes.IsBraking,
                parkingBrakeActive = brakes.handbrakeValue > 0.02f,
                engineRunning = engine.IsRunning,
                engineStalled = engine.IsStalled,
                grounded = vehicleController.IsGrounded(),
                fullyGrounded = vehicleController.IsFullyGrounded(),
                trailerAttached = trailerAttached,
                trailerId = trailerId,
                worldPosition = transform.position,
                worldRotation = transform.rotation
            };
        }

        private string ResolveAttachedTrailerId()
        {
            if (trailerHitch == null || trailerHitch.module == null || trailerHitch.module.attachedTrailerModule == null)
            {
                return string.Empty;
            }

            VehicleController trailerController = trailerHitch.module.attachedTrailerModule.vehicleController;
            if (trailerController == null)
            {
                return string.Empty;
            }

            LwsVehicleIdentity trailerIdentity = trailerController.GetComponent<LwsVehicleIdentity>();
            return trailerIdentity != null ? trailerIdentity.VehicleId : trailerController.name;
        }
    }
}
