using TMPro;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiPickupVisualState { Hidden, Approaching, Inside, TooFast, Boarding }
    public sealed class TruckTaxiPickupZoneVisualizer : MonoBehaviour
    {
        public TruckTaxiPickupVisualState State { get; private set; }
        public bool Visible => ring!=null && ring.gameObject.activeSelf;
        public float Radius => target!=null ? target.detectionRadius : 0;
        public float Distance { get; private set; }
        public bool Inside { get; private set; }
        public bool show = true, showRadius, showCollider, showPassengerSpawn, showPreferredStop;
        public TruckTaxiPickupVisualState? forcedState;
        public string Feedback => State==TruckTaxiPickupVisualState.TooFast ? "SLOW DOWN / STOP TO PICK UP" :
            State==TruckTaxiPickupVisualState.Boarding ? "PASSENGER BOARDING" : State==TruckTaxiPickupVisualState.Inside ? "IN PICKUP ZONE" : "PICK UP PASSENGER";
        private TruckTaxiBootstrap host;
        private TruckTaxiRideLocation target;
        private LineRenderer ring, beacon;
        private LineRenderer destinationRing, destinationBeacon;
        private TextMeshPro destinationMarker;
        private TruckTaxiStopObjectivePoint privateTarget;
        private LineRenderer privateRing, privateBeacon;
        private TextMeshPro privateMarker;
        private TruckTaxiRegionalWorld regional;
        private LwsTruckControlController controls;
        private Rigidbody playerBody;
        private TruckTaxiRideLocation destinationTarget;
        private MeshRenderer area;
        private TextMeshPro marker;
        private Material material;
        private Mesh disc;
        private MaterialPropertyBlock tint;
        private const int Segments=80;
        public void Initialize(TruckTaxiBootstrap value)
        {
            host=value;
            regional=GetComponent<TruckTaxiRegionalWorld>();
            controls=host.Player.GetComponentInChildren<LwsTruckControlController>(true);
            playerBody=host.Player.GetComponent<Rigidbody>();
            tint=new MaterialPropertyBlock();
            material=new Material(Shader.Find("Sprites/Default"));
            ring=Line("Pickup radius",.2f); ring.loop=true; ring.positionCount=Segments;
            beacon=Line("Pickup beacon",.3f); beacon.positionCount=2;
            destinationRing=Line("Dropoff radius",.28f); destinationRing.loop=true; destinationRing.positionCount=Segments;
            destinationBeacon=Line("Dropoff beacon",.35f); destinationBeacon.positionCount=2;
            privateRing=Line("Private stop parking radius",.4f); privateRing.loop=true; privateRing.positionCount=Segments;
            privateBeacon=Line("Private stop beacon",.32f); privateBeacon.positionCount=2;
            privateMarker=new GameObject("Private stop label",typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            privateMarker.transform.SetParent(transform,false);
            if(host.hud.font!=null) privateMarker.font=host.hud.font;
            privateMarker.fontSize=8; privateMarker.alignment=TextAlignmentOptions.Center;
            privateMarker.rectTransform.sizeDelta=new Vector2(28,6);
            destinationMarker=new GameObject("Dropoff marker",typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            destinationMarker.transform.SetParent(transform,false);
            if(host.hud.font!=null) destinationMarker.font=host.hud.font;
            destinationMarker.fontSize=8; destinationMarker.alignment=TextAlignmentOptions.Center;
            destinationMarker.rectTransform.sizeDelta=new Vector2(26,5);
            var surface=new GameObject("Pickup inner area",typeof(MeshFilter),typeof(MeshRenderer)); surface.transform.SetParent(transform,false);
            area=surface.GetComponent<MeshRenderer>(); area.sharedMaterial=material; area.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            disc=new Mesh { name="Taxi pickup area" }; surface.GetComponent<MeshFilter>().sharedMesh=disc;
            marker=new GameObject("Pickup marker",typeof(TextMeshPro)).GetComponent<TextMeshPro>(); marker.transform.SetParent(transform,false);
            if(host.hud.font!=null) marker.font=host.hud.font;
            marker.fontSize=8; marker.alignment=TextAlignmentOptions.Center; marker.rectTransform.sizeDelta=new Vector2(24,5);
            SetVisible(false);
            SetDestinationVisible(false);
            SetPrivateVisible(false);
            host.Session.Changed+=OnSessionChanged;
        }
        private void OnSessionChanged()
        {
            if(host.Session.State==TruckTaxiState.DrivingToPickup || host.Session.State==TruckTaxiState.PassengerBoarding) return;
            State=TruckTaxiPickupVisualState.Hidden; target=null; SetVisible(false);
        }
        private LineRenderer Line(string name,float width)
        {
            var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>(); line.transform.SetParent(transform,false);
            line.sharedMaterial=material; line.useWorldSpace=true; line.widthMultiplier=width;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows=false; return line;
        }
        public static TruckTaxiPickupVisualState Evaluate(TruckTaxiSession session,Vector3 position,float speed)
        {
            if(session?.Pickup==null || (session.State!=TruckTaxiState.DrivingToPickup && session.State!=TruckTaxiState.PassengerBoarding)) return TruckTaxiPickupVisualState.Hidden;
            if(!session.Pickup.Contains(position)) return TruckTaxiPickupVisualState.Approaching;
            if(speed>session.BoardingMaximumSpeed) return TruckTaxiPickupVisualState.TooFast;
            return session.State==TruckTaxiState.PassengerBoarding ? TruckTaxiPickupVisualState.Boarding : TruckTaxiPickupVisualState.Inside;
        }
        public static bool HasDropoffTarget(TruckTaxiSession session) => session!=null && session.Destination!=null &&
            (session.State==TruckTaxiState.DrivingToDestination || session.State==TruckTaxiState.PassengerExiting);
        private void Update()
        {
            if(host?.Player==null) return;
            var session=host.Session;
            UpdateDestination(session);
            UpdatePrivateStop();
            State=Evaluate(session,host.Player.transform.position,host.Player.GetComponent<Rigidbody>().linearVelocity.magnitude);
            if(State==TruckTaxiPickupVisualState.Hidden || !show) { SetVisible(false); target=null; return; }
            if(forcedState.HasValue) State=forcedState.Value;
            if(target!=session.Pickup) { target=session.Pickup; BuildGeometry(); }
            Distance=Vector3.ProjectOnPlane(host.Player.transform.position-target.StopPosition,Vector3.up).magnitude;
            Inside=target.Contains(host.Player.transform.position);
            SetVisible(true);
            ring.enabled=area.enabled=Distance<180;
            beacon.enabled=Distance>40;
            Color color=State==TruckTaxiPickupVisualState.TooFast ? new Color(1,.3f,.12f) :
                State==TruckTaxiPickupVisualState.Approaching ? new Color(1,.8f,.05f) : new Color(.1f,1,.55f);
            color.a=.8f+.2f*Mathf.Sin(Time.time*(State==TruckTaxiPickupVisualState.Boarding ? 12 : 3));
            ring.startColor=ring.endColor=beacon.startColor=beacon.endColor=color;
            tint.SetColor("_Color",new Color(color.r,color.g,color.b,.08f)); area.SetPropertyBlock(tint);
            marker.color=color; marker.text=Feedback+"\n"+session.Passenger.passengerName+"  "+Distance.ToString("0")+" m";
            marker.transform.position=target.StopPosition+Vector3.up*(Distance>80 ? 14 : 5);
            if(Camera.main!=null) marker.transform.rotation=Quaternion.LookRotation(marker.transform.position-Camera.main.transform.position);
            float scale=Mathf.Clamp(Distance/50,.5f,3); marker.transform.localScale=Vector3.one*scale;
            if(showRadius || showCollider) for(int i=1;i<Segments;i++) Debug.DrawLine(ring.GetPosition(i-1),ring.GetPosition(i),Color.yellow);
            if(showPassengerSpawn && target.passengerSpawnPoint!=null) Debug.DrawRay(target.passengerSpawnPoint.position,Vector3.up*6,Color.cyan);
            if(showPreferredStop) Debug.DrawRay(target.StopPosition,Vector3.up*6,Color.green);
        }
        public void SetPrivateStop(TruckTaxiStopObjectivePoint stop)
        {
            privateTarget=stop;
            if(stop==null) { SetPrivateVisible(false); return; }
            for(int i=0;i<Segments;i++)
            {
                float angle=i*Mathf.PI*2/Segments;
                privateRing.SetPosition(i,Ground(stop.Position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*stop.radius));
            }
            Vector3 center=Ground(stop.Position);
            privateBeacon.SetPosition(0,center);
            privateBeacon.SetPosition(1,center+Vector3.up*34);
        }
        private void UpdatePrivateStop()
        {
            if(privateTarget==null || !show || !privateTarget.gameObject.scene.isLoaded ||
                regional!=null && !regional.IsPositionAvailable(privateTarget.Position))
            { SetPrivateVisible(false); return; }
            var position=host.Player.transform.position;
            float distance=Vector3.ProjectOnPlane(position-privateTarget.Position,Vector3.up).magnitude;
            bool inside=distance<=privateTarget.radius && Mathf.Abs(position.y-privateTarget.Position.y)<5f;
            float speed=playerBody!=null ? playerBody.linearVelocity.magnitude : 0f;
            bool slow=speed<=privateTarget.maximumSpeed;
            bool brake=controls!=null && controls.CurrentState.parkingBrakeOn;
            bool ready=inside && slow && brake;
            Color color=ready ? new Color(.3f,1f,.6f) : inside && !slow ? new Color(1f,.44f,.12f) :
                inside ? new Color(1f,.8f,.3f) : new Color(1f,.28f,.62f);
            color.a=.85f+.15f*Mathf.Sin(Time.time*4f);
            SetPrivateVisible(true);
            privateRing.enabled=distance<260f;
            privateBeacon.enabled=distance>35f;
            privateRing.startColor=privateRing.endColor=privateBeacon.startColor=privateBeacon.endColor=color;
            privateMarker.color=color;
            privateMarker.text=(ready ? "PARKED AT PRIVATE STOP" : inside ? !slow ? "SLOW DOWN" :
                "PARK & SET BRAKE ("+(host.Companions?.ParkingBrakeBinding ?? "P")+")" : "PRIVATE STOP")+
                "\n"+distance.ToString("0")+" m";
            privateMarker.transform.position=privateTarget.Position+Vector3.up*(distance>80f ? 15f : 6f);
            if(Camera.main!=null) privateMarker.transform.rotation=Quaternion.LookRotation(
                privateMarker.transform.position-Camera.main.transform.position);
            privateMarker.transform.localScale=Vector3.one*Mathf.Clamp(distance/50f,.5f,3f);
        }
        private void SetPrivateVisible(bool visible)
        {
            if(privateRing==null) return;
            privateRing.gameObject.SetActive(visible);
            privateBeacon.gameObject.SetActive(visible);
            privateMarker.gameObject.SetActive(visible);
        }
        private void UpdateDestination(TruckTaxiSession session)
        {
            if(!show || !HasDropoffTarget(session))
            { destinationTarget=null; SetDestinationVisible(false); return; }
            if(destinationTarget!=session.CurrentDesiredDestination)
            { destinationTarget=session.CurrentDesiredDestination; BuildDestinationGeometry(); }
            float distance=Vector3.ProjectOnPlane(host.Player.transform.position-destinationTarget.StopPosition,Vector3.up).magnitude;
            bool inside=destinationTarget.Contains(host.Player.transform.position);
            var color=inside ? new Color(.15f,1f,.75f) : new Color(.05f,.75f,1f);
            color.a=.8f+.2f*Mathf.Sin(Time.time*3);
            SetDestinationVisible(true);
            destinationRing.enabled=distance<240;
            destinationBeacon.enabled=distance>40;
            destinationRing.startColor=destinationRing.endColor=destinationBeacon.startColor=destinationBeacon.endColor=color;
            destinationMarker.color=color;
            destinationMarker.text=(inside ? "DROPOFF ZONE" : "PASSENGER DROPOFF")+"\n"+
                destinationTarget.locationName+"  "+distance.ToString("0")+" m";
            destinationMarker.transform.position=destinationTarget.StopPosition+Vector3.up*(distance>80 ? 15 : 6);
            if(Camera.main!=null) destinationMarker.transform.rotation=Quaternion.LookRotation(
                destinationMarker.transform.position-Camera.main.transform.position);
            destinationMarker.transform.localScale=Vector3.one*Mathf.Clamp(distance/50,.5f,3);
        }
        private void BuildDestinationGeometry()
        {
            var center=Ground(destinationTarget.StopPosition);
            for(int i=0;i<Segments;i++)
            {
                float angle=i*Mathf.PI*2/Segments;
                destinationRing.SetPosition(i,Ground(destinationTarget.StopPosition+
                    new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*destinationTarget.detectionRadius));
            }
            destinationBeacon.SetPosition(0,center);
            destinationBeacon.SetPosition(1,center+Vector3.up*32);
        }
        private void SetDestinationVisible(bool value)
        {
            if(destinationRing==null) return;
            destinationRing.gameObject.SetActive(value);
            destinationBeacon.gameObject.SetActive(value);
            destinationMarker.gameObject.SetActive(value);
        }
        private void BuildGeometry()
        {
            var vertices=new Vector3[Segments+1]; var triangles=new int[Segments*3]; var colors=new Color[Segments+1];
            vertices[0]=Ground(target.StopPosition); colors[0]=Color.white;
            for(int i=0;i<Segments;i++)
            {
                float angle=i*Mathf.PI*2/Segments;
                Vector3 point=Ground(target.StopPosition+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*target.detectionRadius);
                ring.SetPosition(i,point); vertices[i+1]=point; colors[i+1]=Color.white;
                triangles[i*3]=0; triangles[i*3+1]=(i+1)%Segments+1; triangles[i*3+2]=i+1;
            }
            disc.Clear(); disc.vertices=vertices; disc.triangles=triangles; disc.colors=colors; disc.RecalculateBounds(); disc.RecalculateNormals();
            // Mesh points are world coordinates; this presentation root stays at scene origin.
            beacon.SetPosition(0,vertices[0]); beacon.SetPosition(1,vertices[0]+Vector3.up*28);
        }
        private Vector3 Ground(Vector3 point)
        {
            var hits=Physics.RaycastAll(point+Vector3.up*30,Vector3.down,80,~0,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
                if(hit.normal.y>.55f && hit.collider.attachedRigidbody==null) return hit.point+Vector3.up*.08f;
            return point+Vector3.up*.1f;
        }
        private void SetVisible(bool value)
        {
            if(ring==null) return;
            ring.gameObject.SetActive(value); area.gameObject.SetActive(value); beacon.gameObject.SetActive(value); marker.gameObject.SetActive(value);
        }
        private void OnDestroy() { if(host!=null && host.Session!=null) host.Session.Changed-=OnSessionChanged; if(material!=null) Destroy(material); if(disc!=null) Destroy(disc); }
    }
}
