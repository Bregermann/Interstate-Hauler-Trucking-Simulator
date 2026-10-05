using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Capability semantics only. ServicePoint remains the authority for bay position, radius and availability.
    public static class TruckTaxiServicePresentation
    {
        public const TruckTaxiServiceCapability Repairs=TruckTaxiServiceCapability.RepairEngine |
            TruckTaxiServiceCapability.RepairTransmission | TruckTaxiServiceCapability.RepairGeneralDamage |
            TruckTaxiServiceCapability.RepairTires | TruckTaxiServiceCapability.RecoverVehicle;
        public const TruckTaxiServiceCapability Advertised=TruckTaxiServiceCapability.Refuel | Repairs |
            TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink | TruckTaxiServiceCapability.Store |
            TruckTaxiServiceCapability.Restroom | TruckTaxiServiceCapability.Sleep;
        public static TruckTaxiServicePoint Find(string id)
        {
            if(string.IsNullOrWhiteSpace(id)) return null;
            foreach(var point in TruckTaxiServicePoint.Points)
                if(point!=null && point.isActiveAndEnabled && point.stableId==id) return point;
            return null;
        }
        public static bool IsUsable(TruckTaxiServicePoint point) => point!=null && point.isActiveAndEnabled &&
            point.gameObject.scene.isLoaded && !string.IsNullOrWhiteSpace(point.stableId) &&
            (point.capabilities & Advertised)!=0 && float.IsFinite(point.InteractionRadius) && point.InteractionRadius>0;
        public static bool RequiresCapability(TruckTaxiMapMarkerType type) =>
            type==TruckTaxiMapMarkerType.FoodStop || type==TruckTaxiMapMarkerType.Drink ||
            type==TruckTaxiMapMarkerType.Bathroom || type==TruckTaxiMapMarkerType.Store ||
            type==TruckTaxiMapMarkerType.Gas || type==TruckTaxiMapMarkerType.Repair ||
            type==TruckTaxiMapMarkerType.Sleep || type==TruckTaxiMapMarkerType.ServiceArea;
        public static bool Matches(TruckTaxiServicePoint point,TruckTaxiMapMarkerType type)
        {
            if(!RequiresCapability(type)) return true;
            if(!IsUsable(point)) return false;
            switch(type)
            {
                case TruckTaxiMapMarkerType.FoodStop: return point.Supports(TruckTaxiServiceCapability.Food);
                case TruckTaxiMapMarkerType.Drink: return point.Supports(TruckTaxiServiceCapability.Drink);
                case TruckTaxiMapMarkerType.Bathroom: return point.Supports(TruckTaxiServiceCapability.Restroom);
                case TruckTaxiMapMarkerType.Store: return point.Supports(TruckTaxiServiceCapability.Store);
                case TruckTaxiMapMarkerType.Gas: return point.Supports(TruckTaxiServiceCapability.Refuel);
                case TruckTaxiMapMarkerType.Repair: return (point.capabilities & Repairs)!=0;
                case TruckTaxiMapMarkerType.Sleep: return point.Supports(TruckTaxiServiceCapability.Sleep);
                default: return true;
            }
        }
        public static TruckTaxiMapMarkerType MarkerType(TruckTaxiServiceCapability capabilities)
        {
            TruckTaxiMapMarkerType result=TruckTaxiMapMarkerType.Place;
            int count=0;
            Add(capabilities,TruckTaxiServiceCapability.Refuel,TruckTaxiMapMarkerType.Gas,ref result,ref count);
            Add(capabilities,Repairs,TruckTaxiMapMarkerType.Repair,ref result,ref count);
            Add(capabilities,TruckTaxiServiceCapability.Food,TruckTaxiMapMarkerType.FoodStop,ref result,ref count);
            Add(capabilities,TruckTaxiServiceCapability.Drink,TruckTaxiMapMarkerType.Drink,ref result,ref count);
            Add(capabilities,TruckTaxiServiceCapability.Store,TruckTaxiMapMarkerType.Store,ref result,ref count);
            Add(capabilities,TruckTaxiServiceCapability.Restroom,TruckTaxiMapMarkerType.Bathroom,ref result,ref count);
            Add(capabilities,TruckTaxiServiceCapability.Sleep,TruckTaxiMapMarkerType.Sleep,ref result,ref count);
            return count>1 ? TruckTaxiMapMarkerType.ServiceArea : result;
        }
        private static void Add(TruckTaxiServiceCapability actual,TruckTaxiServiceCapability required,
            TruckTaxiMapMarkerType type,ref TruckTaxiMapMarkerType result,ref int count)
        { if((actual & required)!=0) { result=type; count++; } }
        public static string Label(TruckTaxiServiceCapability capabilities)
        {
            var labels=new List<string>();
            if((capabilities & TruckTaxiServiceCapability.Refuel)!=0) labels.Add("GAS");
            if((capabilities & Repairs)!=0) labels.Add("REPAIR");
            if((capabilities & TruckTaxiServiceCapability.Food)!=0) labels.Add("FOOD");
            if((capabilities & TruckTaxiServiceCapability.Drink)!=0) labels.Add("DRINK");
            if((capabilities & TruckTaxiServiceCapability.Store)!=0) labels.Add("STORE");
            if((capabilities & TruckTaxiServiceCapability.Restroom)!=0) labels.Add("RESTROOM");
            if((capabilities & TruckTaxiServiceCapability.Sleep)!=0) labels.Add("SLEEP");
            return string.Join(" / ",labels);
        }
    }
}
