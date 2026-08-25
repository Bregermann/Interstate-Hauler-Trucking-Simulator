using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsWorldOriginService : ILwsService
    {
        bool Enabled { get; }
        bool ShiftInProgress { get; }
        bool HasQueuedShift { get; }
        long OriginVersion { get; }
        long ShiftCount { get; }
        LwsWorldPositionD CurrentOriginOffset { get; }
        Vector3 PlayerLocalPosition { get; }
        LwsWorldPositionD PlayerGlobalPosition { get; }
        LwsFloatingOriginTuning ActiveTuning { get; }
        int RegisteredParticipantCount { get; }
        int LastParticipantsShifted { get; }
        int LastChunkRootsShifted { get; }
        int LastDynamicBodiesShifted { get; }
        int LastTrafficParticipantsShifted { get; }
        double LastShiftDurationMilliseconds { get; }
        string LastError { get; }
        LwsOriginShiftEvent LastShiftEvent { get; }

        event Action<LwsOriginShiftEvent> OriginShiftStarting;
        event Action<LwsOriginShiftEvent> OriginShiftCompleted;

        void Configure(LwsFloatingOriginTuning tuning);
        void SetEnabled(bool enabled);
        void UpdatePlayerLocalPosition(Vector3 localPosition);
        LwsWorldPositionD LocalToGlobal(Vector3 localPosition);
        Vector3 GlobalToLocal(LwsWorldPositionD globalPosition);
        bool ShouldShift(Vector3 playerLocalPosition);
        Vector3 CalculateShiftDelta(Vector3 playerLocalPosition);
        bool QueueShift(LwsOriginShiftRequest request);
        bool TryExecuteQueuedShift(out LwsOriginShiftEvent shiftEvent);
        bool ForceShiftNow(Vector3 playerLocalPosition, string reason, out LwsOriginShiftEvent shiftEvent);
        bool SetOriginOffset(LwsWorldPositionD targetOriginOffset, string reason, out LwsOriginShiftEvent shiftEvent);
        bool SetOriginOffsetForValidation(LwsWorldPositionD targetOriginOffset, string reason, out LwsOriginShiftEvent shiftEvent);
        void ResetValidationOrigin();
        void RegisterParticipant(ILwsFloatingOriginParticipant participant);
        void UnregisterParticipant(ILwsFloatingOriginParticipant participant);
    }

    public sealed class LwsWorldOriginService : ILwsWorldOriginService
    {
        private readonly List<ILwsFloatingOriginParticipant> _participants = new List<ILwsFloatingOriginParticipant>();
        private readonly HashSet<ILwsFloatingOriginParticipant> _participantSet = new HashSet<ILwsFloatingOriginParticipant>();

        private LwsOriginShiftRequest _queuedShift;
        private double _lastShiftTime = -999d;

        public string ServiceId => "lws.world.origin";
        public bool Enabled { get; private set; } = true;
        public bool ShiftInProgress { get; private set; }
        public bool HasQueuedShift { get; private set; }
        public long OriginVersion { get; private set; }
        public long ShiftCount { get; private set; }
        public LwsWorldPositionD CurrentOriginOffset { get; private set; } = LwsWorldPositionD.Zero;
        public Vector3 PlayerLocalPosition { get; private set; }
        public LwsWorldPositionD PlayerGlobalPosition => LocalToGlobal(PlayerLocalPosition);
        public LwsFloatingOriginTuning ActiveTuning { get; private set; }
        public int RegisteredParticipantCount => _participants.Count;
        public int LastParticipantsShifted { get; private set; }
        public int LastChunkRootsShifted { get; private set; }
        public int LastDynamicBodiesShifted { get; private set; }
        public int LastTrafficParticipantsShifted { get; private set; }
        public double LastShiftDurationMilliseconds { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public LwsOriginShiftEvent LastShiftEvent { get; private set; }

        public event Action<LwsOriginShiftEvent> OriginShiftStarting;
        public event Action<LwsOriginShiftEvent> OriginShiftCompleted;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            Configure(ActiveTuning);
            return LwsServiceResult.Success("LWS world origin service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _participants.Clear();
            _participantSet.Clear();
            HasQueuedShift = false;
            ShiftInProgress = false;
            CurrentOriginOffset = LwsWorldPositionD.Zero;
            PlayerLocalPosition = Vector3.zero;
            OriginVersion = 0;
            ShiftCount = 0;
            LastError = string.Empty;
            return LwsServiceResult.Success("LWS world origin service shut down.");
        }

        public void Configure(LwsFloatingOriginTuning tuning)
        {
            ActiveTuning = tuning != null ? tuning : LwsFloatingOriginTuning.CreateRuntimeDefault();
            Enabled = ActiveTuning.floatingOriginEnabled;
            if (!ActiveTuning.Validate(out string message))
            {
                LastError = message;
            }
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            if (!enabled)
            {
                HasQueuedShift = false;
            }
        }

        public void UpdatePlayerLocalPosition(Vector3 localPosition)
        {
            PlayerLocalPosition = localPosition;
        }

        public LwsWorldPositionD LocalToGlobal(Vector3 localPosition)
        {
            return LwsWorldCoordinateUtility.LocalToGlobal(localPosition, CurrentOriginOffset);
        }

        public Vector3 GlobalToLocal(LwsWorldPositionD globalPosition)
        {
            return LwsWorldCoordinateUtility.GlobalToLocal(globalPosition, CurrentOriginOffset);
        }

        public bool ShouldShift(Vector3 playerLocalPosition)
        {
            return CalculateShiftDelta(playerLocalPosition).sqrMagnitude > 0.0001f;
        }

        public Vector3 CalculateShiftDelta(Vector3 playerLocalPosition)
        {
            return LwsWorldCoordinateUtility.CalculateGridAlignedShift(playerLocalPosition, ActiveTuning);
        }

        public bool QueueShift(LwsOriginShiftRequest request)
        {
            if (!Enabled || ShiftInProgress || !request.IsValid)
            {
                return false;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            float cooldown = ActiveTuning != null ? Mathf.Max(0f, ActiveTuning.minimumSecondsBetweenShifts) : 0f;
            if (now - _lastShiftTime < cooldown)
            {
                return false;
            }

            _queuedShift = request;
            HasQueuedShift = true;
            return true;
        }

        public bool TryExecuteQueuedShift(out LwsOriginShiftEvent shiftEvent)
        {
            shiftEvent = default;
            if (!HasQueuedShift)
            {
                return false;
            }

            LwsOriginShiftRequest request = _queuedShift;
            HasQueuedShift = false;
            return ExecuteShift(request, out shiftEvent);
        }

        public bool ForceShiftNow(Vector3 playerLocalPosition, string reason, out LwsOriginShiftEvent shiftEvent)
        {
            Vector3 delta = CalculateShiftDelta(playerLocalPosition);
            if (delta.sqrMagnitude <= 0.0001f)
            {
                delta = new Vector3(
                    ActiveTuning != null && ActiveTuning.shiftXAxis ? playerLocalPosition.x : 0f,
                    ActiveTuning != null && ActiveTuning.shiftYAxis ? playerLocalPosition.y : 0f,
                    ActiveTuning != null && ActiveTuning.shiftZAxis ? playerLocalPosition.z : 0f);
            }

            return ExecuteShift(new LwsOriginShiftRequest(delta, reason), out shiftEvent);
        }

        public bool SetOriginOffset(LwsWorldPositionD targetOriginOffset, string reason, out LwsOriginShiftEvent shiftEvent)
        {
            LwsWorldPositionD delta = targetOriginOffset - CurrentOriginOffset;
            if (delta.SqrMagnitude <= 0.000001d)
            {
                shiftEvent = LastShiftEvent;
                return true;
            }

            return ExecuteShift(
                new LwsOriginShiftRequest(delta.ToVector3(), string.IsNullOrWhiteSpace(reason) ? "Origin offset set." : reason),
                out shiftEvent);
        }

        public bool SetOriginOffsetForValidation(LwsWorldPositionD targetOriginOffset, string reason, out LwsOriginShiftEvent shiftEvent)
        {
            return SetOriginOffset(targetOriginOffset, string.IsNullOrWhiteSpace(reason) ? "Validation origin offset set." : reason, out shiftEvent);
        }

        public void ResetValidationOrigin()
        {
            CurrentOriginOffset = LwsWorldPositionD.Zero;
            PlayerLocalPosition = Vector3.zero;
            OriginVersion++;
            ShiftCount = 0;
            LastParticipantsShifted = 0;
            LastChunkRootsShifted = 0;
            LastDynamicBodiesShifted = 0;
            LastTrafficParticipantsShifted = 0;
            LastShiftDurationMilliseconds = 0d;
            LastError = "Validation origin reset. Loaded spatial roots were not translated.";
        }

        public void RegisterParticipant(ILwsFloatingOriginParticipant participant)
        {
            if (!IsAlive(participant) || _participantSet.Contains(participant))
            {
                return;
            }

            _participantSet.Add(participant);
            _participants.Add(participant);
            if (participant.AlignToCurrentOriginOnRegistration &&
                CurrentOriginOffset.SqrMagnitude > 0.000001d)
            {
                Vector3 localDelta = (-CurrentOriginOffset).ToVector3();
                var alignment = new LwsOriginShiftEvent(
                    OriginVersion,
                    OriginVersion,
                    CurrentOriginOffset,
                    CurrentOriginOffset,
                    Vector3.zero,
                    localDelta,
                    "Initial floating-origin alignment.",
                    true,
                    1,
                    participant.ParticipantKind == LwsFloatingOriginParticipantKind.LoadedChunkRoot ? 1 : 0,
                    IsDynamicKind(participant.ParticipantKind) ? 1 : 0,
                    IsTrafficKind(participant.ParticipantKind) ? 1 : 0,
                    0d,
                    "Participant aligned to current origin.");
                participant.ApplyOriginShift(alignment, out _);
            }
        }

        public void UnregisterParticipant(ILwsFloatingOriginParticipant participant)
        {
            if (participant == null || !_participantSet.Remove(participant))
            {
                return;
            }

            _participants.Remove(participant);
        }

        private bool ExecuteShift(LwsOriginShiftRequest request, out LwsOriginShiftEvent shiftEvent)
        {
            shiftEvent = default;
            if (!Enabled || ShiftInProgress || !request.IsValid)
            {
                return false;
            }

            Vector3 shiftDelta = request.GlobalShiftDelta;
            Vector3 localDelta = -shiftDelta;
            LwsWorldPositionD previousOffset = CurrentOriginOffset;
            LwsWorldPositionD newOffset = previousOffset + LwsWorldPositionD.FromVector3(shiftDelta);
            long previousVersion = OriginVersion;
            long newVersion = OriginVersion + 1;
            var starting = new LwsOriginShiftEvent(
                previousVersion,
                newVersion,
                previousOffset,
                newOffset,
                shiftDelta,
                localDelta,
                request.Reason,
                false,
                0,
                0,
                0,
                0,
                0d,
                "Origin shift starting.");

            ShiftInProgress = true;
            OriginShiftStarting?.Invoke(starting);
            double started = Time.realtimeSinceStartupAsDouble;

            int shifted = 0;
            int chunks = 0;
            int dynamicBodies = 0;
            int traffic = 0;
            LastError = string.Empty;

            for (int i = _participants.Count - 1; i >= 0; i--)
            {
                ILwsFloatingOriginParticipant participant = _participants[i];
                if (!IsAlive(participant))
                {
                    _participants.RemoveAt(i);
                    _participantSet.Remove(participant);
                    continue;
                }

                var participantEvent = new LwsOriginShiftEvent(
                    previousVersion,
                    newVersion,
                    previousOffset,
                    newOffset,
                    shiftDelta,
                    localDelta,
                    request.Reason,
                    false,
                    0,
                    0,
                    0,
                    0,
                    0d,
                    "Applying origin shift to participant.");
                if (!participant.ApplyOriginShift(participantEvent, out string message))
                {
                    LastError = string.IsNullOrWhiteSpace(message)
                        ? $"Floating-origin participant {participant.ParticipantId} failed to shift."
                        : message;
                    continue;
                }

                shifted++;
                if (participant.ParticipantKind == LwsFloatingOriginParticipantKind.LoadedChunkRoot)
                {
                    chunks++;
                }

                if (IsDynamicKind(participant.ParticipantKind))
                {
                    dynamicBodies++;
                }

                if (IsTrafficKind(participant.ParticipantKind))
                {
                    traffic++;
                }
            }

            CurrentOriginOffset = newOffset;
            OriginVersion = newVersion;
            ShiftCount++;
            _lastShiftTime = Time.realtimeSinceStartupAsDouble;

            if (ActiveTuning == null || ActiveTuning.syncPhysicsTransforms)
            {
                Physics.SyncTransforms();
            }

            double durationMs = (Time.realtimeSinceStartupAsDouble - started) * 1000d;
            LastParticipantsShifted = shifted;
            LastChunkRootsShifted = chunks;
            LastDynamicBodiesShifted = dynamicBodies;
            LastTrafficParticipantsShifted = traffic;
            LastShiftDurationMilliseconds = durationMs;

            shiftEvent = new LwsOriginShiftEvent(
                previousVersion,
                newVersion,
                previousOffset,
                newOffset,
                shiftDelta,
                localDelta,
                request.Reason,
                false,
                shifted,
                chunks,
                dynamicBodies,
                traffic,
                durationMs,
                string.IsNullOrWhiteSpace(LastError) ? "Origin shift completed." : LastError);
            LastShiftEvent = shiftEvent;
            ShiftInProgress = false;
            OriginShiftCompleted?.Invoke(shiftEvent);
            return true;
        }

        private static bool IsDynamicKind(LwsFloatingOriginParticipantKind kind)
        {
            return kind == LwsFloatingOriginParticipantKind.PlayerTractor ||
                   kind == LwsFloatingOriginParticipantKind.PlayerTrailer ||
                   kind == LwsFloatingOriginParticipantKind.TrafficVehicle;
        }

        private static bool IsTrafficKind(LwsFloatingOriginParticipantKind kind)
        {
            return kind == LwsFloatingOriginParticipantKind.TrafficLaneRoot ||
                   kind == LwsFloatingOriginParticipantKind.TrafficVehicle;
        }

        private static bool IsAlive(ILwsFloatingOriginParticipant participant)
        {
            if (participant == null)
            {
                return false;
            }

            return participant is UnityEngine.Object unityObject
                ? unityObject != null
                : participant.ParticipantTransform != null;
        }
    }

}
