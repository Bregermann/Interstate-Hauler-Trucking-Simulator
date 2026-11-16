#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;
public sealed class DragonKingdomWorldAlignedTowerTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string name,params object[] args)=>Owner.GetMethod(name).Invoke(null,args);
    static object Field(object data,string name)=>data.GetType().GetField(name).GetValue(data);
    static string Input(string name)=>File.ReadAllText((string)Owner.GetField(name).GetValue(null));
    [Test]
    public void ReusesExactInstalledVendorProjectionAndAbsoluteWorldTemplateWithoutUvRemap()
    {
        string source=Input("TowerDetailVendorGraph"),template=Input("TowerWorldAlignedTemplate"),result=(string)Call("BuildVendorWorldAlignedTowerGraph",source,template,10f);
        Assert.That(result,Does.Contain("ed4350c79930b0a448aa5d4103d84d09"));Assert.That(result,Does.Contain("\"m_Space\": 4"));Assert.That(result,Does.Contain("\"x\": 10.0"));Assert.That(result,Does.Not.Contain((string)Owner.GetField("TowerSamplingEdge").GetValue(null)));Assert.That(Input("TowerDetailVendorGraph"),Is.EqualTo(source));Assert.That(Input("TowerWorldAlignedTemplate"),Is.EqualTo(template));
    }
    [TestCase(7.9f)][TestCase(12.1f)][TestCase(float.NaN)]
    public void RejectsUnboundedWorldTexturePeriod(float period)
    {var error=Assert.Throws<TargetInvocationException>(()=>Call("BuildVendorWorldAlignedTowerGraph",Input("TowerDetailVendorGraph"),Input("TowerWorldAlignedTemplate"),period));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
    [TestCase("wrong-world-space")][TestCase("renamed-size")][TestCase("custom-color-edge")]
    public void RejectsAmbiguousOrCustomizedVendorBindings(string fault)
    {
        string source=Input("TowerDetailVendorGraph"),template=Input("TowerWorldAlignedTemplate");switch(fault){case "wrong-world-space":template=template.Replace("\"m_Space\": 4","\"m_Space\": 0");break;case "renamed-size":template=template.Replace("\"m_DisplayName\": \"TextureSize\"","\"m_DisplayName\": \"CustomSize\"");break;case "custom-color-edge":source=source.Replace("ef4f46b84c544482a63d760ae2d12b1a","00000000000000000000000000000000");break;}var error=Assert.Throws<TargetInvocationException>(()=>Call("BuildVendorWorldAlignedTowerGraph",source,template,10f));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void OriginalZeroMaskAndUnboundZeroUvAreAnExplicitNegativeRegression()
    {Assert.That(Call("OriginalVendorTowerInputsWouldSuppressTextureVariation",Input("TowerDetailVendorGraph")),Is.True);}
    [TestCase(false)][TestCase(true)]
    public void ActualInstalledMultiJsonParserAcceptsExactDocumentsAndRejectsThePriorMixedSeparatorFailure(bool mixed)
    {
        string source=Input("TowerDetailVendorGraph"),owned=(string)Call("BuildVendorWorldAlignedTowerGraph",source,Input("TowerWorldAlignedTemplate"),10f);if(mixed){int last=owned.LastIndexOf("\r\n\r\n",StringComparison.Ordinal);Assert.That(last,Is.GreaterThan(0));owned=owned.Substring(0,last)+"\n\n"+owned.Substring(last+4);var error=Assert.Throws<TargetInvocationException>(()=>Call("ValidateTowerWorldAlignedNativeSerialization",source,owned));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.InnerException,Is.TypeOf<ArgumentException>());Assert.That(error.InnerException.Message,Does.Contain("actual installed ShaderGraph MultiJson parser").IgnoreCase);}else Assert.That(Call("ValidateTowerWorldAlignedNativeSerialization",source,owned),Is.EqualTo(664));
    }
    [UnityTest,Timeout(420000)]
    public IEnumerator ActualWorldAlignedColorPreservesOriginalMeshesAndCapturesAllResponseControls()
    {
        yield return (IEnumerator)Call("CaptureWorldAlignedTowerReview");var review=Owner.GetProperty("LastWorldAlignedTowerReview").GetValue(null);Assert.That(Field(review,"status"),Is.EqualTo("WORLD_ALIGNED_ORIGINAL_MESH_COLOR_CAPTURED_REQUIRES_DIRECT_STYLE_REVIEW"));Assert.That(Field(review,"error"),Is.Null.Or.Empty);foreach(string flag in new[]{"originalRendererMeshesExact","noRenderMeshSubstitution","noUvRemapping"})Assert.That(Field(review,flag),Is.True,flag);Assert.That(Field(review,"originalTerrains"),Is.EqualTo(16));Assert.That(Field(review,"originalPerches"),Is.EqualTo(4));Assert.That((int)Field(review,"originalColliders"),Is.GreaterThan(0));var graph=Field(review,"graph");foreach(string flag in new[]{"sourceDeltaReversible","sourceVendorBytesExact","originalPrimaryColorWiringPreserved","nativeMultiJsonBeforeWrite","nativeMultiJsonPersistedBeforeImport","nativeMultiJsonAfterImport"})Assert.That(Field(graph,flag),Is.True,flag);Assert.That(((Array)Field(review,"measuredTowerMasks")).Length,Is.EqualTo(2));var responses=(Array)Field(review,"responses");Assert.That(responses.Length,Is.EqualTo(2));foreach(var response in responses)Assert.That(Field(response,"measuredTowerResponsePassed"),Is.True);var appearance=Field(review,"appearance");Assert.That(((Array)Field(appearance,"frames")).Length,Is.EqualTo(10));foreach(string flag in new[]{"originalAtlasColorPreserved","allColliderReferencesAndStateExact","allHeightfieldsExact","allPerchReferencesAndPosesExact","originalWorldStateRestored","sourceBytesExact","temporaryObjectsDestroyed","incomingSceneContextRestored"})Assert.That(Field(appearance,flag),Is.True,flag);foreach(var material in (Array)Field(review,"materials"))foreach(string name in new[]{"mask0","mask12","mask20"})Assert.That(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>((string)Field(material,name)),Is.Not.Null);TestContext.WriteLine("REAL_WORLD_ALIGNED_RESPONSE_ONLY_DIRECT_STYLE_REVIEW_AND_CANONICAL_ADOPTION_REMAIN_SEPARATE");
    }
}
#endif
