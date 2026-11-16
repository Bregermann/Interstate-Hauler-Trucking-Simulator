#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class AuthoredGroup
        {
            public string name,region,layout,layoutSha256,lastRejectedReason,inputDefinition,inputDefinitionGuid,inputDefinitionSha256,inputDefinitionMetaSha256;public int instances,trial,supportSamples,dryRejectedTrials,rangeRejectedTrials,naturalSupportRejectedTrials,roadRejectedTrials,wingRejectedTrials,overlapRejectedTrials,solidColliders,regionRejectedTrials;public Vector3 hint,position,scale;public Quaternion rotation;public Bounds bounds,regionBounds;public float supportHeightRange,minimumFreeboard;public bool passed,sourceLocalPosesExact,roadObjectiveAndWingBuffersClear,otherArtClear,naturalMode;public AuthoredGroupPart[] parts;public AuthoredGroupTrial[] trials;public AuthoredGroupModel[] models;public AuthoredParcelSeed[] refinementSeeds;
        }
        [Serializable]public sealed class AuthoredGroupTrial
        {public int trial,samples,notDrySamples,underFreeboardSamples,naturalPartIndex=-1,orientationIndex,refinementSeedTrial=-1;public Bounds bounds,naturalPartBounds;public Vector3 rootPosition,lowestPoint,highestPoint,firstRejectedPoint,refinementOffset;public float low,high,range,naturalRange,naturalAllowedRange,naturalGround,naturalMinimumMeshGap,naturalMaximumExposedHeight,naturalExposedFraction,distanceFromHint,parentYaw;public string rejectedReason,naturalPartName,naturalPartSource,phase;public AuthoredGroupFloorPoint[] floorPoints;public AuthoredClearanceWitness blocker;}
        [Serializable]public sealed class AuthoredParcelSeed
        {public int trial,orientation;public Vector3 center;}
        static readonly Vector2[] AuthoredParcelRefinementOffsets=Enumerable.Range(-8,17).SelectMany(x=>Enumerable.Range(-8,17).Select(z=>new Vector2(x*.5f,z*.5f))).Where(p=>p.sqrMagnitude>0&&p.sqrMagnitude<=16).OrderBy(p=>p.sqrMagnitude).ThenBy(p=>p.x).ThenBy(p=>p.y).ToArray();
        [Serializable]public sealed class AuthoredClearanceWitness
        {public string kind,path,road;public Vector3 point,queryExtents;public Quaternion queryRotation;public Bounds bounds;public float distance,margin;}
        [Serializable]public sealed class AuthoredGroupFloorPoint
        {public Vector3 point;public float creekBankDistance;public bool classifiedDry,freeboardPass;}
        [Serializable]public sealed class AuthoredGroupModel
        {public string name,mesh,source;public Bounds bounds;public Vector3 sourceMeshBoundsCenter,sourceMeshBoundsSize;public bool active;}
        public static AuthoredGroup[] LastAuthoredGroups{get;private set;}
        [Serializable]public sealed class AuthoredGroupPart
        {
            public string name,sceneInstanceId,source,guid,sourceSha256,sourceMetaSha256;public Vector3 sourcePosition,sourceScale;public Quaternion sourceRotation;public float supportTranslationY;public bool active,sourcePoseExact,visibleAndSupported;public Bounds bounds;public AssemblyOverrideEvidence[] overrideEvidence;public RidgeGeometryBinding[] geometry;public RidgeCompositionPart actualGrounding;
        }
        sealed class AuthoredGroupPlan{public string name,region,layout;public Vector3 hint;public float yaw,scale;public bool natural;}
        static AuthoredGroupPlan[] ActualAuthoredGroupPlans()=>new[]{
            new AuthoredGroupPlan{name="Owned authored village north garden",region="village",layout=GardenAssembly,hint=new Vector3(-595,0,-280),yaw=90,scale=1.15f},
            new AuthoredGroupPlan{name="Owned authored village south garden",region="village",layout=GardenAssembly,hint=new Vector3(-425,0,-485),yaw=270,scale=1.15f},
            new AuthoredGroupPlan{name="Owned authored harbor garden",region="harbor",layout=GardenAssembly,hint=new Vector3(655,0,-365),yaw=180,scale=1.1f},
            new AuthoredGroupPlan{name="Owned authored forest rock grove",region="forest",layout=NaturalAssembly,hint=new Vector3(-850,0,300),yaw=30,scale=1,natural=true},
            new AuthoredGroupPlan{name="Owned authored nest rock grove",region="nests",layout=NaturalAssembly,hint=new Vector3(-940,0,890),yaw=155,scale=1,natural=true}
        };
        static void ConfigureSourceAssemblyMaterials(Transform root,string folder)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int slot=0;slot<materials.Length;slot++)
                {
                    var source=materials[slot];if(source==null)throw new InvalidOperationException("Actual authored source renderer material is missing.");if(DragonKingdomWorldVisualMigration.HasSupportedUrpShader(source))continue;
                    string path=AssetDatabase.GetAssetPath(source);if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id)||guid.Length!=32)throw new InvalidOperationException("Persistent actual source material GUID/localID required.");bool foliage=path.IndexOf("Bush",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("Leaf",StringComparison.OrdinalIgnoreCase)>=0||source.GetTag("RenderType",false).IndexOf("Cutout",StringComparison.OrdinalIgnoreCase)>=0;
                    string target=folder+"/Materials/Finish/Assembly "+guid+"-"+id+" URP.mat";materials[slot]=DragonKingdomWorldVisualMigration.HasAuthoredAlbedo(source)?DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(source,target,foliage,out _):PreserveLegacySolidColor(source,target);
                }
                renderer.sharedMaterials=materials;
            }
        }
        static AuthoredGroup[] InstallActualAuthoredGroups(DragonKingdomWorld world,string folder,LandmarkFinish finish,List<Placement> records)
            =>InstallActualAuthoredGroupsWithDefinition(world,folder,finish,records,null);
        static AuthoredGroup[] InstallActualAuthoredGroupsWithDefinition(DragonKingdomWorld world,string folder,LandmarkFinish finish,List<Placement> records,OwnedDefinitionProvenance provenance)
        {
            LastAuthoredGroups=Array.Empty<AuthoredGroup>();
            var result=new List<AuthoredGroup>();foreach(var plan in ActualAuthoredGroupPlans())result.Add(InstallActualAuthoredGroupWithDefinition(world,folder,plan,finish,records,true,provenance));return result.ToArray();
        }
        static AuthoredGroup InstallActualAuthoredGroup(DragonKingdomWorld world,string folder,AuthoredGroupPlan plan,LandmarkFinish finish,List<Placement> records,bool configureMaterials=true)
            =>InstallActualAuthoredGroupWithDefinition(world,folder,plan,finish,records,configureMaterials,null);
        static AuthoredGroup InstallActualAuthoredGroupWithDefinition(DragonKingdomWorld world,string folder,AuthoredGroupPlan plan,LandmarkFinish finish,List<Placement> records,bool configureMaterials,OwnedDefinitionProvenance provenance)
        {
            var layout=ReadAssemblySource(plan.layout);var root=new GameObject(plan.name).transform;root.SetParent(world.Region(plan.region).root,false);root.position=Vector3.zero;root.rotation=Quaternion.Euler(0,plan.yaw,0);root.localScale=Vector3.one*plan.scale;var parts=new List<AuthoredGroupPart>();var roots=new List<Transform>();
            foreach(var row in layout.instances)
            {
                var support=new GameObject("Authored terrain support "+row.sceneInstanceId).transform;support.SetParent(root,false);var source=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.transform.SetParent(support,false);instance.name="Artist source "+row.sceneInstanceId+" "+source.name;instance.transform.localPosition=row.position;instance.transform.localRotation=row.rotation;instance.transform.localScale=row.scale;var changes=ApplyOriginalAssemblyOverrides(instance,row);if(configureMaterials)ConfigureSourceAssemblyMaterials(instance.transform,folder);ConfigureSourceAuthoredStaticCollision(instance.transform);roots.Add(instance.transform);finish.sources.Add(row.prefab);
                parts.Add(new AuthoredGroupPart{name=instance.name,sceneInstanceId=row.sceneInstanceId,source=row.prefab,guid=row.prefabGuid,sourceSha256=row.sourceSha256,sourceMetaSha256=row.sourceMetaSha256,sourcePosition=row.position,sourceRotation=row.rotation,sourceScale=row.scale,active=instance.activeSelf,overrideEvidence=changes});
            }
            string inputDefinition=BoundedArtInputDefinition(world,provenance);var proof=new AuthoredGroup{inputDefinition=inputDefinition,inputDefinitionGuid=AssetDatabase.AssetPathToGUID(inputDefinition),inputDefinitionSha256=AssetSha(inputDefinition),inputDefinitionMetaSha256=AssetSha(inputDefinition+".meta"),regionBounds=world.definition.FindRegion(plan.region).bounds,name=plan.name,region=plan.region,layout=plan.layout,layoutSha256=AssetSha(plan.layout),instances=layout.instances.Length,hint=plan.hint,naturalMode=plan.natural,parts=parts.ToArray()};LastAuthoredGroups=(LastAuthoredGroups??Array.Empty<AuthoredGroup>()).Concat(new[]{proof}).ToArray();var trials=new List<AuthoredGroupTrial>();var models=new List<AuthoredGroupModel>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {if(filter.sharedMesh==null||filter.GetComponent<MeshRenderer>()==null)continue;models.Add(new AuthoredGroupModel{name=PathOf(filter.transform,root),mesh=AssetDatabase.GetAssetPath(filter.sharedMesh),source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(filter.gameObject),bounds=BoundsOf(filter.transform),sourceMeshBoundsCenter=filter.sharedMesh.bounds.center,sourceMeshBoundsSize=filter.sharedMesh.bounds.size,active=filter.gameObject.activeInHierarchy});}proof.models=models.ToArray();
            var naturalItems=new RidgeItem[roots.Count];if(plan.natural)for(int i=0;i<roots.Count;i++)if(roots[i].gameObject.activeInHierarchy)
            {var child=roots[i];var item=new RidgeItem{root=child,rock=parts[i].source.IndexOf("/Cliffs/",StringComparison.Ordinal)>=0||parts[i].source.IndexOf("/Rocks/",StringComparison.Ordinal)>=0,tree=parts[i].source.IndexOf("/Trees/",StringComparison.Ordinal)>=0};RefreshAuthoredNaturalGeometry(item);naturalItems[i]=item;}
            var own=new HashSet<Collider>(root.GetComponentsInChildren<Collider>(true));proof.solidColliders=own.Count(c=>c.enabled&&!c.isTrigger);var terrains=world.GetComponentsInChildren<Terrain>(true);bool placed=false;bool villageCourt=plan.region=="village"&&!plan.natural;int baseTrials=villageCourt||plan.natural?1920:192;var parcelSeeds=new List<AuthoredParcelSeed>();proof.refinementSeeds=Array.Empty<AuthoredParcelSeed>();int measuredNaturalOrientation=0;
            for(int trial=0;trial<baseTrials+parcelSeeds.Count*AuthoredParcelRefinementOffsets.Length;trial++)
            {
                foreach(var child in roots)child.parent.localPosition=Vector3.zero;bool refinement=trial>=baseTrials;var seed=refinement?parcelSeeds[(trial-baseTrials)/AuthoredParcelRefinementOffsets.Length]:null;var offset=refinement?AuthoredParcelRefinementOffsets[(trial-baseTrials)%AuthoredParcelRefinementOffsets.Length]:Vector2.zero;int orientation=refinement?seed.orientation:trial/960,candidate=refinement?seed.trial%960:trial%960;root.rotation=Quaternion.Euler(0,plan.yaw+orientation*90,0);bool parcel=candidate>=192;float radius=parcel?Mathf.Sqrt(55*55+(128*128-55*55)*((candidate-192)/767f)):candidate==0?0:4*Mathf.Sqrt(candidate),angle=candidate*2.399963f;var hint=refinement?seed.center+new Vector3(offset.x,0,offset.y):plan.hint+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);if(refinement)radius=Vector2.Distance(new Vector2(hint.x,hint.z),new Vector2(plan.hint.x,plan.hint.z));root.position=hint;var b=BoundsOf(root);root.position+=new Vector3(hint.x-b.center.x,0,hint.z-b.center.z);bool supported=true;float low=float.PositiveInfinity,high=float.NegativeInfinity;int samples=0;var attempt=new AuthoredGroupTrial{trial=trial,orientationIndex=orientation,parentYaw=plan.yaw+orientation*90,refinementSeedTrial=refinement?seed.trial:-1,refinementOffset=new Vector3(offset.x,0,offset.y),phase=refinement?"NATIVE_FLAT_PEER_PARCEL_HALF_METRE_LOCAL_REFINEMENT":(orientation==0?"":"WHOLE_PARENT_QUARTER_TURN_")+(parcel?(plan.natural?"SAME_NATURAL_REGION_55_TO_128M_PARCEL":"SAME_VILLAGE_55_TO_128M_PARCEL"):"ORIGINAL_NEAR_HINT_192"),distanceFromHint=radius,bounds=BoundsOf(root),rootPosition=root.position};trials.Add(attempt);proof.trials=trials.ToArray();proof.trial=trial;proof.bounds=attempt.bounds;proof.position=root.position;proof.rotation=root.rotation;proof.scale=root.localScale;
                if((parcel||orientation>0||refinement)&&(!RidgeInsideRegionXZ(proof.regionBounds,attempt.bounds)||refinement&&radius>128.001f)){proof.regionRejectedTrials++;proof.lastRejectedReason=attempt.rejectedReason=plan.natural?"OUTSIDE_ACTUAL_NATURAL_GROUP_REGION":"OUTSIDE_ORIGINAL_VILLAGE_REGION";continue;}
                if(plan.natural&&orientation!=measuredNaturalOrientation)
                {
                    // Whole-parent rotation preserves each source-local pose,
                    // but its world AABB cannot reuse the original orientation.
                    foreach(var item in naturalItems.Where(i=>i!=null))RefreshAuthoredNaturalGeometry(item);measuredNaturalOrientation=orientation;
                }
                if(!plan.natural)
                {
                    b=BoundsOf(root);var floorPoints=new List<AuthoredGroupFloorPoint>();for(int z=0;z<7;z++)for(int x=0;x<7;x++){var p=new Vector3(Mathf.Lerp(b.min.x,b.max.x,x/6f),0,Mathf.Lerp(b.min.z,b.max.z,z/6f));float ground=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,p);bool dry=IntendedDrySettlementPosition(plan.region,p),freeboard=ground>=-.5f;floorPoints.Add(new AuthoredGroupFloorPoint{point=new Vector3(p.x,ground,p.z),creekBankDistance=CreekBankDistance(p),classifiedDry=dry,freeboardPass=freeboard});if(ground<low){low=ground;attempt.lowestPoint=new Vector3(p.x,ground,p.z);}if(ground>high){high=ground;attempt.highestPoint=new Vector3(p.x,ground,p.z);}bool firstRejected=attempt.notDrySamples==0&&attempt.underFreeboardSamples==0;if(!dry)attempt.notDrySamples++;if(!freeboard)attempt.underFreeboardSamples++;if((!dry||!freeboard)&&firstRejected)attempt.firstRejectedPoint=new Vector3(p.x,ground,p.z);supported&=dry&&freeboard;samples++;}
                    attempt.floorPoints=floorPoints.ToArray();attempt.samples=proof.supportSamples=samples;attempt.low=low;attempt.high=high;attempt.range=proof.supportHeightRange=high-low;proof.minimumFreeboard=low+2;
                    if(!supported||high-low>.6f){if(!supported)proof.dryRejectedTrials++;if(high-low>.6f)proof.rangeRejectedTrials++;proof.lastRejectedReason=attempt.rejectedReason=!supported?"ACTUAL_CLASSIFIED_DRY_FOOTPRINT_OR_FREEBOARD":"ACTUAL_49_POINT_TERRAIN_RANGE";continue;}
                    // Source centerY15 is the artist's terrain plane. Intentional
                    // below-plane house foundations must remain embedded; using
                    // whole-group minY would float the grass2.35m above ground.
                    root.position=new Vector3(root.position.x,high,root.position.z);
                }
                else
                {
                    for(int i=0;i<roots.Count;i++)
                    {
                        var child=roots[i];if(!child.gameObject.activeInHierarchy){parts[i].visibleAndSupported=true;continue;}var item=naturalItems[i];var cb=BoundsOf(child);var primary=RidgeCurrentPrimaryBounds(item);bool rock=item.rock,tree=item.tree;float height=primary.size.y,half=Mathf.Clamp(Mathf.Min(primary.size.x,primary.size.z)*.25f,.2f,rock?7:tree?1.5f:1),range,ground;
                        float allowed=rock?height*.28f:tree?Mathf.Max(.4f,height*.12f):Mathf.Max(.2f,height*.35f);attempt.naturalPartIndex=i;attempt.naturalPartName=parts[i].name;attempt.naturalPartSource=parts[i].source;attempt.naturalPartBounds=primary;attempt.naturalAllowedRange=allowed;bool found=TryGround(world,new Vector3(primary.center.x,0,primary.center.z),half,out ground,out range);attempt.naturalRange=range;attempt.naturalGround=ground;if(!found||range>allowed){supported=false;break;}
                        float embed=rock?Mathf.Min(height*.1f,Mathf.Max(.12f,range*.45f)):.035f;child.parent.localPosition=Vector3.up*((ground-embed-primary.min.y)/root.lossyScale.y);var actual=new RidgeCompositionPart{modelHeight=height,paddedModelHeight=cb.size.y,primaryMeshes=item.primaryMeshes,primaryVertices=item.primaryVertices,primaryMeshBounds=RidgeCurrentPrimaryBounds(item),originalPrimaryMeshBounds=item.primaryBounds,supportHeightRange=range};MeasureRidgeMesh(item,terrains,actual);SeatFloatingAuthoredNaturalCarrier(item,terrains,actual);parts[i].actualGrounding=actual;attempt.naturalMinimumMeshGap=actual.minimumMeshGroundGap;attempt.naturalMaximumExposedHeight=actual.maximumExposedHeight;attempt.naturalExposedFraction=actual.exposedFraction;parts[i].visibleAndSupported=actual.visibleAndSupported;if(!actual.visibleAndSupported){supported=false;break;}samples+=actual.meshSamples;
                    }
                    if(!supported){proof.naturalSupportRejectedTrials++;proof.lastRejectedReason=attempt.rejectedReason="ACTUAL_SOURCE_NATURAL_PART_SUPPORT_OR_MESH_EXPOSURE";continue;}
                }
                Physics.SyncTransforms();b=BoundsOf(root);proof.trial=trial;proof.bounds=b;proof.supportSamples=samples;proof.supportHeightRange=plan.natural?0:high-low;proof.minimumFreeboard=plan.natural?0:low+2;
                if(!FinishBoundsClearOfRoads(world,b,w=>attempt.blocker=w)||!ClearRoadEnvelope(world,root,false,w=>attempt.blocker=w)){proof.roadRejectedTrials++;proof.lastRejectedReason=attempt.rejectedReason="ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE";continue;}if(!RidgeWingClear(world,b)){proof.wingRejectedTrials++;proof.lastRejectedReason=attempt.rejectedReason="ACTUAL_WING_CLEARANCE";continue;}
                var peer=Physics.OverlapBox(b.center,b.extents*.8f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).FirstOrDefault(c=>!own.Contains(c)&&!(c is TerrainCollider)&&!c.name.StartsWith("Chunk",StringComparison.Ordinal)&&c.transform.IsChildOf(world.transform));if(peer!=null){attempt.blocker=new AuthoredClearanceWitness{kind="NATIVE_OTHER_ART_COLLIDER",path=PathOf(peer.transform,world.transform),bounds=peer.bounds,point=b.center,queryExtents=b.extents*.8f,queryRotation=Quaternion.identity};proof.overlapRejectedTrials++;proof.lastRejectedReason=attempt.rejectedReason="ACTUAL_OTHER_ART_COLLIDER_OVERLAP";if(villageCourt&&!refinement&&parcelSeeds.Count<16){parcelSeeds.Add(new AuthoredParcelSeed{trial=trial,orientation=orientation,center=new Vector3(b.center.x,0,b.center.z)});proof.refinementSeeds=parcelSeeds.ToArray();}continue;}
                proof.lastRejectedReason="";proof.roadObjectiveAndWingBuffersClear=true;proof.otherArtClear=true;placed=true;break;
            }
            if(!placed)throw new InvalidOperationException("Exact source-authored group lacks a supported road/wing-clear bounded placement; no omitted children or distant substitute: "+plan.name+"; reason="+proof.lastRejectedReason+"; trials="+trials.Count+"; dry="+proof.dryRejectedTrials+", range="+proof.rangeRejectedTrials+", natural="+proof.naturalSupportRejectedTrials+", road="+proof.roadRejectedTrials+", wing="+proof.wingRejectedTrials+", peer="+proof.overlapRejectedTrials+", region="+proof.regionRejectedTrials+". Full source/model/candidate evidence remains in LastAuthoredGroups and the durable native receipt.");
            var hashes=new Dictionary<string,string>();for(int i=0;i<roots.Count;i++){var child=roots[i];var part=parts[i];part.bounds=BoundsOf(child);part.supportTranslationY=child.parent.localPosition.y;part.sourcePoseExact=child.localPosition==part.sourcePosition&&Quaternion.Angle(child.localRotation,part.sourceRotation)<.0001f&&child.localScale==part.sourceScale;if(!part.sourcePoseExact)throw new InvalidOperationException("Original authored root TRS changed.");part.geometry=RidgeGeometryBindings(child,hashes);if(!plan.natural)part.visibleAndSupported=true;}
            proof.position=root.position;proof.rotation=root.rotation;proof.scale=root.localScale;proof.sourceLocalPosesExact=parts.All(p=>p.sourcePoseExact);proof.passed=proof.sourceLocalPosesExact&&proof.roadObjectiveAndWingBuffersClear&&proof.otherArtClear;records.Add(new Placement{region=plan.region,name=root.name,source=plan.layout,operation=plan.natural?"Preserve exact authored horizontal rock/vegetation assembly and prefab TRS inside explicit native-ground support carriers":"Preserve exact source-author garden court including intentional below-plane foundations and stacked props",before=plan.hint,after=root.position,bounds=proof.bounds,trial=proof.trial});return proof;
        }
        static void RefreshAuthoredNaturalGeometry(RidgeItem item)
        {item.localSamples=RidgeMeshSamples(item.root,out item.primaryBounds,out item.primaryMeshes,out item.primaryVertices);item.primaryRootPosition=item.root.position;}
        static void ValidateAuthoredParcelSearch(DragonKingdomWorld world,AuthoredGroupPlan plan,AuthoredGroup proof,Transform root,Bounds originalRegion)
        {
            bool village=plan.region=="village"&&!plan.natural;int baseTrials=village||plan.natural?1920:192;var seeds=proof.refinementSeeds??Array.Empty<AuthoredParcelSeed>();
            if(seeds.Length>16||!village&&seeds.Length!=0||proof.trial<0||proof.trial>=baseTrials+seeds.Length*AuthoredParcelRefinementOffsets.Length||proof.trials==null||proof.trials.Length!=proof.trial+1||proof.trials.Where((t,i)=>t.trial!=i).Any()||Vector3.Distance(originalRegion.center,proof.regionBounds.center)>.001f||Vector3.Distance(originalRegion.size,proof.regionBounds.size)>.001f)throw new InvalidOperationException("Saved source-group bounded original-region search differs.");
            var colliders=world.GetComponentsInChildren<Collider>(true);
            foreach(var seed in seeds)
            {
                if(seed.trial<0||seed.trial>=baseTrials||seed.trial>=proof.trials.Length||seed.orientation!=seed.trial/960||seeds.Count(p=>p.trial==seed.trial)!=1)throw new InvalidOperationException("Saved local parcel seed is not one unique original candidate.");
                var witness=proof.trials[seed.trial];if(witness.rejectedReason!="ACTUAL_OTHER_ART_COLLIDER_OVERLAP"||witness.samples!=49||witness.notDrySamples!=0||witness.underFreeboardSamples!=0||witness.range>.6f||witness.floorPoints==null||witness.floorPoints.Length!=49||witness.floorPoints.Any(p=>!p.classifiedDry||!p.freeboardPass)||witness.blocker==null||witness.blocker.kind!="NATIVE_OTHER_ART_COLLIDER"||string.IsNullOrEmpty(witness.blocker.path)||!colliders.Any(c=>PathOf(c.transform,world.transform)==witness.blocker.path)||Vector2.Distance(new Vector2(seed.center.x,seed.center.z),new Vector2(witness.bounds.center.x,witness.bounds.center.z))>.002f)throw new InvalidOperationException("Saved local parcel seed lacks its exact earlier dry/flat/road/wing-safe native peer witness.");
            }
            bool refinement=proof.trial>=baseTrials;int orientation,candidate;Vector3 expected;string phase;var last=proof.trials.Last();
            if(refinement)
            {
                int index=proof.trial-baseTrials;var seed=seeds[index/AuthoredParcelRefinementOffsets.Length];var offset=AuthoredParcelRefinementOffsets[index%AuthoredParcelRefinementOffsets.Length];orientation=seed.orientation;candidate=seed.trial%960;expected=seed.center+new Vector3(offset.x,0,offset.y);phase="NATIVE_FLAT_PEER_PARCEL_HALF_METRE_LOCAL_REFINEMENT";
                if(last.refinementSeedTrial!=seed.trial||!last.refinementOffset.Equals(new Vector3(offset.x,0,offset.y))||offset.sqrMagnitude>16||Vector2.Distance(new Vector2(expected.x,expected.z),new Vector2(plan.hint.x,plan.hint.z))>128.002f)throw new InvalidOperationException("Saved local parcel exceeded its exact half-metre/four-metre/same128m source bounds.");
            }
            else
            {
                orientation=proof.trial/960;candidate=proof.trial%960;bool outer=candidate>=192;float radius=outer?Mathf.Sqrt(55*55+(128*128-55*55)*((candidate-192)/767f)):candidate==0?0:4*Mathf.Sqrt(candidate);float angle=candidate*2.399963f;expected=plan.hint+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);phase=(orientation==0?"":"WHOLE_PARENT_QUARTER_TURN_")+(outer?(plan.natural?"SAME_NATURAL_REGION_55_TO_128M_PARCEL":"SAME_VILLAGE_55_TO_128M_PARCEL"):"ORIGINAL_NEAR_HINT_192");
                if(last.refinementSeedTrial!=-1||!last.refinementOffset.Equals(Vector3.zero))throw new InvalidOperationException("Original source-group candidate claimed unrelated local refinement.");
            }
            var bounds=BoundsOf(root);float distance=Vector2.Distance(new Vector2(bounds.center.x,bounds.center.z),new Vector2(plan.hint.x,plan.hint.z));
            if(Vector2.Distance(new Vector2(bounds.center.x,bounds.center.z),new Vector2(expected.x,expected.z))>.01f||last.phase!=phase||last.orientationIndex!=orientation||Mathf.Abs(last.parentYaw-(plan.yaw+orientation*90))>.001f||Mathf.Abs(last.distanceFromHint-distance)>.01f||Quaternion.Angle(root.rotation,Quaternion.Euler(0,plan.yaw+orientation*90,0))>.0001f||(refinement||candidate>=192||orientation>0)&&!RidgeInsideRegionXZ(originalRegion,bounds))throw new InvalidOperationException("Saved source-group exact source-parent orientation/native parcel differs.");
        }
        [Serializable]sealed class SavedSourcePartMismatch
        {public string name,group,source,actualName;public string[] fields;public bool actualActive,expectedActive;public Vector3 actualPosition,expectedPosition,actualScale,expectedScale;public Quaternion actualRotation,expectedRotation;public float rotationAngle;public RidgeGeometryBinding[] actualGeometry,expectedGeometry;}
        public static void ValidateSavedActualAuthoredGroups(DragonKingdomWorld world,LandmarkFinish finish)
        {
            var plans=ActualAuthoredGroupPlans();if(finish.compositionSchema!=3||finish.authoredGroups==null||finish.authoredGroups.Length!=plans.Length||finish.assemblyMaterials==null||!finish.assemblyMaterials.passed||finish.assemblyMaterials.provenanceSha256!=AssemblyImportProvenanceSha)throw new InvalidOperationException("Saved schema3 requires all five exact authored groups and approved51 material provenance.");
            var materialProof=ReadAssemblyMaterialProvenance();foreach(var binding in materialProof.bindings)ValidateAssemblyMaterialBinding(materialProof,binding);var hashes=new Dictionary<string,string>();int total=0;
            foreach(var plan in plans)
            {
                var proof=finish.authoredGroups.Single(p=>p.name==plan.name);var layout=ReadAssemblySource(plan.layout);var root=world.Region(plan.region).root.Cast<Transform>().Single(t=>t.name==plan.name);
                if(!RidgeDefinitionPath(proof.inputDefinition)||AssetSha(proof.inputDefinition)!=proof.inputDefinitionSha256||AssetSha(proof.inputDefinition+".meta")!=proof.inputDefinitionMetaSha256||AssetDatabase.AssetPathToGUID(proof.inputDefinition)!=proof.inputDefinitionGuid||proof.naturalMode!=plan.natural||proof.solidColliders<=0||proof.solidColliders!=root.GetComponentsInChildren<Collider>(true).Count(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Saved source-group original definition/mode/collision provenance differs.");
                var input=AssetDatabase.LoadAssetAtPath<DragonKingdomDefinition>(proof.inputDefinition);ValidateAuthoredParcelSearch(world,plan,proof,root,input.FindRegion(plan.region).bounds);
                if(!proof.passed||!proof.sourceLocalPosesExact||!proof.roadObjectiveAndWingBuffersClear||!proof.otherArtClear||proof.region!=plan.region||proof.layout!=plan.layout||proof.layoutSha256!=AssetSha(plan.layout)||proof.instances!=layout.instances.Length||proof.parts.Length!=proof.instances||root.childCount!=proof.instances||root.position!=proof.position||Quaternion.Angle(root.rotation,proof.rotation)>.0001f||root.localScale!=proof.scale||!FinishBoundsClearOfRoads(world,proof.bounds)||!RidgeWingClear(world,proof.bounds))throw new InvalidOperationException("Saved actual authored group differs: "+plan.name);
                for(int i=0;i<layout.instances.Length;i++)
                {
                    var row=layout.instances[i];var part=proof.parts[i];var support=root.GetChild(i);if(support.name!="Authored terrain support "+row.sceneInstanceId||support.childCount!=1||support.localPosition!=Vector3.up*part.supportTranslationY||support.localRotation!=Quaternion.identity||support.localScale!=Vector3.one)throw new InvalidOperationException("Saved explicit support carrier differs from measured native source support.");var child=support.GetChild(0);
                    var savedGeometry=RidgeGeometryBindings(child,hashes);string actualGeometry=JsonUtility.ToJson(new RidgeGeometryContainer{bindings=savedGeometry}),expectedGeometry=JsonUtility.ToJson(new RidgeGeometryContainer{bindings=part.geometry});var mismatch=new List<string>();
                    if(part.sceneInstanceId!=row.sceneInstanceId)mismatch.Add("sceneInstanceId");if(part.source!=row.prefab)mismatch.Add("sourcePath");if(part.guid!=row.prefabGuid)mismatch.Add("sourceGuid");if(part.sourceSha256!=row.sourceSha256)mismatch.Add("recordedSourceSha");if(part.sourceMetaSha256!=row.sourceMetaSha256)mismatch.Add("recordedSourceMetaSha");if(AssetSha(part.source)!=part.sourceSha256)mismatch.Add("actualSourceSha");if(AssetSha(part.source+".meta")!=part.sourceMetaSha256)mismatch.Add("actualSourceMetaSha");if(child.name!=part.name)mismatch.Add("name");if(child.gameObject.activeSelf!=part.active)mismatch.Add("activeSelf");if(child.localPosition!=row.position)mismatch.Add("localPosition");if(Quaternion.Angle(child.localRotation,row.rotation)>.0001f)mismatch.Add("localRotationAngle");if(child.localScale!=row.scale)mismatch.Add("localScale");if(part.overrideEvidence.Length!=row.overrides.Length)mismatch.Add("overrideCount");if(part.overrideEvidence.Any(o=>o.applied&&!o.appliedValueVerified))mismatch.Add("appliedOverrideVerification");if(!part.visibleAndSupported)mismatch.Add("recordedVisibilitySupport");if(actualGeometry!=expectedGeometry)mismatch.Add("geometryJson");
                    if(mismatch.Count>0)
                    {
                        var detail=new SavedSourcePartMismatch{name=part.name,group=proof.name,source=part.source,fields=mismatch.ToArray(),actualName=child.name,actualActive=child.gameObject.activeSelf,expectedActive=part.active,actualPosition=child.localPosition,expectedPosition=row.position,actualRotation=child.localRotation,expectedRotation=row.rotation,rotationAngle=Quaternion.Angle(child.localRotation,row.rotation),actualScale=child.localScale,expectedScale=row.scale,actualGeometry=savedGeometry,expectedGeometry=part.geometry};
                        string path=System.IO.Path.Combine(Output(),"saved-authored-part-first-mismatch.json");System.IO.Directory.CreateDirectory(Output());System.IO.File.WriteAllText(path,JsonUtility.ToJson(detail,true));throw new InvalidOperationException("Saved actual authored prefab proof differs: "+part.name+" fields="+string.Join(",",mismatch)+"; exact current/source/geometry witness: "+path);
                    }
                    if(plan.natural&&RidgeGeometryObjectActive(child))
                    {var samples=RidgeMeshSamples(child,out var primary,out int meshes,out int vertices);var actual=new RidgeCompositionPart{modelHeight=primary.size.y,supportHeightRange=part.actualGrounding.supportHeightRange};bool rock=part.source.IndexOf("/Cliffs/",StringComparison.Ordinal)>=0||part.source.IndexOf("/Rocks/",StringComparison.Ordinal)>=0,tree=part.source.IndexOf("/Trees/",StringComparison.Ordinal)>=0;MeasureRidgeMesh(new RidgeItem{root=child,localSamples=samples,rock=rock,tree=tree},world.GetComponentsInChildren<Terrain>(true),actual);if(!actual.visibleAndSupported||actual.meshSamples!=part.actualGrounding.meshSamples||meshes!=part.actualGrounding.primaryMeshes||vertices!=part.actualGrounding.primaryVertices||Mathf.Abs(primary.size.y-part.actualGrounding.modelHeight)>.002f||Vector3.Distance(primary.center,part.actualGrounding.primaryMeshBounds.center)>.002f||Vector3.Distance(primary.size,part.actualGrounding.primaryMeshBounds.size)>.002f)throw new InvalidOperationException("Saved actual source grove lost complete primary-mesh terrain exposure/support.");}
                    total++;
                }
                if(!plan.natural){var b=BoundsOf(root);float low=float.PositiveInfinity,high=float.NegativeInfinity;for(int z=0;z<7;z++)for(int x=0;x<7;x++){var p=new Vector3(Mathf.Lerp(b.min.x,b.max.x,x/6f),0,Mathf.Lerp(b.min.z,b.max.z,z/6f));float ground=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,p);low=Mathf.Min(low,ground);high=Mathf.Max(high,ground);if(!IntendedDrySettlementPosition(plan.region,p)||ground<-.5f)throw new InvalidOperationException("Saved authored court lacks dry connected ground.");}if(high-low>.6f||Mathf.Abs(high-root.position.y)>.0001f)throw new InvalidOperationException("Saved authored court terrain plane/support differs.");}
            }
            if(total!=335)throw new InvalidOperationException("Saved authored source groups must retain all335 original instances.");
        }
    }
}
#endif
