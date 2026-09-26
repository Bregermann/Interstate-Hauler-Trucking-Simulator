using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiVehicleObjectiveSetup
    {
        private const string Folder = "Assets/LWS/TruckTaxi/ScriptableObjects/Requests";
        private static readonly TaxiRequestType[] Types = {
            TaxiRequestType.FollowVehicle, TaxiRequestType.RamTargetVehicle,
            TaxiRequestType.LoseVehicle, TaxiRequestType.BlockVehicle,
            TaxiRequestType.ReachLocationBeforeVehicle, TaxiRequestType.DestroyVehicle,
            TaxiRequestType.CollectDroppedObjects
        };

        [MenuItem("Truck Taxi/Vehicle Objectives/Create Missing Request Assets")]
        public static void CreateMissing()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                throw new InvalidOperationException("Truck Taxi request folder is missing: " + Folder);
            foreach (var type in Types)
            {
                string path = Folder + "/" + type + ".asset";
                if (AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(path) != null) continue;
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    throw new InvalidOperationException("Unexpected asset at " + path);
                var definition = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
                definition.requestType = type;
                definition.objectiveId = "taxi.objective." + type;
                definition.description = Description(type);
                definition.timer = type == TaxiRequestType.CollectDroppedObjects ? 120 : 90;
                definition.target = type == TaxiRequestType.FollowVehicle ? 12 :
                    type == TaxiRequestType.DestroyVehicle || type == TaxiRequestType.CollectDroppedObjects ? 3 : 1;
                // Directed pursuit is unavailable; race reads an existing UTS waypoint.
                definition.enabledForSelection = type != TaxiRequestType.LoseVehicle;
                AssetDatabase.CreateAsset(definition, path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Truck Taxi vehicle request assets created where absent. Authored assets were unchanged.");
        }

        [MenuItem("Truck Taxi/Vehicle Objectives/Add Functional Requests To Selected Passenger")]
        public static void AddToSelectedPassenger()
        {
            var profile = Selection.activeObject as PassengerProfile;
            if (profile == null)
                throw new InvalidOperationException("Select one PassengerProfile asset first.");
            var additions = Types
                .Where(type => type != TaxiRequestType.LoseVehicle)
                .Select(type => AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(
                    Folder + "/" + type + ".asset"))
                .ToArray();
            if (additions.Any(definition => definition == null))
                throw new InvalidOperationException("Run Create Missing Request Assets first.");
            Undo.RecordObject(profile, "Add Truck Taxi vehicle objectives");
            profile.possibleRequests = (profile.possibleRequests ??
                Array.Empty<PassengerRequestDefinition>()).Concat(additions).Distinct().ToArray();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log("Added functional vehicle requests to " + profile.passengerId +
                ". Runtime capability checks still gate actual offers.");
        }

        [MenuItem("Truck Taxi/Vehicle Objectives/Validate Request Assets")]
        public static void Validate()
        {
            int errors = 0;
            foreach (var type in Types)
            {
                var definition = AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(
                    Folder + "/" + type + ".asset");
                if (definition == null || definition.requestType != type ||
                    definition.target <= 0 || definition.timer <= 0)
                {
                    Debug.LogError("Missing or invalid vehicle request asset: " + type);
                    errors++;
                }
                else if (type == TaxiRequestType.LoseVehicle &&
                    definition.enabledForSelection)
                {
                    Debug.LogError(type + " is enabled without a verified UTS adapter hook.");
                    errors++;
                }
            }
            Debug.Log("Truck Taxi vehicle objective asset validation: " +
                (errors == 0 ? "passed" : errors + " error(s)"));
        }

        [MenuItem("Truck Taxi/Vehicle Objectives/Force Selected Request In Play Mode")]
        public static void ForceSelectedInPlayMode()
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("Enter Play Mode first.");
            var definition = Selection.activeObject as PassengerRequestDefinition;
            var session = TruckTaxiBootstrap.Instance?.Session;
            if (definition == null || session == null ||
                !TruckTaxiVehicleObjectives.IsVehicleRequest(definition.requestType))
                throw new InvalidOperationException("Select a vehicle request asset in a running Truck Taxi scene.");
            if (!session.GenerateRequest(definition))
                Debug.LogWarning("Vehicle request rejected: " +
                    (session.CanAssign(definition, out var reason) ? "ride state or active request limit" : reason));
            else
            {
                var request = session.Requests.Last();
                var coordinator = TruckTaxiBootstrap.Instance.GetComponent<TruckTaxiVehicleObjectiveCoordinator>();
                string status = coordinator != null ? coordinator.Status(request) : request.Description;
                if (request.State == TaxiRequestState.Failed)
                    Debug.LogWarning("Vehicle request failed at assignment: " + status);
                else Debug.Log("Vehicle request assigned: " + status);
            }
        }

        private static string Description(TaxiRequestType type)
        {
            switch (type)
            {
                case TaxiRequestType.FollowVehicle: return "Follow the assigned UTS vehicle at a safe distance";
                case TaxiRequestType.RamTargetVehicle: return "Ram the assigned UTS vehicle";
                case TaxiRequestType.LoseVehicle: return "Escape the assigned pursuing UTS vehicle";
                case TaxiRequestType.BlockVehicle: return "Obstruct and stop the assigned UTS vehicle";
                case TaxiRequestType.ReachLocationBeforeVehicle: return "Reach the same goal before the assigned UTS vehicle";
                case TaxiRequestType.DestroyVehicle: return "Disable the assigned mission vehicle with repeated rams";
                default: return "Collect the physical objects dropped by the assigned vehicle";
            }
        }
    }
}
