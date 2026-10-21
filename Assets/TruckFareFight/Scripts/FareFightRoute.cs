using System.Collections.Generic;
using UnityEngine;
using LWS.InterstateHauler;
namespace LWS.TruckFareFight {
 // LWS's existing graph planner is the only route authority. Each participant
 // keeps its own result; CNP observes it and never steers a competitor.
 public sealed class FareFightRoute {
  readonly LwsRoutePlanner planner;
  public FareFightRoute():this(LwsNavigationTuning.Default()){}
  public FareFightRoute(LwsNavigationTuning tuning){planner=new LwsRoutePlanner(tuning??LwsNavigationTuning.Default());}
  public readonly List<Vector3> points=new List<Vector3>(128);
  readonly RaycastHit[] obstacles=new RaycastHit[32];
  // Sport-specific tuning; defaults preserve all existing Taxi/Sports callers.
  public float speedScale=1,groundNormalThreshold=.6f;
  public int Revision{get;private set;}public int Searches{get;private set;}public int Cursor{get;private set;}
  public Vector3 Goal{get;private set;}public float Distance{get;private set;}
  public float DesiredSpeed{get;private set;}public string BlockingObstacle{get;private set;}
  float nextSearch,stalled,reverseUntil,turnSign=1;Vector3 progressPosition;bool initialized;
  public bool Update(FareFightDistrict district,Vector3 position,Vector3 goal,int revision,float now,bool force=false,Vector3 heading=default)=>Update(district.graph,position,goal,revision,now,force,heading);
  // Reuse the existing planner/driving adapter in authored Sports road graphs.
  // The original Fare Fight district API forwards without changing behavior.
  public bool Update(LwsRoadGraph graph,Vector3 position,Vector3 goal,int revision,float now,bool force=false,Vector3 heading=default){
   bool changed=revision!=Revision||!initialized;
   if(!force&&!changed&&now<nextSearch)return false;
   // Moving carrier routes update at most once per second, static goals every
   // four seconds. Invalidated possession always updates immediately.
   nextSearch=now+(Vector3.Distance(Goal,goal)>12?1.1f:4f);
   if(!changed&&!force&&Vector3.Distance(Goal,goal)<12&&points.Count>1&&OnRoute(position,18))return false;
   Goal=goal;Revision=revision;Searches++;initialized=true;
   string origin=null;float closest=180;heading=Vector3.ProjectOnPlane(heading,Vector3.up).normalized;
   if(heading.sqrMagnitude>.5f)foreach(var node in graph.nodes){var delta=Vector3.ProjectOnPlane(node.position-position,Vector3.up);float along=Vector3.Dot(delta,heading);float lateral=Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(Vector3.up,heading)));if(along>3&&along<closest&&lateral<9){closest=along;origin=node.nodeId;}}
   var result=planner.PlanRoute(new LwsRouteRequest{originNodeId=origin,useOriginWorldPosition=origin==null,originWorldPosition=position,useDestinationWorldPosition=true,destinationWorldPosition=goal,truckRouteRequired=true},graph);
   points.Clear();Cursor=0;
   if(!result.succeeded)result=planner.PlanRoute(new LwsRouteRequest{useOriginWorldPosition=true,originWorldPosition=position,useDestinationWorldPosition=true,destinationWorldPosition=goal,truckRouteRequired=true},graph);
   if(!result.succeeded)return true;
   points.AddRange(result.waypoints);if(points.Count==0||Vector3.Distance(points[points.Count-1],goal)>1)points.Add(goal);
   Distance=result.distanceMeters;return true;
  }
  // A nearby physical-combat target changes frequently. Retargeting must keep
  // the existing driver's measured stall/reverse recovery instead of resetting
  // it every physics step. Road searches remain in Update above.
  public void DirectGoal(Vector3 position,Vector3 goal){points.Clear();points.Add(goal);Cursor=0;Goal=goal;Distance=Vector3.Distance(position,goal);}
  bool OnRoute(Vector3 p,float radius){for(int i=Cursor;i<points.Count;i++)if((points[i]-p).sqrMagnitude<radius*radius)return true;return false;}
  public Vector2 Drive(Rigidbody body,Transform root,int skill,float dt,float grip,Rigidbody contestedCarrier=null,Collider plannedGround=null){
   if(points.Count==0)return new Vector2(0,-1);
   Vector3 position=body.position;float speed=Vector3.Dot(body.linearVelocity,root.forward);
   while(Cursor<points.Count-1&&Vector3.ProjectOnPlane(points[Cursor]-position,Vector3.up).magnitude<Mathf.Max(7,speed*.5f))Cursor++;
   Vector3 delta=Vector3.ProjectOnPlane(points[Cursor]-position,Vector3.up);float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(root.forward,Vector3.up),delta,Vector3.up);
   float steer=Mathf.Clamp(angle/(skill==0?34:42),-1,1);float desired=(skill==0?9:skill==1?12:15)*Mathf.Sqrt(grip)*Mathf.Clamp(speedScale,.5f,1.6f);
   desired*=Mathf.Lerp(.3f,1,1-Mathf.Clamp01(Mathf.Abs(angle)/85));
   if(Cursor<points.Count-1){var next=points[Cursor+1]-points[Cursor];float bend=Mathf.Abs(Vector3.SignedAngle(delta,next,Vector3.up));if(delta.magnitude<28&&bend>35)desired=Mathf.Min(desired,Mathf.Lerp(9,3,Mathf.InverseLerp(35,120,bend)));}
   if(Cursor==points.Count-1)desired=Mathf.Min(desired,Mathf.Max(3,delta.magnitude*.65f));
   int hits=Physics.SphereCastNonAlloc(position+Vector3.up*1.1f,.8f,root.forward,obstacles,Mathf.Max(8,speed*1.1f),~0,QueryTriggerInteraction.Ignore);
   BlockingObstacle="";float nearest=float.PositiveInfinity;for(int i=0;i<hits;i++){var hit=obstacles[i];if(hit.rigidbody==body||hit.collider==plannedGround||contestedCarrier!=null&&hit.collider.transform.IsChildOf(contestedCarrier.transform)||hit.collider.transform.IsChildOf(root)||hit.normal.y>Mathf.Clamp(groundNormalThreshold,.35f,.8f))continue;if(hit.distance<nearest){nearest=hit.distance;BlockingObstacle=hit.collider.name;}}
   if(nearest<Mathf.Max(7,speed*.9f)){desired=Mathf.Min(desired,Mathf.Max(0,(nearest-4)*.8f));if(nearest<6)turnSign=angle==0?1:Mathf.Sign(angle);}
   DesiredSpeed=desired;if(!initialized){progressPosition=position;initialized=true;}
   if(Vector3.Distance(position,progressPosition)>2){progressPosition=position;stalled=0;}else if(body.linearVelocity.magnitude<.5f&&(desired>1||nearest<7))stalled+=dt;
   if(stalled>5f&&Time.time>=reverseUntil){reverseUntil=Time.time+3.2f;stalled=0;}
   if(Time.time<reverseUntil)return new Vector2(-turnSign,-.8f);
   return new Vector2(steer,speed>desired+1?-.5f:speed<desired-.8f?1:.25f);
  }
  public void Reset(){points.Clear();nextSearch=stalled=reverseUntil=0;Revision=-1;initialized=false;Cursor=0;}
 }
}
