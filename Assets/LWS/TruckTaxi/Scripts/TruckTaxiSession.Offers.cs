using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        private IEnumerator<bool> offerGeneration;
        private string declinedPassengerId, declinedPickupId, declinedDestinationId;
        public int LastOfferCandidateCount { get; private set; }
        public int LastOfferRejectedPickupRoutes { get; private set; }
        public int LastOfferRejectedDestinationRoutes { get; private set; }
        public bool IsGeneratingOffer => offerGeneration != null;
        public int LocalOffersBeforeIntercity => IntercityPolicy.LocalCooldownRemaining;
        public int LastOfferRouteQueries { get; private set; }
        public double LastOfferMaximumStepMs { get; private set; }
        public string LastOfferDiagnostics { get; private set; } = "No offer attempted.";
        private readonly Dictionary<string, double> offerStages = new Dictionary<string, double>();
        private readonly Stopwatch offerClock = new Stopwatch();

        public bool RequestRideOffer(PassengerProfile forcedPassenger = null)
        {
            RefreshManualDeclineCooldown();
            if(ManualDeclineCooldownActive) return false;
            if (IsGeneratingOffer || State != TruckTaxiState.Available || !RideRequestsEnabled || OffersSuppressed) return false;
            LastOfferRouteQueries = 0; LastOfferMaximumStepMs = 0; offerStages.Clear();
            LastOfferCandidateCount=LastOfferRejectedPickupRoutes=LastOfferRejectedDestinationRoutes=0;
            offerClock.Restart();
            offerGeneration = BuildOffer(forcedPassenger);
            return true;
        }

        // Compatibility for focused deterministic fixtures. Normal dispatch/debug uses the incremental API.
        public bool OfferRide(PassengerProfile forcedPassenger = null)
        {
            if(forcedPassenger!=null && IsGeneratingOffer) CancelOfferGeneration();
            if (!IsGeneratingOffer && !RequestRideOffer(forcedPassenger)) return false;
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
            if (State != TruckTaxiState.Available || !RideRequestsEnabled || OffersSuppressed || ManualDeclineCooldownActive ||
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
            var text = new System.Text.StringBuilder($"{result}; {LastOfferCandidateCount} candidates; {LastOfferRouteQueries} routes; pickup rejects={LastOfferRejectedPickupRoutes}; destination rejects={LastOfferRejectedDestinationRoutes}; intercity={LastIntercitySelection}; max step {LastOfferMaximumStepMs:F2}ms; wall {offerClock.Elapsed.TotalMilliseconds:F1}ms");
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
                ? new List<PassengerProfile>(config.passengerDatabase.passengers ?? Array.Empty<PassengerProfile>())
                : new List<PassengerProfile>(config.passengers ?? Array.Empty<PassengerProfile>());
            var eligible = new List<(PassengerProfile profile, float weight)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var references = new HashSet<PassengerProfile>();
            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.available || string.IsNullOrWhiteSpace(candidate.passengerId) || !references.Add(candidate) ||
                    !seen.Add(candidate.passengerId)) continue;
                bool hasPickup = false;
                foreach (var stop in nearby) if (CanStartAt(candidate, stop)) { hasPickup = true; break; }
                float weight = CandidateWeight(candidate);
                if(PrefersSpeedway(candidate))
                    weight*=RaceEventActive ? Mathf.Clamp(config.speedwayPassengerWeight,1,1.5f) :
                        nearby.Exists(IsSpeedwayStop) ? Mathf.Clamp(config.speedwayPassengerWeight,1,1.15f) : 1;
                if (hasPickup && float.IsFinite(weight) && weight > 0) eligible.Add((candidate, weight));
            }
            if (eligible.Count == 0) yield break;
            LastOfferCandidateCount=eligible.Count;
            bool wantIntercity = IntercityPolicy.WantsIntercity(random,out bool guaranteed);
            LastIntercitySelection=guaranteed ? "GUARANTEE" : wantIntercity ? "RANDOM" : "LOCAL";
            if(wantIntercity && !nearby.Exists(p=>locations.Exists(d=>d!=null && d.isActiveAndEnabled && d.dropoffAllowed && IsAcrossRegions(p,d))))
            {
                wantIntercity=guaranteed=false;
                LastIntercitySelection="INELIGIBLE: no cross-region destination; completed-local streak retained";
            }
            while(eligible.Count>0)
            {
                var choices=new List<TruckTaxiPassengerCandidate>(eligible.Count);
                foreach(var item in eligible) choices.Add(new TruckTaxiPassengerCandidate(item.profile.passengerId,item.profile.DispatchArchetype,item.weight));
                string selected=passengerDispatch.Select(choices,random,RaceEventActive,declinedPassengerId);
                int selectedIndex=eligible.FindIndex(item=>string.Equals(item.profile.passengerId,selected,StringComparison.OrdinalIgnoreCase));
                if(selectedIndex<0) yield break;
                var passenger=eligible[selectedIndex].profile;
                eligible.RemoveAt(selectedIndex);
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
                    else LastOfferRejectedPickupRoutes++;
                    yield return true;
                }
                if (pickups.Count == 0) continue;
                var preferred = pickups.FindAll(p => p.leg.Meters / Mathf.Max(1, config.pickupReasonableSpeedMetersPerSecond) >= config.pickupTargetMinimumSeconds &&
                    p.leg.Meters / Mathf.Max(1, config.pickupReasonableSpeedMetersPerSecond) <= config.pickupTargetMaximumSeconds);
                var pickupPriorities=new Dictionary<TruckTaxiRideLocation,double>();
                foreach(var candidate in pickups) pickupPriorities[candidate.stop]=
                    -Math.Log(Math.Max(double.Epsilon,random.NextDouble()))/ValidOfferWeight(PickupWeight?.Invoke(candidate.stop.StopPosition) ?? 1);
                pickups.Sort((a,b)=>
                {
                    int avoid=(a.stop.locationId==declinedPickupId).CompareTo(b.stop.locationId==declinedPickupId);
                    if(avoid!=0) return avoid;
                    int target=preferred.Contains(b).CompareTo(preferred.Contains(a));
                    return target!=0 ? target : pickupPriorities[a.stop].CompareTo(pickupPriorities[b.stop]);
                });
                // Try other legal pickups when a favored bay has no valid destination.
                for (int pass = 0; pass < (wantIntercity && !guaranteed ? 2 : 1); pass++)
                foreach(var pickup in pickups)
                {
                    bool across = wantIntercity && pass == 0;
                    timer.Restart();
                    var destinations = new List<TruckTaxiRideLocation>();
                    float maximum = across ? config.intercityMaximumTripDistance : config.maximumTripDistance;
                    foreach (var destination in locations)
                    {
                        if (destination == null || !destination.isActiveAndEnabled || !destination.dropoffAllowed || destination == pickup.stop ||
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
                    // Stable partition preserves randomized order among alternative destinations.
                    if(!string.IsNullOrEmpty(declinedDestinationId))
                    {
                        var previous=destinations.FindAll(d=>d.locationId==declinedDestinationId);
                        destinations.RemoveAll(d=>d.locationId==declinedDestinationId); destinations.AddRange(previous);
                    }
                    RecordOfferStage("destination discovery", timer);
                    yield return true;
                    int destinationLimit = Math.Min(destinations.Count, Mathf.Clamp(config.offerDestinationCandidateLimit, 1, 24));
                    for (int i = 0; i < destinationLimit; i++)
                    {
                        timer.Restart(); LastOfferRouteQueries++;
                        var trip = routeDistances.BetweenStops(pickup.stop, destinations[i]);
                        RecordOfferStage("destination routes", timer);
                        yield return true;
                        if (requireNavigableRoute && !trip.Navigable || !float.IsFinite(trip.Meters) || trip.Meters < config.minimumTripDistance || trip.Meters > maximum)
                        { LastOfferRejectedDestinationRoutes++; continue; }
                        timer.Restart();
                        CompleteOffer(passenger, pickup.stop, destinations[i], origin, pickup.leg, trip);
                        RecordOfferStage("fare/UI notification/map", timer);
                        yield break;
                    }
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
            passenger.DispatchArchetype=="racing";
    }
}
