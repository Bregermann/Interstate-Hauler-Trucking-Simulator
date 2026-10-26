using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LWS.TruckSports;

namespace LWS.TruckBalloonBattle
{
    [DefaultExecutionOrder(650)] public sealed class BalloonCarnivalHud:MonoBehaviour
    {
        public BalloonMatch match;public RectTransform root,row,countdownRoot;public TMP_Text countdown;public CarnivalArenaHudCard[] cards;public Graphic[] legacyGraphics;
        readonly CarnivalArenaHudSuppression suppression=new CarnivalArenaHudSuppression();
        public bool Showing{get;private set;}public int VisibleCards{get;private set;}
        public string SpikeState(int slot)
        {
            if(!match.rules.Alive(slot))return "ELIMINATED";
            float age=match.rules.AttackAge(slot);
            if(match.rules.AttackSequence(slot)>0&&age>=0&&age<match.rules.telegraphSeconds)return "SPIKE WARNING";
            return match.rules.Extended(slot)?"SPIKE OUT":match.rules.Cooldown(slot)>0?"READY "+match.rules.Cooldown(slot).ToString("F1")+"s":"SPIKE READY";
        }
        void LateUpdate()
        {
            Showing=CarnivalArenaHudLayout.Playing(match);root.gameObject.SetActive(Showing);suppression.Present(match,legacyGraphics,Showing);VisibleCards=Showing?match.setup.count:0;if(!Showing)return;
            CarnivalArenaHudLayout.Row(row,cards,VisibleCards,false);
            for(int i=0;i<VisibleCards;i++)
            {
                CarnivalArenaHudLayout.Data(cards[i],match,i,match.rules.scores[i]+"P",SpikeState(i));
                for(int balloon=0;balloon<3;balloon++)cards[i].pips[balloon].color=match.rules.balloons[i,balloon]?match.setup.players[i].color:new Color(.25f,.28f,.32f,.6f);
            }
            countdownRoot.gameObject.SetActive(match.Countdown>0);if(match.Countdown>0)countdown.text="START IN "+Mathf.CeilToInt(match.Countdown);
        }
        void OnDisable(){suppression.Restore();CarnivalArenaHudLayout.Stop(cards);Showing=false;VisibleCards=0;if(root!=null)root.gameObject.SetActive(false);}
    }
}
