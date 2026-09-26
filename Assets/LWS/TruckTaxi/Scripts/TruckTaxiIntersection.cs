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
        public void Configure(string id, Component signal, Component[] gates, Component path)
        {
            intersectionId = id;
            signalSystem = signal;
            movementGates = gates;
            crosswalkPath = path;
        }
        public bool IsConfigured => signalSystem != null && movementGates != null && movementGates.Length > 0 && crosswalkPath != null;
        public bool VehicleMayProceed => ReadGate("ForwardMoveState");
        public bool PedestrianMayCross => ReadGate("PeopleMoveState");

        private bool ReadGate(string property)
        {
            if (!IsConfigured || movementGates[0] == null) return false;
            PropertyInfo member = movementGates[0].GetType().GetProperty(property);
            return member != null && member.PropertyType == typeof(bool) && (bool)member.GetValue(movementGates[0]);
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
