using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace LWS.TruckBilliards
{
 // UI confetti stays above the dimmed table and cannot participate in physics.
 public sealed class BilliardsCelebration:MonoBehaviour
 {
  public BilliardsSession session;RectTransform[] confetti;Tween[] motion;Tween titleTween;float until;
  public int Bursts{get;private set;}public bool Strong{get;private set;}public bool Active=>Time.realtimeSinceStartup<until;
  void Awake(){confetti=new RectTransform[24];motion=new Tween[24];for(int i=0;i<24;i++){var g=new GameObject("Win confetti "+i,typeof(RectTransform),typeof(Image));g.transform.SetParent(transform,false);var r=g.GetComponent<RectTransform>();r.sizeDelta=new Vector2(8,14);g.GetComponent<Image>().raycastTarget=false;confetti[i]=r;g.SetActive(false);}}
  public void Play(Color color,bool series){Clear();Bursts++;Strong=series;until=Time.realtimeSinceStartup+(series?1.7f:1.05f);int count=series?24:12;
   for(int i=0;i<count;i++){var r=confetti[i];r.gameObject.SetActive(true);r.GetComponent<Image>().color=i%3==0?Color.white:color;r.localRotation=Quaternion.Euler(0,0,i*37);r.anchoredPosition=new Vector2((i%2==0?-1:1)*(380+i*8),230);var destination=r.anchoredPosition+new Vector2((i%2==0?1:-1)*(60+i*5),-260-(i%4)*25);motion[i]=DOTween.To(()=>r.anchoredPosition,value=>r.anchoredPosition=value,destination,series?1.5f:1).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(()=>r.gameObject.SetActive(false));}
   var title=session.menus.resultText.transform;title.localScale=Vector3.one*(series?1.07f:1.03f);titleTween=title.DOScale(1,.45f).SetEase(Ease.OutBack).SetUpdate(true);
  }
  public void Clear(){until=0;if(confetti!=null)for(int i=0;i<confetti.Length;i++){motion[i]?.Kill();confetti[i].gameObject.SetActive(false);}titleTween?.Kill();if(session!=null&&session.menus!=null&&session.menus.resultText!=null)session.menus.resultText.transform.localScale=Vector3.one;}
  void OnDisable(){Clear();}
 }
}

