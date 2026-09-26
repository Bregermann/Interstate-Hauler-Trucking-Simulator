using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiNeedsItemsSetup
    {
        private const string ImpactPath = "Assets/WeatherMaker/Prefab/Sound/Water/WeatherMakerWaterSound_Splash.ogg";

        // Parent world builders may call this on an already-open scene. No scene is opened or saved here.
        public static TruckTaxiStorePoint ConfigureStorePoint(TruckTaxiRideLocation location)
        {
            if (location == null || string.IsNullOrWhiteSpace(location.locationId))
                throw new ArgumentException("An authored location with a stable ID is required.", nameof(location));
            var point = location.GetComponent<TruckTaxiStorePoint>();
            if (point == null) point = Undo.AddComponent<TruckTaxiStorePoint>(location.gameObject);
            string id = "TT_STORE_" + location.locationId;
            if (point.stableId != id || point.displayName != location.locationName + " Store" ||
                point.location != location || !Mathf.Approximately(point.stopRadius, location.detectionRadius))
            {
                Undo.RecordObject(point, "Configure Truck Taxi store");
                point.stableId = id;
                point.displayName = location.locationName + " Store";
                point.location = location;
                point.stopRadius = location.detectionRadius;
                EditorUtility.SetDirty(point);
            }
            return point;
        }

        public static void ConfigureExamples(TruckTaxiBootstrap host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var locations = UnityEngine.Object.FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None)
                .Where(value => value.gameObject.scene == host.gameObject.scene).ToArray();
            foreach (string id in new[] { "taxi.stop.07", "taxi.stop.12" })
            {
                var matches = locations.Where(value => value.locationId == id).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Expected one authored store stop: " + id);
                ConfigureStorePoint(matches[0]);
            }
            var needs = host.GetComponent<TruckTaxiDriverNeedsCoordinator>();
            if (needs != null)
            {
                var impact = AssetDatabase.LoadAssetAtPath<AudioClip>(ImpactPath);
                var serialized = new SerializedObject(needs);
                var existingImpact = serialized.FindProperty("containerImpactClip");
                if (existingImpact.objectReferenceValue == null && impact != null)
                {
                    Undo.RecordObject(needs, "Configure Truck Taxi needs audio");
                    existingImpact.objectReferenceValue = impact;
                    serialized.ApplyModifiedProperties();
                }
            }
        }

        [MenuItem("Truck Taxi/Configure Needs Store Examples")]
        private static void ConfigureSelectedScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var host = UnityEngine.Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            ConfigureExamples(host);
        }
    }
}
