#if UNITY_EDITOR
using System;
using NUnit.Framework;
using System.Reflection;
using System.Linq;
using UnityEngine;

public sealed class DragonKingdomOwnedMountainRoadTerrainTests
{
    static Type Probe => Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor", true);
    [Test]
    public void ActualTwoEdgePavementStationMappingDoesNotGuessWhenTopologyChanges()
    {
        var map = Probe.GetMethod("OwnedTerrainVertexStation");
        Assert.That(map.Invoke(null, new object[] { 1776, 9728, 4864 }), Is.EqualTo(888));
        Assert.That(map.Invoke(null, new object[] { 1777, 9728, 4864 }), Is.EqualTo(888));
        var error = Assert.Throws<TargetInvocationException>(() => map.Invoke(null, new object[] { 1776, 9728, 4800 }));
        Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void OriginalCavernFootprintClassificationRetainsPortalEdgeOutsideWithoutExpandingBounds()
    {
        var classify = Probe.GetMethod("OwnedTerrainUnderOriginalFootprint"); var box = new Bounds(new Vector3(560, 135, 780), new Vector3(400, 140, 260));
        Assert.That(classify.Invoke(null, new object[] { new Vector3(724.7619f, 105.0451f, 910), box }), Is.EqualTo(true));
        Assert.That(classify.Invoke(null, new object[] { new Vector3(715.6613f, 105, 910.3211f), box }), Is.EqualTo(false));
    }
    [Test]
    public void PortalEndControlRequiresActualContiguousExcludedPrefix()
    {
        var select = Probe.GetMethod("OwnedPortalEndMarker");
        Assert.That(select.Invoke(null,new object[]{new[]{true,true,true,true,true,false,false}}),Is.EqualTo(4));
        foreach(var invalid in new[]{new[]{false,true,false},new[]{true,false,true},new[]{true,true}})
        {
            var error=Assert.Throws<TargetInvocationException>(()=>select.Invoke(null,new object[]{invalid}));
            Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
        }
    }
    [Test, Timeout(360000)]
    public void ActualOwned646PortalEndBlendUsesSamePavementInputsAndCapturesEveryRemainingFailure()
    {
        Probe.GetMethod("CaptureOwnedMountainPortalEndBlendFromCommandLine").Invoke(null,null);
        var proof=Probe.GetProperty("LastOwnedLayoutTerrainEvidence").GetValue(null);
        object Read(string name)=>proof.GetType().GetField(name).GetValue(proof);
        Assert.That(Read("status"),Is.EqualTo("ACTUAL_OWNED646_PORTAL_END_BLEND_PAIR_CAPTURED_NOT_WORLD_ACCEPTANCE"));
        foreach(string field in new[]{"portalEndBlendControl","sameActualInputFields","identicalActualPavement","originalSourcesExact","incomingContextExact","randomStateRestored"}) Assert.That(Read(field),Is.EqualTo(true),field);
        Assert.That(Read("pairedUndergroundDifferenceVertices"),Is.EqualTo(0),"A portal blend may not alter the already-retained original underground roof/footprint; captured differences remain a real rejection.");
        Assert.That(Read("pairedProtectedCoreDifferenceVertices"),Is.EqualTo(0),"Existing actual SDK value2 road core must remain exact.");
        Assert.That(Read("pairedChangedHoleCells"),Is.EqualTo(0),"An end blend is not permission to create/remove terrain holes.");
        var cases=((System.Collections.IEnumerable)Read("cases")).Cast<object>().ToArray();Assert.That(cases.Length,Is.EqualTo(2));
        for(int index=0;index<2;index++)
        {
            var row=cases[index];object Field(string name)=>row.GetType().GetField(name).GetValue(row);
            Assert.That(Field("name"),Is.EqualTo(index==0?"ownedPortalEnd16":"ownedPortalEnd0"));Assert.That(Field("actualVendorBuilds"),Is.EqualTo(1));
            foreach(string field in new[]{"markerInputsExact","ownedNetworkDestroyed","randomCaseStartExact"}) Assert.That(Field(field),Is.EqualTo(true),field);
            var levels=((System.Collections.IEnumerable)Field("portalLevels")).Cast<object>().ToArray();Assert.That(levels.Length,Is.EqualTo(5));
            var last=levels.Single(value=>(int)value.GetType().GetField("marker").GetValue(value)==(int)Field("portalEndMarker"));
            Assert.That(last.GetType().GetField("requestedEnd").GetValue(last),Is.EqualTo(index==0?16f:0f));
            if(index==1)Assert.That(last.GetType().GetField("actualEnd").GetValue(last),Is.EqualTo(0f));
        }
        // A captured experiment is not acceptance. Original .3m pavement,
        // underground/core preservation, holes and65/80 slope limits stand.
        // Every measured failure is retained rather than waived by this test.
    }
    [Test, Timeout(360000)]
    public void ActualOwnedRouteGradingComparisonPreservesRawInputsAndCapturesPhysicalResiduals()
    {
        var type = Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor", true);
        type.GetMethod("CaptureOwnedMountainRoadTerrainFromCommandLine").Invoke(null, null);
        var proof = type.GetProperty("LastOwnedLayoutTerrainEvidence").GetValue(null);
        object Read(string name) => proof.GetType().GetField(name).GetValue(proof);
        Assert.That((string)Read("status"), Is.EqualTo("ACTUAL_OWNED_ROUTE_GRADE_COMPARISON_CAPTURED_REQUIRES_CORE_ROOF_AND_WHOLE_WORLD_ACCEPTANCE"));
        foreach (string field in new[] { "sameActualInputFields", "originalSourcesExact", "incomingContextExact", "randomStateRestored" }) Assert.That((bool)Read(field), Is.True, field);
        int count = 0;
        foreach (var row in (System.Collections.IEnumerable)Read("cases"))
        {
            object Field(string name) => row.GetType().GetField(name).GetValue(row);
            Assert.That((int)Field("actualVendorBuilds"), Is.EqualTo(1)); Assert.That((int)Field("physicalSamples"), Is.GreaterThan(0));
            foreach (string field in new[] { "markerInputsExact", "ownedNetworkDestroyed", "randomCaseStartExact" }) Assert.That((bool)Field(field), Is.True, field);
            var outliers = (System.Collections.ICollection)Field("physicalOutliers");
            Assert.That(outliers.Count, Is.EqualTo(Math.Min(128, (int)Field("physicalOverExistingTolerance"))));
            foreach (var field in (System.Collections.IEnumerable)Field("fields"))
            {
                object Detail(string name) => field.GetType().GetField(name).GetValue(field);
                Assert.That(System.IO.File.Exists((string)Detail("inputFloat32")), Is.True, "Retain the exact actual baseline rather than infer new slopes from output alone.");
                Assert.That((Vector3)Detail("actualTerrainSize"), Is.EqualTo(new Vector3(1600, 600, 1600)));
                Assert.That((int)Detail("actualHeightmapResolution"), Is.EqualTo(513));
                Assert.That(System.IO.File.Exists((string)Detail("inputHolesRawBool8")) && System.IO.File.Exists((string)Detail("outputHolesRawBool8")), Is.True);
                Assert.That((int)Detail("createdSteep65") + (int)Detail("retainedSteep65"), Is.EqualTo((int)Detail("steep65")));
                Assert.That((int)Detail("createdSteep80") + (int)Detail("retainedSteep80"), Is.EqualTo((int)Detail("steep80")));
                Assert.That((int)Detail("createdSteep65") - (int)Detail("removedSteep65"), Is.EqualTo((int)Detail("steep65") - (int)Detail("baselineSteep65")));
            }
            count++;
        }
        Assert.That(count, Is.EqualTo(2));
        // This fixture validates the controlled native comparison itself.
        // Recorded pavement/roof/roughness failures remain real open failures;
        // they are never converted to a world/route acceptance flag here.
    }
}
#endif
