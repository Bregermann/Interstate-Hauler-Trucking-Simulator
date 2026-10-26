using DG.Tweening;
using LWS.TruckHockey;
using LWS.TruckSports;
using TMPro;
using UnityEngine;

namespace LWS.TruckAirHockey
{
    // Read-only UI owned by AirHockeyMenus' refresh/lifecycle. It never moves
    // the trucks/puck, changes a rule or accepts a gameplay/menu input.
    public sealed class AirHockeyPresentation : MonoBehaviour
    {
        public AirHockeyMenus menus;
        public RectTransform scoreboard;
        public UnityEngine.UI.Image[] portraits, setupPortraits, setupAccents, resultPortraits, resultAccents;
        public TMP_Text[] names, points, setupStatus, resultNames, resultPoints, resultRanks, pauseScores;
        public TMP_Text clock, controls;
        public GameObject[] legacyDocks;
        int lastGoals = -1;
        readonly Tween[] pulses = new Tween[2];
        static string KeyName(UnityEngine.InputSystem.Key key)
        {
            switch (key) { case UnityEngine.InputSystem.Key.UpArrow: return "UP"; case UnityEngine.InputSystem.Key.LeftArrow: return "LEFT"; case UnityEngine.InputSystem.Key.DownArrow: return "DOWN"; case UnityEngine.InputSystem.Key.RightArrow: return "RIGHT"; default: return key.ToString().ToUpperInvariant(); }
        }
        static string Input(HockeyPlayer player)
        {
            if (player.source == HockeySource.AI) return "AI · " + player.difficulty.ToString().ToUpperInvariant();
            if (player.source == HockeySource.Keyboard) return KeyName(player.forward) + "/" + KeyName(player.left) + "/" + KeyName(player.back) + "/" + KeyName(player.right);
            return player.source == HockeySource.LeftStick ? "LEFT STICK" : player.source == HockeySource.RightStick ? "RIGHT STICK" : "LEFT STICK · RT/LT";
        }
        public void Refresh()
        {
            var match = menus.match; var rules = match.rules;
            foreach (var dock in legacyDocks) if (dock != null) dock.SetActive(false);
            menus.scoreText.gameObject.SetActive(false); menus.resultText.gameObject.SetActive(false);
            controls.gameObject.SetActive(false);
            for (int side = 0; side < 2; side++)
            {
                var player = match.players[side]; var color = player.color;
                names[side].text = resultNames[side].text = player.name;
                names[side].color = resultNames[side].color = color;
                points[side].text = resultPoints[side].text = rules.Score[side].ToString();
                portraits[side].color = setupPortraits[side].color = resultPortraits[side].color = Color.Lerp(color, Color.white, .3f);
                setupAccents[side].color = resultAccents[side].color = color;
                setupStatus[side].text = Input(player) + (player.controller ? match.Ready(side) ? " · CONNECTED" : " · ASSIGN A PAD" : "");
                setupStatus[side].rectTransform.anchoredPosition = new Vector2(0, player.source == HockeySource.AI ? -199 : -51);
                pauseScores[side].text = player.name + "   " + rules.Score[side]; pauseScores[side].color = color;
                resultRanks[side].text = rules.Winner == side ? "WINNER" : "RUNNER-UP"; resultRanks[side].color = rules.Winner == side ? color : SportsBroadcastCanvas.Cream;
            }
            int seconds = Mathf.FloorToInt(match.MatchSeconds);
            clock.text = "PLAY " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            menus.serveText.text = rules.Phase == AirHockeyPhase.Countdown ? match.players[rules.Server].name + " SERVES · " + Mathf.Max(1, Mathf.CeilToInt(rules.Timer))
                : rules.Phase == AirHockeyPhase.Goal ? "GOAL · " + match.players[rules.Scorer].name : "FIRST TO 7 · " + match.players[rules.Server].name + " SERVES";
            menus.serveText.color = match.players[rules.Phase == AirHockeyPhase.Goal ? rules.Scorer : rules.Server].color;
            menus.noticeText.text = match.Notice;
            menus.noticeText.color = rules.Phase == AirHockeyPhase.Goal ? match.players[rules.Scorer].color : SportsBroadcastCanvas.Cream;
            controls.text = match.AllAI ? "AI BROADCAST · ESC / START PAUSE" : match.players[0].name + ": " + Input(match.players[0]) + "   ·   " + match.players[1].name + ": " + Input(match.players[1]) + "   ·   ESC / START PAUSE";
            if (rules.Goals < lastGoals) ResetEffects();
            if (lastGoals >= 0 && rules.Goals > lastGoals && rules.Scorer >= 0)
            {
                int scorer = rules.Scorer; pulses[scorer]?.Kill(); points[scorer].transform.localScale = Vector3.one;
                pulses[scorer] = points[scorer].transform.DOPunchScale(Vector3.one * .1f, .35f, 2, .4f).SetUpdate(true);
            }
            lastGoals = rules.Goals;
        }
        public void ResetEffects()
        {
            for (int i = 0; i < pulses.Length; i++) { pulses[i]?.Kill(); pulses[i] = null; if (points != null && i < points.Length && points[i] != null) points[i].transform.localScale = Vector3.one; }
            lastGoals = -1;
        }
        void OnDisable() { ResetEffects(); }
    }
}
