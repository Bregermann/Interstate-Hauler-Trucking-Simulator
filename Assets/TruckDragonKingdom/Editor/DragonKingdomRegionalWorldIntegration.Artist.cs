#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string ArtistMarketLayout=Root+"PrivateLicensedEnvironment/DerivedVillageLayout/Artist Market Front v3.json";
        public const string ArtistMarketName="Owned original artist market frontage";
        const string ArtistLayoutSha="905b8b5007af673d13fdde44519797fa0a5a82313fe030d935836a5b61ccb238";
        const string OriginalMarketSceneSha="d65f8a0eb8f553e68e00913fa59170471a2dabeace8cbdf0f0d456de972a49bd";
        [Serializable]public sealed class ArtistDependency{public string path,guid,sha256,metaSha256,actualSha256;public bool approvedNativeImport;}
        [Serializable]public sealed class ArtistOverride{public string targetId,targetGuid,property,sourceValue,sourceValueFloatBits,state,sourceComponentGuid,sourceComponentId,nestedSourceInstanceId,sourceComponentType;public float value;public bool applied;}
        [Serializable]public sealed class ArtistInstance
        {public string sceneInstanceId,prefabGuid,prefab,sourceSha256,sourceMetaSha256;public Vector3 position,scale;public Quaternion rotation;public ArtistOverride[] overrides;}
        [Serializable]public sealed class ArtistLayout
        {public int schemaVersion;public string scope,sourceScene,sourceSceneSha256,sourceSceneMetaSha256,identityParent,materialImportProvenance,materialImportProvenanceSha256;public Vector3 center;public float radius;public ArtistInstance[] instances;public ArtistDependency[] dependencies;}
        [Serializable]public sealed class ArtistChild
        {public string name,sceneInstanceId,source,guid,sourceSha256,sourceMetaSha256;public Vector3 localPosition,localScale;public Quaternion localRotation;public Bounds actualBounds;public int sourceOverrides,appliedSourceOverrides,inactiveSourceOverrides;public ArtistOverrideProof[] overrideEvidence;}
        [Serializable]public sealed class ArtistMarket
        {
            public string scope="ONE_EXACT_SOURCE_AUTHORED_MARKET_GROUP_NOT_ARBITRARY_SCATTER_OR_VISUAL_ACCEPTANCE",layout,layoutSha256,layoutGuid,layoutMetaSha256,sourceSceneSha256,identityParent;
            public Vector3 position,scale;public Quaternion rotation;public Bounds bounds;public int groups,instances,prefabs,groundSamples,trial,appliedSourceOverrides,inactiveSourceOverrides;public ArtistImportEvidence materialImports;
            public float supportHeightRange,minimumFreeboard;public bool passed,exactSourcePoses,exactDependencies,roadAndObjectiveBuffersClear;
            public ArtistChild[] children;public ArtistDependency[] dependencies;
            public string placementStatus="UNMEASURED",placementError;public Bounds sourceMeasuredBounds;
            public int dryRejectedTrials,freeboardRejectedTrials,rangeRejectedTrials,roadRejectedTrials,overlapRejectedTrials;public AuthoredGroupTrial[] placementTrials;
        }
        [Serializable]public sealed class MooredShip
        {
            public string scope="ACTUAL_OWNED_TRADER_HULL_SIDE_MOORING_NOT_REPLACEMENT_FERRY_OR_VISUAL_ACCEPTANCE",source,sourceSha256,sourceMetaSha256,sourceGuid;
            public Bounds bounds;public Vector3 position,scale;public Quaternion rotation;public float seaLevel,keel,waterlineWidth,waterlineLength,minimumKeelClearance,departureLateralClearance,hullLength,hullHeight,hullKeel,hullDraft;
            public int depthSamples,waterlineIntersections,departureSweepSamples;public bool passed,originalFerryPoseExact;public Vector3[] waterline;
        }
        static string AssetSha(string path)=>Hash(File.ReadAllBytes(Io(path))).Replace("-","").ToLowerInvariant();
        static ArtistLayout ReadArtistLayout()
        {
            if(!File.Exists(ArtistMarketLayout)||AssetSha(ArtistMarketLayout)!=ArtistLayoutSha)throw new InvalidOperationException("Exact reviewed private artist market source layout required; no generated substitute.");
            var source=JsonUtility.FromJson<ArtistLayout>(File.ReadAllText(ArtistMarketLayout));
            if(source.schemaVersion!=3||source.identityParent!="2075655012"||source.sourceSceneSha256!=OriginalMarketSceneSha||source.instances==null||source.instances.Length!=61||source.instances.Select(i=>i.sceneInstanceId).Distinct().Count()!=61||source.instances.Select(i=>i.prefab).Distinct().Count()!=28||source.dependencies==null||source.dependencies.Length!=103)throw new InvalidOperationException("Exact61-source-instance/28-prefab/103-dependency artist provenance required.");
            foreach(var row in source.instances)if(new[]{row.position.x,row.position.y,row.position.z,row.rotation.x,row.rotation.y,row.rotation.z,row.rotation.w,row.scale.x,row.scale.y,row.scale.z}.Any(v=>float.IsNaN(v)||float.IsInfinity(v))||row.scale.x<=0||row.scale.y<=0||row.scale.z<=0)throw new InvalidOperationException("Nonfinite/invalid active artist TRS rejected.");
            ValidateArtistMaterialImports(source);
            return source;
        }
        [Serializable]public sealed class ArtistSourcePreflight
        {public string status="ACTUAL_SOURCE_PREFLIGHT_NOT_WORLD_OR_VISUAL_ACCEPTANCE",layout,layoutSha256;public ArtistImportEvidence materialImports;public int exactInstances,inactiveOverrides,appliedOverrides;public ArtistOverrideProof[] overrideEvidence;public bool sourceBytesPreserved;}
        public static ArtistSourcePreflight LastArtistSourcePreflight{get;private set;}
        public static void CaptureArtistSourcePreflight()
        {
            PrepareApprovedArtistMaterialImports();var source=ReadArtistLayout();var files=source.dependencies.SelectMany(d=>new[]{d.path,d.path+".meta"}).Concat(new[]{ArtistMarketLayout,ArtistMarketLayout+".meta",ArtistImportProvenance,ArtistImportProvenance+".meta"}).Concat(ReadMaterialProvenance().bindings.SelectMany(b=>new[]{b.snapshot,b.snapshot+".meta"})).Distinct().ToDictionary(p=>p,AssetSha);var evidence=new System.Collections.Generic.List<ArtistOverrideProof>();
            foreach(var row in source.instances)foreach(var change in row.overrides){var proof=MeasureOriginalOverrideTarget(row,change);if(change.state!="INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE"||change.applied||proof.targetMatches!=1||proof.actualLightType!="UnityEngine.Light"||proof.useBoundingSphereOverride||!proof.authoredOriginalOverrideDisabled||proof.examinedPersistentComponents<1)throw new InvalidOperationException("Actual imported inherited Light does not confirm exact disabled source metadata: "+JsonUtility.ToJson(proof)+"");proof.publicOverrideDisabled=true;proof.applied=false;evidence.Add(proof);}
            var result=new ArtistSourcePreflight{layout=ArtistMarketLayout,layoutSha256=AssetSha(ArtistMarketLayout),materialImports=ValidateArtistMaterialImports(source),exactInstances=source.instances.Length,inactiveOverrides=evidence.Count,appliedOverrides=0,overrideEvidence=evidence.ToArray(),sourceBytesPreserved=files.All(p=>AssetSha(p.Key)==p.Value)};LastArtistSourcePreflight=result;
            if(result.inactiveOverrides!=12||result.exactInstances!=61||!result.sourceBytesPreserved)throw new InvalidOperationException("Exact native source preflight failed.");Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"artist-source-preflight.json"),JsonUtility.ToJson(result,true));
        }
        public static ArtistMarket LastArtistMarketPlacement{get;private set;}
        static ArtistMarket InstallArtistMarket(DragonKingdomWorld world,string folder,LandmarkFinish evidence,List<Placement> records)
        {
            var source=ReadArtistLayout();var region=world.Region("village");var root=new GameObject(ArtistMarketName).transform;root.SetParent(region.root,false);root.localScale=Vector3.one*1.75f;root.rotation=Quaternion.Euler(0,180,0);
            var children=new List<ArtistChild>();
            foreach(var row in source.instances)
            {
                if(AssetDatabase.AssetPathToGUID(row.prefab)!=row.prefabGuid||AssetSha(row.prefab)!=row.sourceSha256||AssetSha(row.prefab+".meta")!=row.sourceMetaSha256)throw new InvalidOperationException("Artist source prefab changed: "+row.prefab);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);if(prefab==null)throw new InvalidOperationException("Actual artist source prefab missing.");var item=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var t=item.transform;t.SetParent(root,false);t.name="Artist source "+row.sceneInstanceId+" "+prefab.name;t.localPosition=row.position;t.localRotation=row.rotation;t.localScale=row.scale;
                var overrideEvidence=ApplyArtistOverrides(t,row);ConfigureFinishMaterials(t,folder);
                ConfigureSourceAuthoredStaticCollision(t);
                evidence.sources.Add(row.prefab);children.Add(new ArtistChild{name=t.name,sceneInstanceId=row.sceneInstanceId,source=row.prefab,guid=row.prefabGuid,sourceSha256=row.sourceSha256,sourceMetaSha256=row.sourceMetaSha256,localPosition=row.position,localRotation=row.rotation,localScale=row.scale,sourceOverrides=row.overrides.Length,appliedSourceOverrides=overrideEvidence.Count(o=>o.applied),inactiveSourceOverrides=overrideEvidence.Count(o=>!o.applied),overrideEvidence=overrideEvidence});
            }
            var hint=ActualArtistMarketHint;var result=new ArtistMarket{layout=ArtistMarketLayout,layoutSha256=AssetSha(ArtistMarketLayout),layoutGuid=AssetDatabase.AssetPathToGUID(ArtistMarketLayout),layoutMetaSha256=AssetSha(ArtistMarketLayout+".meta"),sourceSceneSha256=source.sourceSceneSha256,identityParent=source.identityParent,groups=1,instances=61,prefabs=28,dependencies=source.dependencies,materialImports=ValidateArtistMaterialImports(source),appliedSourceOverrides=children.Sum(c=>c.appliedSourceOverrides),inactiveSourceOverrides=children.Sum(c=>c.inactiveSourceOverrides),exactDependencies=true,exactSourcePoses=true};
            LastArtistMarketPlacement=result;result.sourceMeasuredBounds=BoundsOf(root);result.children=children.ToArray();
            try{PlaceActualArtistMarket(world,root,result);}
            catch(Exception error){result.placementStatus="REJECTED_NATIVE_WHOLE_GROUP_PLACEMENT";result.placementError=error.ToString();throw;}
            finally{for(int i=0;i<children.Count;i++)children[i].actualBounds=BoundsOf(root.GetChild(i));result.children=children.ToArray();}
            records.Add(new Placement{region="village",name=root.name,source=ArtistMarketLayout,operation="Add exact61-instance original artist market group preserving all source relative TRS and LOD overrides on a measured dry court",before=hint,after=root.position,bounds=result.bounds,trial=result.trial});return result;
        }
        static Vector3 ActualArtistMarketHint=>new Vector3(-715,0,-380);
        static void PlaceActualArtistMarket(DragonKingdomWorld world,Transform root,ArtistMarket result)
        {
            var terrains=world.GetComponentsInChildren<Terrain>(true);var own=new HashSet<Collider>(root.GetComponentsInChildren<Collider>(true));var hint=ActualArtistMarketHint;bool placed=false;var trials=new List<AuthoredGroupTrial>();
            result.placementStatus="MEASURING_ORIGINAL256_NATIVE_COURTS";result.dryRejectedTrials=result.freeboardRejectedTrials=result.rangeRejectedTrials=result.roadRejectedTrials=result.overlapRejectedTrials=0;result.passed=false;
            for(int trial=0;trial<256;trial++)
            {
                float radius=trial==0?0:5*Mathf.Sqrt(trial),angle=trial*2.399963f;var p=hint+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);root.position=p;var b=BoundsOf(root);float lo=float.PositiveInfinity,hi=float.NegativeInfinity;int samples=0;bool dry=true;
                var attempt=new AuthoredGroupTrial{trial=trial,phase="UNCHANGED_ORIGINAL256_ARTIST_COURTS",rootPosition=p,bounds=b};trials.Add(attempt);var points=new List<AuthoredGroupFloorPoint>();
                for(int z=0;z<7;z++)for(int x=0;x<7;x++){var q=new Vector3(Mathf.Lerp(b.min.x,b.max.x,x/6f),0,Mathf.Lerp(b.min.z,b.max.z,z/6f));float y=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,q);lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);bool intended=IntendedDrySettlementPosition("village",q),freeboard=y>=-.5f;dry&=intended&&freeboard;if(!intended)attempt.notDrySamples++;if(!freeboard)attempt.underFreeboardSamples++;points.Add(new AuthoredGroupFloorPoint{point=new Vector3(q.x,y,q.z),classifiedDry=intended,freeboardPass=freeboard,creekBankDistance=CreekBankDistance(q)});samples++;}
                attempt.samples=samples;attempt.low=lo;attempt.high=hi;attempt.range=hi-lo;attempt.floorPoints=points.ToArray();result.placementTrials=trials.ToArray();result.bounds=b;
                if(!dry){if(attempt.notDrySamples>0)result.dryRejectedTrials++;if(attempt.underFreeboardSamples>0)result.freeboardRejectedTrials++;attempt.rejectedReason="ORIGINAL_DRY12M_BANK_OR_MINIMUM_FREEBOARD";continue;}if(hi-lo>.6f){result.rangeRejectedTrials++;attempt.rejectedReason="ORIGINAL_WHOLE_COURT_0_6M_SUPPORT_RANGE";continue;}AlignBase(root,new Vector3(b.center.x,hi-.035f,b.center.z));Physics.SyncTransforms();b=BoundsOf(root);attempt.bounds=b;attempt.rootPosition=root.position;result.bounds=b;
                if(!FinishBoundsClearOfRoads(world,b,w=>attempt.blocker=w)||!ClearRoadEnvelope(world,root,false,w=>attempt.blocker=w)){result.roadRejectedTrials++;attempt.rejectedReason="ORIGINAL_ROAD_START_OBJECTIVE_CLEARANCE";continue;}
                var blocker=Physics.OverlapBox(b.center,b.extents*.98f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).FirstOrDefault(c=>!own.Contains(c)&&!(c is TerrainCollider)&&!c.name.StartsWith("Chunk",StringComparison.Ordinal)&&c.transform.IsChildOf(world.transform));
                if(blocker!=null){result.overlapRejectedTrials++;attempt.rejectedReason="ORIGINAL_OTHER_WORLD_COLLIDER";attempt.blocker=new AuthoredClearanceWitness{kind="ACTUAL_NATIVE_WORLD_COLLIDER",path=RejectedWorldObjectPath(blocker.transform,world.transform),bounds=blocker.bounds,point=b.center,queryExtents=b.extents*.98f,queryRotation=Quaternion.identity};continue;}attempt.rejectedReason="ACCEPTED_ALL_ORIGINAL_GATES";
                result.groundSamples=samples;result.supportHeightRange=hi-lo;result.minimumFreeboard=lo+2;result.trial=trial;placed=true;break;
            }
            if(!placed)throw new InvalidOperationException("Exact artist group cannot retain its stacked source poses on one actual flat dry roadside court; no isolated item scatter or silent omission.");
            result.position=root.position;result.rotation=root.rotation;result.scale=root.localScale;result.bounds=BoundsOf(root);result.roadAndObjectiveBuffersClear=true;result.passed=true;result.placementStatus="ORIGINAL_WHOLE_GROUP_PLACEMENT_PASSED_REQUIRES_FINAL_WORLD_GATES";
        }
        static void ConfigureSourceAuthoredStaticCollision(Transform root)
        {
            // The existing61-instance marketplace's exact instance-only adapter:
            // retain authored colliders, use primary LOD meshes where absent,
            // and never modify a source prefab or create LOD duplicates.
            var secondary=DragonKingdomWorldAuthoring.ConfigureAccurateLodCollision(root);
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))if(mesh.sharedMesh!=null&&!secondary.Contains(mesh.gameObject)&&mesh.GetComponent<Collider>()==null)mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
        }
        [Serializable]public sealed class ArtistOverrideProof
        {
            public string targetId,targetGuid,property,sourceValue,sourceValueFloatBits,state,authority="ACTUAL_IMPORTED_INHERITED_LIGHT_GUID_LOCAL_ID_AND_PUBLIC_DISABLED_OVERRIDE_CHECK",actualLightType,actualOriginalSourceGuid,actualOriginalSourceLocalId,actualPersistentGuid,actualPersistentLocalId,actualPersistentPrefabId,nativeIdentityMethod;
            public bool applied,publicOverrideDisabled,useBoundingSphereOverride,authoredOriginalOverrideDisabled;public int examinedPersistentComponents,targetMatches;
        }
        static ArtistOverrideProof[] ApplyArtistOverrides(Transform root,ArtistInstance row)
        {
            var result=new System.Collections.Generic.List<ArtistOverrideProof>();
            foreach(var change in row.overrides)
            {
                var proof=MeasureOriginalOverrideTarget(row,change);
                if(change.state!="INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE"||change.applied||proof.targetMatches!=1||proof.actualLightType!="UnityEngine.Light"||proof.useBoundingSphereOverride||!proof.authoredOriginalOverrideDisabled)throw new InvalidOperationException("Exact actually disabled inherited source Light required: "+JsonUtility.ToJson(proof));
                var lights=root.GetComponentsInChildren<Light>(true);if(lights.Length!=1||lights[0].useBoundingSphereOverride)throw new InvalidOperationException("Artist instance must preserve its one source Light's public disabled bound override state.");
                // Do not write the historical disabled bound payload, which
                // includes invalid/NaN values. Preserve exact raw source text
                // and bits as documentary evidence only; the authored light
                // keeps its default finite bounds and public disabled flag.
                proof.publicOverrideDisabled=true;proof.applied=false;result.Add(proof);
            }
            return result.ToArray();
        }
        static ArtistOverrideProof MeasureOriginalOverrideTarget(ArtistInstance row,ArtistOverride change)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);if(source==null)throw new InvalidOperationException("Actual source prefab hierarchy missing.");var components=source.GetComponentsInChildren<Component>(true).Where(c=>c!=null).ToArray();
            var targets=components.Where(c=>SourceIdentityMatches(c,change.targetGuid,change.targetId)).ToArray();string identity="PERSISTENT_OR_GLOBAL_SOURCE_LOCAL_ID";
            if(targets.Length==0)
            {
                targets=components.OfType<Light>().Where(light=>NativeNestedLightIdentity(light,change,row.prefabGuid)).Cast<Component>().ToArray();identity="NATIVE_NESTED_TRANSFORM_AND_ORIGINAL_SOURCE_XOR";
            }
            var proof=new ArtistOverrideProof{targetId=change.targetId,targetGuid=change.targetGuid,property=change.property,sourceValue=change.sourceValue,sourceValueFloatBits=change.sourceValueFloatBits,state=change.state,examinedPersistentComponents=components.Length,targetMatches=targets.Length,nativeIdentityMethod=identity};
            if(targets.Length==1)
            {
                var target=targets[0];proof.actualLightType=target.GetType().FullName;var global=GlobalObjectId.GetGlobalObjectIdSlow(target);proof.actualPersistentGuid=global.assetGUID.ToString();proof.actualPersistentLocalId=global.targetObjectId.ToString(System.Globalization.CultureInfo.InvariantCulture);proof.actualPersistentPrefabId=global.targetPrefabId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var original=PrefabUtility.GetCorrespondingObjectFromOriginalSource(target);if(original==null)original=target;
                if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string guid,out long id)){proof.actualOriginalSourceGuid=guid;proof.actualOriginalSourceLocalId=id.ToString(System.Globalization.CultureInfo.InvariantCulture);}
                var light=target as Light;proof.useBoundingSphereOverride=light==null||light.useBoundingSphereOverride;proof.authoredOriginalOverrideDisabled=original is Light authored&&!authored.useBoundingSphereOverride;
                if(change.sourceComponentType!="UnityEngine.Light"||proof.actualOriginalSourceGuid!=change.sourceComponentGuid||proof.actualOriginalSourceLocalId!=change.sourceComponentId)throw new InvalidOperationException("Actual inherited component's original source GUID/local ID differs: "+JsonUtility.ToJson(proof));
            }
            return proof;
        }
        static bool NativeNestedLightIdentity(Light light,ArtistOverride change,string outerGuid)
        {
            var original=PrefabUtility.GetCorrespondingObjectFromOriginalSource(light);if(original==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string guid,out long sourceId)||guid!=change.sourceComponentGuid||sourceId.ToString(System.Globalization.CultureInfo.InvariantCulture)!=change.sourceComponentId)return false;
            var currentRoot=PrefabUtility.GetNearestPrefabInstanceRoot(light.gameObject);if(currentRoot==null)return false;var originalTransform=PrefabUtility.GetCorrespondingObjectFromOriginalSource(currentRoot.transform);if(originalTransform==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(originalTransform,out string transformGuid,out long originalTransformId)||transformGuid!=change.sourceComponentGuid)return false;
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(currentRoot.transform,out string currentGuid,out long currentTransformId)||currentGuid!=outerGuid)return false;
            ulong instance=unchecked((ulong)currentTransformId)^unchecked((ulong)originalTransformId);return instance.ToString(System.Globalization.CultureInfo.InvariantCulture)==change.nestedSourceInstanceId&&(instance^unchecked((ulong)sourceId)).ToString(System.Globalization.CultureInfo.InvariantCulture)==change.targetId;
        }
        static bool SourceIdentityMatches(UnityEngine.Object value,string guid,string id)
        {
            foreach(var source in new[]{value,PrefabUtility.GetCorrespondingObjectFromSource(value),PrefabUtility.GetCorrespondingObjectFromOriginalSource(value)})
                if(source!=null)
                {
                    if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string actualGuid,out long actualId)&&actualGuid==guid&&actualId.ToString(System.Globalization.CultureInfo.InvariantCulture)==id)return true;
                    var global=GlobalObjectId.GetGlobalObjectIdSlow(source);if(global.assetGUID.ToString()==guid&&(global.targetObjectId.ToString(System.Globalization.CultureInfo.InvariantCulture)==id||(global.targetObjectId^global.targetPrefabId).ToString(System.Globalization.CultureInfo.InvariantCulture)==id))return true;
                }
            return false;
        }
        public static void ValidateArtistMarket(Transform root,DragonKingdomWorld world,Receipt receipt)
        {
            var proof=receipt.finish?.artistMarket;var source=ReadArtistLayout();if(proof==null||!proof.passed||!proof.exactDependencies||!proof.exactSourcePoses||!proof.roadAndObjectiveBuffersClear||proof.groups!=1||proof.instances!=61||proof.prefabs!=28||root.name!=ArtistMarketName||root.parent!=world.Region("village").root||root.childCount!=61||proof.children.Length!=61||proof.dependencies.Length!=103||proof.layout!=ArtistMarketLayout||proof.layoutSha256!=AssetSha(ArtistMarketLayout)||proof.layoutMetaSha256!=AssetSha(ArtistMarketLayout+".meta")||proof.layoutGuid!=AssetDatabase.AssetPathToGUID(ArtistMarketLayout)||proof.sourceSceneSha256!=OriginalMarketSceneSha||proof.identityParent!="2075655012"||proof.appliedSourceOverrides!=0||proof.inactiveSourceOverrides!=12||proof.materialImports==null||!proof.materialImports.passed||proof.groundSamples!=49||proof.supportHeightRange>.6f||proof.minimumFreeboard<1.5f)throw new InvalidOperationException("Exact artist layout, dependency and dry court evidence changed.");
            if(JsonUtility.ToJson(proof.materialImports)!=JsonUtility.ToJson(ValidateArtistMaterialImports(source)))throw new InvalidOperationException("Saved native material import provenance differs from exact current source proof.");
            foreach(var dependency in source.dependencies){var saved=proof.dependencies.SingleOrDefault(d=>d.path==dependency.path);if(saved==null||saved.guid!=dependency.guid||saved.sha256!=dependency.sha256||saved.metaSha256!=dependency.metaSha256||saved.actualSha256!=dependency.actualSha256||saved.approvedNativeImport!=dependency.approvedNativeImport)throw new InvalidOperationException("Saved artist dependency original/imported provenance differs.");}
            if(Vector3.Distance(root.position,proof.position)>.001f||Vector3.Distance(root.localScale,proof.scale)>.0001f||Quaternion.Angle(root.rotation,proof.rotation)>.001f)throw new InvalidOperationException("Artist court parent pose changed.");
            for(int i=0;i<61;i++)
            {
                var t=root.GetChild(i);var row=source.instances[i];var saved=proof.children[i];if(t.name!=saved.name||saved.sceneInstanceId!=row.sceneInstanceId||saved.source!=row.prefab||saved.guid!=row.prefabGuid||Vector3.Distance(t.localPosition,row.position)>.0001f||Vector3.Distance(t.localScale,row.scale)>.0001f||Quaternion.Angle(t.localRotation,row.rotation)>.005f||saved.sourceOverrides!=row.overrides.Length)throw new InvalidOperationException("Artist source identity or relative authored pose changed: "+i);
                var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(row.prefab,true),StringComparer.Ordinal);if(t.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==null||!dependencies.Contains(AssetDatabase.GetAssetPath(f.sharedMesh))))throw new InvalidOperationException("Artist group gained geometry outside actual source dependency closure.");
                if(saved.appliedSourceOverrides!=0||saved.inactiveSourceOverrides!=row.overrides.Length||saved.overrideEvidence.Length!=row.overrides.Length)throw new InvalidOperationException("Exact actual disabled inherited Light metadata count changed.");
                for(int j=0;j<row.overrides.Length;j++){var change=row.overrides[j];var measured=MeasureOriginalOverrideTarget(row,change);var prior=saved.overrideEvidence[j];if(change.state!="INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE"||change.applied||measured.targetMatches!=1||measured.useBoundingSphereOverride||!measured.authoredOriginalOverrideDisabled||prior.applied||!prior.publicOverrideDisabled||prior.targetMatches!=1||prior.useBoundingSphereOverride||!prior.authoredOriginalOverrideDisabled||prior.actualLightType!="UnityEngine.Light"||prior.actualOriginalSourceGuid!=change.sourceComponentGuid||prior.actualOriginalSourceLocalId!=change.sourceComponentId||prior.examinedPersistentComponents!=measured.examinedPersistentComponents||prior.examinedPersistentComponents<1||prior.authority!=measured.authority||prior.targetId!=change.targetId||prior.targetGuid!=change.targetGuid||prior.property!=change.property||prior.sourceValue!=change.sourceValue||prior.sourceValueFloatBits!=change.sourceValueFloatBits)throw new InvalidOperationException("Actual imported disabled inherited Light override classification changed.");}
            }
            if(!FinishBoundsClearOfRoads(world,BoundsOf(root)))throw new InvalidOperationException("Artist court obstructs actual road/objective buffers.");
        }
        public static void ValidateMooredShip(Transform root,DragonKingdomWorld world,Receipt receipt)
        {
            var proof=receipt.finish?.mooredShip;if(proof==null||!proof.passed||!proof.originalFerryPoseExact||proof.depthSamples!=25||proof.departureSweepSamples!=53||proof.minimumKeelClearance<3||proof.departureLateralClearance<10||proof.waterlineWidth<5||proof.waterlineLength<15||proof.waterlineIntersections<12||Mathf.Abs(proof.hullLength-32)>.01f||Mathf.Abs(proof.seaLevel-proof.hullKeel-proof.hullDraft)>.01f||proof.waterline==null||proof.waterline.Length!=proof.waterlineIntersections||proof.seaLevel!= -2||root.name!="Actual owned side-moored trader ship"||root.parent!=world.Region("harbor").root||proof.sourceGuid!=AssetDatabase.AssetPathToGUID(proof.source)||proof.sourceSha256!=AssetSha(proof.source)||proof.sourceMetaSha256!=AssetSha(proof.source+".meta"))throw new InvalidOperationException("Actual trader source, geometry or waterline/departure evidence changed.");
            var bounds=BoundsOf(root);if(Vector3.Distance(root.position,proof.position)>.001f||Quaternion.Angle(root.rotation,proof.rotation)>.001f||Vector3.Distance(root.localScale,proof.scale)>.0001f||Vector3.Distance(bounds.center,proof.bounds.center)>.01f||Vector3.Distance(bounds.size,proof.bounds.size)>.01f||Mathf.Abs(bounds.min.y-proof.keel)>.01f||proof.waterline.Any(p=>Mathf.Abs(p.y-proof.seaLevel)>.001f))throw new InvalidOperationException("Actual side-moored hull pose/bounds/waterline differs from measured source evidence.");
            var terrains=world.GetComponentsInChildren<Terrain>(true);float clearance=float.PositiveInfinity;for(int z=0;z<5;z++)for(int x=0;x<5;x++){var p=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,x/4f),0,Mathf.Lerp(bounds.min.z,bounds.max.z,z/4f));clearance=Mathf.Min(clearance,bounds.min.y-DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,p));}if(Mathf.Abs(clearance-proof.minimumKeelClearance)>.01f)throw new InvalidOperationException("Actual trader hull seabed support changed.");
        }
        const string MooredTraderSource=DragonKingdomWorldAuthoring.Art+"Tarbo Studio/TARBO-FantasyVillage/Prefabs/Props/Tools/TB_Prop_Ship.prefab";
        static Transform PrepareMooredTraderHull(Transform parent,out Bounds hullBounds,out float draft)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(MooredTraderSource);if(source==null||source.GetComponentsInChildren<Behaviour>(true).Length!=0)throw new InvalidOperationException("Audited static trader source required; preview must not invoke gameplay callbacks.");
            var hint=new Vector3(805,0,-735);var ship=DragonKingdomWorldAuthoring.PlaceOwnedArtForReview(parent,MooredTraderSource,hint,0,1,true);ship.name="Actual owned side-moored trader ship";
            // One pose authority for both the depth authoring probe and the
            // actual installed source hull:32m length and60% physical draft.
            var hull=ship.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.sharedMesh.name=="TB_Prop_Ship").transform;hullBounds=BoundsOf(hull);float authoredLength=Mathf.Max(hullBounds.size.x,hullBounds.size.z);if(authoredLength<.01f)throw new InvalidOperationException("Actual source hull length missing.");ship.localScale*=32/authoredLength;hullBounds=BoundsOf(hull);ship.rotation=Quaternion.Euler(0,hullBounds.size.x>=hullBounds.size.z?90:0,0);hullBounds=BoundsOf(hull);draft=hullBounds.size.y*.6f;ship.position+=new Vector3(hint.x-hullBounds.center.x,-2-draft-hullBounds.min.y,hint.z-hullBounds.center.z);Physics.SyncTransforms();hullBounds=BoundsOf(hull);return ship;
        }
        static MooredShip InstallMooredTraderShip(DragonKingdomWorld world,string folder,LandmarkFinish evidence,List<Placement> records)
        {
            const string source=MooredTraderSource;var ferry=world.extractionSites.Single(site=>site.vessel!=null).vessel;var ferryPose=Pose(ferry.transform);var hint=new Vector3(805,0,-735);var ship=PrepareMooredTraderHull(world.Region("harbor").root,out var hullBounds,out float draft);ConfigureFinishMaterials(ship,folder);
            var hull=ship.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.sharedMesh.name=="TB_Prop_Ship").transform;
            var b=BoundsOf(ship);var result=new MooredShip{source=source,sourceGuid=AssetDatabase.AssetPathToGUID(source),sourceSha256=AssetSha(source),sourceMetaSha256=AssetSha(source+".meta"),bounds=b,position=ship.position,scale=ship.localScale,rotation=ship.rotation,seaLevel=-2,keel=b.min.y,hullLength=Mathf.Max(hullBounds.size.x,hullBounds.size.z),hullHeight=hullBounds.size.y,hullKeel=hullBounds.min.y,hullDraft=draft,minimumKeelClearance=float.PositiveInfinity,departureLateralClearance=b.min.x-(ferry.transform.position.x+12),originalFerryPoseExact=Pose(ferry.transform)==ferryPose};
            var crossing=new List<Vector3>();
            foreach(var filter in new[]{hull.GetComponent<MeshFilter>()})
            {
                var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)for(int e=0;e<3;e++){var a=filter.transform.TransformPoint(vertices[triangles[i+e]]);var c=filter.transform.TransformPoint(vertices[triangles[i+(e+1)%3]]);if((a.y+2)*(c.y+2)>=0||Mathf.Abs(a.y-c.y)<.00001f)continue;crossing.Add(Vector3.Lerp(a,c,(-2-a.y)/(c.y-a.y)));}
            }
            result.waterlineIntersections=crossing.Count;result.waterline=crossing.ToArray();if(crossing.Count>0){result.waterlineWidth=crossing.Max(p=>p.x)-crossing.Min(p=>p.x);result.waterlineLength=crossing.Max(p=>p.z)-crossing.Min(p=>p.z);}
            var terrains=world.GetComponentsInChildren<Terrain>(true);for(int z=0;z<5;z++)for(int x=0;x<5;x++){var p=new Vector3(Mathf.Lerp(b.min.x,b.max.x,x/4f),0,Mathf.Lerp(b.min.z,b.max.z,z/4f));float bed=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,p);result.minimumKeelClearance=Mathf.Min(result.minimumKeelClearance,b.min.y-bed);result.depthSamples++;}
            var own=new HashSet<Collider>(ship.GetComponentsInChildren<Collider>(true));bool departureClear=true;
            for(int i=0;i<=52;i++){var center=Vector3.Lerp(ferry.transform.position,ferry.departureWaypoints.Last().position,i/52f);var hit=Physics.OverlapBox(center+Vector3.up*8,new Vector3(12,12,26),ferry.transform.rotation,~0,QueryTriggerInteraction.Ignore);if(hit.Any(c=>own.Contains(c)))departureClear=false;result.departureSweepSamples++;}
            result.passed=Mathf.Abs(result.hullLength-32)<.01f&&Mathf.Abs(result.seaLevel-result.hullKeel-result.hullDraft)<.01f&&result.originalFerryPoseExact&&result.depthSamples==25&&result.minimumKeelClearance>=3&&result.departureLateralClearance>=10&&result.waterlineIntersections>=12&&result.waterlineWidth>=5&&result.waterlineLength>=15&&departureClear&&FinishBoundsClearOfRoads(world,b);
            evidence.mooredShip=result;if(!result.passed)throw new InvalidOperationException("Actual side-moored hull/waterline/depth/ferry corridor rejects placement: "+JsonUtility.ToJson(result));
            evidence.sources.Add(source);records.Add(new Placement{region="harbor",name=ship.name,source=source,operation="Add complete owned art with actual material/collision, measured hull waterline/seabed clearance and unchanged evacuation departure corridor",before=hint,after=ship.position,bounds=b});return result;
        }
    }
}
#endif
