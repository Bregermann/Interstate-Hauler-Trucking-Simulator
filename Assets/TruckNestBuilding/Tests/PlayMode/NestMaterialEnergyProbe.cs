#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Test-owned observer, after installed NWH order100. Reads original public
// WheelUAPI contact/load state; never changes casts, layers, bodies or forces.
[DefaultExecutionOrder(32000)]
public sealed class NestMaterialEnergyProbe : MonoBehaviour
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    sealed class Piece
    {
        public Component component;public Rigidbody body;public string id;public float previousSpeed;
        public string lastWheel="";public float lastWheelClock=-1;
    }
    [Serializable]sealed class Row
    {
        public string kind,piece,materialKind,other,otherBody,wheel,previousWheel,ownerMode;public int frame,lastOwner,reserved,owner,fixedSamples,wheelContactEntries,speedOnsets,collisionEvents,pieceCount,wheelCount;
        public float clock,fixedTime,mass,speed,previousSpeed,load,springForce,otherBodyForceScale,previousWheelClock,jointBreakForce,otherMass;
        public Vector3 position,velocity,point,impulse,relativeVelocity,hand,handTarget,handLocalTarget,carrierPosition,carrierVelocity,gripForce,gripTorque;public bool gripped,grounded;
    }
    sealed class Lease:IDisposable {public GameObject root;public void Dispose(){if(root!=null)UnityEngine.Object.DestroyImmediate(root);}}
    Component match;Component[] builders,wheels;int[] owners;Piece[] pieces;readonly Dictionary<Rigidbody,Piece> byBody=new Dictionary<Rigidbody,Piece>();
    readonly Dictionary<Component,Collider> previousHit=new Dictionary<Component,Collider>();string path;int rows,liveSamples,wheelContactEntries,speedOnsets,collisionEvents,ordinaryBodyRows,liningBodyRows,criticalRows;
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static Component[] Components(object a)=>((IEnumerable)a).Cast<Component>().ToArray();
    public static IDisposable Begin(Component match,string path)
    {
        var root=new GameObject("Read-only natural material energy observer");root.transform.SetParent(match.transform,false);
        var observer=root.AddComponent<NestMaterialEnergyProbe>();observer.Configure(match,path);return new Lease{root=root};
    }
    void Configure(Component actual,string output)
    {
        match=actual;path=output;builders=Components(Get(match,"builders"));var allWheels=new List<Component>();var allOwners=new List<int>();
        foreach(var builder in builders){var truck=Get(builder,"truck");foreach(var wheel in Components(Get(truck,"wheels"))){allWheels.Add(wheel);allOwners.Add((int)Get(builder,"slot"));}}
        wheels=allWheels.ToArray();owners=allOwners.ToArray();
        pieces=Components(Get(match,"materials")).Select(c=>new Piece{component=c,body=(Rigidbody)Get(c,"body"),id=(string)Get(c,"pieceId")}).ToArray();
        foreach(var piece in pieces){byBody.Add(piece.body,piece);var observer=piece.component.gameObject.AddComponent<NestMaterialCollisionProbe>();observer.owner=this;observer.body=piece.body;}
        Write(new Row{kind="OBSERVER_CONFIGURED_READ_ONLY",frame=Time.frameCount,clock=(float)Get(match,"Clock"),pieceCount=pieces.Length,wheelCount=wheels.Length});
    }
    bool Live=>match!=null&&(bool)Get(match,"Live");
    Row Data(Piece piece,string kind)
    {
        int owner=(int)Get(piece.component,"LastOwner");var row=new Row{kind=kind,frame=Time.frameCount,clock=(float)Get(match,"Clock"),fixedTime=Time.fixedTime,piece=piece.id,mass=piece.body.mass,position=piece.body.position,velocity=piece.body.linearVelocity,speed=piece.body.linearVelocity.magnitude,previousSpeed=piece.previousSpeed,lastOwner=owner,reserved=(int)Get(piece.component,"ReservedBy"),gripped=(FixedJoint)Get(piece.component,"Grip")!=null,previousWheel=piece.lastWheel,previousWheelClock=piece.lastWheelClock,ownerMode=owner>=0&&owner<builders.Length?Get(builders[owner],"Mode").ToString():"UNCLAIMED"};
        row.materialKind=Get(piece.component,"kind").ToString();var grip=(FixedJoint)Get(piece.component,"Grip");if(grip!=null){row.gripForce=grip.currentForce;row.gripTorque=grip.currentTorque;}
        if(owner>=0&&owner<builders.Length){var builder=builders[owner];row.hand=((Rigidbody)Get(builder,"hand")).position;row.handTarget=(Vector3)Get(builder,"HandTarget");row.handLocalTarget=(Vector3)Get(builder,"localTarget");var carrier=(Rigidbody)Get(Get(builder,"truck"),"body");row.carrierPosition=carrier.position;row.carrierVelocity=carrier.linearVelocity;}return row;
    }
    void Write(Row row)
    {
        bool body=row.kind=="ACTUAL_PHYSX_BODY_COLLISION",lining=row.materialKind=="Leaves"||row.materialKind=="Moss";
        bool record=body?(lining?liningBodyRows++<128:ordinaryBodyRows++<64):criticalRows++<128;
        if(record){rows++;File.AppendAllText(path,JsonUtility.ToJson(row)+"\n");}
    }
    void FixedUpdate()
    {
        if(!Live)return;liveSamples++;
        for(int i=0;i<wheels.Length;i++)
        {
            var wheel=wheels[i];var hit=(Collider)Get(wheel,"HitCollider");bool grounded=(bool)Get(wheel,"IsGrounded");
            if(grounded&&hit!=null&&hit.attachedRigidbody!=null&&byBody.TryGetValue(hit.attachedRigidbody,out var piece))
            {
                piece.lastWheel=owners[i]+"/"+wheel.name;piece.lastWheelClock=(float)Get(match,"Clock");
                if(!previousHit.TryGetValue(wheel,out var prior)||prior!=hit)
                {
                    wheelContactEntries++;var row=Data(piece,"NWH_PUBLIC_REAL_MATERIAL_SUPPORT");row.owner=owners[i];row.wheel=wheel.name;row.grounded=true;row.point=(Vector3)Get(wheel,"HitPoint");row.load=(float)Get(wheel,"Load");row.springForce=(float)Get(wheel,"SpringForce");row.otherBodyForceScale=(float)Get(wheel,"otherBodyForceScale");Write(row);
                }
            }
            previousHit[wheel]=grounded?hit:null;
        }
        foreach(var piece in pieces)
        {
            float speed=piece.body.linearVelocity.magnitude;if(speed>20&&piece.previousSpeed<=20){speedOnsets++;Write(Data(piece,"MATERIAL_SPEED_CROSSED20_READ_ONLY"));}piece.previousSpeed=speed;
        }
    }
    public void Collision(Rigidbody body,Collision collision)
    {
        if(!Live||!byBody.TryGetValue(body,out var piece)||(collision.impulse.magnitude<30&&collision.relativeVelocity.magnitude<20))return;
        collisionEvents++;var row=Data(piece,"ACTUAL_PHYSX_BODY_COLLISION");row.other=collision.collider.name;var otherBody=collision.collider.attachedRigidbody;row.otherBody=otherBody==null?"STATIC":otherBody.name;row.otherMass=otherBody==null?0:otherBody.mass;row.impulse=collision.impulse;row.relativeVelocity=collision.relativeVelocity;if(collision.contactCount>0)row.point=collision.GetContact(0).point;Write(row);
    }
    public void JointBreak(Rigidbody body,float force)
    {
        if(!Live||!byBody.TryGetValue(body,out var piece))return;var row=Data(piece,"ACTUAL_PHYSX_JOINT_BREAK");row.jointBreakForce=force;Write(row);
    }
    void OnDestroy()
    {
        if(pieces==null)return;
        File.AppendAllText(path,JsonUtility.ToJson(new Row{kind="OBSERVER_COMPLETE_READ_ONLY",frame=Time.frameCount,clock=match==null?-1:(float)Get(match,"Clock"),fixedSamples=liveSamples,wheelContactEntries=wheelContactEntries,speedOnsets=speedOnsets,collisionEvents=collisionEvents,pieceCount=pieces.Length,wheelCount=wheels.Length})+"\n");
        foreach(var piece in pieces)if(piece.component!=null)foreach(var probe in piece.component.GetComponents<NestMaterialCollisionProbe>())if(probe.owner==this)UnityEngine.Object.DestroyImmediate(probe);
    }
}
public sealed class NestMaterialCollisionProbe:MonoBehaviour
{
    public NestMaterialEnergyProbe owner;public Rigidbody body;
    void OnJointBreak(float force){if(owner!=null)owner.JointBreak(body,force);}
    void OnCollisionEnter(Collision collision){if(owner!=null)owner.Collision(body,collision);}
    void OnCollisionStay(Collision collision){if(owner!=null)owner.Collision(body,collision);}
}
#endif
