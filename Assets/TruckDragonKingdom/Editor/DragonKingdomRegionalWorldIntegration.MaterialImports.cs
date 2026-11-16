#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string ArtistImportProvenance=Root+"PrivateLicensedEnvironment/DerivedVillageLayout/Material Import Provenance v1.json";
        const string ArtistImportProvenanceSha="4c0f984d7487a2c7728c1c05559632c3bd90eaf3467ef3a2d1a85ce92a637415";
        [Serializable]public sealed class ImportedProperty{public string kind,name,sourceValue,importedValue,reason;}
        [Serializable]public sealed class ArtistMaterialImport
        {
            public string path,guid,originalSha256,importedSha256,metaSha256,snapshot,snapshotSha256,snapshotMetaSha256,snapshotGuid;
            public int originalRenderQueue,importedRenderQueue;public ImportedProperty[] protectedProperties,convertedProperties;
        }
        [Serializable]public sealed class ArtistMaterialProvenance
        {
            public int schemaVersion,nativeUpgraderFromVersion,nativeUpgraderToVersion;public string scope,editorVersion,urpVersion,upgraderPackage,upgraderRelativePath,upgraderSourceSha256,sourceNativeRun;
            public ArtistMaterialImport[] bindings;
        }
        [Serializable]public sealed class ArtistImportEvidence
        {
            public string provenance,provenanceSha256,provenanceGuid,provenanceMetaSha256,editorVersion,urpVersion,upgraderSourceSha256;
            public int rawMaterialSnapshots,approvedNativeImports,exactNonmaterialDependencies,exactMetadataAndGuids,protectedProperties,convertedProperties;
            public bool passed;public ArtistMaterialImport[] bindings;
        }
        static ArtistMaterialProvenance ReadMaterialProvenance()
        {
            if(AssetSha(ArtistImportProvenance)!=ArtistImportProvenanceSha)throw new InvalidOperationException("Exact reviewed nineteen-material V201 native import provenance required; no generic material waiver.");
            var proof=JsonUtility.FromJson<ArtistMaterialProvenance>(File.ReadAllText(ArtistImportProvenance));var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).Assembly);
            if(proof.schemaVersion!=1||proof.bindings==null||proof.bindings.Length!=19||proof.bindings.Select(b=>b.path).Distinct().Count()!=19||proof.editorVersion!=Application.unityVersion||package==null||proof.urpVersion!=package.version||proof.upgraderPackage!=package.name||proof.upgraderRelativePath!="Editor/AssetPostProcessors/MaterialPostprocessor.cs"||proof.nativeUpgraderFromVersion!=1||proof.nativeUpgraderToVersion!=10||proof.sourceNativeRun!="V201"||AssetSha(Path.Combine(package.resolvedPath,proof.upgraderRelativePath))!=proof.upgraderSourceSha256)throw new InvalidOperationException("Native import version/public upgrader source differs from the actual reviewed V201 conversion.");
            return proof;
        }
        static void PrepareApprovedArtistMaterialImports()
        {
            var proof=ReadMaterialProvenance();
            foreach(var binding in proof.bindings)
            {
                ValidateOriginalMaterialSnapshot(binding);string current=AssetSha(binding.path);
                if(current!=binding.originalSha256&&current!=binding.importedSha256)throw new InvalidOperationException("Material differs from both exact original and approved vendor import: "+binding.path);
                // Complete the vendor's public import/save before freezing the
                // generation's source hashes. This does not patch/skip its
                // upgrader. The original bytes remain in immutable .bytes data.
                if(current==binding.originalSha256)
                {
                    foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(binding.path))if(asset!=null)AssetDatabase.SaveAssetIfDirty(asset);
                }
                ValidateImportedMaterial(binding);
            }
        }
        static void ValidateOriginalMaterialSnapshot(ArtistMaterialImport binding)
        {
            if(!binding.path.StartsWith(DragonKingdomWorldAuthoring.Private,StringComparison.Ordinal))throw new InvalidOperationException("Private source material required.");
            if(!binding.path.EndsWith(".mat",StringComparison.Ordinal)||!binding.snapshot.StartsWith(Root+"PrivateLicensedEnvironment/DerivedVillageLayout/OriginalMaterialSources/",StringComparison.Ordinal)||!binding.snapshot.EndsWith("/"+binding.guid+".bytes",StringComparison.Ordinal)||binding.originalSha256!=binding.snapshotSha256||AssetSha(binding.snapshot)!=binding.originalSha256||AssetSha(binding.snapshot+".meta")!=binding.snapshotMetaSha256||AssetDatabase.AssetPathToGUID(binding.snapshot)!=binding.snapshotGuid||AssetDatabase.AssetPathToGUID(binding.path)!=binding.guid||AssetSha(binding.path+".meta")!=binding.metaSha256)throw new InvalidOperationException("Immutable original source material/GUID/metadata provenance changed: "+binding.path);
        }
        static string PropertyText(string source,ImportedProperty property)
        {
            string prefix=property.kind=="Shader"?"  ":"    - ";string pattern=property.kind=="Texture"?@"(?m)^    - "+Regex.Escape(property.name)+@":\r?\n((?:        .*\r?\n)+)":@"(?m)^"+Regex.Escape(prefix+property.name)+@": ([^\r\n]+)";
            var match=Regex.Match(source,pattern);if(!match.Success)throw new InvalidOperationException("Protected authored material property missing: "+property.kind+"/"+property.name);return match.Groups[1].Value.Replace("\r\n","\n").Trim();
        }
        static void ValidateImportedMaterial(ArtistMaterialImport binding)
        {
            ValidateOriginalMaterialSnapshot(binding);if(AssetSha(binding.path)!=binding.importedSha256)throw new InvalidOperationException("Actual imported material differs from its exact measured V201 output: "+binding.path);
            string original=File.ReadAllText(binding.snapshot),current=File.ReadAllText(binding.path);
            foreach(var property in binding.protectedProperties)if(PropertyText(original,property)!=property.sourceValue||PropertyText(current,property)!=property.sourceValue)throw new InvalidOperationException("Native import changed protected authored shader/color/texture/scalar: "+binding.path+"/"+property.name);
            foreach(var property in binding.convertedProperties)
            {
                if(Path.GetFileName(binding.path)!="M_CLR_yellow_E.mat"||property.kind!="Color"||(property.name!="_Color"&&property.name!="_EmissionColor")||PropertyText(original,property)!=property.sourceValue||PropertyText(current,property)!=property.importedValue)throw new InvalidOperationException("Unreviewed native material conversion.");
                if(property.name=="_EmissionColor")
                {
                    float Number(string name)=>float.Parse(Regex.Match(property.sourceValue,name+@": ([^,} ]+)").Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
                    var expected=new Color(Number("r"),Number("g"),Number("b"),Number("a")).linear;var material=AssetDatabase.LoadAssetAtPath<Material>(binding.path);var actual=material.GetColor("_EmissionColor");if(Mathf.Abs(expected.r-actual.r)>.00002f||Mathf.Abs(expected.g-actual.g)>.00002f||Mathf.Abs(expected.b-actual.b)>.00002f||Mathf.Abs(expected.a-actual.a)>.00002f)throw new InvalidOperationException("Actual vendor HDR gamma-to-linear compatibility conversion differs.");
                }
            }
            int Queue(string text)=>int.Parse(Regex.Match(text,@"(?m)^  m_CustomRenderQueue: (-?\d+)").Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
            if(Queue(original)!=binding.originalRenderQueue||Queue(current)!=binding.importedRenderQueue)throw new InvalidOperationException("Exact reviewed vendor render queue migration differs.");
        }
        static ArtistImportEvidence ValidateArtistMaterialImports(ArtistLayout source)
        {
            var proof=ReadMaterialProvenance();if(source.materialImportProvenance!=ArtistImportProvenance||source.materialImportProvenanceSha256!=ArtistImportProvenanceSha)throw new InvalidOperationException("Artist layout must bind exact current native material provenance.");
            int exact=0,converted=0;
            foreach(var dependency in source.dependencies)
            {
                if(!dependency.path.StartsWith(DragonKingdomWorldAuthoring.Private,StringComparison.Ordinal)||AssetDatabase.AssetPathToGUID(dependency.path)!=dependency.guid||AssetSha(dependency.path+".meta")!=dependency.metaSha256)throw new InvalidOperationException("Artist dependency metadata/GUID changed: "+dependency.path);
                var binding=proof.bindings.SingleOrDefault(b=>b.path==dependency.path);
                if(binding==null){if(AssetSha(dependency.path)!=dependency.sha256)throw new InvalidOperationException("Artist nonmaterial dependency bytes changed: "+dependency.path);exact++;}
                else{if(binding.originalSha256!=dependency.sha256||binding.guid!=dependency.guid||binding.metaSha256!=dependency.metaSha256)throw new InvalidOperationException("Material provenance differs from exact artist dependency.");ValidateImportedMaterial(binding);converted++;}
                dependency.actualSha256=AssetSha(dependency.path);dependency.approvedNativeImport=binding!=null;
            }
            if(exact!=84||converted!=19)throw new InvalidOperationException("Exact84 raw nonmaterial /19 vendor-imported material closure required.");
            var result=new ArtistImportEvidence{provenance=ArtistImportProvenance,provenanceSha256=ArtistImportProvenanceSha,provenanceGuid=AssetDatabase.AssetPathToGUID(ArtistImportProvenance),provenanceMetaSha256=AssetSha(ArtistImportProvenance+".meta"),editorVersion=proof.editorVersion,urpVersion=proof.urpVersion,upgraderSourceSha256=proof.upgraderSourceSha256,rawMaterialSnapshots=19,approvedNativeImports=19,exactNonmaterialDependencies=84,exactMetadataAndGuids=103,protectedProperties=proof.bindings.Sum(b=>b.protectedProperties.Length),convertedProperties=proof.bindings.Sum(b=>b.convertedProperties.Length),bindings=proof.bindings,passed=true};
            if(result.protectedProperties!=769||result.convertedProperties!=2)throw new InvalidOperationException("Exact reviewed authored property/conversion count changed.");return result;
        }
    }
}
#endif
