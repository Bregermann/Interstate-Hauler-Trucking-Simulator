#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonForestMaterialAuthoring
    {
        [Serializable] sealed class WorldViewInput {public string preview=null,prefab=null;public int width=0,height=0;public RegionViewInput[] regions=null;}
        [Serializable] sealed class RegionViewInput {public string id=null;public ViewInput eye=null,overview=null;}
        [Serializable] sealed class ViewInput {public Vector3 cameraPosition=default,cameraEuler=default;public string png=null;}
        [Serializable] sealed class MeshFact {public string name,mesh;public int vertices,colorVertices;public Bounds bounds;public string[] materials,shaders;}
        [Serializable] sealed class LodSurface {public int level;public float transition;public MeshFact[] renderers;}
        [Serializable] sealed class TreeProjection {public string path;public Vector3 reference;public float worldSize,distance,relativeHeightWithLodBias;public float[] transitions;public int predictedLod;public bool inFrustum;public LodSurface[] surfaces;}
        [Serializable] sealed class ViewProjection {public string originalPng,kind;public Vector3 cameraPosition,cameraEuler;public float fov,lodBias;public int maximumLodLevel,inFrustum,predictedCulled;public TreeProjection[] trees;}
        [Serializable] sealed class ProjectionReceipt {public string scope="Read-only actual native LOD metadata and projection using the exact retained world-review camera poses (58-degree protocol). LOD prediction is distance/size math, not a claimed renderer selection or pixel acceptance. No ForceLOD or production threshold tuning.";public string originalReview,preview;public bool sceneBytesExact,prefabBytesExact;public ViewProjection[] views;}
        public static int CaptureForestLodProjection()
        {
            Guard();var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonForestViewEvidence");if(at<0||at+1>=args.Length)throw new ArgumentException("Supply exact retained world-render-review.json through -tsDragonForestViewEvidence; no guessed camera.");
            string input=args[at+1];var review=JsonUtility.FromJson<WorldViewInput>(File.ReadAllText(input));if(review==null||review.regions==null||review.width!=1280||review.height!=720||!File.Exists(review.preview)||!File.Exists(review.prefab))throw new InvalidOperationException("Actual retained saved-world 1280x720 camera review required.");
            var region=review.regions.Single(value=>value.id=="forest");string sceneHash=Hash(review.preview),prefabHash=Hash(review.prefab);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;bool opened=false;GameObject cameraObject=null;ViewProjection[] views=null;
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty user scene before read-only LOD projection.");
            try
            {
                scene=SceneManager.GetSceneByPath(review.preview);if(!scene.IsValid()||!scene.isLoaded){scene=EditorSceneManager.OpenScene(review.preview,OpenSceneMode.Additive);opened=true;}
                var world=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();var forest=world.Region("forest");
                var groups=forest.root.GetComponentsInChildren<LODGroup>(true).Where(group=>group.name=="P_ENV_tree_ancient_03").ToArray();if(groups.Length==0)throw new InvalidOperationException("Actual tree03 LOD groups required; no density substitute.");
                cameraObject=new GameObject("Read-only original forest camera projection");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.aspect=16f/9;camera.nearClipPlane=.3f;camera.farClipPlane=6500;
                views=new[]{Tuple.Create("eye",region.eye),Tuple.Create("overview",region.overview)}.Select(pair=>
                {
                    camera.transform.SetPositionAndRotation(pair.Item2.cameraPosition,Quaternion.Euler(pair.Item2.cameraEuler));var planes=GeometryUtility.CalculateFrustumPlanes(camera);
                    var trees=groups.Select(group=>
                    {
                        var lods=group.GetLODs();Vector3 scale=group.transform.lossyScale;float size=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));Vector3 reference=group.transform.TransformPoint(group.localReferencePoint);float distance=Vector3.Distance(reference,camera.transform.position);float relative=size*QualitySettings.lodBias/Mathf.Max(.0001f,2*distance*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad));int level=-1;
                        for(int i=0;i<lods.Length;i++)if(relative>=lods[i].screenRelativeTransitionHeight){level=Mathf.Min(lods.Length-1,Mathf.Max(i,QualitySettings.maximumLODLevel));break;}
                        var renderers=lods.SelectMany(lod=>lod.renderers).Where(renderer=>renderer!=null).Distinct().ToArray();
                        return new TreeProjection{path=TreePath(group.transform),reference=reference,worldSize=size,distance=distance,relativeHeightWithLodBias=relative,transitions=lods.Select(lod=>lod.screenRelativeTransitionHeight).ToArray(),predictedLod=level,inFrustum=renderers.Any(renderer=>GeometryUtility.TestPlanesAABB(planes,renderer.bounds)),surfaces=lods.Select((lod,lodIndex)=>new LodSurface{level=lodIndex,transition=lod.screenRelativeTransitionHeight,renderers=lod.renderers.Where(renderer=>renderer!=null).Select(renderer=>
                        {
                            var filter=renderer.GetComponent<MeshFilter>();var mesh=filter==null?null:filter.sharedMesh;return new MeshFact{name=renderer.name,mesh=mesh==null?"none":AssetDatabase.GetAssetPath(mesh)+"#"+mesh.name,vertices=mesh==null?0:mesh.vertexCount,colorVertices=mesh!=null&&mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)?mesh.vertexCount:0,bounds=renderer.bounds,materials=renderer.sharedMaterials.Select(material=>material==null?"none":AssetDatabase.GetAssetPath(material)).ToArray(),shaders=renderer.sharedMaterials.Select(material=>material==null||material.shader==null?"none":material.shader.name).ToArray()};
                        }).ToArray()}).ToArray()};
                    }).ToArray();return new ViewProjection{originalPng=pair.Item2.png,kind=pair.Item1,cameraPosition=camera.transform.position,cameraEuler=camera.transform.eulerAngles,fov=58,lodBias=QualitySettings.lodBias,maximumLodLevel=QualitySettings.maximumLODLevel,inFrustum=trees.Count(tree=>tree.inFrustum),predictedCulled=trees.Count(tree=>tree.inFrustum&&tree.predictedLod<0),trees=trees};
                }).ToArray();
            }
            finally{if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            bool sceneExact=sceneHash==Hash(review.preview),prefabExact=prefabHash==Hash(review.prefab);if(!sceneExact||!prefabExact)throw new InvalidOperationException("Read-only projection changed saved scene/prefab.");
            var receipt=new ProjectionReceipt{originalReview=input,preview=review.preview,sceneBytesExact=sceneExact,prefabBytesExact=prefabExact,views=views};string json=JsonUtility.ToJson(receipt,true);
            var roundTrip=JsonUtility.FromJson<ProjectionReceipt>(json);if(roundTrip.views.Any(view=>view.trees.Any(tree=>tree.surfaces==null||tree.surfaces.Length!=3||tree.surfaces.Any(lod=>lod.renderers==null||lod.renderers.Length==0))))throw new InvalidOperationException("All three actual mesh/material LOD surfaces must survive evidence serialization.");
            at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Unique dragon evidence output required.");string path=Path.Combine(args[at+1],"ForestMaterialProof","actual-view-lod-projection.json");Directory.CreateDirectory(Path.GetDirectoryName(path));if(File.Exists(path))throw new IOException("Preserve previous projection evidence.");File.WriteAllText(path,json+"\n");Debug.Log("FOREST_ACTUAL_VIEW_LOD_PROJECTION "+path);return views[0].trees.Length;
        }
        static string TreePath(Transform value)=>value.parent==null?value.name:TreePath(value.parent)+"/"+value.name;
    }
}
#endif
