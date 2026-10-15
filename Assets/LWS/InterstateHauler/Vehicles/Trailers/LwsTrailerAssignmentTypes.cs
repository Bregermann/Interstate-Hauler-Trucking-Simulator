using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public static class LwsTrailerTypeIds
    {
        public const string DryVan = "trailer.dry-van";
    }

    [Serializable]
    public struct LwsTrailerAssignment
    {
        public bool hasAssignedTrailer;
        public string stableTrailerId;
        public string trailerTypeId;
        public string trailerDisplayName;
        public string originDepotId;
        public bool hasPickupGlobalPosition;
        public double pickupGlobalX;
        public double pickupGlobalY;
        public double pickupGlobalZ;
        public long assignedUtcTicks;
        public long pickupCompletedUtcTicks;

        public bool IsValid => !hasAssignedTrailer ||
                               (!string.IsNullOrWhiteSpace(stableTrailerId) &&
                                !string.IsNullOrWhiteSpace(trailerTypeId));

        public LwsWorldPositionD PickupGlobalPosition => new LwsWorldPositionD(pickupGlobalX, pickupGlobalY, pickupGlobalZ);

        public static LwsTrailerAssignment None => default;

        public static LwsTrailerAssignment Create(
            string stableTrailerId,
            string trailerTypeId,
            string trailerDisplayName,
            string originDepotId,
            bool hasPickupGlobalPosition,
            LwsWorldPositionD pickupGlobalPosition)
        {
            return new LwsTrailerAssignment
            {
                hasAssignedTrailer = true,
                stableTrailerId = Normalize(stableTrailerId),
                trailerTypeId = Normalize(trailerTypeId),
                trailerDisplayName = trailerDisplayName ?? string.Empty,
                originDepotId = Normalize(originDepotId),
                hasPickupGlobalPosition = hasPickupGlobalPosition,
                pickupGlobalX = pickupGlobalPosition.x,
                pickupGlobalY = pickupGlobalPosition.y,
                pickupGlobalZ = pickupGlobalPosition.z,
                assignedUtcTicks = DateTime.UtcNow.Ticks
            };
        }

        public bool MatchesRequiredType(string requiredTrailerTypeId)
        {
            return string.Equals(Normalize(trailerTypeId), Normalize(requiredTrailerTypeId), StringComparison.Ordinal);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public struct LwsTrailerPickupSnapshot
    {
        public bool hasActiveJob;
        public string activeJobId;
        public LwsJobStatus jobStatus;
        public string requiredTrailerTypeId;
        public bool hasAssignedTrailer;
        public string assignedTrailerId;
        public string assignedTrailerDisplayName;
        public bool assignedTrailerResolved;
        public string attachedTrailerId;
        public bool correctTrailerAttached;
        public bool pickupObjectiveActive;
        public float pickupDistanceMeters;
        public string lastMessage;
    }

    public interface ILwsTrailerRegistryService : ILwsService
    {
        IReadOnlyList<LwsTrailerGameplayIdentity> RegisteredTrailers { get; }
        event Action<LwsTrailerGameplayIdentity> TrailerRegistered;
        event Action<string> TrailerUnregistered;
        LwsServiceResult RegisterTrailer(LwsTrailerGameplayIdentity trailer);
        LwsServiceResult UnregisterTrailer(LwsTrailerGameplayIdentity trailer);
        bool TryResolveTrailer(string stableTrailerId, out LwsTrailerGameplayIdentity trailer);
        IReadOnlyList<LwsTrailerGameplayIdentity> FindAssignableTrailers(string requiredTrailerTypeId, string preferredOriginDepotId, string activeJobId);
    }

    public interface ILwsTrailerPickupService : ILwsService
    {
        LwsTrailerPickupSnapshot CurrentSnapshot { get; }
        string LastMessage { get; }
        event Action<LwsTrailerPickupSnapshot> PickupStateChanged;
        LwsServiceResult EnsureAssignmentForCurrentJob(string reason);
        LwsServiceResult FocusPickupObjective(string reason);
        LwsServiceResult ActivateDestinationObjective(string reason);
        LwsServiceResult ValidateAttachedTrailerForPickup(LwsTrailerAttachmentState attachmentState, string reason);
    }
}
