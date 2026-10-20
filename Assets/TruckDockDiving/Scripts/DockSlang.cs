using UnityEngine;
using TMPro;
using DG.Tweening;
namespace LWS.TruckDockDiving {
// Presentation only. The existing DockMatch remains the sole physics/scoring owner.
public sealed class DockSlang:MonoBehaviour {
 public DockMatch match;public RectTransform bubble;public CanvasGroup group;public TMP_Text caption,owner;public UnityEngine.UI.Image accent;
 public int Bubbles{get;private set;}public int Speaker{get;private set;}=-1;
 static readonly string[] Lines={"Eh!","Beauty!","Oh, bud!","Give’r!","Holy smokes!","Oh, fer sure!","Sorry!","Bud!"};
 readonly System.Random reactions=new System.Random();Sequence tween;int seen=-1,spoken=-1,lastLine=-1;float airborne;Canvas canvas;RectTransform canvasRect;
 void Awake(){canvas=bubble.GetComponentInParent<Canvas>();canvasRect=canvas.transform as RectTransform;Hide();}
 void Update(){
  if(match==null||!match.Running||match.Finished){airborne=0;seen=spoken=-1;if(match!=null&&!match.Running)Bubbles=0;if(group.alpha>0)Hide();return;}
  // Pause freezes both the delay and the scaled DOTween sequence. It must not
  // reset the launch token and produce a second reaction on resume.
  if(match.Paused)return;
  if(match.Phase!=DockPhase.Flight){airborne=0;if(group.alpha>0)Hide();return;}
  if(seen!=match.Launches){if(match.Launches<seen)Bubbles=0;seen=match.Launches;airborne=0;}
  airborne+=Time.deltaTime;
  if(airborne>=.42f&&airborne<100){airborne=100;if((seen+match.Active)%3!=0)Speak();}
 }
 public void Speak(){if(!match.Live||match.Phase!=DockPhase.Flight||spoken==match.Launches)return;Hide();spoken=match.Launches;Speaker=match.Active;int line=lastLine<0?reactions.Next(Lines.Length):reactions.Next(Lines.Length-1);if(lastLine>=0&&line>=lastLine)line++;lastLine=line;caption.text=Lines[line];var participant=match.setup.players[Speaker];if(owner!=null){owner.text=participant.name;owner.color=Color.Lerp(participant.color,Color.black,.35f);}if(accent!=null)accent.color=participant.color;Bubbles++;bubble.localScale=Vector3.one*.65f;
  tween=DOTween.Sequence().Append(group.DOFade(1,.12f)).Join(bubble.DOScale(1,.3f).SetEase(Ease.OutBack)).AppendInterval(1.15f).Append(group.DOFade(0,.22f)).Join(bubble.DOScale(.92f,.22f));
 }
 void LateUpdate(){if(group.alpha<=0||!match.Live||match.broadcast==null)return;var camera=match.broadcast.view;var p=camera.WorldToViewportPoint(match.truck.body.position+Vector3.up*2.1f);if(p.z<=0){Hide();return;}bubble.anchoredPosition=PositionInViewport(canvasRect.rect,camera.rect,bubble.rect.size,new Vector2(p.x,p.y));}
 // Map camera-local coordinates into its own canvas region. Also supports
 // cropped spectator viewports without spilling into another player's HUD.
 public static Vector2 PositionInViewport(Rect canvas,Rect viewport,Vector2 size,Vector2 point){var area=new Rect(canvas.xMin+viewport.xMin*canvas.width,canvas.yMin+viewport.yMin*canvas.height,viewport.width*canvas.width,viewport.height*canvas.height);float hx=Mathf.Min(size.x*.5f+18,area.width*.48f),hy=Mathf.Min(size.y*.5f+18,area.height*.3f);return new Vector2(Mathf.Clamp(area.xMin+point.x*area.width,area.xMin+hx,area.xMax-hx),Mathf.Clamp(area.yMin+point.y*area.height+54,area.yMin+hy+area.height*.1f,area.yMax-hy-area.height*.12f));}
 public void Hide(){tween?.Kill();tween=null;if(group!=null)group.alpha=0;Speaker=-1;}
 public void ResetMatch(){Hide();seen=spoken=-1;airborne=0;Bubbles=0;}
 void OnDisable(){Hide();}void OnDestroy(){Hide();}
}
}
