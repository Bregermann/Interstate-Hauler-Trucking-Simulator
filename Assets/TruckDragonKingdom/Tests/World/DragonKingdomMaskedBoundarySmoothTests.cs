#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class DragonKingdomMaskedBoundarySmoothTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);
    [TestCase("50",50)][TestCase("100",100)][TestCase("200",200)][TestCase("500",500)][TestCase("1000",1000)]
    public void ExplicitPublicSmoothProfilesKeepExactBoundedIterationCounts(string selection,int expected)
    {var values=(int[])Integration.GetMethod("MaskedSmoothIterations",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{selection});CollectionAssert.AreEqual(new[]{expected},values);}
    [Test]
    public void PublicSmoothAllProfilesAndInvalidSelectionNeverBecomeAnUnboundedCustomFilter()
    {
        var method=Integration.GetMethod("MaskedSmoothIterations",BindingFlags.Static|BindingFlags.NonPublic);CollectionAssert.AreEqual(new[]{50,100,200},(int[])method.Invoke(null,new object[]{"All"}));CollectionAssert.AreEqual(new[]{500,1000},(int[])method.Invoke(null,new object[]{"Extended"}));var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{"10000"}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [TestCase(50,10)][TestCase(100,10)][TestCase(200,10)][TestCase(500,50)][TestCase(1000,50)]
    public void ExtendedProfilesBoundReadbackCadenceWhileKeepingVendorYieldAtTenIterations(int iterations,int expected)
    {Assert.That((int)Integration.GetMethod("MaskedSmoothReadbackCadence",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{iterations}),Is.EqualTo(expected));}
    [TestCase(1000,10,50,true)][TestCase(1000,1,50,false)][TestCase(1000,10,10,false)][TestCase(2000,10,50,false)]
    public void SharedAdapterRequiresProvenExplicitPublicParameters(int iterations,int perFrame,int cadence,bool valid)
    {
        var method=Integration.GetMethod("RequirePublicMaskedFeedbackParameters",BindingFlags.Static|BindingFlags.NonPublic);if(valid)Assert.DoesNotThrow(()=>method.Invoke(null,new object[]{iterations,perFrame,cadence}));else{var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{iterations,perFrame,cadence}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());}
    }
    [Test]
    public void SharedCurrentInputAdapterCannotReuseHistoricalEvidenceOrSelectASavedTerrain()
    {
        var proof=Activator.CreateInstance(Integration.GetNestedType("PublicMaskedFeedbackEvidence"));proof.GetType().GetField("receipt").SetValue(proof,"ALREADY_RECORDED_CURRENT_INPUT");var routine=(IEnumerator)Integration.GetMethod("ExecutePublicMaskedCurrentFeedback",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{new float[1025,1025],new float[1025,1025],1000,10,50,"UNUSED_NO_FILES_WRITTEN",proof,null});var failure=Assert.Throws<InvalidOperationException>(()=>routine.MoveNext());Assert.That(failure.Message,Does.Contain("Fresh per-invocation"));Assert.That(Directory.Exists("UNUSED_NO_FILES_WRITTEN"),Is.False);
    }
    [TestCase("protected")][TestCase("belowDomain")][TestCase("aboveDomain")][TestCase("nonfinite")]
    public void EveryActualReadbackRequiresExactZeroCoreAndValidHeightDomainWithoutRepair(string mutation)
    {
        var input=new float[1025,1025];var mask=new float[1025,1025];var pixels=new float[1025*1025];for(int z=0;z<1025;z++)for(int x=0;x<1025;x++){input[z,x]=.25f;mask[z,x]=1;pixels[z*1025+x]=.25f;}mask[5,5]=0;int at=5*1025+5;switch(mutation){case "protected":pixels[at]+=.001f;break;case "belowDomain":pixels[at]=-.001f;break;case "aboveDomain":pixels[at]=1.001f;break;case "nonfinite":pixels[at]=float.NaN;break;}int unchanged=BitConverter.SingleToInt32Bits(pixels[at]);var proof=Integration.GetMethod("InspectMaskedSmoothPixels",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{pixels,input,mask,1,false});Assert.That((bool)Read(proof,"passed"),Is.False);Assert.That((int)Read(proof,"firstFailureIndex"),Is.EqualTo(at));Assert.That((int)Read(proof,"protectedChanged"),Is.EqualTo(1));Assert.That(BitConverter.SingleToInt32Bits(pixels[at]),Is.EqualTo(unchanged),"Guard must not clamp or pull protected values back");Assert.That(input[5,5],Is.EqualTo(.25f));
    }
    [Test]
    public void ExactInputIdentityIsRequiredBeforeTheFirstVendorSmoothIteration()
    {
        var input=new float[1025,1025];var mask=new float[1025,1025];var pixels=new float[1025*1025];for(int z=0;z<1025;z++)for(int x=0;x<1025;x++)mask[z,x]=1;pixels[12]=.01f;var method=Integration.GetMethod("InspectMaskedSmoothPixels",BindingFlags.Static|BindingFlags.NonPublic);var identity=method.Invoke(null,new object[]{pixels,input,mask,0,true});Assert.That((bool)Read(identity,"passed"),Is.False);Assert.That((bool)Read(identity,"fullInputIdentityExact"),Is.False);var ordinary=method.Invoke(null,new object[]{pixels,input,mask,1,false});Assert.That((bool)Read(ordinary,"passed"),Is.True,"Valid editable changes are allowed after iteration begins");
    }
    [UnityTest,Timeout(900000)]
    public IEnumerator ActualSavedV422NativeFieldsUsePublic1025MaskedFeedbackAndUnchangedAcceptanceGates()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"MaskedSmoothRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve incoming user scene before explicit masked vendor probe");}
        yield return (IEnumerator)Integration.GetMethod("CaptureMaskedBoundarySmooth").Invoke(null,null);var proof=Integration.GetProperty("LastMaskedBoundarySmoothReport").GetValue(null);foreach(string gate in new[]{"originalSourcesExact","originalSourceMemoryExact","ownedSelectionAndRandomRestored","vendorTransientResourcesRestored"})Assert.That((bool)Read(proof,gate),Is.True,gate);Assert.That((bool)Read(proof,"originalRoofRequirementRemainsSeparate"),Is.True);Assert.That((int)Read(proof,"sourceOriginalRoofFoundationMismatchesIncludingSharedSeams"),Is.EqualTo(2881),"Existing2851unique roof damage includes30 duplicated seam samples; this probe must not claim roof repair");var trials=((IEnumerable)Read(proof,"trials")).Cast<object>().ToArray();Assert.That(trials.Length,Is.EqualTo(((int[])Read(proof,"selectedIterations")).Length));
        foreach(var trial in trials)
        {
            Assert.That((string)Read(trial,"publicApi"),Is.EqualTo("Pinwheel.Vista.VistaLib.SmoothProgressive"));Assert.That((int)Read(trial,"canvasResolution"),Is.EqualTo(1025));Assert.That((int)Read(trial,"iterationPerFrame"),Is.EqualTo(10));Assert.That((int)Read(trial,"readbackEveryIterations"),Is.EqualTo((int)Read(trial,"iterations")>=500?50:10));string kernelPath=(string)Read(trial,"kernelReceipt");Assert.That(File.Exists(kernelPath),Is.True);using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(kernelPath))).Replace("-","").ToLowerInvariant(),Is.EqualTo((string)Read(trial,"kernelReceiptSha256")));var kernel=JsonUtility.FromJson(File.ReadAllText(kernelPath),Integration.GetNestedType("PublicMaskedFeedbackEvidence"));foreach(string kernelGate in new[]{"maskIdentityExact","everyGpuReadbackExact","inputArraysExact","sourceBytesExact","resourcesRestored"})Assert.That((bool)Read(kernel,kernelGate),Is.True,kernelGate);Assert.That((string)Read(kernel,"inputSha256"),Is.EqualTo((string)Read(proof,"inputMosaicSha256")));Assert.That((string)Read(kernel,"maskSha256"),Is.EqualTo((string)Read(proof,"maskSha256")));foreach(string gate in new[]{"actualInputQualityExact","actualMaskExact","everyGpuReadbackExact","allNativeProtectedSamplesExact","sourceGraphAndCriticalPosesExact","qualityLimitsPreserved"})Assert.That((bool)Read(trial,gate),Is.True,gate);var readbacks=((IEnumerable)Read(trial,"readbacks")).Cast<object>().ToArray();Assert.That(readbacks.Length,Is.GreaterThan(2));Assert.That((int)Read(readbacks[0],"iteration"),Is.EqualTo(0));Assert.That((bool)Read(readbacks[0],"fullInputIdentityExact"),Is.True);Assert.That((int)Read(readbacks.Last(),"iteration"),Is.EqualTo((int)Read(trial,"iterations")));foreach(var readback in readbacks){Assert.That((bool)Read(readback,"passed"),Is.True);Assert.That((int)Read(readback,"protectedChanged"),Is.EqualTo(0));Assert.That((int)Read(readback,"pixels"),Is.EqualTo(1025*1025));Assert.That(File.Exists((string)Read(readback,"rawFloatPath")),Is.True);}foreach(var field in (Array)Read(trial,"fields")){Assert.That((int)Read(field,"protectedChanged"),Is.EqualTo(0));Assert.That((bool)Read(field,"holesAlphaLayersExact"),Is.True);Assert.That((int)Read(field,"originalRoofFoundationMismatchAfter"),Is.EqualTo((int)Read(field,"originalRoofFoundationMismatchBefore")));}TestContext.WriteLine("ACTUAL_PUBLIC_MASKED_SMOOTH_NATIVE_RESULT_NOT_FULL_WORLD_OR_ROOF_ACCEPTANCE "+JsonUtility.ToJson(trial));
        }
        var arguments=Environment.GetCommandLineArgs();int referenceAt=Array.IndexOf(arguments,"-tsDragonMaskedSmoothReferenceReceipt");if(referenceAt>=0)
        {
            Assert.That(referenceAt+1<arguments.Length,Is.True);string referencePath=arguments[referenceAt+1];Assert.That(Path.IsPathRooted(referencePath)&&File.Exists(referencePath),Is.True);var reference=JsonUtility.FromJson(File.ReadAllText(referencePath),proof.GetType());Assert.That((bool)Read(reference,"originalSourcesExact")&&(bool)Read(reference,"originalSourceMemoryExact")&&(bool)Read(reference,"ownedSelectionAndRandomRestored"),Is.True);Assert.That(Read(reference,"inputMosaicSha256"),Is.EqualTo(Read(proof,"inputMosaicSha256")));Assert.That(Read(reference,"maskSha256"),Is.EqualTo(Read(proof,"maskSha256")));var oldTrials=((IEnumerable)Read(reference,"trials")).Cast<object>().ToArray();foreach(var trial in trials){var old=oldTrials.Single(t=>(int)Read(t,"iterations")== (int)Read(trial,"iterations"));var oldFields=((Array)Read(old,"fields")).Cast<object>().ToArray();foreach(var field in ((Array)Read(trial,"fields")).Cast<object>()){var original=oldFields.Single(f=>Path.GetFileName((string)Read(f,"candidate"))==Path.GetFileName((string)Read(field,"candidate")));Assert.That(Read(field,"actualHeightSha256"),Is.EqualTo(Read(original,"actualHeightSha256")),"Shared adapter must reproduce actual proven V426 native output on identical input/mask");}TestContext.WriteLine("SHARED_PUBLIC_ADAPTER_REPRODUCED_ACTUAL_REFERENCE_NATIVE_HEIGHT_SHA "+Read(trial,"profile"));}
        }
        Assert.That(trials.Any(t=>(string)Read(t,"status")=="BOUNDED_VENDOR_MASKED_SMOOTH_PASSED_REQUIRES_ORIGINAL_ROOF_AND_FULL_WORLD_NATIVE_ACCEPTANCE"),Is.True,"No public mask-feedback profile passed unchanged quality/mass/physical guards; retain measured failure and do not claim completion");
    }
}
#endif
