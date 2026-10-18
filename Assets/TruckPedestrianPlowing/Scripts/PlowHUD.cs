using UnityEngine;
using TMPro;
using DG.Tweening;
namespace LWS.TruckPedestrianPlowing {
 public sealed class PlowHUD:MonoBehaviour {
  public PlowMatch match;public TMP_Text[] summary,cards;public RectTransform[] summaryBackings;public RectTransform[] cardRoots;
  public TMP_Text countdown,announcement;public TMP_Text[] popups;public RectTransform popupLayer;
  readonly int[] popupOwners=new int[24];readonly float[] expiry=new float[24];readonly Vector3[] points=new Vector3[24];int next;
  public void ResetHUD(){next=0;for(int i=0;i<popups.Length;i++){popups[i].gameObject.SetActive(false);expiry[i]=0;}}
  public void Popup(Vector3 position,int score,Color color,int owner){int i=next++%popups.Length;points[i]=position;popupOwners[i]=owner;expiry[i]=Time.time+1.5f;var t=popups[i];t.text=(score>0?"+":"")+score;t.color=score<0?new Color(1,.15f,.2f):color;t.gameObject.SetActive(true);t.transform.DOKill();t.transform.localScale=Vector3.one*.6f;t.transform.DOScale(1,.2f).SetEase(Ease.OutBack);}
  public void Refresh(){if(match.Round==null)return;bool shared=match.option==0;for(int v=0;v<summary.Length;v++){bool visible=!match.ReplayControls&&v<match.cameras.Length;summary[v].gameObject.SetActive(visible);if(summaryBackings!=null&&v<summaryBackings.Length)summaryBackings[v].gameObject.SetActive(visible);if(!visible)continue;int slot=match.views.Owner(v);int seconds=Mathf.CeilToInt(Mathf.Max(0,match.Round.Duration-match.Round.Elapsed));string timer=$"{seconds/60:0}:{seconds%60:00}";string legend=$"{match.Round.PositiveRemaining} left  ·  <color=#55D9EE>+10</color>  <color=#FFD348>STAR 100</color>  <color=#FF555D>STOP −100</color>";if(slot>=0&&!string.IsNullOrEmpty(match.LastTargets(slot)))legend=match.LastTargets(slot)+" · "+legend;summary[v].text=slot<0?"<b>"+timer+"</b>  ·  "+legend:"<b>"+match.setup.players[slot].name+" · "+match.Round.Score(slot)+" pts   "+timer+"</b>\n"+legend+"\n<size=90%>"+match.Controls(slot)+(match.cabExperience?" · "+match.cabExtras.ShortPrompt(slot)+"\n"+match.cabExtras.Status(slot):"")+"</size>";
    if(summaryBackings!=null&&v<summaryBackings.Length){float width=Mathf.Min(500,match.menus.viewPanels[v].rect.width-24);summary[v].rectTransform.sizeDelta=new Vector2(width-22,match.cabExperience?126:92);summaryBackings[v].sizeDelta=new Vector2(width,match.cabExperience?140:106);}
}
   for(int i=0;i<cards.Length;i++){bool active=!match.ReplayControls&&shared&&i<match.setup.count;cardRoots[i].gameObject.SetActive(active);if(active){var p=match.setup.players[i];cards[i].color=p.color;cards[i].text=$"<b>{p.name}</b>\n<size=120%>{match.Round.Score(i)}</size>  <size=75%>PTS</size>";}}
   countdown.gameObject.SetActive(match.Live&&match.Intro>0);countdown.text=match.Countdown;announcement.text=match.ReplayControls?"IMPACT HIGHLIGHT":shared?match.Status:"";
  }
  void LateUpdate(){if(match.Round==null||match.cameras.Length==0)return;for(int i=0;i<popups.Length;i++){int view=0;for(int v=0;v<match.cameras.Length;v++)if(match.views.Owner(v)==popupOwners[i])view=v;var camera=match.cameras[view];if(expiry[i]<=Time.time){popups[i].gameObject.SetActive(false);continue;}var p=camera.WorldToViewportPoint(points[i]+Vector3.up*(2+(1.5f-(expiry[i]-Time.time))*1.5f));bool visible=p.z>0&&p.x>0&&p.x<1&&p.y>0&&p.y<1;popups[i].gameObject.SetActive(visible);if(visible){var r=camera.rect;popups[i].rectTransform.anchorMin=popups[i].rectTransform.anchorMax=new Vector2(r.x+p.x*r.width,r.y+p.y*r.height);popups[i].rectTransform.anchoredPosition=Vector2.zero;}}}
  void OnDestroy(){foreach(var p in popups)if(p!=null)p.transform.DOKill();}
 }
}
