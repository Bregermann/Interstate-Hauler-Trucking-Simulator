using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class JengaLiftClearanceTests
{
    static bool Overlap(Bounds a,Bounds b)=>(bool)Type.GetType("LWS.TruckJenga.JengaMatch, Assembly-CSharp",true).GetMethod("ColumnFootprintsOverlap",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{a,b});
    [Test]public void FloorLevelBodyCannotBeDeclaredLiftClearUnderActualDisplacedTowerOverhang()
    {
        // Real original box dimensions, explicit geometric fixture. This does not
        // claim the upper body pose was sampled at the old handoff instant.
        var load=new Bounds(new Vector3(10.66145f,1.2f,.129315f),new Vector3(9.575f,2.4f,3.175f));
        var overhang=new Bounds(new Vector3(4.6f,6,0),new Vector3(3.175f,2.4f,9.575f));
        Assert.That(load.Intersects(overhang),Is.False,"Vertical separation makes the superseded3D-only gate pass.");Assert.That(Overlap(load,overhang),Is.True,"The actual vertical lift column is still obstructed.");
    }
    [Test]public void GenuinePlanarSeparationCanPassDespiteDifferentHeightAndConservativeExpandedShapes()
    {
        var a=new Bounds(new Vector3(12,1.2f,0),new Vector3(9.575f,2.4f,3.175f));var b=new Bounds(new Vector3(4.6f,6,0),new Vector3(3.175f,2.4f,9.575f));b.Expand(.08f);
        Assert.That(Overlap(a,b),Is.False);a.center=new Vector3(0,1.2f,12);Assert.That(Overlap(a,b),Is.False);
    }
}
