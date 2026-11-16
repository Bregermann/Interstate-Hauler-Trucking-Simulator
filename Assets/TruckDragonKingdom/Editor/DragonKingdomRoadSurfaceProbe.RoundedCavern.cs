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
        [Serializable]public sealed class RoundedCavernFrames
        {public string scope="CONTROLLED_ACTUAL_SAVED_ROUNDED_CAVERN_GEOMETRY_VIEWS_NOT_PLAYER_BUILD_OR_GAMEPLAY_ACCEPTANCE",preview,status="IN_PROGRESS";public bool sourcesExact,physicsAndHeightsExact;public int actualSavedDiggerRenderers;public Frame[] frames;public Frame[] shadowFrames=Array.Empty<Frame>();public bool shadowDiagnosticRequested,shadowDiagnosticPerformed,directionalLightRestored,shadowCameraPoseExact,voxelMembershipExact;public string directionalLightIdentity,shadowLightJsonBefore,shadowLightJsonWhileDisabled,shadowLightJsonAfter,shadowLightPoseBefore,shadowLightPoseAfter,shadowCameraPose;public Frame[] normalFrames=Array.Empty<Frame>();public bool normalDiagnosticRequested,normalDiagnosticPerformed,normalStateRestored,normalTemporaryCopiesDestroyed,normalTexturesUvExact,normalNonAmplitudePropertiesExact,normalCameraAndLightsExact,normalMeshBindingsExact,normalRendererBindingsExact,normalOriginalMaterialsExact;public int normalTemporaryCopies,normalScaleChannelsZeroed,normalAuthoredMapsWithNonzeroScale;public NormalDiagnosticMaterial[] normalMaterials;public Frame[] lodFrames=Array.Empty<Frame>();public bool lodDiagnosticRequested,lodDiagnosticPerformed,lodStateRestored,lodRendererReferencesExact,lodCameraAndLightsExact,lodMeshMaterialBindingsExact;public int lodGroupsChanged,lodLevelsObserved,lodNonemptyFirstLevelMeshes;public LodDiagnosticGroup[] lodGroups;public Frame[] neutralFrames=Array.Empty<Frame>();public bool neutralDiagnosticRequested,neutralDiagnosticPerformed,neutralStateRestored,neutralOriginalMaterialsExact,neutralRendererMeshBindingsExact,neutralCameraLightsLodsExact,neutralCopiesDestroyed;public int neutralTemporaryCopies,neutralAuthoredDiffuseMaps,neutralRendererSlotsChanged;public NeutralDiagnosticMaterial[] neutralMaterials;public Frame[] weldedNormalFrames=Array.Empty<Frame>();public bool weldedNormalDiagnosticRequested,weldedNormalDiagnosticPerformed,weldedNormalStateRestored,weldedNormalTemporaryObjectsDestroyed;public int weldedNormalMeshes,weldedNormalChangedMeshes;public WeldedNormalGroup[] weldedNormalGroups;public bool standardLitDiagnosticRequested;public StandardLitCavernEvidence standardLitDiagnostic;public bool normalKeywordDiagnosticRequested,normalKeywordDiagnosticPerformed,normalKeywordAllPropertiesExact,normalKeywordMeshPayloadExact,normalKeywordPropertyBlocksAbsent;public int normalKeywordsDisabled;public string normalDiagnosticMode;public Frame[] normalKeywordFrames=Array.Empty<Frame>();public bool baryRgbDiagnosticRequested;public BaryRgbCavernEvidence baryRgbDiagnostic;public bool rockTilingDiagnosticRequested;public CavernRockTilingEvidence rockTilingDiagnostic;public bool ownedRockMaterialDiagnosticRequested;public CavernRockTilingEvidence ownedRockMaterialDiagnostic;}
        [Serializable]public sealed class NormalDiagnosticMaterial
        {public string source,guid,originalJsonSha256;public string[] channels,normalTextureSources;public float[] originalScales;public bool keywordOnly,allOtherPropertiesExact;public string[] originalKeywords,diagnosticKeywords;}
        public static void ValidateCavernNormalDiagnosticModes(bool amplitudeOnly,bool keywordOnly)
        {if(amplitudeOnly&&keywordOnly)throw new InvalidOperationException("Choose exactly one normal diagnostic: amplitude-only or keyword-only; simultaneous changes would confound the result.");}
        public static bool CavernNormalKeywordOnlyPropertiesExact(Material source,Material candidate)
        {
            if(source==null||candidate==null||source==candidate||source.shader!=candidate.shader||!source.IsKeywordEnabled("_NORMALMAP")||candidate.IsKeywordEnabled("_NORMALMAP"))return false;
            // EditorJsonUtility can encode distinct transient Texture objects identically; require actual native texture identities as well.
            var textureNames=source.GetTexturePropertyNames();
            if(!textureNames.SequenceEqual(candidate.GetTexturePropertyNames())||textureNames.Any(name=>source.GetTexture(name)!=candidate.GetTexture(name)||!source.GetTextureScale(name).Equals(candidate.GetTextureScale(name))||!source.GetTextureOffset(name).Equals(candidate.GetTextureOffset(name))))return false;
            var expected=new Material(source){name=candidate.name,hideFlags=candidate.hideFlags};
            try{expected.DisableKeyword("_NORMALMAP");return EditorJsonUtility.ToJson(expected)==EditorJsonUtility.ToJson(candidate);}
            finally{UnityEngine.Object.DestroyImmediate(expected);}
        }
        public static Material CreateCavernNormalKeywordDiagnosticMaterial(Material source)
        {
            if(source==null||!source.shader.name.StartsWith("Digger/Mesh/URP/Lit-Pass",StringComparison.Ordinal)||!source.IsKeywordEnabled("_NORMALMAP"))throw new InvalidOperationException("Actual normal-map-enabled Digger material required for a meaningful keyword-only comparison.");
            var copy=new Material(source){name=source.name+" temporary NORMALMAP-keyword-only diagnostic",hideFlags=HideFlags.HideAndDontSave};
            try{copy.DisableKeyword("_NORMALMAP");if(!CavernNormalKeywordOnlyPropertiesExact(source,copy))throw new InvalidOperationException("Keyword-only clone changed other original serialized material properties.");return copy;}
            catch{UnityEngine.Object.DestroyImmediate(copy);throw;}
        }
        [Serializable]public sealed class LodDiagnosticLevel
        {public int level;public float transition,fadeWidth;public string[] renderers,meshes;public int[] vertices;}
        [Serializable]public sealed class LodDiagnosticGroup
        {public string identity,beforeJson,highDetailJson,afterJson,poseBefore,poseAfter;public float size;public LodDiagnosticLevel[] before,highDetail,after;}
        [Serializable]public sealed class NeutralDiagnosticMaterial
        {public string source,guid,shader,originalJsonSha256;public string[] originalDiffuseTextures,originalKeywords,neutralKeywords;public bool sameShader,uniformDiffuse,zeroNormalsMasksMetallicSmoothness,uvExact;}
        static void CaptureCavernNeutralSurfaceDiagnostic(Camera camera,DragonKingdomWorld world,HashSet<Collider> roads,Shader shader,string output,RoundedCavernFrames proof,Renderer[] renderers)
        {
            var bindings=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);var originals=bindings.Values.SelectMany(m=>m).Distinct().ToArray();
            if(originals.Length==0||originals.Any(m=>m==null||!m.shader.name.StartsWith("Digger/Mesh/URP/Lit-Pass",StringComparison.Ordinal)))throw new InvalidOperationException("Actual complete native Digger URP materials required for neutral-surface diagnosis.");
            string Pose(Transform t)=>EditorJsonUtility.ToJson(t)+"/"+t.gameObject.activeSelf+"/"+t.gameObject.layer+"/"+(int)t.gameObject.hideFlags;
            var originalJson=originals.ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));var rendererJson=renderers.ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r));var meshBindings=renderers.ToDictionary(r=>r,r=>r.GetComponent<MeshFilter>()?.sharedMesh);var lodJson=world.GetComponentsInChildren<Digger.Modules.Core.Sources.DiggerSystem>(true).SelectMany(s=>s.GetComponentsInChildren<LODGroup>(true)).ToDictionary(g=>g,g=>EditorJsonUtility.ToJson(g));
            var lights=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>l,l=>EditorJsonUtility.ToJson(l)+"/"+Pose(l.transform));string cameraJson=EditorJsonUtility.ToJson(camera)+"/"+Pose(camera.transform);
            var copies=new Dictionary<Material,Material>();var rows=new List<NeutralDiagnosticMaterial>();var paired=new List<Frame>();proof.neutralDiagnosticPerformed=true;
            bool Restored()
            {
                proof.neutralOriginalMaterialsExact=originalJson.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key));proof.neutralRendererMeshBindingsExact=bindings.All(p=>p.Key!=null&&p.Key.sharedMaterials.SequenceEqual(p.Value)&&rendererJson[p.Key]==EditorJsonUtility.ToJson(p.Key)&&meshBindings[p.Key]==p.Key.GetComponent<MeshFilter>()?.sharedMesh);proof.neutralCameraLightsLodsExact=cameraJson==EditorJsonUtility.ToJson(camera)+"/"+Pose(camera.transform)&&lights.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)+"/"+Pose(p.Key.transform))&&lodJson.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key));proof.neutralCopiesDestroyed=copies.Values.All(m=>m==null);return proof.neutralOriginalMaterialsExact&&proof.neutralRendererMeshBindingsExact&&proof.neutralCameraLightsLodsExact&&proof.neutralCopiesDestroyed;
            }
            try
            {
                foreach(var material in originals)
                {
                    var copy=new Material(material){name=material.name+" temporary same-shader neutral-surface diagnosis",hideFlags=HideFlags.HideAndDontSave};copies.Add(material,copy);var row=new NeutralDiagnosticMaterial{source=AssetDatabase.GetAssetPath(material),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material)),shader=material.shader.name,originalJsonSha256=Hash(System.Text.Encoding.UTF8.GetBytes(originalJson[material])),originalKeywords=material.shaderKeywords,originalDiffuseTextures=Enumerable.Range(0,4).Select(i=>material.HasProperty("_Splat"+i)&&material.GetTexture("_Splat"+i)!=null?AssetDatabase.GetAssetPath(material.GetTexture("_Splat"+i)):"").ToArray(),sameShader=copy.shader==material.shader,uniformDiffuse=true,zeroNormalsMasksMetallicSmoothness=true,uvExact=true};
                    for(int i=0;i<4;i++)
                    {
                        string splat="_Splat"+i,remap="_DiffuseRemapScale"+i;if(!copy.HasProperty(splat)||!copy.HasProperty(remap))throw new InvalidOperationException("All four original native diffuse layers/remap properties are required.");copy.SetTexture(splat,Texture2D.grayTexture);copy.SetVector(remap,Vector4.one);row.uniformDiffuse&=copy.GetTexture(splat)==Texture2D.grayTexture&&copy.GetVector(remap)==Vector4.one;
                        foreach(string prefix in new[]{"_NormalScale","_LayerHasMask","_Metallic","_Smoothness"}){string name=prefix+i;if(!copy.HasProperty(name))throw new InvalidOperationException("Original native neutralization property missing: "+name);copy.SetFloat(name,0);row.zeroNormalsMasksMetallicSmoothness&=copy.GetFloat(name)==0;}
                        if(copy.HasProperty("_MaskMapRemapScale"+i))copy.SetVector("_MaskMapRemapScale"+i,Vector4.zero);if(copy.HasProperty("_MaskMapRemapOffset"+i))copy.SetVector("_MaskMapRemapOffset"+i,new Vector4(0,1,0,0));
                    }
                    if(copy.HasProperty("_EnableHeightBlend"))copy.SetFloat("_EnableHeightBlend",0);foreach(string keyword in new[]{"_NORMALMAP","_MASKMAP","_TERRAIN_BLEND_HEIGHT"})copy.DisableKeyword(keyword);row.neutralKeywords=copy.shaderKeywords;
                    row.uvExact=material.GetTexturePropertyNames().All(name=>material.GetTextureScale(name)==copy.GetTextureScale(name)&&material.GetTextureOffset(name)==copy.GetTextureOffset(name));proof.neutralAuthoredDiffuseMaps+=row.originalDiffuseTextures.Count(p=>p.StartsWith("Assets/",StringComparison.Ordinal));if(!row.sameShader||!row.uniformDiffuse||!row.zeroNormalsMasksMetallicSmoothness||!row.uvExact)throw new InvalidOperationException("Neutral diagnosis did not retain original shader/UVs or establish constant diffuse/no bump/mask/specular settings.");rows.Add(row);
                }
                proof.neutralTemporaryCopies=copies.Count;proof.neutralMaterials=rows.ToArray();if(proof.neutralAuthoredDiffuseMaps==0)throw new InvalidOperationException("Meaningful diagnosis requires actual authored diffuse maps before neutralization.");foreach(var pair in bindings){pair.Key.sharedMaterials=pair.Value.Select(m=>copies[m]).ToArray();proof.neutralRendererSlotsChanged+=pair.Value.Length;}
                if(!bindings.All(p=>p.Key.sharedMaterials.SequenceEqual(p.Value.Select(m=>copies[m]))))throw new InvalidOperationException("Actual Digger renderer slots did not receive the disposable same-shader materials.");
                var neutral=Capture(camera,world,roads,shader,output,"rounded-entry-neutral-surface-diagnostic","same-shader-neutral-lit");paired.Add(neutral);proof.neutralFrames=paired.ToArray();if(neutral.shaderErrors.Length!=0)throw new InvalidOperationException("Actual Digger shader errors in neutral-surface diagnosis.");
            }
            finally
            {
                foreach(var pair in bindings)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;foreach(var copy in copies.Values)if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);proof.neutralStateRestored=Restored();
            }
            if(!proof.neutralStateRestored)throw new InvalidOperationException("Read-only neutral-surface diagnosis failed exact original materials/renderers/meshes/LOD/light/camera restoration.");var restored=Capture(camera,world,roads,shader,output,"rounded-entry-neutral-surface-diagnostic","original-materials-restored");paired.Add(restored);proof.neutralFrames=paired.ToArray();proof.neutralStateRestored&=Restored();if(restored.shaderErrors.Length!=0||!proof.neutralStateRestored)throw new InvalidOperationException("Restored neutral-pair capture changed original source rendering state.");
        }
        public static RoundedCavernFrames LastRoundedCavernFrames{get;private set;}
        static void CaptureCavernLodDiagnostic(Camera camera,DragonKingdomWorld world,HashSet<Collider> roads,Shader shader,string output,RoundedCavernFrames proof)
        {
            var groups=world.GetComponentsInChildren<Digger.Modules.Core.Sources.DiggerSystem>(true).SelectMany(s=>s.GetComponentsInChildren<LODGroup>(true)).Distinct().ToArray();
            if(groups.Length==0||groups.Any(g=>g.GetComponent<Digger.Modules.Core.Sources.ChunkLODGroup>()==null))throw new InvalidOperationException("Only actual saved native Digger chunk LODGroups qualify for this diagnosis.");
            string Identity(UnityEngine.Object value)=>value==null?"NONE":GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
            string Pose(Transform t)=>EditorJsonUtility.ToJson(t)+"/"+t.gameObject.activeSelf+"/"+t.gameObject.layer+"/"+(int)t.gameObject.hideFlags;
            LodDiagnosticLevel[] Levels(LOD[] levels)=>levels.Select((level,i)=>new LodDiagnosticLevel{level=i,transition=level.screenRelativeTransitionHeight,fadeWidth=level.fadeTransitionWidth,renderers=level.renderers.Select(Identity).ToArray(),meshes=level.renderers.Select(r=>Identity(r==null?null:r.GetComponent<MeshFilter>()?.sharedMesh)).ToArray(),vertices=level.renderers.Select(r=>r==null?0:r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount??0).ToArray()}).ToArray();
            var originals=groups.ToDictionary(g=>g,g=>g.GetLODs().Select(l=>new LOD(l.screenRelativeTransitionHeight,l.renderers.ToArray()){fadeTransitionWidth=l.fadeTransitionWidth}).ToArray());
            if(originals.Any(p=>p.Value.Length!=3||p.Value.Any(l=>l.renderers.Length!=1||l.renderers[0]==null)))throw new InvalidOperationException("The actual native three-level 1/2/4-voxel renderer layout is required without replacing renderer references.");
            var groupJson=groups.ToDictionary(g=>g,g=>EditorJsonUtility.ToJson(g));var poses=groups.ToDictionary(g=>g,g=>Pose(g.transform));
            var allRenderers=originals.Values.SelectMany(l=>l.SelectMany(v=>v.renderers)).Distinct().ToArray();var rendererJson=allRenderers.ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r));var meshBindings=allRenderers.ToDictionary(r=>r,r=>r.GetComponent<MeshFilter>()?.sharedMesh);var materialBindings=allRenderers.ToDictionary(r=>r,r=>r.sharedMaterials);
            var materialJson=materialBindings.Values.SelectMany(v=>v).Where(m=>m!=null).Distinct().ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));
            var lights=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>l,l=>EditorJsonUtility.ToJson(l)+"/"+Pose(l.transform));string cameraJson=EditorJsonUtility.ToJson(camera)+"/"+Pose(camera.transform);
            bool ReferencesExact()=>originals.All(p=>{var now=p.Key.GetLODs();return now.Length==p.Value.Length&&now.Where((l,i)=>!l.renderers.SequenceEqual(p.Value[i].renderers)||BitConverter.SingleToInt32Bits(l.fadeTransitionWidth)!=BitConverter.SingleToInt32Bits(p.Value[i].fadeTransitionWidth)).Count()==0;});
            bool Restored()
            {
                proof.lodRendererReferencesExact=ReferencesExact();proof.lodCameraAndLightsExact=cameraJson==EditorJsonUtility.ToJson(camera)+"/"+Pose(camera.transform)&&lights.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)+"/"+Pose(p.Key.transform));
                proof.lodMeshMaterialBindingsExact=allRenderers.All(r=>r!=null&&rendererJson[r]==EditorJsonUtility.ToJson(r)&&meshBindings[r]==r.GetComponent<MeshFilter>()?.sharedMesh&&materialBindings[r].SequenceEqual(r.sharedMaterials))&&materialJson.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key));
                return proof.lodRendererReferencesExact&&proof.lodCameraAndLightsExact&&proof.lodMeshMaterialBindingsExact&&groups.All(g=>groupJson[g]==EditorJsonUtility.ToJson(g)&&poses[g]==Pose(g.transform));
            }
            var rows=groups.ToDictionary(g=>g,g=>new LodDiagnosticGroup{identity=Identity(g),beforeJson=groupJson[g],poseBefore=poses[g],size=g.size,before=Levels(originals[g])});proof.lodGroups=groups.Select(g=>rows[g]).ToArray();proof.lodLevelsObserved=originals.Values.Sum(v=>v.Length);proof.lodNonemptyFirstLevelMeshes=originals.Count(p=>p.Value[0].renderers.Any(r=>r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount>0));
            if(proof.lodNonemptyFirstLevelMeshes==0||!originals.Any(p=>p.Key.enabled&&p.Key.gameObject.activeInHierarchy&&p.Value[0].screenRelativeTransitionHeight>.000001f))throw new InvalidOperationException("Meaningful LOD diagnosis requires actual active native high-detail meshes and a genuinely changed threshold.");
            var paired=new List<Frame>();proof.lodDiagnosticPerformed=true;
            try
            {
                foreach(var group in groups)
                {
                    var levels=originals[group].Select((l,i)=>new LOD(i==0?.000001f:i==1?.0000005f:0,l.renderers.ToArray()){fadeTransitionWidth=l.fadeTransitionWidth}).ToArray();group.SetLODs(levels);proof.lodGroupsChanged++;
                    rows[group].highDetailJson=EditorJsonUtility.ToJson(group);rows[group].highDetail=Levels(group.GetLODs());
                }
                if(!ReferencesExact())throw new InvalidOperationException("Public SetLODs changed actual renderer references or original fade widths.");
                var high=Capture(camera,world,roads,shader,output,"rounded-entry-lod-diagnostic","native-lod0-preferred");paired.Add(high);proof.lodFrames=paired.ToArray();if(high.shaderErrors.Length!=0)throw new InvalidOperationException("Actual saved Digger shader errors in native LOD diagnosis.");
            }
            finally
            {
                foreach(var group in groups)if(group!=null){group.SetLODs(originals[group]);rows[group].afterJson=EditorJsonUtility.ToJson(group);rows[group].poseAfter=Pose(group.transform);rows[group].after=Levels(group.GetLODs());}
                proof.lodStateRestored=Restored();
            }
            if(!proof.lodStateRestored)throw new InvalidOperationException("Read-only LOD diagnosis failed exact original group JSON/flags/renderer/mesh/material/light/camera/pose restoration.");
            var restored=Capture(camera,world,roads,shader,output,"rounded-entry-lod-diagnostic","native-lod-restored");paired.Add(restored);proof.lodFrames=paired.ToArray();proof.lodStateRestored&=Restored();if(restored.shaderErrors.Length!=0||!proof.lodStateRestored)throw new InvalidOperationException("Restored LOD-pair capture changed actual original group or rendering state.");
        }
        public static void CaptureSavedRoundedCavern()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Stopped isolated Editor required for saved cavern rendering.");
            var args=Environment.GetCommandLineArgs();ValidateCavernNormalDiagnosticModes(args.Contains("-tsDragonCavernNormalDiagnostic"),args.Contains("-tsDragonCavernNormalKeywordDiagnostic"));int at=Array.IndexOf(args,"-tsDragonCavernProbe");string tag=at>=0&&at+1<args.Length?args[at+1]:"";
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Exact saved owned cavern probe required.");
            string preview=Root+"VisualCandidates/"+tag+"/Cavern Input Probe.unity",data="Assets/DiggerData/Scenes/DK-"+tag;
            if(!File.Exists(IoPath(preview))||!Directory.Exists(data))throw new InvalidOperationException("Actual saved rounded cavern and vendor voxel persistence required.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty||string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))throw new InvalidOperationException("Preserve saved incoming scene context before controlled inspection.");
            var ownedRockDependencies=args.Contains("-tsDragonCavernOwnedRockMaterialDiagnostic")?new[]{CavernRoughRockDiffuse,CavernRoughRockNormal}:Array.Empty<string>();
            foreach(string dependency in ownedRockDependencies.SelectMany(p=>new[]{p,p+".meta"}))if(!File.Exists(IoPath(dependency)))throw new InvalidOperationException("Owned rock source dependency missing before native watch: "+dependency);
            var watched=AssetDatabase.GetDependencies(preview,true).Concat(new[]{preview}).Concat(ownedRockDependencies).Concat(Directory.GetFiles(data,"*",SearchOption.AllDirectories)).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(IoPath(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(IoPath(p))));
            var voxelMembers=Directory.GetFiles(data,"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            var proof=LastRoundedCavernFrames=new RoundedCavernFrames{preview=preview,shadowDiagnosticRequested=args.Contains("-tsDragonCavernShadowDiagnostic"),normalDiagnosticRequested=args.Contains("-tsDragonCavernNormalDiagnostic"),normalKeywordDiagnosticRequested=args.Contains("-tsDragonCavernNormalKeywordDiagnostic"),baryRgbDiagnosticRequested=args.Contains("-tsDragonCavernBaryRgbDiagnostic"),rockTilingDiagnosticRequested=args.Contains("-tsDragonCavernRockTilingDiagnostic"),ownedRockMaterialDiagnosticRequested=args.Contains("-tsDragonCavernOwnedRockMaterialDiagnostic"),lodDiagnosticRequested=args.Contains("-tsDragonCavernLodDiagnostic"),neutralDiagnosticRequested=args.Contains("-tsDragonCavernNeutralSurfaceDiagnostic"),weldedNormalDiagnosticRequested=args.Contains("-tsDragonCavernWeldedNormalDiagnostic"),standardLitDiagnosticRequested=args.Contains("-tsDragonCavernStandardLitDiagnostic")};var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Camera camera=null;DragonKingdomWorld world=null;string physics=null;string[] heights=null;bool async=ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);heights=world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)).ToArray();
                var renderers=world.GetComponentsInChildren<Digger.Modules.Core.Sources.DiggerSystem>(true).SelectMany(d=>d.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.enabled&&r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();proof.actualSavedDiggerRenderers=renderers.Length;
                if(renderers.Length==0)throw new InvalidOperationException("Saved actual Digger mesh renderers required, not authoring brush geometry.");
                var shader=Shader.Find("Digger/Mesh/URP/Lit-Pass0");if(shader==null||!shader.isSupported)throw new InvalidOperationException("Actual installed Digger URP shader required.");
                var roads=new HashSet<Collider>(world.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.transform.parent!=null&&c.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)));
                var go=new GameObject("Temporary actual saved rounded cavern inspection camera");SceneManager.MoveGameObjectToScene(go,scene);camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=67;camera.nearClipPlane=.15f;camera.farClipPlane=6500;camera.aspect=(float)Width/Height;camera.clearFlags=CameraClearFlags.Skybox;
                var views=new[]{new{label="rounded-entry",eye=new Vector3(448,113,780),target=new Vector3(530,134,799)},new{label="rounded-hoard",eye=new Vector3(556,120,829),target=new Vector3(483,128,775)},new{label="rounded-flight",eye=new Vector3(525,146,805),target=new Vector3(510,125,774)}};
                var frames=new List<Frame>();string output=Output();Directory.CreateDirectory(output);Physics.SyncTransforms();
                foreach(var view in views)
                {
                    camera.transform.SetPositionAndRotation(view.eye,Quaternion.LookRotation(view.target-view.eye,Vector3.up));
                    var frame=Capture(camera,world,roads,shader,output,view.label,"actual-saved");frames.Add(frame);
                    if(frame.shaderErrors.Length!=0)throw new InvalidOperationException("Actual saved Digger shader errors in controlled cavern view.");
                    if(proof.weldedNormalDiagnosticRequested&&view.label=="rounded-entry")CaptureCaveWeldedNormalDiagnostic(camera,world,roads,shader,output,proof,renderers);
                    if(proof.standardLitDiagnosticRequested&&view.label=="rounded-entry"){proof.standardLitDiagnostic=new StandardLitCavernEvidence();CaptureCaveStandardLitDiagnostic(camera,world,roads,shader,output,renderers,proof.standardLitDiagnostic);}
                    if(proof.neutralDiagnosticRequested&&view.label=="rounded-entry")CaptureCavernNeutralSurfaceDiagnostic(camera,world,roads,shader,output,proof,renderers);
                    if(proof.lodDiagnosticRequested&&view.label=="rounded-entry")CaptureCavernLodDiagnostic(camera,world,roads,shader,output,proof);
                    if(proof.shadowDiagnosticRequested&&view.label=="rounded-entry")
                    {
                        var light=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Single(l=>l.type==LightType.Directional&&l.enabled&&l.gameObject.activeInHierarchy);
                        var originalShadows=light.shadows;if(originalShadows==LightShadows.None)throw new InvalidOperationException("Actual shadow-casting directional light required for meaningful paired shadow diagnosis.");
                        string Pose(Transform t)=>t.position.ToString("R")+"/"+t.rotation.ToString("R")+"/"+t.localScale.ToString("R");
                        proof.directionalLightIdentity=GlobalObjectId.GetGlobalObjectIdSlow(light).ToString();proof.shadowLightJsonBefore=EditorJsonUtility.ToJson(light);proof.shadowLightPoseBefore=Pose(light.transform);proof.shadowCameraPose=Pose(camera.transform);proof.shadowDiagnosticPerformed=true;
                        var paired=new List<Frame>();
                        try
                        {
                            light.shadows=LightShadows.None;proof.shadowLightJsonWhileDisabled=EditorJsonUtility.ToJson(light);
                            var noShadow=Capture(camera,world,roads,shader,output,"rounded-entry-shadow-diagnostic","directional-shadows-none");paired.Add(noShadow);proof.shadowFrames=paired.ToArray();if(noShadow.shaderErrors.Length!=0)throw new InvalidOperationException("Actual saved Digger shader errors in shadow diagnostic.");
                        }
                        finally
                        {
                            light.shadows=originalShadows;proof.shadowLightJsonAfter=EditorJsonUtility.ToJson(light);proof.shadowLightPoseAfter=Pose(light.transform);proof.directionalLightRestored=light.shadows==originalShadows&&proof.shadowLightJsonBefore==proof.shadowLightJsonAfter&&proof.shadowLightPoseBefore==proof.shadowLightPoseAfter;proof.shadowCameraPoseExact=Pose(camera.transform)==proof.shadowCameraPose;
                        }
                        if(!proof.directionalLightRestored||!proof.shadowCameraPoseExact)throw new InvalidOperationException("Read-only directional shadow diagnosis failed exact light JSON/flags/pose or camera-pose restoration.");
                        var restored=Capture(camera,world,roads,shader,output,"rounded-entry-shadow-diagnostic","directional-shadows-restored");paired.Add(restored);proof.shadowFrames=paired.ToArray();proof.directionalLightRestored&=EditorJsonUtility.ToJson(light)==proof.shadowLightJsonBefore&&Pose(light.transform)==proof.shadowLightPoseBefore;proof.shadowCameraPoseExact&=Pose(camera.transform)==proof.shadowCameraPose;if(restored.shaderErrors.Length!=0||!proof.directionalLightRestored||!proof.shadowCameraPoseExact)throw new InvalidOperationException("Restored shadow-pair capture changed exact original light/camera state or reported shader errors.");
                    }
                    if(proof.rockTilingDiagnosticRequested&&view.label=="rounded-entry"){proof.rockTilingDiagnostic=new CavernRockTilingEvidence();CaptureCavernRockTilingDiagnostic(camera,world,roads,shader,output,renderers,frame,proof.rockTilingDiagnostic);}
                    if(proof.ownedRockMaterialDiagnosticRequested&&view.label=="rounded-entry"){proof.ownedRockMaterialDiagnostic=new CavernRockTilingEvidence();CaptureCavernRockTilingDiagnostic(camera,world,roads,shader,output,renderers,frame,proof.ownedRockMaterialDiagnostic,true);}
                    if(proof.baryRgbDiagnosticRequested&&view.label=="rounded-entry"){proof.baryRgbDiagnostic=new BaryRgbCavernEvidence();CaptureCavernBaryRgbDiagnostic(camera,world,roads,shader,output,renderers,proof.baryRgbDiagnostic);}
                    if((proof.normalDiagnosticRequested||proof.normalKeywordDiagnosticRequested)&&view.label=="rounded-entry")
                    {
                        bool keywordOnly=proof.normalKeywordDiagnosticRequested;proof.normalDiagnosticMode=keywordOnly?"KEYWORD_ONLY_NORMALMAP_DISABLED":"AMPLITUDE_ONLY_SCALES_ZERO";if(keywordOnly){RequireCavernStandardLitNoPropertyBlocks(renderers);proof.normalKeywordPropertyBlocksAbsent=true;}
                        var bindings=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);var originals=bindings.Values.SelectMany(m=>m).Distinct().ToArray();if(originals.Any(m=>m==null))throw new InvalidOperationException("Actual Digger mesh materials must be complete before normal-map diagnosis.");
                        var originalJson=originals.ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>l,l=>EditorJsonUtility.ToJson(l)+"/"+EditorJsonUtility.ToJson(l.transform));string cameraJson=EditorJsonUtility.ToJson(camera)+"/"+EditorJsonUtility.ToJson(camera.transform);
                        var meshBindings=renderers.Select(r=>r.GetComponent<MeshFilter>()).ToDictionary(f=>f,f=>f.sharedMesh);var copies=new Dictionary<Material,Material>();var materialRows=new List<NormalDiagnosticMaterial>();var paired=new List<Frame>();proof.normalDiagnosticPerformed=!keywordOnly;proof.normalKeywordDiagnosticPerformed=keywordOnly;proof.normalTexturesUvExact=true;proof.normalNonAmplitudePropertiesExact=true;proof.normalKeywordAllPropertiesExact=keywordOnly;var keywordPayload=keywordOnly?meshBindings.Values.Distinct().ToDictionary(m=>m,StandardLitMeshPayload):null;
                        try
                        {
                            foreach(var material in originals)
                            {
                                var channels=Enumerable.Range(0,4).Select(i=>"_NormalScale"+i).Where(material.HasProperty).ToArray();if(channels.Length==0)continue;
                                var copy=keywordOnly?CreateCavernNormalKeywordDiagnosticMaterial(material):new Material(material){name=material.name+" temporary normal-amplitude diagnostic",hideFlags=HideFlags.HideAndDontSave};copies.Add(material,copy);var scales=channels.Select(material.GetFloat).ToArray();var textures=channels.Select(channel=>{string name="_Normal"+channel.Substring("_NormalScale".Length);return material.HasProperty(name)&&material.GetTexture(name)!=null?AssetDatabase.GetAssetPath(material.GetTexture(name)):"";}).ToArray();
                                proof.normalAuthoredMapsWithNonzeroScale+=Enumerable.Range(0,channels.Length).Count(i=>Mathf.Abs(scales[i])>.00001f&&textures[i].StartsWith("Assets/",StringComparison.Ordinal));if(keywordOnly){proof.normalKeywordsDisabled++;proof.normalKeywordAllPropertiesExact&=CavernNormalKeywordOnlyPropertiesExact(material,copy);}else foreach(string channel in channels){copy.SetFloat(channel,0);if(copy.GetFloat(channel)!=0)throw new InvalidOperationException("Native material normal-map amplitude did not reach zero.");proof.normalScaleChannelsZeroed++;}
                                for(int i=0;i<material.shader.GetPropertyCount();i++)
                                {
                                    string name=material.shader.GetPropertyName(i);if(!keywordOnly&&channels.Contains(name))continue;switch(material.shader.GetPropertyType(i))
                                    {case UnityEngine.Rendering.ShaderPropertyType.Color:proof.normalNonAmplitudePropertiesExact&=material.GetColor(name)==copy.GetColor(name);break;case UnityEngine.Rendering.ShaderPropertyType.Vector:proof.normalNonAmplitudePropertiesExact&=material.GetVector(name)==copy.GetVector(name);break;case UnityEngine.Rendering.ShaderPropertyType.Float:case UnityEngine.Rendering.ShaderPropertyType.Range:proof.normalNonAmplitudePropertiesExact&=material.GetFloat(name)==copy.GetFloat(name);break;case UnityEngine.Rendering.ShaderPropertyType.Int:proof.normalNonAmplitudePropertiesExact&=material.GetInteger(name)==copy.GetInteger(name);break;}
                                }
                                proof.normalNonAmplitudePropertiesExact&=material.renderQueue==copy.renderQueue&&(keywordOnly?CavernNormalKeywordOnlyPropertiesExact(material,copy):material.shaderKeywords.SequenceEqual(copy.shaderKeywords))&&material.enableInstancing==copy.enableInstancing;
                                proof.normalTexturesUvExact&=material.shader==copy.shader&&material.GetTexturePropertyNames().All(name=>material.GetTexture(name)==copy.GetTexture(name)&&material.GetTextureScale(name)==copy.GetTextureScale(name)&&material.GetTextureOffset(name)==copy.GetTextureOffset(name));materialRows.Add(new NormalDiagnosticMaterial{source=AssetDatabase.GetAssetPath(material),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material)),originalJsonSha256=Hash(System.Text.Encoding.UTF8.GetBytes(originalJson[material])),channels=channels,originalScales=scales,normalTextureSources=textures,keywordOnly=keywordOnly,allOtherPropertiesExact=!keywordOnly||CavernNormalKeywordOnlyPropertiesExact(material,copy),originalKeywords=material.shaderKeywords,diagnosticKeywords=copy.shaderKeywords});
                            }
                            proof.normalTemporaryCopies=copies.Count;proof.normalMaterials=materialRows.ToArray();if(copies.Count==0||proof.normalAuthoredMapsWithNonzeroScale==0||!proof.normalTexturesUvExact||!proof.normalNonAmplitudePropertiesExact||keywordOnly&&!proof.normalKeywordAllPropertiesExact)throw new InvalidOperationException("Meaningful normal-map diagnosis requires actual nonzero authored maps and unchanged original shader/albedo/other properties/textures/UVs.");
                            foreach(var pair in bindings)pair.Key.sharedMaterials=pair.Value.Select(m=>copies.TryGetValue(m,out var copy)?copy:m).ToArray();
                            var noNormal=Capture(camera,world,roads,shader,output,keywordOnly?"rounded-entry-normal-keyword-diagnostic":"rounded-entry-normal-diagnostic",keywordOnly?"normalmap-keyword-disabled":"normal-amplitude-none");paired.Add(noNormal);if(keywordOnly)proof.normalKeywordFrames=paired.ToArray();else proof.normalFrames=paired.ToArray();if(noNormal.shaderErrors.Length!=0)throw new InvalidOperationException("Actual saved Digger shader errors in normal-amplitude diagnosis.");if(keywordOnly){proof.normalKeywordAllPropertiesExact&=copies.All(p=>CavernNormalKeywordOnlyPropertiesExact(p.Key,p.Value));if(!proof.normalKeywordAllPropertiesExact)throw new InvalidOperationException("Keyword-only capture changed another original material property.");}
                        }
                        finally
                        {
                            foreach(var pair in bindings)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;foreach(var copy in copies.Values)if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);
                            proof.normalTemporaryCopiesDestroyed=copies.Values.All(m=>m==null);proof.normalRendererBindingsExact=bindings.All(p=>p.Key!=null&&p.Key.sharedMaterials.SequenceEqual(p.Value));proof.normalOriginalMaterialsExact=originalJson.All(p=>p.Key!=null&&EditorJsonUtility.ToJson(p.Key)==p.Value);proof.normalCameraAndLightsExact=cameraJson==EditorJsonUtility.ToJson(camera)+"/"+EditorJsonUtility.ToJson(camera.transform)&&lights.All(p=>p.Key!=null&&EditorJsonUtility.ToJson(p.Key)+"/"+EditorJsonUtility.ToJson(p.Key.transform)==p.Value);proof.normalMeshBindingsExact=meshBindings.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value);proof.normalStateRestored=proof.normalTemporaryCopiesDestroyed&&proof.normalRendererBindingsExact&&proof.normalOriginalMaterialsExact&&proof.normalCameraAndLightsExact&&proof.normalMeshBindingsExact;if(keywordOnly){proof.normalKeywordMeshPayloadExact=keywordPayload.All(p=>p.Key!=null&&p.Value==StandardLitMeshPayload(p.Key));RequireCavernStandardLitNoPropertyBlocks(renderers);proof.normalStateRestored&=proof.normalKeywordMeshPayloadExact;}
                        }
                        if(!proof.normalStateRestored)throw new InvalidOperationException("Read-only normal-map diagnosis failed exact source material/renderer/mesh/light/camera restoration.");
                        var restored=Capture(camera,world,roads,shader,output,keywordOnly?"rounded-entry-normal-keyword-diagnostic":"rounded-entry-normal-diagnostic",keywordOnly?"normalmap-keyword-restored":"normal-amplitude-restored");paired.Add(restored);if(keywordOnly)proof.normalKeywordFrames=paired.ToArray();else proof.normalFrames=paired.ToArray();proof.normalStateRestored&=bindings.All(p=>p.Key.sharedMaterials.SequenceEqual(p.Value))&&originalJson.All(p=>EditorJsonUtility.ToJson(p.Key)==p.Value)&&cameraJson==EditorJsonUtility.ToJson(camera)+"/"+EditorJsonUtility.ToJson(camera.transform)&&lights.All(p=>EditorJsonUtility.ToJson(p.Key)+"/"+EditorJsonUtility.ToJson(p.Key.transform)==p.Value)&&meshBindings.All(p=>p.Key.sharedMesh==p.Value);if(keywordOnly){proof.normalKeywordMeshPayloadExact&=keywordPayload.All(p=>p.Key!=null&&p.Value==StandardLitMeshPayload(p.Key));RequireCavernStandardLitNoPropertyBlocks(renderers);proof.normalStateRestored&=proof.normalKeywordMeshPayloadExact;}if(restored.shaderErrors.Length!=0||!proof.normalStateRestored)throw new InvalidOperationException("Restored normal-pair capture changed original bindings/state or reported shader errors.");
                    }
                }
                proof.frames=frames.ToArray();proof.status="ACTUAL_SAVED_CAVERN_IMAGES_RECORDED_REQUIRES_DIRECT_VISUAL_REVIEW";
            }
            finally
            {
                if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);
                proof.physicsAndHeightsExact=world!=null&&physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject)&&heights.SequenceEqual(world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)));
                ShaderUtil.allowAsyncCompilation=async;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);
                proof.voxelMembershipExact=voxelMembers.SequenceEqual(Directory.GetFiles(data,"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal));proof.sourcesExact=watched.All(p=>File.Exists(IoPath(p.Key))&&Hash(File.ReadAllBytes(IoPath(p.Key)))==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-saved-rounded-cavern-frames.json"),JsonUtility.ToJson(proof,true));
                if(!proof.sourcesExact||!proof.physicsAndHeightsExact||!proof.voxelMembershipExact||proof.shadowDiagnosticPerformed&&(!proof.directionalLightRestored||!proof.shadowCameraPoseExact)||(proof.normalDiagnosticPerformed||proof.normalKeywordDiagnosticPerformed)&&!proof.normalStateRestored||proof.lodDiagnosticPerformed&&!proof.lodStateRestored||proof.neutralDiagnosticPerformed&&!proof.neutralStateRestored||proof.weldedNormalDiagnosticPerformed&&(!proof.weldedNormalStateRestored||!proof.weldedNormalTemporaryObjectsDestroyed))throw new InvalidOperationException("Controlled saved cavern render changed actual dependency/voxel membership/bytes, physical/height state or original light/camera/material/renderer/mesh bindings.");
            }
        }
    }
}
#endif
