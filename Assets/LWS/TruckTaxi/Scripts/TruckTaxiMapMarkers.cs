using System;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    // Typed presentation for the existing Compass instances. No camera, route computation or navigation state.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiMapMarkers : MonoBehaviour
    {
        public TruckTaxiMapIconRegistry registry;
        [SerializeField] private List<TruckTaxiMapMarker> markers=new List<TruckTaxiMapMarker>();
        public IReadOnlyList<TruckTaxiMapMarker> Markers
        {
            get { PruneDestroyedMarkers(); return markers; }
        }
        public TruckTaxiMapIconRegistry Registry => registry;
        private readonly Dictionary<string,TruckTaxiMapMarker> byId=new Dictionary<string,TruckTaxiMapMarker>(StringComparer.Ordinal);
        public readonly struct RegionalPoint
        {
            public readonly string Id,Label,Kind;
            public readonly Vector3 Position;
            public readonly TruckTaxiMapMarkerType IconType;
            public readonly bool Routeable;
            public RegionalPoint(string id,string label,string kind,Vector3 position,TruckTaxiMapMarkerType iconType,bool routeable)
            { Id=id; Label=label; Kind=kind; Position=position; IconType=iconType; Routeable=routeable; }
        }
        private readonly Dictionary<string,RegionalPoint> regionalPoints=new Dictionary<string,RegionalPoint>(StringComparer.Ordinal);
        private readonly HashSet<string> syntheticRegionalIds=new HashSet<string>(StringComparer.Ordinal);
        public IReadOnlyCollection<RegionalPoint> RegionalPoints => regionalPoints.Values;
        private readonly HashSet<string> completed=new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> discovered=new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> warnedDuplicates=new HashSet<string>(StringComparer.Ordinal);
        private readonly List<TruckTaxiMapMarker> nearby=new List<TruckTaxiMapMarker>();
        private readonly List<TruckTaxiMapMarker> stops=new List<TruckTaxiMapMarker>(),shortcuts=new List<TruckTaxiMapMarker>(),impacts=new List<TruckTaxiMapMarker>();
        private readonly List<TruckTaxiImpactTarget> propertyTargets=new List<TruckTaxiImpactTarget>();
        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld regionalWorld;
        private TruckTaxiGPSAdapter gps;
        private CompassApi api;
        private Transform markerRoot,legacyRoot;
        private float nextRefresh,nextTrafficRefresh;
        private bool dirty;
        private Component endpoint;
        private TruckTaxiMapMarker serviceTarget;
        private TruckTaxiMapMarkerState serviceTargetOriginalState;
        private bool serviceTargetReused;
        private string presentationRide;
        private const string DiscoveryPrefix="TruckTaxi.Map.Shortcut.v1.";

        public void Initialize(TruckTaxiBootstrap value,TruckTaxiGPSAdapter adapter)
        {
            if(host!=null || value?.Session==null || value.Player==null) return;
            registry=registry!=null ? registry : Resources.Load<TruckTaxiMapIconRegistry>(TruckTaxiMapIconRegistry.ResourcePath);
            if(registry==null) { Debug.LogError("Truck Taxi map icons missing. Run TruckTaxiMapIconSetup.EnsureAssets before building.",this); return; }
            host=value; gps=adapter; regionalWorld=host.GetComponent<TruckTaxiRegionalWorld>();
            api=new CompassApi(gps.CabCompass ?? gps.HudCompass);
            if(!api.Available) { Debug.LogError("Truck Taxi markers require the existing CompassProPOI public API.",this); return; }
            markerRoot=new GameObject("Truck Taxi typed Compass POIs").transform; markerRoot.SetParent(transform,false);
            var presenter=host.Player.GetComponentInChildren<LwsCompassNavigatorProAdapter>(true);
            if(presenter!=null) legacyRoot=presenter.transform.Find("IH Compass Navigator Pro POIs");
            // One initialization inventory. Moving markers follow cached transforms; no scene scans in Update.
            foreach(var authored in FindObjectsByType<TruckTaxiMapMarker>(FindObjectsSortMode.None))
                if(authored.gameObject.scene==host.gameObject.scene) Register(authored);
            foreach(var point in regionalPoints.Values) EnsureRegionalPoint(point);
            foreach(var stop in FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None))
                if(stop.gameObject.scene==host.gameObject.scene && stop.isActiveAndEnabled)
                    stops.Add(Ensure(stop.stableId,stop.displayName,TruckTaxiMapIconRegistry.ForStop(stop.category),stop.transform,stop.Position));
            RefreshStreamedContent();
            host.Session.Changed+=RequestRefresh; host.Session.DestinationChanged+=RequestRefresh;
            host.Session.RequestCreated+=OnRequest; host.Session.RequestResolved+=OnRequest;
            ConfigureCompass(gps.CabCompass); ConfigureCompass(gps.HudCompass);
            Refresh();
        }
        public bool Register(TruckTaxiMapMarker marker)
        {
            if(marker==null || string.IsNullOrWhiteSpace(marker.stableId)) return false;
            PruneDestroyedMarkers();
            if(byId.TryGetValue(marker.stableId,out var existing))
            {
                if(existing==null) { byId.Remove(marker.stableId); markers.Remove(existing); }
                else
                {
                    if(existing!=marker) Debug.LogWarning("Duplicate Truck Taxi map marker ID: "+marker.stableId,marker);
                    return existing==marker;
                }
            }
            byId.Add(marker.stableId,marker); markers.Add(marker); marker.Owner=this; dirty=true; return true;
        }
        // Parent calls on regional AvailabilityChanged. Only known additive scenes and the host
        // are inventoried; this method never requests a scene load or scans every frame.
        public void RefreshStreamedContent()
        {
            if(host==null || markerRoot==null) return;
            for(int i=shortcuts.Count-1;i>=0;i--)
            {
                var marker=shortcuts[i];
                if(marker!=null && marker.source!=null && AllowedScene(marker.source.gameObject.scene)) continue;
                shortcuts.RemoveAt(i);
                if(marker==null) continue;
                Unregister(marker);
                if(Application.isPlaying) Destroy(marker.gameObject); else DestroyImmediate(marker.gameObject);
            }
            propertyTargets.RemoveAll(target=>target==null || !AllowedScene(target.gameObject.scene));
            foreach(var scene in AllowedScenes()) foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var shortcut in root.GetComponentsInChildren<ShortcutTrigger>(true))
                {
                    if(!shortcut.isActiveAndEnabled || shortcut.scenicPoint || string.IsNullOrEmpty(shortcut.shortcutId)) continue;
                    var marker=Ensure(shortcut.shortcutId,shortcut.displayName,TruckTaxiMapMarkerType.Shortcut,shortcut.transform,shortcut.transform.position);
                    if(marker==null) continue;
                    if(PlayerPrefs.GetInt(DiscoveryPrefix+shortcut.shortcutId,0)!=0) discovered.Add(shortcut.shortcutId);
                    marker.markerType=discovered.Contains(shortcut.shortcutId) ? TruckTaxiMapMarkerType.DiscoveredShortcut : TruckTaxiMapMarkerType.Shortcut;
                    if(!shortcuts.Contains(marker)) shortcuts.Add(marker);
                    (shortcut.GetComponent<TruckTaxiShortcutMapDiscovery>() ?? shortcut.gameObject.AddComponent<TruckTaxiShortcutMapDiscovery>()).Initialize(shortcut,this);
                }
                foreach(var target in root.GetComponentsInChildren<TruckTaxiImpactTarget>(true))
                    if(target.isActiveAndEnabled && target.kind==TaxiImpactKind.Property && !propertyTargets.Contains(target)) propertyTargets.Add(target);
            }
            dirty=true; nextTrafficRefresh=0;
        }
        private IEnumerable<Scene> AllowedScenes()
        {
            var home=host.gameObject.scene;
            if(home.IsValid() && home.isLoaded) yield return home;
            if(regionalWorld==null) yield break;
            foreach(var region in regionalWorld.regions)
            {
                if(region==null || string.IsNullOrEmpty(region.sceneName)) continue;
                var scene=SceneManager.GetSceneByName(region.sceneName);
                if(scene.IsValid() && scene.isLoaded && scene!=home) yield return scene;
            }
        }
        private bool AllowedScene(Scene scene)
        {
            if(!scene.IsValid() || !scene.isLoaded) return false;
            if(scene==host.gameObject.scene) return true;
            if(regionalWorld==null) return false;
            foreach(var region in regionalWorld.regions)
                if(region!=null && region.sceneName==scene.name) return true;
            return false;
        }
        // Registration is metadata-only until initialization; it never asks Scene Streamer to load a chunk.
        public bool RegisterRegionalPoint(string id,string label,string kind,Vector3 position,TruckTaxiMapMarkerType iconType,bool routeable=false)
        {
            if(string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label) || regionalPoints.ContainsKey(id)) return false;
            var point=new RegionalPoint(id,label,kind,position,iconType,routeable);
            regionalPoints.Add(id,point);
            if(markerRoot!=null) EnsureRegionalPoint(point);
            dirty=true;
            return true;
        }
        public bool UnregisterRegionalPoint(string id)
        {
            if(!regionalPoints.Remove(id)) return false;
            if(syntheticRegionalIds.Remove(id) && byId.TryGetValue(id,out var marker) && marker!=null)
            { Unregister(marker); if(Application.isPlaying) Destroy(marker.gameObject); else DestroyImmediate(marker.gameObject); }
            dirty=true;
            return true;
        }
        private void EnsureRegionalPoint(RegionalPoint point)
        {
            if(byId.ContainsKey(point.Id)) return;
            var marker=Ensure(point.Id,point.Label,point.IconType,null,point.Position);
            if(marker!=null) { marker.state=TruckTaxiMapMarkerState.Known; syntheticRegionalIds.Add(point.Id); }
        }
        public bool Unregister(TruckTaxiMapMarker marker)
        {
            // A destroyed Unity object still holds our managed identity and owned POI reference.
            if(ReferenceEquals(marker,null) || !byId.TryGetValue(marker.stableId,out var existing) || !ReferenceEquals(existing,marker)) return false;
            byId.Remove(marker.stableId); markers.Remove(marker); stops.Remove(marker); shortcuts.Remove(marker); impacts.Remove(marker);
            if(serviceTarget==marker) serviceTarget=null;
            if(marker.CompassPoi!=null)
            {
                marker.CompassPoi.gameObject.SetActive(false);
                if(Application.isPlaying) Destroy(marker.CompassPoi.gameObject); else DestroyImmediate(marker.CompassPoi.gameObject);
            }
            marker.Owner=null; marker.BindPresentation(null,null,false); dirty=true; return true;
        }
        private void PruneDestroyedMarkers()
        {
            // OnDestroy is not guaranteed for never-activated objects or ordinary EditMode fixtures.
            for(int i=markers.Count-1;i>=0;i--)
                if(markers[i]==null && !Unregister(markers[i])) markers.RemoveAt(i);
        }
        public Component RenderedPoi(TruckTaxiMapMarker marker) => marker!=null && marker.Presented &&
            endpoint!=null && marker.stableId==gps?.TargetId ? endpoint : marker?.CompassPoi;
        private TruckTaxiMapMarker Ensure(string id,string label,TruckTaxiMapMarkerType type,Transform source,Vector3 position)
        {
            if(string.IsNullOrWhiteSpace(id)) return null;
            if(!byId.TryGetValue(id,out var marker) || marker==null)
            {
                var go=new GameObject("Map: "+id); go.transform.SetParent(markerRoot,false);
                marker=go.AddComponent<TruckTaxiMapMarker>(); marker.stableId=id; Register(marker);
            }
            else if(marker.source!=null && source!=null && marker.source!=source)
            {
                if(warnedDuplicates.Add(id)) Debug.LogWarning("Truck Taxi map sources share stable ID: "+id+". Keeping the first registered source.",source);
                return null;
            }
            marker.label=label; marker.markerType=type; marker.source=source;
            marker.sourceLocalOffset=source!=null ? source.InverseTransformPoint(position) : Vector3.zero;
            marker.transform.position=position; return marker;
        }
        public bool IsShortcutDiscovered(string id) => !string.IsNullOrEmpty(id) && discovered.Contains(id);
        public static string ShortcutPreferenceKey(string id) => DiscoveryPrefix+id;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Probe-only transient reset. The caller separately restores exact preference key existence/value.
        public void RestoreShortcutDiscoveryForProbe(string id,bool wasDiscovered)
        {
            if(wasDiscovered) discovered.Add(id); else discovered.Remove(id);
            if(byId.TryGetValue(id,out var marker) && marker!=null)
                marker.markerType=wasDiscovered ? TruckTaxiMapMarkerType.DiscoveredShortcut : TruckTaxiMapMarkerType.Shortcut;
            dirty=true;
        }
