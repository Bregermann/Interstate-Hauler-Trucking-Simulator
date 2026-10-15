using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public sealed class LwsDeliveryService : ILwsDeliveryService
    {
        private readonly Dictionary<string, LwsDeliveryArea> _areasByDestinationId = new Dictionary<string, LwsDeliveryArea>(StringComparer.Ordinal);
        private readonly List<LwsDeliveryArea> _registeredAreas = new List<LwsDeliveryArea>();
        private ILwsActiveJobService _activeJobs;
        private ILwsTrailerRegistryService _trailers;
        private ILwsPlayerVehicleService _playerVehicles;
        private ILwsGameplayStateService _gameplay;
        private ILwsNavigationService _navigation;
        private ILwsRoadGraphService _roads;
        private ILwsSaveService _save;
        private ILwsWorldOriginService _origin;
        private LwsDeliveryArea _activeArea;
        private LwsDeliverySnapshot _snapshot;
        private bool _playerInsideArea;
        private bool _completing;
        private float _validHoldSeconds;
        private float _deliveryStartedAt;

        public string ServiceId => "lws.delivery";
        public IReadOnlyList<LwsDeliveryArea> RegisteredDeliveryAreas => _registeredAreas;
        public LwsDeliveryArea ActiveDeliveryArea => _activeArea;
        public LwsDeliveryParkingZone ActiveParkingZone => _activeArea != null ? _activeArea.ParkingZone : null;
        public LwsDeliverySnapshot CurrentSnapshot => _snapshot;
        public string LastMessage { get; private set; } = "Delivery service not initialized.";
        public event Action<LwsDeliverySnapshot> DeliveryStateChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            context.Registry.TryGet(out _activeJobs);
            context.Registry.TryGet(out _trailers);
            context.Registry.TryGet(out _playerVehicles);
            context.Registry.TryGet(out _gameplay);
            context.Registry.TryGet(out _navigation);
            context.Registry.TryGet(out _roads);
            context.Registry.TryGet(out _save);
            context.Registry.TryGet(out _origin);
            if (_activeJobs != null) _activeJobs.ActiveJobChanged += OnActiveJobChanged;
            if (_playerVehicles != null)
            {
                _playerVehicles.TrailerAttached += OnTrailerAttachmentChanged;
                _playerVehicles.TrailerDetached += OnTrailerAttachmentChanged;
                _playerVehicles.PlayerTruckReady += OnPlayerTruckReady;
            }
            if (_save != null) _save.LoadCompleted += OnLoadCompleted;
            if (_origin != null) _origin.OriginShiftStarting += OnOriginShiftStarting;
            LastMessage = "LWS delivery service initialized.";
            SynchronizeFromActiveJob(LastMessage);
            return LwsServiceResult.Success(LastMessage);
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            if (_activeJobs != null) _activeJobs.ActiveJobChanged -= OnActiveJobChanged;
            if (_playerVehicles != null)
            {
                _playerVehicles.TrailerAttached -= OnTrailerAttachmentChanged;
                _playerVehicles.TrailerDetached -= OnTrailerAttachmentChanged;
                _playerVehicles.PlayerTruckReady -= OnPlayerTruckReady;
            }
            if (_save != null) _save.LoadCompleted -= OnLoadCompleted;
            if (_origin != null) _origin.OriginShiftStarting -= OnOriginShiftStarting;
            SetActiveArea(null, false);
            _areasByDestinationId.Clear();
            _registeredAreas.Clear();
            _activeJobs = null;
            _trailers = null;
            _playerVehicles = null;
            _gameplay = null;
            _navigation = null;
            _roads = null;
            _save = null;
            _origin = null;
            DeliveryStateChanged = null;
            return LwsServiceResult.Success("LWS delivery service shut down.");
        }

        public LwsServiceResult RegisterDeliveryArea(LwsDeliveryArea area)
        {
            if (area == null)
                return LwsServiceResult.Failure("Cannot register a null delivery area.");
            if (!area.Validate(out string validation))
                return LwsServiceResult.Failure(validation);
            string id = Normalize(area.DestinationId);
            if (_areasByDestinationId.TryGetValue(id, out LwsDeliveryArea existing) && existing != null && existing != area)
                return LwsServiceResult.Failure($"Duplicate delivery area for destination '{id}' on {existing.name} and {area.name}.");
            _areasByDestinationId[id] = area;
            if (!_registeredAreas.Contains(area)) _registeredAreas.Add(area);
            SynchronizeFromActiveJob($"Delivery area {id} registered.");
            return LwsServiceResult.Success($"Registered delivery area {id}.");
        }

        public LwsServiceResult UnregisterDeliveryArea(LwsDeliveryArea area)
        {
            if (area == null) return LwsServiceResult.Success("No delivery area to unregister.");
            _registeredAreas.Remove(area);
            string id = Normalize(area.DestinationId);
            if (_areasByDestinationId.TryGetValue(id, out LwsDeliveryArea existing) && existing == area)
                _areasByDestinationId.Remove(id);
            if (_activeArea == area)
            {
                SetActiveArea(null, false);
                Publish($"Active delivery area {id} unloaded; waiting for runtime destination registration.");
            }
            return LwsServiceResult.Success($"Unregistered delivery area {id}.");
        }

        public bool TryResolveDeliveryArea(string destinationId, out LwsDeliveryArea area)
        {
            PruneDestroyedAreas();
            return _areasByDestinationId.TryGetValue(Normalize(destinationId), out area) && area != null;
        }

        public LwsServiceResult TryEnterDeliveryContext(LwsDeliveryArea area, LwsPlayerTruck playerTruck, string reason)
        {
            if (area == null || playerTruck == null)
                return Fail("Delivery context requires a registered area and the canonical player truck.");
            if (_playerVehicles != null && _playerVehicles.ActiveTruck != null && _playerVehicles.ActiveTruck != playerTruck)
                return Fail("A non-canonical tractor entered the delivery area.");

            LwsActiveJob job = _activeJobs?.CurrentActiveJob;
            if (job == null || !job.IsValid) return Fail($"Entered {area.DestinationId}, but no active haul exists.");
            if (!LwsDeliveryValidation.DestinationMatches(job.destinationId, area.DestinationId))
                return Fail($"Wrong destination: active job requires {job.destinationId}, entered {area.DestinationId}.");
            if (job.status != LwsJobStatus.HaulActive && job.status != LwsJobStatus.Delivery)
                return Fail($"Active job status {job.status} cannot enter delivery context.");
            if (!job.trailerAssignment.hasAssignedTrailer) return Fail("Active job has no assigned trailer.");

            ILwsTrailerCoupling coupling = playerTruck.CouplingAdapter;
            string attachedId = coupling != null ? coupling.CurrentTrailerId : string.Empty;
            if (coupling == null || !coupling.IsTrailerAttached ||
                !LwsDeliveryValidation.AssignedTrailerMatches(job.trailerAssignment.stableTrailerId, attachedId))
                return Fail($"Delivery requires coupled assigned trailer {job.trailerAssignment.stableTrailerId}; attached trailer is {DisplayId(attachedId)}.");
            if (_trailers == null || !_trailers.TryResolveTrailer(job.trailerAssignment.stableTrailerId, out _))
                return Fail($"Assigned physical trailer {job.trailerAssignment.stableTrailerId} is not registered.");

            SetActiveArea(area, true);
            if (job.status == LwsJobStatus.HaulActive)
            {
                LwsServiceResult entered = _activeJobs.EnterDelivery(area.DestinationId, attachedId,
                    string.IsNullOrWhiteSpace(reason) ? "Matching delivery area entered." : reason);
                if (!entered.Succeeded) return Fail(entered.Message);
                _deliveryStartedAt = Time.realtimeSinceStartup;
                FocusDeliveryTarget("Delivery context entered; focus parking target.");
                _save?.RequestAutosave("delivery.entered");
            }
            else
            {
                FocusDeliveryTarget("Delivery context re-entered; focus parking target.");
            }
            Publish($"Delivery active at {area.DestinationDisplayName}; park assigned trailer {attachedId}.");
            return LwsServiceResult.Success(LastMessage);
        }

        public void NotifyPlayerExitedDeliveryArea(LwsDeliveryArea area, LwsPlayerTruck playerTruck)
        {
            if (area == null || area != _activeArea) return;
            _playerInsideArea = false;
            ResetValidationHold($"Player exited delivery area {area.DestinationId}; delivery remains active.");
        }

        public LwsServiceResult EvaluateParking(LwsDeliveryParkingZone zone, float deltaTime)
        {
            if (zone == null || _activeArea == null || _activeArea.ParkingZone != zone)
            {
                zone?.SetRuntimeVisualState(false, false);
                return LwsServiceResult.Success("Parking zone is not the active delivery target.");
            }

            LwsActiveJob job = _activeJobs?.CurrentActiveJob;
            if (job == null || job.status != LwsJobStatus.Delivery)
            {
                zone.SetRuntimeVisualState(false, false);
                _validHoldSeconds = 0f;
                return LwsServiceResult.Success("Parking validation is dormant outside Delivery.");
            }

            bool correctDestination = LwsDeliveryValidation.DestinationMatches(job.destinationId, _activeArea.DestinationId);
            LwsTrailerGameplayIdentity trailer = null;
            bool trailerResolved = job.trailerAssignment.hasAssignedTrailer && _trailers != null &&
                                   _trailers.TryResolveTrailer(job.trailerAssignment.stableTrailerId, out trailer);
            LwsPlayerTruck truck = _playerVehicles != null ? _playerVehicles.ActiveTruck : null;
            ILwsTrailerCoupling coupling = truck != null ? truck.CouplingAdapter : null;
            bool physicallyCoupled = coupling != null && coupling.IsTrailerAttached;
            string attachedId = coupling != null ? coupling.CurrentTrailerId : string.Empty;
            bool correctTrailer = physicallyCoupled &&
                                  LwsDeliveryValidation.AssignedTrailerMatches(job.trailerAssignment.stableTrailerId, attachedId);
            LwsDeliveryParkingEvaluation evaluation = trailerResolved ? zone.EvaluateTrailer(trailer) : default;
            bool valid = correctDestination && _playerInsideArea && trailerResolved && correctTrailer &&
                         evaluation.trailerInsideZone && evaluation.headingValid && evaluation.stopped;
            _validHoldSeconds = LwsDeliveryValidation.AdvanceValidHold(_validHoldSeconds, valid, deltaTime);
            bool ready = valid && _validHoldSeconds >= zone.MinimumValidHoldSeconds;
            string message = ValidationMessage(correctDestination, trailerResolved, physicallyCoupled, correctTrailer, evaluation);

            _snapshot = BuildBaseSnapshot(message);
            _snapshot.deliveryAreaActive = true;
            _snapshot.playerInsideDeliveryArea = _playerInsideArea;
            _snapshot.attachedTrailerId = attachedId ?? string.Empty;
            _snapshot.correctDestination = correctDestination;
            _snapshot.correctTrailer = correctTrailer;
            _snapshot.physicallyCoupled = physicallyCoupled;
            _snapshot.trailerResolved = trailerResolved;
            _snapshot.trailerInsideZone = evaluation.trailerInsideZone;
            _snapshot.containmentRatio = evaluation.containmentRatio;
            _snapshot.headingErrorDegrees = evaluation.headingErrorDegrees;
            _snapshot.headingValid = evaluation.headingValid;
            _snapshot.trailerLinearSpeedMetersPerSecond = evaluation.trailerLinearSpeedMetersPerSecond;
            _snapshot.trailerAngularSpeedDegreesPerSecond = evaluation.trailerAngularSpeedDegreesPerSecond;
            _snapshot.stopped = evaluation.stopped;
            _snapshot.lateralOffsetMeters = evaluation.lateralOffsetMeters;
            _snapshot.longitudinalOffsetMeters = evaluation.longitudinalOffsetMeters;
            _snapshot.validHoldSeconds = _validHoldSeconds;
            _snapshot.requiredHoldSeconds = zone.MinimumValidHoldSeconds;
            _snapshot.deliveryReady = ready;
            LastMessage = message;
            zone.SetRuntimeVisualState(true, ready);
            DeliveryStateChanged?.Invoke(_snapshot);
            return ready && !_completing ? CompleteCurrentDelivery() : LwsServiceResult.Success(message);
        }

        public LwsServiceResult FocusDeliveryTarget(string reason)
        {
            LwsDeliveryParkingZone target = ActiveParkingZone;
            LwsPlayerTruck truck = _playerVehicles != null ? _playerVehicles.ActiveTruck : null;
            if (target == null) return Fail("Active delivery parking target is not resolved.");
            if (_navigation == null || truck == null)
                return Fail("Delivery target resolved, but navigation or the player truck is unavailable.");

            _navigation.ClearRoute();
            LwsRoadGraph graph = _roads != null ? _roads.ActiveGraph : null;
            if (graph == null)
            {
                Publish("Road route cleared; local delivery parking marker is active.");
                return LwsServiceResult.Success(LastMessage);
            }

            LwsActiveJob job = _activeJobs?.CurrentActiveJob;
            string destinationId = job != null ? job.destinationId : _activeArea.DestinationId;
            LwsRouteResult route = _navigation.RequestRoute(new LwsRouteRequest
            {
                requestId = $"delivery.parking.{destinationId}",
                destinationId = $"delivery.parking.{destinationId}",
                useOriginWorldPosition = true,
                originWorldPosition = truck.transform.position,
                useDestinationWorldPosition = true,
                destinationWorldPosition = target.transform.position,
                truckRouteRequired = true
            }, graph);
            Publish(route.succeeded
                ? $"Delivery parking objective focused for {destinationId}."
                : $"Road route cleared; local delivery marker active because parking route could not resolve: {route.message}");
            return LwsServiceResult.Success(LastMessage);
        }

        public LwsServiceResult ReevaluateDelivery(string reason)
        {
            if (ActiveParkingZone == null) return Fail("No active delivery parking target to re-evaluate.");
            LastMessage = string.IsNullOrWhiteSpace(reason) ? "Delivery re-evaluation requested." : reason;
            return EvaluateParking(ActiveParkingZone, 0f);
        }

        public LwsServiceResult ForceEnterDeliveryContextForDevelopment(string reason)
        {
            LwsActiveJob job = _activeJobs?.CurrentActiveJob;
            if (job == null) return Fail("No active job is available for development delivery entry.");
            if (!TryResolveDeliveryArea(job.destinationId, out LwsDeliveryArea area))
                return Fail($"No runtime delivery area is registered for {job.destinationId}.");
            if (job.status == LwsJobStatus.Delivery)
            {
                SetActiveArea(area, true);
                FocusDeliveryTarget(reason);
                Publish("Development UI restored the existing delivery context.");
                return LwsServiceResult.Success(LastMessage);
            }
            return TryEnterDeliveryContext(area, _playerVehicles != null ? _playerVehicles.ActiveTruck : null,
                string.IsNullOrWhiteSpace(reason) ? "Development UI requested delivery context." : reason);
        }

        public void ResetValidationHold(string reason)
        {
            _validHoldSeconds = 0f;
            if (_activeArea != null && _activeArea.ParkingZone != null)
                _activeArea.ParkingZone.SetRuntimeVisualState(true, false);
            Publish(string.IsNullOrWhiteSpace(reason) ? "Delivery validation hold reset." : reason);
        }

        private LwsServiceResult CompleteCurrentDelivery()
        {
            LwsActiveJob job = _activeJobs?.CurrentActiveJob;
            if (job == null || job.status != LwsJobStatus.Delivery)
                return Fail("Delivery completion requires an active job in Delivery status.");

            _completing = true;
            try
            {
                var completion = new LwsDeliveryCompletionSnapshot
                {
                    completedSuccessfully = true,
                    activeJobId = job.stableActiveJobId,
                    destinationId = job.destinationId,
                    assignedTrailerId = job.trailerAssignment.stableTrailerId,
                    completedUtcTicks = DateTime.UtcNow.Ticks,
                    containmentRatio = _snapshot.containmentRatio,
                    headingErrorDegrees = _snapshot.headingErrorDegrees,
                    lateralOffsetMeters = _snapshot.lateralOffsetMeters,
                    longitudinalOffsetMeters = _snapshot.longitudinalOffsetMeters,
                    validHoldSeconds = _validHoldSeconds,
                    deliveryContextDurationSeconds = _deliveryStartedAt > 0f ? Mathf.Max(0f, Time.realtimeSinceStartup - _deliveryStartedAt) : 0f,
                    validationMode = "assigned-trailer-coupled-auto-hold"
                };
                LwsServiceResult result = _activeJobs.CompleteDelivery(completion, "Assigned trailer parked successfully.");
                if (!result.Succeeded) return Fail(result.Message);
                _navigation?.ClearRoute();
                _save?.RequestAutosave("delivery.completed");
                LastMessage = $"Delivery complete for active job {job.stableActiveJobId}; awaiting results processing.";
                _snapshot = BuildBaseSnapshot(LastMessage);
                _snapshot.deliveryReady = true;
                _snapshot.correctDestination = true;
                _snapshot.correctTrailer = true;
                _snapshot.physicallyCoupled = true;
                _snapshot.trailerResolved = true;
                _snapshot.trailerInsideZone = true;
                _snapshot.containmentRatio = completion.containmentRatio;
                _snapshot.headingErrorDegrees = completion.headingErrorDegrees;
                _snapshot.headingValid = true;
                _snapshot.stopped = true;
                _snapshot.lateralOffsetMeters = completion.lateralOffsetMeters;
                _snapshot.longitudinalOffsetMeters = completion.longitudinalOffsetMeters;
                _snapshot.validHoldSeconds = completion.validHoldSeconds;
                _snapshot.requiredHoldSeconds = ActiveParkingZone != null ? ActiveParkingZone.MinimumValidHoldSeconds : completion.validHoldSeconds;
                ActiveParkingZone?.SetRuntimeVisualState(true, true);
                DeliveryStateChanged?.Invoke(_snapshot);
                return LwsServiceResult.Success(LastMessage);
            }
            finally
            {
                _completing = false;
            }
        }

        private void SynchronizeFromActiveJob(string reason)
        {
            LwsActiveJob job = _activeJobs?.CurrentActiveJob;
            if (job == null || !job.IsValid)
            {
                SetActiveArea(null, false);
                Publish("No active delivery job.");
                return;
            }
            if (job.status != LwsJobStatus.Delivery && job.status != LwsJobStatus.Completed)
            {
                SetActiveArea(null, false);
                Publish(string.IsNullOrWhiteSpace(reason) ? $"Active job status is {job.status}." : reason);
                return;
            }
            if (!TryResolveDeliveryArea(job.destinationId, out LwsDeliveryArea area))
            {
                SetActiveArea(null, false);
                Publish($"Delivery runtime for destination {job.destinationId} is not loaded.");
                return;
            }

            SetActiveArea(area, job.status == LwsJobStatus.Delivery);
            if (job.status == LwsJobStatus.Delivery)
            {
                if (_deliveryStartedAt <= 0f) _deliveryStartedAt = Time.realtimeSinceStartup;
                FocusDeliveryTarget("Restored delivery status; focus parking target.");
                Publish($"Delivery context restored for {job.destinationId}.");
            }
            else
            {
                _navigation?.ClearRoute();
                Publish($"Completed delivery restored for results handoff: {job.destinationId}.");
                _snapshot.deliveryReady = job.deliveryCompletion.IsValid;
                area.ParkingZone?.SetRuntimeVisualState(true, job.deliveryCompletion.IsValid);
            }
        }

        private void SetActiveArea(LwsDeliveryArea area, bool playerInside)
        {
            if (_activeArea != null && _activeArea.ParkingZone != null && _activeArea != area)
                _activeArea.ParkingZone.SetRuntimeVisualState(false, false);
            _activeArea = area;
            _playerInsideArea = area != null && playerInside;
            _validHoldSeconds = 0f;
            if (ActiveParkingZone != null) ActiveParkingZone.SetRuntimeVisualState(true, false);
        }

        private LwsDeliverySnapshot BuildBaseSnapshot(string message)
        {
            LwsActiveJob job = _activeJobs?.CurrentActiveJob;
            LwsPlayerTruck truck = _playerVehicles != null ? _playerVehicles.ActiveTruck : null;
            ILwsTrailerCoupling coupling = truck != null ? truck.CouplingAdapter : null;
            string attachedId = coupling != null ? coupling.CurrentTrailerId : string.Empty;
            string assignedId = job != null && job.trailerAssignment.hasAssignedTrailer ? job.trailerAssignment.stableTrailerId : string.Empty;
            return new LwsDeliverySnapshot
            {
                hasActiveJob = job != null && job.IsValid,
                activeJobId = job != null ? job.stableActiveJobId : string.Empty,
                jobStatus = job != null ? job.status : LwsJobStatus.None,
                destinationId = job != null ? job.destinationId : string.Empty,
                currentDestinationId = _activeArea != null ? _activeArea.DestinationId : string.Empty,
                currentDestinationDisplayName = _activeArea != null ? _activeArea.DestinationDisplayName : string.Empty,
                deliveryAreaResolved = _activeArea != null,
                deliveryAreaActive = _activeArea != null && job != null &&
                    (job.status == LwsJobStatus.Delivery || job.status == LwsJobStatus.Completed),
                playerInsideDeliveryArea = _playerInsideArea,
                parkingTargetName = ActiveParkingZone != null ? ActiveParkingZone.name : string.Empty,
                parkingTargetResolved = ActiveParkingZone != null,
                assignedTrailerId = assignedId ?? string.Empty,
                attachedTrailerId = attachedId ?? string.Empty,
                correctDestination = job != null && _activeArea != null && LwsDeliveryValidation.DestinationMatches(job.destinationId, _activeArea.DestinationId),
                correctTrailer = LwsDeliveryValidation.AssignedTrailerMatches(assignedId, attachedId),
                physicallyCoupled = coupling != null && coupling.IsTrailerAttached,
                requiredHoldSeconds = ActiveParkingZone != null ? ActiveParkingZone.MinimumValidHoldSeconds : 0f,
                lastMessage = message ?? string.Empty
            };
        }

        private string ValidationMessage(bool correctDestination, bool trailerResolved, bool physicallyCoupled,
            bool correctTrailer, LwsDeliveryParkingEvaluation evaluation)
        {
            if (!correctDestination) return "Delivery rejected: destination does not match the active job.";
            if (!_playerInsideArea) return "Delivery waiting: enter the destination delivery area.";
            if (!trailerResolved) return "Delivery waiting: assigned trailer is not resolved.";
            if (!physicallyCoupled) return "Delivery waiting: assigned trailer must remain coupled.";
            if (!correctTrailer) return "Delivery rejected: the physically attached trailer is not the assigned trailer.";
            if (!evaluation.trailerInsideZone) return $"Parking: move assigned trailer fully into target ({evaluation.containmentRatio:P0}).";
            if (!evaluation.headingValid) return $"Parking: align trailer with target ({evaluation.headingErrorDegrees:0.0} deg error).";
            if (!evaluation.stopped) return $"Parking: stop and settle trailer ({evaluation.trailerLinearSpeedMetersPerSecond:0.00} m/s).";
            return $"Delivery valid: hold position {_validHoldSeconds:0.0}/{ActiveParkingZone.MinimumValidHoldSeconds:0.0}s.";
        }

        private LwsServiceResult Fail(string message)
        {
            Publish(message);
            return LwsServiceResult.Failure(message);
        }

        private void Publish(string message)
        {
            LastMessage = message ?? string.Empty;
            LwsDeliverySnapshot next = BuildBaseSnapshot(LastMessage);
            next.validHoldSeconds = _validHoldSeconds;
            LwsActiveJob job = _activeJobs?.CurrentActiveJob;
            next.deliveryReady = job != null &&
                                 job.status == LwsJobStatus.Completed &&
                                 job.deliveryCompletion.IsValid;
            _snapshot = next;
            DeliveryStateChanged?.Invoke(_snapshot);
        }

        private void OnActiveJobChanged(LwsActiveJob activeJob)
        {
            if (!_completing) SynchronizeFromActiveJob("Active job changed; delivery context synchronized.");
        }

        private void OnTrailerAttachmentChanged(LwsPlayerTruck truck, LwsTrailerAttachmentState state)
        {
            if (_activeJobs?.CurrentActiveJob?.status == LwsJobStatus.Delivery)
                ResetValidationHold(state.attached ? $"Trailer attachment changed to {DisplayId(state.trailerId)}; delivery hold reset." : "Trailer detached; delivery hold reset.");
        }

        private void OnPlayerTruckReady(LwsPlayerTruck truck) => SynchronizeFromActiveJob("Player truck ready; delivery context synchronized.");

        private void OnLoadCompleted(LwsSaveOperationResult result, LwsManualSaveSlotMetadata slot)
        {
            if (result.Succeeded) SynchronizeFromActiveJob("Pixel Crushers load completed; delivery context synchronized.");
        }

        private void OnOriginShiftStarting(LwsOriginShiftEvent shiftEvent)
        {
            if (_activeJobs?.CurrentActiveJob?.status == LwsJobStatus.Delivery)
                ResetValidationHold("Floating-origin shift started; delivery hold reset.");
        }

        private void PruneDestroyedAreas()
        {
            for (int i = _registeredAreas.Count - 1; i >= 0; i--)
                if (_registeredAreas[i] == null) _registeredAreas.RemoveAt(i);
            var missing = new List<string>();
            foreach (KeyValuePair<string, LwsDeliveryArea> pair in _areasByDestinationId)
                if (pair.Value == null) missing.Add(pair.Key);
            for (int i = 0; i < missing.Count; i++) _areasByDestinationId.Remove(missing[i]);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        private static string DisplayId(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value;
    }
}
