using System;
using System.Collections.Generic;

namespace LWS.TruckSports
{
    public static class SportsHitchesResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (session == null || !session.Finished) return false;
            if (session is LWS.TruckWaterSports.WaterMatch water)
                snapshot = Capture(session, water.ResultPlace, i => water.ResultHasOutcome(i) ? (double?)water.ResultScore(i) : null,
                    water.ResultWinner, water.ResultUnit, water.Cooperative, i => "GATES " + water.Gates[i], water.ResultHigherIsBetter);
            else if (session is LWS.TruckHorrorSports.HorrorMatch horror)
                snapshot = Capture(session, horror.ResultPlace, i => horror.Cooperative || horror.ResultEligible(i) ? (double?)horror.Score(i) : null,
                    horror.ResultWinner, "PTS", horror.Cooperative,
                    i => "OBJECTIVES " + horror.Objectives(i) + " · " + (horror.Escaped(i) ? "ESCAPED" : "SURVIVAL " + horror.SurvivalSeconds(i).ToString("0") + "s"));
            else if (session is LWS.TruckHitchSports.HitchCompetitionSession hitch)
                snapshot = Capture(session, hitch.ResultPlace, i => hitch.ResultEligible(i) ? (double?)hitch.Scores[i] : null,
                    hitch.ResultWinner, "PTS", hitch.Cooperative, hitch.ResultDetail);
            else if (session is LWS.TruckExtremeHauling.ExtremeHaulingMatch hauling)
                snapshot = Capture(session, hauling.ResultPlace, i => hauling.ResultEligible(i) ? (double?)hauling.ResultScore(i) : null, hauling.ResultWinner, "PTS", hauling.Cooperative,
                    i => "PHYSICAL OBJECTIVES " + hauling.ObjectivesCompleted(i));
            return snapshot != null;
        }
        static SportsResultsSnapshot Capture(SportsEventSession session, Func<int, int> place, Func<int, double?> measurement,
            Func<int, bool> winner, string unit, bool cooperative, Func<int, string> detail, bool higherIsBetter = true)
        {
            var rows = new List<SportsResultRow>(session.setup.count);
            for (int i = 0; i < session.setup.count; i++)
                rows.Add(new SportsResultRow(session.setup.players[i], i, cooperative ? 0 : place(i), !cooperative && winner(i),
                    measurement(i), unit, detail(i), "NO QUALIFYING OUTCOME", "0.###", higherIsBetter ? SportsRecordPreference.Higher : SportsRecordPreference.Lower));
            return new SportsResultsSnapshot(session.Title + " RESULTS", cooperative ? "COOPERATIVE OUTCOME · NO INDIVIDUAL WINNER POINTS" : "MEASURED GAMEPLAY RESULTS", rows,
                cooperative ? SportsMatchKind.Cooperative : SportsMatchKind.Competitive);
        }
    }
}
