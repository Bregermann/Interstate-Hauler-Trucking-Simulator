#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const float NativeFootingSamplingMaximumExpansion=3.125f;
        [Serializable]public sealed class NativeFootingSamplingSupportRow
        {
            public string footingId;public Bounds originalPhysicalFootprint,terrainSamplingSupport;
            public Vector2Int minimumVertex,maximumVertex;public int supportVertices,physicalCoreVertices,additionalSamplingVertices;
        }
        [Serializable]public sealed class NativeFootingSamplingSupportEvidence
        {
            public string policy="ORIGINAL_513_VERTEX_1600M_TERRAIN_TRIANGLE_INTERPOLATION_BASIS_FLOOR_MIN_CEIL_MAX_NOT_EXPANDED_SOURCE_GEOMETRY";
            public Vector2 mosaicOrigin=new Vector2(-1600,-800);public float vertexSpacing=3.125f;
            public int measuredFootings,uniqueSupportVertices,uniqueAdditionalVertices,differentTargetConflicts;
            public NativeFootingSamplingSupportRow[] rows;
        }
        sealed class NativeFootingSamplingSupportContext
        {
            public NativeFootingSamplingSupportEvidence evidence;public Dictionary<string,Bounds> bounds;
            public Bounds For(NativeArchitectureFooting footing)=>bounds.TryGetValue(footing.id,out var value)?value:throw new InvalidOperationException("Unknown measured footing interpolation authority: "+footing.id);
        }
        static Bounds NativeFootingTerrainSamplingBounds(Bounds physical)
        {
            if(!ArchitectureFinite(physical.center)||!ArchitectureFinite(physical.size)||physical.size.x<=0||physical.size.z<=0||physical.min.x< -1600||physical.max.x>1600||physical.min.z< -800||physical.max.z>2400)throw new InvalidOperationException("Finite original physical footprint inside the actual four513/1600m fields required.");
            // Terrain.SampleHeight interpolates triangles. A perimeter point
            // depends on vertices OUTSIDE the measured mesh footprint. Preserve
            // that minimal lattice basis, keeping source geometry unchanged.
            float x0=-1600+Mathf.Floor((physical.min.x+1600)/3.125f)*3.125f,x1=-1600+Mathf.Ceil((physical.max.x+1600)/3.125f)*3.125f,z0=-800+Mathf.Floor((physical.min.z+800)/3.125f)*3.125f,z1=-800+Mathf.Ceil((physical.max.z+800)/3.125f)*3.125f;
            return new Bounds(new Vector3((x0+x1)*.5f,physical.center.y,(z0+z1)*.5f),new Vector3(x1-x0,physical.size.y,z1-z0));
        }
        static NativeFootingSamplingSupportContext BuildNativeFootingSamplingSupports(IReadOnlyList<NativeArchitectureFooting> footings)
        {
            if(footings==null||footings.Any(f=>f==null||string.IsNullOrEmpty(f.id))||footings.Select(f=>f.id).Distinct().Count()!=footings.Count)throw new InvalidOperationException("Distinct original measured footing IDs required for terrain interpolation support.");var context=new NativeFootingSamplingSupportContext{bounds=new Dictionary<string,Bounds>(StringComparer.Ordinal),evidence=new NativeFootingSamplingSupportEvidence{measuredFootings=footings.Count}};var rows=new List<NativeFootingSamplingSupportRow>();var targets=new Dictionary<Vector2Int,float>();var additional=new HashSet<Vector2Int>();
            foreach(var footing in footings)
            {
                var source=footing.physicalFootprint;var support=NativeFootingTerrainSamplingBounds(source);context.bounds.Add(footing.id,support);var row=new NativeFootingSamplingSupportRow{footingId=footing.id,originalPhysicalFootprint=source,terrainSamplingSupport=support,minimumVertex=new Vector2Int(Mathf.RoundToInt((support.min.x+1600)/3.125f),Mathf.RoundToInt((support.min.z+800)/3.125f)),maximumVertex=new Vector2Int(Mathf.RoundToInt((support.max.x+1600)/3.125f),Mathf.RoundToInt((support.max.z+800)/3.125f))};
                for(int z=row.minimumVertex.y;z<=row.maximumVertex.y;z++)for(int x=row.minimumVertex.x;x<=row.maximumVertex.x;x++)
                {
                    var key=new Vector2Int(x,z);var p=new Vector3(-1600+x*3.125f,0,-800+z*3.125f);row.supportVertices++;if(InsideXZ(source,p))row.physicalCoreVertices++;else{row.additionalSamplingVertices++;additional.Add(key);}if(targets.TryGetValue(key,out float target)){if(Mathf.Abs(target-footing.surfaceY)>.3f){context.evidence.differentTargetConflicts++;throw new InvalidOperationException("Incompatible original footing terrain-sampling targets at "+p.ToString("R")+"; never choose a convenient building height or override another physical authority.");}}else targets.Add(key,footing.surfaceY);
                }
                rows.Add(row);
            }
            context.evidence.rows=rows.ToArray();context.evidence.uniqueSupportVertices=targets.Count;context.evidence.uniqueAdditionalVertices=additional.Count;return context;
        }
    }
}
#endif
