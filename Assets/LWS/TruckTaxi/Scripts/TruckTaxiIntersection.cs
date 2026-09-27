using System.Collections;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Observation of UTS signal authority, not a second signal controller.
    public sealed class TruckTaxiIntersection : MonoBehaviour
    {
        public string intersectionId;
        public Component signalSystem;
        public Component[] movementGates;
        public Component crosswalkPath;
        private Component cachedFirst;
        private PropertyInfo vehiclePermission, pedestrianPermission;
        private Gate[] cachedGates;
        private struct Gate
        {
            public Component Component;
            public BoxCollider Box;
            public PropertyInfo Permission;
        }
        public void Configure(string id, Component signal, Component[] gates, Component path)
        {
            intersectionId = id;
            signalSystem = signal;
            movementGates = gates;
            crosswalkPath = path;
            cachedFirst = null; cachedGates = null;
        }
        public bool IsConfigured => signalSystem != null && movementGates != null && movementGates.Length > 0 && crosswalkPath != null;
        public bool VehicleMayProceed => ReadGate("ForwardMoveState");
        public bool PedestrianMayCross => ReadGate("PeopleMoveState");

        private bool ReadGate(string property)
        {
            if (!IsConfigured || movementGates[0] == null) return false;
            if (cachedFirst != movementGates[0])
            {
                cachedFirst = movementGates[0];
                vehiclePermission = cachedFirst.GetType().GetProperty("ForwardMoveState");
                pedestrianPermission = cachedFirst.GetType().GetProperty("PeopleMoveState");
            }
            PropertyInfo member = property == "ForwardMoveState" ? vehiclePermission : pedestrianPermission;
            return member != null && member.PropertyType == typeof(bool) && (bool)member.GetValue(movementGates[0]);
        }

        public bool TryGetStopDistance(Vector3 front, Vector3 forward, float reach, out float distance)
        {
            distance = float.PositiveInfinity;
            if (!IsConfigured) return false;
            bool occupied = TruckTaxiBootstrap.Instance?.pedestrians?.LegalCrossingCount(this) > 0;
            if (cachedGates == null)
            {
                cachedGates = new Gate[movementGates.Length];
                for (int i = 0; i < movementGates.Length; i++)
                {
                    var gate = movementGates[i];
                    if (gate == null) continue;
                    cachedGates[i] = new Gate { Component = gate, Box = gate.GetComponent<BoxCollider>(), Permission = gate.GetType().GetProperty("ForwardMoveState") };
                }
            }
            foreach (var gate in cachedGates)
            {
                if (gate.Component == null || gate.Box == null || gate.Permission == null || (!occupied && (bool)gate.Permission.GetValue(gate.Component))) continue;
                var bounds = gate.Box.bounds;
                Vector3 towardCentre = Vector3.ProjectOnPlane(transform.position - bounds.center, Vector3.up).normalized;
                if (towardCentre.sqrMagnitude > .5f && Vector3.Dot(forward, towardCentre) < .65f) continue;
                Vector3 delta = bounds.center - front;
                float ahead = Vector3.Dot(delta, forward);
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float halfWidth = Mathf.Abs(right.x) * bounds.extents.x + Mathf.Abs(right.z) * bounds.extents.z;
                float halfDepth = Mathf.Abs(forward.x) * bounds.extents.x + Mathf.Abs(forward.z) * bounds.extents.z;
                if (ahead < 0 || ahead > reach + halfDepth || Mathf.Abs(Vector3.Dot(delta, right)) > halfWidth + 1) continue;
                distance = Mathf.Min(distance, Mathf.Max(0, ahead - halfDepth - 2));
            }
            return !float.IsPositiveInfinity(distance);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Observe UTS Signal Phases (60s)")]
        public void ObserveSignalPhases()
        {
            if (!Application.isPlaying || !IsConfigured) { Debug.LogWarning("Configure the UTS signal and enter Play Mode first.", this); return; }
            StartCoroutine(Observe());
        }

        private IEnumerator Observe()
        {
            float deadline = Time.realtimeSinceStartup + 60;
            int vehiclePhases = 0, pedestrianPhases = 0, conflictingSamples = 0;
            bool lastVehicles = VehicleMayProceed, lastPedestrians = PedestrianMayCross;
            while (Time.realtimeSinceStartup < deadline)
            {
                bool vehicles = VehicleMayProceed, pedestrians = PedestrianMayCross;
                if (vehicles && !lastVehicles) vehiclePhases++;
                if (pedestrians && !lastPedestrians) pedestrianPhases++;
                if (vehicles && pedestrians) conflictingSamples++;
                lastVehicles = vehicles; lastPedestrians = pedestrians;
                yield return new WaitForSecondsRealtime(.1f);
            }
            Debug.Log($"UTS SIGNAL {intersectionId}: vehicle phases={vehiclePhases}, pedestrian phases={pedestrianPhases}, conflicting samples={conflictingSamples}. Observe actual vehicles and pedestrians at the gates separately.", this);
        }
#endif
    }
}
