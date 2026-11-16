#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;

public sealed class DragonKingdomRoundedCavernVendorInputTests
{
    [Test,Timeout(90000)]
    public void RecordedOriginalActionsAndActualWorldSkinProduceBoundedRoundedColumnsWithoutExcavatingOrClaimingFullFlightAcceptance()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);
        var evidence=Activator.CreateInstance(type.GetNestedType("Evidence"));
        var columns=(Array)type.GetMethod("PrepareRoundedProfile",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{evidence});
        var proof=evidence.GetType().GetField("roundedProfile").GetValue(evidence);
        object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
        Assert.That(columns.Length,Is.GreaterThan(11776));
        Assert.That((int)Field("sourceActions"),Is.EqualTo(31));Assert.That((int)Field("liveFrames"),Is.EqualTo(2913));Assert.That((int)Field("liveFlightFrames"),Is.EqualTo(2622));
        Assert.That((bool)Field("partialVisitedLiveEvidence"),Is.True);Assert.That((bool)Field("nativeRequiredColumnsPassed"),Is.False,"No Digger modification or native floor/roof contact occurred in this input qualification.");
    }
    [Test,Timeout(90000)]
    public void ActualInstalledCubicGridVoxelizerPreservesClosedInteriorAndExteriorSignsBeforeFreshExcavation()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);
        var proof=type.GetMethod("ProbeInstalledRoundedCubicInputVoxelizer").Invoke(null,null);
        object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
        Assert.That((bool)Field("finite"),Is.True);Assert.That((int)Field("vertices"),Is.EqualTo(834));Assert.That((int)Field("triangles"),Is.EqualTo(1664));
        Assert.That((float)Field("center"),Is.GreaterThan(0));foreach(string name in new[]{"aboveApex","belowFloor","outsideWall"})Assert.That((float)Field(name),Is.LessThan(0),name);
    }
    [Test,Timeout(90000)]
    public void ActualInstalledVoxelizerRecognizesRoundedInteriorOutsideFloorWallAndApexWithoutExcavation()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);
        var proof=type.GetMethod("ProbeInstalledRoundedInputVoxelizer").Invoke(null,null);
        object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
        Assert.That((bool)Field("finite"),Is.True);
        Assert.That((int)Field("vertices"),Is.EqualTo(834));
        Assert.That((int)Field("triangles"),Is.EqualTo(1664));
        Assert.That((int)Field("voxels"),Is.GreaterThan(10000));
        Assert.That((float)Field("center"),Is.GreaterThan(0),"Actual Digger custom Dig input is positive inside, matching its sphere radius-minus-distance convention.");
        foreach(string name in new[]{"aboveApex","belowFloor","outsideWall"})Assert.That((float)Field(name),Is.LessThan(0),name);
        TestContext.WriteLine("Actual installed Digger MeshToVoxelsJob: finite closed rounded input with correct interior/exterior signs. No terrain modification, persistence, roof, floor or live-flight acceptance is inferred.");
    }
    [TestCase(1.5625f,.5f,1.5625f,2.2097087f)][TestCase(.78125f,.78125f,.78125f,1.10485435f)][TestCase(1f,.5f,2f,2.236068f)]
    public void MeasuredNativeGridMarginCoversRotatedHorizontalRasterizationWithoutChangingRequiredRoadWidth(float x,float y,float z,float expected)
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);var grids=new[]{new UnityEngine.Vector3(x,y,z),new UnityEngine.Vector3(x*.5f,y*2,z*.5f)};
        float band=(float)type.GetMethod("MeasuredHorizontalVoxelGuardBand").Invoke(null,new object[]{grids});Assert.That(band,Is.EqualTo(expected).Within(.000001f));
        float originalRequiredWidth=18+14;Assert.That(originalRequiredWidth+2*band,Is.GreaterThan(originalRequiredWidth));Assert.That(grids[0],Is.EqualTo(new UnityEngine.Vector3(x,y,z)),"Do not rewrite the vendor's actual grid");
    }
    [TestCase("null")][TestCase("missing")][TestCase("zero")][TestCase("nan")][TestCase("overflow")]
    public void MissingOrInvalidNativeTileGridsCannotAuthorizeAClearanceBand(string invalid)
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);var grids=new[]{UnityEngine.Vector3.one,UnityEngine.Vector3.one};
        switch(invalid){case "null":grids=null;break;case "missing":grids=new[]{UnityEngine.Vector3.one};break;case "zero":grids[0]=UnityEngine.Vector3.zero;break;case "nan":grids[0]=new UnityEngine.Vector3(float.NaN,1,1);break;case "overflow":grids[0]=new UnityEngine.Vector3(float.MaxValue,1,float.MaxValue);break;}
        var error=Assert.Throws<TargetInvocationException>(()=>type.GetMethod("MeasuredHorizontalVoxelGuardBand").Invoke(null,new object[]{grids}));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());
    }
    [Test]
    public void OmittedNativeRasterPolicyPreservesOriginalNominalCorridorRecipe()
    {
        Assert.That(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernRasterClearance"),Is.EqualTo(-1),"Run this original-recipe regression without the opt-in, separate from a fresh changed-geometry excavation");
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);Assert.That((bool)type.GetMethod("MeasuredRasterClearanceRequested",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null),Is.False);
    }

}
#endif
