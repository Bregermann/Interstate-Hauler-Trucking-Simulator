using System;
using UnityEngine;
using LWS.InterstateHauler;
using LWS.TruckSports;
namespace LWS.TruckSnake {
 public sealed class SnakeWorld:MonoBehaviour {
  [Serializable]public sealed class GroundRoad{public string name;public Vector3[] markers;public float width;}public GroundRoad[] groundRoads;
  [Serializable]public struct RoadPoint{public Vector3 position;public bool hard;}
  public Vector3 jumpLaunch,jumpDirection;public Terrain[] terrain;public LwsRoadGraph graph;public RoadPoint[] pickupPoints;
  public Vector3[] starts;public Quaternion[] rotations;public Bounds playableBounds;
  public float verifiedUsableSquareMetres;public string[] regions;
  readonly Collider[] clearance=new Collider[128];
  public bool Contains(Vector3 p)=>p.x>=playableBounds.min.x&&p.x<=playableBounds.max.x&&p.z>=playableBounds.min.z&&p.z<=playableBounds.max.z&&p.y<180;
  public float Height(Vector3 p){foreach(var tile in terrain){var local=p-tile.transform.position;var size=tile.terrainData.size;if(local.x>=0&&local.x<=size.x&&local.z>=0&&local.z<=size.z)return tile.SampleHeight(p)+tile.transform.position.y;}return -100;}
  public Vector3 ReachablePickup(Vector3 from,Vector3 heading,System.Random random,bool golden){
   Vector3 candidate=Vector3.positiveInfinity;float best=float.PositiveInfinity;float preferred=(float)(random.NextDouble()<.8?24+random.NextDouble()*156:180+random.NextDouble()*470);
   for(int attempt=0;attempt<120;attempt++){var point=pickupPoints[random.Next(pickupPoints.Length)];if(golden&&!point.hard)continue;var delta=Vector3.ProjectOnPlane(point.position-from,Vector3.up);float distance=delta.magnitude;float alignment=Vector3.Dot(delta.normalized,heading);if(distance<24||distance>650||alignment<.15f)continue;float cost=Mathf.Abs(distance-preferred)+distance*(1-alignment)*1.5f+Mathf.Abs(point.position.y-from.y)*8;if(cost>=best)continue;candidate=point.position;best=cost;}
   if(float.IsInfinity(best))return Vector3.positiveInfinity;return candidate;
  }
  public bool PickupClear(Vector3 position,SportsEventTruck[] trucks,SnakeConvoy[] convoys,SnakePickup[] pickups){
   if(float.IsInfinity(position.x)||!Contains(position))return false;
   foreach(var truck in trucks)if(truck.gameObject.activeInHierarchy&&(truck.body.position-position).sqrMagnitude<18*18)return false;
   foreach(var pickup in pickups)if(pickup.Live&&(pickup.transform.position-position).sqrMagnitude<22*22)return false;
   int count=Physics.OverlapBoxNonAlloc(position+Vector3.up*1.6f,new Vector3(2.5f,1.2f,3),clearance,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);if(count==clearance.Length)return false;
   for(int i=0;i<count;i++){var c=clearance[i];if(c.GetComponentInParent<SnakePickup>()!=null)continue;bool support=c is TerrainCollider||c.GetComponent<LwsRoadSurface>()!=null||c.GetComponent<LWS.TruckTaxi.TruckTaxiSurface>() is LWS.TruckTaxi.TruckTaxiSurface surface&&surface.isRoad;if(support){if(c.Raycast(new Ray(position+Vector3.up*8,Vector3.down),out var hit,16)&&hit.point.y>position.y+.6f)return false;continue;}if(c.bounds.max.y>position.y+.6f)return false;}return true;
  }
 }
}
