using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckHideAndSeek
{
 public sealed class HidePerception:MonoBehaviour
 {
  public HideMatch match;public readonly HideKnowledge knowledge=new HideKnowledge();
  readonly RaycastHit[] hits=new RaycastHit[96];float nextScan,nextClue;
  public int VisibleObservations{get;private set;}public int AudibleObservations{get;private set;}public int BlockedObservations{get;private set;}
  public void ResetKnowledge(){knowledge.Reset();nextScan=0;nextClue=30;VisibleObservations=AudibleObservations=BlockedObservations=0;}
  public bool Occluded(SportsEventTruck observer,SportsEventTruck target)
  {
   var from=observer.body.position+Vector3.up*2.3f;var to=target.body.position+Vector3.up*2.3f;var delta=to-from;
   for(int direction=0;direction<2;direction++){int count=Physics.RaycastNonAlloc(direction==0?from:to,direction==0?delta.normalized:-delta.normalized,hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);if(count==hits.Length)return true;for(int i=0;i<count;i++){var c=hits[i].collider;if(c.transform.IsChildOf(observer.transform)||c.transform.IsChildOf(target.transform))continue;return true;}}return false;
  }
  public bool Visible(int observer,int target)
  {
   var a=match.trucks[observer];var b=match.trucks[target];var delta=b.body.position-a.body.position;float facing=Vector3.Dot(a.transform.forward,delta.normalized);
   return HideSignalRules.CanSee(delta.magnitude,facing,Occluded(a,b),match.engines[target].Running,match.engines[target].Lights,b.body.linearVelocity.magnitude>1,match.engines[observer].Lights,match.Visibility);
  }
  public void Step(float now)
  {
   if(now<nextScan||match.rules.Phase!=HidePhase.Seeking)return;nextScan=now+.2f;int seeker=match.rules.Seeker;var source=match.trucks[seeker];float loudest=0;int heard=-1;
   for(int i=0;i<match.setup.count;i++)if(i!=seeker&&!match.rules.found[i])
   {
    var t=match.trucks[i];bool wall=Occluded(source,t);float distance=Vector3.Distance(source.body.position,t.body.position);if(Visible(seeker,i)){knowledge.See(i,t.body.position,now);VisibleObservations++;}else if(wall)BlockedObservations++;
    float range=HideSignalRules.AudibleRange(match.engines[i].Running,t.body.linearVelocity.magnitude,wall);float strength=range-Mathf.Max(1,distance);if(strength>loudest){loudest=strength;heard=i;}
   }
   if(heard>=0){knowledge.Hear(source.body.position,match.trucks[heard].body.position,now);AudibleObservations++;}
   if(now>=nextClue){nextClue=now+30;int next=-1;for(int j=1;j<match.setup.count;j++){int i=(seeker+j)%match.setup.count;if(!match.rules.found[i]){next=i;break;}}if(next>=0){knowledge.Hear(source.body.position,match.trucks[next].body.position,now);match.Announce("BROAD CLUE · "+knowledge.SoundBand+" · SECTOR "+(knowledge.SoundSector+1));}}
  }
 }
}
