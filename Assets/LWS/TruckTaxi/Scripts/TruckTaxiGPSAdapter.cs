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
        private string serviceTargetLabel;
        private TruckTaxiMapMarkerType serviceTargetType=TruckTaxiMapMarkerType.Bathroom;
        private bool guidanceSuppressed;
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
        private bool fullMapOpen, transientHudHidden;
        private int regularCanvasOrder;
        private RectTransform cabMapRect,hudMapRect;
        private TruckTaxiMapTileLayer tileLayer;
        private bool attemptedTileLoad;
        private Camera tileCamera;
        private int originalTileCameraMask;
        private Vector2 lastCabMapSize,lastHudMapSize;
        // Vendor "pixel" widths are local UI units, not final display pixels. A 240-unit
        // reference gives the default route 5% and a normal POI 8% of the map's short side.
        private const float MapStyleReferenceSize=240f;
        public TruckTaxiGPSDisplaySettings DisplaySettings { get; private set; }
        public Component CabCompass => cabCompass;
        public Component HudCompass => previewCompass;
        public bool HudVisible => DisplaySettings != null && DisplaySettings.showHud;
        public bool FullMapOpen => fullMapOpen;
        public float FullMapZoomLevel => previewCompass?.GetType().GetProperty("miniMapFullScreenZoomLevel")?.GetValue(previewCompass) is float value ? value : 0;
        public int BakedTileCount => tileLayer!=null ? tileLayer.transform.childCount : 0;
        public bool BakedTilesVisible => fullMapOpen && tileCamera!=null &&
            (tileCamera.cullingMask & (1<<TruckTaxiMapTileLayer.MapLayer))!=0 && BakedTileCount>0;
        public event System.Action DisplaySettingsChanged;
        public RectTransform DashboardScreen { get; private set; }
        public TruckTaxiMapMarkers MapMarkers { get; private set; }
        public Camera PreviewCamera => previewCompass != null ? previewCameraField?.GetValue(previewCompass) as Camera : null;
        public string TargetId { get; private set; }
        public bool IsServiceDestination => serviceTargetActive;
        public bool GuidanceSuppressed => guidanceSuppressed;
        public bool HasNavigationTarget => !string.IsNullOrEmpty(TargetId);
        public bool HasReachedServiceDestination => serviceTargetActive && player!=null &&
            Vector3.ProjectOnPlane(serviceTargetPosition-player.position,Vector3.up).sqrMagnitude<=9;
        public bool RouteReady => !guidanceSuppressed && player!=null && (target != null || stopTarget!=null || serviceTargetActive) && ((target!=null && target.Contains(player.position)) ||
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
                    // Compass gates its parent CanvasGroup through this public interaction setting.
                    Set(component,"miniMapIconEvents",true);
                    previewCameraField=type.GetField("miniMapCamera");
                    followOffsetProperty=type.GetProperty("miniMapFollowOffset");
                    // Below the existing taxi menus, but visible in every driving camera.
                    canvas.sortingOrder=850;
                    regularCanvasOrder=canvas.sortingOrder;
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
        internal static void Set(Component component,string property,object value)
        {
            var type=component.GetType();
            var member=type.GetProperty(property);
            if(member!=null) member.SetValue(component,value);
            else type.GetField(property)?.SetValue(component,value);
        }
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
                if(!previewing && !fullMapOpen) followOffsetProperty?.SetValue(previewCompass,Vector3.zero);
                // Offer preview keeps using this camera even when the player's HUD is off.
                // Only suppress the duplicate canvas while that modal map owns the texture.
                previewCanvas.enabled=fullMapOpen || (!previewing && !transientHudHidden);
            }
            cameraPresentation?.SetGpsPresentationPolicy(fullMapOpen || previewing || (DisplaySettings.showHud && !transientHudHidden)
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
            Set(compass,"miniMapShowPOIs",!offer && (fullMapOpen || DisplaySettings.showPois));
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
            if(compass==previewCompass && fullMapOpen)
            {
                // Regional markers stay legible without occupying 8% of the enlarged map each.
                Set(compass,"routeWidth",Mathf.Clamp(DisplaySettings.routeWidth,2,8));
                Set(compass,"miniMapIconSize",1f);
                Set(compass,"miniMapPlayerIconSize",2f);
                return;
            }
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
            if(fullMapOpen) EnableMapTiles();
            // Compare cached rects only; reapply after vendor layout/resolution changes, not every frame.
            RefreshMapElementSizing();
            if(!guidanceSuppressed && HasReachedServiceDestination && serviceTargetType!=TruckTaxiMapMarkerType.PrivateEventStop) ClearDestination();
        }
        // Temporary overlay ownership, deliberately independent of the persisted GPS preference.
        public void SetHudTemporarilyHidden(bool hidden)
        { transientHudHidden=hidden; ApplyDisplaySettings(false); }
        public bool SetFullMapOpen(bool open,float regionSpan=1500f,Vector3? center=null)
        {
            if(previewCompass==null || previewCanvas==null || player==null || (open && previewing)) return false;
            if(fullMapOpen==open) return true;
            if(open)
            {
                Set(previewCompass,"miniMapFullScreenWorldCenter",center ?? player.position);
                Set(previewCompass,"miniMapFullScreenWorldSize",new Vector3(Mathf.Max(500,regionSpan),0,Mathf.Max(500,regionSpan)));
                Set(previewCompass,"miniMapFollowOffset",Vector3.zero);
                Set(previewCompass,"miniMapFullScreenAllowUserDrag",true);
                Set(previewCompass,"miniMapFullScreenAutoResetDrag",false);
                Set(previewCompass,"miniMapFullScreenClampToWorldEdges",false);
                Set(previewCompass,"miniMapFullScreenDragMaxDistance",100000f);
                Set(previewCompass,"miniMapFullScreenFreezeCamera",false);
                Set(previewCompass,"miniMapFullScreenWorldCenterFollows",false);
                Set(previewCompass,"miniMapFullScreenSize",1f);
                Set(previewCompass,"miniMapKeepAspectRatio",false);
                SetEnum(previewCompass,"miniMapFullScreenContents","TopDownWorldView");
                Set(previewCompass,"miniMapZoomMax",4f);
                Set(previewCompass,"miniMapFullScreenZoomLevel",1f);
            }
            Set(previewCompass,"miniMapFullScreenState",open);
            fullMapOpen=open;
            if(open) EnableMapTiles();
            else if(tileCamera!=null)
            { tileLayer?.ShowOnlyFor(null); tileCamera.cullingMask=originalTileCameraMask; tileCamera=null; }
            previewCanvas.sortingOrder=open ? 910 : regularCanvasOrder;
            ApplyDisplaySettings(false);
            MapMarkers?.RequestRefresh();
            return true;
        }
        private void EnableMapTiles()
        {
            if(tileLayer==null)
            {
                if(attemptedTileLoad) return;
                attemptedTileLoad=true;
                var catalog=Resources.Load<TruckTaxiMapTileCatalog>(TruckTaxiMapTileCatalog.ResourcePath);
                if(catalog==null) return;
                var root=new GameObject("Persistent regional map tiles");
                root.transform.SetParent(transform,false);
                tileLayer=root.AddComponent<TruckTaxiMapTileLayer>();
                if(!tileLayer.Initialize(catalog)) { Destroy(root); tileLayer=null; return; }
            }
            var camera=PreviewCamera;
            if(camera==null) return;
            if(tileCamera!=camera)
            {
                if(tileCamera!=null) tileCamera.cullingMask=originalTileCameraMask;
                tileCamera=camera; originalTileCameraMask=camera.cullingMask;
            }
            camera.cullingMask|=1<<TruckTaxiMapTileLayer.MapLayer;
            tileLayer.ShowOnlyFor(camera);
        }
        public void CenterFullMapOnPlayer()
        {
            if(!fullMapOpen || previewCompass==null || player==null) return;
            Set(previewCompass,"miniMapFullScreenWorldCenter",player.position);
            Set(previewCompass,"miniMapFollowOffset",Vector3.zero);
        }
        public void PanFullMap(Vector2 direction,float unscaledSeconds)
        {
            if(!fullMapOpen || previewCompass==null) return;
            var offset=followOffsetProperty?.GetValue(previewCompass) is Vector3 value ? value : Vector3.zero;
            offset+=new Vector3(direction.x,0,direction.y)*Mathf.Max(1,FullMapSpan)*.45f*unscaledSeconds;
            followOffsetProperty?.SetValue(previewCompass,offset);
        }
        public void ZoomFullMap(float steps)
        {
            if(!fullMapOpen || previewCompass==null || Mathf.Approximately(steps,0)) return;
            var property=previewCompass.GetType().GetProperty("miniMapFullScreenZoomLevel");
            if(property==null) return;
            var zoom=(float)property.GetValue(previewCompass);
            // Compass's own MiniMapZoomIn increases this value. Its default max is only 1.
            property.SetValue(previewCompass,Mathf.Clamp(zoom*Mathf.Pow(1.2f,steps),.12f,4f));
        }
        public float FullMapSpan => previewCompass?.GetType().GetProperty("miniMapFullScreenWorldSize")?.GetValue(previewCompass) is Vector3 size ? size.x : 1500f;
        public bool CanSetMapServiceDestination => target==null && stopTarget==null &&
            string.IsNullOrEmpty(GetComponent<TruckTaxiBootstrap>()?.Session?.CurrentRideId);
        public bool TrySetMapServiceDestination(string id,string label,Vector3 position)
        {
            if(!CanSetMapServiceDestination || string.IsNullOrWhiteSpace(id) || navigation==null || graph==null || player==null) return false;
            var type=TruckTaxiMapMarkerType.ServiceArea;
            if(MapMarkers!=null)
            {
                foreach(var point in MapMarkers.RegionalPoints)
                    if(point.Id==id) { type=point.IconType; break; }
                foreach(var marker in MapMarkers.Markers)
                    if(marker!=null && marker.stableId==id) { type=marker.markerType; break; }
            }
            return TrySetServiceDestination(id,label,position,type);
        }
        private void OnDestroy()
        {
            if(tileCamera!=null) tileCamera.cullingMask=originalTileCameraMask;
            if(cameraPresentation!=null) cameraPresentation.SetGpsPresentationPolicy(originalPolicy);
        }
        public void SetPickupDestination(TruckTaxiRideLocation location) => SetDestination(location, "pickup");
        public void SetRideDestination(TruckTaxiRideLocation location)
        { if (!serviceTargetActive) SetDestination(location, "dropoff"); }
        public void SetStopDestination(TruckTaxiStopObjectivePoint point)
        {
            if(serviceTargetActive) return;
            if(point==null || navigation==null || player==null || graph==null) return;
            serviceTargetActive=false; guidanceSuppressed=false; MapMarkers?.ClearServiceTarget();
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
            serviceTargetActive=false; guidanceSuppressed=false; MapMarkers?.ClearServiceTarget();
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
        public void SetServiceDestination(string stableId,string displayName,Vector3 position) =>
            TrySetServiceDestination(stableId,displayName,position,TruckTaxiMapMarkerType.Bathroom);
        public bool SetPrivateStopDestination(TruckTaxiStopObjectivePoint point) => point!=null &&
            point.category==TruckTaxiStopCategory.PrivateMeeting &&
            TrySetServiceDestination(point.stableId,point.displayName,point.Position,TruckTaxiMapMarkerType.PrivateEventStop);
        public bool ClearPrivateStopDestination(string stableId) =>
            serviceTargetActive && serviceTargetType==TruckTaxiMapMarkerType.PrivateEventStop && CompleteServiceDestination(stableId);
        public bool TrySetServiceDestination(string stableId,string displayName,Vector3 position,
            TruckTaxiMapMarkerType type)
        {
            if(string.IsNullOrWhiteSpace(stableId) || navigation==null || player==null || graph==null) return false;
            bool arrived=Vector3.ProjectOnPlane(position-player.position,Vector3.up).sqrMagnitude<=9;
            if(arrived) navigation.ClearRoute();
            else if(!RequestTargetRoute(stableId,position,"taxi.service")) return false;
            TargetId=stableId; target=null; stopTarget=null; serviceTargetActive=true;
            serviceTargetPosition=position; serviceTargetLabel=displayName; serviceTargetType=type; guidanceSuppressed=false;
            MapMarkers?.SetServiceTarget(stableId,displayName,position,type);
            MapMarkers?.RequestRefresh();
            return true;
        }
        private bool RequestTargetRoute(string id,Vector3 position,string purpose)
        {
            var result=navigation.RequestRoute(new LwsRouteRequest {
                requestId=purpose,destinationId=id,useOriginWorldPosition=true,originWorldPosition=player.position,
                useDestinationWorldPosition=true,destinationWorldPosition=position,truckRouteRequired=true
            },graph.Graph);
            if(!result.succeeded) Debug.LogWarning("Truck Taxi GPS route: "+result.message,this);
            return result.succeeded;
        }
        public void StopNavigation()
        {
            if(!HasNavigationTarget || guidanceSuppressed) return;
            guidanceSuppressed=true;
            navigation?.ClearRoute();
            MapMarkers?.RequestRefresh();
        }
        public bool RestoreRideRoute()
        {
            if(!guidanceSuppressed || serviceTargetActive) return false;
            var session=GetComponent<TruckTaxiBootstrap>()?.Session;
            if(session==null || string.IsNullOrEmpty(session.CurrentRideId)) return false;
            if(session.SafeDropRequested) SetSafeDropDestination(session.CurrentDesiredDestination);
            else if(session.ActiveStop?.StopPoint!=null) SetStopDestination(session.ActiveStop.StopPoint);
            else if(session.HasPassenger) SetRideDestination(session.CurrentDesiredDestination);
            else SetPickupDestination(session.Pickup);
            return !guidanceSuppressed;
        }
        public bool RestoreServiceRoute()
        {
            if(!guidanceSuppressed || !serviceTargetActive) return false;
            return TrySetServiceDestination(TargetId,serviceTargetLabel,serviceTargetPosition,serviceTargetType);
        }
        public void SetSafeDropDestination(TruckTaxiRideLocation location) => SetDestination(location,"safe-drop");
        // Session state transitions only release ride-owned navigation. An offer preview
        // never claims a route, so declining it leaves the exact service route in place.
        public void ClearRideDestination()
        {
            if(!serviceTargetActive) ClearDestination();
        }
        public bool CompleteServiceDestination(string stableId)
        {
            if(!serviceTargetActive || TargetId!=stableId) return false;
            ClearDestination();
            return true;
        }
        public void ClearDestination()
        { target=null; stopTarget=null; serviceTargetActive=false; guidanceSuppressed=false; TargetId=""; MapMarkers?.ClearServiceTarget(); navigation?.ClearRoute(); }
    }
}
