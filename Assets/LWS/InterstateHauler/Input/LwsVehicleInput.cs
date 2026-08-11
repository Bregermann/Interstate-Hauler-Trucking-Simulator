using System;

namespace LWS.InterstateHauler
{
    public enum LwsMomentaryIntent
    {
        None,
        Pressed,
        Released,
        Held
    }

    public enum LwsTruckShifterGate
    {
        None,
        Reverse,
        Neutral,
        Gate1,
        Gate2,
        Gate3,
        Gate4,
        Gate5,
        Gate6,
        Gate7,
        Gate8
    }

    public enum LwsTruckRange
    {
        Low,
        High
    }

    public enum LwsTruckSplitter
    {
        Low,
        High
    }

    [Serializable]
    public struct LwsVehicleContinuousInput
    {
        public float steering;
        public float throttle;
        public float brake;
        public float clutch;
        public float parkingBrake;
    }

    [Serializable]
    public struct LwsTruckGearIntent
    {
        public LwsTruckShifterGate physicalGate;
        public LwsTruckRange range;
        public LwsTruckSplitter splitter;
        public int requestedLogicalGear;
        public bool neutralRequested;
        public bool reverseRequested;
    }

    [Serializable]
    public struct LwsVehicleCommandFrame
    {
        public LwsMomentaryIntent ignition;
        public LwsMomentaryIntent horn;
        public LwsMomentaryIntent lowBeamLights;
        public LwsMomentaryIntent highBeamLights;
        public LwsMomentaryIntent hazardLights;
        public LwsMomentaryIntent leftIndicator;
        public LwsMomentaryIntent rightIndicator;
        public LwsMomentaryIntent wipers;
        public LwsMomentaryIntent cruiseControl;
        public LwsMomentaryIntent engineBrake;
        public LwsMomentaryIntent retarder;
        public LwsMomentaryIntent differentialLock;
        public LwsMomentaryIntent trailerAttachDetach;
    }

    public interface ILwsVehicleInputSource
    {
        string SourceId { get; }
        LwsVehicleContinuousInput ReadContinuousInput();
        LwsVehicleCommandFrame ReadCommandFrame();
        LwsTruckGearIntent ReadGearIntent();
    }

    public interface ILwsVehicleInputService : ILwsService, ILwsVehicleInputSource
    {
        void SetInputSource(ILwsVehicleInputSource source);
    }

    public sealed class LwsVehicleInputService : ILwsVehicleInputService
    {
        private ILwsVehicleInputSource _source;

        public string ServiceId => "lws.input.vehicle";
        public string SourceId => _source?.SourceId ?? "lws.input.none";

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            return LwsServiceResult.Success("LWS vehicle input service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _source = null;
            return LwsServiceResult.Success("LWS vehicle input service shut down.");
        }

        public void SetInputSource(ILwsVehicleInputSource source)
        {
            _source = source;
        }

        public LwsVehicleContinuousInput ReadContinuousInput()
        {
            return _source?.ReadContinuousInput() ?? default;
        }

        public LwsVehicleCommandFrame ReadCommandFrame()
        {
            return _source?.ReadCommandFrame() ?? default;
        }

        public LwsTruckGearIntent ReadGearIntent()
        {
            return _source?.ReadGearIntent() ?? default;
        }
    }
}
