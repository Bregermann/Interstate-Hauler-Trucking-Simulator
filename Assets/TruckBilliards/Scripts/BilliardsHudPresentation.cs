using DG.Tweening;
using LWS.TruckSports;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;

namespace LWS.TruckBilliards
{
    // Read-only presentation. BilliardsHUD retains its existing 10 Hz refresh
    // authority; rules, series, input ownership and physical shots are unchanged.
    public sealed class BilliardsHudPresentation : MonoBehaviour
    {
        public BilliardsHUD hud;
        public GameObject content;
        public RectTransform[] cards;
        public UnityEngine.UI.Image[] accents, portraits, turns;
        public TMP_Text[] names, scores, groups, remaining;
        public UnityEngine.UI.Image[] ballFaces;
        public TMP_Text[] ballNumbers;
        public TMP_Text rack, turn, message, powerText, speedText, bindings;
        public ProgressBar power;
        int lastTurn = -1;
        Tween pulse;

        public static Color BallColor(int number)
        {
            switch ((number - 1) % 8 + 1)
            {
                case 1: return new Color(1, .76f, .025f);
                case 2: return new Color(.025f, .15f, .8f);
                case 3: return new Color(.8f, .035f, .055f);
                case 4: return new Color(.36f, .055f, .56f);
                case 5: return new Color(1, .28f, .035f);
                case 6: return new Color(.035f, .34f, .12f);
                case 7: return new Color(.38f, .025f, .04f);
                default: return new Color(.015f, .018f, .024f);
            }
        }

        public void Refresh(bool display)
        {
            content.SetActive(display);
            if (!display) { ResetPulse(); return; }
            var session = hud.match.session;
            var rules = hud.match.rules;
            int active = rules.Phase == MatchPhase.Ended ? Mathf.Clamp(rules.Winner, 0, 1) : rules.Turn;
            for (int side = 0; side < 2; side++)
            {
                bool shown = !session.IsPractice || side == 0;
                cards[side].gameObject.SetActive(shown);
                if (!shown) continue;
                var color = session.players[side].color;
                accents[side].color = color;
                portraits[side].color = Color.Lerp(color, Color.white, .3f);
                turns[side].color = side == active ? color : new Color(.2f, .24f, .28f, .55f);
                names[side].text = session.Name(side);
                names[side].color = color;
                scores[side].text = session.IsPractice ? "PRACTICE" : session.series.Wins[side].ToString();
                int group = rules.Group(side), count = 0, slot = 0;
                bool eight = !session.IsPractice && group != 0 && rules.Remaining(side) == 0;
                groups[side].text = session.IsPractice ? "FREE PLAY" : eight ? "8-BALL NEXT" : group == 0 ? "OPEN TABLE" : group == 1 ? "SOLIDS" : "STRIPES";
                for (int n = 1; n <= 15; n++)
                {
                    bool eligible = session.IsPractice || (eight ? n == 8 : group == 0 ? n != 8 : BilliardsRules.BallGroup(n) == group);
                    if (!eligible) continue;
                    int index = side * 15 + slot++;
                    bool live = !rules.Pocketed.Contains(n);
                    if (live) count++;
                    ballFaces[index].transform.parent.gameObject.SetActive(true);
                    ballFaces[index].color = BallColor(n);
                    ballFaces[index].rectTransform.sizeDelta = new Vector2(30, n > 8 ? 13 : 30);
                    ballNumbers[index].text = n.ToString();
                    ballFaces[index].transform.parent.GetComponent<CanvasGroup>().alpha = live ? 1 : .22f;
                }
                for (; slot < 15; slot++) ballFaces[side * 15 + slot].transform.parent.gameObject.SetActive(false);
                remaining[side].text = count + (eight ? " TO WIN" : " REMAINING");
            }
            rack.text = session.IsPractice ? "TRUCK BILLIARDS · PRACTICE" : "RACK " + session.series.RackNumber + " · FIRST TO " + session.series.Required;
            turn.text = rules.Phase == MatchPhase.Resolving ? "BALLS IN MOTION" : rules.Phase == MatchPhase.Placement ? "CUE BALL IN HAND" : rules.Phase == MatchPhase.Ended ? "RACK COMPLETE" : session.Name(active).ToUpperInvariant() + " · " + (session.IsAI(active) ? "AI SHOT" : "YOUR SHOT");
            turn.color = session.players[active].color;
            message.text = rules.Notice;
            int owner = session.mode == BilliardsMode.HotSeat ? session.sharedOwner : rules.Turn;
            var truck = session.players[owner].truck;
            float strength = Mathf.Clamp01(truck.Speed * truck.powerScale / truck.maxShotSpeed) * 100;
            power.currentValue = strength;
            power.UpdateUI();
            power.barImage.color = truck.Armed ? session.players[owner].color : new Color(.35f, .4f, .46f);
            powerText.text = "SHOT POWER " + Mathf.RoundToInt(strength) + "% · " + (truck.Armed ? "READY" : "DISARMED");
            speedText.text = (Mathf.Abs(truck.Speed) * 2.236936f).ToString("0.0") + " MPH";
            bindings.text = session.Bindings(owner).Replace('\n', ' ');
            // Actual assigned bindings remain in setup/help. Active play keeps
            // the existing power, speed, turn and outcome cards instead.
            bindings.gameObject.SetActive(false);
            if (lastTurn != active)
            {
                ResetPulse(); lastTurn = active;
                pulse = turns[active].transform.DOScaleY(.65f, .22f).SetLoops(2, LoopType.Yoyo).SetUpdate(true);
            }
        }
        void ResetPulse()
        {
            pulse?.Kill(); pulse = null;
            if (turns != null) foreach (var marker in turns) if (marker != null) marker.transform.localScale = Vector3.one;
        }
        void OnDisable() { ResetPulse(); lastTurn = -1; }
        void OnDestroy() { ResetPulse(); }
    }
}
