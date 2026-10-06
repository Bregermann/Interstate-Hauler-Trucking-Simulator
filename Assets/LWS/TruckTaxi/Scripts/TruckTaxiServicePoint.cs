using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Flags]
    public enum TruckTaxiServiceCapability
    {
        None = 0, Refuel = 1, RepairEngine = 2, RepairTransmission = 4,
        RepairGeneralDamage = 8, RepairTires = 16, RecoverVehicle = 32,
        Restroom = 64, DisposeWaste = 128, CleanCab = 256,
        Food = 512, Drink = 1024, PissJugs = 2048, Store = 4096, Sleep = 8192
    }

    // Capabilities augment existing authored locations; they do not replace their identity.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiServicePoint : MonoBehaviour
    {
        public const float ServiceStopSpeedMetersPerSecond = 0.44704f;
        private static readonly HashSet<TruckTaxiServicePoint> points = new HashSet<TruckTaxiServicePoint>();
        public static IReadOnlyCollection<TruckTaxiServicePoint> Points => points;
        public string stableId;
        public string displayName = "Service stop";
        public TruckTaxiServiceCapability capabilities;
        public TruckTaxiRideLocation location;
        public Transform recoveryAnchor;
        [Tooltip("Generate simple Truck Taxi validation geometry. Disable when this point has authored facility art/interactables.")]
        public bool generateValidationFacility = true;
        [Min(2)] public float radius = 18;
        [Min(.5f)] public float verticalTolerance = 3;
        [Min(0)] public int assistanceBaseCostCents = 5000;
        [Min(0)] public int sleepCostPerHourCents;
        public long SleepCostCents(int hours) => TruckTaxiFatigueRules.IsSleepDuration(hours) ? (long)Mathf.Max(0, sleepCostPerHourCents) * hours : 0;
        public Vector3 Position => location != null ? location.StopPosition : transform.position;
        public Vector3 RecoveryPosition => recoveryAnchor != null ? recoveryAnchor.position : Position;
        public float InteractionRadius => Supports(TruckTaxiServiceCapability.Store) ? Mathf.Max(18, radius) : radius;
        public bool Supports(TruckTaxiServiceCapability required) => isActiveAndEnabled && (capabilities & required) == required;
        public bool CanUse(Vector3 position, float speed, float maximumSpeed) => isActiveAndEnabled &&
            float.IsFinite(speed) && speed >= 0 && speed <= Mathf.Max(ServiceStopSpeedMetersPerSecond, maximumSpeed) &&
            Mathf.Abs(position.y - Position.y) <= verticalTolerance &&
            Vector3.ProjectOnPlane(position - Position, Vector3.up).sqrMagnitude <= InteractionRadius * InteractionRadius;
        private void OnEnable() => points.Add(this);
        private void OnDisable() => points.Remove(this);
        public static TruckTaxiServicePoint Nearest(Vector3 position, TruckTaxiServiceCapability required,
            UnityEngine.SceneManagement.Scene scene, bool matchAny = false)
        {
            TruckTaxiServicePoint best = null; float distance = float.PositiveInfinity;
            foreach (var point in points)
            {
                if (point == null || !point.isActiveAndEnabled || point.gameObject.scene != scene ||
                    (matchAny ? (point.capabilities & required) == 0 : !point.Supports(required)) || string.IsNullOrWhiteSpace(point.stableId)) continue;
                float candidate = (point.Position - position).sqrMagnitude;
                if (candidate < distance || (Mathf.Approximately(candidate, distance) && string.CompareOrdinal(point.stableId, best?.stableId) < 0))
                { best = point; distance = candidate; }
            }
            return best;
        }

        public static void BindExistingPoints(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var point in FindObjectsByType<TruckTaxiBathroomPoint>(FindObjectsSortMode.None))
            {
                if (point.gameObject.scene != scene) continue;
                var service = BindLocationService(point.gameObject, point.stableId, point.displayName, point.location, point.stopRadius, point.verticalTolerance);
                service.capabilities |= TruckTaxiServiceCapability.Restroom |
                    (point.allowsJugDisposal ? TruckTaxiServiceCapability.DisposeWaste : 0) |
                    (point.allowsCabCleanup ? TruckTaxiServiceCapability.CleanCab : 0);
            }
            foreach (var point in FindObjectsByType<TruckTaxiStorePoint>(FindObjectsSortMode.None))
            {
                if (point.gameObject.scene != scene) continue;
                var service = BindLocationService(point.gameObject, point.stableId, point.displayName, point.location, point.stopRadius, point.verticalTolerance);
                service.capabilities |= TruckTaxiServiceCapability.Store | TruckTaxiServiceCapability.Food |
                    TruckTaxiServiceCapability.Drink | TruckTaxiServiceCapability.PissJugs | TruckTaxiServiceCapability.Restroom |
                    TruckTaxiServiceCapability.DisposeWaste | TruckTaxiServiceCapability.CleanCab;
            }
            foreach (var point in FindObjectsByType<TruckTaxiGasStationPoint>(FindObjectsSortMode.None))
            {
                if (point == null || point.gameObject.scene != scene) continue;
                var service = BindLocationService(point.gameObject, point.stableId, point.displayName, null, point.stoppingRadius, point.verticalTolerance);
                var store = point.GetComponent<TruckTaxiStorePoint>();
                if (store == null) store = point.gameObject.AddComponent<TruckTaxiStorePoint>();
                if (store.stableId == "TT_STORE_") store.stableId = point.stableId + ".store";
                if (store.displayName == "Truck stop store") store.displayName = point.displayName + " store";
                store.stopRadius = Mathf.Max(store.stopRadius, point.stoppingRadius);
                if (service.recoveryAnchor == null) service.recoveryAnchor = point.recoveryAnchor;
                service.capabilities |= TruckTaxiServiceCapability.Refuel | TruckTaxiServiceCapability.Restroom |
                    TruckTaxiServiceCapability.DisposeWaste | TruckTaxiServiceCapability.CleanCab |
                    TruckTaxiServiceCapability.Store | TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink;
            }
            BindRestaurantLocations(scene);
            BindSleepLocations(scene);
        }
        private static void BindRestaurantLocations(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var location in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
            {
                if (location.gameObject.scene != scene || location.locationType != TaxiLocationType.Restaurant ||
                    location.truckStopPoint == null || string.IsNullOrWhiteSpace(location.locationId)) continue;
                var service = BindLocationService(location.gameObject, "TT_FOOD_" + location.locationId,
                    location.locationName, location, Mathf.Max(18, location.detectionRadius), 3);
                service.capabilities |= TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Restroom;
            }
        }
        private static TruckTaxiServicePoint BindLocationService(GameObject owner, string id, string label,
            TruckTaxiRideLocation location, float radius, float tolerance)
        {
            var service = owner.GetComponent<TruckTaxiServicePoint>();
            if (service == null)
            {
                service = owner.AddComponent<TruckTaxiServicePoint>();
                service.radius = radius; service.verticalTolerance = tolerance;
            }
            if (string.IsNullOrWhiteSpace(service.stableId)) service.stableId = id;
            if (service.location == null) service.location = location;
            if (string.IsNullOrWhiteSpace(service.displayName) || service.displayName == "Service stop") service.displayName = label;
            return service;
        }

        public static bool IsAuthoredHotel(TruckTaxiRideLocation location)
        {
            if (location == null) return false;
            string id = location.locationId ?? string.Empty;
            // The original validation builder cycled locationType by index. Its stable IDs,
            // not that legacy enum, identify the four actually authored hotels/inns.
            if (id.StartsWith("taxi.stop.", System.StringComparison.Ordinal) ||
                id.StartsWith("taxi.town02.stop.", System.StringComparison.Ordinal))
                return id == "taxi.stop.08" || id == "taxi.stop.15" ||
                    id == "taxi.town02.stop.08" || id == "taxi.town02.stop.15";
            return location.locationType == TaxiLocationType.Hotel;
        }

        // Authored hotels with an explicit parking anchor get the coordinator sleep interaction.
        // Call again after a streamed region loads; no duplicate components or competing identities.
        public static void BindSleepLocations(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var location in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
            {
                if (location.gameObject.scene != scene || !IsAuthoredHotel(location) ||
                    location.truckStopPoint == null || string.IsNullOrWhiteSpace(location.locationId)) continue;
                var point = location.GetComponent<TruckTaxiServicePoint>();
                if (point == null) point = location.gameObject.AddComponent<TruckTaxiServicePoint>();
                if (string.IsNullOrWhiteSpace(point.stableId)) point.stableId = "TT_SLEEP_" + location.locationId;
                if (point.location == null) { point.location = location; point.radius = Mathf.Max(3, location.detectionRadius); }
                if (string.IsNullOrWhiteSpace(point.displayName) || point.displayName == "Service stop") point.displayName = location.locationName;
                point.capabilities |= TruckTaxiServiceCapability.Sleep;
            }
        }
    }
}
