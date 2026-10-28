using TMPro;
using UnityEngine;
namespace LWS.TruckBallet
{
    public sealed class BalletHud:MonoBehaviour
    {
        public BalletMatch match;
        public TMP_Text[] cards;
        public TMP_Text cue;
        float next;
        void Update()
        {
            if(!match.Live||Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;
            cue.text=match.rules.Move==BalletMove.SmallHop?"SMALL HOP · RISE ABOVE1.05m, THEN LOWER GENTLY":"STEADY POSTURE · MATCH THE TARGET · HOLD1.8s";
            for(int i=0;i<cards.Length;i++)
            {
                cards[i].transform.parent.gameObject.SetActive(i<match.setup.count);if(i>=match.setup.count)continue;var actual=match.rigs[i].Achieved;var goal=match.rules.Goal(i);
                cards[i].text="<color=#"+ColorUtility.ToHtmlStringRGB(match.setup.players[i].color)+">"+match.setup.players[i].name+"</color> · "+match.rules.score[i].ToString("0")+" PTS\n"+match.rigs[i].BankLabel+" · HOLD "+Mathf.Min(BalletRules.HoldRequired,match.rules.hold[i]).ToString("0.0")+"/1.8s\nP"+actual.pitch.ToString("0")+"/"+goal.pitch.ToString("0")+"  L"+actual.lean.ToString("0")+"/"+goal.lean.ToString("0")+"  H"+actual.lift.ToString("0.00")+"/"+goal.lift.ToString("0.00")+"m\nTURN "+actual.turnRate.ToString("0")+"/"+goal.turnRate.ToString("0")+" deg/s · "+match.rules.turn[i].ToString("0")+" deg";
            }
        }
    }
}
