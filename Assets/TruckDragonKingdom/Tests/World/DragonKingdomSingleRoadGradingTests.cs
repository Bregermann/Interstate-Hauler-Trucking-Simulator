#if UNITY_EDITOR
using System;
using NUnit.Framework;
public sealed class DragonKingdomSingleRoadGradingTests
{
    [Test,Timeout(180000)]
    public void ActualSingleRoadBuildRecordsNativeContoursAndExactGridFieldsWithoutChangingSources()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);
        type.GetMethod("CaptureSingleRoadGradingFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastSingleRoadEvidence").GetValue(null);
        object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
        Assert.That((string)Field("status"),Is.EqualTo("ACTUAL_SINGLE_VENDOR_BUILD_CAPTURED_REQUIRES_TRANSITION_ANALYSIS_NOT_WORLD_ACCEPTANCE"));Assert.That((int)Field("actualVendorBuilds"),Is.EqualTo(1));
        foreach(string field in new[]{"markerReturnsExact","existingTypesExact","sourceBytesExact","incomingContextExact"})Assert.That((bool)Field(field),Is.True,field);
    }
}
#endif
