using TMPro;
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
        private MeshRenderer area;
        private TextMeshPro marker;
        private Material material;
        private Mesh disc;
        private MaterialPropertyBlock tint;
        private const int Segments=80;
        public void Initialize(TruckTaxiBootstrap value)
        {
            host=value;
            tint=new MaterialPropertyBlock();
            material=new Material(Shader.Find("Sprites/Default"));
            ring=Line("Pickup radius",.2f); ring.loop=true; ring.positionCount=Segments;
            beacon=Line("Pickup beacon",.3f); beacon.positionCount=2;
            var surface=new GameObject("Pickup inner area",typeof(MeshFilter),typeof(MeshRenderer)); surface.transform.SetParent(transform,false);
            area=surface.GetComponent<MeshRenderer>(); area.sharedMaterial=material; area.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            disc=new Mesh { name="Taxi pickup area" }; surface.GetComponent<MeshFilter>().sharedMesh=disc;
            marker=new GameObject("Pickup marker",typeof(TextMeshPro)).GetComponent<TextMeshPro>(); marker.transform.SetParent(transform,false);
            if(host.hud.font!=null) marker.font=host.hud.font;
            marker.fontSize=8; marker.alignment=TextAlignmentOptions.Center; marker.rectTransform.sizeDelta=new Vector2(24,5);
            SetVisible(false);
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
        private void Update()
        {
            if(host?.Player==null) return;
            var session=host.Session;
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
