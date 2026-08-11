using System;
using System.Collections.Generic;
using System.Linq;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsSaveParticipantState
    {
        public string participantId;
        public int payloadVersion;
        public string payloadJson;
    }

    [Serializable]
    public sealed class LwsSaveSnapshot
    {
        public int schemaVersion = 1;
        public string profileId;
        public long capturedUtcTicks;
        public List<LwsSaveParticipantState> participants = new List<LwsSaveParticipantState>();
    }

    public readonly struct LwsSaveOperationResult
    {
        public bool Succeeded { get; }
        public string Message { get; }

        private LwsSaveOperationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        public static LwsSaveOperationResult Success(string message = "")
        {
            return new LwsSaveOperationResult(true, message);
        }

        public static LwsSaveOperationResult Failure(string message)
        {
            return new LwsSaveOperationResult(false, string.IsNullOrWhiteSpace(message) ? "Save operation failed." : message);
        }
    }

    public interface ILwsSaveParticipant
    {
        string ParticipantId { get; }
        int PayloadVersion { get; }
        LwsSaveParticipantState CaptureState();
        LwsSaveOperationResult RestoreState(LwsSaveParticipantState state);
        LwsSaveOperationResult ClearState();
        LwsSaveOperationResult ValidateParticipant();
    }

    public interface ILwsSaveStorage
    {
        bool Exists(string slotId);
        LwsSaveOperationResult Write(string slotId, LwsSaveSnapshot snapshot);
        LwsSaveOperationResult Read(string slotId, out LwsSaveSnapshot snapshot);
        LwsSaveOperationResult Delete(string slotId);
    }

    public interface ILwsSaveService : ILwsService
    {
        IReadOnlyList<ILwsSaveParticipant> Participants { get; }
        LwsSaveOperationResult RegisterParticipant(ILwsSaveParticipant participant);
        LwsSaveOperationResult UnregisterParticipant(string participantId);
        LwsSaveOperationResult ValidateParticipants();
        LwsSaveSnapshot CaptureSnapshot(string profileId);
        LwsSaveOperationResult RestoreSnapshot(LwsSaveSnapshot snapshot);
        LwsSaveOperationResult ClearAllParticipants();
    }

    public sealed class LwsSaveService : ILwsSaveService
    {
        private readonly List<ILwsSaveParticipant> _participants = new List<ILwsSaveParticipant>();
        private readonly HashSet<string> _participantIds = new HashSet<string>(StringComparer.Ordinal);

        public string ServiceId => "lws.save";
        public IReadOnlyList<ILwsSaveParticipant> Participants => _participants;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            RegisterParticipant(new LwsPlaceholderSaveParticipant("pixel-crushers.dialogue", 1));
            RegisterParticipant(new LwsPlaceholderSaveParticipant("compass.navigator", 1));
            RegisterParticipant(new LwsPlaceholderSaveParticipant("vehicle.truck", 1));
            RegisterParticipant(new LwsPlaceholderSaveParticipant("world.state", 1));
            RegisterParticipant(new LwsPlaceholderSaveParticipant("weather.state", 1));
            RegisterParticipant(new LwsPlaceholderSaveParticipant("jobs.state", 1));

            LwsSaveOperationResult validation = ValidateParticipants();
            return validation.Succeeded
                ? LwsServiceResult.Success("LWS save service initialized.")
                : LwsServiceResult.Failure(validation.Message);
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _participants.Clear();
            _participantIds.Clear();
            return LwsServiceResult.Success("LWS save service shut down.");
        }

        public LwsSaveOperationResult RegisterParticipant(ILwsSaveParticipant participant)
        {
            if (participant == null)
            {
                return LwsSaveOperationResult.Failure("Cannot register a null save participant.");
            }

            if (string.IsNullOrWhiteSpace(participant.ParticipantId))
            {
                return LwsSaveOperationResult.Failure("Save participant ID must be stable and non-empty.");
            }

            if (!_participantIds.Add(participant.ParticipantId))
            {
                return LwsSaveOperationResult.Failure($"Duplicate save participant ID: {participant.ParticipantId}");
            }

            _participants.Add(participant);
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult UnregisterParticipant(string participantId)
        {
            int index = _participants.FindIndex(p => p.ParticipantId == participantId);
            if (index < 0)
            {
                return LwsSaveOperationResult.Failure($"Save participant not found: {participantId}");
            }

            _participantIds.Remove(participantId);
            _participants.RemoveAt(index);
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipants()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ILwsSaveParticipant participant in _participants)
            {
                if (participant == null)
                {
                    return LwsSaveOperationResult.Failure("Null save participant registered.");
                }

                if (string.IsNullOrWhiteSpace(participant.ParticipantId))
                {
                    return LwsSaveOperationResult.Failure("Save participant has an empty ID.");
                }

                if (!seen.Add(participant.ParticipantId))
                {
                    return LwsSaveOperationResult.Failure($"Duplicate save participant ID: {participant.ParticipantId}");
                }

                LwsSaveOperationResult participantResult = participant.ValidateParticipant();
                if (!participantResult.Succeeded)
                {
                    return participantResult;
                }
            }

            return LwsSaveOperationResult.Success();
        }

        public LwsSaveSnapshot CaptureSnapshot(string profileId)
        {
            return new LwsSaveSnapshot
            {
                profileId = profileId ?? string.Empty,
                capturedUtcTicks = DateTime.UtcNow.Ticks,
                participants = _participants.Select(p => p.CaptureState()).ToList()
            };
        }

        public LwsSaveOperationResult RestoreSnapshot(LwsSaveSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return LwsSaveOperationResult.Failure("Cannot restore a null save snapshot.");
            }

            foreach (LwsSaveParticipantState payload in snapshot.participants)
            {
                ILwsSaveParticipant participant = _participants.FirstOrDefault(p => p.ParticipantId == payload.participantId);
                if (participant == null)
                {
                    continue;
                }

                LwsSaveOperationResult result = participant.RestoreState(payload);
                if (!result.Succeeded)
                {
                    return result;
                }
            }

            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ClearAllParticipants()
        {
            foreach (ILwsSaveParticipant participant in _participants)
            {
                LwsSaveOperationResult result = participant.ClearState();
                if (!result.Succeeded)
                {
                    return result;
                }
            }

            return LwsSaveOperationResult.Success();
        }
    }

    public sealed class LwsPlaceholderSaveParticipant : ILwsSaveParticipant
    {
        public LwsPlaceholderSaveParticipant(string participantId, int payloadVersion)
        {
            ParticipantId = participantId;
            PayloadVersion = payloadVersion;
        }

        public string ParticipantId { get; }
        public int PayloadVersion { get; }

        public LwsSaveParticipantState CaptureState()
        {
            return new LwsSaveParticipantState
            {
                participantId = ParticipantId,
                payloadVersion = PayloadVersion,
                payloadJson = "{}"
            };
        }

        public LwsSaveOperationResult RestoreState(LwsSaveParticipantState state)
        {
            if (state == null || state.participantId != ParticipantId)
            {
                return LwsSaveOperationResult.Failure($"Payload does not belong to {ParticipantId}.");
            }

            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ClearState()
        {
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            return PayloadVersion > 0
                ? LwsSaveOperationResult.Success()
                : LwsSaveOperationResult.Failure($"Invalid payload version for {ParticipantId}.");
        }
    }

    public sealed class LwsInMemorySaveStorage : ILwsSaveStorage
    {
        private readonly Dictionary<string, LwsSaveSnapshot> _snapshots = new Dictionary<string, LwsSaveSnapshot>(StringComparer.Ordinal);

        public bool Exists(string slotId)
        {
            return _snapshots.ContainsKey(slotId ?? string.Empty);
        }

        public LwsSaveOperationResult Write(string slotId, LwsSaveSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                return LwsSaveOperationResult.Failure("Save slot ID is required.");
            }

            if (snapshot == null)
            {
                return LwsSaveOperationResult.Failure("Cannot write a null snapshot.");
            }

            _snapshots[slotId] = snapshot;
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult Read(string slotId, out LwsSaveSnapshot snapshot)
        {
            if (_snapshots.TryGetValue(slotId ?? string.Empty, out snapshot))
            {
                return LwsSaveOperationResult.Success();
            }

            return LwsSaveOperationResult.Failure($"Save slot not found: {slotId}");
        }

        public LwsSaveOperationResult Delete(string slotId)
        {
            _snapshots.Remove(slotId ?? string.Empty);
            return LwsSaveOperationResult.Success();
        }
    }
}
