#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderStartFormationTests
{
    static Type Tool=>Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderStartFormationAuthoring, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] values)=>Tool.GetMethod(method,BindingFlags.Public|BindingFlags.Static).Invoke(null,values);
    [TestCase(0f)][TestCase(80f)][TestCase(240f)]
    public void MeasuredLargeMountFanSeparatesAllFourCrewsWithoutGrantingAForwardHeadStart(float altitude)
    {
        const float radius=51.61f,margin=4;var target=new Vector3(0,260,540);var normal=Quaternion.Euler(0,37,0)*Vector3.forward;
        float circle=(radius*2+margin)/(2*Mathf.Sin(40f/3*Mathf.Deg2Rad))+1;var poses=(Pose[])Call("ProposeArc",target,normal,circle,altitude);
        Assert.That(poses.Length,Is.EqualTo(4));Assert.That((bool)Call("Separate",poses,radius,margin),Is.True);
        float distance=Vector3.Distance(poses[0].position,target);foreach(var pose in poses)
        {
            Assert.That(Vector3.Distance(pose.position,target),Is.EqualTo(distance).Within(.001f));
            Assert.That(Vector3.Dot(pose.position-target,normal),Is.LessThan(0),"No participant starts beyond the first gate plane");
            Assert.That((pose.rotation*Vector3.forward-(target-pose.position).normalized).magnitude,Is.LessThan(.000001f),"Use direct direction error; float acos quantizes nearly-identical directions to about.02degrees");
        }
    }
    [Test]
    public void OriginalThirtyMetreStartSpacingCannotQualifyWholeLargeMountSeparation()
    {
        var starts=Enumerable.Range(0,4).Select(i=>new Pose(new Vector3(-45+i*30,160,-30),Quaternion.identity)).ToArray();
        Assert.That((bool)Call("Separate",starts,51.61f,4f),Is.False,"The real authored30m spacing must reproduce the measured-envelope rejection");
    }
    [TestCase(0f)][TestCase(-1f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
    public void InvalidSourceEnvelopeCannotCreateAnApparentlySafeFormation(float radius)
    {var e=Assert.Throws<TargetInvocationException>(()=>Call("ProposeArc",Vector3.zero,Vector3.forward,radius,0f));Assert.That(e.InnerException,Is.TypeOf<ArgumentException>());}
    [Test]
    public void MissingActualQualificationOrExplicitOwnedSceneIsRejectedBeforeAnySceneMutation()
    {
        var measure=Assert.Throws<TargetInvocationException>(()=>Call("Measure",null,null));Assert.That(measure.InnerException,Is.TypeOf<InvalidOperationException>());
        var install=Assert.Throws<TargetInvocationException>(()=>Call("InstallOnExplicitReviewScene",null,null));Assert.That(install.InnerException,Is.TypeOf<InvalidOperationException>());
    }
}
#endif
