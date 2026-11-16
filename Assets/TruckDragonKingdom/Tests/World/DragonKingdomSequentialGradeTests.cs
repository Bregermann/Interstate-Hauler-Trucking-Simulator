#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;

public sealed class DragonKingdomSequentialGradeTests
{
    [Test, Timeout(300000)]
    public void ActualFullOriginalContextFourLegMasksCompareBothPublicBuildOrders()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureOriginalVistaFullContextLegGradesFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastSequentialGradeEvidence").GetValue(null);object Read(object obj,string name)=>obj.GetType().GetField(name).GetValue(obj);Assert.That((string)Read(proof,"status"),Is.EqualTo("FULL_CONTEXT_PUBLIC_MASK_STAGES_CAPTURED_TARGET_CORRESPONDENCE_PASSED_REQUIRES_PHYSICS_AND_QUALITY"));foreach(string name in new[]{"sameInputs","sameCreatedContours","sourceBytesExact","incomingContextExact","globalRandomStateRestored"})Assert.That((bool)Read(proof,name),Is.True,name);int cases=0;foreach(object row in (IEnumerable)Read(proof,"cases")){foreach(object target in (Array)Read(row,"fullContextLegTargets"))Assert.That((bool)Read(target,"passed"),Is.True);int stages=0;foreach(object stage in (IEnumerable)Read(row,"stages")){foreach(string name in new[]{"currentInputChainExact","allRoadContoursExact","settingsExact"})Assert.That((bool)Read(stage,name),Is.True,name);int first=(int)Read(stage,"originalFirstMarker"),last=(int)Read(stage,"originalLastMarker");var mask=(bool[])Read(stage,"actualAuxiliaryMarkerDeformation");Assert.That(mask.Length,Is.EqualTo(17));for(int i=0;i<16;i++)Assert.That(mask[i],Is.EqualTo(i>=5&&i>=first&&i<last),"Public source interval mask"+i);stages++;}Assert.That(stages,Is.EqualTo(cases++==0?1:4));}Assert.That(cases,Is.EqualTo(3));
    }
    [Test, Timeout(300000)]
    public void ActualSequentialSourceFragmentBuildsPreserveInputChainAndCompareBothOrders()
    {
        var type = Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor", true);
        type.GetMethod("CaptureOriginalVistaSequentialGradesFromCommandLine").Invoke(null, null);
        var proof = type.GetProperty("LastSequentialGradeEvidence").GetValue(null);
        object Read(object obj, string name) => obj.GetType().GetField(name).GetValue(obj);
        Assert.That((string)Read(proof, "status"), Is.EqualTo("ACTUAL_SEQUENTIAL_PUBLIC_BUILDS_CAPTURED_TARGET_CORRESPONDENCE_PASSED_REQUIRES_PHYSICAL_AND_QUALITY_ANALYSIS"));
        foreach (string name in new[] { "sameInputs", "sameCreatedContours", "sourceBytesExact", "incomingContextExact", "globalRandomStateRestored" }) Assert.That((bool)Read(proof, name), Is.True, name);
        int caseIndex = 0;
        foreach (object row in (IEnumerable)Read(proof, "cases"))
        {
            int stages = 0;
            foreach (object stage in (IEnumerable)Read(row, "stages"))
            {
                foreach (string name in new[] { "currentInputChainExact", "allRoadContoursExact", "settingsExact" }) Assert.That((bool)Read(stage, name), Is.True, name);
                Assert.That(((Array)Read(stage, "fields")).Length, Is.EqualTo(2)); stages++;
            }
            Assert.That(stages, Is.EqualTo(caseIndex++ == 0 ? 1 : 2));
        }
        Assert.That(caseIndex, Is.EqualTo(3));
    }
}
#endif
