using System;
using System.Collections.Generic;
using LWS.TruckBeerPong;
using LWS.TruckBungeeDemolition;
using LWS.TruckCurling;
using LWS.TruckDanceOff;
using LWS.TruckDerby;
using LWS.TruckFareFight;
using LWS.TruckMusicalParking;
using LWS.TruckSnake;

namespace LWS.TruckSports
{
    // Prepared only. Reads retained results; does not change any game authority.
    public static class SportsRoundLegacyResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (session == null || !session.Finished) return false;
            if (session is PongMatch pong)
            {
                var rows = new List<SportsResultRow>(session.setup.count);
                for (int i = 0; i < session.setup.count; i++)
                {
                    int team = pong.Practice || i < pong.rules.TeamA ? 0 : 1;
                    bool winner = !pong.Practice && team == pong.rules.Winner;
                    rows.Add(new SportsResultRow(session.setup.players[i], i, pong.Practice || pong.rules.Winner < 0 ? 0 : winner ? 1 : 2,
                        winner, pong.rules.hits[team], pong.Practice ? "CUPS HIT" : "TEAM CUPS HIT",
                        (team == 0 ? "BLUE" : "ORANGE") + " · " + pong.rules.shots[team] + (pong.Practice ? " SHOTS · " : " TEAM SHOTS · ") + pong.rules.remaining[team] + " OWN CUPS LEFT", format: "0"));
                }
                snapshot = new SportsResultsSnapshot(pong.Practice ? "BEER PONG PRACTICE" : "BEER PONG RESULTS",
                    pong.Practice ? pong.rules.PracticeRacks + " RACKS COMPLETED" : pong.rules.Overtime + " OVERTIME PERIODS · SHARED TEAM STATISTICS", rows);
            }
            else if (session is BungeeMatch bungee)
                snapshot = Capture(session, "BUNGEE DEMOLITION RESULTS", bungee.CompletedRounds + " ROUNDS · BANKED DESTRUCTION POINTS"
                    + (session.setup.count == 1 ? " · PERSONAL BEST " + SportsPrefs.GetInt("TruckBungeeDemolition.PersonalBest", 0) + " PTS" : ""),
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, bungee.total, bungee.Winner), i => bungee.Winner == i,
                    i => bungee.total[i], "PTS", i => "FINAL ROUND " + bungee.bays[i].Score + " PTS");
            else if (session is CurlingMatch curling)
            {
                int winner = curling.scores[0] == curling.scores[1] ? -1 : curling.scores[0] > curling.scores[1] ? 0 : 1;
                snapshot = Capture(session, "CURLING TEAM RESULTS", curling.EndsScored + " ENDS · " + curling.GuardViolations + " GUARD-ZONE RESTORATIONS",
                    i => winner < 0 ? 0 : (i < curling.teamA ? 0 : 1) == winner ? 1 : 2,
                    i => winner >= 0 && (i < curling.teamA ? 0 : 1) == winner,
                    i => curling.scores[i < curling.teamA ? 0 : 1], "TEAM PTS", i => i < curling.teamA ? "BLUE TEAM" : "ORANGE TEAM");
            }
            else if (session is DanceMatch dance)
                snapshot = Capture(session, "DANCE-OFF RESULTS", dance.Round + " SONGS · TOTAL POINTS / FINAL SONG STATISTICS",
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, dance.total),
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, dance.total) == 1,
                    i => dance.total[i], "PTS", i => DanceDetail(dance, i));
            else if (session is DerbyMatch derby)
                snapshot = Capture(session, "DEMOLITION DERBY CHAMPION", derby.rules.Heat + " HEATS · " + derby.Hits + " QUALIFYING IMPACTS · " + derby.Explosions + " EXPLOSIONS · " + derby.Draws + " DRAWS",
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, derby.rules.wins, derby.rules.Winner), i => derby.rules.Winner == i,
                    i => derby.rules.wins[i], "WINS", i => "FINAL HEAT · " + (derby.rules.alive[i] ? "SURVIVED" : derby.rules.reasons[i]));
            else if (session is FareFightMatch fare)
                snapshot = Capture(session, "FARE FIGHT RESULTS", fare.rules.Clock.ToString("F1") + " SEC PLAYED · SUCCESSFUL PASSENGER DELIVERIES",
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, fare.rules.score), i => fare.rules.winners[i],
                    i => fare.rules.score[i], "FARES", i => fare.rules.steals[i] + " PASSENGER STEALS");
            else if (session is SnakeMatch snake)
                snapshot = Capture(session, "TRUCK SNAKE RESULTS", "ACTUALLY ATTACHED TRAILERS · FINAL SCORE MULTIPLIERS",
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, snake.rules.score), i => snake.rules.winners[i],
                    i => snake.rules.score[i], "PTS", i => SnakeDetail(snake, i));
            else if (session is ParkingMatch parking)
            {
                var rows = new List<SportsResultRow>(session.setup.count);
                for (int i = 0; i < session.setup.count; i++)
                {
                    bool winner = parking.Winner == i;
                    rows.Add(new SportsResultRow(session.setup.players[i], i, winner ? 1 : 0, winner, null, "",
                        "FINAL ROUND · " + parking.Eligibility(i), winner ? "LAST SPACE" : "ELIMINATED"));
                }
                snapshot = new SportsResultsSnapshot(parking.Winner < 0 ? "NO PARKING SURVIVOR" : "MUSICAL PARKING CHAMPION",
                    parking.Round + " ROUNDS · " + parking.TotalCaptures + " PHYSICAL BAY CAPTURES", rows);
            }
            return snapshot != null;
        }
        static string DanceDetail(DanceMatch match, int slot)
        {
            var rule = match.Rules[slot];
            return "FINAL SONG · " + rule.Perfect + " PERFECT · " + rule.Great + " GREAT · " + rule.Good + " GOOD · " + rule.Miss + " MISS · MAX COMBO " + rule.MaximumCombo;
        }
        static string SnakeDetail(SnakeMatch match, int slot)
        {
            var rules = match.rules;
            // The original completion path writes the record without refreshing
            // the cached HUD best[]. Read its existing preference, without writes.
            int best = SportsPrefs.GetInt("TruckSnake.Best." + match.setup.players[slot].participantId, 0);
            return rules.attached[slot] + " ATTACHED · x" + rules.ScoreMultiplier(slot)
                + " · " + SportsMeasure.Feet(rules.distance[slot])
                + " · " + rules.survived[slot].ToString("F1") + " SEC"
                + " · " + rules.jumps[slot] + " JUMPS · BEST " + best;
        }
        static SportsResultsSnapshot Capture(SportsEventSession session, string heading, string summary,
            Func<int, int> place, Func<int, bool> winner, Func<int, double> measurement, string unit, Func<int, string> detail)
        {
            var rows = new List<SportsResultRow>(session.setup.count);
            for (int i = 0; i < session.setup.count; i++)
                rows.Add(new SportsResultRow(session.setup.players[i], i, place(i), winner(i), measurement(i), unit, detail(i), format: "0"));
            return new SportsResultsSnapshot(heading, summary, rows);
        }
    }
}
