#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string TowerDetailVendorGraph="Assets/LeartesStudios/Garage Warehouse Environment/Art/ShaderGraphs/M_Basic_PBR.shadergraph";
        const string TowerDetailSourceLayer=DragonKingdomWorldAuthoring.Art+"Polyart Studio/HandpaintedEnvironmentVol1/Terrain/Rock.terrainlayer";
        const string TowerDetailMeshGuid="5c2734c723764924ebd0458ab8293423";
        const string TowerDetailAtlasGuid="370214d29559d094bbd95faa22ad92b7";
        [Serializable]sealed class TowerAppearanceParent{public string tag,prefab,preview,status;public bool originalPhysicalGatesPassed,sourceBytesPreserved;public TowerAppearanceCavern cavern;}
        [Serializable]sealed class TowerAppearanceCavern{public string dataFolder;}
        [Serializable]public sealed class TowerAppearanceFrame{public string intervention,png,sha256;public int width,height;public Vector3 position,target;public string[] shaderErrors;}
        [Serializable]public sealed class TowerAppearanceMeshProof{public string source,sourceGuid;public bool originalUv1Usable,generatedSecondaryUv,orientedTriangleCornersExceptUv1Exact;public int sourceVertices,reviewVertices;}
        [Serializable]public sealed class TowerAppearanceMaterialProof{public string source,sourceShader,vendorShader,atlas,atlasGuid;public Color sourceColor;public float sourceSmoothness,sourceMetallic;public bool atlasTransformRetained;}
        [Serializable]public sealed class TowerAppearanceSource{public string path,resolvedPath,sha256,resourceKind,packageName,packageVersion;}
        [Serializable]public sealed class TowerAppearanceReview
        {
            public string scope="TEMPORARY_VENDOR_DETAIL_MATERIAL_AND_RENDER_ONLY_UV_REVIEW_NOT_WORLD_ADOPTION_OR_FINISHED_APPEARANCE_ACCEPTANCE",candidate,status="IN_PROGRESS",error,vendorGraph;
            public float detailMask=.12f,actualQuantizedDetailMask,detailUvRepeat=12;public int towers,perches;public bool originalAtlasColorPreserved,allColliderReferencesAndStateExact,allHeightfieldsExact,allPerchReferencesAndPosesExact,originalWorldStateRestored,sourceBytesExact,temporaryObjectsDestroyed,incomingSceneContextRestored;
            public TowerAppearanceMeshProof[] meshes;public TowerAppearanceMaterialProof[] materials;public TowerAppearanceSource[] watchedSources;public TowerAppearanceFrame[] frames;public string[] changedSources;
        }
        public static void ValidateTowerDetailParameters(float mask,float repeat)
        {if(!float.IsFinite(mask)||mask<0||mask>.2f||!float.IsFinite(repeat)||repeat<4||repeat>32)throw new ArgumentException("Retained-tower comparison accepts bounded0–.2detail and4–32UV repeat only.");}
        public sealed class TowerDetailUvBinding:IDisposable
        {
            public Mesh mesh;public bool originalUv1Usable,generatedSecondaryUv,orientedTriangleCornersExceptUv1Exact;
            public void Dispose(){if(generatedSecondaryUv&&mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);}
        }
        static int TowerAttributeBytes(VertexAttributeFormat format)
        {
            switch(format){case VertexAttributeFormat.Float32:case VertexAttributeFormat.UInt32:case VertexAttributeFormat.SInt32:return 4;case VertexAttributeFormat.Float16:case VertexAttributeFormat.UNorm16:case VertexAttributeFormat.SNorm16:case VertexAttributeFormat.UInt16:case VertexAttributeFormat.SInt16:return 2;case VertexAttributeFormat.UNorm8:case VertexAttributeFormat.SNorm8:case VertexAttributeFormat.UInt8:case VertexAttributeFormat.SInt8:return 1;default:throw new InvalidOperationException("Unsupported native attribute format; no approximate mesh comparison.");}
        }
        static string[] TowerCornerRecords(Mesh mesh,bool omitUv1)
        {
            var layout=mesh.GetVertexAttributes().Where(a=>!omitUv1||a.attribute!=VertexAttribute.TexCoord1).OrderBy(a=>(int)a.attribute).ToArray();
            string header=string.Join("|",layout.Select(a=>a.attribute+":"+a.format+":"+a.dimension));var records=new List<string>();
            using(var access=MeshUtility.AcquireReadOnlyMeshData(mesh))
            {
                var data=access[0];var streams=Enumerable.Range(0,mesh.vertexBufferCount).Select(i=>data.GetVertexData<byte>(i).ToArray()).ToArray();var attributes=new string[mesh.vertexCount];
                for(int v=0;v<attributes.Length;v++)
                {
                    using(var bytes=new MemoryStream())
                    {foreach(var a in layout){int start=v*mesh.GetVertexBufferStride(a.stream)+mesh.GetVertexAttributeOffset(a.attribute),length=TowerAttributeBytes(a.format)*a.dimension;bytes.Write(streams[a.stream],start,length);}attributes[v]=Convert.ToBase64String(bytes.ToArray());}
                }
                int[] indices=mesh.indexFormat==IndexFormat.UInt16?data.GetIndexData<ushort>().Select(v=>(int)v).ToArray():data.GetIndexData<int>().ToArray();
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    var section=data.GetSubMesh(sub);if(section.topology!=MeshTopology.Triangles||section.indexCount%3!=0)throw new InvalidOperationException("Complete oriented triangle submeshes required.");
                    for(int i=0;i<section.indexCount;i+=3)
                    {
                        var a=attributes[indices[section.indexStart+i]+section.baseVertex];var b=attributes[indices[section.indexStart+i+1]+section.baseVertex];var c=attributes[indices[section.indexStart+i+2]+section.baseVertex];
                        var oriented=new[]{a+"|"+b+"|"+c,b+"|"+c+"|"+a,c+"|"+a+"|"+b};Array.Sort(oriented,StringComparer.Ordinal);records.Add(header+"#"+sub+"#"+oriented[0]);
                    }
                }
            }
            records.Sort(StringComparer.Ordinal);return records.ToArray();
        }
        public static bool TowerMeshOnlyDetailUvDiffers(Mesh source,Mesh review)=>source!=null&&review!=null&&source.subMeshCount==review.subMeshCount&&source.bounds==review.bounds&&TowerCornerRecords(source,true).SequenceEqual(TowerCornerRecords(review,true));
        static bool TowerNativeUv1Usable(Mesh mesh)
        {
            if(!mesh.HasVertexAttribute(VertexAttribute.TexCoord1)||mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord1)<2)return false;
            using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))using(var uv=new Unity.Collections.NativeArray<Vector2>(mesh.vertexCount,Unity.Collections.Allocator.Temp))
            {
                data[0].GetUVs(1,uv);if(uv.Length==0)return false;foreach(var p in uv)if(!float.IsFinite(p.x)||!float.IsFinite(p.y))return false;
                int[] indices=mesh.indexFormat==IndexFormat.UInt16?data[0].GetIndexData<ushort>().Select(v=>(int)v).ToArray():data[0].GetIndexData<int>().ToArray();int usable=0;
                for(int sub=0;sub<mesh.subMeshCount;sub++){var section=data[0].GetSubMesh(sub);if(section.topology!=MeshTopology.Triangles||section.indexCount%3!=0)return false;for(int i=0;i<section.indexCount;i+=3){var a=uv[indices[section.indexStart+i]+section.baseVertex];var b=uv[indices[section.indexStart+i+1]+section.baseVertex];var c=uv[indices[section.indexStart+i+2]+section.baseVertex];var x=b-a;var y=c-a;if(Mathf.Abs(x.x*y.y-x.y*y.x)<=1e-12f)return false;usable++;}}return usable>0;
            }
        }
        public static TowerDetailUvBinding CreateTowerDetailUvBinding(Mesh source)
        {
            if(source==null||source.vertexCount==0)throw new ArgumentException("Populated original render mesh required.");var proof=new TowerDetailUvBinding{mesh=source,originalUv1Usable=TowerNativeUv1Usable(source),orientedTriangleCornersExceptUv1Exact=true};
            if(proof.originalUv1Usable)return proof;
            var before=TowerCornerRecords(source,false);proof.mesh=UnityEngine.Object.Instantiate(source);proof.mesh.name=source.name+" TEMP vendor-detail UV1";proof.mesh.hideFlags=HideFlags.HideAndDontSave;proof.generatedSecondaryUv=true;
            try
            {
                if(!Unwrapping.GenerateSecondaryUVSet(proof.mesh)||!TowerNativeUv1Usable(proof.mesh))throw new InvalidOperationException("Unity's native secondary-UV unwrapper did not produce usable detail coordinates.");
                proof.orientedTriangleCornersExceptUv1Exact=TowerMeshOnlyDetailUvDiffers(source,proof.mesh);
                if(!proof.orientedTriangleCornersExceptUv1Exact||!before.SequenceEqual(TowerCornerRecords(source,false)))throw new InvalidOperationException("Native UV preparation changed original oriented geometry/UV0/normals/tangents/colors/source data.");return proof;
            }
            catch{proof.Dispose();throw;}
        }
        public static string[] TowerDetailVendorProperties()=>new[]{"_Base_Color","_ORM","_Normal","_Normal_Strength","_UV0_Scale","_UV0_U_Offset","_UV0_V_Offset","_MinRoughness","_MaxRoughness","_Roughness_Strength","_UV1_Scale","_UV1_U_Offset","_UV1_V_Offset","_Enable_Secondary_Material","_Use_Coord_Index_1_for_Secondary_Material","_Use_Coord_Index_1_for_Secondary_Material_Mask","_Secondary_Base_Color","_Secondary_Normals","_Secondary_ARM","_Secondary_Material_Mask","_Enable_Color_Tint","_Enable_Emission","_Enable_Rim_Color","_Enable_Opacity_Mask","_Use_Opacity_from_Alpha_channel","_Enable_Paint_Tint_from_Alpha","_Dust_on_top"};
        public static TowerAppearanceSource ResolveTowerAppearanceSource(string path)
        {
            if(string.IsNullOrWhiteSpace(path))throw new InvalidOperationException("Empty dependency must not be silently omitted.");
            if(path=="Resources/unity_builtin_extra"||path=="Library/unity default resources")return new TowerAppearanceSource{path=path,resourceKind="UNITY_BUILTIN_VIRTUAL_RESOURCE_NO_DISK_FILE"};
            var row=new TowerAppearanceSource{path=path,resourceKind="EXACT_DISK_SOURCE",resolvedPath=path=="Packages/manifest.json"||path=="Packages/packages-lock.json"?Io(path):CavernJoinPhysicalSource(path)};
            if(path.StartsWith("Packages/",StringComparison.Ordinal)&&path!="Packages/manifest.json"&&path!="Packages/packages-lock.json")
            {
                var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);if(package==null)throw new InvalidOperationException("Exact registered package required: "+path);row.packageName=package.name;row.packageVersion=package.version;row.resourceKind="REGISTERED_PACKAGE_DISK_SOURCE";
            }
            if(!File.Exists(row.resolvedPath))throw new InvalidOperationException("Required real dependency is missing at its exact resolved path: "+path+" -> "+row.resolvedPath);
            row.sha256=TowerFileHash(row.resolvedPath);return row;
        }
        static void TowerWatchSource(Dictionary<string,TowerAppearanceSource> sources,string path,bool meta)
        {
            if(sources.ContainsKey(path))return;var row=ResolveTowerAppearanceSource(path);sources.Add(path,row);
            if(meta&&row.resourceKind!="UNITY_BUILTIN_VIRTUAL_RESOURCE_NO_DISK_FILE")TowerWatchSource(sources,path+".meta",false);
        }
        static void TowerWatchPipelineShaderSources(Dictionary<string,TowerAppearanceSource> sources)
        {
            // Shader Graph's generated passes also compile package includes which
            // AssetDatabase graph references need not enumerate. Snapshot the
            // actual registered shader/include/template source closure, not a
            // guessed PackageCache directory or an invented replacement shader.
            // Unity-excluded ~ documentation/sample folders are not imported
            // shader dependencies; registered dependencies above remain strict.
            var extensions=new HashSet<string>(new[]{".hlsl",".shader",".compute",".template",".shadergraph",".shadersubgraph",".cginc",".cg",".glsl",".glslinc"},StringComparer.OrdinalIgnoreCase);
            foreach(string name in new[]{"com.unity.render-pipelines.universal","com.unity.render-pipelines.core","com.unity.shadergraph"})
            {
                string assetRoot="Packages/"+name;var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetRoot);
                if(package==null||package.name!=name||package.assetPath!=assetRoot||string.IsNullOrEmpty(package.resolvedPath))throw new InvalidOperationException("Actual registered rendering package required: "+assetRoot);
                TowerWatchSource(sources,assetRoot+"/package.json",false);
                var shaderFiles=Directory.GetFiles(package.resolvedPath,"*",SearchOption.AllDirectories).Where(f=>extensions.Contains(Path.GetExtension(f))&&!f.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Any(part=>part.EndsWith("~",StringComparison.Ordinal))).ToArray();if(shaderFiles.Length==0)throw new InvalidOperationException("Actual package shader sources missing: "+assetRoot);
                foreach(string file in shaderFiles)TowerWatchSource(sources,assetRoot+"/"+file.Substring(package.resolvedPath.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Length+1).Replace('\\','/'),true);
            }
            TowerWatchSource(sources,"Packages/manifest.json",false);TowerWatchSource(sources,"Packages/packages-lock.json",false);
        }
        static Material TowerVendorDetailMaterial(Material source,Shader shader,Texture2D mask,Texture2D orm,TerrainLayer rock,bool detail)
        {
            ValidateTowerDetailParameters(.12f,12);var atlasProperties=new[]{"_BaseMap","_MainTex"}.Where(p=>source.HasProperty(p)&&source.GetTexture(p)!=null).ToArray();var atlases=atlasProperties.Select(source.GetTexture).Distinct().ToArray();var atlas=atlases.Length==1?atlases[0]:null;
            var color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white;
            if(atlas==null||AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(atlas))!=TowerDetailAtlasGuid||color!=Color.white||source.renderQueue>=2500||atlasProperties.Any(p=>source.GetTextureScale(p)!=Vector2.one||source.GetTextureOffset(p)!=Vector2.zero))throw new InvalidOperationException("Only actual unchanged opaque white-tinted source palette towers are eligible; preserve custom materials/atlas transforms.");
            float smoothness=source.HasProperty("_Smoothness")?source.GetFloat("_Smoothness"):source.HasProperty("_Glossiness")?source.GetFloat("_Glossiness"):.2f;float metallic=source.HasProperty("_Metallic")?source.GetFloat("_Metallic"):0;
            if(!float.IsFinite(smoothness)||smoothness<0||smoothness>1||metallic!=0)throw new InvalidOperationException("The opaque nonmetal source's exact roughness must remain representable by this bounded review.");
            var material=new Material(shader){name="TEMP retained tower "+(detail?"painted detail12percent":"vendor neutral"),hideFlags=HideFlags.HideAndDontSave,renderQueue=source.renderQueue,enableInstancing=source.enableInstancing,doubleSidedGI=source.doubleSidedGI};
            try
            {
                foreach(string property in TowerDetailVendorProperties())if(!material.HasProperty(property))throw new InvalidOperationException("Actual installed vendor shader property missing: "+property);
                foreach(string property in new[]{"_Enable_Color_Tint","_Enable_Emission","_Enable_Rim_Color","_Enable_Opacity_Mask","_Use_Opacity_from_Alpha_channel","_Enable_Paint_Tint_from_Alpha","_Dust_on_top"})material.SetFloat(property,0);
                material.SetTexture("_Base_Color",atlas);material.SetTexture("_ORM",orm);material.SetTexture("_Normal",Texture2D.normalTexture);material.SetFloat("_Normal_Strength",0);material.SetFloat("_UV0_Scale",1);material.SetFloat("_UV0_U_Offset",0);material.SetFloat("_UV0_V_Offset",0);material.SetFloat("_MinRoughness",1-smoothness);material.SetFloat("_MaxRoughness",1-smoothness);material.SetFloat("_Roughness_Strength",1);
                material.SetFloat("_Enable_Secondary_Material",detail?1:0);material.SetFloat("_Use_Coord_Index_1_for_Secondary_Material",1);material.SetFloat("_Use_Coord_Index_1_for_Secondary_Material_Mask",1);material.SetFloat("_UV1_Scale",12);material.SetFloat("_UV1_U_Offset",0);material.SetFloat("_UV1_V_Offset",0);
                material.SetTexture("_Secondary_Base_Color",rock.diffuseTexture);material.SetTexture("_Secondary_Normals",Texture2D.normalTexture);material.SetTexture("_Secondary_ARM",orm);material.SetTexture("_Secondary_Material_Mask",mask);
                if(material.GetTexture("_Base_Color")!=atlas||material.GetFloat("_UV0_Scale")!=1||material.GetFloat("_UV0_U_Offset")!=0||material.GetFloat("_UV0_V_Offset")!=0)throw new InvalidOperationException("Original source atlas/UV0 binding was not preserved.");return material;
            }
            catch{UnityEngine.Object.DestroyImmediate(material);throw;}
        }
        static Texture2D TowerConstant(Color color,string name)
        {var texture=new Texture2D(1,1,TextureFormat.RGBA32,false,true){name=name,hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Point};texture.SetPixel(0,0,color);texture.Apply();return texture;}
        static string TowerFileHash(string path){using(var stream=File.OpenRead(Io(path)))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        static IEnumerator TowerSettle(Camera camera,Vector3 position,Vector3 target)
        {
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));double start=EditorApplication.timeSinceStartup;int frames=0;var rt=new RenderTexture(640,360,24);camera.targetTexture=rt;
            try{do{EditorApplication.QueuePlayerLoopUpdate();yield return null;camera.Render();frames++;}while(frames<12||EditorApplication.timeSinceStartup-start<Math.Max(1.5,LODGroup.crossFadeAnimationDuration+.5));}
            finally{camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
        static TowerAppearanceFrame TowerCapture(Camera camera,string output,string intervention,int width,int height,Vector3 target,Shader[] shaders)
        {
            string path=Path.Combine(output,intervention+"-"+width+"x"+height+".png");var rt=new RenderTexture(width,height,24);var prior=RenderTexture.active;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Io(path),texture.EncodeToPNG());return new TowerAppearanceFrame{intervention=intervention,png=path,sha256=TowerFileHash(path),width=width,height=height,position=camera.transform.position,target=target,shaderErrors=shaders.SelectMany(ShaderUtil.GetShaderMessages).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray()};}
            finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
        }
        static string TowerAppearanceArg(string key,string fallback)
        {
            var args=Environment.GetCommandLineArgs();var matches=Enumerable.Range(0,args.Length).Where(i=>args[i]==key).ToArray();
            if(matches.Length==0)return fallback;
            if(matches.Length!=1||matches[0]+1>=args.Length||string.IsNullOrWhiteSpace(args[matches[0]+1])||args[matches[0]+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("Exactly one nonempty value required for "+key);
            return args[matches[0]+1];
        }

    }
}
#endif
