using System.Collections.Generic;
using UnityEngine;
using LWS.InterstateHauler;

namespace LWS.TruckFareFight
{
    // Initial placement only. The shared graph/planner owns road connectivity;
    // this sport samples equal remaining driving distance along those routes.
    public sealed class FareFightStartingGrid
    {
        struct Candidate { public Vector3 position,forward; public float distance; }
        readonly FareFightRoadAccess access=new FareFightRoadAccess(new LwsRoutePlanner(LwsNavigationTuning.Default()));
        readonly List<Vector3> path=new List<Vector3>(256),verified=new List<Vector3>(256);
        readonly List<Candidate> candidates=new List<Candidate>(64);
        public string Diagnostic{get;private set;}
        public bool TryBuild(LwsRoadGraph graph,Vector3 pickup,int count,float separation,int rotation,Vector3[] positions,Vector3[] forwards,float[] distances,System.Func<Vector3,Vector3,bool> clear=null)
        {
            candidates.Clear();const float target=240;int routed=0,joins=0,rejected=0;string examples="";
            foreach(float approachDistance in new[]{232f,248f})
            foreach(var node in graph.nodes)
            {
                if(!access.TryBuild(graph,node.position,pickup,path,out var length)||length<target+15)continue;
                routed++;
                float remaining=approachDistance;Vector3 point=default,forward=default;bool found=false;
                for(int i=path.Count-1;i>0;i--)
                {
                    float segment=Vector3.Distance(path[i],path[i-1]);
                    if(segment<remaining){remaining-=segment;continue;}
                    forward=Vector3.ProjectOnPlane(path[i]-path[i-1],Vector3.up).normalized;
                    point=path[i]-forward*remaining;found=forward.sqrMagnitude>.9f;break;
                }
                if(!found)continue;joins++;
                // Right-hand lane, leaving the authored centerline available
                // to ordinary traffic. Re-measure the actual access route.
                point+=Vector3.Cross(Vector3.up,forward)*2;point.y+=.2f;
                if(clear!=null&&!clear(point,forward))continue;
                bool duplicate=false;foreach(var candidate in candidates)if(Vector3.Distance(candidate.position,point)<separation){duplicate=true;break;}
                if(duplicate)continue;
                if(!access.TryBuild(graph,point,pickup,verified,out var measured)||Mathf.Abs(measured-target)>target*.08f){if(rejected<3)examples+=" from="+point+" actual="+measured;rejected++;continue;}
                candidates.Add(new Candidate{position=point,forward=forward,distance=measured});
            }
            Diagnostic="candidates="+candidates.Count+" routed="+routed+" joins="+joins+" distanceRejected="+rejected+examples;
            if(candidates.Count<count)return false;
            var chosen=new List<Candidate>(count);
            for(int slot=0;slot<count;slot++)
            {
                int best=-1;float spread=-1;
                for(int i=0;i<candidates.Count;i++)
                {
                    float nearest=chosen.Count==0?Vector3.Distance(candidates[i].position,pickup):float.PositiveInfinity;
                    foreach(var other in chosen)nearest=Mathf.Min(nearest,Vector3.Distance(other.position,candidates[i].position));
                    if(nearest>spread){spread=nearest;best=i;}
                }
                if(best<0||chosen.Count>0&&spread<separation)return false;
                chosen.Add(candidates[best]);candidates.RemoveAt(best);
            }
            for(int slot=0;slot<count;slot++){var candidate=chosen[(slot+rotation%count)%count];positions[slot]=candidate.position;forwards[slot]=candidate.forward;distances[slot]=candidate.distance;}
            return true;
        }
    }
}
