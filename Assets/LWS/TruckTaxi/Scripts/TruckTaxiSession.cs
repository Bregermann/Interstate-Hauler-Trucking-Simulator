using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiState { Inactive, StartingShift, Available, RideOffered, DrivingToPickup, PassengerBoarding,
        DrivingToDestination, PassengerExiting, RideComplete, RideFailed }

    public sealed class TaxiRequestProgress
    {
        public PassengerRequestDefinition Definition { get; }
        public TaxiRequestState State { get; internal set; }
        public float Progress { get; internal set; }
        public float Elapsed { get; internal set; }
        public float Target { get; }
        internal float ResolvedAt;
        internal readonly HashSet<string> Targets = new HashSet<string>();
        public TaxiRequestProgress(PassengerRequestDefinition definition, float difficulty)
        {
            Definition = definition;
            float value=definition.target*difficulty;
            bool continuous=definition.requestType==TaxiRequestType.Offroad || definition.requestType==TaxiRequestType.MaximumChaos;
            Target=continuous ? Mathf.Max(.1f,value) : Mathf.Max(1,Mathf.RoundToInt(value));
        }
        public float Remaining => Mathf.Max(0, Definition.timer - Elapsed);
    }

    public sealed class TaxiFare
    {
        public long Base, Distance, Time, Requests, Chaos, Tip, Penalties;
        public long Total => Math.Max(0, Base + Distance + Time + Requests + Chaos + Tip - Penalties);
        public float Rating;
        public int ChaosScore;
        public int Score;
    }

    // One local ride session. No freight ActiveJob, global gameplay state, files or save slots.
    public sealed class TruckTaxiSession
    {
        public TruckTaxiState State { get; private set; } = TruckTaxiState.Inactive;
        public PassengerProfile Passenger { get; private set; }
        public TruckTaxiRideLocation Pickup { get; private set; }
        public TruckTaxiRideLocation Destination { get; private set; }
        public IReadOnlyList<TaxiRequestProgress> Requests => requests;
        public TaxiFare LastFare { get; private set; }
        public float ElapsedRide { get; private set; }
        public float DistanceDriven { get; private set; }
        public float Satisfaction { get; private set; } = 4;
        public int ChaosScore => Mathf.FloorToInt(chaos);
        public int TrackedCollisions { get; private set; }
        public long ShiftEarnings { get; private set; }
        public int CompletedRides { get; private set; }
        public int ShiftScore { get; private set; }
        public float StateAge { get; private set; }
        public string Reaction { get; private set; }
        public float ReactionAge { get; private set; }
        public float OfferRemaining => Mathf.Max(0, config.offerDuration - StateAge);
        public bool HasPassenger => State == TruckTaxiState.DrivingToDestination || State == TruckTaxiState.PassengerExiting;
        public event Action Changed;
        public event Action<TaxiRequestProgress> RequestResolved;
        public event Action<TaxiRequestProgress> RequestCreated;
        public event Action<TaxiEventType> DrivingEvent;
        private readonly TruckTaxiConfiguration config;
        private readonly System.Random random;
        private readonly List<TruckTaxiRideLocation> locations;
        private readonly List<TaxiRequestProgress> requests = new List<TaxiRequestProgress>();
        private float chaos, requestClock, nextSpeedReaction;
        private Vector3 lastPosition;
        private bool hasPosition;

        public TruckTaxiSession(TruckTaxiConfiguration config, IEnumerable<TruckTaxiRideLocation> locations, int seed)
        {
            this.config = config;
            this.locations = new List<TruckTaxiRideLocation>(locations);
            random = new System.Random(seed);
        }
        private void SetState(TruckTaxiState value)
        {
            State = value; StateAge = 0; Changed?.Invoke();
        }
        public void StartShift()
        {
            if (State != TruckTaxiState.Inactive) return;
            SetState(TruckTaxiState.StartingShift);
            SetState(TruckTaxiState.Available);
        }
        public void EndShift()
        {
            Passenger = null; Pickup = Destination = null; requests.Clear(); hasPosition = false;
            SetState(TruckTaxiState.Inactive);
        }
        public bool OfferRide()
        {
            if (State != TruckTaxiState.Available || config.passengers == null || config.passengers.Length == 0) return false;
            var pairs = new List<(TruckTaxiRideLocation, TruckTaxiRideLocation)>();
            foreach (var from in locations)
            foreach (var to in locations)
            {
                if (from == null || to == null || from == to || from.locationId == to.locationId || !from.pickupAllowed || !to.dropoffAllowed) continue;
                float distance = Vector3.Distance(from.StopPosition, to.StopPosition);
                if (distance >= config.minimumTripDistance && distance <= config.maximumTripDistance) pairs.Add((from,to));
            }
            if (pairs.Count == 0) return false;
            var pair = pairs[random.Next(pairs.Count)];
            Pickup = pair.Item1; Destination = pair.Item2;
            Passenger = config.passengers[random.Next(config.passengers.Length)];
            if (Passenger == null) return false;
            requests.Clear(); chaos = 0; TrackedCollisions = 0; ElapsedRide = DistanceDriven = 0;
            Satisfaction = 4; requestClock = 0; nextSpeedReaction = 0; hasPosition = false; LastFare = null; Reaction = "";
            SetState(TruckTaxiState.RideOffered);
            return true;
        }
        public bool AcceptRide()
        {
            if (State != TruckTaxiState.RideOffered) return false;
            if (Passenger.dialogueSet != null && Passenger.dialogueSet.Length > 0)
                React(Passenger.dialogueSet[random.Next(Passenger.dialogueSet.Length)]);
            SetState(TruckTaxiState.DrivingToPickup); return true;
        }
        public void DeclineRide()
        {
            if (State != TruckTaxiState.RideOffered) return;
            Passenger = null; Pickup = Destination = null; SetState(TruckTaxiState.Available);
        }
        public void ContinueShift()
        {
            if (State != TruckTaxiState.RideComplete && State != TruckTaxiState.RideFailed) return;
            Passenger = null; Pickup = Destination = null; requests.Clear(); SetState(TruckTaxiState.Available);
        }
        public void Tick(float deltaTime, Vector3 playerPosition, float speed, float acceleration, bool onRoad)
        {
            if (deltaTime <= 0 || State == TruckTaxiState.Inactive) return;
            StateAge += deltaTime; ReactionAge += deltaTime;
            if (State == TruckTaxiState.Available && StateAge >= config.rideFrequency) OfferRide();
            else if (State == TruckTaxiState.RideOffered && StateAge >= config.offerDuration) DeclineRide();
            else if (State == TruckTaxiState.DrivingToPickup && Pickup.Contains(playerPosition) && speed <= config.stoppedSpeed)
                SetState(TruckTaxiState.PassengerBoarding);
            else if (State == TruckTaxiState.PassengerBoarding)
            {
                if (!Pickup.Contains(playerPosition) || speed > config.stoppedSpeed) SetState(TruckTaxiState.DrivingToPickup);
                else if (StateAge >= config.boardingSeconds)
                {
                    hasPosition = false; SetState(TruckTaxiState.DrivingToDestination); GenerateRequest();
                }
            }
            else if (State == TruckTaxiState.DrivingToDestination)
            {
                ElapsedRide += deltaTime;
                if (speed > 2 && Mathf.Abs(acceleration) <= config.gentleAcceleration)
                    Satisfaction = Mathf.Clamp(Satisfaction + Passenger.smoothAffinity * config.smoothRatingPerSecond * deltaTime,1,5);
                if (speed >= config.fastDrivingSpeed && ElapsedRide >= nextSpeedReaction && ReactionAge > 5)
                {
                    React(Passenger.speedReaction);
                    nextSpeedReaction = ElapsedRide + config.speedReactionCooldown;
                }
                if (hasPosition) DistanceDriven += Mathf.Min(Vector3.Distance(playerPosition,lastPosition), speed * deltaTime + 2);
                lastPosition = playerPosition; hasPosition = true;
                if (!onRoad && speed > 1) chaos += deltaTime * config.offroadScorePerSecond;
                foreach (var request in requests)
                {
                    if (request.State != TaxiRequestState.Active) continue;
                    request.Elapsed += deltaTime;
                    if (request.Definition.requestType == TaxiRequestType.Offroad && !onRoad && speed > 1) request.Progress += deltaTime;
                    if (request.Definition.requestType == TaxiRequestType.MaximumChaos) request.Progress = ChaosScore;
                    if (request.Definition.requestType == TaxiRequestType.SmoothRide && Mathf.Abs(acceleration) > request.Definition.maximumAcceleration)
                        Resolve(request, false);
                    if (request.State != TaxiRequestState.Active) continue;
                    if (!IsArrivalRequest(request.Definition.requestType) && request.Progress >= request.Target) Resolve(request,true);
                    else if (request.Elapsed > request.Definition.timer) Resolve(request,false);
                }
                requestClock += deltaTime;
                if (requestClock >= Passenger.requestFrequency) { GenerateRequest(); requestClock = 0; }
                if (ElapsedRide >= Passenger.basePatience) FailRide("Passenger patience exhausted.");
                else if (Destination.Contains(playerPosition) && speed <= config.stoppedSpeed)
                    SetState(TruckTaxiState.PassengerExiting);
            }
            else if (State == TruckTaxiState.PassengerExiting)
            {
                if (!Destination.Contains(playerPosition) || speed > config.stoppedSpeed) SetState(TruckTaxiState.DrivingToDestination);
                else if (StateAge >= config.exitingSeconds) FinishRide();
            }
        }
        private static bool IsArrivalRequest(TaxiRequestType type) =>
            type == TaxiRequestType.FastDelivery || type == TaxiRequestType.NoCollisions || type == TaxiRequestType.SmoothRide;

        public bool GenerateRequest()
        {
            if (State != TruckTaxiState.DrivingToDestination || requests.Count >= 8) return false;
            int active = 0; foreach (var r in requests) if (r.State == TaxiRequestState.Active) active++;
            if (active >= 3) return false;
            var source = Passenger.possibleRequests;
            if (source == null || source.Length == 0) return false;
            var eligible = new List<PassengerRequestDefinition>();
            foreach (var definition in source)
            {
                if (definition == null) continue;
                bool allowed = true;
                foreach (var old in requests)
                    if (old.Definition == definition && (old.State == TaxiRequestState.Active || !definition.allowMultipleInstances ||
                        ElapsedRide - old.ResolvedAt < definition.cooldown)) allowed = false;
                if (allowed) eligible.Add(definition);
            }
            if (eligible.Count == 0) return false;
            var chosen = eligible[random.Next(eligible.Count)];
            float difficulty = Mathf.Lerp(Passenger.requestDifficultyRange.x, Passenger.requestDifficultyRange.y, (float)random.NextDouble());
            var progress = new TaxiRequestProgress(chosen, difficulty);
            requests.Add(progress);
            RequestCreated?.Invoke(progress);
            React(string.IsNullOrEmpty(chosen.dialogue) ? chosen.description : chosen.dialogue);
            Changed?.Invoke(); return true;
        }
        public void RecordEvent(TaxiEventType type, string targetId, float impactSpeed = 0, int scoreOverride = -1, string passengerDialogue = null)
        {
            if (State != TruckTaxiState.DrivingToDestination) return;
            bool impact = type == TaxiEventType.Collision || type == TaxiEventType.TrafficRam ||
                type == TaxiEventType.PedestrianHit || type == TaxiEventType.PropDamage;
            if (impact && impactSpeed < config.minimumImpactSpeed) return;
            chaos += scoreOverride >= 0 ? scoreOverride : config.Score(type);
            if (impact)
            {
                TrackedCollisions++;
                Satisfaction = Mathf.Clamp(Satisfaction + Passenger.chaosAffinity * 0.18f, 1, 5);
                React(Passenger.collisionReaction);
            }
            else if (type == TaxiEventType.Shortcut || type == TaxiEventType.ScenicPoint)
                React(string.IsNullOrWhiteSpace(passengerDialogue) ? Passenger.shortcutReaction : passengerDialogue);
            foreach (var r in requests)
            {
                if (r.State != TaxiRequestState.Active) continue;
                var definition = r.Definition;
                if (impact && (definition.requestType == TaxiRequestType.NoCollisions || definition.requestType == TaxiRequestType.SmoothRide))
                { Resolve(r,false); continue; }
                bool matches =
                    (type == TaxiEventType.Shortcut && definition.requestType == TaxiRequestType.Shortcut) ||
                    (type == TaxiEventType.ScenicPoint && definition.requestType == TaxiRequestType.ScenicRoute) ||
                    (type == TaxiEventType.TrafficRam && definition.requestType == TaxiRequestType.RamTraffic) ||
                    (type == TaxiEventType.PedestrianHit && definition.requestType == TaxiRequestType.HitPedestrian) ||
                    (type == TaxiEventType.PropDamage && definition.requestType == TaxiRequestType.PropertyDamage) ||
                    (type == TaxiEventType.NearMiss && definition.requestType == TaxiRequestType.NearMiss);
                if (matches && (string.IsNullOrEmpty(definition.targetId) || definition.targetId == targetId) && r.Targets.Add(targetId))
                    r.Progress++;
                if (definition.requestType == TaxiRequestType.MaximumChaos) r.Progress = ChaosScore;
                if (r.Progress >= r.Target) Resolve(r,true);
            }
            DrivingEvent?.Invoke(type); Changed?.Invoke();
        }
        private void React(string value) { Reaction = value; ReactionAge = 0; }
        private void Resolve(TaxiRequestProgress request, bool success)
        {
            if (request.State != TaxiRequestState.Active) return;
            request.State = success ? TaxiRequestState.Succeeded : TaxiRequestState.Failed;
            request.ResolvedAt = ElapsedRide;
            React((success ? "REQUEST COMPLETE: " : "REQUEST FAILED: ") + request.Definition.description);
            Satisfaction = Mathf.Clamp(Satisfaction + (success ? request.Definition.ratingModifier : -0.2f),1,5);
            RequestResolved?.Invoke(request);
        }
        public TaxiFare EstimateFare()
        {
            var fare = new TaxiFare
            {
                Base = config.baseFareCents,
                Distance = (long)Math.Round(DistanceDriven * config.centsPerMeter),
                Time = (long)Math.Round(ElapsedRide * config.centsPerSecond),
                Rating = Satisfaction, ChaosScore = ChaosScore, Score = ChaosScore
            };
            foreach (var r in requests) if (r.State == TaxiRequestState.Succeeded)
            { fare.Requests += r.Definition.bonusMoneyCents; fare.Score += r.Definition.bonusScore; }
            if (Passenger != null && Passenger.chaosAffinity > 0)
                fare.Chaos = (long)Math.Round(ChaosScore * config.chaosCentsPerPoint * Passenger.chaosAffinity);
            if (Passenger != null && Passenger.chaosAffinity < 0) fare.Penalties = config.impactPenaltyCents * TrackedCollisions;
            return fare;
        }
        private void FinishRide()
        {
            if (State != TruckTaxiState.PassengerExiting) return;
            foreach (var r in requests)
                if (r.State == TaxiRequestState.Active) Resolve(r,IsArrivalRequest(r.Definition.requestType) && r.Elapsed <= r.Definition.timer);
            LastFare = EstimateFare();
            if (random.NextDouble() <= Passenger.baseTipChance * Mathf.Clamp01((Satisfaction-1)/4))
                LastFare.Tip = (long)Math.Round(LastFare.Total * config.tipFraction);
            ShiftEarnings += LastFare.Total; ShiftScore += LastFare.Score; CompletedRides++;
            SetState(TruckTaxiState.RideComplete);
        }
        public void FailRide(string reason)
        {
            if (State != TruckTaxiState.DrivingToPickup && State != TruckTaxiState.PassengerBoarding && !HasPassenger) return;
            foreach (var r in requests) Resolve(r,false);
            React(reason); SetState(TruckTaxiState.RideFailed);
        }
        // Explicit debug seams exercise the same guarded transitions, never pay twice.
        public void DebugBoard()
        {
            if (State == TruckTaxiState.DrivingToPickup) { SetState(TruckTaxiState.PassengerBoarding); }
        }
        public void DebugComplete()
        {
            if (State == TruckTaxiState.DrivingToDestination) { SetState(TruckTaxiState.PassengerExiting); FinishRide(); }
        }
        public void DebugResolveRequest(bool success)
        { foreach (var r in requests) if (r.State == TaxiRequestState.Active) { Resolve(r,success); break; } }
        public void DebugChaos() { if (HasPassenger) chaos += 100; }
        public void DiscardTeleportDistance() { hasPosition = false; }
    }
}
