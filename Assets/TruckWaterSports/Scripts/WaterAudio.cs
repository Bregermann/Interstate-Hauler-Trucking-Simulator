using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckWaterSports {
// Existing Sports settings remain the only audio preference authority.
public sealed class WaterAudio:MonoBehaviour {
 public WaterMatch match;public AudioSource ambience;public float gain=.15f;
 public int paddleFeedbackIndex=-1,capsizeFeedbackIndex=-1,recoveryFeedbackIndex=-1,impactFeedbackIndex=-1;
 public int PaddleCueIndex=>paddleFeedbackIndex>=0?paddleFeedbackIndex:0;
 readonly int[] capsizes=new int[4];readonly bool[] recoveryArmed=new bool[4];readonly float[] nextImpact=new float[4];string matchId;
 void Awake(){if(match!=null&&match.vessels!=null)foreach(var vessel in match.vessels)if(vessel!=null)vessel.HullImpact+=Impact;}
 void Update(){
  if(match==null||match.settings==null)return;
  if(ambience!=null){float target=gain;if(match is BlackwaterMatch horror&&horror.entity!=null&&horror.vessels!=null&&horror.vessels.Length>0&&horror.Clock>20&&Vector3.Distance(horror.entity.position,horror.vessels[0].body.position)<32)target*=.05f;ambience.volume=Mathf.MoveTowards(ambience.volume,match.settings.sfx*target,Time.unscaledDeltaTime*.08f);bool audible=match.Live&&Application.isFocused;if(audible&&!ambience.isPlaying)ambience.Play();else if(!audible&&ambience.isPlaying)ambience.Stop();}
  if(!match.Live)return;
  if(matchId!=match.MatchId){matchId=match.MatchId;System.Array.Clear(capsizes,0,4);System.Array.Clear(recoveryArmed,0,4);System.Array.Clear(nextImpact,0,4);}
  for(int i=0;i<match.vessels.Length;i++){
   var vessel=match.vessels[i];if(vessel==null||!vessel.gameObject.activeInHierarchy)continue;
   if(vessel.Capsizes>capsizes[i]){capsizes[i]=vessel.Capsizes;recoveryArmed[i]=true;Cue(capsizeFeedbackIndex,vessel.body.position,12);}
   if(recoveryArmed[i]&&!vessel.Capsized){recoveryArmed[i]=false;Cue(recoveryFeedbackIndex,vessel.body.position,7);}
  }
 }
 void Cue(int index,Vector3 position,float speed){if(index>=0&&match.Live&&Application.isFocused)match.feedback.PlayAt(index,position,speed);}
 void Impact(WaterVessel vessel,Collision collision){
  if(match==null||!match.Live||collision.contactCount==0)return;
  int index=System.Array.IndexOf(match.vessels,vessel);if(index<0||match.Clock<nextImpact[index])return;
  float closing=0;for(int contact=0;contact<collision.contactCount;contact++)closing=Mathf.Max(closing,Mathf.Abs(Vector3.Dot(collision.relativeVelocity,collision.GetContact(contact).normal)));
  if(closing<2)return;nextImpact[index]=match.Clock+1.2f;Cue(impactFeedbackIndex,collision.GetContact(0).point,closing);
 }
 void OnDestroy(){if(match!=null&&match.vessels!=null)foreach(var vessel in match.vessels)if(vessel!=null)vessel.HullImpact-=Impact;}
 void OnDisable(){if(ambience!=null)ambience.Stop();}
}
}
