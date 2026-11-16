#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable] public sealed class CavernTextureSampler
        {
            public string property,path,guid,format,graphicsFormat,filterMode,wrapU,wrapV;
            public int mipCount,aniso;public float mipBias;public bool present;
        }
        [Serializable] public sealed class CavernRockTilingMaterial
        {
            public string source,guid;public Vector2 originalScale,diagnosticScale,offset;
            public CavernTextureSampler[] actualTextures,candidateTextures;
            public CavernTextureSampler sharedNormalSampler;
        }
        [Serializable] public sealed class CavernRockTilingEvidence
        {
            public string scope="REVERSIBLE_ROCK_REPEAT_ONLY_DIAGNOSTIC_NOT_PRODUCTION_REPAIR";
            public Frame[] frames=Array.Empty<Frame>();public CavernRockTilingMaterial[] materials=Array.Empty<CavernRockTilingMaterial>();
            public string baselinePngSha256,restoredPngSha256;public bool restoredImageByteExact,samplerMetadataExact;
            public bool performed,onlyRockScaleChanged,stateRestored,copiesDestroyed,originalMaterialsExact,meshBindingsExact,meshPayloadExact,collidersExact,cameraLightsLodsExact,rendererStateExact,propertyBlocksAbsent;
            public int temporaryMaterials,rendererSlotsChanged;
            public bool ownedRockMapsRequested,onlyApprovedRockInputsChanged;
            public string diagnosticPngSha256;public bool diagnosticImageChanged,ownedRockSourceBytesExact;
            public string[] ownedRockSourcePaths=Array.Empty<string>(),ownedRockSourceSha256=Array.Empty<string>();
        }
        static CavernTextureSampler MeasureCavernTextureSampler(Material material,string property)
        {
            var texture=material.GetTexture(property);var path=texture!=null?AssetDatabase.GetAssetPath(texture):"";var t=texture as Texture2D;
            return new CavernTextureSampler{property=property,present=texture!=null,path=path,guid=AssetDatabase.AssetPathToGUID(path),format=t!=null?t.format.ToString():"",graphicsFormat=texture!=null?texture.graphicsFormat.ToString():"",mipCount=t!=null?t.mipmapCount:0,filterMode=texture!=null?texture.filterMode.ToString():"",wrapU=texture!=null?texture.wrapModeU.ToString():"",wrapV=texture!=null?texture.wrapModeV.ToString():"",aniso=texture!=null?texture.anisoLevel:0,mipBias=texture!=null?texture.mipMapBias:0};
        }
        public static bool CavernRockRepeatOnlyPropertiesExact(Material source,Material candidate)
        {
            if(source==null||candidate==null||source==candidate||source.shader!=candidate.shader||!source.HasProperty("_Splat3"))return false;
            var names=source.GetTexturePropertyNames();if(!names.SequenceEqual(candidate.GetTexturePropertyNames()))return false;
            foreach(string name in names)
                if(source.GetTexture(name)!=candidate.GetTexture(name)||!source.GetTextureOffset(name).Equals(candidate.GetTextureOffset(name))||!(name=="_Splat3"?source.GetTextureScale(name)*.5f:source.GetTextureScale(name)).Equals(candidate.GetTextureScale(name)))return false;
            var expected=new Material(source){name=candidate.name,hideFlags=candidate.hideFlags};
            try{expected.SetTextureScale("_Splat3",source.GetTextureScale("_Splat3")*.5f);return EditorJsonUtility.ToJson(expected)==EditorJsonUtility.ToJson(candidate);}
            finally{UnityEngine.Object.DestroyImmediate(expected);}
        }
        public static Material CreateCavernDoubleRockRepeatMaterial(Material source)
        {
            if(source==null||!source.shader.name.StartsWith("Digger/Mesh/URP/Lit-Pass",StringComparison.Ordinal)||!source.HasProperty("_Splat3")||source.GetTexture("_Splat3")==null)throw new InvalidOperationException("Actual textured Digger rock layer required.");
            var scale=source.GetTextureScale("_Splat3");if(!float.IsFinite(scale.x)||!float.IsFinite(scale.y)||scale.x<=0||scale.y<=0)throw new InvalidOperationException("Actual positive finite rock tiling required.");
            var copy=new Material(source){name=source.name+" temporary doubled rock repeat",hideFlags=HideFlags.HideAndDontSave};
            try{copy.SetTextureScale("_Splat3",scale*.5f);if(!CavernRockRepeatOnlyPropertiesExact(source,copy))throw new InvalidOperationException("Rock repeat copy changed another native material property.");return copy;}
            catch{UnityEngine.Object.DestroyImmediate(copy);throw;}
        }
        const string CavernRoughRockDiffuse="Assets/Digger/Misc/Textures/Rough_rock_019_COLOR.png";
        const string CavernRoughRockNormal="Assets/Digger/Misc/Textures/Rough_rock_019_NRM.png";
        static void ApplyCavernOwnedRockInputs(Material material)
        {
            var diffuse=AssetDatabase.LoadAssetAtPath<Texture2D>(CavernRoughRockDiffuse);var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(CavernRoughRockNormal);
            if(diffuse==null||normal==null||!material.HasProperty("_Splat3")||!material.HasProperty("_Normal3"))throw new InvalidOperationException("Installed paired Digger rough-rock textures and native layer3 properties required.");
            var normalImporter=AssetImporter.GetAtPath(CavernRoughRockNormal) as TextureImporter;
            if(normalImporter==null||normalImporter.textureType!=TextureImporterType.NormalMap||normalImporter.sRGBTexture)throw new InvalidOperationException("The existing owned rock normal must retain its actual linear NormalMap importer.");
            material.SetTexture("_Splat3",diffuse);material.SetTexture("_Normal3",normal);material.SetTextureScale("_Splat3",Vector2.one/12f);
        }
        public static bool CavernOwnedRockPropertiesExact(Material source,Material candidate)
        {
            if(source==null||candidate==null||source==candidate||source.shader!=candidate.shader)return false;
            var expected=new Material(source){name=candidate.name,hideFlags=candidate.hideFlags};
            try
            {
                ApplyCavernOwnedRockInputs(expected);
                var names=expected.GetTexturePropertyNames();if(!names.SequenceEqual(candidate.GetTexturePropertyNames()))return false;
                foreach(string name in names)if(expected.GetTexture(name)!=candidate.GetTexture(name)||!expected.GetTextureScale(name).Equals(candidate.GetTextureScale(name))||!expected.GetTextureOffset(name).Equals(candidate.GetTextureOffset(name)))return false;
                return EditorJsonUtility.ToJson(expected)==EditorJsonUtility.ToJson(candidate);
            }
            finally{UnityEngine.Object.DestroyImmediate(expected);}
        }
        public static Material CreateCavernOwnedRoughRockMaterial(Material source)
        {
            if(source==null||!source.shader.name.StartsWith("Digger/Mesh/URP/Lit-Pass",StringComparison.Ordinal)||!source.IsKeywordEnabled("_NORMALMAP"))throw new InvalidOperationException("Original normal-mapped Digger material required; do not combine shader/keyword changes with the owned rock surface.");
            var copy=new Material(source){name=source.name+" temporary owned rough rock12m",hideFlags=HideFlags.HideAndDontSave};
            try{ApplyCavernOwnedRockInputs(copy);if(!CavernOwnedRockPropertiesExact(source,copy))throw new InvalidOperationException("Owned rock variant changed inputs beyond approved layer3 maps/repeat.");return copy;}
            catch{UnityEngine.Object.DestroyImmediate(copy);throw;}
        }
        static void CaptureCavernRockTilingDiagnostic(Camera camera,DragonKingdomWorld world,HashSet<Collider> roads,Shader shader,string output,MeshRenderer[] renderers,Frame baseline,CavernRockTilingEvidence proof,bool ownedRockMaps=false)
        {
            proof.ownedRockMapsRequested=ownedRockMaps;if(ownedRockMaps)proof.scope="REVERSIBLE_INSTALLED_DIGGER_ROUGH_ROCK_MAPS_AND12M_REPEAT_NOT_PRODUCTION_OR_VISUAL_ACCEPTANCE";
            if(ownedRockMaps){proof.ownedRockSourcePaths=new[]{CavernRoughRockDiffuse,CavernRoughRockNormal}.SelectMany(p=>new[]{p,p+".meta"}).ToArray();proof.ownedRockSourceSha256=proof.ownedRockSourcePaths.Select(p=>Hash(File.ReadAllBytes(IoPath(p)))).ToArray();}
            bool Exact(Material source,Material candidate)=>ownedRockMaps?CavernOwnedRockPropertiesExact(source,candidate):CavernRockRepeatOnlyPropertiesExact(source,candidate);
            string label=ownedRockMaps?"rounded-entry-owned-rock-diagnostic":"rounded-entry-rock-tiling-diagnostic";
            proof.performed=true;proof.baselinePngSha256=Hash(File.ReadAllBytes(IoPath(baseline.png)));RequireCavernStandardLitNoPropertyBlocks(renderers);proof.propertyBlocksAbsent=true;
            var materialBindings=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);var originals=materialBindings.Values.SelectMany(m=>m).Distinct().ToArray();var originalJson=originals.ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));
            string Samplers(Material m)=>EditorJsonUtility.ToJson(MeasureCavernTextureSampler(m,"_Splat0"))+EditorJsonUtility.ToJson(MeasureCavernTextureSampler(m,"_Splat3"));var samplerJson=originals.ToDictionary(m=>m,Samplers);
            var meshes=renderers.Select(r=>r.GetComponent<MeshFilter>()).ToDictionary(f=>f,f=>f.sharedMesh);var payload=meshes.Values.Distinct().ToDictionary(m=>m,StandardLitMeshPayload);
            var rendererJson=renderers.ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r)+EditorJsonUtility.ToJson(r.transform));
            var physical=world.GetComponentsInChildren<MeshCollider>(true).ToDictionary(c=>c,c=>c.sharedMesh);var physicalJson=physical.Keys.ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c)+EditorJsonUtility.ToJson(c.transform));
            var lods=world.GetComponentsInChildren<LODGroup>(true).ToDictionary(g=>g,g=>EditorJsonUtility.ToJson(g)+EditorJsonUtility.ToJson(g.transform));
            var lights=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>l,l=>EditorJsonUtility.ToJson(l)+EditorJsonUtility.ToJson(l.transform));string cameraJson=EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform);
            var copies=new Dictionary<Material,Material>();var frames=new List<Frame>();
            bool Restored()
            {
                proof.samplerMetadataExact=samplerJson.All(p=>p.Key!=null&&p.Value==Samplers(p.Key));
                proof.ownedRockSourceBytesExact=!ownedRockMaps||proof.ownedRockSourcePaths.Select((p,i)=>File.Exists(IoPath(p))&&Hash(File.ReadAllBytes(IoPath(p)))==proof.ownedRockSourceSha256[i]).All(v=>v);
                proof.originalMaterialsExact=originalJson.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key));
                proof.rendererStateExact=materialBindings.All(p=>p.Key!=null&&p.Key.sharedMaterials.SequenceEqual(p.Value)&&rendererJson[p.Key]==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform));
                proof.meshBindingsExact=meshes.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value);proof.meshPayloadExact=payload.All(p=>p.Key!=null&&p.Value==StandardLitMeshPayload(p.Key));
                proof.collidersExact=physical.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value&&physicalJson[p.Key]==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform));
                proof.cameraLightsLodsExact=cameraJson==EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform)&&lights.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform))&&lods.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform));
                RequireCavernStandardLitNoPropertyBlocks(renderers);proof.copiesDestroyed=copies.Values.All(m=>m==null);
                return proof.ownedRockSourceBytesExact&&proof.samplerMetadataExact&&proof.originalMaterialsExact&&proof.rendererStateExact&&proof.meshBindingsExact&&proof.meshPayloadExact&&proof.collidersExact&&proof.cameraLightsLodsExact&&proof.copiesDestroyed;
            }
            try
            {
                var rows=new List<CavernRockTilingMaterial>();
                foreach(var original in originals)
                {
                    var copy=ownedRockMaps?CreateCavernOwnedRoughRockMaterial(original):CreateCavernDoubleRockRepeatMaterial(original);copies.Add(original,copy);
                    rows.Add(new CavernRockTilingMaterial{source=AssetDatabase.GetAssetPath(original),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original)),originalScale=original.GetTextureScale("_Splat3"),diagnosticScale=copy.GetTextureScale("_Splat3"),offset=original.GetTextureOffset("_Splat3"),actualTextures=new[]{MeasureCavernTextureSampler(original,"_Splat0"),MeasureCavernTextureSampler(original,"_Splat3")},candidateTextures=new[]{MeasureCavernTextureSampler(copy,"_Splat3"),MeasureCavernTextureSampler(copy,"_Normal3")},sharedNormalSampler=MeasureCavernTextureSampler(original,"_Normal0")});
                }
                proof.materials=rows.ToArray();proof.temporaryMaterials=copies.Count;if(copies.Count==0)throw new InvalidOperationException("Actual Digger rock material required for tiling comparison.");
                foreach(var binding in materialBindings){binding.Key.sharedMaterials=binding.Value.Select(m=>copies[m]).ToArray();proof.rendererSlotsChanged+=binding.Value.Length;}
                proof.onlyApprovedRockInputsChanged=copies.All(p=>Exact(p.Key,p.Value));if(!proof.onlyApprovedRockInputsChanged)throw new InvalidOperationException("Rock variant changed unapproved native material inputs.");
                frames.Add(Capture(camera,world,roads,shader,output,label,ownedRockMaps?"installed-rough-rock12m":"doubled-rock-repeat"));proof.frames=frames.ToArray();proof.onlyApprovedRockInputsChanged&=copies.All(p=>Exact(p.Key,p.Value));proof.onlyRockScaleChanged=!ownedRockMaps&&proof.onlyApprovedRockInputsChanged;
                proof.diagnosticPngSha256=Hash(File.ReadAllBytes(IoPath(frames[0].png)));proof.diagnosticImageChanged=proof.baselinePngSha256!=proof.diagnosticPngSha256;
                if(ownedRockMaps&&!proof.diagnosticImageChanged)throw new InvalidOperationException("The owned rock capture must visibly exercise the changed material; a byte-identical image does not establish an applied variant.");
                if(!proof.onlyApprovedRockInputsChanged||frames[0].shaderErrors.Length!=0)throw new InvalidOperationException("Rock variant capture changed other material properties or reported shader errors.");
            }
            finally
            {
                foreach(var binding in materialBindings)if(binding.Key!=null)binding.Key.sharedMaterials=binding.Value;
                foreach(var copy in copies.Values)if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);proof.stateRestored=Restored();
            }
            if(!proof.stateRestored)throw new InvalidOperationException("Rock tiling diagnostic failed exact original state restoration.");
            frames.Add(Capture(camera,world,roads,shader,output,label,"original-rock-repeat-restored"));proof.frames=frames.ToArray();proof.stateRestored&=Restored();proof.restoredPngSha256=Hash(File.ReadAllBytes(IoPath(frames[1].png)));proof.restoredImageByteExact=proof.baselinePngSha256==proof.restoredPngSha256;
            if(!proof.stateRestored||!proof.restoredImageByteExact||frames[1].shaderErrors.Length!=0)throw new InvalidOperationException("Restored rock tiling frame changed original state or reported shader errors.");
        }
    }
}
#endif
