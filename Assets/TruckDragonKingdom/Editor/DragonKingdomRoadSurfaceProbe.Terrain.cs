#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable]public sealed class TerrainSurfaceFrame
        {public string view,intervention,png,pixelSha256,terrainPixelSha256;public int terrainSamples,channelsDifferentFromBaseline,maximumChannelDeltaFromBaseline;public float meanTerrainLuminance,meanChannelDeltaFromBaseline,publicNormalOption;public bool normalKeywordEnabled;public Vector2[] tileSizes;public float[] normalScales;public string[] shaderErrors;public bool samplerOnly,alphaMapsExact;public string shader;}
        [Serializable]public sealed class TerrainSurfaceReceipt
        {
            public string scope="ACTUAL_SAVED_TERRAIN_ONLY_SAME_CAMERA_PUBLIC_LAYER_UV_NORMAL_ABA_NON_TERRAIN_ART_MASKED_NO_ADOPTION",preview,status="IN_PROGRESS",error;
            public int terrains,colliderTerrainBindings,maskedNonTerrainRenderers;public MaskedTerrainArt[] maskedArt;public TerrainSamplerState[] samplerStates;public string globalAnisotropicFiltering;public TerrainPaletteSource[] paletteSources;public bool sourceBytesPreserved,physicsAndHeightsExact,originalTerrainDataRestored,colliderBindingsUnchanged,baselinePixelsRestored,nonTerrainRendererStatesRestored,materialBindingsRestored,publicNormalBindingVerified,textureSamplerStatesRestored;
            public TerrainSurfaceFrame[] frames;
        }
        [Serializable]public sealed class TerrainPaletteSource{public string path,guid,sha256,metaSha256;}
        static string TerrainAlphaHash(TerrainData data){var values=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var bytes=new byte[values.Length*sizeof(float)];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);return Hash(bytes);}
        [Serializable]public sealed class TerrainSamplerState
        {public string source,guid,sourceImageHash,configuredImageHash;public int width,height,mips,sourceAniso,configuredAniso;public string sourceFilter,configuredFilter;public bool identicalContent,restoredSampler;[NonSerialized]public Texture2D texture;[NonSerialized]public FilterMode originalFilter;[NonSerialized]public bool originallyDirty;}
        static void RestoreTerrainSampler(TerrainSamplerState state)
        {state.texture.filterMode=state.originalFilter;state.texture.anisoLevel=state.sourceAniso;if(!state.originallyDirty&&EditorUtility.IsDirty(state.texture))EditorUtility.ClearDirty(state.texture);state.restoredSampler=state.texture.filterMode==state.originalFilter&&state.texture.anisoLevel==state.sourceAniso&&state.texture.imageContentsHash.ToString()==state.sourceImageHash&&EditorUtility.IsDirty(state.texture)==state.originallyDirty;}
        [Serializable]public sealed class MaskedTerrainArt{public string name,globalId,sourceMesh;public bool originallyEnabled;}
        static string SourceTerrainMaskMesh(Renderer renderer)
        {var filter=renderer.GetComponent<MeshFilter>();return filter==null||filter.sharedMesh==null?null:AssetDatabase.GetAssetPath(filter.sharedMesh);}
        static byte[] TerrainPixelData(Camera camera,DragonKingdomWorld world,HashSet<Collider> terrains,string png)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);try{if(!texture.LoadImage(File.ReadAllBytes(png)))throw new InvalidOperationException("Actual captured PNG decode failed.");var pixels=texture.GetPixels32();var samples=new List<byte>();for(int y=4;y<Height;y+=8)for(int x=4;x<Width;x+=8){var hits=Physics.RaycastAll(camera.ScreenPointToRay(new Vector3(x,y,0)),6500,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==world.gameObject.scene).OrderBy(h=>h.distance).ToArray();if(hits.Length==0||!terrains.Contains(hits[0].collider))continue;var color=pixels[y*Width+x];samples.Add(color.r);samples.Add(color.g);samples.Add(color.b);}return samples.ToArray();}finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
        public static TerrainSurfaceReceipt LastTerrainSurfaceReceipt{get;private set;}
        public static void CaptureActualTerrainSurfaceComparison()=>CaptureActualTerrainSurfaceComparison(false,false);
        public static void CaptureActualTerrainPublicNormalComparison()=>CaptureActualTerrainSurfaceComparison(true,false);
        public static void CaptureActualAuthorTerrainPaletteComparison()=>CaptureActualTerrainSurfaceComparison(false,true);
        static void CaptureActualTerrainSurfaceComparison(bool normalMode,bool paletteMode)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");
            if(at<0||at+1>=args.Length||args[at+1].Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new InvalidOperationException("Exact immutable retained candidate required.");
            string preview=Root+"VisualCandidates/"+args[at+1]+"/DragonKingdomPreview.unity";
            if(!File.Exists(IoPath(preview)))throw new InvalidOperationException("Actual retained world required.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty||string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))throw new InvalidOperationException("Preserve user edits before isolated read-only diagnosis.");
            // Public native import preflight deliberately precedes the immutable source snapshot.
            DragonKingdomRegionalWorldIntegration.CaptureArtistSourcePreflight();
            string art=Root+"PrivateLicensedEnvironment/Plugins/";var palettePaths=new[]{art+"Tidal Flask Studios/Fantastic Village Pack/2d/textures/terrain_layers/Layer_grass_01.terrainlayer",art+"Polyart Studio/HandpaintedEnvironmentVol1/Terrain/GrassGreen.terrainlayer",art+"Polyart Studio/HandpaintedEnvironmentVol1/Terrain/GrassYellow.terrainlayer",art+"Polyart Studio/HandpaintedEnvironmentVol1/Terrain/Dirt.terrainlayer",art+"Polyart Studio/HandpaintedEnvironmentVol1/Terrain/Rock.terrainlayer"};
            var paletteDependencies=paletteMode?palettePaths.SelectMany(p=>AssetDatabase.GetDependencies(p,true)):Array.Empty<string>();
            var files=AssetDatabase.GetDependencies(preview,true).Concat(paletteDependencies).Concat(new[]{preview}).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(IoPath(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(IoPath(p))));
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Camera camera=null;DragonKingdomWorld world=null;string physics="";string[] heights=null;
            var original=new Dictionary<Terrain,TerrainData>();var colliders=new Dictionary<TerrainCollider,TerrainData>();var temporary=new List<UnityEngine.Object>();var masked=new Dictionary<Renderer,bool>();var templates=new Dictionary<Terrain,Material>();var materialCopies=new Dictionary<Material,Material>();var samplerStates=new Dictionary<Texture2D,TerrainSamplerState>();bool async=ShaderUtil.allowAsyncCompilation;
            var receipt=new TerrainSurfaceReceipt{preview=preview,globalAnisotropicFiltering=QualitySettings.anisotropicFiltering.ToString()};LastTerrainSurfaceReceipt=receipt;if(paletteMode)receipt.scope="ACTUAL_SOURCE_AUTHOR_TERRAIN_PALETTES_SAME_HEIGHT_ALPHA_DIGGER_SHADER_ABA_PLUS_STANDARD_URP_REFERENCE_ONLY";else if(normalMode)receipt.scope="ACTUAL_SAVED_TERRAIN_ONLY_EXISTING_DIGGER_PUBLIC_INSTANCED_NORMAL_OPTION_ABA_NO_SHADER_EDIT_OR_ADOPTION";string output=Path.Combine(Output(),paletteMode?"ArtistTerrainPalette":normalMode?"TerrainPublicNormal":"TerrainSurface");Directory.CreateDirectory(output);
            try
            {
                ShaderUtil.allowAsyncCompilation=false;scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);
                foreach(var t in world.GetComponentsInChildren<Terrain>(true)){original.Add(t,t.terrainData);templates.Add(t,t.materialTemplate);}
                if(normalMode||paletteMode)
                {
                    foreach(var source in templates.Values.Distinct())
                    {if(source==null||source.shader.name!="Digger/Terrain/URP/Lit"||!source.HasProperty("_EnableInstancedPerPixelNormal")||!source.shader.keywordSpace.FindKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL").isValid)throw new InvalidOperationException("Actual existing public Digger shader option/property/local keyword required; no invented configuration.");var material=new Material(source){hideFlags=HideFlags.HideAndDontSave};if(paletteMode){var reference=Shader.Find("Universal Render Pipeline/Terrain/Lit");if(reference==null||!reference.isSupported)throw new InvalidOperationException("Actual installed standard URP terrain shader required for diagnostic reference only.");material.shader=reference;}temporary.Add(material);materialCopies.Add(source,material);}
                    receipt.publicNormalBindingVerified=true;
                }
                foreach(var c in world.GetComponentsInChildren<TerrainCollider>(true))colliders.Add(c,c.terrainData);
                receipt.terrains=original.Count;receipt.colliderTerrainBindings=colliders.Count;if(original.Count!=16||colliders.Count!=16)throw new InvalidOperationException("Actual complete16-tile terrain and collision foundation required.");
                heights=original.Values.Select(DragonKingdomWorldVisualMigration.HeightHash).ToArray();
                // CPU terrain raycasts cannot see collision-free sea or shader-wind foliage.
                // This explicit render-only control masks their actual renderers, never terrain,
                // Digger geometry, lights, collider components or material source assignments.
                var digger=new HashSet<Renderer>(world.GetComponentsInChildren<Digger.Modules.Core.Sources.DiggerSystem>(true).SelectMany(d=>d.GetComponentsInChildren<Renderer>(true)));
                foreach(var renderer in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)))if(!digger.Contains(renderer)){masked.Add(renderer,renderer.enabled);renderer.enabled=false;}
                receipt.maskedNonTerrainRenderers=masked.Count;receipt.maskedArt=masked.Select(pair=>new MaskedTerrainArt{name=pair.Key.name,globalId=GlobalObjectId.GetGlobalObjectIdSlow(pair.Key).ToString(),originallyEnabled=pair.Value,sourceMesh=SourceTerrainMaskMesh(pair.Key)}).ToArray();
                if(masked.Count==0)throw new InvalidOperationException("Actual non-terrain water/art renderer mask required for causal control.");
                var dataCopies=new Dictionary<TerrainData,TerrainData>();var layerCopies=new Dictionary<TerrainLayer,TerrainLayer>();
                foreach(var source in original.Values.Distinct())
                {
                    var copy=UnityEngine.Object.Instantiate(source);copy.hideFlags=HideFlags.HideAndDontSave;temporary.Add(copy);dataCopies.Add(source,copy);
                    var layers=source.terrainLayers.Select(layer=>{if(!layerCopies.ContainsKey(layer)){var clone=UnityEngine.Object.Instantiate(layer);clone.hideFlags=HideFlags.HideAndDontSave;temporary.Add(clone);layerCopies.Add(layer,clone);}return layerCopies[layer];}).ToArray();
                    if(layers.Length!=4)throw new InvalidOperationException("Four actual Vista source-backed layers required.");copy.terrainLayers=layers;
                }
                var paletteSlots=new List<TerrainLayer[]>();
                if(paletteMode)
                {
                    receipt.paletteSources=paletteDependencies.Distinct().Where(p=>File.Exists(IoPath(p))).Select(p=>new TerrainPaletteSource{path=p,guid=AssetDatabase.AssetPathToGUID(p),sha256=Hash(File.ReadAllBytes(IoPath(p))),metaSha256=Hash(File.ReadAllBytes(IoPath(p+".meta")))}).ToArray();
                    foreach(var indices in new[]{new[]{0,0,3,4},new[]{1,2,3,4}}){var slots=new List<TerrainLayer>();foreach(int index in indices){var originalLayer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(palettePaths[index]);if(originalLayer==null)throw new InvalidOperationException("Actual pinned source-authored terrain layer required.");var clone=UnityEngine.Object.Instantiate(originalLayer);clone.hideFlags=HideFlags.HideAndDontSave;temporary.Add(clone);slots.Add(clone);}paletteSlots.Add(slots.ToArray());}
                }
                var sourceAlpha=original.ToDictionary(p=>p.Key,p=>TerrainAlphaHash(p.Value));
                var terrainColliders=new HashSet<Collider>(colliders.Keys);var frames=new List<TerrainSurfaceFrame>();
                var go=new GameObject("Temporary actual saved terrain comparison camera");SceneManager.MoveGameObjectToScene(go,scene);camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=58;camera.aspect=(float)Width/Height;camera.clearFlags=CameraClearFlags.Skybox;
                var views=new[]{("village-eye",new Vector3(-506.6667f,6,-393.6842f),new Vector3(-572.8314f,12.795f,-368.7309f)),("forest-eye",new Vector3(-802.392f,30,230.128f),new Vector3(-760.367f,54.644f,275.892f)),("village-surface-overview",new Vector3(-596.661f,235,-620),new Vector3(-596.661f,0,-384.945f))};
                var baselinePixels=new Dictionary<string,byte[]>();
                foreach(var view in views)
                {
                    camera.transform.position=view.Item2;camera.transform.LookAt(view.Item3);
                    foreach(string label in paletteMode?new[]{"saved-10m-baseline","source-village-grass2m-polyart-dirt-rock","source-polyart-authored11m","original-vista-standard-urp-reference","restored-baseline"}:normalMode?new[]{"saved-10m-baseline","public-instanced-normal-off","same-normal-trilinear-aniso16","restored-baseline"}:new[]{"saved-10m-baseline","vendor-2m-all","grass-2m-3p125m","grass-2m-3p125m-normal-off","restored-baseline"})
                    {
                        bool baseline=label=="saved-10m-baseline"||label=="restored-baseline",samplerOnly=label=="same-normal-trilinear-aniso16",standardReference=label=="original-vista-standard-urp-reference";
                        if(!samplerOnly)foreach(var state in samplerStates.Values)RestoreTerrainSampler(state);
                        foreach(var binding in templates)
                        {binding.Key.materialTemplate=normalMode&&!baseline&&!samplerOnly||paletteMode&&standardReference?materialCopies[binding.Value]:binding.Value;if(normalMode&&!baseline&&!samplerOnly){binding.Key.materialTemplate.SetFloat("_EnableInstancedPerPixelNormal",0);binding.Key.materialTemplate.DisableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");}}

                        foreach(var pair in layerCopies)
                        {
                            var index=Array.IndexOf(original.Values.First().terrainLayers,pair.Key);pair.Value.tileSize=pair.Key.tileSize;pair.Value.tileOffset=pair.Key.tileOffset;pair.Value.normalScale=pair.Key.normalScale;pair.Value.diffuseTexture=pair.Key.diffuseTexture;
                            if(samplerOnly)
                            {
                                var source=pair.Key.diffuseTexture;if(source==null)throw new InvalidOperationException("Actual source terrain diffuse texture required.");
                                if(!samplerStates.ContainsKey(source))samplerStates.Add(source,new TerrainSamplerState{texture=source,source=AssetDatabase.GetAssetPath(source),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)),sourceImageHash=source.imageContentsHash.ToString(),width=source.width,height=source.height,mips=source.mipmapCount,sourceAniso=source.anisoLevel,sourceFilter=source.filterMode.ToString(),originalFilter=source.filterMode,originallyDirty=EditorUtility.IsDirty(source)});
                                var state=samplerStates[source];source.filterMode=FilterMode.Trilinear;source.anisoLevel=16;state.configuredFilter=source.filterMode.ToString();state.configuredAniso=source.anisoLevel;state.configuredImageHash=source.imageContentsHash.ToString();state.identicalContent=state.sourceImageHash==state.configuredImageHash&&state.width==source.width&&state.height==source.height&&state.mips==source.mipmapCount;receipt.samplerStates=samplerStates.Values.ToArray();if(!state.identicalContent)throw new InvalidOperationException("In-memory sampler control changed native image hash/dimensions/mips.");pair.Value.diffuseTexture=source;
                            }
                            if(label=="vendor-2m-all")pair.Value.tileSize=new Vector2(2,2);
                            if(label.StartsWith("grass-",StringComparison.Ordinal)){pair.Value.tileSize=index==0?new Vector2(2,2):index==1?new Vector2(3.125f,3.125f):pair.Key.tileSize;if(label.EndsWith("normal-off",StringComparison.Ordinal))pair.Value.normalScale=0;}
                        }
                        if(paletteMode&&!baseline&&!standardReference)
                        {int palette=label=="source-polyart-authored11m"?1:0;foreach(var copy in dataCopies){copy.Value.terrainLayers=paletteSlots[palette];copy.Value.SetAlphamaps(0,0,copy.Key.GetAlphamaps(0,0,copy.Key.alphamapWidth,copy.Key.alphamapHeight));}}
                        foreach(var pair in original){pair.Key.terrainData=normalMode&&!samplerOnly||baseline||standardReference?pair.Value:dataCopies[pair.Value];pair.Key.Flush();}
                        Physics.SyncTransforms();
                        var frame=Capture(camera,world,terrainColliders,original.Keys.First().materialTemplate.shader,output,view.Item1,label);
                        var activeLayers=original.Keys.First().terrainData.terrainLayers;
                        var pixelData=TerrainPixelData(camera,world,terrainColliders,frame.png);if(label=="saved-10m-baseline")baselinePixels.Add(view.Item1,pixelData);var baselineData=baselinePixels[view.Item1];if(pixelData.Length!=baselineData.Length)throw new InvalidOperationException("Same-camera terrain sample mask changed dimensions.");int different=0,maximum=0;long total=0;for(int i=0;i<pixelData.Length;i++){int delta=Math.Abs(pixelData[i]-baselineData[i]);if(delta!=0)different++;maximum=Math.Max(maximum,delta);total+=delta;}
                        var actualMaterial=original.Keys.First().materialTemplate;frames.Add(new TerrainSurfaceFrame{shader=actualMaterial.shader.name,alphaMapsExact=original.All(p=>TerrainAlphaHash(p.Key.terrainData)==sourceAlpha[p.Key]),samplerOnly=samplerOnly,publicNormalOption=actualMaterial.HasProperty("_EnableInstancedPerPixelNormal")?actualMaterial.GetFloat("_EnableInstancedPerPixelNormal"):-1,normalKeywordEnabled=actualMaterial.IsKeywordEnabled("_TERRAIN_INSTANCED_PERPIXEL_NORMAL"),channelsDifferentFromBaseline=different,maximumChannelDeltaFromBaseline=maximum,meanChannelDeltaFromBaseline=pixelData.Length==0?0:(float)total/pixelData.Length,view=view.Item1,intervention=label,png=frame.png,pixelSha256=Hash(File.ReadAllBytes(frame.png)),terrainPixelSha256=Hash(pixelData),terrainSamples=frame.roadSamples,meanTerrainLuminance=frame.meanRoadLuminance,tileSizes=activeLayers.Select(l=>l.tileSize).ToArray(),normalScales=activeLayers.Select(l=>l.normalScale).ToArray(),shaderErrors=frame.shaderErrors});receipt.frames=frames.ToArray();
                        if(!frames.Last().alphaMapsExact)throw new InvalidOperationException("Diagnostic changed the actual original control/alphamap values.");
                        if(frame.roadSamples<100||frame.shaderErrors.Length!=0)throw new InvalidOperationException("Actual terrain pixel coverage and error-free real Digger shader required.");
                        if(colliders.Any(p=>p.Key.terrainData!=p.Value))throw new InvalidOperationException("Render-only layer diagnosis changed a physical collider data binding.");
                    }
                }
                receipt.baselinePixelsRestored=views.All(v=>frames.Single(f=>f.view==v.Item1&&f.intervention=="saved-10m-baseline").terrainPixelSha256==frames.Single(f=>f.view==v.Item1&&f.intervention=="restored-baseline").terrainPixelSha256);
                if(!receipt.baselinePixelsRestored)throw new InvalidOperationException("Same-camera restored baseline pixels differ; cannot infer isolated layer causality.");
                receipt.status=paletteMode?"CAPTURED_SOURCE_AUTHORED_PALETTES_REQUIRES_IMAGE_CHOICE_NOT_WORLD_ACCEPTANCE":normalMode?"CAPTURED_PUBLIC_NORMAL_OPTION_REQUIRES_ACTUAL_CAUSAL_IMAGE_REVIEW_NOT_A_FIX":"CAPTURED_REQUIRES_TEXTURE_NORMAL_IMAGE_REVIEW_NOT_A_FIX";
            }
            catch(Exception exception){receipt.status="REJECTED_TERRAIN_PROBE";receipt.error=exception.ToString();throw;}
            finally
            {
                foreach(var state in samplerStates.Values)RestoreTerrainSampler(state);receipt.textureSamplerStatesRestored=samplerStates.Values.All(state=>state.restoredSampler);
                foreach(var pair in templates)if(pair.Key!=null)pair.Key.materialTemplate=pair.Value;receipt.materialBindingsRestored=templates.All(p=>p.Key!=null&&p.Key.materialTemplate==p.Value);
                foreach(var pair in original)if(pair.Key!=null){pair.Key.terrainData=pair.Value;pair.Key.Flush();}
                foreach(var pair in masked)if(pair.Key!=null)pair.Key.enabled=pair.Value;receipt.nonTerrainRendererStatesRestored=masked.All(pair=>pair.Key!=null&&pair.Key.enabled==pair.Value);
                receipt.originalTerrainDataRestored=original.All(p=>p.Key!=null&&p.Key.terrainData==p.Value);receipt.colliderBindingsUnchanged=colliders.All(p=>p.Key!=null&&p.Key.terrainData==p.Value);
                receipt.physicsAndHeightsExact=world!=null&&heights!=null&&physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject)&&heights.SequenceEqual(original.Values.Select(DragonKingdomWorldVisualMigration.HeightHash));
                if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);foreach(var item in temporary.AsEnumerable().Reverse())UnityEngine.Object.DestroyImmediate(item);ShaderUtil.allowAsyncCompilation=async;
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);
                receipt.sourceBytesPreserved=files.All(p=>File.Exists(IoPath(p.Key))&&Hash(File.ReadAllBytes(IoPath(p.Key)))==p.Value);
                File.WriteAllText(Path.Combine(output,paletteMode?"artist-terrain-palette-comparison.json":normalMode?"terrain-public-normal-comparison.json":"terrain-surface-comparison.json"),JsonUtility.ToJson(receipt,true));
                if(!receipt.sourceBytesPreserved||!receipt.originalTerrainDataRestored||!receipt.colliderBindingsUnchanged||!receipt.physicsAndHeightsExact||!receipt.nonTerrainRendererStatesRestored||!receipt.materialBindingsRestored||!receipt.textureSamplerStatesRestored)throw new InvalidOperationException("Read-only terrain diagnosis failed exact source/physics/restoration guards.");
            }
        }
    }
}
#endif
