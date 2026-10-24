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
                        (team == 0 ? "BLUE" : "ORANGE") + " · " + Count(pong.rules.shots[team], pong.Practice ? "SHOT" : "TEAM SHOT", pong.Practice ? "SHOTS" : "TEAM SHOTS") + " · " + pong.rules.remaining[team] + " OWN CUPS LEFT", format: "0"));
                }
                snapshot = new SportsResultsSnapshot(pong.Practice ? "BEER PONG PRACTICE" : "BEER PONG RESULTS",
                    pong.Practice ? Count(pong.rules.PracticeRacks, "RACK COMPLETED", "RACKS COMPLETED") : Count(pong.rules.Overtime, "OVERTIME PERIOD", "OVERTIME PERIODS") + " · SHARED TEAM STATISTICS", rows);
            }
            else if (session is BungeeMatch bungee)
                snapshot = Capture(session, "BUNGEE DEMOLITION RESULTS", Count(bungee.CompletedRounds, "ROUND", "ROUNDS") + " · BANKED DESTRUCTION POINTS"
                    + (session.setup.count == 1 ? " · PERSONAL BEST " + SportsPrefs.GetInt("TruckBungeeDemolition.PersonalBest", 0) + " PTS" : ""),
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, bungee.total, bungee.Winner), i => bungee.Winner == i,
                    i => bungee.total[i], "PTS", i => "FINAL ROUND " + bungee.bays[i].Score + " PTS");
            else if (session is CurlingMatch curling)
            {
                int winner = curling.scores[0] == curling.scores[1] ? -1 : curling.scores[0] > curling.scores[1] ? 0 : 1;
                snapshot = Capture(session, "CURLING TEAM RESULTS", Count(curling.EndsScored, "END", "ENDS") + " · " + Count(curling.GuardViolations, "GUARD-ZONE RESTORATION", "GUARD-ZONE RESTORATIONS"),
                    i => winner < 0 ? 0 : (i < curling.teamA ? 0 : 1) == winner ? 1 : 2,
                    i => winner >= 0 && (i < curling.teamA ? 0 : 1) == winner,
                    i => curling.scores[i < curling.teamA ? 0 : 1], "TEAM PTS", i => i < curling.teamA ? "BLUE TEAM" : "ORANGE TEAM");
            }
            else if (session is DanceMatch dance)
                snapshot = Capture(session, "DANCE-OFF RESULTS", Count(dance.Round, "SONG", "SONGS") + " · TOTAL POINTS / FINAL SONG STATISTICS",
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, dance.total),
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, dance.total) == 1,
                    i => dance.total[i], "PTS", i => DanceDetail(dance, i));
            else if (session is DerbyMatch derby)
                snapshot = Capture(session, "DEMOLITION DERBY CHAMPION", Count(derby.rules.Heat, "HEAT", "HEATS") + " · " + Count(derby.Hits, "QUALIFYING IMPACT", "QUALIFYING IMPACTS") + " · " + Count(derby.Explosions, "EXPLOSION", "EXPLOSIONS") + " · " + Count(derby.Draws, "DRAW", "DRAWS"),
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, derby.rules.wins, derby.rules.Winner), i => derby.rules.Winner == i,
                    i => derby.rules.wins[i], "WINS", i => "FINAL HEAT · " + (derby.rules.alive[i] ? "SURVIVED" : derby.rules.reasons[i]), i => derby.rules.wins[i] == 1 ? "WIN" : "WINS");
            else if (session is FareFightMatch fare)
                snapshot = Capture(session, "FARE FIGHT RESULTS", fare.rules.Clock.ToString("F1") + " SEC PLAYED · SUCCESSFUL PASSENGER DELIVERIES",
                    i => SportsResultPlacings.FromFinalTotals(i, session.setup.count, fare.rules.score), i => fare.rules.winners[i],
                    i => fare.rules.score[i], "FARES", i => Count(fare.rules.steals[i], "PASSENGER STEAL", "PASSENGER STEALS"), i => fare.rules.score[i] == 1 ? "FARE" : "FARES");
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
                    Count(parking.Round, "ROUND", "ROUNDS") + " · " + Count(parking.TotalCaptures, "PHYSICAL BAY CAPTURE", "PHYSICAL BAY CAPTURES"), rows);
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
                + " · " + Count(rules.jumps[slot], "JUMP", "JUMPS") + " · BEST " + best;
        }
        static string Count(int value, string singular, string plural) => value + " " + (value == 1 ? singular : plural);
        static SportsResultsSnapshot Capture(SportsEventSession session, string heading, string summary,
            Func<int, int> place, Func<int, bool> winner, Func<int, double> measurement, string unit, Func<int, string> detail, Func<int, string> unitAt = null)
        {
            var rows = new List<SportsResultRow>(session.setup.count);
            for (int i = 0; i < session.setup.count; i++)
                rows.Add(new SportsResultRow(session.setup.players[i], i, place(i), winner(i), measurement(i), unitAt == null ? unit : unitAt(i), detail(i), format: "0"));
            return new SportsResultsSnapshot(heading, summary, rows);
        }
    }
}
