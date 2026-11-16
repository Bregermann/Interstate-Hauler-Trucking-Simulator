#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderMountedSizeNativeTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report
    {public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public Profile[] profiles;}
    [Serializable]sealed class Profile
    {
        public string species,profile,status,failure;
        public bool sourceMotionUnchanged,hasFiniteUniformSize,exactOriginalBoxAcceptance,originalFootprintFits;
        public float roundedCandidateFactor,maximumActualWheelGap,actualCandidateSkinScaleError;
        public int actualPoses,invalidAnatomicalPoses,intervalUnitControls,candidateSatPairs;
    }
#pragma warning restore CS0649
    [Test,Timeout(1800000)]
    public void V279ConnectedNormalFlightUsesOriginalCenteredMaterialSupportAndRetainsFullSourceActionSizeGates()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        var report=JsonUtility.FromJson<Report>((string)author.GetMethod("InspectOriginalCenteredConnectedMountedSizeWithoutSavingAssets").Invoke(null,null));
        Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);
        Assert.That(report.profiles,Has.Length.EqualTo(1));var profile=report.profiles.Single();
        Assert.That(profile.species,Is.EqualTo("dragon-inferno"));Assert.That(profile.profile,Does.StartWith("V279_CONNECTED_NORMAL_FLIGHT_ORIGINAL_CENTERED"));
        Assert.That(profile.failure,Is.Null.Or.Empty);Assert.That(profile.sourceMotionUnchanged,Is.True);
        Assert.That(profile.actualPoses+profile.invalidAnatomicalPoses,Is.GreaterThan(650));Assert.That(profile.intervalUnitControls,Is.GreaterThan(100000));
        if(profile.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED"))
        {
            Assert.That(profile.hasFiniteUniformSize&&profile.exactOriginalBoxAcceptance&&profile.originalFootprintFits,Is.True);
            Assert.That(profile.candidateSatPairs,Is.Zero);Assert.That(profile.actualCandidateSkinScaleError,Is.LessThan(.001f));Assert.That(profile.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));
        }
        else Assert.That(profile.status,Does.StartWith("REJECTED_"));
        TestContext.WriteLine("ACTUAL_V279_CONNECTED_ORIGINAL_CENTERED_FULL_ACTION_SIZE_DIAGNOSIS status="+profile.status+" factor="+profile.roundedCandidateFactor+" invalidAnatomy="+profile.invalidAnatomicalPoses+" NO_NEW_SUPPORT_OR_CURVE_RECIPE_NO_PRODUCTION_OR_RUNTIME_ACCEPTANCE");
    }
    [Test,Timeout(1800000)]
    public void OriginalMappedAndSavedMountedMotionsDeriveOrRejectExactUniformSizeIntervalsWithoutChangingSources()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        var report=JsonUtility.FromJson<Report>((string)author.GetMethod("InspectOriginalMotionMountedSizeWithoutSavingAssets").Invoke(null,null));
        Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);
        Assert.That(report.profiles,Has.Length.EqualTo(4));
        foreach(string species in new[]{"dragon-inferno","dragon-fire"})
        {
            var profiles=report.profiles.Where(profile=>profile.species==species).ToArray();Assert.That(profiles,Has.Length.EqualTo(2));
            Assert.That(profiles.Any(profile=>profile.profile.StartsWith("ORIGINAL_NATIVE_GRAPH_KEYS")),Is.True);
            Assert.That(profiles.Any(profile=>profile.profile.StartsWith("EXISTING_SAVED_MOUNTED")),Is.True);
            foreach(var profile in profiles)
            {
                Assert.That(profile.failure,Is.Null.Or.Empty,"An exception is not a successful sizing diagnosis.");
                Assert.That(profile.sourceMotionUnchanged,Is.True);Assert.That(profile.actualPoses+profile.invalidAnatomicalPoses,Is.GreaterThan(400));
                Assert.That(profile.intervalUnitControls,Is.GreaterThan(100000));
                Assert.That(profile.status,Is.Not.EqualTo("IN_PROGRESS"));
                if(profile.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED"))
                {
                    Assert.That(profile.hasFiniteUniformSize&&profile.exactOriginalBoxAcceptance&&profile.originalFootprintFits,Is.True);
                    Assert.That(profile.roundedCandidateFactor,Is.GreaterThanOrEqualTo(1));Assert.That(profile.candidateSatPairs,Is.Zero);
                    Assert.That(profile.actualCandidateSkinScaleError,Is.LessThan(.001f));Assert.That(profile.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));
                }
                else Assert.That(profile.status,Does.StartWith("REJECTED_"));
                TestContext.WriteLine("ACTUAL_ORIGINAL_MOTION_SIZE_DIAGNOSIS species="+profile.species+" profile="+profile.profile+" status="+profile.status+" factor="+profile.roundedCandidateFactor+" invalidAnatomy="+profile.invalidAnatomicalPoses+" actualUnchangedOriginalNwhSat="+profile.candidateSatPairs+" NO_SOURCE_CURVE_CASCADE_NO_SAVED_VARIANT_NO_RUNTIME_OR_RELEASE_ACCEPTANCE");
            }
        }
    }
}
#endif
