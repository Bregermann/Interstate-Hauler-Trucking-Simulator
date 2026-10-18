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
  public void Refresh(){
   if(match.Round==null)return;bool shared=match.option==0;float canvasWidth=((RectTransform)transform).rect.width;
   for(int v=0;v<summary.Length;v++){
    bool visible=!match.ReplayControls&&v<match.cameras.Length;summary[v].gameObject.SetActive(visible);if(summaryBackings!=null&&v<summaryBackings.Length)summaryBackings[v].gameObject.SetActive(visible);if(!visible)continue;
    int slot=match.views.Owner(v),seconds=Mathf.CeilToInt(Mathf.Max(0,match.Round.Duration-match.Round.Elapsed));string timer=$"{seconds/60:0}:{seconds%60:00}";
    string legend="<color=#55D9EE>NOSE +10</color>   <color=#FFD348>MASK +100</color>   <color=#FF555D>STOP −100</color>";
    string score=slot<0?"PARADE PANDEMONIUM":match.setup.players[slot].name+"   "+match.Round.Score(slot)+" PTS";
    summary[v].text=$"<b>{score}   {timer}</b>\n<size=85%>{match.Round.PositiveRemaining} TARGETS LEFT   ·   {legend}</size>";
    if(slot>=0&&match.Round.PositiveRemaining<=5)summary[v].text+="\n"+match.LastTargets(slot);
    if(match.cabExperience&&slot>=0)summary[v].text+="\n<size=85%>"+match.cabExtras.ShortPrompt(slot)+" · "+match.cabExtras.Status(slot)+"</size>";
    float width=Mathf.Min(shared?850:700,match.menus.viewPanels[v].rect.width-32),height=match.cabExperience?116:82;
    summary[v].rectTransform.sizeDelta=new Vector2(width-28,height-16);summary[v].fontSizeMax=29;summary[v].fontSizeMin=24;
    if(summaryBackings!=null&&v<summaryBackings.Length)summaryBackings[v].sizeDelta=new Vector2(width,height);
   }
   int columns=Mathf.Min(4,match.setup.count);float cardWidth=Mathf.Min(360,(canvasWidth-36)/columns-10);
   for(int i=0;i<cards.Length;i++){
    bool active=!match.ReplayControls&&shared&&i<match.setup.count;cardRoots[i].gameObject.SetActive(active);if(!active)continue;
    var rect=cardRoots[i];rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(18+(i%columns)*(cardWidth+10),18+(i/columns)*86);rect.sizeDelta=new Vector2(cardWidth,78);
    var portrait=rect.Find("Truck portrait") as RectTransform;if(portrait!=null){portrait.anchorMin=portrait.anchorMax=new Vector2(0,.5f);portrait.pivot=new Vector2(0,.5f);portrait.anchoredPosition=new Vector2(10,0);portrait.sizeDelta=new Vector2(62,62);}
    var text=cards[i].rectTransform;text.anchorMin=Vector2.zero;text.anchorMax=Vector2.one;text.offsetMin=new Vector2(84,5);text.offsetMax=new Vector2(-12,-5);
    var p=match.setup.players[i];cards[i].alignment=TextAlignmentOptions.MidlineLeft;cards[i].overflowMode=TextOverflowModes.Ellipsis;cards[i].textWrappingMode=TextWrappingModes.NoWrap;cards[i].color=p.color;cards[i].fontSizeMin=19;cards[i].fontSizeMax=25;cards[i].text=$"<b>{p.name}</b>\n<size=115%>{match.Round.Score(i)}</size>  <size=75%>PTS</size>";
   }
   countdown.gameObject.SetActive(match.Live&&match.Intro>0);countdown.text=match.Countdown;
   announcement.rectTransform.anchorMin=announcement.rectTransform.anchorMax=new Vector2(.5f,1);announcement.rectTransform.pivot=new Vector2(.5f,1);announcement.rectTransform.anchoredPosition=new Vector2(0,-130);announcement.rectTransform.sizeDelta=new Vector2(Mathf.Min(1400,canvasWidth-36),65);announcement.text=match.ReplayControls?"IMPACT HIGHLIGHT":shared?match.Status:"";
  }
  void LateUpdate(){if(match.Round==null||match.cameras.Length==0)return;for(int i=0;i<popups.Length;i++){int view=0;for(int v=0;v<match.cameras.Length;v++)if(match.views.Owner(v)==popupOwners[i])view=v;var camera=match.cameras[view];if(expiry[i]<=Time.time){popups[i].gameObject.SetActive(false);continue;}var p=camera.WorldToViewportPoint(points[i]+Vector3.up*(2+(1.5f-(expiry[i]-Time.time))*1.5f));bool visible=p.z>0&&p.x>0&&p.x<1&&p.y>0&&p.y<1;popups[i].gameObject.SetActive(visible);if(visible){var r=camera.rect;popups[i].rectTransform.anchorMin=popups[i].rectTransform.anchorMax=new Vector2(r.x+p.x*r.width,r.y+p.y*r.height);popups[i].rectTransform.anchoredPosition=Vector2.zero;}}}
  void OnDestroy(){foreach(var p in popups)if(p!=null)p.transform.DOKill();}
 }
}
