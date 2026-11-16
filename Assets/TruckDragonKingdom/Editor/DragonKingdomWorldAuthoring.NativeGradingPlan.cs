#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomWorldAuthoring
    {
        [Serializable] public sealed class NativeGradingSourceSpan{public string lower;public float start,end;}
        [Serializable] public sealed class NativeGradingSourceInterval{public int marker;public float from,to;public bool excluded;}
        [Serializable] public sealed class NativeGradingSourcePlan
        {
            public string roadId;public float width;public bool physicalCrossings;public Bounds underground;
            public Vector3[] originalRoadPoints,actualGradingMarkers;public NativeGradingSourceSpan[] spans;public NativeGradingSourceInterval[] intervals;
        }
        [Serializable] public sealed class NativeHairpinSourcePlan{public string roadName,typeName;public float width,indent,surrounding;public Vector3[] markers;}
        public static NativeHairpinSourcePlan InspectOriginalHairpinGradingPlan(string roadId,int cornerIndex)
        {
            var turn=HairpinTurns(Roads()).Single(t=>t.Item1==roadId&&t.Item2==cornerIndex);
            return new NativeHairpinSourcePlan{roadName=turn.Item1+" hairpin grading "+turn.Item2,typeName=turn.Item1+" hairpin turning area",width=42,indent=3,surrounding=6,markers=new[]{turn.Item3-Vector3.forward*21,turn.Item3+Vector3.forward*21}};
        }
        // Read-only inspection of the existing production marker/physical-span
        // authorities. No native road, terrain, network or source asset is edited.
        public static NativeGradingSourcePlan InspectNativeMainRoadGradingPlan(string roadId,Bounds? underground,bool physicalCrossings)
        {
            var specifications=Roads();var specification=specifications.Single(r=>r.id==roadId);
            var spans=physicalCrossings
                ? DragonKingdomNativeRoadCrossings.Find(specifications,CanyonWaterPolyline(),3.125f).Where(row=>row.upper==roadId).Select(row=>new BridgeSpan{upper=specification,lower=row.lower,start=row.start,end=row.end,crossing=row.crossing,point=row.point,forward=row.upperForward}).ToList()
                : BridgeSpans(specifications).Where(row=>row.upper.id==roadId).ToList();
#if VISTA
            if(underground.HasValue)
            {
                float along=0;
                for(int i=1;i<specification.points.Length;i++)
                {
                    var a=specification.points[i-1];var b=specification.points[i];
                    if(DragonKingdomDiggerCavernAuthoring.Clip(underground.Value,a,b,out var entry,out var exit))spans.Add(new BridgeSpan{upper=specification,lower="Measured underground cavern exclusion",start=along+Vector3.Distance(a,entry),end=along+Vector3.Distance(a,exit)});
                    along+=Vector3.Distance(a,b);
                }
            }
#endif
            var markers=GradingMarkers(specification,spans.ToArray());var intervals=new NativeGradingSourceInterval[Math.Max(0,markers.Length-1)];float distance=0;
            for(int i=1;i<markers.Length;i++){float previous=distance;distance+=Vector3.Distance(markers[i-1],markers[i]);intervals[i-1]=new NativeGradingSourceInterval{marker=i-1,from=previous,to=distance,excluded=spans.Any(span=>(previous+distance)*.5f>span.start&&(previous+distance)*.5f<span.end)};}
            return new NativeGradingSourcePlan{roadId=roadId,width=specification.width,physicalCrossings=physicalCrossings,underground=underground??default,originalRoadPoints=specification.points.ToArray(),actualGradingMarkers=markers,spans=spans.Select(s=>new NativeGradingSourceSpan{lower=s.lower,start=s.start,end=s.end}).ToArray(),intervals=intervals};
        }
    }
}
#endif
