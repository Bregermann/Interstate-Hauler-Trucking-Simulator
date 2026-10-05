using System;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    // Observes the existing session authority. Never persists or restores gameplay state.
    public sealed class TruckTaxiAnalyticsSessionObserver : IDisposable
    {
        private readonly TruckTaxiSession session;
        private readonly Action<TruckTaxiAnalyticsEvent> record;
        private readonly TruckTaxiAnalyticsContentIds contentIds;
        private readonly Dictionary<string, TruckTaxiAnalyticsArchetype> roster = new Dictionary<string, TruckTaxiAnalyticsArchetype>(StringComparer.Ordinal);
        private readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        private TruckTaxiRideOffer offer;
        private TruckTaxiState previous;
        private string passengerId = "", acceptedId = "";
        private int historyCursor;
        private bool disposed;
        public int RosterCount => roster.Count;
        public int UniquePassengers => seen.Count;
        public int CompletedRides { get; private set; }
        public int Crashes { get; private set; }
        public int Offers { get; private set; }
        public double CoveragePercent => roster.Count > 0 ? 100d * seen.Count / roster.Count : 0;
        public string ApprovedPassenger(string id) => id != null && roster.ContainsKey(id) ? id : "";

        public TruckTaxiAnalyticsSessionObserver(TruckTaxiSession session, IEnumerable<PassengerProfile> profiles, Action<TruckTaxiAnalyticsEvent> record,
            TruckTaxiAnalyticsContentIds contentIds = null)
        {
            this.contentIds = contentIds ?? new TruckTaxiAnalyticsContentIds();
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.record = record ?? throw new ArgumentNullException(nameof(record));
            foreach (var profile in profiles ?? Array.Empty<PassengerProfile>())
            {
                if (profile == null || roster.Count >= 512) continue;
                string id = TruckTaxiAnalyticsPrivacy.Token(profile.passengerId);
                if (id == "" || roster.ContainsKey(id)) continue;
                roster.Add(id, Archetype(profile.archetype));
                record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.RosterMember, passengerId = id, archetype = roster[id] });
            }
            historyCursor = session.RideHistory.Count;
            previous = session.State;
            // Attach before the first shift for full coverage; existing history is intentionally not replayed.
            offer = session.Offer;
            passengerId = ApprovedPassenger(session.Passenger?.passengerId);
            acceptedId = session.CurrentRideId;
            session.Changed += Changed;
            session.RequestCreated += Created;
            session.RequestResolved += Resolved;
            session.DiversionOffered += Diversion;
            session.DrivingEvent += Driving;
            session.CustomItemInteraction += CustomItem;
            session.PassengerCustomInteractionStarted += CustomStarted;
            session.PassengerCustomInteractionCompleted += CustomCompleted;
        }
        private static TruckTaxiAnalyticsArchetype Archetype(string authored)
        {
            switch ((authored ?? "").ToLowerInvariant())
            {
                case "racing": case "race-driver": return TruckTaxiAnalyticsArchetype.Racing;
                case "business": return TruckTaxiAnalyticsArchetype.Business;
                case "chaos": case "chaotic": return TruckTaxiAnalyticsArchetype.Chaos;
                case "glamorous": return TruckTaxiAnalyticsArchetype.Glamorous;
                case "commuter": return TruckTaxiAnalyticsArchetype.Commuter;
                case "elderly": return TruckTaxiAnalyticsArchetype.Elderly;
                case "game-parody": return TruckTaxiAnalyticsArchetype.GameParody;
                case "fantasy": case "fantasy/special": return TruckTaxiAnalyticsArchetype.Fantasy;
                default: return TruckTaxiAnalyticsArchetype.Other;
            }
        }
        private TruckTaxiAnalyticsEvent New(TruckTaxiAnalyticsKind kind)
        {
            return new TruckTaxiAnalyticsEvent { kind = kind, passengerId = passengerId,
                archetype = roster.TryGetValue(passengerId, out var a) ? a : TruckTaxiAnalyticsArchetype.Other,
                rideId = TruckTaxiAnalyticsPrivacy.RandomId(session.CurrentRideId),
                gameMinutes = session.GameMinutes, intercity = session.IsIntercityRide };
        }
        private void Changed()
        {
            if (disposed) return;
            if (session.Offer != null && !ReferenceEquals(offer, session.Offer))
            {
                offer = session.Offer; passengerId = ApprovedPassenger(session.Passenger?.passengerId);
                acceptedId = ""; Offers++;
                if (passengerId != "") seen.Add(passengerId);
                else record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.Error, code = (int)TruckTaxiAnalyticsError.InvalidPassenger });
                var e = New(TruckTaxiAnalyticsKind.OfferGenerated); e.demandMultiplier = session.DemandFareMultiplier; record(e);
            }
            if (previous == TruckTaxiState.RideOffered && session.Offer == null)
            {
                // DeclineRide also handles expiry/recovery. Its Changed event has already reset StateAge.
                record(New(TruckTaxiAnalyticsKind.OfferClosedUnknown)); offer = null;
            }
            if (session.State == TruckTaxiState.DrivingToPickup && session.CurrentRideId != acceptedId)
            { acceptedId = session.CurrentRideId; record(New(TruckTaxiAnalyticsKind.OfferAccepted)); }
            if (session.State == TruckTaxiState.DrivingToDestination && previous == TruckTaxiState.PassengerBoarding)
            {
                var e = New(TruckTaxiAnalyticsKind.PickupCompleted); e.pickupSeconds = session.PickupElapsed; record(e);
            }
            var history = session.RideHistory;
            if (historyCursor > history.Count) historyCursor = history.Count;
            while (historyCursor < history.Count)
            {
                var ride = history[historyCursor++];
                var e = New(TruckTaxiAnalyticsKind.RideFinished);
                e.passengerId = ApprovedPassenger(ride.PassengerId); e.rideId = TruckTaxiAnalyticsPrivacy.RandomId(ride.RideId);
                e.outcome = (int)ride.Outcome; e.gameMinutes = ride.EndGameMinutes;
                e.rideSeconds = ride.RideSeconds; e.pickupSeconds = session.PickupElapsed; e.distanceMeters = ride.DistanceMeters;
                e.fareCents = ride.Fare?.Total ?? 0; e.tipCents = ride.Fare?.Tip ?? 0;
                e.eventBonusCents = ride.Fare?.EventBonus ?? 0; e.weatherBonusCents = ride.Fare?.WeatherBonus ?? 0;
                e.rating = ride.Fare?.Rating ?? -1; e.patience = session.OnboardPatience;
                e.demandMultiplier = session.DemandFareMultiplier;
                if (ride.Outcome == TruckTaxiRideOutcome.Completed) CompletedRides++;
                record(e);
            }
            previous = session.State;
        }
        private void Objective(TaxiRequestProgress request, TruckTaxiAnalyticsKind kind)
        {
            if (request?.Definition == null) return;
            var e = New(kind); e.code = (int)request.Definition.requestType; e.outcome = (int)request.State;
            e.rideSeconds = request.Elapsed; record(e);
        }
        private void Created(TaxiRequestProgress request) => Objective(request, TruckTaxiAnalyticsKind.ObjectiveCreated);
        private void Resolved(TaxiRequestProgress request) => Objective(request, TruckTaxiAnalyticsKind.ObjectiveResolved);
        private void Diversion(TaxiRequestProgress request) => Objective(request, TruckTaxiAnalyticsKind.DiversionOffered);
        private void Driving(TaxiEventType type)
        {
            if (type == TaxiEventType.Collision || type == TaxiEventType.TrafficRam) Crashes++;
            var e = New(TruckTaxiAnalyticsKind.DrivingEvent); e.code = (int)type; record(e);
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            session.Changed -= Changed; session.RequestCreated -= Created; session.RequestResolved -= Resolved;
            session.DiversionOffered -= Diversion; session.DrivingEvent -= Driving;
            session.CustomItemInteraction -= CustomItem;
            session.PassengerCustomInteractionStarted -= CustomStarted;
            session.PassengerCustomInteractionCompleted -= CustomCompleted;
        }
        private void CustomItem(TruckTaxiCustomItemSignal signal)
        {
            var e = New(TruckTaxiAnalyticsKind.CustomItemInteraction);
            e.customEventId = contentIds.Alias(signal.EventId);
            if (e.customEventId == "") return;
            e.customItemId = contentIds.Alias(signal.ItemId);
            e.customContentId = contentIds.Alias(signal.InteractionId); record(e);
        }
        private void CustomStarted(string id) => CustomLifecycle(id, TruckTaxiAnalyticsKind.CustomItemStarted);
        private void CustomCompleted(string id) => CustomLifecycle(id, TruckTaxiAnalyticsKind.CustomItemCompleted);
        private void CustomLifecycle(string id, TruckTaxiAnalyticsKind kind)
        {
            string alias = contentIds.Alias(id); if (alias == "") return;
            var e = New(kind); e.customContentId = alias; record(e);
        }
    }
}
