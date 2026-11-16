#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class DragonKingdomCurrentFinalTerrainRecipeTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);
    static void Set(object value,string field,object content)=>value.GetType().GetField(field).SetValue(value,content);
    static object New(string name)=>Activator.CreateInstance(Integration.GetNestedType(name,BindingFlags.Public|BindingFlags.NonPublic));
    static MethodInfo Method(string name)=>Integration.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
    static object[] Rows(object owner,string field)=>((Array)Read(owner,field)).Cast<object>().ToArray();
    static void ArrayField(object owner,string field,params object[] rows){var array=Array.CreateInstance(owner.GetType().GetField(field).FieldType.GetElementType(),rows.Length);for(int i=0;i<rows.Length;i++)array.SetValue(rows[i],i);Set(owner,field,array);}
    static void True(object owner,params string[] fields){foreach(var field in fields)Set(owner,field,true);}
    static string Digest(char c)=>new string(c,64);
    static object Fixture()
    {
        var receipt=New("Receipt");Set(receipt,"tag","CurrentFinalContract01");Set(receipt,"terrainCoordinationMode","VendorGradedRelief01");
        var native=New("NativeRoadReliefEvidence");True(native,"passed","actualVendorGradeCapturedBeforeCoordination","sourceRoadPosesAndGraphUnchanged","wholeTownAndGroveBoundingBoxesNotConstraints");Set(receipt,"nativeRoadRelief",native);
        var urban=New("UrbanBankProof");True(urban,"passed","neverLowered","outsideMaskExact","waterCoreExact","roadsAndPosesExact","seamsExact");Set(receipt,"urbanBanks",urban);
        var berth=New("MooringBerth");True(berth,"passed","neverRaised","outsideMaskExact","seamsExact","shoreExact","ferryBedExact");Set(berth,"policy","NATIVE_ACTUAL_SOURCE_HULL_25_KEEL_CLEARANCES_AND_53_CONSERVATIVE_FERRY_SWEEPS");Set(receipt,"berth",berth);
        var hull=New("BerthHullClearance");True(hull,"passed","sourceBytesExact","departureClear");Set(hull,"depthSamples",25);Set(hull,"departureSweepSamples",53);Set(hull,"minimumKeelClearance",3f);Set(hull,"source","CONTRACT_ONLY_NO_NATIVE_ASSET_LOADED");Set(hull,"sourceGuid","CONTRACT_ONLY");foreach(string field in new[]{"sourceSha256","sourceMetaSha256","actualGeometrySha256"})Set(hull,field,Digest('a'));Set(berth,"actualHull",hull);
        var architecture=New("NativeFinishedIntegrationEvidence");True(architecture,"inventoryIdentityPreserved");Set(architecture,"totalIndividualFootings",550);Set(receipt,"nativeArchitectureIntegration",architecture);
        var records=new object[4];var ns=new object[4];var us=new object[4];var bs=new object[4];int i=0;
        for(int z=0;z<2;z++)for(int x=0;x<2;x++,i++)
        {
            string terrain="Assets/TruckDragonKingdom/VisualCandidates/CurrentFinalContract01/Terrain/Kingdom "+x+" "+z+".asset";
            records[i]=New("TileRecord");Set(records[i],"candidate",terrain);Set(records[i],"foundation","CONTRACT_FOUNDATION_"+i);Set(records[i],"resolution",513);
            ns[i]=New("NativeReliefTile");Set(ns[i],"terrain",terrain);Set(ns[i],"vendorGradedHeightSha256",Digest('a'));Set(ns[i],"compositionReferenceHeightSha256",Digest('a'));Set(ns[i],"coordinatedHeightSha256",Digest('b'));Set(ns[i],"authorizedMaskSha256",Digest('c'));Set(ns[i],"districtRaisedMaskSha256",Digest('d'));True(ns[i],"outsideDeclaredCoordinationAreasExact","validOriginalStartExtractionContactsExact","nativeUndergroundSamplesExact");
            us[i]=New("UrbanBankTile");Set(us[i],"terrain",terrain);Set(us[i],"beforeHeightSha256",Digest('b'));Set(us[i],"afterHeightSha256",Digest('b'));Set(us[i],"beforeOutsideSha256",Digest('c'));Set(us[i],"afterOutsideSha256",Digest('c'));Set(us[i],"afterBankSha256",Digest('d'));Set(us[i],"outsideSamples",513*513);Set(us[i],"resolution",513);Set(us[i],"origin",new Vector3(-1600+x*1600,-20,-800+z*1600));Set(us[i],"size",new Vector3(1600,600,1600));
            bs[i]=New("BerthTile");Set(bs[i],"terrain",terrain);Set(bs[i],"beforeHeightSha256",Digest('b'));Set(bs[i],"afterHeightSha256",Digest('b'));Set(bs[i],"beforeOutsideSha256",Digest('c'));Set(bs[i],"afterOutsideSha256",Digest('c'));Set(bs[i],"outsideSamples",513*513);
        }
        ArrayField(receipt,"terrains",records);ArrayField(native,"tiles",ns);ArrayField(urban,"tiles",us);ArrayField(berth,"tiles",bs);Set(urban,"outsideSamples",4*513*513);Set(berth,"outsideSamples",4*513*513);return receipt;
    }
    [Test]
    public void MissingFlagLeavesEveryExistingDefaultPathUnchanged()
    {Assert.That((bool)Method("RequireCurrentFinalTerrainRecipeSelection").Invoke(null,new object[]{Array.Empty<string>(),"LegacyContinuousEnvelope",false,false,false,false}),Is.False);}
    [TestCase("missing")][TestCase("unknown")][TestCase("duplicate")][TestCase("legacy")][TestCase("architecture")][TestCase("art")][TestCase("priorVista")][TestCase("falloff")]
    public void InvalidOrMixedRecipeCannotReachSdkOrGpu(string mutation)
    {
        string flag="-tsDragonFinalTerrainRecipe",profile="CurrentFinalThermalAndFeedback1000v1";var args=new[]{flag,profile};string mode="VendorGradedRelief01";bool architecture=true,art=true,prior=false,falloff=false;
        switch(mutation){case "missing":args=new[]{flag};break;case "unknown":args=new[]{flag,"AnythingElse"};break;case "duplicate":args=new[]{flag,profile,flag,profile};break;case "legacy":mode="LegacyContinuousEnvelope";break;case "architecture":architecture=false;break;case "art":art=false;break;case "priorVista":prior=true;break;case "falloff":falloff=true;break;}
        var error=Assert.Throws<TargetInvocationException>(()=>Method("RequireCurrentFinalTerrainRecipeSelection").Invoke(null,new object[]{args,mode,architecture,art,prior,falloff}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void ExplicitCurrentRecipePreservesCompletedCurrentPhaseHashesWithoutClaimingHistoricalSourceAcceptance()
    {
        var receipt=Fixture();Assert.That((bool)Read(receipt,"sourceBytesPreserved"),Is.False,"Outer source guard runs later in the live world finally");var rows=((Array)Method("QualifyCurrentFinalTerrainPhases").Invoke(null,new[]{receipt})).Cast<object>().ToArray();Assert.That(rows.Length,Is.EqualTo(4));foreach(var row in rows){Assert.That(Read(row,"actualSdkRawHeightSha256"),Is.EqualTo(Digest('a')));Assert.That(Read(row,"finalNativeHeightSha256"),Is.EqualTo(Digest('b')));Assert.That((string)Read(row,"terrain"),Does.Contain("CurrentFinalContract01"));Assert.That((string)Read(row,"sourceRole"),Is.EqualTo("ACTUAL_CURRENT_WORLD_FINAL_NATIVE_COORDINATOR_URBAN_BANKS_MERCHANT_BERTH_NOT_RAW_SDK"));}
        Assert.That((bool)Method("RequireCurrentFinalTerrainRecipeSelection").Invoke(null,new object[]{new[]{"-tsDragonFinalTerrainRecipe","CurrentFinalThermalAndFeedback1000v1"},"VendorGradedRelief01",true,true,false,false}),Is.True);
    }
    [TestCase("coordinatorLink")][TestCase("berthLink")][TestCase("sdkAlias")][TestCase("outside")][TestCase("duplicate")][TestCase("origin")][TestCase("frame")][TestCase("sourceMask")][TestCase("footings")][TestCase("hull")][TestCase("historicalStatus")][TestCase("changedCount")]
    public void CurrentPhaseMutationIsRejectedBeforeAnyVendorWork(string mutation)
    {
        var receipt=Fixture();var native=Read(receipt,"nativeRoadRelief");var urban=Read(receipt,"urbanBanks");var berth=Read(receipt,"berth");var n=Rows(native,"tiles");var u=Rows(urban,"tiles");var b=Rows(berth,"tiles");
        switch(mutation){case "coordinatorLink":Set(n[0],"coordinatedHeightSha256",Digest('e'));break;case "berthLink":Set(b[0],"beforeHeightSha256",Digest('e'));break;case "sdkAlias":Set(n[0],"compositionReferenceHeightSha256",Digest('b'));break;case "outside":Set(u[0],"afterOutsideSha256",Digest('e'));break;case "duplicate":Set(b[0],"terrain",Read(b[1],"terrain"));break;case "origin":Set(u[0],"origin",Read(u[1],"origin"));break;case "frame":Set(u[0],"size",new Vector3(1600,500,1600));break;case "sourceMask":Set(native,"sourceRoadPosesAndGraphUnchanged",false);break;case "footings":Set(Read(receipt,"nativeArchitectureIntegration"),"totalIndividualFootings",549);break;case "hull":Set(Read(berth,"actualHull"),"sourceBytesExact",false);break;case "historicalStatus":Set(receipt,"status","REJECTED_CANDIDATE");break;case "changedCount":Set(u[0],"changedSamples",1);break;}
        var error=Assert.Throws<TargetInvocationException>(()=>Method("QualifyCurrentFinalTerrainPhases").Invoke(null,new[]{receipt}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("before GPU"));
    }
    [Test]
    public void CurrentFinalInputHasItsOwnTruthfulRoleAndDistinctActualRawSdkReference()
    {
        const string role="ACTUAL_CURRENT_WORLD_FINAL_NATIVE_COORDINATOR_URBAN_BANKS_MERCHANT_BERTH_NOT_RAW_SDK";var authority=New("NativeVistaInputAuthority");Set(authority,"terrain","CURRENT_FIELD");Set(authority,"observedInputRole",role);Set(authority,"expectedObservedHeightSha256",Digest('b'));Set(authority,"actualSdkRawHeightSha256",Digest('a'));Set(authority,"sourceReceipt","CURRENT_CHECKPOINT");Set(authority,"sourceReceiptSha256",Digest('c'));var evidence=Method("CreateNativeVistaInputEvidence").Invoke(null,new[]{"CURRENT_FIELD",Digest('b'),authority});Assert.That(Read(evidence,"observedInputRole"),Is.EqualTo(role));Assert.That(Read(evidence,"actualSdkRawHeightSha256"),Is.EqualTo(Digest('a')));Assert.That(Read(evidence,"observedInputHeightSha256"),Is.EqualTo(Digest('b')));Assert.That(Read(evidence,"actualSdkInputSha256"),Is.EqualTo(Digest('b')),"Legacy field remains an explicitly labelled observed-input alias");
        Set(authority,"observedInputRole","ACTUAL_SDK_RAW_BEFORE_NATIVE_COORDINATOR");var failure=Assert.Throws<TargetInvocationException>(()=>Method("CreateNativeVistaInputEvidence").Invoke(null,new[]{"CURRENT_FIELD",Digest('b'),authority}));Assert.That(failure.InnerException.Message,Does.Contain("later composition"));
    }
    [Test]
    public void OmittedOriginalRoofFlagsDoNotLoadATokenOrChangeTheDefaultRecipe()
    {Assert.That(Method("RequireCurrentFinalOriginalRoofSelection").Invoke(null,new object[]{Array.Empty<string>()}),Is.Null);Assert.That(Integration.GetField("currentFinalOriginalRoofAuthority",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null),Is.Null);}
    [TestCase("profileOnly")][TestCase("receiptOnly")][TestCase("testsOnly")][TestCase("unknown")][TestCase("duplicate")][TestCase("relativeReceipt")][TestCase("relativeTests")]
    public void IncompleteUnknownOrAmbiguousRoofProfileCannotLoadAnyAuthority(string mutation)
    {
        var args=new[]{"-tsDragonFinalOriginalRoof","CavernRoadColliderRoof01","-tsDragonFinalOriginalRoofReceipt",@"F:\ActualNative\receipt.json","-tsDragonFinalOriginalRoofTests",@"F:\ActualNative\tests.xml"};switch(mutation){case "profileOnly":args=args.Take(2).ToArray();break;case "receiptOnly":args=args.Skip(2).Take(2).ToArray();break;case "testsOnly":args=args.Skip(4).ToArray();break;case "unknown":args[1]="AnyCavern";break;case "duplicate":args=args.Concat(args.Take(2)).ToArray();break;case "relativeReceipt":args[3]="receipt.json";break;case "relativeTests":args[5]="tests.xml";break;}
        var e=Assert.Throws<TargetInvocationException>(()=>Method("RequireCurrentFinalOriginalRoofSelection").Invoke(null,new object[]{args}));Assert.That(e.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void AnOriginalRoofSelectionRequiresTheCurrentRecipeAndKeepsAllThreeExplicitNativePaths()
    {
        var args=new[]{"-tsDragonFinalOriginalRoof","CavernRoadColliderRoof01","-tsDragonFinalOriginalRoofReceipt",@"F:\ActualNative\receipt.json","-tsDragonFinalOriginalRoofTests",@"F:\ActualNative\tests.xml"};var selection=Method("RequireCurrentFinalOriginalRoofSelection").Invoke(null,new object[]{args});Assert.That(Read(selection,"profile"),Is.EqualTo(args[1]));Assert.That(Read(selection,"receipt"),Is.EqualTo(args[3]));Assert.That(Read(selection,"tests"),Is.EqualTo(args[5]));Assert.That(Assert.Throws<TargetInvocationException>(()=>Method("RequireCurrentFinalTerrainRecipeSelection").Invoke(null,new object[]{args,"VendorGradedRelief01",true,true,false,false})).InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [TestCase("missingBefore")][TestCase("missingFoundation")][TestCase("missingRoofReceipt")][TestCase("oldBeforeRoofLabel")][TestCase("rawSdkLabel")]
    public void PostRoofObservedInputRequiresDistinctTruthfulCheckpointAndRawSdkAuthority(string mutation)
    {
        var a=New("NativeVistaInputAuthority");Set(a,"terrain","CURRENT_FIELD");Set(a,"observedInputRole","ACTUAL_CURRENT_WORLD_FINAL_THEN_QUALIFIED_ORIGINAL_ROOF_SOURCE_PROTECTION_NOT_RAW_SDK");Set(a,"expectedObservedHeightSha256",Digest('c'));Set(a,"actualSdkRawHeightSha256",Digest('a'));Set(a,"sourceReceipt","ACTUAL_POST_ROOF_CHECKPOINT");Set(a,"sourceReceiptSha256",Digest('e'));Set(a,"originalFinalHeightSha256",Digest('b'));Set(a,"originalRoofFoundationHeightSha256",Digest('d'));Set(a,"roofProtectionReceipt","ACTUAL_POST_ROOF_CHECKPOINT");Set(a,"roofProtectionReceiptSha256",Digest('e'));
        var valid=Method("CreateNativeVistaInputEvidence").Invoke(null,new[]{"CURRENT_FIELD",Digest('c'),a});Assert.That(Read(valid,"observedInputHeightSha256"),Is.EqualTo(Digest('c')));Assert.That(Read(valid,"originalFinalHeightSha256"),Is.EqualTo(Digest('b')));Assert.That(Read(valid,"actualSdkRawHeightSha256"),Is.EqualTo(Digest('a')));Assert.That(Read(valid,"originalRoofFoundationHeightSha256"),Is.EqualTo(Digest('d')));
        switch(mutation){case "missingBefore":Set(a,"originalFinalHeightSha256","");break;case "missingFoundation":Set(a,"originalRoofFoundationHeightSha256","");break;case "missingRoofReceipt":Set(a,"roofProtectionReceipt","");break;case "oldBeforeRoofLabel":Set(a,"observedInputRole","ACTUAL_CURRENT_WORLD_FINAL_NATIVE_COORDINATOR_URBAN_BANKS_MERCHANT_BERTH_NOT_RAW_SDK");break;case "rawSdkLabel":Set(a,"observedInputRole","ACTUAL_SDK_RAW_BEFORE_NATIVE_COORDINATOR");break;}
        Assert.That(Assert.Throws<TargetInvocationException>(()=>Method("CreateNativeVistaInputEvidence").Invoke(null,new[]{"CURRENT_FIELD",Digest('c'),a})).InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [UnityTest,Timeout(1800000)]
    public IEnumerator ActualCurrentWorldRunsExplicitRecipeThenUnchangedOriginalRoofAndFullWorldGates()
    {
        var args=Environment.GetCommandLineArgs();Assert.That(args.Contains("-tsDragonFinalTerrainRecipe"),Is.True,"Select this native fullworld case only with the explicit current recipe and existing qualified authoring flags");const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"CurrentFinalRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve incoming user scenes");}
        yield return (IEnumerator)Integration.GetMethod("CreateRoundedReviewCandidate",Type.EmptyTypes).Invoke(null,null);var receipt=Integration.GetProperty("LastReceipt").GetValue(null);var proof=Read(receipt,"currentFinalTerrainRecipe");Assert.That(proof,Is.Not.Null);foreach(string gate in new[]{"exactNativePhysicalCores","layersAlphaHolesExact","originalSourcesExact","originalMemoryExact","graphAndCriticalPosesExact","highlandMassAndPeakRetained","qualityLimitsUnchanged"})Assert.That((bool)Read(proof,gate),Is.True,gate);Assert.That((bool)Read(proof,"roofAuthorityContextRestored"),Is.True);Assert.That((bool)Read(proof,"roofAuthoritySourcesExact"),Is.True);if(args.Contains("-tsDragonFinalOriginalRoof")){Assert.That((bool)Read(proof,"qualifiedOriginalRoofApplied"),Is.True);Assert.That((int)Read(proof,"originalRoofMismatchesAfter"),Is.Zero);Assert.That((int)Read(proof,"otherProtectedCoreChanges"),Is.Zero);Assert.That((string)Read(proof,"observedInputRole"),Is.EqualTo("ACTUAL_CURRENT_WORLD_FINAL_THEN_QUALIFIED_ORIGINAL_ROOF_SOURCE_PROTECTION_NOT_RAW_SDK"));Assert.That((string)Read(proof,"roofCheckpoint"),Is.Not.EqualTo((string)Read(proof,"checkpoint")));}Assert.That((bool)Read(Read(proof,"after"),"passed"),Is.True);foreach(var tile in Rows(receipt,"terrains"))Assert.That((bool)Read(tile,"undergroundFoundationExact"),Is.True,"The separate original foundation roof gate is never waived by successful smoothing");Assert.That((bool)Read(receipt,"originalPhysicalGatesPassed"),Is.True);Assert.That((bool)Read(receipt,"sourceBytesPreserved"),Is.True);TestContext.WriteLine("ACTUAL_CURRENT_WORLD_RECIPE_AND_EXISTING_GATES "+JsonUtility.ToJson(proof));
        if(args.Contains("-tsDragonCavernRasterClearance"))
        {
            var join=Read(receipt,"cavernJoinFinish");Assert.That(join,Is.Not.Null,"Qualified source-rock entrance art must be installed into the actual saved world");foreach(string field in new[]{"originalTransformsMeshesPhysicsTerrainAndGraphExact","originalSourceBytesExact","allOriginalClearancesPreserved","everyPrimaryMeshActuallyIntersectsNative","everyBandActuallyConnected"})Assert.That((bool)Read(join,field),Is.True,field);Assert.That(((Array)Read(join,"rocks")).Length,Is.EqualTo((int)Read(join,"expectedPieces")));
        }
        var coast=Read(receipt,"outerBand");var coordination=Read(coast,"currentCoreCoordination");Assert.That(coordination,Is.Not.Null);foreach(string field in new[]{"coreHeightmapsExact","originalSourcesExact","nonHeightPayloadExact","beyondCollarExact","passed"})Assert.That((bool)Read(coordination,field),Is.True,field);
    }
}
#endif
