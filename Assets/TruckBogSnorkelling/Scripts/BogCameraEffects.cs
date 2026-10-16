using UnityEngine;
namespace LWS.TruckBogSnorkelling {
// Camera-local state only. Never changes global fog, light intensity or a shared volume.
public sealed class BogCameraEffects:MonoBehaviour {
 public BogCourse course;public bool Underwater{get;private set;}public int AppliedFrames{get;set;}public int TargetWidth{get;set;}public int TargetHeight{get;set;}public MaterialPropertyBlock Properties{get;private set;}
 public bool Evaluate(Camera camera){if(course==null||!isActiveAndEnabled)return false;float y=camera.transform.position.y,water=course.WaterHeight;if(Underwater){if(y>water+.15f)Underwater=false;}else if(y<water-.15f)Underwater=true;if(Properties==null)Properties=new MaterialPropertyBlock();Properties.SetFloat("_BogSubmersion",Mathf.Max(0,water-y));Properties.SetFloat("_BogClock",Time.time);return Underwater;}
}
}
