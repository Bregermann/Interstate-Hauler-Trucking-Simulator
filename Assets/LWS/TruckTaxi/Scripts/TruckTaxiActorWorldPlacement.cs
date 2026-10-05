using UnityEngine;
using LWS.InterstateHauler;

namespace LWS.TruckTaxi
{
    // Short scene transitions only; UTS continues to own general pedestrian navigation.
    public static class TruckTaxiActorWorldPlacement
    {
        private static readonly Collider[] overlaps = new Collider[64];
        private static readonly RaycastHit[] hits = new RaycastHit[64];
        private static readonly Vector3[] supportDirections = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };

        public static bool TryGround(Vector3 candidate, Transform truck, out Vector3 ground)
        {
            ground = default;
            int count = Physics.RaycastNonAlloc(candidate + Vector3.up * 4, Vector3.down, hits, 9,
                ~0, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            float nearest = float.PositiveInfinity;
            RaycastHit best = default;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].transform.IsChildOf(truck) || hits[i].distance >= nearest) continue;
                nearest = hits[i].distance; best = hits[i];
            }
            if (best.collider == null || best.collider.attachedRigidbody != null || best.normal.y < .85f) return false;
            var surface = best.collider.GetComponentInParent<TruckTaxiSurface>();
            if (surface == null || surface.isRoad) return false;
            ground = best.point;
            return true;
        }

        public static bool Clear(Vector3 ground, float radius, float height, Transform truck, Transform actor = null)
        {
            radius = Mathf.Max(.3f, radius);
            height = Mathf.Max(radius * 2, height);
            foreach (var direction in supportDirections)
                if (!TryGround(ground + direction * radius, truck, out var support) || Mathf.Abs(support.y - ground.y) > .35f)
                    return false;
            Vector3 low = ground + Vector3.up * (radius + .08f);
            Vector3 high = ground + Vector3.up * (height - radius + .08f);
            int count = Physics.OverlapCapsuleNonAlloc(low, high, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = overlaps[i];
                if (actor != null && hit.transform.IsChildOf(actor)) continue;
                if (hit.transform.IsChildOf(truck) || hit.bounds.max.y > ground.y + .12f) return false;
            }
            var body = new Bounds(ground + Vector3.up * (height * .5f), new Vector3(radius * 2, height, radius * 2));
            foreach (var renderer in truck.GetComponentsInChildren<Renderer>())
            {
                if (renderer.GetComponentInParent<TruckTaxiPassengerActor>() != null || !renderer.enabled) continue;
                if (renderer.bounds.Intersects(body)) return false;
            }
            return true;
        }

        public static bool ClearOfTrafficLanes(Vector3 point, LwsTrafficLaneDefinition[] lanes, float radius)
        {
            if (lanes == null) return true;
            foreach (var lane in lanes)
            {
                if (lane?.centerline == null) continue;
                float clearance = Mathf.Max(1.5f, lane.laneWidthMeters * .5f) + radius + .75f;
                for (int i = 1; i < lane.centerline.Length; i++)
                {
                    Vector3 a = lane.centerline[i - 1], b = lane.centerline[i];
                    Vector3 delta = Vector3.ProjectOnPlane(b - a, Vector3.up);
                    float t = delta.sqrMagnitude > .001f ? Mathf.Clamp01(Vector3.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
                    Vector3 nearest = Vector3.Lerp(a, b, t);
                    if (Mathf.Abs(point.y - nearest.y) < 3 && Vector3.ProjectOnPlane(point - nearest, Vector3.up).sqrMagnitude < clearance * clearance)
                        return false;
                }
            }
            return true;
        }

        public static bool TryExit(TruckTaxiPassengerActor actor, Transform truck, out Vector3 exit)
        {
            exit = default;
            for (int side = 0; side < 2; side++)
                for (float distance = 2; distance <= 14; distance += .75f)
                {
                    Vector3 candidate = truck.position + truck.right * distance * (side == 0 ? 1 : -1);
                    if (TryGround(candidate, truck, out exit) && Clear(exit, actor.StandingRadius,
                        actor.StandingHeight, truck, actor.transform)) return true;
                }
            return false;
        }

        public static bool ClearSegment(Vector3 from, Vector3 to, float radius, float height, Transform truck, Transform actor = null)
        {
            float distance = Vector3.Distance(from, to);
            if (distance > 60) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Min(.4f, Mathf.Max(.1f, radius))));
            Vector3 previous = from;
            for (int i = 0; i <= steps; i++)
            {
                if (!TryGround(Vector3.Lerp(from, to, (float)i / steps), truck, out var ground) ||
                    Mathf.Abs(ground.y - previous.y) > .35f || !Clear(ground, radius, height, truck, actor)) return false;
                previous = ground;
            }
            return true;
        }
    }
}
