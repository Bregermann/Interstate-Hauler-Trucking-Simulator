using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiGPSAdapter : MonoBehaviour
    {
        private ILwsNavigationService navigation;
        private LwsRoadGraphProvider graph;
        private Transform player;
        private TruckTaxiRideLocation target;
        public string TargetId { get; private set; }
        public bool RouteReady => target != null && (target.Contains(player.position) ||
            (navigation?.CurrentRoute != null && navigation.CurrentRoute.succeeded));
        public void Initialize(Transform tractor, LwsRoadGraphProvider provider)
        {
            player = tractor; graph = provider;
            LwsApplicationBootstrap.Instance.Registry.TryGet(out navigation);
        }
        public void ConfigureDemoPresentation()
        {
            // Configure the existing HUD instance once; Compass still owns all map rendering.
            foreach (var component in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var type=component.GetType();
                if(type.FullName!="CompassNavigatorPro.CompassPro") continue;
                var canvas=component.GetComponent<Canvas>();
                if(canvas==null || canvas.renderMode!=RenderMode.ScreenSpaceOverlay) continue;
                var location=type.GetProperty("miniMapLocation");
                location.SetValue(component,System.Enum.Parse(location.PropertyType,"BottomLeft"));
                type.GetProperty("miniMapLocationOffset").SetValue(component,new Vector2(24,24));
                type.GetProperty("miniMapCaptureSize").SetValue(component,650f);
                type.GetProperty("miniMapCameraHeightVSFollow").SetValue(component,400f);
                type.GetProperty("miniMapCameraDepth").SetValue(component,900f);
            }
        }
        public void SetPickupDestination(TruckTaxiRideLocation location) => SetDestination(location, "pickup");
        public void SetRideDestination(TruckTaxiRideLocation location) => SetDestination(location, "dropoff");
        private void SetDestination(TruckTaxiRideLocation location, string purpose)
        {
            if (location == null || navigation == null || player == null || graph == null) return;
            TargetId = location.locationId;
            target = location;
            // A pickup can legitimately be at the previous dropoff. No zero-length route needed.
            if (location.Contains(player.position)) { navigation.ClearRoute(); return; }
            var result = navigation.RequestRoute(new LwsRouteRequest {
                requestId = "taxi." + purpose, destinationId = TargetId,
                useOriginWorldPosition = true, originWorldPosition = player.position,
                useDestinationWorldPosition = true, destinationWorldPosition = location.StopPosition,
                truckRouteRequired = true
            }, graph.Graph);
            if (!result.succeeded) Debug.LogWarning("Truck Taxi GPS: " + result.message, this);
        }
        public void ClearDestination() { target = null; TargetId = ""; navigation?.ClearRoute(); }
    }
}
