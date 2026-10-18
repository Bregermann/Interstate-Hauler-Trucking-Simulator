using UnityEngine;using Cinemachine;using LWS.TruckSports;
namespace LWS.TruckTargetLaunch {
// Cinemachine owns composition/dolly. LWS only supplies the owning participant,
// actual target rim anchors, live/replay membership and the sport transition.
public sealed class TargetCameras:MonoBehaviour {
 public TargetMatch match;public CinemachineTargetGroup[] groups,impactGroups;public Transform[] rim,ghostRim;
 public void Present(float dt){bool replaying=match.Stage==TargetStage.Replay;int impact=match.volcano!=null?match.volcano.LastImpactSlot:-1;if(impact>=0&&((replaying&&match.replay.Cursor>=match.volcano.LastImpactTime)||(match.HumanViews==0&&match.Stage==TargetStage.Settling))){Frame(match.broadcast,impactGroups[impact],match.replay.Angle==0?new Vector3(8,6,-12):match.replay.Angle==1?new Vector3(-12,4,6):new Vector3(0,16,-3));return;}if(replaying||match.Stage==TargetStage.RoundResult){var group=groups[4];for(int i=0;i<4;i++){group.m_Targets[i].target=replaying?match.replay.ghosts[i]:match.racers[i].truck.transform;group.m_Targets[i].weight=i<match.setup.count&&(match.rules.Eligible&(1<<i))!=0?1:0;}for(int i=0;i<8;i++){group.m_Targets[i+4].target=replaying?ghostRim[i]:rim[i];group.m_Targets[i+4].weight=1;}Frame(match.broadcast,group,match.replay.Angle==0?new Vector3(30,23,-36):match.replay.Angle==1?new Vector3(-33,16,18):new Vector3(0,48,-5));return;}
  if(match.HumanViews==0){var group=groups[4];bool airborne=false;for(int i=0;i<4;i++){group.m_Targets[i].target=match.racers[i].truck.transform;bool active=i<match.setup.count&&(match.rules.Eligible&(1<<i))!=0&&!match.rules.resolved[i];group.m_Targets[i].weight=active?1:0;airborne|=active&&match.racers[i].Phase!=TargetFlight.Approach;}for(int i=0;i<8;i++){group.m_Targets[i+4].target=rim[i];group.m_Targets[i+4].weight=Mathf.MoveTowards(group.m_Targets[i+4].weight,airborne||match.rules.RoundComplete?1:0,dt*1.8f);}Frame(match.broadcast,group,new Vector3(27,23,-36));return;}
  for(int owner=0;owner<4;owner++)if(match.playerCameras[owner].view.enabled){int subject=owner;if((match.rules.Eligible&(1<<owner))==0)for(int i=0;i<match.setup.count;i++)if((match.rules.Eligible&(1<<i))!=0){subject=i;break;}var racer=match.racers[subject];if(racer.Destroyed){Frame(match.playerCameras[owner],impactGroups[subject],new Vector3(8,6,-12));continue;}var group=groups[owner];group.m_Targets[0].target=racer.truck.transform;group.m_Targets[0].weight=1;bool flying=racer.Phase!=TargetFlight.Approach;for(int i=1;i<9;i++)group.m_Targets[i].weight=Mathf.MoveTowards(group.m_Targets[i].weight,flying?1:0,dt*1.8f);Frame(match.playerCameras[owner],group,flying?new Vector3(0,8,-16):new Vector3(0,4,-10));}
 }
 static void Frame(SportsEventCamera camera,CinemachineTargetGroup group,Vector3 offset){group.DoUpdate();camera.rig.Follow=camera.rig.LookAt=group.transform;camera.rig.m_Lens=LensSettings.FromCamera(camera.view);
 // Keep the initial dolly outside the complete group: an inside-group view
 // drops members behind its eye before Cinemachine can calculate their angular bounds.
 float distance=Mathf.Max(offset.magnitude,group.Sphere.radius*2.5f+8);
 camera.Pose(group.transform.position,offset.normalized*distance,65);}
}
}
