using UnityEngine;
using Cinemachine;
namespace LWS.TruckSports {
public sealed class SportsEventCamera:MonoBehaviour {
 public SportsCabSteering cabSteering;public Camera view;public CinemachineVirtualCamera rig;public CinemachineTransposer follow;public Transform target,aim;
 public SportsBroadcastFraming broadcastFraming;
 public bool LegacyPoseAllowed=>broadcastFraming==null||!broadcastFraming.OwnsShot;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 int evaluationFrame=-1;public int FrameEvaluations{get;private set;}
 void ObserveEvaluation(){if(evaluationFrame!=Time.frameCount){evaluationFrame=Time.frameCount;FrameEvaluations=0;}FrameEvaluations++;}
#endif
 public void Pose(Vector3 focus,Vector3 offset,float fov=58,bool snap=false){if(broadcastFraming!=null){broadcastFraming.Request(focus);if(broadcastFraming.OwnsShot)return;broadcastFraming.Release();}PoseRaw(focus,offset,fov,snap);}
 public void PoseRaw(Vector3 focus,Vector3 offset,float fov=58,bool snap=false){
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 ObserveEvaluation();
#endif
 view.fieldOfView=fov;if(broadcastFraming!=null)rig.m_Lens.ModeOverride=view.orthographic?LensSettings.OverrideModes.Orthographic:LensSettings.OverrideModes.Perspective;target.position=focus;aim.position=focus;follow.m_FollowOffset=offset;rig.m_Lens.FieldOfView=fov;rig.InternalUpdateCameraState(Vector3.up,snap?-1:Time.unscaledDeltaTime);transform.SetPositionAndRotation(rig.State.FinalPosition,rig.State.FinalOrientation);if(broadcastFraming!=null){view.fieldOfView=rig.State.Lens.FieldOfView;view.orthographicSize=rig.State.Lens.OrthographicSize;}}
}
}
