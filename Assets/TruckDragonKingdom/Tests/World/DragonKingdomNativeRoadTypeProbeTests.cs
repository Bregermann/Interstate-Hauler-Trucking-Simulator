#if UNITY_EDITOR
using System;
using NUnit.Framework;
public sealed class DragonKingdomNativeRoadTypeProbeTests
{
    [Test,Timeout(180000)]
    public void PublicTypeRegistrationCreatesOwnedDefaultsAndRealMarkersWithoutChangingSourcesOrExistingTypes()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastEvidence").GetValue(null);
        object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
        Assert.That((string)Field("status"),Is.EqualTo("ACTUAL_PUBLIC_TYPE_DEFAULTS_AND_MARKERS_PASS_REQUIRES_REAL_GRADING_AND_WORLD_ACCEPTANCE"));
        foreach(string name in new[]{"existingTypesExact","collisionRejected","markerReturnsExact","actualBindingIdentityExact","sourceBytesExact","terrainHeightsExact","incomingContextExact"})Assert.That((bool)Field(name),Is.True,name);
    }
}
#endif
