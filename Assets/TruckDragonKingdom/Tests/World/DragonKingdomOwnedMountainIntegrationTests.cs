#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DragonKingdomOwnedMountainIntegrationTests
{
    static Type Author=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
#pragma warning disable CS0649
    [Serializable]sealed class Probe{public string candidatePrefab,candidatePreview,error;public Road road;public bool originalWorldSourcesExact,originalTerrainMemoryExact,savedReloaded,legacyRestoredAfterForcedException,nestedSelectionRejected,ownedDefinitionProvenanceExact,ownedDefinitionUnknownChangeRejected;}
    [Serializable]sealed class Road{public int points,physicalPieces,physicalQueries,physicalMisses,navigationQueries;public float maximumPhysicalHeightError;public bool intentionalTargetDelta,otherEdgesAndNodesExact,prefixAndSemanticEndpointsExact,physicalAuthorityExact,savedAuthorityExact,sourceBytesPreserved,legacySelectionRestored,randomStateRestored;public string originalGraphSha256,candidateGraphSha256,error;public Retired[] retired;public string[] preservedUndergroundPads;public Grade gradeInput;}
    [Serializable]sealed class Retired{public string name;public bool beforeActive,afterActive;public long sourceFileId;}
    [Serializable]sealed class Grade{public bool exact,nativeMeshMatchesSaved,nativePhysicalMeshMatchesSaved,nativeWorldPosesMatchSaved;public int markers;public string authoritativePointsSha256,actualSdkInputPointsSha256;}
#pragma warning restore CS0649
    [Test,Timeout(300000)]
    public void QualifiedSavedMountainRoadSharesGraphPhysicalSdkAuthorityAndRestoresOriginalSources()
    {
        var report=JsonUtility.FromJson<Probe>((string)Author.GetMethod("CreateSavedOwnedMountainAuthorityCandidateFromCommandLine").Invoke(null,null));Assert.That(report.error,Is.Null.Or.Empty);Assert.That(report.originalWorldSourcesExact&&report.originalTerrainMemoryExact&&report.savedReloaded&&report.legacyRestoredAfterForcedException&&report.nestedSelectionRejected&&report.ownedDefinitionProvenanceExact&&report.ownedDefinitionUnknownChangeRejected,Is.True);Assert.That(File.Exists(report.candidatePrefab)&&File.Exists(report.candidatePreview),Is.True);AssertRoad(report.road);Assert.That(report.road.physicalQueries,Is.EqualTo(1920));Assert.That(report.road.physicalMisses,Is.Zero);Assert.That(report.road.maximumPhysicalHeightError,Is.LessThanOrEqualTo(.3f));Assert.That(report.road.navigationQueries,Is.EqualTo(5));
        TestContext.WriteLine("SAVED646_OWNED_ROUTE GRAPH_RENDER_COLLIDER_AND_ACTUAL_SDK_INPUT_EQUAL 1920ACTUAL_CONTACTS NAV_LOOKUPS5 SOURCE_AND_SCOPE_RESTORED NO_TERRAIN_PORTAL_DIGGING_FULLWORLD_TRAVERSAL_OR_ADOPTION_ACCEPTANCE");
    }
    [Test,Timeout(120000)]
    public void OriginalUnlinkedRoadIdentityAcceptsOnlyExactAuthoredGeometryAndRejectsDesignerChanges()
    {
        const string preview="Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity",prefab="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";var originalScene=File.ReadAllBytes(preview);var originalPrefab=File.ReadAllBytes(prefab);Scene scene=default;
        try
        {
            scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Kingdom caverns-volcano · EasyRoads generated course");
            var method=Author.GetMethod("OriginalOwnedRoadSource",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(method,Is.Not.Null);void Require()=>method.Invoke(null,new object[]{root,3637298366336087090L});Require();
            var position=root.localPosition;try{root.localPosition=position+Vector3.right*.125f;var error=Assert.Throws<TargetInvocationException>(()=>Require());Assert.That(error.InnerException.Message,Does.Contain("authored property changed"));}finally{root.localPosition=position;}Require();
            var collider=root.GetComponentsInChildren<MeshCollider>(true).First();bool enabled=collider.enabled;try{collider.enabled=!enabled;var error=Assert.Throws<TargetInvocationException>(()=>Require());Assert.That(error.InnerException.Message,Does.Contain("authored property changed"));}finally{collider.enabled=enabled;}Require();
            TestContext.WriteLine("ACTUAL_UNLINKED_ORIGINAL_ROAD_ACCEPTED POSITION_AND_COLLIDER_EDITS_REJECTED SOURCE_ASSETS_UNCHANGED NO_WORLD_ADOPTION");
        }
        finally
        {
            if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);CollectionAssert.AreEqual(originalScene,File.ReadAllBytes(preview));CollectionAssert.AreEqual(originalPrefab,File.ReadAllBytes(prefab));
        }
    }
    [UnityTest,Timeout(1800000)]
    public IEnumerator ExplicitQualifiedMountainProfileRetainsExistingFullWorldAcceptanceGates()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";
        Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var existing=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(existing.path))
                Assert.That(EditorSceneManager.SaveScene(existing,generated+"OwnedMountainWorldRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(existing.isDirty||string.IsNullOrEmpty(existing.path),Is.False,"Preserve incoming user scenes.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var incoming=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolation,generated+"OwnedMountainWorldIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
        Assert.That(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonOwnedMountainRoad"),Is.GreaterThanOrEqualTo(0));yield return (IEnumerator)Author.GetMethod("CreateRoundedReviewCandidate",Type.EmptyTypes).Invoke(null,null);var receipt=Author.GetProperty("LastReceipt").GetValue(null);object Field(string name)=>receipt.GetType().GetField(name).GetValue(receipt);Assert.That((string)Field("status"),Is.EqualTo("PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW"));foreach(string name in new[]{"sourceBytesPreserved","candidateRoadAuthorityAndCriticalPosesExact","originalPhysicalGatesPassed","broadVendorHighlandMassRetained"})Assert.That((bool)Field(name),Is.True,name);Assert.That((bool)Field("roadsAndCriticalPosesExact"),Is.False,"The target graph intentionally changed; never call its old graph byte-exact.");var road=JsonUtility.FromJson<Road>(JsonUtility.ToJson(Field("ownedMountainRoad")));AssertRoad(road);var quality=Field("terrainQuality");Assert.That((bool)quality.GetType().GetField("passed").GetValue(quality),Is.True);var cavern=Field("cavern");Assert.That(cavern,Is.Not.Null);Assert.That((string)cavern.GetType().GetField("status").GetValue(cavern),Is.EqualTo("MEASURED_CAVERN_REQUIRES_FULL_WORLD_GATES_AND_RENDER_REVIEW"));
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernOwnedRockMaterials")>=0)
        {
            var materials=Field("cavernMaterials");Assert.That(materials,Is.Not.Null);foreach(string name in new[]{"originalSourcesExact","geometryExact","onlyApprovedInputs","providerAndRendererBindingsExact","savedPrefabBindingsExact"})Assert.That((bool)materials.GetType().GetField(name).GetValue(materials),Is.True,name);
        }

        }
        finally
        {
            var digger=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);
            if(!(bool)digger.GetProperty("IsBusy").GetValue(null))EditorSceneManager.RestoreSceneManagerSetup(setup);
            foreach(var entry in incoming)Assert.That(File.ReadAllBytes(entry.Key),Is.EqualTo(entry.Value),"Original incoming scene changed: "+entry.Key);
        }
    }
    static void AssertRoad(Road road)
    {
        Assert.That(road,Is.Not.Null);Assert.That(road.points,Is.EqualTo(646));Assert.That(road.physicalPieces,Is.GreaterThan(0));Assert.That(road.intentionalTargetDelta&&road.otherEdgesAndNodesExact&&road.prefixAndSemanticEndpointsExact&&road.physicalAuthorityExact&&road.savedAuthorityExact&&road.sourceBytesPreserved&&road.legacySelectionRestored&&road.randomStateRestored,Is.True);Assert.That(road.originalGraphSha256,Is.Not.EqualTo(road.candidateGraphSha256));Assert.That(road.gradeInput,Is.Not.Null);Assert.That(road.gradeInput.markers,Is.EqualTo(646));Assert.That(road.gradeInput.exact&&road.gradeInput.nativePhysicalMeshMatchesSaved&&road.gradeInput.nativeWorldPosesMatchSaved,Is.True);Assert.That(road.gradeInput.actualSdkInputPointsSha256,Is.EqualTo(road.gradeInput.authoritativePointsSha256));Assert.That(road.retired.Length,Is.EqualTo(4));foreach(var retired in road.retired){Assert.That(retired.sourceFileId,Is.GreaterThan(0));Assert.That(retired.beforeActive,Is.True);Assert.That(retired.afterActive,Is.False);}Assert.That(road.preservedUndergroundPads,Is.EqualTo(new[]{"Kingdom hairpin caverns-volcano 2 · EasyRoads generated course"}));
    }
}
#endif
