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
        public bool TryCapture(out SportsResultsSnapshot snapshot)
        {
            snapshot = null;
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
                snapshot = new SportsResultsSnapshot(escaped > 0 ? "MOUNTAIN CONQUERED" : "FURTHEST SURVIVORS", escaped + " / " + session.setup.count + " ESCAPED · ACTUAL FINISH TIMES & SURVIVAL DISTANCE", rows);
                return true;
            }
            if (session is NinjaMatch ninja)
            {
                bool anyFinish = false;
                for (int i = 0; i < session.setup.count; i++)
                {
                    bool finished = ninja.rules.finished[i]; anyFinish |= finished; int place = ninja.rules.Place(i);
                    string detail = ninja.rules.cleared[i] + " / " + ninja.rules.Obstacles + " OBSTACLES · " + ninja.rules.falls[i] + " FALLS · +" + (ninja.rules.falls[i] * 5) + " SEC PENALTIES";
                    rows.Add(new SportsResultRow(session.setup.players[i], i, place, place == 1,
                        finished ? (double?)ninja.rules.adjustedTime[i] : null, "SEC", detail, "COURSE INCOMPLETE", "0.000"));
                }
                snapshot = new SportsResultsSnapshot(anyFinish ? "COURSE CHAMPIONS" : "FURTHEST ATHLETES", "ADJUSTED TIME INCLUDES THE ORIGINAL FIVE-SECOND FALL PENALTY", rows);
                return true;
            }
            return false;
        }
    }
}
