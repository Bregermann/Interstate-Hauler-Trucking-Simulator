#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderCanonicalEditorEpochTests
{
    static Type Author=>Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
    static string ReviewPath(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsRiderEditorConsumerEpochTestReview");Assert.That(at>=0&&at+1<args.Length,Is.True,"Actual reviewed manifest must bind an actually successful course and passed331source world sidecar; no placeholders qualify.");return args[at+1];}
    static object Review()=>JsonUtility.FromJson(File.ReadAllText(ReviewPath()),Author.GetNestedType("EditorEpochReview",BindingFlags.NonPublic));
    static object Get(object v,string name)=>v.GetType().GetField(name).GetValue(v);
    static void Set(object v,string name,object value)=>v.GetType().GetField(name).SetValue(v,value);
    [Test,Timeout(1800000)]
    public void ActualPriorCourseAndPassedWorldSidecarQualifyOnlyExactReviewedEditorChanges()
    {
        string review=ReviewPath();var before=File.ReadAllBytes(review);string result=(string)Author.GetMethod("ProbeActualEditorConsumerEpoch").Invoke(null,new object[]{review});var json=JsonUtility.FromJson(result,Author.GetNestedType("EditorEpochProof",BindingFlags.NonPublic));Assert.That(Get(json,"status"),Is.EqualTo("TEST_ONLY_EXACT_EDITOR_EPOCH_KERNEL_PASSED_NO_SCENE_OR_ADOPTION_AUTHORITY"));Assert.That(Get(json,"testOnly"),Is.EqualTo(true));foreach(string field in new[]{"sourceRowsExact","sceneDataExact","priorNativeAuthorityExact"})Assert.That(Get(json,field),Is.EqualTo(true));Assert.That(Get(json,"mountRows"),Is.EqualTo(5894));Assert.That((int)Get(json,"courseRows"),Is.GreaterThanOrEqualTo(5894));CollectionAssert.AreEqual(before,File.ReadAllBytes(review));
    }
    [TestCase("missing")][TestCase("duplicate")][TestCase("unlisted")][TestCase("wrong-old")][TestCase("wrong-new")]
    public void ReviewedEpochRejectsMissingConflictingAndUnlistedTransitions(string fault)
    {
        var review=Review();var rows=(Array)Get(review,"transitions");var rowType=rows.GetType().GetElementType();var working=(Array)rows.Clone();
        if(fault=="missing"){working=Array.CreateInstance(rowType,3);Array.Copy(rows,working,3);}if(fault=="duplicate")working.SetValue(working.GetValue(0),1);if(fault=="unlisted"){var copy=JsonUtility.FromJson(JsonUtility.ToJson(working.GetValue(0)),rowType);Set(copy,"path","Assets/TruckSports/Scripts/SportsSnowBodyContact.cs");working.SetValue(copy,0);}Set(review,"transitions",working);
        var shape=Author.GetMethod("EpochRequireShape",BindingFlags.NonPublic|BindingFlags.Static);
        if(fault=="missing"||fault=="duplicate"||fault=="unlisted"){var error=Assert.Throws<TargetInvocationException>(()=>shape.Invoke(null,new[]{review}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());return;}
        var row=working.GetValue(0);string original=(string)Get(row,"oldSha256"),expected=(string)Get(row,"newSha256"),path=(string)Get(row,"path");if(fault=="wrong-old")Set(row,"oldSha256",new string('0',64));else Set(row,"newSha256",new string('0',64));var compare=Author.GetMethod("EpochRequireComparison",BindingFlags.NonPublic|BindingFlags.Static);var rejected=Assert.Throws<TargetInvocationException>(()=>compare.Invoke(null,new[]{review,path,original,expected}));Assert.That(rejected.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void DefaultAndEveryUnlistedRuntimeArtConfigOrMetadataRowRemainStrict()
    {
        var review=Review();var expected=Author.GetMethod("EpochExpected",BindingFlags.NonPublic|BindingFlags.Static);var compare=Author.GetMethod("EpochRequireComparison",BindingFlags.NonPublic|BindingFlags.Static);const string old="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",changed="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        foreach(string path in new[]{"Assets/TruckSports/Scripts/SportsSnowBodyContact.cs","Assets/TruckDragonRiders/Materials/Royal skyfire gate gold.mat","ProjectSettings/TagManager.asset","Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAdoptionApply.cs.meta"})
        {Assert.That(expected.Invoke(null,new object[]{null,path,old}),Is.EqualTo(old),"Default reader never substitutes a hash.");Assert.That(expected.Invoke(null,new[]{review,path,old}),Is.EqualTo(old),"Only exact four reviewed CSharp paths may differ.");var error=Assert.Throws<TargetInvocationException>(()=>compare.Invoke(null,new[]{review,path,old,changed}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());}
    }
}
#endif
