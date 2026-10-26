using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LWS.TruckSports;

namespace LWS.TruckSports
{
    // Serialized card references shared by this bounded four-sport authoring pass.
    // The existing Heat theme, broadcast plate, portrait and DOTween remain the UI foundation.
    [Serializable] public sealed class CarnivalArenaHudCard
    {
        public RectTransform root; public TMP_Text name,value,detail;
        public Image portrait,stripe,progress; public Image[] pips;
        [NonSerialized] public int slot; [NonSerialized] public string previousValue;
    }
    internal static class CarnivalArenaHudLayout
    {
        public static bool Playing(SportsEventSession match)=>match!=null&&match.menus!=null&&match.menus.Screen==SportsEventScreen.Play&&!match.ReplayControls;
        public static int Views(SportsEventSession match){int count=0;foreach(var camera in match.cameras)if(camera!=null&&camera.enabled&&camera.gameObject.activeInHierarchy)count++;return count;}
        public static void Box(RectTransform rect,float x,float y,float width,float height){rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.up;rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
        public static void Row(RectTransform root,CarnivalArenaHudCard[] cards,int count,bool center,int columns=0,float centerCardWidth=170,int[] order=null)
        {
            columns=columns>0?Mathf.Min(columns,count):count;int rows=Mathf.CeilToInt(count/(float)columns);
            float width=center?Mathf.Min(1280,columns*centerCardWidth):Mathf.Min(1860,columns*375);float cardWidth=(width+6)/columns-6;
            root.anchorMin=root.anchorMax=root.pivot=center?new Vector2(.5f,.5f):new Vector2(.5f,0);root.anchoredPosition=new Vector2(0,center?0:10);root.sizeDelta=new Vector2(width,rows*82+(rows-1)*5);
            for(int i=0;i<cards.Length;i++)
            {
                var card=cards[order!=null&&i<count?order[i]:i];card.root.gameObject.SetActive(i<count);if(i>=count)continue;
                Box(card.root,(i%columns)*(cardWidth+6),(i/columns)*87,cardWidth,82);
                Box(card.stripe.rectTransform,0,6,4,70);Box(card.portrait.rectTransform,8,7,32,32);
                Box(card.name.rectTransform,48,4,cardWidth-56,27);Box(card.value.rectTransform,8,44,78,28);Box(card.detail.rectTransform,92,45,cardWidth-100,25);
                card.progress.rectTransform.anchorMin=card.progress.rectTransform.anchorMax=card.progress.rectTransform.pivot=Vector2.up;card.progress.rectTransform.anchoredPosition=new Vector2(8,-77);
                if(card.pips!=null)for(int p=0;p<card.pips.Length;p++)Box(card.pips[p].rectTransform,cardWidth-55+p*15,33,11,11);
            }
        }
        public static void Data(CarnivalArenaHudCard card,SportsEventSession match,int slot,string value,string detail,float progress=-1,bool pulse=true,bool owner=false)
        {
            var player=match.setup.players[slot];card.slot=slot;card.name.text=player.name;card.name.color=card.stripe.color=player.color;card.portrait.color=Color.Lerp(player.color,Color.white,.35f);card.detail.text=detail;
            if(pulse&&card.previousValue!=null&&card.previousValue!=value){card.value.transform.DOKill();card.value.transform.localScale=Vector3.one;card.value.transform.DOPunchScale(Vector3.one*.10f,.24f,2,.4f).SetUpdate(true).SetLink(card.root.gameObject);}
            card.value.text=value;card.previousValue=value;card.root.GetComponent<Image>().color=owner?SportsBroadcastCanvas.Raised:SportsBroadcastCanvas.Ink;
            card.progress.gameObject.SetActive(progress>=0);card.progress.color=player.color;card.progress.rectTransform.sizeDelta=new Vector2(Mathf.Max(0,card.root.rect.width-16)*Mathf.Clamp01(progress),3);
        }
        public static void Stop(CarnivalArenaHudCard[] cards){if(cards==null)return;foreach(var card in cards)if(card?.root!=null){card.value.transform.DOKill();card.value.transform.localScale=Vector3.one;card.previousValue=null;}}
    }
    internal sealed class CarnivalArenaHudSuppression
    {
        readonly Dictionary<Graphic,bool> original=new Dictionary<Graphic,bool>();readonly List<Graphic> scratch=new List<Graphic>(96);
        void Hide(Graphic graphic){if(graphic==null)return;if(!original.ContainsKey(graphic))original.Add(graphic,graphic.enabled);graphic.enabled=false;}
        void Tree(Transform root){if(root==null)return;root.GetComponentsInChildren(true,scratch);foreach(var graphic in scratch)Hide(graphic);}
        public void Present(SportsEventSession match,Graphic[] graphics,bool active)
        {
            if(!active){Restore();return;}foreach(var graphic in graphics)Hide(graphic);
            Tree(match.menus.hud.transform.Find("Broadcast participant cards"));Tree(match.menus.hud.transform.Find("Broadcast status backplate"));
            foreach(var panel in match.menus.viewPanels){Tree(panel.Find("View data backing"));Tree(panel.Find("Compact owned viewport score"));Tree(panel.Find("Assigned truck portrait"));Tree(panel.Find("Assigned participant color"));}
        }
        public void Restore(){foreach(var pair in original)if(pair.Key!=null)pair.Key.enabled=pair.Value;original.Clear();}
    }
}
namespace LWS.TruckKeepaway
{
    [DefaultExecutionOrder(650)] public sealed class KeepawayCarnivalHud:MonoBehaviour
    {
        public KeepawayMatch match;public RectTransform root,row;public CarnivalArenaHudCard[] cards;public Graphic[] legacyGraphics;
        public Renderer[] originalNames;public SpriteRenderer[] icons;
        readonly CarnivalArenaHudSuppression suppression=new CarnivalArenaHudSuppression();bool[] nameVisibility;
        public bool Showing{get;private set;}public int VisibleCards{get;private set;}
        void LateUpdate()
        {
            bool playing=CarnivalArenaHudLayout.Playing(match);Showing=playing&&match.MultiViewHUD;root.gameObject.SetActive(Showing);suppression.Present(match,legacyGraphics,Showing);VisibleCards=Showing?match.setup.count:0;
            if(Showing)
            {
                CarnivalArenaHudLayout.Row(row,cards,VisibleCards,CarnivalArenaHudLayout.Views(match)>1,0,240);
                for(int i=0;i<VisibleCards;i++)CarnivalArenaHudLayout.Data(cards[i],match,i,match.possession[i].ToString("F1")+"s","/ "+match.Target+"s · "+(!match.alive[i]?"RESPAWNING":match.Carrier==i?match.ParticipantLocked(i)?"BALL · SAFE":"BALL":match.ParticipantLocked(i)?"PROTECTED":""),match.possession[i]/match.Target,false);
            }
            if(nameVisibility==null){nameVisibility=new bool[originalNames.Length];for(int i=0;i<nameVisibility.Length;i++)nameVisibility[i]=originalNames[i].enabled;}
            for(int i=0;i<icons.Length;i++)
            {
                bool active=playing&&i<match.setup.count&&match.alive[i];icons[i].gameObject.SetActive(active);
                originalNames[i].enabled=playing?false:nameVisibility[i];if(!active)continue;
                icons[i].transform.position=match.trucks[i].body.position+Vector3.up*2.8f;icons[i].color=match.setup.players[i].color;
            }
        }
        void OnDisable(){suppression.Restore();CarnivalArenaHudLayout.Stop(cards);Showing=false;VisibleCards=0;if(root!=null)root.gameObject.SetActive(false);if(icons!=null)foreach(var icon in icons)if(icon!=null)icon.gameObject.SetActive(false);if(nameVisibility!=null)for(int i=0;i<originalNames.Length;i++)if(originalNames[i]!=null)originalNames[i].enabled=nameVisibility[i];}
    }
}
