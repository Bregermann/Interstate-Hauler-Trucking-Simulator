using System;
using System.Collections.Generic;
using System.Globalization;
using LWS.TruckDockDiving;
using LWS.TruckFieldGoal;
using LWS.TruckTargetLaunch;
using LWS.TruckSkeeBall;
using LWS.TruckGiantSwing;
using LWS.TruckTrailerPlug;

namespace LWS.TruckSports
{
    // Completed physical attempt data only. Existing sport owners expose the
    // display placing and actual winner; UI never awards or reevaluates points.
    public static class SportsAttemptResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (session == null || !session.Finished) return false;
            if (session is DockMatch dock)
                snapshot = Capture(session, session.setup.count == 1 ? "PERSONAL-BEST CHALLENGE" : "DOCK DIVING RESULTS",
                    "THREE JUMPS · LONGEST VALID DISTANCE" + (dock.ResultHadTiebreak ? " · WINNING EXTRA JUMP " + DockMatch.Format(dock.ResultWinningExtraInches) : "") + SoloRecord(session, DockMatch.Format(dock.ResultPersonalBest)),
                    dock.ResultPlace, i => dock.Winner == i, i => dock.best[i] / 12d, "FT",
                    i => "JUMPS · 1: " + DockMatch.Format(dock.history[i,0]) + " · 2: " + DockMatch.Format(dock.history[i,1]) + " · 3: " + DockMatch.Format(dock.history[i,2]), "0.00");
            else if (session is FieldGoalMatch field)
                snapshot = Capture(session, field.rules.Practice ? "FIELD GOAL PRACTICE" : "FIELD GOAL RESULTS", "SUCCESSFUL KICKS & ATTEMPTS" + SoloRecord(session, field.ResultPersonalBest + " GOOD"),
                    field.ResultPlace, i => field.rules.Winner == i, i => field.rules.goals[i], "GOOD", i => field.rules.attempts[i] + " KICKS");
            else if (session is TargetMatch target)
                snapshot = Capture(session, "TARGET LAUNCH RESULTS", "FIVE ROUNDS · EXTRA ROUNDS: " + target.rules.ExtraRound + SoloRecord(session, target.ResultPersonalBest + " PTS"),
                    target.ResultPlace, i => target.rules.Winner == i, i => target.rules.totals[i], "PTS", i => target.rules.attempts[i] + " RESOLVED LAUNCHES");
            else if (session is SkeeMatch skee)
                snapshot = Capture(session, "SKEE BALL RESULTS", "NINE LAUNCHES · EXTRA ROUNDS: " + skee.rules.ExtraRound + SoloRecord(session, skee.ResultPersonalBest + " PTS"),
                    skee.ResultPlace, i => skee.rules.Winner == i, i => skee.rules.totals[i], "PTS", i => skee.rules.attempts[i] + " LAUNCHES · LAST " + skee.rules.last[i] + " PTS");
            else if (session is SwingMatch swing)
                snapshot = Capture(session, swing.Winner < 0 ? "SHARED GIANT SWING RESULTS" : "GIANT SWING RESULTS", "THREE ROUNDS · EXTRA ROUNDS: " + swing.ResultExtraRounds + SoloRecord(session, swing.ResultPersonalBest.ToString("0.###", CultureInfo.InvariantCulture) + " PTS"),
                    swing.ResultPlace, i => swing.Winner < 0 ? swing.ResultPlace(i) == 1 : swing.Winner == i, i => swing.points[i], "PTS", i => swing.attempts[i] + " THROWS");
            else if (session is TrailerPlugMatch trailer)
                snapshot = Capture(session, "TRAILER PLUG RESULTS", trailer.rules.Rounds + " ROUNDS · IMPACTS & HOLE COVERAGE",
                    trailer.ResultPlace, i => trailer.rules.winners[i], i => trailer.rules.score[i], "PTS", i => trailer.rules.attempts[i] + " / " + trailer.rules.Rounds + " DROPS");
            return snapshot != null;
        }
        static string SoloRecord(SportsEventSession session, string record) => session.setup.count == 1 ? " · RECORD " + record : "";
        static SportsResultsSnapshot Capture(SportsEventSession session, string heading, string summary, Func<int,int> place,
            Func<int,bool> winner, Func<int,double> measurement, string unit, Func<int,string> detail, string format = "0")
        {
            var rows = new List<SportsResultRow>(session.setup.count);
            for (int i = 0; i < session.setup.count; i++)
                rows.Add(new SportsResultRow(session.setup.players[i], i, place(i), winner(i), measurement(i), unit, detail(i), format: format));
            return new SportsResultsSnapshot(heading, summary, rows);
        }
    }
}
