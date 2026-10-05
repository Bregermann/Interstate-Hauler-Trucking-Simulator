using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        public TruckTaxiPersistentState CapturePersistentState()
        {
            return new TruckTaxiPersistentState
            {
                shiftEarningsCents=ShiftEarnings,purchaseChargesCents=purchaseCharges,serviceChargesCents=serviceCharges,
                rideRequestsEnabled=RideRequestsEnabled,gameMinutes=gameMinutes,
                statistics=new TruckTaxiPersistentStatistics
                {
                    completedRides=CompletedRides,failedRides=FailedRides,ejectedRides=EjectedRides,pickupCancellations=PickupCancellations,
                    totalStarsEarned=TotalStarsEarned,shiftScore=ShiftScore,totalCollisions=TotalCollisions,trafficHits=TrafficHits,
                    pedestriansHit=PedestriansHit,jugsSucceeded=JugsSucceeded,jugsSpilled=JugsSpilled,containersThrown=ContainersThrown,
                    specialStopsCompleted=SpecialStopsCompleted,offersDeclined=OffersDeclined,offersExpired=OffersExpired,
                    racesEntered=RacesEntered,raceWins=RaceWins,racePodiums=RacePodiums,raceChaosEarned=RaceChaosEarned,
                    thunderBowlBestLapSeconds=ThunderBowlBestLapSeconds,racePrizeEarningsCents=RacePrizeEarningsCents
                },
                workArea=new TruckTaxiPersistentWorkArea { modeId=PersistentWorkAreaId(workArea.mode),
                    centerX=workArea.center.x,centerY=workArea.center.y,centerZ=workArea.center.z,radius=workArea.radius,label=workArea.label },
                passengerSelection=passengerDispatch.CaptureState(),
                intercity=intercityPolicy?.CaptureState() ?? new TruckTaxiIntercityState()
            };
        }

        public static bool ValidatePersistentState(TruckTaxiPersistentState snapshot,out string reason)
        {
            if(snapshot==null || snapshot.version!=1) return InvalidPersistentState("Missing snapshot or unsupported version.",out reason);
            if(snapshot.statistics==null || snapshot.workArea==null || snapshot.passengerSelection==null || snapshot.intercity==null)
                return InvalidPersistentState("Missing required semantic state.",out reason);
            if(snapshot.shiftEarningsCents<0 || snapshot.purchaseChargesCents<0 || snapshot.serviceChargesCents<0)
                return InvalidPersistentState("Negative cash component.",out reason);
            try
            {
                long balance=checked(snapshot.shiftEarningsCents-snapshot.purchaseChargesCents-snapshot.serviceChargesCents);
                if(balance<0) { _=checked(-balance); }
            }
            catch(OverflowException) { return InvalidPersistentState("Wallet or debt would overflow.",out reason); }
            if(double.IsNaN(snapshot.gameMinutes) || double.IsInfinity(snapshot.gameMinutes) || snapshot.gameMinutes<0)
                return InvalidPersistentState("Invalid game minutes.",out reason);
            if(!TryPersistentWorkAreaMode(snapshot.workArea.modeId,out var mode))
                return InvalidPersistentState("Unknown work-area mode ID.",out reason);
            var area=snapshot.workArea;
            if(!new TruckTaxiRideWorkArea { mode=mode,center=new Vector3(area.centerX,area.centerY,area.centerZ),radius=area.radius }.IsValid)
                return InvalidPersistentState("Nonfinite or out-of-range work area.",out reason);
            double x=Math.Abs((double)area.centerX)+area.radius,y=Math.Abs((double)area.centerY)+area.radius,z=Math.Abs((double)area.centerZ)+area.radius;
            if(x*x+y*y+z*z>float.MaxValue/4.0)
                return InvalidPersistentState("Work-area distance calculations would overflow.",out reason);

            var stats=snapshot.statistics;
            var counters=new[] { stats.completedRides,stats.failedRides,stats.ejectedRides,stats.pickupCancellations,
                stats.totalStarsEarned,stats.totalCollisions,stats.trafficHits,stats.pedestriansHit,stats.jugsSucceeded,
                stats.jugsSpilled,stats.containersThrown,stats.specialStopsCompleted,stats.offersDeclined,stats.offersExpired,
                stats.racesEntered,stats.raceWins,stats.racePodiums,stats.raceChaosEarned };
            foreach(int value in counters) if(value<0 || value==int.MaxValue)
                return InvalidPersistentState("Negative or exhausted statistics counter.",out reason);
            if(stats.shiftScore==int.MinValue || stats.shiftScore==int.MaxValue ||
                stats.pickupCancellations>stats.failedRides || stats.raceWins>stats.racePodiums ||
                stats.totalStarsEarned<stats.completedRides || stats.totalStarsEarned>5L*stats.completedRides)
                return InvalidPersistentState("Inconsistent statistics.",out reason);
            if(!float.IsFinite(stats.thunderBowlBestLapSeconds) || stats.thunderBowlBestLapSeconds<0 ||
                stats.racePrizeEarningsCents<0 || stats.racePrizeEarningsCents>snapshot.shiftEarningsCents)
                return InvalidPersistentState("Invalid racing statistics.",out reason);

            var selection=snapshot.passengerSelection;
            // Validate with a detached policy, never by tentatively restoring the live session.
            if(!new TruckTaxiPassengerDispatchPolicy().RestoreState(selection) || selection.offerIndex==int.MaxValue)
                return InvalidPersistentState("Invalid passenger selection state.",out reason);
            long offered=0,accepted=0,completed=0;
            var lastIndices=new HashSet<int>();
            foreach(var entry in selection.passengers)
            {
                if(entry.timesOffered<=0 || entry.lastOfferedRideIndex<=0 || !lastIndices.Add(entry.lastOfferedRideIndex) ||
                    entry.lastOfferedGameMinutes>snapshot.gameMinutes || string.IsNullOrWhiteSpace(entry.archetype))
                    return InvalidPersistentState("Inconsistent passenger exposure record.",out reason);
                offered+=entry.timesOffered; accepted+=entry.timesAccepted; completed+=entry.timesCompleted;
            }
            if(offered!=selection.offerIndex || completed!=stats.completedRides ||
                (long)stats.completedRides+stats.failedRides+stats.ejectedRides>accepted ||
                (long)stats.offersDeclined+stats.offersExpired+accepted>offered ||
                selection.recentArchetypes.Count!=Math.Min(20,selection.offerIndex))
                return InvalidPersistentState("Exposure totals do not agree with session statistics.",out reason);
            foreach(var category in selection.recentArchetypes) if(string.IsNullOrWhiteSpace(category))
                return InvalidPersistentState("Missing recent archetype.",out reason);
            var intercity=snapshot.intercity;
            if(intercity.version!=1 || intercity.completedLocalStreak<0 || intercity.completedLocalStreak>stats.completedRides ||
                intercity.localCooldownRemaining<0 || intercity.localCooldownRemaining>Math.Max(0,2-intercity.completedLocalStreak) ||
                intercity.localCooldownRemaining>0 && stats.completedRides<=intercity.completedLocalStreak)
                return InvalidPersistentState("Invalid completed-local cooldown or drought.",out reason);
            reason=null; return true;
        }

        public bool TryRestorePersistentState(TruckTaxiPersistentState snapshot,out string reason)
        {
            // Detach before validation so caller-owned objects are never installed in live state.
            var prepared=snapshot?.Copy();
            if(!ValidatePersistentState(prepared,out reason)) return false;
            var restoredIntercity=new TruckTaxiIntercityPolicy(config.intercityRideChance,2,3);
            if(!restoredIntercity.RestoreState(prepared.intercity)) return InvalidPersistentState("Invalid intercity state.",out reason);
            TryPersistentWorkAreaMode(prepared.workArea.modeId,out var mode);
            var restoredArea=new TruckTaxiRideWorkArea { mode=mode,
                center=new Vector3(prepared.workArea.centerX,prepared.workArea.centerY,prepared.workArea.centerZ),
                radius=prepared.workArea.radius,label=prepared.workArea.label };

            // Everything above is validation/preparation. No payouts, refunds, cancellation events,
            // demand callbacks, or PlayerPrefs setters run while installing a validated snapshot.
            CancelOfferGeneration();
            ResetPersistentRideTransients();
            State=TruckTaxiState.Available;
            RestorePassengerSelectionState(prepared.passengerSelection);
            intercityPolicy=restoredIntercity; workArea=restoredArea;
            ShiftEarnings=prepared.shiftEarningsCents; purchaseCharges=prepared.purchaseChargesCents; serviceCharges=prepared.serviceChargesCents;
            RideRequestsEnabled=prepared.rideRequestsEnabled; gameMinutes=prepared.gameMinutes;
            var stats=prepared.statistics;
            CompletedRides=stats.completedRides; FailedRides=stats.failedRides; EjectedRides=stats.ejectedRides; PickupCancellations=stats.pickupCancellations;
            TotalStarsEarned=stats.totalStarsEarned; ShiftScore=stats.shiftScore; TotalCollisions=stats.totalCollisions;
            TrafficHits=stats.trafficHits; PedestriansHit=stats.pedestriansHit; JugsSucceeded=stats.jugsSucceeded; JugsSpilled=stats.jugsSpilled;
            ContainersThrown=stats.containersThrown; SpecialStopsCompleted=stats.specialStopsCompleted;
            OffersDeclined=stats.offersDeclined; OffersExpired=stats.offersExpired; RacesEntered=stats.racesEntered;
            RaceWins=stats.raceWins; RacePodiums=stats.racePodiums; RaceChaosEarned=stats.raceChaosEarned;
            ThunderBowlBestLapSeconds=stats.thunderBowlBestLapSeconds; RacePrizeEarningsCents=stats.racePrizeEarningsCents;
            Changed?.Invoke();
            return true;
        }

        private void ResetPersistentRideTransients()
        {
            ResetRideBehavior(); offerSuppressors.Clear();
            Passenger=null; Pickup=Destination=null; Offer=null; CurrentRideId=null; LastFare=null;
            requests.Clear(); rideHistory.Clear(); continuity.Clear(); hitPedestrians.Clear(); LastPedestrianImpact=null;
            ElapsedRide=DistanceDriven=StateAge=ReactionAge=0; Satisfaction=4; Reaction="";
            chaos=requestClock=nextSpeedReaction=maximumRideAcceleration=0; hasPosition=false; lastPosition=Vector3.zero;
            TrackedCollisions=0; rideTrafficHits=ridePedestriansHit=rideSpecialStopsCompleted=rideJugsSucceeded=rideJugsSpilled=rideContainersThrown=0;
            MechanicFareAdjustment=0; SpecialAppreciationAccepted=IsRepeatPassenger=IsIntercityRide=stopStarted=false;
            OfferDuration=PickupElapsed=PickupDuration=0; OfferTimerPaused=false;
            AcceptedFareModifiers=default; DemandFareMultiplier=IntercityFareBonusMultiplier=1;
            declinedPassengerId=declinedPickupId=declinedDestinationId=null;
            DispatchRemaining=LastDispatchDelay=0; scheduledDemand=1; DispatchDemandLabel="RESTORED";
            LastIntercitySelection="LOCAL"; LastIntercityJourney="NONE"; LastSketchyEligibilityReason="NOT CHECKED";
            LastOfferCandidateCount=LastOfferRejectedPickupRoutes=LastOfferRejectedDestinationRoutes=LastOfferRouteQueries=0;
            LastOfferMaximumStepMs=0; LastOfferDiagnostics="Restored available session."; offerStages.Clear();
            // Entry charges are already in purchaseCharges. Discarding a reservation must not refund them.
            pendingRaceEntryCents=-1; RaceEventActive=false;
        }

        private static bool InvalidPersistentState(string message,out string reason) { reason=message; return false; }
        private static string PersistentWorkAreaId(TruckTaxiWorkAreaMode mode)
        {
            switch(mode)
            {
                case TruckTaxiWorkAreaMode.AnywhereNearMe: return "nearby";
                case TruckTaxiWorkAreaMode.CurrentDistrict: return "district";
                case TruckTaxiWorkAreaMode.CurrentTown: return "town";
                case TruckTaxiWorkAreaMode.Custom: return "custom";
                default: return null;
            }
        }
        private static bool TryPersistentWorkAreaMode(string id,out TruckTaxiWorkAreaMode mode)
        {
            switch(id)
            {
                case "nearby": mode=TruckTaxiWorkAreaMode.AnywhereNearMe; return true;
                case "district": mode=TruckTaxiWorkAreaMode.CurrentDistrict; return true;
                case "town": mode=TruckTaxiWorkAreaMode.CurrentTown; return true;
                case "custom": mode=TruckTaxiWorkAreaMode.Custom; return true;
                default: mode=default; return false;
            }
        }
    }
}
