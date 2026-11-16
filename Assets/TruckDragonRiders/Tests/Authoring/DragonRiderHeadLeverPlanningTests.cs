#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderHeadLeverPlanningTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public bool finiteCandidate,sourceExact;public float totalPitch,analyticExtraDegrees,candidateExtraDegrees,candidateNeckDegrees,candidateHeadDegrees;public Row[] poses;}
    [Serializable]sealed class Row{public int owner,sourceVertices,sourceTriangles,constrainedHeadVertices,rigidHeadRayTriangles,mixedNeckTriangles,baselineOwnHeadHits,candidateOwnHeadHits;public bool baselineCenterHead,candidateCenterHead;public float baselineCenterDistanceError;}
#pragma warning restore CS0649
    [Test,Timeout(180000)]
    public void ActualQualifiedFireCabFramesDeriveBoundedConnectedHeadLeverWithoutMovingCameraOrAssets()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsRiderHeadLeverEvidence");Assert.That(at>=0&&at+1<args.Length,Is.True,"Exact preserved actual qualified Fire normal-family cab sightlines required.");
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)author.GetMethod("InspectFireHeadLeverRedistributionWithoutSavingAssets").Invoke(null,new object[]{args[at+1]}));
        Assert.That(report.sourceExact,Is.True);Assert.That(report.totalPitch,Is.EqualTo(45));Assert.That(report.poses.Length,Is.EqualTo(2));
        foreach(var row in report.poses){Assert.That(row.sourceVertices,Is.EqualTo(2064));Assert.That(row.sourceTriangles,Is.EqualTo(2698));Assert.That(row.baselineCenterHead,Is.True);Assert.That(row.baselineCenterDistanceError,Is.LessThan(.001f));Assert.That(row.rigidHeadRayTriangles,Is.GreaterThan(0));}
        if(report.finiteCandidate){Assert.That(report.candidateExtraDegrees,Is.InRange(0,45));Assert.That(report.candidateNeckDegrees+report.candidateHeadDegrees,Is.EqualTo(45));Assert.That(report.candidateNeckDegrees,Is.LessThanOrEqualTo(60));Assert.That(report.candidateHeadDegrees,Is.GreaterThanOrEqualTo(-15));Assert.That(report.poses.All(row=>!row.candidateCenterHead&&row.candidateOwnHeadHits<=5),Is.True,"An analytically feasible candidate must clear the actual preserved center rays and original head-grid ceiling before full native profile proof.");}
        TestContext.WriteLine("READ_ONLY_ACTUAL_FRAME_HEAD_LEVER finiteCandidate="+report.finiteCandidate+" analyticExtra="+report.analyticExtraDegrees+" roundedExtra="+report.candidateExtraDegrees+" neck="+report.candidateNeckDegrees+" head="+report.candidateHeadDegrees+" NO_ANIMATION_OR_FULL_PROFILE_OR_RUNTIME_ACCEPTANCE");
    }
}
#endif
