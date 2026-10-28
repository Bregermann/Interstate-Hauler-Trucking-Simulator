#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using static WildernessNativeScope;

public sealed class CampingWaterRenderNativeTests
{
    static Type Author => Type.GetType("LWS.TruckCamping.Editor.CampingWaterPresentationAuthoring, Assembly-CSharp-Editor",true);
    static object RenderCall(string method,params object[] args)=>Author.GetMethod(method).Invoke(null,args);
    static string Hash(string path)=>(string)RenderCall("Digest",path);
    const string Shared="Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab";
    const string Timber="Assets/TruckCamping/Models/Camp Haul bound rounded timber.asset";
    const string OldMaterial="Assets/TruckCamping/Materials/Camp Haul River Modeler flowing water.mat";
    [Serializable] sealed class FrameFact { public string file,sha256,variant,view;public int width,height;public Vector3 position,subjectCenter,subjectSize;public Quaternion rotation;public float fieldOfView;public string sourceMeshDiagnostics,shader,baseTexture,normalTexture,materialName;public string[] keywords;public Vector2 tiling;public Color tint;public float smoothness,bumpScale; }
    [Serializable] sealed class Report
    {
        public string status="CONTROLLED_NATIVE_ACTUAL_WORLD_UV_MATERIAL_AB_NOT_WINDOWS_OR_BEAUTY_ACCEPTANCE",worldSha,pipeline,weather,timeChoice;
        public string comparison="A original mesh/material; B corrected UV/tangents only; C original mesh with working Fishing/RiverModeler foundation; D both fixes. Same actual water geometry/camera/weather each variant; transient in-memory references restored.";
        public int actualWaterRenderers,originalTrees;public bool sourceAssetsExact,originalCameraComponentsExact,weatherCameraListExact;
        public FrameFact[] frames;public string[] retainedInputs;
    }
    sealed class RenderScope : IDisposable
    {
        public Camera camera;
        readonly GameObject owner;
        readonly Camera original;
        readonly int[] originalComponents;
        readonly IList allowed;
        readonly Camera[] priorAllowed;
        readonly MethodInfo clear;
        public RenderScope(WildernessNativeScope scope)
        {
            original=((Camera[])Get(scope.match,"cameras")).First(c=>c.enabled);originalComponents=original.GetComponents<Component>().Select(c=>c.GetInstanceID()).ToArray();
            owner=new GameObject("TEST ONLY controlled water comparison camera");camera=owner.AddComponent<Camera>();camera.CopyFrom(original);camera.enabled=false;camera.targetTexture=null;camera.rect=new Rect(0,0,1,1);camera.orthographic=false;camera.usePhysicalProperties=false;camera.lensShift=Vector2.zero;camera.fieldOfView=44;camera.nearClipPlane=.2f;camera.farClipPlane=Mathf.Max(1200,original.farClipPlane);camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix();
            var dataType=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true);var oldData=original.GetComponent(dataType);
            if(oldData!=null)
            {
                var data=camera.gameObject.AddComponent(dataType);
                foreach(string name in new[]{"renderShadows","renderPostProcessing","requiresDepthTexture","requiresColorTexture","volumeLayerMask"}){var pi=dataType.GetProperty(name);pi.SetValue(data,pi.GetValue(oldData));}
                dataType.GetProperty("volumeTrigger").SetValue(data,camera.transform);Call(data,"SetRenderer",(int)dataType.GetField("m_RendererIndex",Any).GetValue(oldData));
            }
            var weather=Get(scope.match,"presentationWeather");var vendor=(Component)Get(weather,"vendor");allowed=(IList)Get(vendor,"AllowCameras");priorAllowed=allowed.Cast<Camera>().ToArray();allowed.Add(camera);clear=vendor.GetType().GetMethod("ClearShouldIgnoreCameraCache",BindingFlags.Public|BindingFlags.Static);clear.Invoke(null,null);
            Assert.That((bool)vendor.GetType().GetMethod("ShouldIgnoreCamera",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{vendor,camera,false}),Is.False);
        }
        public void Dispose()
        {
            if(camera!=null)allowed.Remove(camera);clear.Invoke(null,null);UnityEngine.Object.DestroyImmediate(owner);
            CollectionAssert.AreEqual(priorAllowed,allowed.Cast<Camera>().ToArray());CollectionAssert.AreEqual(originalComponents,original.GetComponents<Component>().Select(c=>c.GetInstanceID()).ToArray());
        }
    }
    static IEnumerator ClearDay(WildernessNativeScope scope)
    {
        var weather=Get(scope.match,"presentationWeather");Call(Get(weather,"presets"),"Select","9aad5ff5dfa09ce468205f676714f891");Sports("SportsTimeOfDay","Select",Enum.Parse(Runtime("LWS.TruckSports.SportsTimeChoice"),"Day"));scope.Configure(1,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(1);
        Assert.That((string)Get(weather,"PresetId"),Is.EqualTo("9aad5ff5dfa09ce468205f676714f891"));
    }
    static FrameFact Save(Camera camera,MeshFilter subject,string folder,string variant,string view,int width,int height)
    {
        var bounds=subject.GetComponent<Renderer>().bounds;
        if(view=="lake")camera.transform.SetPositionAndRotation(new Vector3(145,45,-30),Quaternion.LookRotation(new Vector3(145,5.5f,83)-new Vector3(145,45,-30)));
        else camera.transform.SetPositionAndRotation(bounds.center+new Vector3(0,4,-38),Quaternion.LookRotation(new Vector3(0,-4,38)));
        camera.aspect=width/(float)height;Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),bounds),Is.True,"Actual subject must intersect the identical controlled view.");
        var center=camera.WorldToViewportPoint(bounds.center);Assert.That(center.z,Is.GreaterThan(.2f));Assert.That(center.x,Is.InRange(.05f,.95f));Assert.That(center.y,Is.InRange(.05f,.95f));
        string file=Path.Combine(folder,variant+"-"+view+"-"+width+".png");Assert.That(File.Exists(file),Is.False);
        var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var old=camera.targetTexture;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        Assert.That(new FileInfo(file).Length,Is.GreaterThan(5000));
        var material=subject.GetComponent<Renderer>().sharedMaterial;
        return new FrameFact { file=file,sha256=Hash(file),variant=variant,view=view,width=width,height=height,position=camera.transform.position,rotation=camera.transform.rotation,fieldOfView=camera.fieldOfView,subjectCenter=bounds.center,subjectSize=bounds.size,sourceMeshDiagnostics=(string)RenderCall("MeshDiagnostics",subject.sharedMesh),shader=material.shader.name,materialName=material.name,baseTexture=AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")),normalTexture=AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")),keywords=material.shaderKeywords,tiling=material.GetTextureScale("_BaseMap"),tint=material.GetColor("_BaseColor"),smoothness=material.GetFloat("_Smoothness"),bumpScale=material.GetFloat("_BumpScale") };
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator OriginalWorldWaterTwoByTwoUvAndVendorMaterialComparisonRestoresEveryReferenceAndAsset()
    {
        Assert.That(Hash(Shared),Is.EqualTo("a8448bc181521b1f01734f143c0bbf108893636e47324d44c65875b7574e0cdd"),"Run controlled A/B BEFORE the guarded production water author; don't relabel a modified source as the original.");
        using(var scope=new WildernessNativeScope("camp-water-ab"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");yield return ClearDay(scope);
            var world=(Component)Get(scope.match,"world");var terrains=(Terrain[])Get(world,"terrains");Assert.That(terrains.Sum(t=>t.terrainData.treeInstanceCount),Is.EqualTo(2297));
            var filters=(MeshFilter[])RenderCall("WaterFilters",world.gameObject);var originals=filters.Select(f=>f.sharedMesh).ToArray();var renderers=filters.Select(f=>f.GetComponent<MeshRenderer>()).ToArray();var originalMaterials=renderers.Select(r=>r.sharedMaterial).ToArray();
            string[] files=new[]{Shared,Shared+".meta",Timber,Timber+".meta",OldMaterial,OldMaterial+".meta","Assets/TruckFishing/Materials/Clear fishing river.mat","Assets/RiverModeler/ShaderGraph/Default River.mat","Assets/RiverModeler/Textures/WaveNormal.png","Assets/RiverModeler/Textures/WaveNormal.png.meta"}.Concat(terrains.SelectMany(t=>new[]{AssetDatabase.GetAssetPath(t.terrainData),AssetDatabase.GetAssetPath(t.terrainData)+".meta"})).Distinct().ToArray();var hashes=files.Select(Hash).ToArray();
            var source=AssetDatabase.LoadAssetAtPath<Mesh>(Timber);foreach(var mesh in originals)Assert.That(mesh,Is.SameAs(source));var derived=(Mesh)RenderCall("CreateTransientWaterMesh",source);
            var materials=new Dictionary<string,Material>();foreach(string kind in new[]{"lake","creek","cascade"})materials.Add(kind,(Material)RenderCall("CreateTransientWaterMaterial",kind));
            var frames=new List<FrameFact>();string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingWaterComparisonNative/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            Call(scope.match,"Pause",0,false);yield return null;yield return null;
            try
            {
                using(var render=new RenderScope(scope))
                {
                    foreach(string variant in new[]{"A-original","B-uv-only","C-material-only","D-uv-and-foundation"})
                    {
                        bool fixedUv=variant.StartsWith("B-",StringComparison.Ordinal)||variant.StartsWith("D-",StringComparison.Ordinal),fixedMaterial=variant.StartsWith("C-",StringComparison.Ordinal)||variant.StartsWith("D-",StringComparison.Ordinal);
                        for(int i=0;i<filters.Length;i++){filters[i].sharedMesh=fixedUv?derived:originals[i];renderers[i].sharedMaterial=fixedMaterial?materials[(string)RenderCall("Kind",filters[i].name)]:originalMaterials[i];}
                        yield return null;yield return null;
                        foreach(string view in new[]{"lake","cascade"})foreach(int width in new[]{1280,1920})
                        {
                            var subject=filters.Single(f=>view=="lake"?f.name=="Flowing reflective Willow Lake":f.name=="River Modeler flowing silver cascade");
                            frames.Add(Save(render.camera,subject,folder,variant,view,width,width==1280?720:1080));
                        }
                    }
                }
            }
            finally
            {
                for(int i=0;i<filters.Length;i++){if(filters[i]!=null)filters[i].sharedMesh=originals[i];if(renderers[i]!=null)renderers[i].sharedMaterial=originalMaterials[i];}
                UnityEngine.Object.DestroyImmediate(derived);foreach(var material in materials.Values)UnityEngine.Object.DestroyImmediate(material);
            }
            for(int i=0;i<filters.Length;i++){Assert.That(filters[i].sharedMesh,Is.SameAs(originals[i]));Assert.That(renderers[i].sharedMaterial,Is.SameAs(originalMaterials[i]));}
            CollectionAssert.AreEqual(hashes,files.Select(Hash).ToArray());Assert.That(frames.Count,Is.EqualTo(16));
            File.WriteAllText(Path.Combine(folder,"comparison.json"),JsonUtility.ToJson(new Report { worldSha=Hash(Shared),pipeline=AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),weather="ACTUAL_WM_CLEAR",timeChoice="Day",actualWaterRenderers=19,originalTrees=2297,sourceAssetsExact=true,originalCameraComponentsExact=true,weatherCameraListExact=true,frames=frames.ToArray(),retainedInputs=files.Select((p,i)=>p+":"+hashes[i]).ToArray() },true),new System.Text.UTF8Encoding(false));
            TestContext.WriteLine("CAMPING_WATER_AB_OUTPUT "+folder+";16 actual native720/1080 frames, original layout/physics/asset bytes/reference restoration exact. Inspect before choosing production; measurement pass is not beauty/human/Windows acceptance.");
        }
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator CascadeReadabilityComparesRetainedDarkAndPaleCyanAndActualVendorFoamWithoutAuthoring()
    {
        Assert.That(Hash(Shared),Is.EqualTo("a8448bc181521b1f01734f143c0bbf108893636e47324d44c65875b7574e0cdd"));
        using(var scope=new WildernessNativeScope("camp-cascade-readability"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");yield return ClearDay(scope);
            var world=(Component)Get(scope.match,"world");var filters=(MeshFilter[])RenderCall("WaterFilters",world.gameObject);var originals=filters.Select(f=>f.sharedMesh).ToArray();var renderers=filters.Select(f=>f.GetComponent<MeshRenderer>()).ToArray();var originalMaterials=renderers.Select(r=>r.sharedMaterial).ToArray();
            var source=AssetDatabase.LoadAssetAtPath<Mesh>(Timber);var derived=(Mesh)RenderCall("CreateTransientWaterMesh",source);var created=new List<Material>();
            var lake=(Material)RenderCall("CreateTransientWaterMaterial","lake");var creek=(Material)RenderCall("CreateTransientWaterMaterial","creek");created.Add(lake);created.Add(creek);
            var candidates=new Dictionary<string,Material>();foreach(string look in new[]{"legacy-dark","pale-cyan","flowing-foam"}){var material=(Material)RenderCall("CreateTransientCascadeCandidate",look);candidates.Add(look,material);created.Add(material);}
            string foam="Assets/RiverModeler/Textures/WaterFoam.png";string[] retained={Shared,Shared+".meta",Timber,Timber+".meta",OldMaterial,foam,foam+".meta","Assets/TruckFishing/Materials/Clear fishing river.mat"};var hashes=retained.Select(Hash).ToArray();
            string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingCascadeReadabilityNative/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);var frames=new List<FrameFact>();Call(scope.match,"Pause",0,false);yield return null;yield return null;
            try
            {
                using(var render=new RenderScope(scope))
                {
                    for(int i=0;i<filters.Length;i++){filters[i].sharedMesh=derived;string kind=(string)RenderCall("Kind",filters[i].name);renderers[i].sharedMaterial=kind=="lake"?lake:kind=="creek"?creek:candidates["legacy-dark"];}
                    foreach(var pair in candidates)
                    {
                        var cascade=filters.Single(f=>f.name=="River Modeler flowing silver cascade");cascade.GetComponent<MeshRenderer>().sharedMaterial=pair.Value;
                        yield return null;yield return null;
                        foreach(int width in new[]{1280,1920})frames.Add(Save(render.camera,cascade,folder,pair.Key,"cascade",width,width==1280?720:1080));
                    }
                    var lakeFilter=filters.Single(f=>f.name=="Flowing reflective Willow Lake");
                    foreach(int width in new[]{1280,1920})frames.Add(Save(render.camera,lakeFilter,folder,"unchanged-D-foundation","lake",width,width==1280?720:1080));
                }
            }
            finally
            {
                for(int i=0;i<filters.Length;i++){if(filters[i]!=null)filters[i].sharedMesh=originals[i];if(renderers[i]!=null)renderers[i].sharedMaterial=originalMaterials[i];}
                UnityEngine.Object.DestroyImmediate(derived);foreach(var material in created)UnityEngine.Object.DestroyImmediate(material);
            }
            CollectionAssert.AreEqual(hashes,retained.Select(Hash).ToArray());Assert.That(frames.Count,Is.EqualTo(8));
            for(int i=0;i<filters.Length;i++){Assert.That(filters[i].sharedMesh,Is.SameAs(originals[i]));Assert.That(renderers[i].sharedMaterial,Is.SameAs(originalMaterials[i]));}
            File.WriteAllText(Path.Combine(folder,"cascade-readability-comparison.json"),JsonUtility.ToJson(new Report { status="CASCADE_READABILITY_CHOICES_ACTUAL_NATIVE_IMAGES_REQUIRE_REVIEW_NO_AUTHORING",worldSha=Hash(Shared),pipeline=AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),weather="ACTUAL_WM_CLEAR",timeChoice="Day",actualWaterRenderers=19,originalTrees=2297,sourceAssetsExact=true,originalCameraComponentsExact=true,weatherCameraListExact=true,frames=frames.ToArray(),retainedInputs=retained.Select((p,i)=>p+":"+hashes[i]).ToArray() },true),new System.Text.UTF8Encoding(false));
            TestContext.WriteLine("CAMPING_CASCADE_READABILITY_OUTPUT "+folder+"; retained R55 dark vs pale-cyan and actual continuous vendor foam,6cascade+2unchangedlake native720/1080PNGs. No emission/newlight/geometry/physics changes; choose one actual reviewed source look before author.");
        }
    }
    [UnityTest,Timeout(65000)]
    public IEnumerator InstalledWaterHasRealUvFramesFlowPauseRematchAndControlledNativeViews()
    {
        Assert.That((string)RenderCall("VerifyWaterDescendsFrom","a8448bc181521b1f01734f143c0bbf108893636e47324d44c65875b7574e0cdd"),Does.Contain("ALL_OTHER_WORLD_BYTES_EXACT"));
        using(var scope=new WildernessNativeScope("camp-water-installed"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");yield return ClearDay(scope);var world=(Component)Get(scope.match,"world");var filters=(MeshFilter[])RenderCall("WaterFilters",world.gameObject);
            var derived=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckCamping/Models/Camp Haul water-only UV tangent surface.asset");var source=AssetDatabase.LoadAssetAtPath<Mesh>(Timber);
            foreach(var filter in filters){Assert.That(filter.sharedMesh,Is.SameAs(derived));RenderCall("ValidateDerivedMesh",source,filter.sharedMesh);Assert.That(filter.GetComponent<Renderer>().sharedMaterial.shader.isSupported,Is.True);}
            var mats=filters.Select(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray();var before=mats.Select(m=>m.GetTextureOffset("_BaseMap")).ToArray();yield return scope.Advance(.6f);
            Assert.That(mats.Zip(before,(m,p)=>Vector2.Distance(m.GetTextureOffset("_BaseMap"),p)).All(d=>d>.01f),Is.True,"Existing sole FishingWaterFlow/DOTween authority actually advances all19 material instances.");
            Call(scope.match,"Pause",0,false);yield return null;yield return null;var paused=mats.Select(m=>m.GetTextureOffset("_BaseMap")).ToArray();float until=Time.realtimeSinceStartup+.2f;while(Time.realtimeSinceStartup<until)yield return null;
            foreach(var pair in mats.Select((m,i)=>new{m,i}))Assert.That(Vector2.Distance(pair.m.GetTextureOffset("_BaseMap"),paused[pair.i]),Is.LessThan(1e-5f));
            string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingWaterInstalledNative/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            using(var render=new RenderScope(scope))foreach(string view in new[]{"lake","cascade"})foreach(int width in new[]{1280,1920})Save(render.camera,filters.Single(f=>view=="lake"?f.name=="Flowing reflective Willow Lake":f.name=="River Modeler flowing silver cascade"),folder,"installed",view,width,width==1280?720:1080);
            Call(scope.match,"Resume");yield return scope.Advance(.3f);Call(scope.match,"SetupScreen");scope.Configure(1,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(.3f);
            foreach(var filter in filters){Assert.That(filter.sharedMesh,Is.SameAs(derived));Assert.That(filter.GetComponent<Renderer>().sharedMaterial.GetTexture("_BumpMap"),Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/WaveNormal.png")));}
            TestContext.WriteLine("CAMPING_WATER_INSTALLED_OUTPUT "+folder+" actual original flow/pause/rematch and controlled water rendering; no gameplay/Windows/human claim.");
        }
    }
}
#endif
