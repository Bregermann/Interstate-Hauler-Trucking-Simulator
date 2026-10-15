using UnityEngine;
using UnityEngine.Rendering;
namespace LWS.TruckCornhole {
// Stable driver-seat view. Replay selection never changes a live body.
public sealed class CornholeCamera:MonoBehaviour {
 public CornholeCourse course;public Camera view;public Renderer[] interior;
 public float Yaw{get;private set;}float pitch;bool snap=true;
 public void NewAttempt(){snap=true;}
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering+=End;}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.endCameraRendering-=End;Restore();}
 void Begin(ScriptableRenderContext context,Camera camera){if(camera!=view)return;bool replay=course.replay!=null&&course.replay.Playing;bool cab=!replay||course.replay.Angle==0;foreach(var t in course.trucks){if(t==null)continue;foreach(var r in t.Visuals)if(r!=null)r.forceRenderingOff=replay||cab&&t==course.Current;}foreach(var r in interior)r.forceRenderingOff=!cab;if(replay)course.replay.ShowGhosts(cab);}
 void End(ScriptableRenderContext context,Camera camera){if(camera==view)Restore();}
 void Restore(){if(course==null)return;foreach(var t in course.trucks){if(t==null)continue;foreach(var r in t.Visuals)if(r!=null)r.forceRenderingOff=false;}if(interior!=null)foreach(var r in interior)if(r!=null)r.forceRenderingOff=false;}
 void LateUpdate(){if(course==null||!view.enabled)return;var replay=course.replay;if(replay!=null&&replay.Playing){replay.PositionCamera(this);return;}var truck=course.Current;if(truck==null){transform.SetPositionAndRotation(course.StartPosition+Vector3.up*.43f,Quaternion.Euler(13.5f,0,0));return;}Cab(truck.body.position,truck.body.rotation,truck.Launched,Time.deltaTime);}
 public void Cab(Vector3 position,Quaternion rotation,bool flight,float dt){float desiredYaw=flight?Mathf.Clamp(Mathf.Atan2(course.center-position.x,Mathf.Max(3,course.board.position.z-position.z))*Mathf.Rad2Deg,-35,35):Mathf.DeltaAngle(0,rotation.eulerAngles.y);float desiredPitch=flight?Mathf.Clamp(-Mathf.Atan2(course.board.position.y-position.y,Mathf.Max(6,course.board.position.z-position.z))*Mathf.Rad2Deg,-25,65):13.5f;if(snap){Yaw=desiredYaw;pitch=desiredPitch;snap=false;}else{Yaw=Mathf.MoveTowardsAngle(Yaw,desiredYaw,90*dt);pitch=Mathf.MoveTowards(pitch,desiredPitch,45*dt);}transform.SetPositionAndRotation(position+Vector3.up*.43f+Quaternion.Euler(0,Yaw,0)*Vector3.forward*.05f,Quaternion.Euler(pitch,Yaw,0));}
}
}
