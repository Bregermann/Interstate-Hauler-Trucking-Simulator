using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Bounded candidate-only off-road composition. Existing EasyRoads meshes
    // and original TerrainData remain immutable; candidate terrain is cloned.
    public static class DragonKingdomTerrainShoulderAuthoring
    {
        const float Radius=220,LevelShoulder=45,Cell=64;
        sealed class Segment {public Vector3 a,b;public float width;public bool ground;}
        [Serializable]public sealed class TileEvidence
        {public string original,candidate;public int protectedSamples,changedSamples;public float maximumHeightChange;public bool protectedSamplesExact,originalHeightSamplesExact;}
        [Serializable]public sealed class Evidence
        {public string scope="CANDIDATE_OFF_ROAD_HEIGHT_BLEND_NOT_ACCEPTED_TERRAIN";public TileEvidence[] tiles;public Vector3 escapeTreeFoot=new Vector3(-284.66f,22.5f,-6.95f);public float escapeTreeTerrainBefore,escapeTreeTerrainAfter;}
        public static Evidence CloneAndBlend(DragonKingdomWorld world,string folder)
        {
            string terrainFolder=folder+"/Terrain";Directory.CreateDirectory(terrainFolder);AssetDatabase.Refresh();
            var spans=DragonKingdomWorldAuthoring.StructuralRoadTerrainSpans();var index=new Dictionary<long,List<Segment>>();
            foreach(var edge in world.definition.graph.edges)for(int i=1;i<edge.samples.Count;i++)
            {
                var a=edge.samples[i-1];var b=edge.samples[i];float distance=(a.distanceFromStartMeters+b.distanceFromStartMeters)*.5f;
                bool bridge=spans.TryGetValue(edge.edgeId,out var runs)&&runs.Any(r=>distance>=r.x&&distance<=r.y);
                bool ground=!bridge&&edge.edgeId!="boarding-deck"&&!(edge.edgeId=="harbor-loading"&&(a.position.z+b.position.z)*.5f< -600);
                var segment=new Segment{a=a.position,b=b.position,width=a.roadWidthMeters,ground=ground};
                for(int z=Mathf.FloorToInt((Mathf.Min(segment.a.z,segment.b.z)-Radius)/Cell);z<=Mathf.FloorToInt((Mathf.Max(segment.a.z,segment.b.z)+Radius)/Cell);z++)
                for(int x=Mathf.FloorToInt((Mathf.Min(segment.a.x,segment.b.x)-Radius)/Cell);x<=Mathf.FloorToInt((Mathf.Max(segment.a.x,segment.b.x)+Radius)/Cell);x++)
                {long key=Key(x,z);if(!index.TryGetValue(key,out var list))index.Add(key,list=new List<Segment>());list.Add(segment);}
            }
            var protectedAreas=new List<Bounds>();
            foreach(var region in world.regions)foreach(var start in region.truckStarts)
            {
                var bounds=new Bounds(start.position,Vector3.zero);
                foreach(float x in new[]{-2.2f,2.2f})foreach(float z in new[]{-8f,8f})bounds.Encapsulate(start.position+start.right*x+start.forward*z);
                bounds.Expand(new Vector3(8,20,8));protectedAreas.Add(bounds);
            }
            foreach(var site in world.extractionSites){var b=site.secureZone.bounds;b.Expand(8);protectedAreas.Add(b);}
            foreach(Transform t in world.transform)
                if(t.name.Contains("junction")||t.name.Contains("hairpin")||t.name.Contains("Nesting loading"))
                    {var b=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(t);b.Expand(4);protectedAreas.Add(b);}
            var evidence=new Evidence();evidence.escapeTreeTerrainBefore=TerrainHeight(world,evidence.escapeTreeFoot);
            var rows=new List<TileEvidence>();var creek=DragonKingdomWorldAuthoring.CanyonWaterPolyline();
            foreach(var terrain in world.GetComponentsInChildren<Terrain>(true))
            {
                var original=terrain.terrainData;string originalHash=DragonKingdomWorldVisualMigration.HeightHash(original);
                var clone=UnityEngine.Object.Instantiate(original);clone.name=original.name+" cosmetic shoulders";
                int resolution=original.heightmapResolution;var heights=original.GetHeights(0,0,resolution,resolution);var before=(float[,])heights.Clone();
                var protect=new bool[resolution,resolution];var row=new TileEvidence{original=AssetDatabase.GetAssetPath(original),candidate=terrainFolder+"/"+original.name+".asset"};
                float spacing=Mathf.Max(original.size.x,original.size.z)/(resolution-1);
                for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
                {
                    Vector3 p=terrain.transform.position+new Vector3((float)x/(resolution-1)*original.size.x,0,(float)z/(resolution-1)*original.size.z);
                    bool held=protectedAreas.Any(b=>p.x>=b.min.x&&p.x<=b.max.x&&p.z>=b.min.z&&p.z<=b.max.z);
                    // Preserve actual lake/boat bed and canyon stream corridor.
                    if(p.z< -650&&p.x> -200&&p.x<1600)held=true;
                    if(!held)for(int i=1;i<creek.Length;i++)if(Distance(p,creek[i-1],creek[i],out _)<=29){held=true;break;}
                    if(!index.TryGetValue(Key(Mathf.FloorToInt(p.x/Cell),Mathf.FloorToInt(p.z/Cell)),out var near)){protect[z,x]=held;if(held)row.protectedSamples++;continue;}
                    float best=float.PositiveInfinity,target=0,weightedHeight=0,totalWeight=0;
                    foreach(var segment in near)
                    {
                        float distance=Distance(p,segment.a,segment.b,out float t);
                        if(distance<=segment.width*.5f+spacing)held=true;
                        if(segment.ground&&distance<Radius)
                        {
                            float height=Mathf.Lerp(segment.a.y,segment.b.y,t)-.08f;
                            float weight=Mathf.Pow(1-distance/Radius,4)/((distance+8)*(distance+8));weightedHeight+=height*weight;totalWeight+=weight;
                            if(distance<best){best=distance;target=height;}
                        }
                    }
                    if(held){protect[z,x]=true;row.protectedSamples++;continue;}
                    if(best>=Radius)continue;
                    if(totalWeight>0)target=Mathf.Lerp(target,weightedHeight/totalWeight,Mathf.SmoothStep(0,1,Mathf.InverseLerp(20,70,best)));
                    float previous=terrain.transform.position.y+heights[z,x]*original.size.y;
                    float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(LevelShoulder,Radius,best));
                    float changed=Mathf.Lerp(previous,target,blend);float normalized=Mathf.Clamp01((changed-terrain.transform.position.y)/original.size.y);
                    if(Mathf.Abs(normalized-heights[z,x])>1e-7f){row.changedSamples++;row.maximumHeightChange=Mathf.Max(row.maximumHeightChange,Mathf.Abs(changed-previous));heights[z,x]=normalized;}
                }
                for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)if(protect[z,x]&&heights[z,x]!=before[z,x])throw new InvalidOperationException("Candidate changed a protected road/start/extraction terrain sample.");
                clone.SetHeights(0,0,heights);
                var actual=clone.GetHeights(0,0,resolution,resolution);
                for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)if(protect[z,x]&&actual[z,x]!=before[z,x])throw new InvalidOperationException("Persisted candidate changed a protected terrain height sample.");
                AssetDatabase.CreateAsset(clone,row.candidate);
                terrain.terrainData=clone;var collider=terrain.GetComponent<TerrainCollider>();if(collider==null)throw new InvalidOperationException("Actual terrain collider required.");collider.terrainData=clone;
                DragonKingdomWorldVisualMigration.RepairTerrainAppearance(clone);
                row.protectedSamplesExact=true;row.originalHeightSamplesExact=DragonKingdomWorldVisualMigration.HeightHash(original)==originalHash;
                if(!row.originalHeightSamplesExact)throw new InvalidOperationException("Candidate cloning modified original accepted height samples.");rows.Add(row);
            }
            evidence.tiles=rows.ToArray();evidence.escapeTreeTerrainAfter=TerrainHeight(world,evidence.escapeTreeFoot);
            if(Mathf.Abs(evidence.escapeTreeTerrainAfter-evidence.escapeTreeFoot.y)>3)throw new InvalidOperationException("V87 actual tree foot still has no usable near-road shoulder: "+JsonUtility.ToJson(evidence));
            return evidence;
        }
        public static float TerrainHeight(DragonKingdomWorld world,Vector3 p)
            =>TerrainHeight(world.GetComponentsInChildren<Terrain>(true),p);
        public static float TerrainHeight(Terrain[] terrains,Vector3 p)
        {foreach(var terrain in terrains){var local=p-terrain.transform.position;var size=terrain.terrainData.size;if(local.x>=0&&local.z>=0&&local.x<=size.x&&local.z<=size.z)return terrain.SampleHeight(p)+terrain.transform.position.y;}throw new InvalidOperationException("Candidate point has no actual terrain tile: "+p);}
        static long Key(int x,int z)=>((long)x<<32)^(uint)z;
        static float Distance(Vector3 p,Vector3 a,Vector3 b,out float t)
        {Vector3 v=b-a;float length=v.x*v.x+v.z*v.z;t=length>1e-6f?Mathf.Clamp01(((p.x-a.x)*v.x+(p.z-a.z)*v.z)/length):0;float x=p.x-a.x-v.x*t,z=p.z-a.z-v.z*t;return Mathf.Sqrt(x*x+z*z);}
    }
}
