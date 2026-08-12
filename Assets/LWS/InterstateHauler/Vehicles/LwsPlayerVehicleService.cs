using System;

namespace LWS.InterstateHauler
{
    public interface ILwsPlayerVehicleService : ILwsService
    {
        LwsPlayerTruck ActiveTruck { get; }
        bool HasActiveTruck { get; }
        event Action<LwsPlayerTruck> PlayerTruckSpawned;
        event Action<LwsPlayerTruck> PlayerTruckReady;
        event Action<LwsPlayerTruck> EngineStarted;
        event Action<LwsPlayerTruck> EngineStopped;
        event Action<LwsPlayerTruck, LwsTrailerAttachmentState> TrailerAttached;
        event Action<LwsPlayerTruck, LwsTrailerAttachmentState> TrailerDetached;
        LwsServiceResult RegisterActiveTruck(LwsPlayerTruck truck);
        LwsServiceResult ClearActiveTruck(LwsPlayerTruck truck);
        void PublishTruckReady(LwsPlayerTruck truck);
        void PublishEngineState(LwsPlayerTruck truck, bool engineRunning);
        void PublishTrailerState(LwsPlayerTruck truck, LwsTrailerAttachmentState state);
    }

    public sealed class LwsPlayerVehicleService : ILwsPlayerVehicleService
    {
        private bool _lastEngineRunning;
        private bool _lastTrailerAttached;

        public string ServiceId => "lws.vehicle.player";
        public LwsPlayerTruck ActiveTruck { get; private set; }
        public bool HasActiveTruck => ActiveTruck != null;

        public event Action<LwsPlayerTruck> PlayerTruckSpawned;
        public event Action<LwsPlayerTruck> PlayerTruckReady;
        public event Action<LwsPlayerTruck> EngineStarted;
        public event Action<LwsPlayerTruck> EngineStopped;
        public event Action<LwsPlayerTruck, LwsTrailerAttachmentState> TrailerAttached;
        public event Action<LwsPlayerTruck, LwsTrailerAttachmentState> TrailerDetached;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            ActiveTruck = null;
            _lastEngineRunning = false;
            _lastTrailerAttached = false;
            return LwsServiceResult.Success("LWS player vehicle service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            ActiveTruck = null;
            return LwsServiceResult.Success("LWS player vehicle service shut down.");
        }

        public LwsServiceResult RegisterActiveTruck(LwsPlayerTruck truck)
        {
            if (truck == null)
            {
                return LwsServiceResult.Failure("Cannot register a null player truck.");
            }

            if (ActiveTruck != null && ActiveTruck != truck)
            {
                return LwsServiceResult.Failure($"Active player truck already registered: {ActiveTruck.VehicleId}");
            }

            ActiveTruck = truck;
            PlayerTruckSpawned?.Invoke(truck);
            return LwsServiceResult.Success($"Active player truck registered: {truck.VehicleId}");
        }

        public LwsServiceResult ClearActiveTruck(LwsPlayerTruck truck)
        {
            if (truck == null || ActiveTruck == null)
            {
                return LwsServiceResult.Success("No active player truck to clear.");
            }

            if (ActiveTruck != truck)
            {
                return LwsServiceResult.Failure($"Cannot clear non-active player truck: {truck.VehicleId}");
            }

            ActiveTruck = null;
            _lastEngineRunning = false;
            _lastTrailerAttached = false;
            return LwsServiceResult.Success("Active player truck cleared.");
        }

        public void PublishTruckReady(LwsPlayerTruck truck)
        {
            if (truck != null && truck == ActiveTruck)
            {
                PlayerTruckReady?.Invoke(truck);
            }
        }

        public void PublishEngineState(LwsPlayerTruck truck, bool engineRunning)
        {
            if (truck == null || truck != ActiveTruck || engineRunning == _lastEngineRunning)
            {
                return;
            }

            _lastEngineRunning = engineRunning;
            if (engineRunning)
            {
                EngineStarted?.Invoke(truck);
            }
            else
            {
                EngineStopped?.Invoke(truck);
            }
        }

        public void PublishTrailerState(LwsPlayerTruck truck, LwsTrailerAttachmentState state)
        {
            if (truck == null || truck != ActiveTruck || state.attached == _lastTrailerAttached)
            {
                return;
            }

            _lastTrailerAttached = state.attached;
            if (state.attached)
            {
                TrailerAttached?.Invoke(truck, state);
            }
            else
            {
                TrailerDetached?.Invoke(truck, state);
            }
        }
    }
}
