#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
public sealed class ArchaeologyFootprintTests
{
 [Test]public void AllThreeOriginalPhysicalBoxesIncludeActualEdgesInteriorAndRetainedOuterNineWithinBoundedPlan()
 {
  var type=Type.GetType("LWS.TruckArchaeology.ArchaeologyTerrain, Assembly-CSharp",true);var method=type.GetMethod("FootprintPoints");foreach(Vector3 size in new[]{new Vector3(3,1.6f,2.8f),new Vector3(1.5f,1.3f,4),new Vector3(1.9f,2.5f,1.9f)})
  {
   var bounds=new Bounds(new Vector3(-54,1.8f,5),size);var points=((IEnumerable)method.Invoke(null,new object[]{bounds})).Cast<Vector3>().ToArray();int nx=Mathf.CeilToInt(size.x/.25f),nz=Mathf.CeilToInt(size.z/.25f);Assert.That(points.Length,Is.EqualTo((nx+1)*(nz+1)+9));Assert.That(points.Length,Is.LessThanOrEqualTo(634));for(int x=0;x<2;x++)for(int z=0;z<2;z++){var corner=new Vector3(x==0?bounds.min.x:bounds.max.x,bounds.center.y,z==0?bounds.min.z:bounds.max.z);Assert.That(points.Any(p=>Vector3.Distance(p,corner)<1e-5f),Is.True,"Actual box corner must be probed, not only outside wells");}Assert.That(size.x/nx,Is.LessThanOrEqualTo(.250001f));Assert.That(size.z/nz,Is.LessThanOrEqualTo(.250001f));int i=points.Length-9;for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)Assert.That(Vector3.Distance(points[i++],bounds.center+new Vector3(x*(bounds.extents.x+.25f),0,z*(bounds.extents.z+.25f))),Is.LessThan(1e-5f),"All original outer-nine queries remain");Assert.That(points.Take(points.Length-9).Any(p=>Mathf.Abs(p.x-bounds.center.x)>.01f&&Mathf.Abs(p.z-bounds.center.z)>.01f&&bounds.Contains(p)),Is.True,"Plan includes real footprint interior missed by old9");
  }
 }
}
#endif