#endif
        public bool DiscoverShortcut(string id)
        {
            if(string.IsNullOrEmpty(id) || !byId.TryGetValue(id,out var marker) ||
                (marker.markerType!=TruckTaxiMapMarkerType.Shortcut && marker.markerType!=TruckTaxiMapMarkerType.DiscoveredShortcut) || !discovered.Add(id)) return false;
            PlayerPrefs.SetInt(DiscoveryPrefix+id,1); PlayerPrefs.Save();
            marker.markerType=TruckTaxiMapMarkerType.DiscoveredShortcut; dirty=true; return true;
        }
        private void ConfigureCompass(Component compass)
        {
            if(compass==null) return;
            var player=registry.Find(TruckTaxiMapMarkerType.Player);
            if(player!=null) { TruckTaxiGPSAdapter.Set(compass,"miniMapPlayerIconSprite",player.icon); TruckTaxiGPSAdapter.Set(compass,"miniMapPlayerIconColor",player.color); }
            var route=registry.Find(TruckTaxiMapMarkerType.ActiveRoute);
            if(route!=null) TruckTaxiGPSAdapter.Set(compass,"routeCompassBarSprite",route.icon);
        }
        private void OnRequest(TaxiRequestProgress request)
        {
            if(request.State==TaxiRequestState.Succeeded && request.StopPoint!=null) completed.Add(request.StopPoint.stableId);
            if(request.State==TaxiRequestState.Succeeded) foreach(var id in request.Targets) completed.Add(id);
            dirty=true; nextTrafficRefresh=0;
        }
        public void RequestRefresh() { dirty=true; }
        public void SetServiceTarget(string id,string label,Vector3 position)
        {
            ClearServiceTarget();
            if(markerRoot==null) return;
            serviceTargetReused=byId.TryGetValue(id,out var previous) && previous!=null;
            if(serviceTargetReused)
                serviceTargetOriginalState=previous.state;
            serviceTarget=serviceTargetReused ? previous : Ensure(id,label,TruckTaxiMapMarkerType.Bathroom,null,position);
            if(serviceTarget!=null) serviceTarget.state=TruckTaxiMapMarkerState.Active;
            dirty=true;
        }
        public void ClearServiceTarget()
        {
            if(serviceTarget!=null)
            {
                serviceTarget.state=serviceTargetReused ? serviceTargetOriginalState : TruckTaxiMapMarkerState.Hidden;
            }
            serviceTarget=null; serviceTargetReused=false; dirty=true;
        }
        private void LateUpdate()
        {
            if(host==null || api==null || !api.Available) return;
            if(dirty || Time.unscaledTime>=nextRefresh) Refresh();
            // Only cached target transforms move each frame; public vendor fields are changed on refresh.
            foreach(var marker in markers) if(marker!=null && marker.source!=null && marker.CompassPoi!=null)
                marker.CompassPoi.transform.position=marker.Position;
        }
        public void Refresh()
        {
            PruneDestroyedMarkers();
            if(host?.Session==null || registry==null || api==null || !api.Available) return;
            dirty=false; nextRefresh=Time.unscaledTime+.4f;
            var session=host.Session;
            if(presentationRide!=session.CurrentRideId)
            {
                presentationRide=session.CurrentRideId; completed.Clear();
            }
            foreach(var marker in markers) if(marker!=null && marker.source!=null && marker.source.GetComponent<TruckTaxiRideLocation>()!=null)
                marker.state=TruckTaxiMapMarkerState.Hidden;
            bool ride=session.State==TruckTaxiState.RideOffered || session.State==TruckTaxiState.DrivingToPickup ||
                session.State==TruckTaxiState.PassengerBoarding || session.HasPassenger || session.State==TruckTaxiState.RideComplete;
            foreach(var marker in stops) if(marker!=null) marker.state=completed.Contains(marker.stableId) ? TruckTaxiMapMarkerState.Completed : TruckTaxiMapMarkerState.Optional;
            foreach(var marker in shortcuts) if(marker!=null)
            {
                bool known=IsShortcutDiscovered(marker.stableId);
                marker.markerType=known ? TruckTaxiMapMarkerType.DiscoveredShortcut : TruckTaxiMapMarkerType.Shortcut;
                marker.state=known ? TruckTaxiMapMarkerState.Known : registry.showUndiscoveredShortcuts ? TruckTaxiMapMarkerState.Optional : TruckTaxiMapMarkerState.Hidden;
            }
            if(ride)
            {
                SetRideMarker(session.Pickup,TruckTaxiMapMarkerType.PassengerPickup,
                    gps.IsServiceDestination ? TruckTaxiMapMarkerState.Known : session.State==TruckTaxiState.RideOffered || session.State==TruckTaxiState.DrivingToPickup || session.State==TruckTaxiState.PassengerBoarding
                        ? TruckTaxiMapMarkerState.Active : TruckTaxiMapMarkerState.Completed);
                SetRideMarker(session.Destination,TruckTaxiMapMarkerType.Destination,
                    session.State==TruckTaxiState.RideComplete ? TruckTaxiMapMarkerState.Completed : session.HasPassenger && session.ActiveStop==null && !gps.IsServiceDestination
                        ? TruckTaxiMapMarkerState.Active : TruckTaxiMapMarkerState.Known);
            }
            foreach(var request in session.Requests)
            {
                if(request.State!=TaxiRequestState.Active) continue;
                if(request.StopPoint!=null && byId.TryGetValue(request.StopPoint.stableId,out var stop))
                    stop.state=request==session.ActiveStop && !gps.IsServiceDestination ? TruckTaxiMapMarkerState.Active : TruckTaxiMapMarkerState.Optional;
                if(request.Definition.requestType==TaxiRequestType.Shortcut)
                    foreach(var shortcut in shortcuts) if(shortcut!=null &&
                        (string.IsNullOrEmpty(request.Definition.targetId) || request.Definition.targetId==shortcut.stableId))
                        shortcut.state=request.Targets.Contains(shortcut.stableId) ? TruckTaxiMapMarkerState.Completed : TruckTaxiMapMarkerState.Optional;
            }
            RefreshImpactTargets();
            endpoint=FindAndStyleLegacyPois();
            nearby.Clear();
            foreach(var marker in markers) if(marker!=null && marker.state!=TruckTaxiMapMarkerState.Hidden && marker.state!=TruckTaxiMapMarkerState.Active &&
                marker.gameObject.activeInHierarchy && (marker.source==null || marker.source.gameObject.activeInHierarchy) &&
                (marker.markerType!=TruckTaxiMapMarkerType.Debug || registry.showDebugPoints))
                nearby.Add(marker);
            nearby.Sort(CompareNearby);
            foreach(var marker in markers)
            {
                if(marker==null) continue;
                bool important=marker.state==TruckTaxiMapMarkerState.Active || (ride && session.Destination!=null && marker.stableId==session.Destination.locationId);
                bool visible=marker.state!=TruckTaxiMapMarkerState.Hidden && marker.isActiveAndEnabled &&
                    (marker.source==null || marker.source.gameObject.activeInHierarchy) &&
                    (marker.markerType!=TruckTaxiMapMarkerType.Debug || registry.showDebugPoints) &&
                    (important || gps.FullMapOpen || (nearby.IndexOf(marker)<registry.maximumNearbyPoints &&
                        (marker.Position-host.Player.transform.position).sqrMagnitude<=registry.nearbyPointRange*registry.nearbyPointRange));
                bool borrowed=endpoint!=null && marker.stableId==gps.TargetId;
                var poi=marker.CompassPoi;
                if(poi==null && visible && !borrowed) poi=api.Create(markerRoot,marker.stableId);
                if(poi!=null) api.Apply(poi,marker,registry,visible && !borrowed);
                marker.BindPresentation(registry.Find(marker.markerType)?.icon,poi,visible);
            }
        }
        private int CompareNearby(TruckTaxiMapMarker a,TruckTaxiMapMarker b)
        {
            int result=(a.Position-host.Player.transform.position).sqrMagnitude.CompareTo((b.Position-host.Player.transform.position).sqrMagnitude);
            return result!=0 ? result : string.CompareOrdinal(a.stableId,b.stableId);
        }
        private void SetRideMarker(TruckTaxiRideLocation location,TruckTaxiMapMarkerType type,TruckTaxiMapMarkerState state)
        {
            if(location==null) return;
            var marker=Ensure(location.locationId,location.locationName,type,location.transform,location.StopPosition);
            if(marker!=null) marker.state=state;
        }
        private void RefreshImpactTargets()
        {
            for(int i=impacts.Count-1;i>=0;i--)
            {
                var expired=impacts[i]; if(expired!=null && expired.source!=null) continue;
                impacts.RemoveAt(i);
                if(expired==null) continue;
                byId.Remove(expired.stableId); markers.Remove(expired);
                if(expired.CompassPoi!=null) Destroy(expired.CompassPoi.gameObject);
                Destroy(expired.gameObject);
            }
            foreach(var marker in impacts) if(marker!=null)
                marker.state=completed.Contains(marker.stableId) ? TruckTaxiMapMarkerState.Completed : TruckTaxiMapMarkerState.Hidden;
            bool traffic=false,property=false;
            foreach(var request in host.Session.Requests) if(request.State==TaxiRequestState.Active)
            { traffic|=request.Definition.requestType==TaxiRequestType.RamTraffic; property|=request.Definition.requestType==TaxiRequestType.PropertyDamage; }
            if((traffic || property) && Time.unscaledTime>=nextTrafficRefresh)
            {
                nextTrafficRefresh=Time.unscaledTime+3;
                // Traffic is runtime-spawned. Inspect only its known owner, at a bounded rate while relevant.
                if(traffic && host.traffic!=null) AddImpacts(host.traffic.GetComponentsInChildren<TruckTaxiImpactTarget>());
                if(property) AddImpacts(propertyTargets);
            }
            foreach(var marker in impacts)
            {
                if(marker==null || marker.source==null) continue;
                var target=marker.source.GetComponent<TruckTaxiImpactTarget>(); if(target==null) continue;
                foreach(var request in host.Session.Requests)
                    if(request.State==TaxiRequestState.Active &&
                        ((target.kind==TaxiImpactKind.Traffic && request.Definition.requestType==TaxiRequestType.RamTraffic) ||
                         (target.kind==TaxiImpactKind.Property && request.Definition.requestType==TaxiRequestType.PropertyDamage)) &&
                        (string.IsNullOrEmpty(request.Definition.targetId) || request.Definition.targetId==target.targetId))
                        marker.state=request.Targets.Contains(target.targetId) || target.Damaged ? TruckTaxiMapMarkerState.Completed : TruckTaxiMapMarkerState.Optional;
            }
        }
        private void AddImpacts(IEnumerable<TruckTaxiImpactTarget> targets)
        {
            foreach(var target in targets)
            {
                if(target==null || target.kind==TaxiImpactKind.Pedestrian || string.IsNullOrEmpty(target.targetId)) continue;
                var marker=Ensure(target.targetId,target.kind==TaxiImpactKind.Traffic ? "TRAFFIC TARGET" : "PROPERTY TARGET",
                    target.kind==TaxiImpactKind.Traffic ? TruckTaxiMapMarkerType.TargetVehicle : TruckTaxiMapMarkerType.ActiveObjective,target.transform,target.transform.position);
                if(marker!=null && !impacts.Contains(marker)) impacts.Add(marker);
            }
        }
        private Component FindAndStyleLegacyPois()
        {
            if(legacyRoot==null) return null;
            Component active=null;
            foreach(var poi in legacyRoot.GetComponentsInChildren(api.PoiType,true))
            {
                string id=api.StableId(poi);
                if(id=="lws.destination.active" && byId.TryGetValue(gps.TargetId ?? "",out var target))
                { active=poi; api.Apply(poi,target,registry,true); }
                else if(id!=null && id.StartsWith("lws.road.",StringComparison.Ordinal))
                    api.ApplyDebug(poi,registry);
            }
            return active;
        }
        private void OnDestroy()
        {
            if(host?.Session!=null)
            { host.Session.Changed-=RequestRefresh; host.Session.DestinationChanged-=RequestRefresh;
                host.Session.RequestCreated-=OnRequest; host.Session.RequestResolved-=OnRequest; }
            if(markerRoot!=null) Destroy(markerRoot.gameObject);
        }

        // Existing project assembly boundary: Compass ships in Assembly-CSharp, so cache its public members.
        private sealed class CompassApi
        {
            public Type PoiType { get; }
            public bool Available => PoiType!=null;
            private readonly Dictionary<string,FieldInfo> fields=new Dictionary<string,FieldInfo>();
            private readonly PropertyInfo stableId;
            public CompassApi(Component compass)
            {
                PoiType=compass?.GetType().Assembly.GetType("CompassNavigatorPro.CompassProPOI");
                if(PoiType==null) return;
                foreach(var field in PoiType.GetFields(BindingFlags.Instance|BindingFlags.Public)) fields[field.Name]=field;
                stableId=PoiType.GetProperty("StableId");
            }
            public string StableId(Component poi) => stableId?.GetValue(poi) as string;
            public Component Create(Transform parent,string id)
            {
                var go=new GameObject("Compass marker: "+id); go.SetActive(false); go.transform.SetParent(parent,false);
                var poi=go.AddComponent(PoiType); stableId?.SetValue(poi,"taxi.map."+id);
                Field(poi,"visibility",2); Field(poi,"miniMapVisibility",2); Field(poi,"canBeVisited",false);
                Field(poi,"showOnScreenIndicator",false); Field(poi,"showOffScreenIndicator",false);
                Field(poi,"playAudioClipWhenVisited",false); go.SetActive(true); return poi;
            }
            public void Apply(Component poi,TruckTaxiMapMarker marker,TruckTaxiMapIconRegistry registry,bool visible)
            {
                var entry=registry.Find(marker.markerType); var active=marker.state==TruckTaxiMapMarkerState.Active;
                poi.transform.position=marker.Position;
                Field(poi,"title",(active ? "ACTIVE: " : marker.state==TruckTaxiMapMarkerState.Completed ? "DONE: " : "")+marker.label);
                Style(poi,entry,registry.Tint(marker.markerType,marker.state),registry.Scale(marker.markerType,marker.state),visible);
                Field(poi,"miniMapClampPosition",active); Field(poi,"miniMapShowCircle",active);
                Field(poi,"miniMapCircleRadius",8f); Field(poi,"miniMapCircleColor",new Color(1,1,1,.65f));
                Field(poi,"miniMapCircleInnerColor",Color.clear); Field(poi,"miniMapCircleAnimationWhenAppears",false);
            }
            public void ApplyDebug(Component poi,TruckTaxiMapIconRegistry registry) => Style(poi,registry.Find(TruckTaxiMapMarkerType.Debug),
                registry.Tint(TruckTaxiMapMarkerType.Debug,TruckTaxiMapMarkerState.Known),.7f,registry.showDebugPoints);
            private void Style(Component poi,TruckTaxiMapIconRegistry.Entry entry,Color tint,float scale,bool visible)
            {
                Field(poi,"iconNonVisited",entry?.icon); Field(poi,"iconVisited",entry?.icon);
                Field(poi,"tintColor",tint); Field(poi,"miniMapIconScale",scale); Field(poi,"iconScaleIsFixed",true);
                Field(poi,"miniMapVisibility",visible && entry?.icon!=null ? 1 : 2);
            }
            private void Field(Component target,string name,object value)
            { if(fields.TryGetValue(name,out var field)) field.SetValue(target,field.FieldType.IsEnum ? Enum.ToObject(field.FieldType,value) : value); }
        }
    }
}
