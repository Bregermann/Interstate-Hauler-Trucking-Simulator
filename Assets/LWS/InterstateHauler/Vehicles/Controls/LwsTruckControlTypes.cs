using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsIgnitionState
    {
        Off,
        Electrical,
        EngineRunning
    }

    public enum LwsTurnSignalState
    {
        Off,
        Left,
        Right
    }

    public enum LwsWiperState
    {
        Off,
        Intermittent,
        Low,
        High
    }

    public enum LwsTruckGestureType
    {
        None,
        FlipOffDriver
    }

    public enum LwsTruckNativePulse
    {
        LowBeamLights,
        HighBeamLights,
        LeftBlinker,
        RightBlinker,
        HazardLights
    }

    [Serializable]
    public struct LwsTruckControlCapabilities
    {
        public bool supportsIgnition;
        public bool supportsEngineStartStop;
        public bool supportsParkingBrake;
        public bool supportsHeadlights;
        public bool supportsHighBeams;
        public bool supportsTurnSignals;
        public bool supportsHazards;
        public bool supportsWipers;
        public bool supportsHorn;
        public bool supportsSeparateAirHorn;
        public bool supportsEngineBrake;
        public int engineBrakeLevels;
        public bool supportsRetarder;
        public int retarderLevels;
        public bool supportsDifferentialLock;
        public bool supportsCruiseControl;
        public bool supportsIndependentTrailerBrake;
        public bool supportsTrailerAttachDetach;
        public bool supportsCameraCycle;
        public bool supportsLookReset;
        public bool supportsFlipOffDriver;

        public static LwsTruckControlCapabilities NwhSemiDevelopmentDefault()
        {
            return new LwsTruckControlCapabilities
            {
                supportsIgnition = true,
                supportsEngineStartStop = true,
                supportsParkingBrake = true,
                supportsHeadlights = true,
                supportsHighBeams = true,
                supportsTurnSignals = true,
                supportsHazards = true,
                supportsWipers = false,
                supportsHorn = true,
                supportsSeparateAirHorn = false,
                supportsEngineBrake = false,
                engineBrakeLevels = 0,
                supportsRetarder = false,
                retarderLevels = 0,
                supportsDifferentialLock = true,
                supportsCruiseControl = true,
                supportsIndependentTrailerBrake = false,
                supportsTrailerAttachDetach = true,
                supportsCameraCycle = true,
                supportsLookReset = true,
                supportsFlipOffDriver = true
            };
        }

        public bool Validate(out string message)
        {
            if (supportsEngineBrake && engineBrakeLevels <= 0)
            {
                message = "Engine brake support requires at least one level.";
                return false;
            }

            if (supportsRetarder && retarderLevels <= 0)
            {
                message = "Retarder support requires at least one level.";
                return false;
            }

            message = "Truck control capabilities are valid.";
            return true;
        }
    }

    [Serializable]
    public struct LwsTruckControlState
    {
        public string vehicleId;
        public LwsVehicleInputOwner inputOwner;
        public string inputSourceId;
        public LwsIgnitionState ignitionState;
        public bool engineRunning;
        public bool engineStalled;
        public bool parkingBrakeOn;
        public float serviceBrakeInput;
        public bool trailerBrakeHeld;
        public int engineBrakeLevel;
        public int retarderLevel;
        public bool headlightsOn;
        public bool highBeamsOn;
        public LwsTurnSignalState turnSignal;
        public bool hazardsOn;
        public LwsWiperState wiperState;
        public bool hornActive;
        public bool airHornActive;
        public bool differentialLocked;
        public bool cruiseEnabled;
        public float cruiseTargetSpeedMetersPerSecond;
        public float cruiseThrottleOutput;
        public float cruiseBrakeOutput;
        public bool trailerAttached;
        public string trailerId;
        public bool cameraCycleRequested;
        public bool lookResetRequested;
        public LwsTruckGestureType lastGesture;
        public string lastGestureTargetId;
        public float lastGestureTargetDistance;
        public float gestureCooldownRemaining;
        public bool lastGestureHadTarget;
        public LwsTransmissionDisplayState transmission;
    }

    [Serializable]
    public struct LwsTruckControlEvent
    {
        public string vehicleId;
        public string controlName;
        public string value;
        public float timestamp;
    }

    [Serializable]
    public struct LwsDriverGestureEvent
    {
        public LwsTruckGestureType gestureType;
        public string sourceVehicleId;
        public string targetVehicleId;
        public bool hasValidTarget;
        public float targetDistanceMeters;
        public Vector3 sourcePosition;
        public Vector3 targetPosition;
        public Vector3 sourceForward;
        public float timestamp;
    }

    public interface ILwsTruckControlService : ILwsService
    {
        LwsTruckControlController ActiveController { get; }
        LwsTruckControlState ActiveState { get; }
        event Action<LwsTruckControlState> StateChanged;
        event Action<LwsTruckControlEvent> ControlEventPublished;
        event Action<LwsDriverGestureEvent> DriverGesturePublished;
        LwsServiceResult RegisterActiveController(LwsTruckControlController controller);
        LwsServiceResult ClearActiveController(LwsTruckControlController controller);
        void PublishState(LwsTruckControlController controller, LwsTruckControlState state);
        void PublishControlEvent(LwsTruckControlController controller, string controlName, string value);
        void PublishDriverGesture(LwsTruckControlController controller, LwsDriverGestureEvent gestureEvent);
    }
}
