using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsFloatingOriginParticipantKind
    {
        GenericSpatialRoot,
        LoadedChunkRoot,
        PlayerTractor,
        PlayerTrailer,
        TrafficLaneRoot,
        TrafficVehicle,
        RoadConditionPresentation,
        WeatheradePresentation
    }

    public readonly struct LwsOriginShiftRequest
    {
        public LwsOriginShiftRequest(Vector3 globalShiftDelta, string reason)
        {
            GlobalShiftDelta = globalShiftDelta;
            Reason = reason ?? string.Empty;
            RequestedAtSeconds = Time.realtimeSinceStartupAsDouble;
        }

        public Vector3 GlobalShiftDelta { get; }
        public string Reason { get; }
        public double RequestedAtSeconds { get; }
        public bool IsValid => GlobalShiftDelta.sqrMagnitude > 0.0001f;
    }

    public readonly struct LwsOriginShiftEvent
    {
        public LwsOriginShiftEvent(
            long previousOriginVersion,
            long newOriginVersion,
            LwsWorldPositionD previousOriginOffset,
            LwsWorldPositionD newOriginOffset,
            Vector3 globalShiftDelta,
            Vector3 localTranslationDelta,
            string reason,
            bool initialAlignment,
            int participantsShifted,
            int chunkRootsShifted,
            int dynamicBodiesShifted,
            int trafficParticipantsShifted,
            double durationMilliseconds,
            string message)
        {
            PreviousOriginVersion = previousOriginVersion;
            NewOriginVersion = newOriginVersion;
            PreviousOriginOffset = previousOriginOffset;
            NewOriginOffset = newOriginOffset;
            GlobalShiftDelta = globalShiftDelta;
            LocalTranslationDelta = localTranslationDelta;
            Reason = reason ?? string.Empty;
            InitialAlignment = initialAlignment;
            ParticipantsShifted = participantsShifted;
            ChunkRootsShifted = chunkRootsShifted;
            DynamicBodiesShifted = dynamicBodiesShifted;
            TrafficParticipantsShifted = trafficParticipantsShifted;
            DurationMilliseconds = durationMilliseconds;
            Message = message ?? string.Empty;
        }

        public long PreviousOriginVersion { get; }
        public long NewOriginVersion { get; }
        public LwsWorldPositionD PreviousOriginOffset { get; }
        public LwsWorldPositionD NewOriginOffset { get; }
        public Vector3 GlobalShiftDelta { get; }
        public Vector3 LocalTranslationDelta { get; }
        public string Reason { get; }
        public bool InitialAlignment { get; }
        public int ParticipantsShifted { get; }
        public int ChunkRootsShifted { get; }
        public int DynamicBodiesShifted { get; }
        public int TrafficParticipantsShifted { get; }
        public double DurationMilliseconds { get; }
        public string Message { get; }
    }

    public interface ILwsFloatingOriginParticipant
    {
        string ParticipantId { get; }
        LwsFloatingOriginParticipantKind ParticipantKind { get; }
        Transform ParticipantTransform { get; }
        bool AlignToCurrentOriginOnRegistration { get; }
        bool ApplyOriginShift(LwsOriginShiftEvent shiftEvent, out string message);
    }

    public static class LwsWorldCoordinateUtility
    {
        public static LwsWorldPositionD LocalToGlobal(Vector3 localPosition, LwsWorldPositionD originOffset)
        {
            return LwsWorldPositionD.FromVector3(localPosition) + originOffset;
        }

        public static Vector3 GlobalToLocal(LwsWorldPositionD globalPosition, LwsWorldPositionD originOffset)
        {
            return (globalPosition - originOffset).ToVector3();
        }

        public static Vector3 CalculateGridAlignedShift(Vector3 playerLocalPosition, LwsFloatingOriginTuning tuning)
        {
            if (tuning == null || !tuning.floatingOriginEnabled)
            {
                return Vector3.zero;
            }

            float horizontalDistance = new Vector2(playerLocalPosition.x, playerLocalPosition.z).magnitude;
            if (horizontalDistance < tuning.shiftThresholdMeters)
            {
                return Vector3.zero;
            }

            float grid = Mathf.Max(1f, tuning.shiftGridMeters);
            return new Vector3(
                tuning.shiftXAxis ? TruncateToGrid(playerLocalPosition.x, grid) : 0f,
                tuning.shiftYAxis ? TruncateToGrid(playerLocalPosition.y, grid) : 0f,
                tuning.shiftZAxis ? TruncateToGrid(playerLocalPosition.z, grid) : 0f);
        }

        public static Bounds GlobalBoundsToLocal(Bounds globalBounds, LwsWorldPositionD originOffset)
        {
            Vector3 localCenter = GlobalToLocal(LwsWorldPositionD.FromVector3(globalBounds.center), originOffset);
            return new Bounds(localCenter, globalBounds.size);
        }

        private static float TruncateToGrid(float value, float grid)
        {
            if (Mathf.Abs(value) < grid)
            {
                return 0f;
            }

            return Mathf.Sign(value) * Mathf.Floor(Mathf.Abs(value) / grid) * grid;
        }
    }
}
