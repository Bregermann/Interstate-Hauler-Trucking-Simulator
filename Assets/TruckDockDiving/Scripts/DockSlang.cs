using UnityEngine;
using TMPro;
using DG.Tweening;
namespace LWS.TruckDockDiving {
// Presentation only. The existing DockMatch remains the sole physics/scoring owner.
public sealed class DockSlang:MonoBehaviour {
 public DockMatch match;public RectTransform bubble;public CanvasGroup group;public TMP_Text caption;
 public int Bubbles{get;private set;}public int Speaker{get;private set;}=-1;
 static readonly string[] Lines={"Eh!","Beauty!","Oh, bud!","Give’r!","Holy smokes!","Oh, fer sure!","Sorry!","Bud!"};
 Sequence tween;int seen=-1,line;float airborne;Canvas canvas;RectTransform canvasRect;
 void Awake(){canvas=bubble.GetComponentInParent<Canvas>();canvasRect=canvas.transform as RectTransform;Hide();}
 void Update(){
  if(match==null||!match.Live||match.Phase!=DockPhase.Flight){airborne=0;if(group.alpha>0)Hide();return;}
  if(seen!=match.Launches){seen=match.Launches;airborne=0;}
  airborne+=Time.deltaTime;
  if(airborne>=.42f&&airborne<100){airborne=100;if((seen+match.Active)%3!=0)Speak();}
 }
 public void Speak(){if(!match.Live||match.Phase!=DockPhase.Flight)return;Hide();Speaker=match.Active;caption.text=Lines[line++%Lines.Length];Bubbles++;bubble.localScale=Vector3.one*.65f;
  tween=DOTween.Sequence().Append(group.DOFade(1,.12f)).Join(bubble.DOScale(1,.3f).SetEase(Ease.OutBack)).AppendInterval(1.15f).Append(group.DOFade(0,.22f)).Join(bubble.DOScale(.92f,.22f));
 }
 void LateUpdate(){if(group.alpha<=0||match.broadcast==null)return;var camera=match.broadcast.view;var p=camera.WorldToViewportPoint(match.truck.body.position+Vector3.up*2.1f);if(p.z<=0){group.alpha=0;return;}var rect=canvasRect.rect;float halfX=bubble.rect.width*.5f+18,halfY=bubble.rect.height*.5f+18;float x=Mathf.Clamp(rect.xMin+p.x*rect.width,rect.xMin+halfX,rect.xMax-halfX);float y=Mathf.Clamp(rect.yMin+p.y*rect.height+54,rect.yMin+halfY+130,rect.yMax-halfY-110);bubble.anchoredPosition=new Vector2(x,y);}
 public void Hide(){tween?.Kill();tween=null;if(group!=null)group.alpha=0;Speaker=-1;}
 void OnDisable(){Hide();}void OnDestroy(){Hide();}
}
}
