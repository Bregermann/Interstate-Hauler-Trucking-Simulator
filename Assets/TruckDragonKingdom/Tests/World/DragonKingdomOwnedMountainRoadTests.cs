#if UNITY_EDITOR
using System;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public sealed class DragonKingdomOwnedMountainRoadTests
{
    static Type Author => Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomMountainRoadLayoutAuthoring, Assembly-CSharp-Editor", true);
    [Test]
    public void TwoQuarterInputsShareExactSemanticMidpointAndHaveNonparallelTangentGuards()
    {
        var quarters = (Vector3[][])Author.GetMethod("QuarterCircleGuides").Invoke(null, null);
        Assert.That(quarters.Length, Is.EqualTo(2));
        Assert.That(quarters[0][1], Is.EqualTo(new Vector3(1100, 190, 1320)));
        Assert.That(quarters[0][2], Is.EqualTo(new Vector3(1340, 215, 1080)));
        Assert.That(quarters[1][1], Is.EqualTo(quarters[0][2]));
        Assert.That(quarters[1][2], Is.EqualTo(new Vector3(1100, 240, 840)));
        foreach (var q in quarters)
        {
            var incoming = Vector3.ProjectOnPlane(q[1] - q[0], Vector3.up).normalized;
            var outgoing = Vector3.ProjectOnPlane(q[3] - q[2], Vector3.up).normalized;
            Assert.That(Vector3.Angle(incoming, outgoing), Is.EqualTo(90).Within(.0001f), "Avoid the exact180-degree parallel-tangent center singularity; native output remains separately required.");
        }
    }
    [Test]
    public void VendorSamplesRejectUnboundedNonfiniteAndRunawayCurvesBeforeStationSearch()
    {
        var validate = Author.GetMethod("ValidateVendorCurveSamples");
        Assert.DoesNotThrow(() => validate.Invoke(null, new object[] { new[] { Vector3.zero, Vector3.right, Vector3.right * 2 }, "bounded" }));
        foreach (var bad in new[] { new Vector3[16001], new[] { Vector3.zero, new Vector3(float.NaN, 0, 0), Vector3.right }, new[] { Vector3.zero, Vector3.right * 3000, Vector3.right * 6000 } })
        {
            var failure = Assert.Throws<TargetInvocationException>(() => validate.Invoke(null, new object[] { bad, "actual invalid SDK output" }));
            Assert.That(failure.InnerException, Is.TypeOf<InvalidOperationException>());
        }
    }
    [Test]
    public void VertexBudgetUsesActualTransverseShapeAndRejectsLongFinalRoadBeforeRefresh()
    {
        var estimate = Author.GetMethod("EstimateVendorVertexBudget");
        var points = new[] { Vector3.zero, Vector3.right * 1500 };
        int basic = (int)estimate.Invoke(null, new object[] { points, new[] { false, false }, 2, 0, .35f, 45f });
        Assert.That(basic, Is.InRange(1, 64999));
        var failure = Assert.Throws<TargetInvocationException>(() => estimate.Invoke(null, new object[] { points, new[] { false, false }, 16, 8, .35f, 45f }));
        Assert.That(failure.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(failure.InnerException.Message, Does.Contain("BEFORE Refresh"));
    }
    [Test, Timeout(240000)]
    public void ActualVendorMountainRoutePreservesCavePrefixAndBakesOneSavedPhysicalAuthority()
    {
        var type = Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor", true);
        type.GetMethod("CaptureOwnedMountainRoadLayoutFromCommandLine").Invoke(null, null);
        var proof = type.GetProperty("LastOwnedLayoutEvidence").GetValue(null);
        object Read(string name) => proof.GetType().GetField(name).GetValue(proof);
        Assert.That((string)Read("status"), Is.EqualTo("ACTUAL_VENDOR_ROUTE_MESH_AND_SAVED_AUTHORITY_PASSED_TERRAIN_AND_WORLD_PENDING"));
        foreach (string field in new[] { "originalSourcesExact", "incomingContextExact", "randomStateRestored", "saveReloadExact", "unrelatedSpecsExact", "originalCavePrefixExact" }) Assert.That((bool)Read(field), Is.True, field);
        Assert.That((int)Read("physicalPieces"), Is.GreaterThan(0)); Assert.That((int)Read("physicalQueries"), Is.GreaterThan(0)); Assert.That((int)Read("physicalMisses"), Is.Zero);
        Assert.That((float)Read("maximumPhysicalHeightError"), Is.LessThanOrEqualTo(.3f)); Assert.That((float)Read("maximumGrade"), Is.LessThanOrEqualTo(.18f));
        Assert.That((int)Read("actualVendorBuilds"), Is.Zero, "Physical route bake must not mutate terrain.");
        var layout = UnityEditor.AssetDatabase.LoadMainAssetAtPath((string)Read("layoutPath"));
        var quarters = (Array)layout.GetType().GetField("circularQuarters").GetValue(layout);
        Assert.That(quarters.Length, Is.EqualTo(2), "Both actual SDK quarters must survive saved-asset reload.");
        foreach (var quarter in quarters)
        {
            object Field(string name) => quarter.GetType().GetField(name).GetValue(quarter);
            Assert.That((float)Field("actualRadius"), Is.InRange(238.25f, 241.75f));
            Assert.That(((Vector3[])Field("actualMarkers")).Length, Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { "StraightXZY", "Circular", "StraightXZY", "StraightXZY" }, (string[])Field("actualControls"));
            Assert.That(((Vector3[])Field("actualSpline")).Length, Is.InRange(3, 16000));
            Assert.That(((Vector3[])Field("selectedSamples")).Length, Is.GreaterThan(2));
            Assert.That((float)Field("maximumSemanticEndpointSampleDistance"), Is.LessThanOrEqualTo(.3f));
        }
    }
}
#endif
