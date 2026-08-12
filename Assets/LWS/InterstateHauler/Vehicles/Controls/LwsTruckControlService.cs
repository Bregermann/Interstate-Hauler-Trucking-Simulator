using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public sealed class LwsTruckControlService : ILwsTruckControlService
    {
        public string ServiceId => "lws.vehicle.truck.controls";
        public LwsTruckControlController ActiveController { get; private set; }
        public LwsTruckControlState ActiveState { get; private set; }

        public event Action<LwsTruckControlState> StateChanged;
        public event Action<LwsTruckControlEvent> ControlEventPublished;
        public event Action<LwsDriverGestureEvent> DriverGesturePublished;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            ActiveController = null;
            ActiveState = default;
            return LwsServiceResult.Success("LWS truck control service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            ActiveController = null;
            ActiveState = default;
            return LwsServiceResult.Success("LWS truck control service shut down.");
        }

        public LwsServiceResult RegisterActiveController(LwsTruckControlController controller)
        {
            if (controller == null)
            {
                return LwsServiceResult.Failure("Cannot register a null truck control controller.");
            }

            if (ActiveController != null && ActiveController != controller)
            {
                return LwsServiceResult.Failure($"Active truck control controller already registered: {ActiveState.vehicleId}");
            }

            ActiveController = controller;
            return LwsServiceResult.Success("Active truck control controller registered.");
        }

        public LwsServiceResult ClearActiveController(LwsTruckControlController controller)
        {
            if (controller == null || ActiveController == null)
            {
                return LwsServiceResult.Success("No active truck control controller to clear.");
            }

            if (ActiveController != controller)
            {
                return LwsServiceResult.Failure("Cannot clear a non-active truck control controller.");
            }

            ActiveController = null;
            ActiveState = default;
            return LwsServiceResult.Success("Active truck control controller cleared.");
        }

        public void PublishState(LwsTruckControlController controller, LwsTruckControlState state)
        {
            if (controller == null || ActiveController != controller)
            {
                return;
            }

            ActiveState = state;
            StateChanged?.Invoke(state);
        }

        public void PublishControlEvent(LwsTruckControlController controller, string controlName, string value)
        {
            if (controller == null || ActiveController != controller)
            {
                return;
            }

            ControlEventPublished?.Invoke(new LwsTruckControlEvent
            {
                vehicleId = ActiveState.vehicleId,
                controlName = controlName ?? string.Empty,
                value = value ?? string.Empty,
                timestamp = Time.time
            });
        }

        public void PublishDriverGesture(LwsTruckControlController controller, LwsDriverGestureEvent gestureEvent)
        {
            if (controller == null || ActiveController != controller)
            {
                return;
            }

            DriverGesturePublished?.Invoke(gestureEvent);
        }
    }
}
