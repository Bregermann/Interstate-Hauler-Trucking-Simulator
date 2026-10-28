#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SportsReleaseMetadataTests
{
    static Type Metadata=>Type.GetType("LWS.TruckSports.SportsReleaseMetadata, Assembly-CSharp",true);
    static Type Builder=>Type.GetType("LWS.TruckSports.Editor.SportsBuilder, Assembly-CSharp-Editor",true);
    static object Load()=>Metadata.GetMethod("Load").Invoke(null,null);
    static string Field(object value,string name)=>(string)Metadata.GetField(name).GetValue(value);
    static void Set(object value,string name,string text)=>Metadata.GetField(name).SetValue(value,text);
    [Test]public void OwnedImpossibleResourceIsAlphaZeroSevenAndDoesNotChangeHostVersion()
    {
        string version=PlayerSettings.bundleVersion;byte[] bytes=File.ReadAllBytes("ProjectSettings/ProjectSettings.asset");var metadata=Load();Assert.That(Field(metadata,"version"),Is.EqualTo("0.7.0"));Assert.That(Field(metadata,"releaseName"),Is.EqualTo("The Impossible Games"));Assert.That(Field(metadata,"stage"),Is.EqualTo("Alpha"));Assert.That(PlayerSettings.bundleVersion,Is.EqualTo(version));CollectionAssert.AreEqual(bytes,File.ReadAllBytes("ProjectSettings/ProjectSettings.asset"));
    }
    [TestCase(null),TestCase("0.6"),TestCase("0.6.0-alpha"),TestCase("0.6.0.1"),TestCase("00.6.0"),TestCase(" 0.6.0"),TestCase("0.-6.0")]
    public void InvalidOrAmbiguousVersionsRefuseBeforeBuildScope(string version)
    {
        var metadata=Load();Set(metadata,"version",version);var failure=Assert.Throws<TargetInvocationException>(()=>Metadata.GetMethod("Validate").Invoke(metadata,null));Assert.That(failure.InnerException,Is.TypeOf<ArgumentException>());
    }
    [Test]public void EmptyOrMultilineReleaseLabelsAreRejectedWithoutEditingTheResource()
    {
        foreach(var sample in new[]{("releaseName",""),("releaseName","Carnival\nof Chaos"),("stage"," "),("stage","Alpha\r\nRelease")}){var metadata=Load();Set(metadata,sample.Item1,sample.Item2);Assert.That(Assert.Throws<TargetInvocationException>(()=>Metadata.GetMethod("Validate").Invoke(metadata,null)).InnerException,Is.TypeOf<ArgumentException>());}Assert.That(Field(Load(),"releaseName"),Is.EqualTo("The Impossible Games"));Assert.That(Field(Load(),"stage"),Is.EqualTo("Alpha"));
    }
    [TestCase(false),TestCase(true)]public void ExistingBuildScopeAppliesReleaseVersionAndRestoresExactHostOnSuccessAndFailure(bool fail)
    {
        string name=PlayerSettings.productName,company=PlayerSettings.companyName,version=PlayerSettings.bundleVersion;var scenes=EditorBuildSettings.scenes.Select(scene=>(scene.path,scene.enabled)).ToArray();byte[] playerBytes=File.ReadAllBytes("ProjectSettings/ProjectSettings.asset"),sceneBytes=File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset");bool entered=false;var scope=Builder.GetMethod("WithSportsTitle",BindingFlags.NonPublic|BindingFlags.Static);
        Action body=()=>{entered=true;Assert.That(PlayerSettings.productName,Is.EqualTo("Truck Sports"));Assert.That(PlayerSettings.bundleVersion,Is.EqualTo(Field(Load(),"version")));Assert.That(PlayerSettings.bundleVersion,Is.EqualTo("0.7.0"));PlayerSettings.companyName="Owned Sports restoration fixture";EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/TruckSports/Scenes/TruckSports.unity",false)};if(fail)throw new IOException("Owned Sports version restore fixture");};
        if(fail){var error=Assert.Throws<TargetInvocationException>(()=>scope.Invoke(null,new object[]{body}));Assert.That(error.InnerException,Is.TypeOf<IOException>());Assert.That(error.InnerException.Message,Is.EqualTo("Owned Sports version restore fixture"));}else scope.Invoke(null,new object[]{body});Assert.That(entered,Is.True);Assert.That(PlayerSettings.productName,Is.EqualTo(name));Assert.That(PlayerSettings.companyName,Is.EqualTo(company));Assert.That(PlayerSettings.bundleVersion,Is.EqualTo(version));CollectionAssert.AreEqual(scenes,EditorBuildSettings.scenes.Select(scene=>(scene.path,scene.enabled)));CollectionAssert.AreEqual(playerBytes,File.ReadAllBytes("ProjectSettings/ProjectSettings.asset"));CollectionAssert.AreEqual(sceneBytes,File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset"));TestContext.WriteLine("SPORTS_VERSION_SCOPE fail="+fail+" scoped0.7.0 restored="+version+" host product/company/scenes/API+exactbytes unchanged.");
    }
    [Test]public void ExistingDeliberateFailureFixtureAlsoChecksTheReleaseVersion()
    {
        Builder.GetMethod("VerifyFailureRestoration").Invoke(null,null);
    }
}
#endif
