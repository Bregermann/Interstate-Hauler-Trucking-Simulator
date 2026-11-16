using System.Linq;
namespace LWS.TruckSports
{
    // Explicit semantics for persistence. These projections do not alter the
    // original winner, placing, scoring or result-screen presentation.
    public static class SportsStatisticsResults
    {
        public static SportsResultsSnapshot Prepare(SportsEventSession session, SportsResultsSnapshot result)
        {
            SportsMatchKind kind = result.Kind;
            if (session is LWS.TruckCamping.CampingMatch || session is LWS.TruckSnowmanBuilding.SnowmanMatch ||
                session is LWS.TruckNestBuilding.NestMatch nest && nest.rules.Cooperative ||
                session is LWS.TruckArchaeology.ArchaeologyMatch archaeology && archaeology.Rules.Cooperative ||
                session is LWS.TruckDragonEscape.DragonEscapeMatch escape && escape.Cooperative) kind = SportsMatchKind.Cooperative;
            else if (session is LWS.TruckYoga.YogaMatch yoga && yoga.rules.Free || session is LWS.TruckBeerPong.PongMatch pong && pong.Practice) kind = SportsMatchKind.Practice;
            var rows = result.Rows.Select(r => new SportsResultRow(new ResultParticipant(r), r.Slot, r.Place,
                kind == SportsMatchKind.Competitive && r.Winner, r.Measurement, r.Unit, r.Detail, r.FallbackValue, r.Format,
                Preference(session, r))).ToArray();
            return new SportsResultsSnapshot(result.Heading, result.Summary, rows, kind);
        }
        static SportsRecordPreference Preference(SportsEventSession session, SportsResultRow row)
        {
            if (row.RecordPreference != SportsRecordPreference.Unspecified || !row.Measurement.HasValue) return row.RecordPreference;
            // Standardized units come from the typed result contract, never HUD text.
            switch (row.Unit)
            {
                case "PTS": case "POINTS": case "TEAM PTS": case "CHIPS": case "WINS": case "TEAM WINS":
                case "SHEEP": case "GOALS": case "TEAM GOALS": case "CUPS HIT": case "TEAM CUPS HIT":
                case "FARES": case "CAPTURES": case "VALUE": case "TEAM DELIVERIES": case "M": case "FT": case "FEET":
                    return SportsRecordPreference.Higher;
                case "SEC":
                    return session != null && session.SupportsRaceCountdown || session is LWS.TruckDragonRiders.DragonRidersMatch ||
                        session is LWS.TruckDragonEscape.DragonEscapeMatch || session is LWS.TruckEggSpoonRace.EggSpoonMatch
                        ? SportsRecordPreference.Lower : SportsRecordPreference.Unspecified;
                default: return SportsRecordPreference.Unspecified;
            }
        }
        sealed class ResultParticipant : ISportsParticipantSlot
        {
            readonly SportsResultRow row;
            public ResultParticipant(SportsResultRow row) { this.row = row; }
            public string ParticipantId { get => row.ParticipantId; set { } }
            public string DisplayName { get => row.DisplayName; set { } }
            public UnityEngine.Color ParticipantColor { get => row.Color; set { } }
            public bool IsBot => row.IsAI;
        }
    }
}
