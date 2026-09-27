using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiTrafficPopulation
    {
        public sealed class Car
        {
            public string Id;
            public int VehicleType;
            public TruckTaxiDriverPersonality Personality;
            public int AppearanceSeed;
            public string RoadId;
            public string LaneId;
            public int Lane;
            public int Point;
            public float SegmentProgress;
            public float Speed;
            public float DesiredSpeed;
            public int Direction;
            public bool MissionReserved;
            public GameObject Actor;
            public bool FullPhysics;
        }

        private readonly List<Car> cars = new List<Car>();
        private readonly LwsTrafficLaneDefinition[] lanes;
        public IReadOnlyList<Car> Cars => cars;
        public int Count => cars.Count;

        public TruckTaxiTrafficPopulation(IReadOnlyList<LwsTrafficLaneDefinition> laneDefinitions, int count, int prefabCount, int firstSerial)
        {
            lanes = new LwsTrafficLaneDefinition[laneDefinitions.Count];
            for (int i = 0; i < lanes.Length; i++) lanes[i] = laneDefinitions[i];
            if (lanes.Length == 0 || prefabCount <= 0) return;
            var spawnLanes = new List<int>();
            for (int i = 0; i < lanes.Length; i++)
                if (lanes[i] != null && lanes[i].spawnEnabled && lanes[i].centerline != null &&
                    lanes[i].centerline.Length >= 5) spawnLanes.Add(i);
            if (spawnLanes.Count == 0) return;
            for (int i = 0; i < count; i++)
            {
                int laneIndex = spawnLanes[i % spawnLanes.Count];
                var lane = lanes[laneIndex];
                uint hash = Hash((uint)(firstSerial + i + 1));
                string id = "taxi.traffic." + (firstSerial + i + 1);
                int appearanceSeed = (int)(hash & 0x7fffffff);
                cars.Add(new Car {
                    Id = id, VehicleType = appearanceSeed % prefabCount,
                    AppearanceSeed = appearanceSeed, Personality = PersonalityForId(id),
                    RoadId = lane.roadId, LaneId = lane.laneId, Lane = laneIndex,
                    Point = 2 + (int)((hash / 5) % (uint)(lane.centerline.Length - 4)),
                    Speed = Mathf.Max(2, lane.speedLimitMph * .44704f * .55f),
                    DesiredSpeed = Mathf.Max(2, lane.speedLimitMph * .44704f * .7f), Direction = 1
                });
            }
        }

        public Vector3 Position(Car car)
        {
            var points = lanes[car.Lane].centerline;
            int next = car.Point + 1 < points.Length - 1 ? car.Point + 1 : 1;
            return Vector3.Lerp(points[car.Point], points[next], car.SegmentProgress);
        }

        public Vector3 Forward(Car car)
        {
            var points = lanes[car.Lane].centerline;
            int next = car.Point + 1 < points.Length - 1 ? car.Point + 1 : 1;
            Vector3 direction = points[next] - points[car.Point];
            return direction.sqrMagnitude > .001f ? direction.normalized : Vector3.forward;
        }

        public void Advance(Car car, float seconds)
        {
            if (car.FullPhysics || seconds <= 0) return;
            var points = lanes[car.Lane].centerline;
            float distance = car.Speed * seconds;
            int guard = points.Length * 2;
            while (distance > 0 && guard-- > 0)
            {
                int next = car.Point + 1 < points.Length - 1 ? car.Point + 1 : 1;
                float length = Vector3.Distance(points[car.Point], points[next]);
                if (length < .01f) { car.Point = next; car.SegmentProgress = 0; continue; }
                float remaining = length * (1 - car.SegmentProgress);
                if (distance < remaining) { car.SegmentProgress += distance / length; break; }
                distance -= remaining; car.Point = next; car.SegmentProgress = 0;
            }
        }

        public void Capture(Car car, Vector3 position, float speed)
        {
            var points = lanes[car.Lane].centerline;
            car.RoadId = lanes[car.Lane].roadId;
            car.LaneId = lanes[car.Lane].laneId;
            float best = float.PositiveInfinity;
            for (int i = 1; i < points.Length - 1; i++)
            {
                int next = i + 1 < points.Length - 1 ? i + 1 : 1;
                Vector3 segment = points[next] - points[i];
                float progress = Mathf.Clamp01(Vector3.Dot(position - points[i], segment) / Mathf.Max(.001f, segment.sqrMagnitude));
                float sqr = (position - Vector3.Lerp(points[i], points[next], progress)).sqrMagnitude;
                if (sqr >= best) continue;
                best = sqr; car.Point = i; car.SegmentProgress = progress;
            }
            car.Speed = Mathf.Clamp(speed, 2, car.DesiredSpeed * 1.5f);
        }

        // Reassign only distant records; a live UTS actor remains authoritative.
        public bool RetargetUnmaterialized(Car car, Vector3 destination)
        {
            if (car == null || car.Actor != null) return false;
            int bestLane = -1, bestPoint = -1;
            float best = float.PositiveInfinity;
            for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
            {
                var lane = lanes[laneIndex];
                if (lane == null || !lane.spawnEnabled || lane.centerline == null) continue;
                for (int point = 2; point < lane.centerline.Length - 2; point++)
                {
                    float sqr = (lane.centerline[point] - destination).sqrMagnitude;
                    if (sqr >= best) continue;
                    best = sqr; bestLane = laneIndex; bestPoint = point;
                }
            }
            if (bestLane < 0) return false;
            car.Lane = bestLane; car.Point = bestPoint; car.SegmentProgress = 0;
            car.RoadId = lanes[bestLane].roadId; car.LaneId = lanes[bestLane].laneId;
            car.Speed = Mathf.Max(2, lanes[bestLane].speedLimitMph * .44704f * .6f);
            car.DesiredSpeed = Mathf.Max(car.Speed, lanes[bestLane].speedLimitMph * .44704f * .8f);
            return true;
        }

        public static bool WantFull(float distance, bool alreadyFull, float radius, float hysteresis) =>
            distance <= radius + (alreadyFull ? hysteresis : 0);

        public static bool WantVisible(float distance, bool alreadyVisible, float radius, float hysteresis) =>
            distance <= radius + (alreadyVisible ? hysteresis : 0);

        public static bool CanMaterialize(int active, int full, int maxFull, int maxVisible, bool requestedFull) =>
            active < Mathf.Max(0, maxFull) + Mathf.Max(0, maxVisible) &&
            (!requestedFull || full < maxFull);

        public static TruckTaxiDriverPersonality PersonalityForId(string id)
        {
            uint hash = 2166136261;
            foreach (char c in id) hash = (hash ^ c) * 16777619;
            int pick = (int)(hash % 100);
            return pick < 20 ? TruckTaxiDriverPersonality.Cautious : pick < 65 ? TruckTaxiDriverPersonality.Normal :
                pick < 84 ? TruckTaxiDriverPersonality.Impatient : pick < 96 ? TruckTaxiDriverPersonality.Aggressive :
                TruckTaxiDriverPersonality.Reckless;
        }

        private static uint Hash(uint value)
        {
            value ^= value >> 16; value *= 0x7feb352d; value ^= value >> 15;
            value *= 0x846ca68b; return value ^ (value >> 16);
        }
    }
}
