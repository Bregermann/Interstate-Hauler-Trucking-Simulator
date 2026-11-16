#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class TerrainQuality
        {
            public string scope="ACTUAL_INTEGRATED_HEIGHTFIELD_VISIBLE_SLOPES_EXCLUDING_DECLARED_WATER_AND_UNDERGROUND_PORTALS_NOT_RAW_FOUNDATION_ONLY";
            public int rawSteep65,integratedSteep65,integratedSteep80,excludedHydrologyOrPortalCells;
            public int allowedSteep65,allowedSteep80=500;public float maximumSlopeDegrees,maximumTileSeamDelta;public bool passed;
            public SteepCell[] worst;public SteepBin[] bins;public ConstraintCount[] ownership;
            public int conflictingGroundCoreSamples;public GroundConflict[] groundConflicts;
        }
        [Serializable]public sealed class GroundConflict{public Vector3 position;public string requiredOwner,cutOwner;public float requiredHeight,coordinatedHeight;}
        [Serializable]public sealed class SteepCell{public Vector3 position;public float slope,height,neighborHeight,rawHeight;public string owner,neighborOwner;public int tag,neighborTag;}
        [Serializable]public sealed class SteepBin{public Vector2 origin;public int count;}
        [Serializable]public sealed class ConstraintCount{public string owner;public int count;}
        sealed class FieldSegment{public Vector3 a,b;public float width,fromAlong,toAlong,spanFrom,spanTo;public bool bridge,suppressStartCap,suppressEndCap;public string owner;}
        sealed class FieldPad{public Bounds bounds;public float height;public string owner;}
        sealed class FixedField{public Bounds bounds;public float height;public string owner;}
        static Bounds MeasuredChamberRoofCore=>new Bounds(new Vector3(520,0,780),new Vector3(204.2f,0,152.2f));
        static readonly Vector3[] FieldCreek=DragonKingdomWorldAuthoring.CanyonWaterPolyline();
        static readonly Vector3[] FieldLava={new Vector3(1370,330,1050),new Vector3(1450,260,900),new Vector3(1410,160,600),new Vector3(1300,90,430)};
        static void CoordinateVendorFields(DragonKingdomWorld world,TileState[] tiles,Bounds? underground)
        {
            var spans=DragonKingdomWorldAuthoring.StructuralRoadTerrainSpans();var segments=new List<FieldSegment>();var settlementFields=CoherentSettlementFields(world);
            foreach(var edge in world.definition.graph.edges)
            {
                // This graph link is the real moving hull deck, never ground.
                // Grading it raised the lake bed to0 beneath the vessel.
                if(edge.edgeId=="boarding-deck")continue;
                for(int i=1;i<edge.samples.Count;i++)
                {
                    var a=edge.samples[i-1];var b=edge.samples[i];float lo=a.distanceFromStartMeters,hi=b.distanceFromStartMeters;if(hi-lo<.0001f)continue;
                    var intervals=spans.TryGetValue(edge.edgeId,out var values)?values:Array.Empty<Vector2>();var cuts=new SortedSet<float>{lo,hi};
                    foreach(var span in intervals){if(span.x>lo&&span.x<hi)cuts.Add(span.x);if(span.y>lo&&span.y<hi)cuts.Add(span.y);}
                    var points=cuts.ToArray();for(int j=1;j<points.Length;j++)
                    {
                        float start=points[j-1],end=points[j],middle=(start+end)*.5f;bool bridge=intervals.Any(s=>middle>s.x&&middle<s.y);
                        float spanFrom=bridge?intervals.Where(s=>middle>s.x&&middle<s.y).Min(s=>s.x):0,spanTo=bridge?intervals.Where(s=>middle>s.x&&middle<s.y).Max(s=>s.y):0;
                        bool suppressStart=!bridge&&intervals.Any(s=>Mathf.Abs(s.y-start)<.01f),suppressEnd=!bridge&&intervals.Any(s=>Mathf.Abs(s.x-end)<.01f);
                        var p=Vector3.Lerp(a.position,b.position,(start-lo)/(hi-lo));var q=Vector3.Lerp(a.position,b.position,(end-lo)/(hi-lo));var last=segments.LastOrDefault();
                        if(last!=null&&last.owner==edge.edgeId&&last.b==p&&last.bridge==bridge&&last.spanFrom==spanFrom&&last.spanTo==spanTo&&Mathf.Abs(last.width-b.roadWidthMeters)<.001f&&Vector3.Dot((last.b-last.a).normalized,(q-p).normalized)>.999999f){last.b=q;last.toAlong=end;last.suppressEndCap=suppressEnd;}
                        else segments.Add(new FieldSegment{a=p,b=q,width=b.roadWidthMeters,bridge=bridge,fromAlong=start,toAlong=end,spanFrom=spanFrom,spanTo=spanTo,owner=edge.edgeId,suppressStartCap=suppressStart,suppressEndCap=suppressEnd});
                    }
                }
            }
            var fixedAreas=new List<FixedField>();foreach(var region in world.regions)foreach(var start in region.truckStarts)
            {var b=new Bounds(start.position,Vector3.zero);foreach(float x in new[]{-2.2f,2.2f})foreach(float z in new[]{-8f,8f})b.Encapsulate(start.position+start.right*x+start.forward*z);b.Expand(6.25f);fixedAreas.Add(new FixedField{bounds=b,height=start.position.y-1,owner="start:"+region.id+":"+start.name});}
            foreach(var site in world.extractionSites)
            {if(site.id=="boat")continue;var b=site.secureZone.bounds;b.Expand(6.25f);fixedAreas.Add(new FixedField{bounds=b,height=world.Region(site.id=="fortress"?"castle":"refuge").root.position.y,owner="extraction:"+site.id});}
            var pads=new List<FieldPad>();
            foreach(Transform t in world.transform)
                if(t.name.Contains("junction")||t.name.Contains("hairpin")||t.name.Contains("nest loading bay"))
                    foreach(var filter in t.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.GetComponent<MeshRenderer>()!=null))
                    {
                        var b=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(filter.transform);
                        // The real upper viaduct/turning deck remains unchanged.
                        // Its pavement must not secretly reintroduce a ground
                        // plateau across the lower road's open arch.
                        bool elevated=segments.Any(segment=>segment.bridge&&t.name.StartsWith("Kingdom hairpin "+segment.owner+" ",StringComparison.Ordinal)&&BridgePad(segment,b));
                        if(elevated)continue;
                        b.Expand(new Vector3(6.25f,0,6.25f));pads.Add(new FieldPad{bounds=b,height=b.center.y,owner=t.name});
                    }
            foreach(var region in world.regions.Where(r=>r.id=="village"||r.id=="harbor"||r.id=="castle"||r.id=="refuge"))foreach(Transform t in region.root)
                if((t.name.StartsWith("P_BLD_",StringComparison.Ordinal)&&!t.name.Contains("windmill_sail"))||t.name.Contains("sanctuary"))
                {var b=BoundsOf(t);b.Expand(new Vector3(4,0,4));pads.Add(new FieldPad{bounds=b,height=t.name.Contains("sanctuary")?region.root.position.y:BoundsOf(t).min.y,owner=region.id+":"+t.name});}
            const float bin=64,influence=600;var bins=new Dictionary<long,List<FieldSegment>>();
            foreach(var segment in segments)for(int z=Mathf.FloorToInt((Mathf.Min(segment.a.z,segment.b.z)-influence-25)/bin);z<=Mathf.FloorToInt((Mathf.Max(segment.a.z,segment.b.z)+influence+25)/bin);z++)
                for(int x=Mathf.FloorToInt((Mathf.Min(segment.a.x,segment.b.x)-influence-25)/bin);x<=Mathf.FloorToInt((Mathf.Max(segment.a.x,segment.b.x)+influence+25)/bin);x++)
                {long key=Key(x,z);if(!bins.TryGetValue(key,out var list))bins.Add(key,list=new List<FieldSegment>());list.Add(segment);}
            foreach(var tile in tiles)
            {
                int n=tile.record.resolution;var heights=tile.candidate.GetHeights(0,0,n,n);var held=new bool[n,n];tile.fieldTags=new byte[n,n];tile.fieldOwners=new string[n,n];
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    Vector3 p=tile.terrain.transform.position+new Vector3(x*1600f/(n-1),0,z*1600f/(n-1));float foundation=tile.foundation[z,x]*600-20;
                    if(underground.HasValue&&InsideXZ(MeasuredChamberRoofCore,p)){heights[z,x]=tile.foundation[z,x];tile.record.undergroundFoundationSamples++;tile.fieldTags[z,x]=8;tile.fieldOwners[z,x]="measured chamber roof";continue;}
                    var exact=fixedAreas.FirstOrDefault(b=>DistanceTo(b.bounds,p)==0);
                    if(exact!=null)
                    {
                        // A trigger volume/anchor does not require preserving
                        // the old-20m void BELOW its real unchanged road deck.
                        // Keep valid original contact samples verbatim; correct
                        // unsupported subsurface to just below actual footing.
                        float old=tile.before[z,x]*600-20;bool valid=Mathf.Abs(old-exact.height)<=.3f;
                        held[z,x]=valid;heights[z,x]=valid?tile.before[z,x]:(exact.height-.08f+20)/600;
                        if(valid)tile.record.protectedSamples++;else tile.record.roadGroundingSamples++;tile.fieldTags[z,x]=(byte)(valid?4:5);tile.fieldOwners[z,x]=exact.owner;continue;
                    }
                    // Blend ALL physical ground constraints continuously into
                    // the real rounded Vista field. Selecting one nearest old
                    // height produced V154's 227m/3m Voronoi curtains. Elevated
                    // bridge spans never impose the old-20m ground strip.
                    // Continuous cut/fill envelopes coordinate the vendor field.
                    // Every ground constraint contributes its whole collar;
                    // min/max of these bounded-gradient fields stays continuous.
                    // Inverse-square competition produced 30m steps between
                    // neighboring high/low roads even outside both pavements.
                    const float collarGrade=1f;
                    float protectedFill=float.NegativeInfinity,protectedCut=float.PositiveInfinity,fill=float.NegativeInfinity,cut=float.PositiveInfinity,core=float.PositiveInfinity,nearest=float.PositiveInfinity,cap=float.PositiveInfinity;string owner="Vista",coreOwner=null,cutOwner=null;
                    if(bins.TryGetValue(Key(Mathf.FloorToInt(p.x/bin),Mathf.FloorToInt(p.z/bin)),out var nearby))foreach(var segment in nearby)
                    {
                        var a=segment.a;var direction=segment.b-a;var flat=Vector3.ProjectOnPlane(direction,Vector3.up);var delta=Vector3.ProjectOnPlane(p-a,Vector3.up);
                        float projected=flat.sqrMagnitude>.0001f?Vector3.Dot(delta,flat)/flat.sqrMagnitude:0;float t=Mathf.Clamp01(projected);var q=Vector3.Lerp(a,segment.b,t);
                        float distance=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(q.x,q.z)),outside=Mathf.Max(0,distance-segment.width*.5f-3.125f);
                        if(segment.bridge)
                        {
                            float along=Mathf.Lerp(segment.fromAlong,segment.toAlong,t),endDistance=Mathf.Max(0,Mathf.Min(along-segment.spanFrom,segment.spanTo-along));
                            float below=.08f+9.92f*Mathf.SmoothStep(0,1,endDistance/40);
                            if(distance<influence)cap=Mathf.Min(cap,q.y-below+Mathf.Max(0,distance-segment.width*.5f-6));continue;
                        }
                        // A ground segment's rounded end cap must not
                        // overwrite the adjoining graded BRIDGE. V176's25
                        // contacts were raised by the next segment's constant
                        // endpoint height up to13m back inside the bridge.
                        // Keep true lateral road width, but feather longitudinal
                        // endpoint influence only at actual bridge boundaries.
                        // Ordinary ground corners retain the real road miter
                        // wedge; V177 found3remaining contacts in that wedge.
                        float beyond=(projected<0&&segment.suppressStartCap||projected>1&&segment.suppressEndCap)?Mathf.Abs(projected-t)*flat.magnitude:0;outside=Mathf.Max(outside,beyond);
                        nearest=Mathf.Min(nearest,outside);float target=q.y-.08f;
                        if(outside>=influence)continue;
                        if(outside==0&&target<core){core=target;coreOwner=segment.owner;}
                        float ceiling=target+outside*collarGrade,floor=target-outside*collarGrade;
                        if(ceiling<cut){cut=ceiling;cutOwner=segment.owner;}if(floor>fill)fill=floor;
                        protectedCut=Mathf.Min(protectedCut,target+outside*1.8f);protectedFill=Mathf.Max(protectedFill,target-outside*1.8f);
                    }
                    foreach(var pad in pads)
                    {
                        float outside=DistanceTo(pad.bounds,p);nearest=Mathf.Min(nearest,outside);float target=pad.height-.08f;
                        if(outside>=influence)continue;
                        if(outside==0&&target<core){core=target;coreOwner=pad.owner;}
                        float ceiling=target+outside*collarGrade,floor=target-outside*collarGrade;
                        if(ceiling<cut){cut=ceiling;cutOwner=pad.owner;}if(floor>fill)fill=floor;
                        protectedCut=Mathf.Min(protectedCut,target+outside*1.8f);protectedFill=Mathf.Max(protectedFill,target-outside*1.8f);
                    }
                    // Exact starts/extraction footing also have continuous collars;
                    // their hard interior is handled above, including the valid
                    // original sample preservation contract.
                    foreach(var area in fixedAreas)
                    {
                        float outside=DistanceTo(area.bounds,p);if(outside>=influence)continue;float target=area.height-.08f;
                        float ceiling=target+outside*collarGrade,floor=target-outside*collarGrade;
                        if(ceiling<cut){cut=ceiling;cutOwner=area.owner;}if(floor>fill)fill=floor;
                        protectedCut=Mathf.Min(protectedCut,target+outside*1.8f);protectedFill=Mathf.Max(protectedFill,target-outside*1.8f);
                    }
                    // Prefer45-degree broad shoulders. Where neighboring actual
                    // high/low footings need a steeper collar, preserve their
                    // contact through a continuous61-degree protection envelope,
                    // still below the unchanged65-degree quality threshold.
                    float gentle=Mathf.Min(cut,Mathf.Max(foundation,fill));float height=Mathf.Min(protectedCut,Mathf.Max(gentle,protectedFill));owner=height<foundation?cutOwner:(float.IsPositiveInfinity(core)?"Continuous ground fill":coreOwner);
                    tile.fieldTags[z,x]=(byte)(float.IsPositiveInfinity(core)?2:1);tile.fieldOwners[z,x]=owner;
                    if(!float.IsPositiveInfinity(core))tile.record.roadGroundingSamples++;
                    else tile.record.transitionSamples++;
                    float waterDistance=HydrologyDistance(p);
                    if(waterDistance<160){height=Mathf.Lerp(height,foundation,1-Mathf.SmoothStep(0,1,waterDistance/160));tile.fieldTags[z,x]=(byte)(waterDistance==0?6:7);}
                    if(underground.HasValue)
                    {
                        float outside=DistanceTo(MeasuredChamberRoofCore,p);var bounds=underground.Value;
                        float inside=InsideXZ(bounds,p)?Mathf.Min(Mathf.Min(p.x-bounds.min.x,bounds.max.x-p.x),Mathf.Min(p.z-bounds.min.z,bounds.max.z-p.z)):0;
                        float awayFromRoad=Mathf.SmoothStep(0,1,nearest/60),roadEntrance=awayFromRoad+(1-awayFromRoad)*Mathf.SmoothStep(0,1,inside/60);
                        if(outside<160){height=Mathf.Lerp(height,foundation,(1-Mathf.SmoothStep(0,1,outside/160))*roadEntrance);tile.fieldTags[z,x]=9;}
                    }
                    // Water and lava retain their actual basins while their collars
                    // obey the same continuous road cut/fill envelopes. A clamp
                    // only INSIDE the pavement caused V163's 142m/3m lava wall.
                    float settlementFloor=CoherentSettlementFloor(p,height,settlementFields,out string settlementOwner);
                    if(height<settlementFloor){height=settlementFloor;tile.fieldTags[z,x]=10;tile.fieldOwners[z,x]=settlementOwner;}
                    if(!underground.HasValue||!InsideXZ(underground.Value,p))height=Mathf.Min(protectedCut,Mathf.Max(height,protectedFill));
                    if(!float.IsPositiveInfinity(core))cap=Mathf.Max(cap,core);
                    if(height>cap){height=cap;tile.record.bridgeCappedSamples++;tile.fieldTags[z,x]=3;}
                    if(!float.IsPositiveInfinity(core)&&height<core-.3f&&(!underground.HasValue||!InsideXZ(underground.Value,p)))
                    {
                        tile.groundConflictCount++;
                        tile.groundConflicts.Add(new GroundConflict{position=p,requiredOwner=coreOwner,cutOwner=cutOwner,requiredHeight=core,coordinatedHeight=height});
                    }
                    heights[z,x]=Mathf.Clamp01((height+20)/600);
                    float changed=Mathf.Abs(height-foundation);if(changed>.001f){tile.record.changedByVendor++;tile.record.maximumVendorDelta=Mathf.Max(tile.record.maximumVendorDelta,changed);tile.record.maximumTransitionChange=Mathf.Max(tile.record.maximumTransitionChange,changed);}
                }
                tile.candidate.SetHeights(0,0,heights);var after=tile.candidate.GetHeights(0,0,n,n);
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    if(held[z,x]&&after[z,x]!=tile.before[z,x])throw new InvalidOperationException("Continuous coordination changed an exact original start/extraction ground sample.");
                    var p=tile.terrain.transform.position+new Vector3(x*1600f/(n-1),0,z*1600f/(n-1));if(underground.HasValue&&InsideXZ(MeasuredChamberRoofCore,p)&&after[z,x]!=tile.foundation[z,x])throw new InvalidOperationException("Continuous coordination changed the measured overhead Vista chamber field.");
                    float oldY=tile.foundation[z,x]*600-20,newY=after[z,x]*600-20;if(oldY>=200)tile.record.foundationAbove200++;if(newY>=200)tile.record.integratedAbove200++;if(oldY>=400)tile.record.foundationAbove400++;if(newY>=400)tile.record.integratedAbove400++;tile.record.integratedMaximumY=Mathf.Max(tile.record.integratedMaximumY,newY);
                }
                tile.record.protectedSamplesExact=true;tile.record.undergroundFoundationExact=true;
            }
        }
        static bool BridgePad(FieldSegment segment,Bounds bounds)
        {
            var a=segment.a;var d=segment.b-a;var flat=Vector3.ProjectOnPlane(d,Vector3.up);var delta=Vector3.ProjectOnPlane(bounds.center-a,Vector3.up);
            float projected=flat.sqrMagnitude>.0001f?Vector3.Dot(delta,flat)/flat.sqrMagnitude:0;float t=Mathf.Clamp01(projected);var q=Vector3.Lerp(a,segment.b,t);
            return DistanceTo(bounds,q)<.01f&&Mathf.Abs(q.y-bounds.center.y)<.3f;
        }
        static float DistanceTo(Bounds b,Vector3 p)
        {float x=Mathf.Max(0,Mathf.Max(b.min.x-p.x,p.x-b.max.x)),z=Mathf.Max(0,Mathf.Max(b.min.z-p.z,p.z-b.max.z));return Mathf.Sqrt(x*x+z*z);}
        static bool HydrologyCore(Vector3 p)
            =>HydrologyDistance(p)==0;
        static float HydrologyDistance(Vector3 p)
        {
            float distance=float.PositiveInfinity;
            if(p.x> -200)distance=Mathf.Min(distance,Mathf.Max(0,p.z+650));
            for(int i=1;i<FieldCreek.Length;i++)distance=Mathf.Min(distance,Mathf.Max(0,DistanceXZ(p,FieldCreek[i-1],FieldCreek[i])-34));
            for(int i=1;i<FieldLava.Length;i++)distance=Mathf.Min(distance,Mathf.Max(0,DistanceXZ(p,FieldLava[i-1],FieldLava[i])-32));return distance;
        }
        static TerrainQuality ValidateCoordinatedTerrain(DragonKingdomWorld world,TileState[] tiles,Bounds? underground)
        {
            var result=new TerrainQuality();var steep=new List<SteepCell>();foreach(var tile in tiles)
            {
                int n=tile.record.resolution;var h=tile.candidate.GetHeights(0,0,n,n);float step=1600f/(n-1);var portal=underground.GetValueOrDefault();portal.Expand(40);
                for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++)
                {
                    var p=tile.terrain.transform.position+new Vector3((x+.5f)*step,0,(z+.5f)*step);
                    if(HydrologyCore(p)||underground.HasValue&&InsideXZ(portal,p)){result.excludedHydrologyOrPortalCells++;continue;}
                    float raw=Mathf.Max(Mathf.Abs(tile.foundation[z,x+1]-tile.foundation[z,x]),Mathf.Abs(tile.foundation[z+1,x]-tile.foundation[z,x]))*600/step;
                    float slope=Mathf.Max(Mathf.Abs(h[z,x+1]-h[z,x]),Mathf.Abs(h[z+1,x]-h[z,x]))*600/step;
                    if(raw>Mathf.Tan(65*Mathf.Deg2Rad))result.rawSteep65++;if(slope>Mathf.Tan(65*Mathf.Deg2Rad))result.integratedSteep65++;if(slope>Mathf.Tan(80*Mathf.Deg2Rad))result.integratedSteep80++;result.maximumSlopeDegrees=Mathf.Max(result.maximumSlopeDegrees,Mathf.Atan(slope)*Mathf.Rad2Deg);
                    if(slope>Mathf.Tan(65*Mathf.Deg2Rad))
                    {bool east=Mathf.Abs(h[z,x+1]-h[z,x])>=Mathf.Abs(h[z+1,x]-h[z,x]);int nx=x+(east?1:0),nz=z+(east?0:1);steep.Add(new SteepCell{position=p,slope=Mathf.Atan(slope)*Mathf.Rad2Deg,height=h[z,x]*600-20,neighborHeight=h[nz,nx]*600-20,rawHeight=tile.foundation[z,x]*600-20,owner=tile.fieldOwners[z,x],neighborOwner=tile.fieldOwners[nz,nx],tag=tile.fieldTags[z,x],neighborTag=tile.fieldTags[nz,nx]});}
                }
                foreach(var other in tiles)
                {
                    var d=other.terrain.transform.position-tile.terrain.transform.position;var adjacent=other.candidate.GetHeights(0,0,n,n);
                    if(d==Vector3.right*1600)for(int z=0;z<n;z++)result.maximumTileSeamDelta=Mathf.Max(result.maximumTileSeamDelta,Mathf.Abs(h[z,n-1]-adjacent[z,0])*600);
                    if(d==Vector3.forward*1600)for(int x=0;x<n;x++)result.maximumTileSeamDelta=Mathf.Max(result.maximumTileSeamDelta,Mathf.Abs(h[n-1,x]-adjacent[0,x])*600);
                }
            }
            result.conflictingGroundCoreSamples=tiles.Sum(t=>t.groundConflictCount);result.groundConflicts=tiles.SelectMany(t=>t.groundConflicts).OrderByDescending(c=>c.requiredHeight-c.coordinatedHeight).Take(128).ToArray();
            result.allowedSteep65=Mathf.Max(2000,result.rawSteep65*4);result.passed=result.integratedSteep65<=result.allowedSteep65&&result.integratedSteep80<=result.allowedSteep80&&result.maximumTileSeamDelta<=.04f;
            result.worst=steep.OrderByDescending(s=>s.slope).Take(256).ToArray();
            result.bins=steep.GroupBy(s=>new Vector2(Mathf.Floor(s.position.x/100)*100,Mathf.Floor(s.position.z/100)*100)).Select(g=>new SteepBin{origin=g.Key,count=g.Count()}).OrderByDescending(g=>g.count).ToArray();
            result.ownership=steep.GroupBy(s=>s.tag+":"+s.owner+" -> "+s.neighborTag+":"+s.neighborOwner).Select(g=>new ConstraintCount{owner=g.Key,count=g.Count()}).OrderByDescending(g=>g.count).ToArray();
            return result;
        }
    }
}
#endif
