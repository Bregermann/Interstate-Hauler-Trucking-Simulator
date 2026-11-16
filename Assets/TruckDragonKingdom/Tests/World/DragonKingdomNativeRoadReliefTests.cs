#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomNativeRoadReliefTests
{
    [Test,Timeout(120000)]
    public void InactiveRetainedStagingPadsAreExcludedFromCurrentPhysicalTerrainAuthority()
    {
        const string path="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";var bytes=File.ReadAllBytes(path);var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var worldType=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);var method=type.GetMethod("ActivePhysicalStagingRoots",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);var scene=EditorSceneManager.NewPreviewScene();GameObject instance=null;
        try
        {
            instance=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);var world=instance.GetComponent(worldType);Transform[] Roots()=>((System.Collections.IEnumerable)method.Invoke(null,new object[]{world})).Cast<Transform>().ToArray();var original=Roots();int[] numbers={6,9,12};long[] expected={4520377880275211363L,8847699847918239384L,1382603855827951494L};var retired=new Transform[3];
            for(int i=0;i<3;i++){string name="Kingdom hairpin caverns-volcano "+numbers[i]+" "+'\u00b7'+" EasyRoads generated course";var canonical=source.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(canonical.gameObject,out string _,out long id),Is.True);Assert.That(id,Is.EqualTo(expected[i]));retired[i]=instance.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);Assert.That(original.Contains(retired[i]),Is.True);retired[i].gameObject.SetActive(false);}
            var after=Roots();Assert.That(after.Length,Is.EqualTo(original.Length-3));Assert.That(after.Intersect(retired),Is.Empty);Assert.That(after.ToHashSet().SetEquals(original.Except(retired)),Is.True,"Preserve every other actual active staging root; no broad road exclusion.");foreach(var t in retired)t.gameObject.SetActive(true);Assert.That(Roots().SequenceEqual(original),Is.True);
        }
        finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(scene);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));}
    }
    [TestCase(true,true,1,24.002929f,24,.009176f,true)]
    [TestCase(true,true,1,24.04f,24,.009176f,false)]
    [TestCase(true,false,1,24.002929f,24,.009176f,false)]
    [TestCase(false,true,1,24.002929f,24,.009176f,false)]
    [TestCase(true,true,.25f,24.002929f,24,.009176f,false)]
    [TestCase(true,true,1,24.002929f,24,.5f,false)]
    public void NativeEmbeddedGroundOnlyAcceptsMeasuredTerrainAtActualPavementWithinSmallEncodingBound(bool terrain,bool top,float normal,float ground,float pavement,float bound,bool expected)
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        var actual=(bool)type.GetMethod("NativeTerrainPavementContactWithinBound").Invoke(null,new object[]{terrain,top,normal,ground,pavement,bound});Assert.That(actual,Is.EqualTo(expected));
    }
    [UnityTest,Timeout(1500000)]
    public IEnumerator ActualEasyRoadsReliefCandidatePreservesNativeSamplesAndPassesOriginalPhysicalWorldGates()
    {
        string[] args=Environment.GetCommandLineArgs();int mode=Array.IndexOf(args,"-tsDragonTerrainCoordination");
        Assert.That(mode>=0&&mode+1<args.Length,Is.True,"This is an explicit new-candidate terrain composition, never a normal-build regeneration.");
        Assert.That(args[mode+1],Is.EqualTo("VendorGradedRelief01"));
        Assert.That(args,Does.Contain("-tsDragonFinishArt"),"Retain the actual owned source art path and its physical/source checks.");
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";
        Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var existing=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(existing.path))
                Assert.That(EditorSceneManager.SaveScene(existing,generated+"NativeReliefRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(existing.isDirty||string.IsNullOrEmpty(existing.path),Is.False,"Preserve incoming user scenes.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var incoming=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolation,generated+"NativeReliefIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        try
        {
            yield return (IEnumerator)type.GetMethod("CreateRoundedReviewCandidate",Type.EmptyTypes).Invoke(null,null);
            var receipt=type.GetProperty("LastReceipt").GetValue(null);
            Assert.That((string)Field(receipt,"terrainCoordinationMode"),Is.EqualTo("VendorGradedRelief01"));
            Assert.That((string)Field(receipt,"status"),Is.EqualTo("PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW"));
            if(args.Contains("-tsDragonNativeGradeProfile"))
            {
                var selection=Field(receipt,"nativeGradeSelection");Assert.That((string)Field(selection,"profile"),Is.EqualTo("SourceRoadsWithoutHairpinAprons01"));Assert.That((int)Field(selection,"excludedHairpinGrades"),Is.GreaterThan(0));Assert.That((int)Field(selection,"selectedRoads")+(int)Field(selection,"excludedHairpinGrades"),Is.EqualTo((int)Field(selection,"actualCreatedRoads")));
                foreach(string name in new[]{"allOriginalMainRoadsSelected","allOtherCreatedGradesSelected","onlyTrackedHairpinGradesExcluded","originalBuildChannelFlagsPreserved"})Assert.That((bool)Field(selection,name),Is.True,name);
            }
            foreach(string name in new[]{"sourceBytesPreserved","roadsAndCriticalPosesExact","originalPhysicalGatesPassed","broadVendorHighlandMassRetained"})
                Assert.That((bool)Field(receipt,name),Is.True,name);
            var proof=Field(receipt,"nativeRoadRelief");Assert.That(proof,Is.Not.Null);
            Assert.That((bool)Field(proof,"physicalCrossingIntervalsEnabled"),Is.EqualTo(args.Contains("-tsDragonPhysicalCrossings")));
            if(args.Contains("-tsDragonPhysicalCrossings"))Assert.That(((Array)Field(proof,"physicalCrossings")).Length,Is.GreaterThan(0));
            foreach(string name in new[]{"actualVendorGradeCapturedBeforeCoordination","wholeTownAndGroveBoundingBoxesNotConstraints","sourceRoadPosesAndGraphUnchanged","passed"})
                Assert.That((bool)Field(proof,name),Is.True,name);
            Assert.That((int)Field(proof,"physicalRoadSegments"),Is.GreaterThan(0));
            Assert.That((string)Field(proof,"connectedDryDistrictsSha256"),Has.Length.EqualTo(64));
            var districts=((Array)Field(proof,"connectedDryDistricts")).Cast<object>().ToArray();Assert.That(districts.Select(d=>(string)Field(d,"region")),Is.EqualTo(new[]{"village","harbor"}));
            foreach(var district in districts){Assert.That((float)Field(district,"originalCreekTransitionMeters"),Is.EqualTo(40));Assert.That((float)Field(district,"transitionFalloff"),Is.EqualTo(.2f));}
            var tiles=((Array)Field(proof,"tiles")).Cast<object>().ToArray();Assert.That(tiles.Length,Is.EqualTo(4));
            foreach(var tile in tiles)
            {
                foreach(string name in new[]{"outsideDeclaredCoordinationAreasExact","validOriginalStartExtractionContactsExact","nativeUndergroundSamplesExact"})
                    Assert.That((bool)Field(tile,name),Is.True,name);
                Assert.That((int)Field(tile,"untouchedNativeSamples"),Is.GreaterThan(0));
                Assert.That((string)Field(tile,"vendorGradedHeightSha256"),Has.Length.EqualTo(64));
                foreach(string hash in new[]{"coordinatedHeightSha256","authorizedMaskSha256","districtRaisedMaskSha256"})Assert.That((string)Field(tile,hash),Has.Length.EqualTo(64));
            }
            if(args.Contains("-tsDragonVistaPostGrade"))
            {
                var vista=Field(receipt,"nativeVistaPostGrade");Assert.That((string)Field(vista,"profile"),Is.EqualTo("NativeErodedMountainRange01"));
                foreach(string name in new[]{"authoredNodeExact","sourceBytesExact","graphAndCriticalPosesExact","allProtectedInputSamplesExact","actualUnityReadbackVerified"})Assert.That((bool)Field(vista,name),Is.True,name);
                Assert.That((float)Field(vista,"maximumProtectedDelta"),Is.Zero);Assert.That((int)Field(vista,"changedSamples"),Is.GreaterThan(0));Assert.That((float)Field(vista,"maximumActualSeamDelta"),Is.LessThanOrEqualTo(.04f));
                Assert.That((int)Field(vista,"configuredGraphResolution"),Is.EqualTo(1024));Assert.That((int)Field(vista,"actualInputTextureWidth"),Is.EqualTo(1025));Assert.That((int)Field(vista,"vendorResolution"),Is.EqualTo(1032));Assert.That((int)Field(vista,"actualOutputWidth"),Is.EqualTo(1032));Assert.That((int)Field(vista,"actualOutputHeight"),Is.EqualTo(1032));Assert.That(new FileInfo((string)Field(vista,"actualGpuFloatPath")).Length,Is.EqualTo(1032L*1032*4));Assert.That(new FileInfo((string)Field(vista,"resampledVendorFloatPath")).Length,Is.EqualTo(1025L*1025*4));
                var stages=((Array)Field(vista,"tiles")).Cast<object>().ToDictionary(t=>(string)Field(t,"terrain"));
                foreach(var tile in tiles){var stage=stages[(string)Field(tile,"terrain")];Assert.That((string)Field(tile,"vendorGradedHeightSha256"),Is.EqualTo((string)Field(stage,"actualSdkInputSha256")));Assert.That((string)Field(tile,"compositionReferenceHeightSha256"),Is.EqualTo((string)Field(stage,"actualVistaOutputSha256")));Assert.That((string)Field(tile,"actualVistaOutputHeightSha256"),Is.EqualTo((string)Field(stage,"actualVistaOutputSha256")));}
            }
            var berth=Field(receipt,"berth");Assert.That((string)Field(berth,"policy"),Is.EqualTo("NATIVE_ACTUAL_SOURCE_HULL_25_KEEL_CLEARANCES_AND_53_CONSERVATIVE_FERRY_SWEEPS"));
            var hull=Field(berth,"actualHull");Assert.That((bool)Field(hull,"passed"),Is.True);Assert.That((int)Field(hull,"depthSamples"),Is.EqualTo(25));Assert.That((int)Field(hull,"departureSweepSamples"),Is.EqualTo(53));Assert.That((float)Field(hull,"minimumKeelClearance"),Is.GreaterThanOrEqualTo(3));
            foreach(string name in new[]{"terrainQuality","groundingReview"})Assert.That((bool)Field(Field(receipt,name),"passed"),Is.True,name);
            int falloffProfile=Array.IndexOf(args,"-tsDragonVendorFalloffProfile");
            if(falloffProfile>=0&&falloffProfile+1<args.Length&&args[falloffProfile+1]=="MeasuredNeSourceFragment")
            {
                var falloffType=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomVendorFalloffAuthoring, Assembly-CSharp-Editor",true);var falloff=falloffType.GetProperty("LastEvidence").GetValue(null);var fragment=Field(falloff,"fragment");
                Assert.That((string)Field(falloff,"profile"),Is.EqualTo("MeasuredNeSourceFragment"));Assert.That((int)Field(falloff,"selectedRoads"),Is.EqualTo(2));Assert.That((int)Field(falloff,"operations"),Is.GreaterThanOrEqualTo(4));
                foreach(string name in new[]{"settingsRestored","sourceBytesExact","originalPreSdkRoofExact"})Assert.That((bool)Field(falloff,name),Is.True,name);
                foreach(string name in new[]{"sourceSliceExact","controlsExact","sourceIntervalsNotExcluded","creationHeightfieldsExact","originalRoadAuthoritiesExact","originalTypeRegistryExact","footprintExcludesOriginalRoof","temporaryRoadRemoved"})Assert.That((bool)Field(fragment,name),Is.True,name);
            }
            Assert.That((int)Field(Field(receipt,"groundingReview"),"unsupportedPavement"),Is.Zero);
            Assert.That((bool)Field(Field(receipt,"bearings"),"nativeMeasuredGroundContactPolicy"),Is.True);
            if(args.Contains("-tsDragonFinishedArchitecture"))
            {
                var architecture=Field(receipt,"finishedArchitecture");var retained=Field(receipt,"retainedNativeBuildings");var integration=Field(receipt,"nativeArchitectureIntegration");
                Assert.That((int)Field(architecture,"instances"),Is.EqualTo(678));Assert.That((int)Field(architecture,"footings"),Is.EqualTo(525));Assert.That((int)Field(retained,"buildings"),Is.EqualTo(25));Assert.That((int)Field(proof,"explicitArchitectureFootings"),Is.EqualTo(550));
                foreach(string name in new[]{"inventoryIdentityPreserved","measuredFramesExcludedFromLooseRegrounding","framePosesExact","retainedFinalTerrainContactsVerified"})Assert.That((bool)Field(integration,name),Is.True,name);
                var support=Field(integration,"actualArchitectureTerrainSupport");Assert.That((bool)Field(support,"passed"),Is.True);Assert.That((bool)Field(support,"nativeColliderRaysPerformed"),Is.True);Assert.That((int)Field(support,"parcels"),Is.EqualTo(525));Assert.That((int)Field(support,"samples"),Is.GreaterThanOrEqualTo(4725));
                Assert.That((bool)Field(architecture,"terrainSupportVerified"),Is.True);Assert.That((int)Field(Field(receipt,"finish"),"compositionSchema"),Is.EqualTo(4));Assert.That((bool)Field(Field(architecture,"waterfront"),"actualBedAndPhysicalSupportsPassed"),Is.True);
            }
            Assert.That(((Array)Field(Field(receipt,"bearings"),"geometry")).Length,Is.GreaterThan(0));
            Assert.That((float)Field(Field(receipt,"terrainQuality"),"maximumTileSeamDelta"),Is.LessThanOrEqualTo(.04f));
            var cavern=Field(receipt,"cavern");Assert.That(cavern,Is.Not.Null);
            Assert.That((string)Field(cavern,"status"),Is.EqualTo("MEASURED_CAVERN_REQUIRES_FULL_WORLD_GATES_AND_RENDER_REVIEW"));
            Assert.That((bool)Field(cavern,"exactHeightmaps"),Is.True,"Real Digger excavation must preserve the retained native terrain surface.");
            Assert.That((bool)Field(cavern,"outsideHolesExact"),Is.True);
            // This passes only the real native physical/composition scope.
            // Eighteen regional views and complete driving remain separate.
        }
        finally
        {
            var digger=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);
            if(!(bool)digger.GetProperty("IsBusy").GetValue(null))EditorSceneManager.RestoreSceneManagerSetup(setup);
            foreach(var entry in incoming)Assert.That(File.ReadAllBytes(entry.Key),Is.EqualTo(entry.Value),"Original incoming scene changed: "+entry.Key);
        }
    }
}
#endif
