using System;
using System.Collections.Generic;
using LWS.TruckMinefield;
using LWS.TruckFishing;
using LWS.TruckSumo;
using LWS.TruckLogRolling;
using LWS.TruckLimbo;

namespace LWS.TruckSports
{
    // Pending next results integration. Read-only original match statistics;
    // no winner, survival outcome, heat award or lower-bar score is recomputed.
    public static class SportsSurvivalResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (session == null || !session.Finished) return false;
            if (session is MinefieldMatch mine)
                snapshot = Capture(session, "MINEFIELD RESULTS", "CROSS THE FINISH OR WIN THE HEATS",
                    mine.ResultPlace, i => mine.Winner == i, i => mine.points[i], "PTS",
                    i => (mine.racers[i].Phase == MineAttempt.DrivenFinish ? "CROSSED THE FINISH · " : "FINAL ") + SportsMeasure.Feet(mine.racers[i].CurrentDistance * .1f) + " · BEST " + SportsMeasure.Feet(mine.personalBest[i] * .1f));
            else if (session is FishingMatch fish)
                snapshot = Capture(session, fish.setup.count == 1 ? "FISHING SURVIVAL" : fish.rules.Winner < 0 ? "SIMULTANEOUS ELIMINATION · DRAW" : "LAST ANGLER STANDING",
                    (fish.setup.count == 1 ? "SURVIVAL & CAUGHT WEIGHT" : "CAUGHT WEIGHT · LAST SURVIVOR WINS") + " · SOLO BEST " + fish.ResultSurvivalBest.ToString("F1") + " SEC / " + fish.ResultCaughtBest + " LB", fish.ResultPlace, i => fish.rules.Winner == i,
                    i => fish.rules.caught[i], "LB", i => fish.ResultSurvivalSeconds(i).ToString("F1") + " SEC · " + (fish.rules.alive[i] ? fish.rules.missed[i] + " LB MISSED" : fish.rules.reason[i]));
            else if (session is SumoMatch sumo)
                snapshot = Capture(session, "SUMO CHAMPION", "FIRST TO FIVE HEAT WINS · " + sumo.HeatDraws + " DRAWN HEATS",
                    sumo.ResultPlace, i => sumo.rules.Winner == i, i => sumo.rules.wins[i], "WINS",
                    i => sumo.rules.points[i] + " PTS · " + sumo.rules.ringouts[i] + " CREDITED RING-OUTS");
            else if (session is LogMatch log)
                snapshot = Capture(session, "LOG ROLLING RESULTS", "FIRST TO FIVE ROUND WINS · " + log.Draws + " DRAWN ROUNDS",
                    log.ResultPlace, i => log.rules.Winner == i, i => log.rules.wins[i], "WINS", i => log.rules.Winner == i ? "LAST TRUCK ON THE LOG" : "SERIES COMPLETE");
            else if (session is LimboMatch limbo)
                snapshot = Capture(session, "LIMBO PARTY RESULTS", limbo.rules.Format == LimboFormat.Elimination ? "LAST TRUCK UNDER THE BAR" : "LOWEST BAR · POINTS BREAK TIES",
                    limbo.ResultPlace, i => limbo.rules.winners[i], i => limbo.rules.points[i], "PTS",
                    i => limbo.rules.clears[i] + " CLEARS · " + (float.IsInfinity(limbo.rules.best[i]) ? "NO BAR CLEARED" : limbo.rules.best[i].ToString("F2") + " M LOWEST"));
            return snapshot != null;
        }
        static SportsResultsSnapshot Capture(SportsEventSession session, string heading, string summary, Func<int,int> place,
            Func<int,bool> winner, Func<int,double> measurement, string unit, Func<int,string> detail)
        {
            var rows = new List<SportsResultRow>(session.setup.count);
            for (int i = 0; i < session.setup.count; i++)
                rows.Add(new SportsResultRow(session.setup.players[i], i, place(i), winner(i), measurement(i), unit, detail(i), format: "0"));
            return new SportsResultsSnapshot(heading, summary, rows);
        }
    }
}
