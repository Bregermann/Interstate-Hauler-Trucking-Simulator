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
        // ============================================================
        // TRUCK TAXI DASHBOARD GPS FIT (current tractor interior mesh)
        // Measured physical screen: 0.1472 x 0.0782 metres. No shared prefab edits.
        // ============================================================
        public Vector3 dashboardScreenPosition = new Vector3(-.10599f, .71074f, .38486f);
        public Vector3 dashboardScreenNormal = new Vector3(-.365503f, .168422f, -.915446f);
        public Vector2 dashboardScreenPixels = new Vector2(640, 340);
        public float dashboardPixelScale = .00023f;
        private Component previewCompass;
        private Canvas previewCanvas;
        private ILwsCameraPresentationService cameraPresentation;
        private LwsGpsPresentationPolicy originalPolicy;
        private System.Reflection.FieldInfo previewCameraField;
        private System.Reflection.PropertyInfo followOffsetProperty;
        private Vector3 previewCenter;
        private bool previewing;
        public RectTransform DashboardScreen { get; private set; }
        public Camera PreviewCamera => previewCompass != null ? previewCameraField?.GetValue(previewCompass) as Camera : null;
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
            LwsApplicationBootstrap.Instance.Registry.TryGet(out cameraPresentation);
            if (cameraPresentation != null)
            {
                originalPolicy = cameraPresentation.GpsPresentationPolicy;
                // Its existing camera supplies the offer texture, not a second map renderer.
                cameraPresentation.SetGpsPresentationPolicy(LwsGpsPresentationPolicy.ForceHudMinimapOn);
            }
            Transform interior = null;
            foreach (var t in player.GetComponentsInChildren<Transform>(true)) if (t.name == "interior") { interior = t; break; }
            foreach (var component in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var type=component.GetType();
                if(type.FullName!="CompassNavigatorPro.CompassPro") continue;
                var canvas=component.GetComponent<Canvas>();
                if(canvas==null) continue;
                SetEnum(component,"miniMapStyle","SolidBox");
                Set(component,"miniMapVignette",false);
                Set(component,"miniMapClampBorderCircular",false);
                Set(component,"miniMapCameraHeightVSFollow",400f);
                Set(component,"miniMapCameraDepth",900f);
                Set(component,"miniMapShowMaximizeButton",false);
                Set(component,"miniMapShowZoomInOutButtons",false);
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    previewCompass=component; previewCanvas=canvas;
                    previewCameraField=type.GetField("miniMapCamera");
                    followOffsetProperty=type.GetProperty("miniMapFollowOffset");
                    Set(component,"miniMapKeepStraight",true);
                    Set(component,"miniMapShowPlayerIcon",false);
                    Set(component,"miniMapShowPOIs",false);
                    canvas.enabled=false;
                }
                else if (canvas.renderMode == RenderMode.WorldSpace && interior != null)
                {
                    DashboardScreen=component.transform as RectTransform;
                    DashboardScreen.SetParent(interior,false);
                    DashboardScreen.localPosition=dashboardScreenPosition;
                    DashboardScreen.localRotation=Quaternion.LookRotation(-dashboardScreenNormal,Vector3.up);
                    DashboardScreen.localScale=Vector3.one*dashboardPixelScale;
                    FitScreen(DashboardScreen);
                    foreach(var rect in DashboardScreen.GetComponentsInChildren<RectTransform>(true))
                        if(rect.name=="MiniMap Root" || rect.name=="MiniMap" || rect.name=="MiniMapMask") FitScreen(rect);
                    Set(component,"miniMapCaptureSize",260f);
                }
            }
        }
        private void FitScreen(RectTransform rect)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=dashboardScreenPixels;
            if(rect!=DashboardScreen) { rect.anchoredPosition3D=Vector3.zero; rect.localScale=Vector3.one; }
        }
        internal static void Set(Component component,string property,object value) => component.GetType().GetProperty(property)?.SetValue(component,value);
        private static void SetEnum(Component component,string property,string value)
        {
            var info=component.GetType().GetProperty(property);
            if(info!=null) info.SetValue(component,System.Enum.Parse(info.PropertyType,value));
        }
        public void FrameOffer(TruckTaxiRideOffer offer)
        {
            previewing=offer!=null;
            if(offer==null || previewCompass==null) return;
            var bounds=new Bounds(offer.PlayerPosition,Vector3.zero);
            foreach(var point in offer.ToPickup.Points) bounds.Encapsulate(point);
            foreach(var point in offer.Trip.Points) bounds.Encapsulate(point);
            previewCenter=bounds.center;
            Set(previewCompass,"miniMapCaptureSize",Mathf.Max(160,Mathf.Max(bounds.size.x,bounds.size.z)*1.3f));
            UpdatePreviewCenter();
        }
        private void UpdatePreviewCenter()
        {
            if(!previewing || previewCompass==null || player==null) return;
            var offset=previewCenter-player.position; offset.y=0;
            followOffsetProperty?.SetValue(previewCompass,offset);
        }
        private void LateUpdate()
        {
            UpdatePreviewCenter();
            if(previewCanvas!=null) previewCanvas.enabled=false;
        }
        private void OnDestroy()
        {
            if(cameraPresentation!=null) cameraPresentation.SetGpsPresentationPolicy(originalPolicy);
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
