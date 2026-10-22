using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LWS.TruckSports;

namespace LWS.TruckHockey
{
    // Read-only view of HockeyRules. Team accents are separate from Your Crew colors.
    public sealed class HockeyPresentation : MonoBehaviour
    {
        public HockeyMenus menus;
        public RectTransform scoreBug,noticePlate;
        public TMP_Text blueTitle,orangeTitle,matchType,pauseSummary;
        public TMP_Text[] participantNames,participantSources,setupSlots;
        public Image[] portraits,stripes,setupPortraits;
        public TMP_Text[] resultTeams;
        public static readonly Color Blue=new Color(.2f,.72f,1),Orange=new Color(1,.49f,.14f);
        int lastBlue=-1,lastOrange=-1;HockeyPhase lastPhase;float next;
        public static Color TeamColor(int team)=>team==0?Blue:Orange;
        public static string SourceLabel(HockeyPlayer player)
        {
            switch(player.source)
            {
                case HockeySource.AI:return "AI · "+player.difficulty.ToString().ToUpperInvariant();
                case HockeySource.Keyboard:return "KEYBOARD";
                case HockeySource.LeftStick:return "LEFT STICK";
                case HockeySource.RightStick:return "RIGHT STICK";
                default:return "GAMEPAD";
            }
        }
        void LateUpdate(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;Refresh();}
        public void Refresh()
        {
            if(menus==null||menus.match==null||scoreBug==null)return;
            var match=menus.match;var rules=match.rules;
            for(int i=0;i<6;i++)
            {
                var player=match.players[i];participantNames[i].text=player.name;participantNames[i].color=stripes[i].color=player.color;
                portraits[i].color=setupPortraits[i].color=Color.Lerp(player.color,Color.white,.3f);
                participantSources[i].text=(i<3?"B":"O")+(i%3+1)+" · "+SourceLabel(player);
                setupSlots[i].color=TeamColor(i/3);
            }
            menus.color1Text.color=Blue;menus.color2Text.color=Orange;
            if(pauseSummary!=null)pauseSummary.text="BLUE  "+rules.Score[0]+"  :  "+rules.Score[1]+"  ORANGE   ·   "+Clock(rules);
            if(menus.Screen==HockeyScreen.Result)
            {
                menus.resultText.text=match.TeamName(rules.Winner)+" WINS\n<size=64>"+rules.Score[0]+"  :  "+rules.Score[1]+"</size>\n<size=24>"+(rules.Overtime?"SUDDEN-DEATH VICTORY":"REGULATION COMPLETE")+" · "+rules.Goals+" GOALS</size>";
                menus.resultText.color=TeamColor(rules.Winner);
                for(int team=0;team<2;team++)resultTeams[team].text=match.TeamName(team)+"\n<size=23>"+match.PlayerName(team*3)+" · "+match.PlayerName(team*3+1)+" · "+match.PlayerName(team*3+2)+"</size>";
            }
            if(!match.InMatch)return;
            menus.scoreText.text=rules.Score[0]+"   :   "+rules.Score[1];menus.scoreText.color=SportsBroadcastCanvas.Cream;
            menus.clockText.text=Clock(rules);matchType.text=rules.Overtime?"NEXT GOAL WINS":"TRUCK HOCKEY · 3v3";
            menus.teamText.text="BLUE · DEFENDS "+(rules.DefendingSign(0)<0?"LEFT":"RIGHT");menus.teamText.color=Blue;
            menus.orangeTeamText.text="ORANGE · DEFENDS "+(rules.DefendingSign(1)<0?"LEFT":"RIGHT");menus.orangeTeamText.color=Orange;
            var phase=rules.Phase;
            string notice=phase==HockeyPhase.Pregame||phase==HockeyPhase.Faceoff?(match.Notice.StartsWith("PUCK RECOVERY")?"PUCK RECOVERY · ":"FACEOFF · ")+Mathf.Max(1,Mathf.CeilToInt(rules.PhaseTimer)):phase==HockeyPhase.Goal?match.Notice:rules.Overtime?"SUDDEN DEATH":"";
            menus.messageText.text=notice;menus.messageText.color=phase==HockeyPhase.Goal?TeamColor(rules.LastScorer):SportsBroadcastCanvas.Cream;noticePlate.gameObject.SetActive(notice.Length>0);
            menus.controlsText.text="3v3 · DRIVE INTO THE PUCK    |    "+rules.Goals+" GOALS    |    ESC / START · PAUSE";
            if(menus.Screen==HockeyScreen.Match&&lastBlue>=0&&(lastBlue!=rules.Score[0]||lastOrange!=rules.Score[1]))
            {
                menus.scoreText.transform.DOKill();menus.scoreText.transform.localScale=Vector3.one;
                menus.scoreText.transform.DOPunchScale(Vector3.one*.1f,.3f,2,.4f).SetUpdate(true).SetLink(gameObject);
            }
            if(menus.Screen==HockeyScreen.Match&&phase!=lastPhase&&notice.Length>0)
            {
                noticePlate.DOKill();noticePlate.localScale=Vector3.one;
                noticePlate.DOPunchScale(Vector3.one*.04f,.25f,2,.4f).SetUpdate(true).SetLink(gameObject);
            }
            lastBlue=rules.Score[0];lastOrange=rules.Score[1];lastPhase=phase;
        }
        static string Clock(HockeyRules rules)
        {
            if(rules.Overtime)return "OVERTIME";
            int seconds=Mathf.CeilToInt(rules.Remaining);return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00");
        }
        void OnDisable(){if(menus!=null&&menus.scoreText!=null){menus.scoreText.transform.DOKill();menus.scoreText.transform.localScale=Vector3.one;}if(noticePlate!=null){noticePlate.DOKill();noticePlate.localScale=Vector3.one;}}
    }
}
