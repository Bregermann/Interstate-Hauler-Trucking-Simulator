#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderPointwiseTorsoTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public Profile[] profiles;}
    [Serializable]sealed class Profile{public string species,profile,status,failure;public bool nativeDerivedInfernoMaterialPatch,actualConfiguredStartupTrial,sourceMotionUnchanged,exactOriginalBoxAcceptance,originalFootprintFits;public int actualPoses,invalidAnatomicalPoses,unsafeExpandedPlanningMisses,candidateSatPairs,candidateWheelContactPoses;public float nativeMaterialRearward,roundedCandidateFactor,actualCandidateSkinScaleError,maximumActualWheelGap;public float[] strictTorsoWeights;}
#pragma warning restore CS0649
    [Test,Timeout(1800000)]
    public void ActualPointwiseFiveRayIntervalsAcrossAll12SourcePhasesDeriveOnePatchBeforeUnchanged699NativeGeometry()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsRiderStartupEvidence");Assert.That(at>=0&&at+1<args.Length,Is.True);var type=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)type.GetMethod("InspectPrecisePointwiseInfernoMaterialMountedSizeWithoutSavingAssets").Invoke(null,new object[]{args[at+1]}));Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);Assert.That(report.profiles.Length,Is.EqualTo(2));
        foreach(var row in report.profiles)
        {
            Assert.That(row.failure,Is.Null.Or.Empty);Assert.That(row.species,Is.EqualTo("dragon-inferno"));Assert.That(row.nativeDerivedInfernoMaterialPatch&&row.actualConfiguredStartupTrial&&row.sourceMotionUnchanged,Is.True);Assert.That(row.nativeMaterialRearward,Is.LessThan(0));Assert.That(row.strictTorsoWeights.Length,Is.EqualTo(5));foreach(float weight in row.strictTorsoWeights)Assert.That(weight,Is.GreaterThan(.5f));Assert.That(row.actualPoses+row.invalidAnatomicalPoses,Is.EqualTo(699));Assert.That(row.unsafeExpandedPlanningMisses,Is.Zero);
            if(row.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED")){Assert.That(row.actualPoses,Is.EqualTo(699));Assert.That(row.invalidAnatomicalPoses,Is.Zero);Assert.That(row.exactOriginalBoxAcceptance&&row.originalFootprintFits,Is.True);Assert.That(row.candidateSatPairs,Is.Zero);Assert.That(row.candidateWheelContactPoses,Is.EqualTo(699));Assert.That(row.actualCandidateSkinScaleError,Is.LessThan(.001f));Assert.That(row.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));}
            TestContext.WriteLine("ACTUAL_POINTWISE_ALL12_INFERNO_MATERIAL profile="+row.profile+" sourceDerivedCenter="+row.nativeMaterialRearward+" status="+row.status+" factor="+row.roundedCandidateFactor+" valid="+row.actualPoses+" invalid="+row.invalidAnatomicalPoses+" ORIGINAL_FIVE_RAYS_AND_POINT15_BASELINE_NO_POSTURE_HEIGHT_OR_SOURCE_CHANGE_NOT_RUNTIME_OR_COURSE_ACCEPTANCE");
        }
    }
}
#endif
