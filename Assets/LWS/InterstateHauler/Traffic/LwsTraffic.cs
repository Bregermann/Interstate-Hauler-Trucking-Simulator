using System;
using System.Collections.Generic;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsTrafficProfile
    {
        public string profileId = "default";
        public float density01 = 0.5f;
        public float heavyTruckShare01 = 0.15f;
        public bool pedestriansEnabled = true;
        public int maxActiveHighwayVehicles = 8;
        public float spawnIntervalSeconds = 4f;
        public float minimumPlayerSpawnDistanceMeters = 140f;
        public float despawnDistanceMeters = 850f;
    }

    [Serializable]
    public sealed class LwsTrafficTagLayerPolicy
    {
        public string playerVehicleTag = "Player";
        public string trafficVehicleTag = "Car";
        public string bicycleTag = "Bcycle";
        public string pedestrianTag = "People";
        public string pedestrianSignalTag = "PeopleSemaphore";
    }

    public interface ILwsTrafficService : ILwsService
    {
        LwsTrafficProfile CurrentProfile { get; }
        LwsTrafficTagLayerPolicy TagLayerPolicy { get; }
        IReadOnlyList<LwsTrafficIdentity> ActiveTrafficVehicles { get; }
        void SetTrafficProfile(LwsTrafficProfile profile);
        LwsServiceResult RegisterTrafficVehicle(LwsTrafficIdentity identity);
        LwsServiceResult UnregisterTrafficVehicle(LwsTrafficIdentity identity);
    }

    public sealed class LwsTrafficService : ILwsTrafficService
    {
        private readonly List<LwsTrafficIdentity> _activeTrafficVehicles = new List<LwsTrafficIdentity>();
        private readonly HashSet<string> _activeTrafficIds = new HashSet<string>(StringComparer.Ordinal);

        public string ServiceId => "lws.traffic";
        public LwsTrafficProfile CurrentProfile { get; private set; }
        public LwsTrafficTagLayerPolicy TagLayerPolicy { get; private set; }
        public IReadOnlyList<LwsTrafficIdentity> ActiveTrafficVehicles => _activeTrafficVehicles;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            CurrentProfile = new LwsTrafficProfile();
            TagLayerPolicy = new LwsTrafficTagLayerPolicy();
            return LwsServiceResult.Success("LWS traffic service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _activeTrafficVehicles.Clear();
            _activeTrafficIds.Clear();
            CurrentProfile = null;
            TagLayerPolicy = null;
            return LwsServiceResult.Success("LWS traffic service shut down.");
        }

        public void SetTrafficProfile(LwsTrafficProfile profile)
        {
            CurrentProfile = profile ?? new LwsTrafficProfile();
        }

        public LwsServiceResult RegisterTrafficVehicle(LwsTrafficIdentity identity)
        {
            if (identity == null)
            {
                return LwsServiceResult.Failure("Traffic identity is null.");
            }

            string trafficId = identity.TrafficId;
            if (string.IsNullOrWhiteSpace(trafficId))
            {
                return LwsServiceResult.Failure($"{identity.name} has no stable traffic ID.");
            }

            if (_activeTrafficIds.Contains(trafficId))
            {
                return LwsServiceResult.Failure($"Duplicate active traffic ID: {trafficId}");
            }

            _activeTrafficIds.Add(trafficId);
            _activeTrafficVehicles.Add(identity);
            identity.MarkRegistered(true);
            return LwsServiceResult.Success($"Registered traffic vehicle {trafficId}.");
        }

        public LwsServiceResult UnregisterTrafficVehicle(LwsTrafficIdentity identity)
        {
            if (identity == null)
            {
                return LwsServiceResult.Success("Traffic identity was already destroyed.");
            }

            _activeTrafficVehicles.Remove(identity);
            if (!string.IsNullOrWhiteSpace(identity.TrafficId))
            {
                _activeTrafficIds.Remove(identity.TrafficId);
            }

            identity.MarkRegistered(false);
            return LwsServiceResult.Success($"Unregistered traffic vehicle {identity.TrafficId}.");
        }
    }
}
