using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        private TruckTaxiRideWorkArea workArea = new TruckTaxiRideWorkArea();
        private TruckTaxiIntercityPolicy intercityPolicy;
        private readonly TruckTaxiOfferMix offerMix = new TruckTaxiOfferMix();
        public int LocalOfferStreak => offerMix.LocalStreak;
        private readonly TruckTaxiPassengerDispatchPolicy passengerDispatch=new TruckTaxiPassengerDispatchPolicy();
        public bool RaceEventActive { get; set; }
        public int OffersGenerated => passengerDispatch.OfferIndex;
        public int OffersDeclined { get; private set; }
        public int OffersExpired { get; private set; }
        private int consecutiveManualDeclines;
        private double manualDeclineCooldownUntilGameMinutes;
        public int ConsecutiveManualDeclines { get { RefreshManualDeclineCooldown(); return consecutiveManualDeclines; } }
        public double ManualDeclineCooldownRemainingGameMinutes =>
            Math.Max(0,manualDeclineCooldownUntilGameMinutes-gameMinutes);
        public bool ManualDeclineCooldownActive => ManualDeclineCooldownRemainingGameMinutes>0;

        private void RefreshManualDeclineCooldown()
        {
            if(manualDeclineCooldownUntilGameMinutes<=0 || ManualDeclineCooldownActive) return;
            manualDeclineCooldownUntilGameMinutes=0;
            consecutiveManualDeclines=0;
        }
        private void RecordManualDecline()
        {
            RefreshManualDeclineCooldown();
            consecutiveManualDeclines=Math.Min(5,consecutiveManualDeclines+1);
            if(consecutiveManualDeclines<5) return;
            manualDeclineCooldownUntilGameMinutes=gameMinutes+10;
            CancelOfferGeneration();
            DispatchDemandLabel="MANUAL DECLINE COOLDOWN";
        }
        // Eligibility is evaluated afresh; route validity remains the route service's authority.
        public IReadOnlyList<TruckTaxiPassengerEligibilityRecord> CapturePassengerEligibility()
        {
            var result=new List<TruckTaxiPassengerEligibilityRecord>();
            var source=config.passengerDatabase!=null ? config.passengerDatabase.passengers : config.passengers;
            var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var origin=playerPosition();
            var nearby=locations.FindAll(stop=>TruckTaxiOfferEligibility.PublicStop(stop) && stop.isActiveAndEnabled && stop.pickupAllowed && workArea.Contains(stop.StopPosition) &&
                Vector3.Distance(stop.StopPosition,origin)<=Mathf.Max(1,config.pickupHardMaximumSeconds)*Mathf.Max(1,config.pickupReasonableSpeedMetersPerSecond));
            nearby.Sort((a,b)=>CompareNear(a,b,origin));
            int limit=Mathf.Clamp(config.offerPickupCandidateLimit,1,12);
            if(nearby.Count>limit) nearby.RemoveRange(limit,nearby.Count-limit);
            foreach(var profile in source ?? Array.Empty<PassengerProfile>())
            {
                if(profile==null) { result.Add(new TruckTaxiPassengerEligibilityRecord { Reason="Missing profile reference" }); continue; }
                float weight=CandidateWeight(profile);
                string reason=string.IsNullOrWhiteSpace(profile.passengerId) ? "Missing stable ID" : !ids.Add(profile.passengerId) ? "Duplicate stable ID" :
                    !profile.available ? "Unavailable" : !float.IsFinite(weight) || weight<=0 ? "Nonpositive/invalid context weight" :
                    !nearby.Exists(stop=>CanStartAt(profile,stop)) ? "No pickup in work area/range/district/continuity candidate set" : null;
                result.Add(new TruckTaxiPassengerEligibilityRecord { PassengerId=profile.passengerId,DisplayName=profile.passengerName,
                    Archetype=profile.DispatchArchetype,Reason=reason ?? "Pickup metadata eligible; route validation pending",
                    PickupMetadataEligible=reason==null,SpawnWeight=profile.spawnWeight,ContextWeight=weight,Rarity=profile.rarity,
                    Districts=(string[])(profile.districts ?? Array.Empty<string>()).Clone(),Exposure=passengerDispatch.Exposure(profile.passengerId) });
            }
            return result.AsReadOnly();
        }
        public TruckTaxiPassengerSelectionState CapturePassengerSelectionState() => passengerDispatch.CaptureState();
        public bool RestorePassengerSelectionState(TruckTaxiPassengerSelectionState snapshot)
        {
            if(State!=TruckTaxiState.Inactive && State!=TruckTaxiState.Available || IsGeneratingOffer ||
                !passengerDispatch.RestoreState(snapshot)) return false;
            acceptedPassengerIds.Clear(); recentOfferIds.Clear();
            foreach(var entry in snapshot.passengers) if(entry.timesAccepted>0) acceptedPassengerIds.Add(entry.passengerId);
            var recent=new List<TruckTaxiPassengerExposure>(snapshot.passengers);
            recent.Sort((a,b)=>a.lastOfferedRideIndex.CompareTo(b.lastOfferedRideIndex));
            foreach(var entry in recent) if(entry.timesOffered>0) recentOfferIds.Add(entry.passengerId);
            int limit=Mathf.Max(1,config.recentOfferCount);
            if(recentOfferIds.Count>limit) recentOfferIds.RemoveRange(0,recentOfferIds.Count-limit);
            return true;
        }
        public TruckTaxiRideWorkArea WorkArea => workArea.Copy();
        public TruckTaxiIntercityPolicy IntercityPolicy => intercityPolicy ??
            (intercityPolicy=new TruckTaxiIntercityPolicy(config.intercityRideChance,2,3));
        public Func<Vector3,float> RegionalDemandMultiplier { get; set; }
        public Func<Vector3,float> PickupWeight { get; set; }
        public Func<Vector3,float> DestinationWeight { get; set; }
        public Func<Vector3,Vector3,TruckTaxiFareModifiers> FareModifiersProvider { get; set; }
        public TruckTaxiFareModifiers AcceptedFareModifiers { get; private set; }
        public float IntercityFareBonusMultiplier { get; private set; }=1;
        public float DispatchRemaining { get; private set; }
        public float LastDispatchDelay { get; private set; }
        public string DispatchDemandLabel { get; private set; }="NORMAL";
        public string LastIntercitySelection { get; private set; }="LOCAL";
        public string LastIntercityJourney { get; private set; }="NONE";
        private float scheduledDemand=1;

        public bool SetWorkArea(TruckTaxiRideWorkArea area)
        {
            if(area==null || !area.IsValid) return false;
            workArea=area.Copy();
            CancelOfferGeneration();
            if(State==TruckTaxiState.RideOffered && Pickup!=null && !workArea.Contains(Pickup.StopPosition)) EndOffer(false,false);
            else if(State==TruckTaxiState.Available) ScheduleDispatch();
            Changed?.Invoke(); return true;
        }
        // Session-only snapshot seam. No career file or platform storage is introduced.
        public TruckTaxiRideWorkArea CaptureWorkArea() => workArea.Copy();
        public bool RestoreWorkArea(TruckTaxiRideWorkArea snapshot) => SetWorkArea(snapshot);
        public void SetWorkAreaPreset(TruckTaxiWorkAreaMode mode)
        {
            var position=playerPosition();
            var area=new TruckTaxiRideWorkArea { mode=mode,center=position,radius=config.defaultWorkAreaRadius };
            area.label=mode==TruckTaxiWorkAreaMode.AnywhereNearMe ? "ANYWHERE NEAR ME" : mode==TruckTaxiWorkAreaMode.Custom ? "CUSTOM" :
                mode==TruckTaxiWorkAreaMode.CurrentTown ? "CURRENT TOWN" : "CURRENT DISTRICT";
            if(mode==TruckTaxiWorkAreaMode.CurrentDistrict || mode==TruckTaxiWorkAreaMode.CurrentTown)
            {
                TruckTaxiRideLocation nearest=null; float distance=float.MaxValue;
                foreach(var stop in locations) if(stop!=null && stop.pickupAllowed && (stop.StopPosition-position).sqrMagnitude<distance)
                { nearest=stop; distance=(stop.StopPosition-position).sqrMagnitude; }
                if(nearest!=null)
                {
                    var bounds=new Bounds(nearest.StopPosition,Vector3.zero);
                    string town=RegionResolver?.Invoke(nearest.StopPosition);
                    foreach(var stop in locations) if(stop!=null && stop.pickupAllowed &&
                        (mode==TruckTaxiWorkAreaMode.CurrentDistrict ? stop.district==nearest.district : !string.IsNullOrEmpty(town) && RegionResolver?.Invoke(stop.StopPosition)==town))
                        bounds.Encapsulate(stop.StopPosition);
                    area.center=bounds.center;
                    area.radius=Mathf.Clamp(new Vector2(bounds.extents.x,bounds.extents.z).magnitude+50,50,10000);
                }
            }
            SetWorkArea(area);
        }
        private float CurrentDemand()
        {
            var position=workArea.Restricted ? workArea.center : playerPosition();
            float factor=RegionalDemandMultiplier?.Invoke(position) ?? 1;
            return Mathf.Clamp((float.IsFinite(factor) ? factor : 1)/FrequencyMultiplier(),.1f,8);
        }
        private void ScheduleDispatch()
        {
            scheduledDemand=CurrentDemand();
            Vector2 range;
            if(scheduledDemand>=2.5f) { range=config.veryHighDemandDelay; DispatchDemandLabel="EVENT SURGE"; }
            else if(scheduledDemand>=1.4f) { range=config.highDemandDelay; DispatchDemandLabel="HIGH"; }
            else if(scheduledDemand>=.8f) { range=config.normalDemandDelay; DispatchDemandLabel="NORMAL"; }
            else if(scheduledDemand>=.45f) { range=config.lowDemandDelay; DispatchDemandLabel="LOW"; }
            else { range=config.veryLowDemandDelay; DispatchDemandLabel="VERY LOW"; }
            LastDispatchDelay=Mathf.Lerp(Mathf.Max(1,range.x),Mathf.Max(range.x,range.y),(float)random.NextDouble());
            DispatchRemaining=LastDispatchDelay;
        }
        private void TickDispatch(float dt)
        {
            RefreshManualDeclineCooldown();
            if(ManualDeclineCooldownActive) { DispatchDemandLabel="MANUAL DECLINE COOLDOWN"; return; }
            if(!RideRequestsEnabled || OffersSuppressed) return;
            DispatchRemaining=Mathf.Max(0,DispatchRemaining-dt*Mathf.Clamp(CurrentDemand()/scheduledDemand,.25f,4));
        }
        private void CaptureFareModifiers()
        {
            var external=FareModifiersProvider?.Invoke(Pickup.StopPosition,Destination.StopPosition) ?? default;
            float timeBonus=IsMorning ? config.morningFareMultiplier : IsEvening ? config.eveningFareMultiplier :
                IsLateNight ? config.lateNightFareMultiplier : IsPreDawn ? config.preDawnFareMultiplier : 1;
            float weatherBonus=weather==TruckTaxiDemandWeather.Rain ? config.rainFareMultiplier : weather==TruckTaxiDemandWeather.Storm ? config.stormFareMultiplier :
                weather==TruckTaxiDemandWeather.Snow ? config.snowFareMultiplier : weather==TruckTaxiDemandWeather.HeavySnow ? config.heavySnowFareMultiplier :
                weather==TruckTaxiDemandWeather.Blizzard ? config.blizzardFareMultiplier : 1;
            AcceptedFareModifiers=new TruckTaxiFareModifiers(external.EventMultiplier,Mathf.Max(weatherBonus,external.WeatherMultiplier),
                Mathf.Max(timeBonus,external.OtherMultiplier),external.EventLabel,external.OtherLabel ?? "TIME OF DAY");
            IntercityFareBonusMultiplier=IsIntercityRide ? Mathf.Max(1,config.intercityFareMultiplier) : 1;
            AcceptedTownFareMultiplier=TownFareMultiplier(TownIdAtPosition?.Invoke(Pickup.StopPosition));
            DemandFareMultiplier=1+(AcceptedFareModifiers.EventMultiplier-1)+(AcceptedFareModifiers.WeatherMultiplier-1)+
                (AcceptedFareModifiers.OtherMultiplier-1)+(IntercityFareBonusMultiplier-1)+(AcceptedTownFareMultiplier-1);
        }
        private void ApplyFareBonuses(TaxiFare fare)
        {
            long basis=fare.Base+fare.Distance+fare.Time;
            fare.EventBonus=(long)Math.Round(basis*(AcceptedFareModifiers.EventMultiplier-1));
            fare.WeatherBonus=(long)Math.Round(basis*(AcceptedFareModifiers.WeatherMultiplier-1));
            fare.OtherBonus=(long)Math.Round(basis*(AcceptedFareModifiers.OtherMultiplier-1));
            fare.IntercityBonus=(long)Math.Round(basis*(IntercityFareBonusMultiplier-1));
            fare.TownBonus=(long)Math.Round(basis*(AcceptedTownFareMultiplier-1));
        }
        private static float ValidOfferWeight(float value) => float.IsFinite(value) ? Mathf.Clamp(value,.01f,20) : 1;
    }

    // Offer fairness is intentionally session-local. Completed-fare persistence remains unchanged.
    public sealed class TruckTaxiOfferMix
    {
        private float deficit;
        public int LocalStreak { get; private set; }
        public bool WantsIntercity(System.Random random, float target, out bool guaranteed)
        {
            target = float.IsFinite(target) ? Mathf.Clamp01(target) : .25f;
            guaranteed = target > 0 && LocalStreak >= 7;
            return guaranteed || random.NextDouble() < Mathf.Clamp01(target + deficit * .25f);
        }
        public void RecordOffer(bool intercity, float target)
        {
            target = float.IsFinite(target) ? Mathf.Clamp01(target) : .25f;
            deficit = Mathf.Clamp(deficit + target - (intercity ? 1 : 0), -1, 2);
            LocalStreak = intercity ? 0 : Math.Min(1000, LocalStreak + 1);
        }
    }

    public static class TruckTaxiOfferEligibility
    {
        public static string ComplexId(TruckTaxiRideLocation stop)
        {
            if (stop == null) return null;
            if ((stop.locationId ?? "").StartsWith("taxi.speedway.", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stop.district, "Speedway", StringComparison.OrdinalIgnoreCase))
                return TruckTaxiSpeedwayLayout.RegionId;
            foreach (string tag in stop.tags ?? Array.Empty<string>())
                if (tag != null && tag.StartsWith("complex:", StringComparison.OrdinalIgnoreCase) && tag.Length > 8)
                    return tag.Substring(8);
            return null;
        }

        public static bool PublicStop(TruckTaxiRideLocation stop)
        {
            if (stop == null) return false;
            foreach (string tag in stop.tags ?? Array.Empty<string>())
                if (string.Equals(tag, "race-surface", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tag, "infield", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tag, "restricted-venue", StringComparison.OrdinalIgnoreCase)) return false;
            if (ComplexId(stop) != TruckTaxiSpeedwayLayout.RegionId) return true;
            return TruckTaxiSpeedwayLayout.IsPublicFareStop(stop.locationId);
        }

        public static bool Pair(TruckTaxiRideLocation pickup, TruckTaxiRideLocation destination)
        {
            if (!PublicStop(pickup) || !PublicStop(destination) || pickup == destination ||
                string.Equals(pickup.locationId, destination.locationId, StringComparison.OrdinalIgnoreCase)) return false;
            string complex = ComplexId(pickup);
            return complex == null || !string.Equals(complex, ComplexId(destination), StringComparison.OrdinalIgnoreCase);
        }
    }
}
