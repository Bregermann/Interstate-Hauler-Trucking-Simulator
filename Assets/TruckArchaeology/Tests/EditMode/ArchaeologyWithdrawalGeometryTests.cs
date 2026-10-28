#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class ArchaeologyWithdrawalGeometryTests
{
 [TestCase(0f),TestCase(.25f)]public void ActualV8BottomOverlapEscapesDownThenSideAndRiseWithoutRecrossingOriginalBox(float centerOffset)
 {
  var a=new GameObject("Measured V8 original femur collider");var b=new GameObject("Measured solid excavation sphere");
  try
  {
   var box=a.AddComponent<BoxCollider>();box.size=new Vector3(1.5f,1.3f,4);a.transform.position=new Vector3(-54,1.8f,5);var sphere=b.AddComponent<SphereCollider>();sphere.radius=.25f;Physics.SyncTransforms();
   var method=Type.GetType("LWS.TruckArchaeology.ArchaeologyEquipment, Assembly-CSharp",true).GetMethod("SideExit",BindingFlags.Public|BindingFlags.Static);
   {
    sphere.center=Vector3.up*centerOffset;b.transform.position=new Vector3(-54.75f,.968791f,3.75f);
    Assert.That(Physics.ComputePenetration(sphere,b.transform.position,b.transform.rotation,box,a.transform.position,a.transform.rotation,out Vector3 normal,out float depth),Is.True);TestContext.WriteLine("ARCHAEOLOGY_ACTUAL_WITHDRAWAL_GEOMETRY centerOffset="+centerOffset.ToString("R")+" actualNormal="+normal.ToString("F6")+" actualDepth="+depth.ToString("R"));
    if(centerOffset==0){Assert.That(Vector3.Dot(normal,Vector3.down),Is.GreaterThan(.99f),"Exact ORIGINAL V8 center0 sphere intrudes from below; UP is the wrong escape");Assert.That(depth,Is.EqualTo(.068791f).Within(1e-5f));}
    // Corrected center is y1.218791, inside the box's vertical span and at
    // its x-min face. A lateral MTV is shorter than the old DOWN escape.
    // The real query chooses the direction; both cases must actually clear.
    Assert.That(normal.sqrMagnitude,Is.EqualTo(1).Within(1e-5f));Assert.That(depth,Is.GreaterThan(0));
    Vector3 escape=b.transform.position+normal*(depth+.06f);float previous=depth;
    while(Vector3.Distance(b.transform.position,escape)>.00001f)
    {
     b.transform.position=Vector3.MoveTowards(b.transform.position,escape,9*.02f);bool overlap=Physics.ComputePenetration(sphere,b.transform.position,b.transform.rotation,box,a.transform.position,a.transform.rotation,out _,out float d);Assert.That(overlap?d:0,Is.LessThanOrEqualTo(previous+1e-5f),"Measured outward escape never increases actual overlap");previous=overlap?d:0;
    }
    Vector3 center=b.transform.TransformPoint(sphere.center);Vector3 side=(Vector3)method.Invoke(null,new object[]{center,box.bounds,.25f});Vector3 goal=b.transform.position+side-center;
    if(centerOffset==0)Assert.That(Vector3.Distance(goal,b.transform.position),Is.GreaterThan(.1f),"Original bottom escape must leave footprint before rising");
    // A real lateral MTV may already have left most of the footprint. Do not
    // require an invented minimum side-travel distance for that geometry.
    Bounds bounds=box.bounds;Assert.That(side.x<=bounds.min.x-.33f+.00001f||side.x>=bounds.max.x+.33f-.00001f||side.z<=bounds.min.z-.33f+.00001f||side.z>=bounds.max.z+.33f-.00001f,Is.True,"Rise starts outside actual radius-expanded footprint in either native escape direction");
    AssertSegmentClear(sphere,box,goal);goal=new Vector3(b.transform.position.x,9.3f,b.transform.position.z);AssertSegmentClear(sphere,box,goal);
    Assert.That(sphere.transform.TransformPoint(sphere.center).y-.25f,Is.GreaterThan(box.bounds.max.y));AssertSegmentClear(sphere,box,new Vector3(a.transform.position.x,9.3f,a.transform.position.z));
    Assert.That(box.size,Is.EqualTo(new Vector3(1.5f,1.3f,4)));Assert.That(a.transform.position,Is.EqualTo(new Vector3(-54,1.8f,5)));Assert.That(sphere.radius,Is.EqualTo(.25f));Assert.That(!sphere.isTrigger&&sphere.enabled,Is.True);
   }
  }
  finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
 }
 static void AssertSegmentClear(SphereCollider sphere,BoxCollider box,Vector3 destination)
 {
  int steps=0;while(Vector3.Distance(sphere.transform.position,destination)>.00001f)
  {
   Assert.That(++steps,Is.LessThan(200));sphere.transform.position=Vector3.MoveTowards(sphere.transform.position,destination,9*.02f);bool overlap=Physics.ComputePenetration(sphere,sphere.transform.position,sphere.transform.rotation,box,box.transform.position,box.transform.rotation,out _,out float depth);Assert.That(!overlap||depth<1e-5f,Is.True,"Actual solid sphere must not cross the original artifact on side/up/above-load segments: "+depth);
  }
 }
}
#endif
