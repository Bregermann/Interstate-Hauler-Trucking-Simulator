using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiWorkAreaMode { AnywhereNearMe, CurrentDistrict, CurrentTown, Custom }

    [Serializable]
    public sealed class TruckTaxiRideWorkArea
    {
        public TruckTaxiWorkAreaMode mode;
        public Vector3 center;
        public float radius = 500;
        public string label = "ANYWHERE NEAR ME";
        public bool Restricted => mode != TruckTaxiWorkAreaMode.AnywhereNearMe;
        public bool Contains(Vector3 position) => !Restricted ||
            Vector2.Distance(new Vector2(position.x,position.z),new Vector2(center.x,center.z)) <= radius;
        public TruckTaxiRideWorkArea Copy() => new TruckTaxiRideWorkArea { mode=mode,center=center,radius=radius,label=label };
        public bool IsValid => Enum.IsDefined(typeof(TruckTaxiWorkAreaMode),mode) &&
            float.IsFinite(center.x) && float.IsFinite(center.y) && float.IsFinite(center.z) &&
            float.IsFinite(radius) && radius>=50 && radius<=10000;
    }

    // Completed fares, never generated/declined candidates, advance spacing and drought state.
    public sealed class TruckTaxiIntercityPolicy
    {
        public int CompletedLocalStreak { get; private set; }
        public int LocalCooldownRemaining { get; private set; }
        public int Threshold { get; }
        public int LocalSpacing { get; }
        public float RandomChance { get; }
        public bool GuaranteePending => CompletedLocalStreak>=Threshold && LocalCooldownRemaining==0;
        public TruckTaxiIntercityPolicy(float chance,int spacing,int threshold)
        { RandomChance=float.IsNaN(chance) || float.IsInfinity(chance) ? 0 : Math.Max(0,Math.Min(1,chance));
            LocalSpacing=Math.Max(0,Math.Min(10,spacing)); Threshold=Math.Max(LocalSpacing,Math.Max(1,threshold)); }
        public bool WantsIntercity(System.Random random,out bool guaranteed)
        {
            guaranteed=GuaranteePending;
            return guaranteed || LocalCooldownRemaining==0 && random.NextDouble()<RandomChance;
        }
        public void CompleteRide(bool intercity)
        {
            if(intercity) { CompletedLocalStreak=0; LocalCooldownRemaining=LocalSpacing; }
            else { if(CompletedLocalStreak<int.MaxValue) CompletedLocalStreak++; LocalCooldownRemaining=Math.Max(0,LocalCooldownRemaining-1); }
        }
        public TruckTaxiIntercityState CaptureState() => new TruckTaxiIntercityState
            { completedLocalStreak=CompletedLocalStreak,localCooldownRemaining=LocalCooldownRemaining };
        public bool RestoreState(TruckTaxiIntercityState state)
        {
            if(state==null || state.version!=1 || state.completedLocalStreak<0 || state.localCooldownRemaining<0) return false;
            CompletedLocalStreak=state.completedLocalStreak;
            LocalCooldownRemaining=Math.Min(Math.Max(0,LocalSpacing-CompletedLocalStreak),state.localCooldownRemaining);
            return true;
        }
    }

    [Serializable]
    public sealed class TruckTaxiIntercityState
    {
        public int version=1, completedLocalStreak, localCooldownRemaining;
    }

    public readonly struct TruckTaxiFareModifiers
    {
        private readonly float eventMultiplier,weatherMultiplier,otherMultiplier;
        public float EventMultiplier => Valid(eventMultiplier);
        public float WeatherMultiplier => Valid(weatherMultiplier);
        public float OtherMultiplier => Valid(otherMultiplier);
        public readonly string EventLabel, OtherLabel;
        public TruckTaxiFareModifiers(float eventMultiplier=1,float weatherMultiplier=1,float otherMultiplier=1,string eventLabel=null,string otherLabel=null)
        { this.eventMultiplier=Valid(eventMultiplier); this.weatherMultiplier=Valid(weatherMultiplier); this.otherMultiplier=Valid(otherMultiplier); EventLabel=eventLabel; OtherLabel=otherLabel; }
        private static float Valid(float value) => float.IsFinite(value) && value>=1 ? Mathf.Min(4,value) : 1;
    }
}
