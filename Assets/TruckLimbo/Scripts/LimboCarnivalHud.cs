using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LWS.TruckSports;

namespace LWS.TruckLimbo
{
    [DefaultExecutionOrder(650)] public sealed class LimboCarnivalHud:MonoBehaviour
    {
        public LimboMatch match;public RectTransform root,row,contextRoot;public TMP_Text context;public CarnivalArenaHudCard[] cards;public Graphic[] legacyGraphics;
        readonly CarnivalArenaHudSuppression suppression=new CarnivalArenaHudSuppression();readonly int[] order=new int[8];
        public bool Showing{get;private set;}public int VisibleCards{get;private set;}
        public int SlotAtRank(int rank)=>order[rank];
        public string ClearanceState(int slot)
        {
            var racer=match.racers[slot];
            if(racer.Phase==LimboAttempt.Eliminated)return "ELIMINATED";
            if(racer.Phase==LimboAttempt.Clear)return "CLEARED";
            if(racer.Phase==LimboAttempt.Failed)return racer.HitBar?"BAR HIT":"FAILED";
            if(racer.Phase==LimboAttempt.Waiting)return "READY";
            if(racer.AttemptTime>25)return Mathf.Max(0,35-racer.AttemptTime).ToString("F0")+"s LEFT";
            return float.IsFinite(racer.ClosestClearance)?Mathf.Max(0,racer.ClosestClearance*100).ToString("F0")+"cm CLEARANCE":"APPROACH";
        }
        void LateUpdate()
        {
            Showing=CarnivalArenaHudLayout.Playing(match);root.gameObject.SetActive(Showing);suppression.Present(match,legacyGraphics,Showing);VisibleCards=Showing?match.setup.count:0;if(!Showing)return;
            for(int i=0;i<VisibleCards;i++)order[i]=i;
            for(int i=1;i<VisibleCards;i++){int slot=order[i],place=match.ResultPlace(slot),j=i-1;while(j>=0&&match.ResultPlace(order[j])>place){order[j+1]=order[j];j--;}order[j+1]=slot;}
            CarnivalArenaHudLayout.Row(row,cards,VisibleCards,false,0,170,order);
            for(int rank=0;rank<VisibleCards;rank++){int slot=order[rank];CarnivalArenaHudLayout.Data(cards[slot],match,slot,match.rules.points[slot]+"P",ClearanceState(slot));}
            context.text="ROUND "+match.rules.Round+" · BAR "+match.rules.Height.ToString("F2")+"m"+(match.Phase==LimboPhase.CountIn?" · START IN "+Mathf.Max(1,Mathf.CeilToInt(3-match.Age)):match.Phase==LimboPhase.RoundResult?" · RESULTS LOCKED":"");
        }
        void OnDisable(){suppression.Restore();CarnivalArenaHudLayout.Stop(cards);Showing=false;VisibleCards=0;if(root!=null)root.gameObject.SetActive(false);}
    }
}
