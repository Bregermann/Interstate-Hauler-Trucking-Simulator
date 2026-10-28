using TMPro;
using UnityEngine;
namespace LWS.TruckSubmarine {
 public sealed class SubmarinePresentation:MonoBehaviour {
  public SubmarineMatch match;public TextMeshPro buoyBoard;public Light buoyLamp;public ParticleSystem bankBubbles;public SubmarineSquid squid;public Animator[] wildlife;public AudioSource waterAmbience;
  float deliveryUntil;
  public void ResetExpedition(string id){deliveryUntil=0;if(squid!=null)squid.ResetEncounter(id);if(waterAmbience!=null&&!waterAmbience.isPlaying)waterAmbience.Play();}
  public void Tick(float dt){if(buoyBoard!=null)buoyBoard.text="SALVAGE RECEPTION\n"+match.NativeDeliveries+" DELIVERIES\n"+Mathf.CeilToInt(match.rules.Remaining)+"s";if(buoyLamp!=null)buoyLamp.intensity=match.rules.Clock<deliveryUntil?5:2;if(waterAmbience!=null)waterAmbience.volume=Application.isFocused&&match.Live?match.settings.sfx*.16f:0;squid?.Tick(dt);}
  public void Delivery(int slot,int value){deliveryUntil=match.rules.Clock+2;bankBubbles?.Play();}
  public void Pause(bool pause){if(bankBubbles!=null){if(pause)bankBubbles.Pause();else if(bankBubbles.isPaused)bankBubbles.Play();}foreach(var animator in wildlife)if(animator!=null)animator.speed=pause?0:1;if(waterAmbience!=null){if(pause)waterAmbience.Pause();else if(!waterAmbience.isPlaying)waterAmbience.UnPause();}}
  public void StopSound(){if(waterAmbience!=null)waterAmbience.Stop();}
 }
}
