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
        Food = 512, Drink = 1024, PissJugs = 2048, Store = 4096
    }

    // Capabilities augment existing authored locations; they do not replace their identity.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiServicePoint : MonoBehaviour
    {
        private static readonly HashSet<TruckTaxiServicePoint> points = new HashSet<TruckTaxiServicePoint>();
        public static IReadOnlyCollection<TruckTaxiServicePoint> Points => points;
        public string stableId;
        public string displayName = "Service stop";
        public TruckTaxiServiceCapability capabilities;
        public TruckTaxiRideLocation location;
        public Transform recoveryAnchor;
        [Min(2)] public float radius = 11;
        [Min(.5f)] public float verticalTolerance = 3;
        [Min(0)] public int assistanceBaseCostCents = 5000;
        public Vector3 Position => location != null ? location.StopPosition : transform.position;
        public Vector3 RecoveryPosition => recoveryAnchor != null ? recoveryAnchor.position : Position;
        public bool Supports(TruckTaxiServiceCapability required) => isActiveAndEnabled && (capabilities & required) == required;
        public bool CanUse(Vector3 position, float speed, float maximumSpeed) => isActiveAndEnabled &&
            float.IsFinite(speed) && speed >= 0 && speed <= maximumSpeed &&
            Mathf.Abs(position.y - Position.y) <= verticalTolerance &&
            Vector3.ProjectOnPlane(position - Position, Vector3.up).sqrMagnitude <= radius * radius;
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
                if (point.gameObject.scene != scene || point.GetComponent<TruckTaxiServicePoint>() != null) continue;
                var service = point.gameObject.AddComponent<TruckTaxiServicePoint>();
                service.stableId = point.stableId; service.displayName = point.displayName; service.location = point.location;
                service.radius = point.stopRadius; service.verticalTolerance = point.verticalTolerance;
                service.capabilities = TruckTaxiServiceCapability.Restroom |
                    (point.allowsJugDisposal ? TruckTaxiServiceCapability.DisposeWaste : 0) |
                    (point.allowsCabCleanup ? TruckTaxiServiceCapability.CleanCab : 0);
            }
            foreach (var point in FindObjectsByType<TruckTaxiStorePoint>(FindObjectsSortMode.None))
            {
                if (point.gameObject.scene != scene || point.GetComponent<TruckTaxiServicePoint>() != null) continue;
                var service = point.gameObject.AddComponent<TruckTaxiServicePoint>();
                service.stableId = point.stableId; service.displayName = point.displayName; service.location = point.location;
                service.radius = point.stopRadius; service.verticalTolerance = point.verticalTolerance;
                service.capabilities = TruckTaxiServiceCapability.Store | TruckTaxiServiceCapability.Food |
                    TruckTaxiServiceCapability.Drink | TruckTaxiServiceCapability.PissJugs | TruckTaxiServiceCapability.Restroom |
                    TruckTaxiServiceCapability.DisposeWaste | TruckTaxiServiceCapability.CleanCab;
            }
            foreach (var point in FindObjectsByType<TruckTaxiGasStationPoint>(FindObjectsSortMode.None))
            {
                if (point == null || point.gameObject.scene != scene || point.GetComponent<TruckTaxiServicePoint>() != null) continue;
                var service = point.gameObject.AddComponent<TruckTaxiServicePoint>();
                service.stableId = point.stableId; service.displayName = point.displayName;
                service.radius = point.stoppingRadius; service.verticalTolerance = point.verticalTolerance; service.recoveryAnchor = point.recoveryAnchor;
                service.capabilities = TruckTaxiServiceCapability.Refuel | TruckTaxiServiceCapability.Restroom |
                    TruckTaxiServiceCapability.DisposeWaste | TruckTaxiServiceCapability.CleanCab;
            }
        }
    }
}
