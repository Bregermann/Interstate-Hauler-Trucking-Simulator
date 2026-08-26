using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(menuName = "Interstate Hauler/Gameplay/Destination Definition", fileName = "IH_DestinationDefinition")]
    public sealed class LwsDestinationDefinition : ScriptableObject
    {
        [SerializeField] private int schemaVersion = LwsSaveSchema.CurrentVersion;
        [SerializeField] private string stableDestinationId = "destination.unassigned";
        [SerializeField] private string displayName = "Interstate Destination";
        [SerializeField] private string stableWorldId = LwsSaveSchema.DefaultStableWorldId;
        [SerializeField] private LwsDestinationType destinationType = LwsDestinationType.Generic;
        [SerializeField] private bool enabled = true;
        [SerializeField] private bool hasSemanticGlobalPosition = true;
        [SerializeField] private double globalX;
        [SerializeField] private double globalY;
        [SerializeField] private double globalZ;
        [TextArea(2, 6)]
        [SerializeField] private string developerNotes;

        public int SchemaVersion => schemaVersion;
        public string StableDestinationId => stableDestinationId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? stableDestinationId : displayName;
        public string StableWorldId => stableWorldId;
        public LwsDestinationType DestinationType => destinationType;
        public bool Enabled => enabled;
        public bool HasSemanticGlobalPosition => hasSemanticGlobalPosition;
        public LwsWorldPositionD SemanticGlobalPosition => new LwsWorldPositionD(globalX, globalY, globalZ);
        public string DeveloperNotes => developerNotes;

        public void ConfigureForTests(string id, string label, string worldId, LwsDestinationType type = LwsDestinationType.Generic, bool isEnabled = true)
        {
            stableDestinationId = id;
            displayName = label;
            stableWorldId = worldId;
            destinationType = type;
            enabled = isEnabled;
            OnValidate();
        }

        public bool Validate(out string message)
        {
            if (schemaVersion <= 0 || schemaVersion > LwsSaveSchema.CurrentVersion)
            {
                message = $"{name} has unsupported destination schema version {schemaVersion}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(stableDestinationId))
            {
                message = $"{name} has no stable destination ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                message = $"{stableDestinationId} has no destination display name.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(stableWorldId))
            {
                message = $"{stableDestinationId} has no stable world ID.";
                return false;
            }

            if (!LwsWorldResumeCatalog.TryResolve(stableWorldId, string.Empty, out _))
            {
                message = $"{stableDestinationId} references unknown stable world ID '{stableWorldId}'.";
                return false;
            }

            if (hasSemanticGlobalPosition && (double.IsNaN(globalX) || double.IsNaN(globalY) || double.IsNaN(globalZ)))
            {
                message = $"{stableDestinationId} has an invalid semantic global position.";
                return false;
            }

            message = $"{stableDestinationId} destination definition is valid.";
            return true;
        }

        public static bool ValidateUniqueIds(IEnumerable<LwsDestinationDefinition> definitions, out string message)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (LwsDestinationDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.StableDestinationId))
                {
                    message = $"{definition.name} has an empty destination ID.";
                    return false;
                }

                if (!seen.Add(definition.StableDestinationId))
                {
                    message = $"Duplicate destination ID: {definition.StableDestinationId}";
                    return false;
                }
            }

            message = "Destination IDs are unique.";
            return true;
        }

        private void OnValidate()
        {
            schemaVersion = Mathf.Clamp(schemaVersion, 1, LwsSaveSchema.CurrentVersion);
            stableDestinationId = Normalize(stableDestinationId);
            stableWorldId = Normalize(stableWorldId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
