using UnityEngine;
using Cinemachine;
namespace LWS.TruckSports {
public sealed class SportsEventCamera:MonoBehaviour {
 public SportsCabSteering cabSteering;public Camera view;public CinemachineVirtualCamera rig;public CinemachineTransposer follow;public Transform target,aim;
 public void Pose(Vector3 focus,Vector3 offset,float fov=58,bool snap=false){view.fieldOfView=fov;target.position=focus;aim.position=focus;follow.m_FollowOffset=offset;rig.m_Lens.FieldOfView=fov;rig.InternalUpdateCameraState(Vector3.up,snap?-1:Time.unscaledDeltaTime);transform.SetPositionAndRotation(rig.State.FinalPosition,rig.State.FinalOrientation);}
}
}
