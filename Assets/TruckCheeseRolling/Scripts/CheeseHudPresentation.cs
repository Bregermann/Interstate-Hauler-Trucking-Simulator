using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckCheeseRolling
{
    [Serializable] public sealed class CheeseOpponentCard { public GameObject root; public TMP_Text name, catches; public UnityEngine.UI.Image accent; }
    [Serializable] public sealed class CheeseHudView
    {
        public RectTransform card, markerBack;
        public TMP_Text name, catches, rank, speed, clock, prompt;
        public UnityEngine.UI.Image portrait, accent, progress;
        public CheeseOpponentCard[] opponents;
    }
    public sealed class CheeseHudPresentation : MonoBehaviour
    {
        public int authoredVersion; public CheeseMenus menus; public CheeseHudView[] views;
        public GameObject[] resultCards; public TMP_Text[] resultNames, resultScores, resultPlaces;
        public UnityEngine.UI.Image[] resultPortraits, resultAccents; public TMP_Text resultSummary;
        readonly int[] last = { -1, -1, -1, -1 };
        public void Refresh()
        {
            var m = menus.match;
            for (int i = 0; i < m.setup.count; i++)
            {
                if (!m.cameras[i].view.enabled) continue; var p = m.setup.players[i]; var view = views[i]; var truck = m.trucks[i];
                view.name.text = p.name; view.name.color = view.accent.color = p.color; view.portrait.color = Color.Lerp(p.color, Color.white, .35f);
                int total = m.totals[i]; view.catches.text = total + "/5"; view.progress.fillAmount = total / 5f; view.progress.color = p.color;
                if (last[i] >= 0 && total > last[i] && !m.setup.reducedMotion) { view.catches.transform.DOKill(); view.catches.transform.localScale = Vector3.one; view.catches.transform.DOPunchScale(Vector3.one * .12f, .3f, 2).SetLink(view.catches.gameObject); } last[i] = total;
                int rank = 1; float distance = (truck.body.position - m.cheese.body.position).sqrMagnitude;
                for (int j = 0; j < m.setup.count; j++) if (j != i && (m.trucks[j].body.position - m.cheese.body.position).sqrMagnitude < distance) rank++;
                view.rank.text = m.Live ? "CHASE " + rank + "/" + m.setup.count : m.Starting ? "GET READY" : m.Phase == CheesePhase.Settling ? "GRID" : "ROUND OVER";
                view.speed.text = (truck.body.linearVelocity.magnitude * 2.2369363f).ToString("0") + " MPH";
                int seconds = Mathf.FloorToInt(m.RaceTime); view.clock.text = "R" + m.Round + "  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
                view.prompt.text = p.source == BowlingSource.Keyboard ? "ESC · MENU" : p.source == BowlingSource.AI ? "AI SPECTATOR" : "START · MENU";
                int index = 0;
                for (int slot = 0; slot < 4; slot++)
                {
                    if (slot == i) continue; var opponent = view.opponents[index++]; bool active = slot < m.setup.count; opponent.root.SetActive(active); if (!active) continue;
                    opponent.name.text = m.setup.players[slot].name; opponent.name.color = opponent.accent.color = m.setup.players[slot].color; opponent.catches.text = m.totals[slot] + "/5";
                }
                var marker = menus.indicators[i].rectTransform; view.markerBack.anchorMin = marker.anchorMin; view.markerBack.anchorMax = marker.anchorMax; view.markerBack.anchoredPosition = marker.anchoredPosition; view.markerBack.gameObject.SetActive(menus.indicators[i].gameObject.activeSelf);
            }
        }
        public void Results()
        {
            var m = menus.match;
            for (int i = 0; i < 4; i++)
            {
                resultCards[i].SetActive(i < m.setup.count); if (i >= m.setup.count) continue; var p = m.setup.players[i];
                var rect = (RectTransform)resultCards[i].transform; int columns = Mathf.Min(2, m.setup.count), rows = Mathf.CeilToInt(m.setup.count / 2f);
                float x = (i % columns - (columns - 1) * .5f) * (rect.sizeDelta.x + 40), y = 50 + ((rows - 1) * .5f - i / columns) * (rect.sizeDelta.y + 25);
                if (m.setup.count % columns != 0 && i == m.setup.count - 1) x = 0; rect.anchoredPosition = new Vector2(x, y);
                resultNames[i].text = p.name; resultNames[i].color = resultAccents[i].color = p.color; resultPortraits[i].color = Color.Lerp(p.color, Color.white, .35f); resultScores[i].text = m.totals[i] + "/5";
                int rank = 1; for (int j = 0; j < m.setup.count; j++) if (m.totals[j] > m.totals[i]) rank++;
                resultPlaces[i].text = i == m.Winner ? m.setup.count == 1 ? "FIVE CATCHES" : "WINNER" : "PLACE " + rank;
            }
            resultSummary.text = (m.setup.count == 1 ? "SOLO CHALLENGE COMPLETE" : m.Winner >= 0 ? m.setup.players[m.Winner].name + " WINS" : "MATCH COMPLETE") + "  /  " + m.Elapsed.ToString("0.0") + "s";
        }
        void OnDisable() { if (views != null) foreach (var view in views) if (view?.catches != null) { view.catches.transform.DOKill(); view.catches.transform.localScale = Vector3.one; } }
    }
}
