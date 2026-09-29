using System;

namespace LWS.TruckTaxi
{
    public readonly struct TruckTaxiExtremeAttempt
    {
        public TruckTaxiExtremeAttempt(TruckTaxiExtremeKind kind, DateTime time, bool eligible,
            string reason, string source, string zoneId, string regionId, float playerDistanceMeters,
            bool started, bool visualMaterialized, bool alertSent, string endReason)
        {
            Kind = kind; Time = time; Eligible = eligible; Reason = reason; Source = source;
            ZoneId = zoneId; RegionId = regionId; PlayerDistanceMeters = playerDistanceMeters;
            Started = started; VisualMaterialized = visualMaterialized; AlertSent = alertSent;
            EndReason = endReason;
        }
        public TruckTaxiExtremeKind Kind { get; }
        public DateTime Time { get; }
        public bool Eligible { get; }
        public string Reason { get; }
        public string Source { get; }
        public string ZoneId { get; }
        public string RegionId { get; }
        public float PlayerDistanceMeters { get; }
        public bool Started { get; }
        public bool VisualMaterialized { get; }
        public bool AlertSent { get; }
        public string EndReason { get; }
    }

    public readonly struct TruckTaxiExtremeScheduleStatus
    {
        public TruckTaxiExtremeScheduleStatus(TruckTaxiExtremeKind kind, DateTime lastOccurrence,
            int eligibleDryDays, int daysSinceOccurrence, float probability, bool eligible,
            string rejectionReason)
        { Kind = kind; LastOccurrence = lastOccurrence; EligibleDryDays = eligibleDryDays;
          DaysSinceOccurrence = daysSinceOccurrence; Probability = probability;
          Eligible = eligible; RejectionReason = rejectionReason; }
        public TruckTaxiExtremeKind Kind { get; }
        public DateTime LastOccurrence { get; }
        public int EligibleDryDays { get; }
        public int DaysSinceOccurrence { get; }
        public float Probability { get; }
        public bool Eligible { get; }
        public string RejectionReason { get; }
    }
}
