using System;
using UnityEngine;
namespace LWS.TruckAvalancheEscape
{
 public sealed class AvalancheWorld:MonoBehaviour
 {
  [Serializable]public sealed class Route{public string name;public Vector3[] points;public bool shortcut;}
  public Terrain terrain;public Route[] routes;public Vector3[] starts;public Quaternion[] startRotations;public Transform finish;public float length=2400,halfWidth=250;
  public float Height(Vector3 point)=>terrain.SampleHeight(point)+terrain.transform.position.y;
  public float Progress(Vector3 p)=>Mathf.Clamp(p.z,0,length);
  public float FrontHeight(float z){var points=routes[0].points;for(int i=1;i<points.Length;i++)if(z<=points[i].z)return Mathf.Lerp(points[i-1].y,points[i].y,Mathf.InverseLerp(points[i-1].z,points[i].z,z));return points[points.Length-1].y;}
  public Vector3 Target(int route,Vector3 p,ref int segment,float lateral=0){var points=routes[route].points;segment=Mathf.Clamp(segment,0,points.Length-1);var goal=Waypoint(points,segment,lateral);while(segment<points.Length-1){var direction=Vector3.ProjectOnPlane(points[segment]-points[Mathf.Max(0,segment-1)],Vector3.up).normalized;if(direction.sqrMagnitude<.5f)direction=Vector3.forward;var delta=Vector3.ProjectOnPlane(p-goal,Vector3.up);if(Vector3.Dot(delta,direction)<-4&&delta.magnitude>=13)break;segment++;goal=Waypoint(points,segment,lateral);}return goal;}
  static Vector3 Waypoint(Vector3[] points,int index,float lateral){var direction=Vector3.ProjectOnPlane(points[index]-points[Mathf.Max(0,index-1)],Vector3.up).normalized;if(direction.sqrMagnitude<.5f)direction=Vector3.forward;return points[index]+Vector3.Cross(Vector3.up,direction)*lateral;}
 }
}
