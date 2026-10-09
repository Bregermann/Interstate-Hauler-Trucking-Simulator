using System;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    // Semantic data only: no live ride, Unity references, storage, or enum ordinals.
    [Serializable]
    public sealed class TruckTaxiPersistentState
    {
        public int version=2;
        public long shiftEarningsCents, purchaseChargesCents, serviceChargesCents;
        public bool rideRequestsEnabled=true;
        public double gameMinutes;
        public int consecutiveManualDeclines;
        public double manualDeclineCooldownUntilGameMinutes;
        public TruckTaxiPersistentStatistics statistics=new TruckTaxiPersistentStatistics();
        // Existing statistics/wallet remain lifetime authority. This contains only additional detail.
        public TruckTaxiCareerStatistics career=new TruckTaxiCareerStatistics();
        // Optional additive v2 data. Missing on v1/v2 legacy saves until explicitly bound by the parent.
        public TruckTaxiCareerProfileData ownedProfile;
        // Optional on legacy v1/v2 saves. No live trailer/context references are persisted.
        public TruckTaxiMoralityState morality;
        public TruckTaxiLastSessionStatistics lastSession;
        public TruckTaxiPersistentWorkArea workArea=new TruckTaxiPersistentWorkArea();
        public TruckTaxiPassengerSelectionState passengerSelection=new TruckTaxiPassengerSelectionState();
        public TruckTaxiIntercityState intercity=new TruckTaxiIntercityState();

        internal TruckTaxiPersistentState Copy()
        {
            var copy=(TruckTaxiPersistentState)MemberwiseClone();
            copy.statistics=statistics?.Copy(); copy.workArea=workArea?.Copy();
            copy.career=career?.Copy(); copy.lastSession=lastSession?.Copy();
            copy.ownedProfile=ownedProfile?.Copy();
            copy.morality=morality?.Copy();
            copy.intercity=intercity==null ? null : new TruckTaxiIntercityState { version=intercity.version,
                completedLocalStreak=intercity.completedLocalStreak,localCooldownRemaining=intercity.localCooldownRemaining };
            if(passengerSelection!=null)
            {
                copy.passengerSelection=new TruckTaxiPassengerSelectionState { version=passengerSelection.version,
                    offerIndex=passengerSelection.offerIndex,passengers=null,
                    recentArchetypes=passengerSelection.recentArchetypes==null ? null : new List<string>(passengerSelection.recentArchetypes) };
                if(passengerSelection.passengers!=null)
                {
                    copy.passengerSelection.passengers=new List<TruckTaxiPassengerExposure>(passengerSelection.passengers.Count);
                    foreach(var entry in passengerSelection.passengers) copy.passengerSelection.passengers.Add(entry?.Copy());
                }
            }
            return copy;
        }
    }

    [Serializable]
    public sealed class TruckTaxiCareerStatistics
    {
        public int version=1;
        public bool historicalDetailsIncomplete;
        public long chaosScore, chaosEvents;
        public int localRides, intercityRides, cancelledRides, stolenFaresCompleted, racesCompleted;
        public int oneStar, twoStars, threeStars, fourStars, fiveStars;
        public double playtimeSeconds;
        public List<TruckTaxiCareerGoalStatistics> goals=new List<TruckTaxiCareerGoalStatistics>();
        public List<TruckTaxiCareerEventCount> events=new List<TruckTaxiCareerEventCount>();
        public List<string> discoveredToiletIds=new List<string>();
        public int GoalsCompleted
        {
            get { long count=0; foreach(var goal in goals) count+=goal.completed; return (int)Math.Min(int.MaxValue-1L,count); }
        }
        internal TruckTaxiCareerStatistics Copy()
        {
            var copy=(TruckTaxiCareerStatistics)MemberwiseClone();
            copy.goals=goals==null ? null : goals.ConvertAll(g=>g?.Copy());
            copy.events=events==null ? null : events.ConvertAll(e=>e==null ? null : new TruckTaxiCareerEventCount { id=e.id,count=e.count });
            copy.discoveredToiletIds=discoveredToiletIds==null ? null : new List<string>(discoveredToiletIds);
            return copy;
        }
    }

    [Serializable]
    public sealed class TruckTaxiCareerGoalStatistics
    {
        public string id;
        public int offered, attempted, completed, failed;
        internal TruckTaxiCareerGoalStatistics Copy() => (TruckTaxiCareerGoalStatistics)MemberwiseClone();
    }

    [Serializable]
    public sealed class TruckTaxiCareerEventCount
    {
        public string id;
        public long count;
    }

    [Serializable]
    public sealed class TruckTaxiLastSessionStatistics
    {
        public int rides, totalStars, pedestriansHit, trafficCollisions, passengersEjected;
        public int jugEvents, bottlesThrown, specialEvents, objectivesCompleted;
        public long earningsCents, chaos;
        public double playtimeSeconds;
        internal TruckTaxiLastSessionStatistics Copy() => (TruckTaxiLastSessionStatistics)MemberwiseClone();
        public TruckTaxiMainMenuStats ToMenuStats() => new TruckTaxiMainMenuStats {
            rides=rides, earningsCents=earningsCents, averageStars=rides==0 ? 0 : (float)totalStars/rides,
            chaos=chaos, pedestriansHit=pedestriansHit, trafficCollisions=trafficCollisions,
            passengersEjected=passengersEjected,jugEvents=jugEvents,bottlesThrown=bottlesThrown,
            specialEvents=specialEvents,objectivesCompleted=objectivesCompleted
        };
    }

    [Serializable]
    public sealed class TruckTaxiPersistentStatistics
    {
        public int completedRides, failedRides, ejectedRides, pickupCancellations;
        public int totalStarsEarned, shiftScore, totalCollisions, trafficHits, pedestriansHit;
        public int jugsSucceeded, jugsSpilled, containersThrown, specialStopsCompleted;
        public int offersDeclined, offersExpired;
        public int racesEntered, raceWins, racePodiums, raceChaosEarned;
        public float thunderBowlBestLapSeconds;
        // Informational subset of earnings, never added to the wallet on restore.
        public long racePrizeEarningsCents;
        internal TruckTaxiPersistentStatistics Copy() => (TruckTaxiPersistentStatistics)MemberwiseClone();
    }

    [Serializable]
    public sealed class TruckTaxiPersistentWorkArea
    {
        public string modeId="nearby";
        public float centerX, centerY, centerZ, radius=500;
        public string label="ANYWHERE NEAR ME";
        internal TruckTaxiPersistentWorkArea Copy() => (TruckTaxiPersistentWorkArea)MemberwiseClone();
    }
}
