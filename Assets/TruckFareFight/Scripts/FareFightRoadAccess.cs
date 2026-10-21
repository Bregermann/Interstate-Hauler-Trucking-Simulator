using System;
using System.Collections.Generic;
using UnityEngine;
using LWS.InterstateHauler;

namespace LWS.TruckFareFight
{
    // Sport-owned access legs over the existing authored road samples.
    // The established LWS planner still owns every network path. This adapter
    // connects its endpoint routes to actual truck/passenger positions without
    // aiming diagonally across a city block at a distant intersection.
    public sealed class FareFightRoadAccess
    {
        readonly LwsRoutePlanner planner;
        readonly List<Vector3> candidate=new List<Vector3>(256);
        readonly List<LwsRoadLookupResult> starts=new List<LwsRoadLookupResult>(2),finishes=new List<LwsRoadLookupResult>(2);
        readonly LwsRoadGraph lookupGraph=new LwsRoadGraph();
        public int NetworkSearches{get;private set;}
        public FareFightRoadAccess(LwsRoutePlanner planner){this.planner=planner??throw new ArgumentNullException(nameof(planner));}
        static LwsRoadEdge Edge(LwsRoadGraph graph,string id){foreach(var edge in graph.edges)if(edge.edgeId==id)return edge;return null;}
        static bool Allowed(LwsRoadEdge edge)
        {
            if(edge==null||edge.samples==null||edge.samples.Count<2)return false;
            if(edge.restrictions!=null)foreach(var restriction in edge.restrictions)if(restriction!=null&&!restriction.truckAllowed)return false;
            return true;
        }
        static void Add(List<Vector3> points,Vector3 point){if(points.Count==0||(points[points.Count-1]-point).sqrMagnitude>.0001f)points.Add(point);}
        static float End(LwsRoadEdge edge,int side)=>edge.samples[side==0?0:edge.samples.Count-1].distanceFromStartMeters;
        static Vector3 EndPoint(LwsRoadEdge edge,int side)=>edge.samples[side==0?0:edge.samples.Count-1].position;
        static string EndId(LwsRoadEdge edge,int side)=>side==0?edge.fromNodeId:edge.toNodeId;
        static void Leg(List<Vector3> points,LwsRoadEdge edge,float from,float to,Vector3 start,Vector3 finish)
        {
            Add(points,start);
            if(to>=from){foreach(var sample in edge.samples)if(sample.distanceFromStartMeters>from&&sample.distanceFromStartMeters<to)Add(points,sample.position);}
            else for(int i=edge.samples.Count-1;i>=0;i--){var sample=edge.samples[i];if(sample.distanceFromStartMeters<from&&sample.distanceFromStartMeters>to)Add(points,sample.position);}
            Add(points,finish);
        }
        static float Length(List<Vector3> points){float result=0;for(int i=1;i<points.Count;i++)result+=Vector3.Distance(points[i-1],points[i]);return result;}
        void Directions(LwsRoadGraph graph,LwsRoadLookupResult nearest,List<LwsRoadLookupResult> options)
        {
            options.Clear();options.Add(nearest);var original=Edge(graph,nearest.EdgeId);
            // Taxi's two-way streets are authored as coincident, opposing
            // one-way edges. A nearest-road tie must not discard the legal
            // opposite direction and force a lap around the block.
            foreach(var edge in graph.edges)
            {
                if(edge==original||!Allowed(edge)||edge.fromNodeId!=original.toNodeId||edge.toNodeId!=original.fromNodeId)continue;
                lookupGraph.edges.Clear();lookupGraph.edges.Add(edge);
                if(LwsRoadGraphQuery.TryFindNearestRoad(lookupGraph,nearest.NearestPosition,.25f,out var reverse))options.Add(reverse);
            }
        }
        public bool TryBuild(LwsRoadGraph graph,Vector3 origin,Vector3 destination,List<Vector3> output,out float distance)
        {
            output.Clear();distance=float.PositiveInfinity;
            if(!LwsRoadGraphQuery.TryFindNearestRoad(graph,origin,150,out var start)||!LwsRoadGraphQuery.TryFindNearestRoad(graph,destination,150,out var finish))return false;
            if(!Allowed(Edge(graph,start.EdgeId))||!Allowed(Edge(graph,finish.EdgeId)))return false;
            Directions(graph,start,starts);Directions(graph,finish,finishes);
            foreach(var startDirection in starts)foreach(var finishDirection in finishes)
            {
            var from=Edge(graph,startDirection.EdgeId);var to=Edge(graph,finishDirection.EdgeId);
            if(from.edgeId==to.edgeId&&(!from.oneWay||finishDirection.DistanceAlongSegmentMeters>=startDirection.DistanceAlongSegmentMeters))
            {
                if(from.oneWay&&finishDirection.DistanceAlongSegmentMeters<startDirection.DistanceAlongSegmentMeters)continue;
                candidate.Clear();Add(candidate,origin);Leg(candidate,from,startDirection.DistanceAlongSegmentMeters,finishDirection.DistanceAlongSegmentMeters,startDirection.NearestPosition,finishDirection.NearestPosition);Add(candidate,destination);float direct=Length(candidate);
                if(direct<distance){distance=direct;output.Clear();output.AddRange(candidate);}continue;
            }
            for(int exit=0;exit<2;exit++)
            {
                if(from.oneWay&&exit==0)continue;
                for(int entry=0;entry<2;entry++)
                {
                    if(to.oneWay&&entry==1)continue;
                    NetworkSearches++;
                    var route=planner.PlanRoute(new LwsRouteRequest{originNodeId=EndId(from,exit),destinationNodeId=EndId(to,entry),truckRouteRequired=true},graph);
                    if(!route.succeeded)continue;
                    candidate.Clear();Add(candidate,origin);
                    Leg(candidate,from,startDirection.DistanceAlongSegmentMeters,End(from,exit),startDirection.NearestPosition,EndPoint(from,exit));
                    foreach(var point in route.waypoints)Add(candidate,point);
                    Leg(candidate,to,End(to,entry),finishDirection.DistanceAlongSegmentMeters,EndPoint(to,entry),finishDirection.NearestPosition);Add(candidate,destination);
                    float cost=Length(candidate);
                    if(cost<distance){distance=cost;output.Clear();output.AddRange(candidate);}
                }
            }
            }
            return output.Count>1;
        }
    }
}
