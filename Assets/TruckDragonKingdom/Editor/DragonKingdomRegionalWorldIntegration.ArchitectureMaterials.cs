#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string ArchitectureLayoutFolder=Root+"PrivateLicensedEnvironment/DerivedArchitectureLayoutsV1/";
        public static readonly string[] ArchitectureLayoutPaths={ArchitectureLayoutFolder+"Ventuar Authored Fortified Quarter v1.json",ArchitectureLayoutFolder+"Tarbo Authored Lumber Warehouse Yard v1.json",ArchitectureLayoutFolder+"Fantastic Authored Walled Town v1.json",ArchitectureLayoutFolder+"Tarbo Authored Fishing Warehouse Yard v1.json"};
        static readonly string[] ArchitectureLayoutHashes={"a262d58983bd01bf4dd8c56aa3d207408cf2399eda4204bf2a87352775b176c7","9d7426c9b5aab5cf5575ec23d400f48e738bee4d4caacfec0eea903f04dffec7","bc168d07db8c7bf4e611705e9e291015c70f1bcb1baf682cf77be585d8a0ed5b","7aab43d0c151f583a6e9cf4e6901f464bf2d172c10a53c8280e7e66c97d008bf"};
        [Serializable]public sealed class ArchitectureDependency{public string path,guid,sha256,metaSha256;}
        [Serializable]public sealed class ArchitectureMetadataSnapshot{public string path,sha256,guid,metaSha256;}
        [Serializable]public sealed class ArchitectureSnapshot{public string path,sha256,guid,metaSha256,source,sourceGuid,sourceMetaSha256;public ArchitectureMetadataSnapshot originalMetadata;}
        [Serializable]public sealed class ArchitectureSceneSnapshotWitness{public string layout,expectedSceneSha256,actualSceneSha256,expectedMetadataSha256,actualMetadataSha256;public bool metadataDescriptorPresent,exact;}
        [Serializable]public sealed class ArchitectureMaterialLayout{public int schemaVersion,instanceCount,prefabCount;public string name,compositionKey,sourceScene,sourceSceneSha256,sourceSceneMetaSha256;public ArchitectureDependency[] dependencies;public ArchitectureSnapshot[] originalMaterials;public ArchitectureSnapshot sourceSceneSnapshot;}
        [Serializable]public sealed class ArchitectureNativeDependency{public string path,guid,sourceSha256,nativeSha256,sourceMetaSha256,nativeMetaSha256,authority;public bool sourceContentProtected,metadataExact;}
        [Serializable]public sealed class ArchitectureVariantMap{public string sourceProperty,targetProperty,textureGuid,texturePath;public long textureFileId;public Vector2 scale,offset;public bool preserved;}
        [Serializable]public sealed class ArchitectureVariant
        {
            public string source,guid,sourceSha256,sourceMetaSha256,sourceShader,variantShader,publicUpgrader,publicUpgraderSha256,variantSerializedJsonSha256;public Color sourceColor,variantColor,sourceEmission,variantEmission;public float sourceSmoothness,variantSmoothness,sourceMetallic,variantMetallic;public bool textureUvColorSmoothnessMetallicAndEmissionPreserved,supported,temporaryOnly;public ArchitectureVariantMap[] maps;public string[] shaderErrors;
        }
        [Serializable]public sealed class ArchitectureMaterialPreflight
        {
            public string scope="NATIVE_SOURCE_ARCHITECTURE_CLOSURE_AND_TEMPORARY_PUBLIC_STANDARD_TO_URP_VARIANTS_NOT_WORLD_PLACEMENT_OR_VISUAL_ACCEPTANCE",status="IN_PROGRESS",error,unityVersion,urpVersion,publicUpgraderPackage,publicUpgraderPath,publicUpgraderSha256;
            public int layouts,sourceInstances,sourceDependencies,rawMaterials,existingApprovedMaterialImports,newStandardSources,temporaryUrpVariants;
            public bool passed,sourceBytesPreserved,allRawSnapshotsExact,allSourceIdentitiesExact,noUnknownDependencyChanges,temporaryVariantsDestroyed;
            public ArchitectureSceneSnapshotWitness[] sceneSnapshots;public ArchitectureNativeDependency[] dependencies;public ArchitectureVariant[] variants;public string[] failures,changedSourcePaths;
        }
        public static ArchitectureMaterialPreflight LastArchitectureMaterialPreflight{get;private set;}

        public static ArchitectureMaterialPreflight CaptureArchitectureMaterialPreflight()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required for explicit architecture material preparation.");
            // Reuse the exact already-reviewed native import authorities. This
            // phase precedes any architecture geometry no-write snapshot.
            CaptureArtistSourcePreflight();CaptureExact51AssemblyMaterialProof();var extra=PrepareValidatedQuayNativeImports();if(!extra.passed)throw new InvalidOperationException("Existing approved source import preparation failed.");
            var proof=new ArchitectureMaterialPreflight{unityVersion=Application.unityVersion};LastArchitectureMaterialPreflight=proof;
            string output=Output();Directory.CreateDirectory(output);var errors=new List<string>();var temporary=new List<Material>();Dictionary<string,string> frozen=null;
            try
            {
                var layouts=new List<ArchitectureMaterialLayout>();
                for(int i=0;i<ArchitectureLayoutPaths.Length;i++)
                {string path=ArchitectureLayoutPaths[i];if(AssetSha(path)!=ArchitectureLayoutHashes[i])throw new InvalidOperationException("Exact reviewed four-layout architecture source required: "+path);var layout=JsonUtility.FromJson<ArchitectureMaterialLayout>(File.ReadAllText(Io(path)));if(layout.schemaVersion!=1)throw new InvalidOperationException("Unknown authored architecture schema.");layouts.Add(layout);}
                proof.layouts=layouts.Count;proof.sourceInstances=layouts.Sum(l=>l.instanceCount);
                if(proof.sourceInstances!=678||!layouts.Select(l=>l.instanceCount).SequenceEqual(new[]{110,156,382,30}))throw new InvalidOperationException("Exact four complete authored ensembles required; the broken optional full town is excluded.");
                var deps=layouts.SelectMany(l=>l.dependencies).GroupBy(d=>d.path).Select(g=>{if(g.Select(d=>d.guid+"/"+d.sha256+"/"+d.metaSha256).Distinct().Count()!=1)throw new InvalidOperationException("Conflicting architecture source closure.");return g.First();}).ToArray();
                var snapshots=layouts.SelectMany(l=>l.originalMaterials).GroupBy(s=>s.source).Select(g=>g.First()).ToArray();proof.sourceDependencies=deps.Length;proof.rawMaterials=snapshots.Length;
                if(deps.Length!=518||snapshots.Length!=26)throw new InvalidOperationException("Exact518-dependency/26-raw-material closure required.");
                foreach(var source in layouts.Select(l=>l.sourceSceneSnapshot))ValidateArchitectureSnapshot(source,true);foreach(var source in snapshots)ValidateArchitectureSnapshot(source,false);
                proof.sceneSnapshots=layouts.Select(layout=>new ArchitectureSceneSnapshotWitness{layout=layout.name,expectedSceneSha256=layout.sourceSceneSha256,actualSceneSha256=layout.sourceSceneSnapshot.sha256,expectedMetadataSha256=layout.sourceSceneMetaSha256,actualMetadataSha256=layout.sourceSceneSnapshot.originalMetadata?.sha256,metadataDescriptorPresent=layout.sourceSceneSnapshot.originalMetadata!=null,exact=layout.sourceSceneSnapshot.sha256==layout.sourceSceneSha256&&layout.sourceSceneSnapshot.originalMetadata!=null&&layout.sourceSceneSnapshot.originalMetadata.sha256==layout.sourceSceneMetaSha256}).ToArray();foreach(var witness in proof.sceneSnapshots)if(!witness.exact)throw new InvalidOperationException("Original complete authored scene evidence differs: "+JsonUtility.ToJson(witness));
                proof.allRawSnapshotsExact=true;
                var known=ReadMaterialProvenance().bindings.Select(b=>Tuple.Create(b.path,b.importedSha256,"EXACT_V201_19_MATERIAL_PROVENANCE")).Concat(ReadAssemblyMaterialProvenance().bindings.Select(b=>Tuple.Create(b.path,b.importedSha256,"EXACT_V228_51_MATERIAL_PROVENANCE"))).GroupBy(b=>b.Item1).ToDictionary(g=>g.Key,g=>g.First());
                foreach(var b in QuayExtraMaterials.Where(b=>b.path!=QuayCracks))known[b.path]=Tuple.Create(b.path,b.after,"EXACT_VALIDATED_QUAY_EXTRA_MATERIAL_PROVENANCE");
                var native=new List<ArchitectureNativeDependency>();
                foreach(var dep in deps)
                {
                    var row=new ArchitectureNativeDependency{path=dep.path,guid=AssetDatabase.AssetPathToGUID(dep.path),sourceSha256=dep.sha256,nativeSha256=AssetSha(dep.path),sourceMetaSha256=dep.metaSha256,nativeMetaSha256=AssetSha(dep.path+".meta"),authority="EXACT_ORIGINAL_SOURCE"};
                    row.metadataExact=row.nativeMetaSha256==row.sourceMetaSha256;row.sourceContentProtected=row.nativeSha256==row.sourceSha256;
                    if(dep.path.EndsWith(".mat",StringComparison.Ordinal)&&known.TryGetValue(dep.path,out var accepted)){row.authority=accepted.Item3;row.sourceContentProtected=row.nativeSha256==accepted.Item2;proof.existingApprovedMaterialImports++;}
                    if(row.guid!=dep.guid)errors.Add("Source GUID differs: "+dep.path);if(!row.metadataExact)errors.Add("Unapproved source metadata difference: "+dep.path);if(!row.sourceContentProtected)errors.Add("Unapproved source content difference: "+dep.path);native.Add(row);
                }
                proof.dependencies=native.ToArray();proof.allSourceIdentitiesExact=native.All(r=>deps.Single(d=>d.path==r.path).guid==r.guid);proof.noUnknownDependencyChanges=errors.Count==0;
                var allPaths=deps.SelectMany(d=>new[]{d.path,d.path+".meta"}).Concat(ArchitectureLayoutPaths.SelectMany(p=>new[]{p,p+".meta"})).Concat(snapshots.Concat(layouts.Select(l=>l.sourceSceneSnapshot)).SelectMany(s=>new[]{s.path,s.path+".meta"})).Where(p=>File.Exists(Io(p))).Distinct().ToArray();frozen=allPaths.ToDictionary(p=>p,AssetSha);
                var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).Assembly);if(package==null)throw new InvalidOperationException("Actual installed URP package required.");proof.urpVersion=package.version;proof.publicUpgraderPackage=package.name;proof.publicUpgraderPath="Editor/Tools/MaterialUpgrader/MaterialUpgraderDefinitions.cs";proof.publicUpgraderSha256=AssetSha(Path.Combine(package.resolvedPath,proof.publicUpgraderPath));
                var variants=new List<ArchitectureVariant>();
                foreach(var snapshot in snapshots.Where(s=>!known.ContainsKey(s.source)))
                {
                    if(!snapshot.source.Contains("/Tarbo Studio/TARBO-FantasyVillage/Materials/")&&!snapshot.source.Contains("/Ventuar3D/MedievalCastlePack/FileAssets/Materials/"))throw new InvalidOperationException("Unknown new architecture material source.");
                    var source=AssetDatabase.LoadAssetAtPath<Material>(snapshot.source);if(source==null||source.shader==null||source.shader.name!="Standard"||AssetSha(snapshot.source)!=snapshot.sha256)throw new InvalidOperationException("Exact unchanged original Standard material required: "+snapshot.source);
                    var copy=new Material(source){hideFlags=HideFlags.HideAndDontSave,name=source.name+" public URP diagnostic only"};temporary.Add(copy);
                    new StandardUpgrader("Standard").Upgrade(copy,MaterialUpgrader.UpgradeFlags.None);
                    variants.Add(MeasureArchitectureVariant(source,copy,proof));proof.newStandardSources++;proof.temporaryUrpVariants++;
                }
                proof.variants=variants.ToArray();
                if(proof.newStandardSources!=4||proof.existingApprovedMaterialImports!=22)errors.Add("Actual expected source split is22approved existing plus4original Standard materials.");
                foreach(var v in variants)if(!v.supported||!v.textureUvColorSmoothnessMetallicAndEmissionPreserved||v.shaderErrors.Length!=0)errors.Add("Public native Standard→URP variant failed: "+v.source);
                proof.failures=errors.ToArray();proof.passed=proof.noUnknownDependencyChanges&&errors.Count==0;proof.status=proof.passed?"SOURCE_AND_PUBLIC_TEMPORARY_URP_VARIANTS_VERIFIED_REQUIRES_GEOMETRY_PLACEMENT_RENDERING":"REJECTED_NATIVE_SOURCE_ARCHITECTURE_PREFLIGHT";
            }
            catch(Exception e){proof.error=e.ToString();proof.status="REJECTED_NATIVE_SOURCE_ARCHITECTURE_PREFLIGHT";throw;}
            finally
            {
                foreach(var material in temporary)if(material!=null)UnityEngine.Object.DestroyImmediate(material);proof.temporaryVariantsDestroyed=temporary.All(m=>m==null);proof.changedSourcePaths=frozen==null?Array.Empty<string>():frozen.Where(p=>!File.Exists(Io(p.Key))||AssetSha(p.Key)!=p.Value).Select(p=>p.Key).ToArray();proof.sourceBytesPreserved=frozen!=null&&proof.changedSourcePaths.Length==0;
                proof.passed&=proof.sourceBytesPreserved&&proof.temporaryVariantsDestroyed;File.WriteAllText(Path.Combine(output,"architecture-material-native-preflight.json"),JsonUtility.ToJson(proof,true));
            }
            if(!proof.passed)throw new InvalidOperationException("Native architecture source/material preflight rejected; complete dependency/material differences retained.");return proof;
        }
        static void ValidateArchitectureSnapshot(ArchitectureSnapshot source,bool requireOriginalMetadata)
        {
            if(source==null||string.IsNullOrEmpty(source.path)||!source.path.StartsWith(ArchitectureLayoutFolder,StringComparison.Ordinal)||AssetSha(source.path)!=source.sha256||AssetSha(source.path+".meta")!=source.metaSha256||AssetDatabase.AssetPathToGUID(source.path)!=source.guid)throw new InvalidOperationException("Immutable original architecture source snapshot differs.");
            var child=source.originalMetadata;bool empty=child==null||string.IsNullOrEmpty(child.path)&&string.IsNullOrEmpty(child.sha256)&&string.IsNullOrEmpty(child.guid)&&string.IsNullOrEmpty(child.metaSha256);
            if(requireOriginalMetadata){if(empty)throw new InvalidOperationException("Required original authored scene metadata descriptor is missing: "+source.path);ValidateArchitectureMetadataSnapshot(child);}
            else if(!empty)throw new InvalidOperationException("Raw material snapshot unexpectedly declares original scene metadata: "+source.path);
        }
        static void ValidateArchitectureMetadataSnapshot(ArchitectureMetadataSnapshot source)
        {if(source==null||string.IsNullOrEmpty(source.path)||!source.path.StartsWith(ArchitectureLayoutFolder,StringComparison.Ordinal)||AssetSha(source.path)!=source.sha256||AssetSha(source.path+".meta")!=source.metaSha256||AssetDatabase.AssetPathToGUID(source.path)!=source.guid)throw new InvalidOperationException("Immutable original architecture metadata snapshot differs.");}
        static ArchitectureVariant MeasureArchitectureVariant(Material source,Material copy,ArchitectureMaterialPreflight context)
        {
            string path=AssetDatabase.GetAssetPath(source);var row=new ArchitectureVariant{source=path,guid=AssetDatabase.AssetPathToGUID(path),sourceSha256=AssetSha(path),sourceMetaSha256=AssetSha(path+".meta"),sourceShader=source.shader.name,variantShader=copy.shader.name,publicUpgrader=context.publicUpgraderPath,publicUpgraderSha256=context.publicUpgraderSha256,sourceColor=source.GetColor("_Color"),variantColor=copy.GetColor("_BaseColor"),sourceEmission=source.GetColor("_EmissionColor"),variantEmission=copy.GetColor("_EmissionColor"),sourceSmoothness=source.GetFloat("_Glossiness"),variantSmoothness=copy.GetFloat("_Smoothness"),sourceMetallic=source.GetFloat("_Metallic"),variantMetallic=copy.GetFloat("_Metallic"),temporaryOnly=true,supported=copy.shader.isSupported};
            var maps=new List<ArchitectureVariantMap>();
            foreach(var pair in new[]{Tuple.Create("_MainTex","_BaseMap"),Tuple.Create("_BumpMap","_BumpMap"),Tuple.Create("_EmissionMap","_EmissionMap"),Tuple.Create("_MetallicGlossMap","_MetallicGlossMap"),Tuple.Create("_OcclusionMap","_OcclusionMap"),Tuple.Create("_ParallaxMap","_ParallaxMap"),Tuple.Create("_DetailMask","_DetailMask"),Tuple.Create("_DetailAlbedoMap","_DetailAlbedoMap"),Tuple.Create("_DetailNormalMap","_DetailNormalMap")})
            {var texture=source.GetTexture(pair.Item1);string texturePath=texture==null?null:AssetDatabase.GetAssetPath(texture);string guid=null;long id=0;if(texture!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture,out guid,out id);Vector2 scale=source.GetTextureScale(pair.Item1),offset=source.GetTextureOffset(pair.Item1);maps.Add(new ArchitectureVariantMap{sourceProperty=pair.Item1,targetProperty=pair.Item2,texturePath=texturePath,textureGuid=guid,textureFileId=id,scale=scale,offset=offset,preserved=copy.HasProperty(pair.Item2)&&copy.GetTexture(pair.Item2)==texture&&copy.GetTextureScale(pair.Item2)==scale&&copy.GetTextureOffset(pair.Item2)==offset});}
            row.maps=maps.ToArray();row.textureUvColorSmoothnessMetallicAndEmissionPreserved=maps.All(m=>m.preserved)&&row.sourceColor==row.variantColor&&row.sourceEmission==row.variantEmission&&row.sourceSmoothness==row.variantSmoothness&&row.sourceMetallic==row.variantMetallic;row.shaderErrors=ShaderUtil.GetShaderMessages(copy.shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray();row.variantSerializedJsonSha256=Hash(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(copy))).Replace("-","").ToLowerInvariant();return row;
        }
    }
}
#endif
