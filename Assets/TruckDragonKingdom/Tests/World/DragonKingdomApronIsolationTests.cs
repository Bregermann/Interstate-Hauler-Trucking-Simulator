#if UNITY_EDITOR
using System;
using NUnit.Framework;
public sealed class DragonKingdomApronIsolationTests
{
    [Test,Timeout(240000)]
    public void ActualPublicNetworkSurroundingFactorControlPreservesExplicitInputsAndPavement()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureOriginalVistaSurroundingFactorFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastApronIsolationEvidence").GetValue(null);object Read(object obj,string name)=>obj.GetType().GetField(name).GetValue(obj);Assert.That((string)Read(proof,"status"),Is.EqualTo("ACTUAL_PUBLIC_SURROUNDING_FACTOR_CONTROL_CAPTURED_REQUIRES_WHOLE_FIELD_CORE_AND_QUALITY_ANALYSIS"));foreach(string name in new[]{"sameActualInputFields","sourceBytesExact","incomingContextExact","globalRandomStateRestored","samePavementVerticesExact"})Assert.That((bool)Read(proof,name),Is.True,name);int cases=0;foreach(object row in (System.Collections.IEnumerable)Read(proof,"cases")){float expected=cases++ == 0 ? .5f : 0;Assert.That((float)Read(row,"networkFactorPrefab"),Is.EqualTo(.5f));Assert.That((float)Read(row,"requestedNetworkFactor"),Is.EqualTo(expected));Assert.That((bool)Read(row,"networkFactorReadbacksExact"),Is.True);Assert.That((int)Read(row,"actualVendorBuilds"),Is.EqualTo(1));foreach(string phase in new[]{"mainBeforeFinalRefresh","mainAfterFinalRefresh","mainAfterBuild"}){var sample=Read(row,phase);Assert.That((float)Read(sample,"factor"),Is.EqualTo(expected));Assert.That((float)Read(sample,"requestedGlobalIndent"),Is.EqualTo(28));Assert.That((float)Read(sample,"requestedGlobalSurrounding"),Is.EqualTo(110));Assert.That(((float[])Read(sample,"actualMarkerLeftAdjusted")).Length,Is.EqualTo(17));Assert.That(((float[])Read(sample,"actualMarkerRightAdjusted")).Length,Is.EqualTo(17));Assert.That((int)Read(sample,"pavementVertices"),Is.GreaterThan(0));Assert.That(System.IO.File.Exists((string)Read(sample,"contourJson")),Is.True);}}Assert.That(cases,Is.EqualTo(2));
    }
    [Test,Timeout(240000)]
    public void ActualNativeGradeIsInvariantToWholeScenePlus20YTranslation()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureOriginalVistaYTranslationFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastApronIsolationEvidence").GetValue(null);object Read(object obj,string name)=>obj.GetType().GetField(name).GetValue(obj);foreach(string name in new[]{"sameActualInputFields","sourceBytesExact","incomingContextExact","globalRandomStateRestored"})Assert.That((bool)Read(proof,name),Is.True,name);var comparison=Read(proof,"yTranslation");Assert.That((int)Read(comparison,"comparedVertices"),Is.EqualTo(2*513*513));Assert.That((string)Read(proof,"status"),Is.EqualTo("ACTUAL_TRANSLATED_NATIVE_FIELDS_EQUIVALENT_WITHIN_PUBLIC_HEIGHT_ENCODING_BOUND"));Assert.That((bool)Read(comparison,"passed"),Is.True);Assert.That((int)Read(comparison,"overEncodingBound"),Is.Zero);
    }
    [Test,Timeout(240000)]
    public void WiderOriginalSourceFragmentTargetsRespectExistingPhysicalGroundTolerance()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureWiderVistaFragmentGroundTargetsFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastApronIsolationEvidence").GetValue(null);object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);foreach(string name in new[]{"sameActualInputFields","sourceBytesExact","incomingContextExact","globalRandomStateRestored"})Assert.That((bool)Field(name),Is.True,name);Assert.That((string)Field("status"),Is.EqualTo("WIDER_FRAGMENT_ORIGINAL_GROUND_TARGET_TOLERANCE_PASSED_NOT_FULL_PHYSICS_ACCEPTANCE"));var targets=Field("fragmentGroundTargets");object Target(string name)=>targets.GetType().GetField(name).GetValue(targets);Assert.That((bool)Target("passed"),Is.True);Assert.That((int)Target("cases"),Is.EqualTo(2));Assert.That((int)Target("affectedNativeVertices"),Is.GreaterThan(0));Assert.That((int)Target("unmatchedOriginalTargets"),Is.Zero);Assert.That((int)Target("rejectedTargets"),Is.Zero);Assert.That((float)Target("maximumDelta"),Is.LessThanOrEqualTo((float)Target("existingPhysicalGroundTolerance")));
    }
    [Test,Timeout(240000)]
    public void ActualPublicPreserveTerrainSettingIsolatedOnIdenticalFullMainAndRawVista()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureOriginalVistaPreservationIsolationFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastApronIsolationEvidence").GetValue(null);object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);Assert.That((string)Field("status"),Is.EqualTo("ACTUAL_PUBLIC_PRESERVATION_ISOLATION_CAPTURED_REQUIRES_CAUSAL_ANALYSIS_NOT_WORLD_ACCEPTANCE"));foreach(string name in new[]{"sameActualInputFields","sourceBytesExact","incomingContextExact","preservationOnlyRoadInputsExact","preservationMainContoursExact","globalRandomStateRestored"})Assert.That((bool)Field(name),Is.True,name);
        int index=0;foreach(object comparison in (System.Collections.IEnumerable)Field("cases")){object Read(string name)=>comparison.GetType().GetField(name).GetValue(comparison);Assert.That((float)Read("prefabPreserveTerrainFloat"),Is.EqualTo(1));Assert.That((float)Read("requestedPreserveTerrainFloat"),Is.EqualTo(index++==0?1:0));foreach(string name in new[]{"creationHeightfieldsExact","preservationSettingExact","roadConfigurationExactAfterBuild","caseStartRandomStateExact"})Assert.That((bool)Read(name),Is.True,name);Assert.That((int)Read("actualVendorBuilds"),Is.EqualTo(1));Assert.That(((Array)Read("stations")).Length,Is.EqualTo(32));}Assert.That(index,Is.EqualTo(2));
    }
    [Test,Timeout(240000)]
    public void ActualSourceFragmentMatchesPhysicalMainContourBeforeComparingSelfOverlap()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureOriginalVistaFragmentIsolationFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastApronIsolationEvidence").GetValue(null);object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);Assert.That((string)Field("status"),Is.EqualTo("ACTUAL_SOURCE_FRAGMENT_ISOLATION_CAPTURED_REQUIRES_CAUSAL_ANALYSIS_NOT_WORLD_ACCEPTANCE"));foreach(string name in new[]{"sameActualInputFields","sourceBytesExact","incomingContextExact","fragmentContoursQualified"})Assert.That((bool)Field(name),Is.True,name);
    }
    [Test,Timeout(240000)]
    public void ActualOriginalVistaMainAndHairpinComparisonPreservesInputsSourcesAndScene()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureOriginalVistaApronIsolationFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastApronIsolationEvidence").GetValue(null);object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);Assert.That((string)Field("status"),Is.EqualTo("ACTUAL_ORIGINAL_VISTA_APRON_ISOLATION_CAPTURED_REQUIRES_CAUSAL_ANALYSIS_NOT_WORLD_ACCEPTANCE"));foreach(string name in new[]{"sameActualInputFields","sourceBytesExact","incomingContextExact","globalRandomStateRestored"})Assert.That((bool)Field(name),Is.True,name);
        foreach(object comparison in (System.Collections.IEnumerable)Field("cases"))foreach(object road in (Array)comparison.GetType().GetField("roads").GetValue(comparison))
        {
            object Read(string name)=>road.GetType().GetField(name).GetValue(road);Assert.That((bool)Read("actualReturnsMatchPublicClamp"),Is.True);float expected=Math.Max((float)Read("requestedMarkerSurrounding"),(float)Read("publicMinIndent"));var returned=(float[])Read("returnedSurrounding");Assert.That(returned.Length,Is.EqualTo(((Array)Read("markers")).Length));foreach(float value in returned)Assert.That(value,Is.EqualTo(expected),"Surrounding must actually execute even when the preceding Indent call is clamped.");
        }
    }
}
#endif
