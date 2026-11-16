#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonForestSteadyViewNativeTests
{
    [Serializable] sealed class Review {public string preview=null,prefab=null;public Region[] regions=null;}
    [Serializable] sealed class Region {public string id=null;public OriginalView eye=null,overview=null;}
    [Serializable] sealed class OriginalView {public Vector3 cameraPosition=default,cameraEuler=default;public string png=null;}
    [Serializable] sealed class Surface {public string path,mesh;public bool enabled,active,forceOff,visible,frustum;public Bounds bounds;public string[] materials,shaders;}
    [Serializable] sealed class Lod {public int index;public float threshold;public Surface[] surfaces;}
    [Serializable] sealed class Tree {public string path,fadeMode;public bool animateCrossFading;public Vector3 reference;public float worldSize;public Lod[] lods;}
    [Serializable] sealed class Frame {public int frame;public double elapsedReal;public Tree[] trees;}
    [Serializable] sealed class View {public string kind,originalPng,startPng,settledPng;public Vector3 cameraPosition,cameraEuler;public float fov,lodBias,crossFadeDuration;public int maximumLodLevel;public Frame[] frames;}
    [Serializable] sealed class Evidence {public string scope="Actual saved forest at retained V125 camera poses after genuine Editor frames/player-loop updates and authored LOD crossfade duration. No ForceLOD, threshold, material, transform or scoring changes. Renderer.isVisible is recorded, not asserted to uniquely identify this camera's selected LOD. Controlled native images still require visual review.";public string sourceReview,preview;public bool sceneBytesExact,prefabBytesExact,lodConfigurationExact;public View[] views;}
    static string PathOf(Transform value)=>value.parent==null?value.name:PathOf(value.parent)+"/"+value.name;
    static string Hash(string path){using(var sha=SHA256.Create())using(var input=File.OpenRead(path))return string.Concat(sha.ComputeHash(input).Select(value=>value.ToString("x2")));}
    static Tree[] Observe(LODGroup[] groups,Camera camera)
    {
        var planes=GeometryUtility.CalculateFrustumPlanes(camera);return groups.Select(group=>new Tree{path=PathOf(group.transform),fadeMode=group.fadeMode.ToString(),animateCrossFading=group.animateCrossFading,reference=group.transform.TransformPoint(group.localReferencePoint),worldSize=group.size*Mathf.Max(Mathf.Abs(group.transform.lossyScale.x),Mathf.Abs(group.transform.lossyScale.y),Mathf.Abs(group.transform.lossyScale.z)),lods=group.GetLODs().Select((lod,index)=>new Lod{index=index,threshold=lod.screenRelativeTransitionHeight,surfaces=lod.renderers.Where(renderer=>renderer!=null).Select(renderer=>
        {var filter=renderer.GetComponent<MeshFilter>();return new Surface{path=PathOf(renderer.transform),mesh=filter==null||filter.sharedMesh==null?"none":AssetDatabase.GetAssetPath(filter.sharedMesh)+"#"+filter.sharedMesh.name,enabled=renderer.enabled,active=renderer.gameObject.activeInHierarchy,forceOff=renderer.forceRenderingOff,visible=renderer.isVisible,frustum=GeometryUtility.TestPlanesAABB(planes,renderer.bounds),bounds=renderer.bounds,materials=renderer.sharedMaterials.Select(material=>material==null?"none":AssetDatabase.GetAssetPath(material)).ToArray(),shaders=renderer.sharedMaterials.Select(material=>material==null||material.shader==null?"none":material.shader.name).ToArray()};}).ToArray()}).ToArray()}).ToArray();
    }
    static void Capture(Camera camera,RenderTexture rt,string path)
    {
        var previous=RenderTexture.active;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        try{camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
    }
    [UnityTest,Timeout(240000)]
    public IEnumerator OriginalForestEyeAndOverviewSettleWithAuthoredLodsAfterRealFrames()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonForestViewEvidence");Assert.That(at>=0&&at+1<args.Length,Is.True);string source=args[at+1];var review=JsonUtility.FromJson<Review>(File.ReadAllText(source));var region=review.regions.Single(value=>value.id=="forest");
        at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);string output=Path.Combine(args[at+1],"ForestSteadyViews");Directory.CreateDirectory(output);
        foreach(var pair in new[]{Tuple.Create("a35168ec5f8b41447ba5916c93191534","Foliage Wind"),Tuple.Create("d768d77458b842644b856380df9a044f","PBR Vertex Color Fade")})
        {var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDragonKingdom/Materials/Environment/"+pair.Item1+" URP.mat");Assert.That(material,Is.Not.Null);Assert.That(material.shader.name,Does.Contain(pair.Item2),"Run the explicit canonical two-material upgrade before steady world review.");}
        for(int i=0;i<SceneManager.sceneCount;i++)Assert.That(SceneManager.GetSceneAt(i).isDirty,Is.False,"Preserve dirty user scenes.");var setup=EditorSceneManager.GetSceneManagerSetup();string sceneHash=Hash(review.preview),prefabHash=Hash(review.prefab);bool priorAsync=ShaderUtil.allowAsyncCompilation;Scene scene=default;bool opened=false;GameObject owner=null;RenderTexture rt=null;var views=new List<View>();Dictionary<LODGroup,string> groupConfig=null;
        try
        {
            ShaderUtil.allowAsyncCompilation=false;scene=SceneManager.GetSceneByPath(review.preview);if(!scene.IsValid()||!scene.isLoaded){scene=EditorSceneManager.OpenScene(review.preview,OpenSceneMode.Additive);opened=true;}
            var worldType=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);var world=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(worldType,true)).Single();var forest=worldType.GetMethod("Region").Invoke(world,new object[]{"forest"});var root=(Transform)forest.GetType().GetField("root").GetValue(forest);var groups=root.GetComponentsInChildren<LODGroup>(true).Where(group=>group.name=="P_ENV_tree_ancient_03").ToArray();Assert.That(groups.Length,Is.GreaterThan(0));groupConfig=groups.ToDictionary(group=>group,group=>EditorJsonUtility.ToJson(group));
            owner=new GameObject("Actual steady forest retained-view camera");var camera=owner.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.aspect=16f/9;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.cullingMask=~0;camera.clearFlags=CameraClearFlags.Skybox;camera.allowHDR=true;rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
            foreach(var entry in new[]{Tuple.Create("eye",region.eye),Tuple.Create("overview",region.overview)})
            {
                camera.transform.SetPositionAndRotation(entry.Item2.cameraPosition,Quaternion.Euler(entry.Item2.cameraEuler));string start=Path.Combine(output,entry.Item1+"-first-frame.png"),settled=Path.Combine(output,entry.Item1+"-steady-authored-lods.png");Capture(camera,rt,start);double began=EditorApplication.timeSinceStartup;var frames=new List<Frame>();int frame=0;double required=Math.Max(1.5,LODGroup.crossFadeAnimationDuration+.5);
                do
                {
                    EditorApplication.QueuePlayerLoopUpdate();yield return null;camera.Render();frame++;
                    if(frame<=3||frame%4==0)frames.Add(new Frame{frame=frame,elapsedReal=EditorApplication.timeSinceStartup-began,trees=Observe(groups,camera)});
                }while(frame<12||EditorApplication.timeSinceStartup-began<required);
                Capture(camera,rt,settled);frames.Add(new Frame{frame=frame,elapsedReal=EditorApplication.timeSinceStartup-began,trees=Observe(groups,camera)});Assert.That(frame,Is.GreaterThanOrEqualTo(12));Assert.That(EditorApplication.timeSinceStartup-began,Is.GreaterThanOrEqualTo(required));
                views.Add(new View{kind=entry.Item1,originalPng=entry.Item2.png,startPng=start,settledPng=settled,cameraPosition=camera.transform.position,cameraEuler=camera.transform.eulerAngles,fov=58,lodBias=QualitySettings.lodBias,maximumLodLevel=QualitySettings.maximumLODLevel,crossFadeDuration=LODGroup.crossFadeAnimationDuration,frames=frames.ToArray()});
                Assert.That(groups.All(group=>EditorJsonUtility.ToJson(group)==groupConfig[group]),Is.True,"Authored production LOD settings must remain exact.");
                File.WriteAllText(Path.Combine(output,"steady-view-observations.json"),JsonUtility.ToJson(new Evidence{sourceReview=source,preview=review.preview,sceneBytesExact=sceneHash==Hash(review.preview),prefabBytesExact=prefabHash==Hash(review.prefab),lodConfigurationExact=true,views=views.ToArray()},true));
            }
        }
        finally{if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(owner!=null)UnityEngine.Object.DestroyImmediate(owner);if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);ShaderUtil.allowAsyncCompilation=priorAsync;}
        Assert.That(Hash(review.preview),Is.EqualTo(sceneHash));Assert.That(Hash(review.prefab),Is.EqualTo(prefabHash));TestContext.WriteLine("ACTUAL_STEADY_FOREST_VIEWS "+output+" · inspect retained pixels; no production LOD tuning or whole-world acceptance inferred.");
    }
}
#endif
