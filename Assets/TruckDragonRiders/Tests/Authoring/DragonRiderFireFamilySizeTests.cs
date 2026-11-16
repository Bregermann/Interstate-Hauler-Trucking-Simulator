#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderFireFamilySizeTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public Profile[] profiles;}
    [Serializable]sealed class Profile
    {
        public string species,profile,status,failure;public bool precisePlanning,strictMaterialPatch,sourceMotionUnchanged,hasFiniteUniformSize,exactOriginalBoxAcceptance,originalFootprintFits;
        public int actualPoses,invalidAnatomicalPoses,unsafeExpandedPlanningMisses,conservativeExpandedPlanningPairs,candidateSatPairs,candidateWheelContactPoses;
        public float roundedCandidateFactor,actualCandidateSkinScaleError,maximumActualWheelGap,maximumPlanningAnchorDistance;public float[] strictTorsoWeights;
    }
#pragma warning restore CS0649
    [Test,Timeout(1800000)]
    public void V279FireNormalFlightFamilyDerivesOwnedSizeThroughAllOriginalAndStrictTorsoActionGates()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)author.GetMethod("InspectPreciseFireNormalFamilyMountedSizeWithoutSavingAssets").Invoke(null,null));Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);Assert.That(report.profiles.Length,Is.EqualTo(2));
        foreach(var profile in report.profiles)
        {
            Assert.That(profile.species,Is.EqualTo("dragon-fire"));Assert.That(profile.failure,Is.Null.Or.Empty);Assert.That(profile.profile,Does.StartWith("V279_FIRE_NORMAL_FAMILY_HALF_FLAP_"));Assert.That(profile.precisePlanning&&profile.sourceMotionUnchanged,Is.True);Assert.That(profile.actualPoses,Is.EqualTo(537));Assert.That(profile.invalidAnatomicalPoses,Is.Zero);Assert.That(profile.unsafeExpandedPlanningMisses,Is.Zero);Assert.That(profile.maximumPlanningAnchorDistance,Is.LessThan(.001f));
            if(profile.strictMaterialPatch){Assert.That(profile.strictTorsoWeights.Length,Is.EqualTo(5));Assert.That(profile.strictTorsoWeights.All(weight=>weight>.5f),Is.True);}
            if(profile.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED")){Assert.That(profile.hasFiniteUniformSize&&profile.exactOriginalBoxAcceptance&&profile.originalFootprintFits,Is.True);Assert.That(profile.candidateSatPairs,Is.Zero);Assert.That(profile.candidateWheelContactPoses,Is.EqualTo(537));Assert.That(profile.actualCandidateSkinScaleError,Is.LessThan(.001f));Assert.That(profile.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));}
            TestContext.WriteLine("ACTUAL_V279_FIRE_FAMILY_SIZE profile="+profile.profile+" status="+profile.status+" factor="+profile.roundedCandidateFactor+" actualPoses="+profile.actualPoses+" actualNativeSAT="+profile.candidateSatPairs+" conservativeExpandedPlanningOnly="+profile.conservativeExpandedPlanningPairs+" NO_RUNTIME_CAB_OR_RELEASE_ACCEPTANCE");
        }
        Assert.That(report.profiles.Any(profile=>profile.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED")),Is.True,"No family/size profile may be installed before complete original source/action/layer, finite native box, skin, footprint and wheel gates pass.");
    }
}
#endif
