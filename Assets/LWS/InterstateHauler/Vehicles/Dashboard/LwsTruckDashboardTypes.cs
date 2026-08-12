using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsDashboardSpeedUnit
    {
        MilesPerHour,
        KilometersPerHour
    }

    public enum LwsDashboardIndicatorId
    {
        Ignition,
        EngineRunning,
        EngineStalled,
        ParkingBrake,
        Headlights,
        HighBeams,
        LeftSignal,
        RightSignal,
        Hazards,
        EngineBrake,
        Retarder,
        DifferentialLock,
        Cruise,
        TrailerAttached,
        CheckEngine
    }

    public enum LwsDashboardBindingSupport
    {
        Missing,
        AvailableArtNoLogic,
        Partial,
        WorkingNative,
        LwsRuntimeBinding
    }

    [Serializable]
    public struct LwsDashboardGaugeMapping
    {
        public float minimumValue;
        public float maximumValue;
        public float startAngle;
        public float endAngle;
        [Range(0f, 1f)] public float smoothing;

        public float Normalize(float value)
        {
            if (maximumValue <= minimumValue)
            {
                return 0f;
            }

            return Mathf.InverseLerp(minimumValue, maximumValue, value);
        }

        public float MapToAngle(float value)
        {
            return Mathf.Lerp(startAngle, endAngle, Normalize(value));
        }

        public bool Validate(out string message)
        {
            if (maximumValue <= minimumValue)
            {
                message = "Gauge maximum value must be greater than minimum value.";
                return false;
            }

            if (smoothing < 0f || smoothing > 1f)
            {
                message = "Gauge smoothing must be in the 0..1 range.";
                return false;
            }

            message = "Gauge mapping is valid.";
            return true;
        }
    }

    [Serializable]
    public struct LwsTruckDashboardCapabilities
    {
        public bool analogSpeedometer;
        public bool analogTachometer;
        public bool digitalGearDisplay;
        public bool parkingBrakeLamp;
        public bool highBeamLamp;
        public bool turnSignalLamps;
        public bool engineBrakeDisplay;
        public bool retarderDisplay;
        public bool differentialLockLamp;
        public bool cruiseLamp;
        public bool trailerStateIndicator;
        public bool steeringWheelAnimation;
        public bool leftMirror;
        public bool rightMirror;
        public bool dashboardAccessoryAnchors;
        public bool hangingAccessoryAnchors;
        public bool passengerSeatAnchor;
        public bool sleeperAnchor;
        public bool mementoAnchors;

        public static LwsTruckDashboardCapabilities NwhSemiDevelopmentDefault()
        {
            return new LwsTruckDashboardCapabilities
            {
                analogSpeedometer = true,
                analogTachometer = true,
                digitalGearDisplay = true,
                parkingBrakeLamp = true,
                highBeamLamp = true,
                turnSignalLamps = true,
                engineBrakeDisplay = false,
                retarderDisplay = false,
                differentialLockLamp = true,
                cruiseLamp = true,
                trailerStateIndicator = true,
                steeringWheelAnimation = true,
                leftMirror = true,
                rightMirror = true,
                dashboardAccessoryAnchors = true,
                hangingAccessoryAnchors = true,
                passengerSeatAnchor = true,
                sleeperAnchor = true,
                mementoAnchors = true
            };
        }
    }

    [Serializable]
    public struct LwsTruckDashboardSnapshot
    {
        public string vehicleId;
        public float rawSpeedMetersPerSecond;
        public float displaySpeed;
        public LwsDashboardSpeedUnit speedUnit;
        public float engineRpm;
        public LwsIgnitionState ignitionState;
        public bool engineRunning;
        public bool engineStalled;
        public bool parkingBrakeOn;
        public float serviceBrakeInput;
        public bool trailerBrakeHeld;
        public bool headlightsOn;
        public bool highBeamsOn;
        public LwsTurnSignalState turnSignal;
        public bool hazardsOn;
        public LwsWiperState wiperState;
        public int engineBrakeLevel;
        public int retarderLevel;
        public bool differentialLocked;
        public bool cruiseEnabled;
        public float cruiseTargetSpeedMetersPerSecond;
        public bool trailerAttached;
        public string trailerId;
        public string transmissionLabel;
        public LwsTruckRange requestedRange;
        public LwsTruckRange engagedRange;
        public LwsTruckSplitter requestedSplitter;
        public LwsTruckSplitter engagedSplitter;
        public LwsTransmissionShiftState shiftState;
        public LwsShiftRejectionReason rejectionReason;
        public LwsTransmissionAbuseSeverity abuseSeverity;
        public bool requiresShifterSynchronization;
        public float steeringInput;
        public float throttleInput;
        public float brakeInput;
        public float clutchInput;

        public bool ShowLeftSignal(float blinkTime)
        {
            return Blink(blinkTime) && (hazardsOn || turnSignal == LwsTurnSignalState.Left);
        }

        public bool ShowRightSignal(float blinkTime)
        {
            return Blink(blinkTime) && (hazardsOn || turnSignal == LwsTurnSignalState.Right);
        }

        public bool ShowCheckEngine()
        {
            return engineStalled ||
                   abuseSeverity == LwsTransmissionAbuseSeverity.Severe ||
                   abuseSeverity == LwsTransmissionAbuseSeverity.CatastrophicRisk;
        }

        private static bool Blink(float blinkTime)
        {
            return Mathf.Repeat(Mathf.Max(0f, blinkTime), 1f) < 0.5f;
        }
    }

    public interface ILwsTruckDashboardService : ILwsService
    {
        LwsTruckDashboardController ActiveController { get; }
        LwsTruckDashboardSnapshot ActiveSnapshot { get; }
        event Action<LwsTruckDashboardSnapshot> SnapshotChanged;
        LwsServiceResult RegisterActiveController(LwsTruckDashboardController controller);
        LwsServiceResult ClearActiveController(LwsTruckDashboardController controller);
        void PublishSnapshot(LwsTruckDashboardController controller, LwsTruckDashboardSnapshot snapshot);
    }

    public sealed class LwsTruckDashboardService : ILwsTruckDashboardService
    {
        public string ServiceId => "lws.truck.dashboard";
        public LwsTruckDashboardController ActiveController { get; private set; }
        public LwsTruckDashboardSnapshot ActiveSnapshot { get; private set; }
        public event Action<LwsTruckDashboardSnapshot> SnapshotChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            ActiveController = null;
            ActiveSnapshot = default;
            return LwsServiceResult.Success("LWS truck dashboard service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            ActiveController = null;
            ActiveSnapshot = default;
            return LwsServiceResult.Success("LWS truck dashboard service shut down.");
        }

        public LwsServiceResult RegisterActiveController(LwsTruckDashboardController controller)
        {
            if (controller == null)
            {
                return LwsServiceResult.Failure("Cannot register a null truck dashboard controller.");
            }

            if (ActiveController != null && ActiveController != controller)
            {
                return LwsServiceResult.Failure($"Active truck dashboard controller already registered: {ActiveController.name}");
            }

            ActiveController = controller;
            return LwsServiceResult.Success($"Registered active truck dashboard controller: {controller.name}");
        }

        public LwsServiceResult ClearActiveController(LwsTruckDashboardController controller)
        {
            if (ActiveController == controller)
            {
                ActiveController = null;
                ActiveSnapshot = default;
            }

            return LwsServiceResult.Success("Truck dashboard controller cleared.");
        }

        public void PublishSnapshot(LwsTruckDashboardController controller, LwsTruckDashboardSnapshot snapshot)
        {
            if (ActiveController != controller)
            {
                return;
            }

            ActiveSnapshot = snapshot;
            SnapshotChanged?.Invoke(snapshot);
        }
    }
}
