using DG.Tweening;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;

namespace LWS.TruckBowling
{
    // Called by the existing BowlingMenus refresh loop. Lane/session ownership,
    // physical pinfall, replay timing and the pinsetter remain unchanged.
    public sealed class BowlingHudPresentation : MonoBehaviour
    {
        public BowlingMenus menus;
        public UnityEngine.UI.Image[] portraits, accents;
        public TMP_Text[] totals, frames, states;
        public ProgressBar[] progress;
        readonly int[] previousTotals = { -1, -1, -1, -1 };
        readonly Tween[] pulses = new Tween[4];
        readonly bool[] compactTelemetry = new bool[4];
        public void RefreshLane(int index)
        {
            var lane = menus.match.lanes[index]; var player = lane.Owner; var score = player.score;
            menus.headers[index].text = player.name; menus.headers[index].color = player.color;
            portraits[index].color = Color.Lerp(player.color, Color.white, .3f); accents[index].color = player.color;
            int total = score.Finished ? score.FinalTotal : score.ResolvedTotal;
            totals[index].text = total.ToString();
            frames[index].text = score.Finished ? "FINAL SCORE" : "FRAME " + (score.Frame + 1) + "/10 · BALL " + (score.Ball + 1);
            states[index].text = score.Finished ? "COMPLETE" : "LANE " + (index + 1) + " · " + score.RemainingPins + " IN RACK";
            progress[index].currentValue = score.Finished ? 100 : (score.Frame + 1) * 10; progress[index].UpdateUI(); progress[index].barImage.color = player.color;
            if (previousTotals[index] != total)
            {
                pulses[index]?.Kill(); totals[index].transform.localScale = Vector3.one;
                if (previousTotals[index] >= 0 && !menus.match.setup.reducedMotion)
                    pulses[index] = totals[index].transform.DOPunchScale(Vector3.one * .055f, .3f, 2, .4f).SetUpdate(true);
                previousTotals[index] = total;
            }
            if(!compactTelemetry[index])
            {
                var footer=menus.guidance[index].rectTransform.parent as RectTransform;
                footer.anchorMin=footer.anchorMax=footer.pivot=new Vector2(.5f,0);footer.anchoredPosition=new Vector2(0,12);footer.sizeDelta=new Vector2(520,44);
                var label=menus.guidance[index];label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(8,2);label.rectTransform.offsetMax=new Vector2(-8,-2);label.alignment=TextAlignmentOptions.Center;
                compactTelemetry[index]=true;
            }
            menus.guidance[index].text = lane.Phase == BowlingPhase.Approach ? (Mathf.Abs(lane.truck.Speed)*2.236936f).ToString("0")+" MPH · APPROACH"
                : lane.Phase == BowlingPhase.Rolling ? (Mathf.Abs(lane.truck.Speed) * 2.236936f).ToString("0") + " MPH · PIN ACTION"
                : lane.Phase == BowlingPhase.Replay ? "REPLAY · " + lane.highlights.replay.Speed.ToString("0.##") + "× · CAMERA " + (lane.highlights.replay.Angle + 1)
                : lane.Phase == BowlingPhase.Finished ? "LANE COMPLETE"
                : "PINSETTER · SCORE LOCKED";
            menus.popups[index].text = lane.highlights != null && lane.highlights.group.alpha > .01f ? ""
                : lane.Phase >= BowlingPhase.Popup && lane.Phase <= BowlingPhase.Setting ? lane.Result : "";
            menus.popups[index].transform.parent.gameObject.SetActive(menus.popups[index].text.Length > 0);
        }
        public void ResetVisuals()
        {
            for (int i = 0; i < pulses.Length; i++)
            { pulses[i]?.Kill(); pulses[i] = null; previousTotals[i] = -1; if (totals != null && i < totals.Length && totals[i] != null) totals[i].transform.localScale = Vector3.one; }
        }
        void OnDisable() { ResetVisuals(); }
    }
}
