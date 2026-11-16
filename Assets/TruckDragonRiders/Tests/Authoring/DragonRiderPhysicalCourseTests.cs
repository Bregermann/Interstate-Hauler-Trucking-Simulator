#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderPhysicalCourseTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public string candidateScene,failure;public float clearRadius,uniformScale,sourceInnerRadius,physicalOuterRadius,physicalDepth,measuredRearReach,measuredAllAttitudeRadius,straightExitDistance;public bool sourceBytesExact,savedReloaded,orderedRouteExact,sourceArtExact,physicalControlsPassed,rulesControlsPassed;public Gate[] gates;}
    [Serializable]sealed class Gate{public int index,solidRimHits,clearInnerRays,clearOuterRays;public float actualInnerRadius,actualOuterRadius,actualDepth,maximumMatrixDifference;public bool originalMeshAndMaterial,renderColliderMatrixExact,sourcePosePreserved,centerClearBothDirections;}
#pragma warning restore CS0649
    [Test,Timeout(240000)]
    public void SavedMeasuredLargePhysicalRingsRetainSourceArtAndOrderedScoringWithNativeRimControls()
    {
        var args=Environment.GetCommandLineArgs();string Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,key);return args[at+1];}var type=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)type.GetMethod("CreateMeasuredPhysicalGateCourseTestCandidate").Invoke(null,new object[]{Required("-tsRiderCourseEnvelopeEvidence"),Required("-tsRiderPhysicalCourseOutputFolder")}));
        Assert.That(report.failure,Is.Null.Or.Empty);Assert.That(report.sourceBytesExact&&report.savedReloaded&&report.orderedRouteExact&&report.sourceArtExact&&report.physicalControlsPassed&&report.rulesControlsPassed,Is.True);Assert.That(File.Exists(report.candidateScene),Is.True);Assert.That(report.candidateScene,Does.StartWith("Assets/TruckDragonRiders/Tests/Generated/"));Assert.That(report.clearRadius,Is.EqualTo(60));Assert.That(report.uniformScale*report.sourceInnerRadius,Is.EqualTo(60).Within(.001f));Assert.That(report.measuredAllAttitudeRadius+5,Is.LessThanOrEqualTo(report.clearRadius));Assert.That(report.straightExitDistance,Is.EqualTo(55));Assert.That(report.straightExitDistance,Is.GreaterThan(report.measuredRearReach+report.physicalDepth*.5f+5));Assert.That(report.gates.Length,Is.EqualTo(12));
        foreach(var gate in report.gates){Assert.That(gate.originalMeshAndMaterial&&gate.renderColliderMatrixExact&&gate.sourcePosePreserved&&gate.centerClearBothDirections,Is.True,"gate"+gate.index);Assert.That(gate.actualInnerRadius,Is.EqualTo(60).Within(.001f));Assert.That(gate.actualOuterRadius,Is.EqualTo(report.physicalOuterRadius).Within(.001f));Assert.That(gate.actualDepth,Is.EqualTo(report.physicalDepth).Within(.001f));Assert.That(gate.maximumMatrixDifference,Is.LessThanOrEqualTo(.001f));Assert.That(gate.solidRimHits,Is.EqualTo(16));Assert.That(gate.clearInnerRays,Is.EqualTo(8));Assert.That(gate.clearOuterRays,Is.EqualTo(8));}
        TestContext.WriteLine("SAVED_CANDIDATE_FRESH_MEASURED60M_INNER_RING12 SAME_RENDER_COLLIDER_MATRICES ORIGINAL_ART_ORDER_PRESERVED NATIVE_CENTER_RIM_OUTER_RAYS_AND_ORDERED_RULES NO_CASTLE_START_WORLD_CLEARANCE_LIVE_AI_RACE_OR_WINDOWS_ACCEPTANCE");
    }
}
#endif
