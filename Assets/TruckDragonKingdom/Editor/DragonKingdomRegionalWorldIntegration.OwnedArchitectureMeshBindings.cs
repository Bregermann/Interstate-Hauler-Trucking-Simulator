#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class OwnedArchitectureMeshRepair
        {
            public string authority="V324_EXACT_ORIGINAL_FBX_META_AND_UNIQUE_NATIVE_MODEL_MESH_AFTER_RAW_INSTANCE_GEOMETRY_BASELINE",instance,sourcePrefab,sourcePrefabSha256,sourcePrefabMetaSha256,filterLocalId,meshAsset,meshAssetGuid,meshAssetSha256,meshAssetMetaSha256,originalRequestedId,originalRequestedName,repairedId,repairedName;
            public int repairedVertices,repairedIndices;public bool rawNativeReferenceWasNull,sourcePoseAndMaterialsPreserved,sourceBytesPreserved;
        }
        [Serializable]public sealed class OwnedLadderPersistenceProbe
        {public string scope="ACTUAL_DISPOSABLE_OWNED_INSTANCE_MESH_OVERRIDE_PREFAB_SAVE_RELOAD_AND_SOURCE_PRESERVATION_NOT_WORLD_RENDER_ACCEPTANCE",prefabType;public bool rawSourceStillNull,reloadedExactMesh,reloadedMatchingCollision,sourceBytesPreserved,temporaryAssetsRemoved;public OwnedArchitectureMeshRepair repair;}
        public static string CaptureOwnedLadderBindingPersistenceProbe()
        {
            var layout=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(ArchitectureLayoutPaths[1])));if(AssetSha(ArchitectureLayoutPaths[1])!=ArchitectureLayoutHashes[1])throw new InvalidOperationException("Exact original lumber layout required.");var row=layout.instances.Single(r=>r.sceneInstanceId=="1007988382");var source=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject instance=null;string folder=Root+"VisualCandidates/OwnedLadderPersistenceProbe-"+Guid.NewGuid().ToString("N");bool created=false;var proof=new OwnedLadderPersistenceProbe();
            try
            {
                instance=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);instance.transform.localPosition=row.position;instance.transform.localRotation=row.rotation;instance.transform.localScale=row.scale;var baseline=RidgeGeometryBindings(instance.transform,new System.Collections.Generic.Dictionary<string,string>());proof.repair=RepairOwnedArchitectureMeshAfterRawBaseline(row,instance.transform,baseline);ConfigureSourceAuthoredStaticCollision(instance.transform);
                if(Directory.Exists(Io(folder)))throw new InvalidOperationException("Fresh uniquely owned probe folder required.");Directory.CreateDirectory(Io(folder));created=true;AssetDatabase.Refresh();string path=folder+"/Owned Ladder.prefab";if(PrefabUtility.SaveAsPrefabAsset(instance,path)==null)throw new InvalidOperationException("Actual owned prefab save failed.");var loaded=AssetDatabase.LoadAssetAtPath<GameObject>(path);var filter=loaded.GetComponentsInChildren<MeshFilter>(true).Single();proof.prefabType=PrefabUtility.GetPrefabAssetType(loaded).ToString();proof.reloadedExactMesh=filter.sharedMesh!=null&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh,out string guid,out long id)&&guid==proof.repair.meshAssetGuid&&id==4300002;proof.reloadedMatchingCollision=loaded.GetComponentsInChildren<MeshCollider>(true).Any(c=>c.sharedMesh==filter.sharedMesh&&c.enabled&&!c.isTrigger);proof.rawSourceStillNull=source.GetComponentsInChildren<MeshFilter>(true).Single().sharedMesh==null;
            }
            finally
            {
                if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);if(created){if(!folder.StartsWith(Root+"VisualCandidates/OwnedLadderPersistenceProbe-",StringComparison.Ordinal))throw new InvalidOperationException("Unexpected temporary asset cleanup scope.");AssetDatabase.DeleteAsset(folder);}proof.temporaryAssetsRemoved=!Directory.Exists(Io(folder));proof.sourceBytesPreserved=AssetSha(row.prefab)==row.sourceSha256&&AssetSha(row.prefab+".meta")==row.sourceMetaSha256&&(proof.repair==null||AssetSha(proof.repair.meshAsset)==proof.repair.meshAssetSha256&&AssetSha(proof.repair.meshAsset+".meta")==proof.repair.meshAssetMetaSha256);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"owned-ladder-instance-mesh-save-reload.json"),JsonUtility.ToJson(proof,true));
            }
            if(proof.repair==null||!proof.rawSourceStillNull||!proof.reloadedExactMesh||!proof.reloadedMatchingCollision||!proof.sourceBytesPreserved||!proof.temporaryAssetsRemoved)throw new InvalidOperationException("Actual owned ladder source/override persistence proof rejected: "+JsonUtility.ToJson(proof));return JsonUtility.ToJson(proof,true);
        }
        static OwnedArchitectureMeshRepair RepairOwnedArchitectureMeshAfterRawBaseline(ArchitectureInstanceSource row,Transform root,RidgeGeometryBinding[] rawBaseline)
        {
            if(row.sceneInstanceId!="1007988382"||row.prefabGuid!="231c22fbd5f9acf46901b04d3bda8604")return null;
            var audit=CaptureExactLadderMeshReferenceAudit();if(!audit.exactSources||!audit.sourceBytesPreserved||audit.requestedNativeMeshExists||!audit.otherNamedNativeMeshUnique||audit.prefab!=row.prefab||audit.prefabSha256!=row.sourceSha256||audit.prefabMetaSha256!=row.sourceMetaSha256||audit.missingPrefabScripts!=0||audit.missingModelScripts!=0)throw new InvalidOperationException("Exact V324 source/native ladder evidence required; no guessed mesh substitution.");
            const string filterId="33846281388073860";var filter=root.GetComponentsInChildren<MeshFilter>(true).Single(f=>SourceIdentityMatches(f,row.prefabGuid,filterId));if(filter.sharedMesh!=null||rawBaseline.Length!=1||rawBaseline[0].type!=typeof(MeshFilter).FullName||!string.IsNullOrEmpty(rawBaseline[0].mesh)||rawBaseline[0].primaryLod)throw new InvalidOperationException("Only the actual raw-null source ladder binding may receive this declared owned-instance correction.");
            var mesh=AssetDatabase.LoadAllAssetsAtPath(audit.meshAsset).OfType<Mesh>().Single(m=>m.name=="TB_Prop_Ladder"&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m,out string guid,out long id)&&guid==audit.meshAssetGuid&&id==4300002);var native=audit.allNativeSubassets.Single(a=>a.type==typeof(Mesh).FullName&&a.localId=="4300002");if(native.vertices!=216||native.indices!=324||!native.allVerticesFinite)throw new InvalidOperationException("Exact actual same-FBX ladder geometry must remain the measured216finite vertices/324indices.");
            var renderers=root.GetComponentsInChildren<Renderer>(true);var materials=renderers.Select(r=>r.sharedMaterials).ToArray();var position=root.localPosition;var rotation=root.localRotation;var scale=root.localScale;filter.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            var result=new OwnedArchitectureMeshRepair{instance=row.sceneInstanceId,sourcePrefab=row.prefab,sourcePrefabSha256=row.sourceSha256,sourcePrefabMetaSha256=row.sourceMetaSha256,filterLocalId=filterId,meshAsset=audit.meshAsset,meshAssetGuid=audit.meshAssetGuid,meshAssetSha256=audit.meshAssetSha256,meshAssetMetaSha256=audit.meshAssetMetaSha256,originalRequestedId="4300000",originalRequestedName="TB_Prop_Ledder",repairedId="4300002",repairedName=mesh.name,repairedVertices=mesh.vertexCount,repairedIndices=native.indices,rawNativeReferenceWasNull=true,sourcePoseAndMaterialsPreserved=root.localPosition==position&&root.localRotation==rotation&&root.localScale==scale&&renderers.Select((r,i)=>r.sharedMaterials.SequenceEqual(materials[i])).All(v=>v),sourceBytesPreserved=AssetSha(row.prefab)==row.sourceSha256&&AssetSha(row.prefab+".meta")==row.sourceMetaSha256&&AssetSha(audit.meshAsset)==audit.meshAssetSha256&&AssetSha(audit.meshAsset+".meta")==audit.meshAssetMetaSha256};
            if(!result.sourcePoseAndMaterialsPreserved||!result.sourceBytesPreserved)throw new InvalidOperationException("Owned ladder binding changed unrelated source pose/material/assets.");return result;
        }
    }
}
#endif
