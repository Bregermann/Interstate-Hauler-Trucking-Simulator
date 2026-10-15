using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(135)]
    [DisallowMultipleComponent]
    public sealed class LwsTrailerGameplayIdentity : MonoBehaviour
    {
        private const string UnassignedTrailerId = "trailer.unassigned";

        [Header("Identity")]
        [SerializeField] private string stableTrailerId = UnassignedTrailerId;
        [SerializeField] private string trailerTypeId = LwsTrailerTypeIds.DryVan;
        [SerializeField] private string displayName = "Dry Van Trailer";
        [SerializeField] private string originDepotId;

        [Header("Assignment")]
        [SerializeField] private bool availableForAssignment = true;
        [SerializeField] private bool assignedToJob;
        [SerializeField] private string assignedActiveJobId;

        private ILwsTrailerRegistryService _registryService;
        private bool _registered;

        public string StableTrailerId => stableTrailerId;
        public string TrailerTypeId => trailerTypeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? stableTrailerId : displayName;
        public string OriginDepotId => originDepotId;
        public bool AvailableForAssignment => availableForAssignment;
        public bool AssignedToJob => assignedToJob;
        public string AssignedActiveJobId => assignedActiveJobId;

        public void Configure(
            string newStableTrailerId,
            string newTrailerTypeId,
            string newDisplayName,
            string newOriginDepotId,
            bool isAvailableForAssignment)
        {
            string normalizedStableId = Normalize(newStableTrailerId);
            bool identityChanged = !string.Equals(Normalize(stableTrailerId), normalizedStableId, StringComparison.Ordinal);
            if (_registered && identityChanged && _registryService != null)
            {
                _registryService.UnregisterTrailer(this);
                _registered = false;
                _registryService = null;
            }

            stableTrailerId = normalizedStableId;
            trailerTypeId = Normalize(newTrailerTypeId);
            displayName = string.IsNullOrWhiteSpace(newDisplayName) ? displayName : newDisplayName;
            originDepotId = Normalize(newOriginDepotId);
            availableForAssignment = isAvailableForAssignment;
            SyncVehicleIdentity();
            TryRegister();
        }

        public bool IsCompatibleWith(string requiredTrailerTypeId)
        {
            return string.Equals(Normalize(trailerTypeId), Normalize(requiredTrailerTypeId), StringComparison.Ordinal);
        }

        public bool CanAssignTo(string activeJobId, string requiredTrailerTypeId)
        {
            if (!IsCompatibleWith(requiredTrailerTypeId))
            {
                return false;
            }

            if (assignedToJob)
            {
                return string.Equals(Normalize(assignedActiveJobId), Normalize(activeJobId), StringComparison.Ordinal);
            }

            return availableForAssignment;
        }

        public void MarkAssignedToJob(string activeJobId)
        {
            assignedToJob = true;
            assignedActiveJobId = Normalize(activeJobId);
            availableForAssignment = false;
        }

        public void ClearAssignment(string activeJobId = null)
        {
            if (!string.IsNullOrWhiteSpace(activeJobId) && !string.Equals(Normalize(assignedActiveJobId), Normalize(activeJobId), StringComparison.Ordinal))
            {
                return;
            }

            assignedToJob = false;
            assignedActiveJobId = string.Empty;
            availableForAssignment = true;
        }

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(stableTrailerId) || string.Equals(stableTrailerId, UnassignedTrailerId, StringComparison.Ordinal))
            {
                message = $"{name} has no configured stable trailer ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(trailerTypeId))
            {
                message = $"{stableTrailerId} has no trailer type ID.";
                return false;
            }

            message = $"{stableTrailerId} trailer identity is valid.";
            return true;
        }

        private void Reset()
        {
            SyncVehicleIdentity();
        }

        private void OnEnable()
        {
            SyncVehicleIdentity();
            TryRegister();
        }

        private void Start()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            if (_registered && _registryService != null)
            {
                _registryService.UnregisterTrailer(this);
            }

            _registered = false;
            _registryService = null;
        }

        private void OnValidate()
        {
            stableTrailerId = Normalize(stableTrailerId);
            trailerTypeId = Normalize(trailerTypeId);
            originDepotId = Normalize(originDepotId);
            assignedActiveJobId = Normalize(assignedActiveJobId);
        }

        private void TryRegister()
        {
            if (_registered || !HasRegisterableIdentity())
            {
                return;
            }

            if (!TryResolveRegistry(out ILwsTrailerRegistryService registry))
            {
                return;
            }

            LwsServiceResult result = registry.RegisterTrailer(this);
            if (!result.Succeeded)
            {
                Debug.LogWarning(result.Message, this);
                return;
            }

            _registryService = registry;
            _registered = true;
        }

        private bool HasRegisterableIdentity()
        {
            return !string.IsNullOrWhiteSpace(stableTrailerId) &&
                   !string.Equals(stableTrailerId, UnassignedTrailerId, StringComparison.Ordinal) &&
                   !string.IsNullOrWhiteSpace(trailerTypeId);
        }

        private static bool TryResolveRegistry(out ILwsTrailerRegistryService registry)
        {
            registry = null;
            return LwsApplicationBootstrap.Instance != null &&
                   LwsApplicationBootstrap.Instance.Registry != null &&
                   LwsApplicationBootstrap.Instance.Registry.TryGet(out registry);
        }

        private void SyncVehicleIdentity()
        {
            LwsVehicleIdentity vehicleIdentity = GetComponent<LwsVehicleIdentity>();
            if (vehicleIdentity == null)
            {
                return;
            }

            vehicleIdentity.Configure(
                string.IsNullOrWhiteSpace(stableTrailerId) ? vehicleIdentity.VehicleId : stableTrailerId,
                string.IsNullOrWhiteSpace(trailerTypeId) ? vehicleIdentity.DefinitionId : trailerTypeId,
                LwsVehicleRole.Trailer,
                DisplayName,
                false);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
