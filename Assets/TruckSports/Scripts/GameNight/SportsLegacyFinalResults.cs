using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace LWS.TruckSports
{
    // The older sessions keep their original lifecycle, rules and specialized
    // scoreboards. This bridge reads only a completed sport/series result.
    public static class SportsLegacyFinalResults
    {
        public static bool TryCapture(Component owner, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            switch (owner)
            {
                case LWS.TruckBowling.BowlingMatch m when m.InMatch && m.Finished:
                {
                    int[] totals = m.setup.players.Take(m.setup.count).Select(p => p.score.FinalTotal).ToArray();
                    snapshot = Capture(m.setup.players, m.setup.count, "TRUCK BOWLING RESULTS", "TEN FRAMES · FINAL BONUS-RESOLVED TOTALS",
                        i => SportsResultPlacings.FromFinalTotals(i, m.setup.count, totals), i => Array.IndexOf(m.Winners, i) >= 0, i => totals[i], "PTS");
                    break;
                }
                case LWS.TruckCornhole.CornholeMatch m when m.InMatch && m.Finished:
                    return SportsFinishResultsSource.TryCaptureCornhole(m, out snapshot);
                case LWS.TruckCheeseRolling.CheeseMatch m when m.InMatch && m.Finished:
                    snapshot = Capture(m.setup.players, m.setup.count, "CHEESE ROLLING RESULTS", "COMPLETE RACE SERIES · BANKED POINTS",
                        i => SportsResultPlacings.FromFinalTotals(i, m.setup.count, m.totals, m.Winner), i => m.Winner == i, i => m.totals[i], "PTS");
                    break;
                case LWS.TruckHockey.HockeyMatch m when m.InMatch && m.rules.Phase == LWS.TruckHockey.HockeyPhase.Finished:
                    snapshot = Capture(m.players, m.players.Length, "TRUCK HOCKEY RESULTS", "FINAL TEAM GOALS · INCLUDING OVERTIME",
                        i => m.rules.Winner < 0 ? 0 : m.TeamOf(i) == m.rules.Winner ? 1 : 2,
                        i => m.rules.Winner >= 0 && m.TeamOf(i) == m.rules.Winner, i => m.rules.Score[m.TeamOf(i)], "TEAM GOALS");
                    break;
                case LWS.TruckAirHockey.AirHockeyMatch m when m.InMatch && m.rules.Phase == LWS.TruckAirHockey.AirHockeyPhase.Finished:
                    snapshot = Capture(m.players, 2, "TRUCK AIR HOCKEY RESULTS", "FIRST TO SEVEN · COMPLETE MATCH",
                        i => m.rules.Winner == i ? 1 : 2, i => m.rules.Winner == i, i => m.rules.Score[i], "GOALS");
                    break;
                case LWS.TruckWallball.WallballMatch m when m.InMatch && m.rules.Phase == LWS.TruckWallball.WallballPhase.Finished:
                    return SportsFinishResultsSource.TryCaptureWallball(m, out snapshot);
                case LWS.TruckBilliards.BilliardsSession m when m.InMatch && m.series.Finished:
                    snapshot = Capture(m.players, m.IsPractice ? 1 : 2, m.IsPractice ? "BILLIARDS PRACTICE" : "TRUCK BILLIARDS SERIES RESULTS",
                        "FINAL RACK WINS · SERIES CHAMPION", i => m.IsPractice ? 0 : m.series.Winner == i ? 1 : 2,
                        i => !m.IsPractice && m.series.Winner == i, i => m.series.Wins[i], "RACK WINS",
                        m.IsPractice ? SportsMatchKind.Practice : SportsMatchKind.Competitive);
                    break;
            }
            return snapshot != null;
        }
        static SportsResultsSnapshot Capture(ISportsParticipantSlot[] players, int count, string title, string detail,
            Func<int, int> place, Func<int, bool> winner, Func<int, double> value, string unit,
            SportsMatchKind kind = SportsMatchKind.Competitive)
        {
            var rows = new List<SportsResultRow>(count);
            for (int i = 0; i < count; i++) rows.Add(new SportsResultRow(players[i], i, place(i), winner(i), value(i), unit,
                "AUTHORITATIVE FINAL RESULT", "—", "0", SportsRecordPreference.Higher));
            return new SportsResultsSnapshot(title, detail, rows, kind);
        }
    }
}
