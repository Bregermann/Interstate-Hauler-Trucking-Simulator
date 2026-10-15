using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsJobCatalogService : ILwsService
    {
        IReadOnlyList<LwsJobCatalog> Catalogs { get; }
        int JobDefinitionCount { get; }
        int DepotDefinitionCount { get; }
        int DestinationDefinitionCount { get; }
        LwsServiceResult RegisterCatalog(LwsJobCatalog catalog);
        LwsServiceResult UnregisterCatalog(LwsJobCatalog catalog);
        bool TryResolveDepot(string depotId, out LwsDepotDefinition depot);
        bool TryResolveDestination(string destinationId, out LwsDestinationDefinition destination);
        bool TryResolveJob(string jobDefinitionId, out LwsJobDefinition job);
        IReadOnlyList<LwsJobDefinition> GetEnabledJobsForOrigin(string depotId);
        LwsServiceResult ValidateCatalogs();
    }

    public interface ILwsJobOfferProvider : ILwsService
    {
        IReadOnlyList<LwsJobOffer> GetOffersForDepot(string depotId);
    }

    public interface ILwsActiveJobService : ILwsService
    {
        bool HasActiveJob { get; }
        LwsActiveJob CurrentActiveJob { get; }
        event Action<LwsActiveJob> ActiveJobAccepted;
        event Action<LwsActiveJob> ActiveJobChanged;
        LwsServiceResult AcceptJob(LwsJobOffer offer, out LwsActiveJob activeJob, string reason);
        LwsServiceResult RestoreActiveJob(LwsActiveJob activeJob, string reason, bool deriveGameplayState);
        LwsServiceResult AssignTrailerToActiveJob(LwsTrailerAssignment assignment, string reason);
        LwsServiceResult CompleteTrailerPickup(string attachedTrailerId, string reason);
        LwsServiceResult EnterDelivery(string destinationId, string attachedTrailerId, string reason);
        LwsServiceResult CompleteDelivery(LwsDeliveryCompletionSnapshot completion, string reason);
        LwsServiceResult ClearActiveJob(string reason);
        LwsGameplayState DeriveGameplayStateForCurrentJob();
        bool TryDeriveGameplayState(string reason, out string message);
    }

    public interface ILwsJobBoardService : ILwsService
    {
        bool IsOpen { get; }
        IReadOnlyList<LwsJobOffer> CurrentOffers { get; }
        LwsJobOffer SelectedOffer { get; }
        string LastMessage { get; }
        event Action JobBoardOpened;
        event Action JobBoardClosed;
        event Action<IReadOnlyList<LwsJobOffer>> OffersChanged;
        event Action<LwsActiveJob> JobAccepted;
        LwsServiceResult CanOpenCurrentDepot();
        LwsServiceResult OpenJobBoard(string reason);
        LwsServiceResult CloseJobBoard(string reason, bool acceptedJob);
        LwsServiceResult SelectOffer(string stableOfferId);
        LwsServiceResult AcceptSelectedOffer();
        LwsServiceResult RefreshOffers();
    }

    public sealed class LwsJobCatalogService : ILwsJobCatalogService
    {
        public const string ValidationCatalogResourcePath = "InterstateHauler/Gameplay/IH_JobCatalog_Validation";

        private readonly List<LwsJobCatalog> _catalogs = new List<LwsJobCatalog>();
        private ILwsDepotService _depotService;

        public string ServiceId => "lws.jobs.catalog";
        public IReadOnlyList<LwsJobCatalog> Catalogs => _catalogs;
        public int JobDefinitionCount => _catalogs.Sum(c => c != null ? c.JobDefinitions.Count : 0);
        public int DepotDefinitionCount => _catalogs.Sum(c => c != null ? c.DepotDefinitions.Count : 0);
        public int DestinationDefinitionCount => _catalogs.Sum(c => c != null ? c.DestinationDefinitions.Count : 0);

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            context.Registry.TryGet(out _depotService);
            LwsJobCatalog validationCatalog = Resources.Load<LwsJobCatalog>(ValidationCatalogResourcePath);
            if (validationCatalog != null)
            {
                LwsServiceResult result = RegisterCatalog(validationCatalog);
                if (!result.Succeeded)
                {
                    return result;
                }
            }

            return LwsServiceResult.Success(validationCatalog != null
                ? "LWS authored job catalog service initialized with validation catalog."
                : "LWS authored job catalog service initialized; no Resources catalog is currently loaded.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _catalogs.Clear();
            _depotService = null;
            return LwsServiceResult.Success("LWS authored job catalog service shut down.");
        }

        public LwsServiceResult RegisterCatalog(LwsJobCatalog catalog)
        {
            if (catalog == null)
            {
                return LwsServiceResult.Failure("Cannot register a null job catalog.");
            }

            if (!_catalogs.Contains(catalog))
            {
                _catalogs.Add(catalog);
            }

            LwsServiceResult validation = ValidateCatalogs();
            if (!validation.Succeeded)
            {
                _catalogs.Remove(catalog);
                return validation;
            }

            foreach (LwsDepotDefinition depot in catalog.DepotDefinitions)
            {
                if (depot != null)
                {
                    LwsServiceResult result = _depotService?.RegisterDepotDefinition(depot) ?? LwsServiceResult.Success();
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }
            }

            return LwsServiceResult.Success($"Registered authored job catalog {catalog.CatalogId}.");
        }

        public LwsServiceResult UnregisterCatalog(LwsJobCatalog catalog)
        {
            if (catalog == null)
            {
                return LwsServiceResult.Success("No job catalog to unregister.");
            }

            _catalogs.Remove(catalog);
            return LwsServiceResult.Success($"Unregistered authored job catalog {catalog.CatalogId}.");
        }

        public bool TryResolveDepot(string depotId, out LwsDepotDefinition depot)
        {
            foreach (LwsJobCatalog catalog in _catalogs)
            {
                if (catalog != null && catalog.TryResolveDepot(depotId, out depot))
                {
                    return true;
                }
            }

            depot = null;
            return false;
        }

        public bool TryResolveDestination(string destinationId, out LwsDestinationDefinition destination)
        {
            foreach (LwsJobCatalog catalog in _catalogs)
            {
                if (catalog != null && catalog.TryResolveDestination(destinationId, out destination))
                {
                    return true;
                }
            }

            destination = null;
            return false;
        }

        public bool TryResolveJob(string jobDefinitionId, out LwsJobDefinition job)
        {
            foreach (LwsJobCatalog catalog in _catalogs)
            {
                if (catalog != null && catalog.TryResolveJob(jobDefinitionId, out job))
                {
                    return true;
                }
            }

            job = null;
            return false;
        }

        public IReadOnlyList<LwsJobDefinition> GetEnabledJobsForOrigin(string depotId)
        {
            return _catalogs
                .Where(c => c != null)
                .SelectMany(c => c.GetEnabledJobsForOrigin(depotId))
                .OrderBy(j => j.StableJobDefinitionId, StringComparer.Ordinal)
                .ToList();
        }

        public LwsServiceResult ValidateCatalogs()
        {
            var depotIds = new HashSet<string>(StringComparer.Ordinal);
            var destinationIds = new HashSet<string>(StringComparer.Ordinal);
            var jobIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (LwsJobCatalog catalog in _catalogs)
            {
                if (catalog == null)
                {
                    continue;
                }

                if (!catalog.ValidateCatalog(out string message))
                {
                    return LwsServiceResult.Failure(message);
                }

                foreach (LwsDepotDefinition depot in catalog.DepotDefinitions.Where(d => d != null))
                {
                    if (!depotIds.Add(depot.StableDepotId))
                    {
                        return LwsServiceResult.Failure($"Duplicate depot ID across job catalogs: {depot.StableDepotId}");
                    }
                }

                foreach (LwsDestinationDefinition destination in catalog.DestinationDefinitions.Where(d => d != null))
                {
                    if (!destinationIds.Add(destination.StableDestinationId))
                    {
                        return LwsServiceResult.Failure($"Duplicate destination ID across job catalogs: {destination.StableDestinationId}");
                    }
                }

                foreach (LwsJobDefinition job in catalog.JobDefinitions.Where(j => j != null))
                {
                    if (!jobIds.Add(job.StableJobDefinitionId))
                    {
                        return LwsServiceResult.Failure($"Duplicate job definition ID across job catalogs: {job.StableJobDefinitionId}");
                    }
                }
            }

            return LwsServiceResult.Success("Authored job catalogs are valid.");
        }
    }

    public sealed class LwsAuthoredJobOfferProvider : ILwsJobOfferProvider
    {
        private ILwsJobCatalogService _catalogService;

        public string ServiceId => "lws.jobs.authored-offers";

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            context.Registry.TryGet(out _catalogService);
            return LwsServiceResult.Success("LWS authored job offer provider initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _catalogService = null;
            return LwsServiceResult.Success("LWS authored job offer provider shut down.");
        }

        public IReadOnlyList<LwsJobOffer> GetOffersForDepot(string depotId)
        {
            if (_catalogService == null || string.IsNullOrWhiteSpace(depotId))
            {
                return Array.Empty<LwsJobOffer>();
            }

            return _catalogService.GetEnabledJobsForOrigin(depotId)
                .Select(j => j.ToOffer())
                .Where(o => o != null && o.IsValid)
                .OrderBy(o => o.jobDefinitionId, StringComparer.Ordinal)
                .ToList();
        }
    }

    public sealed class LwsActiveJobService : ILwsActiveJobService
    {
        private ILwsGameplayStateService _gameplayStateService;
        private ILwsSaveService _saveService;
        private LwsActiveJob _currentActiveJob;

        public string ServiceId => "lws.jobs.active";
        public bool HasActiveJob => _currentActiveJob != null && _currentActiveJob.IsValid;
        public LwsActiveJob CurrentActiveJob => _currentActiveJob != null ? _currentActiveJob.Clone() : null;

        public event Action<LwsActiveJob> ActiveJobAccepted;
        public event Action<LwsActiveJob> ActiveJobChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            context.Registry.TryGet(out _gameplayStateService);
            if (context.Registry.TryGet(out _saveService) && _saveService != null)
            {
                _saveService.LoadCompleted += OnLoadCompleted;
            }

            return LwsServiceResult.Success("LWS active job service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            if (_saveService != null)
            {
                _saveService.LoadCompleted -= OnLoadCompleted;
            }

            _saveService = null;
            _gameplayStateService = null;
            _currentActiveJob = null;
            ActiveJobAccepted = null;
            ActiveJobChanged = null;
            return LwsServiceResult.Success("LWS active job service shut down.");
        }

        public LwsServiceResult AcceptJob(LwsJobOffer offer, out LwsActiveJob activeJob, string reason)
        {
            activeJob = null;
            if (HasActiveJob)
            {
                return LwsServiceResult.Failure("An active job already exists.");
            }

            if (offer == null || !offer.IsValid)
            {
                return LwsServiceResult.Failure("Cannot accept an invalid authored job offer.");
            }

            activeJob = LwsActiveJob.FromOffer(offer);
            if (activeJob == null || !activeJob.IsValid)
            {
                return LwsServiceResult.Failure("Accepted authored job snapshot is invalid.");
            }

            _currentActiveJob = activeJob.Clone();
            ActiveJobAccepted?.Invoke(CurrentActiveJob);
            ActiveJobChanged?.Invoke(CurrentActiveJob);
            TryDeriveGameplayState(string.IsNullOrWhiteSpace(reason) ? "Authored job accepted." : reason, out _);
            return LwsServiceResult.Success($"Accepted authored job {offer.jobDefinitionId}.");
        }

        public LwsServiceResult RestoreActiveJob(LwsActiveJob activeJob, string reason, bool deriveGameplayState)
        {
            if (activeJob == null || activeJob.status == LwsJobStatus.None)
            {
                _currentActiveJob = null;
                ActiveJobChanged?.Invoke(null);
                return LwsServiceResult.Success("No active job restored.");
            }

            if (!activeJob.IsValid)
            {
                return LwsServiceResult.Failure("Restored active job payload is invalid.");
            }

            _currentActiveJob = activeJob.Clone();
            ActiveJobChanged?.Invoke(CurrentActiveJob);
            if (deriveGameplayState)
            {
                TryDeriveGameplayState(reason, out _);
            }

            return LwsServiceResult.Success($"Restored active job {_currentActiveJob.stableActiveJobId}.");
        }


        public LwsServiceResult AssignTrailerToActiveJob(LwsTrailerAssignment assignment, string reason)
        {
            if (!HasActiveJob)
            {
                return LwsServiceResult.Failure("Cannot assign a trailer without an active job.");
            }

            if (!assignment.hasAssignedTrailer || !assignment.IsValid)
            {
                return LwsServiceResult.Failure("Cannot assign an invalid trailer assignment.");
            }

            if (!_currentActiveJob.trailerAssignment.MatchesRequiredType(_currentActiveJob.requiredTrailerTypeId) && _currentActiveJob.trailerAssignment.hasAssignedTrailer)
            {
                return LwsServiceResult.Failure($"Existing trailer assignment {_currentActiveJob.trailerAssignment.stableTrailerId} does not match required trailer type {_currentActiveJob.requiredTrailerTypeId}.");
            }

            if (!assignment.MatchesRequiredType(_currentActiveJob.requiredTrailerTypeId))
            {
                return LwsServiceResult.Failure($"Assigned trailer type {assignment.trailerTypeId} does not match required type {_currentActiveJob.requiredTrailerTypeId}.");
            }

            if (_currentActiveJob.trailerAssignment.hasAssignedTrailer &&
                !string.Equals(_currentActiveJob.trailerAssignment.stableTrailerId, assignment.stableTrailerId, StringComparison.Ordinal))
            {
                return LwsServiceResult.Failure($"Active job already has assigned trailer {_currentActiveJob.trailerAssignment.stableTrailerId}; refusing to replace it with {assignment.stableTrailerId}.");
            }

            _currentActiveJob.trailerAssignment = assignment;
            ActiveJobChanged?.Invoke(CurrentActiveJob);
            return LwsServiceResult.Success(string.IsNullOrWhiteSpace(reason) ? $"Assigned trailer {assignment.stableTrailerId}." : reason);
        }

        public LwsServiceResult CompleteTrailerPickup(string attachedTrailerId, string reason)
        {
            if (!HasActiveJob)
            {
                return LwsServiceResult.Failure("Cannot complete trailer pickup without an active job.");
            }

            if (_currentActiveJob.status != LwsJobStatus.AwaitingTrailerPickup)
            {
                return LwsServiceResult.Failure($"Active job status {_currentActiveJob.status} is not awaiting trailer pickup.");
            }

            if (!_currentActiveJob.trailerAssignment.hasAssignedTrailer)
            {
                return LwsServiceResult.Failure("Active job has no assigned trailer to validate.");
            }

            if (!string.Equals(_currentActiveJob.trailerAssignment.stableTrailerId, attachedTrailerId, StringComparison.Ordinal))
            {
                return LwsServiceResult.Failure($"Attached trailer {attachedTrailerId} does not match assigned trailer {_currentActiveJob.trailerAssignment.stableTrailerId}.");
            }

            LwsTrailerAssignment assignment = _currentActiveJob.trailerAssignment;
            assignment.pickupCompletedUtcTicks = DateTime.UtcNow.Ticks;
            _currentActiveJob.trailerAssignment = assignment;
            _currentActiveJob.status = LwsJobStatus.HaulActive;
            ActiveJobChanged?.Invoke(CurrentActiveJob);
            TryDeriveGameplayState(string.IsNullOrWhiteSpace(reason) ? "Assigned trailer pickup completed." : reason, out _);
            return LwsServiceResult.Success($"Trailer pickup complete for active job {_currentActiveJob.stableActiveJobId}.");
        }

        public LwsServiceResult EnterDelivery(string destinationId, string attachedTrailerId, string reason)
        {
            if (!HasActiveJob)
            {
                return LwsServiceResult.Failure("Cannot enter delivery without an active job.");
            }

            if (_currentActiveJob.status == LwsJobStatus.Delivery)
            {
                return LwsServiceResult.Success("Active job is already in Delivery.");
            }

            if (_currentActiveJob.status != LwsJobStatus.HaulActive)
            {
                return LwsServiceResult.Failure($"Active job status {_currentActiveJob.status} cannot enter Delivery.");
            }

            if (!LwsDeliveryValidation.DestinationMatches(_currentActiveJob.destinationId, destinationId))
            {
                return LwsServiceResult.Failure($"Delivery destination {destinationId} does not match active job destination {_currentActiveJob.destinationId}.");
            }

            if (!_currentActiveJob.trailerAssignment.hasAssignedTrailer ||
                !LwsDeliveryValidation.AssignedTrailerMatches(_currentActiveJob.trailerAssignment.stableTrailerId, attachedTrailerId))
            {
                return LwsServiceResult.Failure($"Attached trailer {attachedTrailerId} does not match assigned trailer {_currentActiveJob.trailerAssignment.stableTrailerId}.");
            }

            _currentActiveJob.status = LwsJobStatus.Delivery;
            _currentActiveJob.deliveryCompletion = LwsDeliveryCompletionSnapshot.None;
            ActiveJobChanged?.Invoke(CurrentActiveJob);
            TryDeriveGameplayState(string.IsNullOrWhiteSpace(reason) ? "Matching destination delivery area entered." : reason, out _);
            return LwsServiceResult.Success($"Active job {_currentActiveJob.stableActiveJobId} entered Delivery.");
        }

        public LwsServiceResult CompleteDelivery(LwsDeliveryCompletionSnapshot completion, string reason)
        {
            if (!HasActiveJob)
            {
                return LwsServiceResult.Failure("Cannot complete delivery without an active job.");
            }

            if (_currentActiveJob.status == LwsJobStatus.Completed && _currentActiveJob.deliveryCompletion.IsValid)
            {
                return LwsServiceResult.Success("Active job delivery is already complete.");
            }

            if (_currentActiveJob.status != LwsJobStatus.Delivery)
            {
                return LwsServiceResult.Failure($"Active job status {_currentActiveJob.status} cannot complete delivery.");
            }

            if (!completion.IsValid ||
                !string.Equals(_currentActiveJob.stableActiveJobId, completion.activeJobId, StringComparison.Ordinal) ||
                !LwsDeliveryValidation.DestinationMatches(_currentActiveJob.destinationId, completion.destinationId) ||
                !LwsDeliveryValidation.AssignedTrailerMatches(_currentActiveJob.trailerAssignment.stableTrailerId, completion.assignedTrailerId))
            {
                return LwsServiceResult.Failure("Delivery completion snapshot does not match the active job, destination, and assigned trailer.");
            }

            _currentActiveJob.deliveryCompletion = completion;
            _currentActiveJob.status = LwsJobStatus.Completed;
            ActiveJobChanged?.Invoke(CurrentActiveJob);
            TryDeriveGameplayState(string.IsNullOrWhiteSpace(reason) ? "Delivery gameplay completed; results handoff ready." : reason, out _);
            return LwsServiceResult.Success($"Active job {_currentActiveJob.stableActiveJobId} completed delivery gameplay.");
        }

        public LwsServiceResult ClearActiveJob(string reason)
        {
            _currentActiveJob = null;
            ActiveJobChanged?.Invoke(null);
            return LwsServiceResult.Success(string.IsNullOrWhiteSpace(reason) ? "Active job cleared." : reason);
        }

        public LwsGameplayState DeriveGameplayStateForCurrentJob()
        {
            if (!HasActiveJob)
            {
                return LwsGameplayState.FreeDrive;
            }

            switch (_currentActiveJob.status)
            {
                case LwsJobStatus.AwaitingTrailerPickup:
                    return LwsGameplayState.TrailerPickup;
                case LwsJobStatus.HaulActive:
                    return LwsGameplayState.HaulActive;
                case LwsJobStatus.Delivery:
                    return LwsGameplayState.Delivery;
                case LwsJobStatus.Completed:
                    return LwsGameplayState.DeliveryResults;
                default:
                    return LwsGameplayState.FreeDrive;
            }
        }

        public bool TryDeriveGameplayState(string reason, out string message)
        {
            message = string.Empty;
            if (_gameplayStateService == null || !HasActiveJob)
            {
                message = "Gameplay state or active job is not available.";
                return false;
            }

            LwsGameplayState target = DeriveGameplayStateForCurrentJob();
            if (target == LwsGameplayState.FreeDrive && _currentActiveJob.status == LwsJobStatus.AwaitingTrailerPickup)
            {
                message = "Active job status could not be mapped to a gameplay state.";
                return false;
            }

            LwsGameplayStateTransitionResult transition = _gameplayStateService.TryTransitionTo(
                target,
                string.IsNullOrWhiteSpace(reason) ? $"Active job {_currentActiveJob.stableActiveJobId} derived gameplay state." : reason);
            message = transition.Message;
            return transition.Succeeded;
        }

        private void OnLoadCompleted(LwsSaveOperationResult result, LwsManualSaveSlotMetadata slot)
        {
            if (result.Succeeded && HasActiveJob)
            {
                TryDeriveGameplayState("Active job restored after Pixel Crushers load.", out _);
            }
        }
    }

    public sealed class LwsJobBoardService : ILwsJobBoardService
    {
        private readonly List<LwsJobOffer> _currentOffers = new List<LwsJobOffer>();
        private ILwsDepotService _depotService;
        private ILwsJobOfferProvider _offerProvider;
        private ILwsActiveJobService _activeJobService;
        private ILwsGameplayStateService _gameplayStateService;
        private ILwsSaveService _saveService;
        private LwsJobOffer _selectedOffer;

        public string ServiceId => "lws.jobs.board";
        public bool IsOpen { get; private set; }
        public IReadOnlyList<LwsJobOffer> CurrentOffers => _currentOffers;
        public LwsJobOffer SelectedOffer => _selectedOffer != null ? _selectedOffer.Clone() : null;
        public string LastMessage { get; private set; } = "Job board not opened.";

        public event Action JobBoardOpened;
        public event Action JobBoardClosed;
        public event Action<IReadOnlyList<LwsJobOffer>> OffersChanged;
        public event Action<LwsActiveJob> JobAccepted;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            context.Registry.TryGet(out _depotService);
            context.Registry.TryGet(out _offerProvider);
            context.Registry.TryGet(out _activeJobService);
            context.Registry.TryGet(out _gameplayStateService);
            context.Registry.TryGet(out _saveService);
            return LwsServiceResult.Success("LWS job board service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _currentOffers.Clear();
            _selectedOffer = null;
            IsOpen = false;
            _depotService = null;
            _offerProvider = null;
            _activeJobService = null;
            _gameplayStateService = null;
            _saveService = null;
            JobBoardOpened = null;
            JobBoardClosed = null;
            OffersChanged = null;
            JobAccepted = null;
            return LwsServiceResult.Success("LWS job board service shut down.");
        }

        public LwsServiceResult CanOpenCurrentDepot()
        {
            if (_depotService == null || !_depotService.IsPlayerAtDepot || _depotService.CurrentDepotDefinition == null)
            {
                return LwsServiceResult.Failure("Player is not at a registered depot.");
            }

            if (!_depotService.CurrentDepotDefinition.HasJobBoard)
            {
                return LwsServiceResult.Failure("Current depot does not have a job board.");
            }

            if (_gameplayStateService == null || _gameplayStateService.CurrentState != LwsGameplayState.AtDepot)
            {
                return LwsServiceResult.Failure("Job board can only open from AT_DEPOT.");
            }

            return LwsServiceResult.Success("Job board can open.");
        }

        public LwsServiceResult OpenJobBoard(string reason)
        {
            if (IsOpen)
            {
                return LwsServiceResult.Success("Job board is already open.");
            }

            LwsServiceResult canOpen = CanOpenCurrentDepot();
            if (!canOpen.Succeeded)
            {
                LastMessage = canOpen.Message;
                return canOpen;
            }

            LwsGameplayStateTransitionResult transition = _gameplayStateService.TryTransitionTo(
                LwsGameplayState.JobSelection,
                string.IsNullOrWhiteSpace(reason) ? "Player opened depot job board." : reason);
            if (!transition.Succeeded)
            {
                LastMessage = transition.Message;
                return LwsServiceResult.Failure(transition.Message);
            }

            IsOpen = true;
            RefreshOffers();
            LastMessage = "Job board opened.";
            JobBoardOpened?.Invoke();
            return LwsServiceResult.Success(LastMessage);
        }

        public LwsServiceResult CloseJobBoard(string reason, bool acceptedJob)
        {
            if (!IsOpen)
            {
                return LwsServiceResult.Success("Job board is already closed.");
            }

            IsOpen = false;
            _selectedOffer = null;
            if (!acceptedJob && _gameplayStateService != null && _gameplayStateService.CurrentState == LwsGameplayState.JobSelection)
            {
                _gameplayStateService.TryTransitionTo(LwsGameplayState.AtDepot, string.IsNullOrWhiteSpace(reason) ? "Job board closed." : reason);
            }

            LastMessage = string.IsNullOrWhiteSpace(reason) ? "Job board closed." : reason;
            JobBoardClosed?.Invoke();
            return LwsServiceResult.Success(LastMessage);
        }

        public LwsServiceResult SelectOffer(string stableOfferId)
        {
            LwsJobOffer offer = _currentOffers.FirstOrDefault(o => o != null && string.Equals(o.stableOfferId, stableOfferId, StringComparison.Ordinal));
            if (offer == null)
            {
                LastMessage = $"Job offer not found: {stableOfferId}";
                return LwsServiceResult.Failure(LastMessage);
            }

            _selectedOffer = offer.Clone();
            LastMessage = $"Selected {offer.jobDefinitionId}.";
            return LwsServiceResult.Success(LastMessage);
        }

        public LwsServiceResult AcceptSelectedOffer()
        {
            if (!IsOpen || _gameplayStateService == null || _gameplayStateService.CurrentState != LwsGameplayState.JobSelection)
            {
                LastMessage = "A job can only be accepted while the job board is open.";
                return LwsServiceResult.Failure(LastMessage);
            }

            if (_selectedOffer == null)
            {
                LastMessage = "No authored job offer is selected.";
                return LwsServiceResult.Failure(LastMessage);
            }

            if (_activeJobService != null && _activeJobService.HasActiveJob)
            {
                LastMessage = "An active job already exists.";
                return LwsServiceResult.Failure(LastMessage);
            }

            if (_depotService == null || !_depotService.IsPlayerAtDepot || !string.Equals(_depotService.CurrentDepotId, _selectedOffer.originDepotId, StringComparison.Ordinal))
            {
                LastMessage = "Selected job offer does not belong to the current depot.";
                return LwsServiceResult.Failure(LastMessage);
            }

            RefreshOffers();
            if (!_currentOffers.Any(o => o != null && string.Equals(o.stableOfferId, _selectedOffer.stableOfferId, StringComparison.Ordinal)))
            {
                LastMessage = "Selected job offer is no longer available.";
                return LwsServiceResult.Failure(LastMessage);
            }

            LwsActiveJob activeJob = null;
            LwsServiceResult accepted = _activeJobService != null
                ? _activeJobService.AcceptJob(_selectedOffer, out activeJob, "Player accepted authored job from depot job board.")
                : LwsServiceResult.Failure("Active job service is missing.");
            if (!accepted.Succeeded)
            {
                LastMessage = accepted.Message;
                return accepted;
            }

            _saveService?.RequestAutosave("job.accepted");
            CloseJobBoard("Authored job accepted.", true);
            LastMessage = accepted.Message;
            JobAccepted?.Invoke(activeJob != null ? activeJob.Clone() : null);
            return accepted;
        }

        public LwsServiceResult RefreshOffers()
        {
            _currentOffers.Clear();
            if (_depotService == null || !_depotService.IsPlayerAtDepot || _offerProvider == null)
            {
                _selectedOffer = null;
                OffersChanged?.Invoke(_currentOffers);
                return LwsServiceResult.Success("No depot job offers are available.");
            }

            _currentOffers.AddRange(_offerProvider.GetOffersForDepot(_depotService.CurrentDepotId).Select(o => o.Clone()));
            if (_selectedOffer == null || !_currentOffers.Any(o => o.stableOfferId == _selectedOffer.stableOfferId))
            {
                _selectedOffer = _currentOffers.Count > 0 ? _currentOffers[0].Clone() : null;
            }

            OffersChanged?.Invoke(_currentOffers);
            LastMessage = $"{_currentOffers.Count} authored job offer(s) available.";
            return LwsServiceResult.Success(LastMessage);
        }
    }
}
