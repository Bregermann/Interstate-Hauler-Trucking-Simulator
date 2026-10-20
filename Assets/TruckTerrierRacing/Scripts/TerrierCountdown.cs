using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using LWS.TruckSports;
namespace LWS.TruckTerrierRacing {
public sealed class TerrierCountdown:MonoBehaviour {
 // Keep the original serialized Terrier binding. Other races reuse this exact
 // Heat/DOTween display through session, without adding a simulation timer.
 public TerrierMatch match;public SportsEventSession session;
 public RectTransform[] panels,badges;public TMP_Text[] labels;public Image[] rings;public CanvasGroup[] fades;public AudioSource beatAudio;
 public SportsEventSession Session=>session!=null?session:match;
 public int Beat{get;private set;}=-1;public int Cues{get;private set;}
 int previous=-1;float lastRemaining=-1;
 void Update(){
  var race=Session;if(race==null)return;
  float remaining=race.RaceCountdownSeconds,elapsed=race.RaceStartSeconds;
  bool visible=race.Running&&!race.Finished&&!race.Paused&&race.menus.Screen==SportsEventScreen.Play&&(remaining>=0||elapsed>=0&&elapsed<1);
  Beat=visible?remaining>=0?Mathf.Clamp(Mathf.CeilToInt(remaining),1,3):0:-1;
  if(!visible){foreach(var panel in panels)panel.gameObject.SetActive(false);beatAudio.Stop();if(!race.Running||race.Finished||remaining<0&&elapsed>=1){previous=-1;lastRemaining=-1;}return;}
  if(remaining>=0&&remaining>lastRemaining+.001f)previous=-1;
  lastRemaining=remaining;
  bool changed=Beat!=previous;previous=Beat;
  for(int i=0;i<panels.Length;i++){
   bool active=i<race.cameras.Length&&race.cameras[i].enabled;panels[i].gameObject.SetActive(active);if(!active)continue;
   var viewport=race.cameras[i].rect;panels[i].anchorMin=viewport.min;panels[i].anchorMax=viewport.max;panels[i].offsetMin=panels[i].offsetMax=Vector2.zero;
   fades[i].alpha=Beat==0?1-Mathf.SmoothStep(0,1,elapsed):1;
   rings[i].fillAmount=Beat==0?1:1-Mathf.Repeat(3-remaining,1);
   rings[i].color=Beat==0?new Color(.25f,1,.75f):Beat==1?new Color(1,.35f,.12f):new Color(1,.76f,.22f);
   if(changed){
    labels[i].text=Beat==0?"GO GO GO!":Beat.ToString();labels[i].fontSize=Beat==0?64:88;labels[i].rectTransform.sizeDelta=new Vector2(Beat==0?440:145,115);
    badges[i].DOKill();badges[i].localScale=Vector3.one;badges[i].DOPunchScale(Vector3.one*(Beat==1?.16f:.1f),.36f,3).SetLink(badges[i].gameObject);
   }
  }
  if(changed){Cues++;if(Application.isFocused&&race.settings.master>0&&race.settings.sfx>0){
   beatAudio.pitch=Beat==0?1.15f:Mathf.Lerp(.8f,1.3f,(3-Beat)*.5f);beatAudio.volume=race.settings.sfx*.4f;
   var clips=race.feedback.clips;int index=Beat==0?5:4;if(clips!=null&&clips.Length>index&&clips[index]!=null)beatAudio.PlayOneShot(clips[index]);
  }}
  if(!Application.isFocused)beatAudio.Stop();
 }
 void OnDisable(){if(beatAudio!=null)beatAudio.Stop();if(badges!=null)foreach(var badge in badges)if(badge!=null)badge.DOKill();}
 void OnApplicationFocus(bool focus){if(!focus&&beatAudio!=null)beatAudio.Stop();}
}
}
