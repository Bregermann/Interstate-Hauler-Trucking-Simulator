using System;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsTrafficProfile
    {
        public string profileId = "default";
        public float density01 = 0.5f;
        public float heavyTruckShare01 = 0.15f;
        public bool pedestriansEnabled = true;
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
        void SetTrafficProfile(LwsTrafficProfile profile);
    }

    public sealed class LwsTrafficService : ILwsTrafficService
    {
        public string ServiceId => "lws.traffic";
        public LwsTrafficProfile CurrentProfile { get; private set; }
        public LwsTrafficTagLayerPolicy TagLayerPolicy { get; private set; }

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            CurrentProfile = new LwsTrafficProfile();
            TagLayerPolicy = new LwsTrafficTagLayerPolicy();
            return LwsServiceResult.Success("LWS traffic service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            CurrentProfile = null;
            TagLayerPolicy = null;
            return LwsServiceResult.Success("LWS traffic service shut down.");
        }

        public void SetTrafficProfile(LwsTrafficProfile profile)
        {
            CurrentProfile = profile ?? new LwsTrafficProfile();
        }
    }
}
