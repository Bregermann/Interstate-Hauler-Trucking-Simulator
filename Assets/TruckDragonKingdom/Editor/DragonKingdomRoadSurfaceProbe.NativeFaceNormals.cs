#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Digger.Modules.Core.Sources;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable]public sealed class NativeFaceMeshEvidence
        {public string sourceMesh,filter,chunk;public int lod;public DragonKingdomNativeFaceGeometry.Evidence comparison;}
        [Serializable]public sealed class NativeFaceNormalsEvidence
        {
            public string scope="DISPOSABLE_EXISTING_NATIVE_FACE_POLYGONIZER_DIAGNOSIS_NOT_PRODUCTION_STYLE_OR_CAVE_ACCEPTANCE",tag,status="IN_PROGRESS",error;public int loadedVoxelChunks,repolygonizedMeshes,meshesWithIntendedNormalChanges;public NativeFaceMeshEvidence[] meshes=Array.Empty<NativeFaceMeshEvidence>();public Frame[] frames=Array.Empty<Frame>();
            public bool allNativeFaceNormalsVerified,allGeometryAndNonNormalAttributesExact,allVoxelBytesExact,physicsAndHeightsExact,sourceBytesExact,voxelMembershipExact,originalBindingsAndStateRestored,temporaryObjectsDestroyed;
        }
        public static NativeFaceNormalsEvidence LastNativeFaceNormals{get;private set;}
        static Dictionary<VoxelChunk,string> NativeFaceVoxelBytes(IEnumerable<DiggerSystem> systems)
        {
            var result=new Dictionary<VoxelChunk,string>();foreach(var chunk in systems.SelectMany(s=>s.GetComponentsInChildren<VoxelChunk>(true)).Where(c=>c.IsLoaded))using(var data=new NativeArray<Voxel>(chunk.VoxelArray,Allocator.Temp)){result.Add(chunk,Hash(data.Reinterpret<byte>(UnsafeUtility.SizeOf<Voxel>()).ToArray()));}return result;
        }
        static string NativeFaceTerrainState(DragonKingdomWorld world)
        {
            using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))foreach(var terrain in world.GetComponentsInChildren<Terrain>(true)){writer.Write(DragonKingdomWorldVisualMigration.HeightHash(terrain.terrainData));foreach(bool hole in terrain.terrainData.GetHoles(0,0,terrain.terrainData.holesResolution,terrain.terrainData.holesResolution))writer.Write(hole);}return Hash(stream.ToArray());}
        }
        // Call ONLY after fresh/copy excavation and shared finishing while the
        // native voxel arrays remain loaded. Never initializes a saved source
        // Digger system or alters its master/provider/collider configuration.
        public static NativeFaceNormalsEvidence CaptureNativeFaceNormalDiagnostic(DragonKingdomWorld world,string tag)
        {
            var proof=LastNativeFaceNormals=new NativeFaceNormalsEvidence{tag=tag};if(world==null||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped fresh owned candidate world required.");
            var systems=world.GetComponentsInChildren<DiggerSystem>(true);if(systems.Length==0||systems.Any(s=>s.Terrain==null||!DragonKingdomNativeFacePolygonizerProvider.IsOwnedCandidateContext(AssetDatabase.GetAssetPath(s.Terrain.terrainData),s.BasePathData,tag)))throw new InvalidOperationException("Exact fresh/copy candidate terrain and voxel identity required; canonical/source assets are forbidden.");
            var filters=systems.SelectMany(s=>s.GetComponentsInChildren<ChunkObject>(true)).Select(c=>c.GetComponent<MeshFilter>()).Where(f=>f!=null&&f.sharedMesh!=null&&f.sharedMesh.vertexCount>0).Distinct().ToArray();if(filters.Length==0)throw new InvalidOperationException("Actual existing nonempty native Digger meshes required.");
            var sourceMeshes=filters.ToDictionary(f=>f,f=>f.sharedMesh);var renderers=filters.Select(f=>f.GetComponent<Renderer>()).ToArray();if(renderers.Any(r=>r==null))throw new InvalidOperationException("Original native renderer bindings required.");var rendererState=renderers.ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r));var materialBindings=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);var materialState=materialBindings.Values.SelectMany(m=>m).Where(m=>m!=null).Distinct().ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));
            var colliders=world.GetComponentsInChildren<MeshCollider>(true).ToDictionary(c=>c,c=>c.sharedMesh);var colliderState=colliders.Keys.ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));var lods=systems.SelectMany(s=>s.GetComponentsInChildren<LODGroup>(true)).ToDictionary(g=>g,g=>EditorJsonUtility.ToJson(g));var lights=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>l,l=>EditorJsonUtility.ToJson(l)+EditorJsonUtility.ToJson(l.transform));
            string physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject),terrainState=NativeFaceTerrainState(world);var voxelBytes=NativeFaceVoxelBytes(systems);proof.loadedVoxelChunks=voxelBytes.Count;if(voxelBytes.Count==0||filters.Any(f=>f.GetComponentInParent<Chunk>()?.GetComponentsInChildren<VoxelChunk>(true).SingleOrDefault()?.IsLoaded!=true))throw new InvalidOperationException("Actual voxel arrays must already be loaded by fresh/copy excavation; saved-scene initialization is forbidden.");
            var folders=systems.Select(s=>s.BasePathData).Distinct().ToArray();string[] Members()=>folders.SelectMany(p=>Directory.GetFiles(p,"*",SearchOption.AllDirectories)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();var members=Members();var assets=EditorUtility.CollectDependencies(new UnityEngine.Object[]{world.gameObject}).Select(AssetDatabase.GetAssetPath).Where(p=>!string.IsNullOrEmpty(p)).Distinct().SelectMany(p=>AssetDatabase.GetDependencies(p,true));var files=assets.Concat(members).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(IoPath(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(IoPath(p))));
            var temporaryMeshes=new List<Mesh>();var providers=new Dictionary<DiggerSystem,DragonKingdomNativeFacePolygonizerProvider>();var originalNativeProviders=systems.ToDictionary(s=>s,s=>s.PolygonizerProvider);var rows=new List<NativeFaceMeshEvidence>();Camera camera=null;bool async=ShaderUtil.allowAsyncCompilation;
            bool BindingsRestored()=>originalNativeProviders.All(p=>p.Key!=null&&p.Key.PolygonizerProvider==p.Value)&&sourceMeshes.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value)&&rendererState.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)&&materialBindings[p.Key].SequenceEqual(p.Key.sharedMaterials))&&materialState.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key))&&colliders.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value&&colliderState[p.Key]==EditorJsonUtility.ToJson(p.Key))&&lods.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key))&&lights.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform));
            try
            {
                ShaderUtil.allowAsyncCompilation=false;foreach(var system in systems){var provider=new DragonKingdomNativeFacePolygonizerProvider();provider.ConfigureCandidate(system,tag);providers.Add(system,provider);}
                var goCamera=new GameObject("Temporary native face-normal inspection camera"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(goCamera,world.gameObject.scene);camera=goCamera.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=67;camera.nearClipPlane=.15f;camera.farClipPlane=6500;camera.aspect=(float)Width/Height;camera.clearFlags=CameraClearFlags.Skybox;camera.transform.SetPositionAndRotation(new Vector3(448,113,780),Quaternion.LookRotation(new Vector3(530,134,799)-new Vector3(448,113,780),Vector3.up));string cameraState=EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform);
                var shader=Shader.Find("Digger/Mesh/URP/Lit-Pass0");if(shader==null||!shader.isSupported)throw new InvalidOperationException("Original native URP shader required.");var roads=new HashSet<Collider>(world.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.transform.parent!=null&&c.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)));string output=Output();Directory.CreateDirectory(output);
                foreach(var filter in filters)
                {
                    var source=sourceMeshes[filter];var chunk=filter.GetComponentInParent<Chunk>();var voxel=chunk.GetComponentsInChildren<VoxelChunk>(true).Single();var group=filter.GetComponentInParent<ChunkLODGroup>();int level=filter.transform.GetSiblingIndex();if(group==null||level<0||level>=group.LODCount||filter.GetComponent<ChunkObject>()?.Digger!=voxel.Digger)throw new InvalidOperationException("Exact native chunk/group/voxel ownership and sibling LOD identity required.");int lod=ChunkLODGroup.IndexToLod(level);
                    var result=new Mesh{name=source.name+" temporary native face-normal result",hideFlags=HideFlags.HideAndDontSave};temporaryMeshes.Add(result);var polygonizer=providers[voxel.Digger].NewPolygonizer(voxel.Digger);var handle=polygonizer.BuildMesh(voxel,lod);handle.Complete();if(!polygonizer.CompleteBuildMesh(result,source.bounds))throw new InvalidOperationException("Native face polygonizer failed to produce the original nonempty chunk.");
                    var row=new NativeFaceMeshEvidence{sourceMesh=AssetDatabase.GetAssetPath(source),filter=GlobalObjectId.GetGlobalObjectIdSlow(filter).ToString(),chunk=chunk.name,lod=lod,comparison=DragonKingdomNativeFaceGeometry.Compare(source,result)};rows.Add(row);proof.meshes=rows.ToArray();proof.repolygonizedMeshes++;if(!row.comparison.originalNormalAwareCanonicalExact)proof.meshesWithIntendedNormalChanges++;
                    if(!row.comparison.geometryExact||!row.comparison.nonNormalAttributesExact||!row.comparison.nativeFaceNormalsVerified||result.bounds!=source.bounds)throw new InvalidOperationException("Native face-only result changed positions/winding/submesh/non-normal attributes or failed genuine unit face normals: "+chunk.name+"/LOD"+lod+" "+JsonUtility.ToJson(row.comparison));filter.sharedMesh=result;
                }
                proof.allGeometryAndNonNormalAttributesExact=rows.All(r=>r.comparison.geometryExact&&r.comparison.nonNormalAttributesExact);proof.allNativeFaceNormalsVerified=rows.All(r=>r.comparison.nativeFaceNormalsVerified);if(proof.meshesWithIntendedNormalChanges==0)throw new InvalidOperationException("Meaningful face-normal diagnosis requires an actual intended stored-normal change.");
                var face=Capture(camera,world,roads,shader,output,"rounded-entry-native-face-diagnostic","native-face-normals");proof.frames=new[]{face};if(face.shaderErrors.Length!=0||cameraState!=EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform))throw new InvalidOperationException("Native face diagnostic changed camera or reported shader errors.");
                foreach(var pair in sourceMeshes)pair.Key.sharedMesh=pair.Value;if(!BindingsRestored())throw new InvalidOperationException("Restored exact original mesh/collider/material/LOD/light bindings required before paired image.");var restored=Capture(camera,world,roads,shader,output,"rounded-entry-native-face-diagnostic","original-normals-restored");proof.frames=new[]{face,restored};if(restored.shaderErrors.Length!=0||cameraState!=EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform))throw new InvalidOperationException("Restored native-normal diagnostic changed camera or reported shader errors.");proof.status="NATIVE_FACE_NORMAL_PAIR_RECORDED_REQUIRES_DIRECT_VISUAL_REVIEW";
            }
            catch(Exception error){proof.error=error.ToString();proof.status="REJECTED_NATIVE_FACE_NORMAL_DIAGNOSTIC";throw;}
            finally
            {
                foreach(var pair in sourceMeshes)if(pair.Key!=null)pair.Key.sharedMesh=pair.Value;foreach(var mesh in temporaryMeshes)if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);ShaderUtil.allowAsyncCompilation=async;
                proof.temporaryObjectsDestroyed=temporaryMeshes.All(m=>m==null)&&camera==null;proof.originalBindingsAndStateRestored=BindingsRestored();var afterVoxels=NativeFaceVoxelBytes(systems);proof.allVoxelBytesExact=voxelBytes.Count==afterVoxels.Count&&voxelBytes.All(p=>afterVoxels.TryGetValue(p.Key,out string value)&&value==p.Value);proof.physicsAndHeightsExact=physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject)&&terrainState==NativeFaceTerrainState(world);proof.voxelMembershipExact=members.SequenceEqual(Members());proof.sourceBytesExact=files.All(p=>File.Exists(IoPath(p.Key))&&Hash(File.ReadAllBytes(IoPath(p.Key)))==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-native-face-normal-diagnostic.json"),JsonUtility.ToJson(proof,true));
                if(!proof.temporaryObjectsDestroyed||!proof.originalBindingsAndStateRestored||!proof.allVoxelBytesExact||!proof.physicsAndHeightsExact||!proof.voxelMembershipExact||!proof.sourceBytesExact)throw new InvalidOperationException("Native face-normal diagnosis failed exact voxel/position/collision/material/source restoration or temporary cleanup.");
            }
            return proof;
        }
    }
}
#endif
