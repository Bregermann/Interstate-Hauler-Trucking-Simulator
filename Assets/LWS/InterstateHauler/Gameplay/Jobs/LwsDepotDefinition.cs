using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(menuName = "Interstate Hauler/Gameplay/Depot Definition", fileName = "IH_DepotDefinition")]
    public sealed class LwsDepotDefinition : ScriptableObject
    {
        [SerializeField] private int schemaVersion = LwsSaveSchema.CurrentVersion;
        [SerializeField] private string stableDepotId = "depot.unassigned";
        [SerializeField] private string displayName = "Interstate Depot";
        [SerializeField] private string stableWorldId = LwsSaveSchema.DefaultStableWorldId;
        [SerializeField] private bool hasJobBoard = true;
        [SerializeField] private bool enabled = true;
        [SerializeField] private bool hasSemanticGlobalPosition;
        [SerializeField] private double globalX;
        [SerializeField] private double globalY;
        [SerializeField] private double globalZ;
        [TextArea(2, 6)]
        [SerializeField] private string developerNotes;

        public int SchemaVersion => schemaVersion;
        public string StableDepotId => stableDepotId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? stableDepotId : displayName;
        public string StableWorldId => stableWorldId;
        public bool HasJobBoard => hasJobBoard;
        public bool Enabled => enabled;
        public bool HasSemanticGlobalPosition => hasSemanticGlobalPosition;
        public LwsWorldPositionD SemanticGlobalPosition => new LwsWorldPositionD(globalX, globalY, globalZ);
        public string DeveloperNotes => developerNotes;

        public void ConfigureForTests(string id, string label, string worldId, bool jobBoard = true, bool isEnabled = true)
        {
            stableDepotId = id;
            displayName = label;
            stableWorldId = worldId;
            hasJobBoard = jobBoard;
            enabled = isEnabled;
            OnValidate();
        }

        public bool Validate(out string message)
        {
            if (schemaVersion <= 0 || schemaVersion > LwsSaveSchema.CurrentVersion)
            {
                message = $"{name} has unsupported depot schema version {schemaVersion}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(stableDepotId))
            {
                message = $"{name} has no stable depot ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                message = $"{stableDepotId} has no depot display name.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(stableWorldId))
            {
                message = $"{stableDepotId} has no stable world ID.";
                return false;
            }

            if (!LwsWorldResumeCatalog.TryResolve(stableWorldId, string.Empty, out _))
            {
                message = $"{stableDepotId} references unknown stable world ID '{stableWorldId}'.";
                return false;
            }

            if (hasSemanticGlobalPosition && (double.IsNaN(globalX) || double.IsNaN(globalY) || double.IsNaN(globalZ)))
            {
                message = $"{stableDepotId} has an invalid semantic global position.";
                return false;
            }

            message = $"{stableDepotId} depot definition is valid.";
            return true;
        }

        public static bool ValidateUniqueIds(IEnumerable<LwsDepotDefinition> definitions, out string message)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (LwsDepotDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.StableDepotId))
                {
                    message = $"{definition.name} has an empty depot ID.";
                    return false;
                }

                if (!seen.Add(definition.StableDepotId))
                {
                    message = $"Duplicate depot ID: {definition.StableDepotId}";
                    return false;
                }
            }

            message = "Depot IDs are unique.";
            return true;
        }

        private void OnValidate()
        {
            schemaVersion = Mathf.Clamp(schemaVersion, 1, LwsSaveSchema.CurrentVersion);
            stableDepotId = Normalize(stableDepotId);
            stableWorldId = Normalize(stableWorldId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
