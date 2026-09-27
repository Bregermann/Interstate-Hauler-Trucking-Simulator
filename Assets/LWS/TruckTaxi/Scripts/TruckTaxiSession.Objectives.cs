using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        public string LastSketchyEligibilityReason { get; private set; } = "NOT CHECKED";

        // Replace the direct progress constructor in GenerateRequest with this method.
        private TaxiRequestProgress CreatePolicyProgress(PassengerRequestDefinition definition)
        {
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
            return new TaxiRequestProgress(definition, difficulty, stop, Capabilities.TargetLimit(definition.requestType));
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
