using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(menuName = "Interstate Hauler/Vehicles/Truck Definition", fileName = "IH_TruckDefinition")]
    public sealed class LwsTruckDefinition : ScriptableObject
    {
        [SerializeField] private string stableId = "ih.truck.unassigned";
        [SerializeField] private string displayName = "Interstate Hauler Truck";
        [SerializeField] private string category = "tractor";
        [SerializeField] private string manufacturerFamily = "validation";
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject sourceVendorPrefab;
        [SerializeField] private GameObject validationTrailerPrefab;
        [SerializeField] private LwsTruckControlCapabilities controlCapabilities = LwsTruckControlCapabilities.NwhSemiDevelopmentDefault();
        [SerializeField] private LwsTruckDashboardDefinition dashboardDefinition;
        [SerializeField] private LwsTruckDashboardCapabilities dashboardCapabilities = LwsTruckDashboardCapabilities.NwhSemiDevelopmentDefault();
        [SerializeField] private string notes;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public string Category => category;
        public string ManufacturerFamily => manufacturerFamily;
        public GameObject PlayerPrefab => playerPrefab;
        public GameObject SourceVendorPrefab => sourceVendorPrefab;
        public GameObject ValidationTrailerPrefab => validationTrailerPrefab;
        public LwsTruckControlCapabilities ControlCapabilities => controlCapabilities;
        public LwsTruckDashboardDefinition DashboardDefinition => dashboardDefinition;
        public LwsTruckDashboardCapabilities DashboardCapabilities => dashboardCapabilities;
        public string Notes => notes;

        public void ConfigureForTests(string newStableId, GameObject newPlayerPrefab)
        {
            stableId = newStableId;
            playerPrefab = newPlayerPrefab;
        }

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                message = $"{name} has no stable truck definition ID.";
                return false;
            }

            if (playerPrefab == null)
            {
                message = $"{stableId} has no player prefab assigned.";
                return false;
            }

            if (!controlCapabilities.Validate(out message))
            {
                message = $"{stableId} has invalid control capabilities: {message}";
                return false;
            }

            if (dashboardDefinition != null && !dashboardDefinition.Validate(out message))
            {
                message = $"{stableId} has invalid dashboard definition: {message}";
                return false;
            }

            message = $"{stableId} truck definition is valid.";
            return true;
        }

        public static bool ValidateUniqueIds(IEnumerable<LwsTruckDefinition> definitions, out string message)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (LwsTruckDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.StableId))
                {
                    message = $"{definition.name} has an empty truck definition ID.";
                    return false;
                }

                if (!seen.Add(definition.StableId))
                {
                    message = $"Duplicate truck definition ID: {definition.StableId}";
                    return false;
                }
            }

            message = "Truck definition IDs are unique.";
            return true;
        }

        private void OnValidate()
        {
            stableId = Normalize(stableId);
            category = Normalize(category);
            manufacturerFamily = Normalize(manufacturerFamily);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
