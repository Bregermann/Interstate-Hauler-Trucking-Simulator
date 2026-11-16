#if UNITY_EDITOR
using System;
using NUnit.Framework;
public sealed class DragonKingdomVistaOutputGridTests
{
    static Type Authoring=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    [Test]
    public void ActualAuthoredRelativeInputResolutionRounds1025To1032UnpaddedOutput()
    {
        var method=Authoring.GetMethod("InstalledErosionRecipeResolution");Assert.That((int)method.Invoke(null,new object[]{1024,1025}),Is.EqualTo(1032));Assert.That((int)method.Invoke(null,new object[]{1024,1024}),Is.EqualTo(1024));
    }
    [Test]
    public void ActualVertexGridTransferRetainsEndpointsAndCenterWithoutCroppingMargins()
    {
        const int source=1032,target=1025;var pixels=new float[source*source];for(int z=0;z<source;z++)for(int x=0;x<source;x++)pixels[z*source+x]=(float)x/(source-1)+2f*z/(source-1);var grid=(float[,])Authoring.GetMethod("ResampleVistaVertexGrid").Invoke(null,new object[]{pixels,source,source,target});Assert.That(grid[0,0],Is.Zero);Assert.That(grid[target-1,target-1],Is.EqualTo(3));Assert.That(grid[target/2,target/2],Is.EqualTo(1.5f).Within(.000001f));Assert.That(grid[0,target-1],Is.EqualTo(1));Assert.That(grid[target-1,0],Is.EqualTo(2));
    }
    [Test]
    public void GridTransferRejectsMissingActualPixels()
    {
        var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>Authoring.GetMethod("ResampleVistaVertexGrid").Invoke(null,new object[]{new float[8],3,3,3}));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());
    }
}
#endif
