using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckFashionShow
{
    // Deliberate runway choreography, adapted from existing DanceTruckRig. It is not a second drivetrain.
    public sealed class FashionStrut:MonoBehaviour
    {
        public SportsEventTruck truck;public Vector3 Position {get;private set;}public Quaternion Heading {get;private set;}=Quaternion.identity;
        public float Distance {get;private set;}public int PoseChanges {get;private set;}public bool Performing {get;private set;}
        Rigidbody[] bodies;Vector3[] local;Quaternion[] rotation;bool[] kinematic,wheelEnabled;bool vehicleEnabled,initialized;float gait,hitAt=-10;int pose;bool hit;
        public void Begin(Vector3 position,Quaternion heading)
        {
            if(!initialized)
            {
                bodies=truck.GetComponentsInChildren<Rigidbody>(true);local=new Vector3[bodies.Length];rotation=new Quaternion[bodies.Length];kinematic=new bool[bodies.Length];
                for(int i=0;i<bodies.Length;i++){local[i]=truck.transform.InverseTransformPoint(bodies[i].position);rotation[i]=Quaternion.Inverse(truck.transform.rotation)*bodies[i].rotation;kinematic[i]=bodies[i].isKinematic;}
                wheelEnabled=new bool[truck.wheels.Length];for(int i=0;i<truck.wheels.Length;i++)wheelEnabled[i]=truck.wheels[i].enabled;vehicleEnabled=truck.tractorMotor.vehicle.enabled;initialized=true;
            }
            truck.Prepare(position,heading,false);truck.Power(false);truck.tractorMotor.vehicle.enabled=false;for(int i=0;i<truck.wheels.Length;i++)truck.wheels[i].enabled=false;
            foreach(var b in bodies){if(!b.isKinematic)b.linearVelocity=b.angularVelocity=Vector3.zero;b.isKinematic=true;}
            Position=position;Heading=heading;Distance=gait=0;PoseChanges=0;Performing=true;hitAt=-10;Pose(0,0);
        }
        public void Drive(Vector2 command,float dt,float clock)
        {
            Heading=Quaternion.Euler(0,Heading.eulerAngles.y+command.x*85*dt,0);Move(Heading*Vector3.forward*(Mathf.Clamp(command.y,-1,1)*3.4f*dt),dt,clock);
        }
        public bool WalkTo(Vector3 target,float dt,float clock)
        {
            Vector3 d=Vector3.ProjectOnPlane(target-Position,Vector3.up);if(d.magnitude<.35f){Pose(clock,0);return true;}
            Heading=Quaternion.RotateTowards(Heading,Quaternion.LookRotation(d),140*dt);float alignment=Mathf.Clamp01(Vector3.Dot(Heading*Vector3.forward,d.normalized));Move(d.normalized*Mathf.Min(d.magnitude,3.4f*dt)*alignment,dt,clock);return false;
        }
        void Move(Vector3 step,float dt,float clock)
        {
            Vector3 next=Position+step;next.x=Mathf.Clamp(next.x,-55,55);next.z=Mathf.Clamp(next.z,-51,46);
            bool blocked=false;foreach(var c in Physics.OverlapBox(next+Vector3.up*2,new Vector3(2,1.5f,3.8f),Heading,~0,QueryTriggerInteraction.Ignore))
            {
                if(c.transform.IsChildOf(truck.transform)||c.transform==truck.transform)continue;
                // Only physical world/other original trucks block a walk; display props are render-only.
                blocked=true;break;
            }
            float moved=blocked?0:Vector3.Distance(Position,next);if(!blocked)Position=next;Distance+=moved;gait+=moved*2.6f;Pose(clock,moved/Mathf.Max(.001f,dt));
            for(int i=0;i<truck.wheels.Length;i++){var w=truck.wheels[i];if(w.wheel.rotatingContainer!=null)w.wheel.rotatingContainer.Rotate(Vector3.right,moved*57.2958f/Mathf.Max(.1f,w.wheel.radius),Space.Self);}
        }
        public void Strike(int style,bool success,float clock){pose=style%5;hit=success;hitAt=clock;PoseChanges++;}
        public void Face(Quaternion target,float dt,float clock){Heading=Quaternion.RotateTowards(Heading,target,140*dt);Pose(clock,0);}
        public void Idle(float clock){Pose(clock,0);}
        void Pose(float clock,float speed)
        {
            if(!Performing)return;float walk=Mathf.Clamp01(speed/2),pulse=Mathf.Sin(gait),age=clock-hitAt;Vector3 offset=Vector3.up*(.035f+.11f*Mathf.Abs(pulse)*walk);Vector3 e=new Vector3(Mathf.Sin(clock*2)*1.3f,pulse*2*walk,pulse*3.5f*walk);
            if(age>=0&&age<1.2f){float t=age/1.2f,a=Mathf.Sin(t*Mathf.PI);if(!hit)e.x+=a*5;else switch(pose){case 0:e.z+=a*9;break;case 1:offset.y+=a*.22f;e.x-=a*10;break;case 2:e.y+=Mathf.SmoothStep(0,1,t)*360;break;case 3:e.x+=a*8;e.z+=a*4;break;case 4:e.z-=a*11;offset.y+=a*.12f;break;}}
            Quaternion world=Heading*Quaternion.Euler(e);Vector3 position=Position+offset;
            for(int i=0;i<bodies.Length;i++){Vector3 p=position+world*Vector3.Scale(local[i],truck.transform.lossyScale);Quaternion q=world*rotation[i];bodies[i].MovePosition(p);bodies[i].MoveRotation(q);}
        }
        public void Stop(){Performing=false;if(truck!=null){truck.Power(false);truck.StopSound();}}
        public void RestoreOriginal()
        {
            if(!initialized||truck==null)return;Stop();if(truck.tractorMotor!=null&&truck.tractorMotor.vehicle!=null)truck.tractorMotor.vehicle.enabled=vehicleEnabled;for(int i=0;i<truck.wheels.Length;i++)if(truck.wheels[i]!=null)truck.wheels[i].enabled=wheelEnabled[i];for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null)bodies[i].isKinematic=kinematic[i];
        }
        void OnDestroy(){RestoreOriginal();}
    }
}
