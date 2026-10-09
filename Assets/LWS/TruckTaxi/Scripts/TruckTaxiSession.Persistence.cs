using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        private TruckTaxiCareerStatistics careerStatistics=new TruckTaxiCareerStatistics();
        private TruckTaxiLastSessionStatistics statisticsBaseline=new TruckTaxiLastSessionStatistics();
        private TruckTaxiLastSessionStatistics previousSessionStatistics;
        private readonly HashSet<TaxiRequestProgress> offeredCareerGoals=new HashSet<TaxiRequestProgress>();
        public long LifetimeChaosScore => AddCareerCount(careerStatistics.chaosScore,RaceChaosEarned);
        public int LifetimeObjectivesCompleted => careerStatistics.GoalsCompleted;
        public TruckTaxiCareerStatistics CaptureCareerStatistics() => careerStatistics.Copy();
        public TruckTaxiMainMenuStats CaptureMainMenuStats() =>
            CaptureLastSessionStatistics()?.ToMenuStats() ?? default;

        private static int IncrementCareerCount(int value) => value>=int.MaxValue-1 ? int.MaxValue-1 : value+1;
        private static long AddCareerCount(long value,long amount) => value>long.MaxValue-amount ? long.MaxValue : value+amount;
        private TruckTaxiCareerGoalStatistics CareerGoal(string id)
        {
            foreach(var goal in careerStatistics.goals) if(goal.id==id) return goal;
            var added=new TruckTaxiCareerGoalStatistics { id=id };
            careerStatistics.goals.Add(added); return added;
        }
        private void RecordCareerGoalOffer(TaxiRequestProgress request)
        {
            if(request?.Definition==null || !offeredCareerGoals.Add(request)) return;
            var goal=CareerGoal(request.Definition.StableId);
            goal.offered=IncrementCareerCount(goal.offered);
        }
        private void RecordCareerGoalAttempt(TaxiRequestProgress request)
        {
            RecordCareerGoalOffer(request);
            if(request?.Definition==null) return;
            var goal=CareerGoal(request.Definition.StableId);
            goal.attempted=IncrementCareerCount(goal.attempted);
        }
        public bool RecordCareerEvent(string stableEventId)
        {
            if(string.IsNullOrWhiteSpace(stableEventId) || stableEventId.Length>256) return false;
            foreach(var entry in careerStatistics.events)
                if(entry.id==stableEventId) { entry.count=AddCareerCount(entry.count,1); return true; }
            careerStatistics.events.Add(new TruckTaxiCareerEventCount { id=stableEventId,count=1 });
            return true;
        }
        private void RecordCareerDrivingEvent(TaxiEventType type)
        {
            string id;
            switch(type)
            {
                case TaxiEventType.Collision: id="taxi.driving.collision"; break;
                case TaxiEventType.TrafficRam: id="taxi.driving.traffic-ram"; break;
                case TaxiEventType.PedestrianHit: id="taxi.driving.pedestrian-hit"; break;
                case TaxiEventType.PropDamage: id="taxi.driving.property-damage"; break;
                case TaxiEventType.Shortcut: id="taxi.driving.shortcut"; break;
                case TaxiEventType.ScenicPoint: id="taxi.driving.scenic-point"; break;
                case TaxiEventType.NearMiss: id="taxi.driving.near-miss"; break;
                case TaxiEventType.HardLanding: id="taxi.driving.hard-landing"; break;
                default: return;
            }
            RecordCareerEvent(id);
            careerStatistics.chaosEvents=AddCareerCount(careerStatistics.chaosEvents,1);
        }
        // Call exactly once from the owning system's successful settlement, never from UI refresh.
        public void RecordCareerRaceCompleted() => careerStatistics.racesCompleted=IncrementCareerCount(careerStatistics.racesCompleted);
        public void RecordCareerStolenFareCompleted() => careerStatistics.stolenFaresCompleted=IncrementCareerCount(careerStatistics.stolenFaresCompleted);
        public bool RecordCareerToiletDiscovery(string stableToiletId)
        {
            if(string.IsNullOrWhiteSpace(stableToiletId) || stableToiletId.Length>256 ||
                careerStatistics.discoveredToiletIds.Contains(stableToiletId)) return false;
            careerStatistics.discoveredToiletIds.Add(stableToiletId); return true;
        }

        public TruckTaxiLastSessionStatistics CaptureLifetimeSummary() => new TruckTaxiLastSessionStatistics {
            rides=CompletedRides,totalStars=TotalStarsEarned,earningsCents=ShiftEarnings,chaos=LifetimeChaosScore,
            pedestriansHit=PedestriansHit,trafficCollisions=TrafficHits,passengersEjected=EjectedRides,
            jugEvents=(int)Math.Min(int.MaxValue-1L,(long)JugsSucceeded+JugsSpilled),bottlesThrown=ContainersThrown,
            specialEvents=SpecialStopsCompleted,objectivesCompleted=LifetimeObjectivesCompleted,playtimeSeconds=careerStatistics.playtimeSeconds
        };
        public TruckTaxiLastSessionStatistics CaptureLastSessionStatistics()
        {
            var total=CaptureLifetimeSummary();
            var result=new TruckTaxiLastSessionStatistics {
                rides=total.rides-statisticsBaseline.rides,totalStars=total.totalStars-statisticsBaseline.totalStars,
                earningsCents=total.earningsCents-statisticsBaseline.earningsCents,chaos=total.chaos-statisticsBaseline.chaos,
                pedestriansHit=total.pedestriansHit-statisticsBaseline.pedestriansHit,
                trafficCollisions=total.trafficCollisions-statisticsBaseline.trafficCollisions,
                passengersEjected=total.passengersEjected-statisticsBaseline.passengersEjected,
                jugEvents=total.jugEvents-statisticsBaseline.jugEvents,bottlesThrown=total.bottlesThrown-statisticsBaseline.bottlesThrown,
                specialEvents=total.specialEvents-statisticsBaseline.specialEvents,
                objectivesCompleted=total.objectivesCompleted-statisticsBaseline.objectivesCompleted,
                playtimeSeconds=total.playtimeSeconds-statisticsBaseline.playtimeSeconds
            };
            bool activity=result.playtimeSeconds>0 || result.rides>0 || result.earningsCents>0 || result.chaos>0 ||
                result.pedestriansHit>0 || result.trafficCollisions>0 || result.passengersEjected>0 || result.jugEvents>0 ||
                result.bottlesThrown>0 || result.specialEvents>0 || result.objectivesCompleted>0;
            return activity ? result : previousSessionStatistics?.Copy();
        }

        // A logical new play session, not every shift or autosave. Restore establishes this boundary itself.
        public void BeginStatisticsSession()
        {
            previousSessionStatistics=CaptureLastSessionStatistics();
            statisticsBaseline=CaptureLifetimeSummary();
        }

        public TruckTaxiPersistentState CapturePersistentState()
        {
            return new TruckTaxiPersistentState
            {
                shiftEarningsCents=ShiftEarnings,purchaseChargesCents=purchaseCharges,serviceChargesCents=serviceCharges,
                rideRequestsEnabled=RideRequestsEnabled,gameMinutes=gameMinutes,
                consecutiveManualDeclines=ConsecutiveManualDeclines,
                manualDeclineCooldownUntilGameMinutes=manualDeclineCooldownUntilGameMinutes,
                career=careerStatistics.Copy(),lastSession=CaptureLastSessionStatistics(),
                ownedProfile=CaptureCareerProfile(),
                morality=CaptureMoralityState(),
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
            snapshot=PrepareOptionalPersistentData(snapshot);
            if(snapshot==null || snapshot.version<1 || snapshot.version>2) return InvalidPersistentState("Missing snapshot or unsupported version.",out reason);
            if(snapshot.statistics==null || snapshot.workArea==null || snapshot.passengerSelection==null || snapshot.intercity==null)
                return InvalidPersistentState("Missing required semantic state.",out reason);
            if(snapshot.ownedProfile!=null && !snapshot.ownedProfile.Validate(out reason))
                return InvalidPersistentState(reason ?? "Invalid Career identity or ownership data.",out reason);
            if(snapshot.morality!=null && !snapshot.morality.Validate(snapshot.shiftEarningsCents,out reason)) return false;
            if(snapshot.version==2 && !ValidateCareerStatistics(snapshot,out reason)) return false;
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
            if(snapshot.version==2 && (snapshot.consecutiveManualDeclines<0 || snapshot.consecutiveManualDeclines>5 ||
                !double.IsFinite(snapshot.manualDeclineCooldownUntilGameMinutes) || snapshot.manualDeclineCooldownUntilGameMinutes<0 ||
                snapshot.manualDeclineCooldownUntilGameMinutes>snapshot.gameMinutes+10.0001 ||
                snapshot.manualDeclineCooldownUntilGameMinutes>snapshot.gameMinutes && snapshot.consecutiveManualDeclines!=5))
                return InvalidPersistentState("Invalid manual-decline cooldown.",out reason);
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

        private static bool ValidateCareerStatistics(TruckTaxiPersistentState snapshot,out string reason)
        {
            var c=snapshot.career;
            if(c==null || c.version!=1 || c.goals==null || c.events==null || c.discoveredToiletIds==null ||
                c.chaosScore<0 || c.chaosEvents<0 || !double.IsFinite(c.playtimeSeconds) || c.playtimeSeconds<0)
                return InvalidPersistentState("Missing or invalid Career statistics.",out reason);
            foreach(int count in new[] { c.localRides,c.intercityRides,c.cancelledRides,c.stolenFaresCompleted,c.racesCompleted,
                c.oneStar,c.twoStars,c.threeStars,c.fourStars,c.fiveStars })
                if(count<0 || count==int.MaxValue) return InvalidPersistentState("Invalid Career counter.",out reason);
            var s=snapshot.statistics;
            if((long)c.localRides+c.intercityRides>s.completedRides ||
                (long)c.oneStar+c.twoStars+c.threeStars+c.fourStars+c.fiveStars>s.completedRides ||
                c.oneStar+2L*c.twoStars+3L*c.threeStars+4L*c.fourStars+5L*c.fiveStars>s.totalStarsEarned ||
                c.cancelledRides>s.failedRides)
                return InvalidPersistentState("Career detail exceeds lifetime totals.",out reason);
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var goal in c.goals)
            {
                if(goal==null || !ValidCareerId(goal.id) || !ids.Add(goal.id) ||
                    goal.offered<0 || goal.attempted<0 || goal.completed<0 || goal.failed<0 ||
                    goal.offered==int.MaxValue || goal.attempted>goal.offered || (long)goal.completed+goal.failed>goal.attempted)
                    return InvalidPersistentState("Invalid or duplicate Career goal.",out reason);
            }
            ids.Clear();
            foreach(var entry in c.events)
                if(entry==null || !ValidCareerId(entry.id) || !ids.Add(entry.id) || entry.count<0)
                    return InvalidPersistentState("Invalid or duplicate Career event.",out reason);
            ids.Clear();
            foreach(var id in c.discoveredToiletIds)
                if(!ValidCareerId(id) || !ids.Add(id)) return InvalidPersistentState("Invalid Career toilet identity.",out reason);
            var last=snapshot.lastSession;
            if(last!=null)
            {
                foreach(int count in new[] { last.rides,last.totalStars,last.pedestriansHit,last.trafficCollisions,
                    last.passengersEjected,last.jugEvents,last.bottlesThrown,last.specialEvents,last.objectivesCompleted })
                    if(count<0) return InvalidPersistentState("Negative last-session counter.",out reason);
                if(last.rides>s.completedRides || last.totalStars<last.rides || last.totalStars>5L*last.rides ||
                    last.totalStars>s.totalStarsEarned || last.pedestriansHit>s.pedestriansHit ||
                    last.trafficCollisions>s.trafficHits || last.passengersEjected>s.ejectedRides ||
                    last.jugEvents>(long)s.jugsSucceeded+s.jugsSpilled || last.bottlesThrown>s.containersThrown ||
                    last.specialEvents>s.specialStopsCompleted || last.objectivesCompleted>c.GoalsCompleted ||
                    last.earningsCents<0 || last.earningsCents>snapshot.shiftEarningsCents || last.chaos<0 ||
                    last.chaos>AddCareerCount(c.chaosScore,Math.Max(0,s.raceChaosEarned)) ||
                    !double.IsFinite(last.playtimeSeconds) || last.playtimeSeconds<0 || last.playtimeSeconds>c.playtimeSeconds)
                    return InvalidPersistentState("Last-session statistics exceed lifetime totals.",out reason);
            }
            reason=null; return true;
        }
        private static bool ValidCareerId(string id) => !string.IsNullOrWhiteSpace(id) && id.Length<=256;

        // JsonUtility inline classes cannot reliably retain null. Compare against its exact
        // null encoding, not merely an empty ID: partially populated ownership must still fail.
        internal static TruckTaxiPersistentState PrepareOptionalPersistentData(TruckTaxiPersistentState snapshot)
        {
            if(snapshot==null) return null;
            var prepared=snapshot.Copy();
            var absent=JsonUtility.FromJson<OptionalPersistentData>(JsonUtility.ToJson(new OptionalPersistentData()));
            if(prepared.ownedProfile!=null && absent.ownedProfile!=null &&
                JsonUtility.ToJson(prepared.ownedProfile)==JsonUtility.ToJson(absent.ownedProfile))
                prepared.ownedProfile=null;
            if(prepared.morality!=null && absent.morality!=null &&
                JsonUtility.ToJson(prepared.morality)==JsonUtility.ToJson(absent.morality))
                prepared.morality=null;
            return prepared;
        }

        [Serializable]
        private sealed class OptionalPersistentData
        {
            public TruckTaxiCareerProfileData ownedProfile=null;
            public TruckTaxiMoralityState morality=null;
        }

        public bool TryRestorePersistentState(TruckTaxiPersistentState snapshot,out string reason)
            => TryRestorePreparedCareerRun(boundCareerRunProfileId,snapshot,out reason);

        // Explicit profile switch on the existing Session; the parent validates the outer save first.
        public bool TryRestoreCareerRun(string runProfileId,TruckTaxiPersistentState snapshot,out string reason)
        {
            if(!TruckTaxiCareerProfileData.ValidId(runProfileId))
                return InvalidPersistentState("Invalid target Career run profile.",out reason);
            return TryRestorePreparedCareerRun(runProfileId,snapshot,out reason);
        }

        private bool TryRestorePreparedCareerRun(string targetRunProfileId,TruckTaxiPersistentState snapshot,out string reason)
        {
            if(careerMutationInProgress) return InvalidPersistentState("A Career transaction is in progress.",out reason);
            // Detach before validation so caller-owned objects are never installed in live state.
            var prepared=PrepareOptionalPersistentData(snapshot);
            if(!ValidatePersistentState(prepared,out reason)) return false;
            if(prepared.ownedProfile!=null && targetRunProfileId!=null &&
                prepared.ownedProfile.runProfileId!=targetRunProfileId)
                return InvalidPersistentState("Saved Career belongs to another run profile.",out reason);
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
            consecutiveManualDeclines=prepared.version==1 ? 0 : prepared.consecutiveManualDeclines;
            manualDeclineCooldownUntilGameMinutes=prepared.version==1 ? 0 : prepared.manualDeclineCooldownUntilGameMinutes;
            RefreshManualDeclineCooldown();
            var stats=prepared.statistics;
            CompletedRides=stats.completedRides; FailedRides=stats.failedRides; EjectedRides=stats.ejectedRides; PickupCancellations=stats.pickupCancellations;
            TotalStarsEarned=stats.totalStarsEarned; ShiftScore=stats.shiftScore; TotalCollisions=stats.totalCollisions;
            TrafficHits=stats.trafficHits; PedestriansHit=stats.pedestriansHit; JugsSucceeded=stats.jugsSucceeded; JugsSpilled=stats.jugsSpilled;
            ContainersThrown=stats.containersThrown; SpecialStopsCompleted=stats.specialStopsCompleted;
            OffersDeclined=stats.offersDeclined; OffersExpired=stats.offersExpired; RacesEntered=stats.racesEntered;
            RaceWins=stats.raceWins; RacePodiums=stats.racePodiums; RaceChaosEarned=stats.raceChaosEarned;
            ThunderBowlBestLapSeconds=stats.thunderBowlBestLapSeconds; RacePrizeEarningsCents=stats.racePrizeEarningsCents;
            careerStatistics=prepared.version==1 ? new TruckTaxiCareerStatistics { historicalDetailsIncomplete=true } : prepared.career;
            ownedProfile=prepared.ownedProfile;
            morality=prepared.morality ?? new TruckTaxiMoralityState();
            presentedMoralChoice=null;
            boundCareerRunProfileId=targetRunProfileId;
            restoredWithoutCareerIdentity=ownedProfile==null;
            previousSessionStatistics=prepared.version==1 ? null : prepared.lastSession;
            statisticsBaseline=CaptureLifetimeSummary();
            MoralityChanged?.Invoke();
            CareerEquipmentChanged?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private void ResetPersistentRideTransients()
        {
            ResetRivalPassengerTheft();
            ResetRideBehavior(); offerSuppressors.Clear();
            Passenger=null; Pickup=Destination=null; Offer=null; CurrentRideId=null; LastFare=null;
            requests.Clear(); rideHistory.Clear(); continuity.Clear(); hitPedestrians.Clear(); LastPedestrianImpact=null;
            offeredCareerGoals.Clear();
            ElapsedRide=DistanceDriven=StateAge=ReactionAge=0; Satisfaction=4; Reaction="";
            chaos=requestClock=nextSpeedReaction=maximumRideAcceleration=0; hasPosition=false; lastPosition=Vector3.zero;
            TrackedCollisions=0; rideTrafficHits=ridePedestriansHit=rideSpecialStopsCompleted=rideJugsSucceeded=rideJugsSpilled=rideContainersThrown=0;
            MechanicFareAdjustment=0; SpecialAppreciationAccepted=IsRepeatPassenger=IsIntercityRide=stopStarted=false;
            OfferDuration=PickupElapsed=PickupDuration=0; OfferTimerPaused=false;
            AcceptedFareModifiers=default; DemandFareMultiplier=IntercityFareBonusMultiplier=1;
            AcceptedTownFareMultiplier=1;
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
