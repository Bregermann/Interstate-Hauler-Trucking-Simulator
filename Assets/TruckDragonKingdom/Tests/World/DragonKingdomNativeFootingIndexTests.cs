#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomNativeFootingIndexTests
{
    [Test]
    public void SpatialFootingCandidatesPreserveEveryActualCoreAndTransitionIncludingNegativeCellsAndSeamsInSourceOrder()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        var footingType=type.GetNestedType("NativeArchitectureFooting");var indexType=type.GetNestedType("NativeFootingIndex");var footings=Array.CreateInstance(footingType,350);var bounds=new Bounds[350];
        for(int i=0;i<350;i++)
        {
            var f=Activator.CreateInstance(footingType);footingType.GetField("id").SetValue(f,"actual-index-case-"+i);
            bounds[i]=new Bounds(new Vector3((i%25)*92-1104,0,(i/25)*91-637),new Vector3(8+i%5*3,.16f,9+i%7*2));footingType.GetField("physicalFootprint").SetValue(f,bounds[i]);footings.SetValue(f,i);
        }
        var ordinal=footings.Cast<object>().Select((f,i)=>new{f,i}).ToDictionary(pair=>pair.f,pair=>pair.i);
        var index=Activator.CreateInstance(indexType,new object[]{footings,12.5f});var at=indexType.GetMethod("At");int queries=0,examined=0,linear=0;
        foreach(var b in bounds)
        foreach(float x in new[]{b.min.x-12.5f,b.min.x,b.center.x,b.max.x,b.max.x+12.5f})
        foreach(float z in new[]{b.min.z-12.5f,b.min.z,b.center.z,b.max.z,b.max.z+12.5f})
        {
            var p=new Vector3(x,0,z);var actual=((IEnumerable)at.Invoke(index,new object[]{p})).Cast<object>().ToArray();
            var expected=Enumerable.Range(0,bounds.Length).Where(i=>Distance(bounds[i],p)<=12.5f).Select(i=>footings.GetValue(i)).ToArray();
            Assert.That(expected.All(actual.Contains),Is.True,"No exact core/transition candidate may be omitted.");
            var ordinals=actual.Select(f=>ordinal[f]).ToArray();Assert.That(ordinals,Is.Ordered,"Preserve source priority for overlapping equal-height parcels.");
            queries++;examined+=actual.Length;linear+=350;
        }
        foreach(float x in new[]{-1600f,-800f,-64f,0f,64f,800f,1600f})foreach(float z in new[]{-800f,0f,800f,1600f,2400f})
        {var p=new Vector3(x,0,z);var actual=((IEnumerable)at.Invoke(index,new object[]{p})).Cast<object>().ToArray();Assert.That(Enumerable.Range(0,bounds.Length).Where(i=>Distance(bounds[i],p)<=12.5f).Select(i=>footings.GetValue(i)).All(actual.Contains),Is.True);}
        Assert.That(queries,Is.EqualTo(8750));Assert.That(examined,Is.LessThan(linear/20),"Spatial candidates should remove the350checks-per-height-sample cost.");
        var empty=Activator.CreateInstance(indexType,new object[]{Array.CreateInstance(footingType,0),12.5f});Assert.That(((IEnumerable)at.Invoke(empty,new object[]{Vector3.zero})).Cast<object>(),Is.Empty);
    }
    static float Distance(Bounds b,Vector3 p)
    {float x=Mathf.Max(0,Mathf.Max(b.min.x-p.x,p.x-b.max.x)),z=Mathf.Max(0,Mathf.Max(b.min.z-p.z,p.z-b.max.z));return Mathf.Sqrt(x*x+z*z);}
}
#endif
