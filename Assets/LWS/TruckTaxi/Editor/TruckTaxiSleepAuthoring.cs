using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiSleepAuthoring
    {
        // Parent invokes on already-open generated scenes. Never opens/saves/rebuilds a scene.
        public static TruckTaxiServicePoint ConfigureSleepLocation(TruckTaxiRideLocation location, int costPerHourCents = 0)
        {
            if (location == null || string.IsNullOrWhiteSpace(location.locationId) || location.truckStopPoint == null)
                throw new ArgumentException("Sleep needs a stable location ID and an explicit truck parking anchor.", nameof(location));
            var point = location.GetComponent<TruckTaxiServicePoint>();
            if (point == null) point = Undo.AddComponent<TruckTaxiServicePoint>(location.gameObject);
            int cost = Mathf.Max(0, costPerHourCents);
            if ((point.capabilities & TruckTaxiServiceCapability.Sleep) != 0 && point.sleepCostPerHourCents == cost &&
                point.location != null && !string.IsNullOrWhiteSpace(point.stableId)) return point;
            Undo.RecordObject(point, "Configure Truck Taxi sleep service");
            if (string.IsNullOrWhiteSpace(point.stableId)) point.stableId = "TT_SLEEP_" + location.locationId;
            if (point.location == null) { point.location = location; point.radius = Mathf.Max(3, location.detectionRadius); }
            if (string.IsNullOrWhiteSpace(point.displayName) || point.displayName == "Service stop") point.displayName = location.locationName;
            point.capabilities |= TruckTaxiServiceCapability.Sleep; point.sleepCostPerHourCents = cost;
            EditorUtility.SetDirty(point);
            PrefabUtility.RecordPrefabInstancePropertyModifications(point);
            return point;
        }
        public static int ConfigureHotels(Scene scene)
        {
            int count = 0;
            foreach (var location in UnityEngine.Object.FindObjectsByType<TruckTaxiRideLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (location.gameObject.scene == scene && IsAuthoredHotel(location) &&
                    location.truckStopPoint != null && !string.IsNullOrWhiteSpace(location.locationId))
                { ConfigureSleepLocation(location); count++; }
            return count;
        }

        public static bool IsAuthoredHotel(TruckTaxiRideLocation location)
        {
            return TruckTaxiServicePoint.IsAuthoredHotel(location);
        }
    }
}
