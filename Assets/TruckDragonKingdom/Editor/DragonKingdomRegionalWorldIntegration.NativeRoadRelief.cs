#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string NativeRoadReliefMode="VendorGradedRelief01";
        [Serializable]public sealed class NativeArchitectureFooting
        {
            public string id,layout,layoutSha256,instance,sourcePrefab,sourcePrefabGuid,sourcePrefabSha256,sourcePrefabMetaSha256,sourceGeometryWitness,sourceGeometryWitnessSha256;
            public Bounds physicalFootprint;public float surfaceY;
            public string purpose="EXPLICIT_MEASURED_INDIVIDUAL_GROUND_BEARING_ARCHITECTURE_NOT_WHOLE_ENSEMBLE_AABB";
        }
        [Serializable]public sealed class NativeReliefTile
        {public string terrain,vendorGradedHeightSha256,compositionReferenceHeightSha256,actualVistaOutputHeightSha256,referenceAuthority="ACTUAL_EASYROADS_OUTPUT",coordinatedHeightSha256,authorizedMaskSha256,districtRaisedMaskSha256;public int untouchedNativeSamples,roadCoreSamples,startExtractionSamples,architectureFootingSamples,localTransitionSamples,bridgeClearanceSamples,roofSamples,undergroundNativeSamples,connectedDistrictSamples;public float maximumCoreDelta,maximumTransitionDelta,maximumDistrictRaise;public bool outsideDeclaredCoordinationAreasExact,validOriginalStartExtractionContactsExact,nativeUndergroundSamplesExact;}
        [Serializable]public sealed class NativeReliefDistrict{public string region;public Bounds bounds;public float footing,transitionFalloff=.2f,outsideInfluenceMeters=120,originalCreekTransitionMeters=40;}
        [Serializable]sealed class NativeReliefDistrictSet{public NativeReliefDistrict[] districts;}
        [Serializable]public sealed class NativeRoadConstraintWitness
        {public string owner;public bool bridge;public Vector3 a,b,nearestRoadPoint;public float width,fromAlong,toAlong,spanFrom,spanTo,projection,clampedProjection,distance,physicalTarget,blendedCandidate,featherOutside;}
        [Serializable]public sealed class NativeCrossingConstraintFailure
        {public Vector3 point;public string tile,groundOwner;public float originalNativeHeight,groundCore,derivedCandidateCap;public NativeRoadConstraintWitness ground,upper;}
        [Serializable]public sealed class NativeRoadReliefEvidence
        {
            public string mode=NativeRoadReliefMode,scope="ACTUAL_EASYROADS_GRADED_VISTA_RELIEF_WITH_PHYSICAL_CORES_EXPLICIT_FOOTINGS_LOCAL_TRANSITIONS_AND_DECLARED_CONNECTED_DRY_DISTRICTS_REQUIRES_FULL_NATIVE_WORLD_AND_18_VIEW_ACCEPTANCE";
            public string connectedDryDistrictsSha256;
            public float localCoreFeatherMeters=12.5f,bridgeClearanceFeatherMeters=48,oldMountainwideLinearCollarMetersRemoved=600;
            public int physicalRoadSegments,physicalStagingPads,protectedStartsExtractions,explicitArchitectureFootings;
            public bool physicalCrossingIntervalsEnabled;public DragonKingdomNativeRoadCrossings.Crossing[] physicalCrossings;
            public NativeCrossingConstraintFailure incompatibleCrossing;
            public bool actualVendorGradeCapturedBeforeCoordination,wholeTownAndGroveBoundingBoxesNotConstraints,sourceRoadPosesAndGraphUnchanged,passed;
            public NativeFootingSamplingSupportEvidence footingSamplingSupport;
            public bool measuredCavernRoofDeclared;public Bounds measuredCavernRoof;public NativeArchitectureFooting[] footings;public NativeReliefDistrict[] connectedDryDistricts;public NativeReliefTile[] tiles;
        }
        public static NativeRoadReliefEvidence LastNativeRoadReliefEvidence{get;private set;}

        // Next-candidate alternative to CoordinateVendorFields. EasyRoads has
        // already graded these exact four candidate heightfields. Do not discard
        // its real relief and reconstruct600m planar cut/fill cones from Vista.
        // Explicit footing data is supplied by the new measured architecture
        // composer; a whole-town/grove renderer AABB is never inferred as a pad.
        // Retired road pieces keep their original objects/IDs but no longer
        // collide. They must not silently keep deforming the landscape or
        // protecting obsolete footprint masks after a road is replaced.
        static IEnumerable<Transform> ActivePhysicalStagingRoots(DragonKingdomWorld world)
            =>world.transform.Cast<Transform>().Where(t=>t.gameObject.activeInHierarchy&&(t.name.Contains("junction")||t.name.Contains("hairpin")||t.name.Contains("nest loading bay")));
        static NativeRoadReliefEvidence CoordinateNativeRoadRelief(DragonKingdomWorld world,TileState[] tiles,Bounds? undergroundTraversal,Bounds? measuredRoof,IReadOnlyList<NativeArchitectureFooting> architectureFootings)
        {
            if(world==null||tiles.Length!=4||architectureFootings==null)throw new InvalidOperationException("Four actual vendor-graded fields and an explicit architecture-footing list required.");
            foreach(var f in architectureFootings)
                if(f==null||string.IsNullOrEmpty(f.id)||string.IsNullOrEmpty(f.layout)||string.IsNullOrEmpty(f.layoutSha256)||string.IsNullOrEmpty(f.instance)||string.IsNullOrEmpty(f.sourcePrefabGuid)||string.IsNullOrEmpty(f.sourcePrefabSha256)||string.IsNullOrEmpty(f.sourceGeometryWitness)||string.IsNullOrEmpty(f.sourceGeometryWitnessSha256)||string.IsNullOrEmpty(f.sourcePrefabMetaSha256)||AssetSha(f.layout)!=f.layoutSha256||AssetSha(f.sourcePrefab)!=f.sourcePrefabSha256||AssetSha(f.sourcePrefab+".meta")!=f.sourcePrefabMetaSha256||AssetDatabase.AssetPathToGUID(f.sourcePrefab)!=f.sourcePrefabGuid||AssetSha(f.sourceGeometryWitness)!=f.sourceGeometryWitnessSha256||f.physicalFootprint.size.x<=0||f.physicalFootprint.size.z<=0||float.IsNaN(f.surfaceY)||float.IsInfinity(f.surfaceY))throw new InvalidOperationException("Every new ground-bearing architecture parcel needs actual source/layout/instance/geometry provenance and a finite measured physical footprint.");
            if(architectureFootings.Select(f=>f.id).Distinct().Count()!=architectureFootings.Count)throw new InvalidOperationException("Architecture footing IDs must be unique.");
            var graphJson=JsonUtility.ToJson(world.definition.graph);var critical=CriticalTransforms(world).Distinct().ToDictionary(t=>t,t=>Pose(t));
            var proof=new NativeRoadReliefEvidence{actualVendorGradeCapturedBeforeCoordination=true,wholeTownAndGroveBoundingBoxesNotConstraints=true,measuredCavernRoofDeclared=measuredRoof.HasValue,measuredCavernRoof=measuredRoof.GetValueOrDefault(),footings=architectureFootings.ToArray(),explicitArchitectureFootings=architectureFootings.Count};LastNativeRoadReliefEvidence=proof;
            if(NativeVistaPostGradeRequested())proof.scope="ACTUAL_EASYROADS_INPUT_THEN_EXPLICIT_AUTHORED_VISTA_OUTPUT_AS_COMPOSITION_REFERENCE_WITH_UNCHANGED_PHYSICAL_CORES_REQUIRES_FULL_WORLD_ACCEPTANCE";
            var districts=CoherentSettlementFields(world);proof.connectedDryDistricts=districts.Select(d=>new NativeReliefDistrict{region=d.region,bounds=d.bounds,footing=d.footing}).ToArray();proof.connectedDryDistrictsSha256=FinishDigest(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new NativeReliefDistrictSet{districts=proof.connectedDryDistricts})));
            var segments=NativeReliefRoadSegments(world);proof.physicalRoadSegments=segments.Count;proof.physicalCrossingIntervalsEnabled=NativeCrossingsRequested();if(proof.physicalCrossingIntervalsEnabled)proof.physicalCrossings=DragonKingdomNativeRoadCrossings.Original();
            var fixedAreas=new List<FixedField>();
            foreach(var region in world.regions)foreach(var start in region.truckStarts)
            {var b=new Bounds(start.position,Vector3.zero);foreach(float x in new[]{-2.2f,2.2f})foreach(float z in new[]{-8f,8f})b.Encapsulate(start.position+start.right*x+start.forward*z);b.Expand(6.25f);fixedAreas.Add(new FixedField{bounds=b,height=start.position.y-1,owner="start:"+region.id+":"+start.name});}
            foreach(var site in world.extractionSites.Where(s=>s.id!="boat")){var b=site.secureZone.bounds;b.Expand(6.25f);fixedAreas.Add(new FixedField{bounds=b,height=world.Region(site.id=="fortress"?"castle":"refuge").root.position.y,owner="extraction:"+site.id});}proof.protectedStartsExtractions=fixedAreas.Count;
            var pads=new List<FieldPad>();
            foreach(var t in ActivePhysicalStagingRoots(world))foreach(var mesh in t.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.GetComponent<MeshRenderer>()!=null))
            {var b=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(mesh.transform);bool elevated=segments.Any(s=>s.bridge&&t.name.StartsWith("Kingdom hairpin "+s.owner+" ",StringComparison.Ordinal)&&BridgePad(s,b));if(elevated)continue;b.Expand(new Vector3(6.25f,0,6.25f));pads.Add(new FieldPad{bounds=b,height=b.center.y,owner=t.name});}proof.physicalStagingPads=pads.Count;
            const float bin=64,coreFeather=12.5f,bridgeFeather=48;
            long Key(int x,int z)=>((long)x<<32)^(uint)z;var bins=new Dictionary<long,List<FieldSegment>>();
            foreach(var segment in segments)
            {float influence=segment.width*.5f+(segment.bridge?6+bridgeFeather:3.125f+coreFeather);int x0=Mathf.FloorToInt((Mathf.Min(segment.a.x,segment.b.x)-influence)/bin),x1=Mathf.FloorToInt((Mathf.Max(segment.a.x,segment.b.x)+influence)/bin),z0=Mathf.FloorToInt((Mathf.Min(segment.a.z,segment.b.z)-influence)/bin),z1=Mathf.FloorToInt((Mathf.Max(segment.a.z,segment.b.z)+influence)/bin);for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){long key=Key(x,z);if(!bins.TryGetValue(key,out var list))bins.Add(key,list=new List<FieldSegment>());list.Add(segment);}}
            var footingIndex=new NativeFootingIndex(architectureFootings,coreFeather+NativeFootingSamplingMaximumExpansion);var footingSampling=BuildNativeFootingSamplingSupports(architectureFootings);proof.footingSamplingSupport=footingSampling.evidence;
            var evidence=new List<NativeReliefTile>();
            foreach(var tile in tiles)
            {
                int n=tile.record.resolution;var native=tile.candidate.GetHeights(0,0,n,n);var result=(float[,])native.Clone();var authorized=new bool[n,n];var districtRaisedMask=new bool[n,n];var held=new bool[n,n];var undergroundHeld=new bool[n,n];tile.fieldTags=new byte[n,n];tile.fieldOwners=new string[n,n];
                string referenceHash=FinishFloatHash(native);var row=new NativeReliefTile{terrain=tile.record.candidate,vendorGradedHeightSha256=referenceHash,compositionReferenceHeightSha256=referenceHash};
                if(NativeVistaPostGradeRequested())
                {
                    var stage=LastNativeVistaPostGradeEvidence;if(stage==null||!stage.actualUnityReadbackVerified||!stage.allProtectedInputSamplesExact||!stage.sourceBytesExact||!stage.graphAndCriticalPosesExact)throw new InvalidOperationException("Actual explicit Vista stage must pass its independent source/core/readback proof before composition.");var field=stage.tiles.Single(t=>t.terrain==tile.record.candidate);if(field.actualVistaOutputSha256!=referenceHash)throw new InvalidOperationException("Native composition input does not match actual Vista output.");row.vendorGradedHeightSha256=field.actualSdkInputSha256;row.actualVistaOutputHeightSha256=field.actualVistaOutputSha256;row.referenceAuthority="ACTUAL_AUTHORED_VISTA_POST_EASYROADS_OUTPUT_SDK_INPUT_RETAINED_SEPARATELY";
                }
                evidence.Add(row);proof.tiles=evidence.ToArray();
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    var p=tile.terrain.transform.position+new Vector3(x*1600f/(n-1),0,z*1600f/(n-1));float original=native[z,x]*600-20,core=float.PositiveInfinity,cap=float.PositiveInfinity,weightedDelta=0,totalWeight=0,maxWeight=0;string coreOwner=null;FieldSegment coreSegment=null,capSegment=null;Vector3 coreRoadPoint=default,capRoadPoint=default;float coreProjection=0,capProjection=0,coreDistance=0,capDistance=0,capTarget=0,capOutside=0;
                    // Preserve the actual native roof over underground traversal.
                    // Its starts, floor and pavement are supported by real
                    // Digger geometry; they must not grade the roof to105m.
                    if(undergroundTraversal.HasValue&&InsideXZ(undergroundTraversal.Value,p))
                    {undergroundHeld[z,x]=true;row.undergroundNativeSamples++;tile.fieldTags[z,x]=8;tile.fieldOwners[z,x]="actual native graded underground roof";continue;}
                    if(measuredRoof.HasValue&&InsideXZ(measuredRoof.Value,p)){result[z,x]=tile.foundation[z,x];authorized[z,x]=true;row.roofSamples++;tile.record.undergroundFoundationSamples++;tile.fieldTags[z,x]=8;tile.fieldOwners[z,x]="actual measured cavern roof source field";continue;}
                    var exact=fixedAreas.FirstOrDefault(a=>DistanceTo(a.bounds,p)==0);
                    if(exact!=null)
                    {float prior=tile.before[z,x]*600-20;bool valid=Mathf.Abs(prior-exact.height)<=.3f;held[z,x]=valid;result[z,x]=valid?tile.before[z,x]:(exact.height-.08f+20)/600;authorized[z,x]=true;row.startExtractionSamples++;if(valid)tile.record.protectedSamples++;else tile.record.roadGroundingSamples++;tile.fieldTags[z,x]=(byte)(valid?4:5);tile.fieldOwners[z,x]=exact.owner;continue;}
                    void Constraint(float outside,float target,string owner,int kind,FieldSegment segment=null,Vector3 roadPoint=default,float projection=0,float distance=0)
                    {if(outside==0){if(target<core){core=target;coreOwner=owner;coreSegment=segment;coreRoadPoint=roadPoint;coreProjection=projection;coreDistance=distance;}if(kind==1)row.roadCoreSamples++;else if(kind==2)row.architectureFootingSamples++;}else if(outside<coreFeather){float w=1-Mathf.SmoothStep(0,1,outside/coreFeather);weightedDelta+=(target-original)*w;totalWeight+=w;maxWeight=Mathf.Max(maxWeight,w);}}
                    if(bins.TryGetValue(Key(Mathf.FloorToInt(p.x/bin),Mathf.FloorToInt(p.z/bin)),out var near))foreach(var s in near)
                    {
                        var flat=Vector3.ProjectOnPlane(s.b-s.a,Vector3.up);float projected=flat.sqrMagnitude>.0001f?Vector3.Dot(Vector3.ProjectOnPlane(p-s.a,Vector3.up),flat)/flat.sqrMagnitude:0,t=Mathf.Clamp01(projected);var q=Vector3.Lerp(s.a,s.b,t);float distance=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(q.x,q.z));
                        if(undergroundTraversal.HasValue&&InsideXZ(undergroundTraversal.Value,p))continue; // real Digger floor owns the underground road; do not pull its roof down to pavement.
                        if(s.bridge)
                        {if(proof.physicalCrossingIntervalsEnabled&&!DragonKingdomNativeRoadCrossings.PhysicalBridgeSpanIncludesProjection(projected,s.fromAlong,s.toAlong,s.spanFrom,s.spanTo))continue;float outside=Mathf.Max(0,distance-s.width*.5f-6);if(outside>=bridgeFeather)continue;float along=Mathf.Lerp(s.fromAlong,s.toAlong,t),endDistance=Mathf.Max(0,Mathf.Min(along-s.spanFrom,s.spanTo-along)),below=.08f+9.92f*Mathf.SmoothStep(0,1,endDistance/40),target=q.y-below;float blended=Mathf.Lerp(original,Mathf.Min(original,target),1-Mathf.SmoothStep(0,1,outside/bridgeFeather));if(blended<cap){cap=blended;capSegment=s;capRoadPoint=q;capProjection=projected;capDistance=distance;capTarget=target;capOutside=outside;}continue;}
                        float beyond=(projected<0&&s.suppressStartCap||projected>1&&s.suppressEndCap)?Mathf.Abs(projected-t)*flat.magnitude:0;float outsideCore=Mathf.Max(Mathf.Max(0,distance-s.width*.5f-3.125f),beyond);Constraint(outsideCore,q.y-.08f,s.owner,1,s,q,projected,distance);
                    }
                    foreach(var pad in pads)Constraint(DistanceTo(pad.bounds,p),pad.height-.08f,pad.owner,1);
                    foreach(var area in fixedAreas)Constraint(DistanceTo(area.bounds,p),area.height-.08f,area.owner,0);
                    foreach(var footing in footingIndex.At(p))Constraint(DistanceTo(footingSampling.For(footing),p),footing.surfaceY-.08f,"authored-footing:"+footing.id,2);
                    float height=float.IsPositiveInfinity(core)?original+(totalWeight>0?weightedDelta/totalWeight*maxWeight:0):core;
                    // These are the existing declared connected village/harbor
                    // land footprints, not a mesh/grove renderer AABB. The old
                    // broad road coordinator also supplied this necessary low
                    // dry land. Retain its exact hydrology-aware40m transition
                    // before the separately measured12m bank refinement.
                    float districtFloor=CoherentSettlementFloor(p,original,districts,out string districtOwner);
                    bool districtRaised=float.IsPositiveInfinity(core)&&height<districtFloor;
                    if(districtRaised)
                    {height=districtFloor;districtRaisedMask[z,x]=true;row.connectedDistrictSamples++;row.maximumDistrictRaise=Mathf.Max(row.maximumDistrictRaise,height-original);}
                    if(!float.IsPositiveInfinity(core)){tile.record.roadGroundingSamples++;tile.fieldTags[z,x]=1;tile.fieldOwners[z,x]=coreOwner;row.maximumCoreDelta=Mathf.Max(row.maximumCoreDelta,Mathf.Abs(height-original));}
                    else if(totalWeight>0){tile.record.transitionSamples++;row.localTransitionSamples++;tile.fieldTags[z,x]=2;tile.fieldOwners[z,x]="local physical core correction over actual EasyRoads grade";row.maximumTransitionDelta=Mathf.Max(row.maximumTransitionDelta,Mathf.Abs(height-original));}
                    if(districtRaised){tile.fieldTags[z,x]=10;tile.fieldOwners[z,x]=districtOwner;}
                    if(proof.physicalCrossingIntervalsEnabled&&!float.IsPositiveInfinity(core)&&!float.IsPositiveInfinity(cap))
                    {
                        NativeRoadConstraintWitness Witness(FieldSegment s,Vector3 q,float projected,float distance,float target,float blended,float outside)=>s==null?null:new NativeRoadConstraintWitness{owner=s.owner,bridge=s.bridge,a=s.a,b=s.b,width=s.width,fromAlong=s.fromAlong,toAlong=s.toAlong,spanFrom=s.spanFrom,spanTo=s.spanTo,nearestRoadPoint=q,projection=projected,clampedProjection=Mathf.Clamp01(projected),distance=distance,physicalTarget=target,blendedCandidate=blended,featherOutside=outside};
                        if(cap<core-.3f){proof.incompatibleCrossing=new NativeCrossingConstraintFailure{point=p,tile=tile.record.candidate,groundOwner=coreOwner,originalNativeHeight=original,groundCore=core,derivedCandidateCap=cap,ground=Witness(coreSegment,coreRoadPoint,coreProjection,coreDistance,core,core,0),upper=Witness(capSegment,capRoadPoint,capProjection,capDistance,capTarget,cap,capOutside)};throw new InvalidOperationException("Measured crossing constraint rejected: "+JsonUtility.ToJson(proof.incompatibleCrossing));}
                        height=DragonKingdomNativeRoadCrossings.RequireCompatibleGroundCore(core,cap,coreOwner,capSegment?.owner??"actual upper-road crossing");
                    }
                    else if(height>cap){height=cap;tile.record.bridgeCappedSamples++;row.bridgeClearanceSamples++;tile.fieldTags[z,x]=3;tile.fieldOwners[z,x]="bounded actual upper-road body clearance";}
                    if(!float.IsPositiveInfinity(core)&&height<core-.3f){tile.groundConflictCount++;tile.groundConflicts.Add(new GroundConflict{position=p,requiredOwner=coreOwner,cutOwner="actual local bridge clearance",requiredHeight=core,coordinatedHeight=height});}
                    result[z,x]=Mathf.Clamp01((height+20)/600);authorized[z,x]=!float.IsPositiveInfinity(core)||totalWeight>0||!float.IsPositiveInfinity(cap)||!float.IsNegativeInfinity(districtFloor);float delta=Mathf.Abs(height-(tile.foundation[z,x]*600-20));if(delta>.001f){tile.record.changedByVendor++;tile.record.maximumVendorDelta=Mathf.Max(tile.record.maximumVendorDelta,delta);tile.record.maximumTransitionChange=Mathf.Max(tile.record.maximumTransitionChange,delta);}
                }
                row.authorizedMaskSha256=NativeReliefMaskHash(authorized);row.districtRaisedMaskSha256=NativeReliefMaskHash(districtRaisedMask);
                tile.candidate.SetHeights(0,0,result);var after=tile.candidate.GetHeights(0,0,n,n);row.coordinatedHeightSha256=FinishFloatHash(after);row.outsideDeclaredCoordinationAreasExact=true;row.validOriginalStartExtractionContactsExact=true;row.nativeUndergroundSamplesExact=true;tile.record.undergroundFoundationExact=true;
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {if(after[z,x]==native[z,x])row.untouchedNativeSamples++;if(undergroundHeld[z,x]&&after[z,x]!=native[z,x])row.nativeUndergroundSamplesExact=false;if(undergroundHeld[z,x]&&after[z,x]!=tile.foundation[z,x])tile.record.undergroundFoundationExact=false;if(!authorized[z,x]&&after[z,x]!=native[z,x])row.outsideDeclaredCoordinationAreasExact=false;if(held[z,x]&&after[z,x]!=tile.before[z,x])row.validOriginalStartExtractionContactsExact=false;float oldY=tile.foundation[z,x]*600-20,newY=after[z,x]*600-20;if(oldY>=200)tile.record.foundationAbove200++;if(newY>=200)tile.record.integratedAbove200++;if(oldY>=400)tile.record.foundationAbove400++;if(newY>=400)tile.record.integratedAbove400++;tile.record.integratedMaximumY=Mathf.Max(tile.record.integratedMaximumY,newY);}
                tile.record.protectedSamplesExact=row.validOriginalStartExtractionContactsExact;proof.tiles=evidence.ToArray();
                if(!row.outsideDeclaredCoordinationAreasExact||!row.validOriginalStartExtractionContactsExact||!row.nativeUndergroundSamplesExact)throw new InvalidOperationException("Native relief altered a preserved EasyRoads sample outside its declared physical/semantic coordination areas or an original valid contact.");
            }
            proof.sourceRoadPosesAndGraphUnchanged=JsonUtility.ToJson(world.definition.graph)==graphJson&&critical.All(p=>p.Key!=null&&Pose(p.Key)==p.Value);proof.passed=proof.sourceRoadPosesAndGraphUnchanged&&evidence.Count==4&&evidence.All(r=>r.outsideDeclaredCoordinationAreasExact&&r.validOriginalStartExtractionContactsExact&&r.nativeUndergroundSamplesExact);if(!proof.passed)throw new InvalidOperationException("Native relief coordination changed an original meaningful road/start/objective pose or graph.");return proof;
        }
        static string NativeReliefMaskHash(bool[,] mask)
        {var bytes=new byte[mask.Length];int i=0;foreach(bool value in mask)bytes[i++]=value?(byte)1:(byte)0;return FinishDigest(bytes);}
        static List<FieldSegment> NativeReliefRoadSegments(DragonKingdomWorld world)
        {
            var spans=NativeCrossingsRequested()?DragonKingdomNativeRoadCrossings.TerrainSpans(DragonKingdomNativeRoadCrossings.Original()):DragonKingdomWorldAuthoring.StructuralRoadTerrainSpans();var result=new List<FieldSegment>();
            foreach(var edge in world.definition.graph.edges.Where(e=>e.edgeId!="boarding-deck"))for(int i=1;i<edge.samples.Count;i++)
            {
                var a=edge.samples[i-1];var b=edge.samples[i];float lo=a.distanceFromStartMeters,hi=b.distanceFromStartMeters;if(hi-lo<.0001f)continue;var intervals=spans.TryGetValue(edge.edgeId,out var rows)?rows:Array.Empty<Vector2>();var cuts=new SortedSet<float>{lo,hi};foreach(var s in intervals){if(s.x>lo&&s.x<hi)cuts.Add(s.x);if(s.y>lo&&s.y<hi)cuts.Add(s.y);}var points=cuts.ToArray();
                for(int j=1;j<points.Length;j++)
                {float start=points[j-1],end=points[j],middle=(start+end)*.5f;bool bridge=intervals.Any(s=>middle>s.x&&middle<s.y);float from=bridge?intervals.Where(s=>middle>s.x&&middle<s.y).Min(s=>s.x):0,to=bridge?intervals.Where(s=>middle>s.x&&middle<s.y).Max(s=>s.y):0;bool suppressStart=!bridge&&intervals.Any(s=>Mathf.Abs(s.y-start)<.01f),suppressEnd=!bridge&&intervals.Any(s=>Mathf.Abs(s.x-end)<.01f);var p=Vector3.Lerp(a.position,b.position,(start-lo)/(hi-lo));var q=Vector3.Lerp(a.position,b.position,(end-lo)/(hi-lo));var last=result.LastOrDefault();if(last!=null&&last.owner==edge.edgeId&&last.b==p&&last.bridge==bridge&&last.spanFrom==from&&last.spanTo==to&&Mathf.Abs(last.width-b.roadWidthMeters)<.001f&&Vector3.Dot((last.b-last.a).normalized,(q-p).normalized)>.999999f){last.b=q;last.toAlong=end;last.suppressEndCap=suppressEnd;}else result.Add(new FieldSegment{a=p,b=q,width=b.roadWidthMeters,bridge=bridge,fromAlong=start,toAlong=end,spanFrom=from,spanTo=to,owner=edge.edgeId,suppressStartCap=suppressStart,suppressEndCap=suppressEnd});}
            }
            return result;
        }
    }
}
#endif
