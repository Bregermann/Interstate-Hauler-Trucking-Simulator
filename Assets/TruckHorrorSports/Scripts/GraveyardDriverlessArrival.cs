using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckHorrorSports
{
    /// <summary>One authored visitor route, driven by the existing NWH adapter; never a traffic authority.</summary>
    public sealed class GraveyardDriverlessArrival : MonoBehaviour
    {
        public SportsEventTruck truck;
        public Transform[] road;
        public Light[] headlights;
        public int startPoint=48,arrivalPoint;
        public int Arrivals { get; private set; }
        public float DistanceDriven { get; private set; }
        public bool Driving { get; private set; }
        Vector3 start,last;
        Quaternion initialRotation;
        Rigidbody[] bodies;
        bool cached,armed,summoned;
        int waypoint;
        readonly RaycastHit[] hits=new RaycastHit[12];
        public void ResetArrival()
        {
            if(!cached){start=truck.transform.position;initialRotation=truck.transform.rotation;bodies=truck.GetComponentsInChildren<Rigidbody>(true);cached=true;}
            Arrivals=0;DistanceDriven=0;Driving=armed=summoned=false;waypoint=(startPoint+1)%road.Length;
            foreach(var body in bodies)body.isKinematic=false;
            truck.tractorMotor.Configure();truck.Prepare(start,initialRotation);truck.ApplyColor(new Color(.22f,.24f,.15f));
            foreach(var body in bodies)body.isKinematic=true;
            last=start;foreach(var light in headlights){light.enabled=false;light.color=new Color(.85f,.88f,.67f);}
        }
        public void Arm()=>armed=true;
        public void AnsweredSecondCall()
        {summoned=true;foreach(var light in headlights)light.color=new Color(1,.29f,.15f);}
        public bool InSecurityView(Camera camera)
        {return Driving && camera!=null && truck.paint!=null && GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),truck.paint.bounds) && !Physics.Linecast(camera.transform.position,truck.paint.bounds.center,1,QueryTriggerInteraction.Ignore);}
        public void Tick(float dt)
        {
            if(!armed || !truck.tractorMotor.vehicle.IsInitialized)return;
            if(!Driving){foreach(var body in bodies)body.isKinematic=false;Driving=true;foreach(var light in headlights)light.enabled=true;}
            float travel=Vector3.ProjectOnPlane(truck.body.position-last,Vector3.up).magnitude;last=truck.body.position;
            if(travel<20*dt+1)DistanceDriven+=travel;
            Vector3 target=road[waypoint].position;
            if(Vector3.Distance(truck.body.position,target)<8)
            {
                if(waypoint==arrivalPoint && DistanceDriven>40)Arrivals++;
                waypoint=(waypoint+1)%road.Length;target=road[waypoint].position;
            }
            Vector3 local=truck.transform.InverseTransformPoint(target);float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float desired=(summoned?10:8)*Mathf.Lerp(1,.45f,Mathf.Clamp01(Mathf.Abs(angle)/75));
            int count=Physics.SphereCastNonAlloc(truck.body.position+truck.transform.forward*4+Vector3.up*2,.8f,truck.transform.forward,hits,12,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(hits[i].rigidbody!=truck.body && hits[i].normal.y<.7f && !hits[i].collider.transform.IsChildOf(truck.transform))desired=0;
            float speed=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);
            truck.TickDrive(new Vector2(Mathf.Clamp(angle/32,-1,1),Mathf.Clamp((desired-speed)*.5f,-1,1)));
        }
        public void Freeze()
        {
            armed=Driving=false;
            if(truck!=null){if(truck.tractorMotor!=null&&truck.tractorMotor.vehicle!=null)truck.Power(false);truck.StopSound();}
            if(bodies!=null)foreach(var body in bodies)if(body!=null)body.isKinematic=true;
            if(headlights!=null)foreach(var light in headlights)if(light!=null)light.enabled=false;
        }
        void OnDisable(){if(cached)Freeze();}
    }
}
