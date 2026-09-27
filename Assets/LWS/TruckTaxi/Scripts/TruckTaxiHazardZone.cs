using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiExtremeKind { Tornado, Hurricane, Earthquake, Avalanche, Mudslide }

    [Flags]
    public enum TruckTaxiHazardExposure
    {
        None = 0, OpenTerrain = 1, Coastal = 2, MountainSnow = 4, RainSoakedSlope = 8, BuiltArea = 16
    }

    // Author in persistent metadata so an unloaded region retains its hazard definition.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiHazardZone : MonoBehaviour
    {
        public string zoneId;
        public string displayName;
        public string regionId;
        public TruckTaxiExtremeKind kind;
        public TruckTaxiHazardExposure exposure;
        [Min(5)] public float radiusMeters = 80;
        [Tooltip("World positions in travel order; slides need summit, slope and runout.")]
        public Vector3[] path = Array.Empty<Vector3>();
        public GameObject nearbyVisualPrefab;
        [Tooltip("At most 12 explicitly authored lightweight props/signs for earthquake shake.")]
        public Transform[] shakeProps = Array.Empty<Transform>();
        [Tooltip("Optional owned one-shot clip; leave unassigned if none exists.")]
        public AudioClip startClip;

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(zoneId) || string.IsNullOrWhiteSpace(regionId))
            { reason = "Zone and region IDs are required."; return false; }
            if (!float.IsFinite(radiusMeters) || radiusMeters < 5 || radiusMeters > 2000)
            { reason = "Radius must be 5-2000 meters."; return false; }
            if (shakeProps != null && shakeProps.Length > 12)
            { reason = "Earthquake prop budget is twelve."; return false; }
            int required = kind == TruckTaxiExtremeKind.Tornado ? 2 :
                (kind == TruckTaxiExtremeKind.Avalanche || kind == TruckTaxiExtremeKind.Mudslide ? 3 : 1);
            if (path == null || path.Length < required)
            { reason = $"{kind} requires at least {required} authored world points."; return false; }
            for (int i = 0; i < path.Length; i++)
                if (!float.IsFinite(path[i].x) || !float.IsFinite(path[i].y) || !float.IsFinite(path[i].z))
                { reason = "Path contains a non-finite coordinate."; return false; }
            for (int i = 1; i < path.Length; i++)
                if ((path[i] - path[i - 1]).sqrMagnitude < 1)
                { reason = "Consecutive path points must be at least one meter apart."; return false; }
            if (kind == TruckTaxiExtremeKind.Avalanche && (exposure & TruckTaxiHazardExposure.MountainSnow) == 0)
            { reason = "Avalanche requires authored mountain-snow exposure."; return false; }
            if (kind == TruckTaxiExtremeKind.Mudslide && (exposure & TruckTaxiHazardExposure.RainSoakedSlope) == 0)
            { reason = "Mudslide requires an authored rain-soaked slope."; return false; }
            if (kind == TruckTaxiExtremeKind.Hurricane && (exposure & TruckTaxiHazardExposure.Coastal) == 0)
            { reason = "Hurricane requires coastal exposure."; return false; }
            if (kind == TruckTaxiExtremeKind.Tornado && (exposure & TruckTaxiHazardExposure.OpenTerrain) == 0)
            { reason = "Tornado requires an open-terrain path."; return false; }
            if ((kind == TruckTaxiExtremeKind.Avalanche || kind == TruckTaxiExtremeKind.Mudslide) &&
                path[0].y <= path[path.Length - 1].y + 3)
            { reason = "Slide must descend at least three meters toward the runout."; return false; }
            reason = null;
            return true;
        }

        public Vector3 PositionAt(float progress)
        {
            if (path == null || path.Length == 0) return transform.position;
            if (path.Length == 1) return path[0];
            float scaled = Mathf.Clamp01(progress) * (path.Length - 1);
            int from = Mathf.Min(path.Length - 2, Mathf.FloorToInt(scaled));
            return Vector3.Lerp(path[from], path[from + 1], scaled - from);
        }
    }
}
