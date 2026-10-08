using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Immutable trip identity; only the existing Session may transfer or settle it.
    public sealed class TruckTaxiRivalFare
    {
        public string Id { get; }
        public string RivalId { get; }
        public string PassengerId { get; }
        public PassengerProfile Passenger { get; }
        public string DestinationId { get; }
        public string DestinationName { get; }
        public Vector3 DestinationPosition { get; }
        public float DestinationRadius { get; }
        public long PayoutCents { get; }
        internal TruckTaxiRivalFare(string rivalId, PassengerProfile passenger, TruckTaxiRideLocation destination, long cents)
        {
            Id = rivalId + ".fare." + Guid.NewGuid().ToString("N");
            RivalId = rivalId; Passenger = passenger; PassengerId = passenger.passengerId;
            DestinationId = destination.locationId; DestinationName = destination.locationName;
            DestinationPosition = destination.StopPosition; DestinationRadius = destination.detectionRadius;
            PayoutCents = cents;
        }
    }

    public sealed partial class TruckTaxiSession
    {
        private readonly Dictionary<string, TruckTaxiRivalFare> rivalFares =
            new Dictionary<string, TruckTaxiRivalFare>(StringComparer.Ordinal);
        private readonly object rivalOfferSuppression = new object();
        private int rivalNotificationDepth;
        public TruckTaxiRivalFare StolenPassengerFare { get; private set; }
        public bool HasStolenPassenger => StolenPassengerFare != null;
        public event Action RivalPassengerChanged;

        public bool TryCreateRivalFare(string rivalId, PassengerProfile profile, Vector3 origin,
            out TruckTaxiRivalFare fare)
        {
            fare = null;
            if (rivalNotificationDepth != 0 || State == TruckTaxiState.Inactive || string.IsNullOrWhiteSpace(rivalId) ||
                !rivalId.StartsWith("taxi.rival.", StringComparison.Ordinal) || !Finite(origin) ||
                !TruckTaxiRivalPopulation.IsIdentityEligible(profile, Passenger, null) || rivalFares.Count >= 32)
                return false;
            foreach (var existing in rivalFares.Values)
                if (existing.RivalId == rivalId || existing.PassengerId == profile.passengerId) return false;
            TruckTaxiRideLocation destination = null;
            TruckTaxiRouteLeg trip = null;
            float shortest = float.PositiveInfinity;
            foreach (var candidate in locations)
            {
                if (candidate == null || !candidate.isActiveAndEnabled || !candidate.dropoffAllowed ||
                    string.IsNullOrWhiteSpace(candidate.locationId) || !Finite(candidate.StopPosition) ||
                    !float.IsFinite(candidate.detectionRadius) || candidate.detectionRadius <= 0) continue;
                // Ambiguous authored IDs cannot become a payout target.
                int matches = 0;
                foreach (var other in locations)
                    if (other != null && other.locationId == candidate.locationId) matches++;
                if (matches != 1) continue;
                var leg = routeDistances.Measure(origin, candidate.StopPosition);
                if (requireNavigableRoute && !leg.Navigable || !float.IsFinite(leg.Meters) ||
                    leg.Meters < Mathf.Max(candidate.detectionRadius * 2, config.minimumTripDistance) ||
                    leg.Meters > config.maximumTripDistance || leg.Meters >= shortest) continue;
                shortest = leg.Meters; destination = candidate; trip = leg;
            }
            if (destination == null) return false;
            double quoted = config.baseFareCents + Math.Round((double)trip.Meters * config.centsPerMeter);
            if (double.IsNaN(quoted) || double.IsInfinity(quoted) || quoted <= 0 || quoted >= long.MaxValue) return false;
            fare = new TruckTaxiRivalFare(rivalId, profile, destination, (long)quoted);
            rivalFares.Add(fare.Id, fare);
            return true;
        }

        public bool OwnsRivalFare(string rivalId, string fareId) =>
            fareId != null && rivalFares.TryGetValue(fareId, out var fare) &&
            fare.RivalId == rivalId && !ReferenceEquals(fare, StolenPassengerFare);

        public bool CanStealRivalPassenger(string rivalId, string fareId, bool hasSecondaryCapacity)
        {
            if (rivalNotificationDepth != 0 || !hasSecondaryCapacity || HasStolenPassenger || RaceEventActive || SafeDropRequested ||
                State != TruckTaxiState.Available && State != TruckTaxiState.DrivingToDestination ||
                !OwnsRivalFare(rivalId, fareId)) return false;
            var fare = rivalFares[fareId];
            return fare.Passenger != null && fare.Passenger.passengerId == fare.PassengerId &&
                TruckTaxiRivalPopulation.IsIdentityEligible(fare.Passenger, Passenger, null);
        }

        public bool TryStealRivalPassenger(string rivalId, string fareId, bool hasSecondaryCapacity)
        {
            if (!CanStealRivalPassenger(rivalId, fareId, hasSecondaryCapacity)) return false;
            var transferred = rivalFares[fareId];
            StolenPassengerFare = transferred;
            AcquireOfferSuppression(rivalOfferSuppression);
            React("PASSENGER STOLEN\nNEW STOP ADDED");
            PublishRivalPassengerChange();
            return ReferenceEquals(StolenPassengerFare, transferred);
        }

        public bool TryDeliverStolenPassenger(string fareId, string destinationId, Vector3 position, float speed)
        {
            var fare = StolenPassengerFare;
            if (rivalNotificationDepth != 0 || fare == null || fare.Id != fareId || fare.DestinationId != destinationId || !Finite(position) ||
                !float.IsFinite(speed) || speed < 0 || speed > config.stoppedSpeed || State == TruckTaxiState.Inactive ||
                Vector3.ProjectOnPlane(position - fare.DestinationPosition, Vector3.up).sqrMagnitude >
                    fare.DestinationRadius * fare.DestinationRadius || ShiftEarnings > long.MaxValue - fare.PayoutCents)
                return false;
            // Consume the exact fare before publishing events; reentrant/repeated callbacks cannot pay twice.
            if (!rivalFares.Remove(fare.Id)) return false;
            StolenPassengerFare = null;
            ShiftEarnings += fare.PayoutCents;
            React("STOLEN PASSENGER DELIVERED");
            PublishRivalPassengerChange(releaseSuppression: true);
            return true;
        }

        public bool ReleaseRivalFare(string rivalId, string fareId)
        {
            if (!OwnsRivalFare(rivalId, fareId)) return false;
            return rivalFares.Remove(fareId);
        }

        public bool CancelStolenPassenger(string fareId)
        {
            if (StolenPassengerFare == null || StolenPassengerFare.Id != fareId) return false;
            rivalFares.Remove(fareId); StolenPassengerFare = null;
            PublishRivalPassengerChange(releaseSuppression: true);
            return true;
        }

        // In-flight fares are transient, like the primary ride. Call at shift reset and accepted load.
        public void ResetRivalPassengerTheft()
        {
            rivalFares.Clear(); StolenPassengerFare = null;
            // Teardown/load must not invoke demand callbacks or schedule new dispatch.
            offerSuppressors.Remove(rivalOfferSuppression);
            PublishRivalPassengerChange(notifySession: false);
        }

        private void PublishRivalPassengerChange(bool releaseSuppression = false, bool notifySession = true)
        {
            rivalNotificationDepth++;
            try
            {
                if (releaseSuppression) ReleaseOfferSuppression(rivalOfferSuppression);
                RivalPassengerChanged?.Invoke();
                if (notifySession) Changed?.Invoke();
            }
            finally { rivalNotificationDepth--; }
        }

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
