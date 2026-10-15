using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct LwsDeliveryCompletionSnapshot
    {
        public bool completedSuccessfully;
        public string activeJobId;
        public string destinationId;
        public string assignedTrailerId;
        public long completedUtcTicks;
        public float containmentRatio;
        public float headingErrorDegrees;
        public float lateralOffsetMeters;
        public float longitudinalOffsetMeters;
        public float validHoldSeconds;
        public float deliveryContextDurationSeconds;
        public string validationMode;

        public bool IsValid => completedSuccessfully &&
                               !string.IsNullOrWhiteSpace(activeJobId) &&
                               !string.IsNullOrWhiteSpace(destinationId) &&
                               !string.IsNullOrWhiteSpace(assignedTrailerId) &&
                               completedUtcTicks > 0;

        public static LwsDeliveryCompletionSnapshot None => default;
    }

    [Serializable]
    public struct LwsDeliveryParkingEvaluation
    {
        public int samplePointCount;
        public float containmentRatio;
        public bool trailerInsideZone;
        public float headingErrorDegrees;
        public bool headingValid;
        public float trailerLinearSpeedMetersPerSecond;
        public float trailerAngularSpeedDegreesPerSecond;
        public bool stopped;
        public float lateralOffsetMeters;
        public float longitudinalOffsetMeters;
    }

    [Serializable]
    public struct LwsDeliverySnapshot
    {
        public bool hasActiveJob;
        public string activeJobId;
        public LwsJobStatus jobStatus;
        public string destinationId;
        public string currentDestinationId;
        public string currentDestinationDisplayName;
        public bool deliveryAreaResolved;
        public bool deliveryAreaActive;
        public bool playerInsideDeliveryArea;
        public string parkingTargetName;
        public bool parkingTargetResolved;
        public string assignedTrailerId;
        public string attachedTrailerId;
        public bool correctDestination;
        public bool correctTrailer;
        public bool physicallyCoupled;
        public bool trailerResolved;
        public bool trailerInsideZone;
        public float containmentRatio;
        public float headingErrorDegrees;
        public bool headingValid;
        public float trailerLinearSpeedMetersPerSecond;
        public float trailerAngularSpeedDegreesPerSecond;
        public bool stopped;
        public float lateralOffsetMeters;
        public float longitudinalOffsetMeters;
        public float validHoldSeconds;
        public float requiredHoldSeconds;
        public bool deliveryReady;
        public string lastMessage;
    }

    public interface ILwsDeliveryService : ILwsService
    {
        IReadOnlyList<LwsDeliveryArea> RegisteredDeliveryAreas { get; }
        LwsDeliveryArea ActiveDeliveryArea { get; }
        LwsDeliveryParkingZone ActiveParkingZone { get; }
        LwsDeliverySnapshot CurrentSnapshot { get; }
        string LastMessage { get; }
        event Action<LwsDeliverySnapshot> DeliveryStateChanged;
        LwsServiceResult RegisterDeliveryArea(LwsDeliveryArea area);
        LwsServiceResult UnregisterDeliveryArea(LwsDeliveryArea area);
        bool TryResolveDeliveryArea(string destinationId, out LwsDeliveryArea area);
        LwsServiceResult TryEnterDeliveryContext(LwsDeliveryArea area, LwsPlayerTruck playerTruck, string reason);
        void NotifyPlayerExitedDeliveryArea(LwsDeliveryArea area, LwsPlayerTruck playerTruck);
        LwsServiceResult EvaluateParking(LwsDeliveryParkingZone parkingZone, float deltaTime);
        LwsServiceResult FocusDeliveryTarget(string reason);
        LwsServiceResult ReevaluateDelivery(string reason);
        LwsServiceResult ForceEnterDeliveryContextForDevelopment(string reason);
        void ResetValidationHold(string reason);
    }

    public static class LwsDeliveryValidation
    {
        public static bool DestinationMatches(string activeJobDestinationId, string deliveryAreaDestinationId)
        {
            return !string.IsNullOrWhiteSpace(activeJobDestinationId) &&
                   string.Equals(Normalize(activeJobDestinationId), Normalize(deliveryAreaDestinationId), StringComparison.Ordinal);
        }

        public static bool AssignedTrailerMatches(string assignedTrailerId, string physicalTrailerId)
        {
            return !string.IsNullOrWhiteSpace(assignedTrailerId) &&
                   string.Equals(Normalize(assignedTrailerId), Normalize(physicalTrailerId), StringComparison.Ordinal);
        }

        public static float CalculateHorizontalHeadingError(Vector3 trailerForward, Vector3 desiredForward, bool allowOppositeHeading)
        {
            trailerForward.y = 0f;
            desiredForward.y = 0f;
            if (trailerForward.sqrMagnitude <= 0.0001f || desiredForward.sqrMagnitude <= 0.0001f)
            {
                return 180f;
            }

            float error = Vector3.Angle(trailerForward.normalized, desiredForward.normalized);
            return allowOppositeHeading ? Mathf.Min(error, 180f - error) : error;
        }

        public static bool IsHeadingValid(float headingErrorDegrees, float allowedHeadingErrorDegrees)
        {
            return headingErrorDegrees <= Mathf.Max(0f, allowedHeadingErrorDegrees);
        }

        public static float CalculateContainmentRatio(IReadOnlyList<Vector3> zoneLocalSamplePoints, Vector3 zoneDimensions, float toleranceMeters)
        {
            if (zoneLocalSamplePoints == null || zoneLocalSamplePoints.Count == 0)
            {
                return 0f;
            }

            Vector3 half = new Vector3(
                Mathf.Max(0.01f, zoneDimensions.x * 0.5f + toleranceMeters),
                Mathf.Max(0.01f, zoneDimensions.y * 0.5f + toleranceMeters),
                Mathf.Max(0.01f, zoneDimensions.z * 0.5f + toleranceMeters));
            int contained = 0;
            for (int i = 0; i < zoneLocalSamplePoints.Count; i++)
            {
                Vector3 point = zoneLocalSamplePoints[i];
                if (Mathf.Abs(point.x) <= half.x &&
                    Mathf.Abs(point.y) <= half.y &&
                    Mathf.Abs(point.z) <= half.z)
                {
                    contained++;
                }
            }

            return contained / (float)zoneLocalSamplePoints.Count;
        }

        public static bool IsStopped(
            float linearSpeedMetersPerSecond,
            float angularSpeedDegreesPerSecond,
            float maximumLinearSpeedMetersPerSecond,
            float maximumAngularSpeedDegreesPerSecond)
        {
            return linearSpeedMetersPerSecond <= Mathf.Max(0f, maximumLinearSpeedMetersPerSecond) &&
                   angularSpeedDegreesPerSecond <= Mathf.Max(0f, maximumAngularSpeedDegreesPerSecond);
        }

        public static float AdvanceValidHold(float currentSeconds, bool currentlyValid, float deltaTime)
        {
            return currentlyValid ? Mathf.Max(0f, currentSeconds) + Mathf.Max(0f, deltaTime) : 0f;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}