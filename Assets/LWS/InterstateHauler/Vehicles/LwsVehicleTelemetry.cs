using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct LwsVehicleTelemetry
    {
        public string vehicleId;
        public string definitionId;
        public float speedMetersPerSecond;
        public float signedSpeedMetersPerSecond;
        public float engineRpm;
        public int currentGear;
        public string currentGearName;
        public bool neutral;
        public bool reverse;
        public float throttleInput;
        public float brakeInput;
        public float clutchInput;
        public float steeringInput;
        public float parkingBrakeInput;
        public bool serviceBrakeActive;
        public bool parkingBrakeActive;
        public bool engineRunning;
        public bool engineStalled;
        public bool grounded;
        public bool fullyGrounded;
        public bool trailerAttached;
        public string trailerId;
        public Vector3 worldPosition;
        public Quaternion worldRotation;
    }

    [Serializable]
    public struct LwsSerializablePose
    {
        public Vector3 position;
        public Quaternion rotation;

        public static LwsSerializablePose FromTransform(Transform transform)
        {
            if (transform == null)
            {
                return default;
            }

            return new LwsSerializablePose
            {
                position = transform.position,
                rotation = transform.rotation
            };
        }
    }

    [Serializable]
    public struct LwsTrailerAttachmentState
    {
        public bool attached;
        public string towingVehicleId;
        public string trailerId;
        public LwsSerializablePose trailerPose;
    }

    [Serializable]
    public struct LwsPlayerTruckState
    {
        public string vehicleId;
        public string truckDefinitionId;
        public LwsSerializablePose pose;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public bool engineRunning;
        public int nwhGear;
        public string nwhGearName;
        public LwsTransmissionState transmissionState;
        public LwsTrailerAttachmentState trailerAttachment;
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
