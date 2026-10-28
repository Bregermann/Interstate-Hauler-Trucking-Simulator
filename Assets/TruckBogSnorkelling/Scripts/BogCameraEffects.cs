using UnityEngine;
namespace LWS.TruckBogSnorkelling {
// Camera-local state only. Never changes global fog, light intensity or a shared volume.
public sealed class BogCameraEffects:MonoBehaviour {
 public BogCourse course;public Transform waterSurface;public Bounds waterBounds;[Min(.1f)]public float densityScale=1;public bool Underwater{get;private set;}public int AppliedFrames{get;set;}public int TargetWidth{get;set;}public int TargetHeight{get;set;}public MaterialPropertyBlock Properties{get;private set;}
 public bool Evaluate(Camera camera){if(!isActiveAndEnabled||course==null&&waterSurface==null)return false;if(course==null){var p=camera.transform.position;if(p.x<waterBounds.min.x||p.x>waterBounds.max.x||p.z<waterBounds.min.z||p.z>waterBounds.max.z){Underwater=false;return false;}}float y=camera.transform.position.y,water=course!=null?course.WaterHeight:waterSurface.position.y;if(Underwater){if(y>water+.15f)Underwater=false;}else if(y<water-.15f)Underwater=true;if(Properties==null)Properties=new MaterialPropertyBlock();Properties.SetFloat("_BogSubmersion",Mathf.Max(0,water-y));Properties.SetFloat("_BogClock",Time.time);Properties.SetFloat("_BogDensityScale",float.IsFinite(densityScale)?Mathf.Clamp(densityScale,.1f,2):1);return Underwater;}
}
}
