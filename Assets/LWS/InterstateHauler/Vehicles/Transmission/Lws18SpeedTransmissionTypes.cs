using System;

namespace LWS.InterstateHauler
{
    public enum Lws18SpeedGearId
    {
        Neutral,
        Reverse1,
        LowLow,
        LowHigh,
        Gear1Low,
        Gear1High,
        Gear2Low,
        Gear2High,
        Gear3Low,
        Gear3High,
        Gear4Low,
        Gear4High,
        Gear5Low,
        Gear5High,
        Gear6Low,
        Gear6High,
        Gear7Low,
        Gear7High,
        Gear8Low,
        Gear8High
    }

    public enum LwsTransmissionShiftState
    {
        Idle,
        Preselected,
        WaitingForNeutral,
        WaitingForClutch,
        WaitingForSynchronization,
        Engaging,
        Engaged,
        Rejected,
        Grinding,
        ShifterMismatch
    }

    public enum LwsShiftRejectionReason
    {
        None,
        ClutchNotDepressed,
        RpmMismatch,
        InvalidRangeGateCombination,
        InvalidReverseRequest,
        TransmissionModeConflict,
        GearUnavailable,
        VehicleSpeedMismatch,
        PredictedOverspeed,
        DuplicateRequestSuppressed,
        InputUnavailable,
        ShifterPositionMismatch
    }

    public enum LwsAutomaticTransmissionSelector
    {
        Drive,
        Neutral,
        Reverse
    }

    public enum LwsManualShiftAssistMode
    {
        AssistedManual,
        HardcoreManual
    }

    public enum LwsTransmissionAbuseSeverity
    {
        None,
        Minor,
        Moderate,
        Severe,
        CatastrophicRisk
    }

    public enum LwsTransmissionAbuseCause
    {
        None,
        ExcessiveRpmMismatch,
        ClutchlessPoorSynchronization,
        HighSpeedLowGearSelection,
        ReverseWhileMovingForward,
        ExtremeDownshift,
        GearUnavailable
    }

    [Serializable]
    public struct Lws18SpeedResolvedGear
    {
        public bool valid;
        public Lws18SpeedGearId gearId;
        public LwsTruckShifterGate physicalGate;
        public LwsTruckRange range;
        public LwsTruckSplitter splitter;
        public int logicalRatioIndex;
        public int nwhGearIndex;
        public float gearRatio;
        public string displayLabel;
        public bool neutral;
        public bool reverse;
        public LwsShiftRejectionReason invalidReason;

        public static Lws18SpeedResolvedGear Neutral()
        {
            return new Lws18SpeedResolvedGear
            {
                valid = true,
                gearId = Lws18SpeedGearId.Neutral,
                logicalRatioIndex = 0,
                nwhGearIndex = 0,
                displayLabel = "N",
                neutral = true
            };
        }

        public static Lws18SpeedResolvedGear Invalid(
            LwsTruckShifterGate gate,
            LwsTruckRange range,
            LwsTruckSplitter splitter,
            LwsShiftRejectionReason reason)
        {
            return new Lws18SpeedResolvedGear
            {
                valid = false,
                physicalGate = gate,
                range = range,
                splitter = splitter,
                displayLabel = "INVALID",
                invalidReason = reason
            };
        }
    }

    [Serializable]
    public struct LwsTransmissionAbuseEvent
    {
        public LwsTransmissionAbuseSeverity severity;
        public LwsTransmissionAbuseCause cause;
        public Lws18SpeedGearId attemptedGear;
        public int attemptedNwhGear;
        public float currentRpm;
        public float predictedRpm;
        public float rpmError;
        public float speedMetersPerSecond;
        public string message;

        public bool HasAbuse => severity != LwsTransmissionAbuseSeverity.None;
    }

    [Serializable]
    public struct LwsTransmissionDisplayState
    {
        public LwsTransmissionMode mode;
        public LwsTruckShifterGate physicalGate;
        public LwsTruckRange requestedRange;
        public LwsTruckRange engagedRange;
        public LwsTruckSplitter requestedSplitter;
        public LwsTruckSplitter engagedSplitter;
        public Lws18SpeedGearId logicalGear;
        public string displayLabel;
        public int logicalRatioIndex;
        public int nwhGear;
        public float gearRatio;
        public float clutch;
        public float engineRpm;
        public float signedSpeedMetersPerSecond;
        public float predictedTargetRpm;
        public float rpmError;
        public LwsAutomaticTransmissionSelector automaticSelector;
        public int automaticTargetNwhGear;
        public string automaticTargetLabel;
        public LwsTransmissionShiftState shiftState;
        public LwsShiftRejectionReason lastRejectionReason;
        public LwsTransmissionAbuseSeverity lastAbuseSeverity;
        public bool requiresShifterSynchronization;
    }

    [Serializable]
    public sealed class Lws18SpeedTransmissionSavePayload
    {
        public int schemaVersion = 1;
        public LwsTransmissionMode mode;
        public Lws18SpeedGearId logicalGear;
        public int logicalRatioIndex;
        public int nwhGear;
        public LwsTruckShifterGate physicalGate;
        public LwsTruckRange requestedRange;
        public LwsTruckRange engagedRange;
        public LwsTruckSplitter requestedSplitter;
        public LwsTruckSplitter engagedSplitter;
        public LwsAutomaticTransmissionSelector automaticSelector;
        public LwsTransmissionShiftState shiftState;
        public bool requiresShifterSynchronization;
    }
}
