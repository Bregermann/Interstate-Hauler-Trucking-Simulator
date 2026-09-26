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
        private TruckTaxiStopObjectivePoint stopTarget;
        private bool serviceTargetActive;
        private Vector3 serviceTargetPosition;
        // ============================================================
        // TRUCK TAXI DASHBOARD GPS FIT (current tractor interior mesh)
        // Measured physical screen: 0.1472 x 0.0782 metres. No shared prefab edits.
        // ============================================================
        public Vector3 dashboardScreenPosition = new Vector3(-.10599f, .71074f, .38486f);
        public Vector3 dashboardScreenNormal = new Vector3(-.365503f, .168422f, -.915446f);
        public Vector2 dashboardScreenPixels = new Vector2(640, 340);
        public float dashboardPixelScale = .00023f;
        [Min(0),Tooltip("Local clearance in front of the physical screen. Keeps the depth-tested vendor route out of the cab mesh.")]
        public float dashboardScreenClearance = .01f;
        private Component previewCompass;
        private Component cabCompass;
        private Canvas previewCanvas;
        private ILwsCameraPresentationService cameraPresentation;
        private LwsGpsPresentationPolicy originalPolicy;
        private System.Reflection.FieldInfo previewCameraField;
        private System.Reflection.PropertyInfo followOffsetProperty;
        private Vector3 previewCenter;
        private bool previewing;
        private RectTransform cabMapRect,hudMapRect;
        private Vector2 lastCabMapSize,lastHudMapSize;
        // Vendor "pixel" widths are local UI units, not final display pixels. A 240-unit
        // reference gives the default route 5% and a normal POI 8% of the map's short side.
        private const float MapStyleReferenceSize=240f;
        public TruckTaxiGPSDisplaySettings DisplaySettings { get; private set; }
        public Component CabCompass => cabCompass;
        public Component HudCompass => previewCompass;
        public bool HudVisible => DisplaySettings != null && DisplaySettings.showHud;
        public event System.Action DisplaySettingsChanged;
        public RectTransform DashboardScreen { get; private set; }
        public TruckTaxiMapMarkers MapMarkers { get; private set; }
        public Camera PreviewCamera => previewCompass != null ? previewCameraField?.GetValue(previewCompass) as Camera : null;
        public string TargetId { get; private set; }
        public bool IsServiceDestination => serviceTargetActive;
        public bool RouteReady => player!=null && (target != null || stopTarget!=null || serviceTargetActive) && ((target!=null && target.Contains(player.position)) ||
            (serviceTargetActive && Vector3.ProjectOnPlane(serviceTargetPosition-player.position,Vector3.up).sqrMagnitude<=9) ||
            (stopTarget!=null && stopTarget.IsValidStop(player.position,0)) ||
            (navigation?.CurrentRoute != null && navigation.CurrentRoute.succeeded));
        public void Initialize(Transform tractor, LwsRoadGraphProvider provider)
        {
            player = tractor; graph = provider;
            DisplaySettings = TruckTaxiGPSDisplaySettings.Load();
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
                Set(component,"miniMapShowViewCone",false);
                var northOnly = Resources.Load<Sprite>("TruckTaxi/NorthOnly");
                if (northOnly != null)
                {
                    Set(component,"miniMapCardinalsSprite",northOnly);
                    Set(component,"miniMapShowCardinals",true);
                }
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
                    // Below the existing taxi menus, but visible in every driving camera.
                    canvas.sortingOrder=850;
                    SetEnum(component,"miniMapLocation","BottomLeft");
                    Set(component,"miniMapLocationOffset",new Vector2(28,220));
                }
                else if (canvas.renderMode == RenderMode.WorldSpace && interior != null)
                {
                    cabCompass=component;
                    DashboardScreen=component.transform as RectTransform;
                    DashboardScreen.SetParent(interior,false);
                    // The vendor map ignores depth, but its route does not. Keep the entire
                    // display just in front of the authored physical screen, not inside it.
                    DashboardScreen.localPosition=dashboardScreenPosition+dashboardScreenNormal.normalized*dashboardScreenClearance;
                    DashboardScreen.localRotation=Quaternion.LookRotation(-dashboardScreenNormal,Vector3.up);
                    DashboardScreen.localScale=Vector3.one*dashboardPixelScale;
                    FitScreen(DashboardScreen);
                    foreach(var rect in DashboardScreen.GetComponentsInChildren<RectTransform>(true))
                        if(rect.name=="MiniMap Root" || rect.name=="MiniMap" || rect.name=="MiniMapMask") FitScreen(rect);
                }
            }
            cabMapRect=cabCompass!=null ? cabCompass.transform.Find("MiniMap Root/MiniMap/MiniMapMask") as RectTransform : null;
            hudMapRect=previewCompass!=null ? previewCompass.transform.Find("MiniMap Root/MiniMap/MiniMapMask") as RectTransform : null;
            ApplyDisplaySettings(false);
            MapMarkers=GetComponent<TruckTaxiMapMarkers>() ?? gameObject.AddComponent<TruckTaxiMapMarkers>();
            MapMarkers.Initialize(GetComponent<TruckTaxiBootstrap>(),this);
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
            if(previewCompass==null) return;
            ApplyDisplaySettings(false);
            if(offer==null) return;
            var bounds=new Bounds(offer.PlayerPosition,Vector3.zero);
            foreach(var point in offer.ToPickup.Points) bounds.Encapsulate(point);
            foreach(var point in offer.Trip.Points) bounds.Encapsulate(point);
            previewCenter=bounds.center;
            Set(previewCompass,"miniMapCaptureSize",Mathf.Max(160,Mathf.Max(bounds.size.x,bounds.size.z)*1.3f));
            UpdatePreviewCenter();
        }
        public void ToggleHud()
        {
            DisplaySettings.showHud=!DisplaySettings.showHud;
            ApplyDisplaySettings();
        }
        public void ResetDisplaySettings()
        {
            DisplaySettings=new TruckTaxiGPSDisplaySettings();
            ApplyDisplaySettings();
        }
        public void ApplyDisplaySettings(bool save = true)
        {
            if(DisplaySettings==null) return;
            DisplaySettings.Validate();
            ConfigureDisplay(cabCompass,DisplaySettings.cabRangeMeters,false);
            ConfigureDisplay(previewCompass,DisplaySettings.hudRangeMeters,previewing);
            if(previewCompass!=null)
            {
                Set(previewCompass,"miniMapSize",DisplaySettings.hudSize);
                if(!previewing) followOffsetProperty?.SetValue(previewCompass,Vector3.zero);
                // Offer preview keeps using this camera even when the player's HUD is off.
                // Only suppress the duplicate canvas while that modal map owns the texture.
                previewCanvas.enabled=!previewing;
            }
            cameraPresentation?.SetGpsPresentationPolicy(previewing || DisplaySettings.showHud
                ? LwsGpsPresentationPolicy.ForceHudMinimapOn : LwsGpsPresentationPolicy.ForceHudMinimapOff);
            RefreshMapElementSizing(true);
            if(save) DisplaySettings.Save();
            DisplaySettingsChanged?.Invoke();
        }
        private void ConfigureDisplay(Component compass,float range,bool offer)
        {
            if(compass==null) return;
            if(!offer) Set(compass,"miniMapCaptureSize",range);
            Set(compass,"miniMapKeepStraight",offer || DisplaySettings.northUp);
            Set(compass,"miniMapShowPlayerIcon",!offer);
            Set(compass,"miniMapShowPOIs",!offer && DisplaySettings.showPois);
            Set(compass,"showRoute",true);
            Set(compass,"routeShowOnMiniMap",true);
            Set(compass,"routeColor",DisplaySettings.RouteColor);
            SetEnum(compass,"routeWidthSpace","Pixels");
            // Retain antialiasing without feathering away the entire physical cab line.
            Set(compass,"routeEdgeFeather",.5f);
        }
        public static float MapElementScale(Vector2 mapSize) => mapSize.x>1 && mapSize.y>1
            ? Mathf.Min(mapSize.x,mapSize.y)/MapStyleReferenceSize : 0;
        private void RefreshMapElementSizing(bool force=false)
        {
            if(DisplaySettings==null) return;
            ApplyMapElementSizing(cabCompass,cabMapRect,ref lastCabMapSize,force);
            ApplyMapElementSizing(previewCompass,hudMapRect,ref lastHudMapSize,force);
        }
        private void ApplyMapElementSizing(Component compass,RectTransform map,ref Vector2 priorSize,bool force)
        {
            if(compass==null || map==null) return;
            Vector2 size=map.rect.size;
            if(!force && size==priorSize) return;
            float scale=MapElementScale(size);
            if(scale<=0) return;
            priorSize=size;
            Set(compass,"routeWidth",DisplaySettings.routeWidth*scale);
            // Vendor POI rects are 24 units; its player arrow is 12. Match their base size.
            Set(compass,"miniMapIconSize",.8f*scale);
            Set(compass,"miniMapPlayerIconSize",1.6f*scale);
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
            // Compare cached rects only; reapply after vendor layout/resolution changes, not every frame.
            RefreshMapElementSizing();
        }
        private void OnDestroy()
        {
            if(cameraPresentation!=null) cameraPresentation.SetGpsPresentationPolicy(originalPolicy);
        }
        public void SetPickupDestination(TruckTaxiRideLocation location) => SetDestination(location, "pickup");
        public void SetRideDestination(TruckTaxiRideLocation location) => SetDestination(location, "dropoff");
        public void SetStopDestination(TruckTaxiStopObjectivePoint point)
        {
            if(point==null || navigation==null || player==null || graph==null) return;
            serviceTargetActive=false; MapMarkers?.ClearServiceTarget();
            stopTarget=point; target=null; TargetId=point.stableId;
            if(point.IsValidStop(player.position,0)) { navigation.ClearRoute(); return; }
            var result=navigation.RequestRoute(new LwsRouteRequest {
                requestId="taxi.optional-stop",destinationId=TargetId,useOriginWorldPosition=true,originWorldPosition=player.position,
                useDestinationWorldPosition=true,destinationWorldPosition=point.Position,truckRouteRequired=true
            },graph.Graph);
            if(!result.succeeded) Debug.LogWarning("Taxi optional stop route: "+result.message,this);
            MapMarkers?.RequestRefresh();
        }
        private void SetDestination(TruckTaxiRideLocation location, string purpose)
        {
            if (location == null || navigation == null || player == null || graph == null) return;
            serviceTargetActive=false; MapMarkers?.ClearServiceTarget();
            TargetId = location.locationId;
            target = location;
            stopTarget=null;
            // A pickup can legitimately be at the previous dropoff. No zero-length route needed.
            if (location.Contains(player.position)) { navigation.ClearRoute(); return; }
            var result = navigation.RequestRoute(new LwsRouteRequest {
                requestId = "taxi." + purpose, destinationId = TargetId,
                useOriginWorldPosition = true, originWorldPosition = player.position,
                useDestinationWorldPosition = true, destinationWorldPosition = location.StopPosition,
                truckRouteRequired = true
            }, graph.Graph);
            if (!result.succeeded) Debug.LogWarning("Truck Taxi GPS: " + result.message, this);
            MapMarkers?.RequestRefresh();
        }
        public void SetServiceDestination(string stableId,string displayName,Vector3 position)
        {
            if(string.IsNullOrWhiteSpace(stableId) || navigation==null || player==null || graph==null) return;
            TargetId=stableId; target=null; stopTarget=null; serviceTargetActive=true; serviceTargetPosition=position;
            MapMarkers?.SetServiceTarget(stableId,displayName,position);
            if(Vector3.ProjectOnPlane(position-player.position,Vector3.up).sqrMagnitude<=9) { navigation.ClearRoute(); return; }
            var result=navigation.RequestRoute(new LwsRouteRequest {
                requestId="taxi.service",destinationId=stableId,useOriginWorldPosition=true,originWorldPosition=player.position,
                useDestinationWorldPosition=true,destinationWorldPosition=position,truckRouteRequired=true
            },graph.Graph);
            if(!result.succeeded) Debug.LogWarning("Taxi service route: "+result.message,this);
            MapMarkers?.RequestRefresh();
        }
        public void ClearDestination()
        { target=null; stopTarget=null; serviceTargetActive=false; TargetId=""; MapMarkers?.ClearServiceTarget(); navigation?.ClearRoute(); }
    }
}
