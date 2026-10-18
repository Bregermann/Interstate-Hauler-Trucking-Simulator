using UnityEngine;
using UnityEngine.AI;
namespace LWS.TruckPedestrianPlowing {
 // Bounded crowd traversal over the existing NavMeshAgent; truck impacts still own ragdoll/scoring.
 public sealed class PlowBonusEvasion:MonoBehaviour {
  PlowPedestrian pedestrian;Vector3 from,to,ground,velocity;float age,nextAttempt;int phase;Collider perch;
  [Range(4,15)]public float cooldownSeconds=7;
  [Range(.4f,1.5f)]public float perchSeconds=1.1f;
  public int JumpCount{get;private set;}public int ClimbCount{get;private set;}
  public bool Active=>phase!=0;public Vector3 Velocity=>velocity;
  void Awake(){pedestrian=GetComponent<PlowPedestrian>();}
  public void ResetMotion(){phase=0;velocity=Vector3.zero;nextAttempt=Time.time+2+(pedestrian.id%7);JumpCount=ClimbCount=0;if(pedestrian.agent!=null)pedestrian.agent.updatePosition=true;}
  public bool TryEscape(Vector3 away){
   if(Active||pedestrian.Claimed||pedestrian.role!=PlowTargetKind.BonusRunner||Time.time<nextAttempt||away.sqrMagnitude<.01f)return false;
   nextAttempt=Time.time+cooldownSeconds+(pedestrian.id%4);var agent=pedestrian.agent;var match=pedestrian.match;
   Vector3 direction=away.normalized;ground=transform.position;
   if(!match.TryRoute(ground,ground+direction*1.8f,out var exit))return false;
   from=ground;to=exit;perch=null;
   if(Physics.SphereCast(ground+Vector3.up*.8f,.3f,(exit-ground).normalized,out _,Vector3.Distance(exit,ground),1,QueryTriggerInteraction.Ignore))return false;
   foreach(var surface in match.climbSurfaces){
    if(surface==null||!surface.enabled||!surface.gameObject.activeInHierarchy)continue;var bounds=surface.bounds;
    Vector3 top=new Vector3(bounds.center.x,bounds.max.y+.04f,bounds.center.z);float height=top.y-ground.y;
    if(height<.45f||height>1.75f||Vector3.ProjectOnPlane(top-ground,Vector3.up).sqrMagnitude>7||bounds.size.x<1.2f||bounds.size.z<1.2f)continue;
    if(surface.attachedRigidbody!=null&&surface.attachedRigidbody.linearVelocity.sqrMagnitude>.25f)continue;
    if(Physics.Linecast(ground+Vector3.up*1.4f,top+Vector3.up*1.4f,out var obstruction,1,QueryTriggerInteraction.Ignore)&&obstruction.collider!=surface)continue;
    if(Physics.CheckCapsule(top+Vector3.up*.5f,top+Vector3.up*1.3f,.35f,1,QueryTriggerInteraction.Ignore))continue;
    perch=surface;to=top;break;
   }
   agent.isStopped=true;agent.updatePosition=false;phase=1;age=0;JumpCount++;if(perch!=null)ClimbCount++;return true;
  }
  public bool Step(float dt){
   if(!Active)return false;
   if(pedestrian.Claimed){phase=0;velocity=Vector3.zero;return false;}
   age+=dt;Vector3 previous=transform.position;
   if(phase==2){
    if(age<perchSeconds&&perch!=null&&perch.enabled&&(perch.attachedRigidbody==null||perch.attachedRigidbody.linearVelocity.sqrMagnitude<4))return true;
    from=transform.position;to=ground;age=0;phase=3;
   }
   float t=Mathf.Clamp01(age/.68f);Vector3 position=Vector3.Lerp(from,to,t)+Vector3.up*(4*.65f*t*(1-t));
   transform.position=position;velocity=(position-previous)/Mathf.Max(.001f,dt);
   if(t>=1){if(phase==1&&perch!=null){phase=2;age=0;velocity=Vector3.zero;}else{phase=0;var agent=pedestrian.agent;agent.Warp(to);agent.updatePosition=true;agent.isStopped=false;velocity=Vector3.zero;}}
   return true;
  }
 }
}
