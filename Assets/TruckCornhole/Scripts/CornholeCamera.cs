using UnityEngine;
using UnityEngine.Rendering;
using LWS.TruckSports;
namespace LWS.TruckCornhole {
// Stable driver-seat view. Replay selection never changes a live body.
public sealed class CornholeCamera:MonoBehaviour {
 public CornholeCourse course;public Camera view;public Renderer[] interior;
 public LWS.TruckSports.SportsTruckAppearanceCabBinding appearanceCab;
 public void PrepareAppearance(){if(appearanceCab==null)return;appearanceCab.SetOwner(course.Current!=null?course.Current.appearanceHost:null);interior=appearanceCab.GetComponentsInChildren<Renderer>(true);}
 public float Yaw{get;private set;}float pitch;bool snap=true;
 CornholeTruck flightTruck;int flightRevision;Renderer[] flightMeshes;bool[] flightBody;Vector3[] flightPoints;
 bool Spectating{get{if(course==null||LWS.TruckSports.SportsCameraOptions.ShowAI)return false;for(int i=0;i<course.match.setup.count;i++)if(course.match.setup.players[i].source!=LWS.TruckBowling.BowlingSource.AI)return false;return true;}}
 public void NewAttempt(){snap=true;flightTruck=null;}
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering+=End;}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.endCameraRendering-=End;Restore();}
 void Begin(ScriptableRenderContext context,Camera camera){if(camera!=view)return;bool replay=course.replay!=null&&course.replay.Playing;bool cab=replay?course.replay.Angle==0:!Spectating;foreach(var owner in course.match.courses)foreach(var t in owner.trucks){if(t==null)continue;foreach(var r in t.Visuals)if(r!=null)r.forceRenderingOff=replay||cab&&t==course.Current;}foreach(var r in interior)if(r!=null)r.forceRenderingOff=!cab;if(replay)course.replay.ShowGhosts(cab);}
 void End(ScriptableRenderContext context,Camera camera){if(camera==view)Restore();}
 void Restore(){if(course==null)return;foreach(var owner in course.match.courses)foreach(var t in owner.trucks){if(t==null)continue;foreach(var r in t.Visuals)if(r!=null)r.forceRenderingOff=false;}if(interior!=null)foreach(var r in interior)if(r!=null)r.forceRenderingOff=false;}
 void LateUpdate(){if(course==null||!view.enabled)return;var replay=course.replay;if(replay!=null&&replay.Playing){replay.PositionCamera(this);return;}var truck=course.Current;if(truck==null){transform.SetPositionAndRotation(course.StartPosition+Vector3.up*.43f,course.ApproachRotation*Quaternion.Euler(13.5f,0,0));return;}if(Spectating){if(truck.Launched&&!truck.Landed&&!truck.Holed&&!truck.Excluded&&FrameSpectatorFlight(truck))return;var p=truck.body.position;var focus=Vector3.Lerp(p+Vector3.up,course.board.position+Vector3.up*1.3f,truck.Launched?.35f:0);var offset=course.ApproachDirection(new Vector3(8,8,-14-Mathf.Min(14,Mathf.Abs(course.ApproachLocalDirection(course.board.position-p).z)*.15f)));transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset,Vector3.up));return;}Cab(truck.body.position,truck.body.rotation,truck.Launched,Time.deltaTime);}
 // Only the original all-AI spectator participates. Human/split cabs and
 // recorded replay keep their original paths and designer lens/projection.
 bool FrameSpectatorFlight(CornholeTruck truck){
  var host=truck.appearanceHost;if(host==null||!host.Applied)return false;
  if(flightTruck!=truck||flightRevision!=host.AppearanceRevision){
   flightTruck=truck;flightRevision=host.AppearanceRevision;flightMeshes=host.AppearanceRoot.GetComponentsInChildren<Renderer>(true);flightBody=new bool[flightMeshes.Length];flightPoints=new Vector3[flightMeshes.Length*8];
   for(int i=0;i<flightMeshes.Length;i++){var tag=flightMeshes[i].GetComponentInParent<SportsTruckAppearanceMountTag>();flightBody[i]=tag==null||tag.slotId!=SportsTruckAppearanceSlots.Wheels;}
  }
  Bounds bounds=default;bool found=false;int count=0;
  for(int i=0;i<flightMeshes.Length;i++){
   var mesh=flightMeshes[i];if(mesh==null||!(mesh is MeshRenderer||mesh is SkinnedMeshRenderer)||!mesh.enabled||!mesh.gameObject.activeInHierarchy||mesh.transform.lossyScale.sqrMagnitude<=1e-8f||mesh.bounds.size.sqrMagnitude<=1e-8f)continue;
   if(!found){bounds=mesh.bounds;found=true;}else bounds.Encapsulate(mesh.bounds);if(!flightBody[i])continue;
   var local=mesh.localBounds;var matrix=mesh.localToWorldMatrix;for(int corner=0;corner<8;corner++)flightPoints[count++]=matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
  }
  if(count==0)return false;
  var focus=bounds.center;var board=course.board.GetComponent<MeshCollider>();var target=board!=null&&board.sharedMesh!=null?board.transform.TransformPoint(board.sharedMesh.bounds.center):course.board.position;
  // The old eye followed a point35% toward a distant board: near launch it
  // passed beside the projectile, then looked below it at the apex. Keep the
  // eye relative to the actual primary and bisect the two viewing directions.
  var offset=course.ApproachDirection(new Vector3(4,6,-10));var direction=offset.normalized;
  const float readable=.04f;
  float distance=Mathf.Min(offset.magnitude,SportsBroadcastFraming.MaximumReadableCornersDistance(flightPoints,count,focus,Quaternion.LookRotation(-direction,Vector3.up),view.fieldOfView,readable)*.95f);
  Vector3 eye=focus+direction*distance;Quaternion rotation=Quaternion.identity;
  for(int step=0;step<5;step++){
   eye=focus+direction*distance;var aim=(focus-eye).normalized+(target-eye).normalized;if(aim.sqrMagnitude<1e-6f)aim=focus-eye;rotation=Quaternion.LookRotation(aim,Vector3.up);
   // Recheck the real renderer corners after angular composition. Existing
   // broadcast math sets the initial readability bound; this local dolly
   // accounts for the primary being deliberately off the optical center.
   var inverse=Quaternion.Inverse(rotation);float low=float.PositiveInfinity,high=float.NegativeInfinity;float tangent=Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f);
   for(int point=0;point<count;point++){var local=inverse*(flightPoints[point]-eye);if(local.z<=view.nearClipPlane)return false;float y=local.y/(2*local.z*tangent);low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
   float height=high-low;if(height>=readable*1.05f||step==4)break;distance*=Mathf.Clamp(height/(readable*1.05f),.5f,.95f);
  }
  transform.SetPositionAndRotation(eye,rotation);return true;
 }
 public void Cab(Vector3 position,Quaternion rotation,bool flight,float dt){var relative=course.ApproachLocalDirection(course.board.position-position);float desiredYaw=flight?course.ApproachRotation.eulerAngles.y+Mathf.Clamp(Mathf.Atan2(relative.x,Mathf.Max(3,relative.z))*Mathf.Rad2Deg,-35,35):Mathf.DeltaAngle(0,rotation.eulerAngles.y);float desiredPitch=flight?Mathf.Clamp(-Mathf.Atan2(relative.y,Mathf.Max(6,relative.z))*Mathf.Rad2Deg,-25,65):13.5f;if(snap){Yaw=desiredYaw;pitch=desiredPitch;snap=false;}else{Yaw=Mathf.MoveTowardsAngle(Yaw,desiredYaw,90*dt);pitch=Mathf.MoveTowards(pitch,desiredPitch,45*dt);}transform.SetPositionAndRotation(position+Vector3.up*.43f+Quaternion.Euler(0,Yaw,0)*Vector3.forward*.05f,Quaternion.Euler(pitch,Yaw,0));}
}
}
