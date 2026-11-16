#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        // Spatial candidates only. The existing actual DistanceTo/Constraint
        // calculation remains authoritative for exact physical influence.
        public sealed class NativeFootingIndex
        {
            public const float CellMeters=64;
            readonly Dictionary<long,NativeArchitectureFooting[]> cells;
            public int Footings{get;}public int CellReferences{get;}
            static long Key(int x,int z)=>((long)x<<32)^(uint)z;
            public NativeFootingIndex(IReadOnlyList<NativeArchitectureFooting> footings,float featherMeters)
            {
                if(footings==null||!float.IsFinite(featherMeters)||featherMeters<0)throw new ArgumentException("Actual footing list and finite nonnegative transition distance required.");
                Footings=footings.Count;var build=new Dictionary<long,List<NativeArchitectureFooting>>();int references=0;
                foreach(var footing in footings)
                {
                    if(footing==null)throw new ArgumentException("Null architecture footing.");var bounds=footing.physicalFootprint;
                    if(!float.IsFinite(bounds.center.x)||!float.IsFinite(bounds.center.z)||!float.IsFinite(bounds.size.x)||!float.IsFinite(bounds.size.z)||bounds.size.x<=0||bounds.size.z<=0)throw new ArgumentException("Finite actual individual footing XZ bounds required.");
                    int x0=Mathf.FloorToInt((bounds.min.x-featherMeters)/CellMeters),x1=Mathf.FloorToInt((bounds.max.x+featherMeters)/CellMeters),z0=Mathf.FloorToInt((bounds.min.z-featherMeters)/CellMeters),z1=Mathf.FloorToInt((bounds.max.z+featherMeters)/CellMeters);
                    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                    {long key=Key(x,z);if(!build.TryGetValue(key,out var list))build.Add(key,list=new List<NativeArchitectureFooting>());list.Add(footing);references++;}
                }
                cells=build.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToArray());CellReferences=references;
            }
            public IReadOnlyList<NativeArchitectureFooting> At(Vector3 point)
            {
                return cells.TryGetValue(Key(Mathf.FloorToInt(point.x/CellMeters),Mathf.FloorToInt(point.z/CellMeters)),out var found)?found:Array.Empty<NativeArchitectureFooting>();
            }
        }
    }
}
#endif
