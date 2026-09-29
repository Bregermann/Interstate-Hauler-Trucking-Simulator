using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiTrafficTuning
    {
        [Min(0)] public int cautiousWeight = 15, normalWeight = 65, impatientWeight = 14, aggressiveWeight = 5, recklessWeight = 1;
        [Min(0)] public int rivalNormalWeight = 35, rivalImpatientWeight = 40, rivalAggressiveWeight = 20, rivalRecklessWeight = 5;
        [Min(1)] public float minimumLaneChangeCooldown = 6, maximumLaneChangeCooldown = 12;

        public TruckTaxiDriverPersonality Choose(uint hash, bool rival)
        {
            int a = rival ? 0 : Mathf.Max(0, cautiousWeight);
            int b = Mathf.Max(0, rival ? rivalNormalWeight : normalWeight);
            int c = Mathf.Max(0, rival ? rivalImpatientWeight : impatientWeight);
            int d = Mathf.Max(0, rival ? rivalAggressiveWeight : aggressiveWeight);
            int e = Mathf.Max(0, rival ? rivalRecklessWeight : recklessWeight);
            int total = a + b + c + d + e;
            if (total == 0) return TruckTaxiDriverPersonality.Normal;
            int roll = (int)(hash % (uint)total);
            if (roll < a) return TruckTaxiDriverPersonality.Cautious;
            roll -= a;
            if (roll < b) return TruckTaxiDriverPersonality.Normal;
            roll -= b;
            if (roll < c) return TruckTaxiDriverPersonality.Impatient;
            roll -= c;
            if (roll < d) return TruckTaxiDriverPersonality.Aggressive;
            return TruckTaxiDriverPersonality.Reckless;
        }

        public float LaneChangeCooldown(TruckTaxiDriverPersonality personality, uint hash)
        {
            float low = Mathf.Max(1, minimumLaneChangeCooldown), high = Mathf.Max(low, maximumLaneChangeCooldown);
            float modifier = personality == TruckTaxiDriverPersonality.Cautious ? 1.3f :
                personality >= TruckTaxiDriverPersonality.Aggressive ? .8f : 1f;
            return Mathf.Lerp(low, high, (hash % 101) / 100f) * modifier;
        }
    }
}
