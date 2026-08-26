using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(menuName = "Interstate Hauler/Gameplay/Job Catalog", fileName = "IH_JobCatalog")]
    public sealed class LwsJobCatalog : ScriptableObject
    {
        [SerializeField] private int schemaVersion = LwsSaveSchema.CurrentVersion;
        [SerializeField] private string catalogId = "catalog.jobs.unassigned";
        [SerializeField] private string displayName = "Interstate Job Catalog";
        [SerializeField] private List<LwsDepotDefinition> depotDefinitions = new List<LwsDepotDefinition>();
        [SerializeField] private List<LwsDestinationDefinition> destinationDefinitions = new List<LwsDestinationDefinition>();
        [SerializeField] private List<LwsJobDefinition> jobDefinitions = new List<LwsJobDefinition>();
        [TextArea(2, 6)]
        [SerializeField] private string developerNotes;

        public int SchemaVersion => schemaVersion;
        public string CatalogId => catalogId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? catalogId : displayName;
        public IReadOnlyList<LwsDepotDefinition> DepotDefinitions => depotDefinitions;
        public IReadOnlyList<LwsDestinationDefinition> DestinationDefinitions => destinationDefinitions;
        public IReadOnlyList<LwsJobDefinition> JobDefinitions => jobDefinitions;
        public string DeveloperNotes => developerNotes;

        public void ConfigureForTests(
            IEnumerable<LwsDepotDefinition> depots,
            IEnumerable<LwsDestinationDefinition> destinations,
            IEnumerable<LwsJobDefinition> jobs)
        {
            depotDefinitions = depots?.Where(d => d != null).ToList() ?? new List<LwsDepotDefinition>();
            destinationDefinitions = destinations?.Where(d => d != null).ToList() ?? new List<LwsDestinationDefinition>();
            jobDefinitions = jobs?.Where(j => j != null).ToList() ?? new List<LwsJobDefinition>();
        }

        public bool TryResolveDepot(string depotId, out LwsDepotDefinition definition)
        {
            definition = depotDefinitions.FirstOrDefault(d => d != null && string.Equals(d.StableDepotId, depotId, StringComparison.Ordinal));
            return definition != null;
        }

        public bool TryResolveDestination(string destinationId, out LwsDestinationDefinition definition)
        {
            definition = destinationDefinitions.FirstOrDefault(d => d != null && string.Equals(d.StableDestinationId, destinationId, StringComparison.Ordinal));
            return definition != null;
        }

        public bool TryResolveJob(string jobDefinitionId, out LwsJobDefinition definition)
        {
            definition = jobDefinitions.FirstOrDefault(j => j != null && string.Equals(j.StableJobDefinitionId, jobDefinitionId, StringComparison.Ordinal));
            return definition != null;
        }

        public IReadOnlyList<LwsJobDefinition> GetEnabledJobsForOrigin(string depotId)
        {
            if (string.IsNullOrWhiteSpace(depotId))
            {
                return Array.Empty<LwsJobDefinition>();
            }

            return jobDefinitions
                .Where(j => j != null &&
                            j.Enabled &&
                            string.Equals(j.OriginDepotId, depotId, StringComparison.Ordinal) &&
                            j.Validate(out _))
                .OrderBy(j => j.StableJobDefinitionId, StringComparer.Ordinal)
                .ToList();
        }

        public bool ValidateCatalog(out string message)
        {
            if (schemaVersion <= 0 || schemaVersion > LwsSaveSchema.CurrentVersion)
            {
                message = $"{name} has unsupported job catalog schema version {schemaVersion}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(catalogId))
            {
                message = $"{name} has no stable catalog ID.";
                return false;
            }

            if (!LwsDepotDefinition.ValidateUniqueIds(depotDefinitions, out message))
            {
                return false;
            }

            if (!LwsDestinationDefinition.ValidateUniqueIds(destinationDefinitions, out message))
            {
                return false;
            }

            if (!LwsJobDefinition.ValidateUniqueIds(jobDefinitions, out message))
            {
                return false;
            }

            foreach (LwsDepotDefinition depot in depotDefinitions)
            {
                if (depot != null && !depot.Validate(out message))
                {
                    return false;
                }
            }

            foreach (LwsDestinationDefinition destination in destinationDefinitions)
            {
                if (destination != null && !destination.Validate(out message))
                {
                    return false;
                }
            }

            foreach (LwsJobDefinition job in jobDefinitions)
            {
                if (job != null && !job.Validate(out message))
                {
                    return false;
                }
            }

            message = $"{catalogId} job catalog is valid.";
            return true;
        }

        private void OnValidate()
        {
            schemaVersion = Mathf.Clamp(schemaVersion, 1, LwsSaveSchema.CurrentVersion);
            catalogId = string.IsNullOrWhiteSpace(catalogId) ? string.Empty : catalogId.Trim();
        }
    }
}
