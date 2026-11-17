using UnityEngine;
namespace LWS.TruckWaterSports {
// Course authoring owns the flow field; Archimeds remains the hydrostatic solver.
public sealed class WaterCourse : MonoBehaviour {
 public Vector3[] stations;public Vector3[] eddies=System.Array.Empty<Vector3>();public float eddyStrength=3; public float width=28,currentSpeed=5,turbulence=.7f; public bool lake;
 public Vector3 Point(int gate)=>stations[Mathf.Clamp(gate,0,stations.Length-1)];
 public int Nearest(Vector3 p){int result=0;float best=float.MaxValue;for(int i=0;i<stations.Length-1;i++){var a=stations[i];var d=stations[i+1]-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);float cost=(p-a-d*t).sqrMagnitude;if(cost<best){best=cost;result=i;}}return result;}
 public Vector3 SurfacePoint(Vector3 p){int i=Nearest(p);var a=stations[i];var d=stations[i+1]-a;return a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);}
 public Vector3 Current(Vector3 p,float clock){if(lake)return new Vector3(Mathf.Sin(clock*.18f),0,.35f)*.3f;int i=Nearest(p);var direction=(stations[i+1]-stations[i]).normalized;float sway=Mathf.Sin(p.z*.07f+clock)*turbulence;var flow=direction*currentSpeed+new Vector3(sway,0,-sway*.2f);foreach(var center in eddies){var radial=Vector3.ProjectOnPlane(p-center,Vector3.up);float falloff=Mathf.Clamp01(1-radial.magnitude/14);if(radial.sqrMagnitude>.1f)flow+=(Vector3.Cross(Vector3.up,radial.normalized)-radial.normalized*.35f)*eddyStrength*falloff;}return flow;}
}
}
