using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiExtremeFootprintSettings
    {
        [Min(50)] public float tornadoMinimumMeters = 300;
        [Min(50)] public float tornadoMaximumMeters = 800;
        [Min(50)] public float slideMinimumMeters = 150;
        [Min(50)] public float slideMaximumMeters = 500;
        [Min(20)] public float tornadoRadiusMeters = 125;
        [Min(20)] public float slideRadiusMeters = 65;
        [Min(20)] public float quakeRadiusMeters = 250;
        [Min(10)] public float hurricaneRadiusMeters = 1500;
    }

    // A frozen world-space footprint. It never follows the player after creation.
    public sealed class TruckTaxiExtremeFootprint
    {
        public TruckTaxiHazardZone Zone { get; private set; }
        public TruckTaxiExtremeKind Kind { get; private set; }
        public string Id { get; private set; }
        public string RegionId { get; private set; }
        public string DisplayName { get; private set; }
        public Vector3[] Path { get; private set; }
        public Vector3 Source => Path[0];
        public float RadiusMeters { get; private set; }
        public bool IsProcedural => Zone == null;
        public bool IsFlatFallback { get; private set; }
        public GameObject VisualPrefab => Zone != null ? Zone.nearbyVisualPrefab : null;
        public AudioClip StartClip => Zone != null ? Zone.startClip : null;
        public Transform[] ShakeProps => Zone != null ? Zone.shakeProps : null;
        public Vector3 WindDirection => Zone != null ? Zone.transform.right : Vector3.right;
        public Vector3[] DebrisPositions
        {
            get
            {
                if (Kind != TruckTaxiExtremeKind.Avalanche && Kind != TruckTaxiExtremeKind.Mudslide)
                    return Array.Empty<Vector3>();
                Vector3 center = Path[Path.Length / 2];
                return new[] { center + Vector3.left * 3.4f, center + Vector3.right * 3.4f };
            }
        }

        public bool ContainsDanger(Vector3 position, float progress)
        {
            Vector3 delta = position - PositionAt(progress);
            delta.y = 0;
            return delta.sqrMagnitude <= RadiusMeters * RadiusMeters;
        }

        public static TruckTaxiExtremeFootprint FromZone(TruckTaxiHazardZone zone)
        {
            if (zone == null || !zone.IsValid(out _)) return null;
            return new TruckTaxiExtremeFootprint { Zone = zone, Kind = zone.kind, Id = zone.zoneId,
                RegionId = zone.regionId, DisplayName = zone.displayName, Path = (Vector3[])zone.path.Clone(),
                RadiusMeters = zone.radiusMeters };
        }

        public Vector3 PositionAt(float progress)
        {
            if (Path.Length == 1) return Path[0];
            float scaled = Mathf.Clamp01(progress) * (Path.Length - 1);
            int index = Mathf.Min(Path.Length - 2, Mathf.FloorToInt(scaled));
            return Vector3.Lerp(Path[index], Path[index + 1], scaled - index);
        }

        public static TruckTaxiExtremeFootprint FromSaved(TruckTaxiSavedExtreme value, TruckTaxiHazardZone zone,
            Func<LWS.InterstateHauler.LwsWorldPositionD, Vector3> toLocal)
        {
            if (!TruckTaxiSavedExtreme.Valid(value) || toLocal == null) return null;
            var path = new Vector3[value.path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                var point = value.path[i];
                path[i] = toLocal(new LWS.InterstateHauler.LwsWorldPositionD(point.x, point.y, point.z));
            }
            return new TruckTaxiExtremeFootprint { Zone = zone, Kind = value.kind, Id = value.id,
                RegionId = value.regionId, DisplayName = value.displayName, Path = path,
                RadiusMeters = value.radius, IsFlatFallback = value.flatFallback };
        }

        public static bool TryCreate(TruckTaxiExtremeKind kind, string id, string regionId,
            Vector3 player, Vector3 forward, TruckTaxiExtremeFootprintSettings settings,
            Func<Vector3, bool> available, Func<Vector3, Vector3?> ground,
            out TruckTaxiExtremeFootprint footprint, out string reason)
        {
            footprint = null;
            settings = settings ?? new TruckTaxiExtremeFootprintSettings();
            if (string.IsNullOrWhiteSpace(regionId) || !Finite(player))
            { reason = "REGION_UNLOADED"; return false; }
            if (available == null || !available(player))
            { reason = "REGION_UNLOADED"; return false; }
            Vector3 heading = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (heading.sqrMagnitude < .01f) heading = Vector3.forward;
            float minimum = kind == TruckTaxiExtremeKind.Tornado ? settings.tornadoMinimumMeters :
                settings.slideMinimumMeters;
            float maximum = kind == TruckTaxiExtremeKind.Tornado ? settings.tornadoMaximumMeters :
                settings.slideMaximumMeters;
            if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || maximum < minimum)
            { reason = "NO_PROCEDURAL_CANDIDATE"; return false; }
            Vector3[] path;
            bool flat = false;
            if (kind == TruckTaxiExtremeKind.Earthquake || kind == TruckTaxiExtremeKind.Hurricane)
            {
                path = new[] { player };
            }
            else
            {
                Vector3 source = default;
                Vector3 runout = default;
                Vector3 fallbackSource = default;
                Vector3 fallbackRunout = default;
                bool found = false;
                bool haveFlatFallback = false;
                for (int ring = 0; ring < 3 && !found; ring++)
                {
                    float distance = Mathf.Lerp(minimum, maximum, ring / 2f);
                    for (int angle = 0; angle < 8; angle++)
                    {
                        Vector3 direction = Quaternion.Euler(0, angle * 45f, 0) * heading;
                        Vector3 candidate = player + direction * distance;
                        if (!available(candidate)) continue;
                        Vector3? contact = ground != null ? ground(candidate) : candidate;
                        if (!contact.HasValue || !Finite(contact.Value)) continue;
                        if (kind == TruckTaxiExtremeKind.Tornado)
                        { source = contact.Value; found = true; break; }
                        Vector3 proposedRunout = Vector3.Lerp(contact.Value, player, .65f);
                        if (!available(proposedRunout)) continue;
                        Vector3? runoutContact = ground != null ? ground(proposedRunout) : proposedRunout;
                        if (!runoutContact.HasValue || !Finite(runoutContact.Value)) continue;
                        if (contact.Value.y - runoutContact.Value.y >= 3)
                        {
                            source = contact.Value;
                            runout = runoutContact.Value;
                            found = true;
                            break;
                        }
                        if (haveFlatFallback) continue;
                        fallbackSource = contact.Value;
                        fallbackRunout = runoutContact.Value;
                        haveFlatFallback = true;
                    }
                }
                if (!found && haveFlatFallback)
                { source = fallbackSource; runout = fallbackRunout; flat = true; found = true; }
                if (!found) { reason = "NO_PROCEDURAL_CANDIDATE"; return false; }
                Vector3 toward = Vector3.ProjectOnPlane(player - source, Vector3.up).normalized;
                if (kind == TruckTaxiExtremeKind.Tornado)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, toward) * 110;
                    Vector3 mid = source + toward * 180 + side;
                    Vector3 end = source + toward * 360 + side * 1.5f;
                    if (!available(mid)) mid = Vector3.Lerp(source, player, .35f);
                    if (!available(end)) end = Vector3.Lerp(source, player, .6f);
                    path = new[] { source, mid, end };
                }
                else
                {
                    if (flat) source.y += 9;
                    path = new[] { source, Vector3.Lerp(source, runout, .5f), runout };
                }
            }
            float radius = kind == TruckTaxiExtremeKind.Tornado ? settings.tornadoRadiusMeters :
                kind == TruckTaxiExtremeKind.Earthquake ? settings.quakeRadiusMeters :
                kind == TruckTaxiExtremeKind.Hurricane ? settings.hurricaneRadiusMeters : settings.slideRadiusMeters;
            footprint = new TruckTaxiExtremeFootprint { Kind = kind, Id = id, RegionId = regionId,
                DisplayName = regionId, Path = path, RadiusMeters = Mathf.Clamp(radius, 10, 2000),
                IsFlatFallback = flat };
            reason = null;
            return true;
        }

        private static bool Finite(Vector3 point) =>
            float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
    }
}
