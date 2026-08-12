using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct LwsNwhTransmissionRuntimeState
    {
        public bool available;
        public int nwhGear;
        public string nwhGearName;
        public float currentTotalGearRatio;
        public float engineRpm;
        public float revLimiterRpm;
        public float idleRpm;
        public float signedSpeedMetersPerSecond;
        public float clutchInput;
        public bool engineRunning;
        public bool engineStalled;
        public bool isShifting;
    }

    [DisallowMultipleComponent]
    public sealed class LwsNwh18SpeedTransmissionAdapter : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private bool configureRuntimeGearList = true;
        [SerializeField] private bool forceManualNwhTransmission = true;
        [SerializeField] private bool bypassNwhStockClutchShiftGate = true;

        private Lws18SpeedTransmissionDefinition _configuredDefinition;
        private bool _configured;

        public VehicleController VehicleController => vehicleController;
        public bool Configured => _configured;

        private void Reset()
        {
            vehicleController = GetComponent<VehicleController>();
        }

        private void Awake()
        {
            ResolveVehicleController();
        }

        public bool ConfigureForDefinition(Lws18SpeedTransmissionDefinition definition, out string message)
        {
            ResolveVehicleController();
            if (vehicleController == null)
            {
                _configured = false;
                message = "NWH VehicleController is missing.";
                return false;
            }

            if (definition == null)
            {
                _configured = false;
                message = "18-speed transmission definition is missing.";
                return false;
            }

            TransmissionComponent transmission = vehicleController.powertrain.transmission;
            if (transmission == null)
            {
                _configured = false;
                message = "NWH TransmissionComponent is missing.";
                return false;
            }

            if (!definition.ValidateDefinition(out message))
            {
                _configured = false;
                return false;
            }

            if (configureRuntimeGearList && definition.ConfigureNwhRuntimeGears)
            {
                if (transmission.gears == null)
                {
                    transmission.gears = new List<float>();
                }

                transmission.gears.Clear();
                transmission.gears.AddRange(definition.ReverseRatios);
                transmission.gears.Add(0f);
                transmission.gears.AddRange(definition.ForwardRatios);
                transmission.reverseGearCount = definition.ReverseRatios.Count;
                transmission.forwardGearCount = definition.ForwardRatios.Count;
                transmission.finalGearRatio = definition.NwhFinalDriveRatio;
            }

            if (forceManualNwhTransmission)
            {
                transmission.transmissionType = TransmissionComponent.TransmissionShiftType.Manual;
                transmission.holdToKeepInGear = false;
            }

            if (bypassNwhStockClutchShiftGate)
            {
                // LWS owns clutch and float-shift validation for Truck18Speed.
                transmission.clutchInputShiftThreshold = 2f;
            }

            _configuredDefinition = definition;
            _configured = true;
            message = $"Configured NWH transmission for {definition.StableId}: {transmission.forwardGearCount} forward / {transmission.reverseGearCount} reverse.";
            return true;
        }

        public LwsNwhTransmissionRuntimeState ReadRuntimeState()
        {
            ResolveVehicleController();
            if (vehicleController == null)
            {
                return default;
            }

            TransmissionComponent transmission = vehicleController.powertrain.transmission;
            EngineComponent engine = vehicleController.powertrain.engine;
            ClutchComponent clutch = vehicleController.powertrain.clutch;

            return new LwsNwhTransmissionRuntimeState
            {
                available = transmission != null && engine != null,
                nwhGear = transmission != null ? transmission.Gear : 0,
                nwhGearName = transmission != null ? transmission.GearName : string.Empty,
                currentTotalGearRatio = transmission != null ? transmission.currentGearRatio : 0f,
                engineRpm = engine != null ? engine.OutputRPM : 0f,
                revLimiterRpm = engine != null ? engine.revLimiterRPM : 0f,
                idleRpm = engine != null ? engine.idleRPM : 0f,
                signedSpeedMetersPerSecond = vehicleController.SpeedSigned,
                clutchInput = clutch != null ? clutch.clutchInput : vehicleController.input.Clutch,
                engineRunning = engine != null && engine.IsRunning,
                engineStalled = engine != null && engine.IsStalled,
                isShifting = transmission != null && transmission.isShifting
            };
        }

        public float GetConfiguredTotalRatio(int nwhGearIndex)
        {
            ResolveVehicleController();
            TransmissionComponent transmission = vehicleController != null ? vehicleController.powertrain.transmission : null;
            if (transmission == null)
            {
                return 0f;
            }

            try
            {
                return transmission.GetGearRatio(nwhGearIndex);
            }
            catch
            {
                if (_configuredDefinition != null &&
                    _configuredDefinition.TryGetMappingForNwhGear(nwhGearIndex, out Lws18SpeedRatioMapping mapping))
                {
                    return mapping.gearRatio * _configuredDefinition.NwhFinalDriveRatio;
                }

                return 0f;
            }
        }

        public bool TryShiftInto(int nwhGearIndex, bool instant, out string message)
        {
            ResolveVehicleController();
            TransmissionComponent transmission = vehicleController != null ? vehicleController.powertrain.transmission : null;
            if (transmission == null)
            {
                message = "NWH TransmissionComponent is missing.";
                return false;
            }

            if (nwhGearIndex == transmission.Gear)
            {
                message = $"NWH transmission already in gear {nwhGearIndex}.";
                return true;
            }

            transmission.ShiftInto(nwhGearIndex, instant);
            message = $"Requested NWH ShiftInto({nwhGearIndex}, instant: {instant}).";
            return true;
        }

        private void ResolveVehicleController()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }
        }
    }
}
