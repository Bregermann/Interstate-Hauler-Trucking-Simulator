using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiVibinPersistence
    {
        private readonly List<TruckTaxiWorldVisit> worldVisits = new List<TruckTaxiWorldVisit>();
        public string WorldTransferBlockReason => TruckTaxiWorldTravel.IsBusy ? "Regional transfer is already active." :
            SaveBlockReason() ?? (host.WorldLife?.ActiveNozzleStation != null ? "Return the fuel nozzle before regional travel." :
            host.WorldLife?.FocusRoot != null ? "Close the current interaction before regional travel." : null);

        // Both snapshots use existing participants. No independent serialization or physical storage.
        public LwsSaveSnapshot CaptureWorldTransfer()
        {
            if (WorldTransferBlockReason != null) throw new InvalidOperationException(WorldTransferBlockReason);
            var snapshot = SaveService.CaptureSnapshot(RunProfileId);
            var payload = ReadTravelPayload(snapshot);
            string scene = SceneManager.GetActiveScene().name;
            var clock = host.Environment.Clock.CurrentSnapshot;
            var global = origin.LocalToGlobal(body.position);
            var visit = new TruckTaxiWorldVisit { scene = scene, position = global.ToVector3(), rotation = body.rotation,
                gameSeconds = CalendarSeconds(clock), clockRate = clock.timeScale, environment = payload.environment,
                extremes = payload.extremes, workArea = payload.session.workArea,
                weather = snapshot.participants.Find(p => p.participantId == "lws.weather.semantic"),
                roadCondition = snapshot.participants.Find(p => p.participantId == "lws.road-condition.semantic") };
            worldVisits.RemoveAll(v => v.scene == scene); worldVisits.Add(visit);
            payload.worldVisits = worldVisits.ToArray();
            WriteTravelPayload(snapshot, payload);
            return snapshot;
        }

        private static double CalendarSeconds(LwsGameClockSnapshot clock) =>
            TruckTaxiCalendarWeatherService.SnapshotDateTime(clock).Ticks / (double)TimeSpan.TicksPerSecond;

        public static TruckTaxiVibinSavePayload ReadTravelPayload(LwsSaveSnapshot snapshot)
        {
            var state = snapshot.participants.Find(p => p.participantId == TruckTaxiVibinSaveParticipant.Id);
            var result = TruckTaxiVibinSaveParticipant.Read(state, out var payload);
            if (!result.Succeeded) throw new InvalidOperationException(result.Message);
            return payload;
        }
        private static void WriteTravelPayload(LwsSaveSnapshot snapshot, TruckTaxiVibinSavePayload payload) =>
            snapshot.participants.Find(p => p.participantId == TruckTaxiVibinSaveParticipant.Id).payloadJson = JsonUtility.ToJson(payload);

        public Vector3 WorldTransferPreparationPoint(LwsSaveSnapshot snapshot, bool last)
        {
            var visit = Array.Find(ReadTravelPayload(snapshot).worldVisits ?? Array.Empty<TruckTaxiWorldVisit>(), v => v.scene == gameObject.scene.name);
            if (last && visit != null) return origin.GlobalToLocal(new LwsWorldPositionD(visit.position.x, visit.position.y, visit.position.z));
            var exit = FindObjectsByType<TruckTaxiWorldExit>(FindObjectsSortMode.None).SingleOrDefault(e => e.gameObject.scene == gameObject.scene);
            return exit != null && exit.arrival != null ? exit.arrival.position : host.playerSpawn.position;
        }
        public LwsSaveOperationResult ApplyWorldTransfer(LwsSaveSnapshot snapshot, bool returnToLastPosition, bool rollback)
        {
            if (!Initialized || IsBusy || snapshot == null || !TruckTaxiWorldTravel.IsBusy)
                return LwsSaveOperationResult.Failure("World transfer was not prepared.");
            string scene = SceneManager.GetActiveScene().name;
            var payload = ReadTravelPayload(snapshot);
            var visit = Array.Find(payload.worldVisits ?? Array.Empty<TruckTaxiWorldVisit>(), v => v.scene == scene);
            var exit = FindObjectsByType<TruckTaxiWorldExit>(FindObjectsSortMode.None).SingleOrDefault(e => e.gameObject.scene == gameObject.scene);
            if (exit == null || exit.arrival == null) return LwsSaveOperationResult.Failure("This region has no validated regional entry road.");
            Vector3 point = (returnToLastPosition || rollback) && visit != null ? origin.GlobalToLocal(new LwsWorldPositionD(visit.position.x, visit.position.y, visit.position.z)) : exit.arrival.position;
            Quaternion facing = (returnToLastPosition || rollback) && visit != null ? visit.rotation : exit.arrival.rotation;
            Physics.SyncTransforms();
            if (!regional.IsPositionAvailable(point) || !TruckTaxiRecoverySpace.TryGroundedPose(body, point, facing, .35f, body, null, out preparedPosition))
            {
                point = exit.arrival.position; facing = exit.arrival.rotation;
                if (!TruckTaxiRecoverySpace.TryGroundedPose(body, point, facing, .35f, body, null, out preparedPosition))
                    return LwsSaveOperationResult.Failure("No clear supported entry space. Original save was not changed.");
            }
            var resume = new LwsWorldResumeContextPayload { stableWorldId = TruckTaxiWorldCatalog.WorldId(scene), authoredSceneName = scene,
                hasGlobalPosition = true, localX = preparedPosition.x, localY = preparedPosition.y, localZ = preparedPosition.z,
                rotationX = facing.x, rotationY = facing.y, rotationZ = facing.z, rotationW = facing.w, headingDegrees = facing.eulerAngles.y };
            var global = origin.LocalToGlobal(preparedPosition);
            resume.savedGlobalX = global.x; resume.savedGlobalY = global.y; resume.savedGlobalZ = global.z;
            var position = new LwsGlobalPositionSavePayload { globalX = global.x, globalY = global.y, globalZ = global.z,
                localX = preparedPosition.x, localY = preparedPosition.y, localZ = preparedPosition.z,
                rotationX = facing.x, rotationY = facing.y, rotationZ = facing.z, rotationW = facing.w };
            var truckState = snapshot.participants.Find(p => p.participantId == "lws.vehicle.player-truck");
            var truck = JsonUtility.FromJson<LwsPlayerTruckSavePayload>(truckState.payloadJson);
            truck.globalX = global.x; truck.globalY = global.y; truck.globalZ = global.z; truck.hasGlobalPosition = true;
            truck.state.pose = new LwsSerializablePose { position = preparedPosition, rotation = facing };
            truck.state.linearVelocity = truck.state.angularVelocity = Vector3.zero;
            truckState.payloadJson = JsonUtility.ToJson(truck);
            snapshot.sceneName = scene;
            snapshot.participants.Find(p => p.participantId == LwsSaveSchema.WorldResumeContextParticipantId).payloadJson = JsonUtility.ToJson(resume);
            snapshot.participants.Find(p => p.participantId == "lws.world.global-position").payloadJson = JsonUtility.ToJson(position);
            if (!rollback) snapshot.participants.Find(p => p.participantId == "lws.navigation.destination-intent").payloadJson = JsonUtility.ToJson(new LwsNavigationSavePayload());
            // Global clock/driver/truck state travels. World-specific weather, snow and events do not.
            foreach (string id in new[] { "lws.weather.semantic", "lws.road-condition.semantic" })
            {
                var record = id == "lws.weather.semantic" ? visit?.weather : visit?.roadCondition;
                var fresh = record ?? SaveService.Participants.Single(p => p.ParticipantId == id).CaptureState();
                var target = snapshot.participants.Find(p => p.participantId == id);
                target.payloadJson = fresh.payloadJson;
                if (record == null && id == "lws.road-condition.semantic")
                {
                    var road = JsonUtility.FromJson<LwsRoadConditionSavePayload>(target.payloadJson);
                    road.mode = LwsRoadConditionOverrideMode.AutoFromWeather;
                    road.snapshot = LwsRoadConditionSnapshot.CreateDry("offroad.default", "", "offroad.default",
                        LwsRoadSurfaceType.AsphaltInterstate, host.Environment.Weather.CurrentSnapshot.ambientTemperatureC, LwsRoadConditionProfile.Default());
                    target.payloadJson = JsonUtility.ToJson(road);
                }
            }
            payload.regionId = regional.ResolveRegion(preparedPosition);
            payload.environment = visit?.environment ?? host.Environment.Capture();
            payload.extremes = visit?.extremes;
            payload.hasExtremeState = payload.extremes != null;
            payload.session.workArea = visit?.workArea ?? host.Session.CapturePersistentState().workArea;
            WriteTravelPayload(snapshot, payload);
            if (!LwsWorldResumeCatalog.TryResolve(resume.stableWorldId, scene, out var binding))
                return LwsSaveOperationResult.Failure("Unknown target world identity.");
            prepared = new LwsLoadApplicationContext { ProfileId = snapshot.profileId, Snapshot = snapshot, ResumeContext = resume, WorldBinding = binding };
            IsBusy = permitApply = true; applied = false;
            try
            {
                var valid = ValidateSnapshot(snapshot);
                if (!valid.Succeeded) return valid;
                var result = SaveService.LoadCoordinator.ApplyPreparedWorldTransfer(prepared);
                if (!result.Succeeded || !applied || !restoreResult.Succeeded) return result.Succeeded ? restoreResult : result;
                RunProfileId = snapshot.profileId; profileReady = true; loadFailed = false;
                if (!rollback && visit != null)
                {
                    TruckTaxiWeatherCondition? observed = visit.weather == null ? null :
                        (TruckTaxiWeatherCondition?)TruckTaxiCalendarWeatherService.ConditionFor(JsonUtility.FromJson<LwsWeatherSavePayload>(visit.weather.payloadJson).weatherPresetId);
                    TruckTaxiWorldVisit.AdvanceSnow(payload.environment, CalendarSeconds(host.Environment.Clock.CurrentSnapshot) - visit.gameSeconds,
                        visit.clockRate, host.Environment.settings, observed);
                    host.Environment.Snow.Restore(payload.environment.snow);
                }
                if (!host.Environment.CalendarWeather.Restore(payload.environment.forecast, false))
                    return LwsSaveOperationResult.Failure("Regional forecast could not be restored.");
                if (LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsRoadConditionService roads) && roads is LwsRoadConditionCoordinator roadAuthority)
                {
                    var roadState = JsonUtility.FromJson<LwsRoadConditionSavePayload>(snapshot.participants.Find(p => p.participantId == "lws.road-condition.semantic").payloadJson);
                    roadAuthority.RestoreWorldAccumulation(roadState.snapshot);
                }
                host.WorldLife.ReturnToTruck(); host.ComponentDamage?.ReapplyPhysicalState();
                host.Session.DiscardTeleportDistance(); host.GPS.RequestPresentationRefresh();
                RememberCounters(); boundaryPending = false;
                return LwsSaveOperationResult.Success("Career restored in " + TruckTaxiWorldCatalog.Label(scene));
            }
            finally { IsBusy = permitApply = false; prepared = null; }
        }
    }
}
