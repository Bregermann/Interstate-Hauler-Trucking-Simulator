#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonKingdomNativeSdkStencilTests
{
    static object Measure(Vector3[] left,Vector3[] right,bool[] bridge,int phase)
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomVendorFalloffAuthoring, Assembly-CSharp-Editor",true);return type.GetMethod("MeasureInstalledSdkStencil").Invoke(null,new object[]{left,right,bridge,new Vector3(0,-20,800),new Vector3(1600,600,1600),new Vector3(3.125f,600,3.125f),513,110f,phase});
    }
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    [Test]
    public void Installed513CenterAndCeiledRadiusIncludeMeasuredTwoCellReachButRejectNextCell()
    {
        var proof=Measure(new[]{new Vector3(1411.4897f,0,1064.0625f)},new[]{Vector3.zero},new[]{false},-1);var allowed=(bool[])Field(proof,"allowed");
        Assert.That((int)Field(proof,"radiusX"),Is.EqualTo(36));Assert.That((int)Field(proof,"radiusZ"),Is.EqualTo(36));Assert.That((int)Field(proof,"allowedSamples"),Is.EqualTo(72*72));Assert.That((int)Field(proof,"eligibleLeft"),Is.EqualTo(1));Assert.That((int)Field(proof,"eligibleRight"),Is.Zero);Assert.That((int)Field(proof,"expectedFinalPhase"),Is.Zero);
        // SDK center453, left phase-1 and36-cell radius => columns418..489.
        Assert.That(allowed[85*513+418],Is.True);Assert.That(allowed[85*513+489],Is.True);Assert.That(allowed[85*513+417],Is.False);Assert.That(allowed[85*513+490],Is.False);Assert.That(((Bounds)Field(proof,"actualGridBounds")).max.x,Is.EqualTo(1528.125f));
    }
    [Test]
    public void BridgeExclusionAndTerrainInsetCannotGainAnUnmeasuredSupportRegion()
    {
        var excluded=Measure(new[]{new Vector3(1411.4897f,0,1064.0625f)},new[]{Vector3.zero},new[]{true},-1);Assert.That((int)Field(excluded,"allowedSamples"),Is.Zero);Assert.That((int)Field(excluded,"expectedFinalPhase"),Is.EqualTo(-1));
        var inset=Measure(new[]{new Vector3(1435,0,1064.0625f)},new[]{Vector3.zero},new[]{false},0);Assert.That((int)Field(inset,"allowedSamples"),Is.Zero);Assert.That((int)Field(inset,"expectedFinalPhase"),Is.Zero);
    }
}
#endif
