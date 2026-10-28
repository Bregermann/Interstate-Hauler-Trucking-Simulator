#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckBakeOff.Tests
{
    // Measurement-only listener on the ORIGINAL source Rigidbody. Both native
    // Collision callbacks and actual current shape geometry are recorded; no
    // callback is forwarded and no gameplay/physics state is modified.
    [DefaultExecutionOrder(32000)]
    public sealed class BakeSourceContactDiagnosticObserver : MonoBehaviour
    {
        const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        [Serializable]public sealed class Gear
        {
            public string name,body;
            public int id;
            public bool trigger,penetrates;
            public float penetration,minimumYAboveSourceTop;
            public Bounds bounds;
        }
        [Serializable]public sealed class Sample
        {
            public string reason,otherCollider,otherBody;
            public int owner,task,carrier,candidates,otherColliderId;
            public float clock,sourceVolume,throttle,brakes,handbrake,requestedReach;
            public Vector3 originalTruckPosition,originalTruckVelocity,originalTruckEuler,armBase,grip,gripVelocity,sourcePosition,sourceVelocity,sourceEuler,requestedGrip,nativeRelativeVelocity,nativeImpulse;
            public Vector3[] contactPoints,contactNormals;
            public Gear[] actualWristShapes;
        }
        [Serializable]sealed class Report {public string scope="Controlled native read-only original source/wrist/vehicle collision diagnostic; no gameplay acceptance or Windows claim.";public int owner,round;public Sample[] samples;}
        Component match,arm,stock;Rigidbody source,truckBody,grip;object rules,truck,states;int owner;bool bound;
        readonly List<Sample> samples=new List<Sample>(1000);
        static object Read(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
        public int Count=>samples.Count;
        public void Bind(Component originalMatch,Component originalArm,Component originalStock,int slot)
        {
            match=originalMatch;arm=originalArm;stock=originalStock;owner=slot;rules=Read(match,"rules");truck=Read(arm,"truck");source=(Rigidbody)Read(stock,"body");truckBody=(Rigidbody)Read(truck,"body");grip=(Rigidbody)Read(arm,"grip");states=Read(Read(Read(truck,"tractorMotor"),"vehicle"),"input");bound=true;samples.Clear();
        }
        public void ResetMeasurement(){samples.Clear();}
        void FixedUpdate(){if(bound&&match!=null&&(float)Read(rules,"Clock")<=16)Record("FIXED",null);}
        void OnCollisionEnter(Collision collision){if(bound)Record("NATIVE_COLLISION_ENTER",collision);}
        void OnCollisionStay(Collision collision){if(bound&&samples.Count<1000)Record("NATIVE_COLLISION_STAY",collision);}
        void Record(string reason,Collision collision)
        {
            if(match==null||source==null||samples.Count>=1200)return;
            var physical=((Collider[])Read(stock,"physical")).Where(shape=>shape!=null&&shape.enabled).ToArray();float top=physical.Select(shape=>shape.bounds.max.y).DefaultIfEmpty(source.position.y).Max();var actualStates=Read(states,"states");var basePoint=((Component)truck).transform.TransformPoint((Vector3)Read(arm,"mount"));
            var row=new Sample{reason=reason,owner=owner,clock=(float)Read(rules,"Clock"),task=((int[])Read(match,"task"))[owner],carrier=(int)Read(stock,"LastCarrier"),candidates=(int)Read(arm,"NativeCandidates"),sourceVolume=(float)Read(stock,"Volume"),originalTruckPosition=truckBody.position,originalTruckVelocity=truckBody.linearVelocity,originalTruckEuler=truckBody.rotation.eulerAngles,armBase=basePoint,grip=grip.position,gripVelocity=grip.linearVelocity,sourcePosition=source.position,sourceVelocity=source.linearVelocity,sourceEuler=source.rotation.eulerAngles,throttle=(float)Read(actualStates,"throttle"),brakes=(float)Read(actualStates,"brakes"),handbrake=(float)Read(actualStates,"handbrake")};
            row.requestedGrip=(Vector3)match.GetType().GetMethod("SourcePickupPoint",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{stock});row.requestedReach=Vector3.Distance(basePoint,row.requestedGrip);
            row.actualWristShapes=grip.GetComponentsInChildren<Collider>(true).Where(shape=>shape.enabled&&shape.gameObject.activeInHierarchy).Select(shape=>
            {
                float deepest=0;bool overlap=false;foreach(var own in physical)if(Physics.ComputePenetration(own,own.transform.position,own.transform.rotation,shape,shape.transform.position,shape.transform.rotation,out _,out float depth)){overlap=true;deepest=Mathf.Max(deepest,depth);}
                return new Gear{name=shape.name,id=shape.GetInstanceID(),body=shape.attachedRigidbody!=null?shape.attachedRigidbody.name:"STATIC",trigger=shape.isTrigger,bounds=shape.bounds,penetrates=overlap,penetration=deepest,minimumYAboveSourceTop=shape.bounds.min.y-top};
            }).ToArray();
            if(collision!=null)
            {
                row.otherCollider=collision.collider.name;row.otherColliderId=collision.collider.GetInstanceID();row.otherBody=collision.rigidbody!=null?collision.rigidbody.name:"STATIC";row.nativeRelativeVelocity=collision.relativeVelocity;row.nativeImpulse=collision.impulse;
                var contacts=new ContactPoint[collision.contactCount];collision.GetContacts(contacts);row.contactPoints=contacts.Select(point=>point.point).ToArray();row.contactNormals=contacts.Select(point=>point.normal).ToArray();
            }
            samples.Add(row);
        }
        public void Save(string folder,int round)
        {
            Directory.CreateDirectory(folder);string path=Path.Combine(folder,"round"+round+"-owner"+owner+".json");if(File.Exists(path))throw new IOException("Preserve earlier actual contact diagnostic.");File.WriteAllText(path,JsonUtility.ToJson(new Report{owner=owner,round=round,samples=samples.ToArray()},true));
        }
    }
}
#endif
