using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(menuName = "Interstate Hauler/Gameplay/Job Definition", fileName = "IH_JobDefinition")]
    public sealed class LwsJobDefinition : ScriptableObject
    {
        [SerializeField] private int schemaVersion = LwsSaveSchema.CurrentVersion;
        [SerializeField] private string stableJobDefinitionId = "job.unassigned";
        [SerializeField] private string displayName = "Interstate Haul";
        [SerializeField] private bool enabled = true;
        [SerializeField] private LwsDepotDefinition originDepot;
        [SerializeField] private LwsDestinationDefinition destination;
        [SerializeField] private string cargoId = "cargo.general-freight";
        [SerializeField] private string cargoDisplayName = "General Freight";
        [TextArea(3, 8)]
        [SerializeField] private string flavorText;
        [SerializeField] private string requiredTrailerTypeId = "trailer.dry-van";
        [SerializeField] private int cargoWeightLbs = 24000;
        [SerializeField] private float estimatedDistanceMiles = 12f;
        [SerializeField] private int quotedGrossPayCents = 125000;
        [TextArea(2, 6)]
        [SerializeField] private string developerNotes;

        public int SchemaVersion => schemaVersion;
        public string StableJobDefinitionId => stableJobDefinitionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? stableJobDefinitionId : displayName;
        public bool Enabled => enabled;
        public LwsDepotDefinition OriginDepot => originDepot;
        public LwsDestinationDefinition Destination => destination;
        public string OriginDepotId => originDepot != null ? originDepot.StableDepotId : string.Empty;
        public string OriginDepotDisplayName => originDepot != null ? originDepot.DisplayName : string.Empty;
        public string DestinationId => destination != null ? destination.StableDestinationId : string.Empty;
        public string DestinationDisplayName => destination != null ? destination.DisplayName : string.Empty;
        public string CargoId => cargoId;
        public string CargoDisplayName => string.IsNullOrWhiteSpace(cargoDisplayName) ? cargoId : cargoDisplayName;
        public string FlavorText => flavorText;
        public string RequiredTrailerTypeId => requiredTrailerTypeId;
        public int CargoWeightLbs => cargoWeightLbs;
        public float EstimatedDistanceMiles => estimatedDistanceMiles;
        public int QuotedGrossPayCents => quotedGrossPayCents;
        public string DeveloperNotes => developerNotes;

        public void ConfigureForTests(
            string id,
            string label,
            LwsDepotDefinition origin,
            LwsDestinationDefinition target,
            string cargoStableId = "cargo.general-freight",
            string cargoLabel = "General Freight",
            string trailerTypeId = "trailer.dry-van",
            bool isEnabled = true)
        {
            stableJobDefinitionId = id;
            displayName = label;
            originDepot = origin;
            destination = target;
            cargoId = cargoStableId;
            cargoDisplayName = cargoLabel;
            requiredTrailerTypeId = trailerTypeId;
            enabled = isEnabled;
            OnValidate();
        }

        public bool Validate(out string message)
        {
            if (schemaVersion <= 0 || schemaVersion > LwsSaveSchema.CurrentVersion)
            {
                message = $"{name} has unsupported job schema version {schemaVersion}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(stableJobDefinitionId))
            {
                message = $"{name} has no stable job definition ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                message = $"{stableJobDefinitionId} has no job display name.";
                return false;
            }

            if (originDepot == null)
            {
                message = $"{stableJobDefinitionId} has no origin depot.";
                return false;
            }

            if (destination == null)
            {
                message = $"{stableJobDefinitionId} has no destination.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(cargoId) || string.IsNullOrWhiteSpace(cargoDisplayName))
            {
                message = $"{stableJobDefinitionId} has no cargo ID/display name.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(requiredTrailerTypeId))
            {
                message = $"{stableJobDefinitionId} has no required trailer type ID.";
                return false;
            }

            if (cargoWeightLbs < 0)
            {
                message = $"{stableJobDefinitionId} has negative cargo weight.";
                return false;
            }

            if (estimatedDistanceMiles < 0f)
            {
                message = $"{stableJobDefinitionId} has negative estimated distance.";
                return false;
            }

            if (quotedGrossPayCents < 0)
            {
                message = $"{stableJobDefinitionId} has negative quoted gross pay.";
                return false;
            }

            message = $"{stableJobDefinitionId} job definition is valid.";
            return true;
        }

        public LwsJobOffer ToOffer()
        {
            return new LwsJobOffer
            {
                stableOfferId = $"offer.{stableJobDefinitionId}",
                jobDefinitionId = stableJobDefinitionId,
                originDepotId = OriginDepotId,
                originDepotDisplayName = OriginDepotDisplayName,
                destinationId = DestinationId,
                destinationDisplayName = DestinationDisplayName,
                cargoId = cargoId,
                cargoDisplayName = CargoDisplayName,
                flavorText = flavorText ?? string.Empty,
                requiredTrailerTypeId = requiredTrailerTypeId,
                cargoWeightLbs = cargoWeightLbs,
                estimatedDistanceMiles = estimatedDistanceMiles,
                quotedGrossPayCents = quotedGrossPayCents
            };
        }

        public static bool ValidateUniqueIds(IEnumerable<LwsJobDefinition> definitions, out string message)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (LwsJobDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.StableJobDefinitionId))
                {
                    message = $"{definition.name} has an empty job definition ID.";
                    return false;
                }

                if (!seen.Add(definition.StableJobDefinitionId))
                {
                    message = $"Duplicate job definition ID: {definition.StableJobDefinitionId}";
                    return false;
                }
            }

            message = "Job definition IDs are unique.";
            return true;
        }

        private void OnValidate()
        {
            schemaVersion = Mathf.Clamp(schemaVersion, 1, LwsSaveSchema.CurrentVersion);
            stableJobDefinitionId = Normalize(stableJobDefinitionId);
            cargoId = Normalize(cargoId);
            requiredTrailerTypeId = Normalize(requiredTrailerTypeId);
            cargoWeightLbs = Mathf.Max(0, cargoWeightLbs);
            estimatedDistanceMiles = Mathf.Max(0f, estimatedDistanceMiles);
            quotedGrossPayCents = Math.Max(0, quotedGrossPayCents);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
