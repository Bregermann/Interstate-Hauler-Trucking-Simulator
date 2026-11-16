#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string GardenAssembly=Root+"PrivateLicensedEnvironment/DerivedFinishAssemblies/Village Garden Court v1.json";
        public const string NaturalAssembly=Root+"PrivateLicensedEnvironment/DerivedFinishAssemblies/Autumn Roadside Assembly v1.json";
        const string GardenAssemblySha="df4f949e293852f8f14885f0d59df166a367baa06dcaf229558c0102d3ced7fe",NaturalAssemblySha="7916ccb9daf0f9c304aa7f76df631894338f8cd40096c51ed066df37e63272c3";
        [Serializable]public sealed class AssemblySource
        {public int schemaVersion;public string name,scope,sourceScene,sourceSceneSha256,sourceSceneMetaSha256;public Vector3 center;public float radius;public AssemblySourceRow[] instances;public ArtistDependency[] dependencies;public AssemblyRawMaterial[] originalMaterials;}
        [Serializable]public sealed class AssemblySourceRow
        {public string sceneInstanceId,prefabGuid,prefab,sourceSha256,sourceMetaSha256,sourceParent;public Vector3 position,scale;public Quaternion rotation;public AssemblyOverride[] overrides;}
        [Serializable]public sealed class AssemblyOverride
        {public string targetId,targetGuid,property,sourceValue,objectReference;}
        [Serializable]public sealed class AssemblyRawMaterial
        {public string path,source,sourceGuid,sha256,metaSha256,guid;}
        [Serializable]public sealed class AssemblyOverrideEvidence
        {public string instance,targetId,targetGuid,property,rawValue,rawFloatBits,actualType,state,assignedAssetGuid,assignedAssetLocalId,sourceGlobalId,instanceGlobalId,nativeIdentityMethod,sourceHierarchySiblingPath;public bool applied,disabledPublicLightOverride,appliedValueVerified;public string actualValue;public int targetMatches,sourceObjectCount,instanceObjectCount,missingScripts;}
        [Serializable]public sealed class AssemblyMaterialObservation
        {public string source,guid,originalSha256,nativeSha256,metaSha256,snapshot,shader;public int protectedProperties;public ImportedProperty[] unchangedProperties,changedProperties;public bool protectedShaderAndTextureBindingsExact;}
        [Serializable]public sealed class AssemblyGroupObservation{public string layout,layoutSha256,name;public int instances;public Bounds sourceBounds;}
        [Serializable]public sealed class AssemblyEffectObservation{public string group,instance,name;public bool active,playOnAwake,loop,emissionEnabled;public string[] materials;}
        [Serializable]public sealed class AssemblyPreflight
        {public string scope="EXACT_NATIVE_AUTHORED_ASSEMBLIES_COMPONENT_AND_MATERIAL_OBSERVATION_NOT_WORLD_INTEGRATION_OR_VISUAL_APPROVAL",status="IN_PROGRESS",error;public int groups,instances,overrides,appliedOverrides,inactiveLightFields,obsoleteNativeAbsentFields,materials,exactNonmaterials,exactMetadata;public bool nativeSourcesPreserved,missingLegacyTextureIsInactive;public string legacyMaterial,legacyProperty,actualAlbedoGuid;public AssemblyOverrideEvidence[] overrideEvidence;public AssemblyMaterialObservation[] materialObservations;public AssemblyGroupObservation[] sourceGroups;public NativeAssemblyTarget[] nativeTargetProof;public AssemblyCoverage nativeTargetCoverage;public AssemblyEffectObservation[] actualEffects;}
        public static AssemblyPreflight LastAssemblyPreflight{get;private set;}
        static AssemblySource ReadAssemblySource(string path)
        {
            string expected=path==GardenAssembly?GardenAssemblySha:path==NaturalAssembly?NaturalAssemblySha:null;if(expected==null||AssetSha(path)!=expected)throw new InvalidOperationException("Exact separately reviewed owned source assembly required.");
            var source=JsonUtility.FromJson<AssemblySource>(File.ReadAllText(Io(path)));int count=path==GardenAssembly?89:34,distinct=path==GardenAssembly?44:14;
            if(source.schemaVersion!=1||source.instances.Length!=count||source.instances.Select(r=>r.sceneInstanceId).Distinct().Count()!=count||source.instances.Select(r=>r.prefab).Distinct().Count()!=distinct||source.dependencies.Length!=318||source.originalMaterials.Length!=51)throw new InvalidOperationException("Exact source selection, material snapshots and dependency closure required.");
            foreach(var row in source.instances)if(new[]{row.position.x,row.position.y,row.position.z,row.rotation.x,row.rotation.y,row.rotation.z,row.rotation.w,row.scale.x,row.scale.y,row.scale.z}.Any(v=>float.IsNaN(v)||float.IsInfinity(v))||row.scale.x<=0||row.scale.y<=0||row.scale.z<=0)throw new InvalidOperationException("Invalid active authored source TRS.");return source;
        }
        static UnityEngine.Object[] AssemblyObjects(GameObject root)=>root.GetComponentsInChildren<Component>(true).Where(c=>c!=null).Cast<UnityEngine.Object>().Concat(root.GetComponentsInChildren<Transform>(true).Select(t=>(UnityEngine.Object)t.gameObject)).ToArray();
        static UnityEngine.Object ResolveOriginalAssemblyAsset(string reference,out string guid,out string localId)
        {
            var match=Regex.Match(reference,@"fileID: (-?\d+), guid: ([a-f0-9]{32})");if(!match.Success)throw new InvalidOperationException("Exact original material override asset GUID/localID required.");guid=match.Groups[2].Value;localId=match.Groups[1].Value;string g=guid,id=localId,path=AssetDatabase.GUIDToAssetPath(g);var found=AssetDatabase.LoadAllAssetsAtPath(path).Where(o=>o!=null&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string actualGuid,out long actualId)&&actualGuid==g&&actualId.ToString(CultureInfo.InvariantCulture)==id).ToArray();if(found.Length!=1)throw new InvalidOperationException("Original authored asset does not resolve uniquely: "+reference);return found[0];
        }
        [Serializable]public sealed class NativeAssemblyIdentity
        {public string type,siblingPath,guid,localId,globalId,globalGuid,globalObjectId,globalPrefabId,sourceGuid,sourceLocalId,originalGuid,originalLocalId;public bool persistent,isLight,usesLightBounds;public int componentSlot;public AssemblyIdentityLink[] correspondenceChain;}
        [Serializable]public sealed class NativeAssemblyTarget
        {public string prefab,prefabGuid,prefabSha256,requestedGuid,requestedLocalId;public int sourceMatches,instanceMatches,missingScripts;public NativeAssemblyIdentity[] sourceObjects,instanceObjects;}
        [Serializable]public sealed class NativeAssemblyIdentityDiagnostic
        {public string scope="READ_ONLY_ALL_NATIVE_CURRENT_GENERATED_AND_INHERITED_COMPONENT_IDENTITIES_FOR_EXACT_FOUR_DEMO_BOUND_TARGETS_NOT_ORPHAN_CLASSIFICATION",status="IN_PROGRESS",error;public bool sourceBytesPreserved;public NativeAssemblyTarget[] targets;}
        public static NativeAssemblyIdentityDiagnostic LastAssemblyIdentityDiagnostic{get;private set;}
        static NativeAssemblyIdentity DescribeActualAssemblyIdentity(GameObject root,UnityEngine.Object value)
        {
            var transform=value is GameObject go?go.transform:(value as Component)?.transform;var indices=new Stack<int>();for(var t=transform;t!=null&&t!=root.transform;t=t.parent)indices.Push(t.GetSiblingIndex());var global=GlobalObjectId.GetGlobalObjectIdSlow(value);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id);var source=PrefabUtility.GetCorrespondingObjectFromSource(value);var original=PrefabUtility.GetCorrespondingObjectFromOriginalSource(value);string sg=null,og=null;long si=0,oi=0;if(source!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out sg,out si);if(original!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out og,out oi);
            return new NativeAssemblyIdentity{type=value.GetType().FullName,siblingPath=string.Join("/",indices),guid=guid,localId=id.ToString(CultureInfo.InvariantCulture),globalId=global.ToString(),globalGuid=global.assetGUID.ToString(),globalObjectId=global.targetObjectId.ToString(CultureInfo.InvariantCulture),globalPrefabId=global.targetPrefabId.ToString(CultureInfo.InvariantCulture),sourceGuid=sg,sourceLocalId=si.ToString(CultureInfo.InvariantCulture),originalGuid=og,originalLocalId=oi.ToString(CultureInfo.InvariantCulture),correspondenceChain=AllNativeAssemblyCorrespondences(value),componentSlot=value is Component?Array.IndexOf(transform.GetComponents<Component>(),value):-1,persistent=EditorUtility.IsPersistent(value),isLight=value is Light,usesLightBounds=value is Light light&&light.useBoundingSphereOverride};
        }
        public static void CaptureOriginalAssemblyNativeIdentityDiagnostic()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped native Editor required.");var layout=ReadAssemblySource(GardenAssembly);var requested=layout.instances.SelectMany(row=>row.overrides.Where(c=>c.property.StartsWith("m_BoundingSphereOverride.",StringComparison.Ordinal)).Select(c=>new{row,change=c})).GroupBy(x=>x.row.prefab+"/"+x.change.targetGuid+"/"+x.change.targetId).Select(g=>g.First()).ToArray();if(requested.Length!=4)throw new InvalidOperationException("Exact four independently reviewed original garden bound targets required.");var files=layout.dependencies.SelectMany(d=>new[]{d.path,d.path+".meta"}).Concat(new[]{GardenAssembly,GardenAssembly+".meta"}).ToDictionary(path=>path,AssetSha);var isolated=EditorSceneManager.NewPreviewScene();var result=new NativeAssemblyIdentityDiagnostic();LastAssemblyIdentityDiagnostic=result;var targets=new List<NativeAssemblyTarget>();var instances=new List<GameObject>();
            try
            {
                foreach(var request in requested)
                {
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(request.row.prefab);if(source==null)throw new InvalidOperationException("Actual original source prefab missing.");var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,isolated);instances.Add(instance);var sourceObjects=AssemblyObjects(source);var sceneObjects=AssemblyObjects(instance);int missing=source.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))+instance.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                    targets.Add(new NativeAssemblyTarget{prefab=request.row.prefab,prefabGuid=request.row.prefabGuid,prefabSha256=AssetSha(request.row.prefab),requestedGuid=request.change.targetGuid,requestedLocalId=request.change.targetId,sourceMatches=sourceObjects.Count(o=>SourceIdentityMatches(o,request.change.targetGuid,request.change.targetId)),instanceMatches=sceneObjects.Count(o=>SourceIdentityMatches(o,request.change.targetGuid,request.change.targetId)),missingScripts=missing,sourceObjects=sourceObjects.Select(o=>DescribeActualAssemblyIdentity(source,o)).ToArray(),instanceObjects=sceneObjects.Select(o=>DescribeActualAssemblyIdentity(instance,o)).ToArray()});
                }
                result.targets=targets.ToArray();result.status="ACTUAL_NATIVE_IDENTITIES_CAPTURED_REQUIRE_EXACT_TARGET_REVIEW_NO_CLASSIFICATION";
            }
            catch(Exception exception){result.error=exception.ToString();result.status="REJECTED_NATIVE_IDENTITY_DIAGNOSTIC";throw;}
            finally{foreach(var instance in instances)if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(isolated);result.targets=targets.ToArray();result.sourceBytesPreserved=files.All(f=>AssetSha(f.Key)==f.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-native-assembly-target-identities.json"),JsonUtility.ToJson(result,true));if(!result.sourceBytesPreserved)throw new InvalidOperationException("Read-only native hierarchy identity diagnosis changed actual source bytes.");}
        }
        [Serializable]public sealed class AssemblyIdentityLink{public string guid,localId,globalGuid,globalObjectId,globalPrefabId;}
        [Serializable]public sealed class AssemblyCoverageField
        {public string instance,prefab,prefabGuid,prefabSha256,targetGuid,targetId,property,rawValue,objectReference,targetAssetPath,rawTargetHeaderClass;public int sourceMatches,instanceMatches,sourceChainMatches,instanceChainMatches,sourceObjects,instanceObjects,missingScripts;public bool exactApprovedObsoleteMetadata;public NativeAssemblyIdentity[] matchedSourceObjects,matchedInstanceObjects,sourceCandidateObjects;}
        [Serializable]public sealed class AssemblyCoverage
        {public string scope="READ_ONLY_COMPLETE_ALL55_AUTHOR_OVERRIDE_TARGET_NATIVE_COVERAGE_BEFORE_ANY_OVERRIDE_WRITE_NOT_AUTOMATIC_STALE_CLASSIFICATION",status="IN_PROGRESS",error;public int fields,directMissingFields,unapprovedMissingFields,ambiguousFields;public bool sourceBytesPreserved;public AssemblyCoverageField[] coverage;}
        public static AssemblyCoverage LastAssemblyCoverage{get;private set;}
        static AssemblyIdentityLink[] AllNativeAssemblyCorrespondences(UnityEngine.Object value)
        {
            var result=new List<AssemblyIdentityLink>();var visited=new HashSet<UnityEngine.Object>();var current=value;
            while(current!=null&&visited.Add(current))
            {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(current,out string guid,out long id);var global=GlobalObjectId.GetGlobalObjectIdSlow(current);result.Add(new AssemblyIdentityLink{guid=guid,localId=id.ToString(CultureInfo.InvariantCulture),globalGuid=global.assetGUID.ToString(),globalObjectId=global.targetObjectId.ToString(CultureInfo.InvariantCulture),globalPrefabId=global.targetPrefabId.ToString(CultureInfo.InvariantCulture)});current=PrefabUtility.GetCorrespondingObjectFromSource(current);}
            return result.ToArray();
        }
        static bool AssemblyChainIdentityMatches(UnityEngine.Object value,string guid,string localId)
        {return AllNativeAssemblyCorrespondences(value).Any(link=>link.guid==guid&&link.localId==localId||link.globalGuid==guid&&(link.globalObjectId==localId||ulong.TryParse(link.globalObjectId,out ulong objectId)&&ulong.TryParse(link.globalPrefabId,out ulong prefabId)&&(objectId^prefabId).ToString(CultureInfo.InvariantCulture)==localId));}
        public static void CaptureCompleteOriginalAssemblyOverrideCoverage()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped native Editor required.");var layouts=new[]{ReadAssemblySource(GardenAssembly),ReadAssemblySource(NaturalAssembly)};var paths=layouts[0].dependencies.SelectMany(d=>new[]{d.path,d.path+".meta"}).ToDictionary(path=>path,AssetSha);var isolated=EditorSceneManager.NewPreviewScene();var report=new AssemblyCoverage();LastAssemblyCoverage=report;var rows=new List<AssemblyCoverageField>();var instances=new List<GameObject>();
            try
            {
                foreach(var row in layouts.SelectMany(l=>l.instances).Where(r=>r.overrides.Length!=0))
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);if(prefab==null)throw new InvalidOperationException("Actual source prefab missing during complete coverage.");var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,isolated);instances.Add(instance);var sourceObjects=AssemblyObjects(prefab);var sceneObjects=AssemblyObjects(instance);int missing=prefab.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))+instance.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));string raw=File.ReadAllText(Io(row.prefab));
                    foreach(var change in row.overrides)
                    {
                        var sources=sourceObjects.Where(o=>SourceIdentityMatches(o,change.targetGuid,change.targetId)).ToArray();var counterparts=sceneObjects.Where(o=>SourceIdentityMatches(o,change.targetGuid,change.targetId)).ToArray();string key=row.prefabGuid+"/"+change.targetId+"/"+change.property;bool stale=ExactObsoleteAssemblyOverrides.TryGetValue(key,out string approved)&&approved==change.sourceValue;var header=Regex.Match(raw,@"(?m)^--- !u!(\d+) &"+Regex.Escape(change.targetId)+@"(?:\s|$)");
                        var field=new AssemblyCoverageField{instance=row.sceneInstanceId,prefab=row.prefab,prefabGuid=row.prefabGuid,prefabSha256=AssetSha(row.prefab),targetGuid=change.targetGuid,targetId=change.targetId,property=change.property,rawValue=change.sourceValue,objectReference=change.objectReference,targetAssetPath=AssetDatabase.GUIDToAssetPath(change.targetGuid),rawTargetHeaderClass=header.Success?header.Groups[1].Value:null,sourceMatches=sources.Length,instanceMatches=counterparts.Length,sourceChainMatches=sourceObjects.Count(o=>AssemblyChainIdentityMatches(o,change.targetGuid,change.targetId)),instanceChainMatches=sceneObjects.Count(o=>AssemblyChainIdentityMatches(o,change.targetGuid,change.targetId)),sourceObjects=sourceObjects.Length,instanceObjects=sceneObjects.Length,missingScripts=missing,exactApprovedObsoleteMetadata=stale,matchedSourceObjects=sources.Select(o=>DescribeActualAssemblyIdentity(prefab,o)).ToArray(),matchedInstanceObjects=counterparts.Select(o=>DescribeActualAssemblyIdentity(instance,o)).ToArray(),sourceCandidateObjects=sources.Length==0?sourceObjects.Select(o=>DescribeActualAssemblyIdentity(prefab,o)).ToArray():Array.Empty<NativeAssemblyIdentity>()};rows.Add(field);if(sources.Length==0){report.directMissingFields++;if(!stale)report.unapprovedMissingFields++;}if(sources.Length>1)report.ambiguousFields++;
                    }
                }
                report.fields=rows.Count;report.coverage=rows.ToArray();report.status="ALL55_NATIVE_TARGETS_CAPTURED_REQUIRES_COMPLETE_COVERAGE_REVIEW_BEFORE_ANY_OVERRIDE_APPLY";
            }
            catch(Exception exception){report.status="REJECTED_COMPLETE_TARGET_COVERAGE_DIAGNOSTIC";report.error=exception.ToString();throw;}
            finally{foreach(var instance in instances)if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(isolated);report.coverage=rows.ToArray();report.sourceBytesPreserved=paths.All(p=>AssetSha(p.Key)==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"complete55-original-override-native-coverage.json"),JsonUtility.ToJson(report,true));if(!report.sourceBytesPreserved)throw new InvalidOperationException("Read-only all55 coverage changed actual source bytes.");}
        }
        static UnityEngine.Object PairSourceAssemblyObject(GameObject source,GameObject instance,UnityEngine.Object value,out string siblingPath,bool requireDisposable=true)
        {
            var transform=value is GameObject go?go.transform:(value as Component)?.transform;if(transform==null)throw new InvalidOperationException("Actual source transform/component required.");var indices=new Stack<int>();var cursor=transform;while(cursor!=source.transform){if(cursor==null||cursor.parent==null)throw new InvalidOperationException("Inherited source component escaped its exact outer prefab hierarchy.");indices.Push(cursor.GetSiblingIndex());cursor=cursor.parent;}siblingPath=string.Join("/",indices);var paired=instance.transform;
            foreach(int index in indices){if(index<0||index>=paired.childCount)throw new InvalidOperationException("Native instantiated hierarchy differs from exact original prefab sibling chain.");paired=paired.GetChild(index);}
            if(value is GameObject)return paired.gameObject;var sourceComponents=transform.GetComponents<Component>();int slot=Array.IndexOf(sourceComponents,value);var targetComponents=paired.GetComponents<Component>();if(slot<0||slot>=targetComponents.Length||targetComponents[slot]==null||targetComponents[slot].GetType()!=value.GetType()||sourceComponents.Length!=targetComponents.Length)throw new InvalidOperationException("Native source/instance component slot/type correspondence differs.");var result=targetComponents[slot];if(requireDisposable&&(EditorUtility.IsPersistent(result)||!result.gameObject.scene.IsValid()))throw new InvalidOperationException("Only disposable actual instantiated scene components may receive authored override values.");return result;
        }
        static readonly Dictionary<string,string> ExactObsoleteAssemblyOverrides=new Dictionary<string,string>
        {
            {"ba43096d063f9e141bc66d23a6aad4b6/1439406413011755468/m_BoundingSphereOverride.x","10"},
            {"ba43096d063f9e141bc66d23a6aad4b6/1439406413011755468/m_BoundingSphereOverride.y","0"},
            {"ba43096d063f9e141bc66d23a6aad4b6/1439406413011755468/m_BoundingSphereOverride.z","-1.7014118e+38"},
            {"ba43096d063f9e141bc66d23a6aad4b6/1439406413011755468/m_BoundingSphereOverride.w","0.12109375"},
            {"c0cacda008ac5324598ff77198b809b2/7892989606626299767/m_LocalPosition.x","-0"},
            {"c0cacda008ac5324598ff77198b809b2/7892989606626299767/m_LocalPosition.y","0"},
            {"c0cacda008ac5324598ff77198b809b2/7892989606626299767/m_LocalPosition.z","0"},
            {"c0cacda008ac5324598ff77198b809b2/7892989606626299767/m_LocalRotation.x","0"},
            {"c0cacda008ac5324598ff77198b809b2/7892989606626299767/m_LocalRotation.y","0"},
            {"c0cacda008ac5324598ff77198b809b2/7892989606626299767/m_LocalRotation.z","0"},
            {"c0cacda008ac5324598ff77198b809b2/7892989606626299767/m_LocalRotation.w","1"},
            {"8930c424f7d0037468691ac2f225bae8/6481620312478966445/m_IsActive","0"},
            {"8930c424f7d0037468691ac2f225bae8/6845380403947263104/m_BoundingSphereOverride.y","1.4033e-41"},
            {"8930c424f7d0037468691ac2f225bae8/6845380403947263104/m_BoundingSphereOverride.z","0"},
            {"8930c424f7d0037468691ac2f225bae8/6845380403947263104/m_BoundingSphereOverride.w","7.0696675e-31"},
            {"8930c424f7d0037468691ac2f225bae8/6845380403947263104/m_BoundingSphereOverride.x","9.6e-43"},
            {"8930c424f7d0037468691ac2f225bae8/2298868114731749878/m_BoundingSphereOverride.y","2.5350226e-38"},
            {"8930c424f7d0037468691ac2f225bae8/2298868114731749878/m_BoundingSphereOverride.z","1.4e-44"},
            {"8930c424f7d0037468691ac2f225bae8/2298868114731749878/m_BoundingSphereOverride.w","2.3510647e-38"},
            {"8930c424f7d0037468691ac2f225bae8/2298868114731749878/m_BoundingSphereOverride.x","0"},
        };
        static bool PreserveExactObsoleteAssemblyMetadata(GameObject sourceRoot,GameObject instance,AssemblySourceRow row,AssemblyOverride change,AssemblyOverrideEvidence proof)
        {
            string key=row.prefabGuid+"/"+change.targetId+"/"+change.property;if(!ExactObsoleteAssemblyOverrides.TryGetValue(key,out string raw)||raw!=change.sourceValue||change.targetGuid!=row.prefabGuid||change.objectReference!="fileID: 0")return false;
            string expected=row.prefabGuid=="ba43096d063f9e141bc66d23a6aad4b6"?"3a3b5a6f7a64a67e7d99b70ecd9f5d641f12527a64ed1cec258b52bcbab5bf36" : row.prefabGuid=="c0cacda008ac5324598ff77198b809b2"?"c6d8fa4eca07072c6e8555b5490232dacb712cdf09ca55a0c3c426fc47637232" : row.prefabGuid=="8930c424f7d0037468691ac2f225bae8"?"cfbc9055e88c85a7880a8cccfb89f720f48a53b703e8793883aa2062e138358a" : null;
            if(expected==null||row.sourceSha256!=expected||AssetSha(row.prefab)!=expected)throw new InvalidOperationException("Exact obsolete-source target allowlist cannot apply to a changed prefab.");
            var originals=AssemblyObjects(sourceRoot);var cloned=AssemblyObjects(instance);int count=row.prefabGuid=="ba43096d063f9e141bc66d23a6aad4b6"?125:row.prefabGuid=="c0cacda008ac5324598ff77198b809b2"?81:112;int missing=sourceRoot.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))+instance.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if(originals.Length!=count||cloned.Length!=count||missing!=0||originals.Any(o=>SourceIdentityMatches(o,change.targetGuid,change.targetId))||cloned.Any(o=>SourceIdentityMatches(o,change.targetGuid,change.targetId)))throw new InvalidOperationException("Exact stale bound target must be absent from both complete current native/generated/inherited hierarchies, without missing scripts.");
            proof.sourceObjectCount=originals.Length;proof.instanceObjectCount=cloned.Length;proof.missingScripts=missing;proof.state=change.property.StartsWith("m_BoundingSphereOverride.",StringComparison.Ordinal)?"EXACT_OBSOLETE_SOURCE_BOUND_TARGET_ABSENT_IN_NATIVE_HIERARCHY":change.property=="m_IsActive"?"EXACT_OBSOLETE_SOURCE_GAMEOBJECT_TARGET_ABSENT_IN_NATIVE_HIERARCHY":"EXACT_OBSOLETE_SOURCE_TRANSFORM_TARGET_ABSENT_IN_NATIVE_HIERARCHY";proof.nativeIdentityMethod="EXACT_PINNED_PREFAB_SHA_AND_COMPLETE_NATIVE_SOURCE_INSTANCE_ID_ABSENCE";proof.applied=false;proof.disabledPublicLightOverride=false;proof.rawFloatBits=BitConverter.ToString(BitConverter.GetBytes(float.Parse(change.sourceValue,CultureInfo.InvariantCulture))).Replace("-","").ToLowerInvariant();return true;
        }
        static AssemblyOverrideEvidence[] ApplyOriginalAssemblyOverrides(GameObject instance,AssemblySourceRow row,Func<GameObject,AssemblyOverride,UnityEngine.Object[]> exactSourceResolver=null)
        {
            var sourceRoot=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);if(sourceRoot==null)throw new InvalidOperationException("Exact original source prefab required.");var objects=AssemblyObjects(sourceRoot);var evidence=new List<AssemblyOverrideEvidence>();var serializedTargets=new Dictionary<UnityEngine.Object,SerializedObject>();var pending=new List<(UnityEngine.Object target,AssemblyOverride change,AssemblyOverrideEvidence proof)>();
            foreach(var change in row.overrides)
            {
                var targets=exactSourceResolver==null?objects.Where(o=>SourceIdentityMatches(o,change.targetGuid,change.targetId)).ToArray():exactSourceResolver(sourceRoot,change);
                if(exactSourceResolver!=null&&(targets==null||targets.Any(o=>o==null||!objects.Contains(o))||targets.Distinct().Count()!=targets.Length))throw new InvalidOperationException("An explicit source resolver must return unique actual members of the unchanged original prefab.");
                var proof=new AssemblyOverrideEvidence{instance=row.sceneInstanceId,targetId=change.targetId,targetGuid=change.targetGuid,property=change.property,rawValue=change.sourceValue,targetMatches=targets.Length};evidence.Add(proof);
                if(targets.Length==0&&PreserveExactObsoleteAssemblyMetadata(sourceRoot,instance,row,change,proof))continue;
                if(targets.Length!=1)throw new InvalidOperationException("Actual authored override must resolve to exactly one inherited original GUID/localID: "+JsonUtility.ToJson(proof));
                var originalTarget=targets[0];var target=PairSourceAssemblyObject(sourceRoot,instance,originalTarget,out proof.sourceHierarchySiblingPath);proof.nativeIdentityMethod="ACTUAL_PERSISTENT_SOURCE_GUID_LOCAL_ID_AND_EXACT_PREFAB_SIBLING_COMPONENT_SLOT_MAPPING";proof.sourceGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(originalTarget).ToString();proof.instanceGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();proof.actualType=target.GetType().FullName;
                if(change.property.StartsWith("m_BoundingSphereOverride.",StringComparison.Ordinal))
                {
                    float raw=float.Parse(change.sourceValue,CultureInfo.InvariantCulture);proof.rawFloatBits=BitConverter.ToString(BitConverter.GetBytes(raw)).Replace("-","").ToLowerInvariant();
                    if(!(target is Light light)||light.useBoundingSphereOverride)throw new InvalidOperationException("Only genuinely disabled public source Light bound metadata may remain documentary.");var original=PrefabUtility.GetCorrespondingObjectFromOriginalSource(originalTarget);if(original==null)original=originalTarget;if(!(originalTarget is Light originalLight)||originalLight.useBoundingSphereOverride||!(original is Light authored)||authored.useBoundingSphereOverride)throw new InvalidOperationException("Original source public bound override is enabled.");proof.state="INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE";proof.disabledPublicLightOverride=true;proof.applied=false;continue;
                }
                if(!serializedTargets.TryGetValue(target,out var serialized)){serialized=new SerializedObject(target);serializedTargets.Add(target,serialized);}var property=serialized.FindProperty(change.property);if(property==null)throw new InvalidOperationException("Original authored serialized field is missing: "+change.property);
                if(change.property=="m_IsActive")
                {if(property.propertyType!=SerializedPropertyType.Boolean||(change.sourceValue!="0"&&change.sourceValue!="1"))throw new InvalidOperationException("Exact authored active flag required.");property.boolValue=change.sourceValue=="1";}
                else if(change.property.StartsWith("m_LocalPosition.",StringComparison.Ordinal)||change.property.StartsWith("m_LocalRotation.",StringComparison.Ordinal)||change.property.StartsWith("m_LocalScale.",StringComparison.Ordinal))
                {float value=float.Parse(change.sourceValue,CultureInfo.InvariantCulture);if(!(target is Transform)||property.propertyType!=SerializedPropertyType.Float||float.IsNaN(value)||float.IsInfinity(value))throw new InvalidOperationException("Only finite real child transform fields may be applied.");property.floatValue=value;}
                else if(Regex.IsMatch(change.property,@"^m_Materials\.Array\.data\[\d+\]$"))
                {if(!(target is Renderer)||property.propertyType!=SerializedPropertyType.ObjectReference)throw new InvalidOperationException("Real original renderer material slot required.");property.objectReferenceValue=ResolveOriginalAssemblyAsset(change.objectReference,out proof.assignedAssetGuid,out proof.assignedAssetLocalId);if(!(property.objectReferenceValue is Material))throw new InvalidOperationException("Actual authored material override must resolve to a Material.");}
                else throw new InvalidOperationException("Unreviewed active authored override: "+change.property);
                pending.Add((target,change,proof));proof.state="APPLIED_EXACT_ORIGINAL_AUTHOR_VALUE";
            }
            // Commit each real component atomically so partial quaternion fields
            // never normalize through an unintended intermediate orientation.
            foreach(var serialized in serializedTargets.Values)serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach(var item in pending)
            {
                var actual=new SerializedObject(item.target).FindProperty(item.change.property);bool valid=false;
                if(actual.propertyType==SerializedPropertyType.Boolean){item.proof.actualValue=actual.boolValue?"1":"0";valid=item.proof.actualValue==item.change.sourceValue;}
                else if(actual.propertyType==SerializedPropertyType.Float){item.proof.actualValue=actual.floatValue.ToString("R",CultureInfo.InvariantCulture);valid=Mathf.Abs(actual.floatValue-float.Parse(item.change.sourceValue,CultureInfo.InvariantCulture))<=.00002f;}
                else if(actual.propertyType==SerializedPropertyType.ObjectReference&&actual.objectReferenceValue!=null){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual.objectReferenceValue,out string guid,out long id);item.proof.actualValue=guid+"/"+id.ToString(CultureInfo.InvariantCulture);valid=guid==item.proof.assignedAssetGuid&&id.ToString(CultureInfo.InvariantCulture)==item.proof.assignedAssetLocalId;}
                item.proof.appliedValueVerified=valid;item.proof.applied=valid;if(!valid)throw new InvalidOperationException("Native authored override did not retain its exact assigned scalar/active/material value: "+JsonUtility.ToJson(item.proof));
            }
            return evidence.ToArray();
        }
        static IEnumerable<ImportedProperty> OriginalMaterialProperties(string text)
        {
            var shader=Regex.Match(text,@"(?m)^  m_Shader: ([^\r\n]+)");if(!shader.Success)throw new InvalidOperationException("Original material shader required.");yield return new ImportedProperty{kind="Shader",name="m_Shader",sourceValue=shader.Groups[1].Value.Trim()};
            foreach(var section in new[]{("m_TexEnvs","Texture"),("m_Ints","Int"),("m_Floats","Float"),("m_Colors","Color")})
            {
                var match=Regex.Match(text,@"(?ms)^    "+section.Item1+@":(?:\r?\n| \[\]\r?\n)(.*?)(?=^    m_|^  m_|\z)");if(!match.Success)continue;
                foreach(Match name in Regex.Matches(match.Groups[1].Value,@"(?m)^    - ([^:\r\n]+):")){var property=new ImportedProperty{kind=section.Item2,name=name.Groups[1].Value};property.sourceValue=PropertyText(text,property);yield return property;}
            }
        }
        public static void CaptureOriginalAuthoredAssemblyPreflight()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped Editor required.");
            PrepareApprovedArtistMaterialImports();var sources=new[]{ReadAssemblySource(GardenAssembly),ReadAssemblySource(NaturalAssembly)};var first=sources[0];var proof=new AssemblyPreflight{groups=2};LastAssemblyPreflight=proof;var temporary=new List<GameObject>();var evidence=new List<AssemblyOverrideEvidence>();var materials=new List<AssemblyMaterialObservation>();var groups=new List<AssemblyGroupObservation>();var effects=new List<AssemblyEffectObservation>();Dictionary<string,string> frozen=null;
            try
            {
                // Observe the vendor's actual public import/save; never alter its shader or source properties ourselves.
                foreach(var raw in first.originalMaterials)
                {
                    if(AssetSha(raw.path)!=raw.sha256||AssetSha(raw.path+".meta")!=raw.metaSha256||AssetDatabase.AssetPathToGUID(raw.path)!=raw.guid)throw new InvalidOperationException("Immutable original material snapshot/GUID changed.");var source=AssetDatabase.LoadAssetAtPath<Material>(raw.source);if(source==null||AssetDatabase.AssetPathToGUID(raw.source)!=raw.sourceGuid)throw new InvalidOperationException("Actual source material missing.");foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(raw.source))if(asset!=null)AssetDatabase.SaveAssetIfDirty(asset);
                    string original=File.ReadAllText(Io(raw.path)),current=File.ReadAllText(Io(raw.source));var changed=new List<ImportedProperty>();var unchanged=new List<ImportedProperty>();foreach(var property in OriginalMaterialProperties(original)){property.importedValue=PropertyText(current,property);if(property.importedValue==property.sourceValue)unchanged.Add(property);else{property.reason="ACTUAL_NATIVE_IMPORT_DIFFERENCE_REQUIRES_EXPLICIT_REVIEW_BEFORE_WORLD_ADOPTION";changed.Add(property);}}
                    var observation=new AssemblyMaterialObservation{source=raw.source,guid=raw.sourceGuid,originalSha256=raw.sha256,nativeSha256=AssetSha(raw.source),metaSha256=AssetSha(raw.source+".meta"),snapshot=raw.path,shader=source.shader==null?null:source.shader.name,protectedProperties=unchanged.Count,unchangedProperties=unchanged.ToArray(),changedProperties=changed.ToArray(),protectedShaderAndTextureBindingsExact=changed.All(p=>p.kind!="Texture"&&p.kind!="Shader")};materials.Add(observation);if(!observation.protectedShaderAndTextureBindingsExact)throw new InvalidOperationException("Native import changed original authored shader/texture/UV binding; no broad material waiver.");
                }
                proof.materialObservations=materials.ToArray();proof.materials=materials.Count;
                var materialPaths=new HashSet<string>(first.originalMaterials.Select(r=>r.source));
                foreach(var dependency in first.dependencies){if(AssetSha(dependency.path+".meta")!=dependency.metaSha256||AssetDatabase.AssetPathToGUID(dependency.path)!=dependency.guid)throw new InvalidOperationException("Source metadata/GUID changed: "+dependency.path);proof.exactMetadata++;if(!materialPaths.Contains(dependency.path)){if(AssetSha(dependency.path)!=dependency.sha256)throw new InvalidOperationException("Nonmaterial source changed: "+dependency.path);proof.exactNonmaterials++;}}
                var legacy=AssetDatabase.LoadAssetAtPath<Material>(DragonKingdomWorldAuthoring.Art+"Polyart Studio/HandpaintedEnvironmentVol1/Materials/Old/M_Bush.mat");proof.legacyMaterial=AssetDatabase.GetAssetPath(legacy);proof.legacyProperty="_MainTex";proof.missingLegacyTextureIsInactive=legacy!=null&&!legacy.HasProperty("_MainTex")&&legacy.HasProperty("_Albedo")&&legacy.GetTexture("_Albedo")!=null;proof.actualAlbedoGuid=proof.missingLegacyTextureIsInactive?AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(legacy.GetTexture("_Albedo"))):null;if(!proof.missingLegacyTextureIsInactive)throw new InvalidOperationException("Actual public shader must exclude legacy missing MainTex and retain its real authored Albedo texture.");
                var paths=first.dependencies.SelectMany(d=>new[]{d.path,d.path+".meta"}).Concat(first.originalMaterials.SelectMany(r=>new[]{r.path,r.path+".meta"})).Concat(new[]{GardenAssembly,GardenAssembly+".meta",NaturalAssembly,NaturalAssembly+".meta"});frozen=paths.Distinct().ToDictionary(path=>path,AssetSha);
                CaptureCompleteOriginalAssemblyOverrideCoverage();proof.nativeTargetCoverage=LastAssemblyCoverage;if(LastAssemblyCoverage.unapprovedMissingFields!=0||LastAssemblyCoverage.ambiguousFields!=0||!LastAssemblyCoverage.sourceBytesPreserved)throw new InvalidOperationException("Full55 coverage must pass before any original author override write.");
                foreach(var layout in sources)
                {
                    var group=new GameObject("Temporary exact authored source assembly "+layout.name);temporary.Add(group);
                    foreach(var row in layout.instances)
                    {
                    if(AssetDatabase.AssetPathToGUID(row.prefab)!=row.prefabGuid||AssetSha(row.prefab)!=row.sourceSha256||AssetSha(row.prefab+".meta")!=row.sourceMetaSha256)throw new InvalidOperationException("Exact authored source prefab changed.");var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);temporary.Add(instance);instance.transform.SetParent(group.transform,false);instance.transform.position=row.position;instance.transform.rotation=row.rotation;instance.transform.localScale=row.scale;evidence.AddRange(ApplyOriginalAssemblyOverrides(instance,row));foreach(var ps in instance.GetComponentsInChildren<ParticleSystem>(true)){var renderer=ps.GetComponent<ParticleSystemRenderer>();effects.Add(new AssemblyEffectObservation{group=layout.name,instance=row.sceneInstanceId,name=ps.name,active=ps.gameObject.activeInHierarchy,playOnAwake=ps.main.playOnAwake,loop=ps.main.loop,emissionEnabled=ps.emission.enabled,materials=renderer==null?Array.Empty<string>():renderer.sharedMaterials.Where(m=>m!=null).Select(AssetDatabase.GetAssetPath).ToArray()});}proof.instances++;
                    }
                    string layoutPath=layout.name.StartsWith("Village",StringComparison.Ordinal)?GardenAssembly:NaturalAssembly;groups.Add(new AssemblyGroupObservation{layout=layoutPath,layoutSha256=AssetSha(layoutPath),name=layout.name,instances=layout.instances.Length,sourceBounds=BoundsOf(group.transform)});
                }
                proof.sourceGroups=groups.ToArray();proof.actualEffects=effects.ToArray();proof.overrideEvidence=evidence.ToArray();proof.overrides=evidence.Count;proof.appliedOverrides=evidence.Count(e=>e.applied);proof.inactiveLightFields=evidence.Count(e=>e.state=="INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE");proof.obsoleteNativeAbsentFields=evidence.Count(e=>e.state!=null&&e.state.StartsWith("EXACT_OBSOLETE_SOURCE_",StringComparison.Ordinal));proof.status="NATIVE_ORIGINAL_ASSEMBLIES_OBSERVED_REQUIRES_MATERIAL_DIFFERENCE_AND_COMPOSITION_REVIEW";
            }
            catch(Exception exception){proof.status="REJECTED_ASSEMBLY_PREFLIGHT";proof.error=exception.ToString();throw;}
            finally
            {
                proof.overrideEvidence=evidence.ToArray();proof.materialObservations=materials.ToArray();foreach(var instance in temporary)if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);proof.nativeSourcesPreserved=frozen!=null&&frozen.All(p=>AssetSha(p.Key)==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"original-authored-assemblies-preflight.json"),JsonUtility.ToJson(proof,true));
                if(frozen!=null&&!proof.nativeSourcesPreserved)throw new InvalidOperationException("Original authored assembly diagnosis changed frozen native source bytes.");
            }
        }
    }
}
#endif
