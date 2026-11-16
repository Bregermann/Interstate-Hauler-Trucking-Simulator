#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class ArchitectureParentSource
        {public string id,parent,name,sourceTransformSha256,sourceGameObjectSha256;public bool active;public Vector3 position,scale;public Quaternion rotation;}
        [Serializable]public sealed class ArchitectureModification
        {public string targetId,targetGuid,property,sourceValue,objectReference;public int targetType;}
        [Serializable]public sealed class ArchitectureInstanceSource
        {public string sceneInstanceId,prefabGuid,prefab,sourceSha256,sourceMetaSha256,sourceRootTransformId,sourceParent,serializedInstanceSha256,serializedInstance;public Vector3 position,scale;public Quaternion rotation;public ArchitectureModification[] modifications;}
        [Serializable]public sealed class ArchitectureGeometryLayout
        {public int schemaVersion,instanceCount,prefabCount;public string name,compositionKey;public ArchitectureParentSource[] parentTransforms;public ArchitectureInstanceSource[] instances;public ArchitectureDependency[] dependencies;public ArchitectureSnapshot[] originalMaterials;public ArchitectureSnapshot sourceSceneSnapshot;}
        [Serializable]public sealed class ArchitectureTargetObservation
        {
            public string layout,instance,prefab,prefabGuid,targetGuid,targetId,property,rawValue,objectReference,state,error,sourceGlobalId,instanceGlobalId,sourceSiblingPath,actualType;
            public int sourceMatches,targetType;public string nativeResolvedIdentityId;public bool paired,geometryField,applied;public AssemblyOverrideEvidence appliedEvidence;public ArchitectureNestedTargetWitness nativeAlias;
        }
        [Serializable]public sealed class ArchitectureParentObservation
        {public string id,parent,name;public bool activeSelf,localPoseExact;public Vector3 localPosition,localScale;public Quaternion localRotation;}
        [Serializable]public sealed class ArchitectureInstanceGeometry
        {
            public string instance,source,sourceGuid,sourceLocalId,parent,sourceGlobalId;public bool parentAndLocalPoseExact,activeSelf,activeInHierarchy;
            public Vector3 localPosition,localScale;public Quaternion localRotation;public Bounds rendererBounds;public int renderers,primaryMeshes,colliders;public RidgeGeometryBinding[] nativeGeometry;
        }
        [Serializable]public sealed class ArchitectureGateSection
        {public float localY,clearMinX,clearMaxX,clearWidth,authoredWorldClearWidth,authoredColliderClearWidth;public int rays,clearRays;public bool centerClear,authoredColliderCenterClear;}
        [Serializable]public sealed class ArchitectureGateGeometry
        {
            public string instance,source,measurement="DIRECT_NATIVE_RAYCAST_ON_DISPOSABLE_ACTUAL_PRIMARY_MESH_COLLIDERS_BOTH_DIRECTIONS_NOT_GAMEPLAY_COLLISION";
            public Bounds localPrimaryBounds,worldPrimaryBounds;public int primaryMeshes,primaryVertices,originalSourceColliders,temporaryMeasurementColliders;
            public float roadWidthMeters=22,minimumUniformScaleForHorizontal22mOnly,maximumUniformScaleForCurrentCastleRegionXZOnly;
            public bool allMeasuredSectionsHaveCenterOpening,horizontalScaleIntervalNonempty,placementFeasibilityEstablished;
            public ArchitectureGateSection[] sections;
        }
        [Serializable]public sealed class ArchitectureEnsembleGeometry
        {
            public string name,key,layout,layoutSha256,boundsBasis="ALL_TRUE_ACTIVE_PRIMARY_LOD_VERTICES_WITH_ORIGINAL_AUTHORED_PARENTS_AND_ROOT_TRS";
            public int instances,parents,primaryMeshes,primaryVertices,sourceColliders,missingScripts,unresolvedTargets,unhandledStructuralEdits,appliedGeometryFields;
            public Bounds actualPrimaryBounds;public Vector3 authoredFramePivot;public bool authoredLocalPosesExact,geometryOverridesFullyReconciled;
            public ArchitectureParentObservation[] parentMapping;public ArchitectureInstanceGeometry[] parts;public ArchitectureGateGeometry[] gates;
        }
        [Serializable]public sealed class ArchitectureGeometryReceipt
        {
            public string scope="ACTUAL_SOURCE_ARCHITECTURE_GEOMETRY_AND_COMPLETE_OVERRIDE_TARGET_INSPECTION_NOT_PLACEMENT_WORLD_ADOPTION_OR_VISUAL_ACCEPTANCE",status="IN_PROGRESS",error;
            public int layouts,instances,rawOverrideFields,unresolvedTargets,appliedGeometryFields,sourceFilesWatched,incomingSceneCount,auditedAbsentMetadataFields,nativeResolvedTargetFields,existingExactAbsentFields;
            public bool materialPreparationInvoked,sourceBaselineCaptured,inspectionCompleted,sourceBytesPreserved,world29AndCanonicalBytesPreserved,incomingSceneContextRestored,temporaryPreviewDestroyed,allGeometryOverridesReconciled;
            public string[] changedSourcePaths,changedProtectedWorldPaths;public ArchitectureEnsembleGeometry[] ensembles;public ArchitectureTargetObservation[] overrides;public ArchitectureNativeResolvedIdentity[] nativeResolvedIdentities;public ArchitecturePrototypeBoundary[] proposedPrototypeBoundaries;
        }
        [Serializable]public sealed class ArchitectureNativeIdentity
        {public string path,type,guid,localId,globalId,serializedJsonSha256;public string[] sourceChain;}
        [Serializable]public sealed class ArchitectureIncomingReference
        {public ArchitectureNativeIdentity owner,target;public string property;}
        [Serializable]public sealed class ArchitecturePrototypeBoundary
        {
            public string prefab,prefabGuid,prefabSha256,path="Crownwatch Castle/Fortified castle sanctuary",referenceScanScope="ALL_SAVED_WORLD_MONOBEHAVIOURS_AND_BOUNDARY_ANCESTOR_TRANSFORMS_WITHIN_THIS_PREFAB_NOT_ALL_OTHER_ASSETS";
            public ArchitectureNativeIdentity rootGameObject,rootTransform;public ArchitectureNativeIdentity[] rendererAndColliderComponents;public ArchitectureIncomingReference[] incomingReferences;public Bounds actualPrimaryMeshBounds;public int primaryMeshes,primaryVertices;
            public bool retirementOrDeletionApproved;
        }
        public static ArchitectureGeometryReceipt LastArchitectureGeometry{get;private set;}
        sealed class ArchitectureProbeInstance
        {public ArchitectureInstanceSource row;public GameObject source,instance;public ArchitectureEnsembleGeometry ensemble;public readonly List<ArchitectureTargetObservation> observations=new List<ArchitectureTargetObservation>();}

        static string ArchitectureGeometrySha(string path)
        {using(var stream=File.OpenRead(Io(path)))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        static bool ArchitectureFinite(Vector3 value)=>!float.IsNaN(value.x)&&!float.IsNaN(value.y)&&!float.IsNaN(value.z)&&!float.IsInfinity(value.x)&&!float.IsInfinity(value.y)&&!float.IsInfinity(value.z);
        static IEnumerable<string> ArchitectureSnapshotPaths(ArchitectureSnapshot snapshot)
        {
            if(snapshot==null)yield break;yield return snapshot.path;yield return snapshot.path+".meta";
            var metadata=snapshot.originalMetadata;
            if(metadata!=null&&new[]{metadata.path,metadata.sha256,metadata.guid,metadata.metaSha256}.Any(value=>!string.IsNullOrEmpty(value)))
            {
                if(new[]{metadata.path,metadata.sha256,metadata.guid,metadata.metaSha256}.Any(string.IsNullOrEmpty))throw new InvalidOperationException("Partially populated original architecture metadata descriptor cannot be ignored.");
                yield return metadata.path;yield return metadata.path+".meta";
            }
        }
        static Dictionary<string,string> ArchitectureProtectedWorldFiles()
        {
            var paths=new List<string>{Root+"Prefabs/Shared Dragon Kingdom.prefab",Root+"Scenes/DragonKingdomPreview.unity",Root+"Configuration/Dragon Kingdom.asset"};
            string candidate=Root+"VisualCandidates/RoundedIntegrated29";
            if(Directory.Exists(Io(candidate)))paths.AddRange(Directory.GetFiles(Io(candidate),"*",SearchOption.AllDirectories).Select(p=>Path.GetRelativePath(Path.GetFullPath("."),p).Replace('\\','/')));
            return paths.SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,ArchitectureGeometrySha);
        }
        static ArchitectureModification[] ArchitectureGeometryFields(ArchitectureInstanceSource row,ArchitectureTargetObservation[] observations)
        {
            return row.modifications.Where((change,index)=>(observations[index].paired||ArchitectureExactObsolete(observations[index]))&&observations[index].geometryField&&!observations[index].applied&&observations[index].state!="EXACT_AUTHORED_ROOT_TRS_READBACK_REJECTED").ToArray();
        }
        static bool ArchitectureIsGeometryField(string property)=>property=="m_IsActive"||Regex.IsMatch(property,@"^m_Local(?:Position\.[xyz]|Rotation\.[xyzw]|Scale\.[xyz])$")||Regex.IsMatch(property,@"^m_Materials\.Array\.data\[\d+\]$");
        static void VerifyAlreadyAssignedArchitectureRoot(ArchitectureProbeInstance item)
        {
            using(var serialized=new SerializedObject(item.instance.transform))
            foreach(var observation in item.observations.Where(o=>o.paired&&o.actualType==typeof(Transform).FullName&&o.sourceSiblingPath==""&&o.targetGuid==item.row.prefabGuid&&o.targetId==item.row.sourceRootTransformId&&Regex.IsMatch(o.property,@"^m_Local(?:Position\.[xyz]|Rotation\.[xyzw]|Scale\.[xyz])$")))
            {
                var property=serialized.FindProperty(observation.property);float expected=float.Parse(observation.rawValue,CultureInfo.InvariantCulture);
                if(property==null||property.propertyType!=SerializedPropertyType.Float||float.IsNaN(expected)||float.IsInfinity(expected)||float.IsNaN(property.floatValue)||float.IsInfinity(property.floatValue)||Mathf.Abs(property.floatValue-expected)>.00002f)
                {observation.state="EXACT_AUTHORED_ROOT_TRS_READBACK_REJECTED";observation.error="Pinned whole-root TRS differs from its original finite source scalar.";item.ensemble.unresolvedTargets++;continue;}
                observation.applied=true;observation.state="EXACT_AUTHORED_WHOLE_ROOT_TRS_ALREADY_ASSIGNED_AND_NATIVE_READBACK_VERIFIED";item.ensemble.appliedGeometryFields++;
                observation.appliedEvidence=new AssemblyOverrideEvidence{instance=observation.instance,targetId=observation.targetId,targetGuid=observation.targetGuid,property=observation.property,rawValue=observation.rawValue,targetMatches=1,actualType=observation.actualType,state=observation.state,applied=true,appliedValueVerified=true,actualValue=property.floatValue.ToString("R",CultureInfo.InvariantCulture),sourceGlobalId=observation.sourceGlobalId,instanceGlobalId=observation.instanceGlobalId,sourceHierarchySiblingPath="",nativeIdentityMethod="ACTUAL_EXISTING_SOURCE_PAIRING_AND_ALREADY_ASSIGNED_WHOLE_ROOT_TRS_READBACK"};
            }
        }

        public static ArchitectureGeometryReceipt CaptureAuthoredArchitectureGeometry(Action prepareNativeImports=null)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for disposable architecture geometry inspection.");
            var proof=new ArchitectureGeometryReceipt{incomingSceneCount=SceneManager.sceneCount};LastArchitectureGeometry=proof;
            var incoming=SceneManager.GetActiveScene();var incomingSetup=EditorSceneManager.GetSceneManagerSetup();var protectedWorld=ArchitectureProtectedWorldFiles();
            var sourceFiles=new Dictionary<string,string>();var ensembles=new List<ArchitectureEnsembleGeometry>();var all=new List<ArchitectureProbeInstance>();var observations=new List<ArchitectureTargetObservation>();var resolvedIdentities=new Dictionary<string,ArchitectureNativeResolvedIdentity>();
            Scene preview=default;var created=new List<GameObject>();
            try
            {
                if(prepareNativeImports!=null){prepareNativeImports();proof.materialPreparationInvoked=true;}
                var layouts=new List<ArchitectureGeometryLayout>();
                for(int i=0;i<ArchitectureLayoutPaths.Length;i++)
                {
                    string path=ArchitectureLayoutPaths[i];if(ArchitectureGeometrySha(path)!=ArchitectureLayoutHashes[i])throw new InvalidOperationException("Exact four architecture source recipes required.");
                    var layout=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(path)));
                    if(layout.schemaVersion!=1||layout.instances==null||layout.instances.Length!=layout.instanceCount||layout.parentTransforms==null)throw new InvalidOperationException("Complete original architecture hierarchy required.");layouts.Add(layout);
                    foreach(string file in layout.dependencies.SelectMany(d=>new[]{d.path,d.path+".meta"}).Concat(new[]{path,path+".meta"}).Concat(layout.originalMaterials.SelectMany(ArchitectureSnapshotPaths)).Concat(ArchitectureSnapshotPaths(layout.sourceSceneSnapshot)).Distinct())
                    {if(!File.Exists(Io(file)))throw new InvalidOperationException("Actual source dependency missing: "+file);if(!sourceFiles.ContainsKey(file))sourceFiles.Add(file,ArchitectureGeometrySha(file));}
                }
                if(layouts.Sum(l=>l.instanceCount)!=678||!layouts.Select(l=>l.instanceCount).SequenceEqual(new[]{110,156,382,30}))throw new InvalidOperationException("Exactly678 original architecture instances in the reviewed four groups required.");
                proof.sourceFilesWatched=sourceFiles.Count;proof.sourceBaselineCaptured=sourceFiles.Count>0;preview=EditorSceneManager.NewPreviewScene();
                var sourceCache=new Dictionary<string,GameObject>();var targetCache=new Dictionary<string,UnityEngine.Object[]>();
                foreach(var pair in layouts.Select((layout,index)=>new{layout,index}))
                {
                    var layout=pair.layout;var frame=new GameObject(layout.name);created.Add(frame);SceneManager.MoveGameObjectToScene(frame,preview);
                    var result=new ArchitectureEnsembleGeometry{name=layout.name,key=layout.compositionKey,layout=ArchitectureLayoutPaths[pair.index],layoutSha256=ArchitectureLayoutHashes[pair.index],instances=layout.instanceCount,parents=layout.parentTransforms.Length,authoredFramePivot=frame.transform.position};ensembles.Add(result);
                    var parents=layout.parentTransforms.ToDictionary(p=>p.id,p=>{var go=new GameObject(p.name);go.transform.SetParent(frame.transform,false);return go.transform;});
                    foreach(var parent in layout.parentTransforms)
                    {
                        var value=parents[parent.id];value.SetParent(parent.parent=="0"?frame.transform:parents[parent.parent],false);value.localPosition=parent.position;value.localRotation=parent.rotation;value.localScale=parent.scale;value.gameObject.SetActive(parent.active);
                    }
                    result.parentMapping=layout.parentTransforms.Select(p=>new ArchitectureParentObservation{id=p.id,parent=p.parent,name=p.name,activeSelf=parents[p.id].gameObject.activeSelf,localPosition=parents[p.id].localPosition,localRotation=parents[p.id].localRotation,localScale=parents[p.id].localScale,localPoseExact=parents[p.id].localPosition==p.position&&parents[p.id].localScale==p.scale&&Quaternion.Angle(parents[p.id].localRotation,p.rotation)<.0001f&&parents[p.id].gameObject.activeSelf==p.active}).ToArray();
                    foreach(var row in layout.instances)
                    {
                        if(AssetDatabase.AssetPathToGUID(row.prefab)!=row.prefabGuid||ArchitectureGeometrySha(row.prefab)!=row.sourceSha256||ArchitectureGeometrySha(row.prefab+".meta")!=row.sourceMetaSha256)throw new InvalidOperationException("Exact authored source prefab identity changed: "+row.prefab);
                        if(!sourceCache.TryGetValue(row.prefab,out var source)){source=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);if(source==null)throw new InvalidOperationException("Actual original source prefab missing.");sourceCache.Add(row.prefab,source);}
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,preview);instance.transform.SetParent(parents[row.sourceParent],false);instance.transform.localPosition=row.position;instance.transform.localRotation=row.rotation;instance.transform.localScale=row.scale;
                        var item=new ArchitectureProbeInstance{row=row,source=source,instance=instance,ensemble=result};all.Add(item);
                        result.missingScripts+=source.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                        foreach(var change in row.modifications)
                        {
                            var observation=new ArchitectureTargetObservation{layout=layout.name,instance=row.sceneInstanceId,prefab=row.prefab,prefabGuid=row.prefabGuid,targetGuid=change.targetGuid,targetId=change.targetId,targetType=change.targetType,property=change.property,rawValue=change.sourceValue,objectReference=change.objectReference,geometryField=ArchitectureIsGeometryField(change.property)};
                            observations.Add(observation);item.observations.Add(observation);string key=row.prefab+"/"+change.targetGuid+"/"+change.targetId;
                            if(!targetCache.TryGetValue(key,out var targets)){targets=ResolveArchitectureSourceTargets(source,change.targetGuid,change.targetId,out _);targetCache.Add(key,targets);}observation.sourceMatches=targets.Length;
                            if(targets.Length!=1)
                            {
                                if(targets.Length==0&&ObserveExistingExactArchitectureObsolete(item,change,observation))continue;
                                observation.state=targets.Length==0?"UNRESOLVED_ACTUAL_SOURCE_TARGET_NO_GUESS_OR_WRITE":"AMBIGUOUS_ACTUAL_SOURCE_TARGET_NO_WRITE";result.unresolvedTargets++;continue;
                            }
                            try
                            {
                                var target=PairSourceAssemblyObject(source,instance,targets[0],out string sibling);observation.sourceSiblingPath=sibling;observation.sourceGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(targets[0]).ToString();observation.instanceGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();observation.actualType=target.GetType().FullName;observation.paired=true;
                                string identityKey=item.row.prefab+"/"+change.targetGuid+"/"+change.targetId;
                                if(!resolvedIdentities.TryGetValue(identityKey,out var nativeIdentity)){nativeIdentity=MeasureArchitectureResolvedIdentity(source,targets[0],change.targetGuid,change.targetId,sibling);if(nativeIdentity!=null)resolvedIdentities.Add(identityKey,nativeIdentity);}observation.nativeResolvedIdentityId=nativeIdentity?.id;

                                if(!SourceIdentityMatches(targets[0],change.targetGuid,change.targetId))
                                {
                                    observation.nativeAlias=MeasureArchitectureNestedTarget(source,targets[0],change.targetGuid,change.targetId);
                                    if(observation.nativeAlias==null)throw new InvalidOperationException("Missing exact native/serialized nested source ancestry witness.");
                                    if(PrefabUtility.GetCorrespondingObjectFromOriginalSource(target)!=PrefabUtility.GetCorrespondingObjectFromOriginalSource(targets[0]))throw new InvalidOperationException("Paired native instance original source component differs.");
                                }
                                if(observation.geometryField)observation.state="RESOLVED_GEOMETRY_FIELD_PENDING_ATOMIC_EXISTING_ADAPTER";
                                else if(change.property.StartsWith("m_BoundingSphereOverride.",StringComparison.Ordinal))
                                {bool disabled=targets[0] is Light originalLight&&!originalLight.useBoundingSphereOverride&&target is Light cloneLight&&!cloneLight.useBoundingSphereOverride;observation.state=disabled?"RESOLVED_ACTUAL_PUBLIC_DISABLED_LIGHT_BOUND_METADATA_NOT_APPLIED":"UNREVIEWED_ENABLED_OR_NONLIGHT_BOUND_FIELD_NO_WRITE";if(!disabled)result.unresolvedTargets++;}
                                else if(change.property=="m_Name"||change.property=="m_RootOrder"||change.property=="m_StaticEditorFlags"||change.property=="m_CastShadows"||Regex.IsMatch(change.property,@"^m_LocalEulerAnglesHint\.[xyz]$"))observation.state="RESOLVED_EXPLICIT_RAW_PRESENTATION_OR_EDITOR_METADATA_RECORDED_NOT_APPLIED";
                                else{observation.state="UNREVIEWED_RESOLVED_SOURCE_FIELD_NO_WRITE";result.unresolvedTargets++;}
                            }
                            catch(Exception exception){observation.error=exception.Message;observation.state="FAILED_EXACT_SOURCE_TO_INSTANCE_PAIR_NO_WRITE";result.unresolvedTargets++;}
                        }
                        foreach(string field in new[]{"m_RemovedComponents","m_RemovedGameObjects","m_AddedComponents","m_AddedGameObjects"})
                            if(Regex.IsMatch(row.serializedInstance,@"(?m)^\s*"+field+@":\s*\r?\n\s*-"))result.unhandledStructuralEdits++;
                    }
                }
                // Complete every target inventory before applying any of the
                // existing finite TRS/activation/material adapter's fields.
                foreach(var item in all)
                {
                    VerifyAlreadyAssignedArchitectureRoot(item);
                    var fields=ArchitectureGeometryFields(item.row,item.observations.ToArray());
                    bool safe=item.ensemble.unhandledStructuralEdits==0&&item.observations.All(o=>!o.geometryField||o.paired||ArchitectureExactObsolete(o));
                    if(!safe||fields.Length==0)continue;
                    var row=ArchitectureAdapterRow(item.row,fields);
                    try
                    {
                        var applied=ApplyOriginalAssemblyOverrides(item.instance,row,ResolveArchitectureSourceTargets);
                        foreach(var evidence in applied){var observation=item.observations.First(o=>o.targetGuid==evidence.targetGuid&&o.targetId==evidence.targetId&&o.property==evidence.property);observation.applied=evidence.applied;observation.appliedEvidence=evidence;observation.state=evidence.state;if(evidence.applied)item.ensemble.appliedGeometryFields++;}
                    }
                    catch(Exception exception){foreach(var observation in item.observations.Where(o=>o.geometryField)){observation.error=exception.Message;observation.state="GEOMETRY_ADAPTER_REJECTED_PROVISIONAL_PREFAB_BOUNDS_ONLY";}item.ensemble.unresolvedTargets++;}
                }
                var hashes=new Dictionary<string,string>();
                foreach(var result in ensembles)
                {
                    var items=all.Where(i=>i.ensemble==result).ToArray();var frame=items[0].instance.transform;while(frame.parent!=null)frame=frame.parent;
                    RidgeMeshSamples(frame,out result.actualPrimaryBounds,out result.primaryMeshes,out result.primaryVertices);result.sourceColliders=frame.GetComponentsInChildren<Collider>(true).Length;
                    result.parts=items.Select(item=>
                    {
                        var t=item.instance.transform;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item.source,out string guid,out long id);var renderers=t.GetComponentsInChildren<Renderer>(true);var bounds=new Bounds(t.position,Vector3.zero);bool first=true;foreach(var renderer in renderers){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
                        return new ArchitectureInstanceGeometry{instance=item.row.sceneInstanceId,source=item.row.prefab,sourceGuid=guid,sourceLocalId=id.ToString(CultureInfo.InvariantCulture),sourceGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(item.source).ToString(),parent=item.row.sourceParent,parentAndLocalPoseExact=t.localPosition==item.row.position&&t.localScale==item.row.scale&&Quaternion.Angle(t.localRotation,item.row.rotation)<.005f,localPosition=t.localPosition,localRotation=t.localRotation,localScale=t.localScale,activeSelf=item.instance.activeSelf,activeInHierarchy=item.instance.activeInHierarchy,rendererBounds=bounds,renderers=renderers.Length,primaryMeshes=RidgePrimaryMeshFilters(t).Count(),colliders=t.GetComponentsInChildren<Collider>(true).Length,nativeGeometry=RidgeGeometryBindings(t,hashes)};
                    }).ToArray();
                    result.authoredLocalPosesExact=result.parentMapping.All(p=>p.localPoseExact)&&result.parts.All(p=>p.parentAndLocalPoseExact);result.geometryOverridesFullyReconciled=result.authoredLocalPosesExact&&result.unresolvedTargets==0&&result.missingScripts==0&&result.unhandledStructuralEdits==0&&items.All(i=>i.observations.Where(o=>o.geometryField).All(o=>o.applied||ArchitectureExactObsolete(o)));
                    result.gates=items.Where(i=>Regex.IsMatch(Path.GetFileNameWithoutExtension(i.row.prefab),@"^OpenGate(?:Bridge)?$")).Select(i=>MeasureArchitectureGate(i,result.actualPrimaryBounds)).ToArray();
                }
                proof.layouts=ensembles.Count;proof.instances=all.Count;proof.rawOverrideFields=observations.Count;proof.auditedAbsentMetadataFields=observations.Count(ArchitectureExactAuditedAbsentMetadata);proof.existingExactAbsentFields=observations.Count(ArchitectureExactObsolete);proof.nativeResolvedTargetFields=observations.Count(o=>o.paired);proof.unresolvedTargets=ensembles.Sum(e=>e.unresolvedTargets);proof.appliedGeometryFields=ensembles.Sum(e=>e.appliedGeometryFields);proof.allGeometryOverridesReconciled=ensembles.All(e=>e.geometryOverridesFullyReconciled);proof.inspectionCompleted=true;
                proof.proposedPrototypeBoundaries=new[]{Root+"Prefabs/Shared Dragon Kingdom.prefab",Root+"VisualCandidates/RoundedIntegrated29/Shared Dragon Kingdom.prefab"}.Where(p=>File.Exists(Io(p))).Select(ObserveArchitecturePrototypeBoundary).ToArray();
                proof.status=proof.allGeometryOverridesReconciled?"SOURCE_GEOMETRY_INSPECTED_NOT_PLACED_OR_VISUALLY_ACCEPTED":"COMPLETE_TARGET_DIAGNOSTIC_GEOMETRY_PROVISIONAL_UNRECONCILED_SOURCE_FIELDS_OR_POSES_NOT_ADOPTABLE";
                return proof;
            }
            catch(Exception exception){proof.status="REJECTED_SOURCE_GEOMETRY_DIAGNOSTIC";proof.error=exception.ToString();throw;}
            finally
            {
                foreach(var go in created)if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);proof.temporaryPreviewDestroyed=!preview.IsValid()||!preview.isLoaded;
                proof.changedSourcePaths=sourceFiles.Where(pair=>!File.Exists(Io(pair.Key))||ArchitectureGeometrySha(pair.Key)!=pair.Value).Select(pair=>pair.Key).ToArray();proof.sourceBytesPreserved=proof.sourceBaselineCaptured&&proof.changedSourcePaths.Length==0;
                var worldAfter=ArchitectureProtectedWorldFiles();proof.changedProtectedWorldPaths=protectedWorld.Keys.Union(worldAfter.Keys).Where(path=>!protectedWorld.TryGetValue(path,out string before)||!worldAfter.TryGetValue(path,out string afterHash)||before!=afterHash).ToArray();proof.world29AndCanonicalBytesPreserved=proof.changedProtectedWorldPaths.Length==0;
                var after=EditorSceneManager.GetSceneManagerSetup();proof.incomingSceneContextRestored=SceneManager.sceneCount==proof.incomingSceneCount&&SceneManager.GetActiveScene()==incoming&&after.Length==incomingSetup.Length&&after.Zip(incomingSetup,(a,b)=>a.path==b.path&&a.isActive==b.isActive&&a.isLoaded==b.isLoaded).All(x=>x);
                proof.ensembles=ensembles.ToArray();proof.overrides=observations.ToArray();proof.nativeResolvedIdentities=resolvedIdentities.Values.ToArray();Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"authored-architecture-geometry.json"),JsonUtility.ToJson(proof,true));
                if(proof.sourceBaselineCaptured&&!proof.sourceBytesPreserved||!proof.world29AndCanonicalBytesPreserved||!proof.incomingSceneContextRestored||!proof.temporaryPreviewDestroyed)throw new InvalidOperationException("Disposable architecture geometry inspection changed source/world bytes or incoming scene context.");
            }
        }

        static ArchitectureGateGeometry MeasureArchitectureGate(ArchitectureProbeInstance item,Bounds ensembleBounds)
        {
            var root=item.instance.transform;var proof=new ArchitectureGateGeometry{instance=item.row.sceneInstanceId,source=item.row.prefab,originalSourceColliders=root.GetComponentsInChildren<Collider>(true).Length};
            RidgeMeshSamples(root,out proof.worldPrimaryBounds,out proof.primaryMeshes,out proof.primaryVertices);var local=new Bounds();bool firstVertex=true;foreach(var filter in RidgePrimaryMeshFilters(root))
            {
                using(var meshData=Mesh.AcquireReadOnlyMeshData(filter.sharedMesh))using(var vertices=new NativeArray<Vector3>(meshData[0].vertexCount,Allocator.Temp))
                {
                    meshData[0].GetVertices(vertices);foreach(var vertex in vertices)
                    {Vector3 point=root.InverseTransformPoint(filter.transform.TransformPoint(vertex));if(!ArchitectureFinite(point))throw new InvalidOperationException("Nonfinite actual gate primary vertex.");if(firstVertex){local=new Bounds(point,Vector3.zero);firstVertex=false;}else local.Encapsulate(point);}
                }
            }
            if(firstVertex||local.size.x<=.0001f||local.size.y<=.0001f)throw new InvalidOperationException("Actual source gate vertex envelope required.");proof.localPrimaryBounds=local;
            var proxies=new List<MeshCollider>();var proxyObjects=new List<GameObject>();
            try
            {
                foreach(var filter in RidgePrimaryMeshFilters(root))
                {var go=new GameObject("Disposable exact source opening measurement");go.hideFlags=HideFlags.HideAndDontSave;go.transform.SetParent(filter.transform,false);var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;collider.convex=false;proxies.Add(collider);proxyObjects.Add(go);}proof.temporaryMeasurementColliders=proxies.Count;Physics.SyncTransforms();
                var sections=new List<ArchitectureGateSection>();const int intervals=256;var authoredColliders=root.GetComponentsInChildren<Collider>(true).Where(c=>!proxies.Contains(c as MeshCollider)&&c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).ToArray();
                for(int level=1;level<=7;level++)
                {
                    float y=Mathf.Lerp(local.min.y,local.max.y,level/8f),step=local.size.x/intervals;var clear=new bool[intervals+1];var authoredClear=new bool[intervals+1];
                    for(int i=0;i<=intervals;i++)
                    {
                        float x=local.min.x+step*i;Vector3 a=root.TransformPoint(new Vector3(x,y,local.min.z-Mathf.Max(1,local.size.z))),b=root.TransformPoint(new Vector3(x,y,local.max.z+Mathf.Max(1,local.size.z)));Vector3 direction=(b-a).normalized;float distance=Vector3.Distance(a,b);
                        clear[i]=!proxies.Any(c=>c.Raycast(new Ray(a,direction),out _,distance)||c.Raycast(new Ray(b,-direction),out _,distance));
                        authoredClear[i]=!authoredColliders.Any(c=>c.Raycast(new Ray(a,direction),out _,distance)||c.Raycast(new Ray(b,-direction),out _,distance));
                    }
                    int center=Mathf.Clamp(Mathf.RoundToInt((0-local.min.x)/Mathf.Max(step,.000001f)),0,intervals),left=center,right=center;
                    if(clear[center]){while(left>0&&clear[left-1])left--;while(right<intervals&&clear[right+1])right++;}
                    int authoredLeft=center,authoredRight=center;if(authoredClear[center]){while(authoredLeft>0&&authoredClear[authoredLeft-1])authoredLeft--;while(authoredRight<intervals&&authoredClear[authoredRight+1])authoredRight++;}
                    float width=clear[center]?Mathf.Max(0,(right-left-1)*step):0,worldScale=root.TransformVector(Vector3.right).magnitude;
                    sections.Add(new ArchitectureGateSection{localY=y,rays=(intervals+1)*2,clearRays=clear.Count(v=>v)*2,centerClear=clear[center],clearMinX=local.min.x+left*step,clearMaxX=local.min.x+right*step,clearWidth=width,authoredWorldClearWidth=width*worldScale,authoredColliderCenterClear=authoredClear[center],authoredColliderClearWidth=authoredColliders.Length>0&&authoredClear[center]?Mathf.Max(0,(authoredRight-authoredLeft-1)*step)*worldScale:0});
                }
                proof.sections=sections.ToArray();proof.allMeasuredSectionsHaveCenterOpening=sections.All(s=>s.centerClear&&s.authoredWorldClearWidth>0);float minimumWidth=sections.Min(s=>s.authoredWorldClearWidth);proof.minimumUniformScaleForHorizontal22mOnly=minimumWidth>0?22/minimumWidth:0;
                var definition=AssetDatabase.LoadAssetAtPath<DragonKingdomDefinition>(Root+"Configuration/Dragon Kingdom.asset");var region=definition.FindRegion("castle").bounds;proof.maximumUniformScaleForCurrentCastleRegionXZOnly=Mathf.Min(region.size.x/Mathf.Max(.001f,ensembleBounds.size.x),region.size.z/Mathf.Max(.001f,ensembleBounds.size.z));
                proof.horizontalScaleIntervalNonempty=proof.allMeasuredSectionsHaveCenterOpening&&proof.minimumUniformScaleForHorizontal22mOnly<=proof.maximumUniformScaleForCurrentCastleRegionXZOnly;proof.placementFeasibilityEstablished=false;return proof;
            }
            finally{foreach(var go in proxyObjects)if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
        }

        static ArchitectureNativeIdentity ArchitectureDescribeIdentity(UnityEngine.Object value,Transform root)
        {
            if(value==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id))throw new InvalidOperationException("Actual persistent native prototype object identity required.");
            var t=value is GameObject go?go.transform:(value as Component)?.transform;
            var chain=new List<string>();var seen=new HashSet<UnityEngine.Object>();for(var current=value;current!=null&&seen.Add(current);current=PrefabUtility.GetCorrespondingObjectFromSource(current))
            {if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(current,out string sourceGuid,out long sourceId))chain.Add(sourceGuid+"/"+sourceId.ToString(CultureInfo.InvariantCulture)+"/"+AssetDatabase.GetAssetPath(current));}
            string json=EditorJsonUtility.ToJson(value,false);return new ArchitectureNativeIdentity{path=t==null?value.name:PathOf(t,root),type=value.GetType().FullName,guid=guid,localId=id.ToString(CultureInfo.InvariantCulture),globalId=GlobalObjectId.GetGlobalObjectIdSlow(value).ToString(),serializedJsonSha256=Hash(System.Text.Encoding.UTF8.GetBytes(json)).Replace("-","").ToLowerInvariant(),sourceChain=chain.ToArray()};
        }
        static ArchitecturePrototypeBoundary ObserveArchitecturePrototypeBoundary(string path)
        {
            var saved=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(saved==null)throw new InvalidOperationException("Actual saved prototype prefab required for bounded identity inspection.");
            var matches=saved.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Fortified castle sanctuary"&&t.parent!=null&&t.parent.name=="Crownwatch Castle").ToArray();if(matches.Length!=1)throw new InvalidOperationException("Exactly one unchanged explicitly scoped gray sanctuary required.");var boundary=matches[0];
            var proof=new ArchitecturePrototypeBoundary{prefab=path,prefabGuid=AssetDatabase.AssetPathToGUID(path),prefabSha256=ArchitectureGeometrySha(path),rootGameObject=ArchitectureDescribeIdentity(boundary.gameObject,saved.transform),rootTransform=ArchitectureDescribeIdentity(boundary,saved.transform)};
            RidgeMeshSamples(boundary,out proof.actualPrimaryMeshBounds,out proof.primaryMeshes,out proof.primaryVertices);
            proof.rendererAndColliderComponents=boundary.GetComponentsInChildren<Component>(true).Where(c=>c is Renderer||c is Collider).Select(c=>ArchitectureDescribeIdentity(c,saved.transform)).ToArray();
            var inside=new HashSet<UnityEngine.Object>(AssemblyObjects(boundary.gameObject));var owners=saved.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null).Cast<UnityEngine.Object>().ToList();for(var t=boundary.parent;t!=null;t=t.parent)owners.Add(t);var references=new List<ArchitectureIncomingReference>();
            foreach(var owner in owners.Distinct().Where(o=>!inside.Contains(o)))
            {
                using(var serialized=new SerializedObject(owner))
                {
                    var property=serialized.GetIterator();while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue!=null&&inside.Contains(property.objectReferenceValue))references.Add(new ArchitectureIncomingReference{owner=ArchitectureDescribeIdentity(owner,saved.transform),target=ArchitectureDescribeIdentity(property.objectReferenceValue,saved.transform),property=property.propertyPath});
                }
            }
            proof.incomingReferences=references.ToArray();proof.retirementOrDeletionApproved=false;return proof;
        }

        public static void CaptureAuthoredArchitectureGeometryFromCommandLine()=>CaptureAuthoredArchitectureGeometry(()=>{var material=CaptureArchitectureMaterialPreflight();if(!material.passed)throw new InvalidOperationException("Actual architecture material preparation rejected.");});
    }
}
#endif
