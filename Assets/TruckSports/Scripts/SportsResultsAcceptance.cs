#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LWS.TruckSports
{
    // Explicit development acceptance after a genuine completed match. This
    // inspects the real saved results board and captures the Windows backbuffer.
    public static class SportsResultsAcceptance
    {
        public static IEnumerator Capture(SportsEventSession session, Action<bool, string> check, string folder,
            Func<int, int> actualPlace, Func<int, double?> actualMeasurement, Func<int, bool> actualWinner = null)
        {
            var board = session.menus.GetComponentInChildren<SportsResultsPresentation>(true);
            check(session.Finished && session.menus.Screen == SportsEventScreen.Result, "physical match reaches the original results lifecycle before inspecting ranked cards");
            check(board != null, "saved opt-in ranked results board exists"); if (board == null) yield break;
            yield return new WaitForSecondsRealtime(1.6f);
            var snapshot = board.Snapshot;
            check(board.Displaying && snapshot != null && snapshot.Rows.Count == session.setup.count, "ranked cards finish their DOTween reveal for every participant"); if (snapshot == null) yield break;
            bool identities = true, measurements = true, ranks = true;
            foreach (var entry in snapshot.Rows)
            {
                var player = session.setup.players[entry.Slot];
                identities &= entry.ParticipantId == player.participantId && entry.DisplayName == player.name && entry.Color == player.color && entry.IsAI == (player.source == LWS.TruckBowling.BowlingSource.AI);
                ranks &= entry.Place == actualPlace(entry.Slot) && entry.Winner == (actualWinner != null ? actualWinner(entry.Slot) : actualPlace(entry.Slot) == 1);
                measurements &= entry.Measurement == actualMeasurement(entry.Slot);
            }
            check(identities && snapshot.Rows.Select(row => row.ParticipantId).Distinct().Count() == snapshot.Rows.Count, "result identities, names, colors and AI flags match the actual roster independently of names");
            check(ranks, "displayed ranks and declared winners match the original rules");
            check(measurements, "final displayed measurements match the authoritative completed-match statistics");
            check(!session.menus.result.enabled, "only the accepted typed board hides the original text; original result data remains intact");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                yield return SportsWindowCapture.Save(size.x, size.y, folder + "/ranked-results-" + size.y + ".png");
                Canvas.ForceUpdateCanvases(); bool bounds = true, text = true; var page = session.menus.pages[(int)SportsEventScreen.Result];
                var labels = new[] { page.transform.Find("Title"), page.transform.Find("Subtitle"), board.heading.transform, board.summary.transform };
                var active = board.rows.Where(row => row.rect.gameObject.activeInHierarchy).ToArray(); var canvas = Bounds(session.menus.GetComponent<RectTransform>());
                for (int i = 0; i < active.Length; i++)
                {
                    var row = Bounds(active[i].rect); bounds &= row.xMin >= canvas.xMin - .1f && row.xMax <= canvas.xMax + .1f && row.yMin >= canvas.yMin - .1f && row.yMax <= canvas.yMax + .1f;
                    foreach (var action in page.GetComponentsInChildren<SportsMenuAction>(true)) bounds &= !row.Overlaps(Bounds(action.GetComponent<RectTransform>()));
                    foreach (var label in labels) bounds &= !row.Overlaps(Bounds(label.GetComponent<RectTransform>()));
                    for (int j = i + 1; j < active.Length; j++) bounds &= !row.Overlaps(Bounds(active[j].rect));
                    foreach (TMP_Text label in new[] { active[i].place, active[i].name, active[i].control, active[i].value, active[i].detail }) { label.ForceMeshUpdate(); text &= !label.isTextOverflowing; }
                }
                check(bounds, size.x + "x" + size.y + " ranked cards remain screen-safe and clear of headings, other rows and original controls");
                check(text, size.x + "x" + size.y + " result names, values and real statistics have no TMP overflow");
                bool values = active.Length == snapshot.Rows.Count;
                for (int i = 0; i < active.Length && i < snapshot.Rows.Count; i++) values &= active[i].value.text == snapshot.Rows[i].Value;
                check(values, size.x + "x" + size.y + " completed animated metric labels match the immutable result display values");
            }
        }
        static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(point => point.x), corners.Min(point => point.y), corners.Max(point => point.x), corners.Max(point => point.y));
        }
    }
}
#endif
