using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Ride-specific objective semantics; the session remains the state and reward authority.
    public static class TruckTaxiRequestPolicy
    {
        public static bool IsTimed(PassengerRequestDefinition definition) => definition != null &&
            (definition.requestType == TaxiRequestType.FastDelivery || definition.hasDeadline);

        public static bool IsRepeatableCount(TaxiRequestType type) =>
            type == TaxiRequestType.RamTraffic || type == TaxiRequestType.PropertyDamage ||
            type == TaxiRequestType.HitPedestrian || type == TaxiRequestType.NearMiss ||
            type == TaxiRequestType.Shortcut || type == TaxiRequestType.CollectDroppedObjects;

        public static int Count(PassengerRequestDefinition definition, PassengerProfile passenger,
            float remainingRideMeters, int availableTargets, float roll)
        {
            if (definition == null) return 1;
            if (!IsRepeatableCount(definition.requestType)) return Mathf.Max(1, Mathf.RoundToInt(definition.target));
            int bandMinimum = definition.countBand == TaxiRequestCountBand.Chaotic ? 3 :
                definition.countBand == TaxiRequestCountBand.Medium ? 2 : 1;
            int bandMaximum = definition.countBand == TaxiRequestCountBand.Chaotic ? 5 :
                definition.countBand == TaxiRequestCountBand.Medium ? 3 : 2;
            int authored = Mathf.Max(1, Mathf.RoundToInt(definition.target));
            int minimum = definition.minimumCount > 0 ? definition.minimumCount :
                authored > bandMaximum ? authored : bandMinimum;
            int maximum = definition.maximumCount > 0 ? definition.maximumCount :
                Mathf.Max(authored, bandMaximum);
            maximum = Mathf.Max(minimum, maximum);
            // Target count is limited by both accessible world targets and useful driving distance.
            int opportunity = remainingRideMeters < 500 ? 1 : remainingRideMeters < 1500 ? 3 : 5;
            maximum = Mathf.Min(maximum, opportunity, Mathf.Max(1, availableTargets));
            minimum = Mathf.Min(minimum, maximum);
            float bias = Mathf.Clamp01(roll);
            bool destructive = IsDestructive(definition.requestType);
            if (passenger != null)
            {
                if (destructive) bias = Mathf.Clamp01(bias + passenger.chaosAffinity * .18f);
                else if (definition.requestType == TaxiRequestType.NearMiss)
                    bias = Mathf.Clamp01(bias + passenger.speedPreference * .12f);
            }
            return minimum + Mathf.Min(maximum - minimum, Mathf.FloorToInt(bias * (maximum - minimum + 1)));
        }

        public static float SelectionWeight(PassengerRequestDefinition definition, PassengerProfile passenger)
        {
            if (definition == null || passenger == null) return 1;
            if (IsDestructive(definition.requestType))
                return Mathf.Clamp(1 + passenger.chaosAffinity * .8f, .1f, 1.8f);
            if (definition.requestType == TaxiRequestType.NearMiss || definition.requestType == TaxiRequestType.FastDelivery)
                return Mathf.Clamp(1 + passenger.speedPreference * .5f, .5f, 1.5f);
            if (definition.requestType == TaxiRequestType.SmoothRide || definition.requestType == TaxiRequestType.NoCollisions)
                return Mathf.Clamp(1 + passenger.smoothAffinity * .2f, .5f, 1.5f);
            return 1;
        }

        public static float RewardMultiplier(TaxiRequestProgress request) => request != null &&
            IsRepeatableCount(request.Definition.requestType) ? 1 + .35f * (Mathf.Max(1, request.Target) - 1) : 1;

        public static long RewardCents(TaxiRequestProgress request) => request == null ? 0 :
            (long)Math.Round(request.Definition.bonusMoneyCents * RewardMultiplier(request));

        public static int RewardScore(TaxiRequestProgress request) => request == null ? 0 :
            (int)Math.Round(request.Definition.bonusScore * RewardMultiplier(request));

        private static bool IsDestructive(TaxiRequestType type) =>
            type == TaxiRequestType.RamTraffic || type == TaxiRequestType.PropertyDamage ||
            type == TaxiRequestType.HitPedestrian;
    }
}
