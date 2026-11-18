using UnityEngine;
namespace LWS.TruckWaterSports {
// Course authoring owns the flow field; Archimeds remains the hydrostatic solver.
[System.Serializable]
public sealed class WaterCurrentSection {
 public string name="River current section";
 [Min(0)]public int firstSegment,lastSegment;
 [Range(0,2)]public float speedMultiplier=1,turbulenceMultiplier=1;
 [Min(0)]public float edgeBlendMetres=6;
}
public sealed class WaterCourse : MonoBehaviour {
 public WaterCurrentSection[] currentSections=System.Array.Empty<WaterCurrentSection>();
 public Vector2 CurrentMultipliers(Vector3 point)=>lake?Vector2.one:SectionFactors(point,Nearest(point));
 Vector2 SectionFactors(Vector3 point,int segment){
  if(currentSections==null)return Vector2.one;
  foreach(var section in currentSections){
   if(section==null||segment<section.firstSegment||segment>section.lastSegment)continue;
   int first=Mathf.Clamp(section.firstSegment,0,stations.Length-2),last=Mathf.Clamp(section.lastSegment,first,stations.Length-2);
   float total=0,progress=0;
   for(int i=first;i<=last;i++){
    Vector3 d=stations[i+1]-stations[i];float length=d.magnitude;
    if(i<segment)progress+=length;
    else if(i==segment&&d.sqrMagnitude>.0001f)progress+=length*Mathf.Clamp01(Vector3.Dot(point-stations[i],d)/d.sqrMagnitude);
    total+=length;
   }
   float blend=section.edgeBlendMetres<=.001f?1:Mathf.Clamp01(Mathf.Min(progress,total-progress)/section.edgeBlendMetres);
   return new Vector2(Mathf.Lerp(1,Mathf.Clamp(section.speedMultiplier,0,2),blend),Mathf.Lerp(1,Mathf.Clamp(section.turbulenceMultiplier,0,2),blend));
  }
  return Vector2.one;
 }

 public Vector3[] stations;public Vector3[] eddies=System.Array.Empty<Vector3>();public float eddyStrength=3; public float width=28,currentSpeed=5,turbulence=.7f; public bool lake;
 public Vector3 Point(int gate)=>stations[Mathf.Clamp(gate,0,stations.Length-1)];
 public int Nearest(Vector3 p){int result=0;float best=float.MaxValue;for(int i=0;i<stations.Length-1;i++){var a=stations[i];var d=stations[i+1]-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);float cost=(p-a-d*t).sqrMagnitude;if(cost<best){best=cost;result=i;}}return result;}
 public Vector3 SurfacePoint(Vector3 p){int i=Nearest(p);var a=stations[i];var d=stations[i+1]-a;return a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);}
 public Vector3 Current(Vector3 p,float clock){if(lake)return new Vector3(Mathf.Sin(clock*.18f),0,.35f)*.3f;int i=Nearest(p);var direction=(stations[i+1]-stations[i]).normalized;var section=SectionFactors(p,i);float sway=Mathf.Sin(p.z*.07f+clock)*turbulence*section.y;var flow=direction*(currentSpeed*section.x)+new Vector3(sway,0,-sway*.2f);foreach(var center in eddies){var radial=Vector3.ProjectOnPlane(p-center,Vector3.up);float falloff=Mathf.Clamp01(1-radial.magnitude/14);if(radial.sqrMagnitude>.1f)flow+=(Vector3.Cross(Vector3.up,radial.normalized)-radial.normalized*.35f)*eddyStrength*falloff*section.y;}return flow;}
}
}
