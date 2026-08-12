using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsVehicleRole
    {
        PlayerTractor,
        Trailer,
        AiVehicle,
        StaticValidationVehicle
    }

    [DisallowMultipleComponent]
    public sealed class LwsVehicleIdentity : MonoBehaviour
    {
        [SerializeField] private string vehicleId = "ih.vehicle.unassigned";
        [SerializeField] private string definitionId = "ih.definition.unassigned";
        [SerializeField] private string displayName = "Interstate Hauler Vehicle";
        [SerializeField] private LwsVehicleRole role = LwsVehicleRole.PlayerTractor;
        [SerializeField] private bool activePlayerVehicle;

        public string VehicleId => vehicleId;
        public string DefinitionId => definitionId;
        public string DisplayName => displayName;
        public LwsVehicleRole Role => role;
        public bool ActivePlayerVehicle => activePlayerVehicle;

        public void Configure(string newVehicleId, string newDefinitionId, LwsVehicleRole newRole, string newDisplayName, bool isActivePlayerVehicle)
        {
            vehicleId = string.IsNullOrWhiteSpace(newVehicleId) ? vehicleId : newVehicleId;
            definitionId = string.IsNullOrWhiteSpace(newDefinitionId) ? definitionId : newDefinitionId;
            role = newRole;
            displayName = string.IsNullOrWhiteSpace(newDisplayName) ? displayName : newDisplayName;
            activePlayerVehicle = isActivePlayerVehicle;
        }

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                message = $"{name} has no stable vehicle ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(definitionId))
            {
                message = $"{name} has no definition ID.";
                return false;
            }

            message = $"{name} vehicle identity is valid.";
            return true;
        }

        private void OnValidate()
        {
            vehicleId = Normalize(vehicleId);
            definitionId = Normalize(definitionId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
