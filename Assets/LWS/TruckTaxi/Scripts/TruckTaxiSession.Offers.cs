using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        private IEnumerator<bool> offerGeneration;
        public bool IsGeneratingOffer => offerGeneration != null;
        public int LocalOffersBeforeIntercity => IntercityPolicy.LocalCooldownRemaining;
        public int LastOfferRouteQueries { get; private set; }
        public double LastOfferMaximumStepMs { get; private set; }
        public string LastOfferDiagnostics { get; private set; } = "No offer attempted.";
        private readonly Dictionary<string, double> offerStages = new Dictionary<string, double>();
        private readonly Stopwatch offerClock = new Stopwatch();

        public bool RequestRideOffer(PassengerProfile forcedPassenger = null)
        {
            if (IsGeneratingOffer || State != TruckTaxiState.Available || !RideRequestsEnabled || OffersSuppressed) return false;
            LastOfferRouteQueries = 0; LastOfferMaximumStepMs = 0; offerStages.Clear();
            offerClock.Restart();
            offerGeneration = BuildOffer(forcedPassenger);
            return true;
        }

        // Compatibility for focused deterministic fixtures. Normal dispatch/debug uses the incremental API.
        public bool OfferRide(PassengerProfile forcedPassenger = null)
        {
            if (!RequestRideOffer(forcedPassenger)) return false;
            while (IsGeneratingOffer) AdvanceOfferGeneration();
            return State == TruckTaxiState.RideOffered;
        }

        private void CancelOfferGeneration()
        {
            offerGeneration?.Dispose(); offerGeneration = null; offerClock.Stop();
        }

        private void AdvanceOfferGeneration()
        {
            if (offerGeneration == null) return;
            if (State != TruckTaxiState.Available || !RideRequestsEnabled || OffersSuppressed ||
                offerClock.Elapsed.TotalSeconds > Mathf.Clamp(config.offerTimeoutSeconds, 1, 10))
            { FinishOfferDiagnostics("cancelled/timeout"); return; }
            var frame = Stopwatch.StartNew();
            bool pending;
            do
            {
                var step = Stopwatch.StartNew();
                pending = offerGeneration.MoveNext();
                LastOfferMaximumStepMs = Math.Max(LastOfferMaximumStepMs, step.Elapsed.TotalMilliseconds);
            }
            while (pending && frame.Elapsed.TotalMilliseconds < Mathf.Clamp(config.offerFrameBudgetMilliseconds, .5f, 8));
            if (!pending) FinishOfferDiagnostics(State == TruckTaxiState.RideOffered ? "offered" : "no eligible offer");
        }

        private void FinishOfferDiagnostics(string result)
        {
            var text = new System.Text.StringBuilder($"{result}; {LastOfferRouteQueries} routes; max step {LastOfferMaximumStepMs:F2}ms; wall {offerClock.Elapsed.TotalMilliseconds:F1}ms");
            foreach (var stage in offerStages) text.Append($"; {stage.Key}={stage.Value:F2}ms");
            LastOfferDiagnostics = text.ToString();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (config.logOfferTimings) UnityEngine.Debug.Log("TAXI OFFER: " + LastOfferDiagnostics);
#endif
            CancelOfferGeneration();
            if (State == TruckTaxiState.Available) { StateAge = 0; ScheduleDispatch(); }
        }

        private void RecordOfferStage(string stage, Stopwatch clock)
        {
            offerStages.TryGetValue(stage, out double previous);
            offerStages[stage] = previous + clock.Elapsed.TotalMilliseconds;
        }

        private IEnumerator<bool> BuildOffer(PassengerProfile forced)
        {
            Vector3 origin = playerPosition();
            var timer = Stopwatch.StartNew();
            float hardMeters = Mathf.Max(1, config.pickupHardMaximumSeconds) * Mathf.Max(1, config.pickupReasonableSpeedMetersPerSecond);
            var nearby = new List<TruckTaxiRideLocation>();
            foreach (var stop in locations)
                if (stop != null && stop.isActiveAndEnabled && stop.pickupAllowed && workArea.Contains(stop.StopPosition) &&
                    (stop.StopPosition - origin).sqrMagnitude <= hardMeters * hardMeters) nearby.Add(stop);
            nearby.Sort((a, b) => CompareNear(a, b, origin));
            int limit = Mathf.Clamp(config.offerPickupCandidateLimit, 1, 12);
            if (nearby.Count > limit) nearby.RemoveRange(limit, nearby.Count - limit);
            RecordOfferStage("spatial/metadata", timer);
            yield return true;

            timer.Restart();
            var candidates = forced != null ? new List<PassengerProfile> { forced } : config.passengerDatabase != null
                ? new List<PassengerProfile>(config.passengerDatabase.Query(new TruckTaxiPassengerQuery { availableOnly = true }))
                : new List<PassengerProfile>(config.passengers ?? Array.Empty<PassengerProfile>());
            var eligible = new List<(PassengerProfile profile, float weight)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var references = new HashSet<PassengerProfile>();
            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.available || !references.Add(candidate) ||
                    (!string.IsNullOrEmpty(candidate.passengerId) && !seen.Add(candidate.passengerId))) continue;
                bool hasPickup = false;
                foreach (var stop in nearby) if (CanStartAt(candidate, stop)) { hasPickup = true; break; }
                float weight = CandidateWeight(candidate);
                if(PrefersSpeedway(candidate) && nearby.Exists(IsSpeedwayStop)) weight*=Mathf.Max(1,config.speedwayPassengerWeight);
                if (hasPickup && float.IsFinite(weight) && weight > 0) eligible.Add((candidate, weight));
            }
            if (eligible.Count == 0) yield break;
            double total = 0; foreach (var item in eligible) total += item.weight;
            double roll = random.NextDouble() * total;
            var passenger = eligible[eligible.Count - 1].profile;
            foreach (var item in eligible) { roll -= item.weight; if (roll < 0) { passenger = item.profile; break; } }
            RecordOfferStage("passenger/continuity/weight", timer);
            yield return true;

            var pickups = new List<(TruckTaxiRideLocation stop, TruckTaxiRouteLeg leg)>();
            foreach (var stop in nearby)
            {
                if (!CanStartAt(passenger, stop)) continue;
                timer.Restart(); LastOfferRouteQueries++;
                var leg = routeDistances.Measure(origin, stop.StopPosition);
                RecordOfferStage("pickup routes", timer);
                if ((!requireNavigableRoute || leg.Navigable) && float.IsFinite(leg.Meters) && leg.Meters <= hardMeters)
                    pickups.Add((stop, leg));
                yield return true;
            }
            if (pickups.Count == 0) yield break;
            var preferred = pickups.FindAll(p => p.leg.Meters / Mathf.Max(1, config.pickupReasonableSpeedMetersPerSecond) >= config.pickupTargetMinimumSeconds &&
                p.leg.Meters / Mathf.Max(1, config.pickupReasonableSpeedMetersPerSecond) <= config.pickupTargetMaximumSeconds);
            if (preferred.Count > 0) pickups = preferred;
            double pickupTotal=0;
            foreach(var candidate in pickups) pickupTotal+=ValidOfferWeight(PickupWeight?.Invoke(candidate.stop.StopPosition) ?? 1);
            double pickupRoll=random.NextDouble()*pickupTotal;
            var pickup=pickups[pickups.Count-1];
            foreach(var candidate in pickups) { pickupRoll-=ValidOfferWeight(PickupWeight?.Invoke(candidate.stop.StopPosition) ?? 1); if(pickupRoll<0) { pickup=candidate; break; } }
            bool wantIntercity = IntercityPolicy.WantsIntercity(random,out bool guaranteed);
            LastIntercitySelection=guaranteed ? "GUARANTEE" : wantIntercity ? "RANDOM" : "LOCAL";
            // One pickup, bounded destinations. A local-only attempt never becomes an intercity fallback.
            for (int pass = 0; pass < (wantIntercity && !guaranteed ? 2 : 1); pass++)
            {
                bool across = wantIntercity && pass == 0;
                timer.Restart();
                var destinations = new List<TruckTaxiRideLocation>();
                float maximum = across ? config.intercityMaximumTripDistance : config.maximumTripDistance;
                foreach (var destination in locations)
                {
                    if (destination == null || !destination.dropoffAllowed || destination == pickup.stop ||
                        destination.locationId == pickup.stop.locationId || IsAcrossRegions(pickup.stop, destination) != across ||
                        (destination.StopPosition - pickup.stop.StopPosition).sqrMagnitude > maximum * maximum) continue;
                    destinations.Add(destination);
                }
                // Shuffle only cheap metadata. Routed validation stops at the first valid bounded candidate.
                for (int i = destinations.Count - 1; i > 0; i--)
                { int j = random.Next(i + 1); var temp = destinations[i]; destinations[i] = destinations[j]; destinations[j] = temp; }
                if(DestinationWeight!=null)
                {
                    // Weighted random order, not highest-weight-first: quiet destinations remain possible.
                    var priorities=new Dictionary<TruckTaxiRideLocation,double>();
                    foreach(var destination in destinations)
                        priorities[destination]=-Math.Log(Math.Max(double.Epsilon,random.NextDouble())) /
                            ValidOfferWeight(DestinationWeight(destination.StopPosition));
                    destinations.Sort((a,b)=>priorities[a].CompareTo(priorities[b]));
                }
                if(PrefersSpeedway(passenger) && random.NextDouble()<config.racingSpeedwayDestinationChance)
                    destinations.Sort((a,b)=>IsSpeedwayStop(b).CompareTo(IsSpeedwayStop(a)));
                RecordOfferStage("destination discovery", timer);
                yield return true;
                int destinationLimit = Math.Min(destinations.Count, Mathf.Clamp(config.offerDestinationCandidateLimit, 1, 24));
                for (int i = 0; i < destinationLimit; i++)
                {
                    timer.Restart(); LastOfferRouteQueries++;
                    var trip = routeDistances.BetweenStops(pickup.stop, destinations[i]);
                    RecordOfferStage("destination routes", timer);
                    yield return true;
                    if (requireNavigableRoute && !trip.Navigable || trip.Meters < config.minimumTripDistance || trip.Meters > maximum) continue;
                    timer.Restart();
                    CompleteOffer(passenger, pickup.stop, destinations[i], origin, pickup.leg, trip);
                    RecordOfferStage("fare/UI notification/map", timer);
                    yield break;
                }
            }
        }

        private static int CompareNear(TruckTaxiRideLocation a, TruckTaxiRideLocation b, Vector3 origin)
        {
            int distance = (a.StopPosition - origin).sqrMagnitude.CompareTo((b.StopPosition - origin).sqrMagnitude);
            return distance != 0 ? distance : string.CompareOrdinal(a.locationId, b.locationId);
        }
        private static bool IsSpeedwayStop(TruckTaxiRideLocation stop) => stop!=null &&
            string.Equals(stop.district,"Speedway",StringComparison.OrdinalIgnoreCase);
        private static bool PrefersSpeedway(PassengerProfile passenger) => passenger!=null &&
            (HasTag(passenger,"racing","racer") || Array.Exists(passenger.possibleRequests ?? Array.Empty<PassengerRequestDefinition>(),
                request=>request!=null && request.requestType==TaxiRequestType.TakeALap));
    }
}
