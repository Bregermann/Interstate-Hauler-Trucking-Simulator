#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderSizeNumericEvidenceTests
{
#pragma warning disable CS0649
    [Serializable]sealed class NumericReport{public bool sourceBytesExact,originalPhysicsExact,mismatchReproduced;public string actualNativeLayerProof;public NumericRow[] rows;public Skin[] allOriginalSkins;}
    [Serializable]sealed class Skin{public string path,source,guid;public long fileId;public int vertices,triangles;public bool explicitMainTriangleTarget;}
    [Serializable]sealed class NumericRow{public int triangle,a,b,c;public Axis[] nativeFloatAxes,preciseDoubleAxes;}
    [Serializable]sealed class Axis{public string name,precision;}
    [Serializable]sealed class Report{public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public Profile[] profiles;}
    [Serializable]sealed class Profile{public string species,profile,status,failure;public bool precisePlanning,sourceMotionUnchanged,exactOriginalBoxAcceptance,originalFootprintFits;public int actualPoses,invalidAnatomicalPoses,candidateSatPairs;public float maximumPlanningAnchorDistance;}
#pragma warning restore CS0649
    static Type Author=>Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
    [Test,Timeout(120000)]
    public void OneActualRawInfernoLayerPoseReproducesTriangle178NumericDisagreementWithCompleteAxisWitnesses()
    {
        string json=(string)Author.GetMethod("InspectRawInfernoTriangle178NumericMismatchWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<NumericReport>(json);
        Assert.That(report.sourceBytesExact&&report.originalPhysicsExact,Is.True);Assert.That(report.actualNativeLayerProof,Does.Contain("REAL_UNCHANGED_TWO_LAYER_CONTROLLER"));Assert.That(report.rows.Length,Is.EqualTo(2));
        Assert.That(report.allOriginalSkins.Length,Is.GreaterThan(1));Assert.That(report.allOriginalSkins.Count(skin=>skin.explicitMainTriangleTarget),Is.EqualTo(1));Assert.That(report.allOriginalSkins.All(skin=>!string.IsNullOrEmpty(skin.path)&&skin.vertices>=0&&skin.triangles>=0),Is.True);Assert.That(report.allOriginalSkins.Single(skin=>skin.explicitMainTriangleTarget).triangles,Is.GreaterThan(178));
        foreach(var row in report.rows){Assert.That(row.triangle,Is.EqualTo(178));Assert.That(row.nativeFloatAxes.Length,Is.EqualTo(13));Assert.That(row.preciseDoubleAxes.Length,Is.EqualTo(13));}
        Assert.That(report.mismatchReproduced,Is.True,"A different pose is not a successful reproduction; retain the exact measured witness if native evaluation differs.");TestContext.WriteLine("ACTUAL_RAW178_SINGLE_POSE_NUMERIC_MISMATCH_REPRODUCED originalSourceAndNwhExact=true all13FloatAndDoubleAxesRetained=true NO_ORACLE_RELAXATION");
    }
    [Test,Timeout(1800000)]
    public void IsolatedV279ProfileKeepsFullActionGatesIndependentOfRejectedRawControls()
    {
        var report=JsonUtility.FromJson<Report>((string)Author.GetMethod("InspectPreciseOriginalCenteredConnectedMountedSizeWithoutSavingAssets").Invoke(null,null));Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);Assert.That(report.profiles.Length,Is.EqualTo(1));var profile=report.profiles.Single();Assert.That(profile.species,Is.EqualTo("dragon-inferno"));Assert.That(profile.precisePlanning&&profile.sourceMotionUnchanged,Is.True);Assert.That(profile.failure,Is.Null.Or.Empty);Assert.That(profile.actualPoses+profile.invalidAnatomicalPoses,Is.GreaterThan(650));Assert.That(profile.maximumPlanningAnchorDistance,Is.LessThan(.001f));
        if(profile.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED")){Assert.That(profile.exactOriginalBoxAcceptance&&profile.originalFootprintFits,Is.True);Assert.That(profile.candidateSatPairs,Is.Zero);}else Assert.That(profile.status,Does.StartWith("REJECTED_"));TestContext.WriteLine("ISOLATED_PRECISE_V279_FULL_ACTION_DIAGNOSIS status="+profile.status+" poses="+profile.actualPoses+" invalidAnatomy="+profile.invalidAnatomicalPoses+" NOT_PROFILE_OR_RUNTIME_ACCEPTANCE");
    }
}
#endif
