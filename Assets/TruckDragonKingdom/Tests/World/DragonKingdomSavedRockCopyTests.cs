#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonKingdomSavedRockCopyTests
{
    [Serializable]sealed class Proof
    {
        public string scope,sourcePreview,prefab,preview,failure,geometryBefore,geometryAfter,geometryReloaded;
        public bool originalSourcesExact,sourceVoxelMembershipExact,terrainCopiesExact,providerCopiesExact,savedReloaded,stableGeometryExact,stableProviderExact,sourceMeshesReferencedExact,copiedTerrainFilesExact,sourceRasterClearanceVerified;
        public int terrainCopies,providerMaterialCopies,nativeMeshReferences;public MaterialProof materials;
    }
    [Serializable]sealed class MaterialProof { public bool originalSourcesExact,geometryExact,onlyApprovedInputs,providerAndRendererBindingsExact,savedPrefabBindingsExact;public string[] materials;public int diggerSystems,rendererSlots; }
    [Serializable]sealed class PropertyDiagnostic{public string scope,failure;public bool originalBytesExact;public int pairs,approvedPairs;}
    [Test,Explicit("Uses an actually passed fresh raster-cave source and a unique saved diagnostic copy; never adopts it."),Timeout(600000)]
    public void ActualQualifiedRasterCavernPreservesGeometrySourcesAndNativeDoorwaySpaceThroughSavedMaterialCopy()
    {
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);if(!string.IsNullOrEmpty(scene.path))continue;Assert.That(Application.isBatchMode&&!scene.isDirty,Is.True,"Preserve interactive/dirty incoming scenes; only register the empty isolated batch harness");const string harness="Assets/TruckDragonKingdom/Tests/Generated/";Directory.CreateDirectory(harness);AssetDatabase.Refresh();Assert.That(EditorSceneManager.SaveScene(scene,harness+"RasterCopyRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        }
        var args=Environment.GetCommandLineArgs();string Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,key);return args[at+1];}string source=Required("-tsDragonCavernProbe");Assert.That(source,Is.Not.EqualTo("CavernRoundedProbe17"));var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);var result=type.GetMethod("InspectIsolatedActualCavernSavedRockCopy").Invoke(null,new object[]{source,Required("-tsDragonSavedRockCopyTag")});var proof=JsonUtility.FromJson<Proof>(JsonUtility.ToJson(result));
        Assert.That(proof.sourceRasterClearanceVerified,Is.True);Assert.That(proof.sourcePreview,Does.Contain("/"+source+"/"));Assert.That(proof.originalSourcesExact&&proof.sourceVoxelMembershipExact&&proof.terrainCopiesExact&&proof.providerCopiesExact&&proof.savedReloaded&&proof.stableGeometryExact&&proof.stableProviderExact&&proof.sourceMeshesReferencedExact&&proof.copiedTerrainFilesExact,Is.True);Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.terrainCopies,Is.EqualTo(4));Assert.That(proof.providerMaterialCopies,Is.GreaterThan(0));Assert.That(proof.nativeMeshReferences,Is.GreaterThan(0));Assert.That(proof.geometryBefore,Is.EqualTo(proof.geometryAfter));Assert.That(proof.geometryReloaded,Is.EqualTo(proof.geometryBefore));Assert.That(proof.materials,Is.Not.Null);Assert.That(proof.materials.originalSourcesExact&&proof.materials.geometryExact&&proof.materials.onlyApprovedInputs&&proof.materials.providerAndRendererBindingsExact&&proof.materials.savedPrefabBindingsExact,Is.True);Assert.That(proof.materials.materials.Length,Is.EqualTo(proof.providerMaterialCopies));Assert.That(File.Exists(proof.prefab)&&File.Exists(proof.preview),Is.True);TestContext.WriteLine("ACTUAL_SOURCE_QUALIFIED_RASTER_CAVE_MATERIAL_COPY_NATIVE_SPACE_AND_RELOAD_EXACT_NOT_WORLD_OR_VISUAL_ACCEPTANCE");
    }
    [Test,Timeout(60000)]
    public void SavedRockNativeProviderAndRendererReferenceRemapPreservesEveryUnrelatedField()
    {
        var args=Environment.GetCommandLineArgs();string[] Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,key);return args[at+1].Split(';');}var sourcePaths=Required("-tsDragonSavedRockSourceMaterials");var candidatePaths=Required("-tsDragonSavedRockCandidateMaterials");Assert.That(sourcePaths.Length,Is.EqualTo(2));Assert.That(candidatePaths.Length,Is.EqualTo(2));
        var bytes=new Dictionary<string,byte[]>();var map=new Dictionary<Material,Material>();for(int i=0;i<2;i++){foreach(string path in new[]{sourcePaths[i],sourcePaths[i]+".meta",candidatePaths[i],candidatePaths[i]+".meta"})bytes.Add(path,File.ReadAllBytes(path));map.Add(AssetDatabase.LoadAssetAtPath<Material>(sourcePaths[i]),AssetDatabase.LoadAssetAtPath<Material>(candidatePaths[i]));}
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);var remap=(Func<string,string>)type.GetMethod("SavedRockMaterialReferenceRemapper",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{map});var go=new GameObject("Read-only native material reference control"){hideFlags=HideFlags.HideAndDontSave};
        try
        {
            var renderer=go.AddComponent<MeshRenderer>();var digger=go.AddComponent(Type.GetType("Digger.Modules.Core.Sources.DiggerSystem, Digger.Core",true));var materials=digger.GetType().GetField("Materials");var sources=new[]{AssetDatabase.LoadAssetAtPath<Material>(sourcePaths[0]),AssetDatabase.LoadAssetAtPath<Material>(sourcePaths[1])};var candidates=new[]{map[sources[0]],map[sources[1]]};
            materials.SetValue(digger,sources);renderer.sharedMaterials=sources;string providerBefore=EditorJsonUtility.ToJson(digger),rendererBefore=EditorJsonUtility.ToJson(renderer);materials.SetValue(digger,candidates);renderer.sharedMaterials=candidates;Assert.That(remap(providerBefore),Is.EqualTo(EditorJsonUtility.ToJson(digger)));Assert.That(remap(rendererBefore),Is.EqualTo(EditorJsonUtility.ToJson(renderer)));renderer.sortingOrder++;Assert.That(remap(rendererBefore),Is.Not.EqualTo(EditorJsonUtility.ToJson(renderer)),"An unrelated serialized renderer edit must still reject.");
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sources[0],out string guid,out long id);string unknown="{\"fileID\":"+(id+1)+",\"guid\":\""+guid+"\",\"type\":2}",unknownGuid="{\"fileID\":"+id+",\"guid\":\"ffffffffffffffffffffffffffffffff\",\"type\":2}",wrongType="{\"fileID\":"+id+",\"guid\":\""+guid+"\",\"type\":3}",text="{\"label\":\""+guid+"\",\"value\":17}";Assert.That(remap(unknown),Is.EqualTo(unknown));Assert.That(remap(unknownGuid),Is.EqualTo(unknownGuid));Assert.That(remap(wrongType),Is.EqualTo(wrongType));Assert.That(remap(text),Is.EqualTo(text));string transient="{\"instanceID\":"+sources[0].GetInstanceID()+"}";Assert.That(remap(transient),Is.EqualTo("{\"instanceID\":"+candidates[0].GetInstanceID()+"}"));
            TestContext.WriteLine("ACTUAL_NATIVE_DIGGER_AND_RENDERER_JSON_TWO_EXACT_PERSISTENT_MATERIAL_REFS_REMAP_LEGACY_INSTANCE_ID_RETAINED_UNKNOWN_LOCAL_ID_TYPE_TEXT_AND_OTHER_FIELDS_UNCHANGED SOURCE_BYTES_EXACT NO_WORLD_OR_MATERIAL_ACCEPTANCE_INFERRED");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);foreach(var pair in bytes)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),pair.Key);}
    }
    [Test,Timeout(60000)]
    public void RetainedActualSavedRockMaterialPairsExposeExactPropertyGuardResultWithoutSourceMutation()
    {
        var args=Environment.GetCommandLineArgs();string[] Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,key);return args[at+1].Split(';');}
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);var result=type.GetMethod("DiagnoseRetainedSavedRockMaterialProperties",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{Required("-tsDragonSavedRockSourceMaterials"),Required("-tsDragonSavedRockCandidateMaterials")});var proof=JsonUtility.FromJson<PropertyDiagnostic>(JsonUtility.ToJson(result));Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.originalBytesExact,Is.True);Assert.That(proof.pairs,Is.EqualTo(2));Assert.That(proof.approvedPairs,Is.InRange(0,2));TestContext.WriteLine("READ_ONLY_RETAINED_NATIVE_MATERIAL_GUARD_DIAGNOSIS approved="+proof.approvedPairs+"/2 NOT_SAVED_CANDIDATE_ACCEPTANCE");
    }
    [Test,Timeout(300000)]
    public void ActualCave17OwnedMaterialCopyPersistsNativeProvidersAndFullStableGeometryAcrossPrefabReload()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonSavedRockCopyTag");Assert.That(at>=0&&at+1<args.Length,Is.True,"-tsDragonSavedRockCopyTag");
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path)){const string runner="Assets/TruckDragonKingdom/Tests/Generated";Directory.CreateDirectory(runner);AssetDatabase.Refresh();Assert.That(EditorSceneManager.SaveScene(scene,runner+"/SavedRockRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);}}
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);var method=type.GetMethod("InspectIsolatedActualCavernSavedRockCopy",BindingFlags.Public|BindingFlags.Static);Assert.That(method,Is.Not.Null);
        var result=method.Invoke(null,new object[]{"CavernRoundedProbe17",args[at+1]});var proof=JsonUtility.FromJson<Proof>(JsonUtility.ToJson(result));
        Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.scope,Does.Contain("NOT_WORLD_QUALITY"));Assert.That(proof.originalSourcesExact&&proof.sourceVoxelMembershipExact&&proof.terrainCopiesExact&&proof.providerCopiesExact&&proof.savedReloaded&&proof.stableGeometryExact&&proof.stableProviderExact&&proof.sourceMeshesReferencedExact&&proof.copiedTerrainFilesExact,Is.True);
        Assert.That(proof.terrainCopies,Is.EqualTo(4));Assert.That(proof.providerMaterialCopies,Is.GreaterThan(0));Assert.That(proof.nativeMeshReferences,Is.GreaterThanOrEqualTo(428));Assert.That(proof.geometryBefore,Is.EqualTo(proof.geometryAfter));Assert.That(proof.geometryReloaded,Is.EqualTo(proof.geometryBefore));
        Assert.That(proof.materials,Is.Not.Null);Assert.That(proof.materials.originalSourcesExact&&proof.materials.geometryExact&&proof.materials.onlyApprovedInputs&&proof.materials.providerAndRendererBindingsExact&&proof.materials.savedPrefabBindingsExact,Is.True);Assert.That(proof.materials.diggerSystems,Is.GreaterThan(0));Assert.That(proof.materials.rendererSlots,Is.GreaterThanOrEqualTo(428));Assert.That(proof.materials.materials.Length,Is.EqualTo(proof.providerMaterialCopies));Assert.That(File.Exists(proof.prefab)&&File.Exists(proof.preview),Is.True);
        TestContext.WriteLine("ACTUAL_CAVE17_CLONED_TERRAIN4_NATIVE_MATERIAL_PROVIDER_COPY_ONLY_ROUGH_ROCK12M_SAVED_RELOAD_SOURCE_VOXELS_MESH_PHYSICS_HEIGHT_HOLES_ANCHORS_EXACT NOT_NEW_DIGGING_RUNTIME_RENDER_WORLD_QUALITY_OR_CANONICAL_ACCEPTANCE");
    }
}
#endif
