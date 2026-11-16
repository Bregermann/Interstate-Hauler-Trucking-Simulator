#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderFireCabPlanningTests
{
    [Serializable] sealed class Report
    {
        public string scope,failure,clearanceUnits,boxAuthority;
        public bool sourcesExact,fixtureAssetsExact,originalSceneExact,baselineRaysExact,baselineAnchorsExact,originalNwhDependencyBytesExact;
        public int originalBoxes,fullSkinVertices,fullSkinTriangles;public float originalDorsalClearance;public Vector3 saddleUp;public Row[] rows;public Box[] boxes;
    }
    [Serializable] sealed class Row
    {
        public string cameraBasis;public float extraHeadDegrees,extraPostWorldMeters,proposedDorsalClearance,maximumAnchorDisplacement,muzzleWorldAimChange;
        public Vector3 wholeTruckDeckCabTranslation;public int headHits,centerTriangle,fullSkinBoxPairs;public bool centerHead,meetsRayPlanningThreshold,sourceSkinUnchanged,fiveAnchorsInvariant;public Vector3[] candidateAnchors;public Ray[] rays;
    }
    [Serializable] sealed class Box { public float physicsToNearestRecordedTransformPositionDelta,physicsToNearestRecordedTransformQuaternionComponentDelta; }
    [Serializable] sealed class Ray { public Vector2 viewport;public bool head;public int triangle;public float distance; }
    [Test,Timeout(180000)]
    public void ActualFireFrameComparesWholeSupportedPostHeightsAndHeadFlexionWithoutChangingSourceOrFiveSkinAnchors()
    {
        var args=Environment.GetCommandLineArgs();string Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,key);return args[at+1];}
        var authoring=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        string json=(string)authoring.GetMethod("InspectFireCabHeadAndSupportedPostPlanning").Invoke(null,new object[]{Required("-tsRiderFireCabPlanningFrame"),Required("-tsRiderFireCabPlanningProducer")});var proof=JsonUtility.FromJson<Report>(json);
        Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.sourcesExact&&proof.fixtureAssetsExact&&proof.originalSceneExact&&proof.baselineRaysExact&&proof.baselineAnchorsExact&&proof.originalNwhDependencyBytesExact,Is.True);
        Assert.That(proof.scope,Does.Contain("NOT_RENDERED_BOX"));Assert.That(proof.clearanceUnits,Does.Contain("WORLD_METERS"));Assert.That(proof.boxAuthority,Does.Contain("PHYSICS_BODY_POSES"));Assert.That(proof.originalBoxes,Is.EqualTo(2));Assert.That(proof.fullSkinVertices,Is.EqualTo(2064));Assert.That(proof.fullSkinTriangles,Is.EqualTo(2698));Assert.That(proof.originalDorsalClearance,Is.EqualTo(.45f));Assert.That(proof.saddleUp.magnitude,Is.EqualTo(1).Within(.00001f));
        Assert.That(proof.rows.Length,Is.EqualTo(18));Assert.That(proof.boxes.Length,Is.EqualTo(2));foreach(var box in proof.boxes){Assert.That(box.physicsToNearestRecordedTransformPositionDelta,Is.LessThan(.001f));Assert.That(box.physicsToNearestRecordedTransformQuaternionComponentDelta,Is.LessThan(.00001f));}
        foreach(var group in proof.rows.GroupBy(row=>row.cameraBasis))
        {
            Assert.That(group.Count(),Is.EqualTo(9));Assert.That(group.Count(row=>row.extraHeadDegrees>0),Is.EqualTo(2));CollectionAssert.AreEqual(new[]{.5f,1f,2f,3f,4f,5f},group.Where(row=>row.extraPostWorldMeters>0).Select(row=>row.extraPostWorldMeters));
            var control=group.Single(row=>row.extraHeadDegrees==0&&row.extraPostWorldMeters==0);Assert.That(control.headHits,Is.GreaterThan(5),"Retain the actual failing baseline, never relabel it as accepted.");
            foreach(var row in group){Assert.That(row.rays.Length,Is.EqualTo(25));Assert.That(row.candidateAnchors.Length,Is.EqualTo(5));Assert.That(row.fiveAnchorsInvariant,Is.EqualTo(row.maximumAnchorDisplacement<.0001f));Assert.That(row.headHits,Is.EqualTo(row.rays.Count(ray=>ray.head)));Assert.That(row.meetsRayPlanningThreshold,Is.EqualTo(!row.centerHead&&row.headHits<=5));Assert.That(Vector3.Distance(row.wholeTruckDeckCabTranslation,proof.saddleUp*row.extraPostWorldMeters),Is.LessThan(.00001f));Assert.That(row.proposedDorsalClearance,Is.EqualTo(.45f+row.extraPostWorldMeters));Assert.That(row.fullSkinBoxPairs,Is.GreaterThanOrEqualTo(0));if(row.extraHeadDegrees==0){Assert.That(row.fiveAnchorsInvariant,Is.True);Assert.That(row.sourceSkinUnchanged,Is.True);Assert.That(row.muzzleWorldAimChange,Is.Zero);}else{Assert.That(row.sourceSkinUnchanged,Is.False);Assert.That(row.muzzleWorldAimChange,Is.GreaterThan(0));}TestContext.WriteLine(row.cameraBasis+" headExtra="+row.extraHeadDegrees+" realPostExtraWorldMeters="+row.extraPostWorldMeters+" headHits="+row.headHits+" center="+row.centerHead+" physicsSnapshotSat="+row.fullSkinBoxPairs+" anchorsInvariant="+row.fiveAnchorsInvariant+" muzzleAimChange="+row.muzzleWorldAimChange+" PLANNING_ONLY_NOT_FULL537_REAL_POST_CONTACT_RUNTIME_ACCEPTANCE");}
        }
    }
}
#endif
