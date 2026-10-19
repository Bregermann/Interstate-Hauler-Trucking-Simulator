using UnityEngine;
namespace LWS.TruckDanceOff {
public sealed class DanceVenue:MonoBehaviour {
 public DanceMatch match;public Transform dj;public Transform[] meters;public LineRenderer[] lasers;public Animator[] audience;public ParticleSystem[] fog;public ParticleSystem celebration;public float[] meterHeights;
 DancePhase prior;Vector3 djOrigin;Quaternion djRotation;int updateAudience;
 void Awake(){if(dj!=null){djOrigin=dj.localPosition;djRotation=dj.localRotation;}for(int i=0;i<audience.Length;i++)if(audience[i]!=null){audience[i].applyRootMotion=false;audience[i].speed=.9f+(i%5)*.05f;if(audience[i].HasState(0,Animator.StringToHash("Dance")))audience[i].Play("Dance",0,i*.071f%1);else if(audience[i].HasState(0,Animator.StringToHash("cheer")))audience[i].Play("cheer",0,i*.071f%1);}}
 void Update(){bool live=match.Live;double beat=match.Song!=null?match.SongTime/match.Song.Beat:Time.time*.6;float t=(float)beat;float pulse=.35f+.65f*Mathf.Max(0,Mathf.Sin(t*Mathf.PI*2));for(int i=0;i<meters.Length;i++){var scale=meters[i].localScale;scale.y=meterHeights[i]*(.6f+.4f*Mathf.Sin(t*2+i*.63f));meters[i].localScale=scale;}
  for(int i=0;i<lasers.Length;i++){var line=lasers[i];line.enabled=live&&!match.Paused;if(!line.enabled)continue;var from=line.transform.position;line.SetPosition(0,from);line.SetPosition(1,new Vector3(Mathf.Sin(t*.22f+i*1.5f)*21,.7f,Mathf.Cos(t*.17f+i)*12));}
  if(dj!=null){dj.localPosition=djOrigin+Vector3.up*(live?pulse*.09f:0);dj.localRotation=djRotation*Quaternion.Euler(live?Mathf.Sin(t*Mathf.PI)*4:0,live?Mathf.Sin(t)*7:0,0);}
  foreach(var effect in fog){if(live&&!effect.isPlaying)effect.Play();else if(!live&&effect.isPlaying)effect.Pause();}
  if(prior!=match.Phase&&match.Phase==DancePhase.RoundResult){celebration?.Play();for(int i=0;i<audience.Length;i++)if(audience[i]!=null&&audience[i].HasState(0,Animator.StringToHash("Cheer")))audience[i].CrossFade("Cheer",.2f,0,i*.03f%1);}
  if(prior!=match.Phase&&match.Phase==DancePhase.CountIn)foreach(var actor in audience)if(actor!=null&&actor.HasState(0,Animator.StringToHash("Dance")))actor.CrossFade("Dance",.3f);prior=match.Phase;
 }
}
}
