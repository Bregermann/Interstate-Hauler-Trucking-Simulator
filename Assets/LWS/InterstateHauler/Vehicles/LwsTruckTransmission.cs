using System;

namespace LWS.InterstateHauler
{
    public enum LwsTransmissionMode
    {
        Automatic,
        Sequential,
        HPattern,
        RangeSplitter
    }

    [Serializable]
    public struct LwsTransmissionState
    {
        public LwsTransmissionMode mode;
        public int logicalGear;
        public LwsTruckShifterGate physicalGate;
        public LwsTruckRange range;
        public LwsTruckSplitter splitter;
        public float clutchInput;
        public bool neutral;
        public bool reverse;
        public bool engineStalled;
    }

    public interface ILwsTruckTransmission
    {
        LwsTransmissionState CurrentState { get; }
        void ApplyGearIntent(LwsTruckGearIntent gearIntent);
        LwsTransmissionState CaptureState();
        void RestoreState(LwsTransmissionState state);
    }
}
