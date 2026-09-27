using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Persistent semantics; the streamed scene holds only scenery and colliders.
    public static class TruckTaxiSpeedwayLayout
    {
        public const string RegionId = "taxi.speedway";
        public const string SceneName = "Taxi_ThunderBowlSpeedway";
        public const string PoiId = "taxi.speedway.poi";
        public const string DisplayName = "Thunder Bowl Speedway";
        public const float TrackWidth = 28f;
        public const float BankingDegrees = 31f;
        public const string LapStopId = "taxi.speedway.lap";
        public const float LapOfferRadiusMeters = 1800f;
        public static readonly Vector3 Center = new Vector3(2800, 0, -780);
        public static readonly Bounds RegionBounds = new Bounds(new Vector3(2800, 0, -700), new Vector3(780, 120, 1000));

        public readonly struct Stop
        {
            public readonly string Id, Name;
            public readonly Vector3 Position;
            public Stop(string id, string name, Vector3 position) { Id = id; Name = name; Position = position; }
        }
        public static readonly Stop[] Stops = {
            new Stop("taxi.speedway.main-gate", "Thunder Bowl Main Gate", new Vector3(2800,.2f,-470)),
            new Stop("taxi.speedway.pit-entrance", "Thunder Bowl Pit Entrance", new Vector3(2800,.2f,-610)),
            new Stop("taxi.speedway.vip-entrance", "Thunder Bowl VIP Entrance", new Vector3(2960,.2f,-480)),
            new Stop("taxi.speedway.infield", "Thunder Bowl Infield", new Vector3(2800,.2f,-780)),
            new Stop("taxi.speedway.parking", "Thunder Bowl Parking", new Vector3(2800,.2f,-540)),
            new Stop("taxi.speedway.garage", "Thunder Bowl Garage", new Vector3(2660,.2f,-570))
        };

        private static Vector3 TrackPoint(float x, float z) => Center + new Vector3(x, 10, z);
        public static List<Vector3> TrackCenterline()
        {
            var points = new List<Vector3>(55);
            const float halfStraight = 120, radius = 95;
            for (int i=0; i<=6; i++) points.Add(TrackPoint(i*20, radius));
            for (int i=1; i<=16; i++) { float a=Mathf.PI*(.5f-i/16f); points.Add(TrackPoint(halfStraight+radius*Mathf.Cos(a),radius*Mathf.Sin(a))); }
            for (int i=1; i<=12; i++) points.Add(TrackPoint(halfStraight-i*20,-radius));
            for (int i=1; i<=16; i++) { float a=Mathf.PI*(-.5f+i/16f); points.Add(TrackPoint(-halfStraight-radius*Mathf.Cos(a),radius*Mathf.Sin(a))); }
            for (int i=1; i<=5; i++) points.Add(TrackPoint(-halfStraight+i*20,radius));
            return points;
        }
        public static Vector3[] LapCheckpoints()
        { var track=TrackCenterline(); return new[] { track[0],track[9],track[18],track[27],track[36],track[45] }; }
        public static bool IsNearForLapOffer(Vector3 position)
        {
            Vector3 delta=position-Center;
            return delta.x*delta.x+delta.z*delta.z<=LapOfferRadiusMeters*LapOfferRadiusMeters;
        }

        // Applied to EasyRoads' baked mesh, including its collider; no second road generator.
        public static float BankOffset(Vector3 position, IReadOnlyList<Vector3> centerline)
        {
            float nearest=float.PositiveInfinity, outward=0;
            for (int i=0; i<centerline.Count; i++)
            {
                Vector3 a=centerline[i], b=centerline[(i+1)%centerline.Count];
                var segment=new Vector2(b.x-a.x,b.z-a.z);
                if (segment.sqrMagnitude<.001f) continue;
                var delta=new Vector2(position.x-a.x,position.z-a.z);
                Vector2 lateral=delta-segment*Mathf.Clamp01(Vector2.Dot(delta,segment)/segment.sqrMagnitude);
                if (lateral.sqrMagnitude>=nearest) continue;
                nearest=lateral.sqrMagnitude;
                outward=Vector2.Dot(lateral,new Vector2(-segment.y,segment.x).normalized);
            }
            return Mathf.Clamp(outward,-TrackWidth*.5f,TrackWidth*.5f)*Mathf.Tan(BankingDegrees*Mathf.Deg2Rad);
        }

        public static void AddToGraph(LwsRoadGraph graph)
        {
            if (graph==null) throw new ArgumentNullException(nameof(graph));
            if (!graph.Validate().IsValid) throw new InvalidOperationException("Existing road graph is invalid.");
            var highway=graph.nodes.Find(n=>n.nodeId=="taxi.regional.2800");
            if (highway==null) throw new InvalidOperationException("Regional highway node taxi.regional.2800 is missing.");
            var gate=Node(graph,"gate",Stops[0].Position); var parking=Node(graph,"parking",Stops[4].Position);
            var pit=Node(graph,"pit",Stops[1].Position); var vip=Node(graph,"vip",Stops[2].Position);
            var garage=Node(graph,"garage",Stops[5].Position); var infield=Node(graph,"infield",Stops[3].Position);
            var approach=Node(graph,"approach",new Vector3(2800,.2f,-250));
            Both(graph,highway,approach,LwsRoadClass.Ramp,25,2,12);
            Both(graph,approach,gate,LwsRoadClass.LocalRoad,25,2,12);
            Both(graph,gate,parking,LwsRoadClass.LocalRoad,15,2,12);
            Both(graph,gate,vip,LwsRoadClass.LocalRoad,15,2,10);
            Both(graph,parking,pit,LwsRoadClass.LocalRoad,15,2,10);
            Both(graph,parking,garage,LwsRoadClass.LocalRoad,15,2,10);
            Both(graph,pit,infield,LwsRoadClass.LocalRoad,12,2,10);
            var track=TrackCenterline(); var nodes=new LwsRoadNode[track.Count];
            for (int i=0;i<nodes.Length;i++) nodes[i]=Node(graph,"track."+i.ToString("00"),track[i]);
            for (int i=0;i<nodes.Length;i++) Edge(graph,nodes[i],nodes[(i+1)%nodes.Length],LwsRoadClass.LocalRoad,45,4,TrackWidth);
            Both(graph,garage,nodes[43],LwsRoadClass.Ramp,20,2,11);
            var result=graph.Validate();
            if (!result.IsValid) throw new InvalidOperationException("Speedway graph invalid: "+result.Summary);
        }
        private static LwsRoadNode Node(LwsRoadGraph graph,string suffix,Vector3 position)
        {
            string id="taxi.speedway."+suffix;
            var node=graph.nodes.Find(n=>n.nodeId==id);
            if (node!=null) return node;
            node=new LwsRoadNode { nodeId=id,position=position,regionId=RegionId,sceneChunkId=SceneName };
            graph.nodes.Add(node); return node;
        }
        private static void Both(LwsRoadGraph graph,LwsRoadNode a,LwsRoadNode b,LwsRoadClass kind,float speed,int lanes,float width)
        { Edge(graph,a,b,kind,speed,lanes,width); Edge(graph,b,a,kind,speed,lanes,width); }
        private static void Edge(LwsRoadGraph graph,LwsRoadNode a,LwsRoadNode b,LwsRoadClass kind,float speed,int lanes,float width)
        {
            string id=a.nodeId+">"+b.nodeId;
            if (graph.edges.Exists(e=>e.edgeId==id)) return;
            float distance=Vector3.Distance(a.position,b.position); Vector3 forward=(b.position-a.position).normalized;
            graph.edges.Add(new LwsRoadEdge { edgeId=id,roadId=id,segmentId=id,fromNodeId=a.nodeId,toNodeId=b.nodeId,
                roadClass=kind,direction=LwsRoadDirection.Bidirectional,surfaceType=LwsRoadSurfaceType.AsphaltInterstate,
                oneWay=true,distanceMeters=distance,travelCost=distance,speedLimitMph=speed,laneCount=lanes,
                laneWidthMeters=width/lanes,stateOrRegionId=RegionId,sceneChunkId=SceneName,
                samples=new List<LwsRoadSample> {
                    new LwsRoadSample { roadId=id,segmentId=id,position=a.position,forward=forward,roadWidthMeters=width,laneCount=lanes,laneWidthMeters=width/lanes,speedLimitMph=speed },
                    new LwsRoadSample { roadId=id,segmentId=id,position=b.position,forward=forward,distanceFromStartMeters=distance,roadWidthMeters=width,laneCount=lanes,laneWidthMeters=width/lanes,speedLimitMph=speed }
                } });
        }
    }

    // The optional-diversion coordinator owns acceptance and reward; this only validates a driven lap.
    public sealed class TruckTaxiSpeedwayLapTracker
    {
        private readonly Vector3[] checkpoints=TruckTaxiSpeedwayLayout.LapCheckpoints();
        private int next;
        public bool IsActive { get; private set; }
        public bool IsComplete { get; private set; }
        public int NextCheckpointIndex => next%checkpoints.Length;
        public Vector3 NextCheckpoint => checkpoints[NextCheckpointIndex];
        public void Begin() { next=0; IsComplete=false; IsActive=true; }
        public void Cancel() { next=0; IsActive=false; IsComplete=false; }
        public bool Tick(Vector3 position)
        {
            if (!IsActive) return false;
            Vector3 delta=position-NextCheckpoint;
            if (new Vector2(delta.x,delta.z).sqrMagnitude>625 || Mathf.Abs(delta.y)>8) return false;
            next++;
            if (next<=checkpoints.Length) return false;
            IsActive=false; IsComplete=true; return true;
        }
    }
}
