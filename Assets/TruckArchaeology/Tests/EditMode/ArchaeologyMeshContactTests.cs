#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class ArchaeologyMeshContactTests
{
 static object Query()=>Activator.CreateInstance(Type.GetType("LWS.TruckArchaeology.ArchaeologyMeshContact, Assembly-CSharp",true));
 static bool Clip(object query,Bounds box,Vector3 a,Vector3 b,Vector3 c,out Vector3 point){object[] args={box,a,b,c,default(Vector3)};bool ok=(bool)query.GetType().GetMethod("ClipTriangle").Invoke(query,args);point=(Vector3)args[4];return ok;}
 [Test]public void ActualSurfaceClipHandlesFaceSliverAndCrossingWhileRejectingBoundingBoxOnlyAndDegenerateCandidates()
 {
  object query=Query();var box=new Bounds(Vector3.zero,Vector3.one*2);Assert.That(Clip(query,box,new Vector3(-3,.5f,-3),new Vector3(3,.5f,-3),new Vector3(0,.5f,3),out var surface),Is.True);Assert.That(box.Contains(surface),Is.True);Assert.That(surface.y,Is.EqualTo(.5f).Within(1e-6f));
  Assert.That(Clip(query,box,new Vector3(.99f,-2,-2),new Vector3(.99f,2,-2),new Vector3(.99f,0,2),out surface),Is.True);Assert.That(surface.x,Is.EqualTo(.99f).Within(1e-6f));Assert.That(box.Contains(surface),Is.True);
  Assert.That(Clip(query,box,new Vector3(-4,0,0),new Vector3(4,0,0),new Vector3(4,0,.1f),out surface),Is.True,"Crossing terrain triangle need not have any vertex or its original centroid inside artifact");Assert.That(box.Contains(surface),Is.True);
  Assert.That(Clip(query,box,new Vector3(2.2f,.5f,0),new Vector3(.5f,2.2f,0),new Vector3(2.2f,2.2f,0),out _),Is.False,"Triangle AABB overlap is not a genuine surface intersection");Assert.That(Clip(query,box,Vector3.zero,Vector3.one,Vector3.one*2,out _),Is.False);
 }
 [Test]public void ReadOnlyMeshQueryPreservesActualTransformedMeshAndReturnsSurfaceWithinRealOrientedScaledBox()
 {
  object query=Query();var a=new GameObject("Actual artifact box");var b=new GameObject("Actual vendor residual mesh");var mesh=new Mesh();
  try
  {
   a.transform.SetPositionAndRotation(new Vector3(-54,1.8f,5),Quaternion.Euler(0,31,0));a.transform.localScale=new Vector3(1.4f,1,1.8f);var box=a.AddComponent<BoxCollider>();box.size=new Vector3(1.5f,1.3f,4);b.transform.SetPositionAndRotation(new Vector3(-53,3,7),Quaternion.Euler(0,-19,0));b.transform.localScale=new Vector3(.8f,1.2f,1.1f);var local=new[]{new Vector3(-3,.4f,-4),new Vector3(3,.4f,-4),new Vector3(0,.4f,4)};var vertices=Array.ConvertAll(local,p=>b.transform.InverseTransformPoint(a.transform.TransformPoint(p)));mesh.vertices=vertices;mesh.triangles=new[]{0,1,2};mesh.RecalculateBounds();var collider=b.AddComponent<MeshCollider>();collider.sharedMesh=mesh;object[] args={box,collider,default(Vector3),default(Vector3),-1,-1};Assert.That((bool)query.GetType().GetMethod("TryFindSurface").Invoke(query,args),Is.True);var point=a.transform.InverseTransformPoint((Vector3)args[2]);Assert.That(new Bounds(box.center,box.size).SqrDistance(point),Is.LessThan(1e-10f));Assert.That(point.y,Is.EqualTo(.4f).Within(1e-5f));Assert.That(((Vector3)args[3]).magnitude,Is.EqualTo(1).Within(1e-6f));Assert.That((int)args[4],Is.Zero);Assert.That((int)args[5],Is.Zero);CollectionAssert.AreEqual(vertices,mesh.vertices);CollectionAssert.AreEqual(new[]{0,1,2},mesh.triangles);Assert.That(collider.sharedMesh,Is.SameAs(mesh));
  }
  finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);UnityEngine.Object.DestroyImmediate(mesh);}
 }
}
#endif
