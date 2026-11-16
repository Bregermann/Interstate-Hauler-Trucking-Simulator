#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderProvenStartupSizeTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public Profile[] profiles;}
    [Serializable]sealed class Profile
    {
        public string species,profile,status,failure,inactiveImportedStagingScope;public bool actualConfiguredStartupTrial,sourceMotionUnchanged,hasFiniteUniformSize,exactOriginalBoxAcceptance,originalFootprintFits;public int actualPoses,invalidAnatomicalPoses,unsafeExpandedPlanningMisses,candidateSatPairs,candidateWheelContactPoses,inactiveImportedStagingSatPairs;public float startupBonePositionError,startupBoneRotationError,roundedCandidateFactor,actualCandidateSkinScaleError,maximumActualWheelGap;
    }
#pragma warning restore CS0649
    [Test,Timeout(1800000)]
    public void ActualPassedPublicStartupSelectsRealInfernoDefaultWhileRetainingImportedStagingAndAll699NativePoseGates()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsRiderStartupEvidence");Assert.That(at>=0&&at+1<args.Length,Is.True,"Actual passed native source/root/NWH/rematch startup receipt required.");var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)author.GetMethod("InspectPreciseInfernoMountedSizeUsingProvenConfiguredStartupWithoutSavingAssets").Invoke(null,new object[]{args[at+1]}));
        Assert.That(report.intervalControlsPassed&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);Assert.That(report.profiles.Length,Is.EqualTo(2));
        foreach(var row in report.profiles)
        {
            Assert.That(row.species,Is.EqualTo("dragon-inferno"));Assert.That(row.failure,Is.Null.Or.Empty);Assert.That(row.actualConfiguredStartupTrial&&row.sourceMotionUnchanged,Is.True);Assert.That(row.startupBonePositionError,Is.LessThanOrEqualTo(.00001f));Assert.That(row.startupBoneRotationError,Is.LessThanOrEqualTo(.01f));Assert.That(row.inactiveImportedStagingScope,Does.StartWith("IMPORTED_SERIALIZED_SOURCE_FRAME_RETAINED"));Assert.That(row.actualPoses,Is.EqualTo(699));Assert.That(row.invalidAnatomicalPoses,Is.Zero);Assert.That(row.unsafeExpandedPlanningMisses,Is.Zero);
            if(row.status.StartsWith("DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED")){Assert.That(row.hasFiniteUniformSize&&row.exactOriginalBoxAcceptance&&row.originalFootprintFits,Is.True);Assert.That(row.candidateSatPairs,Is.Zero);Assert.That(row.candidateWheelContactPoses,Is.EqualTo(699));Assert.That(row.actualCandidateSkinScaleError,Is.LessThan(.001f));Assert.That(row.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));}
            TestContext.WriteLine("PROVEN_ACTUAL_PUBLIC_STARTUP_INFERNO profile="+row.profile+" activePoses="+row.actualPoses+" status="+row.status+" factor="+row.roundedCandidateFactor+" retainedInactiveImportedFrameContacts="+row.inactiveImportedStagingSatPairs+" NO_RUNTIME_CAB_OR_RELEASE_ACCEPTANCE");
        }
    }
}
#endif
