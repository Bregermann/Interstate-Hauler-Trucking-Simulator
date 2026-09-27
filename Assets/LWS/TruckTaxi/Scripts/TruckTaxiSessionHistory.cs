using System;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiRideOutcome { Completed, Failed, PickupCancelled, Ejected }
    public enum TruckTaxiDemandWeather { Neutral, Rain, Storm, Snow, HeavySnow, Blizzard }

    public sealed class TruckTaxiGoalResult
    {
        public TaxiRequestType Type { get; }
        public string Description { get; }
        public TaxiRequestState State { get; }
        public float Progress { get; }
        public float Target { get; }
        public string StopId { get; }
        public bool Expired { get; }
        public string FailureReason { get; }
        public long RewardCents { get; }
        public int RewardScore { get; }
        public TruckTaxiGoalResult(TaxiRequestProgress request)
        {
            Type=request.Definition.requestType; Description=request.Description; State=request.State;
            Progress=request.Progress; Target=request.Target; StopId=request.StopPoint?.stableId;
            Expired=request.Expired;
            FailureReason=request.FailureReason;
            RewardCents=State==TaxiRequestState.Succeeded ? request.Definition.bonusMoneyCents : 0;
            RewardScore=State==TaxiRequestState.Succeeded ? request.Definition.bonusScore : 0;
        }
    }

    public sealed class TruckTaxiFareResult
    {
        public long Base { get; }
        public long Distance { get; }
        public long Time { get; }
        public long Requests { get; }
        public long Chaos { get; }
        public long Tip { get; }
        public long Penalties { get; }
        public long Total { get; }
        public int Rating { get; }
        public int Score { get; }
        public TruckTaxiFareResult(TaxiFare fare)
        {
            if(fare==null) return;
            Base=fare.Base; Distance=fare.Distance; Time=fare.Time; Requests=fare.Requests;
            Chaos=fare.Chaos; Tip=fare.Tip; Penalties=fare.Penalties; Total=fare.Total;
            Rating=fare.Rating; Score=fare.Score;
        }
    }

    public sealed class TruckTaxiRideRecord
    {
        public string RideId { get; }
        public string PassengerId { get; }
        public string PickupId { get; }
        public string DestinationId { get; }
        public TruckTaxiRideOutcome Outcome { get; }
        public string FailureReason { get; }
        public double EndGameMinutes { get; }
        public bool RepeatPassenger { get; }
        public bool AppreciationAccepted { get; }
        public float RideSeconds { get; }
        public float DistanceMeters { get; }
        public int ChaosScore { get; }
        public int Collisions { get; }
        public int TrafficHits { get; }
        public int PedestriansHit { get; }
        public int SpecialStopsCompleted { get; }
        public int JugsSucceeded { get; }
        public int JugsSpilled { get; }
        public int ContainersThrown { get; }
        public TruckTaxiFareResult Fare { get; }
        public IReadOnlyList<TruckTaxiGoalResult> Goals { get; }
        internal TruckTaxiRideRecord(string rideId, string passengerId, string pickupId, string destinationId,
            TruckTaxiRideOutcome outcome, string failureReason, double endGameMinutes, bool repeatPassenger,
            bool appreciationAccepted, float rideSeconds, float distanceMeters, int chaosScore, int collisions,
            int pedestriansHit, int specialStopsCompleted, int jugsSucceeded, int jugsSpilled, int containersThrown, TaxiFare fare,
            IList<TaxiRequestProgress> requests, int trafficHits=0)
        {
            RideId=rideId; PassengerId=passengerId; PickupId=pickupId; DestinationId=destinationId;
            Outcome=outcome; FailureReason=failureReason; EndGameMinutes=endGameMinutes;
            RepeatPassenger=repeatPassenger; AppreciationAccepted=appreciationAccepted;
            RideSeconds=rideSeconds; DistanceMeters=distanceMeters; ChaosScore=chaosScore;
            Collisions=collisions; PedestriansHit=pedestriansHit; SpecialStopsCompleted=specialStopsCompleted;
            TrafficHits=trafficHits;
            JugsSucceeded=jugsSucceeded;
            JugsSpilled=jugsSpilled; ContainersThrown=containersThrown;
            Fare=fare==null ? null : new TruckTaxiFareResult(fare);
            var goals=new List<TruckTaxiGoalResult>(requests.Count);
            foreach(var request in requests) goals.Add(new TruckTaxiGoalResult(request));
            Goals=goals.AsReadOnly();
        }
    }
}
