using System;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public sealed class LwsTrailerPickupService : ILwsTrailerPickupService
    {
        private ILwsTrailerRegistryService _trailerRegistry;
        private ILwsActiveJobService _activeJobService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private ILwsGameplayStateService _gameplayStateService;
        private ILwsNavigationService _navigationService;
        private ILwsRoadGraphService _roadGraphService;
        private ILwsJobCatalogService _jobCatalogService;
        private ILwsWorldOriginService _originService;
        private ILwsSaveService _saveService;
        private bool _handlingActiveJobChange;

        public string ServiceId => "lws.trailers.pickup";
        public string LastMessage { get; private set; } = "Trailer pickup service not initialized.";
        public LwsTrailerPickupSnapshot CurrentSnapshot => BuildSnapshot();
        public event Action<LwsTrailerPickupSnapshot> PickupStateChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            context.Registry.TryGet(out _trailerRegistry);
            context.Registry.TryGet(out _activeJobService);
            context.Registry.TryGet(out _playerVehicleService);
            context.Registry.TryGet(out _gameplayStateService);
            context.Registry.TryGet(out _navigationService);
            context.Registry.TryGet(out _roadGraphService);
            context.Registry.TryGet(out _jobCatalogService);
            context.Registry.TryGet(out _originService);
            context.Registry.TryGet(out _saveService);

            if (_activeJobService != null)
            {
                _activeJobService.ActiveJobAccepted += OnActiveJobAccepted;
                _activeJobService.ActiveJobChanged += OnActiveJobChanged;
            }

            if (_playerVehicleService != null)
            {
                _playerVehicleService.TrailerAttached += OnPlayerTrailerAttached;
                _playerVehicleService.PlayerTruckReady += OnPlayerTruckReady;
            }

            if (_trailerRegistry != null)
            {
                _trailerRegistry.TrailerRegistered += OnTrailerRegistered;
            }

            if (_saveService != null)
            {
                _saveService.LoadCompleted += OnLoadCompleted;
            }

            LastMessage = "Trailer pickup service initialized.";
            EnsureAssignmentForCurrentJob("Trailer pickup service initialized.");
            return LwsServiceResult.Success(LastMessage);
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            if (_activeJobService != null)
            {
                _activeJobService.ActiveJobAccepted -= OnActiveJobAccepted;
                _activeJobService.ActiveJobChanged -= OnActiveJobChanged;
            }

            if (_playerVehicleService != null)
            {
                _playerVehicleService.TrailerAttached -= OnPlayerTrailerAttached;
                _playerVehicleService.PlayerTruckReady -= OnPlayerTruckReady;
            }

            if (_trailerRegistry != null)
            {
                _trailerRegistry.TrailerRegistered -= OnTrailerRegistered;
            }

            if (_saveService != null)
            {
                _saveService.LoadCompleted -= OnLoadCompleted;
            }

            _trailerRegistry = null;
            _activeJobService = null;
            _playerVehicleService = null;
            _gameplayStateService = null;
            _navigationService = null;
            _roadGraphService = null;
            _jobCatalogService = null;
            _originService = null;
            _saveService = null;
            PickupStateChanged = null;
            return LwsServiceResult.Success("Trailer pickup service shut down.");
        }

        public LwsServiceResult EnsureAssignmentForCurrentJob(string reason)
        {
            if (_handlingActiveJobChange)
            {
                return LwsServiceResult.Success("Trailer assignment is already being resolved.");
            }

            LwsActiveJob activeJob = _activeJobService?.CurrentActiveJob;
            if (activeJob == null || !activeJob.IsValid)
            {
                LastMessage = "No active job requires a trailer.";
                PublishChanged();
                return LwsServiceResult.Success(LastMessage);
            }

            if (activeJob.status == LwsJobStatus.HaulActive)
            {
                LastMessage = "Active haul already has trailer pickup complete.";
                ActivateDestinationObjective(reason);
                PublishChanged();
                return LwsServiceResult.Success(LastMessage);
            }

            if (activeJob.status != LwsJobStatus.AwaitingTrailerPickup)
            {
                LastMessage = $"Active job status {activeJob.status} does not require trailer assignment.";
                PublishChanged();
                return LwsServiceResult.Success(LastMessage);
            }

            if (activeJob.trailerAssignment.hasAssignedTrailer)
            {
                return ResolveExistingAssignment(activeJob, reason);
            }

            LwsTrailerGameplayIdentity assignedTrailer = FindOrSpawnAssignableTrailer(activeJob, string.Empty);
            if (assignedTrailer == null)
            {
                LastMessage = $"No assignable trailer of type {activeJob.requiredTrailerTypeId} is available for active job {activeJob.stableActiveJobId}.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            return AssignTrailer(activeJob, assignedTrailer, reason);
        }

        public LwsServiceResult FocusPickupObjective(string reason)
        {
            LwsActiveJob activeJob = _activeJobService?.CurrentActiveJob;
            if (activeJob == null || !activeJob.trailerAssignment.hasAssignedTrailer)
            {
                LastMessage = "Cannot focus pickup objective without an assigned trailer.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            if (_trailerRegistry == null || !_trailerRegistry.TryResolveTrailer(activeJob.trailerAssignment.stableTrailerId, out LwsTrailerGameplayIdentity trailer))
            {
                LastMessage = $"Assigned trailer {activeJob.trailerAssignment.stableTrailerId} is not currently resolved.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            if (!TryGetPlayerRouteOrigin(out Vector3 playerPosition))
            {
                LastMessage = "Cannot focus pickup objective because the active player truck is missing.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            LwsRoadGraph graph = _roadGraphService?.ActiveGraph;
            if (_navigationService == null || graph == null)
            {
                LastMessage = "Assigned trailer resolved; navigation route is waiting for road graph.";
                PublishChanged();
                return LwsServiceResult.Success(LastMessage);
            }

            LwsRouteResult route = _navigationService.RequestRoute(new LwsRouteRequest
            {
                requestId = $"pickup.{activeJob.trailerAssignment.stableTrailerId}",
                destinationId = $"pickup.{activeJob.trailerAssignment.stableTrailerId}",
                useOriginWorldPosition = true,
                originWorldPosition = playerPosition,
                useDestinationWorldPosition = true,
                destinationWorldPosition = trailer.transform.position,
                truckRouteRequired = true
            }, graph);

            LastMessage = route.succeeded
                ? $"Pickup objective set to trailer {activeJob.trailerAssignment.stableTrailerId}."
                : $"Pickup objective could not route to trailer {activeJob.trailerAssignment.stableTrailerId}: {route.message}";
            PublishChanged();
            return route.succeeded ? LwsServiceResult.Success(LastMessage) : LwsServiceResult.Failure(LastMessage);
        }

        public LwsServiceResult ActivateDestinationObjective(string reason)
        {
            LwsActiveJob activeJob = _activeJobService?.CurrentActiveJob;
            if (activeJob == null || !activeJob.IsValid)
            {
                LastMessage = "Cannot activate destination objective without an active job.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            if (_jobCatalogService == null || !_jobCatalogService.TryResolveDestination(activeJob.destinationId, out LwsDestinationDefinition destination) || destination == null)
            {
                LastMessage = $"Active job destination {activeJob.destinationId} could not be resolved.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            if (!destination.HasSemanticGlobalPosition)
            {
                LastMessage = $"Destination {activeJob.destinationId} has no semantic global position for navigation.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            if (!TryGetPlayerRouteOrigin(out Vector3 playerPosition))
            {
                LastMessage = "Cannot activate destination objective because the active player truck is missing.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            Vector3 destinationPosition = _originService != null
                ? _originService.GlobalToLocal(destination.SemanticGlobalPosition)
                : destination.SemanticGlobalPosition.ToVector3();
            LwsRoadGraph graph = _roadGraphService?.ActiveGraph;
            if (_navigationService == null || graph == null)
            {
                LastMessage = "Destination resolved; navigation route is waiting for road graph.";
                PublishChanged();
                return LwsServiceResult.Success(LastMessage);
            }

            LwsRouteResult route = _navigationService.RequestRoute(new LwsRouteRequest
            {
                requestId = $"job.destination.{activeJob.stableActiveJobId}",
                destinationId = activeJob.destinationId,
                useOriginWorldPosition = true,
                originWorldPosition = playerPosition,
                useDestinationWorldPosition = true,
                destinationWorldPosition = destinationPosition,
                truckRouteRequired = true
            }, graph);

            LastMessage = route.succeeded
                ? $"Destination objective set to {activeJob.destinationDisplayName}."
                : $"Destination objective could not route to {activeJob.destinationId}: {route.message}";
            PublishChanged();
            return route.succeeded ? LwsServiceResult.Success(LastMessage) : LwsServiceResult.Failure(LastMessage);
        }

        public LwsServiceResult ValidateAttachedTrailerForPickup(LwsTrailerAttachmentState attachmentState, string reason)
        {
            if (!attachmentState.attached)
            {
                LastMessage = "Trailer detached.";
                PublishChanged();
                return LwsServiceResult.Success(LastMessage);
            }

            LwsActiveJob activeJob = _activeJobService?.CurrentActiveJob;
            if (activeJob == null || !activeJob.IsValid || activeJob.status != LwsJobStatus.AwaitingTrailerPickup)
            {
                LastMessage = $"Trailer {attachmentState.trailerId} attached; no active trailer pickup is waiting.";
                PublishChanged();
                return LwsServiceResult.Success(LastMessage);
            }

            LwsServiceResult assignmentResult = EnsureAssignmentForCurrentJob(reason);
            if (!assignmentResult.Succeeded)
            {
                return assignmentResult;
            }

            activeJob = _activeJobService.CurrentActiveJob;
            string assignedTrailerId = activeJob.trailerAssignment.stableTrailerId;
            if (!string.Equals(Normalize(attachmentState.trailerId), Normalize(assignedTrailerId), StringComparison.Ordinal))
            {
                LastMessage = $"Wrong trailer attached: {attachmentState.trailerId}. Required: {assignedTrailerId}.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            LwsServiceResult completed = _activeJobService.CompleteTrailerPickup(attachmentState.trailerId, reason);
            if (!completed.Succeeded)
            {
                LastMessage = completed.Message;
                PublishChanged();
                return completed;
            }

            if (_trailerRegistry != null && _trailerRegistry.TryResolveTrailer(assignedTrailerId, out LwsTrailerGameplayIdentity trailer))
            {
                trailer.MarkAssignedToJob(activeJob.stableActiveJobId);
            }

            ActivateDestinationObjective("Correct trailer coupled; activate haul destination.");
            _saveService?.RequestAutosave("trailer.pickup.complete");
            _gameplayStateService?.TryTransitionTo(LwsGameplayState.HaulActive, "Correct assigned trailer coupled.");
            LastMessage = $"Trailer pickup complete: {assignedTrailerId}.";
            PublishChanged();
            return LwsServiceResult.Success(LastMessage);
        }

        private LwsServiceResult ResolveExistingAssignment(LwsActiveJob activeJob, string reason)
        {
            string assignedTrailerId = activeJob.trailerAssignment.stableTrailerId;
            if (_trailerRegistry != null && _trailerRegistry.TryResolveTrailer(assignedTrailerId, out LwsTrailerGameplayIdentity trailer))
            {
                if (!trailer.IsCompatibleWith(activeJob.requiredTrailerTypeId))
                {
                    LastMessage = $"Assigned trailer {assignedTrailerId} type {trailer.TrailerTypeId} does not match required type {activeJob.requiredTrailerTypeId}.";
                    PublishChanged();
                    return LwsServiceResult.Failure(LastMessage);
                }

                trailer.MarkAssignedToJob(activeJob.stableActiveJobId);
                FocusPickupObjective(reason);
                LastMessage = $"Resolved assigned trailer {assignedTrailerId}.";
                PublishChanged();
                return LwsServiceResult.Success(LastMessage);
            }

            LwsTrailerGameplayIdentity respawned = FindOrSpawnAssignableTrailer(activeJob, assignedTrailerId);
            if (respawned == null)
            {
                LastMessage = $"Saved assigned trailer {assignedTrailerId} could not be resolved or respawned.";
                PublishChanged();
                return LwsServiceResult.Failure(LastMessage);
            }

            return AssignTrailer(activeJob, respawned, reason);
        }

        private LwsServiceResult AssignTrailer(LwsActiveJob activeJob, LwsTrailerGameplayIdentity trailer, string reason)
        {
            if (activeJob == null || trailer == null)
            {
                return LwsServiceResult.Failure("Cannot assign a missing trailer to a missing job.");
            }

            trailer.MarkAssignedToJob(activeJob.stableActiveJobId);
            LwsWorldPositionD pickupGlobal = _originService != null
                ? _originService.LocalToGlobal(trailer.transform.position)
                : new LwsWorldPositionD(trailer.transform.position.x, trailer.transform.position.y, trailer.transform.position.z);
            LwsTrailerAssignment assignment = LwsTrailerAssignment.Create(
                trailer.StableTrailerId,
                trailer.TrailerTypeId,
                trailer.DisplayName,
                trailer.OriginDepotId,
                true,
                pickupGlobal);

            _handlingActiveJobChange = true;
            try
            {
                LwsServiceResult result = _activeJobService.AssignTrailerToActiveJob(assignment, reason);
                if (!result.Succeeded)
                {
                    LastMessage = result.Message;
                    PublishChanged();
                    return result;
                }
            }
            finally
            {
                _handlingActiveJobChange = false;
            }

            FocusPickupObjective(reason);
            LastMessage = $"Assigned trailer {trailer.StableTrailerId} to active job {activeJob.stableActiveJobId}.";
            PublishChanged();
            return LwsServiceResult.Success(LastMessage);
        }

        private LwsTrailerGameplayIdentity FindOrSpawnAssignableTrailer(LwsActiveJob activeJob, string forcedStableTrailerId)
        {
            if (!string.IsNullOrWhiteSpace(forcedStableTrailerId))
            {
                if (_trailerRegistry != null && _trailerRegistry.TryResolveTrailer(forcedStableTrailerId, out LwsTrailerGameplayIdentity assigned))
                {
                    return assigned;
                }

                return SpawnTrailerForJob(activeJob, forcedStableTrailerId);
            }

            if (_trailerRegistry != null)
            {
                LwsTrailerGameplayIdentity candidate = _trailerRegistry
                    .FindAssignableTrailers(activeJob.requiredTrailerTypeId, activeJob.originDepotId, activeJob.stableActiveJobId)
                    .FirstOrDefault();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return SpawnTrailerForJob(activeJob, string.Empty);
        }

        private LwsTrailerGameplayIdentity SpawnTrailerForJob(LwsActiveJob activeJob, string forcedStableTrailerId)
        {
            LwsTrailerSpawnPoint spawnPoint = UnityEngine.Object.FindObjectsByType<LwsTrailerSpawnPoint>()
                .Where(s => s != null && s.CanSpawnFor(activeJob.requiredTrailerTypeId, activeJob.originDepotId))
                .OrderByDescending(s => string.Equals(Normalize(s.OriginDepotId), Normalize(activeJob.originDepotId), StringComparison.Ordinal))
                .ThenBy(s => s.StableSpawnPointId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (spawnPoint == null)
            {
                return null;
            }

            string stableTrailerId = !string.IsNullOrWhiteSpace(forcedStableTrailerId)
                ? forcedStableTrailerId
                : BuildAssignedTrailerId(activeJob);
            return spawnPoint.SpawnTrailer(stableTrailerId, activeJob.stableActiveJobId);
        }

        private LwsTrailerPickupSnapshot BuildSnapshot()
        {
            LwsActiveJob activeJob = _activeJobService?.CurrentActiveJob;
            LwsTrailerAssignment assignment = activeJob != null ? activeJob.trailerAssignment : default;
            LwsTrailerGameplayIdentity trailer = null;
            bool resolved = assignment.hasAssignedTrailer &&
                            _trailerRegistry != null &&
                            _trailerRegistry.TryResolveTrailer(assignment.stableTrailerId, out trailer);
            string attachedTrailerId = _playerVehicleService != null && _playerVehicleService.ActiveTruck != null && _playerVehicleService.ActiveTruck.CouplingAdapter != null
                ? _playerVehicleService.ActiveTruck.CouplingAdapter.CurrentTrailerId
                : string.Empty;
            bool correctAttached = assignment.hasAssignedTrailer && string.Equals(Normalize(attachedTrailerId), Normalize(assignment.stableTrailerId), StringComparison.Ordinal);
            float distance = -1f;
            if (resolved && _playerVehicleService != null && _playerVehicleService.ActiveTruck != null)
            {
                distance = Vector3.Distance(_playerVehicleService.ActiveTruck.transform.position, trailer.transform.position);
            }

            return new LwsTrailerPickupSnapshot
            {
                hasActiveJob = activeJob != null && activeJob.IsValid,
                activeJobId = activeJob != null ? activeJob.stableActiveJobId : string.Empty,
                jobStatus = activeJob != null ? activeJob.status : LwsJobStatus.None,
                requiredTrailerTypeId = activeJob != null ? activeJob.requiredTrailerTypeId : string.Empty,
                hasAssignedTrailer = assignment.hasAssignedTrailer,
                assignedTrailerId = assignment.stableTrailerId ?? string.Empty,
                assignedTrailerDisplayName = assignment.trailerDisplayName ?? string.Empty,
                assignedTrailerResolved = resolved,
                attachedTrailerId = attachedTrailerId ?? string.Empty,
                correctTrailerAttached = correctAttached,
                pickupObjectiveActive = activeJob != null && activeJob.status == LwsJobStatus.AwaitingTrailerPickup && assignment.hasAssignedTrailer,
                pickupDistanceMeters = distance,
                lastMessage = LastMessage
            };
        }

        private void OnActiveJobAccepted(LwsActiveJob activeJob)
        {
            EnsureAssignmentForCurrentJob("Active job accepted; assign required trailer.");
        }

        private void OnActiveJobChanged(LwsActiveJob activeJob)
        {
            if (_handlingActiveJobChange)
            {
                PublishChanged();
                return;
            }

            if (activeJob != null && activeJob.status == LwsJobStatus.AwaitingTrailerPickup)
            {
                EnsureAssignmentForCurrentJob("Active job changed; verify trailer assignment.");
            }
            else if (activeJob != null && activeJob.status == LwsJobStatus.HaulActive)
            {
                ActivateDestinationObjective("Active haul restored or changed.");
            }
            else
            {
                PublishChanged();
            }
        }

        private void OnPlayerTruckReady(LwsPlayerTruck truck)
        {
            EnsureAssignmentForCurrentJob("Player truck ready; verify trailer pickup objective.");
        }

        private void OnPlayerTrailerAttached(LwsPlayerTruck truck, LwsTrailerAttachmentState state)
        {
            ValidateAttachedTrailerForPickup(state, "NWH trailer coupling event observed.");
        }

        private void OnTrailerRegistered(LwsTrailerGameplayIdentity trailer)
        {
            EnsureAssignmentForCurrentJob("Trailer registered; verify active job assignment.");
        }

        private void OnLoadCompleted(LwsSaveOperationResult result, LwsManualSaveSlotMetadata slot)
        {
            if (!result.Succeeded)
            {
                return;
            }

            EnsureAssignmentForCurrentJob("Pixel Crushers load completed; restore trailer objective.");
        }

        private bool TryGetPlayerRouteOrigin(out Vector3 playerPosition)
        {
            playerPosition = Vector3.zero;
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                return false;
            }

            playerPosition = truck.transform.position;
            return true;
        }

        private void PublishChanged()
        {
            PickupStateChanged?.Invoke(BuildSnapshot());
        }

        private static string BuildAssignedTrailerId(LwsActiveJob activeJob)
        {
            string source = string.IsNullOrWhiteSpace(activeJob.stableActiveJobId) ? Guid.NewGuid().ToString("N") : activeJob.stableActiveJobId;
            return $"trailer.assigned.{SanitizeId(source)}";
        }

        private static string SanitizeId(string value)
        {
            string normalized = Normalize(value);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Guid.NewGuid().ToString("N");
            }

            char[] chars = normalized.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '.' && chars[i] != '-' && chars[i] != '_')
                {
                    chars[i] = '-';
                }
            }

            return new string(chars);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
