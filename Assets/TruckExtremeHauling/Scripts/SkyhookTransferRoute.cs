using UnityEngine;
using LWS.TruckHitchSports;
namespace LWS.TruckExtremeHauling
{
    /// <summary>Optional two-station precision route; no transported-body pose writes in Tick.</summary>
    public sealed class SkyhookTransferRoute : MonoBehaviour
    {
        public PhysicalCableCar first,second;
        public SuspendedFreightHook firstHook,secondHook;
        public TrailerBody cargo;
        public Transform loadingPose,firstTrolleyPose,secondTrolleyPose;
        public Collider loadingPad,transferPad,receivingPad;
        public Transform receivingTarget;
        public Vector3 eyeLocal;
        public Transform liftingEyeVisual;
        public LineRenderer[] slings;
        public bool Delivered {get;private set;}
        public bool Failed {get;private set;}
        public int Transfers {get;private set;}
        public int Recoveries {get;private set;}
        public float MaximumImpact {get;private set;}
        public float SwingSeconds {get;private set;}
        public float LandingError {get;private set;}
        public string Instruction {get;private set;}
        bool secondStage,firstWasCaptured,secondWasCaptured,releasedAtTransfer;
        float quiet;
        Vector3 originalEnd,originalRailAnchor;
        ConfigurableJoint firstRail;
        bool cached;
        Collider[] cargoShapes;
        readonly RaycastHit[] supportHits=new RaycastHit[16];
        public void SetMode(bool active)
        {
            if(!cached){originalEnd=first.routeEnd;firstRail=first.trolley.GetComponent<ConfigurableJoint>();if(firstRail!=null)originalRailAnchor=firstRail.connectedAnchor;cached=true;}
            if(!active){Freeze();first.routeEnd=originalEnd;if(firstRail!=null)firstRail.connectedAnchor=originalRailAnchor;}
            gameObject.SetActive(active);
        }
        public void BeginRoute()
        {
            cargoShapes=cargo.GetComponentsInChildren<Collider>(true);
            Delivered=Failed=secondStage=firstWasCaptured=secondWasCaptured=releasedAtTransfer=false;Transfers=Recoveries=0;quiet=MaximumImpact=SwingSeconds=LandingError=0;
            firstHook.Release();secondHook.Release();first.Release();second.Release();
            if(firstRail!=null)firstRail.connectedAnchor=firstTrolleyPose.position;
            first.trolley.position=firstTrolleyPose.position;first.trolley.transform.position=firstTrolleyPose.position;first.trolley.linearVelocity=first.trolley.angularVelocity=Vector3.zero;first.trolley.isKinematic=false;
            second.trolley.position=secondTrolleyPose.position;second.trolley.transform.position=secondTrolleyPose.position;second.trolley.linearVelocity=second.trolley.angularVelocity=Vector3.zero;second.trolley.isKinematic=false;
            first.routeEnd=new Vector3(firstTrolleyPose.position.x,firstTrolleyPose.position.y,transferPad.bounds.center.z);
            second.routeEnd=new Vector3(secondTrolleyPose.position.x,secondTrolleyPose.position.y,receivingTarget.position.z);
            // Normal round setup only: spawn the original assembly supported on its authored
            // loading station. Capture, transfer, recovery and delivery never move its pose.
            Quaternion rotation=loadingPose.rotation*Quaternion.Inverse(cargo.Body.rotation);Vector3 before=cargo.Body.position;
            foreach(var part in cargo.GetComponentsInChildren<Rigidbody>(true))
            {Vector3 relative=part.position-before;part.isKinematic=false;part.position=loadingPose.position+rotation*relative;part.rotation=rotation*part.rotation;part.linearVelocity=part.angularVelocity=Vector3.zero;}
            cargo.transform.SetPositionAndRotation(loadingPose.position,loadingPose.rotation);
            if(cargo.Module.module.trailerStand!=null)cargo.Module.module.trailerStand.SetActive(true);
            cargo.SetBrakes(1);firstHook.ResetHook();secondHook.ResetHook();Instruction="LOWER THE FIRST HOOK · TAP ACTION AT THE LIFTING EYE";
        }
        Vector3 Eye => cargo.Body.transform.TransformPoint(eyeLocal);
        float Lowest()
        {float y=float.PositiveInfinity;foreach(var shape in cargoShapes)if(shape.enabled&&!shape.isTrigger)y=Mathf.Min(y,shape.bounds.min.y);return y;}
        bool Supported(Collider platform)
        {
            Vector3 center=cargo.Body.worldCenterOfMass;var bounds=platform.bounds;
            if(Mathf.Abs(center.x-bounds.center.x)>bounds.extents.x-2||Mathf.Abs(center.z-bounds.center.z)>bounds.extents.z-3||cargo.Body.linearVelocity.magnitude>.8f||Vector3.Dot(cargo.transform.up,Vector3.up)<.7f)return false;
            if(!cargo.WheelContact||Lowest()>=bounds.max.y+.5f)return false;
            int count=Physics.RaycastNonAlloc(center+Vector3.up*2,Vector3.down,supportHits,8,1,QueryTriggerInteraction.Ignore);
            if(count==supportHits.Length)return false;
            Collider nearest=null;float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var shape=supportHits[i].collider;
                if(shape==null||shape.attachedRigidbody==cargo.Body||shape.transform.IsChildOf(cargo.transform))continue;
                if(supportHits[i].distance<distance){distance=supportHits[i].distance;nearest=shape;}
            }
            return nearest==platform;
        }
        public void Tick(Vector2 drive,bool held,bool pressed,bool ai,int difficulty,float dt)
        {
            if(Delivered||Failed)return;
            if(cargo.Body.position.y<-20||cargo.CargoCondition<=0){Failed=true;Instruction="LOAD LOST";return;}
            if(!secondStage&&!firstHook.Holding&&firstWasCaptured&&Supported(transferPad))
            {releasedAtTransfer=true;secondStage=true;Instruction="TRANSFER SUPPORTED · LOWER THE SECOND HOOK";}
            var hook=secondStage?secondHook:firstHook;var car=secondStage?second:first;
            var destination=secondStage?receivingPad:transferPad;
            if(secondStage)firstHook.Apply(0,1,dt); // Empty hook retracts physically for transfer clearance.
            if(ai)AIControls(hook,car,destination,difficulty,out drive,out held,out pressed);
            float hoist=held?drive.y:0;
            car.DriveAlongRail(held?0:drive.y,held||Mathf.Abs(drive.y)<.05f?1:0);
            hook.Apply(drive.x,hoist,dt);
            if(pressed&&Mathf.Abs(drive.y)<.15f)
            {
                if(hook.Holding)
                {
                    hook.Release();cargo.SetBrakes(1);
                    if(cargo.Module.module.trailerStand!=null)cargo.Module.module.trailerStand.SetActive(true);
                }
                else if(hook.TryCapture(cargo.Body,eyeLocal))
                {
                    if(secondStage){if(releasedAtTransfer&&!secondWasCaptured)Transfers++;else Recoveries++;secondWasCaptured=true;}
                    else {if(firstWasCaptured)Recoveries++;firstWasCaptured=true;}
                    cargo.SetBrakes(0);if(cargo.Module.module.trailerStand!=null)cargo.Module.module.trailerStand.SetActive(false);
                }
            }
            if(hook.Holding)
            {
                float swing=Vector3.Angle(Vector3.down,hook.body.position-car.trolley.position);SwingSeconds+=swing*dt;
                Instruction=held?"WINCH · RAISE / LOWER THE REAL LOAD":"HAUL TO THE NEXT PLATFORM · HOLD ACTION + DRIVE TO WINCH";
            }
            else Instruction=secondStage?"ALIGN SECOND HOOK · TAP ACTION TO RECAPTURE THE SAME LOAD":"ALIGN FIRST HOOK · TAP ACTION TO ATTACH";
            bool final=secondStage&&secondWasCaptured&&Transfers>0&&!secondHook.Holding&&Supported(receivingPad);
            quiet=final?quiet+dt:0;
            if(quiet>=1.5f){Delivered=true;LandingError=Vector3.ProjectOnPlane(cargo.Body.worldCenterOfMass-receivingTarget.position,Vector3.up).magnitude;Instruction="TWO-STATION FREIGHT DELIVERED";}
        }
        void AIControls(SuspendedFreightHook hook,PhysicalCableCar car,Collider destination,int difficulty,out Vector2 drive,out bool held,out bool pressed)
        {
            drive=Vector2.zero;held=false;pressed=false;
            float speed=car.trolley.linearVelocity.z;
            if(!hook.Holding)
            {
                float z=Eye.z-car.trolley.position.z;
                if(Mathf.Abs(z)>.4f){drive.y=Mathf.Clamp(z*.55f-speed*.8f,-.65f,.65f);return;}
                // The second hook descends outside the retracted first hook before swaying in.
                drive.x=secondStage&&hook.body.position.y>Eye.y+8?0:Mathf.Clamp((Eye.x-hook.body.position.x)*1.5f-hook.body.linearVelocity.x*.8f,-1,1);
                float height=Eye.y+.04f-hook.body.position.y;
                if(Mathf.Abs(height)>.12f){held=true;drive.y=Mathf.Clamp(height*.8f,-1,1);}
                else if(Vector3.Distance(hook.body.position,Eye)<.4f&&(hook.body.linearVelocity-cargo.Body.GetPointVelocity(Eye)).magnitude<.9f){drive.y=0;pressed=true;}
                return;
            }
            Vector3 goal=destination==receivingPad?receivingTarget.position:destination.bounds.center;
            float distance=goal.z-cargo.Body.worldCenterOfMass.z;
            drive.x=Mathf.Clamp((goal.x-cargo.Body.worldCenterOfMass.x)*.7f-cargo.Body.linearVelocity.x*.7f,-1,1);
            if(Mathf.Abs(distance)>5)
            {
                if(Lowest()<5){held=true;drive.y=1;return;}
                // The rail motor accelerates its own trolley mass. A lifted native
                // trailer can weigh twenty times as much: brake for that real assembly,
                // rather than commanding an 8m/s pendulum into a five-metre stop zone.
                float acceleration=car.acceleration*car.trolley.mass/Mathf.Max(1,car.trolley.mass+cargo.Body.mass+hook.body.mass);
                float requested=Mathf.Sign(distance)*Mathf.Min(6+difficulty,Mathf.Sqrt(2*Mathf.Max(.05f,acceleration)*Mathf.Abs(distance))*.6f,Mathf.Abs(distance)*.35f);
                drive.y=Mathf.Clamp((requested-speed)*.6f,-1,1);return;
            }
            if(Mathf.Abs(speed)>.6f){drive.y=Mathf.Clamp(-speed,-1,1);return;}
            if(Supported(destination)){pressed=true;drive.y=0;return;}
            held=true;drive.y=Lowest()>destination.bounds.max.y+.1f?-1:0;
        }
        void LateUpdate()
        {
            if(cargo==null)return;
            if(liftingEyeVisual!=null)liftingEyeVisual.SetPositionAndRotation(Eye,cargo.Body.rotation);
            if(slings!=null)for(int i=0;i<slings.Length;i++)if(slings[i]!=null)
            {Vector3 corner=eyeLocal+new Vector3(i%2==0?-.85f:.85f,-1,(i/2)==0?-1.4f:1.4f);slings[i].positionCount=2;slings[i].SetPosition(0,Eye);slings[i].SetPosition(1,cargo.Body.transform.TransformPoint(corner));}
        }
        public void RegisterImpact(Collision collision){MaximumImpact=Mathf.Max(MaximumImpact,collision.relativeVelocity.magnitude);}
        public void Freeze(){if(firstHook!=null)firstHook.Freeze();if(secondHook!=null)secondHook.Freeze();if(second!=null&&second.trolley!=null)second.trolley.isKinematic=true;}
    }
}
