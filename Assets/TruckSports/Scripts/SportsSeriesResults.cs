using System;
using System.Collections.Generic;
using LWS.TruckBaseJumping;
using LWS.TruckBogSnorkelling;
using LWS.TruckMattressRacing;
using LWS.TruckTerrierRacing;
using LWS.TruckWifeCarrying;
using LWS.TruckSkyTow;

namespace LWS.TruckSports
{
    // Prepared only: original winners and retained series statistics are read,
    // never recomputed by presentation. Physical/gameplay validation is pending.
    public static class SportsSeriesResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (session == null || !session.Finished) return false;
            if (session is BaseJumpMatch jump)
                snapshot = Capture(session, "BASE JUMPING RESULTS", jump.Round + " ROUNDS · POINTS / VALID ERROR / TIME",
                    jump.ResultPlace, i => jump.Winner == i, i => jump.totals[i], "PTS", i => BaseDetail(jump, i));
            else if (session is BogMatch bog)
                snapshot = Capture(session, "BOG SNORKELLING RESULTS", bog.series.completed + " RACES · SERIES POINTS",
                    bog.ResultPlace, i => bog.series.winner == i, i => bog.series.total[i], "PTS",
                    i => bog.racers[i].Checkpoint + " / " + bog.course.checkpoints.Length + " FLAGS · AIR " + bog.racers[i].Air.ToString("F0") + " SEC");
            else if (session is MattressMatch mattress)
                snapshot = Capture(session, "MATTRESS RACING RESULTS", mattress.series.completed + " RACES · SERIES POINTS",
                    mattress.ResultPlace, i => mattress.series.winner == i, i => mattress.series.total[i], "PTS",
                    i => mattress.series.finishes[i * 8] + (mattress.series.finishes[i * 8] == 1 ? " RACE WIN" : " RACE WINS") + " · FINAL RACE " + LastPlace(mattress.series, i));
            else if (session is TerrierMatch terrier)
                snapshot = Capture(session, "TERRIER RACING CHAMPION", "FIRST TO FIVE HEAT WINS · " + terrier.Heat + " HEATS",
                    terrier.ResultPlace, i => terrier.MatchWinner == i, i => terrier.wins[i], "WINS",
                    i => terrier.MatchWinner == i ? "THROUGH THE FINISH FIRST" : "SERIES COMPLETE");
            else if (session is WifeMatch wife)
                snapshot = Capture(session, "WIFE CARRYING TEAM RESULTS", "FOUR TEAMS · " + wife.series.completed + " RACES · SHARED TEAM TOTALS",
                    i => wife.ResultPlace(i % 4), i => wife.series.winner == i % 4, i => wife.series.total[i % 4], "TEAM PTS",
                    i => wife.teams[i % 4] + " · " + (wife.DriverRole(i % 4) == i ? "DRIVER" : "PASSENGER") + " · FINAL RACE " + LastPlace(wife.series, i % 4));
            else if (session is SkyTowMatch tow)
                snapshot = Capture(session, "ARCTIC SKY TOW RESULTS", "PHYSICAL RING CROSSINGS · FINAL POINTS",
                    tow.ResultPlace, i => tow.rules.winners[i], i => tow.rules.score[i], "PTS",
                    i => tow.rules.hits[i] + " / " + tow.rules.Rings + " RINGS · FINAL STREAK " + tow.rules.streak[i]);
            return snapshot != null;
        }
        static string BaseDetail(BaseJumpMatch match, int slot)
        {
            var final = match.ResultLanding(slot);
            string aggregate = "VALID ERROR " + BaseJumpRules.Distance(match.totalErrors[slot]) + " · " + (match.totalTimes[slot] / 1000f).ToString("F3") + " SEC";
            return aggregate + " · FINAL " + (final.valid
                ? (final.inside ? "IN" : "OUT") + " · BAND " + final.Band + "–" + (final.Band + 1) + "m · " + BaseJumpRules.Distance(final.errorMillimetres) + " · " + (final.timeMilliseconds / 1000f).ToString("F3") + " SEC · " + final.points + " PTS"
                : "INVALID · 0 PTS");
        }
        static string LastPlace(SportsRaceSeries series, int slot) => series.ranks[slot] > 0 ? "#" + series.ranks[slot] : "DNF";
        static SportsResultsSnapshot Capture(SportsEventSession session, string heading, string summary, Func<int, int> place,
            Func<int, bool> winner, Func<int, double> measurement, string unit, Func<int, string> detail)
        {
            var rows = new List<SportsResultRow>(session.setup.count);
            for (int i = 0; i < session.setup.count; i++)
                rows.Add(new SportsResultRow(session.setup.players[i], i, place(i), winner(i), measurement(i), unit, detail(i), format: "0"));
            return new SportsResultsSnapshot(heading, summary, rows);
        }
    }
}
