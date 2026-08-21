using System;

namespace LWS.InterstateHauler
{
    public enum LwsTransmissionMode
    {
        Automatic,
        Sequential,
        SimpleHPattern,
        Truck18Speed
    }

    [Serializable]
    public struct LwsTransmissionState
    {
        public LwsTransmissionMode mode;
        public Lws18SpeedGearId logicalGear;
        public int logicalRatioIndex;
        public string displayLabel;
        public LwsTruckShifterGate physicalGate;
        public LwsTruckRange requestedRange;
        public LwsTruckRange engagedRange;
        public LwsTruckSplitter requestedSplitter;
        public LwsTruckSplitter engagedSplitter;
        public LwsAutomaticTransmissionSelector automaticSelector;
        public float clutchInput;
        public bool neutral;
        public bool reverse;
        public bool engineStalled;
        public int nwhGear;
        public float gearRatio;
        public float predictedRpm;
        public float rpmError;
        public LwsTransmissionShiftState shiftState;
        public LwsShiftRejectionReason lastRejectionReason;
        public LwsTransmissionAbuseSeverity lastAbuseSeverity;
        public bool requiresShifterSynchronization;
    }

    public interface ILwsTruckTransmission
    {
        LwsTransmissionState CurrentState { get; }
        void ApplyGearIntent(LwsTruckGearIntent gearIntent);
        LwsTransmissionState CaptureState();
        void RestoreState(LwsTransmissionState state);
    }
}
