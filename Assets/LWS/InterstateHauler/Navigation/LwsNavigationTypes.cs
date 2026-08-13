using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsNavigationManeuverType
    {
        StartRoute,
        ContinueStraight,
        SlightLeft,
        SlightRight,
        TurnLeft,
        TurnRight,
        SharpLeft,
        SharpRight,
        KeepLeft,
        KeepRight,
        MergeLeft,
        MergeRight,
        TakeRampLeft,
        TakeRampRight,
        TakeExitLeft,
        TakeExitRight,
        ForkLeft,
        ForkRight,
        MakeUTurn,
        EnterRoundabout,
        RoundaboutExit1,
        RoundaboutExit2,
        RoundaboutExit3,
        RoundaboutExit4,
        RoundaboutExit5,
        RoundaboutExit6,
        RoundaboutExit7,
        RoundaboutExit8,
        Arrive,
        DestinationOnLeft,
        DestinationOnRight,
        RouteRecalculating,
        RouteRecalculated,
        OffRoute
    }

    public enum LwsNavigationRouteStatus
    {
        Inactive,
        Active,
        OffRoute,
        Recalculating,
        Arrived,
        Failed
    }

    [Serializable]
    public sealed class LwsRouteStep
    {
        public int stepIndex;
        public string edgeId;
        public string roadId;
        public string segmentId;
        public string roadDisplayName;
        public string nextRoadDisplayName;
        public LwsNavigationManeuverType maneuver;
        public string instructionText;
        public Vector3 maneuverPosition;
        public float distanceFromRouteStartMeters;
        public float distanceMeters;
    }

    [Serializable]
    public sealed class LwsNavigationRuntimeState
    {
        public bool routeActive;
        public LwsNavigationRouteStatus status;
        public string routeId;
        public string destinationId;
        public string currentRoadId;
        public string currentRoadDisplayName;
        public string currentEdgeId;
        public string nextRoadId;
        public string nextRoadDisplayName;
        public int currentStepIndex;
        public int totalSteps;
        public LwsNavigationManeuverType nextManeuver;
        public string nextInstructionText;
        public float distanceToNextManeuverMeters;
        public float distanceRemainingMeters;
        public float estimatedTimeRemainingSeconds;
        public bool offRoute;
        public bool recalculating;
        public bool destinationReached;
        public Vector3 matchedPosition;
        public Vector3 playerPosition;
        public Vector3 playerForward;

        public LwsNavigationRuntimeState Clone()
        {
            return (LwsNavigationRuntimeState)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class LwsNavigationTuning
    {
        public float offRouteToleranceMeters = 28f;
        public float rerouteCooldownSeconds = 4f;
        public float arrivalDistanceMeters = 35f;
        public float maneuverAdvanceDistanceMeters = 30f;
        public float highwayVoiceApproachMeters = 805f;
        public float localVoiceApproachMeters = 180f;
        public float routeConnectorToleranceMeters = 35f;
        public float straightAngleDegrees = 12f;
        public float slightAngleDegrees = 35f;
        public float turnAngleDegrees = 100f;

        public static LwsNavigationTuning Default()
        {
            return new LwsNavigationTuning();
        }
    }

    public static class LwsNavigationManeuverCatalog
    {
        private static readonly LwsNavigationManeuverType[] AllManeuvers =
        {
            LwsNavigationManeuverType.StartRoute,
            LwsNavigationManeuverType.ContinueStraight,
            LwsNavigationManeuverType.SlightLeft,
            LwsNavigationManeuverType.SlightRight,
            LwsNavigationManeuverType.TurnLeft,
            LwsNavigationManeuverType.TurnRight,
            LwsNavigationManeuverType.SharpLeft,
            LwsNavigationManeuverType.SharpRight,
            LwsNavigationManeuverType.KeepLeft,
            LwsNavigationManeuverType.KeepRight,
            LwsNavigationManeuverType.MergeLeft,
            LwsNavigationManeuverType.MergeRight,
            LwsNavigationManeuverType.TakeRampLeft,
            LwsNavigationManeuverType.TakeRampRight,
            LwsNavigationManeuverType.TakeExitLeft,
            LwsNavigationManeuverType.TakeExitRight,
            LwsNavigationManeuverType.ForkLeft,
            LwsNavigationManeuverType.ForkRight,
            LwsNavigationManeuverType.MakeUTurn,
            LwsNavigationManeuverType.EnterRoundabout,
            LwsNavigationManeuverType.RoundaboutExit1,
            LwsNavigationManeuverType.RoundaboutExit2,
            LwsNavigationManeuverType.RoundaboutExit3,
            LwsNavigationManeuverType.RoundaboutExit4,
            LwsNavigationManeuverType.RoundaboutExit5,
            LwsNavigationManeuverType.RoundaboutExit6,
            LwsNavigationManeuverType.RoundaboutExit7,
            LwsNavigationManeuverType.RoundaboutExit8,
            LwsNavigationManeuverType.Arrive,
            LwsNavigationManeuverType.DestinationOnLeft,
            LwsNavigationManeuverType.DestinationOnRight,
            LwsNavigationManeuverType.RouteRecalculating,
            LwsNavigationManeuverType.RouteRecalculated,
            LwsNavigationManeuverType.OffRoute
        };

        public static IReadOnlyList<LwsNavigationManeuverType> All => AllManeuvers;

        public static string GetDisplayName(LwsNavigationManeuverType maneuver)
        {
            switch (maneuver)
            {
                case LwsNavigationManeuverType.StartRoute: return "Start route";
                case LwsNavigationManeuverType.ContinueStraight: return "Continue straight";
                case LwsNavigationManeuverType.SlightLeft: return "Slight left";
                case LwsNavigationManeuverType.SlightRight: return "Slight right";
                case LwsNavigationManeuverType.TurnLeft: return "Turn left";
                case LwsNavigationManeuverType.TurnRight: return "Turn right";
                case LwsNavigationManeuverType.SharpLeft: return "Sharp left";
                case LwsNavigationManeuverType.SharpRight: return "Sharp right";
                case LwsNavigationManeuverType.KeepLeft: return "Keep left";
                case LwsNavigationManeuverType.KeepRight: return "Keep right";
                case LwsNavigationManeuverType.MergeLeft: return "Merge left";
                case LwsNavigationManeuverType.MergeRight: return "Merge right";
                case LwsNavigationManeuverType.TakeRampLeft: return "Take ramp left";
                case LwsNavigationManeuverType.TakeRampRight: return "Take ramp right";
                case LwsNavigationManeuverType.TakeExitLeft: return "Take exit left";
                case LwsNavigationManeuverType.TakeExitRight: return "Take exit right";
                case LwsNavigationManeuverType.ForkLeft: return "Fork left";
                case LwsNavigationManeuverType.ForkRight: return "Fork right";
                case LwsNavigationManeuverType.MakeUTurn: return "Make a U-turn";
                case LwsNavigationManeuverType.EnterRoundabout: return "Enter roundabout";
                case LwsNavigationManeuverType.RoundaboutExit1: return "Take first exit";
                case LwsNavigationManeuverType.RoundaboutExit2: return "Take second exit";
                case LwsNavigationManeuverType.RoundaboutExit3: return "Take third exit";
                case LwsNavigationManeuverType.RoundaboutExit4: return "Take fourth exit";
                case LwsNavigationManeuverType.RoundaboutExit5: return "Take fifth exit";
                case LwsNavigationManeuverType.RoundaboutExit6: return "Take sixth exit";
                case LwsNavigationManeuverType.RoundaboutExit7: return "Take seventh exit";
                case LwsNavigationManeuverType.RoundaboutExit8: return "Take eighth exit";
                case LwsNavigationManeuverType.Arrive: return "Arrive";
                case LwsNavigationManeuverType.DestinationOnLeft: return "Destination on left";
                case LwsNavigationManeuverType.DestinationOnRight: return "Destination on right";
                case LwsNavigationManeuverType.RouteRecalculating: return "Recalculating";
                case LwsNavigationManeuverType.RouteRecalculated: return "Route recalculated";
                case LwsNavigationManeuverType.OffRoute: return "Off route";
                default: return maneuver.ToString();
            }
        }
    }

    public static class LwsRoadDisplayNames
    {
        public static string GetRoadDisplayName(string roadId, string segmentId)
        {
            if (string.Equals(roadId, "IH_TEST_I000_NB", StringComparison.Ordinal))
            {
                return "Interstate Test Northbound";
            }

            if (string.Equals(roadId, "IH_TEST_I000_SB", StringComparison.Ordinal))
            {
                return "Interstate Test Southbound";
            }

            if (string.Equals(roadId, "IH_TEST_I000_RAMP", StringComparison.Ordinal))
            {
                return "Entry Ramp";
            }

            if (string.Equals(roadId, "IH_TEST_I000_TURN", StringComparison.Ordinal))
            {
                return "Turnaround Crossover";
            }

            if (!string.IsNullOrWhiteSpace(segmentId))
            {
                return segmentId.Replace('_', ' ');
            }

            return string.IsNullOrWhiteSpace(roadId) ? "Unknown Road" : roadId.Replace('_', ' ');
        }
    }
}
