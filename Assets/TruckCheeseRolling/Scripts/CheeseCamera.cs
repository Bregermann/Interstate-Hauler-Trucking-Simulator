using UnityEngine;
using Cinemachine;
namespace LWS.TruckCheeseRolling {
// Sport semantics select the stable target. Cinemachine owns follow, damping, aim and occlusion.
public sealed class CheeseCamera:MonoBehaviour {
 public CheeseMatch match;public Camera view;public int slot;public Transform target,aim;public CinemachineVirtualCamera rig;public CinemachineTransposer follow;bool snap=true;
 public void ResetView(){snap=true;if(rig!=null)rig.PreviousStateIsValid=false;}
 void LateUpdate(){var t=match.trucks[Mathf.Min(slot,match.setup.count-1)];var p=t.body.position;var path=match.cheese.body.position-p;path.y=0;var heading=path.sqrMagnitude>64?path.normalized:target.forward;if(heading.sqrMagnitude<.1f)heading=Vector3.forward;target.position=p;target.rotation=Quaternion.Slerp(target.rotation,Quaternion.LookRotation(heading,Vector3.up),snap?1:1-Mathf.Exp(-Time.unscaledDeltaTime*4));aim.position=p+Vector3.up*.7f+target.forward*5;float distance=Mathf.Lerp(8,12,Mathf.Clamp01(t.body.linearVelocity.magnitude/35));Vector3 behind=p-target.forward*distance;float rise=Mathf.Max(0,CheeseRules.Height(behind.x,behind.z)-p.y);follow.m_FollowOffset=new Vector3(0,6+rise,-distance);rig.InternalUpdateCameraState(Vector3.up,snap?-1:Time.unscaledDeltaTime);var position=rig.State.FinalPosition;position.y=Mathf.Max(position.y,CheeseRules.Height(position.x,position.z)+1);transform.SetPositionAndRotation(position,rig.State.FinalOrientation);snap=false;
 }
}
}
