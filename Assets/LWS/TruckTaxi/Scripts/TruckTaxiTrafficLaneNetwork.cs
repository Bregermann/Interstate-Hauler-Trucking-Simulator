using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Authored Taxi loop expansion only. Other levels retain their supplied lanes unchanged.
    public static class TruckTaxiTrafficLaneNetwork
    {
        public static LwsTrafficLaneDefinition[] ExpandTaxiBoulevards(LwsTrafficLaneDefinition[] source)
        {
            var lanes = new List<LwsTrafficLaneDefinition>();
            foreach (var lane in source ?? System.Array.Empty<LwsTrafficLaneDefinition>())
            {
                if (lane?.centerline == null || lane.centerline.Length < 5) continue;
                if (lane.roadId == null || !lane.roadId.StartsWith("taxi.city.loop.", System.StringComparison.Ordinal) || lane.laneIndex != 0 || lane.laneWidthMeters < 4.9f)
                { lanes.Add(lane); continue; }
                // Existing loop centres are 5m from street centre on a 20m boulevard.
                // 3.25m / 6.75m lane centres keep both lanes on their own carriageway.
                for (int side = 0; side < 2; side++)
                {
                    var points = new Vector3[lane.centerline.Length];
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 tangent = (lane.centerline[(i + 1) % points.Length] - lane.centerline[(i + points.Length - 1) % points.Length]).normalized;
                        points[i] = lane.centerline[i] + Vector3.Cross(Vector3.up, tangent) * (side == 0 ? -1.75f : 1.75f);
                    }
                    lanes.Add(new LwsTrafficLaneDefinition { laneId = lane.laneId + ".lane." + side, roadId = lane.roadId,
                        segmentId = lane.segmentId, edgeId = lane.edgeId, laneIndex = side, roadClass = lane.roadClass,
                        direction = lane.direction, speedLimitMph = lane.speedLimitMph, laneWidthMeters = 3.5f,
                        lengthMeters = lane.lengthMeters, spawnEnabled = lane.spawnEnabled, centerline = points });
                }
            }
            return lanes.ToArray();
        }

        public static bool AreAdjacent(LwsTrafficLaneDefinition a, LwsTrafficLaneDefinition b) =>
            a != null && b != null && a != b && a.roadId == b.roadId && a.direction == b.direction && Mathf.Abs(a.laneIndex - b.laneIndex) == 1;

        public static bool IsStraight(Vector3 before, Vector3 point, Vector3 after) =>
            Vector3.Dot((point - before).normalized, (after - point).normalized) > .995f;
    }
}
