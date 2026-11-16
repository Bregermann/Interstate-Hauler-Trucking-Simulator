#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string AssemblyImportProvenance=Root+"PrivateLicensedEnvironment/DerivedFinishAssemblies/Material Import Provenance v1.json";
        const string AssemblyImportProvenanceSha="db8dce4f62cbfa89ce6346d3e59bbc62be63f3be36eac7ba7395891dddb75d82";
        [Serializable]public sealed class AssemblyMetadataChange{public string name,sourceValue,importedValue,reason;}
        [Serializable]public sealed class AssemblyPublicShader{public string package,relativePath,sha256,guid,mainColor;}
        [Serializable]public sealed class AssemblyLayoutBinding{public string path,sha256,metaSha256,guid,sourceSceneSha256;public int instances;}
        [Serializable]public sealed class AssemblyMaterialBinding
        {
            public string path,guid,originalSha256,importedSha256,metaSha256,snapshot,snapshotSha256,snapshotMetaSha256,snapshotGuid;public int originalRenderQueue,importedRenderQueue;
            public ImportedProperty[] protectedProperties,convertedProperties,addedProperties;public AssemblyMetadataChange[] metadataChanges;
        }
        [Serializable]public sealed class AssemblyMaterialProvenance
        {
            public int schemaVersion,nativeUpgraderFromVersion,nativeUpgraderToVersion;public string scope,editorVersion,urpVersion,upgraderPackage,upgraderRelativePath,upgraderSourceSha256,sourceNativeRun,sourceNativeArchiveSha256,sourceNativeCaseStatus;public bool materialStageOnly;public AssemblyPublicShader[] publicShaderSources;public AssemblyLayoutBinding[] sourceLayouts;public AssemblyMaterialBinding[] bindings;
        }
        [Serializable]public sealed class AssemblyMaterialEvidence
        {
            public string provenance,provenanceSha256,provenanceGuid,provenanceMetaSha256,sourceNativeRun,sourceNativeArchiveSha256;public int rawMaterialSnapshots,approvedNativeImports,protectedProperties,convertedProperties,addedProperties,metadataChanges,exactNonmaterialDependencies,exactMetadataAndGuids;public bool passed;public AssemblyMaterialBinding[] bindings;
        }
        public static AssemblyMaterialEvidence LastAssemblyMaterialEvidence{get;private set;}
        static AssemblyMaterialProvenance ReadAssemblyMaterialProvenance()
        {
            if(AssetSha(AssemblyImportProvenance)!=AssemblyImportProvenanceSha)throw new InvalidOperationException("Exact private51-material V228 provenance required; no generic .mat waiver.");var proof=JsonUtility.FromJson<AssemblyMaterialProvenance>(File.ReadAllText(Io(AssemblyImportProvenance)));var old=ReadMaterialProvenance();var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).Assembly);
            if(proof.schemaVersion!=1||proof.bindings.Length!=51||proof.bindings.Select(b=>b.path).Distinct().Count()!=51||proof.bindings.Select(b=>b.guid).Distinct().Count()!=51||proof.editorVersion!=Application.unityVersion||package==null||proof.urpVersion!=package.version||proof.upgraderPackage!=package.name||proof.upgraderRelativePath!=old.upgraderRelativePath||proof.upgraderSourceSha256!=old.upgraderSourceSha256||proof.nativeUpgraderFromVersion!=1||proof.nativeUpgraderToVersion!=10||proof.sourceNativeRun!="V228"||proof.sourceNativeArchiveSha256!="067fbccad3f7d3af56944298a07fb630e641d0af109185655e3d5b5f1006d894"||!proof.materialStageOnly||proof.sourceNativeCaseStatus!="REJECTED_ASSEMBLY_PREFLIGHT"||AssetSha(Path.Combine(package.resolvedPath,proof.upgraderRelativePath))!=proof.upgraderSourceSha256)throw new InvalidOperationException("Exact measured native material stage/version/upgrader/archive provenance differs. The original failed hierarchy result must stay explicit.");
            if(proof.bindings.Sum(b=>b.protectedProperties.Length)!=2544||proof.bindings.Sum(b=>b.convertedProperties.Length)!=6||proof.bindings.Sum(b=>b.addedProperties.Length)!=347||proof.bindings.Sum(b=>b.metadataChanges.Length)!=358||proof.bindings.Count(b=>b.originalSha256!=b.importedSha256)!=30||proof.publicShaderSources.Length!=2||proof.sourceLayouts.Length!=2)throw new InvalidOperationException("Exact51/2544/6/347/358 native import accounting required.");
            foreach(var shader in proof.publicShaderSources){string path=Path.Combine(package.resolvedPath,shader.relativePath);if(shader.package!=package.name||shader.mainColor!="_BaseColor"||AssetSha(path)!=shader.sha256||AssetDatabase.AssetPathToGUID(package.assetPath+"/"+shader.relativePath)!=shader.guid||!Regex.IsMatch(File.ReadAllText(Io(path)),@"\[MainColor\] _BaseColor"))throw new InvalidOperationException("Actual public URP shader/BaseColor main-color authority changed.");}
            foreach(var layout in proof.sourceLayouts)if((layout.path!=GardenAssembly&&layout.path!=NaturalAssembly)||AssetSha(layout.path)!=layout.sha256||AssetSha(layout.path+".meta")!=layout.metaSha256||AssetDatabase.AssetPathToGUID(layout.path)!=layout.guid)throw new InvalidOperationException("Exact source author layout/GUID changed.");return proof;
        }
        static string AssemblyMaterialMetadata(string text,string name)
        {
            if(name=="URP.AssetVersion.version"){var v=Regex.Match(text,@"(?m)^  version: (\d+)");return v.Success?v.Groups[1].Value:"<absent>";}var material=Regex.Split(text,@"(?m)^--- !u!").Single(b=>b.StartsWith("21 ",StringComparison.Ordinal));var matches=Regex.Matches(material,@"(?m)^  ([A-Za-z_][A-Za-z_0-9]*):").Cast<Match>().ToArray();for(int i=0;i<matches.Length;i++)if(matches[i].Groups[1].Value==name){int start=matches[i].Index+matches[i].Length,end=i+1<matches.Length?matches[i+1].Index:material.Length;return material.Substring(start,end-start).Trim().Replace("\r\n","\n");}return "<absent>";
        }
        static Color ParseAssemblyMaterialColor(string value)
        {float Number(string name)=>float.Parse(Regex.Match(value,name+@": ([^,} ]+)").Groups[1].Value,CultureInfo.InvariantCulture);return new Color(Number("r"),Number("g"),Number("b"),Number("a"));}
        static void ValidateAssemblyMaterialBinding(AssemblyMaterialProvenance proof,AssemblyMaterialBinding binding)
        {
            if(!binding.path.StartsWith(DragonKingdomWorldAuthoring.Private,StringComparison.Ordinal)||!binding.path.EndsWith(".mat",StringComparison.Ordinal)||binding.snapshot!=Root+"PrivateLicensedEnvironment/DerivedFinishAssemblies/OriginalMaterialSources/"+binding.guid+".bytes"||binding.originalSha256!=binding.snapshotSha256||AssetSha(binding.snapshot)!=binding.originalSha256||AssetSha(binding.snapshot+".meta")!=binding.snapshotMetaSha256||AssetDatabase.AssetPathToGUID(binding.snapshot)!=binding.snapshotGuid||AssetDatabase.AssetPathToGUID(binding.path)!=binding.guid||AssetSha(binding.path+".meta")!=binding.metaSha256||AssetSha(binding.path)!=binding.importedSha256)throw new InvalidOperationException("Exact original/native/material/metadata/GUID byte proof differs: "+binding.path);
            string original=File.ReadAllText(Io(binding.snapshot)),current=File.ReadAllText(Io(binding.path));foreach(var property in binding.protectedProperties)if(PropertyText(original,property)!=property.sourceValue||PropertyText(current,property)!=property.sourceValue)throw new InvalidOperationException("Protected authored material field changed: "+binding.path+"/"+property.name);
            var material=AssetDatabase.LoadAssetAtPath<Material>(binding.path);if(material==null)throw new InvalidOperationException("Actual source Material required.");
            foreach(var property in binding.convertedProperties)
            {
                string name=Path.GetFileName(binding.path);bool alias=property.name=="_Color"&&new[]{"M_CLR_blue.mat","M_CLR_yellow_E.mat","M_FX_steam.mat","M_PROP_paper.mat"}.Contains(name),emission=property.name=="_EmissionColor"&&(name=="M_CLR_blue.mat"||name=="M_CLR_yellow_E.mat");if(property.kind!="Color"||(!alias&&!emission)||PropertyText(original,property)!=property.sourceValue||PropertyText(current,property)!=property.importedValue)throw new InvalidOperationException("Unreviewed51-material conversion.");
                string shaderGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material.shader));if(!proof.publicShaderSources.Any(s=>s.guid==shaderGuid)||!material.HasProperty("_BaseColor"))throw new InvalidOperationException("Public actual URP main-color shader binding required.");
                Color expected=alias?ParseAssemblyMaterialColor(PropertyText(original,new ImportedProperty{kind="Color",name="_BaseColor"})):ParseAssemblyMaterialColor(property.sourceValue).linear;Color actual=ParseAssemblyMaterialColor(property.importedValue);if(Mathf.Abs(actual.r-expected.r)>.00002f||Mathf.Abs(actual.g-expected.g)>.00002f||Mathf.Abs(actual.b-expected.b)>.00002f||Mathf.Abs(actual.a-expected.a)>.00002f)throw new InvalidOperationException("Actual public BaseColor/Color.linear conversion differs.");if(alias&&PropertyText(current,new ImportedProperty{kind="Color",name="_BaseColor"})!=PropertyText(original,new ImportedProperty{kind="Color",name="_BaseColor"}))throw new InvalidOperationException("Legacy color sync changed authored BaseColor.");
            }
            var sourceKeys=new HashSet<string>(OriginalMaterialProperties(original).Select(p=>p.kind+"/"+p.name));foreach(var property in binding.addedProperties)if(property.sourceValue!="<absent>"||sourceKeys.Contains(property.kind+"/"+property.name)||PropertyText(current,property)!=property.importedValue||property.reason!="EXACT_NATIVE_PUBLIC_URP_DEFAULT_FIELD_ADDITION")throw new InvalidOperationException("Exact native added-default material field differs.");
            var allowed=new HashSet<string>(new[]{"serializedVersion","m_ShaderKeywords","m_Parent","m_ModifiedSerializedProperties","m_ValidKeywords","m_InvalidKeywords","m_CustomRenderQueue","disabledShaderPasses","m_LockedProperties","m_BuildTextureStacks","m_AllowLocking","URP.AssetVersion.version"});foreach(var change in binding.metadataChanges)if(!allowed.Contains(change.name)||AssemblyMaterialMetadata(original,change.name)!=change.sourceValue||AssemblyMaterialMetadata(current,change.name)!=change.importedValue)throw new InvalidOperationException("Exact native queue/version/keyword/pass/serialization metadata migration differs.");
            if(AssemblyMaterialMetadata(original,"m_CustomRenderQueue")!=binding.originalRenderQueue.ToString(CultureInfo.InvariantCulture)||AssemblyMaterialMetadata(current,"m_CustomRenderQueue")!=binding.importedRenderQueue.ToString(CultureInfo.InvariantCulture))throw new InvalidOperationException("Exact native queue proof differs.");
        }
        static void PrepareApprovedAssemblyMaterialImports()
        {
            var proof=ReadAssemblyMaterialProvenance();foreach(var binding in proof.bindings){string current=AssetSha(binding.path);if(current!=binding.originalSha256&&current!=binding.importedSha256)throw new InvalidOperationException("Source material differs from exact original and approved measured native output.");if(current==binding.originalSha256)foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(binding.path))if(asset!=null)AssetDatabase.SaveAssetIfDirty(asset);ValidateAssemblyMaterialBinding(proof,binding);}
        }
        public static void CaptureExact51AssemblyMaterialProof()
        {
            PrepareApprovedArtistMaterialImports();PrepareApprovedAssemblyMaterialImports();var proof=ReadAssemblyMaterialProvenance();var layout=ReadAssemblySource(GardenAssembly);var bindings=proof.bindings.ToDictionary(b=>b.path);var evidence=new AssemblyMaterialEvidence{provenance=AssemblyImportProvenance,provenanceSha256=AssetSha(AssemblyImportProvenance),provenanceGuid=AssetDatabase.AssetPathToGUID(AssemblyImportProvenance),provenanceMetaSha256=AssetSha(AssemblyImportProvenance+".meta"),sourceNativeRun=proof.sourceNativeRun,sourceNativeArchiveSha256=proof.sourceNativeArchiveSha256,bindings=proof.bindings,rawMaterialSnapshots=51,approvedNativeImports=51,protectedProperties=2544,convertedProperties=6,addedProperties=347,metadataChanges=358};
            foreach(var dependency in layout.dependencies){if(AssetSha(dependency.path+".meta")!=dependency.metaSha256||AssetDatabase.AssetPathToGUID(dependency.path)!=dependency.guid)throw new InvalidOperationException("Exact source metadata/GUID differs.");evidence.exactMetadataAndGuids++;if(!bindings.ContainsKey(dependency.path)){if(AssetSha(dependency.path)!=dependency.sha256)throw new InvalidOperationException("Exact nonmaterial author asset changed.");evidence.exactNonmaterialDependencies++;}}
            evidence.passed=evidence.exactNonmaterialDependencies==267&&evidence.exactMetadataAndGuids==318;if(!evidence.passed)throw new InvalidOperationException("Exact source318 closure differs.");LastAssemblyMaterialEvidence=evidence;Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"exact51-assembly-material-proof.json"),JsonUtility.ToJson(evidence,true));
        }
    }
}
#endif
