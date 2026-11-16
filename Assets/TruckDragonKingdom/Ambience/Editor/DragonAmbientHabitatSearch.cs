#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Ambience
{
    // Regional roots describe road junctions, not offroad walking ground.
    // Search real owned environment footprints and actual terrain support.
    internal static class DragonAmbientHabitatSearch
    {
        const int MaximumCandidates=1024;
        internal static Vector3[] Find(DragonKingdomWorld world,DragonWorldRegion region,int phase,Vector3[][] reserved)
        {
            var terrain=world.GetComponentsInChildren<Terrain>(true).Where(item=>item.isActiveAndEnabled&&item.terrainData!=null).ToArray();
            var rejected=new List<DragonAmbienceClearanceDiagnostics.Rejection>();var counts=new Dictionary<string,int>();
            var bounds=world.definition.FindRegion(region.id).bounds;int attempts=0;
            void Reject(string reason,Vector3 point)
            {
                counts.TryGetValue(reason,out int count);counts[reason]=count+1;
                // Retain different failure kinds and later search locations,
                // instead of letting the first junction ring consume all24.
                if(rejected.Count<24)rejected.Add(new DragonAmbienceClearanceDiagnostics.Rejection{reason=reason,point=point});
                else if(count==0)rejected[23]=new DragonAmbienceClearanceDiagnostics.Rejection{reason=reason,point=point};
                else if(attempts%43==0)rejected[(attempts/43)%23]=new DragonAmbienceClearanceDiagnostics.Rejection{reason=reason,point=point};
            }
            foreach(var proposed in Centers(region,bounds,phase).Take(MaximumCandidates))
            {
                attempts++;var center=proposed;
                if(!Inside(bounds,center,11)){Reject("outside-actual-region-footprint",center);continue;}
                center.y=TerrainHeight(terrain,center,center.y);
                if(reserved.Any(points=>DragonKingdomAmbienceAuthoring.DistanceXZ(center,points[0],points[0])<30)){Reject("existing-habitat-separation",center);continue;}
                if(!DragonKingdomAmbienceAuthoring.ClearOfRoadsAndObjectives(world,center)){Reject("road-or-objective-margin",center);continue;}
                var points=new Vector3[8];bool valid=true;
                for(int index=0;index<8&&valid;index++)
                {
                    float angle=index*Mathf.PI*.25f;var desired=center+new Vector3(Mathf.Cos(angle)*9,0,Mathf.Sin(angle)*7);
                    if(!Ground(terrain,desired,out points[index],out string reason)){valid=false;Reject(reason,desired);}
                    else if(!DragonKingdomAmbienceAuthoring.ClearOfRoadsAndObjectives(world,points[index])){valid=false;Reject("road-or-objective-margin",points[index]);}
                }
                for(int leg=0;leg<8&&valid;leg++)
                {
                    Vector3 from=points[leg],to=points[(leg+1)%8];int samples=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/.7f));
                    for(int step=0;step<=samples&&valid;step++)
                    {
                        Vector3 point=Vector3.Lerp(from,to,step/(float)samples);
                        if(!Ground(terrain,point,out var ground,out string reason)){valid=false;Reject("ground-segment-"+reason,point);}
                        else if(Mathf.Abs(ground.y-point.y)>=.22f){valid=false;Reject("ground-segment-height-discontinuity",point);}
                        else if(!DragonKingdomAmbienceAuthoring.ClearOfRoadsAndObjectives(world,ground)){valid=false;Reject("ground-segment-road-or-objective-margin",ground);}
                    }
                }
                if(!valid)continue;
                DragonAmbienceClearanceDiagnostics.Write(world,region,"ground-"+phase+"-accepted",attempts,rejected,counts,points);
                return points;
            }
            string report=DragonAmbienceClearanceDiagnostics.Write(world,region,"ground-"+phase,attempts,rejected,counts);
            throw new InvalidOperationException("No supported clear offroad habitat in actual "+region.id+" after owned footprint/terrain search; original support/body/road/objective/path gates retained. Native witnesses: "+report);
        }
        static IEnumerable<Vector3> Centers(DragonWorldRegion region,Bounds bounds,int phase)
        {
            var seeds=new List<Vector3>();
            // Actual environment container pivots include castle sanctuary,
            // houses, trees and rock groups. Ignore bare semantic anchors.
            foreach(Transform child in region.root)
            {
                if(!LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring.TryMeasureOwnedModelBounds(child,out var physical))continue;
                Vector3 pivot=child.position; pivot.y=physical.min.y;
                if(!seeds.Any(old=>DragonKingdomAmbienceAuthoring.DistanceXZ(old,pivot,pivot)<18))seeds.Add(pivot);
                Vector3 center=physical.center;center.y=physical.min.y;
                if(!seeds.Any(old=>DragonKingdomAmbienceAuthoring.DistanceXZ(old,center,center)<18))seeds.Add(center);
            }
            // Interleave bounded offsets across up to16 real footprints so a
            // large early group cannot starve the other regional geometry.
            var selected=seeds.Count<=16?seeds:Enumerable.Range(0,16).Select(index=>seeds[index*(seeds.Count-1)/15]).ToList();
            IEnumerable<Vector3> AroundSources()
            {
                foreach(float radius in new[]{0f,24f,42f,65f,88f,112f})
                    foreach(var seed in selected)
                        for(int index=0;index<(radius==0?1:16);index++)
                        {
                            float angle=(index+phase*2)*Mathf.PI/8;
                            yield return seed+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                        }
            }
            foreach(var point in AroundSources().Take(512))yield return point;
            // Real region AABB coverage handles actual open ground between
            // source footprints. No invented flat deck or ground is created.
            float step=Mathf.Max(24,Mathf.Sqrt(Mathf.Max(1,bounds.size.x-32)*Mathf.Max(1,bounds.size.z-32)/480));
            for(float z=bounds.min.z+16;z<bounds.max.z-16;z+=step)
                for(float x=bounds.min.x+16;x<bounds.max.x-16;x+=step)
                    yield return new Vector3(x,region.root.position.y,z);
        }
        static bool Inside(Bounds bounds,Vector3 point,float margin)=>point.x>=bounds.min.x+margin&&point.x<=bounds.max.x-margin&&point.z>=bounds.min.z+margin&&point.z<=bounds.max.z-margin;
        static float TerrainHeight(Terrain[] terrain,Vector3 point,float fallback)
        {
            foreach(var tile in terrain)
            {
                var origin=tile.transform.position;var size=tile.terrainData.size;
                if(point.x>=origin.x&&point.x<=origin.x+size.x&&point.z>=origin.z&&point.z<=origin.z+size.z)return tile.SampleHeight(point)+origin.y;
            }
            return fallback;
        }
        static bool Ground(Terrain[] terrain,Vector3 desired,out Vector3 result,out string reason)
        {
            // Same30m support band, now around actual local terrain rather
            // than a road junction that may be44m above forest ground.
            float reference=TerrainHeight(terrain,desired,desired.y);result=default;reason="unsupported-normal-or-height";
            var hits=Physics.RaycastAll(new Vector3(desired.x,reference+130,desired.z),Vector3.down,230,~0,QueryTriggerInteraction.Ignore);
            foreach(var hit in hits.Where(item=>item.normal.y>.85f&&item.collider.attachedRigidbody==null&&Mathf.Abs(item.point.y-reference)<30).OrderBy(item=>Mathf.Abs(item.point.y-reference)))
            {
                var foot=hit.point+Vector3.up*.04f;
                if(Physics.CheckCapsule(foot+Vector3.up*.52f,foot+Vector3.up*1.65f,.4f,~0,QueryTriggerInteraction.Ignore)){reason="body-volume";continue;}
                result=foot;return true;
            }
            return false;
        }
    }
}
#endif
