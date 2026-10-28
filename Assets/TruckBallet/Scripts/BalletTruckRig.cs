using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckBallet
{
    // Same stationary kinematic exhibition boundary as accepted Yoga/Dance.
    // NWH retains its exact complete truck/Cab/Wheels hierarchy and settings.
    // ONLY this fixed-step adapter commands the full assembly's body poses.
    public sealed class BalletTruckRig:MonoBehaviour
    {
        public SportsEventTruck truck;
        public Rigidbody[] attachedBodies=Array.Empty<Rigidbody>();
        public Vector3 home;
        public BalletCostume costume;
        public int slot;
        public int Bank{get;private set;}
        public int PoseSteps{get;private set;}
        public int BoundAppearanceRevision{get;private set;}=-1;
        public string BankLabel=>Bank==0?"POSTURE: PITCH / LEAN":"TURN / LIFT";
        public BalletPose Achieved{get;private set;}
        public BalletPose Previous{get;private set;}
        public BalletPose Intent{get;private set;}
        public float FloorSupportRise{get;private set;}
        Quaternion upright;
        Vector3[] positions,footPoints;
        Quaternion[] rotations;
        float pitch,lean,lift,yaw,rate,observedYaw,heading;
        bool initialized;
        void Awake(){truck.Power(false);truck.body.isKinematic=true;foreach(var body in attachedBodies){body.isKinematic=true;body.detectCollisions=false;}}
        void Initialize()
        {
            if(initialized)return;initialized=true;upright=truck.transform.rotation;positions=new Vector3[attachedBodies.Length];rotations=new Quaternion[attachedBodies.Length];
            for(int i=0;i<attachedBodies.Length;i++){positions[i]=truck.transform.InverseTransformPoint(attachedBodies[i].position);rotations[i]=Quaternion.Inverse(truck.transform.rotation)*attachedBodies[i].rotation;}
            footPoints=new Vector3[truck.wheels.Length];for(int i=0;i<footPoints.Length;i++)footPoints[i]=truck.transform.InverseTransformPoint(truck.wheels[i].transform.position)-Vector3.up*(truck.wheels[i].wheel.radius+truck.wheels[i].spring.maxLength);
        }
        public void Begin(bool active)
        {
            Initialize();truck.gameObject.SetActive(active);if(!active)return;truck.Prepare(home,upright,false);truck.Power(false);truck.body.isKinematic=true;truck.body.detectCollisions=false;
            foreach(var body in attachedBodies){body.isKinematic=true;body.detectCollisions=false;}Bank=PoseSteps=0;pitch=lean=lift=yaw=rate=observedYaw=heading=0;Achieved=Previous=Intent=default;FloorSupportRise=0;RefreshAppearance();costume?.Begin();
        }
        void RefreshAppearance(){if(truck.appearanceHost!=null)BoundAppearanceRevision=truck.appearanceHost.AppearanceRevision;}
        float Support(Quaternion rotation)
        {
            float rise=0;foreach(var point in footPoints)rise=Mathf.Max(rise,(upright*point).y-(rotation*point).y);return rise;
        }
        public Vector2 AI(BalletPose goal)
        {
            // Acquire the intended posture first, then keep the turn bank steady.
            // Comparing ever-shrinking error magnitudes switched back to POSTURE
            // mid-spin; that bank correctly stops a turn for human control.
            Bank=Mathf.Abs(goal.pitch-Intent.pitch)>.2f||Mathf.Abs(goal.lean-Intent.lean)>.2f?0:1;
            return Bank==0?Vector2.ClampMagnitude(new Vector2((goal.lean-Intent.lean)/8,(goal.pitch-Intent.pitch)/8),1):new Vector2(Mathf.Clamp((goal.turnRate-Intent.turnRate)/30,-1,1),Mathf.Clamp((goal.lift-Intent.lift)/.2f,-1,1));
        }
        public void Step(Vector2 axes,bool bankAction,float dt)
        {
            if(!truck.gameObject.activeInHierarchy||!float.IsFinite(dt)||dt<=0||dt>.25f)return;RefreshAppearance();if(bankAction)Bank=1-Bank;
            Previous=Achieved;var actual=Quaternion.Inverse(upright)*truck.body.rotation;float angle=Mathf.DeltaAngle(0,actual.eulerAngles.y),delta=Mathf.DeltaAngle(observedYaw,angle);heading+=delta;observedYaw=angle;
            Achieved=new BalletPose(Mathf.DeltaAngle(0,actual.eulerAngles.x),Mathf.DeltaAngle(0,actual.eulerAngles.z),truck.body.position.y-home.y-Support(truck.body.rotation),delta/dt,heading);
            var target=Intent;
            if(Bank==0){target.pitch=Mathf.Clamp(target.pitch+axes.y*30*dt,-35,30);target.lean=Mathf.Clamp(target.lean+axes.x*27*dt,-28,28);target.turnRate=0;}
            else{target.lift=Mathf.Clamp(target.lift+axes.y*.9f*dt,-.18f,1.5f);target.turnRate=Mathf.Clamp(target.turnRate+axes.x*100*dt,-150,150);}
            Intent=target;pitch=Mathf.MoveTowards(pitch,target.pitch,24*dt);lean=Mathf.MoveTowards(lean,target.lean,22*dt);lift=Mathf.MoveTowards(lift,target.lift,.75f*dt);rate=Mathf.MoveTowards(rate,target.turnRate,120*dt);yaw=Mathf.Repeat(yaw+rate*dt+180,360)-180;
            var rotation=upright*Quaternion.Euler(pitch,yaw,lean);FloorSupportRise=Support(rotation);var position=home+Vector3.up*(lift+FloorSupportRise);truck.body.MovePosition(position);truck.body.MoveRotation(rotation);
            for(int i=0;i<attachedBodies.Length;i++){attachedBodies[i].MovePosition(position+rotation*positions[i]);attachedBodies[i].MoveRotation(rotation*rotations[i]);}PoseSteps++;
        }
        public void Stop()
        {
            truck.Power(false);truck.StopSound();truck.body.isKinematic=true;if(!initialized)return;truck.body.position=home;truck.body.rotation=upright;
            for(int i=0;i<attachedBodies.Length;i++){attachedBodies[i].isKinematic=true;attachedBodies[i].position=home+upright*positions[i];attachedBodies[i].rotation=upright*rotations[i];}costume?.Stop();
        }
    }
}
