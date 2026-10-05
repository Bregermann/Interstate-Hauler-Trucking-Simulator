using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public readonly struct TruckTaxiCustomItemSignal
    {
        public readonly string EventId, ItemId, InteractionId;
        public TruckTaxiCustomItemSignal(string eventId, string itemId, string interactionId)
        { EventId = eventId; ItemId = itemId; InteractionId = interactionId; }
    }

    public sealed partial class TruckTaxiSession
    {
        public event Action<string> PassengerCustomInteractionStarted;
        public event Action<string> PassengerCustomInteractionCompleted;
        public event Action<TruckTaxiCustomItemSignal> CustomItemInteraction;
        public Func<PassengerRequestDefinition, int> CustomObjectiveTargetLimit { get; set; }
        private string customInteractionRide;
        private bool applyingCustomInteraction;
        private readonly HashSet<string> completedCustomInteractions = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> customOccurrences = new HashSet<string>(StringComparer.Ordinal);
        private void RefreshCustomInteractionHistory()
        {
            if (customInteractionRide == CurrentRideId) return;
            customInteractionRide = CurrentRideId;
            completedCustomInteractions.Clear(); customOccurrences.Clear();
        }
        public string CustomInteractionBlockReason(TruckTaxiCustomItemInteractionDefinition definition, TruckTaxiDriverNeedsState inventory)
        {
            if (applyingCustomInteraction) return "An item interaction is being applied.";
            RefreshCustomInteractionHistory();
            if (State != TruckTaxiState.DrivingToDestination || Passenger == null) return "No onboard fare passenger.";
            if (definition == null || string.IsNullOrWhiteSpace(definition.stableId)) return "Interaction needs a stable ID.";
            if (definition.rewardCashCents < 0 || definition.rewardCashCents > long.MaxValue - Math.Max(0, ShiftEarnings) ||
                definition.consumeQuantity < 0 || !float.IsFinite(definition.satisfactionDelta)) return "Invalid interaction values.";
            if (definition.oncePerRide && completedCustomInteractions.Contains(definition.stableId)) return "Interaction already completed this ride.";
            if (definition.minimumAge > 0 && (!Passenger.explicitlyAdult || Passenger.minimumAdultAge < definition.minimumAge)) return "Authored age requirement not met.";
            if ((int)definition.requiredGender != 0 && definition.requiredGender != Passenger.AuthoredGender) return "Authored gender requirement not met.";
            foreach (var tag in definition.requiredPassengerTags ?? Array.Empty<string>())
                if (string.IsNullOrWhiteSpace(tag) || Passenger.specialTraits == null ||
                    !Array.Exists(Passenger.specialTraits, value => string.Equals(value, tag, StringComparison.OrdinalIgnoreCase)))
                    return "Passenger tags do not match.";
            var item = TruckTaxiNeedsItems.FindByStableId(definition.requiredItemStableId);
            if (item == null || !item.PlayerOwnable || inventory == null || inventory.JugActive ||
                inventory.Count(item.Item) < Math.Max(1, definition.consumeQuantity)) return "Required inventory item unavailable.";
            return null;
        }
        public bool TryExecuteCustomItemInteraction(TruckTaxiCustomItemInteractionDefinition definition, TruckTaxiDriverNeedsState inventory, out string reason)
        {
            reason = CustomInteractionBlockReason(definition, inventory);
            if (!string.IsNullOrEmpty(reason)) return false;
            applyingCustomInteraction = true;
            try
            {
                if (!inventory.TryConsumeItem(definition.requiredItemStableId, definition.consumeQuantity))
                { reason = "Inventory changed; interaction was not applied."; return false; }
                completedCustomInteractions.Add(definition.stableId);
                // Commit the debit and all rewards before exposing the transaction to observers.
                ShiftEarnings += definition.rewardCashCents;
                Satisfaction = Mathf.Clamp(Satisfaction + definition.satisfactionDelta, 1, 5);
                chaos = (int)Math.Max(0L, Math.Min(int.MaxValue, (long)chaos + Mathf.Clamp(definition.chaosDelta, -100000, 100000)));
                NotifyCustom(() => PassengerCustomInteractionStarted?.Invoke(definition.stableId));
                RaiseCustomItemEvent(definition.customEventId, definition.requiredItemStableId, Guid.NewGuid().ToString("N"), definition.stableId);
                React(string.IsNullOrWhiteSpace(definition.optionalAcceptText) ? definition.displayName : definition.optionalAcceptText);
                NotifyCustom(() => PassengerCustomInteractionCompleted?.Invoke(definition.stableId));
                NotifyCustom(() => Changed?.Invoke()); return true;
            }
            finally { applyingCustomInteraction = false; }
        }
        public bool RaiseCustomItemEvent(string eventId, string itemId, string occurrenceId, string interactionId = null)
        {
            RefreshCustomInteractionHistory();
            if (!HasPassenger || string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(occurrenceId) || !customOccurrences.Add(occurrenceId)) return false;
            foreach (var request in requests.ToArray())
            {
                var definition = request.Definition;
                if (request.State != TaxiRequestState.Active || definition.requestType != TaxiRequestType.CustomItemAction ||
                    !string.Equals(definition.requiredCustomEventId, eventId, StringComparison.Ordinal) ||
                    !string.IsNullOrEmpty(definition.requiredItemStableId) && !string.Equals(definition.requiredItemStableId, itemId, StringComparison.Ordinal)) continue;
                request.Progress = Mathf.Min(request.Target, request.Progress + 1);
                if (request.Progress >= request.Target) NotifyCustom(() => Resolve(request, true));
            }
            NotifyCustom(() => CustomItemInteraction?.Invoke(new TruckTaxiCustomItemSignal(eventId, itemId, interactionId)));
            NotifyCustom(() => Changed?.Invoke()); return true;
        }
        private static void NotifyCustom(Action notification)
        {
            try { notification(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
