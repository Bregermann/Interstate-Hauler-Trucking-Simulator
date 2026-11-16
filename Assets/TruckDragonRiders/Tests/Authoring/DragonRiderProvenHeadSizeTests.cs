#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderProvenHeadSizeTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public Profile[] profiles;}
    [Serializable]sealed class Profile{public string species,profile,status,failure;public bool provenFireHeadLeverRecipe,strictMaterialPatch,sourceMotionUnchanged,hasFiniteUniformSize,exactOriginalBoxAcceptance,originalFootprintFits;public float derivedNeckPitch,derivedHeadPitch,roundedCandidateFactor,actualCandidateSkinScaleError,maximumActualWheelGap;public int actualPoses,invalidAnatomicalPoses,unsafeExpandedPlanningMisses,candidateSatPairs,candidateWheelContactPoses;public float[] strictTorsoWeights;}
#pragma warning restore CS0649
    [Test,Timeout(1800000)]
    public void AnalyticallyDerivedFireNeck60HeadMinus15RetainsEvery537OriginalActionLayerAndWholeNwhGeometryGate()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsRiderHeadExitEvidence");Assert.That(at>=0&&at+1<args.Length,Is.True);var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)author.GetMethod("InspectPreciseProvenFireHeadLeverMountedSizeWithoutSavingAssets").Invoke(null,new object[]{args[at+1]}));Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);Assert.That(report.profiles.Length,Is.EqualTo(2));
        foreach(var row in report.profiles)
        {
            Assert.That(row.species,Is.EqualTo("dragon-fire"));Assert.That(row.provenFireHeadLeverRecipe&&row.sourceMotionUnchanged,Is.True);Assert.That(row.derivedNeckPitch,Is.EqualTo(60));Assert.That(row.derivedHeadPitch,Is.EqualTo(-15));Assert.That(row.failure,Is.Null.Or.Empty);Assert.That(row.actualPoses,Is.EqualTo(537));Assert.That(row.invalidAnatomicalPoses,Is.Zero);Assert.That(row.unsafeExpandedPlanningMisses,Is.Zero);if(row.strictMaterialPatch)Assert.That(row.strictTorsoWeights.Length==5&&row.strictTorsoWeights.All(weight=>weight>.5f),Is.True);
            if(row.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED")){Assert.That(row.hasFiniteUniformSize&&row.exactOriginalBoxAcceptance&&row.originalFootprintFits,Is.True);Assert.That(row.candidateSatPairs,Is.Zero);Assert.That(row.candidateWheelContactPoses,Is.EqualTo(537));Assert.That(row.actualCandidateSkinScaleError,Is.LessThan(.001f));Assert.That(row.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));}
            TestContext.WriteLine("DERIVED_SINGLE_FIRE_HEAD_LEVER profile="+row.profile+" status="+row.status+" factor="+row.roundedCandidateFactor+" nativePoses="+row.actualPoses+" NO_LIVE_CAB_OR_RELEASE_ACCEPTANCE");
        }
        Assert.That(report.profiles.Single(row=>row.strictMaterialPatch).status,Does.StartWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED"),"The exact strict-torso candidate must pass every original source/native geometry gate before any runtime cab installation.");
    }
}
#endif
