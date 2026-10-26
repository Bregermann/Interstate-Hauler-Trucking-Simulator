using System.Collections.Generic;
using UnityEngine;
using LWS.TruckAvalancheEscape;
using LWS.TruckNinjaWarrior;

namespace LWS.TruckSports
{
    // Explicit opt-in adapter. Existing sport rules supply every rank, winner
    // and statistic; specialized Poker/Bowling presentation is untouched.
    public sealed class SportsFinishResultsSource : MonoBehaviour
    {
        public SportsEventSession session;
        public LWS.TruckCornhole.CornholeMatch cornhole;
        public LWS.TruckWallball.WallballMatch wallball;
        public string MatchId => session != null ? session.MatchId : cornhole != null ? cornhole.MatchId : wallball != null ? wallball.MatchId : null;
        public bool ResultReady => session != null ? session.Finished && session.menus.Screen == SportsEventScreen.Result : cornhole != null ? cornhole.InMatch && cornhole.Finished && cornhole.menus.Screen == LWS.TruckCornhole.CornholeScreen.Result : wallball != null && wallball.InMatch && wallball.rules.Phase == LWS.TruckWallball.WallballPhase.Finished && wallball.menus.Screen == LWS.TruckWallball.WallballScreen.Result;
        public TMPro.TMP_Text OriginalResult => session != null ? session.menus.result : cornhole != null ? cornhole.menus.resultText : wallball != null ? wallball.menus.resultText : null;
        public bool TryCapture(out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
            if (wallball != null)
            {
                if (!wallball.InMatch || wallball.rules.Phase != LWS.TruckWallball.WallballPhase.Finished) return false;
                var entries = new List<SportsResultRow>(wallball.Count);
                for (int i = 0; i < wallball.Count; i++)
                {
                    int team = wallball.Team(i); bool winner = team == wallball.rules.Winner;
                    entries.Add(new SportsResultRow(wallball.players[i], i, winner ? 1 : 2, winner,
                        wallball.rules.Score[team], "TEAM PTS", wallball.TeamName(team) + " · SHARED TEAM TOTAL", format: "0"));
                }
                snapshot = new SportsResultsSnapshot(wallball.TeamName(wallball.rules.Winner) + " WINS WALLBALL",
                    wallball.rules.Points + " RALLIES SCORED · " + wallball.LegalReturns + " LEGAL RETURNS · MAX VOLLEY " + wallball.MaximumVolley, entries);
                return true;
            }
            if (cornhole != null)
            {
                if (!cornhole.Finished) return false;
                var entries = new List<SportsResultRow>(cornhole.setup.count);
                for (int i = 0; i < cornhole.setup.count; i++)
                    entries.Add(new SportsResultRow(cornhole.setup.players[i], i,
                        SportsResultPlacings.FromFinalTotals(i, cornhole.setup.count, cornhole.totals), System.Array.IndexOf(cornhole.winners, i) >= 0,
                        cornhole.totals[i], "PTS", "FINAL ROUND +" + cornhole.lastRound[i] + " · " + cornhole.RoundFinalizations + " BANKS"));
                snapshot = new SportsResultsSnapshot(cornhole.setup.count == 1 ? "SOLO CORNHOLE RESULTS" : cornhole.winners.Length > 1 ? "SHARED CORNHOLE VICTORY" : "CORNHOLE RESULTS",
                    cornhole.RoundFinalizations + " ROUNDS BANKED · FOUR TRUCKS PER ROUND", entries);
                return true;
            }
            if (session == null || !session.Finished) return false;
            var rows = new List<SportsResultRow>(session.setup.count);
            if (session is AvalancheMatch avalanche)
            {
                int escaped = 0;
                for (int i = 0; i < session.setup.count; i++) if (avalanche.rules.finished[i]) escaped++;
                for (int i = 0; i < session.setup.count; i++)
                {
                    bool finished = avalanche.rules.finished[i]; int place = avalanche.rules.Place(i);
                    rows.Add(new SportsResultRow(session.setup.players[i], i, place, place == 1,
                        finished ? avalanche.rules.finishTime[i] : avalanche.rules.distance[i], finished ? "SEC" : "M",
                        finished ? "WHOLE TRUCK ESCAPED" : avalanche.rules.reason[i], format: finished ? "0.000" : "0"));
                }
                snapshot = new SportsResultsSnapshot(escaped > 0 ? "MOUNTAIN CONQUERED" : "FURTHEST SURVIVORS", escaped + " / " + session.setup.count + " ESCAPED · FINISH TIMES & DISTANCE", rows);
                return true;
            }
            if (session is NinjaMatch ninja)
            {
                bool anyFinish = false;
                for (int i = 0; i < session.setup.count; i++)
                {
                    bool finished = ninja.rules.finished[i]; anyFinish |= finished; int place = ninja.rules.Place(i);
                    int falls = ninja.rules.falls[i];
                    string detail = ninja.rules.cleared[i] + " / " + ninja.rules.Obstacles + " OBSTACLES · " + falls + (falls == 1 ? " COURSE RESTART" : " COURSE RESTARTS");
                    rows.Add(new SportsResultRow(session.setup.players[i], i, place, place == 1,
                        finished ? (double?)ninja.rules.adjustedTime[i] : null, "SEC", detail, "COURSE INCOMPLETE", "0.000"));
                }
                snapshot = new SportsResultsSnapshot(anyFinish ? "COURSE CHAMPIONS" : "FURTHEST ATHLETES", "COMPLETED ATTEMPT TIME · FALLS RESTART THE COURSE", rows);
                return true;
            }
            if (SportsScoredResults.TryCapture(session, out snapshot)) return true;
            if (SportsContestResults.TryCapture(session, out snapshot)) return true;
            if (SportsAttemptResults.TryCapture(session, out snapshot)) return true;
            if (SportsSurvivalResults.TryCapture(session, out snapshot)) return true;
            if (SportsSeriesResults.TryCapture(session, out snapshot)) return true;
            if (SportsPossessionTeamResults.TryCapture(session, out snapshot)) return true;
            return SportsRoundLegacyResults.TryCapture(session, out snapshot);
        }
    }
}
