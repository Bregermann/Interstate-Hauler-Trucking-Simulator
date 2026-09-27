using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Wires existing authorities; it owns neither the clock, navigation, populations nor fares.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiWorldCoordinator : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiBusService[] buses;
        private float nextRefresh,notificationUntil,nextNotification;
        private DateTime lastObserved,rainStarted;
        private TruckTaxiVenueWeatherImpact weatherImpact;
        private readonly Queue<string> notifications=new Queue<string>();
        private readonly List<TruckTaxiRivalDemandZone> rivalZones=new List<TruckTaxiRivalDemandZone>();
        private readonly Dictionary<string,TruckTaxiMapMarker> markers=new Dictionary<string,TruckTaxiMapMarker>();
        private readonly HashSet<string> activeMarkerIds=new HashSet<string>();
        public TruckTaxiHazardZone[] HazardZones { get; private set; }=Array.Empty<TruckTaxiHazardZone>();
        public string Notification { get; private set; }="";
        public int VenueCrowdActors { get; private set; }
        public int BusActors { get {int n=0; foreach(var bus in buses??Array.Empty<TruckTaxiBusService>()) if(bus!=null) n+=bus.LiveBuses; return n;} }
        public void Initialize(TruckTaxiBootstrap value)
        {
            if(host!=null || value.CalendarWeather==null) return;
            host=value;
            host.Venues.Initialize(host,()=>host.CalendarWeather.CurrentDateTime);
            host.Extremes.Initialize(host,()=>host.CalendarWeather.CurrentDateTime,ApplyExtremeWeather);
            HazardZones=host.GetComponentsInChildren<TruckTaxiHazardZone>(true);
            foreach(var zone in HazardZones) host.Extremes.RegisterZone(zone);
            host.Extremes.WarningRaised+=OnWarning;
            host.Rivals.Initialize(host.traffic);
            host.Session.PickupWeight=host.Venues.GetWeightForPickup;
            host.Session.DestinationWeight=host.Venues.GetWeightForDestination;
            host.Session.RegionalDemandMultiplier=point=>host.Venues.GetDemand(point).RideFrequencyMultiplier;
            host.Session.FareModifiersProvider=(pickup,destination)=> {
                var fare=host.Venues.GetFareQuote(pickup,destination);
                return new TruckTaxiFareModifiers(fare.EventMultiplier,host.CalendarWeather.WeatherFareMultiplier,1,fare.EventLabel);
            };
            host.pedestrians.SetVenueFlow(VenueFlow);
            host.Rivals.SetDemandSampler(p=>host.Venues.GetDemand(p).PickupWeight);
            host.traffic.SetDemandSampler(p=>host.Venues.GetDemand(p).TrafficMultiplier);
            buses=FindObjectsByType<TruckTaxiBusService>(FindObjectsSortMode.None)
                .Where(bus=>bus.gameObject.scene==host.gameObject.scene).ToArray();
            foreach(var bus in buses) bus.SetVenueFlow(VenueFlow);
            foreach(var venue in host.Venues.Venues)
                host.GPS.MapMarkers.RegisterRegionalPoint(venue.id,venue.displayName,venue.type.ToString(),venue.mapPosition,
                    venue.type==TruckTaxiVenueType.Stadium ? TruckTaxiMapMarkerType.SportsStadium :
                    venue.type==TruckTaxiVenueType.ConcertArena ? TruckTaxiMapMarkerType.ConcertVenue : TruckTaxiMapMarkerType.Racetrack,true);
            lastObserved=host.CalendarWeather.CurrentDateTime;
        }
        private float VenueFlow(Vector3 point)
        {
            var demand=host.Venues.GetDemand(point);
            return demand.DestinationWeight>demand.PickupWeight ? demand.DestinationWeight-1 : -(demand.PickupWeight-1);
        }
        private void ApplyExtremeWeather(TruckTaxiExtremeWeatherProfile? profile,TimeSpan duration)
        {
            if(profile.HasValue) host.CalendarWeather.ForceSevereStorm((float)duration.TotalHours,profile.Value);
            else host.CalendarWeather.ClearSevereOverride();
        }
        private void OnWarning(TruckTaxiExtremeWarning warning)
        { if(notifications.Count<12) notifications.Enqueue(warning.Text); }
        private void Update()
        {
            if(host==null || Time.unscaledTime<nextRefresh) return;
            nextRefresh=Time.unscaledTime+1;
            var now=host.CalendarWeather.CurrentDateTime;
            var condition=host.CalendarWeather.CurrentCondition;
            bool rain=condition==TruckTaxiWeatherCondition.HeavyRain || condition==TruckTaxiWeatherCondition.Thunderstorm;
            if(!rain || now<lastObserved) rainStarted=default;
            else if(rainStarted==default) rainStarted=now;
            lastObserved=now;
            host.Extremes.SetWeatherObservation(condition==TruckTaxiWeatherCondition.Thunderstorm,
                condition==TruckTaxiWeatherCondition.HeavySnow || condition==TruckTaxiWeatherCondition.Blizzard,
                rainStarted==default ? 0 : (float)(now-rainStarted).TotalHours);
            var severity=host.CalendarWeather.CurrentSeverity;
            var nextImpact=severity==TruckTaxiWeatherSeverity.Emergency ? TruckTaxiVenueWeatherImpact.Cancel :
                severity==TruckTaxiWeatherSeverity.Warning ? TruckTaxiVenueWeatherImpact.Delay : TruckTaxiVenueWeatherImpact.None;
            if(nextImpact!=weatherImpact || nextImpact!=TruckTaxiVenueWeatherImpact.None)
            {weatherImpact=nextImpact; host.Venues.SetSevereWeather(nextImpact);}
            if(host.Venues.TryDequeueAlert(out var alert) && notifications.Count<12) notifications.Enqueue(alert.Title+"\n"+alert.Message);
            if(Time.unscaledTime>=notificationUntil) Notification="";
            if(!host.Paused && Time.unscaledTime>=nextNotification && notifications.Count>0)
            {Notification=notifications.Dequeue(); notificationUntil=Time.unscaledTime+8; nextNotification=Time.unscaledTime+12;}
            RefreshWorldPresentation();
        }
        private void RefreshWorldPresentation()
        {
            rivalZones.Clear(); activeMarkerIds.Clear(); VenueCrowdActors=0;
            foreach(var venue in host.Venues.Venues)
            {
                var demand=host.Venues.GetDemand(venue.worldPosition);
                if(demand.RideFrequencyMultiplier>1.15f && (demand.PickupWeight>1 || demand.DestinationWeight>1))
                {
                    rivalZones.Add(new TruckTaxiRivalDemandZone(venue.id,venue.worldPosition,venue.influenceRadiusMeters,
                        Mathf.Clamp01(demand.RideFrequencyMultiplier-1)));
                    UpdateMarker("surge:"+venue.id,venue.displayName+" SURGE",TruckTaxiMapMarkerType.Surge,venue.mapPosition);
                }
                if(demand.CrowdMultiplier>1)
                    foreach(var person in host.pedestrians.People)
                        if(person!=null && (person.transform.position-venue.worldPosition).sqrMagnitude<venue.influenceRadiusMeters*venue.influenceRadiusMeters) VenueCrowdActors++;
            }
            host.Rivals.SetDemandZones(rivalZones);
            host.Extremes.BindCamera(Camera.main!=null ? Camera.main.transform : null);
            foreach(var hazard in host.Extremes.ActiveSnapshots)
            {
                UpdateMarker("hazard:"+hazard.ZoneId,hazard.Kind+(hazard.RoadHazard ? " - ROAD HAZARD" : " WARNING"),TruckTaxiMapMarkerType.Hazard,hazard.Position);
                if(!hazard.LoadedNearby || host.Paused) continue;
                host.pedestrians.ReactToHazard(hazard.Position,hazard.RadiusMeters,12);
                int count=0;
                foreach(var car in host.traffic.Vehicles)
                {
                    if(car==null || (car.transform.position-hazard.Position).sqrMagnitude>hazard.RadiusMeters*hazard.RadiusMeters) continue;
                    car.GetComponent<TruckTaxiTrafficBehaviour>()?.ReactToHazard(2);
                    if(++count>=12) break;
                }
            }
            foreach(var pair in markers) if(!activeMarkerIds.Contains(pair.Key)) pair.Value.state=TruckTaxiMapMarkerState.Hidden;
            host.GPS.MapMarkers.RequestRefresh();
        }
        private void UpdateMarker(string id,string label,TruckTaxiMapMarkerType type,Vector3 position)
        {
            activeMarkerIds.Add(id);
            if(!markers.TryGetValue(id,out var marker))
            {
                var go=new GameObject(label); go.transform.SetParent(transform,false);
                marker=go.AddComponent<TruckTaxiMapMarker>(); marker.stableId=id; marker.label=label; marker.markerType=type;
                markers.Add(id,marker); host.GPS.MapMarkers.Register(marker);
            }
            marker.transform.position=position; marker.state=TruckTaxiMapMarkerState.Active;
        }
        private void OnDestroy() {if(host?.Extremes!=null) host.Extremes.WarningRaised-=OnWarning;}
    }
}
