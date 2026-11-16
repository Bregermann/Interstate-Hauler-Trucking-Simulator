#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderPreciseSizePlanningTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Controls
    {
        public bool knownInitialInterval,knownLaterInterval,genuineUnboundedInterval,allTransformedAnchoredPlanesSeparated,nearCoplanarSourceWitnessSeparated;
        public int meaningfulWorldTransforms,independentNativeSatComparisons;public float sourceWitnessActualToPreciseAnchorDistance;
    }
    [Serializable]sealed class Report{public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public Profile[] profiles;}
    [Serializable]sealed class Profile
    {
        public string species,profile,status,failure;public bool sourceMotionUnchanged,precisePlanning,hasFiniteUniformSize,exactOriginalBoxAcceptance,originalFootprintFits;
        public int actualPoses,invalidAnatomicalPoses,candidateSatPairs,candidateWheelContactPoses,intervalUnitControls;
        public float roundedCandidateFactor,maximumPlanningAnchorDistance,maximumActualWheelGap,actualCandidateSkinScaleError;
    }
#pragma warning restore CS0649
    [Test]
    public void DoublePrecisionPlanningRetainsRealIntervalsAndRejectsCancellationAtAuthoredAnchoredFaces()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        string json=(string)author.GetMethod("InspectPreciseMountedSizePlanningControls").Invoke(null,null);var proof=JsonUtility.FromJson<Controls>(json);TestContext.WriteLine(json);
        Assert.That(proof.knownInitialInterval&&proof.knownLaterInterval&&proof.genuineUnboundedInterval&&proof.allTransformedAnchoredPlanesSeparated&&proof.nearCoplanarSourceWitnessSeparated,Is.True);
        Assert.That(proof.meaningfulWorldTransforms,Is.EqualTo(12));Assert.That(proof.independentNativeSatComparisons,Is.EqualTo(96));Assert.That(proof.sourceWitnessActualToPreciseAnchorDistance,Is.LessThan(.001f));
    }
    [Test,Timeout(1800000)]
    public void PreciseAuthoredMaterialPlanningRunsCompleteOriginalAndV279ActionProfilesWithoutChangingNativeAcceptance()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        foreach(string method in new[]{"InspectPreciseOriginalMotionMountedSizeWithoutSavingAssets","InspectPreciseOriginalCenteredConnectedMountedSizeWithoutSavingAssets"})
        {
            var report=JsonUtility.FromJson<Report>((string)author.GetMethod(method).Invoke(null,null));Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);Assert.That(report.profiles.Length,Is.EqualTo(method.Contains("CenteredConnected")?1:4));
            foreach(var profile in report.profiles)
            {
                Assert.That(profile.failure,Is.Null.Or.Empty);Assert.That(profile.precisePlanning&&profile.sourceMotionUnchanged,Is.True);Assert.That(profile.actualPoses+profile.invalidAnatomicalPoses,Is.GreaterThan(500));Assert.That(profile.intervalUnitControls,Is.GreaterThan(100000));Assert.That(profile.maximumPlanningAnchorDistance,Is.LessThan(.001f));
                if(profile.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED"))
                {
                    Assert.That(profile.hasFiniteUniformSize&&profile.exactOriginalBoxAcceptance&&profile.originalFootprintFits,Is.True);Assert.That(profile.candidateSatPairs,Is.Zero);Assert.That(profile.candidateWheelContactPoses,Is.EqualTo(profile.actualPoses));Assert.That(profile.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));Assert.That(profile.actualCandidateSkinScaleError,Is.LessThan(.001f));
                }
                else Assert.That(profile.status,Does.StartWith("REJECTED_"));
                TestContext.WriteLine("PRECISE_MATERIAL_PLANNING_UNCHANGED_NATIVE_ACCEPTANCE species="+profile.species+" profile="+profile.profile+" status="+profile.status+" factor="+profile.roundedCandidateFactor+" actualPoses="+profile.actualPoses+" invalidAnatomy="+profile.invalidAnatomicalPoses+" actualOriginalBoxSAT="+profile.candidateSatPairs+" maximumMaterialAnchorDifference="+profile.maximumPlanningAnchorDistance+" NOT_RUNTIME_OR_RELEASE_ACCEPTANCE");
            }
        }
    }
}
#endif
