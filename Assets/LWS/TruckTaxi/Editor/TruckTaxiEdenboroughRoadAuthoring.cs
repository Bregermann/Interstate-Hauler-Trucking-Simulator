using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiEdenboroughRoadAuthoring
    {
        public static LwsRoadGraph CreateGraph()
        {
            var graph = new LwsRoadGraph { graphId = TruckTaxiEdenboroughLayout.WorldId + ".roads" };
            foreach (var node in TruckTaxiEdenboroughLayout.Nodes)
                graph.nodes.Add(new LwsRoadNode { nodeId = node.Key, position = node.Value,
                    regionId = TruckTaxiEdenboroughLayout.RegionAt(node.Value), sceneChunkId = TruckTaxiEdenboroughLayout.SceneName });
            foreach (var road in TruckTaxiEdenboroughLayout.Roads)
            {
                AddEdge(graph, road, false); AddEdge(graph, road, true);
            }
            var validation = graph.Validate();
            if (!validation.IsValid) throw new InvalidOperationException("Edenborough road graph rejected: " + validation);
            return graph;
        }
        private static void AddEdge(LwsRoadGraph graph, TruckTaxiEdenboroughLayout.Road road, bool reverse)
        {
            string id = road.Id + (reverse ? ".reverse" : ".forward");
            var points = reverse ? road.Points.Reverse().ToArray() : road.Points;
            var edge = new LwsRoadEdge { edgeId = id, roadId = id, segmentId = id,
                fromNodeId = reverse ? road.To : road.From, toNodeId = reverse ? road.From : road.To,
                roadClass = road.Kind == "Highway" ? LwsRoadClass.StateHighway : road.Kind == "ServiceRoad" ? LwsRoadClass.DepotAccess : LwsRoadClass.LocalRoad,
                direction = Direction(points[points.Length - 1] - points[0]), oneWay = true,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate, speedLimitMph = road.SpeedMph,
                laneCount = road.Lanes / 2, laneWidthMeters = road.LaneWidth,
                leftShoulderWidthMeters = road.Shoulder, rightShoulderWidthMeters = road.Shoulder,
                stateOrRegionId = TruckTaxiEdenboroughLayout.RegionAt(points[0]), sceneChunkId = TruckTaxiEdenboroughLayout.SceneName };
            for (int lane = 0; lane < edge.laneCount; lane++) edge.laneCenterOffsetsMeters.Add((lane + .5f) * road.LaneWidth);
            float distance = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0) distance += Vector3.Distance(points[i - 1], points[i]);
                Vector3 forward = (i + 1 < points.Length ? points[i + 1] - points[i] : points[i] - points[i - 1]).normalized;
                edge.samples.Add(new LwsRoadSample { roadId = id, segmentId = id, position = points[i],
                    forward = forward, distanceFromStartMeters = distance, roadWidthMeters = road.Width,
                    laneWidthMeters = road.LaneWidth, laneCount = road.Lanes, speedLimitMph = road.SpeedMph, direction = edge.direction });
            }
            edge.distanceMeters = distance; edge.travelCost = distance;
            graph.edges.Add(edge);
        }
        private static LwsRoadDirection Direction(Vector3 direction) => Mathf.Abs(direction.x) > Mathf.Abs(direction.z)
            ? direction.x > 0 ? LwsRoadDirection.Eastbound : LwsRoadDirection.Westbound
            : direction.z > 0 ? LwsRoadDirection.Northbound : LwsRoadDirection.Southbound;
        public static void Bake(Transform root, string folder, Material asphalt)
        {
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            foreach (var group in TruckTaxiEdenboroughLayout.Roads.GroupBy(r => r.Width).OrderBy(g => g.Key))
                TruckTaxiDemoBuilder.BakeRegionalRoads(root, group.Select(r => r.Points).ToList(), group.Key,
                    "Edenborough_" + Mathf.RoundToInt(group.Key * 100), asphalt, folder);
        }
    }
}
