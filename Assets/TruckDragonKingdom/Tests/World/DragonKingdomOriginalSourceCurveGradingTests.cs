#if UNITY_EDITOR
using System;
using NUnit.Framework;
public sealed class DragonKingdomOriginalSourceCurveGradingTests
{
    [Test,Timeout(180000)]
    public void ActualOriginalRoadSpecCurveRecordsNativeIndentGridAndDeformationFlags()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureOriginalSourceCurveGradingFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastSourceCurveEvidence").GetValue(null);
        object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);Assert.That((string)Field(proof,"status"),Is.EqualTo("ACTUAL_ORIGINAL_SOURCE_CURVE_BUILD_CAPTURED_REQUIRES_RASTER_ANALYSIS_NOT_WORLD_ACCEPTANCE"));
        foreach(string name in new[]{"sourcePointsOnlyTranslatedXZ","actualControlsExact","actualMarkerPositionsExact","actualBridgeFlagsCaptured"})Assert.That((bool)Field(proof,name),Is.True,name);
        var native=Field(proof,"native");Assert.That((int)Field(native,"actualVendorBuilds"),Is.EqualTo(1));foreach(string name in new[]{"sourceBytesExact","incomingContextExact","markerReturnsExact","existingTypesExact"})Assert.That((bool)Field(native,name),Is.True,name);
    }
}
#endif
