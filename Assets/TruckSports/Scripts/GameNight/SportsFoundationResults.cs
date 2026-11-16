using System;
using System.Collections.Generic;
using System.Linq;
namespace LWS.TruckSports
{
    // Read-only bridges for rules owners not covered by the existing broadcast
    // result adapters. No winner is inferred from text or celebration events.
    public static class SportsFoundationResults
    {
        public static bool TryCapture(SportsEventSession session, out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (session == null || !session.Finished) return false;
            switch (session)
            {
                case LWS.TruckBakeOff.BakeMatch m:
                    snapshot = Capture(m, m.rules.Place, i => m.rules.points[i]); break;
                case LWS.TruckBallet.BalletMatch m:
                    snapshot = Capture(m, m.rules.Place, i => m.rules.score[i]); break;
                case LWS.TruckMechanicalBull.MechanicalBullMatch m:
                    snapshot = Capture(m, m.rules.Place, i => m.rules.totals[i]); break;
                case LWS.TruckPinataPanic.PinataMatch m:
                    snapshot = Capture(m, m.rules.Place, i => m.rules.points[i]); break;
                case LWS.TruckSubmarine.SubmarineMatch m:
                    snapshot = Capture(m, m.rules.Place, i => m.rules.points[i]); break;
                case LWS.TruckTeaParty.TeaMatch m:
                    snapshot = Capture(m, m.rules.Place, i => m.rules.points[i]); break;
                case LWS.TruckTornadoRodeo.TornadoMatch m:
                    snapshot = Capture(m, m.rules.Place, i => m.rules.points[i]); break;
                case LWS.TruckPedestrianPlowing.PlowMatch m:
                    snapshot = Capture(m, m.Round.Place, i => m.Round.Score(i)); break;
                case LWS.TruckEggSpoonRace.EggSpoonMatch m:
                    snapshot = Capture(m, m.rules.Place, i => m.rules.finished[i] ? (double?)m.rules.time[i] : null,
                        i => m.rules.finished[i] && m.rules.Place(i) == 1, "SEC", i => m.rules.finished[i] ? "FINISHED" : "COURSE INCOMPLETE"); break;
                case LWS.TruckYoga.YogaMatch m:
                    snapshot = Capture(m, i => m.rules.Free ? 0 : m.rules.Place(i), i => m.rules.Free ? null : (double?)m.rules.score[i],
                        i => !m.rules.Free && m.rules.Place(i) == 1, "PTS", i => m.rules.Free ? "FREE YOGA · NO WINNER POINTS" : "SEVEN-POSE CHALLENGE"); break;
                case LWS.TruckNestBuilding.NestMatch m:
                    snapshot = Capture(m, i => m.rules.Cooperative ? 0 : m.rules.Place(i), i => m.rules.quality[m.rules.Cooperative ? 0 : i],
                        i => !m.rules.Cooperative && m.rules.quality[i] > 0 && m.rules.Place(i) == 1, "PTS", i => m.rules.Cooperative ? "COOPERATIVE WORKSHOP" : "NEST QUALITY"); break;
                case LWS.TruckSnowmanBuilding.SnowmanMatch m:
                    snapshot = Capture(m, i => 0, i => m.Rules.Score, i => false, "TEAM PTS", i => "COOPERATIVE WORKSHOP"); break;
                case LWS.TruckCamping.CampingMatch m:
                    snapshot = Capture(m, i => 0, i => null, i => false, "", i => "SHARED TRIP · NO WINNER POINTS"); break;
                case LWS.TruckJenga.JengaMatch m:
                    snapshot = Capture(m, i => m.Rules.Loser < 0 ? 0 : i == m.Rules.Loser ? 2 : 1, i => null,
                        i => m.Rules.Loser >= 0 && i != m.Rules.Loser, "", i => i == m.Rules.Loser ? "CAUSED THE COLLAPSE" : "TOWER SURVIVOR"); break;
                case LWS.TruckPoker.PokerMatch m:
                    snapshot = Capture(m, i => m.Table.Winner == i ? 1 : 0, i => m.Table.Stack(i), i => m.Table.Winner == i, "CHIPS"); break;
                case LWS.TruckGuessThat.GuessMatch m:
                    snapshot = Capture(m, i => m.Rules.Winner == i ? 1 : 0, i => null, i => m.Rules.Winner == i, "", i => "IDENTIFICATION RESULTS"); break;
                case LWS.TruckTugOfWar.TugMatch m:
                    snapshot = Capture(m, i => m.Winner < 0 ? 0 : m.Team(i) == m.Winner ? 1 : 2,
                        i => m.wins[m.Team(i)], i => m.Winner >= 0 && m.Team(i) == m.Winner, "TEAM WINS"); break;
                case LWS.TruckFashionShow.FashionMatch m:
                    snapshot = Capture(m, i => 1 + m.Competition.Scores.Count(v => v > m.Competition.Scores[i]),
                        i => m.Competition.Scores[i], i => Array.IndexOf(m.Competition.Leaders(), i) >= 0); break;
                case LWS.TruckMoonMadness.MoonMatch m:
                    snapshot = Capture(m, i => 1 + m.Rules.Scores.Take(m.setup.count).Count(v => v > m.Rules.Scores[i]),
                        i => m.Rules.Scores[i], i => m.Rules.Leaders().Contains(i), "CAPTURES"); break;
                case LWS.TruckTruckGoose.GooseMatch m:
                    snapshot = Capture(m, m.Rules.FinalPlace, i => m.Rules.Scores[i]); break;
                case LWS.TruckArchaeology.ArchaeologyMatch m:
                    snapshot = Capture(m, m.Rules.Place, i => m.Rules.Scores[i],
                        i => !m.Rules.Cooperative && m.Rules.Place(i) == 1, "VALUE"); break;
                case LWS.TruckHoardHeist.HoardHeistMatch m:
                    snapshot = Capture(m, m.Rules.Place, i => m.Rules.Scores[i]); break;
                case LWS.TruckDragonEggDelivery.DragonEggDeliveryMatch m:
                    snapshot = Capture(m, i => 1 + m.Rules.Cargo.Take(m.setup.count).Count(c => c.Score > m.Rules.Cargo[i].Score),
                        i => m.Rules.Cargo[i].Score, i => m.Rules.Winners().Contains(i)); break;
                case LWS.TruckDragonRiders.DragonRidersMatch m:
                    snapshot = Capture(m, m.ResultPlace, i => m.rules.Finished(i) ? (double?)m.rules.FinishTime[i] : null,
                        i => m.rules.Finished(i) && m.ResultPlace(i) == 1, "SEC", i => m.rules.Finished(i) ? "PHYSICAL COURSE COMPLETE" : "COURSE INCOMPLETE"); break;
                case LWS.TruckDragonEscape.DragonEscapeMatch m:
                    snapshot = Capture(m, i => m.Cooperative ? 0 : m.Placements[i], i => m.ExtractionTimes[i] >= 0 ? (double?)m.ExtractionTimes[i] : null,
                        i => !m.Cooperative && m.ExtractionTimes[i] >= 0 && m.Placements[i] == 1, "SEC", i => m.Cooperative ? "COOPERATIVE EXTRACTION" : "EXTRACTION RESULTS"); break;
            }
            return snapshot != null;
        }
        static SportsResultsSnapshot Capture(SportsEventSession session, Func<int, int> place, Func<int, double?> score,
            Func<int, bool> winner = null, string unit = "PTS", Func<int, string> detail = null)
        {
            var rows = new List<SportsResultRow>(session.setup.count);
            for (int i = 0; i < session.setup.count; i++)
            {
                int rank = place(i);
                rows.Add(new SportsResultRow(session.setup.players[i], i, rank, winner != null ? winner(i) : rank == 1,
                    score(i), unit, detail != null ? detail(i) : "FINAL RULES RESULT", "NO RESULT"));
            }
            return new SportsResultsSnapshot(session.Title + " RESULTS", "AUTHORITATIVE FINAL MATCH RESULTS", rows);
        }
    }
}
