using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LWS.TruckHotPotato;
using LWS.TruckBalloonBattle;
using LWS.TruckTetherball;
using LWS.TruckJousting;
using LWS.TruckFrogger;
using LWS.TruckRedLightGreenLight;
using LWS.TruckVolleyball;
using LWS.TruckFreezeTag;

namespace LWS.TruckSports
{
    // Read-only result adapters. Existing match/rule owners supply placements
    // and winners; team totals are explicitly labelled and never added to players.
    public static class SportsContestResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (session == null || !session.Finished) return false;
            var rows = new List<SportsResultRow>(session.setup.count);
            string heading, summary;
            if (session is HotPotatoMatch hot)
            {
                heading = "HOT POTATO SURVIVOR"; summary = "LAST TRUCK STANDING · " + hot.rules.Transfers + " TRANSFERS";
                for (int i = 0; i < session.setup.count; i++)
                    rows.Add(Row(session, i, hot.rules.Winner == i ? 1 : 0, hot.rules.Winner == i, hot.rules.scores[i], "PTS",
                        hot.rules.passes[i] + " PASSES · " + hot.rules.survived[i] + " CYCLES SURVIVED"));
            }
            else if (session is BalloonMatch balloon)
            {
                heading = balloon.rules.Winner < 0 ? "MUTUAL POP · DRAW" : "BALLOON BATTLE SURVIVOR";
                summary = "LAST TRUCK STANDING · " + balloon.rules.Pops + " BALLOONS POPPED";
                for (int i = 0; i < session.setup.count; i++)
                    rows.Add(Row(session, i, balloon.rules.Winner == i ? 1 : 0, balloon.rules.Winner == i, balloon.rules.scores[i], "PTS",
                        balloon.rules.creditedPops[i] + " POPS · " + balloon.rules.eliminations[i] + " ELIMINATIONS · " + balloon.rules.Remaining(i) + " LEFT"));
            }
            else if (session is TetherMatch tether)
            {
                heading = "TETHERBALL CHAMPIONS"; summary = "BEST OF " + tether.rules.BestOf + " · " + tether.rules.TargetTurns + " WRAPS PER ROUND";
                for (int i = 0; i < session.setup.count; i++)
                    rows.Add(Row(session, i, tether.ResultPlace(i), tether.rules.Winner == i, tether.rules.wins[i], "WINS", i == 0 ? "COUNTERCLOCKWISE" : "CLOCKWISE"));
            }
            else if (session is JoustMatch joust)
            {
                heading = joust.rules.Winner < 0 ? "SHARED TOURNAMENT VICTORY" : "JOUSTING CHAMPIONS"; summary = joust.rules.PassLimit + " PASSES · LANCE HITS & KNOCKDOWNS";
                for (int i = 0; i < session.setup.count; i++)
                    rows.Add(Row(session, i, joust.ResultPlace(i), joust.rules.Winner < 0 || joust.rules.Winner == i, joust.rules.points[i], "PTS",
                        joust.rules.hits[i] + " HITS · " + joust.rules.centralHits[i] + " CENTRAL · " + joust.rules.knockdowns[i] + " KNOCKDOWNS"));
            }
            else if (session is FrogMatch frog)
            {
                heading = frog.rules.Winner < 0 ? "CROSSING CHAMPIONSHIP RESULTS" : "CROSSING CHAMPION"; summary = frog.rules.Announcement;
                for (int i = 0; i < session.setup.count; i++)
                {
                    int slot = i; var attempts = frog.rules.History.Where(attempt => attempt.participant == slot).ToArray();
                    var finished = attempts.Where(attempt => attempt.success).ToArray();
                    string detail = finished.Length + " / " + attempts.Length + " SUCCESSFUL ATTEMPTS";
                    if (finished.Length > 0) detail += " · BEST " + Seconds(finished.Min(attempt => attempt.seconds)) + " SEC";
                    rows.Add(Row(session, i, frog.rules.Place(i), frog.rules.Winner == i, frog.rules.points[i], "WINS", detail));
                }
            }
            else if (session is RedLightMatch red)
            {
                heading = red.rules.Winner < 0 ? "NO LEGAL FINISHER" : "GREEN LIGHT CHAMPION"; summary = red.LegalFinishes + " LEGAL FINISHES · " + red.Explosions + " ELIMINATIONS";
                for (int i = 0; i < session.setup.count; i++)
                {
                    bool finished = red.rules.outcomes[i] == RedLightOutcome.Finished;
                    rows.Add(Row(session, i, red.ResultPlace(i), red.rules.Winner == i, finished ? (double?)red.rules.finishTimes[i] : null, "SEC",
                        finished ? red.ResultPoints(i) + " PTS · LEGAL WHOLE-TRUCK FINISH" : red.rules.reasons[i],
                        red.rules.outcomes[i] == RedLightOutcome.Eliminated ? "ELIMINATED" : "DID NOT FINISH", "0.000"));
                }
            }
            else if (session is VolleyMatch volley)
            {
                heading = TeamHeading(volley.rules.Winner, "VOLLEYBALL"); summary = TeamSummary(volley.rules.points, "POINTS");
                for (int i = 0; i < session.setup.count; i++)
                {
                    int team = volley.Team(i);
                    rows.Add(Row(session, i, TeamPlace(team, volley.rules.Winner), volley.rules.Winner == team, volley.rules.points[team], "TEAM PTS",
                        TeamName(team) + " · " + volley.rules.spikes[team] + " AERIAL · " + volley.rules.saves[team] + " SAVES · " + volley.rules.teamPlays[team] + " TEAM RETURNS"));
                }
            }
            else if (session is FreezeMatch freeze)
            {
                heading = TeamHeading(freeze.rules.Winner, "FREEZE TAG"); summary = TeamSummary(freeze.rules.wins, "ROUND WINS");
                for (int i = 0; i < session.setup.count; i++)
                {
                    int team = freeze.rules.Team(i);
                    rows.Add(Row(session, i, TeamPlace(team, freeze.rules.Winner), freeze.rules.Winner < 0 || freeze.rules.Winner == team, freeze.rules.wins[team], "TEAM WINS",
                        TeamName(team) + " · " + freeze.rules.tags[i] + " TAGS · " + freeze.rules.rescues[i] + " RESCUES"));
                }
            }
            else return false;
            snapshot = new SportsResultsSnapshot(heading, summary, rows);
            return true;
        }
        static SportsResultRow Row(SportsEventSession session, int slot, int place, bool winner, double? measurement, string unit, string detail, string fallback = "—", string format = "0")
            => new SportsResultRow(session.setup.players[slot], slot, place, winner, measurement, unit, detail, fallback, format);
        static string Seconds(float seconds) => seconds.ToString("0.0", CultureInfo.InvariantCulture);
        static string TeamName(int team) => team == 0 ? "BLUE" : "ORANGE";
        static int TeamPlace(int team, int winner) => winner < 0 || team == winner ? 1 : 2;
        static string TeamHeading(int winner, string sport) => winner < 0 ? "SHARED " + sport + " VICTORY" : TeamName(winner) + " WINS " + sport;
        static string TeamSummary(int[] scores, string unit) => "BLUE " + scores[0] + " : " + scores[1] + " ORANGE · " + unit;
    }
}
