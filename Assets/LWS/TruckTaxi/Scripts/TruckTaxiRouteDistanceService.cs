using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiRouteLeg
    {
        public float Meters { get; }
        public string Source { get; }
        public IReadOnlyList<Vector3> Points { get; }
        public bool Navigable { get; }
        public TruckTaxiRouteLeg(float meters, string source, IList<Vector3> points, bool navigable)
        {
            Meters = meters; Source = source; Navigable = navigable;
            Points = new List<Vector3>(points).AsReadOnly();
        }
    }

    // Read-only route queries reuse LWS's solver without replacing the active GPS route.
    public sealed class TruckTaxiRouteDistanceService
    {
        private readonly LwsRoadGraph graph;
        private readonly LwsRoutePlanner planner = new LwsRoutePlanner(LwsNavigationTuning.Default());
        private readonly Dictionary<string, TruckTaxiRouteLeg> stopRoutes = new Dictionary<string, TruckTaxiRouteLeg>();
        public TruckTaxiRouteDistanceService(LwsRoadGraph graph) { this.graph = graph; }
        public TruckTaxiRouteLeg BetweenStops(TruckTaxiRideLocation from, TruckTaxiRideLocation to)
        {
            string key = from.locationId + ">" + to.locationId;
            if (!stopRoutes.TryGetValue(key, out var leg))
            {
                leg = Measure(from.StopPosition, to.StopPosition);
                stopRoutes.Add(key, leg);
            }
            return leg;
        }
        public TruckTaxiRouteLeg Measure(Vector3 from, Vector3 to)
        {
            if ((from - to).sqrMagnitude < .01f)
                return new TruckTaxiRouteLeg(0, "Road Graph", new[] { from, to }, true);
            if (graph != null)
            {
                var route = planner.PlanRoute(new LwsRouteRequest {
                    requestId = "taxi.offer.preview", useOriginWorldPosition = true, originWorldPosition = from,
                    useDestinationWorldPosition = true, destinationWorldPosition = to, truckRouteRequired = true
                }, graph);
                if (route.succeeded && route.waypoints.Count > 0)
                {
                    var points = new List<Vector3>(route.waypoints);
                    // Include the short access legs from vehicle/bay to graph snap points.
                    if ((points[0] - from).sqrMagnitude > .01f) points.Insert(0, from);
                    if ((points[points.Count - 1] - to).sqrMagnitude > .01f) points.Add(to);
                    float meters = 0;
                    for (int i = 1; i < points.Count; i++) meters += Vector3.Distance(points[i - 1], points[i]);
                    return new TruckTaxiRouteLeg(meters, "Road Graph", points, true);
                }
                return new TruckTaxiRouteLeg(Vector3.Distance(from, to), "Straight-Line Fallback (route unavailable)", new[] { from, to }, false);
            }
            return new TruckTaxiRouteLeg(Vector3.Distance(from, to), "Straight-Line Fallback (no graph)", new[] { from, to }, false);
        }
    }

    public sealed class TruckTaxiRideOffer
    {
        public PassengerProfile Passenger { get; }
        public TruckTaxiRideLocation Pickup { get; }
        public TruckTaxiRideLocation Destination { get; }
        public Vector3 PlayerPosition { get; }
        public TruckTaxiRouteLeg ToPickup { get; }
        public TruckTaxiRouteLeg Trip { get; }
        public long EstimatedFareCents { get; }
        public TruckTaxiRideOffer(PassengerProfile passenger, TruckTaxiRideLocation pickup,
            TruckTaxiRideLocation destination, Vector3 player, TruckTaxiRouteLeg toPickup,
            TruckTaxiRouteLeg trip, TruckTaxiConfiguration config)
        {
            Passenger = passenger; Pickup = pickup; Destination = destination; PlayerPosition = player;
            ToPickup = toPickup; Trip = trip;
            EstimatedFareCents = config.baseFareCents + (long)Math.Round(trip.Meters * config.centsPerMeter);
        }
    }
}
