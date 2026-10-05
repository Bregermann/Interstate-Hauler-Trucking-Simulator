using System;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    // Semantic data only: no live ride, Unity references, storage, or enum ordinals.
    [Serializable]
    public sealed class TruckTaxiPersistentState
    {
        public int version=1;
        public long shiftEarningsCents, purchaseChargesCents, serviceChargesCents;
        public bool rideRequestsEnabled=true;
        public double gameMinutes;
        public TruckTaxiPersistentStatistics statistics=new TruckTaxiPersistentStatistics();
        public TruckTaxiPersistentWorkArea workArea=new TruckTaxiPersistentWorkArea();
        public TruckTaxiPassengerSelectionState passengerSelection=new TruckTaxiPassengerSelectionState();
        public TruckTaxiIntercityState intercity=new TruckTaxiIntercityState();

        internal TruckTaxiPersistentState Copy()
        {
            var copy=(TruckTaxiPersistentState)MemberwiseClone();
            copy.statistics=statistics?.Copy(); copy.workArea=workArea?.Copy();
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
