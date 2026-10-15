using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(125)]
    [DisallowMultipleComponent]
    public sealed class LwsTrailerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string stableSpawnPointId = "trailer.spawn.unassigned";
        [SerializeField] private string originDepotId;
        [SerializeField] private string trailerTypeId = LwsTrailerTypeIds.DryVan;
        [SerializeField] private string displayName = "Trailer Pickup Spawn";
        [SerializeField] private GameObject trailerPrefab;
        [SerializeField] private bool enabledForAssignment = true;

        public string StableSpawnPointId => stableSpawnPointId;
        public string OriginDepotId => originDepotId;
        public string TrailerTypeId => trailerTypeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? stableSpawnPointId : displayName;
        public GameObject TrailerPrefab => trailerPrefab;
        public bool EnabledForAssignment => enabledForAssignment;

        public void Configure(
            string newSpawnPointId,
            string newOriginDepotId,
            string newTrailerTypeId,
            GameObject newTrailerPrefab,
            bool isEnabled,
            string newDisplayName = null)
        {
            stableSpawnPointId = Normalize(newSpawnPointId);
            originDepotId = Normalize(newOriginDepotId);
            trailerTypeId = Normalize(newTrailerTypeId);
            trailerPrefab = newTrailerPrefab;
            enabledForAssignment = isEnabled;
            if (!string.IsNullOrWhiteSpace(newDisplayName))
            {
                displayName = newDisplayName.Trim();
            }
        }

        public bool CanSpawnFor(string requiredTrailerTypeId, string preferredOriginDepotId)
        {
            if (!enabledForAssignment || trailerPrefab == null)
            {
                return false;
            }

            if (!string.Equals(Normalize(trailerTypeId), Normalize(requiredTrailerTypeId), System.StringComparison.Ordinal))
            {
                return false;
            }

            string origin = Normalize(preferredOriginDepotId);
            return string.IsNullOrWhiteSpace(origin) ||
                   string.IsNullOrWhiteSpace(originDepotId) ||
                   string.Equals(Normalize(originDepotId), origin, System.StringComparison.Ordinal);
        }

        public LwsTrailerGameplayIdentity SpawnTrailer(string stableTrailerId, string activeJobId)
        {
            if (trailerPrefab == null)
            {
                return null;
            }

            GameObject trailer = Instantiate(trailerPrefab, transform.position, transform.rotation);
            trailer.name = string.IsNullOrWhiteSpace(stableTrailerId) ? $"IH_JobTrailer_{trailerTypeId}" : stableTrailerId;
            foreach (Rigidbody rb in trailer.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }

            VehicleController controller = trailer.GetComponent<VehicleController>();
            if (controller != null)
            {
                controller.enabled = true;
            }

            LwsVehicleIdentity vehicleIdentity = trailer.GetComponent<LwsVehicleIdentity>();
            if (vehicleIdentity == null)
            {
                vehicleIdentity = trailer.AddComponent<LwsVehicleIdentity>();
            }

            string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Assigned Trailer" : displayName;
            vehicleIdentity.Configure(
                stableTrailerId,
                trailerTypeId,
                LwsVehicleRole.Trailer,
                resolvedDisplayName,
                false);

            LwsTrailerGameplayIdentity identity = trailer.GetComponent<LwsTrailerGameplayIdentity>();
            if (identity == null)
            {
                identity = trailer.AddComponent<LwsTrailerGameplayIdentity>();
            }

            identity.Configure(
                stableTrailerId,
                trailerTypeId,
                resolvedDisplayName,
                originDepotId,
                true);
            identity.MarkAssignedToJob(activeJobId);

            LwsFloatingOriginRigidbodyParticipant originParticipant =
                trailer.GetComponent<LwsFloatingOriginRigidbodyParticipant>() ??
                trailer.AddComponent<LwsFloatingOriginRigidbodyParticipant>();
            originParticipant.Configure(stableTrailerId, LwsFloatingOriginParticipantKind.PlayerTrailer, false);

            Physics.SyncTransforms();
            return identity;
        }

        private void OnValidate()
        {
            stableSpawnPointId = Normalize(stableSpawnPointId);
            originDepotId = Normalize(originDepotId);
            trailerTypeId = Normalize(trailerTypeId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
