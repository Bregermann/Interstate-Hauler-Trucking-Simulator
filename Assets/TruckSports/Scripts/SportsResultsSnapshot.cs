using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace LWS.TruckSports
{
    // Read-only results supplied by the rules owner. Neither sorting nor a UI
    // animation evaluates winners, adds points, or parses an existing HUD.
    public sealed class SportsResultRow
    {
        public readonly string ParticipantId, DisplayName, Detail, Unit, Format, FallbackValue;
        public readonly Color Color;
        public readonly bool IsAI, Winner;
        public readonly int Slot, Place;
        public readonly double? Measurement;
        public SportsResultRow(ISportsParticipantSlot participant, int slot, int place, bool winner,
            double? measurement, string unit, string detail, string fallbackValue = "—", string format = "0.###")
        {
            if (participant == null || string.IsNullOrWhiteSpace(participant.ParticipantId) || slot < 0 || place < 0 || winner && place != 1)
                throw new ArgumentException("Results require a stable identity, valid slot and explicit authoritative placing.");
            if (measurement.HasValue && (double.IsNaN(measurement.Value) || double.IsInfinity(measurement.Value)))
                throw new ArgumentException("A displayed measurement must be finite; use a nonnumeric DNF state for incomplete attempts.");
            ParticipantId = participant.ParticipantId; DisplayName = participant.DisplayName; Color = participant.ParticipantColor;
            IsAI = participant.IsBot; Slot = slot; Place = place; Winner = winner; Measurement = measurement;
            Unit = unit ?? ""; Detail = detail ?? ""; FallbackValue = fallbackValue ?? "—"; Format = format ?? "0.###";
        }
        public string Value => Measurement.HasValue ? Measurement.Value.ToString(Format, CultureInfo.InvariantCulture) + (Unit.Length > 0 ? " " + Unit : "") : FallbackValue;
    }

    public sealed class SportsResultsSnapshot
    {
        public readonly string Heading, Summary;
        public IReadOnlyList<SportsResultRow> Rows { get; }
        public SportsResultsSnapshot(string heading, string summary, IEnumerable<SportsResultRow> rows)
        {
            if (string.IsNullOrWhiteSpace(heading) || rows == null) throw new ArgumentException("Results require a heading and explicit participant rows.");
            var entries = rows.ToArray();
            if (entries.Length < 1 || entries.Length > 12 || entries.Any(row => row == null) ||
                entries.Select(row => row.ParticipantId).Distinct(StringComparer.Ordinal).Count() != entries.Length || entries.Select(row => row.Slot).Distinct().Count() != entries.Length)
                throw new ArgumentException("Results require one unique stable identity and slot per participant, with at most twelve rows.");
            Heading = heading; Summary = summary ?? "";
            Rows = Array.AsReadOnly(entries.OrderBy(row => row.Place == 0 ? int.MaxValue : row.Place).ThenBy(row => row.Slot).ToArray());
        }
    }
}
