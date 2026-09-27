using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiState { Inactive, StartingShift, Available, RideOffered, DrivingToPickup, PassengerBoarding,
        DrivingToDestination, PassengerExiting, RideComplete, RideFailed, PassengerEjected, AppreciationOffer, AppreciationSequence }

    public sealed class TaxiRequestProgress
    {
        public PassengerRequestDefinition Definition { get; }
        public TaxiRequestState State { get; internal set; }
        public float Progress { get; internal set; }
        public float Elapsed { get; internal set; }
        public bool Expired { get; internal set; }
        public string FailureReason { get; internal set; }
        public float Target { get; }
        public TruckTaxiStopObjectivePoint StopPoint { get; }
        internal float ResolvedAt;
        internal readonly HashSet<string> Targets = new HashSet<string>();
        public TaxiRequestProgress(PassengerRequestDefinition definition, float difficulty, TruckTaxiStopObjectivePoint stop=null,int maximumCount=int.MaxValue)
        {
            Definition = definition;
            float value=definition.target*difficulty;
            StopPoint=stop;
            Target=stop!=null ? Mathf.Max(1,Mathf.RoundToInt(stop.durationSeconds)) : definition.requestType==TaxiRequestType.MaximumChaos
                ? Mathf.Max(100,Mathf.RoundToInt(value/100)*100) : definition.requestType==TaxiRequestType.FastDelivery
                ? Mathf.Max(1,Mathf.RoundToInt(definition.timer)) : Mathf.Max(1,Mathf.Min(maximumCount,Mathf.RoundToInt(value)));
        }
        public float Remaining => Mathf.Max(0,(Definition.requestType==TaxiRequestType.FastDelivery ? Target : Definition.timer)-Elapsed);
        public bool IsArrivalGoal => Definition.requestType==TaxiRequestType.FastDelivery ||
            Definition.requestType==TaxiRequestType.NoCollisions || Definition.requestType==TaxiRequestType.SmoothRide;
        public float NormalizedProgress => State==TaxiRequestState.Succeeded ? 1 : State==TaxiRequestState.Failed ? 0 :
            Definition.requestType==TaxiRequestType.NoCollisions || Definition.requestType==TaxiRequestType.SmoothRide ? 1 :
            Definition.requestType==TaxiRequestType.FastDelivery ? Mathf.Clamp01(1-Elapsed/Mathf.Max(1,Target)) :
            Mathf.Clamp01(Progress/Mathf.Max(1,Target));
        public string TargetText => Target.ToString("0",System.Globalization.CultureInfo.InvariantCulture);
        public string ProgressText => Definition.requestType==TaxiRequestType.FastDelivery ? $"{Mathf.FloorToInt(Elapsed)}/{TargetText}s" :
            Definition.requestType==TaxiRequestType.NoCollisions || Definition.requestType==TaxiRequestType.SmoothRide ? "UNTIL ARRIVAL" :
            $"{Mathf.FloorToInt(Progress)}/{TargetText}"+(Definition.IsStop || Definition.requestType==TaxiRequestType.Offroad ? "s" : "");
        public string Description
        {
            get
            {
                switch(Definition.requestType)
                {
                    case TaxiRequestType.FastDelivery: return $"Arrive within {TargetText} seconds";
                    case TaxiRequestType.Shortcut: return Target<=1 ? "Use a shortcut" : $"Use {TargetText} different shortcuts";
                    case TaxiRequestType.RamTraffic: return $"Ram {TargetText} traffic "+(Target<=1 ? "car" : "cars");
                    case TaxiRequestType.HitPedestrian: return Target<=1 ? "Hit a pedestrian" : $"Hit {TargetText} pedestrians";
                    case TaxiRequestType.PropertyDamage: return $"Damage {TargetText} roadside "+(Target<=1 ? "prop" : "props");
                    case TaxiRequestType.Offroad: return $"Drive offroad for {TargetText} seconds";
                    case TaxiRequestType.MaximumChaos: return $"Make {TargetText} Chaos points";
                    case TaxiRequestType.NearMiss: return $"Make {TargetText} close traffic "+(Target<=1 ? "pass" : "passes")+" without contact";
                    case TaxiRequestType.NoCollisions: return "Arrive without a qualifying collision";
                    case TaxiRequestType.SmoothRide: return "Arrive smoothly without impacts or harsh acceleration";
                    case TaxiRequestType.ScenicRoute: return $"Scenic stop: enjoy the view for {TargetText}s";
                    case TaxiRequestType.IllicitStop: return $"Sketchy pickup: wait {TargetText}s";
                    case TaxiRequestType.FollowVehicle: return $"Follow the marked vehicle for {TargetText}s";
                    case TaxiRequestType.RamTargetVehicle: return "Ram the marked vehicle";
                    case TaxiRequestType.LoseVehicle: return "Lose the pursuing vehicle";
                    case TaxiRequestType.BlockVehicle: return "Block the marked vehicle";
                    case TaxiRequestType.ReachLocationBeforeVehicle: return "Reach the finish before the marked vehicle";
                    case TaxiRequestType.DestroyVehicle: return "Disable the marked vehicle";
                    case TaxiRequestType.CollectDroppedObjects: return $"Collect {TargetText} dropped objects";
                    default: return Definition.description ?? "Passenger request";
                }
            }
        }
    }

    public sealed class TaxiFare
    {
        public long Base, Distance, Time, Requests, Chaos, Tip, Penalties;
        public long Total => Math.Max(0, Base + Distance + Time + Requests + Chaos + Tip - Penalties);
        public int Rating;
        public int ChaosScore;
        public int Score;
    }

    // One local ride session. No freight ActiveJob, global gameplay state, files or save slots.
    public sealed class TruckTaxiSession
    {
        public const string RideRequestsPreferenceKey = "TruckTaxi.RideRequestsEnabled.v1";
        public TruckTaxiState State { get; private set; } = TruckTaxiState.Inactive;
        public bool RideRequestsEnabled { get; private set; }
        private readonly HashSet<object> offerSuppressors = new HashSet<object>();
        public bool OffersSuppressed => offerSuppressors.Count > 0;
        public void AcquireOfferSuppression(object owner) { if(owner!=null) offerSuppressors.Add(owner); }
        public void ReleaseOfferSuppression(object owner) { if(owner!=null) offerSuppressors.Remove(owner); }
        public PassengerProfile Passenger { get; private set; }
        public TruckTaxiRideLocation Pickup { get; private set; }
        public TruckTaxiRideLocation Destination { get; private set; }
        public TruckTaxiRideOffer Offer { get; private set; }
        public IReadOnlyList<TaxiRequestProgress> Requests => requests;
        public TaxiFare LastFare { get; private set; }
        public float ElapsedRide { get; private set; }
        public float DistanceDriven { get; private set; }
        public float Satisfaction { get; private set; } = 4;
        public int ChaosScore => Mathf.FloorToInt(chaos);
        public int TrackedCollisions { get; private set; }
        public int TotalCollisions { get; private set; }
        public int TrafficHits { get; private set; }
        private int rideTrafficHits;
        public long ShiftEarnings { get; private set; }
        public long WalletBalanceCents => ShiftEarnings - purchaseCharges - serviceCharges;
        public long ServiceDebtCents => WalletBalanceCents<0 ? -WalletBalanceCents : 0;
        public long PurchaseChargesCents => purchaseCharges;
        public long ServiceChargesCents => serviceCharges;
        public int CompletedRides { get; private set; }
        public int FailedRides { get; private set; }
        public int EjectedRides { get; private set; }
        public int PickupCancellations { get; private set; }
        public int JugsSucceeded { get; private set; }
        public int JugsSpilled { get; private set; }
        public int ContainersThrown { get; private set; }
        public int SpecialStopsCompleted { get; private set; }
        public IReadOnlyList<TruckTaxiRideRecord> RideHistory => rideHistory.AsReadOnly();
        public IReadOnlyList<string> RecentPassengerIds => recentOfferIds.AsReadOnly();
        public bool IsRepeatPassenger { get; private set; }
        public double GameMinutes => gameMinutes;
        public TruckTaxiDemandWeather DemandWeather => weather;
        public int TotalStarsEarned { get; private set; }
        public double DriverAverageRating => CompletedRides==0 ? 0 : (double)TotalStarsEarned/CompletedRides;
        public string DriverAverageText => CompletedRides==0 ? "--" : Math.Round(DriverAverageRating,1,MidpointRounding.AwayFromZero).ToString("0.0");
        public static int StarsForSatisfaction(float value) => value>=4.5f ? 5 : value>=3.5f ? 4 : value>=2.5f ? 3 : value>=1.5f ? 2 : 1;
        public bool SpecialAppreciationAccepted { get; private set; }
        public TruckTaxiObjectiveCapabilities Capabilities { get; } = new TruckTaxiObjectiveCapabilities();
        public Action RefreshObjectiveSupport { get; set; }
        public Func<PassengerRequestDefinition,bool> CanResolveSpecificTarget { get; set; }
        public TaxiRequestProgress ActiveStop
        {
            get { foreach(var r in requests) if(r.State==TaxiRequestState.Active && r.Definition.IsStop) return r; return null; }
        }
        public event Action<TaxiRequestProgress> StopStarted;
        private bool stopStarted;
        private float maximumRideAcceleration;
        public int ShiftScore { get; private set; }
        public float StateAge { get; private set; }
        public string Reaction { get; private set; }
        public float ReactionAge { get; private set; }
        public float OfferDuration { get; private set; }
        public bool OfferTimerPaused { get; set; }
        public float OfferRemaining => State==TruckTaxiState.RideOffered ? Mathf.Max(0, OfferDuration-StateAge) : 0;
        public float OfferRemainingNormalized => OfferDuration>0 ? Mathf.Clamp01(OfferRemaining/OfferDuration) : 0;
        public float PickupDuration { get; private set; }
        public float PickupElapsed { get; private set; }
        public float PickupRemaining => State==TruckTaxiState.DrivingToPickup || State==TruckTaxiState.PassengerBoarding
            ? Mathf.Max(0,PickupDuration-PickupElapsed) : 0;
        public float DemandFareMultiplier { get; private set; } = 1;
        public bool IsIntercityRide { get; private set; }
        public long DemandEstimatedFareCents => Offer==null ? 0 : (long)Math.Round(Offer.EstimatedFareCents*DemandFareMultiplier);
        public bool CanRespondToOffer => State==TruckTaxiState.RideOffered && Offer!=null && OfferRemaining>0;
        public bool HasPassenger => State == TruckTaxiState.DrivingToDestination || State == TruckTaxiState.PassengerExiting ||
            State==TruckTaxiState.AppreciationOffer || State==TruckTaxiState.AppreciationSequence;
        public event Action Changed;
        public event Action<TaxiRequestProgress> RequestResolved;
        public event Action<TaxiRequestProgress> RequestCreated;
        public event Action<TaxiEventType> DrivingEvent;
        public event Action<TruckTaxiPedestrianImpact> PedestrianImpact;
        public TruckTaxiPedestrianImpact? LastPedestrianImpact { get; private set; }
        public int PedestriansHit { get; private set; }
        public string CurrentRideId { get; private set; }
        private readonly HashSet<string> hitPedestrians=new HashSet<string>();
        public event Action PassengerEjected;
        public event Action DestinationChanged;
        public float BoardingProgress => State == TruckTaxiState.PassengerBoarding ? Mathf.Clamp01(StateAge / Mathf.Max(.1f,config.boardingSeconds)) : 0;
        public float BoardingMaximumSpeed => config.stoppedSpeed;
        public Func<bool> PassengerReadyToBoard { get; set; }
        public long MechanicFareAdjustment { get; private set; }
        private readonly TruckTaxiConfiguration config;
        private readonly System.Random random;
        private readonly List<TruckTaxiRideLocation> locations;
        private readonly List<TaxiRequestProgress> requests = new List<TaxiRequestProgress>();
        private readonly List<TruckTaxiRideRecord> rideHistory = new List<TruckTaxiRideRecord>();
        private readonly List<string> recentOfferIds = new List<string>();
        private readonly HashSet<string> acceptedPassengerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PassengerContinuity> continuity = new Dictionary<string, PassengerContinuity>(StringComparer.OrdinalIgnoreCase);
        private long purchaseCharges, serviceCharges;
        private double gameMinutes;
        private bool externalClock;
        private float hourOfDay = 12;
        private TruckTaxiDemandWeather weather;
        private int ridePedestriansHit, rideSpecialStopsCompleted, rideJugsSucceeded, rideJugsSpilled, rideContainersThrown;
        private float chaos, requestClock, nextSpeedReaction;
        private Vector3 lastPosition;
        private bool hasPosition;
        private readonly TruckTaxiRouteDistanceService routeDistances;
        private readonly Func<Vector3> playerPosition;
        public Func<Vector3,string> RegionResolver { get; set; }
        private readonly bool requireNavigableRoute;

        private sealed class PassengerContinuity
        {
            public string LastRideLocationId;
            public Vector3 LastDropoffPosition;
            public string LastKnownDistrict;
            public double LastDropoffMinutes;
        }

        public bool TryGetPassengerContinuity(string passengerId, out string lastRideLocationId,
            out double lastDropoffMinutes, out string lastKnownDistrict)
        {
            if(!string.IsNullOrWhiteSpace(passengerId) && continuity.TryGetValue(passengerId,out var state))
            {
                lastRideLocationId=state.LastRideLocationId; lastDropoffMinutes=state.LastDropoffMinutes;
                lastKnownDistrict=state.LastKnownDistrict; return true;
            }
            lastRideLocationId=lastKnownDistrict=null; lastDropoffMinutes=0; return false;
        }

        public void SetWorldConditions(double elapsedGameMinutes, float localHour, TruckTaxiDemandWeather currentWeather)
        {
            if(double.IsNaN(elapsedGameMinutes) || double.IsInfinity(elapsedGameMinutes) || elapsedGameMinutes<0 ||
                !float.IsFinite(localHour)) return;
            gameMinutes=Math.Max(gameMinutes,elapsedGameMinutes); hourOfDay=Mathf.Repeat(localHour,24); weather=currentWeather; externalClock=true;
        }

        public bool TrySpend(long cents)
        {
            if(cents<0) throw new ArgumentOutOfRangeException(nameof(cents));
            if(cents>WalletBalanceCents) return false;
            purchaseCharges=checked(purchaseCharges+cents); Changed?.Invoke(); return true;
        }

        // Mandatory services incur debt rather than granting a free rescue at zero balance.
        public void ChargeService(long cents)
        {
            if(cents<0) throw new ArgumentOutOfRangeException(nameof(cents));
            serviceCharges=checked(serviceCharges+cents); Changed?.Invoke();
        }

        public void RecordNeedsEvent(TruckTaxiDriverNeedEvent needEvent)
        {
            bool inRide=CurrentRideId!=null && (State==TruckTaxiState.DrivingToPickup || State==TruckTaxiState.PassengerBoarding || HasPassenger);
            switch(needEvent)
            {
                case TruckTaxiDriverNeedEvent.JugSucceeded: JugsSucceeded++; if(inRide) rideJugsSucceeded++; break;
                case TruckTaxiDriverNeedEvent.JugSpilled: JugsSpilled++; if(inRide) rideJugsSpilled++; break;
                case TruckTaxiDriverNeedEvent.JugThrownFromWindow:
                case TruckTaxiDriverNeedEvent.LitterThrown: ContainersThrown++; if(inRide) rideContainersThrown++; break;
            }
            Changed?.Invoke();
        }

        public TruckTaxiSession(TruckTaxiConfiguration config, IEnumerable<TruckTaxiRideLocation> locations, int seed,
            TruckTaxiRouteDistanceService routeDistances = null, Func<Vector3> playerPosition = null,
            Func<Vector3,string> regionAtPosition = null)
        {
            this.config = config;
            RideRequestsEnabled = PlayerPrefs.GetInt(RideRequestsPreferenceKey, 1) != 0;
            this.locations = new List<TruckTaxiRideLocation>(locations);
            random = new System.Random(seed);
            this.routeDistances = routeDistances ?? new TruckTaxiRouteDistanceService(null);
            requireNavigableRoute = routeDistances != null;
            this.playerPosition = playerPosition ?? (() => lastPosition);
            RegionResolver = regionAtPosition;
            Capabilities.Register(TruckTaxiObjectiveCapability.Timer,1);
            Capabilities.Register(TruckTaxiObjectiveCapability.Chaos,1);
        }
        private void SetState(TruckTaxiState value)
        {
            State = value; StateAge = 0; Changed?.Invoke();
        }
        public void SetRideRequestsEnabled(bool enabled)
        {
            if (RideRequestsEnabled == enabled) return;
            RideRequestsEnabled = enabled;
            PlayerPrefs.SetInt(RideRequestsPreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        public void StartShift()
        {
            if (State != TruckTaxiState.Inactive) return;
            SetState(TruckTaxiState.StartingShift);
            SetState(TruckTaxiState.Available);
        }
        public void EndShift()
        {
            if(State==TruckTaxiState.DrivingToPickup || State==TruckTaxiState.PassengerBoarding || HasPassenger)
                FailRide("Shift ended.");
            Passenger = null; Pickup = Destination = null; Offer = null; IsIntercityRide=false; requests.Clear(); hasPosition = false; CurrentRideId=null;
            SetState(TruckTaxiState.Inactive);
        }
        public bool OfferRide(PassengerProfile forcedPassenger = null)
        {
            if (State != TruckTaxiState.Available || !RideRequestsEnabled || OffersSuppressed) return false;
            Vector3 origin = playerPosition();
            var pickups = new Dictionary<TruckTaxiRideLocation,TruckTaxiRouteLeg>();
            foreach (var location in locations)
            {
                if (location == null || !location.isActiveAndEnabled || !location.pickupAllowed) continue;
                var leg = routeDistances.Measure(origin,location.StopPosition);
                if (requireNavigableRoute && !leg.Navigable) continue;
                float eta = leg.Meters / Mathf.Max(1,config.pickupReasonableSpeedMetersPerSecond);
                if (!float.IsFinite(eta) || eta > Mathf.Max(1,config.pickupHardMaximumSeconds)) continue;
                pickups.Add(location,leg);
            }
            if (pickups.Count == 0) return false;
            var pairs = new List<(TruckTaxiRideLocation, TruckTaxiRideLocation)>();
            foreach (var from in pickups.Keys)
            foreach (var to in locations)
            {
                if (to == null || !to.dropoffAllowed || from == to || from.locationId == to.locationId) continue;
                var trip = routeDistances.BetweenStops(from,to);
                if (requireNavigableRoute && !trip.Navigable) continue;
                float maximum = IsAcrossRegions(from,to) ? config.intercityMaximumTripDistance : config.maximumTripDistance;
                if (trip.Meters >= config.minimumTripDistance && trip.Meters <= maximum) pairs.Add((from,to));
            }
            if (pairs.Count == 0) return false;
            var candidates = config.passengerDatabase != null
                ? new List<PassengerProfile>(config.passengerDatabase.Query(new TruckTaxiPassengerQuery { availableOnly=true }))
                : new List<PassengerProfile>(config.passengers ?? Array.Empty<PassengerProfile>());
            var weighted = new List<(PassengerProfile profile, float weight)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenReferences = new HashSet<PassengerProfile>();
            foreach(var candidate in candidates)
            {
                if(candidate==null || !candidate.available || candidate.spawnWeight<=0 ||
                    !seenReferences.Add(candidate) ||
                    (!string.IsNullOrWhiteSpace(candidate.passengerId) && !seen.Add(candidate.passengerId)) ||
                    !HasEligiblePair(candidate,pairs)) continue;
                float weight=CandidateWeight(candidate);
                if(weight>0 && float.IsFinite(weight)) weighted.Add((candidate,weight));
            }
            if(forcedPassenger!=null)
            {
                if(!HasEligiblePair(forcedPassenger,pairs)) return false;
                Passenger=forcedPassenger;
            }
            else if(weighted.Count==0) return false;
            else
            {
                double total=0; foreach(var item in weighted) total+=item.weight;
                double roll=random.NextDouble()*total;
                Passenger=weighted[weighted.Count-1].profile;
                foreach(var item in weighted) { roll-=item.weight; if(roll<0) { Passenger=item.profile; break; } }
            }
            if (Passenger == null) return false;
            var pair = SelectPair(Passenger,pairs,pickups);
            Pickup = pair.Item1; Destination = pair.Item2;
            IsIntercityRide = IsAcrossRegions(Pickup,Destination);
            IsRepeatPassenger=!string.IsNullOrWhiteSpace(Passenger.passengerId) && acceptedPassengerIds.Contains(Passenger.passengerId);
            if(!string.IsNullOrWhiteSpace(Passenger.passengerId))
            {
                recentOfferIds.Add(Passenger.passengerId);
                if(recentOfferIds.Count>Mathf.Max(1,config.recentOfferCount)) recentOfferIds.RemoveAt(0);
            }
            Offer = new TruckTaxiRideOffer(Passenger, Pickup, Destination, origin,
                pickups[Pickup], routeDistances.BetweenStops(Pickup, Destination), config);
            OfferDuration=Mathf.Max(.1f,config.offerDuration);
            OfferTimerPaused=false;
            requests.Clear(); chaos = 0; TrackedCollisions = 0; ElapsedRide = DistanceDriven = 0;
            Satisfaction = Passenger.baseSatisfaction; MechanicFareAdjustment=0;
            DemandFareMultiplier=FareMultiplier()*(IsIntercityRide ? Mathf.Max(1,config.intercityFareMultiplier) : 1);
            PickupElapsed=PickupDuration=0;
            ridePedestriansHit=rideSpecialStopsCompleted=rideJugsSucceeded=rideJugsSpilled=rideContainersThrown=0;
            rideTrafficHits=0;
            maximumRideAcceleration=0; SpecialAppreciationAccepted=false; stopStarted=false;
            requestClock = 0; nextSpeedReaction = 0; hasPosition = false; LastFare = null; Reaction = "";
            SetState(TruckTaxiState.RideOffered);
            return true;
        }

        private bool HasEligiblePair(PassengerProfile profile, List<(TruckTaxiRideLocation,TruckTaxiRideLocation)> pairs)
        {
            foreach(var pair in pairs) if(CanStartAt(profile,pair.Item1)) return true;
            return false;
        }

        private bool CanStartAt(PassengerProfile profile, TruckTaxiRideLocation location)
        {
            continuity.TryGetValue(profile.passengerId ?? "",out var last);
            if(last!=null) return location==ResolveContinuityPickup(last);
            return profile.districts==null || profile.districts.Length==0 ||
                Array.Exists(profile.districts,d=>string.Equals(d,location.district,StringComparison.OrdinalIgnoreCase));
        }

        private TruckTaxiRideLocation ResolveContinuityPickup(PassengerContinuity last)
        {
            TruckTaxiRideLocation nearest=null;
            float best=Mathf.Max(0,config.repeatPickupAccessRadiusMeters);
            foreach(var location in locations)
            {
                if(location==null || !location.isActiveAndEnabled || !location.pickupAllowed) continue;
                if(location.locationId==last.LastRideLocationId) return location;
                float distance=Vector3.Distance(location.StopPosition,last.LastDropoffPosition);
                if(distance>best) continue;
                if(nearest!=null && Mathf.Approximately(distance,best) &&
                    string.CompareOrdinal(location.locationId,nearest.locationId)>=0) continue;
                best=distance; nearest=location;
            }
            return nearest;
        }

        private (TruckTaxiRideLocation,TruckTaxiRideLocation) SelectPair(PassengerProfile profile,
            List<(TruckTaxiRideLocation,TruckTaxiRideLocation)> pairs,
            Dictionary<TruckTaxiRideLocation,TruckTaxiRouteLeg> pickups)
        {
            continuity.TryGetValue(profile.passengerId ?? "",out var last);
            var previousPickup=last!=null ? ResolveContinuityPickup(last) : null;
            var valid=new List<(TruckTaxiRideLocation,TruckTaxiRideLocation)>();
            var same=new List<(TruckTaxiRideLocation,TruckTaxiRideLocation)>();
            var district=new List<(TruckTaxiRideLocation,TruckTaxiRideLocation)>();
            foreach(var pair in pairs)
            {
                if(!CanStartAt(profile,pair.Item1)) continue;
                valid.Add(pair);
                if(pair.Item1==previousPickup) same.Add(pair);
                else if(last!=null && string.Equals(pair.Item1.district,last.LastKnownDistrict,StringComparison.OrdinalIgnoreCase)) district.Add(pair);
            }
            if(last==null && requireNavigableRoute)
            {
                float min=Mathf.Max(0,config.pickupTargetMinimumSeconds);
                float max=Mathf.Min(Mathf.Max(min,config.pickupTargetMaximumSeconds),config.pickupHardMaximumSeconds);
                var target=valid.FindAll(pair => {
                    float eta=pickups[pair.Item1].Meters/Mathf.Max(1,config.pickupReasonableSpeedMetersPerSecond);
                    return eta>=min && eta<=max;
                });
                if(target.Count>0) valid=target;
            }
            var intercity=valid.FindAll(pair=>IsAcrossRegions(pair.Item1,pair.Item2));
            var local=valid.FindAll(pair=>!IsAcrossRegions(pair.Item1,pair.Item2));
            if(intercity.Count>0 && local.Count>0)
                valid=random.NextDouble()<Mathf.Clamp01(config.intercityRideChance) ? intercity : local;
            same.RemoveAll(pair=>!valid.Contains(pair));
            district.RemoveAll(pair=>!valid.Contains(pair));
            double age=last==null ? double.MaxValue : gameMinutes-last.LastDropoffMinutes;
            if(age<config.recentLocationMinutes && same.Count>0) return same[random.Next(same.Count)];
            if(age<config.citywideRelocationMinutes)
            {
                if(same.Count>0 && random.NextDouble()<Mathf.Clamp01(config.moderateSameLocationChance))
                    return same[random.Next(same.Count)];
                if(district.Count>0) return district[random.Next(district.Count)];
            }
            return valid[random.Next(valid.Count)];
        }

        private bool IsAcrossRegions(TruckTaxiRideLocation from, TruckTaxiRideLocation to)
        {
            if(RegionResolver==null) return false;
            string first=RegionResolver(from.StopPosition), second=RegionResolver(to.StopPosition);
            return !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second) &&
                !string.Equals(first,second,StringComparison.OrdinalIgnoreCase);
        }

        private float CandidateWeight(PassengerProfile profile)
        {
            float weight=profile.spawnWeight * Mathf.Max(0,config.RarityWeight(profile.rarity));
            if(recentOfferIds.Contains(profile.passengerId)) weight*=Mathf.Clamp(config.recentOfferWeight,0.01f,1);
            if(IsMorning && HasTag(profile,"commuter","business")) weight*=config.morningCommuterWeight;
            if(IsEvening && HasTag(profile,"social","scenic","glamorous")) weight*=config.eveningSocialWeight;
            if(IsEvening && profile.rarity>=TruckTaxiRarity.Rare) weight*=config.eveningRareWeight;
            if(IsLateNight || IsPreDawn)
            {
                bool late=IsLateNight;
                if(profile.rarity>=TruckTaxiRarity.Rare) weight*=late ? config.lateNightRareWeight : config.preDawnRareWeight;
                if(HasTag(profile,"weird","strange","eccentric","alien","robot")) weight*=late ? config.lateNightWeirdWeight : config.preDawnWeirdWeight;
                if(HasTag(profile,"nightlife","party","club","bartender","entertainer")) weight*=late ? config.lateNightNightlifeWeight : config.preDawnNightlifeWeight;
                if(profile.chaosAffinity>0 || HasTag(profile,"chaotic","chaos"))
                    weight*=late ? config.lateNightChaoticWeight : config.preDawnChaoticWeight;
                if(profile.flirtatiousPresentation || HasTag(profile,"flirtatious"))
                    weight*=late ? config.lateNightFlirtatiousWeight : config.preDawnFlirtatiousWeight;
            }
            return weight;
        }

        private static bool HasTag(PassengerProfile profile, params string[] tags)
        {
            foreach(var authored in profile.specialTraits ?? Array.Empty<string>())
                foreach(var tag in tags) if(string.Equals(authored,tag,StringComparison.OrdinalIgnoreCase)) return true;
            // Legacy profiles use freeform archetypes; explicit tags take precedence as content is authored.
            foreach(var tag in tags)
                if(!string.IsNullOrEmpty(profile.archetype) && profile.archetype.IndexOf(tag,StringComparison.OrdinalIgnoreCase)>=0) return true;
            return false;
        }

        private bool IsMorning => hourOfDay>=6 && hourOfDay<10;
        private bool IsEvening => hourOfDay>=18 && hourOfDay<22;
        private bool IsLateNight => hourOfDay>=22 || hourOfDay<3;
        private bool IsPreDawn => hourOfDay>=3 && hourOfDay<6;
        private float FrequencyMultiplier()
        {
            float result=IsMorning ? config.morningFrequencyMultiplier : IsEvening ? config.eveningFrequencyMultiplier :
                IsLateNight ? config.lateNightFrequencyMultiplier : IsPreDawn ? config.preDawnFrequencyMultiplier : 1;
            if(weather==TruckTaxiDemandWeather.Rain) result*=config.rainFrequencyMultiplier;
            else if(weather==TruckTaxiDemandWeather.Storm) result*=config.stormFrequencyMultiplier;
            else if(weather==TruckTaxiDemandWeather.Snow) result*=config.snowFrequencyMultiplier;
            else if(weather==TruckTaxiDemandWeather.HeavySnow) result*=config.heavySnowFrequencyMultiplier;
            else if(weather==TruckTaxiDemandWeather.Blizzard) result*=config.blizzardFrequencyMultiplier;
            return Mathf.Max(.1f,result);
        }

        private float FareMultiplier()
        {
            float result=IsMorning ? config.morningFareMultiplier : IsEvening ? config.eveningFareMultiplier :
                IsLateNight ? config.lateNightFareMultiplier : IsPreDawn ? config.preDawnFareMultiplier : 1;
            if(weather==TruckTaxiDemandWeather.Rain) result*=config.rainFareMultiplier;
            else if(weather==TruckTaxiDemandWeather.Storm) result*=config.stormFareMultiplier;
            else if(weather==TruckTaxiDemandWeather.Snow) result*=config.snowFareMultiplier;
            else if(weather==TruckTaxiDemandWeather.HeavySnow) result*=config.heavySnowFareMultiplier;
            else if(weather==TruckTaxiDemandWeather.Blizzard) result*=config.blizzardFareMultiplier;
            return Mathf.Max(0,result);
        }
        public bool AcceptRide()
        {
            if (!CanRespondToOffer) return false;
            if (Offer == null) return false;
            Passenger = Offer.Passenger; Pickup = Offer.Pickup; Destination = Offer.Destination;
            CurrentRideId=Guid.NewGuid().ToString("N");
            if(!string.IsNullOrWhiteSpace(Passenger.passengerId)) acceptedPassengerIds.Add(Passenger.passengerId);
            PickupElapsed=0;
            float routeMeters=Mathf.Max(0,routeDistances.Measure(playerPosition(),Pickup.StopPosition).Meters);
            float travel=routeMeters/Mathf.Max(1,config.pickupReasonableSpeedMetersPerSecond);
            float weatherAllowance=weather==TruckTaxiDemandWeather.Neutral ? 1 : Mathf.Max(1,config.badWeatherPickupTimeMultiplier);
            float personalityAllowance=HasTag(Passenger,"TimeObsessed") ? Mathf.Max(.1f,config.timeObsessedPickupMultiplier) : 1;
            PickupDuration=Mathf.Clamp(config.pickupBaseGraceSeconds+travel*config.pickupPatienceMultiplier*
                Passenger.EffectivePickupPatience*personalityAllowance*weatherAllowance,
                Mathf.Max(1,config.pickupMinimumSeconds),Mathf.Max(config.pickupMinimumSeconds,config.pickupMaximumSeconds));
            if (Passenger.dialogueSet != null && Passenger.dialogueSet.Length > 0)
                React(Passenger.dialogueSet[random.Next(Passenger.dialogueSet.Length)]);
            SetState(TruckTaxiState.DrivingToPickup); return true;
        }
        public void DeclineRide()
        {
            if (State != TruckTaxiState.RideOffered) return;
            Passenger = null; Pickup = Destination = null; Offer = null; IsIntercityRide=false; SetState(TruckTaxiState.Available);
        }
        public void ContinueShift()
        {
            if (State != TruckTaxiState.RideComplete && State != TruckTaxiState.RideFailed && State != TruckTaxiState.PassengerEjected) return;
            Passenger = null; Pickup = Destination = null; Offer = null; IsIntercityRide=false; CurrentRideId=null; requests.Clear(); SetState(TruckTaxiState.Available);
        }
        public void CancelTemporaryActionsForRecovery()
        {
            if(State==TruckTaxiState.RideOffered) DeclineRide();
            foreach(var request in requests)
                if(request.State==TaxiRequestState.Active && request.Definition.IsStop)
                    Resolve(request,false,"RECOVERY INTERRUPTED STOP");
            stopStarted=false;
            requestClock=0;
            hasPosition=false;
            OfferTimerPaused=false;
            Changed?.Invoke();
        }
        public void ApplyRoadsideAssistanceConsequence()
        {
            if(!HasPassenger || Passenger==null) return;
            if(Passenger.pickupPatience==TruckTaxiPickupPatience.VeryImpatient || HasTag(Passenger,"TimeObsessed"))
            { FailRide("Passenger cancelled after roadside assistance."); return; }
            if(Passenger.chaosAffinity>0 || HasTag(Passenger,"chaotic","chaos"))
            {
                chaos+=25;
                Satisfaction=Mathf.Clamp(Satisfaction+.1f,1,5);
                React("Passenger enjoyed the roadside detour.");
            }
            else if(Passenger.pickupPatience==TruckTaxiPickupPatience.Patient ||
                Passenger.pickupPatience==TruckTaxiPickupPatience.VeryPatient)
                React("Passenger will wait through roadside assistance.");
            else
            {
                Satisfaction=Mathf.Clamp(Satisfaction-(Passenger.pickupPatience==TruckTaxiPickupPatience.Impatient ? .75f : .5f),1,5);
                React("Passenger was upset by the roadside delay.");
            }
            Changed?.Invoke();
        }
        public void Tick(float deltaTime, Vector3 playerPosition, float speed, float acceleration, bool onRoad)
        {
            if (deltaTime <= 0 || State == TruckTaxiState.Inactive) return;
            if(State==TruckTaxiState.RideOffered && OfferTimerPaused) return;
            if(State!=TruckTaxiState.DrivingToDestination) lastPosition=playerPosition;
            if(!externalClock) gameMinutes+=deltaTime/60.0;
            StateAge += deltaTime; ReactionAge += deltaTime;
            if(State==TruckTaxiState.DrivingToPickup || State==TruckTaxiState.PassengerBoarding)
            {
                PickupElapsed+=deltaTime;
                if(PickupElapsed>=PickupDuration && !(Pickup.Contains(playerPosition) && speed<=config.stoppedSpeed))
                { FailRide("Passenger cancelled: pickup wait expired."); return; }
            }
            if (State == TruckTaxiState.PassengerEjected && StateAge >= 3) { ContinueShift(); return; }
            if (State == TruckTaxiState.Available && RideRequestsEnabled && !OffersSuppressed && StateAge >= config.rideFrequency*FrequencyMultiplier())
            {
                if (!OfferRide()) StateAge=0;
            }
            else if (State == TruckTaxiState.RideOffered && StateAge >= OfferDuration) DeclineRide();
            else if (State == TruckTaxiState.DrivingToPickup && Pickup.Contains(playerPosition) && speed <= config.stoppedSpeed)
                SetState(TruckTaxiState.PassengerBoarding);
            else if (State == TruckTaxiState.PassengerBoarding)
            {
                if (!Pickup.Contains(playerPosition) || speed > config.stoppedSpeed) SetState(TruckTaxiState.DrivingToPickup);
                else if(PickupElapsed>=PickupDuration) FailRide("Passenger cancelled: pickup wait expired.");
                else if (StateAge >= config.boardingSeconds && (PassengerReadyToBoard==null || PassengerReadyToBoard()))
                {
                    hasPosition = false; SetState(TruckTaxiState.DrivingToDestination); GenerateRequest();
                }
            }
            else if (State == TruckTaxiState.DrivingToDestination)
            {
                ElapsedRide += deltaTime;
                maximumRideAcceleration=Mathf.Max(maximumRideAcceleration,Mathf.Abs(acceleration));
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
                    if(request.Definition.IsStop)
                    {
                        bool valid=request.StopPoint!=null && request.StopPoint.IsValidStop(playerPosition,speed);
                        request.Progress=valid ? request.Progress+deltaTime : 0;
                        if(valid && !stopStarted) { stopStarted=true; StopStarted?.Invoke(request); }
                        if(!valid) stopStarted=false;
                    }
                    if (request.Definition.requestType == TaxiRequestType.Offroad && !onRoad && speed > 1) request.Progress += deltaTime;
                    if (request.Definition.requestType == TaxiRequestType.MaximumChaos) request.Progress = ChaosScore;
                    if (request.Definition.requestType == TaxiRequestType.SmoothRide && Mathf.Abs(acceleration) > request.Definition.maximumAcceleration)
                        Resolve(request, false,"HARSH ACCELERATION");
                    if (request.State != TaxiRequestState.Active) continue;
                    if (!IsArrivalRequest(request.Definition.requestType) && request.Progress >= request.Target) Resolve(request,true);
                    else if (request.Remaining<=0) { request.Expired=true; Resolve(request,false); }
                }
                requestClock += deltaTime;
                if (requestClock >= Passenger.requestFrequency) { GenerateRequest(); requestClock = 0; }
                if (ElapsedRide >= Passenger.basePatience) FailRide("Passenger patience exhausted.");
                else if (ActiveStop==null && Destination.Contains(playerPosition) && speed <= config.stoppedSpeed)
                    SetState(TruckTaxiState.PassengerExiting);
            }
            else if (State == TruckTaxiState.PassengerExiting)
            {
                if (!Destination.Contains(playerPosition) || speed > config.stoppedSpeed) SetState(TruckTaxiState.DrivingToDestination);
                else if (StateAge >= config.exitingSeconds) FinishRide();
            }
        }
        public void DebugSetOfferDuration(float seconds)
        {
            if(State!=TruckTaxiState.RideOffered) return;
            OfferDuration=Mathf.Max(.1f,seconds); StateAge=0;
            Changed?.Invoke();
        }
        private static bool IsArrivalRequest(TaxiRequestType type) =>
            type == TaxiRequestType.FastDelivery || type == TaxiRequestType.NoCollisions || type == TaxiRequestType.SmoothRide;

        public bool CanAssign(PassengerRequestDefinition definition,out string reason)
        {
            if(!Capabilities.Supports(definition)) { reason="Disabled or missing capability"; return false; }
            if(Capabilities.TargetLimit(definition.requestType)<=0)
            { reason="No remaining world targets"; return false; }
            if(!definition.IsStop && !string.IsNullOrWhiteSpace(definition.targetId))
            {
                if(CanResolveSpecificTarget==null || !CanResolveSpecificTarget(definition))
                { reason="Specific non-stop target missions do not have a runtime resolver yet"; return false; }
            }
            if((definition.requestType==TaxiRequestType.NoCollisions || definition.requestType==TaxiRequestType.SmoothRide) && TrackedCollisions>0)
            { reason="Ride already contains a collision"; return false; }
            if(definition.requestType==TaxiRequestType.SmoothRide && maximumRideAcceleration>definition.maximumAcceleration)
            { reason="Ride already contains harsh acceleration"; return false; }
            if(definition.IsStop && Capabilities.FindStop(definition,Passenger,playerPosition())==null)
            { reason="No eligible stop point"; return false; }
            if(definition.IsStop && ActiveStop!=null)
            { reason="Another stop already owns the GPS detour"; return false; }
            var set=new List<PassengerRequestDefinition>();
            foreach(var old in requests) set.Add(old.Definition);
            set.Add(definition);
            return Capabilities.ValidateCombination(set,out reason,false);
        }
        public bool GenerateRequest(PassengerRequestDefinition forced=null)
        {
            if (State != TruckTaxiState.DrivingToDestination || requests.Count >= 8) return false;
            RefreshObjectiveSupport?.Invoke();
            int active = 0; foreach (var r in requests) if (r.State == TaxiRequestState.Active) active++;
            if (active >= 3) return false;
            var source = forced!=null ? new[]{forced} : Passenger.possibleRequests;
            if (source == null || source.Length == 0) return false;
            var eligible = new List<PassengerRequestDefinition>();
            foreach (var definition in source)
            {
                if (CanAssign(definition,out _)) eligible.Add(definition);
            }
            if (eligible.Count == 0) return false;
            var chosen = eligible[random.Next(eligible.Count)];
            float difficulty = Mathf.Lerp(Passenger.requestDifficultyRange.x, Passenger.requestDifficultyRange.y, (float)random.NextDouble());
            var progress = new TaxiRequestProgress(chosen, difficulty,chosen.IsStop ? Capabilities.FindStop(chosen,Passenger,playerPosition()) : null,
                Capabilities.TargetLimit(chosen.requestType));
            requests.Add(progress);
            RequestCreated?.Invoke(progress);
            React(progress.Description);
            Changed?.Invoke(); return true;
        }
        public void RecordPedestrianHit(string id,float speed,Vector3 impulse,Vector3 position) =>
            RecordEvent(TaxiEventType.PedestrianHit,id,speed,pedestrianImpact:new TruckTaxiPedestrianImpact(id,speed,impulse,position,
                HasPassenger ? Passenger?.passengerId : null, HasPassenger ? CurrentRideId : null));
        public void RecordEvent(TaxiEventType type, string targetId, float impactSpeed = 0, int scoreOverride = -1, string passengerDialogue = null,
            TruckTaxiPedestrianImpact? pedestrianImpact=null)
        {
            if(type==TaxiEventType.PedestrianHit)
            {
                if(!float.IsFinite(impactSpeed) || impactSpeed<Mathf.Max(.1f,config.pedestrianImpact.minimumRagdollImpactSpeed) ||
                    string.IsNullOrEmpty(targetId) || !hitPedestrians.Add(targetId)) return;
                LastPedestrianImpact=pedestrianImpact ?? new TruckTaxiPedestrianImpact(targetId,impactSpeed,Vector3.zero,lastPosition,
                    HasPassenger ? Passenger?.passengerId : null,HasPassenger ? CurrentRideId : null);
                PedestriansHit++; PedestrianImpact?.Invoke(LastPedestrianImpact.Value);
                if(State==TruckTaxiState.DrivingToDestination) ridePedestriansHit++;
            }
            if(type==TaxiEventType.TrafficRam && impactSpeed>=config.minimumImpactSpeed)
            { TrafficHits++; if(State==TruckTaxiState.DrivingToDestination) rideTrafficHits++; }
            if (State != TruckTaxiState.DrivingToDestination) return;
            bool impact = type == TaxiEventType.Collision || type == TaxiEventType.TrafficRam ||
                type == TaxiEventType.PedestrianHit || type == TaxiEventType.PropDamage;
            if (impact && type!=TaxiEventType.PedestrianHit && impactSpeed < config.minimumImpactSpeed) return;
            chaos += scoreOverride >= 0 ? scoreOverride : config.Score(type);
            if (impact)
            {
                TrackedCollisions++;
                TotalCollisions++;
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
                { Resolve(r,false,"QUALIFYING COLLISION"); continue; }
                bool matches =
                    (type == TaxiEventType.Shortcut && definition.requestType == TaxiRequestType.Shortcut) ||
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
        public void React(string value) { Reaction = value; ReactionAge = 0; }
        public bool RecordObjectiveProgress(TaxiRequestProgress request, float amount, bool complete=false)
        {
            if(!HasPassenger || request==null || request.State!=TaxiRequestState.Active || !requests.Contains(request) ||
                !float.IsFinite(amount) || amount<0) return false;
            request.Progress=Mathf.Min(request.Target,request.Progress+amount);
            if(complete || request.Progress>=request.Target)
            { request.Progress=request.Target; Resolve(request,true); }
            Changed?.Invoke();
            return true;
        }
        public bool FailObjective(TaxiRequestProgress request, string reason)
        {
            if(!HasPassenger || request==null || request.State!=TaxiRequestState.Active || !requests.Contains(request)) return false;
            Resolve(request,false,reason); if(!string.IsNullOrWhiteSpace(reason)) React(reason); Changed?.Invoke(); return true;
        }
        public void ApplyMechanicReward(int chaosReward, long fareCents)
        {
            if (!HasPassenger) return;
            chaos += Mathf.Clamp(chaosReward,-1000,1000);
            MechanicFareAdjustment = Math.Max(-10000,Math.Min(10000,MechanicFareAdjustment + fareCents));
        }
        public bool ChangeDestination()
        {
            if (State != TruckTaxiState.DrivingToDestination) return false;
            var next = locations.Find(l=>l!=null && l.dropoffAllowed && l!=Pickup && l!=Destination);
            if(next==null) return false;
            Destination=next; DestinationChanged?.Invoke(); Changed?.Invoke(); return true;
        }
        public bool EjectPassenger()
        {
            if (!HasPassenger || Passenger == null || !Passenger.canBeEjected) return false;
            foreach(var request in requests) if(request.State==TaxiRequestState.Active) Resolve(request,false,"PASSENGER EJECTED");
            Satisfaction=Mathf.Clamp(Satisfaction-Passenger.ejectionRatingPenalty,1,5);
            chaos+=Passenger.ejectionChaosReward;
            LastFare=EstimateFare(); LastFare.Tip=0; LastFare.Penalties+=Passenger.ejectionFarePenaltyCents;
            ShiftEarnings+=LastFare.Total; ShiftScore+=LastFare.Score;
            React(Passenger.ejectionReaction);
            EjectedRides++;
            RecordRide(TruckTaxiRideOutcome.Ejected,null);
            PassengerEjected?.Invoke();
            SetState(TruckTaxiState.PassengerEjected);
            return true;
        }
        private void Resolve(TaxiRequestProgress request, bool success, string failureReason=null)
        {
            if (request.State != TaxiRequestState.Active) return;
            request.State = success ? TaxiRequestState.Succeeded : TaxiRequestState.Failed;
            request.FailureReason=success ? null : request.Expired ? "EXPIRED" : failureReason ?? "FAILED";
            if(success && request.IsArrivalGoal) request.Progress=request.Target;
            if(success && request.StopPoint!=null) chaos+=request.StopPoint.chaosReward;
            if(success && request.Definition.IsStop) { SpecialStopsCompleted++; rideSpecialStopsCompleted++; }
            if(request.Definition.IsStop) stopStarted=false;
            request.ResolvedAt = ElapsedRide;
            React((success ? "REQUEST COMPLETE: " : "REQUEST FAILED: ") + request.Description);
            Satisfaction = Mathf.Clamp(Satisfaction + (success ? request.Definition.ratingModifier : -0.2f),1,5);
            RequestResolved?.Invoke(request);
        }
        public TaxiFare EstimateFare()
        {
            var fare = new TaxiFare
            {
                Base = (long)Math.Round(config.baseFareCents*DemandFareMultiplier),
                Distance = (long)Math.Round(DistanceDriven * config.centsPerMeter*DemandFareMultiplier),
                Time = (long)Math.Round(ElapsedRide * config.centsPerSecond*DemandFareMultiplier),
                Rating = StarsForSatisfaction(Satisfaction), ChaosScore = ChaosScore, Score = ChaosScore
            };
            foreach (var r in requests) if (r.State == TaxiRequestState.Succeeded)
            { fare.Requests += r.Definition.bonusMoneyCents; fare.Score += r.Definition.bonusScore; }
            if (Passenger != null && Passenger.chaosAffinity > 0)
                fare.Chaos = (long)Math.Round(ChaosScore * config.chaosCentsPerPoint * Passenger.chaosAffinity);
            if (Passenger != null && Passenger.chaosAffinity < 0) fare.Penalties = config.impactPenaltyCents * TrackedCollisions;
            if(MechanicFareAdjustment>=0) fare.Requests+=MechanicFareAdjustment;
            else fare.Penalties-=MechanicFareAdjustment;
            return fare;
        }
        private void FinishRide()
        {
            if (State != TruckTaxiState.PassengerExiting) return;
            foreach (var r in requests)
                if (r.State == TaxiRequestState.Active)
                    Resolve(r,r.IsArrivalGoal && r.Remaining>0,
                        r.IsArrivalGoal ? "EXPIRED" : "DESTINATION REACHED");
            if(Passenger.CanOfferAppreciation && Satisfaction>=Passenger.appreciationMinimumSatisfaction &&
                StarsForSatisfaction(Satisfaction)==5 && random.NextDouble()<Mathf.Clamp01(
                    Mathf.Max(config.appreciationBaseChance,Passenger.appreciationChance)*
                    (IsEvening ? config.eveningAppreciationMultiplier : IsLateNight ? config.lateNightAppreciationMultiplier :
                        IsPreDawn ? config.preDawnAppreciationMultiplier : 1)))
            { SetState(TruckTaxiState.AppreciationOffer); return; }
            AwardFare();
        }
        public bool ChooseAppreciation(bool accept)
        {
            if(State!=TruckTaxiState.AppreciationOffer) return false;
            SpecialAppreciationAccepted=accept;
            if(accept) SetState(TruckTaxiState.AppreciationSequence); else AwardFare();
            return true;
        }
        public void CompleteAppreciation()
        { if(State==TruckTaxiState.AppreciationSequence) AwardFare(); }
        private void AwardFare()
        {
            LastFare = EstimateFare();
            bool neverTips = Array.IndexOf(Passenger.uniqueMechanics ?? Array.Empty<TruckTaxiMechanic>(),TruckTaxiMechanic.NeverTips)>=0;
            if (!neverTips && random.NextDouble() <= Passenger.baseTipChance * Mathf.Clamp01((Satisfaction-1)/4))
                LastFare.Tip = (long)Math.Round(LastFare.Total * config.tipFraction * Passenger.tipMultiplier);
            ShiftEarnings += LastFare.Total; ShiftScore += LastFare.Score; CompletedRides++;
            TotalStarsEarned+=LastFare.Rating;
            if(Passenger!=null && Destination!=null && !string.IsNullOrWhiteSpace(Passenger.passengerId))
                continuity[Passenger.passengerId]=new PassengerContinuity { LastRideLocationId=Destination.locationId,
                    LastDropoffPosition=Destination.StopPosition, LastKnownDistrict=Destination.district,LastDropoffMinutes=gameMinutes };
            RecordRide(TruckTaxiRideOutcome.Completed,null);
            SetState(TruckTaxiState.RideComplete);
        }
        public void FailRide(string reason)
        {
            if (State != TruckTaxiState.DrivingToPickup && State != TruckTaxiState.PassengerBoarding && !HasPassenger) return;
            foreach (var r in requests) Resolve(r,false,reason);
            React(reason);
            bool cancelled=(State==TruckTaxiState.DrivingToPickup || State==TruckTaxiState.PassengerBoarding) &&
                reason=="Passenger cancelled: pickup wait expired.";
            FailedRides++; if(cancelled) PickupCancellations++;
            if(cancelled && Passenger!=null && Pickup!=null && !string.IsNullOrWhiteSpace(Passenger.passengerId))
                continuity[Passenger.passengerId]=new PassengerContinuity { LastRideLocationId=Pickup.locationId,
                    LastDropoffPosition=Pickup.StopPosition, LastKnownDistrict=Pickup.district,LastDropoffMinutes=gameMinutes };
            RecordRide(cancelled ? TruckTaxiRideOutcome.PickupCancelled : TruckTaxiRideOutcome.Failed,reason);
            SetState(TruckTaxiState.RideFailed);
            if(cancelled) ContinueShift();
        }
        private void RecordRide(TruckTaxiRideOutcome outcome, string reason)
        {
            if(string.IsNullOrEmpty(CurrentRideId)) return;
            rideHistory.Add(new TruckTaxiRideRecord(CurrentRideId,Passenger?.passengerId,Pickup?.locationId,
                Destination?.locationId,outcome,reason,gameMinutes,IsRepeatPassenger,SpecialAppreciationAccepted,
                ElapsedRide,DistanceDriven,ChaosScore,TrackedCollisions,ridePedestriansHit,rideSpecialStopsCompleted,rideJugsSucceeded,
                rideJugsSpilled,rideContainersThrown,LastFare,requests,rideTrafficHits));
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
