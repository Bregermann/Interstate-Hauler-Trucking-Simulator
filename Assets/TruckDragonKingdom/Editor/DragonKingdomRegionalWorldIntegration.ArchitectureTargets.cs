#if UNITY_EDITOR && VISTA
using System;
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
        [Serializable]public sealed class ArchitectureResolverControls
        {public bool defaultAndExplicitDirectIdentical,foreignSourceRejected,duplicateSourceRejected,sourceBytesPreserved,temporaryPreviewDestroyed;public string source,sourceSha256;}
        [Serializable]public sealed class ArchitectureNestedTargetWitness
        {
            public string method="ACTUAL_NATIVE_ORIGINAL_COMPONENT_AND_ROOT_WITH_EXACT_SERIALIZED_NESTED_PREFAB_LINKS_NOT_NAME_OR_POSITION_MATCHING";
            public string sourcePrefab,sourcePrefabGuid,sourcePrefabSha256,requestedTargetGuid,requestedTargetId,actualType;
            public string originalComponentGuid,originalComponentId,nestedRootGuid,nestedRootId,originalRootGuid,originalRootId,nestedInstanceId;
            public string rawNestedInstanceSha256,rawStrippedTransformSha256,sourceGlobalId,originalComponentGlobalId;
            public bool persistentOriginalComponent,actualDescendant,serializedSourcePrefabExact,serializedRootCorrespondenceExact,requestedAliasExact;
        }

        static bool ArchitectureExactObsolete(ArchitectureTargetObservation observation)
            =>observation.appliedEvidence!=null&&observation.appliedEvidence.nativeIdentityMethod=="EXACT_PINNED_PREFAB_SHA_AND_COMPLETE_NATIVE_SOURCE_INSTANCE_ID_ABSENCE"
                &&observation.appliedEvidence.state!=null&&observation.appliedEvidence.state.StartsWith("EXACT_OBSOLETE_SOURCE_",StringComparison.Ordinal);

        static AssemblySourceRow ArchitectureAdapterRow(ArchitectureInstanceSource row,ArchitectureModification[] fields)
            =>new AssemblySourceRow{sceneInstanceId=row.sceneInstanceId,prefab=row.prefab,prefabGuid=row.prefabGuid,sourceSha256=row.sourceSha256,sourceMetaSha256=row.sourceMetaSha256,
                overrides=fields.Select(c=>new AssemblyOverride{targetId=c.targetId,targetGuid=c.targetGuid,property=c.property,sourceValue=c.sourceValue,objectReference=c.objectReference}).ToArray()};

        static bool ObserveExistingExactArchitectureObsolete(ArchitectureProbeInstance item,ArchitectureModification change,ArchitectureTargetObservation observation)
        {
            var row=ArchitectureAdapterRow(item.row,new[]{change});var field=row.overrides[0];
            var proof=new AssemblyOverrideEvidence{instance=row.sceneInstanceId,targetId=field.targetId,targetGuid=field.targetGuid,property=field.property,rawValue=field.sourceValue,targetMatches=0};
            // This is the existing exact20-field source/SHA/native-absence
            // authority remains unchanged. Only after it fails may the separate
            // V309 exact85 metadata-only authority account for its own records.
            if(!PreserveExactObsoleteAssemblyMetadata(item.source,item.instance,row,field,proof))return ObserveAuditedExactArchitectureAbsentMetadata(item,change,observation);
            observation.appliedEvidence=proof;observation.state=proof.state;observation.actualType="EXACT_ABSENT_ORIGINAL_TARGET";return true;
        }

        static UnityEngine.Object[] ResolveArchitectureSourceTargets(GameObject source,AssemblyOverride change)
            =>ResolveArchitectureSourceTargets(source,change.targetGuid,change.targetId,out _);

        static UnityEngine.Object[] ResolveArchitectureSourceTargets(GameObject source,string guid,string id,out ArchitectureNestedTargetWitness witness)
        {
            witness=null;var objects=AssemblyObjects(source);var direct=objects.Where(o=>SourceIdentityMatches(o,guid,id)).ToArray();
            if(direct.Length!=0)return direct;
            var matches=objects.Select(o=>new{value=o,proof=MeasureArchitectureNestedTarget(source,o,guid,id)}).Where(o=>o.proof!=null).ToArray();
            if(matches.Length==1)witness=matches[0].proof;
            return matches.Select(o=>o.value).ToArray();
        }

        static string ArchitectureSourceDocument(string text,int type,string id)
        {
            var start=Regex.Match(text,@"(?m)^--- !u!"+type+@" &"+Regex.Escape(id)+@"(?: stripped)?\r?$");
            if(!start.Success)return null;var next=Regex.Match(text.Substring(start.Index+start.Length),@"(?m)^--- !u!\d+ &");
            int end=next.Success?start.Index+start.Length+next.Index:text.Length;return text.Substring(start.Index,end-start.Index);
        }
        static string ArchitectureSourceReference(string text,string field,string part)
        {
            var line=Regex.Match(text,@"(?m)^\s*"+Regex.Escape(field)+@": \{([^\r\n]+)\}\r?$");
            if(!line.Success)return null;var value=Regex.Match(line.Groups[1].Value,@"(?:^|, )"+Regex.Escape(part)+@": ([^,}]+)");
            return value.Success?value.Groups[1].Value:null;
        }
        static ArchitectureNestedTargetWitness MeasureArchitectureNestedTarget(GameObject source,UnityEngine.Object value,string requestedGuid,string requestedId)
        {
            string path=AssetDatabase.GetAssetPath(source);
            if(!path.StartsWith(Root+"PrivateLicensedEnvironment/",StringComparison.Ordinal)||AssetDatabase.AssetPathToGUID(path)!=requestedGuid)return null;
            var t=value is GameObject go?go.transform:(value as Component)?.transform;
            if(t==null||!t.IsChildOf(source.transform)&&t!=source.transform)return null;
            var original=PrefabUtility.GetCorrespondingObjectFromOriginalSource(value);
            if(original==null||!EditorUtility.IsPersistent(original)||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string originalGuid,out long originalId))return null;
            var nested=PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);
            if(nested==null||!t.IsChildOf(nested.transform)&&t!=nested.transform||nested==source)return null;
            var originalRoot=PrefabUtility.GetCorrespondingObjectFromOriginalSource(nested.transform);
            if(originalRoot==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(originalRoot,out string rootGuid,out long rootId)||rootGuid!=originalGuid)return null;
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(nested.transform,out string currentGuid,out long currentId)||currentGuid!=requestedGuid)return null;
            ulong nestedId=unchecked((ulong)currentId)^unchecked((ulong)rootId);
            if((nestedId^unchecked((ulong)originalId)).ToString(CultureInfo.InvariantCulture)!=requestedId)return null;
            string raw=File.ReadAllText(Io(path));string instance=ArchitectureSourceDocument(raw,1001,nestedId.ToString(CultureInfo.InvariantCulture));
            string transform=ArchitectureSourceDocument(raw,4,currentId.ToString(CultureInfo.InvariantCulture));
            // An XOR coincidence is insufficient. Both concrete serialized
            // links must join the actual native root and original source asset.
            if(instance==null||transform==null||ArchitectureSourceReference(instance,"m_SourcePrefab","guid")!=originalGuid
                ||ArchitectureSourceReference(transform,"m_PrefabInstance","fileID")!=nestedId.ToString(CultureInfo.InvariantCulture)
                ||ArchitectureSourceReference(transform,"m_CorrespondingSourceObject","guid")!=rootGuid
                ||ArchitectureSourceReference(transform,"m_CorrespondingSourceObject","fileID")!=rootId.ToString(CultureInfo.InvariantCulture))return null;
            return new ArchitectureNestedTargetWitness{sourcePrefab=path,sourcePrefabGuid=requestedGuid,sourcePrefabSha256=ArchitectureGeometrySha(path),requestedTargetGuid=requestedGuid,requestedTargetId=requestedId,actualType=value.GetType().FullName,
                originalComponentGuid=originalGuid,originalComponentId=originalId.ToString(CultureInfo.InvariantCulture),nestedRootGuid=currentGuid,nestedRootId=currentId.ToString(CultureInfo.InvariantCulture),originalRootGuid=rootGuid,originalRootId=rootId.ToString(CultureInfo.InvariantCulture),nestedInstanceId=nestedId.ToString(CultureInfo.InvariantCulture),
                rawNestedInstanceSha256=Hash(System.Text.Encoding.UTF8.GetBytes(instance)).Replace("-","").ToLowerInvariant(),rawStrippedTransformSha256=Hash(System.Text.Encoding.UTF8.GetBytes(transform)).Replace("-","").ToLowerInvariant(),sourceGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(value).ToString(),originalComponentGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(original).ToString(),
                persistentOriginalComponent=true,actualDescendant=true,serializedSourcePrefabExact=true,serializedRootCorrespondenceExact=true,requestedAliasExact=true};
        }
        [Serializable]public sealed class ArchitectureNativeResolvedIdentity
        {
            public string id,prefab,prefabSha256,requestedGuid,requestedId,actualType,method,selectedNativeValue,selectedGuid,selectedLocalId,globalGuid,globalObjectId,globalPrefabId,sourceGlobalId,sourceHierarchySiblingPath;
            public bool actualPersistentSourceMember,exactDefaultSourceIdentityAuthority;
        }
        static ArchitectureNativeResolvedIdentity MeasureArchitectureResolvedIdentity(GameObject source,UnityEngine.Object value,string requestedGuid,string requestedId,string sibling)
        {
            if(value==null||!EditorUtility.IsPersistent(value)||!AssemblyObjects(source).Contains(value))throw new InvalidOperationException("Actual persistent member of the original source prefab required for native identity authority.");
            var values=new[]{value,PrefabUtility.GetCorrespondingObjectFromSource(value),PrefabUtility.GetCorrespondingObjectFromOriginalSource(value)};var names=new[]{"VALUE","CORRESPONDING_SOURCE","ORIGINAL_SOURCE"};
            for(int i=0;i<values.Length;i++)
            {
                var current=values[i];if(current==null)continue;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(current,out string guid,out long local);var global=GlobalObjectId.GetGlobalObjectIdSlow(current);string localId=local.ToString(CultureInfo.InvariantCulture),globalGuid=global.assetGUID.ToString(),objectId=global.targetObjectId.ToString(CultureInfo.InvariantCulture),prefabId=global.targetPrefabId.ToString(CultureInfo.InvariantCulture);string method=null;
                if(guid==requestedGuid&&localId==requestedId)method="ACTUAL_PUBLIC_ASSET_GUID_AND_LOCAL_FILE_ID";
                else if(globalGuid==requestedGuid&&objectId==requestedId)method="ACTUAL_PUBLIC_GLOBAL_OBJECT_ID";
                else if(globalGuid==requestedGuid&&(global.targetObjectId^global.targetPrefabId).ToString(CultureInfo.InvariantCulture)==requestedId)method="ACTUAL_PUBLIC_GLOBAL_OBJECT_AND_PREFAB_ID_XOR";
                if(method==null)continue;string path=AssetDatabase.GetAssetPath(source);return new ArchitectureNativeResolvedIdentity{id=path+"/"+requestedGuid+"/"+requestedId,prefab=path,prefabSha256=AssetSha(path),requestedGuid=requestedGuid,requestedId=requestedId,actualType=value.GetType().FullName,method=method,selectedNativeValue=names[i],selectedGuid=guid,selectedLocalId=localId,globalGuid=globalGuid,globalObjectId=objectId,globalPrefabId=prefabId,sourceGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(value).ToString(),sourceHierarchySiblingPath=sibling,actualPersistentSourceMember=true,exactDefaultSourceIdentityAuthority=SourceIdentityMatches(value,requestedGuid,requestedId)};
            }
            return null;
        }
        [Serializable]public sealed class ArchitectureAbsentField
        {public string instance,targetGuid,targetId,property,rawValue,objectReference;public bool existingExactObsoleteAuthority;}
        [Serializable]public sealed class ArchitectureAbsentSource
        {public string source,guid,sourceSha256,sourceMetaSha256;public int sourceObjects,instanceObjects,missingScripts;public ArchitectureAbsentField[] fields;public NativeAssemblyIdentity[] originalIdentities,instanceIdentities;public bool everyRequestedIdAbsentFromRawDocuments,allCorrespondenceChainsAbsent,allExactNestedAliasesAbsent;}
        [Serializable]public sealed class ArchitectureAbsenceAudit
        {public string scope="READ_ONLY_COMPLETE_NATIVE_IDENTITIES_FOR_EXACT_UNRESOLVED_ARCHITECTURE_TARGETS_NOT_AUTOMATIC_OBSOLETE_CLASSIFICATION",status="IN_PROGRESS",error,layout,layoutSha256;public int fields,existingExactAuthorityFields,unknownFields;public bool sourceBytesPreserved,temporaryPreviewDestroyed;public ArchitectureAbsentSource[] sources;}
        public static ArchitectureAbsenceAudit CaptureRemainingArchitectureTargetAbsenceWithoutWritingSources()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped native Editor required for complete original target-absence audit.");
            string path=ArchitectureLayoutPaths[2];if(AssetSha(path)!=ArchitectureLayoutHashes[2])throw new InvalidOperationException("Exact pinned walled-town recipe required.");var layout=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(path)));
            var watched=layout.dependencies.SelectMany(d=>new[]{d.path,d.path+".meta"}).Concat(new[]{path,path+".meta"}).Distinct().ToDictionary(p=>p,AssetSha);var preview=EditorSceneManager.NewPreviewScene();var created=new System.Collections.Generic.List<GameObject>();var rows=new System.Collections.Generic.List<ArchitectureAbsentSource>();var proof=new ArchitectureAbsenceAudit{layout=path,layoutSha256=AssetSha(path)};
            try
            {
                foreach(var family in layout.instances.GroupBy(r=>r.prefab))
                {
                    var first=family.First();var source=AssetDatabase.LoadAssetAtPath<GameObject>(first.prefab);if(source==null||AssetSha(first.prefab)!=first.sourceSha256||AssetSha(first.prefab+".meta")!=first.sourceMetaSha256||AssetDatabase.AssetPathToGUID(first.prefab)!=first.prefabGuid)throw new InvalidOperationException("Exact actual native source required for absence audit.");
                    var unresolved=new System.Collections.Generic.List<ArchitectureAbsentField>();var cache=new System.Collections.Generic.Dictionary<string,UnityEngine.Object[]>();
                    foreach(var row in family)foreach(var change in row.modifications)
                    {string key=change.targetGuid+"/"+change.targetId;if(!cache.TryGetValue(key,out var found)){found=ResolveArchitectureSourceTargets(source,change.targetGuid,change.targetId,out _);cache.Add(key,found);}if(found.Length!=0)continue;string oldKey=row.prefabGuid+"/"+change.targetId+"/"+change.property;bool old=ExactObsoleteAssemblyOverrides.TryGetValue(oldKey,out string exact)&&exact==change.sourceValue&&change.objectReference=="fileID: 0";unresolved.Add(new ArchitectureAbsentField{instance=row.sceneInstanceId,targetGuid=change.targetGuid,targetId=change.targetId,property=change.property,rawValue=change.sourceValue,objectReference=change.objectReference,existingExactObsoleteAuthority=old});}
                    if(unresolved.Count==0)continue;var clone=(GameObject)PrefabUtility.InstantiatePrefab(source,preview);created.Add(clone);var originals=AssemblyObjects(source);var clones=AssemblyObjects(clone);string raw=File.ReadAllText(Io(first.prefab));var targets=unresolved.GroupBy(f=>f.targetGuid+"/"+f.targetId).Select(g=>g.First()).ToArray();
                    var rowProof=new ArchitectureAbsentSource{source=first.prefab,guid=first.prefabGuid,sourceSha256=first.sourceSha256,sourceMetaSha256=first.sourceMetaSha256,sourceObjects=originals.Length,instanceObjects=clones.Length,missingScripts=source.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))+clone.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),fields=unresolved.ToArray(),originalIdentities=originals.Select(o=>DescribeActualAssemblyIdentity(source,o)).ToArray(),instanceIdentities=clones.Select(o=>DescribeActualAssemblyIdentity(clone,o)).ToArray(),everyRequestedIdAbsentFromRawDocuments=targets.All(f=>!Regex.IsMatch(raw,@"(?m)^--- !u!\d+ &"+Regex.Escape(f.targetId)+@"(?:\s|$)")),allCorrespondenceChainsAbsent=targets.All(f=>!originals.Any(o=>AssemblyChainIdentityMatches(o,f.targetGuid,f.targetId))&&!clones.Any(o=>AssemblyChainIdentityMatches(o,f.targetGuid,f.targetId))),allExactNestedAliasesAbsent=targets.All(f=>cache[f.targetGuid+"/"+f.targetId].Length==0)};rows.Add(rowProof);proof.fields+=unresolved.Count;proof.existingExactAuthorityFields+=unresolved.Count(f=>f.existingExactObsoleteAuthority);proof.unknownFields+=unresolved.Count(f=>!f.existingExactObsoleteAuthority);
                    if(rowProof.missingScripts!=0||rowProof.sourceObjects!=rowProof.instanceObjects||!rowProof.everyRequestedIdAbsentFromRawDocuments||!rowProof.allCorrespondenceChainsAbsent||!rowProof.allExactNestedAliasesAbsent)throw new InvalidOperationException("Native current/original/generated/inherited alias evidence contradicts proposed absence. No classification or source write is allowed: "+first.prefab);
                }
                proof.sources=rows.ToArray();proof.status="COMPLETE_EXACT_NATIVE_ABSENCE_INVENTORY_REQUIRES_INDIVIDUAL_FIELD_REVIEW_NOT_PLACEMENT_APPROVAL";
            }
            catch(Exception e){proof.error=e.ToString();proof.status="REJECTED_NATIVE_ABSENCE_AUDIT";throw;}
            finally{foreach(var go in created)if(go!=null)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(preview);proof.sources=rows.ToArray();proof.temporaryPreviewDestroyed=created.All(go=>go==null);proof.sourceBytesPreserved=watched.All(p=>AssetSha(p.Key)==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"architecture-remaining-native-absence-audit.json"),JsonUtility.ToJson(proof,true));if(!proof.sourceBytesPreserved)throw new InvalidOperationException("Read-only target absence audit changed exact source bytes.");}
            return proof;
        }
        public static string InspectArchitectureResolverControlsWithoutSavingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for disposable resolver controls.");
            var layout=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(ArchitectureLayoutPaths[0])));var row=layout.instances[0];
            if(ArchitectureGeometrySha(ArchitectureLayoutPaths[0])!=ArchitectureLayoutHashes[0]||ArchitectureGeometrySha(row.prefab)!=row.sourceSha256)throw new InvalidOperationException("Exact original architecture source required.");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);var scene=EditorSceneManager.NewPreviewScene();var objects=new System.Collections.Generic.List<GameObject>();
            var proof=new ArchitectureResolverControls{source=row.prefab,sourceSha256=ArchitectureGeometrySha(row.prefab)};
            try
            {
                GameObject Clone(){var result=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);objects.Add(result);return result;}
                var a=Clone();var b=Clone();var change=new ArchitectureModification{targetGuid=row.prefabGuid,targetId=row.sourceRootTransformId,property="m_LocalPosition.x",sourceValue="0",objectReference="fileID: 0"};var input=ArchitectureAdapterRow(row,new[]{change});
                ApplyOriginalAssemblyOverrides(a,input);ApplyOriginalAssemblyOverrides(b,input,(original,field)=>AssemblyObjects(original).Where(o=>SourceIdentityMatches(o,field.targetGuid,field.targetId)).ToArray());
                var first=a.GetComponentsInChildren<Transform>(true);var second=b.GetComponentsInChildren<Transform>(true);
                proof.defaultAndExplicitDirectIdentical=first.Length==second.Length&&first.Zip(second,(x,y)=>x.name==y.name&&x.localPosition==y.localPosition&&x.localScale==y.localScale&&x.localRotation==y.localRotation&&x.gameObject.activeSelf==y.gameObject.activeSelf&&x.GetComponents<Component>().Select(c=>c==null?null:c.GetType()).SequenceEqual(y.GetComponents<Component>().Select(c=>c==null?null:c.GetType()))).All(v=>v);
                try{ApplyOriginalAssemblyOverrides(b,input,(original,field)=>new UnityEngine.Object[]{a.transform});}catch(InvalidOperationException){proof.foreignSourceRejected=true;}
                var root=AssemblyObjects(source).Single(o=>o is Transform&&SourceIdentityMatches(o,change.targetGuid,change.targetId));
                try{ApplyOriginalAssemblyOverrides(b,input,(original,field)=>new[]{root,root});}catch(InvalidOperationException){proof.duplicateSourceRejected=true;}
            }
            finally
            {
                foreach(var value in objects)if(value!=null)UnityEngine.Object.DestroyImmediate(value);
                EditorSceneManager.ClosePreviewScene(scene);proof.temporaryPreviewDestroyed=objects.All(o=>o==null);proof.sourceBytesPreserved=ArchitectureGeometrySha(row.prefab)==proof.sourceSha256;
            }
            if(!proof.defaultAndExplicitDirectIdentical||!proof.foreignSourceRejected||!proof.duplicateSourceRejected||!proof.sourceBytesPreserved||!proof.temporaryPreviewDestroyed)throw new InvalidOperationException("Original adapter/explicit resolver controls failed: "+JsonUtility.ToJson(proof));
            return JsonUtility.ToJson(proof,true);
        }
    }
}
#endif
