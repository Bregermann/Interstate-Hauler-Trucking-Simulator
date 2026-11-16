#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class GroundingReview
        {
            public string scope="ACTUAL_NATIVE_PAVEMENT_TRIANGLES_TERRAIN_AND_DECLARED_STONE_VIADUCT_CLASSIFICATION_NOT_MATHEMATICAL_BUFFER_ONLY";
            public int predictedConflicts,bufferWithoutPavement,decorativeFoundations,terrainSupported,declaredStoneSupported,ownedBearingSupported,unsupportedPavement;
            public float maximumActualGroundGap;public bool passed;public GroundContact[] contacts;
        }
        [Serializable]public sealed class GroundContact
        {
            public Vector3 position;public string requiredOwner,mathematicalCutOwner,classification,pavement,stone;
            public float predictedHeight,terrainHeight,pavementHeight,actualGap;public bool declaredUpperSpan;
        }
        static void SeatBuildingsOnMeasuredFoundation(DragonKingdomWorld world,TileState[] tiles,List<Placement> records)
        {
            foreach(var region in world.regions.Where(r=>r.id=="village"||r.id=="harbor"||r.id=="refuge"))
            {
                var sites=(region.id=="village"?DragonKingdomWorldAuthoring.VillageHouseSites():region.id=="harbor"?DragonKingdomWorldAuthoring.HarborHouseSites():Enumerable.Empty<Tuple<string,Vector3,float,float>>())
                    .GroupBy(site=>Path.GetFileNameWithoutExtension(site.Item1)).ToDictionary(g=>g.Key,g=>new Queue<Vector3>(g.Select(s=>region.root.TransformPoint(s.Item2))));
                foreach(Transform t in region.root)
                {
                    if(!t.name.StartsWith("P_BLD_house",StringComparison.Ordinal)&&t.name!="Complete owned windmill house14")continue;
                    var b=BoundsOf(t);var before=t.position;string key=t.name=="Complete owned windmill house14"?"P_BLD_house_14":t.name.Split(' ')[0];
                    // Restore the authored streetscape hints now that an empty
                    // steam renderer no longer falsely reports a785m house.
                    // The same complete objects/effects and rotations remain.
                    Vector3 hint=sites.TryGetValue(key,out var positions)&&positions.Count>0?positions.Dequeue():b.center;bool placed=false;
                    for(int trial=0;trial<384;trial++)
                    {
                        float radius=trial==0?0:6*Mathf.Sqrt(trial),angle=trial*2.399963f;var p=hint+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                        float y=region.id=="refuge"?region.root.position.y:Mathf.Max(0,SampleRawFoundation(tiles,p));AlignBase(t,new Vector3(p.x,y,p.z));Physics.SyncTransforms();
                        if((region.id=="village"||region.id=="harbor")&&!BuildingClearOfActualWatercourse(region.id,t))continue;
                        if(!ClearRoadEnvelope(world,t,false))continue;
                        var own=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));var actual=BoundsOf(t);
                        if(Physics.OverlapBox(actual.center,actual.extents*.85f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(c=>!own.Contains(c)&&c.transform.IsChildOf(region.root)&&c.GetComponentsInParent<Transform>(true).Any(parent=>parent!=t&&(parent.name.StartsWith("P_BLD_house",StringComparison.Ordinal)||parent.name=="Complete owned windmill house14"))))continue;
                        records.Add(new Placement{region=region.id,name=t.name,operation="Ground the retained actual building mesh on its dry measured foundation near the original streetscape hint; exclude but retain steam effects",before=before,after=t.position,bounds=BoundsOf(t),trial=trial});placed=true;break;
                    }
                    if(!placed)throw new InvalidOperationException("Actual complete building cannot retain a clear supported streetscape site: "+region.id+"/"+t.name);
                }
            }
        }
        static float SampleRawFoundation(TileState[] tiles,Vector3 p)
        {
            foreach(var tile in tiles)
            {
                var o=tile.terrain.transform.position;float x=(p.x-o.x)/1600,z=(p.z-o.z)/1600;if(x<0||x>1||z<0||z>1)continue;
                int n=tile.record.resolution;float px=x*(n-1),pz=z*(n-1);int a=Mathf.Min(n-2,Mathf.FloorToInt(px)),b=Mathf.Min(n-2,Mathf.FloorToInt(pz));float u=px-a,v=pz-b;
                return Mathf.Lerp(Mathf.Lerp(tile.foundation[b,a],tile.foundation[b,a+1],u),Mathf.Lerp(tile.foundation[b+1,a],tile.foundation[b+1,a+1],u),v)*600+o.y;
            }
            throw new InvalidOperationException("Actual building foundation lies outside the four measured vendor tiles: "+p);
        }
        static GroundingReview ClassifyGroundConflicts(DragonKingdomWorld world,TileState[] tiles)
        {
            var result=new GroundingReview();var rows=new List<GroundContact>();var spans=DragonKingdomWorldAuthoring.StructuralRoadTerrainSpans();
            foreach(var conflict in tiles.SelectMany(t=>t.groundConflicts))
            {
                result.predictedConflicts++;var p=conflict.position;
                var row=new GroundContact{position=p,requiredOwner=conflict.requiredOwner,mathematicalCutOwner=conflict.cutOwner,predictedHeight=conflict.requiredHeight,terrainHeight=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,p)};
                rows.Add(row);
                if(conflict.requiredOwner.Contains(":P_BLD_")||conflict.requiredOwner.Contains(":Fortified castle sanctuary"))
                {row.classification="DECORATIVE_FOUNDATION_NOT_TRAFFIC_PAVEMENT";result.decorativeFoundations++;continue;}
                var hits=Physics.RaycastAll(new Vector3(p.x,1200,p.z),Vector3.down,1800,~0,QueryTriggerInteraction.Ignore);
                var pavement=hits.Where(h=>h.normal.y>.4f&&OwnedPavement(h.collider.transform,world.transform,conflict.requiredOwner))
                    .OrderBy(h=>Mathf.Abs(h.point.y-(conflict.requiredHeight+.08f))).ToArray();
                if(pavement.Length==0)
                {row.classification="CONSERVATIVE_CORE_BUFFER_OUTSIDE_ACTUAL_PAVEMENT_TRIANGLE";result.bufferWithoutPavement++;continue;}
                var contact=pavement[0];row.pavement=PathOf(contact.collider.transform,world.transform);row.pavementHeight=contact.point.y;row.actualGap=contact.point.y-row.terrainHeight;
                result.maximumActualGroundGap=Mathf.Max(result.maximumActualGroundGap,row.actualGap);
                if(row.actualGap<=.3f)
                {row.classification="ACTUAL_PAVEMENT_SUPPORTED_BY_TERRAIN";result.terrainSupported++;continue;}
                var bearing=hits.Where(h=>h.normal.y>.3f&&h.point.y<contact.point.y&&contact.point.y-h.point.y<=1.5f&&AncestorName(h.collider.transform,world.transform,"Owned bridge bearing ")).OrderByDescending(h=>h.point.y).ToArray();
                if(bearing.Length!=0)
                {row.classification="ACTUAL_OWNED_GIRDER_SUPPORTED_BY_MEASURED_CROSSBEAM_AND_PIERS";row.stone=PathOf(bearing[0].collider.transform,world.transform);result.ownedBearingSupported++;continue;}
                string upper=DeclaredUpperEdge(world,spans,p,contact.point.y,conflict.requiredOwner);row.declaredUpperSpan=upper!=null;
                var stone=upper==null?Array.Empty<RaycastHit>():hits.Where(h=>h.normal.y>.3f&&h.point.y<contact.point.y&&contact.point.y-h.point.y<=1.5f&&AncestorName(h.collider.transform,world.transform,"Owned stone viaduct "+upper+" over ")).OrderByDescending(h=>h.point.y).ToArray();
                if(stone.Length!=0)
                {row.classification="DECLARED_UPPER_SPAN_SUPPORTED_BY_ACTUAL_OWNED_STONE_TRIANGLE";row.stone=PathOf(stone[0].collider.transform,world.transform);result.declaredStoneSupported++;continue;}
                row.classification="ACTUAL_PAVEMENT_GAP_REQUIRES_TERRAIN_OR_PHYSICAL_SUPPORT_REPAIR";result.unsupportedPavement++;
            }
            result.contacts=rows.OrderByDescending(r=>r.classification=="ACTUAL_PAVEMENT_GAP_REQUIRES_TERRAIN_OR_PHYSICAL_SUPPORT_REPAIR").ThenByDescending(r=>r.actualGap).ToArray();
            result.passed=result.unsupportedPavement==0;return result;
        }
        static bool OwnedPavement(Transform t,Transform root,string owner)
        {while(t!=null&&t!=root){if(t.name==owner||t.name.StartsWith("Kingdom "+owner+" ",StringComparison.Ordinal))return true;t=t.parent;}return false;}
        static bool AncestorName(Transform t,Transform root,string prefix)
        {while(t!=null&&t!=root){if(t.name.StartsWith(prefix,StringComparison.Ordinal))return true;t=t.parent;}return false;}
        static string DeclaredUpperEdge(DragonKingdomWorld world,Dictionary<string,Vector2[]> spans,Vector3 p,float y,string owner)
        {
            foreach(var edge in world.definition.graph.edges)
            {
                if(!spans.TryGetValue(edge.edgeId,out var intervals))continue;
                if(owner!=edge.edgeId&&!owner.StartsWith("Kingdom hairpin "+edge.edgeId+" ",StringComparison.Ordinal)&&!owner.StartsWith("Kingdom junction ",StringComparison.Ordinal))continue;
                foreach(var pair in edge.samples.Zip(edge.samples.Skip(1),(a,b)=>new{a,b}))
                {
                    var d=pair.b.position-pair.a.position;var flat=Vector3.ProjectOnPlane(d,Vector3.up);float t=flat.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(p-pair.a.position,Vector3.up),flat)/flat.sqrMagnitude):0;
                    var q=Vector3.Lerp(pair.a.position,pair.b.position,t);float along=Mathf.Lerp(pair.a.distanceFromStartMeters,pair.b.distanceFromStartMeters,t);
                    float width=owner.StartsWith("Kingdom junction ",StringComparison.Ordinal)?50:owner.StartsWith("Kingdom hairpin ",StringComparison.Ordinal)?35:pair.b.roadWidthMeters*.5f+4;
                    if(Vector3.ProjectOnPlane(q-p,Vector3.up).magnitude<=width&&Mathf.Abs(y-q.y)<.3f&&intervals.Any(s=>along>=s.x-.05f&&along<=s.y+.05f))return edge.edgeId;
                }
            }
            return null;
        }
    }
}
#endif
