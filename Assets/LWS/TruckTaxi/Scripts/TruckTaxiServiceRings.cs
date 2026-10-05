using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Extends the existing LineRenderer stop feedback. Does not author services, triggers, or world content.
    public sealed class TruckTaxiServiceRings : MonoBehaviour
    {
        public sealed class Ring
        {
            public TruckTaxiServicePoint Service;
            public TruckTaxiStopObjectivePoint Stop;
            public LineRenderer Line;
            public TextMeshPro Label;
            public Vector3 Center;
            public float Radius;
            public TruckTaxiStopRingState State;
        }
        private readonly Dictionary<string,Ring> rings=new Dictionary<string,Ring>();
        private readonly List<TruckTaxiStopObjectivePoint> stops=new List<TruckTaxiStopObjectivePoint>();
        private readonly List<TruckTaxiRideLocation> places=new List<TruckTaxiRideLocation>();
        private readonly HashSet<string> seen=new HashSet<string>();
        private readonly List<string> remove=new List<string>();
        private readonly HashSet<string> interacting=new HashSet<string>();
        private readonly HashSet<string> suppressed=new HashSet<string>();
        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld regional;
        private Material material;
        private Rigidbody body;
        private Transform root;
        private float nextRefresh;
        private readonly TruckTaxiStopRingPalette fallback=new TruckTaxiStopRingPalette();
        [Min(50)] public float visibleDistance=350;
        [Min(5)] public float nearbyDistance=70;
        public IReadOnlyDictionary<string,Ring> Rings => rings;
        public void Initialize(TruckTaxiBootstrap value)
        {
            if(host!=null || value==null) return;
            host=value; body=host.Player.GetComponent<Rigidbody>();
            regional=host.GetComponent<TruckTaxiRegionalWorld>();
            root=new GameObject("Nearby service bay rings").transform; root.SetParent(transform,false);
            material=new Material(Shader.Find("Sprites/Default"));
            if(regional!=null) regional.AvailabilityChanged+=RefreshSources;
            RefreshSources();
        }
        public void RefreshSources()
        {
            stops.Clear();
            foreach(var stop in FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None))
                if(stop!=null && stop.gameObject.scene.isLoaded) stops.Add(stop);
            places.Clear();
            foreach(var place in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
                if(place!=null && place.gameObject.scene.isLoaded) places.Add(place);
            nextRefresh=0;
        }
        public void SetInteracting(string stableId,bool value)
        { if(value) interacting.Add(stableId); else interacting.Remove(stableId); nextRefresh=0; }
        public void SetSuppressed(string stableId,bool value)
        { if(value) suppressed.Add(stableId); else suppressed.Remove(stableId); nextRefresh=0; }
        public static TruckTaxiStopRingState Evaluate(TruckTaxiServicePoint service,Vector3 player,float speed,
            bool selected,bool interacting,float nearbyDistance=70)
        {
            if(!TruckTaxiServicePresentation.IsUsable(service)) return TruckTaxiStopRingState.Unavailable;
            bool inside=Mathf.Abs(player.y-service.Position.y)<=service.verticalTolerance &&
                Vector3.ProjectOnPlane(player-service.Position,Vector3.up).sqrMagnitude<=service.InteractionRadius*service.InteractionRadius;
            if(inside && !service.CanUse(player,speed,TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond))
                return TruckTaxiStopRingState.InsideTooFast;
            if(inside) return interacting ? TruckTaxiStopRingState.Interacting : TruckTaxiStopRingState.Valid;
            if(selected) return TruckTaxiStopRingState.Selected;
            return (player-service.Position).sqrMagnitude<=nearbyDistance*nearbyDistance ? TruckTaxiStopRingState.Nearby : TruckTaxiStopRingState.Ambient;
        }
        private void LateUpdate()
        {
            if(host?.Player==null || Time.unscaledTime<nextRefresh) return;
            nextRefresh=Time.unscaledTime+.2f;
            Refresh();
        }
        public void Refresh()
        {
            if(host?.Player==null || root==null) return;
            seen.Clear();
            Vector3 player=host.Player.transform.position;
            float speed=body!=null ? body.linearVelocity.magnitude : 0;
            foreach(var service in TruckTaxiServicePoint.Points)
            {
                if(!TruckTaxiServicePresentation.IsUsable(service) || !AllowedScene(service.gameObject.scene) || !Relevant(service.Position,player)) continue;
                var state=Evaluate(service,player,speed,host.GPS.TargetId==service.stableId,interacting.Contains(service.stableId),nearbyDistance);
                Draw(service.stableId,service.displayName+"\n"+TruckTaxiServicePresentation.Label(service.capabilities),
                    service.Position,service.InteractionRadius,TruckTaxiServicePresentation.MarkerType(service.capabilities),state,service,null);
            }
            foreach(var stop in stops)
            {
                if(stop==null || !stop.isActiveAndEnabled || !AllowedScene(stop.gameObject.scene) || !Relevant(stop.Position,player) ||
                    string.IsNullOrWhiteSpace(stop.stableId) || seen.Contains(stop.stableId) ||
                    suppressed.Contains(stop.stableId) || SharesServiceBay(stop.Position)) continue;
                bool selected=host.GPS.TargetId==stop.stableId;
                bool inside=stop.IsValidStop(player,0);
                var state=inside ? stop.IsValidStop(player,speed) ? TruckTaxiStopRingState.Valid : TruckTaxiStopRingState.InsideTooFast :
                    selected ? TruckTaxiStopRingState.Selected : (stop.Position-player).sqrMagnitude<=nearbyDistance*nearbyDistance ?
                    TruckTaxiStopRingState.Nearby : TruckTaxiStopRingState.Ambient;
                var type=TruckTaxiMapIconRegistry.ForStop(stop.category);
                // A passenger diner objective alone does not promise a usable food service.
                if(type==TruckTaxiMapMarkerType.FoodStop) type=TruckTaxiMapMarkerType.Place;
                Draw(stop.stableId,stop.displayName,stop.Position,stop.radius,type,state,null,stop);
            }
            foreach(var place in places)
            {
                if(place==null || !place.isActiveAndEnabled || !AllowedScene(place.gameObject.scene) ||
                    (!place.pickupAllowed && !place.dropoffAllowed) || !Relevant(place.StopPosition,player) ||
                    string.IsNullOrWhiteSpace(place.locationId) || seen.Contains(place.locationId) ||
                    SharesServiceBay(place.StopPosition) || RideVisualOwns(place)) continue;
                bool overlaps=false;
                foreach(var stop in stops)
                    if(stop!=null && stop.isActiveAndEnabled && (stop.Position-place.StopPosition).sqrMagnitude<.25f) { overlaps=true; break; }
                if(overlaps) continue;
                bool inside=place.Contains(player);
                var state=inside ? speed<=TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond ? TruckTaxiStopRingState.Valid : TruckTaxiStopRingState.InsideTooFast :
                    host.GPS.TargetId==place.locationId ? TruckTaxiStopRingState.Selected :
                    (place.StopPosition-player).sqrMagnitude<=nearbyDistance*nearbyDistance ? TruckTaxiStopRingState.Nearby : TruckTaxiStopRingState.Ambient;
                Draw(place.locationId,place.locationName,place.StopPosition,place.detectionRadius,TruckTaxiMapMarkerType.Place,state,null,null);
            }
            remove.Clear();
            foreach(var pair in rings)
            {
                if(seen.Contains(pair.Key)) continue;
                var ring=pair.Value;
                if(ring.Service!=null && ring.Service.gameObject.scene.isLoaded && Relevant(ring.Center,player))
                {
                    ring.State=TruckTaxiStopRingState.Unavailable;
                    ring.Line.startColor=ring.Line.endColor=ring.Label.color=fallback.Tint(TruckTaxiMapMarkerType.Place,ring.State);
                    ring.Label.text="UNAVAILABLE"; continue;
                }
                remove.Add(pair.Key);
            }
            foreach(var id in remove) { Destroy(rings[id].Line.gameObject); rings.Remove(id); interacting.Remove(id); }
        }
        private bool Relevant(Vector3 position,Vector3 player) => (position-player).sqrMagnitude<=visibleDistance*visibleDistance;
        private bool AllowedScene(UnityEngine.SceneManagement.Scene scene)
        {
            if(!scene.IsValid() || !scene.isLoaded) return false;
            if(scene==host.gameObject.scene) return true;
            if(regional!=null) foreach(var region in regional.regions)
                if(region!=null && region.sceneName==scene.name) return true;
            return false;
        }
        private bool RideVisualOwns(TruckTaxiRideLocation place)
        {
            var session=host.Session;
            return session!=null && ((place==session.Pickup && (session.State==TruckTaxiState.DrivingToPickup ||
                session.State==TruckTaxiState.PassengerBoarding)) ||
                (TruckTaxiPickupZoneVisualizer.HasDropoffTarget(session) && place==session.CurrentDesiredDestination));
        }
        private static bool SharesServiceBay(Vector3 position)
        {
            foreach(var service in TruckTaxiServicePoint.Points)
                if(TruckTaxiServicePresentation.IsUsable(service) && (service.Position-position).sqrMagnitude<.25f) return true;
            return false;
        }
        private void Draw(string id,string text,Vector3 center,float radius,TruckTaxiMapMarkerType type,
            TruckTaxiStopRingState state,TruckTaxiServicePoint service,TruckTaxiStopObjectivePoint stop)
        {
            if(!seen.Add(id)) return;
            if(!rings.TryGetValue(id,out var ring))
            {
                var go=new GameObject("Stop bay: "+id,typeof(LineRenderer)); go.transform.SetParent(root,false);
                ring=new Ring { Line=go.GetComponent<LineRenderer>(), Radius=-1 };
                ring.Line.sharedMaterial=material; ring.Line.loop=true; ring.Line.useWorldSpace=true; ring.Line.positionCount=64;
                ring.Line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; ring.Line.receiveShadows=false;
                ring.Label=new GameObject("Bay capabilities",typeof(TextMeshPro)).GetComponent<TextMeshPro>();
                ring.Label.transform.SetParent(go.transform,false); ring.Label.fontSize=5;
                ring.Label.alignment=TextAlignmentOptions.Center; ring.Label.rectTransform.sizeDelta=new Vector2(44,8);
                if(host.hud.font!=null) ring.Label.font=host.hud.font;
                rings.Add(id,ring);
            }
            ring.Service=service; ring.Stop=stop; ring.State=state;
            if(ring.Center!=center || !Mathf.Approximately(ring.Radius,radius))
            {
                ring.Center=center; ring.Radius=radius;
                for(int i=0;i<64;i++)
                {
                    float angle=i*Mathf.PI*2/64;
                    ring.Line.SetPosition(i,center+new Vector3(Mathf.Cos(angle)*radius,.12f,Mathf.Sin(angle)*radius));
                }
            }
            var palette=host.GPS.MapMarkers?.Registry?.stopRingPalette ?? fallback;
            Color color=palette.Tint(type,state);
            if(host.GPS.TargetId==id) color=Color.Lerp(color,Color.white,.3f);
            ring.Line.startColor=ring.Line.endColor=ring.Label.color=color;
            ring.Line.widthMultiplier=state==TruckTaxiStopRingState.Selected || state==TruckTaxiStopRingState.Valid ||
                state==TruckTaxiStopRingState.Interacting ? .38f : .18f;
            ring.Label.enabled=state!=TruckTaxiStopRingState.Ambient;
            ring.Label.text=text+(state==TruckTaxiStopRingState.InsideTooFast ? "\nSLOW DOWN" : state==TruckTaxiStopRingState.Interacting ? "\nIN SERVICE" : "");
            ring.Label.transform.position=center+Vector3.up*4;
            if(Camera.main!=null) ring.Label.transform.rotation=Quaternion.LookRotation(ring.Label.transform.position-Camera.main.transform.position);
        }
        private void OnDisable() { if(root!=null) root.gameObject.SetActive(false); }
        private void OnEnable() { if(root!=null) root.gameObject.SetActive(true); nextRefresh=0; }
        private void OnDestroy()
        {
            if(regional!=null) regional.AvailabilityChanged-=RefreshSources;
            if(root!=null) Destroy(root.gameObject);
            if(material!=null) Destroy(material);
        }
    }
}
