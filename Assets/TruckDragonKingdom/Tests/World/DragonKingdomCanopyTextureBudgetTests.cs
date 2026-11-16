#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class DragonKingdomCanopyTextureBudgetTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string name,params object[] args)=>Owner.GetMethod(name).Invoke(null,args);
    static object Field(object data,string name)=>data.GetType().GetField(name).GetValue(data);
    [TestCase(1,16L)][TestCase(4,48L)][TestCase(2048,5592432L)]
    public void BlockMipEstimateIncludesTheSmallestBc7BlocksAndRemainsSeparateFromNativeMemory(int size,long expected)
    {Assert.That(Call("EstimateCanopyBc7MipBytes",size,size),Is.EqualTo(expected));}
    [Test]
    public void ProductionPlatformPolicyIsExplicitWindowsBc7TwoKHighQualityWithoutCrunch()
    {var settings=(TextureImporterPlatformSettings)Call("CanopyProductionPlatformSettings");Assert.That(settings.name,Is.EqualTo("Standalone"));Assert.That(settings.overridden,Is.True);Assert.That(settings.maxTextureSize,Is.EqualTo(2048));Assert.That(settings.format,Is.EqualTo(TextureImporterFormat.BC7));Assert.That(settings.textureCompression,Is.EqualTo(TextureImporterCompression.CompressedHQ));Assert.That(settings.compressionQuality,Is.EqualTo(100));Assert.That(settings.crunchedCompression,Is.False);}
    [TestCase("rgb")][TestCase("coverage")][TestCase("near-threshold")][TestCase("matching")]
    public void LossyQualityGateChecksVisibleColorAndCutoutSilhouetteInsteadOfClaimingLosslessPixels(string fault)
    {var reference=Enumerable.Repeat(new Color32(160,210,70,255),64).ToArray();var production=(Color32[])reference.Clone();if(fault=="rgb")production=Enumerable.Repeat(new Color32(20,20,240,255),64).ToArray();if(fault=="coverage")production=Enumerable.Repeat(new Color32(160,210,70,0),64).ToArray();if(fault=="near-threshold"){reference=Enumerable.Repeat(new Color32(160,210,70,128),64).ToArray();production=Enumerable.Repeat(new Color32(160,210,70,127),64).ToArray();}var proof=Call("MeasureCanopyLossyQuality",reference,production);Assert.That(Field(proof,"acceptableLossyQuality"),Is.EqualTo(fault=="matching"));}
    [Test]
    public void ActualV538PngHeadersReceiptsAndNativeImportsProveMixedSourceSizesWithoutUpscaling()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonCanopyBudgetBaseline");Assert.That(at,Is.GreaterThanOrEqualTo(0));var baseline=JsonUtility.FromJson(System.IO.File.ReadAllText(args[at+1]),Owner.GetNestedType("CanopyTintReview"));var sources=((Array)Field(baseline,"materials")).Cast<object>().Where(v=>(bool)Field(v,"authoredMaskedTint")).ToArray();Assert.That(sources.Length,Is.EqualTo(3));var sizes=new System.Collections.Generic.List<int>();foreach(var source in sources){string path=(string)Field(source,"ownedTexture");var header=(Vector2Int)Call("ReadCanopySourcePngDimensions",path);var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);Assert.That(header.x,Is.EqualTo((int)Field(source,"width")));Assert.That(header.y,Is.EqualTo((int)Field(source,"height")));Assert.That(texture.width,Is.EqualTo(header.x));Assert.That(texture.height,Is.EqualTo(header.y));var capped=(Vector2Int)Call("CapCanopyProductionDimensions",header.x,header.y);Assert.That(capped.x,Is.LessThanOrEqualTo(header.x));Assert.That(capped.y,Is.LessThanOrEqualTo(header.y));sizes.Add(header.x);if((string)Field(source,"sourceGuid")=="ff5f626497301f741b9f81291d3ec3e6"){Assert.That(header,Is.EqualTo(new Vector2Int(1024,1024)));Assert.That(capped,Is.EqualTo(header));}}Assert.That(sizes.OrderBy(v=>v).ToArray(),Is.EqualTo(new[]{1024,4096,4096}));Assert.That((long)Call("CanopyNativeProfilerBudget",2048,2048),Is.EqualTo(11188960L));Assert.That((long)Call("CanopyNativeProfilerBudget",1024,1024),Is.EqualTo(2800352L));
    }
    [Test,Timeout(300000)]
    public void ActualNativeUrpAlbedoAliasSurvivesSaveReloadButOtherTextureAndUvChangesReject()
    {
        var proof=Call("InspectCanopyBudgetNativeAliasSaveReload");Assert.That(Field(proof,"status"),Is.EqualTo("NATIVE_ALIAS_SAVE_RELOAD_AND_UNRELATED_TEXTURE_GUARDS_PASSED"));Assert.That(Field(proof,"error"),Is.Null.Or.Empty);foreach(string flag in new[]{"beforeSaveExact","afterNativeImportExact","legacyAliasOwnsProductionTexture","otherTextureMutationRejected","brokenAliasRejected","uvMutationRejected","sourceBytesExact"})Assert.That(Field(proof,flag),Is.True,flag);TestContext.WriteLine(JsonUtility.ToJson(proof,true));
    }
    [UnityTest,Timeout(900000)]
    public IEnumerator ActualProductionCopiesPreservePngsPlacementAndMaterialsWithMeasuredMemoryAndTwelveViews()
    {
        yield return(IEnumerator)Call("CaptureCanopyTextureBudgetReview");var review=Owner.GetProperty("LastCanopyBudgetReview").GetValue(null);Assert.That(Field(review,"status"),Is.EqualTo("SAVED_2K_BC7_MEMORY_AND_QUALITY_CAPTURED_REQUIRES_DIRECT_STYLE_REVIEW_NOT_ADOPTION"));Assert.That(Field(review,"error"),Is.Null.Or.Empty);Assert.That(Field(review,"instances"),Is.EqualTo(166));Assert.That(Field(review,"canopies"),Is.EqualTo(26));foreach(string flag in new[]{"all166PlacementGeometryExact","allLodAndPhysicsExact","originalWorldExact","savedReloaded","originalSourcesExact","incomingSceneContextRestored","memoryBudgetPassed","uncompressedReviewDependenciesExcluded"})Assert.That(Field(review,flag),Is.True,flag);var textures=(Array)Field(review,"textures");Assert.That(textures.Length,Is.EqualTo(3));foreach(var texture in textures){foreach(string flag in new[]{"pngBytesExact","nonReadable","explicitStandaloneBc7","materialSemanticsExact","alphaCoveragePreserved","sourceHeaderDimensionsExact","notUpscaled","profilerBudgetPassed"})Assert.That(Field(texture,flag),Is.True,flag);Assert.That(Field(texture,"width"),Is.EqualTo(Math.Min((int)Field(texture,"sourcePngWidth"),2048)));Assert.That(Field(texture,"height"),Is.EqualTo(Math.Min((int)Field(texture,"sourcePngHeight"),2048)));Assert.That(Field(texture,"productionFormat"),Is.EqualTo("BC7"));Assert.That((int)Field(texture,"mips"),Is.EqualTo((int)Mathf.Log((int)Field(texture,"width"),2)+1));Assert.That((long)Field(texture,"runtimeTextureAccountingBudgetBytes"),Is.EqualTo(2*(long)Field(texture,"estimatedBc7MipBytes")+4096));Assert.That(Field(Field(texture,"quality"),"acceptableLossyQuality"),Is.True);Assert.That((long)Field(texture,"productionRuntimeTextureBytes"),Is.GreaterThan(0));}Assert.That(((Array)Field(review,"frames")).Length,Is.EqualTo(12));TestContext.WriteLine(JsonUtility.ToJson(review,true));TestContext.WriteLine("NATIVE_RUNTIME_TEXTURE_MEMORY_AND_LOSSY_QUALITY_ONLY_TOTAL_GPU_RESIDENCY_AND_DIRECT_STYLE_ACCEPTANCE_SEPARATE");
    }
}
#endif
