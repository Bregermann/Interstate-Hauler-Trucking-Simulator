using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckWaterSports {
// Existing Sports settings remain the only audio preference authority.
public sealed class WaterAudio:MonoBehaviour {
 public WaterMatch match;public AudioSource ambience;public float gain=.15f;
 void Update(){if(ambience==null||match==null||match.settings==null)return;float target=gain;if(match is BlackwaterMatch horror&&horror.entity!=null&&horror.vessels!=null&&horror.vessels.Length>0&&horror.Clock>20&&Vector3.Distance(horror.entity.position,horror.vessels[0].body.position)<32)target*=.05f;ambience.volume=Mathf.MoveTowards(ambience.volume,match.settings.sfx*target,Time.unscaledDeltaTime*.08f);bool audible=match.Live&&Application.isFocused;if(audible&&!ambience.isPlaying)ambience.Play();else if(!audible&&ambience.isPlaying)ambience.Stop();}
 void OnDisable(){if(ambience!=null)ambience.Stop();}
}
}
