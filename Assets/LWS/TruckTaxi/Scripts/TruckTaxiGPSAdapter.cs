using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.Events;

namespace LWS.TruckTaxi
{
    // After all Update policy writes, before Compass's default-order LateUpdate capture.
    [DefaultExecutionOrder(-50)]
    public sealed class TruckTaxiGPSAdapter : MonoBehaviour
    {
        private ILwsNavigationService navigation;
        private LwsRoadGraphProvider graph;
        private Transform player;
        private bool guidanceSuppressed;
        private sealed class Destination
        {
            public string Id, Label;
            public Vector3 Point;
            public TruckTaxiMapMarkerType Type;
            public TruckTaxiRideLocation Ride;
            public TruckTaxiStopObjectivePoint Stop;
            public TruckTaxiServicePoint Service;
            public float Radius=3, MaximumSpeed=TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond;
            public bool RequiresCompletion;
            public Vector3 Position => Service!=null ? Service.Position : Ride!=null ? Ride.StopPosition : Stop!=null ? Stop.Position : Point;
            public bool Contains(Vector3 position,float speed)
            {
                if(Service!=null) return Service.CanUse(position,speed,MaximumSpeed);
                if(Ride!=null) return Ride.Contains(position);
                if(Stop!=null) return Stop.IsValidStop(position,speed);
                return float.IsFinite(speed) && speed<=MaximumSpeed && Mathf.Abs(position.y-Position.y)<=3 &&
                    Vector3.ProjectOnPlane(position-Position,Vector3.up).sqrMagnitude<=Radius*Radius;
            }
        }
        private readonly TruckTaxiNavigationIntents<Destination> intents=new TruckTaxiNavigationIntents<Destination>();
        private Rigidbody playerBody;
        private System.Reflection.PropertyInfo hudFollow,cabFollow;
        public event System.Action NavigationChanged;
        public TruckTaxiNavigationLayer? ActiveLayer => intents.Active?.Layer;
        public long ActiveLeaseToken => intents.Active?.Token ?? 0;
        public string NavigationStack
        {
            get { var lines=new System.Collections.Generic.List<string>(); foreach(var lease in intents.TopFirst) lines.Add(lease.ToString()); return string.Join("\n",lines); }
        }
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
        private UnityEvent beforeMapCapture,afterMapCapture;
        private UnityEvent beforeCabCapture,afterCabCapture;
        private Camera captureCamera;
        private CameraType captureCameraType;
        private UnityEngine.Rendering.Universal.UniversalAdditionalCameraData captureData;
        private bool capturePostProcessing;
        private float nextRouteCheck;
        private TruckTaxiBootstrap host;
        private bool presentationRefreshPending=true;
        private float observedTimeScale,nextPresentationCheck;
        private LwsRouteResult observedRoute;
        private LwsGameClockSnapshot observedClock;
        private CompassPresentation hudPresentation,cabPresentation;
        private Vector2 lastCabMapSize,lastHudMapSize;
        private float regularPreviewDragDistance=-1;
        // Vendor "pixel" widths are local UI units, not final display pixels. A 240-unit
        // reference gives the default route 5% and a normal POI 8% of the map's short side.
        private const float MapStyleReferenceSize=240f;
        public TruckTaxiGPSDisplaySettings DisplaySettings { get; private set; }
        public Component CabCompass => cabCompass;
        public Component HudCompass => previewCompass;
        public RectTransform MapScreenRect => hudMapRect;
        public bool HudVisible => DisplaySettings != null && DisplaySettings.showHud;
        public bool FullMapOpen => fullMapOpen;
        public float FullMapZoomLevel => previewCompass?.GetType().GetProperty("miniMapFullScreenZoomLevel")?.GetValue(previewCompass) is float value ? value : 0;
        public int BakedTileCount => tileLayer!=null ? tileLayer.transform.childCount : 0;
        public bool BakedTilesVisible => fullMapOpen && tileLayer!=null && tileLayer.IsAssignedTo(PreviewCamera) && BakedTileCount>0;
        public event System.Action DisplaySettingsChanged;
        public RectTransform DashboardScreen { get; private set; }
        public TruckTaxiMapMarkers MapMarkers { get; private set; }
        public Camera PreviewCamera => previewCompass != null ? previewCameraField?.GetValue(previewCompass) as Camera : null;
        public string TargetId => intents.Active?.TargetId ?? "";
        public string TargetLabel => intents.Active?.Value.Label ?? "";
        public float TargetDistanceMeters => player!=null && intents.Active!=null
            ? Vector3.Distance(player.position,intents.Active.Value.Position) : 0;
        public string CompanionDestinationLabel => intents.Get(TruckTaxiNavigationLayer.CompanionTask)?.Value.Label ?? "";
        public float CompanionDistanceMeters
        {
            get
            {
                var lease=intents.Get(TruckTaxiNavigationLayer.CompanionTask);
                return player!=null && lease!=null ? Vector3.Distance(player.position,lease.Value.Position) : 0;
            }
        }
        public bool IsServiceDestination => ActiveLayer==TruckTaxiNavigationLayer.PlayerService ||
            ActiveLayer==TruckTaxiNavigationLayer.CompanionTask;
        public bool GuidanceSuppressed => guidanceSuppressed;
        public bool HasNavigationTarget => !string.IsNullOrEmpty(TargetId);
        public bool HasReachedServiceDestination => IsServiceDestination && player!=null &&
            (intents.Active?.Value.Contains(player.position,playerBody!=null ? playerBody.linearVelocity.magnitude : 0) ?? false);
        public bool RouteReady => !guidanceSuppressed && player!=null && intents.Active!=null &&
            (intents.Active.Value.Contains(player.position,playerBody!=null ? playerBody.linearVelocity.magnitude : 0) ||
            (navigation?.CurrentRoute != null && navigation.CurrentRoute.succeeded && navigation.CurrentRoute.destinationId==TargetId));
        public void Initialize(Transform tractor, LwsRoadGraphProvider provider)
        {
            player = CanonicalTruck(tractor); graph = provider;
            host=GetComponent<TruckTaxiBootstrap>();
            playerBody=player!=null ? player.GetComponent<Rigidbody>() : null;
            intents.Trace+=LogNavigation;
            DisplaySettings = TruckTaxiGPSDisplaySettings.Load();
            LwsApplicationBootstrap.Instance.Registry.TryGet(out navigation);
        }
        public static Transform CanonicalTruck(Transform candidate)
        {
            var body=candidate!=null ? candidate.GetComponentInParent<Rigidbody>() : null;
            return body!=null ? body.transform : candidate;
        }
        private void LogNavigation(string message)
        { if(Debug.isDebugBuild) Debug.Log(message,this); }
        [ContextMenu("Show Navigation Stack")]
        public void ShowNavigationStack() => Debug.Log(string.IsNullOrEmpty(NavigationStack) ? "NAV EMPTY" : NavigationStack,this);
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
                // Compass's shared truck setup uses continuous world-camera renders. The
                // vendor's timed snapshots keep both real maps live without rendering twice
                // on every driving frame; icons and route graphics still update normally.
                SetEnum(component,"miniMapCameraSnapshotFrequency","TimeInterval");
                Set(component,"miniMapSnapshotInterval",canvas.renderMode==RenderMode.WorldSpace ? .15f : .10f);
                SetEnum(component,"miniMapStyle","SolidBox");
                Set(component,"follow",player);
                SetEnum(component,"miniMapOrientation","Follow");
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
            hudFollow=previewCompass?.GetType().GetProperty("follow");
            cabFollow=cabCompass?.GetType().GetProperty("follow");
            hudPresentation=new CompassPresentation(previewCompass);
            cabPresentation=new CompassPresentation(cabCompass);
            beforeMapCapture=previewCompass?.GetType().GetField("OnMiniMapBeforeCapture")?.GetValue(previewCompass) as UnityEvent;
            afterMapCapture=previewCompass?.GetType().GetField("OnMiniMapAfterCapture")?.GetValue(previewCompass) as UnityEvent;
            beforeMapCapture?.AddListener(BeginMapCapture);
            afterMapCapture?.AddListener(EndMapCapture);
            beforeCabCapture=cabCompass?.GetType().GetField("OnMiniMapBeforeCapture")?.GetValue(cabCompass) as UnityEvent;
            afterCabCapture=cabCompass?.GetType().GetField("OnMiniMapAfterCapture")?.GetValue(cabCompass) as UnityEvent;
            beforeCabCapture?.AddListener(BeginCabCapture);
            afterCabCapture?.AddListener(EndMapCapture);
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
            if(previewing && regularPreviewDragDistance<0)
                regularPreviewDragDistance=(float)previewCompass.GetType().GetProperty("miniMapDragMaxDistance").GetValue(previewCompass);
            ApplyDisplaySettings(false);
            if(offer==null)
            {
                if(regularPreviewDragDistance>=0)
                {
                    Set(previewCompass,"miniMapFollowOffset",Vector3.zero);
                    Set(previewCompass,"miniMapDragMaxDistance",regularPreviewDragDistance);
                    regularPreviewDragDistance=-1;
                }
                return;
            }
            var bounds=new Bounds(offer.PlayerPosition,Vector3.zero);
            // Road routing can terminate at the nearest node, outside the authored stop's bay.
            bounds.Encapsulate(offer.Pickup.StopPosition);
            bounds.Encapsulate(offer.Destination.StopPosition);
            foreach(var point in offer.ToPickup.Points) bounds.Encapsulate(point);
            foreach(var point in offer.Trip.Points) bounds.Encapsulate(point);
            previewCenter=bounds.center;
            // Compass clamps follow offsets using its drag limit, including programmatic framing.
            Set(previewCompass,"miniMapDragMaxDistance",Mathf.Max(regularPreviewDragDistance,
                Vector3.ProjectOnPlane(previewCenter-player.position,Vector3.up).magnitude+1));
            Set(previewCompass,"miniMapCaptureSize",Mathf.Max(160,Mathf.Max(bounds.size.x,bounds.size.z)*1.3f));
            UpdatePreviewCenter();
            RequestPresentationRefresh();
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
                // Panning and zooming the modal map must stay responsive even while
                // gameplay time is paused. Only the ordinary driving HUD is throttled.
                SetEnum(previewCompass,"miniMapCameraSnapshotFrequency",previewing || fullMapOpen ? "Continuous" : "TimeInterval");
                Set(previewCompass,"miniMapSize",DisplaySettings.hudSize);
                if(!previewing && !fullMapOpen) followOffsetProperty?.SetValue(previewCompass,Vector3.zero);
                // Offer preview keeps using this camera even when the player's HUD is off.
                // Only suppress the duplicate canvas while that modal map owns the texture.
                previewCanvas.enabled=fullMapOpen || (!previewing && !transientHudHidden);
            }
            cameraPresentation?.SetGpsPresentationPolicy(fullMapOpen || previewing || (DisplaySettings.showHud && !transientHudHidden)
                ? LwsGpsPresentationPolicy.ForceHudMinimapOn : LwsGpsPresentationPolicy.ForceHudMinimapOff);
            RefreshMapElementSizing(true);
            RequestPresentationRefresh();
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
            // LWS owns route completion, including suspended temporary destinations.
            Set(compass,"routeClearOnComplete",false);
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
            RequestPresentationRefresh();
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
            // The base adapter updates camera policy in Update. Rebind both displays to the truck,
            // never the cab camera, player-arrow UI, or a steering-wheel visual.
            if(player!=null) { hudFollow?.SetValue(previewCompass,player); cabFollow?.SetValue(cabCompass,player); }
            UpdatePreviewCenter();
            if(fullMapOpen) EnableMapTiles();
            // Compare cached rects only; reapply after vendor layout/resolution changes, not every frame.
            RefreshMapElementSizing();
            if(Time.unscaledTime>=nextRouteCheck)
            {
                nextRouteCheck=Time.unscaledTime+.5f;
                ReconcileNavigation();
            }
            if(!guidanceSuppressed && HasReachedServiceDestination && intents.Active?.Value.RequiresCompletion==false)
                CompleteServiceDestination(TargetId,ActiveLeaseToken);
            RefreshPresentationIfNeeded();
        }
        // Coalesce transitions into one public vendor refresh, independent of scaled time.
        public void RequestPresentationRefresh() => presentationRefreshPending=true;
        private void OnEnable() => RequestPresentationRefresh();
        private void OnDisable() { EndMapCapture(); tileLayer?.ShowOnlyFor(null); }
        private void OnApplicationFocus(bool focused) { if(focused) RequestPresentationRefresh(); }
        private void OnApplicationPause(bool paused) { if(!paused) RequestPresentationRefresh(); }
        private void RefreshPresentationIfNeeded()
        {
            var route=navigation?.CurrentRoute;
            if(!ReferenceEquals(route,observedRoute) || observedTimeScale!=Time.timeScale)
                RequestPresentationRefresh();
            observedRoute=route; observedTimeScale=Time.timeScale;
            var clock=host?.Environment?.Clock;
            if(clock!=null)
            {
                var current=clock.CurrentSnapshot;
                float expectedSeconds=Mathf.Max(60,Time.unscaledDeltaTime*Mathf.Max(current.timeScale,observedClock.timeScale)*2);
                if(current.paused!=observedClock.paused || current.isNight!=observedClock.isNight ||
                    System.Math.Abs(current.totalGameSeconds-observedClock.totalGameSeconds)>expectedSeconds ||
                    Mathf.Abs(current.timeOfDayHours-observedClock.timeOfDayHours)*3600>expectedSeconds)
                    RequestPresentationRefresh();
                observedClock=current;
            }
            bool hudBecameVisible=hudPresentation?.BecameVisible()==true;
            bool cabBecameVisible=cabPresentation?.BecameVisible()==true;
            if(hudBecameVisible || cabBecameVisible)
                RequestPresentationRefresh();
            bool validRoute=!guidanceSuppressed && intents.Active!=null && route?.destinationId==TargetId &&
                route.succeeded && route.waypoints?.Count>=2;
            if(Time.unscaledTime>=nextPresentationCheck)
            {
                nextPresentationCheck=Time.unscaledTime+.5f;
                if(hudPresentation?.IsStale(validRoute)==true || cabPresentation?.IsStale(validRoute)==true)
                    RequestPresentationRefresh();
            }
            if(!presentationRefreshPending) return;
            presentationRefreshPending=false;
            // Reuse the registered presenter so origin conversion and endpoint ownership stay in LWS.
            if(validRoute) navigation.PresentCurrentRoute();
            hudPresentation?.Refresh(); cabPresentation?.Refresh();
        }
        private sealed class CompassPresentation
        {
            private readonly Component compass;
            private readonly System.Reflection.MethodInfo refresh;
            private readonly System.Reflection.PropertyInfo showMap,hasRoute,showRoute;
            private readonly System.Reflection.FieldInfo camera;
            private bool wasVisible;
            public CompassPresentation(Component value)
            {
                compass=value;
                if(value==null) return;
                var type=value.GetType();
                refresh=type.GetMethod("UpdateMiniMapContents",new[] { typeof(int) });
                showMap=type.GetProperty("showMiniMap"); hasRoute=type.GetProperty("hasRoute");
                showRoute=type.GetProperty("showRoute"); camera=type.GetField("miniMapCamera");
            }
            private bool Visible => compass!=null && (!(compass is Behaviour behaviour) || behaviour.isActiveAndEnabled) &&
                showMap?.GetValue(compass) is bool visible && visible;
            public bool BecameVisible()
            { bool visible=Visible,changed=visible && !wasVisible; wasVisible=visible; return changed; }
            public bool IsStale(bool expectsRoute)
            {
                if(!Visible) return false;
                var mapCamera=camera?.GetValue(compass) as Camera;
                return (mapCamera!=null && mapCamera.targetTexture!=null && !mapCamera.targetTexture.IsCreated()) ||
                    (expectsRoute && hasRoute?.GetValue(compass) is bool present && !present);
            }
            public void Refresh()
            {
                if(compass==null) return;
                // A snapshot refresh does not invalidate Compass's separate UI route mesh.
                if(showRoute?.GetValue(compass) is bool shown && shown)
                { showRoute.SetValue(compass,false); showRoute.SetValue(compass,true); }
                refresh?.Invoke(compass,new object[] { 1 });
            }
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
            else tileLayer?.ShowOnlyFor(null);
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
            tileLayer.ShowOnlyFor(PreviewCamera);
        }
        private void BeginMapCapture()
        {
            BeginInstrumentCapture(PreviewCamera);
        }
        private void BeginCabCapture() => BeginInstrumentCapture(
            cabCompass?.GetType().GetField("miniMapCamera")?.GetValue(cabCompass) as Camera);
        private void BeginInstrumentCapture(Camera camera)
        {
            if(!isActiveAndEnabled || camera==null) return;
            EndMapCapture();
            captureCamera=camera; captureCameraType=camera.cameraType;
            // Weather Maker's public ShouldIgnoreCamera contract excludes Preview cameras.
            // Scope this to the instrument capture; never change world weather or its allow list.
            camera.cameraType=CameraType.Preview;
            captureData=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if(captureData!=null) { capturePostProcessing=captureData.renderPostProcessing; captureData.renderPostProcessing=false; }
            EnableMapTiles();
            tileLayer?.ShowOnlyFor(camera);
            tileLayer?.BeginCapture();
        }
        private void EndMapCapture()
        {
            tileLayer?.EndCapture();
            if(captureData!=null) captureData.renderPostProcessing=capturePostProcessing;
            if(captureCamera!=null) captureCamera.cameraType=captureCameraType;
            captureData=null; captureCamera=null;
            tileLayer?.ShowOnlyFor(PreviewCamera);
        }
        public void CenterFullMapOnPlayer()
        {
            if(!fullMapOpen || previewCompass==null || player==null) return;
            Set(previewCompass,"miniMapFullScreenWorldCenter",player.position);
            Set(previewCompass,"miniMapFollowOffset",Vector3.zero);
            RequestPresentationRefresh();
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
        public bool CanSetMapServiceDestination => navigation!=null && player!=null;
        public bool TrySetMapServiceDestination(string id,string label,Vector3 position)
        {
            if(!CanSetMapServiceDestination || string.IsNullOrWhiteSpace(id)) return false;
            var type=TruckTaxiMapMarkerType.Place;
            if(MapMarkers!=null)
            {
                foreach(var point in MapMarkers.RegionalPoints)
                    if(point.Id==id) { if(!point.Routeable) return false; type=point.IconType; break; }
                foreach(var marker in MapMarkers.Markers)
                    if(marker!=null && marker.stableId==id) { type=marker.markerType; break; }
            }
            return TrySetServiceDestination(id,label,position,type);
        }
        private void OnDestroy()
        {
            intents.Trace-=LogNavigation;
            beforeMapCapture?.RemoveListener(BeginMapCapture);
            afterMapCapture?.RemoveListener(EndMapCapture);
            beforeCabCapture?.RemoveListener(BeginCabCapture);
            afterCabCapture?.RemoveListener(EndMapCapture);
            EndMapCapture();
            tileLayer?.ShowOnlyFor(null);
            if(cameraPresentation!=null) cameraPresentation.SetGpsPresentationPolicy(originalPolicy);
        }
        public void SetPickupDestination(TruckTaxiRideLocation location) => SetDestination(location,"pickup");
        public void SetRideDestination(TruckTaxiRideLocation location) => SetDestination(location,"dropoff");
        public void SetStopDestination(TruckTaxiStopObjectivePoint point)
        {
            if(point==null) return;
            Claim(TruckTaxiNavigationLayer.PassengerDiversion,"passenger",new Destination {
                Id=point.stableId,Label=point.displayName,Point=point.Position,Stop=point,
                Type=TruckTaxiMapIconRegistry.ForStop(point.category),RequiresCompletion=true
            },false);
        }
        public bool ClearStopDestination(string stableId=null) =>
            ReleaseNavigationIntent(TruckTaxiNavigationLayer.PassengerDiversion,"passenger",stableId);
        private void SetDestination(TruckTaxiRideLocation location,string purpose)
        {
            if(location==null) return;
            bool removedActivePickup=purpose!="pickup" && ActiveLayer==TruckTaxiNavigationLayer.RidePickup;
            if(purpose!="pickup") intents.Release(TruckTaxiNavigationLayer.RidePickup,"ride");
            var layer=purpose=="safe-drop" ? TruckTaxiNavigationLayer.Emergency :
                purpose=="pickup" ? TruckTaxiNavigationLayer.RidePickup : TruckTaxiNavigationLayer.RideFinal;
            Claim(layer,
                purpose=="safe-drop" ? "ride-safe-drop" : "ride",new Destination {
                    Id=location.locationId,Label=location.locationName,Point=location.StopPosition,Ride=location,
                    Type=purpose=="pickup" ? TruckTaxiMapMarkerType.PassengerPickup : TruckTaxiMapMarkerType.Dropoff,
                    RequiresCompletion=true
                },false);
            if(removedActivePickup && ActiveLayer!=layer) ApplyActive();
        }
        public void SetServiceDestination(string stableId,string displayName,Vector3 position) =>
            TrySetServiceDestination(stableId,displayName,position,
                TruckTaxiServicePresentation.Find(stableId)!=null ? TruckTaxiMapMarkerType.ServiceArea : TruckTaxiMapMarkerType.Place);
        public bool SetPrivateStopDestination(TruckTaxiStopObjectivePoint point)
        {
            if(point==null || point.category!=TruckTaxiStopCategory.PrivateMeeting) return false;
            return Claim(TruckTaxiNavigationLayer.CompanionTask,"companion",new Destination {
                Id=point.stableId,Label=point.displayName,Point=point.Position,Stop=point,
                Type=TruckTaxiMapMarkerType.PrivateEventStop,RequiresCompletion=true
            },true);
        }
        public bool ClearPrivateStopDestination(string stableId)
        {
            return ReleaseNavigationIntent(TruckTaxiNavigationLayer.CompanionTask,"companion",stableId);
        }
        public bool TrySetServiceDestination(string stableId,string displayName,Vector3 position,TruckTaxiMapMarkerType type)
        {
            var service=TruckTaxiServicePresentation.Find(stableId);
            if(service!=null && !TruckTaxiServicePresentation.IsUsable(service)) return false;
            if(TruckTaxiServicePresentation.RequiresCapability(type) &&
                !TruckTaxiServicePresentation.Matches(service,type)) return false;
            TruckTaxiRideLocation ride=null;
            TruckTaxiStopObjectivePoint stop=null;
            if(service==null && MapMarkers!=null)
                foreach(var marker in MapMarkers.Markers)
                    if(marker!=null && marker.stableId==stableId && marker.source!=null)
                    { ride=marker.source.GetComponent<TruckTaxiRideLocation>(); stop=marker.source.GetComponent<TruckTaxiStopObjectivePoint>(); break; }
            return Claim(TruckTaxiNavigationLayer.PlayerService,"player-service",new Destination {
                Id=stableId,Label=displayName,Point=position,Service=service,Stop=stop,
                Radius=ride!=null ? ride.detectionRadius : 3,
                Type=service!=null ? TruckTaxiServicePresentation.MarkerType(service.capabilities) : type,
                RequiresCompletion=service!=null || type==TruckTaxiMapMarkerType.PrivateEventStop
            },true);
        }
        // Passenger requests and short-lived emergencies may claim/release a named lease without touching other layers.
        public bool TrySetNavigationIntent(TruckTaxiNavigationLayer layer,string ownerId,string stableId,
            string label,Vector3 position,TruckTaxiMapMarkerType type,float radius=3,bool requiresCompletion=true)
        {
            if(layer==TruckTaxiNavigationLayer.PlayerService) ownerId="player-service";
            var service=TruckTaxiServicePresentation.Find(stableId);
            if(TruckTaxiServicePresentation.RequiresCapability(type) &&
                !TruckTaxiServicePresentation.Matches(service,type)) return false;
            return Claim(layer,ownerId,new Destination { Id=stableId,Label=label,Point=position,Type=type,Service=service,
                Radius=Mathf.Max(.1f,radius),RequiresCompletion=service!=null || requiresCompletion },true);
        }
        private bool Claim(TruckTaxiNavigationLayer layer,string owner,Destination destination,bool requireRoute)
        {
            if(string.IsNullOrWhiteSpace(destination.Id) || string.IsNullOrWhiteSpace(owner)) return false;
            var existing=intents.Get(layer);
            if(existing!=null && existing.OwnerId==owner && existing.TargetId==destination.Id &&
                existing.Value.Position==destination.Position && existing.Value.Type==destination.Type)
                return existing==intents.Active ? RestoreActiveRoute() : true;
            var prior=intents.Active;
            bool becomesActive=prior==null || layer>=prior.Layer;
            if(requireRoute && becomesActive && !PresentRoute(destination))
            {
                if(prior!=null && !guidanceSuppressed) PresentRoute(prior.Value);
                else navigation?.ClearRoute();
                return false;
            }
            intents.Set(layer,owner,destination.Id,destination);
            if(becomesActive)
            {
                guidanceSuppressed=false;
                ApplyActive(!requireRoute);
            }
            return true;
        }
        private bool PresentRoute(Destination destination)
        {
            if(navigation==null || player==null) return false;
            if(destination.Contains(player.position,playerBody!=null ? playerBody.linearVelocity.magnitude : 0))
            { navigation.ClearRoute(); return true; }
            if(graph==null) return false;
            return RequestTargetRoute(destination.Id,destination.Position,"taxi."+intents.Active?.Layer);
        }
        private void ApplyActive(bool requestRoute=true)
        {
            var active=intents.Active;
            MapMarkers?.ClearServiceTarget();
            if(active==null) { guidanceSuppressed=false; navigation?.ClearRoute(); }
            else
            {
                // A single endpoint owns the active treatment, irrespective of which layer owns it.
                MapMarkers?.SetServiceTarget(active.TargetId,active.Value.Label,active.Value.Position,active.Value.Type);
                if(requestRoute && !guidanceSuppressed && !PresentRoute(active.Value)) navigation?.ClearRoute();
            }
            MapMarkers?.RequestRefresh();
            RequestPresentationRefresh();
            NavigationChanged?.Invoke();
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
            RequestPresentationRefresh();
            NavigationChanged?.Invoke();
        }
        public bool RestoreRideRoute() => RestoreActiveRoute();
        public bool RestoreServiceRoute() => RestoreActiveRoute();
        // Called periodically and available to the load coordinator after restoring semantic owners.
        // Explicit Stop Navigation remains authoritative until a new intent or manual restore.
        public void ReconcileNavigation()
        {
            if(guidanceSuppressed || navigation==null || player==null) return;
            var active=intents.Active;
            if(active==null)
            {
                if(navigation.CurrentRoute!=null) { navigation.ClearRoute(); RequestPresentationRefresh(); }
                return;
            }
            bool arrived=active.Value.Contains(player.position,playerBody!=null ? playerBody.linearVelocity.magnitude : 0);
            if(arrived)
            {
                if(navigation.CurrentRoute!=null) { navigation.ClearRoute(); RequestPresentationRefresh(); }
            }
            else if(navigation.CurrentRoute?.succeeded!=true || navigation.CurrentRoute.destinationId!=active.TargetId)
                ApplyActive();
        }
        private bool RestoreActiveRoute()
        {
            if(intents.Active==null) return false;
            if((guidanceSuppressed || navigation?.CurrentRoute?.succeeded!=true ||
                navigation.CurrentRoute.destinationId!=intents.Active.TargetId) && !PresentRoute(intents.Active.Value)) return false;
            guidanceSuppressed=false;
            ApplyActive(false);
            return true;
        }
        public void SetSafeDropDestination(TruckTaxiRideLocation location) => SetDestination(location,"safe-drop");
        // Session transitions release only ride-owned entries, including suspended ones.
        public void ClearRideDestination()
        {
            var prior=intents.Active;
            intents.Release(TruckTaxiNavigationLayer.RideFinal,"ride");
            intents.Release(TruckTaxiNavigationLayer.RidePickup,"ride");
            intents.Release(TruckTaxiNavigationLayer.PassengerDiversion,"passenger");
            intents.Release(TruckTaxiNavigationLayer.Emergency,"ride-safe-drop");
            if(prior!=intents.Active) ApplyActive();
        }
        public bool ReleaseNavigationIntent(TruckTaxiNavigationLayer layer,string ownerId,string stableId=null,long token=0)
        {
            var prior=intents.Active;
            if(!intents.Release(layer,ownerId,stableId,token)) return false;
            if(prior!=intents.Active) { guidanceSuppressed=false; ApplyActive(); }
            return true;
        }
        public bool CompleteServiceDestination(string stableId,long token=0) =>
            ReleaseNavigationIntent(TruckTaxiNavigationLayer.PlayerService,"player-service",stableId,token);
        public bool ClearPlayerServiceDestination() =>
            ReleaseNavigationIntent(TruckTaxiNavigationLayer.PlayerService,"player-service");
        // Only whole-session reset/load should discard every layer.
        public void ClearDestination()
        {
            intents.Clear();
            ApplyActive();
        }
    }
}
