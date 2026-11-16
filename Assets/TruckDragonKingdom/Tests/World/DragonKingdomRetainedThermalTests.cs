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

public sealed class DragonKingdomRetainedThermalTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);
    static void Set(object value,string field,object replacement)=>value.GetType().GetField(field).SetValue(value,replacement);
    static object ActualReceipt()=>JsonUtility.FromJson(File.ReadAllText(Path.Combine(Arg("-tsDragonRetainedThermalSource"),"rendered-evidence","regional-world-OwnedRoadRockV417.json")),Integration.GetNestedType("Receipt"));
    static Array QualifiedChain(object receipt)=>(Array)Integration.GetMethod("QualifyRetainedThermalPhaseChain",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new[]{receipt});
    static string Arg(string key){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,key);return args[at+1];}
    [Test]
    public void MissingNativeRetainedEvidenceCannotBecomeAReconstructedTerrainFixture()
    {
        const string original="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";var before=File.ReadAllBytes(original);var values=new object[]{"",null};var failure=Assert.Throws<TargetInvocationException>(()=>Integration.GetMethod("RequireRetainedThermalReceipt",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,values));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("actual native run"));CollectionAssert.AreEqual(before,File.ReadAllBytes(original));
    }
    [Test,Timeout(120000)]
    public void ActualRetainedNativeHeightHashRejectsOneChangedSampleAndPreservesTheSource()
    {
        var receipt=ActualReceipt();var row=QualifiedChain(receipt).GetValue(0);string source=(string)Read(row,"terrain"),expected=(string)Read(row,"finalNativeHeightSha256");var before=File.ReadAllBytes(source);var data=AssetDatabase.LoadAssetAtPath<TerrainData>(source);Assert.That(data,Is.Not.Null);var method=Integration.GetMethod("RequireRetainedThermalHeight",BindingFlags.Static|BindingFlags.NonPublic);Assert.DoesNotThrow(()=>method.Invoke(null,new object[]{data,expected}));var clone=UnityEngine.Object.Instantiate(data);
        try
        {
            float value=clone.GetHeights(180,180,1,1)[0,0];clone.SetHeights(180,180,new float[,]{{value+.005f}});var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{clone,expected}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("final native height SHA"));Assert.DoesNotThrow(()=>method.Invoke(null,new object[]{data,expected}));
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);CollectionAssert.AreEqual(before,File.ReadAllBytes(source));}
    }
    [Test]
    public void ActualV417FinalNativeSourceRetainsEveryEarlierPhaseHashWithoutRelabellingSdkOrCoordinator()
    {
        var receipt=ActualReceipt();var rows=QualifiedChain(receipt).Cast<object>().ToArray();Assert.That(rows.Length,Is.EqualTo(4));
        foreach(var row in rows)
        {
            Assert.That((string)Read(row,"sourceRole"),Does.Contain("AFTER_NATIVE_COORDINATION_URBAN_BANKS_AND_MERCHANT_BERTH_NOT_RAW_SDK"));Assert.That((string)Read(row,"nativeCoordinatedHeightSha256"),Is.EqualTo((string)Read(row,"urbanBeforeHeightSha256")));Assert.That((string)Read(row,"urbanAfterHeightSha256"),Is.EqualTo((string)Read(row,"berthBeforeHeightSha256")));
            Assert.That((string)Read(row,"actualSdkRawHeightSha256"),Is.EqualTo((string)Read(row,"compositionReferenceHeightSha256")));Assert.That((int)Read(row,"resolution"),Is.EqualTo(513));Assert.That((Vector3)Read(row,"size"),Is.EqualTo(new Vector3(1600,600,1600)));
        }
        var southwest=rows.Single(r=>((string)Read(r,"terrain")).EndsWith("Kingdom 0 0.asset"));Assert.That((string)Read(southwest,"nativeCoordinatedHeightSha256"),Is.EqualTo("228ef9920e9956a2a445e994471bdd4b1dfe1d60b7d69604ec85a73ebbc8201c"));Assert.That((string)Read(southwest,"finalNativeHeightSha256"),Is.EqualTo("15c3fba4b0015fa4263441925e249a50df8719e4890b0b68b73a4715ef52b5db"));
        var southeast=rows.Single(r=>((string)Read(r,"terrain")).EndsWith("Kingdom 1 0.asset"));Assert.That((string)Read(southeast,"berthBeforeHeightSha256"),Is.EqualTo("5a007fa810a611bd235d6a01da711f4235b437173124c969aee31f53b67206fd"));Assert.That((string)Read(southeast,"finalNativeHeightSha256"),Is.EqualTo("dcde62e36b19d16e85a8c14e4964b4e5551e62a50e4d4177747108659876f77f"));
    }
    [TestCase("sdkReference")][TestCase("coordinatorLink")][TestCase("urbanBerthLink")][TestCase("urbanOutside")][TestCase("berthOutside")][TestCase("nativeMask")][TestCase("duplicateTile")][TestCase("missingTile")][TestCase("origin")][TestCase("shape")][TestCase("foundation")][TestCase("sourcePreservation")][TestCase("nativeProtected")][TestCase("urbanProtected")][TestCase("berthProtected")][TestCase("hullPreservation")][TestCase("sampleTotals")]
    public void ActualV417PhaseChainMutationCannotAuthorizeGpuOrChangeRetainedSource(string mutation)
    {
        string receiptPath=Path.Combine(Arg("-tsDragonRetainedThermalSource"),"rendered-evidence","regional-world-OwnedRoadRockV417.json");var bytes=File.ReadAllBytes(receiptPath);var receipt=ActualReceipt();Assert.DoesNotThrow(()=>QualifiedChain(receipt));var native=Read(receipt,"nativeRoadRelief");var urban=Read(receipt,"urbanBanks");var berth=Read(receipt,"berth");var n=((Array)Read(native,"tiles")).GetValue(0);var u=((Array)Read(urban,"tiles")).GetValue(0);var b=((Array)Read(berth,"tiles")).GetValue(0);const string different="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        switch(mutation)
        {
            case "sdkReference":Set(n,"compositionReferenceHeightSha256",different);break;
            case "coordinatorLink":Set(n,"coordinatedHeightSha256",different);break;
            case "urbanBerthLink":Set(b,"beforeHeightSha256",different);break;
            case "urbanOutside":Set(u,"afterOutsideSha256",different);break;
            case "berthOutside":Set(b,"afterOutsideSha256",different);break;
            case "nativeMask":Set(n,"authorizedMaskSha256","");break;
            case "duplicateTile":Set(((Array)Read(berth,"tiles")).GetValue(1),"terrain",Read(b,"terrain"));break;
            case "missingTile":var tiles=(Array)Read(urban,"tiles");var fewer=Array.CreateInstance(tiles.GetType().GetElementType(),3);Array.Copy(tiles,fewer,3);Set(urban,"tiles",fewer);break;
            case "origin":Set(u,"origin",(Vector3)Read(u,"origin")+Vector3.right);break;
            case "shape":Set(u,"resolution",257);break;
            case "foundation":Set(((Array)Read(receipt,"terrains")).GetValue(0),"foundation","Assets/Invented.asset");break;
            case "sourcePreservation":Set(receipt,"sourceBytesPreserved",false);break;
            case "nativeProtected":Set(n,"validOriginalStartExtractionContactsExact",false);break;
            case "urbanProtected":Set(urban,"roadsAndPosesExact",false);break;
            case "berthProtected":Set(berth,"ferryBedExact",false);break;
            case "hullPreservation":Set(Read(berth,"actualHull"),"sourceBytesExact",false);break;
            case "sampleTotals":Set(urban,"changedSamples",(int)Read(urban,"changedSamples")+1);break;
            default:Assert.Fail("Unimplemented mutation control");break;
        }
        var failure=Assert.Throws<TargetInvocationException>(()=>QualifiedChain(receipt));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("phase chain rejected"));Assert.That(failure.InnerException.Message,Does.Contain("No thermal GPU work permitted"));CollectionAssert.AreEqual(bytes,File.ReadAllBytes(receiptPath));Assert.DoesNotThrow(()=>QualifiedChain(ActualReceipt()));
    }
    static object VistaInputAuthority(object chain,string receiptPath)
    {
        var authority=Activator.CreateInstance(Integration.GetNestedType("NativeVistaInputAuthority"));Set(authority,"terrain",Read(chain,"terrain"));Set(authority,"observedInputRole",Read(chain,"sourceRole"));Set(authority,"expectedObservedHeightSha256",Read(chain,"finalNativeHeightSha256"));Set(authority,"actualSdkRawHeightSha256",Read(chain,"actualSdkRawHeightSha256"));Set(authority,"sourceReceipt",receiptPath);using(var sha=SHA256.Create())Set(authority,"sourceReceiptSha256",BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(receiptPath))).Replace("-","").ToLowerInvariant());return authority;
    }
    static object VistaInputEvidence(object chain,object authority,bool rawSdk=false)=>Integration.GetMethod("CreateNativeVistaInputEvidence",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new[]{Read(chain,"terrain"),Read(chain,rawSdk?"actualSdkRawHeightSha256":"finalNativeHeightSha256"),authority});
    [Test]
    public void ActualV417FinalStageSchemaKeepsObservedHashRawSdkAndLegacyAliasDistinct()
    {
        string receiptPath=Path.Combine(Arg("-tsDragonRetainedThermalSource"),"rendered-evidence","regional-world-OwnedRoadRockV417.json");var chains=QualifiedChain(ActualReceipt()).Cast<object>().ToArray();var typedRows=Array.CreateInstance(Integration.GetNestedType("NativeVistaPostGradeTile"),4);
        for(int i=0;i<4;i++)
        {
            var chain=chains[i];var row=VistaInputEvidence(chain,VistaInputAuthority(chain,receiptPath));typedRows.SetValue(row,i);Assert.That((string)Read(row,"observedInputRole"),Is.EqualTo((string)Read(chain,"sourceRole")));Assert.That((string)Read(row,"observedInputHeightSha256"),Is.EqualTo((string)Read(chain,"finalNativeHeightSha256")));Assert.That((string)Read(row,"actualSdkRawHeightSha256"),Is.EqualTo((string)Read(chain,"actualSdkRawHeightSha256")));Assert.That((bool)Read(row,"actualSdkRawHeightKnown"),Is.True);Assert.That((string)Read(row,"actualSdkInputSha256"),Is.EqualTo((string)Read(row,"observedInputHeightSha256")));Assert.That((string)Read(row,"legacyActualSdkInputMeaning"),Does.Contain("NOT_PROOF_OF_RAW_SDK"));var reloaded=JsonUtility.FromJson(JsonUtility.ToJson(row),row.GetType());foreach(string field in new[]{"observedInputRole","observedInputHeightSha256","actualSdkRawHeightSha256","actualSdkInputSha256","legacyActualSdkInputMeaning","sourceReceiptSha256"})Assert.That(Read(reloaded,field),Is.EqualTo(Read(row,field)),field);
        }
        var evidence=Activator.CreateInstance(Integration.GetNestedType("NativeVistaPostGradeEvidence"));const string metadataOnlyMosaicHash="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";Integration.GetMethod("RecordNativeVistaInputMosaic",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{evidence,typedRows,metadataOnlyMosaicHash});Assert.That((int)Read(evidence,"evidenceSchema"),Is.EqualTo(2));Assert.That((string)Read(evidence,"observedInputMosaicSha256"),Is.EqualTo(metadataOnlyMosaicHash));Assert.That((string)Read(evidence,"actualSdkMosaicSha256"),Is.EqualTo(metadataOnlyMosaicHash));Assert.That((bool)Read(evidence,"actualSdkRawMosaicKnown"),Is.False,"Four historical raw tile hashes cannot reconstruct raw mosaic bytes");Assert.That((string)Read(evidence,"actualSdkRawMosaicSha256"),Is.Empty);Assert.That((string)Read(evidence,"legacyActualSdkMosaicMeaning"),Does.Contain("NOT_PROOF_OF_RAW_SDK"));
    }
    [Test]
    public void ActualSdkSchemaRetainsCompatibleRawInputAliasWithoutChangingAuthoredProfile()
    {
        var chains=QualifiedChain(ActualReceipt()).Cast<object>().ToArray();var rows=Array.CreateInstance(Integration.GetNestedType("NativeVistaPostGradeTile"),4);for(int i=0;i<4;i++){var row=VistaInputEvidence(chains[i],null,true);rows.SetValue(row,i);Assert.That((string)Read(row,"observedInputRole"),Is.EqualTo("ACTUAL_SDK_RAW_BEFORE_NATIVE_COORDINATOR"));Assert.That((string)Read(row,"observedInputHeightSha256"),Is.EqualTo((string)Read(row,"actualSdkRawHeightSha256")));Assert.That((string)Read(row,"actualSdkInputSha256"),Is.EqualTo((string)Read(row,"actualSdkRawHeightSha256")));}var evidence=Activator.CreateInstance(Integration.GetNestedType("NativeVistaPostGradeEvidence"));const string metadataOnlyMosaicHash="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";Integration.GetMethod("RecordNativeVistaInputMosaic",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{evidence,rows,metadataOnlyMosaicHash});Assert.That((bool)Read(evidence,"actualSdkRawMosaicKnown"),Is.True);Assert.That((string)Read(evidence,"actualSdkRawMosaicSha256"),Is.EqualTo(metadataOnlyMosaicHash));Assert.That((string)Read(evidence,"profile"),Is.EqualTo("NativeErodedMountainRange01"));
    }
    [TestCase("relabelFinalAsRaw")][TestCase("observedMismatch")][TestCase("unknownRole")][TestCase("missingReceipt")]
    public void ActualFinalStageCannotBeRelabelledRawSdkOrLoseItsInputAuthority(string mutation)
    {
        string path=Path.Combine(Arg("-tsDragonRetainedThermalSource"),"rendered-evidence","regional-world-OwnedRoadRockV417.json");var chain=QualifiedChain(ActualReceipt()).GetValue(0);var authority=VistaInputAuthority(chain,path);Assert.That((string)Read(chain,"finalNativeHeightSha256"),Is.Not.EqualTo((string)Read(chain,"actualSdkRawHeightSha256")));switch(mutation){case "relabelFinalAsRaw":Set(authority,"observedInputRole","ACTUAL_SDK_RAW_BEFORE_NATIVE_COORDINATOR");break;case "observedMismatch":Set(authority,"expectedObservedHeightSha256",Read(chain,"nativeCoordinatedHeightSha256"));break;case "unknownRole":Set(authority,"observedInputRole","UNQUALIFIED_REPAINTED_TERRAIN");break;case "missingReceipt":Set(authority,"sourceReceipt","");break;default:Assert.Fail("Missing mutation");break;}var failure=Assert.Throws<TargetInvocationException>(()=>VistaInputEvidence(chain,authority));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [TestCase("NativeThermal1000x200SlopeRepair01",1000,true,"1000")][TestCase("NativeThermal2000x200SlopeRepair01",2000,true,"2000")][TestCase("NativeThermal1000x200SingleResolutionSlopeRepair01",1000,false,"1000")][TestCase("NativeThermal2000x200SingleResolutionSlopeRepair01",2000,false,"2000")]
    public void PublicThermalProfileFamilyKeepsIterationRateTalusResolutionAndOutputLabelExact(string profile,int iterations,bool multi,string label)
    {
        var node=Integration.GetMethod("ConfiguredNativeThermalNode",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{profile});Assert.That((int)node.GetType().GetProperty("iterationCount").GetValue(node),Is.EqualTo(iterations));Assert.That((float)node.GetType().GetProperty("erosionRate").GetValue(node),Is.EqualTo(200));Assert.That((float)node.GetType().GetProperty("talusAngle").GetValue(node),Is.EqualTo(45));Assert.That((bool)node.GetType().GetProperty("useMultiResolution").GetValue(node),Is.EqualTo(multi));Assert.That((string)Integration.GetMethod("NativeThermalOutputLabel",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{profile}),Is.EqualTo(label));Assert.That(JsonUtility.ToJson(node),Does.Contain("\"m_useMultiResolution\":"+(multi?"true":"false")));
    }
    [TestCase("SingleResolution01","1000",1)][TestCase("SingleResolution01","2000",1)][TestCase("SingleResolution01","Both",2)][TestCase("MultiResolution01","Both",2)]
    public void ExplicitBoundedThermalFamilySelectsOnlyRequestedProfiles(string family,string iterations,int count)
    {
        var profiles=(string[])Integration.GetMethod("RetainedThermalProfiles",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{family,iterations});Assert.That(profiles.Length,Is.EqualTo(count));foreach(string profile in profiles){Assert.That(profile.Contains("SingleResolution"),Is.EqualTo(family=="SingleResolution01"));if(iterations!="Both")Assert.That(profile,Does.Contain("Thermal"+iterations+"x200"));}
    }
    [Test]
    public void UnknownThermalFamilyCannotSilentlyFallBackToKnownBadMultiResolution()
    {
        var failure=Assert.Throws<TargetInvocationException>(()=>Integration.GetMethod("RetainedThermalProfiles",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"AutomaticGuess","Both"}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    static object RecordedMultiResolutionDomainFailure()
    {
        string path=Arg("-tsDragonThermalDomainFailureReceipt");Assert.That(Path.IsPathRooted(path)&&File.Exists(path),Is.True,"Explicit immutable actual V420 public multiresolution failure receipt required");var proof=JsonUtility.FromJson(File.ReadAllText(path),Integration.GetNestedType("NativeVistaPostGradeEvidence"));Assert.That((string)Read(proof,"profile"),Is.EqualTo("NativeThermal1000x200SlopeRepair01"));Assert.That((string)Read(proof,"nodeJson"),Does.Contain("\"m_useMultiResolution\":true"));Assert.That((bool)Read(proof,"sourceBytesExact")&&(bool)Read(proof,"graphAndCriticalPosesExact"),Is.True);Assert.That((int)Read(proof,"changedSamples"),Is.EqualTo(0),"Actual rejected GPU output must never have reached native SetHeights");string raw=(string)Read(proof,"actualGpuFloatPath"),resampled=(string)Read(proof,"resampledVendorFloatPath");Assert.That(Path.IsPathRooted(raw)&&Path.IsPathRooted(resampled)&&File.Exists(raw)&&File.Exists(resampled),Is.True);using(var sha=SHA256.Create()){var rawBytes=File.ReadAllBytes(raw);Assert.That(BitConverter.ToString(sha.ComputeHash(rawBytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo((string)Read(proof,"rawVendorOutputSha256")));var rawValues=new float[rawBytes.Length/4];Buffer.BlockCopy(rawBytes,0,rawValues,0,rawBytes.Length);Assert.That(rawValues.All(float.IsFinite),Is.True);Assert.That(rawValues.Count(v=>v<0),Is.EqualTo(407));Assert.That(rawValues.Count(v=>v>1),Is.EqualTo(0));var bytes=File.ReadAllBytes(resampled);Assert.That(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo((string)Read(proof,"resampledVendorOutputSha256")));var values=new float[bytes.Length/4];Buffer.BlockCopy(bytes,0,values,0,bytes.Length);Assert.That(values.All(float.IsFinite),Is.True);Assert.That(values.Count(v=>v<0),Is.EqualTo(407));Assert.That(values.Count(v=>v>1),Is.EqualTo(0));}return proof;
    }
    [Test]
    public void ActualArchivedMultiResolutionDomainFailureRetainsIts407NegativeRawWitnesses()
    {var proof=RecordedMultiResolutionDomainFailure();Assert.That((int)Read(proof,"actualOutputWidth"),Is.EqualTo(1032));Assert.That((bool)Read(proof,"publicThermalNodeConfiguredExact"),Is.True);}
    [UnityTest,Timeout(900000)]
    public IEnumerator ActualV417RetainedFieldsRunPublicThermalProfilesUnderUnchangedQualityAndProtectedSourceGates()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"RetainedThermalRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Save incoming user scenes before this explicit retained-field probe.");}
        yield return (IEnumerator)Integration.GetMethod("CaptureRetainedCoordinatedThermalRepair").Invoke(null,null);var proof=Integration.GetProperty("LastRetainedThermalReport").GetValue(null);
        foreach(string field in new[]{"originalSourcesExact","retainedNativeHeightsQualified","recordedFootingSourcesExact","sourceSceneAndTerrainMemoryExact","ownedSelectionAndRandomRestored"})Assert.That((bool)Read(proof,field),Is.True,field);Assert.That((int)Read(proof,"originalSteep65"),Is.EqualTo(20464));Assert.That((int)Read(proof,"originalSteep80"),Is.EqualTo(4492));Assert.That((int)Read(proof,"individualFootings"),Is.EqualTo(550));Assert.That((string)Read(proof,"sourceRole"),Does.Contain("NOT_RAW_SDK"));Assert.That(((Array)Read(proof,"phaseChains")).Length,Is.EqualTo(4));var trials=((IEnumerable)Read(proof,"trials")).Cast<object>().ToArray();Assert.That(trials.Length,Is.EqualTo(((string[])Read(proof,"selectedProfiles")).Length));
        foreach(var trial in trials)
        {
            Assert.That((bool)Read(trial,"baselineExact"),Is.True);Assert.That((bool)Read(trial,"allProtectedSamplesExact"),Is.True);Assert.That((bool)Read(trial,"originalQualityLimitsPreserved"),Is.True);var native=Read(trial,"vendor");Assert.That((bool)Read(native,"publicThermalNodeConfiguredExact"),Is.True);Assert.That((int)Read(native,"evidenceSchema"),Is.EqualTo(2));Assert.That((string)Read(native,"observedInputRole"),Does.Contain("NOT_RAW_SDK"));Assert.That((bool)Read(native,"actualSdkRawMosaicKnown"),Is.False);foreach(var field in ((Array)Read(native,"tiles")).Cast<object>()){Assert.That((string)Read(field,"actualSdkInputSha256"),Is.EqualTo((string)Read(field,"observedInputHeightSha256")));Assert.That((string)Read(field,"actualSdkRawHeightSha256"),Is.Not.Empty);}Assert.That((bool)Read(native,"authoredNodeExact"),Is.False,"Public configured Thermal3 is distinct from the unchanged authored200iteration Erosion sample.");Assert.That((int)Read(native,"changedSamples"),Is.GreaterThan(0));TestContext.WriteLine((string)Read(trial,"profile")+" actual quality="+JsonUtility.ToJson(Read(trial,"after"))+" peak="+Read(trial,"maximumHeight")+" massRetained="+Read(trial,"highlandMassAndPeakRetained"));
        }
        if((string)Read(proof,"family")=="SingleResolution01")
        {
            var rejected=RecordedMultiResolutionDomainFailure();foreach(var trial in trials){var native=Read(trial,"vendor");Assert.That((string)Read(native,"nodeJson"),Does.Contain("\"m_useMultiResolution\":false"));Assert.That((string)Read(native,"observedInputMosaicSha256"),Is.EqualTo((string)Read(rejected,"actualSdkMosaicSha256")),"Causal control must use identical actual final native input");Assert.That((string)Read(native,"hardnessEditableWeightSha256"),Is.EqualTo((string)Read(rejected,"hardnessEditableWeightSha256")),"Causal control must preserve identical actual physical mask");Assert.That((string)Read(native,"rawVendorOutputSha256"),Is.Not.EqualTo((string)Read(rejected,"rawVendorOutputSha256")));TestContext.WriteLine("ACTUAL_SINGLE_RESOLUTION_CAUSAL_CONTROL_SAME_INPUT_AND_MASK_NO_DOMAIN_CLAMP "+Read(trial,"profile"));}
        }
        Assert.That(trials.Any(t=>(string)Read(t,"status")=="BOUNDED_RETAINED_FIELDS_QUALITY_PASSED_REQUIRES_FULL_WORLD_AND_RENDER_ACCEPTANCE"),Is.True,"Neither measured thermal profile passes the unchanged quality/protected/mass gates; inspect retained native data without claiming a repair.");Assert.That((string)Read(proof,"scope"),Does.Contain("NOT_FULL_WORLD"));
    }
}
#endif
