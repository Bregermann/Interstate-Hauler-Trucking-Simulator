#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomCavernOccupancyTests
{
    [Test,Timeout(900000)]
    public void OriginalConfiguredGuardianActionsAndActualNwhCargoRoadsAreMeasuredWithoutChangingSourceOrSceneContext()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        type.GetMethod("CaptureOriginalCavernOccupancyFromCommandLine").Invoke(null,null);
        var proof=type.GetProperty("LastCavernOccupancy").GetValue(null);
        Assert.That((string)Field(proof,"status"),Is.EqualTo("ORIGINAL_CONFIGURED_ACTION_AND_AUTHORED_OCCUPANCY_MEASURED_LIVE_TRANSITIONS_AND_VAULT_GEOMETRY_PENDING"));
        foreach(string name in new[]{"originalGuardianMeshMembershipExact","sourceBytesPreserved","incomingSceneContextExact","previewDestroyed"})Assert.That((bool)Field(proof,name),Is.True,name);
        Assert.That((int)Field(proof,"actionMappings"),Is.GreaterThan(0));Assert.That((int)Field(proof,"actionSamples"),Is.GreaterThan(0));
        Assert.That((int)Field(proof,"sourceSkinVertices"),Is.GreaterThan(0));Assert.That((int)Field(proof,"originalTrucks"),Is.EqualTo(4));
        Assert.That((int)Field(proof,"originalTreasure"),Is.EqualTo(16));Assert.That((int)Field(proof,"roadSegments"),Is.GreaterThan(0));
        Assert.That((float)Field(proof,"authoredFloorY"),Is.EqualTo(105).Within(.001f));
        var actions=((Array)Field(proof,"actions")).Cast<object>().ToArray();Assert.That(actions.Length,Is.EqualTo((int)Field(proof,"actionMappings")));
        Assert.That(actions.Sum(a=>(int)Field(a,"frames")),Is.EqualTo((int)Field(proof,"actionSamples")));
        foreach(var action in actions)
        {
            foreach(string name in new[]{"catalogClipSha256","clipSha256","timesSha256"})Assert.That((string)Field(action,name),Has.Length.EqualTo(64),name);
            Assert.That((int)Field(action,"frames"),Is.GreaterThan(1));Assert.That(((Bounds)Field(action,"actualWorldSkinBounds")).size.y,Is.GreaterThan(0));
        }
        var bodies=((Array)Field(proof,"bodies")).Cast<object>().ToArray();
        foreach(string kind in new[]{"ORIGINAL_NWH_PRIMARY_CHASSIS","ORIGINAL_NWH_COMPLETE_MODEL","ORIGINAL_CARGO_RACK"})Assert.That(bodies.Count(b=>(string)Field(b,"kind")==kind),Is.EqualTo(4),kind);
        Assert.That(bodies.Count(b=>(string)Field(b,"kind")=="ORIGINAL_TREASURE"),Is.EqualTo(16));
        var sources=((Array)Field(proof,"protectedSources")).Cast<object>().ToArray();Assert.That(sources.Length,Is.GreaterThan(0));
        foreach(var source in sources)Assert.That((string)Field(source,"sha256"),Has.Length.EqualTo(64));
        // This is a read-only source/action measurement, not live Mecanim, driving,
        // new excavation, final roof clearance, rendering or human acceptance.
    }
}
#endif
