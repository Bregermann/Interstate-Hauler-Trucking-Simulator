using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckSports
{
    public enum SportsScoreTier { Small, Major, RoundWinner, MatchChampion }
    public enum SportsScoreKind { Points, Outcome }

    // Read-only presentation contract; each original sport retains its own lifecycle/rules.
    public interface ISportsScorePresentationSource
    {
        string MatchId { get; }
        event Action<SportsScoreEvent> ScoreConfirmed;
        bool ScoreRunning { get; }
        bool ScorePaused { get; }
        bool ScoreFinished { get; }
        bool ScoreResultPage { get; }
        bool ScorePlayPage { get; }
        Camera ScoreCamera(int view);
        bool TryReadViewportCard(int view, out int participant, out string score, out string detail);
    }

    // Immutable presentation receipt. Gameplay owns the award and supplied total.
    public sealed class SportsScoreEvent
    {
        public readonly string SportId, MatchId, EventId, RecipientId, ParticipantId, DisplayName, TeamId, Reason, Unit;
        public readonly Color Color;
        public readonly bool IsAI, IsTeam, RoundEnded, MatchEnded;
        public readonly int Slot;
        public readonly double Delta, Total;
        public readonly SportsScoreTier Tier;
        public readonly SportsScoreKind Kind;
        // Only Outcome receipts carry this non-score measurement.
        public readonly double OutcomeValue;
        internal SportsScoreEvent(string sport, string match, string id, ISportsParticipantSlot player, int slot,
            double delta, double total, string reason, SportsScoreTier tier, string unit, string team, bool roundEnded, bool matchEnded)
        {
            SportId = sport; MatchId = match; EventId = id; ParticipantId = player.ParticipantId;
            RecipientId = "participant:" + ParticipantId; IsTeam = false;
            DisplayName = player.DisplayName; Color = player.ParticipantColor; IsAI = player.IsBot; Slot = slot;
            Delta = delta; Total = total; Reason = reason; Tier = tier; Unit = unit; TeamId = team;
            RoundEnded = roundEnded; MatchEnded = matchEnded;
        }
        internal SportsScoreEvent(string sport, string match, string id, string team, string name, Color color,
            double delta, double total, string reason, SportsScoreTier tier, string unit, bool roundEnded, bool matchEnded)
        {
            SportId = sport; MatchId = match; EventId = id; TeamId = team; RecipientId = "team:" + team;
            ParticipantId = null; Slot = -1; IsTeam = true; IsAI = false;
            DisplayName = name; Color = color; Delta = delta; Total = total; Reason = reason; Tier = tier; Unit = unit;
            RoundEnded = roundEnded; MatchEnded = matchEnded;
        }
        internal SportsScoreEvent(string sport, string match, string id, ISportsParticipantSlot player, int slot,
            double value, string reason, SportsScoreTier tier, string unit, bool roundEnded, bool matchEnded)
            : this(sport, match, id, player, slot, 0, 0, reason, tier, unit, null, roundEnded, matchEnded)
        { Kind = SportsScoreKind.Outcome; OutcomeValue = value; }
    }

    // Match-local semantics, independent of rendering and score storage. A receipt
    // cannot award points, evaluate winners, or read numbers from UI strings.
    public sealed class SportsScoreJournal
    {
        readonly HashSet<string> confirmed = new HashSet<string>(StringComparer.Ordinal);
        readonly string sport, match;
        bool closed;
        public int Count => confirmed.Count;
        public SportsScoreEvent Latest { get; private set; }
        public SportsScoreJournal(string sportId, string matchId)
        {
            if (string.IsNullOrWhiteSpace(sportId) || string.IsNullOrWhiteSpace(matchId)) throw new ArgumentException("Score receipts require a sport and match identity.");
            sport = sportId; match = matchId;
        }
        public bool TryConfirm(string eventId, ISportsParticipantSlot player, int slot, double delta, double total,
            string reason, SportsScoreTier tier, string unit, string team, bool roundEnded, bool matchEnded, out SportsScoreEvent receipt)
        {
            receipt = null;
            if (closed) return false;
            if (string.IsNullOrWhiteSpace(eventId) || player == null || string.IsNullOrWhiteSpace(player.ParticipantId) || slot < 0 ||
                double.IsNaN(delta) || double.IsInfinity(delta) || double.IsNaN(total) || double.IsInfinity(total) || string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Confirmed scores require an event ID, stable participant identity, finite values and a reason.");
            if (!confirmed.Add(eventId)) return false;
            receipt = Latest = new SportsScoreEvent(sport, match, eventId, player, slot, delta, total, reason, tier, unit, team, roundEnded, matchEnded);
            return true;
        }
        public bool TryConfirmOutcome(string eventId, ISportsParticipantSlot player, int slot, double value,
            string reason, SportsScoreTier tier, string unit, bool roundEnded, bool matchEnded, out SportsScoreEvent receipt)
        {
            receipt = null;
            if (closed) return false;
            if (string.IsNullOrWhiteSpace(eventId) || player == null || string.IsNullOrWhiteSpace(player.ParticipantId) || slot < 0 ||
                double.IsNaN(value) || double.IsInfinity(value) || value < 0 || string.IsNullOrWhiteSpace(reason) || string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Confirmed outcomes require an event ID, stable identity, finite nonnegative measurement, reason and unit.");
            if (!confirmed.Add(eventId)) return false;
            receipt = Latest = new SportsScoreEvent(sport, match, eventId, player, slot, value, reason, tier, unit, roundEnded, matchEnded);
            return true;
        }
        public void Close() => closed = true;
        public bool TryConfirmTeam(string eventId, string team, string name, Color color, double delta, double total,
            string reason, SportsScoreTier tier, string unit, bool roundEnded, bool matchEnded, out SportsScoreEvent receipt)
        {
            receipt = null;
            if (closed) return false;
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(team) || string.IsNullOrWhiteSpace(name) ||
                double.IsNaN(delta) || double.IsInfinity(delta) || double.IsNaN(total) || double.IsInfinity(total) || string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Confirmed team scores require an event ID, stable team identity/name, finite values and a reason.");
            if (!confirmed.Add(eventId)) return false;
            receipt = Latest = new SportsScoreEvent(sport, match, eventId, team, name, color, delta, total, reason, tier, unit, roundEnded, matchEnded);
            return true;
        }
    }
}
