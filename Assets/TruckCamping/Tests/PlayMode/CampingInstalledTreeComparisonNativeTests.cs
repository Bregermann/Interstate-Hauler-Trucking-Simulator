#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using static WildernessNativeScope;

// Controlled native asset comparison only. Every model/material/LOD is real;
// transient copies never replace terrain prototypes or create production assets.
public sealed class CampingInstalledTreeComparisonNativeTests
{
    const float Height=16.6f;
    const string Vista="Assets/TruckCamping/Prefabs/Camp Haul living Vista pine.prefab";
    static readonly string[] Sources={Vista,"Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_03.prefab","Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_07.prefab"};
    static readonly string[] Names={"actual-owned-vista","installed-jc03","installed-jc07"};
    [Serializable]sealed class Input { public string path,sha256; }
    [Serializable]sealed class MaterialFact { public string path,shader,texture;public bool supported;public int queue;public float surface,alphaClip; }
    [Serializable]sealed class ModelFact { public string name,source;public float height;public Vector3 boundsSize;public string[] dependencies,meshPaths;public float[] lodThresholds;public MaterialFact[] materials;public int disabledTransientColliders; }
    [Serializable]sealed class FrameFact { public string file,sha256,layout,detail;public int width,height,forcedLod;public float distance,fieldOfView;public Vector3 cameraPosition;public Quaternion cameraRotation;public Vector3[] centres,sizes,viewportMin,viewportMax; }
    [Serializable]sealed class Report
    {
        public string scope="CONTROLLED_NATIVE_REAL_TREE_COMPARISON_NOT_WINDOWS_OR_BEAUTY_ACCEPTANCE",runId,pipeline,weather,timeChoice;
        public bool originalHadAdditionalData,comparisonHadAdditionalData;public int requestedRendererIndex;public string cameraDataPolicy;
        public Vector3 identicalSingleTreeAnchor;public float uniformHeight;public int originalTreeInstances;public bool terrainAndVendorAssetsUnchanged;
        public ModelFact[] models;public Input[] inputs;public FrameFact[] frames;
        public string ordering="Side-by-side from camera left: actual owned Vista, installed JC03, installed JC07. Single-tree captures use the same XYZ/rotation/height/camera per distance.";
    }
    static string Hash(string path){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static Bounds BoundsOf(GameObject root){var renderers=root.GetComponentsInChildren<MeshRenderer>(true);Assert.That(renderers.Length,Is.GreaterThan(0));var b=renderers[0].bounds;foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);return b;}
    static string TerrainDigest(TerrainData data)
    {
        var author=Type.GetType("LWS.TruckCamping.Editor.CampingLivingForestAuthoring, Assembly-CSharp-Editor",true);
        return (string)author.GetMethod("PreservedTerrainDigest",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{data});
    }
    static float Ground(Terrain[] terrains,Vector3 point)
    {
        var terrain=terrains.Single(t=>point.x>=t.transform.position.x&&point.x<t.transform.position.x+t.terrainData.size.x&&point.z>=t.transform.position.z&&point.z<t.transform.position.z+t.terrainData.size.z);
        return terrain.SampleHeight(point)+terrain.transform.position.y;
    }
    static void Place(GameObject root,Vector3 anchor)
    {
        var b=BoundsOf(root);root.transform.position+=new Vector3(anchor.x-b.center.x,anchor.y-b.min.y,anchor.z-b.center.z);
    }
    static MaterialFact Describe(Material m)
    {
        var texture=m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):m.mainTexture;
        return new MaterialFact{path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,supported=m.shader.isSupported,texture=AssetDatabase.GetAssetPath(texture),queue=m.renderQueue,surface=m.HasProperty("_Surface")?m.GetFloat("_Surface"):-1,alphaClip=m.HasProperty("_AlphaClip")?m.GetFloat("_AlphaClip"):-1};
    }
    static FrameFact Capture(Camera camera,GameObject[] models,string folder,string name,string layout,float distance,int width,int height,int forced)
    {
        var bounds=models.Where(m=>m.activeSelf).Select(BoundsOf).ToArray();Assert.That(bounds.Length,Is.GreaterThan(0));var combined=bounds[0];foreach(var b in bounds)combined.Encapsulate(b);
        // Exactly the same low, slightly elevated frontal direction for all
        // candidates; no per-tree reframing that could hide a poor silhouette.
        Vector3 forward=new Vector3(.04f,-.07f,1).normalized;
        camera.transform.SetPositionAndRotation(combined.center-forward*distance,Quaternion.LookRotation(forward));camera.aspect=width/(float)height;
        var mins=new List<Vector3>();var maxs=new List<Vector3>();
        foreach(var b in bounds)
        {
            Vector3 min=Vector3.one*float.PositiveInfinity,max=Vector3.one*float.NegativeInfinity;
            for(int i=0;i<8;i++){var p=camera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
            Assert.That(min.z,Is.GreaterThan(camera.nearClipPlane));Assert.That(min.x,Is.GreaterThan(0));Assert.That(min.y,Is.GreaterThan(0));Assert.That(max.x,Is.LessThan(1));Assert.That(max.y,Is.LessThan(1));mins.Add(min);maxs.Add(max);
        }
        var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var original=camera.targetTexture;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
        string file=Path.Combine(folder,name+"-"+width+".png");Assert.That(File.Exists(file),Is.False);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(file,pixels.EncodeToPNG());}
        finally{camera.targetTexture=original;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(pixels);}
        Assert.That(new FileInfo(file).Length,Is.GreaterThan(5000));
        return new FrameFact{file=file,sha256=Hash(file),layout=layout,detail="Actual URP camera+WM Clear/Day shared-world rendering. PNG is unaltered Camera.Render/ReadPixels; inspect visually before a production choice.",width=width,height=height,distance=distance,forcedLod=forced,fieldOfView=camera.fieldOfView,cameraPosition=camera.transform.position,cameraRotation=camera.transform.rotation,centres=bounds.Select(b=>b.center).ToArray(),sizes=bounds.Select(b=>b.size).ToArray(),viewportMin=mins.ToArray(),viewportMax=maxs.ToArray()};
    }
    [UnityTest,Timeout(90000)] public IEnumerator ControlledActualClearDayComparesVistaAndInstalledJc03Jc07AtEqualHeightWithoutChangingForest()
    {
        using(var scope=new WildernessNativeScope("installed-tree-comparison"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");var world=Get(scope.match,"world");var terrains=(Terrain[])Get(world,"terrains");var data=terrains.Select(t=>t.terrainData).ToArray();var trees=data.Select(d=>d.treeInstances).ToArray();var prototypes=data.Select(d=>d.treePrototypes.Select(p=>p.prefab).ToArray()).ToArray();var digests=data.Select(TerrainDigest).ToArray();Assert.That(trees.Sum(t=>t.Length),Is.EqualTo(2297));
            var weather=Get(scope.match,"presentationWeather");Call(Get(weather,"presets"),"Select","9aad5ff5dfa09ce468205f676714f891");Sports("SportsTimeOfDay","Select",Enum.Parse(Runtime("LWS.TruckSports.SportsTimeChoice"),"Day"));scope.Configure(1,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(1);
            Assert.That((string)Get(weather,"PresetId"),Is.EqualTo("9aad5ff5dfa09ce468205f676714f891"));Assert.That(Get(Get(weather,"matchTime"),"Applied").ToString(),Is.EqualTo("Day"));Call(scope.match,"Pause",0,false);Assert.That((bool)Get(scope.match,"Paused"),Is.True);
            var transient=new GameObject("TEST ONLY installed tree comparison");IList allowed=null;Camera[] originalAllowed=null;MethodInfo clearCache=null;Camera camera=null;
            try
            {
            var models=new List<GameObject>();var facts=new List<ModelFact>();var hashes=new Dictionary<string,string>();var materialRefs=new Dictionary<Renderer,Material[]>();var meshRefs=new Dictionary<MeshFilter,Mesh>();
            var cameras=(Camera[])Get(scope.match,"cameras");var originalCamera=cameras.First(c=>c.enabled);var camObject=new GameObject("Owned native tree comparison camera");camObject.transform.SetParent(transient.transform,false);camera=camObject.AddComponent<Camera>();camera.CopyFrom(originalCamera);camera.enabled=false;camera.targetTexture=null;camera.rect=new Rect(0,0,1,1);camera.orthographic=false;camera.usePhysicalProperties=false;camera.lensShift=Vector2.zero;camera.fieldOfView=44;camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix();camera.nearClipPlane=.2f;camera.farClipPlane=Mathf.Max(1200,originalCamera.farClipPlane);
            var dataType=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true);var originalCameraData=originalCamera.GetComponent(dataType);bool originalHadAdditionalData=originalCameraData!=null;int requestedRendererIndex=-1;var originalComponentIds=originalCamera.GetComponents<Component>().Select(c=>c.GetInstanceID()).ToArray();
            if(originalHadAdditionalData)
            {
                var cameraData=camera.gameObject.AddComponent(dataType);
                foreach(string property in new[]{"renderShadows","renderPostProcessing","requiresDepthTexture","requiresColorTexture","volumeLayerMask"}){var pi=dataType.GetProperty(property);Assert.That(pi,Is.Not.Null);pi.SetValue(cameraData,pi.GetValue(originalCameraData));}
                dataType.GetProperty("volumeTrigger").SetValue(cameraData,camera.transform);var index=dataType.GetField("m_RendererIndex",Any);Assert.That(index,Is.Not.Null);requestedRendererIndex=(int)index.GetValue(originalCameraData);Call(cameraData,"SetRenderer",requestedRendererIndex);
            }
            else
            {
                // Installed URP17.4 GetRenderer(null) uses asset.scriptableRenderer;
                // InitializeAdditionalCameraData(null) uses Base/clearDepthtrue,
                // pipeline shadow/depth/opaque settings and postprocessfalse.
                // Keep the SAME null-component path on this NEW camera rather
                // than adding data to the original through GetUniversal...().
                Assert.That(camera.GetComponent(dataType),Is.Null);
            }
            var vendor=(Component)Get(weather,"vendor");allowed=(IList)Get(vendor,"AllowCameras");originalAllowed=allowed.Cast<Camera>().ToArray();allowed.Add(camera);clearCache=vendor.GetType().GetMethod("ClearShouldIgnoreCameraCache",BindingFlags.Public|BindingFlags.Static);clearCache.Invoke(null,null);
            string run=Guid.NewGuid().ToString("N"),folder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingInstalledTreeComparison/"+run);Directory.CreateDirectory(folder);var frames=new List<FrameFact>();var anchor=new Vector3(105,0,41);anchor.y=Ground(terrains,anchor);Assert.That(anchor.y,Is.InRange(7.5f,8.5f),"Use the actual cleared shared-world ground, not a floating proxy/hidden test plane.");
                Assert.That((bool)vendor.GetType().GetMethod("ShouldIgnoreCamera",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{vendor,camera,false}),Is.False,"The owned comparison camera is explicitly admitted through the vendor public camera list.");
                for(int i=0;i<Sources.Length;i++)
                {
                    var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Sources[i]);Assert.That(asset,Is.Not.Null,Sources[i]);var model=UnityEngine.Object.Instantiate(asset,transient.transform);model.name="Controlled "+Names[i];model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    Assert.That(model.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
                    var colliders=model.GetComponentsInChildren<Collider>(true);foreach(var collider in colliders)collider.enabled=false; // copies only; no added collision/physics authority
                    var b=BoundsOf(model);model.transform.localScale*=Height/b.size.y;Place(model,anchor);b=BoundsOf(model);Assert.That(b.size.y,Is.EqualTo(Height).Within(.002f));
                    var group=model.GetComponentInChildren<LODGroup>();if(i>0){Assert.That(group,Is.Not.Null);Assert.That(group.GetLODs().Length,Is.EqualTo(2));}if(group!=null)group.ForceLOD(-1);
                    var renderers=model.GetComponentsInChildren<Renderer>(true);var mats=renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();Assert.That(renderers.All(r=>(camera.cullingMask&(1<<r.gameObject.layer))!=0),Is.True,"Actual tree layers must be included by the copied original world-camera mask.");foreach(var renderer in renderers)materialRefs[renderer]=(Material[])renderer.sharedMaterials.Clone();foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))meshRefs[filter]=filter.sharedMesh;
                    Assert.That(mats.All(m=>m!=null&&m.shader!=null&&m.shader.isSupported),Is.True);
                    if(i>0)foreach(var mat in mats){Assert.That(mat.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(mat.GetFloat("_Surface"),Is.Zero);Assert.That(mat.GetFloat("_AlphaClip"),Is.Zero);}
                    var dependencies=AssetDatabase.GetDependencies(Sources[i],true);foreach(var p in dependencies.Where(File.Exists)){hashes[p]=Hash(p);if(File.Exists(p+".meta"))hashes[p+".meta"]=Hash(p+".meta");}
                    facts.Add(new ModelFact{name=Names[i],source=Sources[i],height=b.size.y,boundsSize=b.size,dependencies=dependencies,meshPaths=model.GetComponentsInChildren<MeshFilter>(true).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)+"/"+f.sharedMesh.name).ToArray(),lodThresholds=group==null?new float[0]:group.GetLODs().Select(l=>l.screenRelativeTransitionHeight).ToArray(),materials=mats.Select(Describe).ToArray(),disabledTransientColliders=colliders.Length});models.Add(model);model.SetActive(false);
                }
                // All single-tree natural-LOD comparisons share exactly the
                // same ground XYZ, rotation,16.6m height and camera distances.
                for(int i=0;i<models.Count;i++)
                {
                    models[i].SetActive(true);Place(models[i],anchor);
                    foreach(float distance in new[]{35f,90f,220f})foreach(int width in new[]{1280,1920})frames.Add(Capture(camera,models.ToArray(),folder,Names[i]+"-natural-"+distance,"IDENTICAL_XYZ_SINGLE",distance,width,width==1280?720:1080,-1));
                    models[i].SetActive(false);
                }
                for(int i=0;i<models.Count;i++){models[i].SetActive(true);var point=anchor+Vector3.right*((i-1)*24);point.y=Ground(terrains,point);Place(models[i],point);}
                foreach(float distance in new[]{70f,140f,240f})foreach(int width in new[]{1280,1920})frames.Add(Capture(camera,models.ToArray(),folder,"side-by-side-natural-"+distance,"LEFT_VISTA_CENTRE_JC03_RIGHT_JC07",distance,width,width==1280?720:1080,-1));
                foreach(int lod in new[]{0,1})
                {
                    foreach(var model in models){var group=model.GetComponentInChildren<LODGroup>();if(group!=null)group.ForceLOD(lod);}
                    frames.Add(Capture(camera,models.ToArray(),folder,"side-by-side-explicit-lod-"+lod,"CONTROLLED_EXPLICIT_LOD_MESH_COMPARISON",70,1920,1080,lod));
                }
                foreach(var pair in materialRefs)CollectionAssert.AreEqual(pair.Value,pair.Key.sharedMaterials,"No source/material substitution or tint controls in this comparison.");foreach(var pair in meshRefs)Assert.That(pair.Key.sharedMesh,Is.SameAs(pair.Value));foreach(var pair in hashes)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"Original vendor/owned prefab/material/texture/importer bytes preserved: "+pair.Key);
                for(int i=0;i<data.Length;i++){Assert.That(terrains[i].terrainData,Is.SameAs(data[i]));CollectionAssert.AreEqual(trees[i],data[i].treeInstances);CollectionAssert.AreEqual(prototypes[i],data[i].treePrototypes.Select(p=>p.prefab).ToArray());Assert.That(TerrainDigest(data[i]),Is.EqualTo(digests[i]),"All terrain heights/layers/paint/holes/details/2297tree records remain exact.");}
                CollectionAssert.AreEqual(originalComponentIds,originalCamera.GetComponents<Component>().Select(c=>c.GetInstanceID()).ToArray(),"Comparison must not add data or change the original world camera component graph.");Assert.That(originalCamera.GetComponent(dataType),Is.SameAs(originalCameraData));
                Assert.That(frames.Count,Is.EqualTo(26));var report=new Report{originalHadAdditionalData=originalHadAdditionalData,comparisonHadAdditionalData=camera.GetComponent(dataType)!=null,requestedRendererIndex=requestedRendererIndex,cameraDataPolicy=originalHadAdditionalData?"COPY_SOURCE_EXPLICIT_DATA_ON_NEW_CAMERA_ONLY":"SAME_URP_NULL_ADDITIONAL_DATA_PIPELINE_DEFAULT_PATH;effectiveDefaultRenderer=-1;pipelineShadowsDepthOpaque;postprocessing=false;volumeDefaultLayer",runId=run,pipeline=AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),weather=(string)Get(weather,"PresetId"),timeChoice=Get(Get(weather,"matchTime"),"Applied").ToString(),identicalSingleTreeAnchor=anchor,uniformHeight=Height,originalTreeInstances=trees.Sum(t=>t.Length),terrainAndVendorAssetsUnchanged=true,models=facts.ToArray(),inputs=hashes.OrderBy(p=>p.Key).Select(p=>new Input{path=p.Key,sha256=p.Value}).ToArray(),frames=frames.ToArray()};File.WriteAllText(Path.Combine(folder,"controlled-actual-tree-comparison.json"),JsonUtility.ToJson(report,true),System.Text.Encoding.UTF8);
                TestContext.WriteLine("CONTROLLED_INSTALLED_TREE_COMPARISON output="+folder+" actualWM_CLEAR_DAY identicalXYZ/16.6m/realvendorURPmaterials/natural+forcedLODs/26unaltered720+1080PNGs;2297originalplacements+heights/prototypes/vendorbytes unchanged. Diagnostic pass is NOT a finished-beauty/Windows/performance acceptance; inspect originals before choosing any production variant.");
            }
            finally{if(allowed!=null&&camera!=null)allowed.Remove(camera);clearCache?.Invoke(null,null);UnityEngine.Object.DestroyImmediate(transient);if(originalAllowed!=null)CollectionAssert.AreEqual(originalAllowed,allowed.Cast<Camera>().ToArray());}
        }
    }
}
#endif
