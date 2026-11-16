using System;
using System.Collections.Generic;
using System.Linq;
namespace LWS.TruckSports
{
    public enum SportsMatchKind { Competitive, Practice, Cooperative }
    public enum SportsRecordPreference { Unspecified, Higher, Lower }
    [Serializable] public sealed class SportsPlacementStatistic { public int place, count; }
    [Serializable] public sealed class SportsPersonalRecord
    {
        public string variant, variantLabel, unit, matchId, utc;
        public double value;
        public SportsRecordPreference preference;
    }
    [Serializable] public sealed class SportsSportStatistic
    {
        public string sport, title, lastPlayedUtc;
        public int matches, competitive, wins, losses, draws, practice, cooperative, winnerPoints;
        public List<SportsPlacementStatistic> placements = new List<SportsPlacementStatistic>();
        public List<SportsPersonalRecord> records = new List<SportsPersonalRecord>();
    }
    [Serializable] public sealed class SportsProfileStatistics
    {
        public int matches, competitive, wins, losses, draws, practice, cooperative, winnerPoints;
        public double playedSeconds;
        public List<string> sessionIds = new List<string>();
        public List<SportsSportStatistic> sports = new List<SportsSportStatistic>();
        public double WinPercentage => competitive == 0 ? 0 : wins * 100d / competitive;
        public void Normalize()
        {
            sessionIds ??= new List<string>(); sports ??= new List<SportsSportStatistic>();
            sports.RemoveAll(s => s == null);
            foreach (var sport in sports) { sport.placements ??= new List<SportsPlacementStatistic>(); sport.records ??= new List<SportsPersonalRecord>(); sport.placements.RemoveAll(p => p == null); sport.records.RemoveAll(r => r == null); }
        }
        public bool Visit(string sessionId)
        {
            Normalize(); if (string.IsNullOrWhiteSpace(sessionId) || sessionIds.Contains(sessionId)) return false;
            sessionIds.Add(sessionId); return true;
        }
        // Called only after the match authority has accepted a complete result.
        // Persistence/deduplication belongs to the existing profile registry.
        public void Apply(string sportId, string variant, string matchId, string utc, SportsMatchKind kind,
            bool winner, bool anyWinner, int place, double seconds, double? measurement, string unit, SportsRecordPreference preference,
            string title = null, string variantLabel = null)
        {
            if (string.IsNullOrWhiteSpace(sportId) || string.IsNullOrWhiteSpace(matchId) || string.IsNullOrWhiteSpace(utc) ||
                place < 0 || !double.IsFinite(seconds) || seconds < 0 || measurement.HasValue && !double.IsFinite(measurement.Value)) throw new ArgumentException("Statistics require an actual finite final result.");
            Normalize(); var sport = sports.FirstOrDefault(s => s.sport == sportId);
            if (sport == null) { sport = new SportsSportStatistic { sport = sportId }; sports.Add(sport); }
            if (!string.IsNullOrEmpty(title)) sport.title = title;
            matches++; sport.matches++; playedSeconds += seconds; sport.lastPlayedUtc = utc;
            if (kind == SportsMatchKind.Practice) { practice++; sport.practice++; }
            else if (kind == SportsMatchKind.Cooperative) { cooperative++; sport.cooperative++; }
            else
            {
                competitive++; sport.competitive++;
                if (winner) { wins++; sport.wins++; winnerPoints++; sport.winnerPoints++; }
                else if (anyWinner) { losses++; sport.losses++; }
                else { draws++; sport.draws++; }
                if (place > 0)
                {
                    var placing = sport.placements.FirstOrDefault(p => p.place == place);
                    if (placing == null) { placing = new SportsPlacementStatistic { place = place }; sport.placements.Add(placing); }
                    placing.count++;
                }
            }
            if (!measurement.HasValue || preference == SportsRecordPreference.Unspecified || string.IsNullOrEmpty(unit)) return;
            variant ??= "";
            var record = sport.records.FirstOrDefault(r => r.variant == variant && r.unit == unit && r.preference == preference);
            if (record != null && !(preference == SportsRecordPreference.Higher ? measurement.Value > record.value : measurement.Value < record.value)) return;
            if (record == null) { record = new SportsPersonalRecord { variant = variant, unit = unit, preference = preference }; sport.records.Add(record); }
            record.value = measurement.Value; record.matchId = matchId; record.utc = utc; record.variantLabel = variantLabel;
        }
    }
}
