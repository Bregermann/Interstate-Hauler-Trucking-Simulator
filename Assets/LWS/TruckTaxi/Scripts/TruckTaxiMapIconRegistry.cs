using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiMapMarkerType
    {
        Player, PassengerPickup, Destination, ActiveRoute, ActiveObjective, ScenicStop,
        IllicitStop, PrivateEventStop, Shortcut, DiscoveredShortcut, TargetVehicle,
        Collectible, Dropoff, SpecialEvent, Danger, Debug, FoodStop, PhotoStop, Bathroom,
        Store, Gas, Repair, TrainStation, BusTerminal, Racetrack, ServiceArea,
        SportsStadium, ConcertVenue, Surge, Hazard, RoadClosure, WorkArea
    }
    public enum TruckTaxiMapMarkerState { Hidden, Known, Optional, Active, Completed }

    [CreateAssetMenu(menuName="Truck Taxi/Map Icon Registry")]
    public sealed class TruckTaxiMapIconRegistry : ScriptableObject
    {
        public const string ResourcePath="TruckTaxi/TruckTaxiMapIcons";
        [Serializable]
        public sealed class Entry
        {
            public TruckTaxiMapMarkerType type;
            public Sprite icon;
            public string label;
            [TextArea] public string explanation;
            public Color color=Color.white;
            [Range(.5f,3)] public float scale=1;
        }
        public Entry[] entries=Array.Empty<Entry>();
        [Range(.1f,1)] public float knownBrightness=.75f;
        [Range(.1f,1)] public float completedBrightness=.45f;
        [Range(1,2)] public float activeScale=1.3f;
        [Min(20)] public float nearbyPointRange=350;
        [Range(1,24)] public int maximumNearbyPoints=10;
        [Tooltip("Undiscovered shortcut entries use the plain shortcut glyph. Discovery adds a check mark.")]
        public bool showUndiscoveredShortcuts=true;
        public bool showDebugPoints;
        public Entry Find(TruckTaxiMapMarkerType type)
        { foreach(var entry in entries) if(entry!=null && entry.type==type) return entry; return null; }
        public System.Collections.Generic.IEnumerable<Entry> PlayerLegend()
        {
            foreach(var entry in entries)
                if(entry!=null && entry.icon!=null && entry.type!=TruckTaxiMapMarkerType.Debug &&
                   entry.type!=TruckTaxiMapMarkerType.ActiveRoute && !string.IsNullOrWhiteSpace(entry.label)) yield return entry;
        }
        public Color Tint(TruckTaxiMapMarkerType type,TruckTaxiMapMarkerState state)
        {
            Color color=Find(type)?.color ?? Color.white;
            if(state==TruckTaxiMapMarkerState.Completed)
            { float gray=color.grayscale; color=Color.Lerp(color,new Color(gray,gray,gray,color.a),.85f); color*=completedBrightness; }
            else if(state==TruckTaxiMapMarkerState.Known || state==TruckTaxiMapMarkerState.Optional) color*=knownBrightness;
            color.a=state==TruckTaxiMapMarkerState.Hidden ? 0 : 1;
            return color;
        }
        public float Scale(TruckTaxiMapMarkerType type,TruckTaxiMapMarkerState state) =>
            (Find(type)?.scale ?? 1)*(state==TruckTaxiMapMarkerState.Active ? activeScale : state==TruckTaxiMapMarkerState.Completed ? .8f : 1);
        public static TruckTaxiMapMarkerType ForStop(TruckTaxiStopCategory category)
        {
            switch(category)
            {
                case TruckTaxiStopCategory.Scenic: return TruckTaxiMapMarkerType.ScenicStop;
                case TruckTaxiStopCategory.IllicitPickup: return TruckTaxiMapMarkerType.IllicitStop;
                case TruckTaxiStopCategory.PrivateMeeting: return TruckTaxiMapMarkerType.PrivateEventStop;
                case TruckTaxiStopCategory.FoodStop: return TruckTaxiMapMarkerType.FoodStop;
                case TruckTaxiStopCategory.Racetrack: return TruckTaxiMapMarkerType.Racetrack;
                case TruckTaxiStopCategory.PhotoStop: return TruckTaxiMapMarkerType.PhotoStop;
                case TruckTaxiStopCategory.CollectionStop: return TruckTaxiMapMarkerType.Collectible;
                default: return TruckTaxiMapMarkerType.SpecialEvent;
            }
        }
    }
}
