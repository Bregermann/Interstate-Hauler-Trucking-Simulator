using System;
using TMPro;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckHungryHaulers
{
 // Read-only saved view. Physical pickup, release and banking stay in HaulerMatch.
 [DefaultExecutionOrder(450)]public sealed class HaulerCargoHud:MonoBehaviour
 {
  [Serializable]public sealed class Card{public RectTransform root,promptRoot;public TMP_Text name,points,cargo,prompt;public UnityEngine.UI.Image portrait,color;public int owner=-1;[NonSerialized]public int previousPoints=-1,previousCargo=-1,previousCapacity=-1;}
  [Serializable]public sealed class Clock{public RectTransform root;public TMP_Text text;}
  public HaulerMatch match;public RectTransform root;public Card[] cards;public Clock[] clocks=Array.Empty<Clock>();
  public int VisibleCards{get;private set;}
  void LateUpdate()
  {
   bool playing=match!=null&&match.menus.Screen==SportsEventScreen.Play&&!match.ReplayControls;root.gameObject.SetActive(playing);VisibleCards=0;if(!playing)return;bool split=match.MultiViewHUD;
   int remaining=Mathf.Max(0,Mathf.CeilToInt(match.rules.Duration-match.rules.Clock));string clockText=(remaining/60)+":"+(remaining%60).ToString("00")+" · "+match.rules.PositiveRemaining+" LEFT";
   for(int view=0;view<clocks.Length;view++)
   {
    var clock=clocks[view];bool visible=split?view<match.drivingViews.rally.Humans&&view<match.cameras.Length&&match.cameras[view]!=null&&match.cameras[view].enabled:view==0;
    clock.root.gameObject.SetActive(visible);if(!visible)continue;var viewport=split?match.cameras[view].rect:new Rect(0,0,1,1);clock.root.anchorMin=clock.root.anchorMax=new Vector2(viewport.xMax,viewport.yMax);clock.root.pivot=Vector2.one;clock.root.anchoredPosition=new Vector2(-12,-12);clock.text.text=clockText;
   }
   for(int view=0;view<cards.Length;view++)
   {
    var card=cards[view];int owner=split?(view<match.drivingViews.rally.Humans?match.drivingViews.rally.owners[view]:-1):view;bool active=split?view<match.drivingViews.rally.Humans&&view<match.cameras.Length&&match.cameras[view]!=null&&match.cameras[view].enabled&&owner>=0&&owner<match.setup.count:view<match.setup.count;
    card.root.gameObject.SetActive(active);card.owner=active?owner:-1;if(!active)continue;VisibleCards++;var player=match.setup.players[owner];
    if(split){var viewport=match.cameras[view].rect;card.root.anchorMin=card.root.anchorMax=new Vector2(viewport.xMin,viewport.yMax);card.root.pivot=new Vector2(0,1);card.root.anchoredPosition=new Vector2(12,-12);}
    else{card.root.anchorMin=card.root.anchorMax=card.root.pivot=new Vector2(.5f,0);card.root.anchoredPosition=new Vector2((view-(match.setup.count-1)*.5f)*256,14);}
    card.name.text=player.name;card.name.color=card.color.color=player.color;card.portrait.color=Color.Lerp(player.color,Color.white,.3f);if(card.points.text.Length==0||card.previousPoints!=match.rules.points[owner]){card.previousPoints=match.rules.points[owner];card.points.text=card.previousPoints.ToString();}if(card.previousCargo!=match.rules.carried[owner]||card.previousCapacity!=match.rules.Capacity){card.previousCargo=match.rules.carried[owner];card.previousCapacity=match.rules.Capacity;card.cargo.text=card.previousCargo+" / "+card.previousCapacity+" CARGO";}
    string prompt=match.DumpContext(owner);card.promptRoot.gameObject.SetActive(!string.IsNullOrEmpty(prompt));card.prompt.text=prompt??"";card.promptRoot.anchorMin=card.promptRoot.anchorMax=split?Vector2.zero:Vector2.up;card.promptRoot.pivot=split?Vector2.up:Vector2.zero;card.promptRoot.anchoredPosition=new Vector2(0,split?-4:4);
   }
  }
 }
}
