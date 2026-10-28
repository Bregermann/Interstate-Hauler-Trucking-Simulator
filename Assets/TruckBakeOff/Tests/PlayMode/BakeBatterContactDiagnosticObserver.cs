#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckBakeOff.Tests
{
    // Read-only observer on each ORIGINAL bowl/pan Rigidbody. No contact is
    // forwarded, no physics property is written and no gameplay gate is bypassed.
    [DefaultExecutionOrder(32000)]
    public sealed class BakeBatterContactDiagnosticObserver:MonoBehaviour
    {
        const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        [Serializable]public sealed class Shape {public string path;public Bounds bounds;}
        [Serializable]public sealed class Sample
        {
            public string reason,role,stage,otherCollider,otherBody,heldName;
            public int owner,task,heldId;
            public float clock,panVolume,bowlVolume,mix,desiredReach,bowlGoalError,panTop,ovenRequestedReach,ovenBodyError;
            public Vector3 ovenTrayTarget,ovenRequestedGrip,ovenClampedGrip,panHeldOffset;public Bounds originalOvenShelf,originalTruckHull;
            public Vector3 actualDoorPosition,actualDoorEuler,hypotheticalClearLift,hypotheticalHorizontalWithdraw;public float actualDoorAngle,clearLiftReach,horizontalWithdrawReach;public bool wantsClosed;public Shape[] actualWristSolid,actualDoorSolid;
            public Bounds originalDecorationWorktop;public Vector3 actualCounterBodyTarget,counterRequestedGrip,counterExactParkBase;public float counterRequestedReach,counterExactParkReach,counterBottomAtOriginalSeat;public Vector3[] actualNozzleQuadrantGrips;public float[] actualNozzleQuadrantReach;
            public Vector3 originalTruck,truckVelocity,armBase,grip,gripVelocity,worldGoal,bowl,bowlVelocity,bowlEuler,pan,panVelocity,panEuler,panSpawn,desiredBowl,desiredGrip,heldOffset,nativeRelativeVelocity,nativeImpulse;
            public Vector3[] contactPoints,contactNormals;
            public Shape[] actualPanPhysical;
        }
        [Serializable]sealed class Report {public string scope="CONTROLLED NATIVE READ-ONLY actual original bowl/pan contacts and8m requested grip; no Windows/gameplay acceptance claim.";public string role;public int owner;public Sample[] samples;}
        readonly List<Sample> samples=new List<Sample>(1000);
        Component match,arm,bowl,pan;object rules,truck;Rigidbody grip,bowlBody,panBody,truckBody,ovenDoor;Collider decorationWorktop,decorationBase;Transform nozzleSpout;string role;int owner;float next,firstClock;
        static object Read(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
        public int Count=>samples.Count;public int DoorPanContacts{get;private set;}public int CounterBaseContacts{get;private set;}
        public void Bind(Component mode,Component originalArm,Component originalBowl,Component originalPan,int slot,string observedRole,float beginClock=0)
        {
            match=mode;arm=originalArm;bowl=originalBowl;pan=originalPan;owner=slot;role=observedRole;firstClock=beginClock;rules=Read(match,"rules");truck=Read(arm,"truck");truckBody=(Rigidbody)Read(truck,"body");grip=(Rigidbody)Read(arm,"grip");bowlBody=(Rigidbody)Read(bowl,"body");panBody=(Rigidbody)Read(pan,"body");ovenDoor=(Rigidbody)Read(((Array)Read(match,"ovens")).GetValue(owner),"door");var expected=new Vector3((owner-1.5f)*34+8,2.9f,-5);var candidates=pan.transform.root.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.name=="Studio worktop polished stainless"&&(c.bounds.center-expected).sqrMagnitude<.000001f&&(c.bounds.size-new Vector3(6,.2f,9)).sqrMagnitude<.000001f).ToArray();if(candidates.Length!=1)throw new InvalidOperationException("Exact original decoration worktop name/pose/size required; no generic-first-child or guessed collider.");decorationWorktop=candidates[0];decorationBase=pan.transform.root.GetComponentsInChildren<BoxCollider>(true).Single(c=>c.name=="Physical steel baking workstation "+owner+" / 2"&&(c.bounds.center-new Vector3((owner-1.5f)*34+8,1.4f,-5)).sqrMagnitude<.000001f);nozzleSpout=(Transform)Read(((Array)Read(match,"nozzles")).GetValue(owner),"spout");Record("INITIAL_ORIGINAL_POSE",null);
        }
        void LateUpdate()
        {
            if(match==null)return;float clock=(float)Read(rules,"Clock");if(clock<firstClock||clock<next)return;int task=((int[])Read(match,"task"))[owner];next=clock+(task>=5?.1f:1f);Record("LATE_ORIGINAL_POSE",null);
        }
        void OnCollisionEnter(Collision collision){if(match!=null&&collision.rigidbody==ovenDoor)DoorPanContacts++;if(match!=null&&collision.collider==decorationBase)CounterBaseContacts++;if(match!=null)Record("NATIVE_COLLISION_ENTER",collision);}
        void OnCollisionStay(Collision collision)
        {
            if(match!=null&&collision.rigidbody==ovenDoor)DoorPanContacts++;if(match!=null&&collision.collider==decorationBase)CounterBaseContacts++;
            if(match!=null&&(float)Read(rules,"Clock")>80&&(collision.relativeVelocity.sqrMagnitude>.0025f||collision.rigidbody!=null&&collision.impulse.sqrMagnitude>.01f))Record("NATIVE_MOVING_COLLISION_STAY",collision);
        }
        static string PathOf(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
        void Record(string reason,Collision collision)
        {
            if(samples.Count>=1800||(float)Read(rules,"Clock")<firstClock&&reason!="INITIAL_ORIGINAL_POSE")return;var held=Read(arm,"Held") as Component;var target=(Vector3)match.GetType().GetMethod("PourBodyTarget",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{pan});var offset=bowlBody.position-grip.position;var requested=target-offset;var basePoint=((Component)truck).transform.TransformPoint((Vector3)Read(arm,"mount"));var physical=((Collider[])Read(pan,"physical")).Where(shape=>shape!=null&&shape.enabled).ToArray();
            var s=new Sample{reason=reason,role=role,owner=owner,clock=(float)Read(rules,"Clock"),task=((int[])Read(match,"task"))[owner],stage=((Array)Read(rules,"stages")).GetValue(owner).ToString(),heldName=held!=null?held.name:"NONE",heldId=held!=null?(int)Read(held,"id"):-1,panVolume=(float)Read(pan,"Volume"),bowlVolume=(float)Read(bowl,"Volume"),mix=((float[])Read(rules,"mixSeconds"))[owner],originalTruck=truckBody.position,truckVelocity=truckBody.linearVelocity,armBase=basePoint,grip=grip.position,gripVelocity=grip.linearVelocity,worldGoal=(Vector3)Read(arm,"WorldGoal"),bowl=bowlBody.position,bowlVelocity=bowlBody.linearVelocity,bowlEuler=bowlBody.rotation.eulerAngles,pan=panBody.position,panVelocity=panBody.linearVelocity,panEuler=panBody.rotation.eulerAngles,panSpawn=(Vector3)Read(pan,"spawn"),desiredBowl=target,desiredGrip=requested,heldOffset=offset,desiredReach=Vector3.Distance(basePoint,requested),bowlGoalError=Vector3.Distance(bowlBody.position,target),panTop=physical.Select(shape=>shape.bounds.max.y).DefaultIfEmpty(panBody.position.y).Max(),actualPanPhysical=physical.Select(shape=>new Shape{path=PathOf(shape.transform),bounds=shape.bounds}).ToArray()};
            var oven=((Array)Read(match,"ovens")).GetValue(owner);var tray=(Transform)Read(oven,"trayPoint");s.ovenTrayTarget=tray.position+Vector3.up*.2f;s.panHeldOffset=panBody.position-grip.position;s.ovenRequestedGrip=s.ovenTrayTarget-s.panHeldOffset;s.ovenRequestedReach=Vector3.Distance(basePoint,s.ovenRequestedGrip);s.ovenClampedGrip=basePoint+Vector3.ClampMagnitude(s.ovenRequestedGrip-basePoint,8);s.ovenBodyError=Vector3.Distance(panBody.position,s.ovenTrayTarget);s.originalOvenShelf=((Collider)Read(oven,"shelf")).bounds;s.originalTruckHull=((Collider)Read(truck,"shape")).bounds;
            var door=(Rigidbody)Read(oven,"door");s.actualDoorPosition=door.position;s.actualDoorEuler=door.rotation.eulerAngles;s.actualDoorAngle=Quaternion.Angle(door.rotation,Quaternion.identity);s.wantsClosed=(bool)Read(oven,"WantsClosed");var wrist=grip.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).ToArray();var doorPhysical=door.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).ToArray();s.actualWristSolid=wrist.Select(c=>new Shape{path=PathOf(c.transform),bounds=c.bounds}).ToArray();s.actualDoorSolid=doorPhysical.Select(c=>new Shape{path=PathOf(c.transform),bounds=c.bounds}).ToArray();float panHighest=physical.Select(c=>c.bounds.max.y).DefaultIfEmpty(panBody.position.y).Max();float wristLowest=wrist.Select(c=>c.bounds.min.y).DefaultIfEmpty(grip.position.y).Min();s.hypotheticalClearLift=grip.position+Vector3.up*Mathf.Max(0,panHighest+.2f-wristLowest);s.clearLiftReach=Vector3.Distance(basePoint,s.hypotheticalClearLift);s.hypotheticalHorizontalWithdraw=s.hypotheticalClearLift;float wristFront=wrist.Select(c=>c.bounds.max.z).DefaultIfEmpty(grip.position.z).Max()-grip.position.z;float panNear=physical.Select(c=>c.bounds.min.z).DefaultIfEmpty(panBody.position.z).Min();s.hypotheticalHorizontalWithdraw.z=Mathf.Min(s.hypotheticalHorizontalWithdraw.z,panNear-.2f-wristFront);s.horizontalWithdrawReach=Vector3.Distance(basePoint,s.hypotheticalHorizontalWithdraw);
            s.originalDecorationWorktop=decorationWorktop.bounds;s.actualCounterBodyTarget=new Vector3((owner-1.5f)*34+8,3.2f,-5);s.counterRequestedGrip=s.actualCounterBodyTarget-s.panHeldOffset;s.counterRequestedReach=Vector3.Distance(basePoint,s.counterRequestedGrip);var exactPark=new Vector3((owner-1.5f)*34+.8f,truckBody.position.y,-7.5f);s.counterExactParkBase=basePoint+exactPark-truckBody.position;s.counterExactParkReach=Vector3.Distance(s.counterExactParkBase,s.counterRequestedGrip);s.counterBottomAtOriginalSeat=s.actualCounterBodyTarget.y+physical.Select(c=>c.bounds.min.y).DefaultIfEmpty(panBody.position.y).Min()-panBody.position.y;s.actualNozzleQuadrantGrips=new Vector3[4];s.actualNozzleQuadrantReach=new float[4];for(int q=0;q<4;q++){var quadrantGoal=s.actualCounterBodyTarget+new Vector3(q%2==0?-.7f:.7f,2.2f,q<2?-.7f:.7f);s.actualNozzleQuadrantGrips[q]=quadrantGoal-(nozzleSpout.position-grip.position);s.actualNozzleQuadrantReach[q]=Vector3.Distance(basePoint,s.actualNozzleQuadrantGrips[q]);}
            if(collision!=null){s.otherCollider=PathOf(collision.collider.transform);s.otherBody=collision.rigidbody!=null?PathOf(collision.rigidbody.transform):"STATIC";s.nativeRelativeVelocity=collision.relativeVelocity;s.nativeImpulse=collision.impulse;var contacts=new ContactPoint[collision.contactCount];collision.GetContacts(contacts);s.contactPoints=contacts.Select(p=>p.point).ToArray();s.contactNormals=contacts.Select(p=>p.normal).ToArray();}samples.Add(s);
        }
        public void Save(string folder)
        {
            Record("FINAL_ORIGINAL_POSE",null);Directory.CreateDirectory(folder);string path=System.IO.Path.Combine(folder,"owner"+owner+"-"+role+".json");if(File.Exists(path))throw new IOException("Preserve previous actual batter contact diagnostic.");File.WriteAllText(path,JsonUtility.ToJson(new Report{owner=owner,role=role,samples=samples.ToArray()},true));
        }
    }
}
#endif
