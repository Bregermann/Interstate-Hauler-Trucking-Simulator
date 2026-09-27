using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    // Invoke in the persistent DemoCity scene. This only appends routes; it never rewrites the Town 02 circle.
    public static class TruckTaxiAdditionalBusRoutes
    {
        [MenuItem("Truck Taxi/Regional/Add City Shuttle And Regional Buses")]
        public static void AddRoutes()
        {
            var service = UnityEngine.Object.FindFirstObjectByType<TruckTaxiBusService>();
            if (service == null) throw new InvalidOperationException("Load the persistent Taxi scene with its UTS bus service first.");
            var additions = new[] { City(), Shuttle(), Regional() };
            var existing = service.routes ?? Array.Empty<TruckTaxiBusRoute>();
            var missing = additions.Where(route => !existing.Any(current => current != null && current.id == route.id)).ToArray();
            if (missing.Length == 0) { Debug.Log("Taxi additional bus routes already exist."); return; }
            Undo.RecordObject(service, "Add Taxi city shuttle and regional routes");
            service.routes = existing.Concat(missing).ToArray();
            EditorUtility.SetDirty(service);
            Debug.Log("Added " + missing.Length + " UTS bus routes. Save the persistent scene after inspecting stop placement.");
        }

        private static Vector3 P(float x, float z, float y = .25f) => new Vector3(x, y, z);

        private static TruckTaxiBusRoute City() => new TruckTaxiBusRoute {
            id = "taxi.bus.city01", cruiseMetersPerSecond = 8, capacity = 26,
            fleetColor = new Color(.15f, .65f, .75f),
            points = new[] { P(-320,-320), P(-160,-320), P(0,-320), P(160,-320), P(320,-320),
                P(320,-160), P(320,0), P(320,160), P(320,320), P(160,320), P(0,320),
                P(-160,320), P(-320,320), P(-320,160), P(-320,0), P(-320,-160) },
            stops = new[] { P(0,-320), P(320,0), P(0,320), P(-320,0) }
        };

        private static TruckTaxiBusRoute Shuttle() => new TruckTaxiBusRoute {
            id = "taxi.bus.shuttle02", cruiseMetersPerSecond = 7, capacity = 12,
            fleetColor = new Color(.9f, .65f, .15f),
            points = new[] { P(3840,-160), P(4000,-160), P(4160,-160), P(4160,0),
                P(4160,160), P(4000,160), P(3840,160), P(3840,0) },
            stops = new[] { P(4000,-160), P(4160,0), P(4000,160), P(3840,0) }
        };

        private static TruckTaxiBusRoute Regional()
        {
            var points = new[] { P(0,0), P(350,0), P(700,-7), P(1100,-7), P(1460,-7),
                P(1780,-7,8.1f), P(2220,-7,8.1f), P(2540,-7), P(2750,-7),
                P(2800,-45), P(2850,-7), P(3100,-7), P(3600,-7), P(4000,-7),
                P(4000,7), P(3600,7), P(3100,7), P(2850,7), P(2540,7),
                P(2220,7,8.1f), P(1780,7,8.1f), P(1460,7), P(1100,7), P(700,7),
                P(350,7), P(0,7) };
            return new TruckTaxiBusRoute { id = "taxi.bus.regional", points = points,
                stops = new[] { points[0], points[9], points[13], points[17] },
                cruiseMetersPerSecond = 10, capacity = 32, fleetColor = new Color(.85f, .2f, .3f) };
        }
    }
}
