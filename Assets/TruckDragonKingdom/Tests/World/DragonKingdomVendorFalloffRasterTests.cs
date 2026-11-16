#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomVendorFalloffRasterTests
{
    static Bounds Cover(Bounds raw,Vector3 origin,Vector3 spacing)
        =>(Bounds)Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomVendorFalloffAuthoring, Assembly-CSharp-Editor",true).GetMethod("CoverTerrainGridCells").Invoke(null,new object[]{raw,origin,spacing});
    static bool Inside(Bounds b,float x,float z)=>x>=b.min.x&&x<=b.max.x&&z>=b.min.z&&z<=b.max.z;
    [Test]
    public void ActualV343BoundaryRowsAreCoveredExactlyWithoutAnExtraRasterRow()
    {
        var raw=new Bounds(new Vector3(1188.802734375f,331.9701538f,1200),new Vector3(589.87744140625f,273.9742432f,262));var b=Cover(raw,new Vector3(0,-20,800),new Vector3(3.125f,0,3.125f));
        Assert.That(b.min.x,Is.EqualTo(893.75f));Assert.That(b.max.x,Is.EqualTo(1484.375f));Assert.That(b.min.z,Is.EqualTo(1068.75f));Assert.That(b.max.z,Is.EqualTo(1331.25f));
        Assert.That(Inside(raw,1043.75f,1068.75f),Is.False);Assert.That(Inside(b,1043.75f,1068.75f),Is.True);Assert.That(Inside(b,1006.25f,1331.25f),Is.True);
        Assert.That(Inside(b,1180,1065.625f),Is.False);Assert.That(Inside(b,1180,1334.375f),Is.False);
    }
    [Test]
    public void AlignedCoverageAddsNoCellAndNegativeCoordinatesUseTheActualTerrainOrigin()
    {
        var raw=new Bounds(new Vector3(-1100,20,-400),new Vector3(100,40,100));var b=Cover(raw,new Vector3(-1600,-20,-800),new Vector3(3.125f,0,3.125f));Assert.That(b,Is.EqualTo(raw));
        var unaligned=new Bounds(new Vector3(-1100.1f,20,-400.1f),new Vector3(100,40,100));var c=Cover(unaligned,new Vector3(-1600,-20,-800),new Vector3(3.125f,0,3.125f));
        Assert.That(c.min.x,Is.LessThanOrEqualTo(unaligned.min.x));Assert.That(c.max.x,Is.GreaterThanOrEqualTo(unaligned.max.x));Assert.That(unaligned.min.x-c.min.x,Is.LessThan(3.125f));Assert.That(c.max.x-unaligned.max.x,Is.LessThan(3.125f));Assert.That(c.min.z,Is.LessThanOrEqualTo(unaligned.min.z));Assert.That(c.max.z,Is.GreaterThanOrEqualTo(unaligned.max.z));
    }
    [Test]
    public void RecordedQualityMidpointProducesSixteenActualVerticesAndNeverTreatsOldInterpolatedFocusAsAGridVertex()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomVendorFalloffAuthoring, Assembly-CSharp-Editor",true);var points=(Vector3[])type.GetMethod("ActualNeVertexStations").Invoke(null,new object[]{new Vector3(0,-20,800),new Vector3(1600,600,1600),513});Assert.That(points.Length,Is.EqualTo(16));Assert.That(points.Distinct().Count(),Is.EqualTo(16));
        Assert.That(points.Select(p=>p.x).Distinct().OrderBy(v=>v),Is.EqualTo(new[]{1250f,1253.125f,1256.25f,1259.375f}));Assert.That(points.Select(p=>p.z).Distinct().OrderBy(v=>v),Is.EqualTo(new[]{1162.5f,1165.625f,1168.75f,1171.875f}));
        foreach(var point in points){Assert.That(point.x/3.125f,Is.EqualTo(Mathf.Round(point.x/3.125f)));Assert.That((point.z-800)/3.125f,Is.EqualTo(Mathf.Round((point.z-800)/3.125f)));}
        Assert.That(points.Any(p=>p.x==1255),Is.False,"Old interpolated focus must not be labeled a real vertex.");var reference=(Vector3)type.GetField("NeFailureCellReference").GetValue(null);Assert.That(reference,Is.EqualTo(new Vector3(1254.6875f,0,1167.1875f)));
    }
    [Serializable]sealed class Receipt{public Operation[] results;}
    [Serializable]sealed class Operation{public Bounds declaredToolFootprint;public Field[] fields;}
    [Serializable]sealed class Field{public int resolution,changedSamples,outsideDeclaredFootprintChanges;public Vector3 terrainOrigin,terrainSize;public string failedBeforeFloat32,failedAfterFloat32,beforeHeightSha256,afterHeightSha256;}
    static string Sha(byte[] bytes){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    [Test]
    public void RetainedActualV343Float32GridsProveAll169OutsideSamplesBelongToOnlyTheTwoCoveredBoundaryRows()
    {
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonSdkOutsideEvidence");if(at<0||at+1>=args.Length)Assert.Ignore("Optional private native evidence requires explicit -tsDragonSdkOutsideEvidence; pure geometry controls remain portable.");
        var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(args[at+1]));var operation=receipt.results[0];var f=operation.fields.Single(v=>v.outsideDeclaredFootprintChanges==169);var beforeBytes=File.ReadAllBytes(f.failedBeforeFloat32);var afterBytes=File.ReadAllBytes(f.failedAfterFloat32);Assert.That(Sha(beforeBytes),Is.EqualTo(f.beforeHeightSha256));Assert.That(Sha(afterBytes),Is.EqualTo(f.afterHeightSha256));int n=f.resolution;Assert.That(beforeBytes.Length,Is.EqualTo(n*n*4));Assert.That(afterBytes.Length,Is.EqualTo(beforeBytes.Length));var before=new float[n*n];var after=new float[n*n];Buffer.BlockCopy(beforeBytes,0,before,0,beforeBytes.Length);Buffer.BlockCopy(afterBytes,0,after,0,afterBytes.Length);
        var grid=Cover(operation.declaredToolFootprint,f.terrainOrigin,new Vector3(f.terrainSize.x/(n-1),0,f.terrainSize.z/(n-1)));int changed=0,outsideContinuous=0,lowerRow=0,upperRow=0;
        for(int i=0;i<before.Length;i++)if(before[i]!=after[i])
        {
            changed++;float x=f.terrainOrigin.x+i%n*f.terrainSize.x/(n-1),z=f.terrainOrigin.z+i/n*f.terrainSize.z/(n-1);Assert.That(Inside(grid,x,z),Is.True,"No actual changed sample may escape precise raster coverage.");
            if(!Inside(operation.declaredToolFootprint,x,z)){outsideContinuous++;if(z==1068.75f)lowerRow++;else if(z==1331.25f)upperRow++;else Assert.Fail("Unexpected broader SDK change outside measured boundary rows.");}
        }
        Assert.That(changed,Is.EqualTo(6513));Assert.That(outsideContinuous,Is.EqualTo(169));Assert.That(lowerRow,Is.EqualTo(81));Assert.That(upperRow,Is.EqualTo(88));Assert.That(Sha(File.ReadAllBytes(f.failedBeforeFloat32)),Is.EqualTo(f.beforeHeightSha256));Assert.That(Sha(File.ReadAllBytes(f.failedAfterFloat32)),Is.EqualTo(f.afterHeightSha256));
    }
}
#endif
