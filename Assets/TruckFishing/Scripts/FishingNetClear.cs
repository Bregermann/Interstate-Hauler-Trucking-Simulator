using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PixelCrushers.DialogueSystem;
using LWS.TruckSports;
namespace LWS.TruckFishing {
// Local net-clearing semantics. Pixel Crushers owns indicator visibility;
// DOTween owns presentation. No global dialogue, timescale or input owner.
public sealed class FishingNetClear:MonoBehaviour {
 public FishingMatch match;public int slot;public RectTransform panel;public TMP_Text caption;public Image timeBar;public Transform snag;public StandardUIQTEControls indicators=new StandardUIQTEControls();
 public bool Active{get;private set;}public int Completed{get;private set;}public int TimedRecoveries{get;private set;}public int Step{get;private set;}public float Age{get;private set;}public int RequiredDirection=>Step%2==0?-1:1;
 bool released;int shown=-1;float nextAI;long latest=-1;Tween pulse;
 public void ResetChallenge(){Active=false;Completed=TimedRecoveries=Step=0;Age=0;latest=-1;released=false;shown=-1;pulse?.Kill();if(panel!=null)panel.localScale=Vector3.one;if(indicators.QTEIndicators!=null)indicators.SetActive(false);if(snag!=null)snag.gameObject.SetActive(false);}
 public bool Begin(long id){if(Active||id<=latest)return false;latest=id;Age=0;Step=0;released=false;Active=true;nextAI=.4f;shown=-1;if(snag!=null)snag.gameObject.SetActive(true);if(panel!=null){panel.localScale=Vector3.one*.88f;pulse?.Kill();pulse=panel.DOScale(1,.24f).SetEase(Ease.OutBack);}return true;}
 public void Tick(float input,float dt,bool ai,int difficulty){if(!Active)return;Age+=dt;if(ai){input=0;if(Age>=nextAI){input=RequiredDirection;nextAI=Age+Mathf.Lerp(.6f,.3f,difficulty*.5f);}}if(Mathf.Abs(input)<.2f)released=true;else if(released&&input*RequiredDirection>.55f){released=false;Step++;if(panel!=null){pulse?.Kill();panel.localScale=Vector3.one;pulse=panel.DOPunchScale(Vector3.one*.045f,.18f,2,.5f);}}if(Step>=4){Completed++;End();}else if(Age>=6){TimedRecoveries++;End();}}
 void End(){Active=false;pulse?.Kill();if(panel!=null)panel.localScale=Vector3.one;if(indicators.QTEIndicators!=null)indicators.SetActive(false);if(snag!=null)snag.gameObject.SetActive(false);match.feedback.Play(0,0,true);match.Pulse(slot,.18f,.09f);}
 void LateUpdate(){bool visible=Active&&match!=null&&match.Live&&match.menus.Screen==SportsEventScreen.Play;int view=-1;if(visible){if(match.HumanViews==0){if(match.Spectator==slot)view=0;}else for(int i=0;i<match.HumanViews;i++)if(match.Owners[i]==slot){view=i;break;}}if(view<0){if(indicators.areVisible)indicators.SetActive(false);return;}if(!indicators.areVisible)indicators.ShowIndicator(0);var viewport=match.cameras[view].rect;var parent=(RectTransform)panel.parent;parent.anchorMin=viewport.min;parent.anchorMax=viewport.max;parent.offsetMin=parent.offsetMax=Vector2.zero;if(shown!=Step){shown=Step;string key=RequiredDirection<0?"LEFT":"RIGHT";var player=match.setup.players[slot];if(player.source==LWS.TruckBowling.BowlingSource.Keyboard)key=(RequiredDirection<0?player.left:player.right).ToString().Replace("Arrow","").ToUpperInvariant();else if(player.source==LWS.TruckBowling.BowlingSource.LeftStick)key="L STICK "+key;else if(player.source==LWS.TruckBowling.BowlingSource.RightStick)key="R STICK "+key;else if(player.source==LWS.TruckBowling.BowlingSource.WholeController)key="L STICK "+key;caption.text="NET SNAGGED!\n<color=#FFD16B>"+key+"</color>  "+Step+" / 4\n<size=70%>RELEASE BETWEEN SHAKES</size>";}timeBar.fillAmount=1-Mathf.Clamp01(Age/6);}
 void OnDestroy(){pulse?.Kill();}
}
}
