using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using LWS.TruckSports;
namespace LWS.TruckTerrierRacing {
public sealed class TerrierCountdown:MonoBehaviour {
 public TerrierMatch match;public RectTransform[] panels,badges;public TMP_Text[] labels;public Image[] rings;public CanvasGroup[] fades;public AudioSource beatAudio;int previous=-1;
 void Update(){bool visible=match.Running&&!match.Finished&&!match.Paused&&match.menus.Screen==SportsEventScreen.Play&&(match.Phase==TerrierPhase.Countdown||match.Phase==TerrierPhase.Race&&match.HeatTime<1);int beat=visible?match.Phase==TerrierPhase.Countdown?Mathf.Clamp(Mathf.CeilToInt(3-match.CountdownElapsed),1,3):0:-1;bool changed=beat!=previous;previous=beat;
 for(int i=0;i<panels.Length;i++){bool active=visible&&i<match.cameras.Length&&match.cameras[i].enabled;panels[i].gameObject.SetActive(active);if(!active)continue;var viewport=match.cameras[i].rect;panels[i].anchorMin=viewport.min;panels[i].anchorMax=viewport.max;panels[i].offsetMin=panels[i].offsetMax=Vector2.zero;fades[i].alpha=beat==0?1-Mathf.SmoothStep(0,1,match.HeatTime):1;rings[i].fillAmount=beat==0?1:1-Mathf.Repeat(match.CountdownElapsed,1);rings[i].color=beat==0?new Color(.25f,1,.75f):beat==1?new Color(1,.35f,.12f):new Color(1,.76f,.22f);if(changed){labels[i].text=beat==0?"GO GO GO!":beat.ToString();labels[i].fontSize=beat==0?64:88;labels[i].rectTransform.sizeDelta=new Vector2(beat==0?440:145,115);badges[i].DOKill();badges[i].localScale=Vector3.one;badges[i].DOPunchScale(Vector3.one*(beat==1?.16f:.1f),.36f,3).SetLink(badges[i].gameObject);}}
 if(changed&&beat>=0&&Application.isFocused&&match.settings.master>0&&match.settings.sfx>0){beatAudio.pitch=beat==0?1.15f:Mathf.Lerp(.8f,1.3f,(3-beat)*.5f);beatAudio.volume=match.settings.sfx*.4f;beatAudio.PlayOneShot(match.feedback.clips[beat==0?5:4]);}if(match.Paused||!Application.isFocused||!visible)beatAudio.Stop();}
 void OnDisable(){if(beatAudio!=null)beatAudio.Stop();if(badges!=null)foreach(var badge in badges)badge.DOKill();}
 void OnApplicationFocus(bool focus){if(!focus)beatAudio.Stop();}
}
}
