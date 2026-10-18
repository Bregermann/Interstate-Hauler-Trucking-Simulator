using UnityEngine;
using UnityEngine.Rendering;
namespace LWS.TruckCornhole {
// Stable driver-seat view. Replay selection never changes a live body.
public sealed class CornholeCamera:MonoBehaviour {
 public CornholeCourse course;public Camera view;public Renderer[] interior;
 public float Yaw{get;private set;}float pitch;bool snap=true;
 bool Spectating{get{if(course==null||LWS.TruckSports.SportsCameraOptions.ShowAI)return false;for(int i=0;i<course.match.setup.count;i++)if(course.match.setup.players[i].source!=LWS.TruckBowling.BowlingSource.AI)return false;return true;}}
 public void NewAttempt(){snap=true;}
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering+=End;}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.endCameraRendering-=End;Restore();}
 void Begin(ScriptableRenderContext context,Camera camera){if(camera!=view)return;bool replay=course.replay!=null&&course.replay.Playing;bool cab=replay?course.replay.Angle==0:!Spectating;foreach(var t in course.trucks){if(t==null)continue;foreach(var r in t.Visuals)if(r!=null)r.forceRenderingOff=replay||cab&&t==course.Current;}foreach(var r in interior)r.forceRenderingOff=!cab;if(replay)course.replay.ShowGhosts(cab);}
 void End(ScriptableRenderContext context,Camera camera){if(camera==view)Restore();}
 void Restore(){if(course==null)return;foreach(var t in course.trucks){if(t==null)continue;foreach(var r in t.Visuals)if(r!=null)r.forceRenderingOff=false;}if(interior!=null)foreach(var r in interior)if(r!=null)r.forceRenderingOff=false;}
 void LateUpdate(){if(course==null||!view.enabled)return;var replay=course.replay;if(replay!=null&&replay.Playing){replay.PositionCamera(this);return;}var truck=course.Current;if(truck==null){transform.SetPositionAndRotation(course.StartPosition+Vector3.up*.43f,Quaternion.Euler(13.5f,0,0));return;}if(Spectating){var p=truck.body.position;var focus=Vector3.Lerp(p+Vector3.up,course.board.position+Vector3.up*1.3f,truck.Launched?.35f:0);var offset=new Vector3(8,8,-14-Mathf.Min(14,Mathf.Abs(course.board.position.z-p.z)*.15f));transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset,Vector3.up));return;}Cab(truck.body.position,truck.body.rotation,truck.Launched,Time.deltaTime);}
 public void Cab(Vector3 position,Quaternion rotation,bool flight,float dt){float desiredYaw=flight?Mathf.Clamp(Mathf.Atan2(course.center-position.x,Mathf.Max(3,course.board.position.z-position.z))*Mathf.Rad2Deg,-35,35):Mathf.DeltaAngle(0,rotation.eulerAngles.y);float desiredPitch=flight?Mathf.Clamp(-Mathf.Atan2(course.board.position.y-position.y,Mathf.Max(6,course.board.position.z-position.z))*Mathf.Rad2Deg,-25,65):13.5f;if(snap){Yaw=desiredYaw;pitch=desiredPitch;snap=false;}else{Yaw=Mathf.MoveTowardsAngle(Yaw,desiredYaw,90*dt);pitch=Mathf.MoveTowards(pitch,desiredPitch,45*dt);}transform.SetPositionAndRotation(position+Vector3.up*.43f+Quaternion.Euler(0,Yaw,0)*Vector3.forward*.05f,Quaternion.Euler(pitch,Yaw,0));}
}
}
