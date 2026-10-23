using System;
using System.Collections.Generic;
using System.Globalization;
using LWS.TruckWhackAMole;
using LWS.TruckMagnetMayhem;
using LWS.TruckSheepHerding;
using LWS.TruckSeesaw;
using LWS.TruckKingOfTheHill;
using LWS.TruckFloorIsLava;
using LWS.TruckHideAndSeek;
using LWS.TruckHungryHaulers;

namespace LWS.TruckSports
{
    // Read-only adapters use each existing Place method, including its actual
    // tiebreaks. Presentation never ranks by displayed points on its own.
    public static class SportsScoredResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null; if (session == null || !session.Finished) return false;
            if (session is WhackMatch whack)
                snapshot = Capture(session, "FAIRGROUND RESULTS", whack.PhysicalHits + " TARGETS HIT · FINAL POINTS", whack.rules.Place, i => whack.rules.scores[i], i => "TARGET POINTS");
            else if (session is MagnetMatch magnet)
                snapshot = Capture(session, "SCRAPYARD RESULTS", "SCRAP COLLECTION POINTS", magnet.rules.Place, i => magnet.rules.points[i], i => "FINAL SCRAP POINTS");
            else if (session is HerdMatch herd)
                snapshot = Capture(session, "FARM HERDING RESULTS", "SHEEP SECURED · DELIVERY TIME BREAKS TIES", herd.rules.Place, i => herd.rules.points[i],
                    i => float.IsFinite(herd.rules.lastCompletion[i]) ? "LAST DELIVERY · " + Seconds(herd.rules.lastCompletion[i]) + " SEC" : "NO SHEEP DELIVERED", unit: "SHEEP");
            else if (session is SeesawMatch seesaw)
                snapshot = Capture(session, "QUARRY WOBBLE RESULTS", "HEAT " + (seesaw.rules.Heat + 1) + " / " + seesaw.rules.HeatCount + " · TOTAL POINTS", seesaw.rules.Place, i => seesaw.rules.scores[i],
                    i => seesaw.rules.heatWins[i] + " HEAT WINS · " + seesaw.rules.knockoffs[i] + " KNOCKOFFS · " + seesaw.rules.recoveries[i] + " RECOVERIES");
            else if (session is HillMatch hill)
                snapshot = Capture(session, "KING OF THE HILL RESULTS", "CONTROL TIME WINS", hill.rules.Place, i => hill.rules.control[i],
                    i => hill.rules.Winner == i ? "CONTROL TARGET REACHED" : "TOTAL ZONE CONTROL", i => hill.rules.Winner == i, "SEC", "0.0");
            else if (session is LavaMatch lava)
                snapshot = Capture(session, "LAVA SURVIVAL RESULTS", "LONGEST SURVIVAL WINS · SURVIVAL & KNOCKOFF POINTS", lava.rules.Place, i => lava.rules.scores[i],
                    i => Seconds(lava.rules.survived[i]) + " SEC SURVIVED", i => lava.rules.Winner == i);
            else if (session is HideMatch hide)
                snapshot = Capture(session, "HIDE & SEEK RESULTS", "TAG · HIDE · SEEKER SWEEP", hide.rules.Place, i => hide.rules.points[i],
                    i => hide.rules.tags[i] + " TAGS · " + hide.rules.survived[i] + " ROUNDS SURVIVED · " + hide.rules.sweeps[i] + " SWEEPS");
            else if (session is HaulerMatch hauler)
                snapshot = Capture(session, "HUNGRY HAULERS RESULTS", "COLLECT · BANK · DEFEND", hauler.rules.Place, i => hauler.rules.points[i], i => "BANKED DELIVERY POINTS");
            return snapshot != null;
        }
        static SportsResultsSnapshot Capture(SportsEventSession session, string heading, string summary, Func<int, int> place,
            Func<int, double> value, Func<int, string> detail, Func<int, bool> winner = null, string unit = "PTS", string format = "0")
        {
            var rows = new List<SportsResultRow>(session.setup.count);
            for (int i = 0; i < session.setup.count; i++)
            {
                int rank = place(i);
                rows.Add(new SportsResultRow(session.setup.players[i], i, rank, winner != null ? winner(i) : rank == 1, value(i), unit, detail(i), format: format));
            }
            return new SportsResultsSnapshot(heading, summary, rows);
        }
        static string Seconds(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
