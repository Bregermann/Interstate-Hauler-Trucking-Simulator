using System;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct LwsVehicleTelemetry
    {
        public float speedMetersPerSecond;
        public float engineRpm;
        public int currentGear;
        public string currentGearName;
        public float clutchInput;
        public bool engineRunning;
        public bool engineStalled;
        public bool trailerAttached;
    }

    public interface ILwsVehicleRuntimeService : ILwsService
    {
        LwsVehicleTelemetry LastTelemetry { get; }
        void PublishTelemetry(LwsVehicleTelemetry telemetry);
    }

    public sealed class LwsVehicleRuntimeService : ILwsVehicleRuntimeService
    {
        public string ServiceId => "lws.vehicle.runtime";
        public LwsVehicleTelemetry LastTelemetry { get; private set; }

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            LastTelemetry = default;
            return LwsServiceResult.Success("LWS vehicle runtime service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            LastTelemetry = default;
            return LwsServiceResult.Success("LWS vehicle runtime service shut down.");
        }

        public void PublishTelemetry(LwsVehicleTelemetry telemetry)
        {
            LastTelemetry = telemetry;
        }
    }
}
