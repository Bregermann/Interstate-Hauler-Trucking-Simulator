#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Preparation only: neither legacy authoring nor native coordination calls
    // this until the independent type-default experiment has been accepted.
    // Stone-arch visual proportions are deliberately not terrain authority.
    public static class DragonKingdomNativeRoadCrossings
    {
        // These cover the stronger existing authored-art road-body query
        // (2.2,3,8), plus the original +/- .35*width wheel/body sweeps.
        public const float BodyHalfWidth=2.2f,BodyHalfLength=8,BodyTopAboveRoad=6.3f;
        public const float OriginalCreekCoreHalfWidth=34;
        [Serializable]public sealed class Crossing
        {
            public string upper,lower,authority;
            public bool lowerIsWater;
            public int upperSegment,lowerSegment;
            public float start,end,crossing,upperLength,gap,sine,cosine,upperCorridorHalfWidth,lowerCorridorHalfWidth,bodyEndMargin,terrainCellHalfDiagonal;
            public Vector3 point,lowerPoint,upperForward,lowerForward;
        }
        public static Crossing[] Original()
            =>Find(DragonKingdomWorldAuthoring.Roads(),DragonKingdomWorldAuthoring.CanyonWaterPolyline(),3.125f);

        // One result authority is intended for both vendor marker exclusions
        // and native ground/bridge classification; callers must not reconstruct
        // a second mask using renderer bounds or the old aperture factor.
        public static Dictionary<string,Vector2[]> TerrainSpans(Crossing[] crossings)
        {
            if(crossings==null)throw new ArgumentNullException(nameof(crossings));
            var result=new Dictionary<string,Vector2[]>();
            foreach(var group in crossings.GroupBy(c=>c.upper))
            {
                var spans=new List<Vector2>();
                foreach(var row in group.OrderBy(c=>c.start).ThenBy(c=>c.end))
                {
                    if(spans.Count==0||row.start>spans[spans.Count-1].y)spans.Add(new Vector2(row.start,row.end));
                    else{var last=spans[spans.Count-1];last.y=Mathf.Max(last.y,row.end);spans[spans.Count-1]=last;}
                }
                result.Add(group.Key,spans.ToArray());
            }
            return result;
        }

        public static Crossing[] Find(DragonKingdomWorldAuthoring.RoadSpec[] source,Vector3[] creek,float terrainCellMeters)
        {
            if(source==null||!float.IsFinite(terrainCellMeters)||terrainCellMeters<=0||terrainCellMeters>10)throw new ArgumentException("Actual roads and finite native heightfield spacing required.");
            if(source.Select(r=>r?.id).Distinct().Count()!=source.Length)throw new ArgumentException("Road semantic IDs must be unique.");
            foreach(var road in source)ValidateRoad(road);
            var roads=source.ToList();
            if(creek!=null)
            {
                var water=new DragonKingdomWorldAuthoring.RoadSpec("waterfall-canyon-stream","","",OriginalCreekCoreHalfWidth*2,creek);ValidateRoad(water);roads.Add(water);
            }
            var result=new List<Crossing>();
            for(int a=0;a<source.Length;a++)for(int b=a+1;b<roads.Count;b++)
            {
                bool water=roads[b].id=="waterfall-canyon-stream";
                // Keep the existing authored stream crossing, not a new graph.
                if(water&&roads[a].id!="forest-nests")continue;
                float alongA=0;
                for(int i=1;i<roads[a].points.Length;i++)
                {
                    Vector3 p=roads[a].points[i-1],u=roads[a].points[i]-p;float alongB=0;
                    for(int j=1;j<roads[b].points.Length;j++)
                    {
                        Vector3 q=roads[b].points[j-1],v=roads[b].points[j]-q,w=q-p;
                        float det=u.x*v.z-u.z*v.x,uxz=new Vector2(u.x,u.z).magnitude,vxz=new Vector2(v.x,v.z).magnitude;
                        if(Mathf.Abs(det)>1e-5f)
                        {
                            float t=(w.x*v.z-w.z*v.x)/det,s=(w.x*u.z-w.z*u.x)/det;
                            if(t>=0&&t<=1&&s>=0&&s<=1)
                            {
                                Vector3 pa=p+u*t,pb=q+v*s;float gap=Mathf.Abs(pa.y-pb.y);
                                if(gap>.3f&&(!water||pa.y>pb.y))
                                {
                                    if(gap<9)throw new InvalidOperationException("Actual physical road crossing lacks original >=9m overpass clearance: "+roads[a].id+"/"+roads[b].id+" gap="+gap);
                                    bool aUpper=pa.y>pb.y;var upper=aUpper?roads[a]:roads[b];var lower=aUpper?roads[b]:roads[a];
                                    Vector3 upperDelta=aUpper?u:v,lowerDelta=aUpper?v:u;
                                    float upperXZ=aUpper?uxz:vxz,sine=Mathf.Abs(det)/(uxz*vxz),cosine=Mathf.Abs(Vector2.Dot(new Vector2(u.x,u.z)/uxz,new Vector2(v.x,v.z)/vxz));
                                    float upperHalf=CorridorHalfWidth(upper.width),lowerHalf=water?OriginalCreekCoreHalfWidth:CorridorHalfWidth(lower.width);
                                    // Intersection of actual two swept strips: a transverse
                                    // point of the upper corridor shifts along the lower
                                    // corridor by upperHalf*cos(theta). No .22 visual scale.
                                    float cellMargin=terrainCellMeters*Mathf.Sqrt(2)*.5f;
                                    float halfXZ=(lowerHalf+upperHalf*cosine)/sine+BodyHalfLength+cellMargin;
                                    float halfAlong=halfXZ*upperDelta.magnitude/upperXZ;
                                    float distance=aUpper?alongA+u.magnitude*t:alongB+v.magnitude*s,length=Length(upper.points);
                                    var row=new Crossing{upper=upper.id,lower=lower.id,lowerIsWater=water,upperSegment=aUpper?i:j,lowerSegment=aUpper?j:i,
                                        start=Mathf.Max(0,distance-halfAlong),end=Mathf.Min(length,distance+halfAlong),crossing=distance,upperLength=length,gap=gap,sine=sine,cosine=cosine,
                                        upperCorridorHalfWidth=upperHalf,lowerCorridorHalfWidth=lowerHalf,bodyEndMargin=BodyHalfLength,terrainCellHalfDiagonal=cellMargin,
                                        point=aUpper?pa:pb,lowerPoint=aUpper?pb:pa,upperForward=upperDelta.normalized,lowerForward=lowerDelta.normalized,
                                        authority=water?"ORIGINAL_34M_CREEK_CORE_WITH_ACTUAL_UPPER_ROAD_BODY_FOOTPRINT":"ACTUAL_ROAD_WIDTH_AND_ORIGINAL_WHEEL_BODY_SWEEP"};
                                    // A marker shared by adjacent segments can describe the
                                    // same physical crossing twice. Keep its union and the
                                    // more conservative local angle rather than double-count.
                                    var prior=result.FirstOrDefault(c=>c.upper==row.upper&&c.lower==row.lower&&Vector3.Distance(c.point,row.point)<.01f);
                                    if(prior==null)result.Add(row);else{prior.start=Mathf.Min(prior.start,row.start);prior.end=Mathf.Max(prior.end,row.end);prior.sine=Mathf.Min(prior.sine,row.sine);}
                                }
                            }
                        }
                        alongB+=v.magnitude;
                    }
                    alongA+=u.magnitude;
                }
            }
            return result.OrderBy(c=>c.upper,StringComparer.Ordinal).ThenBy(c=>c.crossing).ThenBy(c=>c.lower,StringComparer.Ordinal).ToArray();
        }

        // Exact ground contacts cannot be silently cut by a bridge cap. A cap
        // with sufficient clearance leaves the real lower core authoritative;
        // an incompatible physical envelope is an explicit rejected candidate.
        public static float RequireCompatibleGroundCore(float ground,float upperClearanceCap,string groundOwner,string upperOwner)
        {
            if(!float.IsFinite(ground)||float.IsNaN(upperClearanceCap)||float.IsNegativeInfinity(upperClearanceCap))throw new ArgumentException("Finite physical ground and a finite or absent upper cap required.");
            if(upperClearanceCap<ground-.3f)throw new InvalidOperationException("Incompatible actual crossing core: ground="+groundOwner+" @"+ground+", upper="+upperOwner+" cap="+upperClearanceCap);
            return ground;
        }
        public static bool PhysicalBridgeSpanIncludesProjection(float projected,float segmentFrom,float segmentTo,float spanFrom,float spanTo)
        {
            if(!float.IsFinite(projected)||!float.IsFinite(segmentFrom)||!float.IsFinite(segmentTo)||!float.IsFinite(spanFrom)||!float.IsFinite(spanTo)||segmentTo<=segmentFrom||spanTo<=spanFrom||segmentFrom<spanFrom-.01f||segmentTo>spanTo+.01f)throw new ArgumentException("Actual finite bridge segment inside its declared physical span required.");
            // Do not clamp the projection to the bridge endpoint and extend a
            // radial48m cut into the same rising ground road. The declared
            // interval already includes the measured8m body-end margin.
            float actualAlong=Mathf.LerpUnclamped(segmentFrom,segmentTo,projected);
            return actualAlong>=spanFrom&&actualAlong<=spanTo;
        }
        static float CorridorHalfWidth(float width)=>Mathf.Max(width*.5f,width*.35f+BodyHalfWidth);
        static float Length(Vector3[] points){float result=0;for(int i=1;i<points.Length;i++)result+=Vector3.Distance(points[i-1],points[i]);return result;}
        static void ValidateRoad(DragonKingdomWorldAuthoring.RoadSpec road)
        {
            if(road==null||string.IsNullOrEmpty(road.id)||!float.IsFinite(road.width)||road.width<=0||road.points==null||road.points.Length<2||road.points.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)))throw new ArgumentException("Finite actual road geometry and semantic identity required.");
            for(int i=1;i<road.points.Length;i++)if(Vector3.ProjectOnPlane(road.points[i]-road.points[i-1],Vector3.up).sqrMagnitude<1e-6f)throw new ArgumentException("Road segments need nonzero XZ extent.");
        }
    }
}
#endif
