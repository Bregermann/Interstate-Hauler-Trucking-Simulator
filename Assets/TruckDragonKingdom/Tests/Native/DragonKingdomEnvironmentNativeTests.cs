#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Explicit saved-scene integration, not a weather replacement or authoring path.
// Only ordinary selectors/start/pause/rematch and actual camera render/readback.
public sealed class DragonKingdomEnvironmentNativeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Component match,weather,snow;
    object surface,clock,cycle,vendor,catalog,views;
    IDisposable preferences;
    GameObject isolation;
    string output,sceneName;
    readonly StringBuilder evidence=new StringBuilder();
    readonly Dictionary<string,string> sourceBytes=new Dictionary<string,string>();
    readonly Dictionary<Terrain,Material> authoredTerrains=new Dictionary<Terrain,Material>();
    readonly Dictionary<Terrain,TerrainLayer[]> authoredLayers=new Dictionary<Terrain,TerrainLayer[]>();
    readonly Dictionary<Terrain,Texture2D[]> authoredControls=new Dictionary<Terrain,Texture2D[]>();
    Rigidbody[] originalBodies;
    bool asyncCaptured,previousAsync;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object item,string name)
    {
        for(var type=item.GetType();type!=null;type=type.BaseType)
        {
            var field=type.GetField(name,Any|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(item);
            var property=type.GetProperty(name,Any|BindingFlags.DeclaredOnly);if(property!=null)return property.GetValue(item);
        }
        throw new MissingMemberException(item.GetType().FullName,name);
    }
    static object Call(object item,string name,params object[] args)=>item.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((parameter,index)=>args[index]==null||parameter.ParameterType.IsInstanceOfType(args[index])).All(ok=>ok)).Invoke(item,args);
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    void KeepPath(string path){if(!string.IsNullOrEmpty(path)&&File.Exists(path)&&!sourceBytes.ContainsKey(path))sourceBytes.Add(path,Hash(path));if(!string.IsNullOrEmpty(path)&&File.Exists(path+".meta")&&!sourceBytes.ContainsKey(path+".meta"))sourceBytes.Add(path+".meta",Hash(path+".meta"));}
    void KeepAsset(UnityEngine.Object asset)
    {
        if(asset==null)return;string path=AssetDatabase.GetAssetPath(asset);if(string.IsNullOrEmpty(path))return;
        KeepPath(path);foreach(string dependency in AssetDatabase.GetDependencies(path,true))KeepPath(dependency);
    }
    void SourcesExact(){foreach(var pair in sourceBytes)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"No scene/profile/material/shader/metadata authoring during weather play: "+pair.Key);}
    void Log(string text){evidence.AppendLine(text);TestContext.WriteLine(text);if(output!=null)File.WriteAllText(Path.Combine(output,"observations.txt"),evidence.ToString());}
    Camera[] Cameras()=>((Camera[])Get(match,"cameras")).Where(camera=>camera!=null&&camera.enabled&&camera.gameObject.activeInHierarchy).ToArray();
    Rigidbody[] Bodies()=>((Array)Get(match,"trucks")).Cast<object>().Select(truck=>(Rigidbody)Get(truck,"body")).ToArray();
    ParticleSystem[] SnowParticles()=>((Component)Get(Get(weather,"precipitation"),"SnowScript")).GetComponentsInChildren<ParticleSystem>(true);
    ParticleSystem[] RainParticles()=>((Component)Get(Get(weather,"precipitation"),"RainScript")).GetComponentsInChildren<ParticleSystem>(true);
    static int ParticleCount(ParticleSystem[] systems)=>systems.Sum(system=>system.particleCount);
    static Dictionary<uint,float> ParticleLifetimes(ParticleSystem system)
    {
        var particles=new ParticleSystem.Particle[system.particleCount];int count=system.GetParticles(particles);var result=new Dictionary<uint,float>();
        for(int i=0;i<count;i++)result[particles[i].randomSeed]=particles[i].remainingLifetime;return result;
    }
    void ParticleStates(string label,ParticleSystem[] systems)
    {
        foreach(var system in systems)Log("PARTICLE_STATE "+label+" id="+system.GetEntityId()+" name="+system.name+" active="+system.gameObject.activeInHierarchy+" playing="+system.isPlaying+" paused="+system.isPaused+" emitting="+system.isEmitting+" phase="+system.time+" duration="+system.main.duration+" loop="+system.main.loop+" count="+system.particleCount);
    }
    // Read the product's exact supported-surface/exposure predicate. Counting all
    // trace stamps alone also counts cargo/hoist bodies, including indoor ones.
    bool ExposedMovingWheel(bool report)
    {
        var wheels=(Array)Get(snow,"wheels");var predicate=snow.GetType().GetMethod("Traceable",Any);int grounded=0,exposed=0,moving=0;var facts=new StringBuilder();
        foreach(Component wheel in wheels)
        {
            if(wheel==null||!wheel.gameObject.activeInHierarchy||wheel is Behaviour behaviour&&!behaviour.enabled||!(bool)Get(wheel,"IsGrounded"))continue;
            var shape=Get(wheel,"HitCollider") as Collider;var point=(Vector3)Get(wheel,"HitPoint");var normal=(Vector3)Get(wheel,"HitNormal");if(shape==null||normal.y<.25f)continue;grounded++;
            var body=wheel.GetComponentInParent<Rigidbody>();var args=new object[]{shape,point,body!=null?body.transform:wheel.transform,false};bool clear=(bool)predicate.Invoke(snow,args);float speed=body!=null?body.GetPointVelocity(point).magnitude:0;if(clear){exposed++;if(speed>.1f)moving++;}
            if(report)
            {
                var roofs=Physics.RaycastAll(point+Vector3.up*.15f,Vector3.up,128,~((1<<30)|(1<<29)),QueryTriggerInteraction.Ignore).Where(hit=>body==null||!hit.transform.IsChildOf(body.transform)).OrderBy(hit=>hit.distance).Take(3);
                facts.Append(" [").Append(wheel.name).Append(" hit=").Append(shape.name).Append(" at=").Append(point).Append(" exposed=").Append(clear).Append(" speed=").Append(speed).Append(" overhead=").Append(string.Join(";",roofs.Select(hit=>hit.transform.name+":"+hit.distance))).Append(']');
            }
        }
        if(report)Log("ACTUAL_WHEEL_EXPOSURE grounded="+grounded+" supportedExposed="+exposed+" movingExposed="+moving+facts);return moving>0;
    }
    void ActualPrecipitationRenderer(ParticleSystem[] systems,string label)
    {
        var cameras=Cameras();var rendered=systems.Where(system=>system.gameObject.activeInHierarchy&&system.particleCount>0).Where(system=>
        {
            var renderer=system.GetComponent<ParticleSystemRenderer>();return renderer!=null&&renderer.enabled&&renderer.sharedMaterial!=null&&renderer.sharedMaterial.shader!=null&&renderer.sharedMaterial.shader.isSupported&&cameras.Any(camera=>GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),renderer.bounds));
        }).ToArray();
        Assert.That(rendered.Length,Is.GreaterThan(0),"Actual "+label+" particles need an enabled supported renderer intersecting a real weather viewport, not an intensity field alone.");
        Log("PRECIP_RENDER "+label+" actualSystems="+string.Join(";",rendered.Select(system=>system.name+":"+system.particleCount+":"+system.GetComponent<ParticleSystemRenderer>().bounds)));
    }
    void RenderActualViews()
    {
        var target=RenderTexture.GetTemporary(320,180,24,RenderTextureFormat.ARGB32);
        try {foreach(var camera in Cameras()){var prior=camera.targetTexture;try{camera.targetTexture=target;camera.Render();}finally{camera.targetTexture=prior;}}}
        finally {RenderTexture.ReleaseTemporary(target);}
    }
    void Capture(string label)=>Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(output,label+".png"),null});
    void ActualNoSnowTerrains(string label)
    {
        int visible=0;
        foreach(var pair in authoredTerrains)
        {
            var terrain=pair.Key;var material=terrain.materialTemplate;Assert.That(material,Is.Not.Null);Assert.That(material.shader,Is.Not.Null);Assert.That(material.shader.isSupported,Is.True);
            bool snowShader=material.shader.name=="NOT_Lonely/Weatherade/Snow Coverage (Terrain)";
            Assert.That(material==pair.Value||snowShader||material.shader.name=="NOT_Lonely/Weatherade/Rain Coverage (Terrain)",Is.True,"An actual supported authored or installed vendor terrain shader is required.");
            if(snowShader)Assert.That(material.GetFloat("_CoverageAmount"),Is.LessThan(.001f),"Cached snow-capable material must actually disable snow in a no-snow rematch.");
            if(material!=pair.Value)Assert.That(AssetDatabase.Contains(material),Is.False,"Weather adaptation belongs to the runtime instance, never the source material.");
            CollectionAssert.AreEqual(authoredLayers[terrain],terrain.terrainData.terrainLayers,"Original authored terrain layers remain bound.");CollectionAssert.AreEqual(authoredControls[terrain],terrain.terrainData.alphamapTextures,"Actual terrain blend controls remain bound.");
            var bounds=terrain.terrainData.bounds;bounds.center+=terrain.transform.position;
            if(terrain.enabled&&terrain.drawHeightmap&&terrain.gameObject.activeInHierarchy&&Cameras().Any(camera=>GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),bounds)))visible++;
            Log("NO_SNOW_TERRAIN "+label+" terrain="+terrain.name+" material="+material.name+" shader="+material.shader.name+" snowAmount="+(material.HasProperty("_CoverageAmount")?material.GetFloat("_CoverageAmount").ToString():"n/a")+" keywords="+string.Join(",",material.shaderKeywords.OrderBy(value=>value))+" layers="+string.Join(",",terrain.terrainData.terrainLayers.Select(layer=>layer==null?"null":AssetDatabase.GetAssetPath(layer)))+" controls="+terrain.terrainData.alphamapTextures.Length);
        }
        Assert.That(visible,Is.GreaterThan(0),"Actual no-snow captures must render a supported visible terrain, not just report coverage flags.");Assert.That(Shader.GetGlobalFloat("_TaxiSnowCoverage"),Is.LessThan(.001f));
    }
    void ActualRoadSourceAppearance(string label,bool snowy)
    {
        var clear=Get(snow,"clearRoadMaterial") as Material;var roads=(IDictionary)Get(snow,"runtimeRoads");
        Assert.That(roads.Count,Is.GreaterThan(0),"Actual saved road surfaces must have their existing runtime clones.");
        int boundSlots=0;var originals=(IDictionary)Get(snow,"originalRoads");
        foreach(DictionaryEntry binding in originals)
        {
            var renderer=(MeshRenderer)binding.Key;Assert.That(renderer,Is.Not.Null,"The actual original marked road renderer must remain available.");
            var before=(Material[])binding.Value;var actual=renderer.sharedMaterials;Assert.That(actual.Length,Is.EqualTo(before.Length),"Original road material slots are preserved: "+renderer.name);
            for(int slot=0;slot<before.Length;slot++)
            {
                bool adapted=before[slot]!=null&&roads.Contains(before[slot]);var expected=adapted?(Material)roads[before[slot]]:before[slot];
                Assert.That(actual[slot],Is.EqualTo(expected),"Actual rendered road slot must bind its mapped runtime clone or retain its unrelated original material: "+renderer.name+"/"+slot);
                if(adapted)boundSlots++;
            }
        }
        Assert.That(boundSlots,Is.GreaterThan(0),"A correctly configured but unbound clone cannot establish rendered road acceptance.");
        if(clear!=null)KeepAsset(clear);
        foreach(DictionaryEntry entry in roads)
        {
            var source=(Material)entry.Key;var current=(Material)entry.Value;KeepAsset(source);
            Assert.That(AssetDatabase.Contains(source),Is.True);Assert.That(AssetDatabase.Contains(current),Is.False,"Only scene-local material clones may transition.");
            Material expected=snowy?source:clear;Shader shader=snowy?source.shader:clear!=null?clear.shader:Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(current.shader,Is.EqualTo(shader),"Actual selected clear/snow shader: "+label);
            if(clear==null)
            {
                Assert.That(current.GetTexture("_BaseMap"),Is.EqualTo(source.GetTexture("_BaseMap")));Assert.That(current.GetTextureScale("_BaseMap"),Is.EqualTo(source.GetTextureScale("_BaseMap")));Assert.That(current.GetTextureOffset("_BaseMap"),Is.EqualTo(source.GetTextureOffset("_BaseMap")));
                Assert.That(current.GetFloat("_Smoothness"),Is.EqualTo(.18f).Within(.00001f),"Unconfigured legacy scene retains its established road presentation.");
            }
            else if(snowy)
            {
                // Weatherade owns dynamic coverage, textures and trace controls.
                // The managed base PBR appearance must still match its saved variant.
                foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"})if(source.HasProperty(property))
                {Assert.That(current.GetTexture(property),Is.EqualTo(source.GetTexture(property)),property);Assert.That(current.GetTextureScale(property),Is.EqualTo(source.GetTextureScale(property)),property);Assert.That(current.GetTextureOffset(property),Is.EqualTo(source.GetTextureOffset(property)),property);}
                foreach(string property in new[]{"_BumpScale","_Metallic","_Smoothness","_OcclusionStrength"})if(source.HasProperty(property))Assert.That(current.GetFloat(property),Is.EqualTo(source.GetFloat(property)).Within(.00001f),property);
                foreach(string property in new[]{"_BaseColor","_Color"})if(source.HasProperty(property))Assert.That(current.GetColor(property),Is.EqualTo(source.GetColor(property)),property);
                foreach(string keyword in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"})Assert.That(current.IsKeywordEnabled(keyword),Is.EqualTo(source.IsKeywordEnabled(keyword)),keyword);
                Assert.That(current.renderQueue,Is.EqualTo(source.renderQueue));
                Assert.That(current.IsKeywordEnabled("_TRACES_ON"),Is.True,"Returning to Snow must restore actual vendor trace rendering, not only its remembered material identity.");
                Assert.That(current.GetFloat("_Traces"),Is.EqualTo(1));Assert.That(current.GetFloat("_TracesOverride"),Is.EqualTo(1));
            }
            else
            {
                // The actual native clear source has EasyRoads texture aliases,
                // depth bias and material state; inspect every declared property.
                for(int index=0;index<expected.shader.GetPropertyCount();index++)
                {
                    string property=expected.shader.GetPropertyName(index);Assert.That(current.HasProperty(property),Is.True,property);
                    switch(expected.shader.GetPropertyType(index))
                    {
                        case ShaderPropertyType.Texture:Assert.That(current.GetTexture(property),Is.EqualTo(expected.GetTexture(property)),property);Assert.That(current.GetTextureScale(property),Is.EqualTo(expected.GetTextureScale(property)),property);Assert.That(current.GetTextureOffset(property),Is.EqualTo(expected.GetTextureOffset(property)),property);break;
                        case ShaderPropertyType.Color:Assert.That(current.GetColor(property),Is.EqualTo(expected.GetColor(property)),property);break;
                        case ShaderPropertyType.Vector:Assert.That(current.GetVector(property),Is.EqualTo(expected.GetVector(property)),property);break;
                        case ShaderPropertyType.Int:Assert.That(current.GetInteger(property),Is.EqualTo(expected.GetInteger(property)),property);break;
                        default:Assert.That(current.GetFloat(property),Is.EqualTo(expected.GetFloat(property)).Within(.00001f),property);break;
                    }
                }
                CollectionAssert.AreEquivalent(expected.shaderKeywords,current.shaderKeywords);Assert.That(current.renderQueue,Is.EqualTo(expected.renderQueue));
            }
        }
        Log("ROAD_SOURCE_APPEARANCE "+label+" runtimeClones="+roads.Count+" actualBoundRendererSlots="+boundSlots+" configuredClear="+(clear==null?"legacy-null":AssetDatabase.GetAssetPath(clear))+" actualShaderAndPbrExact=true sourceBytesWatched=true");
    }
    IEnumerator ActualMovingWheelGpu()
    {
        var generator=(Component)Get(surface,"generator");var objects=(Camera)Get(generator,"traceObjsCam");var supports=(Camera)Get(generator,"traceSurfCam");
        int objectRenders=0,supportRenders=0,readbacks=0,maxTracers=0,startStamps=(int)Get(surface,"WheelTraceStampCount"),frames=0;bool pending=false,proved=false;float began=Time.realtimeSinceStartup,until=began+6,nextRequest=0,maxFrame=0;double frameSum=0;long memoryBefore=UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();
        foreach(string field in new[]{"traceObjsRT","surfaceDepthRT","traceMaskRT","traceTex","finalTraceMask","bluredRT"}){var texture=(RenderTexture)Get(generator,field);Log("ACTUAL_TRACE_RESOURCE "+field+" size="+texture.width+"x"+texture.height+" format="+texture.format+" samples="+texture.antiAliasing+" runtimeBytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture));}
        Action<ScriptableRenderContext,Camera> rendered=(context,camera)=>{if(camera==objects)objectRenders++;if(camera==supports)supportRenders++;};RenderPipelineManager.endCameraRendering+=rendered;
        try
        {
            // Ordinary frames keep the existing vendor mask coroutine and real
            // SportsSnowSurface LateUpdate active. Never call SimOneStep/TraceWheel.
            while(Time.realtimeSinceStartup<until&&!proved)
            {
                yield return null;frames++;frameSum+=Time.unscaledDeltaTime;maxFrame=Mathf.Max(maxFrame,Time.unscaledDeltaTime);RenderActualViews();int active=(int)Get(surface,"ActiveWheelTraceSourceCount");maxTracers=Math.Max(maxTracers,active);
                if(pending&&!((string)Get(surface,"TracePixelDiagnostic")).Contains("pending")){pending=false;proved=(int)Get(surface,"TraceActivePixelCount")>0;}
                if(!proved&&!pending&&Time.realtimeSinceStartup>=nextRequest&&active>0&&(int)Get(surface,"WheelTraceStampCount")>startStamps&&ExposedMovingWheel(false))
                {Call(surface,"RequestTracePixelDiagnostic");readbacks++;pending=true;nextRequest=Time.realtimeSinceStartup+.25f;}
            }
            Log("GPU_RENDER_WINDOW framesRemainReal=true newStamps="+((int)Get(surface,"WheelTraceStampCount")-startStamps)+" maxActiveTracers="+maxTracers+" actualDepthRenders="+objectRenders+"/"+supportRenders+" readbacks="+readbacks+" result="+Get(surface,"TracePixelDiagnostic"));ExposedMovingWheel(true);
            Log("TRACE_CANDIDATE_COST actualObjectMask="+objects.targetTexture.width+" fullVolumeSpan="+(objects.orthographicSize*2)+" objectMetersPerPixel="+(objects.orthographicSize*2/objects.targetTexture.width)+" observedRealSeconds="+(Time.realtimeSinceStartup-began)+" frameSamples="+frames+" meanFrameSeconds="+(frames>0?frameSum/frames:0)+" maxFrameSeconds="+maxFrame+" graphicsDriverBytesBefore="+memoryBefore+" after="+UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver()+" unsupportedDriverCounterMayBeZero=true notPerformanceCertification=true");
            Assert.That(objectRenders,Is.GreaterThan(0));Assert.That(supportRenders,Is.GreaterThan(0));Assert.That(readbacks,Is.GreaterThan(0),"Exposed moving contacts must produce active real traces before GPU sampling.");Assert.That(proved,Is.True,"Actual exposed moving NWH wheel requires real GPU contact-mask pixels; no manual stamps.");
        }
        finally{RenderPipelineManager.endCameraRendering-=rendered;}
    }
    void RecordSnowVisibility()
    {
        var coverage=(Component)Get(surface,"coverage");var texture=Shader.GetGlobalTexture("_SRS_depth") as RenderTexture;var roadMaterials=((IEnumerable)Get(surface,"roadTargets")).Cast<MeshRenderer>().Where(renderer=>renderer!=null).SelectMany(renderer=>renderer.sharedMaterials).Where(material=>material!=null).Distinct().ToArray();
        Log("SNOW_VISIBILITY_BINDINGS actualRoadMaterials="+string.Join(";",roadMaterials.Select(material=>material.name+":"+material.shader.name+":"+(material.HasProperty("_CoverageAmount")?material.GetFloat("_CoverageAmount").ToString():"noSnowAmount")))+" globalDepth="+(texture!=null?texture.name+":"+texture.width+"x"+texture.height+":"+texture.format:"missing"));
        foreach(var terrain in authoredTerrains.Keys)
        {
            var material=terrain.materialTemplate;var properties=new[]{"_CoverageAmount","_Coverage","_CoverageAreaBias","_CoverageAreaMaskRange","_CoverageLeakReduction","_PrecipitationDirOffset","_BlendByNormalsPower","_BlendByNormalsStrength","_DistanceFadeStart","_DistanceFadeFalloff"};
            Log("SNOW_TERRAIN_SHADER terrain="+terrain.name+" shader="+material.shader.name+" keywords="+string.Join(",",material.shaderKeywords)+" "+string.Join(" ",properties.Where(material.HasProperty).Select(property=>property+"="+material.GetFloat(property)))+" color="+(material.HasProperty("_CoverageColor")?material.GetColor("_CoverageColor").ToString():"n/a")+" texture="+material.GetTexture("_CoverageTex0"));
        }
        if(texture==null||!texture.IsCreated()){Log("VSM_READONLY unavailable; no visible accumulation acceptance inferred.");return;}
        var previous=RenderTexture.active;var sample=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);var matrix=Shader.GetGlobalMatrix("_depthCamMatrix");float floor=Shader.GetGlobalFloat("_depthCamFarHeight"),exponent=Shader.GetGlobalFloat("_VsmExp"),bias=(float)Get(coverage,"coverageAreaBias"),range=(float)Get(coverage,"coverageAreaMaskRange"),bleed=(float)Get(coverage,"coverageLeakReduction");
        try
        {
            RenderTexture.active=texture;
            foreach(Component wheel in (Array)Get(snow,"wheels"))
            {
                if(wheel==null||!(bool)Get(wheel,"IsGrounded"))continue;var shape=Get(wheel,"HitCollider") as Collider;if(!(shape is TerrainCollider))continue;var point=(Vector3)Get(wheel,"HitPoint");var body=wheel.GetComponentInParent<Rigidbody>();bool exposed=(bool)Call(surface,"IsSnowCoveredOffRoad",shape,point,body!=null?body.transform:wheel.transform);if(!exposed)continue;
                var adjusted=point;adjusted.y=(point.y+floor)*.5f;var clip=matrix.MultiplyPoint3x4(adjusted);clip.z=1-clip.z;var uv=new Vector2(.5f+.5f*clip.x,.5f+.5f*clip.y);int x=Mathf.Clamp(Mathf.FloorToInt(uv.x*texture.width),0,texture.width-1),y=Mathf.Clamp(Mathf.FloorToInt(uv.y*texture.height),0,texture.height-1);sample.ReadPixels(new Rect(x,y,1,1),0,0,false);sample.Apply(false,false);var moments=sample.GetPixel(0,0);
                double warped=Math.Exp(exponent*(-2*clip.z-1)),minimum=bias*exponent*warped;minimum*=minimum;double variance=Math.Max(moments.g-moments.r*moments.r,minimum),delta=warped-moments.r,probability=variance/(variance+delta*delta);probability=Math.Clamp((probability-bleed)/(1-bleed),0,1);if(warped<=moments.r)probability=1;double t=Math.Clamp(5*probability/(10*range),0,1),basic=Math.Sqrt(Math.Clamp(t*t*(3-2*t)*2,0,1));
                Log("VSM_READONLY exposedTerrain="+shape.name+" point="+point.ToString("F3")+" uv="+uv+" clipZ="+clip.z+" nearestMoments="+moments.ToString("F8")+" warped="+warped+" estimatedBasicMaskFromVendorFormula="+basic+" nearestSampleNotFinalSnowMask=true");
            }
        }
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(sample);}
    }
    static Color32[] CapturePixels(string path)
    {
        var image=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try{Assert.That(image.LoadImage(File.ReadAllBytes(path),false),Is.True);Assert.That(image.width,Is.EqualTo(1280));Assert.That(image.height,Is.EqualTo(720));return image.GetPixels32();}
        finally{UnityEngine.Object.DestroyImmediate(image);}
    }
    static int PixelDifference(Color32 a,Color32 b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Max(Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b)));
    void CaptureSnowWorld(string label)
    {
        var canvas=((Component)Get(match,"menus")).GetComponent<Canvas>();bool enabled=canvas.enabled;
        try
        {
            // The ordinary paused UI is separately captured unchanged. Its
            // opaque plate must not attenuate the world-only contrast pixels.
            canvas.enabled=false;Capture(label);
            Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1920,1080,Path.Combine(output,label+"-1080.png"),null});
        }
        finally{canvas.enabled=enabled;}
        Assert.That(canvas.enabled,Is.EqualTo(enabled));
    }
    void RecordSnowContrastWhilePaused()
    {
        Assert.That(Get(match,"Paused"),Is.EqualTo(true));Assert.That(Time.timeScale,Is.Zero);
        var materials=authoredTerrains.Keys.Select(terrain=>terrain.materialTemplate).Concat(((IDictionary)Get(snow,"runtimeRoads")).Values.Cast<Material>()).Where(material=>material!=null&&material.HasProperty("_CoverageAmount")).Distinct().ToArray();Assert.That(materials.Length,Is.GreaterThan(0));foreach(var material in materials)Assert.That(AssetDatabase.Contains(material),Is.False,"Contrast changes only owned runtime clones, never saved materials.");
        var amounts=materials.Select(material=>material.GetFloat("_CoverageAmount")).ToArray();
        CaptureSnowWorld("snow-contrast-applied");var applied=CapturePixels(Path.Combine(output,"snow-contrast-applied.png"));
        try{for(int index=0;index<materials.Length;index++)materials[index].SetFloat("_CoverageAmount",0);CaptureSnowWorld("snow-contrast-runtime-coverage-disabled");}
        finally{for(int index=0;index<materials.Length;index++)materials[index].SetFloat("_CoverageAmount",amounts[index]);}
        var disabled=CapturePixels(Path.Combine(output,"snow-contrast-runtime-coverage-disabled.png"));CaptureSnowWorld("snow-contrast-restored");var restored=CapturePixels(Path.Combine(output,"snow-contrast-restored.png"));
        int changed=applied.Select((color,index)=>PixelDifference(color,disabled[index])>12).Count(value=>value),drift=applied.Select((color,index)=>PixelDifference(color,restored[index])>12).Count(value=>value);var cameras=Cameras();int totalEligible=0,totalChanged=0,totalDrift=0;
        for(int view=0;view<cameras.Length;view++)
        {
            var camera=cameras[view];float aspect=camera.aspect;int eligible=0,effect=0,noise=0;
            try
            {
                camera.aspect=1280f*camera.rect.width/(720f*camera.rect.height);
                for(int y=0;y<9;y++)for(int x=0;x<16;x++)
                {
                    float u=(x+.5f)/16,v=(y+.5f)/9;var ray=camera.ViewportPointToRay(new Vector3(u,v,0));
                    if(!Physics.Raycast(ray,out var hit,camera.farClipPlane,~((1<<29)|(1<<30)|(1<<5)),QueryTriggerInteraction.Ignore))continue;
                    bool road=hit.collider.GetComponentsInParent<MonoBehaviour>(true).Any(item=>item!=null&&item.GetType().FullName=="LWS.InterstateHauler.LwsRoadSurface");if(!(hit.collider is TerrainCollider)&&!road)continue;
                    var args=new object[]{hit.collider,hit.point,null,false};if(!(bool)snow.GetType().GetMethod("Traceable",Any).Invoke(snow,args))continue;
                    int px=Mathf.Clamp(Mathf.FloorToInt((camera.rect.x+u*camera.rect.width)*1280),0,1279),py=Mathf.Clamp(Mathf.FloorToInt((camera.rect.y+v*camera.rect.height)*720),0,719),pixel=py*1280+px;eligible++;if(PixelDifference(applied[pixel],disabled[pixel])>12)effect++;if(PixelDifference(applied[pixel],restored[pixel])>12)noise++;
                }
            }
            finally{camera.aspect=aspect;}
            totalEligible+=eligible;totalChanged+=effect;totalDrift+=noise;Log("SNOW_CONTRAST_VIEW view="+view+" exposedSurfaceSamples="+eligible+" visiblyChanged="+effect+" restoreDrift="+noise+" minimumGenuineEffectOnlyNotSubstantialCoverageAcceptance=true");
            if(eligible>0)Assert.That(effect,Is.GreaterThan(noise),"Actual snow must visibly affect exposed surfaces in each view beyond restored-render drift.");
        }
        Log("SNOW_CONTRAST actualAppliedDisabledRestoredPNGs=true changedPixels="+changed+" restoredDriftPixels="+drift+" realExposedSamples="+totalEligible+" affected="+totalChanged+" affectedRestoreDrift="+totalDrift+" materials="+materials.Length+" noAmountIncrease=true noSynthesizedCoverage=true visualSubstantialnessRequiresLeadReview=true");
        if(totalEligible>0)Assert.That(changed,Is.GreaterThan(drift),"Actual rendered snow effect must exceed the same-frame restore control.");else Log("VISIBLE_ACCUMULATION_UNVERIFIED no actual exposed supported surface is visible in the sampled paused views; no indoor snow is forced.");for(int index=0;index<materials.Length;index++)Assert.That(materials[index].GetFloat("_CoverageAmount"),Is.EqualTo(amounts[index]));SourcesExact();
    }
    float SunAngle(Camera camera)
    {
        var manager=((Component)vendor).GetComponentsInChildren<MonoBehaviour>(true).Single(item=>item.GetType().Name=="WeatherMakerLightManagerScript");
        var celestial=manager.GetType().GetMethod("SunForCamera",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{camera});Assert.That(celestial,Is.Not.Null,"A real rendered viewport must resolve its vendor celestial light.");
        var light=(Light)Get(celestial,"Light");Assert.That(light,Is.Not.Null);return light.transform.eulerAngles.x;
    }
    void ViewOwnership(bool four)
    {
        var cameras=Cameras();Assert.That(cameras.Length,Is.EqualTo(four?4:1));Assert.That(cameras.Distinct().Count(),Is.EqualTo(cameras.Length));
        var allowed=((IEnumerable)Get(vendor,"AllowCameras")).Cast<Camera>().ToArray();CollectionAssert.AreEquivalent(cameras,allowed,"Every viewport uses the same actual Weather Maker authority.");
        Assert.That(UnityEngine.Object.FindObjectsByType(Runtime("SportsChaosWeather"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        var players=(Array)Get(Get(match,"setup"),"players");for(int slot=0;slot<4;slot++){Assert.That(Get(players.GetValue(slot),"source").ToString(),Is.EqualTo("AI"));Assert.That(Bodies()[slot],Is.SameAs(originalBodies[slot]));Assert.That(originalBodies[slot].gameObject.activeInHierarchy&&originalBodies[slot].detectCollisions,Is.True);}
        float area=0;for(int i=0;i<cameras.Length;i++){var a=cameras[i].rect;area+=a.width*a.height;for(int j=0;j<i;j++){var b=cameras[j].rect;Assert.That(Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin)),Is.LessThan(.0001f));}}
        Assert.That(area,Is.EqualTo(1).Within(.0001f));
        Log("ACTUAL_VIEWS count="+cameras.Length+" cameraIds="+string.Join(",",cameras.Select(camera=>camera.GetEntityId()))+" sharedVendor="+((Component)vendor).GetEntityId()+" sunAngles="+string.Join(",",cameras.Select(SunAngle)));
    }
    IEnumerator PauseActualWeather()
    {
        var systems=SnowParticles();var positions=Bodies().Select(body=>body.position).ToArray();var rotations=Bodies().Select(body=>body.rotation).ToArray();
        ParticleStates("before-pause",systems);var live=systems.Select(system=>system.isPlaying&&system.particleCount>0).ToArray();Assert.That(live.Any(value=>value),Is.True);
        Call(match,"Pause",0,false);Assert.That(Get(match,"Paused"),Is.EqualTo(true));yield return null;
        var particleTimes=systems.Select(system=>system.time).ToArray();var particleCounts=systems.Select(system=>system.particleCount).ToArray();float coverage=(float)Get(snow,"Coverage"),seconds=(float)Get(cycle,"TimeOfDay"),age=(float)Get(match,"Clock");int stamps=(int)Get(surface,"WheelTraceStampCount");
        var lifetimes=systems.Select(ParticleLifetimes).ToArray();ParticleStates("paused-snapshot",systems);
        yield return new WaitForSecondsRealtime(.25f);RenderActualViews();
        for(int slot=0;slot<4;slot++){float positionDelta=Vector3.Distance(Bodies()[slot].position,positions[slot]),rotationDelta=Quaternion.Angle(Bodies()[slot].rotation,rotations[slot]);Log("ACTUAL_PAUSED_BODY slot="+slot+" initialPosition="+positions[slot]+" currentPosition="+Bodies()[slot].position+" positionDelta="+positionDelta.ToString("R")+" rotationDelta="+rotationDelta.ToString("R")+" kinematic="+Bodies()[slot].isKinematic);Assert.That(positionDelta,Is.LessThan(.001f));Assert.That(rotationDelta,Is.LessThan(.001f));}
        for(int i=0;i<systems.Length;i++){Assert.That(systems[i].time,Is.EqualTo(particleTimes[i]).Within(.0001f));Assert.That(systems[i].particleCount,Is.EqualTo(particleCounts[i]));var now=ParticleLifetimes(systems[i]);CollectionAssert.AreEquivalent(lifetimes[i].Keys,now.Keys);foreach(var particle in now)Assert.That(particle.Value,Is.EqualTo(lifetimes[i][particle.Key]).Within(.0001f));}
        Assert.That((float)Get(snow,"Coverage"),Is.EqualTo(coverage).Within(.00001f));Assert.That(Get(surface,"WheelTraceStampCount"),Is.EqualTo(stamps));Assert.That((float)Get(cycle,"TimeOfDay"),Is.EqualTo(seconds));Assert.That((float)Get(match,"Clock"),Is.EqualTo(age));
        Capture("snow-day-four-views-ordinary-pause");RecordSnowContrastWhilePaused();Call(match,"Resume");Assert.That(Get(match,"Paused"),Is.EqualTo(false));ParticleStates("immediately-resumed",systems);
        var advanced=new bool[systems.Length];var played=systems.Select(system=>system.isPlaying&&!system.isPaused).ToArray();var naturalEnd=new bool[systems.Length];var proofs=new string[systems.Length];float resumedAt=Time.time,until=Time.realtimeSinceStartup+1;
        while(Time.realtimeSinceStartup<until&&!live.Select((running,index)=>!running||advanced[index]).All(value=>value))
        {
            yield return null;
            for(int i=0;i<systems.Length;i++)if(live[i]&&!advanced[i])
            {
                var current=ParticleLifetimes(systems[i]);bool playing=systems[i].isPlaying&&!systems[i].isPaused;played[i]|=playing;
                if(playing)
                {
                    foreach(var particle in current)if(lifetimes[i].TryGetValue(particle.Key,out float prior)&&particle.Value<prior-.01f){advanced[i]=true;proofs[i]="existingSeedAged="+particle.Key;break;}
                    // A very short-lived old flake may expire between frames.
                    // Require actual playback, new birth AND an old seed absent
                    // only after enough simulated time for its recorded life.
                    if(!advanced[i]&&current.Keys.Any(seed=>!lifetimes[i].ContainsKey(seed))&&lifetimes[i].Any(old=>!current.ContainsKey(old.Key)&&Time.time-resumedAt>=old.Value))
                    {advanced[i]=true;proofs[i]="observedPlaying/newBirth/oldDeathAfterRecordedLifetime";}
                }
                naturalEnd[i]=!systems[i].main.loop&&played[i]&&systems[i].isStopped&&current.Count==0&&Time.time-resumedAt>=lifetimes[i].Values.Max();
                if(naturalEnd[i]){advanced[i]=true;proofs[i]="observedPlaying/nonLoopOldParticlesExpiredAfterRecordedLifetimes";}
            }
        }
        ParticleStates("resume-observed",systems);Assert.That((float)Get(match,"Clock"),Is.GreaterThan(age));
        for(int i=0;i<systems.Length;i++)if(live[i])
        {
            naturalEnd[i]|=!systems[i].main.loop&&played[i]&&systems[i].isStopped&&systems[i].particleCount==0&&Time.time-resumedAt>=lifetimes[i].Values.Max();
            Log("PARTICLE_RESUME_PROOF id="+systems[i].GetEntityId()+" name="+systems[i].name+" progressed="+advanced[i]+" played="+played[i]+" proof="+proofs[i]);
            Assert.That(advanced[i],Is.True,"Each originally playing live snow system needs its own actual aging or proven birth/death: "+systems[i].name);
            Assert.That(systems[i].isPlaying&&!systems[i].isPaused||naturalEnd[i],Is.True,"Originally playing snow system resumes or completes its proven natural nonloop lifetime: "+systems[i].name);
        }
        Log("PAUSE actualBodies/particles/coverage/stamps/vendorClockFrozen=true everyLiveSystemActualProgress=true");
    }
    [UnityTest,Timeout(240000)]public IEnumerator HeistSavedWeatherTimePauseAndRematches()=>Verify("TruckHoardHeist",false);
    [UnityTest,Timeout(240000)]public IEnumerator EggSavedWeatherTimePauseAndRematches()=>Verify("TruckDragonEggDelivery",false);
    [UnityTest,Timeout(240000)]public IEnumerator EscapeSavedWeatherTimePauseAndRematches()=>Verify("TruckDragonEscape",false);
    [UnityTest,Timeout(180000)]public IEnumerator SnowPauseThenSameStormRematchCannotRestartRetiredSnow()=>Verify("TruckDragonEggDelivery",false,true);
    // Parent runs only after the separate mounted-body prerequisite is accepted;
    // running early fails normally, never reports a skipped acceptance.
    [UnityTest,Timeout(240000)]public IEnumerator RidersSavedWeatherTimePauseAndRematches()=>Verify("TruckDragonRiders",true);
    IEnumerator Verify(string scene,bool airborne,bool transitionOnly=false)
    {
        evidence.Clear();sourceBytes.Clear();authoredTerrains.Clear();authoredLayers.Clear();authoredControls.Clear();output=null;
        sceneName=scene;string scenePath="Assets/"+scene+"/Scenes/"+scene+".unity";Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath),Is.Not.Null,"Explicit saved-scene authoring is a prerequisite; this test never creates it.");
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"EnvironmentNative",scene,DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(output);
        if(airborne)Assert.That(args.Contains("-tsDragonRidersMountAccepted"),Is.True,"Riders environment run requires parent acceptance of the separate real v4 mounted-body fixture, then explicit -tsDragonRidersMountAccepted. Missing prerequisite fails, never counts as skipped weather acceptance.");
        preferences=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"DragonEnvironment"+scene});Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        isolation=new GameObject("Explicit four-dragon-mode environment input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        previousAsync=ShaderUtil.allowAsyncCompilation;asyncCaptured=true;ShaderUtil.allowAsyncCompilation=false;Time.timeScale=1;KeepPath(scenePath);
        Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(GraphicsDeviceType.Null),"Actual Weather Maker/Weatherade rendering requires the configured native graphics device.");
        EditorSceneManager.LoadSceneInPlayMode(scenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Assert.That(match,Is.Not.Null);weather=Get(match,"presentationWeather") as Component;Assert.That(weather,Is.Not.Null);snow=match.GetComponent(Runtime("SportsSnowSurface"));Assert.That(snow,Is.Not.Null);
        KeepAsset(Get(snow,"clearRoadMaterial") as UnityEngine.Object);foreach(DictionaryEntry road in (IDictionary)Get(snow,"runtimeRoads"))KeepAsset(road.Key as UnityEngine.Object);
        surface=Get(snow,"surface");clock=Get(weather,"matchTime");cycle=Get(clock,"cycle");vendor=Get(weather,"vendor");catalog=Get(weather,"presets");views=Get(match,"drivingViews");Assert.That(surface,Is.Not.Null);Assert.That(clock,Is.Not.Null);
        KeepAsset((UnityEngine.Object)catalog);KeepAsset((UnityEngine.Object)Get(cycle,"DayNightProfile"));foreach(string field in new[]{"snowTexture","detailTexture","sparkleTexture"})KeepAsset(Get(snow,field) as UnityEngine.Object);
        foreach(var terrain in Terrain.activeTerrains.Where(item=>item.gameObject.scene==match.gameObject.scene)){authoredTerrains.Add(terrain,terrain.materialTemplate);authoredLayers.Add(terrain,terrain.terrainData.terrainLayers);authoredControls.Add(terrain,terrain.terrainData.alphamapTextures);KeepAsset(terrain.materialTemplate);KeepAsset(terrain.terrainData);}
        var entries=((Array)Get(catalog,"entries")).Cast<object>().Where(item=>item!=null&&Get(item,"profile") is UnityEngine.Object profile&&profile!=null&&!(bool)Get(profile,"Disabled")).ToArray();
        object Find(string label)=>entries.FirstOrDefault(item=>string.Equals((string)Get(item,"displayName"),label,StringComparison.OrdinalIgnoreCase));
        var clear=Find("Clear");var snowfall=Find("Snow");Assert.That(clear,Is.Not.Null,"Actual enabled Clear catalog entry required.");Assert.That(snowfall,Is.Not.Null,"Actual enabled Snow catalog entry required.");
        var storm=Find("Thunderstorm")??Find("Storm")??Find("Hurricane");if(storm==null)Log("STORM_UNAVAILABLE no enabled installed Storm/Thunderstorm/Hurricane entry; storm acceptance not claimed.");
        var pickers=((Component)Get(match,"menus")).GetComponentsInChildren(Runtime("SportsWeatherPicker"),true).Cast<Component>().ToArray();var forecast=pickers.Single(item=>!(bool)Get(item,"timeOfDay"));var time=pickers.Single(item=>(bool)Get(item,"timeOfDay"));
        Get(match,"setup").GetType().GetField("count").SetValue(Get(match,"setup"),4);Call(match,"AllBots");match.GetType().GetField("fixedSeed").SetValue(match,4720);originalBodies=Bodies();
        var cases=new List<(object preset,string time,string label)>{(clear,"Day","clear-day"),(snowfall,"Day","snow-day"),(clear,"Night","clear-night"),(snowfall,"Night","snow-night")};if(storm!=null){cases.Add((storm,"Day","storm-day"));cases.Add((storm,"Night","storm-night"));}cases.Add((clear,"Day","clear-day-restored"));
        if(transitionOnly){Assert.That(storm,Is.Not.Null,"Targeted real retirement regression requires an installed enabled storm profile.");cases=new List<(object preset,string time,string label)>{(snowfall,"Day","snow-day"),(storm,"Day","storm-day"),(storm,"Night","storm-night")};}
        string priorMatch=null;float daySun=0;bool haveDaySun=false;Log("SCOPE actual savedscene fourAI ordinary selectors/rematches; timeScale1, controlledCamera.Render—not Windows-window, human, audible, performance or full-match acceptance. airborneMounted="+airborne+" graphics="+SystemInfo.graphicsDeviceType+" asyncGPU="+SystemInfo.supportsAsyncGPUReadback);
        foreach(var test in cases)
        {
            Log("ORDINARY_TRANSITION target="+test.label+" snowIntensityBefore="+Get(Get(Get(weather,"precipitation"),"SnowScript"),"Intensity"));ParticleStates(test.label+"-before-setup",SnowParticles());
            Call(match,"SetupScreen");var allEntries=(Array)Get(catalog,"entries");int index=Array.IndexOf(allEntries.Cast<object>().ToArray(),test.preset);Call(forecast,"Choose",index+1);Call(time,"Choose",(int)Enum.Parse(Runtime("SportsTimeChoice"),test.time));
            Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});Call(views,"Select",1);int baselineStamps=(int)Get(surface,"WheelTraceStampCount");Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));
            string identity=(string)Get(match,"MatchId");Assert.That(identity,Is.Not.EqualTo(priorMatch));priorMatch=identity;Assert.That(Get(match,"AllAI"),Is.EqualTo(true));
            float until=Time.realtimeSinceStartup+35;string countdown=airborne?"Countdown":"Intro";while((float)Get(match,countdown)>0&&Time.realtimeSinceStartup<until)yield return null;Assert.That((float)Get(match,countdown),Is.Zero,"Ordinary supported/flight start must complete before environment observation.");
            if(airborne){var racers=(Array)Get(match,"racers");for(int slot=0;slot<4;slot++)Assert.That(Get(racers.GetValue(slot),"PhysicallyMounted"),Is.EqualTo(true),"Separate actual mounted-body prerequisite must hold; weather fixture cannot substitute a truck prop.");}
            bool snowy=(bool)Get(test.preset,"Snow"),wet=!snowy&&Get(test.preset,"legacyKind").ToString()!="Clear",outdoorNow=false;int peakContacts=0;float nextRender=0;until=Time.realtimeSinceStartup+(snowy?20:12);
            while(Time.realtimeSinceStartup<until)
            {
                peakContacts=Math.Max(peakContacts,(int)Get(snow,"Contacts"));if(Time.realtimeSinceStartup>=nextRender){RenderActualViews();if(snowy&&!airborne)outdoorNow=ExposedMovingWheel(false);nextRender=Time.realtimeSinceStartup+.12f;}
                bool particles=snowy?ParticleCount(SnowParticles())>0:wet?ParticleCount(RainParticles())>0:ParticleCount(SnowParticles())==0&&ParticleCount(RainParticles())==0;
                bool ready=snowy?(bool)Get(surface,"EnvironmentBindingsReady")&&(airborne||outdoorNow&&(int)Get(surface,"WheelTraceStampCount")>baselineStamps):((float)Get(snow,"Coverage")<.001f&&(!wet||ParticleCount(SnowParticles())==0));
                if(particles&&ready&&Time.realtimeSinceStartup>until-(snowy?18:10))break;yield return null;
            }
            RenderActualViews();Log("ORDINARY_TRANSITION_OBSERVED target="+test.label+" snowIntensity="+Get(Get(Get(weather,"precipitation"),"SnowScript"),"Intensity"));ParticleStates(test.label+"-after-natural-transition-window",SnowParticles());Assert.That(Get(weather,"PresetId"),Is.EqualTo(Get(test.preset,"id")));Assert.That(Get(clock,"Applied").ToString(),Is.EqualTo(test.time));Assert.That((float)Get(cycle,"Speed"),Is.Zero);Assert.That((float)Get(cycle,"NightSpeed"),Is.Zero);Assert.That((float)Get(cycle,"TimeOfDay"),Is.EqualTo((float)Runtime("SportsTimeOfDay").GetMethod("Seconds").Invoke(null,new[]{Enum.Parse(Runtime("SportsTimeChoice"),test.time)})).Within(.01f));
            Assert.That(AssetDatabase.Contains((UnityEngine.Object)Get(cycle,"DayNightProfile")),Is.False);Assert.That(Get(vendor,"LastLocalProfile"),Is.Not.SameAs(Get(test.preset,"profile")));ViewOwnership(false);
            float sun=SunAngle(Cameras()[0]);if(test.label=="clear-day"){daySun=sun;haveDaySun=true;}if(test.label=="clear-night"){Assert.That(haveDaySun,Is.True);Assert.That(Mathf.Abs(Mathf.DeltaAngle(daySun,sun)),Is.GreaterThan(45),"Real vendor celestial orientation responds to selected night, after actual rendering.");}
            if(snowy)
            {
                Assert.That((float)Get(Get(Get(weather,"precipitation"),"SnowScript"),"Intensity"),Is.GreaterThan(.1f));Assert.That(ParticleCount(SnowParticles()),Is.GreaterThan(0));ActualPrecipitationRenderer(SnowParticles(),"snow");Assert.That(Get(snow,"Falling"),Is.EqualTo(true));Assert.That((float)Get(snow,"Coverage"),Is.GreaterThan(.5f));Assert.That((float)Get(surface,"Coverage01"),Is.GreaterThan(.5f));Assert.That(Get(surface,"EnvironmentBindingsReady"),Is.EqualTo(true));Assert.That((int)Get(surface,"TerrainSnowSurfaceCount"),Is.GreaterThan(0));Assert.That((int)Get(surface,"GroundSnowSurfaceCount"),Is.GreaterThan(0));Assert.That(Get(surface,"SnowTextureReady"),Is.EqualTo(true));Assert.That((int)Get(surface,"BoundDepthCameraCount"),Is.GreaterThan(0));
                Assert.That(Terrain.activeTerrains.Where(terrain=>terrain.gameObject.scene==match.gameObject.scene).Any(terrain=>terrain.materialTemplate!=null&&terrain.materialTemplate.shader!=null&&terrain.materialTemplate.shader.name=="NOT_Lonely/Weatherade/Snow Coverage (Terrain)"&&terrain.materialTemplate.GetFloat("_CoverageAmount")>.5f&&terrain.materialTemplate.GetTexture("_CoverageTex0")!=null),Is.True,"Actual terrain has the installed vendor snow shader, applied coverage and real texture; metadata alone is insufficient.");
                if(test.label=="snow-day")RecordSnowVisibility();
                if(!airborne)
                {
                    bool outdoor=ExposedMovingWheel(true);
                    if(outdoor)
                    {
                        Assert.That(peakContacts,Is.GreaterThan(0));Assert.That((int)Get(surface,"WheelTraceStampCount"),Is.GreaterThan(baselineStamps),"This rematch needs new actual contact stamps, not an old counter.");Assert.That(SystemInfo.supportsAsyncGPUReadback,Is.True);
                        var coverage=(Component)Get(surface,"coverage");var depth=(Camera)Get(coverage,"sceneDepthCam");
                        Log("GPU_WITNESS activeTracers="+Get(surface,"ActiveWheelTraceSourceCount")+" adapter="+Get(surface,"VisualDiagnostic")+" depthPosition="+depth.transform.position+" depthSpan="+(depth.orthographicSize*2)+" depthNear="+depth.nearClipPlane+" depthFar="+depth.farClipPlane+" follow="+Get(coverage,"followTarget"));
                        Capture(test.label+"-exposed-wheel-before-gpu");
                        yield return ActualMovingWheelGpu();
                    }
                    else Log("OUTDOOR_TRACK_UNVERIFIED no currently moving wheel satisfies the product's supported exposed-contact predicate within the bounded ordinary AI observation; indoors is not forced snowy. Falling snow/surfaces/pause continue; outdoor tire-track acceptance is not claimed for this case.");
                }
                else Log("AIRBORNE_SCOPE actual falling snow/terrain/environment verified; no grounded NWH tire-track acceptance for mounted flight.");
            }
            else {Assert.That((float)Get(snow,"Coverage"),Is.LessThan(.001f));Assert.That((float)Get(surface,"Coverage01"),Is.LessThan(.001f));Assert.That(ParticleCount(SnowParticles()),Is.Zero,"Prior flakes retire through actual profile transition.");ActualNoSnowTerrains(test.label);if(wet){Assert.That((float)Get(Get(Get(weather,"precipitation"),"RainScript"),"Intensity"),Is.GreaterThan(.1f));Assert.That(ParticleCount(RainParticles()),Is.GreaterThan(0));ActualPrecipitationRenderer(RainParticles(),"storm-rain");}else Assert.That(ParticleCount(RainParticles()),Is.Zero);}
            ActualRoadSourceAppearance(test.label,snowy);
            Capture(test.label+"-one-view");int revision=(int)Get(clock,"Revision");var activeProfile=Get(vendor,"LastLocalProfile");Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});Call(views,"Select",1);yield return null;yield return null;RenderActualViews();ViewOwnership(true);Capture(test.label+"-four-views");
            Assert.That(Get(match,"MatchId"),Is.EqualTo(identity));Assert.That(Get(clock,"Revision"),Is.EqualTo(revision));Assert.That(Get(vendor,"LastLocalProfile"),Is.SameAs(activeProfile));if(test.label=="snow-day")yield return PauseActualWeather();
            Log("CASE "+test.label+" preset="+Get(weather,"PresetId")+" match="+identity+" sun="+sun+" snowParticles="+ParticleCount(SnowParticles())+" rainParticles="+ParticleCount(RainParticles())+" snowCoverage="+Get(snow,"Coverage")+" terrainBindings="+Get(surface,"TerrainSnowSurfaceCount")+" contacts="+peakContacts+" NEWstamps="+((int)Get(surface,"WheelTraceStampCount")-baselineStamps)+" GPU="+Get(surface,"TracePixelDiagnostic")+" wind="+Get(Get(weather,"wind"),"CurrentWindVelocity"));SourcesExact();
        }
        Log("PASSED_SCOPE selected ordinary rematches; all4AI originalbodies/sourcebytes, one/fourview sharedvendor, actualsnow/rain/celestial/render/pause. Outdoor tracks require GPU_WITNESS; OUTDOOR_TRACK_UNVERIFIED explicitly retains that missing scope. Humanvisual and WindowsGPU/window acceptance remain separate.");
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {
        if(match!=null)Call(match,"SetupScreen");Time.timeScale=1;if(asyncCaptured){ShaderUtil.allowAsyncCompilation=previousAsync;asyncCaptured=false;}
        var previous=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Dragon environment cleanup "+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(empty);if(previous.IsValid()&&previous.isLoaded)yield return SceneManager.UnloadSceneAsync(previous);yield return null;
        if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);preferences?.Dispose();preferences=null;Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);match=null;SourcesExact();
    }
}
#endif
