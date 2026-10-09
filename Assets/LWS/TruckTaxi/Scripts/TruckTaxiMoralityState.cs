using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiTownState
    {
        public string townId;
        [Range(0,100)] public float safety=50, crime=50, trafficPressure=50;
        [Range(-100,100)] public float reputation, authorityStanding, shadyStanding;
        [Range(.9f,1.1f)] public float fareMultiplier=1;
        public TruckTaxiTownState Copy() => (TruckTaxiTownState)MemberwiseClone();
        internal static bool InRange(float value,float min,float max) => float.IsFinite(value) && value>=min && value<=max;
        public bool IsValid => TruckTaxiCareerProfileData.ValidId(townId) && InRange(safety,0,100) &&
            InRange(crime,0,100) && InRange(trafficPressure,0,100) && InRange(reputation,-100,100) &&
            InRange(authorityStanding,-100,100) && InRange(shadyStanding,-100,100) && InRange(fareMultiplier,.9f,1.1f);
        internal void Apply(TruckTaxiMoralChoiceEffects effects)
        {
            safety=Mathf.Clamp(safety+effects.safety,0,100);
            crime=Mathf.Clamp(crime+effects.crime,0,100);
            trafficPressure=Mathf.Clamp(trafficPressure+effects.trafficPressure,0,100);
            reputation=Mathf.Clamp(reputation+effects.reputation,-100,100);
            authorityStanding=Mathf.Clamp(authorityStanding+effects.authorityStanding,-100,100);
            shadyStanding=Mathf.Clamp(shadyStanding+effects.shadyStanding,-100,100);
            fareMultiplier=Mathf.Clamp(fareMultiplier+effects.fareMultiplier,.9f,1.1f);
        }
    }

    [Serializable]
    public sealed class TruckTaxiMoralChoiceEffects
    {
        // Small authored steps; absolute town values also have hard bounds.
        [Range(-10,10)] public float safety, crime, trafficPressure, reputation, authorityStanding, shadyStanding;
        [Range(-.02f,.02f)] public float fareMultiplier;
        [Tooltip("Authored payout in cents, bounded to 0..100000 by validation.")]
        [Min(0)] public long payoutCents;
        public TruckTaxiMoralChoiceEffects Copy() => (TruckTaxiMoralChoiceEffects)MemberwiseClone();
        public bool IsValid => TruckTaxiTownState.InRange(safety,-10,10) && TruckTaxiTownState.InRange(crime,-10,10) &&
            TruckTaxiTownState.InRange(trafficPressure,-10,10) && TruckTaxiTownState.InRange(reputation,-10,10) &&
            TruckTaxiTownState.InRange(authorityStanding,-10,10) && TruckTaxiTownState.InRange(shadyStanding,-10,10) &&
            TruckTaxiTownState.InRange(fareMultiplier,-.02f,.02f) && payoutCents>=0 && payoutCents<=100000;
    }

    [Serializable]
    public sealed class TruckTaxiAcceptedMoralChoice
    {
        public string eventId, townId, trailerId, choiceId, completionObjectiveId;
        public TruckTaxiMoralChoiceEffects effects;
        public bool completed;
        public TruckTaxiAcceptedMoralChoice Copy()
        {
            var copy=(TruckTaxiAcceptedMoralChoice)MemberwiseClone(); copy.effects=effects?.Copy(); return copy;
        }
        internal bool IsValid => TruckTaxiCareerProfileData.ValidId(eventId) && TruckTaxiCareerProfileData.ValidId(townId) &&
            TruckTaxiCareerProfileData.ValidId(trailerId) && trailerId!="trailer.unassigned" &&
            TruckTaxiMoralChoiceDefinition.ValidChoiceId(choiceId) && TruckTaxiCareerProfileData.ValidId(completionObjectiveId) &&
            effects!=null && effects.IsValid;
    }

    [Serializable]
    public sealed class TruckTaxiMoralityState
    {
        public int version=1;
        public List<TruckTaxiTownState> towns=new List<TruckTaxiTownState>();
        // Completed records are retained: one stable authored event can pay only once per Career.
        public List<TruckTaxiAcceptedMoralChoice> choices=new List<TruckTaxiAcceptedMoralChoice>();
        public TruckTaxiMoralityState Copy() => new TruckTaxiMoralityState {
            version=version,towns=towns?.ConvertAll(t=>t?.Copy()),choices=choices?.ConvertAll(c=>c?.Copy())
        };
        public bool Validate(long lifetimeEarnings,out string reason)
        {
            reason="Invalid town or moral-choice state.";
            if(version!=1 || towns==null || choices==null || towns.Count>128 || choices.Count>2048) return false;
            var townIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var town in towns) if(town==null || !town.IsValid || !townIds.Add(town.townId)) return false;
            var eventIds=new HashSet<string>(StringComparer.Ordinal);
            int active=0; long paid=0;
            foreach(var choice in choices)
            {
                if(choice==null || !choice.IsValid || !townIds.Contains(choice.townId) || !eventIds.Add(choice.eventId)) return false;
                if(choice.completed) paid+=choice.effects.payoutCents;
                else if(++active>1) return false;
            }
            if(paid>lifetimeEarnings) return false;
            reason=null; return true;
        }
    }
}
