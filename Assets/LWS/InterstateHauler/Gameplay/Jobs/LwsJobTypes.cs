using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsDestinationType
    {
        Generic,
        Depot,
        Warehouse,
        DistributionCenter,
        Customer
    }

    public enum LwsJobStatus
    {
        None,
        AwaitingTrailerPickup,
        HaulActive,
        Delivery,
        Completed,
        Failed,
        Abandoned
    }

    [Serializable]
    public sealed class LwsJobOffer
    {
        public string stableOfferId;
        public string jobDefinitionId;
        public string originDepotId;
        public string originDepotDisplayName;
        public string destinationId;
        public string destinationDisplayName;
        public string cargoId;
        public string cargoDisplayName;
        public string flavorText;
        public string requiredTrailerTypeId;
        public int cargoWeightLbs;
        public float estimatedDistanceMiles;
        public int quotedGrossPayCents;

        public bool IsValid => !string.IsNullOrWhiteSpace(stableOfferId) &&
                               !string.IsNullOrWhiteSpace(jobDefinitionId) &&
                               !string.IsNullOrWhiteSpace(originDepotId) &&
                               !string.IsNullOrWhiteSpace(destinationId) &&
                               !string.IsNullOrWhiteSpace(cargoId) &&
                               !string.IsNullOrWhiteSpace(requiredTrailerTypeId) &&
                               cargoWeightLbs >= 0 &&
                               estimatedDistanceMiles >= 0f &&
                               quotedGrossPayCents >= 0;

        public string QuotedGrossDisplay => FormatCents(quotedGrossPayCents);
        public string DistanceDisplay => estimatedDistanceMiles <= 0f ? "-- mi" : $"{estimatedDistanceMiles:0.#} mi";
        public string WeightDisplay => cargoWeightLbs <= 0 ? "-- lb" : $"{cargoWeightLbs:n0} lb";

        public LwsJobOffer Clone()
        {
            return (LwsJobOffer)MemberwiseClone();
        }

        public static string FormatCents(int cents)
        {
            int safeCents = Math.Max(0, cents);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:n2}", safeCents / 100d);
        }
    }

    [Serializable]
    public sealed class LwsActiveJob
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string stableActiveJobId;
        public string sourceJobDefinitionId;
        public string originDepotId;
        public string originDepotDisplayName;
        public string destinationId;
        public string destinationDisplayName;
        public string cargoId;
        public string cargoDisplayName;
        public string flavorText;
        public string requiredTrailerTypeId;
        public int cargoWeightLbs;
        public float estimatedDistanceMiles;
        public int quotedGrossPayCents;
        public LwsJobStatus status = LwsJobStatus.None;
        public long acceptedUtcTicks;

        public bool IsValid => schemaVersion > 0 &&
                               schemaVersion <= LwsSaveSchema.CurrentVersion &&
                               !string.IsNullOrWhiteSpace(stableActiveJobId) &&
                               !string.IsNullOrWhiteSpace(sourceJobDefinitionId) &&
                               !string.IsNullOrWhiteSpace(originDepotId) &&
                               !string.IsNullOrWhiteSpace(destinationId) &&
                               !string.IsNullOrWhiteSpace(cargoId) &&
                               !string.IsNullOrWhiteSpace(requiredTrailerTypeId) &&
                               status != LwsJobStatus.None &&
                               cargoWeightLbs >= 0 &&
                               estimatedDistanceMiles >= 0f &&
                               quotedGrossPayCents >= 0;

        public LwsActiveJob Clone()
        {
            return (LwsActiveJob)MemberwiseClone();
        }

        public static LwsActiveJob FromOffer(LwsJobOffer offer)
        {
            if (offer == null)
            {
                return null;
            }

            return new LwsActiveJob
            {
                schemaVersion = LwsSaveSchema.CurrentVersion,
                stableActiveJobId = $"active.{Guid.NewGuid():N}",
                sourceJobDefinitionId = offer.jobDefinitionId ?? string.Empty,
                originDepotId = offer.originDepotId ?? string.Empty,
                originDepotDisplayName = offer.originDepotDisplayName ?? string.Empty,
                destinationId = offer.destinationId ?? string.Empty,
                destinationDisplayName = offer.destinationDisplayName ?? string.Empty,
                cargoId = offer.cargoId ?? string.Empty,
                cargoDisplayName = offer.cargoDisplayName ?? string.Empty,
                flavorText = offer.flavorText ?? string.Empty,
                requiredTrailerTypeId = offer.requiredTrailerTypeId ?? string.Empty,
                cargoWeightLbs = Mathf.Max(0, offer.cargoWeightLbs),
                estimatedDistanceMiles = Mathf.Max(0f, offer.estimatedDistanceMiles),
                quotedGrossPayCents = Math.Max(0, offer.quotedGrossPayCents),
                status = LwsJobStatus.AwaitingTrailerPickup,
                acceptedUtcTicks = DateTime.UtcNow.Ticks
            };
        }
    }
}
