using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        public float OnboardPatience { get; private set; } = 1;
        public TaxiRequestProgress PendingDiversion { get; private set; }
        public bool SafeDropRequested { get; private set; }
        public TruckTaxiRideLocation SafeDropDestination { get; private set; }
        public TruckTaxiRideLocation CurrentDesiredDestination => SafeDropRequested ? SafeDropDestination : Destination;
        public Func<bool> IsLegitimateTrafficOrServiceStop { get; set; }
        public Func<TaxiRequestProgress,bool> StopPresentationReady { get; set; }
        public event Action<TaxiRequestProgress> DiversionOffered;
        public event Action<TruckTaxiDialogueCategory> BehaviorReaction;
        private readonly HashSet<object> pickupWaitSuppressors=new HashSet<object>();
        public bool PickupPatienceSuspended => pickupWaitSuppressors.Count>0;
        private float progressClock,wrongWaySeconds,unexplainedStopSeconds,lastRoutedRemaining=-1,lastBehaviorBark;
        private string progressTargetId,cancellationReason;
        private Vector3 progressSamplePosition;
        private bool hasProgressSample;

        public void SetPickupPatienceSuspended(object owner,bool suspended)
        {
            if(owner==null) return;
            if(suspended) pickupWaitSuppressors.Add(owner); else pickupWaitSuppressors.Remove(owner);
        }
        private void ResetRideBehavior()
        {
            OnboardPatience=1; SafeDropRequested=false; SafeDropDestination=null; PendingDiversion=null;
            pickupWaitSuppressors.Clear(); progressClock=wrongWaySeconds=unexplainedStopSeconds=0;
            lastRoutedRemaining=-1; progressTargetId=null; cancellationReason=null; hasProgressSample=false; lastBehaviorBark=-60;
        }
        public bool AcceptDiversion()
        {
            if(PendingDiversion==null || State!=TruckTaxiState.DrivingToDestination || SafeDropRequested) return false;
            var request=PendingDiversion; PendingDiversion=null;
            if(request.StopPoint==null || !request.StopPoint.isActiveAndEnabled) return false;
            requests.Add(request); RequestCreated?.Invoke(request); BehaviorReaction?.Invoke(TruckTaxiDialogueCategory.DiversionAccepted);
            React(request.Description); Changed?.Invoke(); return true;
        }
        public bool DeclineDiversion()
        {
            if(PendingDiversion==null) return false;
            PendingDiversion=null; BehaviorReaction?.Invoke(TruckTaxiDialogueCategory.DiversionDeclined);
            Changed?.Invoke(); return true;
        }
        public void AdjustOnboardPatience(float amount)
        {
            if(!HasPassenger || !float.IsFinite(amount)) return;
            OnboardPatience=Mathf.Clamp01(OnboardPatience+amount);
        }
        private void TickOnboardPatience(float dt,Vector3 position,float speed,float acceleration,bool onRoad)
        {
            if(SafeDropRequested) return;
            progressClock+=dt;
            if(progressClock<Mathf.Max(1,config.patienceProgressSampleSeconds)) return;
            float sampleSeconds=progressClock; progressClock=0;
            var stop=ActiveStop?.StopPoint;
            Vector3 target=stop!=null ? stop.Position : Destination.StopPosition;
            string targetId=stop!=null ? stop.stableId : Destination.locationId;
            var leg=routeDistances.Measure(position,target);
            // Unavailable graph data is not evidence that the driver is behaving badly.
            if(requireNavigableRoute && !leg.Navigable) { hasProgressSample=false; return; }
            bool changed=progressTargetId!=targetId || !hasProgressSample;
            float improvement=changed ? 0 : lastRoutedRemaining-leg.Meters;
            float moved=hasProgressSample ? Vector3.Distance(position,progressSamplePosition) : 0;
            progressTargetId=targetId; lastRoutedRemaining=leg.Meters; progressSamplePosition=position; hasProgressSample=true;
            bool legitimate=(stop!=null && Vector3.Distance(position,stop.Position)<=stop.radius+15) ||
                Destination.Contains(position) || IsLegitimateTrafficOrServiceStop?.Invoke()==true;
            if(changed || legitimate)
            { wrongWaySeconds=unexplainedStopSeconds=0; return; }
            bool goodProgress=improvement>2 && speed>1;
            bool away=improvement < -Mathf.Max(8,moved*.2f);
            wrongWaySeconds=away ? wrongWaySeconds+sampleSeconds : Mathf.Max(0,wrongWaySeconds-sampleSeconds);
            unexplainedStopSeconds=speed<1 ? unexplainedStopSeconds+sampleSeconds : 0;
            float drain=0;
            if(wrongWaySeconds>config.wrongWayGraceSeconds)
            { drain+=sampleSeconds; BarkBehavior(TruckTaxiDialogueCategory.WrongWayReaction); }
            if(unexplainedStopSeconds>config.unexplainedStopGraceSeconds)
            { drain+=sampleSeconds; BarkBehavior(TruckTaxiDialogueCategory.StoppedTooLong); }
            if(!onRoad && speed>3 && Passenger.chaosAffinity<=0) drain+=sampleSeconds*.5f;
            if(speed>config.fastDrivingSpeed*1.6f && Passenger.chaosAffinity<0) drain+=sampleSeconds*.25f;
            if(drain>0) AdjustOnboardPatience(-drain/Mathf.Clamp(Passenger.basePatience,60,600));
            else if(goodProgress)
            {
                AdjustOnboardPatience(config.patienceRecoveryPerSecond*sampleSeconds);
                if(ElapsedRide>45) BarkBehavior(TruckTaxiDialogueCategory.GoodProgress);
            }
            if(OnboardPatience<=0) RequestSafeDrop("Passenger requested a safe drop after losing patience.");
        }
        private void BarkBehavior(TruckTaxiDialogueCategory category)
        {
            if(ElapsedRide-lastBehaviorBark<45) return;
            lastBehaviorBark=ElapsedRide; BehaviorReaction?.Invoke(category);
        }
        public bool RequestSafeDrop(string reason)
        {
            if(!HasPassenger || SafeDropRequested) return false;
            SafeDropRequested=true; cancellationReason=reason; PendingDiversion=null;
            foreach(var request in requests) if(request.State==TaxiRequestState.Active) Resolve(request,false,"SAFE DROP REQUESTED");
            var candidates=locations.FindAll(l=>l!=null && l.isActiveAndEnabled && l.dropoffAllowed);
            var origin=playerPosition();
            candidates.Sort((a,b)=>Vector3.SqrMagnitude(a.StopPosition-origin).CompareTo(Vector3.SqrMagnitude(b.StopPosition-origin)));
            // Existing authored dropoff bays are safe semantic targets; never invent a roadside coordinate.
            float best=float.MaxValue;
            for(int i=0;i<Mathf.Min(4,candidates.Count);i++)
            {
                var leg=routeDistances.Measure(origin,candidates[i].StopPosition);
                if((!requireNavigableRoute || leg.Navigable) && leg.Meters<best)
                { best=leg.Meters; SafeDropDestination=candidates[i]; }
            }
            if(SafeDropDestination==null) SafeDropDestination=Destination;
            React("SAFE DROP REQUESTED: "+SafeDropDestination?.locationName);
            DestinationChanged?.Invoke(); Changed?.Invoke(); return true;
        }
        private void CompleteCancellation(string reason)
        {
            PendingDiversion=null;
            foreach(var request in requests) if(request.State==TaxiRequestState.Active) Resolve(request,false,"PASSENGER CANCELLED");
            LastFare=EstimateFare(); LastFare.IsCancellation=true; LastFare.Base=Math.Max(0,config.cancellationFeeCents); LastFare.Tip=0;
            // Already-earned goals and diversions cannot be erased by the abort penalty.
            LastFare.Penalties=Math.Min(LastFare.Penalties,LastFare.Base+LastFare.Distance+LastFare.Time+LastFare.Chaos);
            ShiftEarnings+=LastFare.Total; ShiftScore+=LastFare.Score; FailedRides++;
            if(IsIntercityRide && DistanceDriven>1000) localOffersRemaining=Mathf.Clamp(config.localOffersAfterIntercity,0,10);
            var drop=CurrentDesiredDestination;
            if(Passenger!=null && drop!=null && !string.IsNullOrWhiteSpace(Passenger.passengerId))
                continuity[Passenger.passengerId]=new PassengerContinuity { LastRideLocationId=drop.Contains(playerPosition()) ? drop.locationId : null,
                    LastDropoffPosition=playerPosition(),LastKnownDistrict=drop.district,LastDropoffMinutes=gameMinutes };
            RecordRide(TruckTaxiRideOutcome.PassengerCancelled,reason);
            React("RIDE ABORTED - PASSENGER CANCELLED");
            SetState(TruckTaxiState.RideComplete);
        }
    }
}
