using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public sealed class LwsActiveJobSaveParticipant : ILwsSaveParticipant
    {
        private readonly Func<LwsServiceRegistry> _registryProvider;
        private LwsActiveJobSavePayload _lastRestored;

        public LwsActiveJobSaveParticipant(Func<LwsServiceRegistry> registryProvider)
        {
            _registryProvider = registryProvider;
        }

        public string ParticipantId => LwsSaveSchema.ActiveJobParticipantId;
        public int PayloadVersion => 1;
        public LwsActiveJobSavePayload LastRestored => _lastRestored;

        public LwsSaveParticipantState CaptureState()
        {
            LwsActiveJob activeJob = null;
            if (TryGetService(out ILwsActiveJobService activeJobService))
            {
                activeJob = activeJobService.CurrentActiveJob;
            }

            var payload = new LwsActiveJobSavePayload
            {
                schemaVersion = LwsSaveSchema.CurrentVersion,
                hasActiveJob = activeJob != null && activeJob.IsValid,
                activeJob = activeJob != null && activeJob.IsValid ? activeJob.Clone() : null
            };

            return new LwsSaveParticipantState
            {
                participantId = ParticipantId,
                payloadVersion = PayloadVersion,
                payloadJson = JsonUtility.ToJson(payload)
            };
        }

        public LwsSaveOperationResult RestoreState(LwsSaveParticipantState state)
        {
            if (state == null || state.participantId != ParticipantId)
            {
                return LwsSaveOperationResult.Failure($"Payload does not belong to {ParticipantId}.");
            }

            _lastRestored = JsonUtility.FromJson<LwsActiveJobSavePayload>(state.payloadJson);
            if (!_lastRestored.IsValid)
            {
                return LwsSaveOperationResult.Failure("Active job payload is invalid.");
            }

            if (!TryGetService(out ILwsActiveJobService activeJobService))
            {
                return LwsSaveOperationResult.Failure("Active job service is not available for restore.");
            }

            return activeJobService.RestoreActiveJob(
                _lastRestored.hasActiveJob ? _lastRestored.activeJob : null,
                "Active job restored from Pixel Crushers semantic save payload.",
                false).ToSaveOperationResult();
        }

        public LwsSaveOperationResult ClearState()
        {
            _lastRestored = default;
            if (TryGetService(out ILwsActiveJobService activeJobService))
            {
                activeJobService.ClearActiveJob("Active job cleared by save participant.");
            }

            return LwsSaveOperationResult.Success("Active job semantic state cleared.");
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return TryGetService(out ILwsActiveJobService _)
                ? LwsSaveOperationResult.Success("Active job save participant records semantic job state only.")
                : LwsSaveOperationResult.Failure("Active job save participant requires ILwsActiveJobService.");
        }

        private bool TryGetService<T>(out T service) where T : class, ILwsService
        {
            service = null;
            LwsServiceRegistry registry = _registryProvider?.Invoke();
            return registry != null && registry.TryGet(out service);
        }
    }

    [Serializable]
    public struct LwsActiveJobSavePayload
    {
        public int schemaVersion;
        public bool hasActiveJob;
        public LwsActiveJob activeJob;

        public bool IsValid => schemaVersion > 0 &&
                               schemaVersion <= LwsSaveSchema.CurrentVersion &&
                               (!hasActiveJob || (activeJob != null && activeJob.IsValid));
    }

    internal static class LwsActiveJobSaveResultExtensions
    {
        public static LwsSaveOperationResult ToSaveOperationResult(this LwsServiceResult result)
        {
            return result.Succeeded
                ? LwsSaveOperationResult.Success(result.Message)
                : LwsSaveOperationResult.Failure(result.Message);
        }
    }
}