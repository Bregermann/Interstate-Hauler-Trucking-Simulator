#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class DragonKingdomLandscapeSurfaceFinishTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Owner.GetMethod(method).Invoke(null,args);
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    [TestCase(float.NaN,.5f,100)][TestCase(.5f,float.PositiveInfinity,100)][TestCase(-.01f,.5f,100)][TestCase(.5f,1.01f,100)]
    public void InvalidVendorMasksCannotReachNativeSavedSplatWrites(float meadow,float rock,float height)
    {var error=Assert.Throws<TargetInvocationException>(()=>Call("ComposeLandscapeSurfaceWeights",meadow,rock,height));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());}
    [TestCase("raw")][TestCase("clamped")]
    public void NonfiniteRawOrClampedMasksCannotBeReplacedByMissingDataFill(string fault)
    {var raw=new float[,]{{-.02f,.5f}};var clamped=new float[,]{{0,.5f}};if(fault=="raw")raw[0,0]=float.NaN;else clamped[0,0]=float.NaN;var error=Assert.Throws<TargetInvocationException>(()=>Call("ValidateLandscapeClampedMasks",raw,clamped));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("no missing-data fill"));}
    [TestCase(.05f)][TestCase(.7f)]
    public void AdjacentOriginalNearTieGrassClassesCannotBecomeLargeCategoricalColorStripes(float meadow)
    {
        var proposed=(float[])Call("ComposeLandscapeSurfaceWeights",meadow,0f,20f);var left=(float[])Call("ConserveLandscapeSurfaceClassAndGrassRatio",proposed,new[]{128/255f,127/255f,0f,0f});var right=(float[])Call("ConserveLandscapeSurfaceClassAndGrassRatio",proposed,new[]{127/255f,128/255f,0f,0f});Assert.That(left[0],Is.GreaterThanOrEqualTo(left[1]));Assert.That(right[1],Is.GreaterThan(right[0]));float leftRatio=left[0]/(left[0]+left[1]),rightRatio=right[0]/(right[0]+right[1]);Assert.That(Mathf.Abs(leftRatio-rightRatio),Is.LessThanOrEqualTo(3f/255));
    }
    [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
    public void BoundedQuantizationRetainsEveryOriginalWinnerAndExactUnorm8Mass(int winner)
    {
        var originals=new[]{new[]{128/255f,127/255f,0f,0f},new[]{127/255f,128/255f,0f,0f},new[]{63/255f,63/255f,65/255f,64/255f},new[]{0f,0f,0f,1f}};var proposed=new[]{.90f,.03f,.03f,.04f};var result=(float[])Call("ConserveLandscapeSurfaceClassAndGrassRatio",proposed,originals[winner]);Assert.That(result.Sum(),Is.EqualTo(1).Within(.000001));int actual=Array.IndexOf(result,result.Max());Assert.That(actual,Is.EqualTo(winner));foreach(float value in result){Assert.That(value,Is.InRange(0f,1f));Assert.That(value*255,Is.EqualTo(Mathf.Round(value*255)).Within(.00001));}Assert.That((float[])Call("ConserveLandscapeSurfaceClassAndGrassRatio",proposed,originals[winner]),Is.EqualTo(result));
    }
    [UnityTest,Timeout(180000)]
    public IEnumerator ActualAlternatingOctaveNoisePassesThroughInstalledGpuClampBeforeComposition()
    {
        yield return (IEnumerator)Call("CaptureLandscapeVendorClampRegression");var proof=Owner.GetProperty("LastLandscapeVendorClampProof").GetValue(null);var resolution=Field(proof,"resolution");Assert.That(Field(resolution,"requestedGraphResolution"),Is.EqualTo(1024));foreach(string flag in new[]{"configuredResolutionMatchesVendorContract","graphArgumentExact","nodeOutputsExact","readbackShapesExact"})Assert.That(Field(resolution,flag),Is.True,flag);Assert.That((int)Field(resolution,"rawWidth"),Is.GreaterThan(0));Assert.That((int)Field(proof,"samples"),Is.EqualTo((int)Field(resolution,"rawWidth")*(int)Field(resolution,"rawHeight")));Assert.That(Field(resolution,"noiseResolutionOverride"),Is.EqualTo("RelativeToMainInput"));Assert.That(Field(resolution,"clampResolutionOverride"),Is.EqualTo("RelativeToMainInput"));Assert.That(Field(resolution,"noiseResolutionMultiplier"),Is.EqualTo(1f));Assert.That(Field(resolution,"clampResolutionMultiplier"),Is.EqualTo(1f));Assert.That((int)Field(proof,"rawBelowZero"),Is.GreaterThan(0));foreach(string flag in new[]{"rawAndClampedFinite","clampedNormalized","nativeTexelClampExact","resourcesDisposed"})Assert.That(Field(proof,flag),Is.True,flag);Assert.That((float)Field(proof,"clampedMinimum"),Is.GreaterThanOrEqualTo(0));Assert.That((float)Field(proof,"clampedMaximum"),Is.LessThanOrEqualTo(1));Assert.That((float)Field(proof,"maximumNativeClampError"),Is.LessThanOrEqualTo(.000001f));TestContext.WriteLine(JsonUtility.ToJson(proof,true));
    }
    [UnityTest,Timeout(1200000)]
    public IEnumerator ActualVendorMasksSaveAndReloadVisibleTerrainSurfacesWithoutChangingOriginalWorldOrCollision()
    {
        yield return (IEnumerator)Call("CaptureLandscapeSurfaceFinishReview");var proof=Owner.GetProperty("LastLandscapeSurfaceReview").GetValue(null);Assert.That(Field(proof,"status"),Is.EqualTo("SAVED_VENDOR_LANDSCAPE_SURFACES_CAPTURED_REQUIRES_DIRECT_VISUAL_REVIEW_NOT_ADOPTION"));Assert.That(Field(proof,"error"),Is.Null.Or.Empty);
        foreach(string flag in new[]{"savedReloaded","originalWorldExact","originalSourcesExact","incomingSceneContextRestored","vendorResourcesDisposed"})Assert.That(Field(proof,flag),Is.True,flag);Assert.That(Field(proof,"originalTerrains"),Is.EqualTo(16));Assert.That((int)Field(proof,"originalColliders"),Is.EqualTo(5957));var tiles=(Array)Field(proof,"tiles");Assert.That(tiles.Length,Is.EqualTo(4));foreach(var tile in tiles){Assert.That(Field(tile,"heightAfter"),Is.EqualTo(Field(tile,"heightBefore")));Assert.That(Field(tile,"holesAfter"),Is.EqualTo(Field(tile,"holesBefore")));Assert.That(Field(tile,"sameVisualPhysicalProvider"),Is.True);Assert.That(Field(tile,"interpolationPayloadExact"),Is.True);Assert.That(Field(tile,"layersExact"),Is.True);Assert.That((int)Field(tile,"changedPixels"),Is.GreaterThan(1000));Assert.That((float)Field(tile,"maximumWeightSumError"),Is.LessThanOrEqualTo(1.01f/255));}
        var fields=(Array)Field(proof,"allOriginalFields");Assert.That(fields.Length,Is.EqualTo(16));foreach(var field in fields){Assert.That(Field(field,"originalBytesAndMemoryExact"),Is.True);Assert.That(Field(field,"savedProviderPayloadExact"),Is.True);}
        foreach(var tile in tiles){Assert.That(Field(tile,"dominantLayerMismatches"),Is.EqualTo(0));Assert.That(Field(tile,"nwhIndexMismatches"),Is.EqualTo(0));Assert.That((int)Field(tile,"nwhSampledPoints"),Is.GreaterThanOrEqualTo(1089));Assert.That(Field(tile,"savedDominantTerrainSha256"),Is.EqualTo(Field(tile,"originalDominantTerrainSha256")));}
        foreach(var tile in tiles){Assert.That(Field(tile,"originalGrassRatioPreserved"),Is.True);Assert.That(Field(tile,"boundaryGrassRatiosSmooth"),Is.True);Assert.That((int)Field(tile,"grassClassBoundaryEdges"),Is.GreaterThan(0));Assert.That((float)Field(tile,"maximumGrassRatioCrossProductError"),Is.LessThanOrEqualTo(2.01f/255));Assert.That((float)Field(tile,"savedMeanBoundaryGrassRatioJump"),Is.LessThanOrEqualTo((float)Field(tile,"sourceMeanBoundaryGrassRatioJump")+.02f));Assert.That((float)Field(tile,"savedMaximumBoundaryGrassRatioJump"),Is.LessThanOrEqualTo((float)Field(tile,"sourceMaximumBoundaryGrassRatioJump")+.04f));}
        foreach(string name in new[]{"maskBounds","nativeMaskBounds"}){var mask=Field(proof,name);Assert.That(Field(mask,"rawAndClampedFinite"),Is.True);Assert.That(Field(mask,"clampedNormalized"),Is.True);}Assert.That(Field(Field(proof,"nativeMaskBounds"),"nativeTexelClampExact"),Is.True);
        Assert.That(((Array)Field(proof,"frames")).Length,Is.EqualTo(12));Assert.That(((string[])Field(proof,"changedSources")).Length,Is.Zero);TestContext.WriteLine(JsonUtility.ToJson(proof,true));TestContext.WriteLine("SAVED_NATIVE_VENDOR_SURFACE_COMPOSITION_ONLY_FINISHED_LANDSCAPE_REQUIRES_ACTUAL_SIX_VIEW_REVIEW");
    }
}
#endif
