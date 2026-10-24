using System;
using System.Collections.Generic;
using LWS.TruckBuzkashi;
using LWS.TruckKeepaway;
using LWS.TruckKickball;
using LWS.TruckDodgeball;

namespace LWS.TruckSports
{
    // Prepared follow-on adapter only. Shared team values are labelled as
    // team statistics; this display never awards an individual those values.
    public static class SportsPossessionTeamResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (session == null || !session.Finished) return false;
            var rows = new List<SportsResultRow>(session.setup.count);
            string heading, summary;
            if (session is BuzMatch buz)
            {
                heading = buz.Winner >= 0 ? buz.teams[buz.Winner] + " WINS BUZKASHI" : "BUZKASHI RESULTS";
                summary = buz.Deliveries + " CARGO DELIVERIES · " + buz.Breaks + " LEGAL ROPE BREAKS · SHARED TEAM TOTALS";
                for (int i = 0; i < session.setup.count; i++)
                {
                    int team = BuzRules.Team(i);
                    rows.Add(Row(session, i, TeamPlace(team, buz.Winner), buz.Winner == team,
                        buz.score[team], "TEAM DELIVERIES", buz.teams[team]));
                }
            }
            else if (session is KeepawayMatch keep)
            {
                heading = keep.Winner >= 0 ? "KEEPAWAY CHAMPION" : "KEEPAWAY RESULTS";
                summary = keep.Target + " SEC CUMULATIVE TARGET · " + keep.Pickups + " PICKUPS · " + keep.Steals + " STEALS · " + keep.Blasts + " COMIC EXPLOSIONS";
                for (int i = 0; i < session.setup.count; i++)
                    rows.Add(Row(session, i, KeepPlace(keep, i), keep.Winner == i,
                        keep.possession[i], "SEC", "CUMULATIVE POSSESSION", "0.0"));
            }
            else if (session is KickballMatch kick)
            {
                heading = kick.rules.Winner == 0 ? "BLUE BANDITS WIN KICKBALL" : kick.rules.Winner == 1 ? "ORANGE OUTLAWS WIN KICKBALL" : "KICKBALL RESULTS";
                summary = kick.rules.Inning + " INNINGS · " + kick.PhysicalKicks + " PHYSICAL KICKS · " + kick.PhysicalCatches + " CATCHES · " + kick.PhysicalBaseTouches + " BASE TOUCHES";
                for (int i = 0; i < session.setup.count; i++)
                {
                    int team = kick.rules.Team(i);
                    rows.Add(Row(session, i, TeamPlace(team, kick.rules.Winner), kick.rules.Winner == team,
                        kick.rules.Scores[team], "TEAM RUNS", team == 0 ? "BLUE BANDITS" : "ORANGE OUTLAWS"));
                }
            }
            else if (session is DodgeMatch dodge)
            {
                heading = dodge.rules.Winner >= 0 ? "TEAM " + (dodge.rules.Winner + 1) + " WINS DODGEBALL" : "DODGEBALL RESULTS";
                summary = "FIRST TO THREE ROUND WINS · " + dodge.Draws + " DRAWN ROUNDS · SHARED TEAM TOTALS";
                for (int i = 0; i < session.setup.count; i++)
                {
                    int team = dodge.rules.Team(i);
                    rows.Add(Row(session, i, TeamPlace(team, dodge.rules.Winner), dodge.rules.Winner == team,
                        dodge.rules.wins[team], "TEAM WINS", "TEAM " + (team + 1) + " · FINAL ROUND " + (dodge.rules.alive[i] ? "SURVIVED" : "OUT")));
                }
            }
            else return false;
            snapshot = new SportsResultsSnapshot(heading, summary, rows);
            return true;
        }
        static int TeamPlace(int team, int winner) => winner < 0 ? 0 : team == winner ? 1 : 2;
        static int KeepPlace(KeepawayMatch match, int slot)
        {
            if (slot < 0 || slot >= match.setup.count) throw new ArgumentOutOfRangeException(nameof(slot));
            if (slot == match.Winner) return 1;
            int place = match.Winner >= 0 ? 2 : 1;
            for (int i = 0; i < match.setup.count; i++)
                if (i != match.Winner && match.possession[i] > match.possession[slot]) place++;
            return place;
        }
        static SportsResultRow Row(SportsEventSession session, int slot, int place, bool winner,
            double measurement, string unit, string detail, string format = "0")
            => new SportsResultRow(session.setup.players[slot], slot, place, winner, measurement, unit, detail, format: format);
    }
}
