using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsTrafficIdentity : MonoBehaviour
    {
        [SerializeField] private string trafficId = "ih.traffic.unassigned";
        [SerializeField] private string laneId = "ih.lane.unassigned";
        [SerializeField] private string vehiclePrefabName = "Unknown Traffic Vehicle";
        [SerializeField] private LwsTrafficVehicleKind vehicleKind = LwsTrafficVehicleKind.Unknown;
        [SerializeField] private bool registered;

        public string TrafficId => trafficId;
        public string LaneId => laneId;
        public string VehiclePrefabName => vehiclePrefabName;
        public LwsTrafficVehicleKind VehicleKind => vehicleKind;
        public bool Registered => registered;

        public void Configure(string newTrafficId, string newLaneId, string newPrefabName, LwsTrafficVehicleKind newKind)
        {
            trafficId = string.IsNullOrWhiteSpace(newTrafficId) ? trafficId : newTrafficId.Trim();
            laneId = string.IsNullOrWhiteSpace(newLaneId) ? laneId : newLaneId.Trim();
            vehiclePrefabName = string.IsNullOrWhiteSpace(newPrefabName) ? vehiclePrefabName : newPrefabName.Trim();
            vehicleKind = newKind;
            EnsureVehicleIdentity();
        }

        public void MarkRegistered(bool value)
        {
            registered = value;
        }

        private void OnDisable()
        {
            if (!registered)
            {
                return;
            }

            LwsApplicationBootstrap bootstrap = LwsApplicationBootstrap.Instance;
            if (bootstrap != null &&
                bootstrap.Registry != null &&
                bootstrap.Registry.TryGet(out ILwsTrafficService trafficService))
            {
                trafficService.UnregisterTrafficVehicle(this);
            }
            else
            {
                registered = false;
            }
        }

        private void EnsureVehicleIdentity()
        {
            LwsVehicleIdentity vehicleIdentity = GetComponent<LwsVehicleIdentity>();
            if (vehicleIdentity == null)
            {
                vehicleIdentity = gameObject.AddComponent<LwsVehicleIdentity>();
            }

            vehicleIdentity.Configure(
                trafficId,
                $"ih.traffic.{vehicleKind.ToString().ToLowerInvariant()}",
                LwsVehicleRole.AiVehicle,
                vehiclePrefabName,
                false);
        }
    }
}
