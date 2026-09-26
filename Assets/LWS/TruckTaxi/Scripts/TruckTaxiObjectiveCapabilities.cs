using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Flags]
    public enum TruckTaxiObjectiveCapability
    {
        None=0, Timer=1, Chaos=2, Traffic=4, Pedestrians=8, PedestrianHitDetection=16,
        DestructibleProps=32, Shortcuts=64, Offroad=128, NearMiss=256, ScenicStops=512,
        IllicitStops=1024, PrivateStops=2048, TargetVehicles=4096, Pursuit=8192,
        VehicleDestruction=16384, Collectibles=32768, DestinationChange=65536
    }

    // Runtime evidence, not a list of installed assets. Rebuilt at scene initialization or explicitly in debug.
    public sealed class TruckTaxiObjectiveCapabilities
    {
        public TruckTaxiObjectiveCapability Available { get; private set; }
        public readonly List<TruckTaxiStopObjectivePoint> Stops = new List<TruckTaxiStopObjectivePoint>();
        private readonly Dictionary<TruckTaxiObjectiveCapability,int> counts = new Dictionary<TruckTaxiObjectiveCapability,int>();
        public int Count(TruckTaxiObjectiveCapability capability) => counts.TryGetValue(capability,out int count) ? count : 0;
        public void Register(TruckTaxiObjectiveCapability capability,int count)
        { counts[capability]=count; if(count>0) Available|=capability; else Available&=~capability; }
        public static TruckTaxiObjectiveCapability Required(TaxiRequestType type)
        {
            switch(type)
            {
                case TaxiRequestType.FastDelivery: case TaxiRequestType.SmoothRide: case TaxiRequestType.NoCollisions: return TruckTaxiObjectiveCapability.Timer;
                case TaxiRequestType.Shortcut: return TruckTaxiObjectiveCapability.Shortcuts;
                case TaxiRequestType.RamTraffic: return TruckTaxiObjectiveCapability.Traffic;
                case TaxiRequestType.HitPedestrian: return TruckTaxiObjectiveCapability.Pedestrians|TruckTaxiObjectiveCapability.PedestrianHitDetection;
                case TaxiRequestType.PropertyDamage: return TruckTaxiObjectiveCapability.DestructibleProps;
                case TaxiRequestType.Offroad: return TruckTaxiObjectiveCapability.Offroad;
                case TaxiRequestType.MaximumChaos: return TruckTaxiObjectiveCapability.Chaos;
                case TaxiRequestType.ScenicRoute: return TruckTaxiObjectiveCapability.ScenicStops;
                case TaxiRequestType.IllicitStop: return TruckTaxiObjectiveCapability.IllicitStops;
                case TaxiRequestType.NearMiss: return TruckTaxiObjectiveCapability.Traffic|TruckTaxiObjectiveCapability.NearMiss;
                default: return (TruckTaxiObjectiveCapability)int.MaxValue;
            }
        }
        public bool Supports(PassengerRequestDefinition definition) => definition!=null && definition.enabledForSelection &&
            (Available & definition.RequiredCapabilities)==definition.RequiredCapabilities;
        public int TargetLimit(TaxiRequestType type)
        {
            switch(type)
            {
                case TaxiRequestType.RamTraffic: case TaxiRequestType.NearMiss: return Count(TruckTaxiObjectiveCapability.Traffic);
                case TaxiRequestType.HitPedestrian: return Count(TruckTaxiObjectiveCapability.Pedestrians);
                case TaxiRequestType.PropertyDamage: return Count(TruckTaxiObjectiveCapability.DestructibleProps);
                case TaxiRequestType.Shortcut: return Count(TruckTaxiObjectiveCapability.Shortcuts);
                default: return int.MaxValue;
            }
        }
        public TruckTaxiStopObjectivePoint FindStop(PassengerRequestDefinition definition,PassengerProfile passenger,Vector3 origin)
        {
            var category=definition.requestType==TaxiRequestType.ScenicRoute ? TruckTaxiStopCategory.Scenic : TruckTaxiStopCategory.IllicitPickup;
            TruckTaxiStopObjectivePoint best=null; float distance=float.MaxValue;
            foreach(var stop in Stops)
            {
                if(stop==null || !stop.isActiveAndEnabled || stop.category!=category || !stop.Allows(passenger) ||
                    (!string.IsNullOrEmpty(definition.targetId) && definition.targetId!=stop.stableId)) continue;
                float candidate=(origin-stop.Position).sqrMagnitude;
                if(candidate<distance) { distance=candidate; best=stop; }
            }
            return best;
        }
        public bool ValidateCombination(IReadOnlyList<PassengerRequestDefinition> definitions,out string reason,bool simultaneous=true)
        {
            for(int i=0;i<definitions.Count;i++)
            {
                var a=definitions[i];
                if(!Supports(a)) { reason="Disabled or missing capability: "+a?.StableId; return false; }
                for(int j=0;j<i;j++)
                {
                    var b=definitions[j];
                    bool distinct=a.allowMultipleInstances && b.allowMultipleInstances && !string.IsNullOrEmpty(a.targetId) &&
                        !string.IsNullOrEmpty(b.targetId) && a.targetId!=b.targetId;
                    if((a.StableId==b.StableId && !distinct) || !a.IsCompatibleWith(b) || (simultaneous && a.IsStop && b.IsStop))
                    { reason="Duplicate/conflict: "+a.StableId+" / "+b.StableId; return false; }
                }
            }
            // Chaos is compatible with clean driving when a non-impact scoring path actually exists.
            bool chaos=false; TaxiRequestBehavior forbidden=0;
            foreach(var d in definitions) { chaos|=d.requestType==TaxiRequestType.MaximumChaos; forbidden|=d.ForbiddenBehavior; }
            if(chaos && (forbidden&TaxiRequestBehavior.Impact)!=0 &&
                !Has(TruckTaxiObjectiveCapability.Shortcuts) &&
                (!Has(TruckTaxiObjectiveCapability.NearMiss) || (forbidden&TaxiRequestBehavior.HighSpeed)!=0) &&
                (!Has(TruckTaxiObjectiveCapability.Offroad) || (forbidden&TaxiRequestBehavior.Offroad)!=0))
            { reason="No permitted Chaos scoring source"; return false; }
            reason="Compatible"; return true;
        }
        public bool Has(TruckTaxiObjectiveCapability flag) => (Available&flag)==flag;
    }
}
