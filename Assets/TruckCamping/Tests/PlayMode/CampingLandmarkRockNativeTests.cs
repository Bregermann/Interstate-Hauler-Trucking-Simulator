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
using UnityEngine.TestTools;
using static WildernessNativeScope;
public sealed class CampingLandmarkRockNativeTests
{
    const string Shared="Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab";
    const string ProfilePath="Assets/TruckCamping/Resources/TruckCamping/Camp Haul landmark rock presentation.asset";
    static Type Author=>Type.GetType("LWS.TruckCamping.Editor.CampingLandmarkRockAuthoring, Assembly-CSharp-Editor",true);
    static object Art(string name,params object[] args)=>Author.GetMethod(name).Invoke(null,args);
    static string Hash(string path)=>(string)Art("Digest",path);
    static Transform Resolve(Component world,object target){var t=world.transform;foreach(int i in (int[])Get(target,"siblingPath"))t=t.GetChild(i);return t;}
    static string Physics(Component world)=>string.Join("\n",world.GetComponentsInChildren<Collider>(true).OrderBy(c=>c.GetInstanceID()).Select(c=>c.GetInstanceID()+":"+c.enabled+":"+c.isTrigger+":"+c.bounds.ToString("R")+":"+c.transform.position.ToString("R")+":"+c.transform.rotation.ToString("R")+":"+c.transform.lossyScale.ToString("R")));
    static Bounds WorldEnvelope(Component world)
    {
        var terrains=(Terrain[])Get(world,"terrains");var bounds=new Bounds(terrains[0].transform.position+terrains[0].terrainData.size*.5f,terrains[0].terrainData.size);
        foreach(var terrain in terrains.Skip(1))bounds.Encapsulate(new Bounds(terrain.transform.position+terrain.terrainData.size*.5f,terrain.terrainData.size));foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))bounds.Encapsulate(renderer.bounds);return bounds;
    }
    static string Trucks(Component match)=>string.Join("\n",Components(Get(match,"explorers")).Select(e=>PhysicsIdentity((Component)Get(e,"truck"))));
    static string Water(Component world)=>string.Join("\n",world.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Flowing ")||f.name=="River Modeler flowing silver cascade").Select(f=>f.GetInstanceID()+":"+f.sharedMesh.GetInstanceID()+":"+f.GetComponent<Renderer>().sharedMaterial.GetInstanceID()));
    static IEnumerator ClearDay(WildernessNativeScope scope)=>(IEnumerator)typeof(CampingWaterRenderNativeTests).GetMethod("ClearDay",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{scope});
    sealed class CameraScope:IDisposable
    {
        readonly IDisposable scope;public Camera camera;
        public CameraScope(WildernessNativeScope world)
        {var type=typeof(CampingWaterRenderNativeTests).GetNestedType("RenderScope",BindingFlags.NonPublic);var value=Activator.CreateInstance(type,new object[]{world});scope=(IDisposable)value;camera=(Camera)type.GetField("camera").GetValue(value);}
        public void Dispose(){scope.Dispose();}
    }
    static void Capture(Camera camera,Bounds subject,string folder,string variant,string place,int width)
    {
        int height=width==1280?720:1080;Vector3 offset=place=="cascade"?new Vector3(0,4,-38):place=="isolated"?new Vector3(0,2,-18):place=="base"?new Vector3(0,3,-14):new Vector3(0,3,-55);camera.transform.SetPositionAndRotation(subject.center+offset,Quaternion.LookRotation(-offset));camera.aspect=width/(float)height;
        Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),subject),Is.True);
        var rt=RenderTexture.GetTemporary(width,height,24);var old=camera.targetTexture;var active=RenderTexture.active;var image=new Texture2D(width,height,TextureFormat.RGB24,false);string path=Path.Combine(folder,variant+"-"+place+"-"+width+".png");Assert.That(File.Exists(path),Is.False);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        Assert.That(new FileInfo(path).Length,Is.GreaterThan(5000));TestContext.WriteLine("LANDMARK_ROCK_CONTROLLED_FRAME "+path+" SHA256="+Hash(path)+" pose="+camera.transform.position.ToString("R")+" bounds="+subject.ToString("R"));
    }
    static void AssertActualFooting(Component world,ScriptableObject profile,Transform visualRoot)
    {TestContext.WriteLine("NATURAL_CLOSED_ROCK_RENDERED_GEOMETRY "+Art("VerifyActualRenderedPlacement",world,profile,visualRoot));}
    static IEnumerator IsolatedSourceComparison(Component world,ScriptableObject profile,Camera camera,string folder)
    {
        GameObject source=(GameObject)Art("CreateIsolatedSourceRender"),normalized=UnityEngine.Object.Instantiate((GameObject)Get(profile,"boulderPrefab"));
        try
        {
            normalized.SetActive(true);var sourceRenderer=source.GetComponentInChildren<Renderer>();var prior=sourceRenderer.bounds;var landmark=Components(Get(world,"landmarks"))[0];Vector3 anchor=landmark.transform.position+new Vector3(15,0,-10);anchor.y=(float)Call(world,"Height",anchor);
            source.transform.position=anchor-prior.center+Vector3.up*prior.extents.y;normalized.transform.position=anchor+Vector3.up*prior.extents.y;
            var raw=(Vector3[])Art("ReadActualVertices",(Mesh)Get(profile,"originalRockAMesh"));var a=source.GetComponentInChildren<MeshFilter>().transform;var b=normalized.GetComponentInChildren<MeshFilter>().transform;float error=raw.Max(v=>(a.TransformPoint(v)-b.TransformPoint(v)).magnitude);Assert.That(error,Is.LessThan(.0001f),"Every actual renderer vertex preserves the complete original authored matrix, apart from normalization translation.");
            TestContext.WriteLine("ACTUAL_UPRIGHT_SOURCE_VS_NORMALIZED vertexCount="+raw.Length+" maxWorldVertexError="+error.ToString("R")+" original="+sourceRenderer.bounds.ToString("R")+" normalized="+normalized.GetComponentInChildren<Renderer>().bounds.ToString("R"));
            foreach(bool showSource in new[]{true,false}){source.SetActive(showSource);normalized.SetActive(!showSource);yield return null;yield return null;foreach(int width in new[]{1280,1920})Capture(camera,showSource?sourceRenderer.bounds:normalized.GetComponentInChildren<Renderer>().bounds,folder,showSource?"source-upright-original-authored":"source-upright-owned-normalized","isolated",width);}
        }
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(normalized);}
    }
    [Serializable] sealed class Frame {public string path,sha256;public int bytes;}
    [Serializable] sealed class Comparison { public string status="ACTUAL_NATIVE_CONTROLLED_COMPARISON_REQUIRES_VISUAL_REVIEW_NOT_WINDOWS_OR_HUMAN_ACCEPTANCE",originalWorldSha;public Frame[] images;public string[] unchangedInputs; }
    static Bounds[] Views(Component world)
    {
        var landmarks=Components(Get(world,"landmarks"));var water=landmarks[2].GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="River Modeler flowing silver cascade").bounds;
        var cave=landmarks[6].GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Rock_Cluster_1A").ToArray();var bounds=cave[0].bounds;foreach(var r in cave.Skip(1))bounds.Encapsulate(r.bounds);return new[]{water,bounds};
    }
    [UnityTest,Timeout(65000)] public IEnumerator ActualClosedBoulderAuthoredBasisAndOwnedNormalizationHaveIdenticalVerticesAndUprightNativeViews()
    {
        using(var scope=new WildernessNativeScope("camp-closed-boulder-source"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");yield return ClearDay(scope);Call(scope.match,"Pause",0,false);yield return null;yield return null;var world=(Component)Get(scope.match,"world");var profile=(ScriptableObject)Art("CreateIsolatedProfile");string worldSha=Hash(Shared),physics=Physics(world),water=Water(world);string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingClosedBoulderFoundationNative/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try{TestContext.WriteLine(Art("VerifyActualSourceNormalization",profile));using(var camera=new CameraScope(scope))yield return IsolatedSourceComparison(world,profile,camera.camera,folder);Assert.That(Hash(Shared),Is.EqualTo(worldSha));Assert.That(Physics(world),Is.EqualTo(physics));Assert.That(Water(world),Is.EqualTo(water));TestContext.WriteLine("CLOSED_BOULDER_FOUNDATION_NATIVE "+folder+";4 upright actual source/owned normalized images. Actual direct visual review required before cluster comparison/author; not beauty or human acceptance.");}
            finally{Art("DestroyPreview",profile);}
        }
    }
    [UnityTest,Timeout(90000)] public IEnumerator OriginalMaterialOnlyAndUniformNaturalBouldersCompareWithoutChangingAnyWorldPhysicsWaterOrAssets()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(ProfilePath),Is.Null,"Run comparison before new profile author; don't call installed presentation the original.");
        using(var scope=new WildernessNativeScope("camp-landmark-rock-compare"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");yield return ClearDay(scope);Call(scope.match,"Pause",0,false);yield return null;yield return null;
            var world=(Component)Get(scope.match,"world");var profile=(ScriptableObject)Art("CreatePreviewProfile",world);var targets=(Array)Get(profile,"targets");var originals=targets.Cast<object>().Select(t=>Resolve(world,t).GetComponent<MeshRenderer>()).ToArray();var materials=originals.Select(r=>r.sharedMaterials).ToArray();var views=Views(world);
            var files=AssetDatabase.GetDependencies(Shared,true).Where(File.Exists).SelectMany(p=>File.Exists(p+".meta")?new[]{p,p+".meta"}:new[]{p}).Concat(AssetDatabase.GetDependencies((string)Author.GetField("Source").GetValue(null),true).Where(File.Exists).SelectMany(p=>File.Exists(p+".meta")?new[]{p,p+".meta"}:new[]{p})).Distinct().ToArray();var hashes=files.Select(Hash).ToArray();string physics=Physics(world),trucks=Trucks(scope.match),water=Water(world);var envelope=WorldEnvelope(world);
            string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingLandmarkRockComparisonNative/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);var clones=new List<Material>();Component adapter=null;
            try
            {
                using(var camera=new CameraScope(scope))
                {
                    foreach(string variant in new[]{"A-original","B-material-only","C-natural-closed-boulder"})
                    {
                        if(variant=="B-material-only")for(int i=0;i<originals.Length;i++){var replacements=materials[i].Select(m=>{var copy=new Material(m);copy.SetColor("_BaseColor",Color.white);copy.SetFloat("_Smoothness",.12f);clones.Add(copy);return copy;}).ToArray();originals[i].sharedMaterials=replacements;}
                        if(variant=="C-natural-closed-boulder")
                        {
                            for(int i=0;i<originals.Length;i++)originals[i].sharedMaterials=materials[i];adapter=world.gameObject.AddComponent(Runtime("LWS.TruckCamping.CampLandmarkRockPresentation"));Assert.That((bool)Call(adapter,"Apply",world,profile),Is.True,(string)Get(adapter,"Diagnostic"));Assert.That((int)Get(adapter,"VisualCount"),Is.EqualTo(17));Assert.That((int)Get(adapter,"ClimateDirtyCalls"),Is.GreaterThan(0));AssertActualFooting(world,profile,(Transform)Get(adapter,"VisualRoot"));
                        }
                        yield return null;yield return null;
                        for(int view=0;view<2;view++)foreach(int width in new[]{1280,1920})Capture(camera.camera,views[view],folder,variant,view==0?"cascade":"cave",width);
                        if(variant=="C-natural-closed-boulder")
                        {
                            var visual=(Transform)Get(adapter,"VisualRoot");int start=0,group=0;foreach(var target in targets){if(!(bool)Get(target,"overheadCap")){var baseBounds=visual.GetChild(start).GetComponentInChildren<Renderer>().bounds;Capture(camera.camera,baseBounds,folder,"C-natural-base-"+group,"base",1280);}start+=((Array)Get(target,"boulders")).Length;group++;}
                        }
                        Assert.That(Physics(world),Is.EqualTo(physics));Assert.That(Trucks(scope.match),Is.EqualTo(trucks));Assert.That(Water(world),Is.EqualTo(water));Assert.That(WorldEnvelope(world),Is.EqualTo(envelope));
                    }
                }
                Call(adapter,"Release");yield return null;yield return null;for(int i=0;i<originals.Length;i++){Assert.That(originals[i].enabled,Is.True);CollectionAssert.AreEqual(materials[i],originals[i].sharedMaterials);}
                Assert.That((int)Get(adapter,"VisualCount"),Is.Zero);var filter=originals[0].GetComponent<MeshFilter>();var mesh=filter.sharedMesh;filter.sharedMesh=null;Assert.That((bool)Call(adapter,"Apply",world,profile),Is.False);Assert.That(filter.sharedMesh,Is.Null);Assert.That(originals.All(r=>r.enabled),Is.True);filter.sharedMesh=mesh;
                CollectionAssert.AreEqual(hashes,files.Select(Hash).ToArray());Assert.That(Physics(world),Is.EqualTo(physics));Assert.That(Trucks(scope.match),Is.EqualTo(trucks));Assert.That(Water(world),Is.EqualTo(water));Assert.That(WorldEnvelope(world),Is.EqualTo(envelope));
                File.WriteAllText(Path.Combine(folder,"comparison.json"),JsonUtility.ToJson(new Comparison {originalWorldSha=Hash(Shared),images=Directory.GetFiles(folder,"*.png").OrderBy(p=>p).Select(p=>new Frame {path=p,sha256=Hash(p),bytes=checked((int)new FileInfo(p).Length)}).ToArray(),unchangedInputs=files.Select((p,i)=>p+":"+hashes[i]).ToArray()},true),new System.Text.UTF8Encoding(false));
                TestContext.WriteLine("LANDMARK_ROCK_COMPARISON_OUTPUT "+folder+";12 actual720/1080 original/material/natural landmark images +4actual720 base contact closeups; original foundation separately accepted;17closed rocks/5hidden original renderers, five original colliders/all world physics/water/tree/nav/world bytes exact, unknown customization refused and cleanup restored. Image counts are NOT beauty/human/Windows acceptance.");
            }
            finally
            {
                if(adapter!=null){Call(adapter,"Release");UnityEngine.Object.DestroyImmediate(adapter);}for(int i=0;i<originals.Length;i++)if(originals[i]!=null){originals[i].sharedMaterials=materials[i];originals[i].enabled=true;}foreach(var clone in clones)UnityEngine.Object.DestroyImmediate(clone);Art("DestroyPreview",profile);
            }
        }
    }
    [Serializable] sealed class RockSnowMaterialRow
    {
        public int rendererId,materialId;public string renderer,material,shader,scene;public bool active,enabled,eligible,excluded,exterior,inDiscoveredTargets,inActualGroundBindings,materialMatchesActualBinding,hasCoverageAmount;public float coverageAmount;public string[] hierarchy;
    }
    [Serializable] sealed class RockSnowState
    {
        public string checkpoint,selectedPreset,activePreset,weatherKind,snowSourceKind,matchId,diagnostic,visualDiagnostic;public bool live,running,falling,bindingsReady,sameWeatherAuthority,snowComponentEnabled;public float coverage,coverage01,globalTaxiSnow;public int pending,discovered,cursor,cacheRefreshes,groundBindings,sharedMaterials;public bool environmentShaderSupported,exteriorShaderSupported;public RockSnowMaterialRow[] materials;
    }
    static bool BindingMatches(IDictionary bindings,MeshRenderer renderer)
    {if(!bindings.Contains(renderer))return false;var binding=bindings[renderer];return renderer.sharedMaterials.SequenceEqual((Material[])Get(binding,"snow"));}
    static bool ActualEnvironmentSnowReady(Component match,Transform root,string selectedId)
    {
        var weather=Get(match,"presentationWeather");if((string)Get(weather,"PresetId")!=selectedId)return false;var snow=match.GetComponent(Runtime("LWS.TruckSports.SportsSnowSurface"));if(snow==null||!(bool)Get(snow,"Falling")||(float)Get(snow,"Coverage")<=.05f)return false;var surface=(Component)Get(snow,"surface");if(surface==null||!(bool)Get(surface,"EnvironmentBindingsReady")||(int)Get(surface,"PendingEnvironmentBindingCount")!=0||(float)Get(surface,"Coverage01")<=.05f||Shader.GetGlobalFloat("_TaxiSnowCoverage")<=.05f||!(bool)Get(surface,"SnowTextureReady")||(int)Get(surface,"BoundDepthCameraCount")==0||Shader.GetGlobalTexture("_TaxiSnowTexture")==null||Shader.GetGlobalVector("_TaxiSnowExposure").w<.5f)return false;
        var bindings=(IDictionary)Get(surface,"groundBindings");var environment=Resources.Load<Shader>("TruckTaxi/Snow/EnvironmentLit");var exterior=Resources.Load<Shader>("TruckTaxi/Snow/EnvironmentLitExterior");var renderers=root.GetComponentsInChildren<MeshRenderer>();if(renderers.Length!=17)return false;
        return renderers.All(r=>r.enabled&&r.gameObject.activeInHierarchy&&BindingMatches(bindings,r)&&r.sharedMaterial!=null&&r.sharedMaterial.shader!=null&&r.sharedMaterial.shader.isSupported&&(r.sharedMaterial.shader==environment||r.sharedMaterial.shader==exterior));
    }
    static void SnowSnapshot(Component match,Transform root,string folder,string checkpoint)
    {
        var weather=Get(match,"presentationWeather");var presets=Get(weather,"presets");var snow=match.GetComponent(Runtime("LWS.TruckSports.SportsSnowSurface"));Assert.That(snow,Is.Not.Null,"Existing original scene snow coordinator required for diagnostic.");var surface=(Component)Get(snow,"surface");Assert.That(surface,Is.Not.Null);
        var discovered=(IList)Get(surface,"environmentTargets");var bindings=(IDictionary)Get(surface,"groundBindings");var type=surface.GetType();var sourceWeather=(Component)Get(snow,"weather");var rows=new List<RockSnowMaterialRow>();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            var material=renderer.sharedMaterial;var hierarchy=new List<string>();for(var t=renderer.transform;t!=null;t=t.parent)hierarchy.Add(t.name);
            rows.Add(new RockSnowMaterialRow {rendererId=renderer.GetInstanceID(),materialId=material==null?0:material.GetInstanceID(),renderer=renderer.name,material=material==null?"NULL":material.name,shader=material==null||material.shader==null?"NULL":material.shader.name,scene=renderer.gameObject.scene.name,active=renderer.gameObject.activeInHierarchy,enabled=renderer.enabled,eligible=(bool)type.GetMethod("IsSnowEligibleMaterial",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{material}),excluded=(bool)type.GetMethod("IsExcludedEnvironment",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{renderer.transform}),exterior=(bool)type.GetMethod("IsExteriorSurface",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{renderer}),inDiscoveredTargets=discovered.Contains(renderer),inActualGroundBindings=bindings.Contains(renderer),materialMatchesActualBinding=BindingMatches(bindings,renderer),hasCoverageAmount=material!=null&&material.HasProperty("_CoverageAmount"),coverageAmount=material!=null&&material.HasProperty("_CoverageAmount")?material.GetFloat("_CoverageAmount"):0,hierarchy=hierarchy.ToArray()});
        }
        var environment=Resources.Load<Shader>("TruckTaxi/Snow/EnvironmentLit");var exterior=Resources.Load<Shader>("TruckTaxi/Snow/EnvironmentLitExterior");
        var report=new RockSnowState {checkpoint=checkpoint,selectedPreset=(string)Get(presets,"Selection"),activePreset=(string)Get(weather,"PresetId"),weatherKind=Get(weather,"Kind").ToString(),snowSourceKind=sourceWeather==null?"NULL":Get(sourceWeather,"Kind").ToString(),matchId=(string)Get(match,"MatchId"),live=(bool)Get(match,"Live"),running=(bool)Get(match,"Running"),falling=(bool)Get(snow,"Falling"),sameWeatherAuthority=ReferenceEquals(sourceWeather,weather),snowComponentEnabled=((Behaviour)snow).isActiveAndEnabled,coverage=(float)Get(snow,"Coverage"),coverage01=(float)Get(surface,"Coverage01"),globalTaxiSnow=Shader.GetGlobalFloat("_TaxiSnowCoverage"),bindingsReady=(bool)Get(surface,"EnvironmentBindingsReady"),pending=(int)Get(surface,"PendingEnvironmentBindingCount"),discovered=discovered.Count,cursor=(int)Get(surface,"nextEnvironmentTarget"),cacheRefreshes=(int)Get(surface,"CacheRefreshCount"),groundBindings=bindings.Count,sharedMaterials=(int)Get(surface,"SharedEnvironmentMaterialCount"),diagnostic=(string)Get(surface,"Diagnostic"),visualDiagnostic=(string)Get(surface,"VisualDiagnostic"),environmentShaderSupported=environment!=null&&environment.isSupported,exteriorShaderSupported=exterior!=null&&exterior.isSupported,materials=rows.ToArray()};
        string path=Path.Combine(folder,checkpoint+".json");File.WriteAllText(path,JsonUtility.ToJson(report,true),new System.Text.UTF8Encoding(false));TestContext.WriteLine("INSTALLED_ROCK_SNOW_DIAGNOSTIC "+path+" selected="+report.selectedPreset+" active="+report.activePreset+" kind="+report.weatherKind+" source="+report.snowSourceKind+" falling="+report.falling+" coverage="+report.coverage+" surface="+report.coverage01+" global="+report.globalTaxiSnow+" pending="+report.pending+" discovered="+report.discovered+" actualBindings="+rows.Count(r=>r.inActualGroundBindings)+"/"+rows.Count+" propertyCoverage="+rows.Count(r=>r.hasCoverageAmount)+" shaders="+string.Join("/",rows.Select(r=>r.shader).Distinct()));
    }
    [Serializable] sealed class SnowPhoto
    {public string file,sha256,place;public int width,height;public Vector3 cameraPosition,subjectCenter,subjectSize;public Quaternion cameraRotation;public float fieldOfView,globalCoverage;}
    [Serializable] sealed class SnowSource
    {public string path,sha256;public long bytes;}
    [Serializable] sealed class InstalledSnowPhotos
    {
        public string status="ACTUAL_INSTALLED_NATIVE_SNOW_RENDER_VIEWS_NOT_WINDOWS_OR_HUMAN_OR_VISUAL_APPROVAL",selectedPreset,activePreset,weatherKind,timeChoice,matchId;public float globalCoverage,coordinatorCoverage,surfaceCoverage;public int[] rendererIds,materialIds;public bool originalCamerasRestored,worldPhysicsAndWaterExact,sourceBytesExact;public SnowPhoto[] photos;public SnowSource[] sourceHashes;
    }
    static string OriginalCameraState(Component match)=>string.Join(";",((Camera[])Get(match,"cameras")).Select(c=>c.GetInstanceID()+":"+c.enabled+":"+c.gameObject.activeSelf+":"+c.transform.position.ToString("R")+":"+c.transform.rotation.ToString("R")+":"+c.rect.ToString("R")+":"+c.cullingMask+":"+(c.targetTexture==null?0:c.targetTexture.GetInstanceID())+":"+c.fieldOfView.ToString("R")+":"+c.clearFlags));
    static void CaptureInstalledSnow(WildernessNativeScope scope,Component world,Transform rocks,ScriptableObject profile,string folder)
    {
        var weather=Get(scope.match,"presentationWeather");var coordinator=scope.match.GetComponent(Runtime("LWS.TruckSports.SportsSnowSurface"));var surface=(Component)Get(coordinator,"surface");var actual=rocks.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        var paths=new[]{Shared,ProfilePath,AssetDatabase.GetAssetPath((Material)Get(profile,"ownedMaterial")),AssetDatabase.GetAssetPath((GameObject)Get(profile,"boulderPrefab")),AssetDatabase.GetAssetPath((Mesh)Get(profile,"originalRockAMesh")),(string)Author.GetField("Source").GetValue(null),AssetDatabase.GetAssetPath(actual.GetTexture("_BaseMap")),AssetDatabase.GetAssetPath(actual.GetTexture("_BumpMap")),AssetDatabase.GetAssetPath(actual.shader),"Assets/LWS/TruckTaxi/Resources/TruckTaxi/Snow/EnvironmentSnow.hlsl","Assets/TruckCamping/Scripts/CampLandmarkRockPresentation.cs","Assets/TruckCamping/Tests/PlayMode/CampingLandmarkRockNativeTests.cs"}.Where(p=>!string.IsNullOrEmpty(p)).Distinct().SelectMany(p=>new[]{p,p+".meta"}).Where(File.Exists).ToArray();var sources=paths.Select(p=>new SnowSource {path=p,sha256=Hash(p),bytes=new FileInfo(p).Length}).ToArray();
        string cameras=OriginalCameraState(scope.match),physics=Physics(world),water=Water(world);var views=Views(world);var frames=new List<SnowPhoto>();
        // Reuse the existing scoped camera/weather allow-list. No pause, pose,
        // weather reconfiguration, fog suppression or material intervention.
        // Four synchronous renders stay in the actual post-enrollment frame.
        using(var camera=new CameraScope(scope))
        {
            for(int view=0;view<2;view++)foreach(int width in new[]{1280,1920})
            {
                string place=view==0?"cascade":"cave";Capture(camera.camera,views[view],folder,"installed-snow",place,width);string file=Path.Combine(folder,"installed-snow-"+place+"-"+width+".png");frames.Add(new SnowPhoto {file=file,sha256=Hash(file),place=place,width=width,height=width==1280?720:1080,cameraPosition=camera.camera.transform.position,cameraRotation=camera.camera.transform.rotation,subjectCenter=views[view].center,subjectSize=views[view].size,fieldOfView=camera.camera.fieldOfView,globalCoverage=Shader.GetGlobalFloat("_TaxiSnowCoverage")});
            }
        }
        Assert.That(OriginalCameraState(scope.match),Is.EqualTo(cameras));Assert.That(Physics(world),Is.EqualTo(physics));Assert.That(Water(world),Is.EqualTo(water));CollectionAssert.AreEqual(sources.Select(p=>p.sha256).ToArray(),paths.Select(Hash).ToArray());Assert.That(frames.Count,Is.EqualTo(4));
        var report=new InstalledSnowPhotos {selectedPreset=(string)Get(Get(weather,"presets"),"Selection"),activePreset=(string)Get(weather,"PresetId"),weatherKind=Get(weather,"Kind").ToString(),timeChoice=Runtime("LWS.TruckSports.SportsTimeOfDay").GetProperty("Selection",BindingFlags.Public|BindingFlags.Static).GetValue(null).ToString(),matchId=(string)Get(scope.match,"MatchId"),globalCoverage=Shader.GetGlobalFloat("_TaxiSnowCoverage"),coordinatorCoverage=(float)Get(coordinator,"Coverage"),surfaceCoverage=(float)Get(surface,"Coverage01"),rendererIds=rocks.GetComponentsInChildren<Renderer>().Select(r=>r.GetInstanceID()).ToArray(),materialIds=rocks.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial.GetInstanceID()).ToArray(),originalCamerasRestored=true,worldPhysicsAndWaterExact=true,sourceBytesExact=true,photos=frames.ToArray(),sourceHashes=sources};
        string receipt=Path.Combine(folder,"installed-snow-views.json");File.WriteAllText(receipt,JsonUtility.ToJson(report,true),new System.Text.UTF8Encoding(false));SnowSnapshot(scope.match,rocks,folder,"06-after-installed-snow-views");TestContext.WriteLine("INSTALLED_ROCK_SNOW_PHOTOS "+receipt+";4actualnativeCascade/Cave720/1080images in real selectedSnow/Day after strict17binding gate, source hashes/cameras/world exact; direct visual review required, not Windows/human/allweather approval.");
    }
    [UnityTest,Timeout(85000)] public IEnumerator InstalledProfileAppliesOnceAcrossRematchesAndUsesExistingWeatheradeEnrollmentWithoutPhysicsOrWaterChanges()
    {
        using(var scope=new WildernessNativeScope("camp-landmark-rock-installed"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");yield return ClearDay(scope);var world=(Component)Get(scope.match,"world");var adapter=world.GetComponent(Runtime("LWS.TruckCamping.CampLandmarkRockPresentation"));Assert.That(adapter,Is.Not.Null);Assert.That((bool)Get(adapter,"Applied"),Is.True);Assert.That((int)Get(adapter,"VisualCount"),Is.EqualTo(17));
            var root=(Transform)Get(adapter,"VisualRoot");AssertActualFooting(world,Resources.Load<ScriptableObject>("TruckCamping/Camp Haul landmark rock presentation"),root);Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(root.GetComponentsInChildren<Rigidbody>(),Is.Empty);Assert.That(root.GetComponentsInChildren<MonoBehaviour>(),Is.Empty);
            Call(scope.match,"Pause",0,false);yield return null;yield return null;string physics=Physics(world),water=Water(world),trucks=Trucks(scope.match);var bounds=WorldEnvelope(world);Call(adapter,"Release");yield return null;yield return null;
            var profile=Resources.Load<ScriptableObject>("TruckCamping/Camp Haul landmark rock presentation");Assert.That(profile,Is.Not.Null);Assert.That((bool)Call(adapter,"Apply",world,profile),Is.True);yield return null;yield return null;Assert.That(Physics(world),Is.EqualTo(physics));Assert.That(Water(world),Is.EqualTo(water));Assert.That(Trucks(scope.match),Is.EqualTo(trucks));Assert.That(WorldEnvelope(world),Is.EqualTo(bounds));
            Call(scope.match,"Resume");Call(scope.match,"SetupScreen");scope.Configure(1,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(.5f);Assert.That((int)Get(adapter,"VisualCount"),Is.EqualTo(17));
            string snowFolder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingLandmarkRockSnowNative/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(snowFolder);SnowSnapshot(scope.match,(Transform)Get(adapter,"VisualRoot"),snowFolder,"01-after-clear-rematch");
            var weather=Get(scope.match,"presentationWeather");var presets=Get(weather,"presets");var snow=((Array)Get(presets,"entries")).Cast<object>().First(e=>(bool)Get(e,"Snow")&&!(bool)Get(Get(e,"profile"),"Disabled"));Call(presets,"Select",(string)Get(snow,"id"));SnowSnapshot(scope.match,(Transform)Get(adapter,"VisualRoot"),snowFolder,"02-snow-selected-before-start");Call(scope.match,"SetupScreen");scope.Configure(1,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);
            var currentRoot=(Transform)Get(adapter,"VisualRoot");int[] beforeSnow=currentRoot.GetComponentsInChildren<Renderer>().Select(r=>r.GetInstanceID()).ToArray();float end=Time.realtimeSinceStartup+12;yield return null;yield return null;SnowSnapshot(scope.match,currentRoot,snowFolder,"03-snow-start-two-frames");
            // These existing environment shaders consume GLOBAL _TaxiSnowCoverage,
            // not Weatherade road material's absent _CoverageAmount property.
            // Require every real registry binding/current material, so a global
            // amount alone cannot pass while any new renderer is unadapted.
            string selectedSnowId=(string)Get(snow,"id");while(Time.realtimeSinceStartup<end&&!ActualEnvironmentSnowReady(scope.match,currentRoot,selectedSnowId))yield return null;
            SnowSnapshot(scope.match,currentRoot,snowFolder,"04-before-actual-enrollment-assertion");
            Assert.That(ActualEnvironmentSnowReady(scope.match,currentRoot,selectedSnowId),Is.True,"All17 original renderer IDs must use actual shared existing-authority snow bindings and effective shader-global coverage>.05, with ready vendor depth/texture; see exact snapshots.");
            Assert.That((string)Get(presets,"Selection"),Is.EqualTo(selectedSnowId));Assert.That((string)Get(weather,"PresetId"),Is.EqualTo(selectedSnowId));Assert.That(Get(weather,"Kind").ToString(),Is.EqualTo(Get(snow,"legacyKind").ToString()));
            var coordinator=scope.match.GetComponent(Runtime("LWS.TruckSports.SportsSnowSurface"));var surface=(Component)Get(coordinator,"surface");Assert.That(Get(coordinator,"weather"),Is.SameAs(weather));Assert.That((float)Get(coordinator,"Coverage"),Is.GreaterThan(.05f));Assert.That((float)Get(surface,"Coverage01"),Is.GreaterThan(.05f));Assert.That(Shader.GetGlobalFloat("_TaxiSnowCoverage"),Is.EqualTo((float)Get(surface,"Coverage01")).Within(.00001f));Assert.That((float)Get(coordinator,"Coverage"),Is.EqualTo((float)Get(surface,"Coverage01")).Within(.00001f));
            var all=currentRoot.GetComponentsInChildren<MeshRenderer>();CollectionAssert.AreEqual(beforeSnow,all.Select(r=>r.GetInstanceID()).ToArray());Assert.That(all.Select(r=>r.sharedMaterial).Distinct().Count(),Is.EqualTo(1),"One shared material adapted by the original cache for all17 rocks.");var actualMaterial=all[0].sharedMaterial;Assert.That(actualMaterial,Is.Not.SameAs(Get(profile,"ownedMaterial")),"Runtime variant assigned by original authority; authored source material remains intact.");Assert.That(actualMaterial.GetTexture("_BaseMap"),Is.SameAs(((Material)Get(profile,"ownedMaterial")).GetTexture("_BaseMap")));Assert.That(actualMaterial.GetTexture("_BumpMap"),Is.SameAs(((Material)Get(profile,"ownedMaterial")).GetTexture("_BumpMap")));
            for(int frame=0;frame<8;frame++)yield return null;Assert.That(ActualEnvironmentSnowReady(scope.match,currentRoot,selectedSnowId),Is.True);CollectionAssert.AreEqual(beforeSnow,currentRoot.GetComponentsInChildren<Renderer>().Select(r=>r.GetInstanceID()).ToArray());Assert.That(currentRoot.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial==actualMaterial),Is.True,"No per-frame material allocation or renderer rebuild after enrollment.");SnowSnapshot(scope.match,currentRoot,snowFolder,"05-enrollment-stable-eight-frames");
            CaptureInstalledSnow(scope,world,currentRoot,profile,snowFolder);Assert.That(ActualEnvironmentSnowReady(scope.match,currentRoot,selectedSnowId),Is.True);
            TestContext.WriteLine("INSTALLED_LANDMARK_ROCK_PROFILE original5colliders/water/terrain envelope unchanged,17true closed source-mesh/albedo/normal visuals, rematch count stable, actual existing Weatherade snow adaptation; no gameplay/human/Windows/beauty acceptance inferred.");
        }
    }
}
#endif
