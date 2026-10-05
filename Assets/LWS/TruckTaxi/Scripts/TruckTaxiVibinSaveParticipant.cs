using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiVibinSavePayload
    {
        public int version = 1;
        public string regionId;
        public TruckTaxiPersistentState session;
        public TruckTaxiDriverNeedsSnapshot needs;
    }

    // Embedded participant JSON follows the existing LWS providers. The facade/Pixel Crushers
    // semantic saver owns the enclosing snapshot, serialization, slots, backups and storage.
    public sealed class TruckTaxiVibinSaveParticipant : ILwsSaveParticipant
    {
        public const string Id = "lws.truck-taxi.vibin";
        private readonly Func<TruckTaxiVibinSavePayload> capture;
        private readonly Func<TruckTaxiVibinSavePayload, LwsSaveOperationResult> restore;
        private readonly Func<TruckTaxiVibinSavePayload, LwsSaveOperationResult> validate;

        public TruckTaxiVibinSaveParticipant(Func<TruckTaxiVibinSavePayload> capture,
            Func<TruckTaxiVibinSavePayload, LwsSaveOperationResult> restore,
            Func<TruckTaxiVibinSavePayload, LwsSaveOperationResult> validate)
        { this.capture = capture; this.restore = restore; this.validate = validate; }

        public string ParticipantId => Id;
        public int PayloadVersion => 1;
        public LwsSaveParticipantState CaptureState()
        {
            var payload = capture();
            var result = validate(payload);
            if (!result.Succeeded) throw new InvalidOperationException("Vibin semantic state is not ready to save.");
            return new LwsSaveParticipantState { participantId = Id, payloadVersion = 1, payloadJson = JsonUtility.ToJson(payload) };
        }
        public LwsSaveOperationResult RestoreState(LwsSaveParticipantState state)
        {
            var result = Read(state, out var payload);
            if (!result.Succeeded) return result;
            result = validate(payload);
            return result.Succeeded ? restore(payload) : result;
        }
        public LwsSaveOperationResult ClearState() => LwsSaveOperationResult.Success();
        public LwsSaveOperationResult ValidateParticipant() => capture != null && restore != null && validate != null
            ? LwsSaveOperationResult.Success() : LwsSaveOperationResult.Failure("Vibin participant is not bound.");

        public static LwsSaveOperationResult Read(LwsSaveParticipantState state, out TruckTaxiVibinSavePayload payload)
        {
            payload = null;
            if (state == null || state.participantId != Id || state.payloadVersion != 1 || string.IsNullOrWhiteSpace(state.payloadJson))
                return LwsSaveOperationResult.Failure("Missing or unsupported Vibin participant.");
            try { payload = JsonUtility.FromJson<TruckTaxiVibinSavePayload>(state.payloadJson); }
            catch { return LwsSaveOperationResult.Failure("Unreadable Vibin participant."); }
            return payload != null && payload.version == 1 ? LwsSaveOperationResult.Success()
                : LwsSaveOperationResult.Failure("Unsupported Vibin payload version.");
        }

        public static LwsSaveOperationResult ValidatePayload(TruckTaxiVibinSavePayload payload, TruckTaxiEnvironmentSettings settings)
        {
            if (payload == null || payload.version != 1 || string.IsNullOrWhiteSpace(payload.regionId) || settings == null)
                return LwsSaveOperationResult.Failure("Missing Vibin region or semantic state.");
            try
            {
                if (!TruckTaxiSession.ValidatePersistentState(payload.session, out _))
                    return LwsSaveOperationResult.Failure("Invalid Vibin session state.");
                // Never validate by tentatively applying inventory/needs to the live coordinator.
                if (!new TruckTaxiDriverNeedsState(settings, 17).RestorePersistentState(payload.needs, out _))
                    return LwsSaveOperationResult.Failure("Invalid Vibin needs or inventory state.");
            }
            catch { return LwsSaveOperationResult.Failure("Malformed Vibin semantic state."); }
            return LwsSaveOperationResult.Success();
        }

        public static LwsSaveOperationResult ValidateSnapshot(LwsSaveSnapshot snapshot, TruckTaxiEnvironmentSettings settings,
            out TruckTaxiVibinSavePayload payload, out LwsWorldResumeContextPayload resume)
        {
            payload = null; resume = null;
            if (snapshot == null || snapshot.schemaVersion != LwsSaveSchema.CurrentVersion || snapshot.participants == null)
                return LwsSaveOperationResult.Failure("Missing or unsupported Vibin snapshot.");
            var states = new Dictionary<string, LwsSaveParticipantState>(StringComparer.Ordinal);
            foreach (var state in snapshot.participants)
            {
                if (state == null || string.IsNullOrEmpty(state.participantId) || state.payloadVersion != 1 ||
                    string.IsNullOrWhiteSpace(state.payloadJson) || states.ContainsKey(state.participantId))
                    return LwsSaveOperationResult.Failure("Invalid or duplicate save participant.");
                try
                {
                    if (JsonUtility.FromJson<LwsSaveParticipantState>(state.payloadJson) == null)
                        return LwsSaveOperationResult.Failure("Unreadable save participant.");
                }
                catch { return LwsSaveOperationResult.Failure("Unreadable save participant."); }
                states.Add(state.participantId, state);
            }
            foreach (string id in new[] { Id, LwsSaveSchema.WorldResumeContextParticipantId,
                "lws.world.global-position", "lws.vehicle.player-truck", "lws.game-clock" })
                if (!states.ContainsKey(id)) return LwsSaveOperationResult.Failure("Missing required Vibin save participant.");
            var read = Read(states[Id], out payload);
            if (!read.Succeeded) return read;
            var semantic = ValidatePayload(payload, settings);
            if (!semantic.Succeeded) return semantic;
            try
            {
                resume = JsonUtility.FromJson<LwsWorldResumeContextPayload>(states[LwsSaveSchema.WorldResumeContextParticipantId].payloadJson);
                var truck = JsonUtility.FromJson<LwsPlayerTruckSavePayload>(states["lws.vehicle.player-truck"].payloadJson);
                var global = JsonUtility.FromJson<LwsGlobalPositionSavePayload>(states["lws.world.global-position"].payloadJson);
                var clock = JsonUtility.FromJson<LwsGameClockSavePayload>(states["lws.game-clock"].payloadJson);
                if (resume == null || !resume.IsValid || resume.stableWorldId != LwsWorldResumeCatalog.TruckTaxiWorldId ||
                    !resume.hasGlobalPosition || !Finite(resume.savedGlobalX, resume.savedGlobalY, resume.savedGlobalZ) ||
                    !ValidRotation(new Quaternion(resume.rotationX, resume.rotationY, resume.rotationZ, resume.rotationW)))
                    return LwsSaveOperationResult.Failure("Invalid Vibin world resume context.");
                if (truck == null || truck.schemaVersion != LwsSaveSchema.CurrentVersion || !truck.hasGlobalPosition ||
                    !Finite(truck.globalX, truck.globalY, truck.globalZ) || !ValidRotation(truck.state.pose.rotation) ||
                    truck.state.trailerAttachment.attached)
                    return LwsSaveOperationResult.Failure("Invalid truck pose or unsupported attached trailer.");
                if (!global.IsValid || !Finite(global.globalX, global.globalY, global.globalZ) ||
                    !ValidRotation(new Quaternion(global.rotationX, global.rotationY, global.rotationZ, global.rotationW)) ||
                    Math.Abs(truck.globalX - resume.savedGlobalX) > .25 || Math.Abs(truck.globalY - resume.savedGlobalY) > .25 ||
                    Math.Abs(truck.globalZ - resume.savedGlobalZ) > .25)
                    return LwsSaveOperationResult.Failure("Inconsistent saved global position.");
                if (clock.year < 1 || clock.year > 9999 || clock.month < 1 || clock.month > 12 ||
                    clock.day < 1 || clock.day > DateTime.DaysInMonth(clock.year, clock.month) ||
                    clock.hour < 0 || clock.hour > 23 || clock.minute < 0 || clock.minute > 59 ||
                    !float.IsFinite(clock.second) || clock.second < 0 || clock.second >= 60 ||
                    !float.IsFinite(clock.timeScale) || clock.timeScale < 0 || clock.timeScale > 600 ||
                    !double.IsFinite(clock.totalGameSeconds) || clock.totalGameSeconds < 0)
                    return LwsSaveOperationResult.Failure("Invalid saved game clock.");
            }
            catch { return LwsSaveOperationResult.Failure("Unreadable required save participant."); }
            return LwsSaveOperationResult.Success();
        }

        private static bool Finite(double x, double y, double z) => double.IsFinite(x) && double.IsFinite(y) && double.IsFinite(z);
        private static bool ValidRotation(Quaternion q) => float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) &&
            float.IsFinite(q.w) && q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > .0001f &&
            q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w < 4;
    }
}
