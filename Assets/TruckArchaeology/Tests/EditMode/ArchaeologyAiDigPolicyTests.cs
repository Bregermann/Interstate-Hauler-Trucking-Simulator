#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class ArchaeologyAiDigPolicyTests
{
 static string Choose(float clearance)=>Type.GetType("LWS.TruckArchaeology.ArchaeologyAiDigPolicy, Assembly-CSharp",true).GetMethod("Choose",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{clearance}).ToString();
 [Test]public void ActualV27NativeCutCentersUseLargestExistingBrushThatFitsOutsideActualFemur()
 {
  var go=new GameObject("Actual V27 stationary femur shape");
  try
  {
   go.transform.position=new Vector3(-54,1.8f,5);var box=go.AddComponent<BoxCollider>();box.size=new Vector3(1.5f,1.3f,4);Physics.SyncTransforms();
   // Actual saved geometry and native V27 workpoints; no assumed top-height
   // proxy. The first real Blower at40s was still2.794m from the solid box.
   foreach(var sample in new[]{new Vector3(-54.75f,5.244380f,6),new Vector3(-54,4.765269f,5.5f)})
   {
    float distance=Vector3.Distance(sample,box.ClosestPoint(sample));string selected=Choose(distance);Assert.That(selected,Is.EqualTo(sample.y>5?"Excavator":"Drill"));float radius=selected=="Excavator"?2.4f:1.25f;Assert.That(distance,Is.GreaterThan(radius+.1f));
   }
   Assert.That(Choose(Vector3.Distance(new Vector3(-54,3.75f,5),box.ClosestPoint(new Vector3(-54,3.75f,5)))),Is.EqualTo("Blower"));
   Assert.That(Choose(Vector3.Distance(new Vector3(-54,3,5),box.ClosestPoint(new Vector3(-54,3,5)))),Is.EqualTo("Brush"));Assert.That(box.size,Is.EqualTo(new Vector3(1.5f,1.3f,4)));Assert.That(go.transform.position,Is.EqualTo(new Vector3(-54,1.8f,5)));
  }
  finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [TestCase(2.5f,"Drill"),TestCase(1.35f,"Blower"),TestCase(1.15f,"Brush"),TestCase(0f,"Brush"),TestCase(-1f,"Brush")]
 public void DamageBoundaryAndInvalidGeometryNeverSelectLargerUnsafeBrush(float actualDistance,string expected)=>Assert.That(Choose(actualDistance),Is.EqualTo(expected));
 [Test]public void UnknownGeometryNeverAssumesCoarseClearance(){Assert.That(Choose(float.NaN),Is.EqualTo("Brush"));Assert.That(Choose(float.PositiveInfinity),Is.EqualTo("Brush"));}
}
#endif
