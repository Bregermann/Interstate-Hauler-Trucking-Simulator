using System;
using NUnit.Framework;
using UnityEngine;

public sealed class SkyTowCarnivalBodyGateTests
{
 static bool Sweep(Vector3 start,Vector3 end,Quaternion before,Quaternion after,Vector3 extents,float radius=4.5f,Vector3? center=null,Quaternion? gateRotation=null)
 {
  var type=Type.GetType("LWS.TruckSkyTow.SkyTowBodyGate, Assembly-CSharp",true);return (bool)type.GetMethod("Sweep").Invoke(Activator.CreateInstance(type),new object[]{start,before,end,after,center??Vector3.zero,extents,Vector3.zero,gateRotation??Quaternion.identity,radius});
 }
 [TestCase(4.8f,true)][TestCase(5.1f,false)][TestCase(5.1001f,false)][TestCase(5.16f,false)]
 public void BodyEdgeEntersOpeningButFrameOnlyTangencyAndProjectedCircleDoNot(float x,bool expected)
 {
  Assert.That(Sweep(new Vector3(x,0,-4),new Vector3(x,0,4),Quaternion.identity,Quaternion.identity,new Vector3(.6f,.325f,1.35f)),Is.EqualTo(expected));
 }
 [Test]
 public void FastDiagonalSweepFindsOpeningBetweenBothEndpointPoses()
 {
  Assert.That(Sweep(new Vector3(-9,0,-8),new Vector3(9,0,8),Quaternion.identity,Quaternion.identity,new Vector3(.6f,.325f,1.35f),.12f),Is.True);
  Assert.That(Sweep(new Vector3(-9,2,-8),new Vector3(9,2,8),Quaternion.identity,Quaternion.identity,new Vector3(.6f,.325f,1.35f),.12f),Is.False);
 }
 [Test]
 public void YawedBodyAndOffsetColliderUseTheirActualGeometry()
 {
  var yaw=Quaternion.Euler(0,45,0);Assert.That(Sweep(new Vector3(5.5f,0,-4),new Vector3(5.5f,0,4),yaw,yaw,new Vector3(.6f,.325f,1.35f)),Is.True);
  Assert.That(Sweep(new Vector3(5.5f,0,-4),new Vector3(5.5f,0,4),Quaternion.identity,Quaternion.identity,new Vector3(.6f,.325f,1.35f)),Is.False);
  Assert.That(Sweep(new Vector3(5.5f,0,-4),new Vector3(5.5f,0,4),Quaternion.identity,Quaternion.identity,new Vector3(.6f,.325f,1.35f),4.5f,new Vector3(-1,0,0)),Is.True);
 }
 [Test]
 public void RotatingSweepsRemainInsideTheRealBodyAndGateSpace()
 {
  var before=Quaternion.Euler(0,-55,20);var after=Quaternion.Euler(0,55,-20);Assert.That(Sweep(new Vector3(4.3f,0,-5),new Vector3(4.3f,0,5),before,after,new Vector3(.6f,.325f,1.35f)),Is.True);
  Assert.That(Sweep(new Vector3(6.2f,0,-5),new Vector3(6.2f,0,5),before,after,new Vector3(.6f,.325f,1.35f)),Is.False);
  var gate=Quaternion.Euler(20,65,15);Assert.That(Sweep(gate*new Vector3(4.8f,0,-4),gate*new Vector3(4.8f,0,4),gate,gate,new Vector3(.6f,.325f,1.35f),4.5f,null,gate),Is.True);
 }
 [Test]
 public void AProjectedOpeningWithoutAnyPlaneIntersectionIsNotAPass()
 {
  Assert.That(Sweep(new Vector3(0,0,3),new Vector3(0,0,5),Quaternion.identity,Quaternion.identity,new Vector3(.6f,.325f,1.35f)),Is.False);
  Assert.That(Sweep(new Vector3(0,0,-5),new Vector3(0,0,-3),Quaternion.identity,Quaternion.identity,new Vector3(.6f,.325f,1.35f)),Is.False);
 }
}
