#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Ambience
{
    // Bounded read-only witnesses for explicit native authoring failures. This
    // never moves an actor/obstacle, changes a gate or performs a runtime bake.
    internal static class DragonAmbienceClearanceDiagnostics
    {
        [Serializable] internal sealed class Rejection { public string reason;public Vector3 point; }
        [Serializable] sealed class Probe
        {
            public string reason,nearestRoad,nearestObjective;
            public Vector3 point;
            public float roadCenterDistance,roadWidth,objectiveDistance;
            public string[] supportHits,bodyOverlaps;
        }
        [Serializable] sealed class Report
        {
            public string scope,region,stage;
            public int attempts;
            public Vector3 regionPosition;
            public Probe[] failures;
            public ReasonCount[] allRejectionCounts;
            public Vector3[] acceptedLoop;
        }
        [Serializable] sealed class ReasonCount{public string reason;public int count;}
        internal static void Reject(List<Rejection> list,string reason,Vector3 point)
        {if(list.Count<24)list.Add(new Rejection{reason=reason,point=point});}
        internal static string Write(DragonKingdomWorld world,DragonWorldRegion region,string stage,int attempts,IEnumerable<Rejection> rejections,IReadOnlyDictionary<string,int> counts=null,Vector3[] accepted=null)
        {
            var probes=new List<Probe>();
            foreach(var rejected in rejections.Take(24))
            {
                var probe=new Probe{reason=rejected.reason,point=rejected.point,roadCenterDistance=float.MaxValue,objectiveDistance=float.MaxValue};
                foreach(var edge in world.definition.graph.edges)for(int index=1;index<edge.samples.Count;index++)
                {
                    float distance=DragonKingdomAmbienceAuthoring.DistanceXZ(rejected.point,edge.samples[index-1].position,edge.samples[index].position);
                    if(distance>=probe.roadCenterDistance)continue;
                    probe.roadCenterDistance=distance;probe.nearestRoad=edge.edgeId;
                    probe.roadWidth=Mathf.Max(edge.samples[index-1].roadWidthMeters,edge.samples[index].roadWidthMeters,edge.laneWidthMeters*edge.laneCount+edge.leftShoulderWidthMeters+edge.rightShoulderWidthMeters);
                }
                foreach(var binding in world.regions)foreach(var anchor in binding.truckStarts.Concat(binding.perches).Concat(binding.nests).Concat(binding.treasureCaches))
                {
                    if(anchor==null)continue;float distance=Vector3.Distance(anchor.position,rejected.point);
                    if(distance<probe.objectiveDistance){probe.objectiveDistance=distance;probe.nearestObjective=Path(anchor);}
                }
                probe.supportHits=Physics.RaycastAll(new Vector3(rejected.point.x,rejected.point.y+130,rejected.point.z),Vector3.down,230,~0,QueryTriggerInteraction.Ignore).OrderBy(hit=>hit.distance).Take(8).Select(hit=>Path(hit.collider.transform)+" point="+hit.point.ToString("F3")+" normal="+hit.normal.ToString("F3")+" dynamic="+(hit.collider.attachedRigidbody!=null)).ToArray();
                probe.bodyOverlaps=Physics.OverlapCapsule(rejected.point+Vector3.up*.52f,rejected.point+Vector3.up*1.65f,.4f,~0,QueryTriggerInteraction.Ignore).Take(8).Select(collider=>Path(collider.transform)+" bounds="+collider.bounds).ToArray();
                probes.Add(probe);
            }
            var args=Environment.GetCommandLineArgs();int option=Array.IndexOf(args,"-tsDragonOutput");string output=option>=0&&option+1<args.Length?args[option+1]:"Logs/TruckSports/DragonKingdom";
            output=System.IO.Path.GetFullPath(System.IO.Path.Combine(output,"AmbienceClearance"));Directory.CreateDirectory(output);
            string file=System.IO.Path.Combine(output,DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+region.id+"-"+stage+".json");
            File.WriteAllText(file,JsonUtility.ToJson(new Report{scope="Actual native read-only clearance witnesses; original gates unchanged. At most24 representative rejected probes and8 real support/overlap hits each; new ground search records total rejection counts and accepted route.",region=region.id,stage=stage,attempts=attempts,regionPosition=region.root.position,failures=probes.ToArray(),allRejectionCounts=counts?.OrderBy(pair=>pair.Key).Select(pair=>new ReasonCount{reason=pair.Key,count=pair.Value}).ToArray()??Array.Empty<ReasonCount>(),acceptedLoop=accepted??Array.Empty<Vector3>()},true));return file;
        }
        static string Path(Transform value){string path=value.name;while(value.parent!=null){value=value.parent;path=value.name+"/"+path;}return path;}
    }
}
#endif
