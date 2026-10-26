using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LWS.TruckSports;

namespace LWS.TruckHotPotato
{
    [DefaultExecutionOrder(650)] public sealed class HotPotatoSplitHud:MonoBehaviour
    {
        public HotPotatoMatch match;public RectTransform root,row,contextRoot;public TMP_Text context;public CarnivalArenaHudCard[] cards;public Graphic[] legacyGraphics;
        readonly CarnivalArenaHudSuppression suppression=new CarnivalArenaHudSuppression();
        public bool Showing{get;private set;}public int VisibleCards{get;private set;}
        void LateUpdate()
        {
            // Arena mode is the explicit protected exception, even with one full-screen camera.
            Showing=CarnivalArenaHudLayout.Playing(match)&&match.MultiViewHUD;root.gameObject.SetActive(Showing);suppression.Present(match,legacyGraphics,Showing);VisibleCards=Showing?match.setup.count:0;if(!Showing)return;
            bool center=CarnivalArenaHudLayout.Views(match)>1;CarnivalArenaHudLayout.Row(row,cards,VisibleCards,center,center?4:0);
            for(int i=0;i<VisibleCards;i++)
            {
                bool owner=false;for(int view=0;view<match.drivingViews.rally.Humans;view++)if(match.drivingViews.rally.owners[view]==i)owner=true;
                CarnivalArenaHudLayout.Data(cards[i],match,i,match.rules.scores[i]+"P",!match.rules.alive[i]?"ELIMINATED":match.rules.Carrier==i?"BOMB":"ALIVE",-1,true,owner);
            }
            context.text=match.Countdown>0?"START IN "+Mathf.CeilToInt(match.Countdown):match.rules.Carrier>=0?match.setup.players[match.rules.Carrier].name+" · BOMB "+match.rules.Fuse.ToString("F1")+"s"+(match.rules.Cooldown>0?" · LOCK "+match.rules.Cooldown.ToString("F1")+"s":"")+" · CYCLE "+match.rules.Cycle:"LAST TRUCK STANDING";
            context.color=match.rules.Carrier>=0&&match.rules.Fuse<=match.urgentFuseSeconds?new Color(1,.36f,.25f):SportsBroadcastCanvas.Cream;
        }
        void OnDisable(){suppression.Restore();CarnivalArenaHudLayout.Stop(cards);Showing=false;VisibleCards=0;if(root!=null)root.gameObject.SetActive(false);}
    }
}
