using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        private TruckTaxiRideWorkArea workArea = new TruckTaxiRideWorkArea();
        private TruckTaxiIntercityPolicy intercityPolicy;
        public TruckTaxiRideWorkArea WorkArea => workArea.Copy();
        public TruckTaxiIntercityPolicy IntercityPolicy => intercityPolicy ??
            (intercityPolicy=new TruckTaxiIntercityPolicy(config.intercityRideChance,config.localOffersAfterIntercity,config.intercityDryStreakThreshold));
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
            if(State==TruckTaxiState.RideOffered && Pickup!=null && !workArea.Contains(Pickup.StopPosition)) DeclineRide();
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
            DemandFareMultiplier=1+(AcceptedFareModifiers.EventMultiplier-1)+(AcceptedFareModifiers.WeatherMultiplier-1)+
                (AcceptedFareModifiers.OtherMultiplier-1)+(IntercityFareBonusMultiplier-1);
        }
        private void ApplyFareBonuses(TaxiFare fare)
        {
            long basis=fare.Base+fare.Distance+fare.Time;
            fare.EventBonus=(long)Math.Round(basis*(AcceptedFareModifiers.EventMultiplier-1));
            fare.WeatherBonus=(long)Math.Round(basis*(AcceptedFareModifiers.WeatherMultiplier-1));
            fare.OtherBonus=(long)Math.Round(basis*(AcceptedFareModifiers.OtherMultiplier-1));
            fare.IntercityBonus=(long)Math.Round(basis*(IntercityFareBonusMultiplier-1));
        }
        private static float ValidOfferWeight(float value) => float.IsFinite(value) ? Mathf.Clamp(value,.01f,20) : 1;
    }
}
