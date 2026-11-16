#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Material-only fresh review copy. Original worlds, all height/alpha data,
    // actual geometry/collision, and original material assets stay immutable.
    public static class DragonKingdomCandidateSurfaceReview
    {
        const string Prefix="Assets/TruckDragonKingdom/VisualCandidates/";
        const string RoadShader="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedEasyRoads/URPRoadReview/Owned ER Road URP.shader";
        public const string PackedRoadMap="Assets/EasyRoads3D/textures/roads/default_M_S_AO.tga";
        [Serializable]public sealed class SurfaceBindings
        {public string packedMap,packedMapGuid,packedMapSha256,packedMapMetaSha256,roadMaterial;public long packedMapLocalId;public bool nonzeroSerializedBinding,reloadedSourceBinding;public float metallicPower,smoothnessPower,occlusionStrength;}
        [Serializable]public sealed class Frame{public string view,intervention,png;public string[] errors;}
        [Serializable]public sealed class Receipt
        {
            public string scope="CANDIDATE_ONLY_VENDOR_ROAD_PBR_AND_TERRAIN_TRIPLANAR_REVIEW_NOT_FINAL_WORLD",source,folder,prefab,preview,definition,status="IN_PROGRESS",error;
            public string diffuse,normal,roadShader=RoadShader,terrainShader;
            public Vector2 roadTiling=new Vector2(4,1);public int roadRenderers;public Frame[] frames;
            public bool sourceBytesPreserved,physicsHeightsAndWeightsExact,shaderErrorsAbsent;public string[] changedSourcePaths;
        }
        public static Receipt LastReceipt{get;private set;}
        public static void CreateReviewCandidate()
        {
            string source=Arg("-tsDragonSurfaceSource","VistaIntegrated03"),tag=Arg("-tsDragonWorldCandidate","VistaSurfaces01");
            foreach(string value in new[]{source,tag})if(string.IsNullOrEmpty(value)||value.Length>40||value.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Explicit short source/candidate tags required.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            string preview=Prefix+source+"/DragonKingdomPreview.unity",folder=Prefix+tag;
            if(!File.Exists(Io(preview))||Directory.Exists(folder))throw new InvalidOperationException("Actual source and fresh output candidate required; preserve previous outputs.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.path==preview||s.isDirty||string.IsNullOrEmpty(s.path))throw new InvalidOperationException("Preserve and close user scenes before material review.");}
            var files=AssetDatabase.GetDependencies(preview,true).Concat(new[]{preview,RoadShader}).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(Io(p))));
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Camera camera=null;bool async=ShaderUtil.allowAsyncCompilation;DragonKingdomWorld world=null;string physics=null;
            var receipt=new Receipt{source=source,folder=folder,prefab=folder+"/Shared Dragon Kingdom.prefab",preview=folder+"/DragonKingdomPreview.unity",definition=folder+"/Dragon Kingdom.asset"};LastReceipt=receipt;
            var frames=new List<Frame>();
            try
            {
                ShaderUtil.allowAsyncCompilation=false;Directory.CreateDirectory(folder+"/Materials");AssetDatabase.Refresh();
                scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);
                var terrain=world.GetComponentsInChildren<Terrain>(true);if(terrain.Length!=4)throw new InvalidOperationException("Actual four-tile world required.");
                var terrainMaterials=terrain.ToDictionary(t=>t,t=>t.materialTemplate);
                var roads=world.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.transform.parent!=null&&c.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)).Select(c=>c.GetComponent<Renderer>()).Distinct().ToArray();
                if(roads.Length<17||roads.Any(r=>r==null))throw new InvalidOperationException("Actual complete baked roads required.");receipt.roadRenderers=roads.Length;
                var roadBindings=roads.ToDictionary(r=>r,r=>r.sharedMaterials);
                var surfaces=InstallVendorSurfaces(world,folder);var road=surfaces.road;var projected=surfaces.terrain;var roadShader=road.shader;var terrainShader=projected.shader;
                receipt.diffuse=surfaces.diffuse;receipt.normal=surfaces.normal;receipt.terrainShader=AssetDatabase.GetAssetPath(terrainShader);
                var go=new GameObject("Temporary actual candidate surface review camera");SceneManager.MoveGameObjectToScene(go,scene);camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=58;camera.aspect=1280f/720;camera.clearFlags=CameraClearFlags.Skybox;
                var views=new[]{Tuple.Create("castle",new Vector3(30.769232f,66,286.15384f),new Vector3(0,102,540)),Tuple.Create("forest",new Vector3(-802.39154f,30,230.12761f),new Vector3(-760.36688f,56.194115f,275.68558f))};
                foreach(var view in views)
                {
                    camera.transform.position=view.Item2;camera.transform.LookAt(view.Item3);
                    foreach(int intervention in new[]{0,1,2})
                    {
                        foreach(var binding in roadBindings)binding.Key.sharedMaterials=intervention==0?binding.Value:Enumerable.Repeat(road,binding.Value.Length).ToArray();
                        foreach(var binding in terrainMaterials)binding.Key.materialTemplate=intervention==2?projected:binding.Value;
                        string label=intervention==0?"original":intervention==1?"vendor-road-pbr-tiling":"vendor-road-plus-triplanar-terrain";
                        var frame=Capture(camera,view.Item1,label,roadShader,terrainShader);frames.Add(frame);receipt.frames=frames.ToArray();
                        if(frame.errors.Length!=0)throw new InvalidOperationException("Actual vendor surface compiler errors: "+string.Join(" | ",frame.errors));
                    }
                }
                // The final intervention remains on this fresh review copy.
                foreach(var binding in roadBindings)binding.Key.sharedMaterials=Enumerable.Repeat(road,binding.Value.Length).ToArray();foreach(var t in terrain)t.materialTemplate=projected;
                UnityEngine.Object.DestroyImmediate(camera.gameObject);camera=null;
                if(physics!=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject))throw new InvalidOperationException("Surface review changed actual height/geometry/physics.");
                var definition=UnityEngine.Object.Instantiate(world.definition);AssetDatabase.CreateAsset(definition,receipt.definition);world.definition=definition;
                PrefabUtility.SaveAsPrefabAsset(world.gameObject,receipt.prefab);if(!EditorSceneManager.SaveScene(scene,receipt.preview))throw new InvalidOperationException("Surface candidate save failed.");
                // A global SaveAssets also flushes Unity's automatic upgrades
                // of loaded old vendor materials (V132 stonebrick v6→v8).
                // Persist only this explicit candidate's newly owned assets.
                AssetDatabase.SaveAssetIfDirty(road);AssetDatabase.SaveAssetIfDirty(projected);AssetDatabase.SaveAssetIfDirty(definition);
                receipt.shaderErrorsAbsent=true;receipt.status="CAPTURED_VENDOR_SURFACE_REVIEW_REQUIRES_ACTUAL_IMAGE_APPROVAL";
            }
            catch(Exception e){receipt.status="REJECTED_SURFACE_CANDIDATE";receipt.error=e.ToString();throw;}
            finally
            {
                if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);
                receipt.physicsHeightsAndWeightsExact=world!=null&&physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);
                ShaderUtil.allowAsyncCompilation=async;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);
                receipt.changedSourcePaths=files.Where(p=>!File.Exists(Io(p.Key))||Hash(File.ReadAllBytes(Io(p.Key)))!=p.Value).Select(p=>p.Key).ToArray();receipt.sourceBytesPreserved=receipt.changedSourcePaths.Length==0;
                if(!receipt.sourceBytesPreserved||!receipt.physicsHeightsAndWeightsExact){receipt.status="REJECTED_SURFACE_CANDIDATE";receipt.error+=" Preservation gate failed; changed source paths="+string.Join(" | ",receipt.changedSourcePaths);}
                string output=Output();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"candidate-vendor-surfaces-"+tag+".json"),JsonUtility.ToJson(receipt,true));if(Directory.Exists(folder))File.WriteAllText(folder+"/Surface Receipt.json",JsonUtility.ToJson(receipt,true));
                if(!receipt.sourceBytesPreserved||!receipt.physicsHeightsAndWeightsExact)throw new InvalidOperationException("Candidate surface review violated original source/physics preservation.");
            }
        }
        public sealed class Surfaces{public Material road,terrain;public string diffuse,normal;public SurfaceBindings evidence;}
        public static Surfaces InstallVendorSurfaces(DragonKingdomWorld world,string folder)
        {
            if(world==null||string.IsNullOrEmpty(folder)||!folder.StartsWith(Prefix,StringComparison.Ordinal))throw new InvalidOperationException("Separate actual candidate world/material path required.");
            foreach(string name in new[]{"Actual sediment vendor road.mat","Actual vendor triplanar terrain.mat"})if(File.Exists(folder+"/Materials/"+name))throw new InvalidOperationException("Preserve existing candidate surfaces; use a fresh candidate.");
            var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Sediment/Sediment_VistaSample.terrainlayer");
            if(layer==null||layer.diffuseTexture==null||layer.normalMapTexture==null)throw new InvalidOperationException("Actual owned Vista sediment diffuse and imported normal required.");
            string diffuse=AssetDatabase.GetAssetPath(layer.diffuseTexture),normal=AssetDatabase.GetAssetPath(layer.normalMapTexture);
            var roadShader=AssetDatabase.LoadAssetAtPath<Shader>(RoadShader);var terrainShader=Shader.Find("Digger/Terrain/URP/Lit");
            if(roadShader==null||terrainShader==null||!roadShader.isSupported||!terrainShader.isSupported)throw new InvalidOperationException("Actual installed supported vendor road and triplanar terrain shaders required.");
            var packed=AssetDatabase.LoadAssetAtPath<Texture2D>(PackedRoadMap);if(packed==null)throw new InvalidOperationException("Actual installed EasyRoads neutral packed M/S/AO source required; do not substitute a transient white texture.");
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(packed,out string packedGuid,out long packedId)||packedGuid.Length!=32||packedId==0)throw new InvalidOperationException("Persistent actual vendor packed texture GUID/localID required.");
            var binding=new SurfaceBindings{packedMap=PackedRoadMap,packedMapGuid=packedGuid,packedMapLocalId=packedId,packedMapSha256=SourceDigest(PackedRoadMap),packedMapMetaSha256=SourceDigest(PackedRoadMap+".meta"),roadMaterial=folder+"/Materials/Actual sediment vendor road.mat",metallicPower=layer.metallic,smoothnessPower=layer.smoothness,occlusionStrength=1};
            var road=new Material(roadShader){name="Owned Vista sediment vendor depth gravel",renderQueue=2450};
            road.SetTexture("_MainTex",layer.diffuseTexture);road.SetTextureScale("_MainTex",new Vector2(4,1));road.SetTexture("_BumpMap",layer.normalMapTexture);road.SetTextureScale("_BumpMap",new Vector2(4,1));
            // Installed EasyRoads multiplies the metallic texture's green
            // channel by this property directly. Zero means fully occluded
            // indirect light, producing black roads inside the real cavern.
            // V194's actual same-camera public-property A/B established the
            // neutral packed-map binding: G=1 retains indirect light while
            // zero metallic/smoothness powers keep this sediment rough.
            // Leaving the packed map unbound made shadowed roads nearly black.
            // The installed vendor32x32 packed default has constantG=1 and
            // R=0. V196 proved Texture2D.whiteTexture is transient and saves a
            // null reference, so bind the actual persistent source asset.
            road.SetTexture("_Metallic",packed);
            road.SetColor("_Color",Color.white);road.SetFloat("_BumpScale",.65f);road.SetFloat("_MainMetallicPower",layer.metallic);road.SetFloat("_MainSmoothnessPower",layer.smoothness);road.SetFloat("_OcclusionStrength",1);road.SetFloat("_OffsetFactor",-4);road.SetFloat("_OffsetUnit",-4);
            AssetDatabase.CreateAsset(road,binding.roadMaterial);AssetDatabase.SaveAssetIfDirty(road);
            string saved=File.ReadAllText(Io(binding.roadMaterial));
            binding.nonzeroSerializedBinding=System.Text.RegularExpressions.Regex.IsMatch(saved,@"(?m)^    - _Metallic:\r?\n        m_Texture: \{fileID: "+packedId+", guid: "+packedGuid+@", type: 3\}");
            if(!binding.nonzeroSerializedBinding)throw new InvalidOperationException("Actual saved packed-map field did not retain its nonzero native texture identity.");
            // No scene renderer owns this material yet, so explicitly unload
            // then reload it before assignment. An in-memory value alone was
            // insufficient for V196's transient builtin texture.
            Resources.UnloadAsset(road);road=AssetDatabase.LoadAssetAtPath<Material>(binding.roadMaterial);
            binding.reloadedSourceBinding=road!=null&&road.GetTexture("_Metallic")==packed&&AssetDatabase.GetDependencies(binding.roadMaterial,true).Contains(PackedRoadMap);
            if(!binding.reloadedSourceBinding||SourceDigest(PackedRoadMap)!=binding.packedMapSha256||SourceDigest(PackedRoadMap+".meta")!=binding.packedMapMetaSha256)throw new InvalidOperationException("Actual native material reload or unchanged source packed map proof failed.");
            var projected=new Material(terrainShader){name="Owned Digger triplanar terrain",enableInstancing=true};
            projected.SetFloat("_TerrainWidthInv",1f/1600);projected.SetFloat("_TerrainHeightInv",1f/1600);projected.SetFloat("_EnableHeightBlend",0);projected.DisableKeyword("_TERRAIN_BLEND_HEIGHT");projected.SetFloat("_EnableInstancedPerPixelNormal",1);projected.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
            AssetDatabase.CreateAsset(projected,folder+"/Materials/Actual vendor triplanar terrain.mat");
            foreach(var roadRenderer in world.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.transform.parent!=null&&c.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)).Select(c=>c.GetComponent<Renderer>()).Distinct())
            {if(roadRenderer==null)throw new InvalidOperationException("Actual road renderer missing.");roadRenderer.sharedMaterials=Enumerable.Repeat(road,roadRenderer.sharedMaterials.Length).ToArray();}
            foreach(var t in world.GetComponentsInChildren<Terrain>(true))t.materialTemplate=projected;
            return new Surfaces{road=road,terrain=projected,diffuse=diffuse,normal=normal,evidence=binding};
        }
        static Frame Capture(Camera camera,string view,string label,params Shader[] shaders)
        {
            var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                string output=Output();Directory.CreateDirectory(output);var frame=new Frame{view=view,intervention=label,png=Path.Combine(output,view+"-"+label+".png"),errors=shaders.SelectMany(s=>ShaderUtil.GetShaderMessages(s)).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray()};File.WriteAllBytes(frame.png,image.EncodeToPNG());return frame;
            }
            finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
        static string Arg(string flag,string fallback){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
        static string Output()=>Path.Combine(Arg("-tsDragonOutput","Logs/TruckSports/DragonKingdom"),"CandidateVendorSurfaces");
        static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string SourceDigest(string path)=>Hash(File.ReadAllBytes(Io(path))).Replace("-","").ToLowerInvariant();
        static string Hash(byte[] data){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data));}
    }
}
#endif
