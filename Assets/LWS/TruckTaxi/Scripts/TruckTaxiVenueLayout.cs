using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Persistent coordinates and road semantics. Streamed scenes contain geometry only.
    public static class TruckTaxiVenueLayout
    {
        public const string StadiumId = "taxi.stadium", ConcertId = "taxi.concert", MountainId = "taxi.mountainpass";
        public const string StadiumScene = "Taxi_SportsStadium", ConcertScene = "Taxi_ConcertVenue", MountainScene = "Taxi_MountainPass";
        public const string StadiumPoiId = "taxi.stadium.poi", ConcertPoiId = "taxi.concert.poi", MountainPoiId = "taxi.mountainpass.poi";
        public const string MountainHazardRoadEdgeId = "taxi.mountainpass.lower>taxi.mountainpass.crossing";
        public static readonly Vector3 StadiumCenter = new Vector3(1080, 0, -720);
        public static readonly Vector3 ConcertCenter = new Vector3(3720, 0, -720);
        public static readonly Vector3 MountainCenter = new Vector3(1180, 0, -1450);
        public static readonly Vector3 MountainSlopeCrossing = new Vector3(1180, 8, -1540);
        public static readonly Bounds StadiumBounds = new Bounds(new Vector3(1080, 0, -720), new Vector3(580, 160, 720));
        public static readonly Bounds ConcertBounds = new Bounds(new Vector3(3720, 0, -720), new Vector3(620, 160, 720));
        public static readonly Bounds MountainBounds = new Bounds(new Vector3(1180, 12, -1450), new Vector3(680, 220, 800));

        public readonly struct Stop
        {
            public readonly string Id, Name, District;
            public readonly Vector3 Position;
            public Stop(string id, string name, string district, Vector3 position)
            { Id = id; Name = name; District = district; Position = position; }
        }
        public static readonly Stop[] StadiumStops = {
            new Stop("taxi.stadium.gate", "Union Field Main Gate", "Stadium", new Vector3(1080,.2f,-550)),
            new Stop("taxi.stadium.west", "Union Field West Taxi Bay", "Stadium", new Vector3(920,.2f,-730)),
            new Stop("taxi.stadium.parking", "Union Field Parking", "Stadium", new Vector3(1210,.2f,-620))
        };
        public static readonly Stop[] ConcertStops = {
            new Stop("taxi.concert.entry", "Neon Yard Entrance", "Concert", new Vector3(3720,.2f,-545)),
            new Stop("taxi.concert.pickup", "Neon Yard Taxi Queue", "Concert", new Vector3(3540,.2f,-730)),
            new Stop("taxi.concert.parking", "Neon Yard Parking", "Concert", new Vector3(3860,.2f,-675))
        };
        public static readonly Stop[] MountainStops = {
            new Stop("taxi.mountainpass.overlook", "Pinecrest Pass Overlook", "Mountain Pass", new Vector3(1180,2,-1310)),
            new Stop("taxi.mountainpass.summit", "Pinecrest Pass Summit", "Mountain Pass", new Vector3(1180,16,-1660))
        };
        public static readonly Vector3[] StadiumRoad = {
            new Vector3(1080,.14f,0), new Vector3(1080,.18f,-330), StadiumStops[0].Position,
            new Vector3(1080,.2f,-730), StadiumStops[1].Position
        };
        public static readonly Vector3[] ConcertRoad = {
            new Vector3(3640,.14f,0), new Vector3(3640,.18f,-320), ConcertStops[0].Position,
            new Vector3(3720,.2f,-730), ConcertStops[1].Position
        };
        public static readonly Vector3[] MountainRoad = {
            new Vector3(1080,.2f,-730), new Vector3(1180,.2f,-900), MountainStops[0].Position,
            new Vector3(1180,8,-1540), MountainStops[1].Position
        };

        public static void AddToGraph(LwsRoadGraph graph)
        {
            if (graph == null || !graph.Validate().IsValid) throw new InvalidOperationException("Existing road graph is invalid.");
            var stadiumJunction = graph.nodes.Find(n => n.nodeId == "taxi.regional.1080");
            var concertJunction = graph.nodes.Find(n => n.nodeId == "taxi.regional.3640");
            if (stadiumJunction == null || concertJunction == null)
                throw new InvalidOperationException("Required regional junctions 1080 and 3640 are absent.");
            AddRoute(graph, StadiumId, StadiumScene, StadiumRoad, stadiumJunction, "highway", "approach", "gate", "inner", "west");
            AddSpur(graph, StadiumId, StadiumScene, "gate", "parking", StadiumStops[2].Position);
            AddRoute(graph, ConcertId, ConcertScene, ConcertRoad, concertJunction, "highway", "approach", "entry", "inner", "pickup");
            AddSpur(graph, ConcertId, ConcertScene, "entry", "parking", ConcertStops[2].Position);
            var stadiumInner = graph.nodes.Find(n => n.nodeId == StadiumId + ".inner");
            AddRoute(graph, MountainId, MountainScene, MountainRoad, stadiumInner,
                "stadium", "approach", "lower", "crossing", "upper");
            var validation = graph.Validate();
            if (!validation.IsValid) throw new InvalidOperationException("Mega World graph invalid: " + validation.Summary);
        }
        private static void AddRoute(LwsRoadGraph graph, string region, string scene, Vector3[] points,
            LwsRoadNode connection, params string[] suffixes)
        {
            var previous = connection;
            for (int i = 1; i < points.Length; i++)
            {
                var node = Node(graph, region, scene, suffixes[i], points[i]);
                Both(graph, previous, node, region, scene, i == 1 ? LwsRoadClass.Ramp : LwsRoadClass.LocalRoad);
                previous = node;
            }
        }
        private static void AddSpur(LwsRoadGraph graph, string region, string scene, string from, string to, Vector3 position)
        {
            var node = Node(graph, region, scene, to, position);
            Both(graph, graph.nodes.Find(n => n.nodeId == region + "." + from), node, region, scene, LwsRoadClass.LocalRoad);
        }
        private static LwsRoadNode Node(LwsRoadGraph graph, string region, string scene, string suffix, Vector3 position)
        {
            string id = region + "." + suffix;
            var existing = graph.nodes.Find(n => n.nodeId == id);
            if (existing != null) return existing;
            var node = new LwsRoadNode { nodeId = id, position = position, regionId = region, sceneChunkId = scene };
            graph.nodes.Add(node); return node;
        }
        private static void Both(LwsRoadGraph graph, LwsRoadNode a, LwsRoadNode b, string region, string scene, LwsRoadClass roadClass)
        {
            if (a == null || b == null) throw new InvalidOperationException("Mega World road endpoint is absent.");
            Edge(graph, a, b, region, scene, roadClass);
            Edge(graph, b, a, region, scene, roadClass);
        }
        private static void Edge(LwsRoadGraph graph, LwsRoadNode a, LwsRoadNode b, string region, string scene, LwsRoadClass roadClass)
        {
            string id = a.nodeId + ">" + b.nodeId;
            if (graph.edges.Exists(e => e.edgeId == id)) return;
            float distance = Vector3.Distance(a.position, b.position), speed = roadClass == LwsRoadClass.Ramp ? 22 : 16;
            Vector3 forward = (b.position - a.position).normalized;
            graph.edges.Add(new LwsRoadEdge {
                edgeId = id, roadId = id, segmentId = id, fromNodeId = a.nodeId, toNodeId = b.nodeId,
                roadClass = roadClass, direction = LwsRoadDirection.Bidirectional,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate, oneWay = true,
                distanceMeters = distance, travelCost = distance, speedLimitMph = speed,
                laneCount = 2, laneWidthMeters = 5, stateOrRegionId = region, sceneChunkId = scene,
                samples = new List<LwsRoadSample> {
                    new LwsRoadSample { roadId = id, segmentId = id, position = a.position, forward = forward,
                        roadWidthMeters = 10, laneCount = 2, laneWidthMeters = 5, speedLimitMph = speed },
                    new LwsRoadSample { roadId = id, segmentId = id, position = b.position, forward = forward,
                        distanceFromStartMeters = distance, roadWidthMeters = 10, laneCount = 2,
                        laneWidthMeters = 5, speedLimitMph = speed }
                }
            });
        }
    }
}
