using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckCornhole
{
 // Legacy Cornhole is not a SportsEventSession. This typed, read-only adapter
 // binds its existing owners/banked scores/physical provisional state directly.
 [DefaultExecutionOrder(270)]
 public sealed class CornholeCompactHud:MonoBehaviour
 {
  [Serializable]public sealed class Card
  {
   public RectTransform root;public TMP_Text name,score,delta,context;public UnityEngine.UI.Image portrait,stripe;
   [NonSerialized]public string ownerId;[NonSerialized]public int previousScore=int.MinValue;
  }
  public CornholeMatch match;public Card[] cards,rosterRows;public RectTransform roster;public TMP_FontAsset headingFont,bodyFont;
  public UnityEngine.UI.Graphic[] legacyGraphics;public bool[] originalGraphicEnabled;
  float next;
  public static Vector2 CardAnchor(Rect view)=>new Vector2(view.width<.99f&&view.x>=.49f?1:0,1);
  public string Score(int slot)=>match.totals[slot]+" PTS";
  public string Delta(int slot)=>match.RoundBreak||match.Finished?"+"+match.lastRound[slot]+" FINAL":"+"+match.courses[slot].Provisional+" PROV";
  public string Context(int slot)
  {
   var course=match.courses[slot];var replay=course.replay;
   if(replay.Playing)return "REPLAY · "+replay.AngleName+" · "+replay.Speed.ToString("0.##")+"×  "+replay.Clock.ToString("F1")+" / "+replay.Duration.ToString("F1")+"s";
   string round="R"+match.Round+"/"+CornholeRules.Rounds+" · ";if(match.Finished)return round+"MATCH COMPLETE";if(match.RoundBreak)return round+"POINTS BANKED";
   string phase=course.Phase==CornholePhase.Approach?"APPROACH":course.Phase==CornholePhase.Flight?"AIR":course.Phase==CornholePhase.Settling?"SETTLING":course.Phase==CornholePhase.Waiting?"WAITING":"ATTEMPT COMPLETE";
   string time=course.Phase==CornholePhase.Approach||course.Phase==CornholePhase.Flight||course.Phase==CornholePhase.Settling?" · "+course.PhaseElapsed.ToString("F1")+"s":"";
   return round+"BAG "+Mathf.Clamp(course.Attempts,1,CornholeRules.Deliveries)+"/"+CornholeRules.Deliveries+" · "+phase+time;
  }
  void Fill(Card card,int slot,bool small)
  {
   var player=match.setup.players[slot];card.ownerId=player.participantId;card.name.font=card.score.font=headingFont;card.name.text="P"+(slot+1)+" · "+player.name;card.name.color=card.stripe.color=player.color;card.portrait.color=Color.Lerp(player.color,Color.white,.3f);int value=match.totals[slot];
   if(card.previousScore!=int.MinValue&&card.previousScore!=value){card.score.transform.DOKill();card.score.transform.localScale=Vector3.one;card.score.transform.DOPunchScale(Vector3.one*.1f,.23f,2,.4f).SetUpdate(true).SetLink(card.root.gameObject);}card.previousScore=value;card.score.text=Score(slot);
   if(card.delta!=null){card.delta.font=bodyFont;card.delta.text=Delta(slot);}if(card.context!=null){card.context.font=bodyFont;card.context.text=small?"R"+match.Round+" · BAG "+match.courses[slot].Attempts+"/4 · "+Delta(slot):Context(slot);}
  }
  void LateUpdate()
  {
   if(match==null||cards==null||cards.Length!=4)return;foreach(var graphic in legacyGraphics)if(graphic!=null)graphic.enabled=false;
   bool playing=match.menus.Screen==CornholeScreen.Play;int views=0;for(int i=0;i<4;i++){var camera=match.courses[i].chase.view;bool active=playing&&i<match.setup.count&&camera.enabled;cards[i].root.gameObject.SetActive(active);if(active){views++;var anchor=CardAnchor(camera.rect);cards[i].root.anchorMin=cards[i].root.anchorMax=cards[i].root.pivot=anchor;cards[i].root.anchoredPosition=new Vector2(anchor.x>.5f?-10:10,-10);}}
   bool single=playing&&views==1&&match.setup.count>1;roster.gameObject.SetActive(single);if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;
   for(int i=0;i<4;i++){if(cards[i].root.gameObject.activeInHierarchy)Fill(cards[i],i,false);rosterRows[i].root.gameObject.SetActive(single&&i<match.setup.count);if(single&&i<match.setup.count)Fill(rosterRows[i],i,true);}
  }
  void OnDisable()
  {
   Stop(cards);Stop(rosterRows);if(roster!=null)roster.gameObject.SetActive(false);
   if(legacyGraphics!=null&&originalGraphicEnabled!=null)for(int i=0;i<legacyGraphics.Length&&i<originalGraphicEnabled.Length;i++)if(legacyGraphics[i]!=null)legacyGraphics[i].enabled=originalGraphicEnabled[i];
  }
  static void Stop(Card[] values){if(values==null)return;foreach(var card in values)if(card!=null&&card.root!=null){card.score.transform.DOKill();card.score.transform.localScale=Vector3.one;card.root.gameObject.SetActive(false);}}
 }
}
