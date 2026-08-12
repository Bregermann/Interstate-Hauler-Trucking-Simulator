using NWH.Common.Cameras;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Effects;
using NWH.VehiclePhysics2.Modules.CruiseControl;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsNwhTruckControlAdapter : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private LwsNwhTrailerCouplingAdapter trailerCoupling;
        [SerializeField] private CameraChanger cameraChanger;
        [SerializeField] private CruiseControlModuleWrapper cruiseControlWrapper;
        [SerializeField] private bool allowDifferentialSlipTorqueOverride = true;
        [SerializeField] private float lockedDifferentialSlipTorque = 50000f;

        private bool _differentialSnapshotCaptured;
        private float[] _originalDifferentialSlipTorques;

        public VehicleController VehicleController => vehicleController;
        public bool HasVehicleController => vehicleController != null;
        public bool HasCruiseControl => ResolveCruiseControl() != null;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureDifferentialSnapshotIfNeeded();
        }

        public void ResolveReferences()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            if (trailerCoupling == null)
            {
                trailerCoupling = GetComponent<LwsNwhTrailerCouplingAdapter>();
            }

            if (cameraChanger == null)
            {
                cameraChanger = GetComponentInChildren<CameraChanger>(true);
            }

            if (cruiseControlWrapper == null)
            {
                cruiseControlWrapper = GetComponent<CruiseControlModuleWrapper>();
            }
        }

        public void SetIgnitionElectrical(bool on)
        {
            EngineComponent engine = ResolveEngine();
            if (engine != null)
            {
                engine.ignition = on;
            }
        }

        public void ToggleIgnition()
        {
            EngineComponent engine = ResolveEngine();
            if (engine == null)
            {
                return;
            }

            if (engine.ignition || engine.IsRunning)
            {
                engine.StopEngine();
            }
            else
            {
                engine.ignition = true;
            }
        }

        public void StartEngine()
        {
            ResolveEngine()?.StartEngine();
        }

        public void StopEngine()
        {
            ResolveEngine()?.StopEngine();
        }

        public void RequestTrailerAttachDetach()
        {
            trailerCoupling?.RequestAttachDetach();
        }

        public void CycleCamera()
        {
            if (cameraChanger == null)
            {
                ResolveReferences();
            }

            cameraChanger?.NextCamera();
        }

        public void ResetLook()
        {
            // The selected NWH camera stack does not expose a stable reset-look API.
        }

        public void SetDifferentialLocked(bool locked)
        {
            if (!allowDifferentialSlipTorqueOverride || vehicleController == null)
            {
                return;
            }

            CaptureDifferentialSnapshotIfNeeded();
            if (vehicleController.powertrain.differentials == null)
            {
                return;
            }

            for (int i = 0; i < vehicleController.powertrain.differentials.Count; i++)
            {
                DifferentialComponent differential = vehicleController.powertrain.differentials[i];
                if (differential == null)
                {
                    continue;
                }

                float original = _originalDifferentialSlipTorques != null && i < _originalDifferentialSlipTorques.Length
                    ? _originalDifferentialSlipTorques[i]
                    : differential.slipTorque;
                differential.slipTorque = locked ? Mathf.Max(original, lockedDifferentialSlipTorque) : original;
            }
        }

        public void SetCruise(bool enabled, float targetSpeedMetersPerSecond)
        {
            CruiseControlModule module = ResolveCruiseControl();
            if (module == null)
            {
                return;
            }

            module.cruiseControlActive = enabled && targetSpeedMetersPerSecond > 0.1f;
            module.targetSpeed = module.cruiseControlActive ? targetSpeedMetersPerSecond : 0f;
        }

        public void AdjustCruiseTarget(float deltaMetersPerSecond)
        {
            CruiseControlModule module = ResolveCruiseControl();
            if (module == null)
            {
                return;
            }

            module.targetSpeed = Mathf.Max(0f, module.targetSpeed + deltaMetersPerSecond);
        }

        public void CancelCruise()
        {
            CruiseControlModule module = ResolveCruiseControl();
            if (module == null)
            {
                return;
            }

            module.cruiseControlActive = false;
            module.targetSpeed = 0f;
        }

        public LwsIgnitionState ReadIgnitionState()
        {
            EngineComponent engine = ResolveEngine();
            if (engine == null)
            {
                return LwsIgnitionState.Off;
            }

            if (engine.IsRunning)
            {
                return LwsIgnitionState.EngineRunning;
            }

            return engine.ignition ? LwsIgnitionState.Electrical : LwsIgnitionState.Off;
        }

        public bool ReadEngineRunning()
        {
            return ResolveEngine()?.IsRunning ?? false;
        }

        public bool ReadEngineStalled()
        {
            return ResolveEngine()?.IsStalled ?? false;
        }

        public bool ReadHeadlightsOn()
        {
            VehicleLight light = vehicleController != null &&
                                 vehicleController.effectsManager != null &&
                                 vehicleController.effectsManager.lightsManager != null
                ? vehicleController.effectsManager.lightsManager.lowBeamLights
                : null;
            return light != null && light.On;
        }

        public bool ReadHighBeamsOn()
        {
            VehicleLight light = vehicleController != null &&
                                 vehicleController.effectsManager != null &&
                                 vehicleController.effectsManager.lightsManager != null
                ? vehicleController.effectsManager.lightsManager.highBeamLights
                : null;
            return light != null && light.On;
        }

        public bool ReadCruiseEnabled()
        {
            return ResolveCruiseControl()?.cruiseControlActive ?? false;
        }

        public float ReadCruiseTargetSpeed()
        {
            return ResolveCruiseControl()?.targetSpeed ?? 0f;
        }

        private EngineComponent ResolveEngine()
        {
            ResolveReferences();
            return vehicleController != null ? vehicleController.powertrain.engine : null;
        }

        private CruiseControlModule ResolveCruiseControl()
        {
            ResolveReferences();
            return cruiseControlWrapper != null ? cruiseControlWrapper.module : null;
        }

        private void CaptureDifferentialSnapshotIfNeeded()
        {
            if (_differentialSnapshotCaptured || vehicleController == null || vehicleController.powertrain.differentials == null)
            {
                return;
            }

            _originalDifferentialSlipTorques = new float[vehicleController.powertrain.differentials.Count];
            for (int i = 0; i < _originalDifferentialSlipTorques.Length; i++)
            {
                DifferentialComponent differential = vehicleController.powertrain.differentials[i];
                _originalDifferentialSlipTorques[i] = differential != null ? differential.slipTorque : 0f;
            }

            _differentialSnapshotCaptured = true;
        }
    }
}
