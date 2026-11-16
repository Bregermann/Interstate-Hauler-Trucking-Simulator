#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class RidgeComposition
        {
            public string scope="RECOMPOSE_SAME_172_OWNED_ROOTS_ACTUAL_MESH_EXPOSURE_AND_TERRAIN_SUPPORT_NOT_RENDER_ACCEPTANCE";
            public string inputDefinition,inputDefinitionGuid,inputDefinitionSha256,inputDefinitionMetaSha256;public string placementOrderPolicy="ORIGINAL_ORDER_EXCEPT_REFUGE_ROCKS_DESCENDING_ACTUAL_PRIMARY_XZ_FOOTPRINT";
            public int alpineRocks,nestRoostTrees,underbrush,acceptedParts,rejectedParts;
            public string[] failures=Array.Empty<string>();
            public bool passed,sourceIdentitiesPreserved,countsPreserved,roadStartObjectiveAndWingBuffersClear,actualMeshExposureAndSupport;
            public RidgeCompositionPart[] parts;
        }
        public static RidgeComposition LastRidgeComposition{get;private set;}
        [Serializable]public sealed class RidgeCompositionPart
        {
            public string region,name,source,sourceGuid,sourceLocalId,sourceSha256,sourceMetaSha256,lastRejectedReason,candidatePhase;
            public string primaryGeometryMethod="COMPLETE_ACTIVE_PRIMARY_LOD_MESH_VERTICES_EXCLUDING_PADDED_MESH_BOUNDS";
            public Vector3 before,after,scale,hint;public Quaternion rotation;public Bounds bounds,originalBounds,primaryMeshBounds,originalPrimaryMeshBounds,regionBounds;
            public int originalMeshSamples,originalExposedSamples;public float originalMinimumMeshGroundGap,originalMaximumExposedHeight,originalExposedFraction;public bool originalVisibleAndSupported;
            public int primaryMeshes,primaryVertices,nativeContactRejects,firstPhaseSupportRejects,firstPhaseClearanceRejects,firstPhaseOverlapRejects,firstPhaseMeshRejects,firstPhaseNativeContactRejects,fallbackBoundaryRejects,fallbackDryRejects;public float paddedModelHeight,candidateDistanceFromHint,minimumTerrainHeight;public bool usedRegionFallback;
            public Vector3 lowestGapPrimaryMeshVertex;public RidgeTerrainContact nativeContact;
            public float coarsePrimaryMeshGap,primaryMeshSeatCorrection;public int trial,meshSamples,exposedSamples,supportRejects,clearanceRejects,overlapRejects,meshRejects;public float modelHeight,supportHeightRange,maximumAllowedSupportRange,minimumMeshGroundGap,maximumExposedHeight,exposedFraction;
            public bool sourceIdentityExact,roadStartObjectiveClear,wingClear,otherArtClear,visibleAndSupported,accepted;public string failure;
            public string[] overlapWitnesses=Array.Empty<string>();public RidgeGeometryBinding[] geometry;
        }
        [Serializable]public sealed class RidgeTerrainContact
        {public string collider,terrainData,terrainDataGuid;public Vector3 meshVertex,surface;public float terrainDataHeight,terrainHitDelta,meshGap;public bool passed;}
        [Serializable]public sealed class RidgeGeometryBinding
        {public string key,type,shape,mesh,meshGuid,meshLocalId,meshSha256,meshMetaSha256,materialGuid,materialLocalId;public bool enabled,trigger,primaryLod;public float[] rootMatrix;}
        sealed class RidgeItem
        {public Transform root;public Placement record;public Vector3[] localSamples;public Bounds primaryBounds;public Vector3 primaryRootPosition;public int primaryMeshes,primaryVertices;public bool rock,tree;public Vector3 scale;public Quaternion rotation;public HashSet<Collider> colliders;}

        // Called only by explicit fresh candidate generation, after the existing
        // Finish roots exist. No prefab is spawned, destroyed, scaled or edited.
        public static RidgeComposition RecomposeExistingRidgeArt(DragonKingdomWorld world,string folder,List<Placement> records,bool diagnoseAll=false)
            =>RecomposeExistingRidgeArtWithDefinition(world,folder,records,diagnoseAll,null);
        // Reuse the owned-road context's exact original/candidate certificate.
        // A transient definition alone is never sufficient provenance.
        static string BoundedArtInputDefinition(DragonKingdomWorld world,OwnedDefinitionProvenance provenance)
        {
            if(world==null||world.definition==null)throw new InvalidOperationException("Actual bounded-art world definition required.");
            if(provenance!=null)return provenance.ValidateAndPath(world);
            string path=AssetDatabase.GetAssetPath(world.definition);
            if(!RidgeDefinitionPath(path)||AssetDatabase.LoadAssetAtPath<DragonKingdomDefinition>(path)!=world.definition)throw new InvalidOperationException("Actual persistent definition or exact owned-road provenance required for bounded art.");
            return path;
        }
        static RidgeComposition RecomposeExistingRidgeArtWithDefinition(DragonKingdomWorld world,string folder,List<Placement> records,bool diagnoseAll,OwnedDefinitionProvenance provenance)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||world==null||records==null||
                !folder.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)||folder.Substring((Root+"VisualCandidates/").Length).Contains("/"))
                throw new InvalidOperationException("Explicit stopped fresh world candidate required for ridge composition.");
            string inputPath=BoundedArtInputDefinition(world,provenance);
            var result=new RidgeComposition{inputDefinition=inputPath,inputDefinitionGuid=AssetDatabase.AssetPathToGUID(inputPath),inputDefinitionSha256=AssetSha(inputPath),inputDefinitionMetaSha256=AssetSha(inputPath+".meta")};LastRidgeComposition=result;var items=new List<RidgeItem>();
            foreach(var record in records.Where(r=>r.name.StartsWith("Owned alpine shelter geology ",StringComparison.Ordinal)||r.name.StartsWith("Owned living cliff roost tree ",StringComparison.Ordinal)||r.name.StartsWith("Owned forest underbrush ",StringComparison.Ordinal)))
            {
                var root=world.Region(record.region).root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==record.name);
                var item=new RidgeItem{root=root,record=record,rock=record.name.StartsWith("Owned alpine",StringComparison.Ordinal),tree=record.name.StartsWith("Owned living",StringComparison.Ordinal),scale=root.localScale,rotation=root.rotation,colliders=new HashSet<Collider>(root.GetComponentsInChildren<Collider>(true))};
                item.localSamples=RidgeMeshSamples(root,out item.primaryBounds,out item.primaryMeshes,out item.primaryVertices);item.primaryRootPosition=root.position;items.Add(item);
                if(item.rock)result.alpineRocks++;else if(item.tree)result.nestRoostTrees++;else result.underbrush++;
            }
            if(result.alpineRocks!=24||result.nestRoostTrees!=8||result.underbrush!=140||items.Count!=172)throw new InvalidOperationException("Ridge composition requires exactly the existing24 alpine rocks,8 roost trees and140 underbrush roots.");
            items=OrderRefugeRidgePacking(items).ToList();
            var terrains=world.GetComponentsInChildren<Terrain>(true);var failures=new List<string>();var accepted=new List<Bounds>();var proofs=new List<RidgeCompositionPart>();var geometryHashes=new Dictionary<string,string>();
            var originalSources=items.Select(i=>i.record.source).Distinct().SelectMany(p=>new[]{p,p+".meta"}).ToDictionary(p=>p,AssetSha);
            bool PeerOverlap(RidgeItem item,Bounds b,RidgeCompositionPart proof)
            {
                var peers=accepted.Where(other=>RidgeInterior(b).Intersects(RidgeInterior(other))).Select(other=>"ACCEPTED_RIDGE_BOUNDS "+other.ToString("R"));
                var obstacles=Physics.OverlapBox(b.center,b.extents*.8f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Where(c=>!item.colliders.Contains(c)&&!(c is TerrainCollider)&&!c.name.StartsWith("Chunk",StringComparison.Ordinal)&&c.transform.IsChildOf(world.transform)&&!items.Any(other=>c.transform.IsChildOf(other.root))).Select(c=>c.GetType().Name+" "+c.name+" parent="+(c.transform.parent==null?"":c.transform.parent.name));
                var witnesses=peers.Concat(obstacles).ToArray();if(witnesses.Length==0)return false;proof.overlapWitnesses=proof.overlapWitnesses.Concat(witnesses).Distinct().Take(24).ToArray();return true;
            }
            foreach(var item in items)
            {
                var t=item.root;var initial=t.position;var initialLocalPosition=t.localPosition;int ordinal=RidgeOrdinal(item.record.name);Vector3 hint=RidgeHint(item.record.region,ordinal,item.rock,item.tree);
                float height=item.primaryBounds.size.y;float allowed=item.rock?height*.28f:item.tree?Mathf.Min(2,height*.12f):Mathf.Max(.18f,height*.35f);
                var part=new RidgeCompositionPart{hint=hint,regionBounds=world.definition.FindRegion(item.record.region).bounds,candidatePhase="ORIGINAL_NEAR_HINT_256",region=item.record.region,name=t.name,source=item.record.source,before=initial,after=initial,rotation=t.rotation,scale=t.localScale,bounds=BoundsOf(t),originalBounds=BoundsOf(t),modelHeight=height,paddedModelHeight=BoundsOf(t).size.y,primaryMeshes=item.primaryMeshes,primaryVertices=item.primaryVertices,maximumAllowedSupportRange=allowed};
                var originalMesh=new RidgeCompositionPart{modelHeight=height};MeasureRidgeMesh(item,terrains,originalMesh);part.originalMeshSamples=originalMesh.meshSamples;part.originalExposedSamples=originalMesh.exposedSamples;part.originalMinimumMeshGroundGap=originalMesh.minimumMeshGroundGap;part.originalMaximumExposedHeight=originalMesh.maximumExposedHeight;part.originalExposedFraction=originalMesh.exposedFraction;part.originalVisibleAndSupported=originalMesh.visibleAndSupported;
                part.originalPrimaryMeshBounds=part.primaryMeshBounds=item.primaryBounds;
                // Saved world prefabs add an outer prefab layer. Resolve the
                // exact recorded nested source, never the nearest outer asset.
                var originalSource=PrefabUtility.GetCorrespondingObjectFromSourceAtPath(t.gameObject,part.source);if(originalSource==null||AssetDatabase.GetAssetPath(originalSource)!=part.source||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(originalSource,out string originalGuid,out long originalId))throw new InvalidOperationException("Existing ridge source identity required before placement: "+t.name);
                part.sourceGuid=originalGuid;part.sourceLocalId=originalId.ToString(System.Globalization.CultureInfo.InvariantCulture);part.sourceSha256=AssetSha(part.source);part.sourceMetaSha256=AssetSha(part.source+".meta");part.geometry=RidgeGeometryBindings(t,geometryHashes);
                proofs.Add(part);result.parts=proofs.ToArray();bool placed=false;
                for(int trial=0;trial<RidgeTrialLimit(item.record.region,item.rock,item.tree);trial++)
                {
                    bool fallback=trial>=256;
                    if(trial==256){part.firstPhaseSupportRejects=part.supportRejects;part.firstPhaseClearanceRejects=part.clearanceRejects;part.firstPhaseOverlapRejects=part.overlapRejects;part.firstPhaseMeshRejects=part.meshRejects;part.firstPhaseNativeContactRejects=part.nativeContactRejects;}
                    // Preserve the original256 exact candidates. Only a forest
                    // shoulder that exhausted them may use its bounded32–64m
                    // annulus; mountain rocks may search64–128m. Both stay
                    // nearest to the same measured approach-art hint.
                    float radius=RidgeTrialRadius(item.record.region,item.rock,item.tree,trial),angle=trial*2.399963f;
                    part.usedRegionFallback=fallback;part.candidatePhase=RidgeTrialPhase(item.record.region,item.rock,item.tree,trial);part.candidateDistanceFromHint=radius;
                    Vector3 p=hint+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);var b=BoundsOf(t);var primary=RidgeCurrentPrimaryBounds(item);float half=Mathf.Clamp(Mathf.Min(primary.size.x,primary.size.z)*.35f,.25f,item.rock?10:item.tree?2:1.5f);
                    part.trial=trial;
                    if(fallback&&!RidgeInsideRegionXZ(part.regionBounds,new Bounds(p,new Vector3(primary.size.x,0,primary.size.z)))){part.lastRejectedReason="FALLBACK_OUTSIDE_ACTUAL_INPUT_REGION";part.fallbackBoundaryRejects++;continue;}
                    bool groundFound=TryGround(world,p,half,out float high,out float range);part.supportHeightRange=range;if(!groundFound||range>allowed){part.lastRejectedReason="TERRAIN_SUPPORT_RANGE";part.supportRejects++;continue;}
                    // A30m blanket range used to be subtracted even from8m
                    // rocks. Embed only a small fraction of this real model;
                    // range is an acceptance limit, never a burial distance.
                    if(fallback&&high-range<-.5f){part.lastRejectedReason="FALLBACK_NOT_DRY_ABOVE_EXISTING_SEA_PLUS_1_5M";part.fallbackDryRejects++;continue;}
                    float embed=item.rock?Mathf.Min(height*.10f,Mathf.Max(.12f,range*.45f)):.035f;
                    // Source mesh.bounds can include wind padding, secondary LODs
                    // and rotated empty AABB corners. Seat the complete real
                    // primary-LOD vertex envelope, leaving all authored mesh,
                    // LOD, collider, scale and orientation data untouched.
                    t.position+=new Vector3(p.x-primary.center.x,high-embed-primary.min.y,p.z-primary.center.z);Physics.SyncTransforms();b=BoundsOf(t);part.primaryMeshBounds=RidgeCurrentPrimaryBounds(item);
                    part.after=t.position;part.bounds=b;part.supportHeightRange=range;
                    if(!FinishBoundsClearOfRoads(world,b)||(item.rock||item.tree)&&!ClearRoadEnvelope(world,t,item.tree)||!RidgeWingClear(world,b)){part.lastRejectedReason="ROAD_START_OBJECTIVE_OR_WING_CLEARANCE";part.clearanceRejects++;continue;}
                    bool overlap=PeerOverlap(item,b,part);
                    if(overlap){part.lastRejectedReason="OTHER_ART_OVERLAP";part.overlapRejects++;continue;}
                    MeasureRidgeMesh(item,terrains,part);
                    if(SeatFloatingRidgePrimaryMesh(item,terrains,part)>0)
                    {
                        Physics.SyncTransforms();b=BoundsOf(t);part.after=t.position;part.bounds=b;part.primaryMeshBounds=RidgeCurrentPrimaryBounds(item);
                        if(!FinishBoundsClearOfRoads(world,b)||(item.rock||item.tree)&&!ClearRoadEnvelope(world,t,item.tree)||!RidgeWingClear(world,b)){part.lastRejectedReason="ROAD_START_OBJECTIVE_OR_WING_CLEARANCE";part.clearanceRejects++;continue;}
                        if(PeerOverlap(item,b,part)){part.lastRejectedReason="OTHER_ART_OVERLAP";part.overlapRejects++;continue;}
                    }
                    if(fallback&&(!RidgeInsideRegionXZ(part.regionBounds,b)||part.minimumTerrainHeight<-.5f)){part.lastRejectedReason="FALLBACK_FULL_MODEL_OUTSIDE_REGION_OR_MESH_FOOTPRINT_NOT_DRY";if(!RidgeInsideRegionXZ(part.regionBounds,b))part.fallbackBoundaryRejects++;else part.fallbackDryRejects++;continue;}
                    if(!part.visibleAndSupported){part.lastRejectedReason="ACTUAL_PRIMARY_MESH_SUPPORT_OR_EXPOSURE";part.meshRejects++;continue;}
                    part.nativeContact=RidgeNativeTerrainContact(world,terrains,part.lowestGapPrimaryMeshVertex,height,item.rock);
                    if(!part.nativeContact.passed){part.lastRejectedReason="ACTUAL_NATIVE_TERRAIN_COLLIDER_CONTACT";part.nativeContactRejects++;continue;}
                    var source=PrefabUtility.GetCorrespondingObjectFromSourceAtPath(t.gameObject,part.source);
                    if(source==null||AssetDatabase.GetAssetPath(source)!=part.source||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id))throw new InvalidOperationException("Existing ridge source identity was lost: "+t.name);
                    part.sourceGuid=guid;part.sourceLocalId=id.ToString(System.Globalization.CultureInfo.InvariantCulture);part.sourceSha256=AssetSha(part.source);part.sourceMetaSha256=AssetSha(part.source+".meta");
                    part.after=t.position;part.rotation=t.rotation;part.scale=t.localScale;part.bounds=b;part.trial=trial;part.supportHeightRange=range;part.sourceIdentityExact=t.localScale==item.scale&&Quaternion.Angle(t.rotation,item.rotation)<.001f;part.roadStartObjectiveClear=part.wingClear=part.otherArtClear=true;
                    part.geometry=RidgeGeometryBindings(t,geometryHashes);
                    if(!part.sourceIdentityExact)throw new InvalidOperationException("Ridge placement changed the original source scale/orientation.");
                    records.Add(new Placement{region=item.record.region,name=item.record.name,source=item.record.source,before=initial,after=t.position,bounds=b,trial=trial,operation="Recompose same owned root near measured approach with model-relative slope, actual exposed supported mesh and clear road/start/wing/objective/other-art buffers"});
                    part.lastRejectedReason="";part.accepted=true;result.acceptedParts++;accepted.Add(b);placed=true;break;
                }
                if(!placed)
                {
                    t.localPosition=initialLocalPosition;Physics.SyncTransforms();
                    part.failure="No visible supported clear bounded ridge placement for "+item.record.region+"/"+t.name+"; actual model height="+height+", allowed range="+allowed+"; support="+part.supportRejects+", clearance="+part.clearanceRejects+", overlap="+part.overlapRejects+", actualMesh="+part.meshRejects+", nativeContact="+part.nativeContactRejects+", minMeshGroundGap="+part.minimumMeshGroundGap+", maxExposedHeight="+part.maximumExposedHeight+", exposedFraction="+part.exposedFraction+". No asset/count substitution.";
                    failures.Add(part.failure);result.rejectedParts++;result.failures=failures.ToArray();
                    if(!diagnoseAll)throw new InvalidOperationException(part.failure);
                }
            }
            if(originalSources.Any(p=>AssetSha(p.Key)!=p.Value))throw new InvalidOperationException("Ridge composition modified owned source bytes.");
            result.parts=proofs.ToArray();if(failures.Count>0)throw new InvalidOperationException("Exhaustive ridge diagnostic rejected "+failures.Count+" of172 exact original roots; no partial composition accepted. "+string.Join(" | ",failures));
            result.countsPreserved=result.sourceIdentitiesPreserved=result.roadStartObjectiveAndWingBuffersClear=result.actualMeshExposureAndSupport=result.passed=true;return result;
        }
        // WorldAdoptionPlan supplies a persistent saved prefab. Inspect its
        // actual mesh/heightfield/source/graph data without pretending those
        // prefab-asset colliders participate in the live Physics scene. Fresh
        // generation above owns the separate native collider-query witness.
        public static void ValidateSavedRidgeComposition(DragonKingdomWorld world,RidgeComposition proof)
        {
            if(world==null||proof==null||!proof.passed||!proof.sourceIdentitiesPreserved||!proof.countsPreserved||!proof.roadStartObjectiveAndWingBuffersClear||!proof.actualMeshExposureAndSupport||proof.alpineRocks!=24||proof.nestRoostTrees!=8||proof.underbrush!=140||proof.acceptedParts!=172||proof.rejectedParts!=0||proof.failures==null||proof.failures.Length!=0||proof.parts==null||proof.parts.Length!=172||proof.parts.Select(p=>p.region+"/"+p.name).Distinct().Count()!=172)throw new InvalidOperationException("Complete same172-root native ridge proof required.");
            if(!RidgeDefinitionPath(proof.inputDefinition)||AssetSha(proof.inputDefinition)!=proof.inputDefinitionSha256||AssetSha(proof.inputDefinition+".meta")!=proof.inputDefinitionMetaSha256||AssetDatabase.AssetPathToGUID(proof.inputDefinition)!=proof.inputDefinitionGuid)throw new InvalidOperationException("Saved ridge original input definition bytes/GUID changed.");
            var inputDefinition=AssetDatabase.LoadAssetAtPath<DragonKingdomDefinition>(proof.inputDefinition);if(inputDefinition==null)throw new InvalidOperationException("Actual saved original ridge input definition required.");
            var actual=world.regions.SelectMany(region=>region.root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Owned alpine shelter geology ",StringComparison.Ordinal)||t.name.StartsWith("Owned living cliff roost tree ",StringComparison.Ordinal)||t.name.StartsWith("Owned forest underbrush ",StringComparison.Ordinal)).Select(t=>new{region=region.id,root=t})).ToArray();
            if(actual.Length!=172||actual.Count(t=>t.root.name.StartsWith("Owned alpine",StringComparison.Ordinal))!=24||actual.Count(t=>t.root.name.StartsWith("Owned living",StringComparison.Ordinal))!=8||actual.Count(t=>t.root.name.StartsWith("Owned forest",StringComparison.Ordinal))!=140)throw new InvalidOperationException("Saved ridge root identities/counts differ.");
            var terrains=world.GetComponentsInChildren<Terrain>(true);var geometryHashes=new Dictionary<string,string>();var accepted=new List<Bounds>();
            foreach(var part in proof.parts)
            {
                var t=actual.Single(a=>a.region==part.region&&a.root.name==part.name).root;var b=BoundsOf(t);var source=PrefabUtility.GetCorrespondingObjectFromSourceAtPath(t.gameObject,part.source);
                if(!part.accepted||!string.IsNullOrEmpty(part.failure)||source==null||AssetDatabase.GetAssetPath(source)!=part.source||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long localId)||guid!=part.sourceGuid||localId.ToString(System.Globalization.CultureInfo.InvariantCulture)!=part.sourceLocalId||AssetSha(part.source)!=part.sourceSha256||AssetSha(part.source+".meta")!=part.sourceMetaSha256||Vector3.Distance(t.position,part.after)>.001f||Vector3.Distance(t.localScale,part.scale)>.0001f||Quaternion.Angle(t.rotation,part.rotation)>.001f||Vector3.Distance(b.center,part.bounds.center)>.01f||Vector3.Distance(b.size,part.bounds.size)>.01f)throw new InvalidOperationException("Saved ridge source identity, exact authored scale/pose or actual model bounds differ: "+part.name);
                var bindings=RidgeGeometryBindings(t,geometryHashes);
                if(part.geometry==null||bindings.Length!=part.geometry.Length)throw new InvalidOperationException("Saved ridge collider/model bindings changed.");
                for(int index=0;index<bindings.Length;index++)
                {
                    var a=bindings[index];var p=part.geometry[index];var matrix=a.rootMatrix;var expected=p.rootMatrix;
                    bool Same(string first,string second)=>string.Equals(first??"",second??"",StringComparison.Ordinal);
                    bool exact=Same(a.key,p.key)&&Same(a.type,p.type)&&Same(a.shape,p.shape)&&Same(a.mesh,p.mesh)&&Same(a.meshGuid,p.meshGuid)&&Same(a.meshLocalId,p.meshLocalId)&&Same(a.meshSha256,p.meshSha256)&&Same(a.meshMetaSha256,p.meshMetaSha256)&&Same(a.materialGuid,p.materialGuid)&&Same(a.materialLocalId,p.materialLocalId)&&a.enabled==p.enabled&&a.trigger==p.trigger&&a.primaryLod==p.primaryLod;
                    if(!exact||expected==null||expected.Length!=16||Enumerable.Range(0,16).Any(i=>Mathf.Abs(matrix[i]-expected[i])>.0001f))throw new InvalidOperationException("Saved ridge original collider/model geometry, material, state or relative pose differs: "+part.name);
                }
                bool rock=part.name.StartsWith("Owned alpine",StringComparison.Ordinal),tree=part.name.StartsWith("Owned living",StringComparison.Ordinal);
                var samples=RidgeMeshSamples(t,out var primary,out int primaryMeshes,out int primaryVertices);float height=primary.size.y,allowed=rock?height*.28f:tree?Mathf.Min(2,height*.12f):Mathf.Max(.18f,height*.35f);
                if(part.primaryGeometryMethod!="COMPLETE_ACTIVE_PRIMARY_LOD_MESH_VERTICES_EXCLUDING_PADDED_MESH_BOUNDS"||primaryMeshes!=part.primaryMeshes||primaryVertices!=part.primaryVertices||Mathf.Abs(height-part.modelHeight)>.002f||Vector3.Distance(primary.center,part.primaryMeshBounds.center)>.002f||Vector3.Distance(primary.size,part.primaryMeshBounds.size)>.002f)throw new InvalidOperationException("Saved ridge actual primary-LOD vertex geometry differs: "+part.name);
                float half=Mathf.Clamp(Mathf.Min(primary.size.x,primary.size.z)*.35f,.25f,rock?10:tree?2:1.5f);
                if(!TryGround(world,primary.center,half,out _,out float range)||range>allowed||Mathf.Abs(allowed-part.maximumAllowedSupportRange)>.001f||!FinishBoundsClearOfRoads(world,b)||!RidgeWingClear(world,b)||accepted.Any(other=>RidgeInterior(b).Intersects(RidgeInterior(other))))throw new InvalidOperationException("Saved ridge measured support or original road/start/objective/wing/peer buffer failed: "+part.name);
                var measured=new RidgeCompositionPart{modelHeight=height};MeasureRidgeMesh(new RidgeItem{root=t,rock=rock,tree=tree,localSamples=samples},terrains,measured);
                if(!measured.visibleAndSupported||Vector3.Distance(measured.lowestGapPrimaryMeshVertex,part.lowestGapPrimaryMeshVertex)>.002f||measured.meshSamples!=part.meshSamples||measured.exposedSamples!=part.exposedSamples||Mathf.Abs(measured.minimumMeshGroundGap-part.minimumMeshGroundGap)>.002f||Mathf.Abs(measured.maximumExposedHeight-part.maximumExposedHeight)>.002f)throw new InvalidOperationException("Saved ridge actual exposed/supported primary mesh differs: "+part.name);
                // Main intentionally recomputes final region envelopes after all
                // art. The placement gate is pinned to its unchanged INPUT
                // definition, never retroactively widened by the final roots.
                var regionBounds=inputDefinition.FindRegion(part.region).bounds;bool fallback=part.trial>=256;
                if(part.trial<0||part.trial>=RidgeTrialLimit(part.region,rock,tree)||part.usedRegionFallback!=fallback||Vector3.Distance(regionBounds.center,part.regionBounds.center)>.001f||Vector3.Distance(regionBounds.size,part.regionBounds.size)>.001f||Vector3.Distance(RidgeHint(part.region,RidgeOrdinal(part.name),rock,tree),part.hint)>.001f)throw new InvalidOperationException("Saved ridge bounded approach-search provenance differs: "+part.name);
                float distance=Vector2.Distance(new Vector2(primary.center.x,primary.center.z),new Vector2(part.hint.x,part.hint.z));
                if(Mathf.Abs(distance-part.candidateDistanceFromHint)>.002f||part.candidatePhase!=RidgeTrialPhase(part.region,rock,tree,part.trial)||Mathf.Abs(distance-RidgeTrialRadius(part.region,rock,tree,part.trial))>.002f||fallback&&(!RidgeInsideRegionXZ(regionBounds,b)||measured.minimumTerrainHeight<-.5f))throw new InvalidOperationException("Saved ridge fallback left its bounded actual dry region: "+part.name);
                ValidateSavedRidgeTerrainContact(world,terrains,part,samples,t,rock);
                accepted.Add(b);
            }
        }
        static RidgeGeometryBinding[] RidgeGeometryBindings(Transform root,Dictionary<string,string> hashes)
        {
            string Key(Component component)
            {
                var indices=new Stack<int>();for(var t=component.transform;t!=root;t=t.parent)indices.Push(t.GetSiblingIndex());return string.Join("/",indices)+":"+Array.IndexOf(component.GetComponents<Component>(),component);
            }
            string Digest(string path){if(!hashes.TryGetValue(path,out string hash)){hash=AssetSha(path);hashes.Add(path,hash);}return hash;}
            var primaryFilters=new HashSet<MeshFilter>(RidgePrimaryMeshFilters(root));var rows=new List<RidgeGeometryBinding>();
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                if(!(component is MeshFilter)&&!(component is Collider))continue;
                var row=new RidgeGeometryBinding{key=Key(component),type=component.GetType().FullName};Mesh mesh=null;
                if(component is MeshFilter filter){mesh=filter.sharedMesh;row.primaryLod=primaryFilters.Contains(filter);}
                else if(component is Collider collider)
                {
                    row.enabled=collider.enabled;row.trigger=collider.isTrigger;row.shape="contact="+collider.contactOffset.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                    if(collider.sharedMaterial!=null){if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(collider.sharedMaterial,out string mg,out long mi))throw new InvalidOperationException("Persistent original ridge physics material required.");row.materialGuid=mg;row.materialLocalId=mi.ToString(System.Globalization.CultureInfo.InvariantCulture);}
                    if(collider is MeshCollider mc){mesh=mc.sharedMesh;row.shape+=";convex="+mc.convex+";cooking="+(int)mc.cookingOptions;}
                    else if(collider is BoxCollider box)row.shape+=";center="+box.center.ToString("R")+";size="+box.size.ToString("R");
                    else if(collider is CapsuleCollider capsule)row.shape+=";center="+capsule.center.ToString("R")+";radius="+capsule.radius.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+";height="+capsule.height.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+";axis="+capsule.direction;
                    else if(collider is SphereCollider sphere)row.shape+=";center="+sphere.center.ToString("R")+";radius="+sphere.radius.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                    else throw new InvalidOperationException("Unreviewed ridge collider type: "+collider.GetType().FullName);
                }
                if(mesh!=null){if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long id))throw new InvalidOperationException("Original persistent ridge mesh required.");row.mesh=AssetDatabase.GetAssetPath(mesh);row.meshGuid=guid;row.meshLocalId=id.ToString(System.Globalization.CultureInfo.InvariantCulture);row.meshSha256=Digest(row.mesh);row.meshMetaSha256=Digest(row.mesh+".meta");}
                var matrix=root.worldToLocalMatrix*component.transform.localToWorldMatrix;row.rootMatrix=new float[16];for(int i=0;i<16;i++)row.rootMatrix[i]=matrix[i];rows.Add(row);
            }
            return rows.ToArray();
        }
        static RidgeItem[] OrderRefugeRidgePacking(List<RidgeItem> items)
        {
            // Large rocks have fewer clear slope-supported candidates. Keep
            // every original item and search, and allocate those footprints
            // before small rocks consume the refuge's usable shoulder spaces.
            var refuge=items.Where(i=>i.rock&&i.record.region=="refuge").OrderByDescending(i=>i.primaryBounds.size.x*i.primaryBounds.size.z).ThenBy(i=>i.record.name,StringComparer.Ordinal).ToArray();
            int next=0;return items.Select(i=>i.rock&&i.record.region=="refuge"?refuge[next++]:i).ToArray();
        }
        static int RidgeTrialLimit(string region,bool rock,bool tree)=>region=="forest"&&!rock&&!tree?768:region=="refuge"&&rock?2048:region=="mountains"&&rock?1024:256;
        static string RidgeTrialPhase(string region,bool rock,bool tree,int trial)=>region=="refuge"&&rock&&trial>=1024?"SAME_REFUGE_ACTUAL_REGION_SHOULDER_64_TO_128M_1024":trial<256?"ORIGINAL_NEAR_HINT_256":region=="mountains"&&rock?"SAME_MOUNTAIN_SHOULDER_64_TO_128M_768":region=="refuge"&&rock?"DENSER_SAME_REFUGE_SHOULDER_WITHIN_ORIGINAL_RADIUS_768":"SAME_FOREST_SHOULDER_32_TO_64M_512";
        static float RidgeTrialRadius(string region,bool rock,bool tree,int trial)
        {
            if(trial<256)return trial==0?0:(rock||tree?4:2)*Mathf.Sqrt(trial);
            if(region=="refuge"&&rock)return trial<1024?4*Mathf.Sqrt(255f)*Mathf.Sqrt((trial-256+.5f)/768f):Mathf.Sqrt(64*64+(128*128-64*64)*((trial-1024+.5f)/1024f));
            return region=="mountains"&&rock?Mathf.Sqrt(64*64+(128*128-64*64)*((trial-256)/767f)):Mathf.Sqrt(32*32+(64*64-32*32)*((trial-256)/511f));
        }
        static bool RidgeDefinitionPath(string path)=>path==Root+"Configuration/Dragon Kingdom.asset"||!string.IsNullOrEmpty(path)&&path.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)&&path.EndsWith("/Dragon Kingdom.asset",StringComparison.Ordinal);
        static bool RidgeInsideRegionXZ(Bounds region,Bounds art)=>art.min.x>=region.min.x&&art.max.x<=region.max.x&&art.min.z>=region.min.z&&art.max.z<=region.max.z;
        static Bounds RidgeInterior(Bounds b){b.extents*=.7f;return b;}
        static int RidgeOrdinal(string name)=>int.Parse(name.Substring(name.LastIndexOf(' ')+1),System.Globalization.CultureInfo.InvariantCulture)-1;
        static Vector3 RidgeHint(string region,int ordinal,bool rock,bool tree)
        {
            Vector3 eye,target;
            // Actual V2171280x720 approach witnesses, not arbitrary world halves.
            switch(region)
            {
                case "mountains":eye=new Vector3(-58.483936f,306,1065.99292f);target=new Vector3(-5.684662f,320,1068.36157f);break;
                case "refuge":eye=new Vector3(-301.51605f,336,1174.00708f);target=new Vector3(-360,335,1265);break;
                case "nests":eye=new Vector3(-785.53851f,276,883.69232f);target=new Vector3(-840,274,920);break;
                case "forest":eye=new Vector3(-802.39154f,30,230.12761f);target=new Vector3(-760.36688f,54.644016f,275.89151f);break;
                default:throw new InvalidOperationException("Unexpected existing ridge root region: "+region);
            }
            Vector3 forward=Vector3.ProjectOnPlane(target-eye,Vector3.up).normalized,right=Vector3.Cross(Vector3.up,forward);
            if(rock||tree)return eye+forward*(30+ordinal/2*23)+right*((ordinal%2==0?-1:1)*(32+ordinal%3*12));
            float u=Mathf.Repeat(ordinal*.618033989f,1),v=Mathf.Repeat(ordinal*.754877666f,1);
            return eye+forward*(15+u*165)+right*((ordinal%2==0?-1:1)*(16+v*70));
        }
        static bool RidgeWingClear(DragonKingdomWorld world,Bounds b)
        {
            foreach(var anchor in world.aerialCheckpoints.Concat(world.riderStarts))
                if(anchor!=null&&b.SqrDistance(anchor.position)<18*18)return false;
            for(int i=1;i<world.aerialCheckpoints.Length;i++)
            {
                Vector3 a=world.aerialCheckpoints[i-1].position,z=world.aerialCheckpoints[i].position;
                int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,z)/8));
                for(int j=0;j<=steps;j++)if(b.SqrDistance(Vector3.Lerp(a,z,j/(float)steps))<18*18)return false;
            }
            return true;
        }
        // Existing assembly/bank callers retain their original sampling API;
        // the ridge-only overload additionally measures the complete envelope.
        static Vector3[] RidgeMeshSamples(Transform root)
        {
            var secondary=new HashSet<Renderer>();foreach(var group in root.GetComponentsInChildren<LODGroup>(true))foreach(var lod in group.GetLODs().Skip(1))foreach(var renderer in lod.renderers)secondary.Add(renderer);
            var samples=new List<Vector3>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh==null||renderer==null||!renderer.enabled||!RidgeGeometryObjectActive(renderer.transform)||secondary.Contains(renderer))continue;
                using(var meshes=Mesh.AcquireReadOnlyMeshData(filter.sharedMesh))using(var vertices=new NativeArray<Vector3>(meshes[0].vertexCount,Allocator.Temp))
                {
                    meshes[0].GetVertices(vertices);int stride=Mathf.Max(1,Mathf.CeilToInt(vertices.Length/192f)),lowest=-1;float low=float.PositiveInfinity;
                    for(int i=0;i<vertices.Length;i++)
                    {Vector3 p=root.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));if(p.y<low){low=p.y;lowest=i;}if(i%stride==0)samples.Add(p);}
                    if(lowest>=0)samples.Add(root.InverseTransformPoint(filter.transform.TransformPoint(vertices[lowest])));
                }
            }
            if(samples.Count<8)throw new InvalidOperationException("Actual primary visible mesh samples required: "+root.name);return samples.ToArray();
        }
        static Bounds RidgeCurrentPrimaryBounds(RidgeItem item)=>new Bounds(item.primaryBounds.center+item.root.position-item.primaryRootPosition,item.primaryBounds.size);
        // A persistent prefab asset is not enabled in a loaded scene. Its
        // authored activation still includes EVERY serialized ancestor flag.
        // Live scene objects retain the original native activeInHierarchy rule.
        static bool RidgeGeometryObjectActive(Transform transform)
        {
            if(transform==null)return false;
            if(!EditorUtility.IsPersistent(transform))return transform.gameObject.activeInHierarchy;
            for(var ancestor=transform;ancestor!=null;ancestor=ancestor.parent)if(!ancestor.gameObject.activeSelf)return false;
            return true;
        }
        static IEnumerable<MeshFilter> RidgePrimaryMeshFilters(Transform root)
        {
            var secondary=new HashSet<Renderer>();var primary=new HashSet<Renderer>();
            foreach(var group in root.GetComponentsInChildren<LODGroup>(true))
            {var lods=group.GetLODs();if(lods.Length>0)foreach(var renderer in lods[0].renderers)if(renderer!=null)primary.Add(renderer);foreach(var lod in lods.Skip(1))foreach(var renderer in lod.renderers)if(renderer!=null)secondary.Add(renderer);}
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {var renderer=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh!=null&&renderer!=null&&renderer.enabled&&RidgeGeometryObjectActive(renderer.transform)&&(!secondary.Contains(renderer)||primary.Contains(renderer)))yield return filter;}
        }
        static Vector3[] RidgeMeshSamples(Transform root,out Bounds actualPrimaryBounds,out int primaryMeshes,out int primaryVertices)
        {
            var samples=new List<Vector3>();var bounds=new Bounds();bool first=true;primaryMeshes=primaryVertices=0;
            foreach(var filter in RidgePrimaryMeshFilters(root))
            {
                using(var meshes=Mesh.AcquireReadOnlyMeshData(filter.sharedMesh))using(var vertices=new NativeArray<Vector3>(meshes[0].vertexCount,Allocator.Temp))
                {
                    meshes[0].GetVertices(vertices);primaryMeshes++;primaryVertices+=vertices.Length;int stride=Mathf.Max(1,Mathf.CeilToInt(vertices.Length/192f));
                    var extrema=new int[6];var low=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);var high=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
                    for(int i=0;i<vertices.Length;i++)
                    {
                        Vector3 world=filter.transform.TransformPoint(vertices[i]);
                        if(float.IsNaN(world.x)||float.IsNaN(world.y)||float.IsNaN(world.z)||float.IsInfinity(world.x)||float.IsInfinity(world.y)||float.IsInfinity(world.z))throw new InvalidOperationException("Nonfinite actual primary-LOD vertex: "+root.name);
                        if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);
                        for(int axis=0;axis<3;axis++){if(world[axis]<low[axis]){low[axis]=world[axis];extrema[axis]=i;}if(world[axis]>high[axis]){high[axis]=world[axis];extrema[axis+3]=i;}}
                        if(i%stride==0)samples.Add(root.InverseTransformPoint(world));
                    }
                    foreach(int index in extrema.Distinct())if(index%stride!=0)samples.Add(root.InverseTransformPoint(filter.transform.TransformPoint(vertices[index])));
                }
            }
            if(first||samples.Count<8||bounds.size.y<=.001f)throw new InvalidOperationException("Actual complete primary visible mesh envelope and samples required: "+root.name);actualPrimaryBounds=bounds;return samples.ToArray();
        }
        // Query only this world's matching actual TerrainCollider: a heightfield
        // sample above a Digger hole or an unrelated prop is not ground contact.
        static RidgeTerrainContact RidgeNativeTerrainContact(DragonKingdomWorld world,Terrain[] terrains,Vector3 vertex,float height,bool rock)
        {
            var proof=new RidgeTerrainContact{meshVertex=vertex};
            foreach(var terrain in terrains)
            {
                var local=vertex-terrain.transform.position;var data=terrain.terrainData;if(data==null||local.x<0||local.z<0||local.x>data.size.x||local.z>data.size.z)continue;
                var collider=terrain.GetComponent<TerrainCollider>();proof.terrainData=AssetDatabase.GetAssetPath(data);proof.terrainDataGuid=AssetDatabase.AssetPathToGUID(proof.terrainData);proof.terrainDataHeight=terrain.SampleHeight(vertex)+terrain.transform.position.y;
                if(collider==null||!collider.enabled||collider.isTrigger||collider.terrainData!=data||!collider.transform.IsChildOf(world.transform)||proof.terrainDataGuid.Length!=32)return proof;
                proof.collider=PathOf(collider.transform,world.transform);float above=Mathf.Max(2,height+.5f);
                if(!collider.Raycast(new Ray(new Vector3(vertex.x,proof.terrainDataHeight+above,vertex.z),Vector3.down),out var hit,above+2))return proof;
                proof.surface=hit.point;proof.terrainHitDelta=Mathf.Abs(hit.point.y-proof.terrainDataHeight);proof.meshGap=vertex.y-hit.point.y;
                // Existing native terrain agreement (.03m) and existing ridge
                // contact/burial bands; neither is relaxed for this witness.
                proof.passed=hit.collider==collider&&proof.terrainHitDelta<=.03f&&proof.meshGap<=.25f&&proof.meshGap>=-height*(rock?.4f:.2f);return proof;
            }
            return proof;
        }
        static bool RidgeFinite(Vector3 value)=>!float.IsNaN(value.x)&&!float.IsNaN(value.y)&&!float.IsNaN(value.z)&&!float.IsInfinity(value.x)&&!float.IsInfinity(value.y)&&!float.IsInfinity(value.z);
        static void ValidateSavedRidgeTerrainContact(DragonKingdomWorld world,Terrain[] terrains,RidgeCompositionPart part,Vector3[] samples,Transform root,bool rock)
        {
            var contact=part.nativeContact;if(contact==null||!contact.passed||!RidgeFinite(contact.meshVertex)||!RidgeFinite(contact.surface)||!new[]{contact.terrainDataHeight,contact.terrainHitDelta,contact.meshGap}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v))||contact.terrainHitDelta<0||Vector3.Distance(contact.meshVertex,part.lowestGapPrimaryMeshVertex)>.002f||contact.terrainHitDelta>.03f||contact.meshGap>.25f||contact.meshGap< -part.modelHeight*(rock?.4f:.2f)||Mathf.Abs(contact.meshGap-(contact.meshVertex.y-contact.surface.y))>.001f||Mathf.Abs(contact.surface.x-contact.meshVertex.x)>.001f||Mathf.Abs(contact.surface.z-contact.meshVertex.z)>.001f||!samples.Any(p=>Vector3.Distance(root.TransformPoint(p),contact.meshVertex)<.002f))throw new InvalidOperationException("Saved ridge requires its actual primary-mesh/native terrain contact: "+part.name);
            var terrain=terrains.SingleOrDefault(t=>t.GetComponent<TerrainCollider>()!=null&&PathOf(t.transform,world.transform)==contact.collider);var collider=terrain==null?null:terrain.GetComponent<TerrainCollider>();
            if(terrain==null||collider==null||!collider.enabled||collider.isTrigger||collider.terrainData!=terrain.terrainData||AssetDatabase.GetAssetPath(terrain.terrainData)!=contact.terrainData||AssetDatabase.AssetPathToGUID(contact.terrainData)!=contact.terrainDataGuid||Mathf.Abs(DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,contact.meshVertex)-contact.terrainDataHeight)>.002f||Mathf.Abs(contact.terrainHitDelta-Mathf.Abs(contact.surface.y-contact.terrainDataHeight))>.001f)throw new InvalidOperationException("Saved ridge actual native ground/source identity differs: "+part.name);
        }
        // Coarse max-height/AABB seating can leave every real mesh vertex
        // airborne on a slope. Lower only a genuinely floating model until its
        // nearest actual primary vertex touches the existing heightfield.
        // Callers recheck all original bounds/peer/native-contact gates.
        static float SeatFloatingRidgePrimaryMesh(RidgeItem item,Terrain[] terrains,RidgeCompositionPart proof)
            =>SeatFloatingPrimaryMeshTranslation(item,terrains,proof,item.root);
        static float SeatFloatingAuthoredNaturalCarrier(RidgeItem item,Terrain[] terrains,RidgeCompositionPart proof)
        {
            var carrier=item.root.parent;if(carrier==null||!carrier.name.StartsWith("Authored terrain support ",StringComparison.Ordinal)||carrier.localRotation!=Quaternion.identity||carrier.localScale!=Vector3.one)throw new InvalidOperationException("Exact explicit authored support carrier required; do not move an artist's prefab-local TRS.");
            return SeatFloatingPrimaryMeshTranslation(item,terrains,proof,carrier);
        }
        static float SeatFloatingPrimaryMeshTranslation(RidgeItem item,Terrain[] terrains,RidgeCompositionPart proof,Transform mover)
        {
            proof.coarsePrimaryMeshGap=proof.minimumMeshGroundGap;proof.primaryMeshSeatCorrection=0;
            if(proof.meshSamples!=item.localSamples.Length||!float.IsFinite(proof.minimumMeshGroundGap)||proof.minimumMeshGroundGap<=.25f)return 0;
            float correction=proof.minimumMeshGroundGap;proof.primaryMeshSeatCorrection=correction;mover.position-=Vector3.up*correction;
            MeasureRidgeMesh(item,terrains,proof);proof.primaryMeshBounds=RidgeCurrentPrimaryBounds(item);return correction;
        }
        static void MeasureRidgeMesh(RidgeItem item,Terrain[] terrains,RidgeCompositionPart proof)
        {
            proof.meshSamples=proof.exposedSamples=0;proof.minimumTerrainHeight=float.PositiveInfinity;proof.minimumMeshGroundGap=float.PositiveInfinity;proof.maximumExposedHeight=float.NegativeInfinity;
            foreach(var local in item.localSamples)
            {
                Vector3 actual=item.root.TransformPoint(local);float ground;
                try{ground=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,actual);}catch(InvalidOperationException){proof.visibleAndSupported=false;return;}
                float gap=actual.y-ground;proof.minimumTerrainHeight=Mathf.Min(proof.minimumTerrainHeight,ground);proof.meshSamples++;if(gap>.02f)proof.exposedSamples++;if(gap<proof.minimumMeshGroundGap){proof.minimumMeshGroundGap=gap;proof.lowestGapPrimaryMeshVertex=actual;}proof.maximumExposedHeight=Mathf.Max(proof.maximumExposedHeight,gap);
            }
            proof.exposedFraction=proof.exposedSamples/(float)proof.meshSamples;
            proof.visibleAndSupported=proof.exposedFraction>=(item.rock?.45f:.65f)&&proof.maximumExposedHeight>=proof.modelHeight*(item.rock?.5f:.7f)&&proof.minimumMeshGroundGap<=.25f&&proof.minimumMeshGroundGap>=-proof.modelHeight*(item.rock?.4f:.2f);
        }
    }
}
#endif
