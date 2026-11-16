#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class ArchitectureNativeMeshAsset
        {public string name,type,guid,localId,globalId;public int vertices,subMeshes,indices;public Bounds declaredBounds,actualVertexBounds;public bool allVerticesFinite;}
        [Serializable]public sealed class ArchitecturePrefabMeshReference
        {public string hierarchyPath,filterGuid,filterLocalId,nativeMeshGuid,nativeMeshLocalId,nativeMeshName;public bool authoredActive,rendererEnabled,hasNativeMesh;public int nativeVertexCount;}
        [Serializable]public sealed class ArchitectureRawModelIdName{public string localId,name;}
        [Serializable]public sealed class ArchitectureNativeExternalReference{public string sourceName,sourceType,targetName,targetType,targetGuid,targetLocalId;}
        [Serializable]public sealed class ArchitectureLadderMeshAudit
        {
            public string scope="READ_ONLY_SINGLE_EXACT_SOURCE_LADDER_PREFAB_FBX_MODELIMPORTER_NATIVE_SUBASSET_IDENTITY_NOT_A_BINDING_REPAIR_OR_WORLD_ACCEPTANCE",status="IN_PROGRESS",error,layout,instance,prefab,prefabGuid,prefabSha256,prefabMetaSha256,meshAsset,meshAssetGuid,meshAssetSha256,meshAssetMetaSha256,requestedLocalId,requestedLegacyName,otherLegacyLocalId,otherLegacyName,importerType,importerSerializedJsonSha256;
            public bool exactSources,rawPrefabReferenceConfirmed,requestedNativeMeshExists,otherNamedNativeMeshUnique,sourceBytesPreserved;public int missingPrefabScripts,missingModelScripts,sourceLodGroups;public ArchitectureNativeMeshAsset[] allNativeSubassets;public ArchitecturePrefabMeshReference[] prefabReferences,importedModelReferences;public ArchitectureRawModelIdName[] allLegacyModelIdNames;public ArchitectureNativeExternalReference[] publicImporterExternalReferences;public string[] changedSources;
        }
        public static ArchitectureLadderMeshAudit CaptureExactLadderMeshReferenceAudit()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped native Editor required for exact read-only mesh identity audit.");
            string layoutPath=ArchitectureLayoutPaths[1];if(AssetSha(layoutPath)!=ArchitectureLayoutHashes[1])throw new InvalidOperationException("Exact complete lumber source layout required.");var layout=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(layoutPath)));var row=layout.instances.Single(i=>i.sceneInstanceId=="1007988382");var dependency=layout.dependencies.Single(d=>d.guid=="85c50bb430239ac44ac2cbc5b096ff4a");
            var proof=new ArchitectureLadderMeshAudit{layout=layoutPath,instance=row.sceneInstanceId,prefab=row.prefab,prefabGuid=row.prefabGuid,prefabSha256=row.sourceSha256,prefabMetaSha256=row.sourceMetaSha256,meshAsset=dependency.path,meshAssetGuid=dependency.guid,meshAssetSha256=dependency.sha256,meshAssetMetaSha256=dependency.metaSha256,requestedLocalId="4300000",requestedLegacyName="TB_Prop_Ledder",otherLegacyLocalId="4300002",otherLegacyName="TB_Prop_Ladder"};
            var files=new[]{layoutPath,layoutPath+".meta",row.prefab,row.prefab+".meta",dependency.path,dependency.path+".meta"}.ToDictionary(path=>path,AssetSha);
            try
            {
                proof.exactSources=AssetSha(row.prefab)==row.sourceSha256&&AssetSha(row.prefab+".meta")==row.sourceMetaSha256&&AssetDatabase.AssetPathToGUID(row.prefab)==row.prefabGuid&&AssetSha(dependency.path)==dependency.sha256&&AssetSha(dependency.path+".meta")==dependency.metaSha256&&AssetDatabase.AssetPathToGUID(dependency.path)==dependency.guid;if(!proof.exactSources)throw new InvalidOperationException("Exact pinned original ladder prefab/FBX/meta bytes and GUIDs required.");
                string raw=File.ReadAllText(Io(row.prefab)),meta=File.ReadAllText(Io(dependency.path+".meta"));proof.rawPrefabReferenceConfirmed=Regex.IsMatch(raw,@"m_Mesh: \{fileID: 4300000, guid: 85c50bb430239ac44ac2cbc5b096ff4a, type: 3\}")&&Regex.IsMatch(meta,@"(?m)^    4300000: TB_Prop_Ledder\r?$")&&Regex.IsMatch(meta,@"(?m)^    4300002: TB_Prop_Ladder\r?$");if(!proof.rawPrefabReferenceConfirmed)throw new InvalidOperationException("Actual original requested mesh and two legacy recycle-name mappings differ.");
                var mapping=Regex.Match(meta,@"(?ms)^  fileIDToRecycleName:\r?\n(.*?)(?=^  [A-Za-z]|\z)").Groups[1].Value;proof.allLegacyModelIdNames=Regex.Matches(mapping,@"(?m)^    ([-0-9]+): (.*?)\r?$").Cast<Match>().Select(m=>new ArchitectureRawModelIdName{localId=m.Groups[1].Value,name=m.Groups[2].Value}).ToArray();

                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);var model=AssetDatabase.LoadAssetAtPath<GameObject>(dependency.path);var importer=AssetImporter.GetAtPath(dependency.path) as ModelImporter;if(prefab==null||model==null||importer==null)throw new InvalidOperationException("Native original source prefab, imported FBX model and public ModelImporter required.");proof.importerType=importer.GetType().FullName;proof.importerSerializedJsonSha256=Hash(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(importer))).Replace("-","").ToLowerInvariant();proof.sourceLodGroups=prefab.GetComponentsInChildren<LODGroup>(true).Length;
                proof.publicImporterExternalReferences=importer.GetExternalObjectMap().Select(pair=>{string guid=null;long id=0;if(pair.Value!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pair.Value,out guid,out id);return new ArchitectureNativeExternalReference{sourceName=pair.Key.name,sourceType=pair.Key.type.FullName,targetName=pair.Value==null?null:pair.Value.name,targetType=pair.Value==null?null:pair.Value.GetType().FullName,targetGuid=guid,targetLocalId=id.ToString(System.Globalization.CultureInfo.InvariantCulture)};}).ToArray();

                proof.missingPrefabScripts=prefab.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));proof.missingModelScripts=model.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));var assets=new List<ArchitectureNativeMeshAsset>();
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(dependency.path).Where(a=>a!=null))
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long id);var value=new ArchitectureNativeMeshAsset{name=asset.name,type=asset.GetType().FullName,guid=guid,localId=id.ToString(System.Globalization.CultureInfo.InvariantCulture),globalId=GlobalObjectId.GetGlobalObjectIdSlow(asset).ToString()};
                    if(asset is Mesh mesh)
                    {
                        value.vertices=mesh.vertexCount;value.subMeshes=mesh.subMeshCount;value.declaredBounds=mesh.bounds;value.allVerticesFinite=true;using(var data=Mesh.AcquireReadOnlyMeshData(mesh))using(var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
                        {data[0].GetVertices(vertices);bool first=true;var bounds=new Bounds();foreach(var vertex in vertices){value.allVerticesFinite&=ArchitectureFinite(vertex);if(first){bounds=new Bounds(vertex,Vector3.zero);first=false;}else bounds.Encapsulate(vertex);}value.actualVertexBounds=bounds;for(int i=0;i<data[0].subMeshCount;i++)value.indices+=data[0].GetSubMesh(i).indexCount;}
                    }
                    assets.Add(value);
                }
                proof.allNativeSubassets=assets.ToArray();proof.requestedNativeMeshExists=assets.Any(a=>a.type==typeof(Mesh).FullName&&a.guid==dependency.guid&&a.localId==proof.requestedLocalId);proof.otherNamedNativeMeshUnique=assets.Count(a=>a.type==typeof(Mesh).FullName&&a.guid==dependency.guid&&a.localId==proof.otherLegacyLocalId&&a.name==proof.otherLegacyName&&a.vertices>0&&a.allVerticesFinite)==1;
                proof.prefabReferences=prefab.GetComponentsInChildren<MeshFilter>(true).Select(filter=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter,out string guid,out long id);string mg=null;long mi=0;if(filter.sharedMesh!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh,out mg,out mi);return new ArchitecturePrefabMeshReference{hierarchyPath=PathOf(filter.transform,prefab.transform),filterGuid=guid,filterLocalId=id.ToString(System.Globalization.CultureInfo.InvariantCulture),authoredActive=RidgeGeometryObjectActive(filter.transform),rendererEnabled=filter.GetComponent<MeshRenderer>()!=null&&filter.GetComponent<MeshRenderer>().enabled,hasNativeMesh=filter.sharedMesh!=null,nativeMeshGuid=mg,nativeMeshLocalId=mi.ToString(System.Globalization.CultureInfo.InvariantCulture),nativeMeshName=filter.sharedMesh==null?null:filter.sharedMesh.name,nativeVertexCount=filter.sharedMesh==null?0:filter.sharedMesh.vertexCount};}).ToArray();
                proof.importedModelReferences=model.GetComponentsInChildren<MeshFilter>(true).Select(filter=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter,out string guid,out long id);string mg=null;long mi=0;if(filter.sharedMesh!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh,out mg,out mi);return new ArchitecturePrefabMeshReference{hierarchyPath=PathOf(filter.transform,model.transform),filterGuid=guid,filterLocalId=id.ToString(System.Globalization.CultureInfo.InvariantCulture),authoredActive=RidgeGeometryObjectActive(filter.transform),rendererEnabled=filter.GetComponent<MeshRenderer>()!=null&&filter.GetComponent<MeshRenderer>().enabled,hasNativeMesh=filter.sharedMesh!=null,nativeMeshGuid=mg,nativeMeshLocalId=mi.ToString(System.Globalization.CultureInfo.InvariantCulture),nativeMeshName=filter.sharedMesh==null?null:filter.sharedMesh.name,nativeVertexCount=filter.sharedMesh==null?0:filter.sharedMesh.vertexCount};}).ToArray();
                proof.status="EXACT_SOURCE_PREFAB_REFERENCE_AND_ALL_NATIVE_MODEL_SUBASSETS_OBSERVED_NOT_AUTOMATIC_REPAIR";
            }
            catch(Exception e){proof.error=e.ToString();proof.status="REJECTED_EXACT_LADDER_MESH_AUDIT";throw;}
            finally{proof.changedSources=files.Where(p=>AssetSha(p.Key)!=p.Value).Select(p=>p.Key).ToArray();proof.sourceBytesPreserved=proof.changedSources.Length==0;Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"exact-ladder-prefab-fbx-native-mesh-reference-audit.json"),JsonUtility.ToJson(proof,true));if(!proof.sourceBytesPreserved)throw new InvalidOperationException("Read-only source ladder identity audit changed original assets.");}
            return proof;
        }
    }
}
#endif
