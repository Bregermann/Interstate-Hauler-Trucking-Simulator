using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiStopRingState { Ambient, Nearby, Selected, InsideTooFast, Valid, Interacting, Unavailable }

    [Serializable]
    public sealed class TruckTaxiStopRingPalette
    {
        public Color gas=new Color(1,.78f,.12f), food=new Color(.25f,1,.35f), drink=Color.cyan;
        public Color store=new Color(.2f,.5f,1), restroom=new Color(.45f,.85f,1), sleep=new Color(.7f,.4f,1);
        public Color repair=new Color(1,.35f,.12f), scenic=new Color(1,.55f,.8f), sketchy=new Color(1,.15f,.65f);
        public Color pickup=new Color(.45f,1,.9f), dropoff=new Color(.25f,1,.3f), privateStop=new Color(.85f,.15f,.5f);
        public Color venue=new Color(1,.82f,.3f), combined=new Color(.8f,.95f,1), place=new Color(.8f,.85f,.9f);
        public Color For(TruckTaxiMapMarkerType type)
        {
            switch(type)
            {
                case TruckTaxiMapMarkerType.Gas: return gas;
                case TruckTaxiMapMarkerType.FoodStop: return food;
                case TruckTaxiMapMarkerType.Drink: return drink;
                case TruckTaxiMapMarkerType.Store: return store;
                case TruckTaxiMapMarkerType.Bathroom: return restroom;
                case TruckTaxiMapMarkerType.Sleep: return sleep;
                case TruckTaxiMapMarkerType.Repair: return repair;
                case TruckTaxiMapMarkerType.ScenicStop: case TruckTaxiMapMarkerType.PhotoStop: return scenic;
                case TruckTaxiMapMarkerType.IllicitStop: return sketchy;
                case TruckTaxiMapMarkerType.PassengerPickup: return pickup;
                case TruckTaxiMapMarkerType.Destination: case TruckTaxiMapMarkerType.Dropoff: return dropoff;
                case TruckTaxiMapMarkerType.PrivateEventStop: return privateStop;
                case TruckTaxiMapMarkerType.ServiceArea: return combined;
                case TruckTaxiMapMarkerType.Place: return place;
                default: return venue;
            }
        }
        public Color Tint(TruckTaxiMapMarkerType type,TruckTaxiStopRingState state)
        {
            Color color=For(type);
            if(state==TruckTaxiStopRingState.Unavailable) return new Color(.5f,.5f,.5f,.2f);
            if(state==TruckTaxiStopRingState.InsideTooFast) color=Color.Lerp(color,Color.red,.65f);
            if(state==TruckTaxiStopRingState.Interacting) color=Color.Lerp(color,Color.white,.5f);
            color.a=state==TruckTaxiStopRingState.Ambient ? .25f : state==TruckTaxiStopRingState.Nearby ? .5f : 1;
            return color;
        }
    }
}
