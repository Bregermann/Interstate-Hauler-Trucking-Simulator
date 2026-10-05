using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        public string LastSketchyEligibilityReason { get; private set; } = "NOT CHECKED";
        public Func<TruckTaxiJugGoalContext> JugGoalContextProvider { get; set; }
        private readonly Dictionary<TaxiRequestProgress, int> jugGoalSuccessBaselines = new Dictionary<TaxiRequestProgress, int>();
        private string jugGoalRide;

        public bool CanAssignJugGoal(PassengerRequestDefinition definition, out string reason)
        {
            return JugGoalCapacity(definition, out reason) > 0;
        }

        private int JugGoalCapacity(PassengerRequestDefinition definition, out string reason)
        {
            reason = "No active ordinary jug objective context.";
            if (definition == null || definition.requestType != TaxiRequestType.FillJug || Passenger == null ||
                State != TruckTaxiState.DrivingToDestination || SafeDropRequested || CurrentDesiredDestination == null ||
                JugGoalContextProvider == null || !float.IsFinite(definition.target) || definition.target < .1f) return 0;
            foreach (var old in requests)
                if (old.Definition.requestType == TaxiRequestType.FillJug)
                { reason = "This ride already assigned its jug goal."; return 0; }
            if (ActiveStop != null || PendingDiversion != null)
            { reason = "Another stop already owns the safe completion window."; return 0; }
            float meters = routeDistances.Measure(playerPosition(), CurrentDesiredDestination.StopPosition).Meters;
            float rideSeconds = meters / TruckTaxiJugGoalPolicy.RideBudgetMetersPerSecond;
            float deadline = definition.hasDeadline ? definition.timer : float.MaxValue;
            foreach (var active in requests)
                if (active.State == TaxiRequestState.Active && active.Definition.requestType == TaxiRequestType.FastDelivery)
                    deadline = Mathf.Min(deadline, active.Remaining - rideSeconds);
            return TruckTaxiJugGoalPolicy.Capacity(JugGoalContextProvider(), rideSeconds, deadline, out reason);
        }

        // Call only after the existing successful-QTE counter has advanced. Repeated notifications are harmless.
        public void RefreshJugGoalProgress()
        {
            if (State != TruckTaxiState.DrivingToDestination || CurrentRideId == null || jugGoalRide != CurrentRideId) return;
            string ride = CurrentRideId;
            foreach (var request in requests.ToArray())
            {
                if (CurrentRideId != ride || State != TruckTaxiState.DrivingToDestination) break;
                if (request.State != TaxiRequestState.Active || request.Definition.requestType != TaxiRequestType.FillJug ||
                    !jugGoalSuccessBaselines.TryGetValue(request, out int baseline)) continue;
                request.Progress = Mathf.Min(request.Target, Mathf.Max(0, JugsSucceeded - baseline));
                if (request.Progress >= request.Target) Resolve(request, true);
            }
        }

        // Replace the direct progress constructor in GenerateRequest with this method.
        private TaxiRequestProgress CreatePolicyProgress(PassengerRequestDefinition definition)
        {
            if (definition.requestType == TaxiRequestType.FillJug)
            {
                int capacity = JugGoalCapacity(definition, out _);
                bool authoredTwo = definition.countBand == TaxiRequestCountBand.Chaotic && Passenger.chaosAffinity >= .5f &&
                    (definition.maximumCount > 0 ? definition.maximumCount >= 2 : definition.target >= 2);
                int count = TruckTaxiJugGoalPolicy.SelectCount(capacity, authoredTwo, (float)random.NextDouble());
                if (count == 0) return null;
                var progress = new TaxiRequestProgress(definition, count / definition.target, maximumCount: count);
                if (jugGoalRide != CurrentRideId) { jugGoalSuccessBaselines.Clear(); jugGoalRide = CurrentRideId; }
                jugGoalSuccessBaselines[progress] = JugsSucceeded;
                return progress;
            }
            var stop = definition.IsStop ? Capabilities.FindStop(definition, Passenger, playerPosition()) : null;
            float difficulty = Mathf.Lerp(Passenger.requestDifficultyRange.x, Passenger.requestDifficultyRange.y,
                (float)random.NextDouble());
            if (TruckTaxiRequestPolicy.IsRepeatableCount(definition.requestType))
            {
                float meters = CurrentDesiredDestination == null ? 0 :
                    routeDistances.Measure(playerPosition(), CurrentDesiredDestination.StopPosition).Meters;
                if (!float.IsFinite(meters) || meters <= 0)
                    meters = CurrentDesiredDestination == null ? 0 :
                        Vector3.Distance(playerPosition(), CurrentDesiredDestination.StopPosition);
                int count = TruckTaxiRequestPolicy.Count(definition, Passenger, meters,
                    Capabilities.TargetLimit(definition.requestType), (float)random.NextDouble());
                difficulty = count / Mathf.Max(.1f, definition.target);
            }
            int limit = definition.requestType == TaxiRequestType.CustomItemAction && CustomObjectiveTargetLimit != null
                ? CustomObjectiveTargetLimit(definition) : Capabilities.TargetLimit(definition.requestType);
            return new TaxiRequestProgress(definition, difficulty, stop, limit);
        }

        public string ExplainSketchyEligibility(PassengerRequestDefinition definition, bool selected = false)
        {
            if (definition == null || definition.requestType != TaxiRequestType.IllicitStop) return "NO DEFINITION";
            if (State != TruckTaxiState.DrivingToDestination || SafeDropRequested) return "ACTIVE REQUEST";
            if (Passenger == null || Array.IndexOf(Passenger.possibleRequests ?? Array.Empty<PassengerRequestDefinition>(), definition) < 0)
                return "PASSENGER INELIGIBLE";
            if (ActiveStop != null || PendingDiversion != null) return "ACTIVE REQUEST";
            if (Capabilities.Count(TruckTaxiObjectiveCapability.IllicitStops) <= 0) return "NO LOCATION";
            var stop = Capabilities.FindStop(definition, Passenger, playerPosition());
            if (stop == null) return "PASSENGER INELIGIBLE";
            if (!CanAssign(definition, out _)) return "ACTIVE REQUEST";
            return selected ? "ELIGIBLE" : "ROLL FAILED";
        }

        // Call after normal candidate selection, including failed selections, for concise debug diagnostics.
        private void CaptureSketchySelection(PassengerRequestDefinition selected)
        {
            var definitions = Passenger?.possibleRequests;
            if (definitions == null) { LastSketchyEligibilityReason = "PASSENGER INELIGIBLE"; return; }
            foreach (var definition in definitions)
                if (definition != null && definition.requestType == TaxiRequestType.IllicitStop)
                {
                    LastSketchyEligibilityReason = ExplainSketchyEligibility(definition, selected == definition);
                    return;
                }
            LastSketchyEligibilityReason = "PASSENGER INELIGIBLE";
        }
    }
}
