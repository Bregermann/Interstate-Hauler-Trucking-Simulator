#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class NestNativeTests
{
    [Test]public void MeasuredGroundPickupStowLiftsVerticallyBeforeAnyRetraction()
    {
        var method=Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp",true).GetMethod("CarryStowTarget");
        var current=new Vector3(11.031185f,-.024440566f,10.9806385f);var home=new Vector3(0,3,6.9298878f);
        int liftSteps=0,retractSteps=0;
        for(int i=0;i<600&&Vector3.Distance(current,home)>.0001f;i++)
        {
            var target=(Vector3)method.Invoke(null,new object[]{current,home});var next=Vector3.MoveTowards(current,target,.02f*2);
            Assert.That(Vector3.Distance(current,next),Is.LessThanOrEqualTo(.040001f),"Original two-metre-per-second arm target speed retained.");
            if(current.y<home.y){Assert.That(next.x,Is.EqualTo(current.x));Assert.That(next.z,Is.EqualTo(current.z));Assert.That(next.y,Is.GreaterThan(current.y));liftSteps++;}
            else{Assert.That(next.y,Is.EqualTo(home.y));retractSteps++;}
            current=next;
        }
        Assert.That(liftSteps,Is.GreaterThan(50));Assert.That(retractSteps,Is.GreaterThan(50));Assert.That(Vector3.Distance(current,home),Is.LessThan(.0001f));
    }
    [UnityTest,Timeout(35000)]public IEnumerator OrdinaryInitialAiPhysicallyLiftsAndStowsFiniteGripBeforeDrivingAway()
    {
        using(var scope=new Scope("actual-initial-pickup-stow"))
        {
            yield return scope.Load();var match=scope.match;Set(Get(match,"setup"),"count",4);Set(match,"option",0);Call(match,"AllBots");
            Assert.That((bool)Call(match,"StartMatch"),Is.True);var builders=Components(Get(match,"builders"));
            string[] physical=builders.Select(b=>Geometry((Component)Get(b,"truck"))).ToArray();
            var root=new GameObject("Test-owned read-only physical carry observer");root.transform.SetParent(match.transform,false);
            var observer=root.AddComponent<NestCarryStowProbe>();observer.Configure(match,builders);
            try
            {
                Time.timeScale=12;float deadline=Time.realtimeSinceStartup+20;
                while(observer.completed.Count<2&&observer.failure==null&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(observer.failure,Is.Null,observer.failure);Assert.That(observer.waitingSamples,Is.GreaterThan(50));Assert.That(observer.completed.Count,Is.GreaterThanOrEqualTo(2),"The first two original AI must actually lift, retract and keep their original finite physical loads.");
                foreach(int owner in observer.completed)Assert.That(Geometry((Component)Get(builders[owner],"truck")),Is.EqualTo(physical[owner]),"Original NWH truck/wheel identity, geometry and configured physics remain exact.");
                TestContext.WriteLine("NEST_PHYSICAL_STOW_PASS completed="+string.Join(",",observer.completed)+" fixedWaitingSamples="+observer.waitingSamples+" maxAchievedHandHomeError="+observer.maximumCompletedError.ToString("R")+" originalGrip2500Torque1800=true; no injected poses/input/outcomes.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
[DefaultExecutionOrder(32000)]public sealed class NestCarryStowProbe:MonoBehaviour
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Read(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    Component match;Component[] builders;readonly Dictionary<int,Component> first=new Dictionary<int,Component>();
    public readonly HashSet<int> completed=new HashSet<int>();public string failure;public int waitingSamples;public float maximumCompletedError;
    public void Configure(Component actual,Component[] actors){match=actual;builders=actors;}
    void FixedUpdate()
    {
        if(match==null||!(bool)Read(match,"Live")||failure!=null)return;
        for(int i=0;i<builders.Length;i++)
        {
            if(completed.Contains(i))continue;var builder=builders[i];var carried=(Component)Read(builder,"Carried");bool waiting=(bool)Read(builder,"WaitingForCarryStow");
            if(waiting&&carried!=null)
            {
                waitingSamples++;if(!first.ContainsKey(i))first.Add(i,carried);
                var grip=(FixedJoint)Read(carried,"Grip");var body=(Rigidbody)Read(carried,"body");var truck=(Component)Read(builder,"truck");
                if(first[i]!=carried||grip==null||grip.breakForce!=2500||grip.breakTorque!=1800||body.isKinematic)failure="Original first finite-grip load was lost/changed before physical stow: owner"+i;
                if((Vector2)Read(truck,"command")!=Vector2.zero)failure="NWH received drive during actual physical stow: owner"+i;
            }
            else if(first.TryGetValue(i,out var original))
            {
                if(carried!=original||(FixedJoint)Read(original,"Grip")==null){failure="First actual cargo grip broke before achieved stow: owner"+i;return;}
                var hand=(Rigidbody)Read(builder,"hand");var truck=(Component)Read(builder,"truck");float error=Vector3.Distance(hand.position,truck.transform.TransformPoint((Vector3)Read(builder,"carryHome")));maximumCompletedError=Mathf.Max(maximumCompletedError,error);
                if(error>.2f){failure="Drive released before actual hand reached original home: owner"+i+" error="+error.ToString("R");return;}
                completed.Add(i);
            }
        }
    }
}
#endif
