using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiGasStationPoint : MonoBehaviour
    {
        private static readonly HashSet<TruckTaxiGasStationPoint> points = new HashSet<TruckTaxiGasStationPoint>();
        public static IReadOnlyCollection<TruckTaxiGasStationPoint> Points => points;
        public string stableId;
        public string displayName = "GAS STATION";
        [Min(2)] public float stoppingRadius = 7;
        [Min(.5f)] public float verticalTolerance = 3;
        [Min(1)] public int centsPerLiter = 150;
        [Min(.1f)] public float litersPerSecond = 8;
        public Transform recoveryAnchor;
        private TruckTaxiServicePoint service;
        public Vector3 Position
        {
            get
            {
                if (service == null) service = GetComponent<TruckTaxiServicePoint>();
                return service != null && service.location != null ? service.Position : transform.position;
            }
        }
        public Vector3 RecoveryPosition => recoveryAnchor != null ? recoveryAnchor.position : Position;
        private void OnEnable() => points.Add(this);
        private void OnDisable() => points.Remove(this);
        public bool CanRefuel(Vector3 position, float speed) => isActiveAndEnabled && float.IsFinite(speed) && speed>=0 && speed <= TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond &&
            Mathf.Abs(position.y-Position.y)<=verticalTolerance &&
            Vector3.ProjectOnPlane(position - Position, Vector3.up).sqrMagnitude <= stoppingRadius * stoppingRadius;
        public static TruckTaxiGasStationPoint Nearest(Vector3 position)
        {
            TruckTaxiGasStationPoint best = null; float distance = float.PositiveInfinity;
            foreach (var point in points)
            {
                if (point == null || !point.isActiveAndEnabled) continue;
                float candidate = (point.Position - position).sqrMagnitude;
                if (candidate < distance || (Mathf.Approximately(candidate, distance) &&
                    string.CompareOrdinal(point.stableId, best?.stableId) < 0)) { best = point; distance = candidate; }
            }
            return best;
        }
    }
}
