using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Explicit appearance migration of the accepted world. Never regenerates
    // terrain, roads, meshes, collision or runtime world/route definitions.
    public static class DragonKingdomWorldVisualMigration
    {
        const string Root="Assets/TruckDragonKingdom/";
        const string Prefab=Root+"Prefabs/Shared Dragon Kingdom.prefab";
        const string Preview=Root+"Scenes/DragonKingdomPreview.unity";
        const string Definition=Root+"Configuration/Dragon Kingdom.asset";
        const string LayerFolder=Root+"Materials/TerrainAppearance";
        const string EnvironmentFolder=Root+"Materials/Environment";
        static readonly string[] LayerSources={
            "Grass Light/Grass_Light_VistaSample.terrainlayer",
            "Grass Dark/Grass_Dark_VistaSample.terrainlayer",
            "Sediment/Sediment_VistaSample.terrainlayer",
            "RockFace/RockFace_VistaSample.terrainlayer"};
        [Serializable] public sealed class TerrainResult
        {
            public string path,heightHashBefore,heightHashAfter;public int width,height,channels,zeroSumCells,nonFinite;
            public float minimumSum=1,maximumSum=1,maximumSumError;public bool changed;
        }
        [Serializable] public sealed class Receipt
        {
            public string scope="ACCEPTED_WORLD_APPEARANCE_ONLY_NOT_VISUAL_WINDOWS_OR_HUMAN_ACCEPTANCE";
            public TerrainResult[] terrains;public int changedMaterials,changedRenderers;
            public bool exactHeightSamplesPreserved,geometryFilesPreserved,physicsAndAnchorsPreserved;
            public string prefabPhysicsHash,previewPhysicsHash;public string[] unchangedGeometryPaths;
        }
        public static Receipt LastReceipt {get;private set;}

        [MenuItem("Truck Sports/Dragon Kingdom/Upgrade accepted terrain and foliage appearance")]
        public static void UpgradeAcceptedTerrainAndFoliage()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required for explicit accepted-world appearance migration.");
            foreach(string path in new[]{Prefab,Preview,Definition})if(!File.Exists(path))throw new InvalidOperationException("Accepted saved world required; no generation: "+path);
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                if(scene.path==Preview)throw new InvalidOperationException("Close the accepted preview before its explicit appearance migration.");
                if(scene.isDirty||string.IsNullOrEmpty(scene.path))throw new InvalidOperationException("Preserve current user scenes before this isolated migration: "+scene.name);
            }
            var setup=EditorSceneManager.GetSceneManagerSetup();
            var geometry=GeometryFiles().ToDictionary(p=>p,p=>FileHash(p));
            PreserveBeforeMigration();
            var receipt=new Receipt{unchangedGeometryPaths=geometry.Keys.ToArray()};
            var world=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                string physics=PhysicsAndAnchorHash(world);receipt.prefabPhysicsHash=physics;
                var terrainData=world.GetComponentsInChildren<Terrain>(true).Select(t=>t.terrainData).Distinct().ToArray();
                if(terrainData.Length!=4)throw new InvalidOperationException("Expected the four accepted Vista TerrainData assets.");
                receipt.terrains=terrainData.Select(RepairTerrainAppearance).ToArray();
                int changed=RepairRendererMaterials(world,receipt);
                if(PhysicsAndAnchorHash(world)!=physics)throw new InvalidOperationException("Appearance migration changed accepted prefab physics/anchors.");
                if(changed>0)PrefabUtility.SaveAsPrefabAsset(world,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(world);}
            Scene preview=default;
            try
            {
                preview=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Additive);
                string physics=ScenePhysicsHash(preview);receipt.previewPhysicsHash=physics;int changed=0;
                foreach(var root in preview.GetRootGameObjects())changed+=RepairRendererMaterials(root,receipt);
                if(ScenePhysicsHash(preview)!=physics)throw new InvalidOperationException("Appearance migration changed accepted preview physics/anchors.");
                if(changed>0&&!EditorSceneManager.SaveScene(preview))throw new InvalidOperationException("Accepted preview material bindings did not save.");
            }
            finally
            {
                if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.CloseScene(preview,true);
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            receipt.exactHeightSamplesPreserved=receipt.terrains.All(t=>t.heightHashBefore==t.heightHashAfter);
            receipt.geometryFilesPreserved=geometry.All(p=>FileHash(p.Key)==p.Value);
            receipt.physicsAndAnchorsPreserved=true;
            if(!receipt.exactHeightSamplesPreserved||!receipt.geometryFilesPreserved)throw new InvalidOperationException("Accepted height/geometry bytes changed during appearance migration.");
            LastReceipt=receipt;string output=Output();Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"accepted-world-appearance.json"),JsonUtility.ToJson(receipt,true));
            Debug.Log("DRAGON_KINGDOM_APPEARANCE_MIGRATED terrains="+receipt.terrains.Length+" materials="+receipt.changedMaterials+" renderers="+receipt.changedRenderers+" exactHeights=true geometryAndPhysics=true");
        }

        public static TerrainResult RepairTerrainAppearance(TerrainData data)=>PaintOwnedTerrainAppearance(data,null);
        public static TerrainResult RepairOuterTerrainAppearance(TerrainData data,string candidateTag)
        {
            if(string.IsNullOrEmpty(candidateTag)||candidateTag.Length>40||candidateTag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Exact bounded outer review candidate tag required.");
            string folder=Root+"ReviewCandidates/"+candidateTag+"/Terrain/";
            if(data==null||!AssetDatabase.GetAssetPath(data).StartsWith(folder,StringComparison.Ordinal))throw new InvalidOperationException("Outer appearance helper only owns the explicitly named new review terrain folder.");
            return PaintOwnedTerrainAppearance(data,folder);
        }
        static TerrainResult PaintOwnedTerrainAppearance(TerrainData data,string extraOwnedFolder)
        {
            string path=AssetDatabase.GetAssetPath(data);
            if(!path.StartsWith(Root+"Terrain/",StringComparison.Ordinal)&&!((path.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)||path.StartsWith(Root+"FunctionalShoulders/",StringComparison.Ordinal))&&path.Contains("/Terrain/"))&&(extraOwnedFolder==null||!path.StartsWith(extraOwnedFolder,StringComparison.Ordinal)))throw new InvalidOperationException("Only owned kingdom terrain may be painted: "+path);
            var result=new TerrainResult{path=path,heightHashBefore=HeightHash(data),width=data.alphamapWidth,height=data.alphamapHeight,channels=4};
            var layers=OwnedLayers();var weights=new float[result.height,result.width,4];
            for(int z=0;z<result.height;z++)for(int x=0;x<result.width;x++)
            {
                float u=(float)x/(result.width-1),v=(float)z/(result.height-1);
                float slope=data.GetSteepness(u,v),height=data.GetInterpolatedHeight(u,v)-20;
                // Existing heightfield alone controls the blend. Steep grading
                // cuts receive actual rock texture instead of stretched grass.
                float rock=Mathf.SmoothStep(0,1,Mathf.InverseLerp(24,56,slope));
                float upland=Mathf.SmoothStep(0,1,Mathf.InverseLerp(150,330,height));
                rock=Mathf.Max(rock,upland*.34f);
                float sediment=(1-rock)*Mathf.Lerp(.16f,.42f,upland);
                float grass=1-rock-sediment;
                float variation=.5f+.5f*Mathf.Sin((u*data.size.x+v*data.size.z)*.013f);
                float dark=grass*Mathf.Lerp(.25f,.55f,variation);
                var values=new[]{grass-dark,dark,sediment,rock};
                // Quantize the normalized inputs by largest remainder so the
                // persisted four UNorm8 channels can sum to exactly255.
                var bytes=new int[4];var remainders=new float[4];int used=0;
                float sum=values.Sum();
                for(int i=0;i<4;i++){float scaled=Mathf.Clamp01(values[i]/sum)*255;bytes[i]=Mathf.FloorToInt(scaled);remainders[i]=scaled-bytes[i];used+=bytes[i];}
                while(used++<255){int best=0;for(int i=1;i<4;i++)if(remainders[i]>remainders[best])best=i;bytes[best]++;remainders[best]=-1;}
                for(int i=0;i<4;i++)weights[z,x,i]=bytes[i]/255f;
            }
            bool same=data.terrainLayers.SequenceEqual(layers);
            if(same)
            {
                var old=data.GetAlphamaps(0,0,result.width,result.height);
                for(int z=0;same&&z<result.height;z++)for(int x=0;same&&x<result.width;x++)for(int i=0;i<4;i++)if(Mathf.Abs(old[z,x,i]-weights[z,x,i])>1e-5f){same=false;break;}
            }
            if(!same){data.terrainLayers=layers;data.SetAlphamaps(0,0,weights);data.SetBaseMapDirty();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);result.changed=true;}
            var persisted=data.GetAlphamaps(0,0,result.width,result.height);
            for(int z=0;z<result.height;z++)for(int x=0;x<result.width;x++)
            {
                float sum=0;for(int i=0;i<4;i++){float w=persisted[z,x,i];if(float.IsNaN(w)||float.IsInfinity(w)||w<0||w>1)result.nonFinite++;sum+=w;}
                if(sum<=1e-6f)result.zeroSumCells++;result.minimumSum=Mathf.Min(result.minimumSum,sum);result.maximumSum=Mathf.Max(result.maximumSum,sum);result.maximumSumError=Mathf.Max(result.maximumSumError,Mathf.Abs(sum-1));
            }
            result.heightHashAfter=HeightHash(data);
            if(result.nonFinite>0||result.zeroSumCells>0||result.maximumSumError>2f/255+.00001f||result.heightHashBefore!=result.heightHashAfter)
                throw new InvalidOperationException("Terrain appearance failed normalized-weight/exact-height gates: "+JsonUtility.ToJson(result));
            return result;
        }

        static TerrainLayer[] OwnedLayers()
        {
            EnsureFolder(LayerFolder);var result=new List<TerrainLayer>();
            for(int i=0;i<LayerSources.Length;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/"+LayerSources[i]);
                if(source==null||source.diffuseTexture==null)throw new InvalidOperationException("Owned modern Vista diffuse layer required: "+LayerSources[i]);
                string path=LayerFolder+"/Kingdom "+i+" "+source.name+".terrainlayer";
                var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if(layer==null){layer=UnityEngine.Object.Instantiate(source);layer.name="Kingdom "+source.name;layer.normalScale=.55f;layer.metallic=0;layer.smoothness=.08f;layer.tileSize=i==3?new Vector2(16,16):new Vector2(10,10);AssetDatabase.CreateAsset(layer,path);}
                if(layer.diffuseTexture==null)throw new InvalidOperationException("A migrated layer has no real diffuse texture: "+path);
                result.Add(layer);
            }
            return result.ToArray();
        }

        public static int RepairRendererMaterials(GameObject root,Receipt receipt)
        {
            int count=0;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    var current=materials[i];if(current==null)throw new InvalidOperationException("Missing accepted renderer material: "+renderer.name);
                    string path=AssetDatabase.GetAssetPath(current),sourcePath=path;
                    if(path.StartsWith(EnvironmentFolder+"/",StringComparison.Ordinal))
                    {string guid=Path.GetFileName(path).Split(' ')[0];if(guid.Length==32)sourcePath=AssetDatabase.GUIDToAssetPath(guid);}
                    var source=AssetDatabase.LoadAssetAtPath<Material>(sourcePath);if(source==null)continue;
                    bool foliage=sourcePath.IndexOf("Fantastic Ancient Forest",StringComparison.Ordinal)>=0&&
                        (source.name.IndexOf("leaves",StringComparison.OrdinalIgnoreCase)>=0||source.name.IndexOf("plants",StringComparison.OrdinalIgnoreCase)>=0||
                         source.shader!=null&&source.shader.name.IndexOf("foliage",StringComparison.OrdinalIgnoreCase)>=0);
                    if(!foliage&&!path.StartsWith(EnvironmentFolder+"/",StringComparison.Ordinal))continue;
                    if(!foliage&&!HasAuthoredAlbedo(source))continue; // intentional solid-color vendor materials
                    string target=EnvironmentFolder+"/"+AssetDatabase.AssetPathToGUID(sourcePath)+" URP.mat";
                    var replacement=ConfigureOwnedMaterial(source,target,foliage,out bool materialChanged);
                    if(materialChanged)receipt.changedMaterials++;
                    if(replacement!=current){materials[i]=replacement;changed=true;}
                }
                if(changed){renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);count++;receipt.changedRenderers++;}
            }
            return count;
        }

        public static Material ConfigureOwnedMaterial(Material source,string path,bool foliage,out bool changed)
        {
            if(IsVendorTriplanarFamily(source))return ConfigureVendorTriplanar(source,path,out changed);
            if(IsVendorForestSurfaceFamily(source))return ConfigureVendorForestSurface(source,path,out changed);
            EnsureFolder(EnvironmentFolder);var variant=AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created=variant==null;
            if(created)
            {
                bool vendorFoliage=foliage&&HasSupportedUrpShader(source);
                variant=vendorFoliage?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));
                variant.name=source.name+" kingdom URP";AssetDatabase.CreateAsset(variant,path);
            }
            string before=EditorJsonUtility.ToJson(variant);
            if(foliage&&HasSupportedUrpShader(source)&&variant.shader!=source.shader)
            {variant.shader=source.shader;variant.CopyPropertiesFromMaterial(source);}
            string property=ActualTextureProperty(source,new[]{"_BaseMap","_BaseTexture","_Albedo","_MainTex"});
            Texture albedo=property==null?null:SavedTexture(source,property);
            if(albedo==null)throw new InvalidOperationException("Owned source has no actual albedo binding: "+AssetDatabase.GetAssetPath(source));
            if(variant.HasProperty("_BaseMap")){variant.SetTexture("_BaseMap",albedo);variant.SetTextureScale("_BaseMap",SavedScale(source,property));}
            if(variant.HasProperty("_BaseTexture"))variant.SetTexture("_BaseTexture",albedo);
            Color tint=source.HasProperty("_BaseTexColorTint")?source.GetColor("_BaseTexColorTint"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white;tint.a=1;
            if(variant.HasProperty("_BaseColor"))variant.SetColor("_BaseColor",tint);
            string normalProperty=ActualTextureProperty(source,new[]{"_NormalTexture","_Normal","_BumpMap","_NormalMap"});
            var normal=normalProperty==null?null:SavedTexture(source,normalProperty);
            if(variant.HasProperty("_BumpMap")&&normal!=null){variant.SetTexture("_BumpMap",normal);variant.EnableKeyword("_NORMALMAP");}
            if(variant.HasProperty("_Smoothness"))variant.SetFloat("_Smoothness",.12f);
            if(foliage)
            {
                variant.EnableKeyword("_ALPHATEST_ON");variant.SetOverrideTag("RenderType","TransparentCutout");variant.renderQueue=2450;
                foreach(string p in new[]{"_AlphaClip","_AlphaClipThreshold","_Cutoff"})if(variant.HasProperty(p))variant.SetFloat(p,p=="_AlphaClip"?1:.5f);
                if(variant.HasProperty("_Cull"))variant.SetFloat("_Cull",0);
                variant.doubleSidedGI=true;
            }
            changed=created||before!=EditorJsonUtility.ToJson(variant);
            if(changed){EditorUtility.SetDirty(variant);AssetDatabase.SaveAssetIfDirty(variant);}
            return variant;
        }
        static SerializedProperty SavedTextureEntry(Material source,string property)
        {var a=new SerializedObject(source).FindProperty("m_SavedProperties.m_TexEnvs");for(int i=0;i<a.arraySize;i++){var e=a.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("first").stringValue==property)return e.FindPropertyRelative("second");}return null;}
        static Texture SavedTexture(Material source,string p)=>SavedTextureEntry(source,p)?.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
        static string ActualTextureProperty(Material source,string[] properties)
        {
            // Vendor materials retain obsolete serialized fields across shader
            // migrations (Polyart has old tree-bark _MainTex alongside its real
            // _Albedo). Prefer what the active vendor shader actually samples.
            foreach(string p in properties)if(source.HasProperty(p)&&SavedTexture(source,p)!=null)return p;
            foreach(string p in properties)if(SavedTexture(source,p)!=null)return p;
            return null;
        }
        public static bool HasAuthoredAlbedo(Material source)=>source!=null&&(new[]{"_BaseMap","_BaseTexture","_MainTex","_Albedo"}.Any(p=>SavedTexture(source,p)!=null)||IsVendorTriplanarFamily(source)&&new[]{"_DetailTexture","_TopTexture"}.Any(p=>SavedTexture(source,p)!=null));
        // Material.GetTag consults its currently selected SubShader, which can
        // be unset before the first native render. Inspect the actual imported
        // Shader tags during stopped authoring instead; rendering is verified
        // separately against the unmodified vendor material and mesh.
        public static bool HasSupportedUrpShader(Material source)
        {
            if(source==null||source.shader==null||!source.shader.isSupported)return false;
            var tag=new UnityEngine.Rendering.ShaderTagId("RenderPipeline");
            for(int index=0;index<source.shader.subshaderCount;index++)
                if(source.shader.FindSubshaderTagValue(index,tag).name=="UniversalPipeline")return true;
            return false;
        }
        static bool IsVendorTriplanarFamily(Material source)=>source!=null&&source.shader!=null&&source.shader.name.StartsWith("TidalFlask/Triplanar TopProject URP Advanced",StringComparison.Ordinal);
        static bool IsVendorForestSurfaceFamily(Material source)=>source!=null&&source.shader!=null&&
            AssetDatabase.GetAssetPath(source).StartsWith("Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Fantastic Ancient Forest/Materials/",StringComparison.Ordinal)&&
            (source.shader.name.StartsWith("TidalFlask/Foliage Wind URP Advanced",StringComparison.Ordinal)||source.shader.name.StartsWith("TidalFlask/PBR Vertex Color Fade URP",StringComparison.Ordinal));
        static Material ConfigureVendorForestSurface(Material source,string path,out bool changed)
        {
            if(!HasSupportedUrpShader(source)||!HasAuthoredAlbedo(source))throw new InvalidOperationException("Actual supported owned forest URP surface and authored texture required; no generic fallback: "+AssetDatabase.GetAssetPath(source));
            if(string.IsNullOrEmpty(path)||!path.StartsWith("Assets/",StringComparison.Ordinal)||AssetDatabase.GetAssetPath(source)==path)throw new InvalidOperationException("Separate persistent project-owned forest material required; preserve vendor source.");
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));var target=AssetDatabase.LoadAssetAtPath<Material>(path);bool created=target==null;
            if(created&&File.Exists(path))throw new InvalidOperationException("Preserve existing non-Material forest file: "+path);
            if(created){target=new Material(source){name=source.name+" kingdom vendor URP"};AssetDatabase.CreateAsset(target,path);}
            string before=EditorJsonUtility.ToJson(target);
            if(!SameForestMaterialData(source,target))
            {
                // Preserve every serialized shader property, texture transform,
                // keyword, tag, queue and pass setting, including vertex-color
                // fade and wind/cutout. Retain only the owned asset's name/GUID.
                string name=target.name;EditorUtility.CopySerialized(source,target);target.name=name;
            }
            string difference=ForestMaterialDifference(source,target);
            if(difference!=null)throw new InvalidOperationException("Actual vendor forest surface settings changed during owned copy: "+path+"; "+difference);
            changed=created||before!=EditorJsonUtility.ToJson(target);if(changed){EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);}return target;
        }
        static bool SameForestMaterialData(Material source,Material target)=>ForestMaterialDifference(source,target)==null;
        static string ForestMaterialDifference(Material source,Material target)
        {
            using(var original=new SerializedObject(source))using(var copy=new SerializedObject(target))
            {
                var left=original.GetIterator();var right=copy.GetIterator();bool enterChildren=true;
                while(true)
                {
                    bool a=left.Next(enterChildren),b=right.Next(enterChildren);enterChildren=true;
                    if(a!=b)return "different serialized field count";if(!a)return null;
                    if(left.propertyPath!=right.propertyPath||left.propertyType!=right.propertyType)return "schema "+left.propertyPath+"/"+left.propertyType+" != "+right.propertyPath+"/"+right.propertyType;
                    // A retained owned name includes its string subtree. Do not
                    // accidentally compare name characters on Next(true).
                    if(left.propertyPath=="m_Name"){enterChildren=false;continue;}
                    if(left.propertyType==SerializedPropertyType.Generic)continue;
                    if(!SerializedProperty.DataEquals(left,right))return left.propertyPath+" ("+left.propertyType+") source="+ForestPropertyValue(left)+" target="+ForestPropertyValue(right);
                }
            }
        }
        static string ForestPropertyValue(SerializedProperty value)
        {
            if(value.propertyType==SerializedPropertyType.ObjectReference){var obj=value.objectReferenceValue;return obj==null?"null":AssetDatabase.GetAssetPath(obj)+"#"+obj.name+" instance="+obj.GetInstanceID();}
            try{return value.boxedValue?.ToString()??"null";}catch(Exception){return "unavailable";}
        }
        static Material ConfigureVendorTriplanar(Material source,string path,out bool changed)
        {
            if(!HasSupportedUrpShader(source))throw new InvalidOperationException("Installed original vendor URP triplanar shader must be supported; no generic white fallback: "+AssetDatabase.GetAssetPath(source)+" supported="+source.shader.isSupported+" tag="+source.GetTag("RenderPipeline",false,"")+" pipeline="+UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline+" shaderMessages="+string.Join(" | ",ShaderUtil.GetShaderMessages(source.shader).Select(message=>message.severity+": "+message.message+" "+message.file+":"+message.line)));
            if(!HasAuthoredAlbedo(source))throw new InvalidOperationException("Actual vendor triplanar detail/top texture required: "+AssetDatabase.GetAssetPath(source));
            if(string.IsNullOrEmpty(path)||!path.StartsWith("Assets/",StringComparison.Ordinal)||AssetDatabase.GetAssetPath(source)==path)throw new InvalidOperationException("A separate persistent project-owned material path is required; preserve original vendor source.");
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));var variant=AssetDatabase.LoadAssetAtPath<Material>(path);bool created=variant==null;
            if(created&&File.Exists(path))throw new InvalidOperationException("Preserve existing non-Material file; no overwrite: "+path);
            if(created){variant=new Material(source){name=source.name+" kingdom vendor URP"};AssetDatabase.CreateAsset(variant,path);}
            string before=EditorJsonUtility.ToJson(variant);
            if(!VendorPropertiesMatch(source,variant))
            {
                variant.shader=source.shader;variant.CopyPropertiesFromMaterial(source);
                variant.enableInstancing=source.enableInstancing;variant.doubleSidedGI=source.doubleSidedGI;variant.globalIlluminationFlags=source.globalIlluminationFlags;
            }
            if(!VendorPropertiesMatch(source,variant))throw new InvalidOperationException("Original vendor surface properties were not preserved: "+path);
            changed=created||before!=EditorJsonUtility.ToJson(variant);
            if(changed){EditorUtility.SetDirty(variant);AssetDatabase.SaveAssetIfDirty(variant);}
            return variant;
        }
        static bool VendorPropertiesMatch(Material source,Material target)
        {
            if(target.shader!=source.shader||target.renderQueue!=source.renderQueue||target.enableInstancing!=source.enableInstancing||target.doubleSidedGI!=source.doubleSidedGI||target.globalIlluminationFlags!=source.globalIlluminationFlags||!new HashSet<string>(target.shaderKeywords).SetEquals(source.shaderKeywords))return false;
            for(int index=0;index<source.shader.GetPropertyCount();index++)
            {
                string name=source.shader.GetPropertyName(index);
                switch(source.shader.GetPropertyType(index))
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:if(!target.GetColor(name).Equals(source.GetColor(name)))return false;break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:if(!target.GetVector(name).Equals(source.GetVector(name)))return false;break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        if(target.GetTexture(name)!=source.GetTexture(name)||!target.GetTextureScale(name).Equals(source.GetTextureScale(name))||!target.GetTextureOffset(name).Equals(source.GetTextureOffset(name)))return false;break;
                    default:if(!target.GetFloat(name).Equals(source.GetFloat(name)))return false;break;
                }
            }
            return true;
        }
        static Vector2 SavedScale(Material source,string p)=>SavedTextureEntry(source,p)?.FindPropertyRelative("m_Scale").vector2Value??Vector2.one;
        static void EnsureFolder(string path){if(!Directory.Exists(path)){Directory.CreateDirectory(path);AssetDatabase.Refresh();}}
        public static string HeightHash(TerrainData data)
        {var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);var bytes=new byte[heights.Length*4];Buffer.BlockCopy(heights,0,bytes,0,bytes.Length);return Hash(bytes);}
        public static string PhysicsAndAnchorHash(GameObject root)
        {
            var b=new StringBuilder();
            foreach(var t in root.GetComponentsInChildren<Transform>(true))b.Append(t.name).Append('|').Append(EditorJsonUtility.ToJson(t)).Append('\n');
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))b.Append(collider.GetType().FullName).Append(EditorJsonUtility.ToJson(collider)).Append('\n');
            foreach(var body in root.GetComponentsInChildren<Rigidbody>(true))b.Append(EditorJsonUtility.ToJson(body)).Append('\n');
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))b.Append(AssetDatabase.GetAssetPath(mesh.sharedMesh)).Append('|').Append(mesh.sharedMesh==null?0:mesh.sharedMesh.vertexCount).Append('\n');
            return Hash(Encoding.UTF8.GetBytes(b.ToString()));
        }
        static string ScenePhysicsHash(Scene scene)=>Hash(Encoding.UTF8.GetBytes(string.Join("|",scene.GetRootGameObjects().Select(PhysicsAndAnchorHash))));
        static IEnumerable<string> GeometryFiles()=>Directory.GetFiles(Root+"Models","*",SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta")).Concat(new[]{Definition});
        static void PreserveBeforeMigration()
        {
            // Keep the actual original asset bytes reviewable even if a later
            // material/weight assertion fails. Never overwrite an earlier copy.
            string folder=Path.Combine(Output(),"before-appearance");
            foreach(string path in Directory.GetFiles(Root+"Terrain","*",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Root+"Materials","*",SearchOption.AllDirectories)).Concat(new[]{Prefab,Preview}))
            {
                string target=Path.Combine(folder,path.Replace('/',Path.DirectorySeparatorChar));
                if(File.Exists(target))continue;Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,false);
            }
        }
        static string FileHash(string path)=>Hash(File.ReadAllBytes(path));
        static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static string Output(){string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):"Logs/TruckSports/DragonKingdom/AppearanceMigration";}
    }
}
