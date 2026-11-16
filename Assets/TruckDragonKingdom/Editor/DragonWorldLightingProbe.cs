#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Explicit read-only diagnosis. Captures are comparisons, never acceptance
    // of an artificially relit world. No assets/scenes are saved or generated.
    public static class DragonWorldLightingProbe
    {
        const string Preview="Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity";
        const string World="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";
        const string Definition="Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset";
        const int Width=1280,Height=720;
        [Serializable] sealed class Receipt
        {
            public string scope="READ_ONLY_SAME_CAMERA_LIGHTING_DIAGNOSIS_NOT_GAMEPLAY_WINDOWS_OR_VISUAL_ACCEPTANCE",unityVersion,preview,qualityPipeline,graphicsPipeline,activePipeline,error;
            public Vector3 cameraPosition,cameraTarget;public int width=Width,height=Height,lightmapCount;public Lighting originalLighting;
            public TerrainEvidence[] terrains;public MaterialEvidence[] roadMaterials;public LightEvidence[] lights;public CaptureEvidence[] comparisons;
            public string[] preservedPaths;public bool savedBytesPreserved,sceneSetupRestored,renderSettingsRestored;
        }
        [Serializable] sealed class Lighting
        {
            public string ambientMode,skybox,sun;public Color ambientSky,ambientEquator,ambientGround,ambientLight;public float ambientIntensity,reflectionIntensity;public float[] ambientSH;
        }
        [Serializable] sealed class TerrainEvidence
        {
            public string name,dataPath;public Vector3 position,size;public bool drawInstanced;public float basemapDistance;
            public int alphaWidth,alphaHeight,alphaLayers,weightSamples,nonFinite,zeroSumCells;public float weightMinimum,weightMaximum,sumMinimum,sumMaximum,maximumSumError;public float[] channelMinimum,channelMaximum,channelAverage;
            public LayerEvidence[] layers;public MaterialEvidence material;public TextureEvidence[] alphaTextures;
        }
        [Serializable] sealed class LayerEvidence
        {public string name,path;public TextureEvidence diffuse,normal,mask;public Vector2 tileSize,tileOffset;public float normalScale,metallic,smoothness;public Vector4 diffuseRemapMin,diffuseRemapMax,maskRemapMin,maskRemapMax;}
        [Serializable] sealed class TextureEvidence
        {public string name,path,format,wrap,filter;public int width,height;public bool linear;public float sampledMinimumLuminance,sampledMaximumLuminance,sampledAverageLuminance,sampledAverageAlpha;}
        [Serializable] sealed class MaterialEvidence
        {public string name,path,shader,shaderPath;public bool supported;public string[] keywords,compilerErrors;public TextureBinding[] textures;public string[] importantValues;}
        [Serializable] sealed class TextureBinding
        {public string property;public TextureEvidence texture;public Vector2 scale,offset;}
        [Serializable] sealed class LightEvidence
        {public string name,type,shadows,mode;public bool enabled;public float intensity,shadowStrength;public Color color;public Vector3 position,forward;}
        [Serializable] sealed class CaptureEvidence
        {public string id,png,intervention;public Lighting lighting;public PixelEvidence terrain,road,other;}
        [Serializable] sealed class PixelEvidence
        {public int physicalSamples,blackSamples;public float minimum=1,maximum,average,blackFraction;}
        sealed class RenderSnapshot
        {
            readonly AmbientMode mode=RenderSettings.ambientMode;readonly Color sky=RenderSettings.ambientSkyColor,equator=RenderSettings.ambientEquatorColor,ground=RenderSettings.ambientGroundColor,ambient=RenderSettings.ambientLight;
            readonly float intensity=RenderSettings.ambientIntensity,reflection=RenderSettings.reflectionIntensity;readonly SphericalHarmonicsL2 probe=RenderSettings.ambientProbe;readonly Material skybox=RenderSettings.skybox;readonly Light sun=RenderSettings.sun;
            public void Restore(){RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;RenderSettings.ambientLight=ambient;RenderSettings.ambientIntensity=intensity;RenderSettings.reflectionIntensity=reflection;RenderSettings.skybox=skybox;RenderSettings.sun=sun;RenderSettings.ambientProbe=probe;}
        }
        static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public).GetValue(value);
        public static void CaptureVillageLighting()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for read-only lighting diagnosis.");
            foreach(string path in new[]{Preview,World,Definition})if(!File.Exists(path))throw new InvalidOperationException("Existing accepted world required; no generation by this probe: "+path);
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).path==Preview)throw new InvalidOperationException("Close the preview before this isolated diagnostic; an already open original preview is never reused, discarded or saved.");
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit -tsDragonOutput evidence directory required.");
            string output=Path.GetFullPath(Path.Combine(args[at+1],"VillageLighting"));string assets=Path.GetFullPath("Assets")+Path.DirectorySeparatorChar;if(output.StartsWith(assets,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Evidence must be outside Assets; this probe never creates saved assets.");Directory.CreateDirectory(output);
            var receipt=new Receipt{unityVersion=Application.unityVersion,preview=Preview,qualityPipeline=AssetPath(QualitySettings.renderPipeline),graphicsPipeline=AssetPath(GraphicsSettings.defaultRenderPipeline),activePipeline=AssetPath(GraphicsSettings.currentRenderPipeline)};
            var beforeScene=SceneManager.GetActiveScene();var beforeRender=new RenderSnapshot();var beforeLighting=JsonUtility.ToJson(ReadLighting());var beforeRoots=new Dictionary<GameObject,bool>();
            for(int i=0;i<SceneManager.sceneCount;i++)foreach(var root in SceneManager.GetSceneAt(i).GetRootGameObjects())beforeRoots[root]=root.activeSelf;
            var byteHashes=new Dictionary<string,string>(StringComparer.Ordinal);foreach(string path in new[]{Preview,World,Definition})Watch(path,byteHashes);
            Scene scene=default;GameObject cameraObject=null;Terrain[] terrains=Array.Empty<Terrain>();Material[] terrainMaterials=Array.Empty<Material>();Light[] lights=Array.Empty<Light>();LightShadows[] shadows=Array.Empty<LightShadows>();var temporaryMaterials=new List<Material>();RenderSnapshot worldRender=null;ShadowQuality shadowQuality=QualitySettings.shadows;
            try
            {
                // Isolate the actual saved scene without discarding/saving any
                // prior user/runner scene. Original active flags are restored.
                foreach(var pair in beforeRoots)if(pair.Value)pair.Key.SetActive(false);
                scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);worldRender=new RenderSnapshot();receipt.originalLighting=ReadLighting();receipt.lightmapCount=LightmapSettings.lightmaps.Length;
                var runtime=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);var world=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(runtime,true)).Cast<Component>().Single();
                var objects=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).ToArray();terrains=objects.SelectMany(value=>value.GetComponents<Terrain>()).ToArray();terrainMaterials=terrains.Select(value=>value.materialTemplate).ToArray();
                lights=objects.SelectMany(value=>value.GetComponents<Light>()).ToArray();shadows=lights.Select(value=>value.shadows).ToArray();receipt.lights=lights.Select(value=>new LightEvidence{name=value.name,type=value.type.ToString(),shadows=value.shadows.ToString(),mode=value.lightmapBakeType.ToString(),enabled=value.enabled&&value.gameObject.activeInHierarchy,intensity=value.intensity,color=value.color,position=value.transform.position,forward=value.transform.forward,shadowStrength=value.shadowStrength}).ToArray();
                var renderers=objects.SelectMany(value=>value.GetComponents<Renderer>()).ToArray();foreach(var material in renderers.SelectMany(value=>value.sharedMaterials).Concat(terrainMaterials).Where(value=>value!=null).Distinct())Watch(AssetDatabase.GetAssetPath(material),byteHashes);
                foreach(var terrain in terrains){Watch(AssetDatabase.GetAssetPath(terrain.terrainData),byteHashes);foreach(var layer in terrain.terrainData.terrainLayers)if(layer!=null)Watch(AssetDatabase.GetAssetPath(layer),byteHashes);}
                receipt.terrains=terrains.Select(InspectTerrain).ToArray();
                var roads=objects.SelectMany(value=>value.GetComponents<MeshCollider>()).Where(value=>value.transform.parent!=null&&value.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)).ToArray();
                receipt.roadMaterials=roads.SelectMany(value=>value.GetComponents<Renderer>()).SelectMany(value=>value.sharedMaterials).Where(value=>value!=null).Distinct().Select(InspectMaterial).ToArray();
                var region=((Array)Field(world,"regions")).Cast<object>().Single(value=>(string)Field(value,"id")=="village");var root=(Transform)Field(region,"root");var art=root.GetComponentsInChildren<Renderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy&&!(value is ParticleSystemRenderer)&&value.bounds.size.sqrMagnitude>.01f).ToArray();
                if(art.Length==0)throw new InvalidOperationException("Actual village art required.");var bounds=art[0].bounds;foreach(var renderer in art.Skip(1))bounds.Encapsulate(renderer.bounds);
                var graph=Field(Field(world,"definition"),"graph");var desired=root.position+new Vector3(0,0,-90);Vector3 roadEye=root.position;float nearest=float.PositiveInfinity;
                foreach(var edge in (IEnumerable)Field(graph,"edges"))foreach(var sample in (IEnumerable)Field(edge,"samples")){var point=(Vector3)Field(sample,"position");float distance=Vector3.ProjectOnPlane(point-desired,Vector3.up).sqrMagnitude;if(distance<nearest){nearest=distance;roadEye=point;}}
                if(nearest>300*300)throw new InvalidOperationException("Actual village road camera site required.");receipt.cameraPosition=roadEye+Vector3.up*6;receipt.cameraTarget=Vector3.Lerp(root.position+Vector3.up*10,bounds.center,.35f);
                cameraObject=new GameObject("Read-only actual village lighting diagnostic camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=58;camera.aspect=(float)Width/Height;camera.cullingMask=~0;camera.clearFlags=CameraClearFlags.Skybox;camera.allowHDR=true;camera.transform.SetPositionAndRotation(receipt.cameraPosition,Quaternion.LookRotation(receipt.cameraTarget-receipt.cameraPosition));Physics.SyncTransforms();
                var captures=new List<CaptureEvidence>();
                captures.Add(Capture(camera,output,"01-original","Saved scene lighting, original terrain/road materials, original real shadows."));
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.65f,.70f,.80f);RenderSettings.ambientEquatorColor=new Color(.40f,.45f,.50f);RenderSettings.ambientGroundColor=new Color(.18f,.20f,.22f);RenderSettings.ambientIntensity=1;
                var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.40f,.45f,.50f).linear);sh.AddDirectionalLight(Vector3.up,new Color(.65f,.70f,.80f).linear,1);sh.AddDirectionalLight(Vector3.down,new Color(.18f,.20f,.22f).linear,.5f);RenderSettings.ambientProbe=sh;
                captures.Add(Capture(camera,output,"02-controlled-ambient","Only ambient mode/colors/intensity and explicit SH changed. Original sun/shadows/materials/alpha weights retained."));worldRender.Restore();
                QualitySettings.shadows=ShadowQuality.Disable;foreach(var light in lights)light.shadows=LightShadows.None;
                captures.Add(Capture(camera,output,"03-no-realtime-shadows","Only global realtime shadow quality and actual light shadow settings disabled. Original lighting/materials/alpha weights retained."));QualitySettings.shadows=shadowQuality;for(int i=0;i<lights.Length;i++)lights[i].shadows=shadows[i];worldRender.Restore();
                var shader=Shader.Find("Universal Render Pipeline/Terrain/Lit");if(shader==null||!shader.isSupported||ShaderUtil.GetShaderMessages(shader).Any(value=>value.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new InvalidOperationException("Installed supported error-free URP Terrain/Lit required for the material-only comparison.");
                foreach(var terrain in terrains)
                {
                    var material=new Material(shader){name="Temporary verified URP terrain diagnostic",hideFlags=HideFlags.HideAndDontSave,enableInstancing=terrain.drawInstanced};
                    if(!material.HasProperty("_Control")||!material.HasProperty("_Splat0"))throw new InvalidOperationException("Actual URP terrain engine texture contract absent.");
                    material.SetFloat("_EnableHeightBlend",0);material.DisableKeyword("_TERRAIN_BLEND_HEIGHT");material.SetFloat("_EnableInstancedPerPixelNormal",terrain.drawInstanced?1:0);
                    if(terrain.drawInstanced)material.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");if(terrain.terrainData.terrainLayers.Any(layer=>layer!=null&&layer.normalMapTexture!=null))material.EnableKeyword("_NORMALMAP");if(terrain.terrainData.terrainLayers.Any(layer=>layer!=null&&layer.maskMapTexture!=null))material.EnableKeyword("_MASKMAP");temporaryMaterials.Add(material);terrain.materialTemplate=material;
                }
                captures.Add(Capture(camera,output,"04-fresh-urp-terrain-material","Only terrain material references replaced by temporary verified URP Terrain/Lit defaults/actual layer keywords. Original TerrainData/alpha/layers, lighting and all road materials retained."));receipt.comparisons=captures.ToArray();
            }
            catch(Exception exception){receipt.error=exception.ToString();throw;}
            finally
            {
                for(int i=0;i<terrains.Length;i++)if(terrains[i]!=null)terrains[i].materialTemplate=terrainMaterials[i];for(int i=0;i<lights.Length;i++)if(lights[i]!=null)lights[i].shadows=shadows[i];QualitySettings.shadows=shadowQuality;worldRender?.Restore();
                if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);foreach(var material in temporaryMaterials)UnityEngine.Object.DestroyImmediate(material);
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(beforeScene.IsValid()&&beforeScene.isLoaded)SceneManager.SetActiveScene(beforeScene);foreach(var pair in beforeRoots)if(pair.Key!=null)pair.Key.SetActive(pair.Value);beforeRender.Restore();
                receipt.sceneSetupRestored=SceneManager.GetActiveScene()==beforeScene&&beforeRoots.All(pair=>pair.Key!=null&&pair.Key.activeSelf==pair.Value);receipt.renderSettingsRestored=JsonUtility.ToJson(ReadLighting())==beforeLighting;receipt.preservedPaths=byteHashes.Keys.ToArray();receipt.savedBytesPreserved=byteHashes.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);
                File.WriteAllText(Path.Combine(output,"village-lighting-probe.json"),JsonUtility.ToJson(receipt,true));
                if(!receipt.savedBytesPreserved||!receipt.sceneSetupRestored||!receipt.renderSettingsRestored)throw new InvalidOperationException("Read-only lighting probe restoration/byte guard failed. Inspect retained JSON; no saved asset repair attempted.");
            }
            Debug.Log("DRAGON_VILLAGE_LIGHTING_DIAGNOSIS comparisons4 output="+output+" savedBytesPreserved="+receipt.savedBytesPreserved+". Interpret actual images/weights before any production repair.");
        }
        static TerrainEvidence InspectTerrain(Terrain terrain)
        {
            var data=terrain.terrainData;if(data==null)throw new InvalidOperationException("Actual TerrainData absent.");var weights=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);int channels=weights.GetLength(2);
            var record=new TerrainEvidence{name=terrain.name,dataPath=AssetDatabase.GetAssetPath(data),position=terrain.transform.position,size=data.size,drawInstanced=terrain.drawInstanced,basemapDistance=terrain.basemapDistance,alphaWidth=data.alphamapWidth,alphaHeight=data.alphamapHeight,alphaLayers=channels,channelMinimum=Enumerable.Repeat(float.PositiveInfinity,channels).ToArray(),channelMaximum=Enumerable.Repeat(float.NegativeInfinity,channels).ToArray(),channelAverage=new float[channels],weightMinimum=float.PositiveInfinity,weightMaximum=float.NegativeInfinity,sumMinimum=float.PositiveInfinity,sumMaximum=float.NegativeInfinity,material=InspectMaterial(terrain.materialTemplate),alphaTextures=data.alphamapTextures.Select(value=>InspectTexture(value,false)).ToArray()};
            for(int y=0;y<weights.GetLength(0);y++)for(int x=0;x<weights.GetLength(1);x++)
            {
                float sum=0;for(int c=0;c<channels;c++){float value=weights[y,x,c];if(!float.IsFinite(value)){record.nonFinite++;continue;}record.weightSamples++;record.weightMinimum=Mathf.Min(record.weightMinimum,value);record.weightMaximum=Mathf.Max(record.weightMaximum,value);record.channelMinimum[c]=Mathf.Min(record.channelMinimum[c],value);record.channelMaximum[c]=Mathf.Max(record.channelMaximum[c],value);record.channelAverage[c]+=value;sum+=value;}record.sumMinimum=Mathf.Min(record.sumMinimum,sum);record.sumMaximum=Mathf.Max(record.sumMaximum,sum);record.maximumSumError=Mathf.Max(record.maximumSumError,Mathf.Abs(1-sum));if(sum<.000001f)record.zeroSumCells++;
            }
            for(int i=0;i<channels;i++)record.channelAverage[i]/=weights.GetLength(0)*weights.GetLength(1);
            record.layers=data.terrainLayers.Select(layer=>layer==null?new LayerEvidence{name="MISSING"}:new LayerEvidence{name=layer.name,path=AssetDatabase.GetAssetPath(layer),diffuse=InspectTexture(layer.diffuseTexture,true),normal=InspectTexture(layer.normalMapTexture,false),mask=InspectTexture(layer.maskMapTexture,false),tileSize=layer.tileSize,tileOffset=layer.tileOffset,normalScale=layer.normalScale,metallic=layer.metallic,smoothness=layer.smoothness,diffuseRemapMin=layer.diffuseRemapMin,diffuseRemapMax=layer.diffuseRemapMax,maskRemapMin=layer.maskMapRemapMin,maskRemapMax=layer.maskMapRemapMax}).ToArray();return record;
        }
        static MaterialEvidence InspectMaterial(Material material)
        {
            if(material==null)return new MaterialEvidence{name="MISSING"};var shader=material.shader;var textures=new List<TextureBinding>();if(shader!=null)for(int i=0;i<shader.GetPropertyCount();i++)if(shader.GetPropertyType(i)==ShaderPropertyType.Texture){string property=shader.GetPropertyName(i);textures.Add(new TextureBinding{property=property,texture=InspectTexture(material.GetTexture(property),property=="_BaseMap"||property=="_MainTex"),scale=material.GetTextureScale(property),offset=material.GetTextureOffset(property)});}
            return new MaterialEvidence{name=material.name,path=AssetDatabase.GetAssetPath(material),shader=shader!=null?shader.name:"MISSING",shaderPath=AssetDatabase.GetAssetPath(shader),supported=shader!=null&&shader.isSupported,keywords=material.enabledKeywords.Select(value=>value.name).ToArray(),compilerErrors=shader==null?Array.Empty<string>():ShaderUtil.GetShaderMessages(shader).Where(value=>value.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(value=>value.message).ToArray(),textures=textures.ToArray(),importantValues=new[]{"_BaseColor","_Color","_EnableHeightBlend","_HeightTransition","_EnableInstancedPerPixelNormal","_Smoothness","_Metallic","_BumpScale"}.Where(material.HasProperty).Select(property=>property+"="+(property.EndsWith("Color",StringComparison.Ordinal)?material.GetColor(property).ToString():material.GetFloat(property).ToString("R",System.Globalization.CultureInfo.InvariantCulture))).ToArray()};
        }
        static TextureEvidence InspectTexture(Texture texture,bool sample)
        {
            if(texture==null)return null;var record=new TextureEvidence{name=texture.name,path=AssetDatabase.GetAssetPath(texture),width=texture.width,height=texture.height,format=texture.graphicsFormat.ToString(),wrap=texture.wrapMode.ToString(),filter=texture.filterMode.ToString(),linear=texture.isDataSRGB==false};if(!sample)return record;
            var target=RenderTexture.GetTemporary(32,32,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var prior=RenderTexture.active;var readable=new Texture2D(32,32,TextureFormat.RGBA32,false,true);
            try{Graphics.Blit(texture,target);RenderTexture.active=target;readable.ReadPixels(new Rect(0,0,32,32),0,0);readable.Apply();record.sampledMinimumLuminance=1;foreach(var pixel in readable.GetPixels()){float l=Luminance(pixel);record.sampledMinimumLuminance=Mathf.Min(record.sampledMinimumLuminance,l);record.sampledMaximumLuminance=Mathf.Max(record.sampledMaximumLuminance,l);record.sampledAverageLuminance+=l/1024;record.sampledAverageAlpha+=pixel.a/1024;}}
            finally{RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(readable);}return record;
        }
        static CaptureEvidence Capture(Camera camera,string output,string id,string intervention)
        {
            var target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(Width,Height,TextureFormat.RGB24,false);var prior=RenderTexture.active;var record=new CaptureEvidence{id=id,png=Path.Combine(output,id+".png"),intervention=intervention,lighting=ReadLighting(),terrain=new PixelEvidence(),road=new PixelEvidence(),other=new PixelEvidence()};
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,Width,Height),0,0);texture.Apply();var pixels=texture.GetPixels32();File.WriteAllBytes(record.png,texture.EncodeToPNG());
                for(int y=4;y<Height;y+=8)for(int x=4;x<Width;x+=8)
                {
                    var ray=camera.ViewportPointToRay(new Vector3((x+.5f)/Width,(y+.5f)/Height,0));if(!Physics.Raycast(ray,out var hit,6500,~0,QueryTriggerInteraction.Ignore))continue;
                    var category=hit.collider is TerrainCollider?record.terrain:hit.collider.transform.parent!=null&&hit.collider.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)?record.road:record.other;float l=Luminance(pixels[y*Width+x]);category.physicalSamples++;category.minimum=Mathf.Min(category.minimum,l);category.maximum=Mathf.Max(category.maximum,l);category.average+=l;if(l<.03f)category.blackSamples++;
                }
                foreach(var stats in new[]{record.terrain,record.road,record.other})if(stats.physicalSamples>0){stats.average/=stats.physicalSamples;stats.blackFraction=stats.blackSamples/(float)stats.physicalSamples;}
            }
            finally{camera.targetTexture=null;RenderTexture.active=prior;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);}return record;
        }
        static Lighting ReadLighting()
        {
            var sh=RenderSettings.ambientProbe;var values=new float[27];for(int c=0;c<3;c++)for(int i=0;i<9;i++)values[c*9+i]=sh[c,i];return new Lighting{ambientMode=RenderSettings.ambientMode.ToString(),skybox=AssetPath(RenderSettings.skybox),sun=RenderSettings.sun!=null?RenderSettings.sun.name:"none",ambientSky=RenderSettings.ambientSkyColor,ambientEquator=RenderSettings.ambientEquatorColor,ambientGround=RenderSettings.ambientGroundColor,ambientLight=RenderSettings.ambientLight,ambientIntensity=RenderSettings.ambientIntensity,reflectionIntensity=RenderSettings.reflectionIntensity,ambientSH=values};
        }
        static float Luminance(Color value)=>value.r*.2126f+value.g*.7152f+value.b*.0722f;
        static string AssetPath(UnityEngine.Object value)=>value==null?"none":AssetDatabase.GetAssetPath(value)+" | "+value.name+" | "+value.GetType().FullName;
        static void Watch(string path,Dictionary<string,string> hashes){if(string.IsNullOrEmpty(path)||!File.Exists(path))return;hashes[path]=Hash(path);if(File.Exists(path+".meta"))hashes[path+".meta"]=Hash(path+".meta");}
        static string Hash(string path){using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(File.ReadAllBytes(path)).Select(value=>value.ToString("x2")));}
    }
}
#endif
