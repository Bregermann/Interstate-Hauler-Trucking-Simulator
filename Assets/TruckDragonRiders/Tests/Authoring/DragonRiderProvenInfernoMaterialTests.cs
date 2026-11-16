#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderProvenInfernoMaterialTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public Profile[] profiles;}
    [Serializable]sealed class Profile{public string species,profile,status,failure;public bool nativeDerivedInfernoMaterialPatch,actualConfiguredStartupTrial,sourceMotionUnchanged,hasFiniteUniformSize,exactOriginalBoxAcceptance,originalFootprintFits;public int actualPoses,invalidAnatomicalPoses,unsafeExpandedPlanningMisses,candidateSatPairs,candidateWheelContactPoses;public float nativeMaterialRearward,roundedCandidateFactor,actualCandidateSkinScaleError,maximumActualWheelGap;public float[] strictTorsoWeights;}
#pragma warning restore CS0649
    [Test,Timeout(1800000)]
    public void NativeDerivedStrictTorsoMaterialCenterRetainsAll699OriginalPoseAndWholeNwhGeometryGates()
    {
        var args=Environment.GetCommandLineArgs();int fit=Array.IndexOf(args,"-tsRiderInfernoMaterialEvidence"),startup=Array.IndexOf(args,"-tsRiderStartupEvidence");Assert.That(fit>=0&&fit+1<args.Length&&startup>=0&&startup+1<args.Length,Is.True,"Actual passed source-derived material and public-start receipts required.");var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)author.GetMethod("InspectPreciseInfernoNativeMaterialMountedSizeWithoutSavingAssets").Invoke(null,new object[]{args[fit+1],args[startup+1]}));Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);Assert.That(report.profiles.Length,Is.EqualTo(2));
        foreach(var row in report.profiles)
        {
            Assert.That(row.species,Is.EqualTo("dragon-inferno"));Assert.That(row.failure,Is.Null.Or.Empty);Assert.That(row.nativeDerivedInfernoMaterialPatch&&row.actualConfiguredStartupTrial&&row.sourceMotionUnchanged,Is.True);Assert.That(row.nativeMaterialRearward,Is.EqualTo(-.1627420932f));Assert.That(row.strictTorsoWeights.Length,Is.EqualTo(5));foreach(float weight in row.strictTorsoWeights)Assert.That(weight,Is.GreaterThan(.5f));Assert.That(row.actualPoses+row.invalidAnatomicalPoses,Is.EqualTo(699));Assert.That(row.unsafeExpandedPlanningMisses,Is.Zero);
            if(row.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED")){Assert.That(row.actualPoses,Is.EqualTo(699));Assert.That(row.invalidAnatomicalPoses,Is.Zero);Assert.That(row.hasFiniteUniformSize&&row.exactOriginalBoxAcceptance&&row.originalFootprintFits,Is.True);Assert.That(row.candidateSatPairs,Is.Zero);Assert.That(row.candidateWheelContactPoses,Is.EqualTo(699));Assert.That(row.actualCandidateSkinScaleError,Is.LessThan(.001f));Assert.That(row.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));}
            TestContext.WriteLine("ACTUAL_NATIVE_DERIVED_INFERNO_STRICT_TORSO profile="+row.profile+" center="+row.nativeMaterialRearward+" status="+row.status+" valid="+row.actualPoses+" invalid="+row.invalidAnatomicalPoses+" derivedFactor="+row.roundedCandidateFactor+" NO_RUNTIME_CAB_CURRENT_COURSE_COLD_LOAD_OR_ADOPTION_ACCEPTANCE");
        }
    }
}
#endif
