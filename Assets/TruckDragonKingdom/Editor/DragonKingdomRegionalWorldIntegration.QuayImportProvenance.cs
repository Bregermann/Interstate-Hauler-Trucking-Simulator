#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const string QuayFvpMaterials=Root+"PrivateLicensedEnvironment/Plugins/Tidal Flask Studios/Fantastic Village Pack/materials/";
        const string QuayCracks="Assets/TruckDragonEggDelivery/Materials/Dark visible shell cracks.mat";
        const string QuayDragonFbx=Root+"PrivateLicensedDragons/Monsters Ultimate Pack 02 Cute Series/";
        const string QuayVersionTemplateSha="228831cdcb22e317cec0d61a1a1ee3549687ecae83986323a2074c3d4ad4a334";
        sealed class QuayImportPair
        {public string path,before,after;public QuayImportPair(string path,string before,string after){this.path=path;this.before=before;this.after=after;}}
        static readonly QuayImportPair[] QuayExtraMaterials={
            new QuayImportPair(QuayCracks,"b408d7c0d79df37ba29f40b3a84faa9100f459b280f001ff585163805839e278",null),
            new QuayImportPair(QuayFvpMaterials+"M_CLR_black.mat","cebf853826f7dd071834316c9a133ae92392402305042d3855ec71ae2b051494","b106ce539f2ab9d3c01f648b1d7672e44b7e1207b35ace5f602920b167a8b8c6"),
            new QuayImportPair(QuayFvpMaterials+"M_leather.mat","7af6186881e1211b03b1fffd0ce5298287b6e638b303c29c95e43733f69ce16d","9810dd1d5c9426576d4900dd0de1e0bab940b8e8497849119cd7336802a1842a"),
            new QuayImportPair(QuayFvpMaterials+"M_rooftiles_03.mat","f53e988fcc7b54d0ac60bfc8f6a334b78de56d8cfdb815e8c53e0192684e0cc9","a5ce6aeb5177bbcc9e5df7e4ae4eeb15f831e2bdcc9ed6344cb1b9b012d18f52")};
        static readonly QuayImportPair[] QuayExtraMetadata={
            new QuayImportPair(QuayDragonFbx+"Dragon Fire Cute Series/FBX/Dragon Fire.FBX.meta","dbbb962e749065a4cef1fc69775756318c06627b45b82f704d5b5132d02aecb5","7dd861c62f1430b808d16f3597fbd38c158ada397e376c567a4ae8d20961aab8"),
            new QuayImportPair(QuayDragonFbx+"Dragon Inferno Cute Series/FBX/Dragon Inferno.FBX.meta","e5ee9d5189e4084a1a2a4f610595d04f122204132cf8232e21cace2c0699d78b","13caec2a45c55624772b4e6c340857668b94b1888ab9cea92f0d48830956eaf6"),
            new QuayImportPair(QuayDragonFbx+"Dragon Spark Cute Series/FBX/Dragon Spark.FBX.meta","d24f77c80c290db544ec5ef0c04ce2a52bc71e7dcd8830d6e76c1448e56fbcd2","359a2241ea7a47e87c16cb4ebb45ee24f458d0d5592fb276de83c4732e8620a9"),
            new QuayImportPair(Root+"PrivateLicensedEnvironment/Fantastic Ancient Forest/Shaders/S_foliage_wind_URP_advanced_lit_emission_U6-1.shader.meta","a887211070fbd58c331b98aba5535d9c55d2eb1b835adf2bc464f717629416e3","fc9444c6239ddee359ab0953f142f1bb9e16490fa085a7c1a0f4fdd5f595e633"),
            new QuayImportPair(Root+"ReviewCandidates/RoundedVista01/Foundation Receipt.json.meta","078ea910c53413564c9d955812c1145cb123d5d28cd7b1a2b777d9584fabf592","20c7cbf12bc3d15975a70df603bd1882ad5c0df4a953ba330fe90b6eef1bbc89")};
        [Serializable]public sealed class QuayImportChange
        {public string path,beforeSha256,afterSha256,rule,nativeVersionLocalId;public bool authoredDataPreserved;}
        [Serializable]public sealed class QuayImportEvidence
        {
            public string scope="EXACT_V247_NATIVE_IMPORT_BOOKKEEPING_BEFORE_IMMUTABLE_QUAY_GEOMETRY_BASELINE_NOT_WORLD_OR_VISUAL_ACCEPTANCE";
            public bool passed,allUnknownChangesRejected,protectedModelIdentitiesExact,nonMaterialSourceBytesExact;public int watchedPayloads,protectedModels,sourceFilesWatched,protectedOtherSourceFiles;public QuayImportChange[] changes;public QuayModelImportIdentity[] models;
        }
        [Serializable]public sealed class QuayModelImportIdentity
        {public string path,signature;public int persistentAssets,meshes,clips,hierarchyTransforms;}
        public static QuayModelImportIdentity[] LastQuayProtectedImportModels{get;private set;}=Array.Empty<QuayModelImportIdentity>();
        public static string[] QuayNativeImportSourcePaths()=>QuayExtraMaterials.Select(p=>p.path).Concat(QuayExtraMetadata.Select(p=>p.path.Substring(0,p.path.Length-5))).ToArray();
        static string QuaySha(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static string QuaySha(string text)=>QuaySha(Encoding.UTF8.GetBytes(text));
        static string QuayFloat(float value)=>BitConverter.ToString(BitConverter.GetBytes(value)).Replace("-","");
        static string QuayPersistent(UnityEngine.Object value)
        {if(value==null)return "NONE";if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id)||guid.Length!=32)throw new InvalidOperationException("Actual persistent native model/clip/hierarchy identity required.");return guid+":"+id.ToString(CultureInfo.InvariantCulture)+":"+value.GetType().FullName;}

        public static Dictionary<string,string> CaptureQuayProtectedImportIdentities()
        {
            var result=new Dictionary<string,string>(StringComparer.Ordinal);var models=new List<QuayModelImportIdentity>();
            foreach(var pair in QuayExtraMetadata.Take(3))
            {
                string path=pair.path.Substring(0,pair.path.Length-5);var text=new StringBuilder();var assets=AssetDatabase.LoadAllAssetsAtPath(path).Where(a=>a!=null).OrderBy(QuayPersistent,StringComparer.Ordinal).ToArray();
                int meshes=assets.Count(a=>a is Mesh),clips=assets.Count(a=>a is AnimationClip);if(meshes==0)throw new InvalidOperationException("Actual original dragon mesh required: "+path);
                // These are main model FBXs. Their action clips may be separate
                // source FBXs; an honest zero here is protected, not invented.
                text.Append("ASSETS:").Append(assets.Length).Append(";MESHES:").Append(meshes).Append(";CLIPS:").Append(clips).Append('\n');
                foreach(var asset in assets)
                {
                    text.Append(QuayPersistent(asset)).Append('|').Append(asset.name).Append('\n');
                    if(asset is Mesh mesh)
                    {
                        using(var data=Mesh.AcquireReadOnlyMeshData(mesh))
                        {for(int stream=0;stream<data[0].vertexBufferCount;stream++)text.Append("VB").Append(stream).Append(':').Append(QuaySha(data[0].GetVertexData<byte>(stream).ToArray()));text.Append("IB:").Append(QuaySha(data[0].GetIndexData<byte>().ToArray()));for(int sub=0;sub<data[0].subMeshCount;sub++){var d=data[0].GetSubMesh(sub);text.Append('|').Append(d.indexStart).Append('/').Append(d.indexCount).Append('/').Append(d.baseVertex).Append('/').Append(d.firstVertex).Append('/').Append(d.vertexCount).Append('/').Append((int)d.topology);}}
                        foreach(var matrix in mesh.bindposes)for(int i=0;i<16;i++)text.Append(QuayFloat(matrix[i]));
                    }
                    else if(asset is AnimationClip clip)
                    {
                        text.Append(QuayFloat(clip.length)).Append('/').Append(QuayFloat(clip.frameRate)).Append('/').Append(clip.legacy).Append('/').Append(clip.wrapMode);
                        foreach(var binding in AnimationUtility.GetCurveBindings(clip).OrderBy(b=>b.path+"/"+b.type.FullName+"/"+b.propertyName,StringComparer.Ordinal))
                        {var curve=AnimationUtility.GetEditorCurve(clip,binding);text.Append(binding.path).Append('|').Append(binding.type.FullName).Append('|').Append(binding.propertyName).Append('|').Append(curve.preWrapMode).Append('|').Append(curve.postWrapMode);foreach(var key in curve.keys)text.Append(QuayFloat(key.time)).Append(QuayFloat(key.value)).Append(QuayFloat(key.inTangent)).Append(QuayFloat(key.outTangent)).Append(QuayFloat(key.inWeight)).Append(QuayFloat(key.outWeight)).Append((int)key.weightedMode);}
                        foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip).OrderBy(b=>b.path+"/"+b.propertyName,StringComparer.Ordinal)){text.Append(binding.path).Append('|').Append(binding.type.FullName).Append('|').Append(binding.propertyName);foreach(var key in AnimationUtility.GetObjectReferenceCurve(clip,binding))text.Append(QuayFloat(key.time)).Append(QuayPersistent(key.value));}
                        foreach(var e in AnimationUtility.GetAnimationEvents(clip))text.Append(QuayFloat(e.time)).Append('|').Append(e.functionName).Append('|').Append(e.stringParameter).Append('|').Append(e.intParameter).Append('|').Append(QuayFloat(e.floatParameter)).Append('|').Append(QuayPersistent(e.objectReferenceParameter));
                    }
                }
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(root==null)throw new InvalidOperationException("Actual native imported hierarchy required.");
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {text.Append(QuayPersistent(t)).Append('|').Append(t.name).Append('|').Append(QuayPersistent(t.parent)).Append('|').Append(t.GetSiblingIndex()).Append('|').Append(t.gameObject.activeSelf);foreach(float value in new[]{t.localPosition.x,t.localPosition.y,t.localPosition.z,t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w,t.localScale.x,t.localScale.y,t.localScale.z})text.Append(QuayFloat(value));}
                foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){text.Append(QuayPersistent(skin.sharedMesh)).Append(QuayPersistent(skin.rootBone));foreach(var bone in skin.bones)text.Append(QuayPersistent(bone));}
                string signature=QuaySha(text.ToString());result.Add(path,signature);models.Add(new QuayModelImportIdentity{path=path,signature=signature,persistentAssets=assets.Length,meshes=meshes,clips=clips,hierarchyTransforms=root.GetComponentsInChildren<Transform>(true).Length});
            }
            LastQuayProtectedImportModels=models.ToArray();return result;
        }

        public static void PrepareApprovedQuayImports()
        {
            PrepareApprovedArtistMaterialImports();PrepareApprovedAssemblyMaterialImports();
            foreach(var pair in QuayExtraMetadata)
            {
                string current=AssetSha(pair.path);if(current==pair.after)continue;if(current!=pair.before)throw new InvalidOperationException("Metadata differs from exact original and approved native state: "+pair.path);
                string assetPath=pair.path.Substring(0,pair.path.Length-5);AssetDatabase.LoadAllAssetsAtPath(assetPath);var importer=AssetImporter.GetAtPath(assetPath);if(importer==null)throw new InvalidOperationException("Actual public native importer required: "+assetPath);importer.SaveAndReimport();
                if(AssetSha(pair.path)!=pair.after)throw new InvalidOperationException("Public native importer did not produce the exact reviewed metadata: "+pair.path);
            }
            foreach(var pair in QuayExtraMaterials)
            {
                byte[] bytes=File.ReadAllBytes(Io(pair.path));string current=QuaySha(bytes);
                if(pair.path==QuayCracks){if(current!=pair.before)ValidateQuayCracksNative(bytes);}
                else if(current!=pair.before&&current!=pair.after)throw new InvalidOperationException("Extra quay material differs from both exact approved bytes: "+pair.path);
                // Only the vendor's existing public material import/save path;
                // no rewriting shader fields or manually fabricating subassets.
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(pair.path))if(asset!=null)AssetDatabase.SaveAssetIfDirty(asset);
            }
        }
        // Production and diagnostics share this bounded preparation authority.
        // It does not open/save scenes, generate a world or mutate unrelated
        // assets. The caller freezes its larger geometry baseline afterwards.
        public static QuayImportEvidence PrepareValidatedQuayNativeImports()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required for explicit quay import preflight.");
            var primaries=QuayNativeImportSourcePaths().Concat(ReadMaterialProvenance().bindings.Select(b=>b.path)).Concat(ReadAssemblyMaterialProvenance().bindings.Select(b=>b.path)).Distinct(StringComparer.Ordinal).ToArray();
            var paths=primaries.Concat(primaries.SelectMany(p=>AssetDatabase.GetDependencies(p,true))).Distinct(StringComparer.Ordinal).SelectMany(p=>new[]{p,p+".meta"}).Distinct(StringComparer.Ordinal).Where(p=>File.Exists(Io(p))).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            if(primaries.Any(p=>!paths.Contains(p,StringComparer.Ordinal)||!paths.Contains(p+".meta",StringComparer.Ordinal)))throw new InvalidOperationException("Every exact normalization source and metadata must exist before preparation.");
            bool Payload(string p)=>p.EndsWith(".mat",StringComparison.Ordinal)||p.EndsWith(".meta",StringComparison.Ordinal);
            var beforeHashes=paths.ToDictionary(p=>p,AssetSha,StringComparer.Ordinal);
            var beforePayloads=paths.Where(Payload).ToDictionary(p=>p,p=>File.ReadAllBytes(Io(p)),StringComparer.Ordinal);
            var beforeIdentities=CaptureQuayProtectedImportIdentities();
            PrepareApprovedQuayImports();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var afterIdentities=CaptureQuayProtectedImportIdentities();
            var afterPayloads=paths.Where(Payload).ToDictionary(p=>p,p=>File.ReadAllBytes(Io(p)),StringComparer.Ordinal);
            var evidence=ValidateQuayNativeImportPreflight(beforePayloads,afterPayloads,beforeIdentities,afterIdentities);
            foreach(var path in paths.Where(p=>!Payload(p)))
                if(!File.Exists(Io(path))||AssetSha(path)!=beforeHashes[path])throw new InvalidOperationException("Import preparation changed actual source content rather than approved native bookkeeping: "+path);
            // Reject a late deferred save between the validator and baseline.
            foreach(var pair in afterPayloads)
                if(!File.Exists(Io(pair.Key))||AssetSha(pair.Key)!=QuaySha(pair.Value))throw new InvalidOperationException("Native import payload changed after its exact recipe validation: "+pair.Key);
            evidence.sourceFilesWatched=paths.Length;evidence.protectedOtherSourceFiles=paths.Count(p=>!Payload(p));evidence.nonMaterialSourceBytesExact=true;return evidence;
        }
        static string ReplaceQuayOnce(string text,string oldValue,string replacement)
        {int first=text.IndexOf(oldValue,StringComparison.Ordinal);if(first<0||text.IndexOf(oldValue,first+oldValue.Length,StringComparison.Ordinal)>=0)throw new InvalidOperationException("Exact native importer bookkeeping source field required.");return text.Substring(0,first)+replacement+text.Substring(first+oldValue.Length);}
        static void ValidateQuayMetadataRecipe(QuayImportPair pair,byte[] before,byte[] after)
        {
            if(QuaySha(before)!=pair.before||QuaySha(after)!=pair.after)throw new InvalidOperationException("Exact five native metadata before/after hashes required: "+pair.path);
            string source=Encoding.UTF8.GetString(before),expected=source;
            if(pair.path.EndsWith(".FBX.meta",StringComparison.Ordinal))
            {
                expected=ReplaceQuayOnce(expected,"ModelImporter:\n  serializedVersion: 22200\n","ModelImporter:\n  serializedVersion: 24200\n");
                expected=ReplaceQuayOnce(expected,"    rigImportErrors: \n","");expected=ReplaceQuayOnce(expected,"    rigImportWarnings: \n","");
                expected=ReplaceQuayOnce(expected,"    optimizeBones: 1\n","    optimizeBones: 1\n    generateMeshLods: 0\n    meshLodGenerationFlags: 0\n    maximumMeshLod: -1\n");
                var references=Regex.Matches(expected,@"(?m)^  referencedClips:\n(?:  - [0-9a-f]{32}\n)+");if(references.Count!=1)throw new InvalidOperationException("Exact deprecated reference bookkeeping block required; active clip settings are protected.");var match=references[0];expected=expected.Substring(0,match.Index)+"  referencedClips: []\n"+expected.Substring(match.Index+match.Length);
            }
            else expected+=pair.path.EndsWith(".shader.meta",StringComparison.Ordinal)?"\nShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n":"\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n";
            if(!Encoding.UTF8.GetBytes(expected).SequenceEqual(after))throw new InvalidOperationException("Unknown importer settings, internalIDToNameTable, clip or content-reference change: "+pair.path);
        }
        static string ValidateQuayCracksNative(byte[] bytes)
        {
            string text=Encoding.UTF8.GetString(bytes);var headers=Regex.Matches(text,@"(?m)^--- !u!(\d+) &(-?\d+)\n").Cast<Match>().ToArray();if(headers.Length!=2||headers.Select(h=>h.Groups[1].Value).OrderBy(s=>s,StringComparer.Ordinal).SequenceEqual(new[]{"114","21"})==false)throw new InvalidOperationException("Owned cracks permits only original Material plus one public native version subasset.");
            int mi=Array.FindIndex(headers,h=>h.Groups[1].Value=="21"),vi=1-mi;var material=headers[mi];var version=headers[vi];string document=text.Substring(material.Index,(mi+1<headers.Length?headers[mi+1].Index:text.Length)-material.Index),prefix=text.Substring(0,headers[0].Index);
            if(material.Groups[2].Value!="2100000"||QuaySha(prefix+document)!=QuayExtraMaterials[0].before)throw new InvalidOperationException("Original owned Material2100000 bytes must remain exact.");
            string metadata=text.Substring(version.Index,(vi+1<headers.Length?headers[vi+1].Index:text.Length)-version.Index),canonical=Regex.Replace(metadata,@"\A--- !u!114 &-?\d+\n","--- !u!114 &0\n");
            if(QuaySha(canonical)!=QuayVersionTemplateSha||!long.TryParse(version.Groups[2].Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out long local)||local==0||local==2100000)throw new InvalidOperationException("Only the exact public URP AssetVersion10 bookkeeping template may vary its newly generated header localID.");
            var assets=AssetDatabase.LoadAllAssetsAtPath(QuayCracks).Where(a=>a!=null).ToArray();var native=assets.Where(a=>a.GetType().FullName=="UnityEditor.Rendering.Universal.AssetVersion"&&a.GetType().Assembly.GetName().Name=="Unity.RenderPipelines.Universal.Editor").ToArray();
            if(assets.Length!=2||native.Length!=1||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(native[0],out string guid,out long actual)||actual!=local||guid!=AssetDatabase.AssetPathToGUID(QuayCracks)||new SerializedObject(native[0]).FindProperty("version")?.intValue!=10)throw new InvalidOperationException("Actual persistent public native AssetVersion10 identity must match its one new serialized document.");return local.ToString(CultureInfo.InvariantCulture);
        }
        static void ValidateQuayExtraAuthoredProperties(QuayImportPair pair,byte[] before,byte[] after)
        {
            if(QuaySha(before)!=pair.before||QuaySha(after)!=pair.after)throw new InvalidOperationException("Exact extra FVP material before/after hash required.");string source=Encoding.UTF8.GetString(before),current=Encoding.UTF8.GetString(after);
            foreach(var property in OriginalMaterialProperties(source))
            {
                string value=PropertyText(current,property);if(value==property.sourceValue)continue;
                if(pair.path!=QuayFvpMaterials+"M_CLR_black.mat"||property.kind!="Color"||(property.name!="_Color"&&property.name!="_EmissionColor"))throw new InvalidOperationException("Unexpected authored extra quay shader/texture/UV/color/scalar change.");
                Color expected=property.name=="_Color"?ParseAssemblyMaterialColor(PropertyText(source,new ImportedProperty{kind="Color",name="_BaseColor"})):ParseAssemblyMaterialColor(property.sourceValue).linear;Color actual=ParseAssemblyMaterialColor(value);
                if(Mathf.Abs(expected.r-actual.r)>.00002f||Mathf.Abs(expected.g-actual.g)>.00002f||Mathf.Abs(expected.b-actual.b)>.00002f||Mathf.Abs(expected.a-actual.a)>.00002f)throw new InvalidOperationException("Extra material must use the existing public BaseColor alias/HDR.linear conversion.");
            }
        }
        public static QuayImportEvidence ValidateQuayNativeImportPreflight(Dictionary<string,byte[]> before,Dictionary<string,byte[]> after,Dictionary<string,string> identitiesBefore,Dictionary<string,string> identitiesAfter)
        {
            if(before==null||after==null||!new HashSet<string>(before.Keys,StringComparer.Ordinal).SetEquals(after.Keys)||identitiesBefore==null||identitiesAfter==null||identitiesBefore.Count!=3||identitiesAfter.Count!=3||identitiesBefore.Any(p=>!identitiesAfter.TryGetValue(p.Key,out string hash)||p.Value!=hash||!Regex.IsMatch(hash,@"\A[0-9a-f]{64}\z"))||QuayExtraMetadata.Take(3).Any(p=>!identitiesBefore.ContainsKey(p.path.Substring(0,p.path.Length-5))))throw new InvalidOperationException("Complete same native payloads and exact persistent model/mesh/clip/hierarchy identities required.");
            var known=new Dictionary<string,QuayImportPair>(StringComparer.Ordinal);
            foreach(var b in ReadMaterialProvenance().bindings.Select(b=>new QuayImportPair(b.path,b.originalSha256,b.importedSha256)).Concat(ReadAssemblyMaterialProvenance().bindings.Select(b=>new QuayImportPair(b.path,b.originalSha256,b.importedSha256))))
            {if(known.TryGetValue(b.path,out var prior)&&(prior.before!=b.before||prior.after!=b.after))throw new InvalidOperationException("Conflicting old19/51 approved material provenance.");known[b.path]=b;}
            var changes=new List<QuayImportChange>();
            foreach(var path in before.Keys.OrderBy(p=>p,StringComparer.Ordinal))
            {
                byte[] raw=before[path],native=after[path];if(raw.SequenceEqual(native))continue;string a=QuaySha(raw),b=QuaySha(native),rule,version=null;
                var meta=QuayExtraMetadata.SingleOrDefault(p=>p.path==path);var extra=QuayExtraMaterials.SingleOrDefault(p=>p.path==path);
                if(meta!=null){ValidateQuayMetadataRecipe(meta,raw,native);rule="EXACT_FIVE_NATIVE_IMPORTER_BOOKKEEPING_RECIPES_ALL_AUTHORED_SETTINGS_AND_FILE_IDS_PRESERVED";}
                else if(extra!=null)
                {
                    if(path==QuayCracks){if(a!=extra.before)ValidateQuayCracksNative(raw);version=ValidateQuayCracksNative(native);rule="OWNED_MATERIAL_BODY_BYTE_EXACT_ONLY_PUBLIC_ASSETVERSION10_NEW_HEADER_ID";}
                    else{ValidateQuayExtraAuthoredProperties(extra,raw,native);rule="EXACT_THREE_FVP_NATIVE_BYTES_PROTECTED_AUTHORED_FIELDS_PUBLIC_COLOR_CONVERSIONS_ONLY";}
                }
                else if(known.TryGetValue(path,out var pair)&&a==pair.before&&b==pair.after)rule="EXISTING_EXACT_19_OR_51_MATERIAL_PROVENANCE_UNCHANGED";
                else throw new InvalidOperationException("Unknown native preflight source change rejected: "+path+" before="+a+" after="+b);
                if(File.Exists(Io(path))&&AssetSha(path)!=b)throw new InvalidOperationException("Captured native output no longer matches current actual source bytes: "+path);
                changes.Add(new QuayImportChange{path=path,beforeSha256=a,afterSha256=b,rule=rule,nativeVersionLocalId=version,authoredDataPreserved=true});
            }
            foreach(var pair in QuayExtraMetadata)if(!after.TryGetValue(pair.path,out var bytes)||QuaySha(bytes)!=pair.after)throw new InvalidOperationException("Five native importer migrations must be settled before geometry baseline: "+pair.path);
            foreach(var pair in QuayExtraMaterials)
            {if(!after.TryGetValue(pair.path,out var bytes))throw new InvalidOperationException("All four exact extra material payloads must be watched.");if(pair.path==QuayCracks)ValidateQuayCracksNative(bytes);else if(QuaySha(bytes)!=pair.after)throw new InvalidOperationException("Exact extra public native material output required.");}
            return new QuayImportEvidence{passed=true,allUnknownChangesRejected=true,protectedModelIdentitiesExact=true,watchedPayloads=before.Count,protectedModels=3,changes=changes.ToArray(),models=LastQuayProtectedImportModels};
        }
    }
}
#endif
