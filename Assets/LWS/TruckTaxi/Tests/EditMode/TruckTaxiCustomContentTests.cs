using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests.EditMode
{
    public sealed class TruckTaxiCustomContentTests
    {
        private const string ItemId = "water-bottle";
        private const string EventId = "test.item.handed-over";
        private readonly List<Object> allocated = new List<Object>();
        private readonly List<TruckTaxiCustomContentRunner> runners = new List<TruckTaxiCustomContentRunner>();
        private TruckTaxiSession session;
        private PassengerProfile passenger;
        private TruckTaxiDriverNeedsState inventory;
        private bool hadPreference;
        private int oldPreference;
        private UnityEngine.Random.State randomState;

        [SetUp]
        public void SetUp()
        {
            hadPreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            oldPreference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            randomState = UnityEngine.Random.state;
            passenger = Asset<PassengerProfile>();
            passenger.passengerId = "custom-content-test";
            passenger.passengerName = "Neutral commuter";
            passenger.gender = TruckTaxiPassengerGender.Male;
            passenger.explicitlyAdult = true;
            passenger.minimumAdultAge = 25;
            passenger.specialTraits = new[] { "Commuter", "Prepared" };
            passenger.basePatience = passenger.requestFrequency = 10000;
            passenger.baseSatisfaction = 3;
            passenger.requestDifficultyRange = Vector2.one;
            passenger.possibleRequests = Array.Empty<PassengerRequestDefinition>();
            var config = Asset<TruckTaxiConfiguration>();
            config.minimumTripDistance = 10; config.maximumTripDistance = 1000;
            config.passengers = new[] { passenger };
            var locations = new List<TruckTaxiRideLocation>();
            for (int i = 0; i < 3; i++)
            {
                var go = NewObject("Custom test stop " + i);
                go.transform.position = Vector3.right * (i * 100);
                var point = go.AddComponent<TruckTaxiRideLocation>();
                point.locationId = "custom.stop." + i;
                locations.Add(point);
            }
            session = new TruckTaxiSession(config, locations, 31);
            inventory = new TruckTaxiDriverNeedsState(Asset<TruckTaxiEnvironmentSettings>(), 31);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (var runner in runners) if (runner != null) runner.Cancel();
                for (int i = allocated.Count - 1; i >= 0; i--)
                    if (allocated[i] != null) Object.DestroyImmediate(allocated[i]);
            }
            finally
            {
                runners.Clear(); allocated.Clear();
                UnityEngine.Random.state = randomState;
                if (hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, oldPreference);
                else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
            }
        }

        [TestCase(EventId, ItemId, 1)]
        [TestCase("TEST.ITEM.HANDED-OVER", ItemId, 0)]
        [TestCase("test.item.handed", ItemId, 0)]
        [TestCase("test.item.handed-over.extra", ItemId, 0)]
        [TestCase(EventId, "WATER-BOTTLE", 0)]
        [TestCase(EventId, "Water bottle", 0)]
        [TestCase(EventId, "soda", 0)]
        [TestCase(EventId, null, 0)]
        public void ObjectiveSignal_MatchesExactEventAndOptionalItemFilter(string eventId, string itemId, int expected)
        {
            Board();
            var request = Goal(2);
            TruckTaxiCustomItemSignal received = default;
            int signals = 0;
            session.CustomItemInteraction += signal => { received = signal; signals++; };
            Assert.IsTrue(session.RaiseCustomItemEvent(eventId, itemId, "occurrence", "test.interaction"));
            Assert.AreEqual(expected, request.Progress);
            Assert.AreEqual(TaxiRequestState.Active, request.State);
            Assert.AreEqual(1, signals);
            Assert.AreEqual(eventId, received.EventId);
            Assert.AreEqual(itemId, received.ItemId);
            Assert.AreEqual("test.interaction", received.InteractionId);
        }

        [TestCase(null)]
        [TestCase("")]
        public void EventOnlyObjective_AssignsAndCompletesWithoutAnItemFilter(string optionalItem)
        {
            Board();
            var interaction = Interaction();
            var request = Goal(2, interaction, optionalItem);
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 2));
            Assert.IsTrue(session.RaiseCustomItemEvent("TEST.ITEM.HANDED-OVER", "soda", "wrong-event"));
            Assert.Zero(request.Progress);
            Assert.IsTrue(session.RaiseCustomItemEvent(EventId, "soda", "matching-event"));
            Assert.AreEqual(1, request.Progress, "An omitted item filter must not inherit the registered producer's item.");
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(interaction, inventory, out string reason), reason);
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
            Assert.AreEqual(1, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
        }

        [TestCase(null)]
        [TestCase("")]
        public void EventOnlyCapability_AcceptsAnyRegisteredItemButNeverAnotherEvent(string optionalItem)
        {
            var eventOnly = Objective(1, optionalItem);
            var itemSpecific = Objective(1);
            Assert.IsTrue(eventOnly.HasValidCustomItemActionMetadata);
            Assert.IsFalse(session.Capabilities.Supports(eventOnly));
            Assert.IsTrue(session.Capabilities.RegisterCustomItemAction("TEST.ITEM.HANDED-OVER", ItemId));
            Assert.IsTrue(session.Capabilities.RegisterCustomItemAction(EventId + ".extra", ItemId));
            Assert.IsFalse(session.Capabilities.Supports(eventOnly));
            Assert.IsTrue(session.Capabilities.RegisterCustomItemAction(EventId, "soda"));
            Assert.IsTrue(session.Capabilities.Supports(eventOnly));
            Assert.IsFalse(session.Capabilities.Supports(itemSpecific));
            Assert.IsTrue(session.Capabilities.RegisterCustomItemAction(EventId, "soda", false));
            Assert.IsFalse(session.Capabilities.Supports(eventOnly));
        }

        [Test]
        public void OccurrenceIdentity_IsPerRideOrdinalAndCannotBeReplayedWithAnotherPayload()
        {
            Board();
            var request = Goal(2);
            int resolved = 0, signals = 0;
            session.RequestResolved += _ => resolved++;
            session.CustomItemInteraction += _ => signals++;
            Assert.IsTrue(session.RaiseCustomItemEvent("test.other", ItemId, "same-occurrence"));
            Assert.IsFalse(session.RaiseCustomItemEvent(EventId, ItemId, "same-occurrence"));
            Assert.Zero(request.Progress);
            Assert.IsTrue(session.RaiseCustomItemEvent(EventId, ItemId, "receipt-a"));
            Assert.IsFalse(session.RaiseCustomItemEvent(EventId, ItemId, "receipt-a"));
            Assert.AreEqual(1, request.Progress);
            Assert.IsTrue(session.RaiseCustomItemEvent(EventId, ItemId, "RECEIPT-A"));
            Assert.IsTrue(session.RaiseCustomItemEvent(EventId, ItemId, "after-completion"));
            Assert.AreEqual(2, request.Progress);
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
            Assert.AreEqual(1, resolved);
            Assert.AreEqual(4, signals);
            Assert.AreEqual(request.Definition.bonusMoneyCents, session.EstimateFare().Requests);
        }

        [TestCase(null, "receipt")]
        [TestCase("", "receipt")]
        [TestCase(" ", "receipt")]
        [TestCase(EventId, null)]
        [TestCase(EventId, "")]
        [TestCase(EventId, " ")]
        public void InvalidSignal_IsRejectedWithoutProgressOrOccurrenceReservation(string eventId, string occurrence)
        {
            Board();
            var request = Goal(1);
            int signals = 0;
            session.CustomItemInteraction += _ => signals++;
            Assert.IsFalse(session.RaiseCustomItemEvent(eventId, ItemId, occurrence));
            Assert.Zero(request.Progress); Assert.Zero(signals);
            Assert.IsTrue(session.RaiseCustomItemEvent(EventId, ItemId, "receipt"));
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
            Assert.AreEqual(1, signals);
        }

        [Test]
        public void NoPassenger_CannotExecuteOrReserveAnOccurrenceForTheNextRide()
        {
            var definition = Interaction();
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 2));
            AssertRejectedWithoutMutation(definition, inventory);
            Assert.IsFalse(session.RaiseCustomItemEvent(EventId, ItemId, "receipt"));
            Board();
            var request = Goal(1, definition);
            Assert.IsTrue(session.RaiseCustomItemEvent(EventId, ItemId, "receipt"));
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
        }

        [Test]
        public void InsufficientInventory_IsAtomicAndDoesNotSpendTheOncePerRideAllowance()
        {
            Board();
            var definition = Interaction(); definition.consumeQuantity = 2;
            var request = Goal(1, definition);
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle));
            AssertRejectedWithoutMutation(definition, inventory);
            Assert.Zero(request.Progress);
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle));
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(definition, inventory, out string reason), reason);
            Assert.Zero(inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
        }

        [Test]
        public void Execute_DebitsBeforeCallbacks_GrantsRewardsOnce_AndBlocksReentrantOrClonedDefinition()
        {
            Board();
            var definition = Interaction(); definition.consumeQuantity = 2;
            var request = Goal(1, definition);
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 5));
            var order = new List<string>();
            float satisfaction = session.Satisfaction, thirst = inventory.Thirst;
            int chaos = session.ChaosScore, emptyBottles = inventory.Count(TruckTaxiNeedsItem.EmptyBottle);
            long money = session.ShiftEarnings;
            session.PassengerCustomInteractionStarted += id =>
            {
                order.Add("started"); Assert.AreEqual(definition.stableId, id);
                Assert.AreEqual(3, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
                Assert.IsFalse(session.TryExecuteCustomItemInteraction(definition, inventory, out _));
            };
            session.RequestResolved += _ => order.Add("resolved");
            session.CustomItemInteraction += signal =>
            {
                order.Add("signal"); Assert.AreEqual(EventId, signal.EventId);
                Assert.AreEqual(ItemId, signal.ItemId); Assert.AreEqual(definition.stableId, signal.InteractionId);
            };
            session.PassengerCustomInteractionCompleted += id =>
            { order.Add("completed"); Assert.AreEqual(definition.stableId, id); };
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(definition, inventory, out string reason), reason);
            Assert.That(reason, Is.Null.Or.Empty);
            CollectionAssert.AreEqual(new[] { "started", "resolved", "signal", "completed" }, order);
            Assert.AreEqual(money + 125, session.ShiftEarnings);
            Assert.AreEqual(satisfaction + .5f, session.Satisfaction);
            Assert.AreEqual(chaos + 7, session.ChaosScore);
            Assert.AreEqual(250, session.EstimateFare().Requests);
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
            Assert.AreEqual(thirst, inventory.Thirst, "Generic hand-over must not invoke ordinary drink effects.");
            Assert.AreEqual(emptyBottles, inventory.Count(TruckTaxiNeedsItem.EmptyBottle));
            var sameId = Object.Instantiate(definition); allocated.Add(sameId);
            AssertRejectedWithoutMutation(sameId, inventory);
            Assert.AreEqual(4, order.Count, "Rejected repeat must emit no lifecycle/signal callbacks.");
        }

        [Test]
        public void StartedCallback_ObservesCommittedInventoryCashSatisfactionAndChaos()
        {
            Board();
            var definition = Interaction(); definition.consumeQuantity = 2;
            var request = Goal(1, definition);
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 5));
            long money = session.ShiftEarnings, observedMoney = -1;
            float satisfaction = session.Satisfaction, observedSatisfaction = -1;
            int chaos = session.ChaosScore, observedChaos = -1, observedStock = -1;
            var order = new List<string>();
            session.PassengerCustomInteractionStarted += _ =>
            {
                order.Add("started");
                observedStock = inventory.Count(TruckTaxiNeedsItem.WaterBottle);
                observedMoney = session.ShiftEarnings;
                observedSatisfaction = session.Satisfaction; observedChaos = session.ChaosScore;
            };
            session.RequestResolved += _ => order.Add("resolved");
            session.CustomItemInteraction += _ => order.Add("signal");
            session.PassengerCustomInteractionCompleted += _ => order.Add("completed");
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(definition, inventory, out string reason), reason);
            Assert.AreEqual(3, observedStock);
            Assert.AreEqual(money + definition.rewardCashCents, observedMoney);
            Assert.AreEqual(satisfaction + definition.satisfactionDelta, observedSatisfaction);
            Assert.AreEqual(chaos + definition.chaosDelta, observedChaos);
            Assert.AreEqual(observedStock, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(observedMoney, session.ShiftEarnings);
            Assert.AreEqual(observedSatisfaction, session.Satisfaction); Assert.AreEqual(observedChaos, session.ChaosScore);
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
            CollectionAssert.AreEqual(new[] { "started", "resolved", "signal", "completed" }, order);
        }

        [Test]
        public void InProgressGuard_RejectsRepeatableAndDifferentActionsDuringStartedThenReleases()
        {
            Board();
            var repeatable = Interaction(); repeatable.oncePerRide = false;
            var different = Interaction(); different.stableId = "test.second-water-hand-over";
            var request = Goal(1, repeatable);
            passenger.customItemInteractions = new[] { repeatable, different };
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 5));
            long money = session.ShiftEarnings;
            bool attemptingNested = false, repeatedAccepted = true, differentAccepted = true;
            string repeatedReason = null, differentReason = null;
            var order = new List<string>();
            Action<string> started = _ =>
            {
                order.Add("started");
                // Bound recursion even if the production guard regresses.
                if (attemptingNested) return;
                attemptingNested = true;
                try
                {
                    repeatedAccepted = session.TryExecuteCustomItemInteraction(repeatable, inventory, out repeatedReason);
                    differentAccepted = session.TryExecuteCustomItemInteraction(different, inventory, out differentReason);
                }
                finally { attemptingNested = false; }
            };
            session.PassengerCustomInteractionStarted += started;
            session.RequestResolved += _ => order.Add("resolved");
            session.CustomItemInteraction += _ => order.Add("signal");
            session.PassengerCustomInteractionCompleted += _ => order.Add("completed");
            try
            {
                Assert.IsTrue(session.TryExecuteCustomItemInteraction(repeatable, inventory, out string reason), reason);
            }
            finally { session.PassengerCustomInteractionStarted -= started; }
            Assert.IsFalse(repeatedAccepted); Assert.IsNotEmpty(repeatedReason);
            Assert.IsFalse(differentAccepted); Assert.IsNotEmpty(differentReason);
            Assert.AreEqual(4, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(money + repeatable.rewardCashCents, session.ShiftEarnings);
            Assert.AreEqual(1, request.Progress); Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
            CollectionAssert.AreEqual(new[] { "started", "resolved", "signal", "completed" }, order);
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(repeatable, inventory, out string repeatReason), repeatReason);
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(different, inventory, out string otherReason), otherReason);
            Assert.AreEqual(2, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(money + repeatable.rewardCashCents * 2 + different.rewardCashCents, session.ShiftEarnings);
        }

        [Test]
        public void ThrowingStartedListener_LogsOnceAndPreservesTransactionObjectiveAndCallbackOrder()
        {
            Board();
            var definition = Interaction(); definition.consumeQuantity = 2;
            var request = Goal(1, definition);
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 5));
            long money = session.ShiftEarnings;
            float satisfaction = session.Satisfaction;
            int chaos = session.ChaosScore;
            var order = new List<string>();
            session.PassengerCustomInteractionStarted += _ =>
            {
                order.Add("started");
                throw new InvalidOperationException("Custom content test Started listener failed.");
            };
            session.RequestResolved += _ => order.Add("resolved");
            session.CustomItemInteraction += _ => order.Add("signal");
            session.PassengerCustomInteractionCompleted += _ => order.Add("completed");
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Custom content test Started listener failed\\."));
            bool accepted = false;
            string reason = null;
            Assert.DoesNotThrow(() => accepted = session.TryExecuteCustomItemInteraction(definition, inventory, out reason));
            Assert.IsTrue(accepted, reason); Assert.That(reason, Is.Null.Or.Empty);
            Assert.AreEqual(3, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(money + definition.rewardCashCents, session.ShiftEarnings);
            Assert.AreEqual(satisfaction + definition.satisfactionDelta, session.Satisfaction);
            Assert.AreEqual(chaos + definition.chaosDelta, session.ChaosScore);
            Assert.AreEqual(1, request.Progress); Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
            Assert.AreEqual(definition.rewardCashCents, session.ShiftEarnings - money);
            Assert.AreEqual(request.Definition.bonusMoneyCents, session.EstimateFare().Requests);
            CollectionAssert.AreEqual(new[] { "started", "resolved", "signal", "completed" }, order);
            AssertRejectedWithoutMutation(definition, inventory);
            Assert.AreEqual(4, order.Count);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RepeatableDefinition_ConsumesAndRewardsEachSuccessfulOccurrence()
        {
            Board();
            var definition = Interaction(); definition.oncePerRide = false;
            var request = Goal(2, definition);
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 3));
            int signals = 0;
            session.CustomItemInteraction += _ => signals++;
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(definition, inventory, out _));
            Assert.AreEqual(1, request.Progress);
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(definition, inventory, out _));
            Assert.AreEqual(1, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(250, session.ShiftEarnings);
            Assert.AreEqual(2, signals);
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        public void ZeroConsumeQuantity_StillRequiresPossessionWithoutDebiting(int stock, bool allowed)
        {
            Board();
            var definition = Interaction(); definition.consumeQuantity = 0;
            if (stock > 0) Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, stock));
            Assert.AreEqual(allowed, session.TryExecuteCustomItemInteraction(definition, inventory, out _));
            Assert.AreEqual(stock, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(allowed ? 125 : 0, session.ShiftEarnings);
        }

        [Test]
        public void NewRide_ResetsBothInteractionAndOccurrenceHistory()
        {
            Board();
            var definition = Interaction();
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 3));
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(definition, inventory, out _));
            Assert.IsTrue(session.RaiseCustomItemEvent(EventId, ItemId, "reusable-next-ride"));
            string firstRide = session.CurrentRideId;
            session.DebugComplete(); session.ContinueShift(); Board();
            Assert.AreNotEqual(firstRide, session.CurrentRideId);
            var request = Goal(2, definition);
            Assert.IsTrue(session.RaiseCustomItemEvent(EventId, ItemId, "reusable-next-ride"));
            long before = session.ShiftEarnings;
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(definition, inventory, out string reason), reason);
            Assert.AreEqual(before + 125, session.ShiftEarnings);
            Assert.AreEqual(1, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
        }

        [TestCase(TruckTaxiPassengerGender.Male, TruckTaxiPassengerGender.Male, true)]
        [TestCase(TruckTaxiPassengerGender.Male, TruckTaxiPassengerGender.Female, false)]
        [TestCase(TruckTaxiPassengerGender.Female, TruckTaxiPassengerGender.Female, true)]
        [TestCase(TruckTaxiPassengerGender.Female, TruckTaxiPassengerGender.Male, false)]
        [TestCase(TruckTaxiPassengerGender.Male, TruckTaxiPassengerGender.Unassigned, true)]
        [TestCase(TruckTaxiPassengerGender.Female, TruckTaxiPassengerGender.Unassigned, true)]
        public void GenderGate_UsesAuthoredGenderEvenWhenCastingDisagrees(TruckTaxiPassengerGender authored,
            TruckTaxiPassengerGender required, bool allowed)
        {
            passenger.gender = authored;
            passenger.casting = Asset<TruckTaxiCastingProfile>();
            passenger.casting.sexPresentation = authored == TruckTaxiPassengerGender.Male ? "female" : "male";
            Board();
            var definition = Interaction(); definition.requiredGender = required;
            AssertEligibility(definition, allowed);
        }

        [TestCase(false, 40, 21, false)]
        [TestCase(true, 20, 21, false)]
        [TestCase(true, 21, 21, true)]
        [TestCase(true, 25, 21, true)]
        [TestCase(false, 0, 0, true)]
        public void AgeGate_UsesExplicitMetadataAndInclusiveMinimum(bool explicitAdult, int age, int minimum, bool allowed)
        {
            passenger.explicitlyAdult = explicitAdult; passenger.minimumAdultAge = age;
            Board();
            var definition = Interaction(); definition.minimumAge = minimum;
            AssertEligibility(definition, allowed);
        }

        [TestCase("case-insensitive-all", true)]
        [TestCase("missing-one", false)]
        [TestCase("substring", false)]
        [TestCase("blank", false)]
        [TestCase("null-passenger-tags", false)]
        [TestCase("null-required-tags", true)]
        public void TagGate_RequiresEveryAuthoredTagWithoutSubstringInference(string scenario, bool allowed)
        {
            Board();
            var definition = Interaction();
            definition.requiredPassengerTags = new[] { "commuter", "PREPARED" };
            switch (scenario)
            {
                case "missing-one": definition.requiredPassengerTags = new[] { "commuter", "unlisted" }; break;
                case "substring": definition.requiredPassengerTags = new[] { "commute" }; break;
                case "blank": definition.requiredPassengerTags = new[] { " " }; break;
                case "null-passenger-tags": passenger.specialTraits = null; break;
                case "null-required-tags": definition.requiredPassengerTags = null; break;
            }
            AssertEligibility(definition, allowed);
        }

        [TestCase("null-definition")]
        [TestCase("blank-id")]
        [TestCase("null-inventory")]
        [TestCase("unknown-item")]
        [TestCase("item-display-name")]
        [TestCase("item-case")]
        [TestCase("null-item")]
        [TestCase("negative-quantity")]
        [TestCase("negative-reward")]
        [TestCase("nan-satisfaction")]
        [TestCase("infinite-satisfaction")]
        public void InvalidInteraction_IsRejectedWithoutInventoryRewardsOrEvents(string scenario)
        {
            Board();
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 3));
            var definition = Interaction();
            var suppliedInventory = inventory;
            switch (scenario)
            {
                case "null-definition": definition = null; break;
                case "blank-id": definition.stableId = " "; break;
                case "null-inventory": suppliedInventory = null; break;
                case "unknown-item": definition.requiredItemStableId = "missing-test-item"; break;
                case "item-display-name": definition.requiredItemStableId = "Water bottle"; break;
                case "item-case": definition.requiredItemStableId = "WATER-BOTTLE"; break;
                case "null-item": definition.requiredItemStableId = null; break;
                case "negative-quantity": definition.consumeQuantity = -1; break;
                case "negative-reward": definition.rewardCashCents = -1; break;
                case "nan-satisfaction": definition.satisfactionDelta = float.NaN; break;
                case "infinite-satisfaction": definition.satisfactionDelta = float.PositiveInfinity; break;
            }
            AssertRejectedWithoutMutation(definition, suppliedInventory);
        }

        [Test]
        public void CashOverflow_IsRejectedBeforeTheNextInventoryDebit()
        {
            Board();
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 3));
            var first = Interaction(); first.rewardCashCents = long.MaxValue - 5;
            Assert.IsTrue(session.TryExecuteCustomItemInteraction(first, inventory, out _));
            var second = Interaction(); second.stableId = "test.second"; second.rewardCashCents = 6;
            AssertRejectedWithoutMutation(second, inventory);
        }

        [Test]
        public void ObjectiveAssignment_RequiresRegisteredExactPairAndPassengerAuthoring()
        {
            Board();
            var definition = Objective(1);
            session.Capabilities.Register(TruckTaxiObjectiveCapability.CustomItemActions, 1);
            Assert.IsFalse(session.GenerateRequest(definition), "A capability flag alone is not an executable event/item pair.");
            Assert.IsTrue(session.Capabilities.RegisterCustomItemAction(EventId, "soda"));
            Assert.IsFalse(session.GenerateRequest(definition));
            Assert.IsTrue(session.Capabilities.RegisterCustomItemAction(EventId, ItemId));
            passenger.customItemInteractions = null;
            Assert.IsFalse(session.GenerateRequest(definition));
            passenger.customItemInteractions = new TruckTaxiCustomItemInteractionDefinition[] { null };
            Assert.IsFalse(session.GenerateRequest(definition));
            var interaction = Interaction(); interaction.customEventId = "test.other";
            passenger.customItemInteractions = new[] { interaction };
            Assert.IsFalse(session.GenerateRequest(definition));
            interaction.customEventId = EventId;
            Assert.IsTrue(session.GenerateRequest(definition));
            Assert.IsTrue(session.Capabilities.RegisterCustomItemAction(EventId, ItemId, false));
            Assert.IsFalse(session.Capabilities.Supports(definition));
        }

        [TestCase("null")]
        [TestCase("blank-objective")]
        [TestCase("blank-event")]
        [TestCase("unknown-item")]
        [TestCase("zero-target")]
        [TestCase("nan-target")]
        [TestCase("invalid-deadline")]
        public void InvalidObjective_CannotBeAssigned(string scenario)
        {
            Board();
            passenger.customItemInteractions = new[] { Interaction() };
            session.Capabilities.RegisterCustomItemAction(EventId, ItemId);
            var definition = Objective(1);
            switch (scenario)
            {
                case "null": definition = null; break;
                case "blank-objective": definition.objectiveId = " "; break;
                case "blank-event": definition.requiredCustomEventId = " "; break;
                case "unknown-item": definition.requiredItemStableId = "missing-test-item"; break;
                case "zero-target": definition.target = 0; break;
                case "nan-target": definition.target = float.NaN; break;
                case "invalid-deadline": definition.hasDeadline = true; definition.timer = 0; break;
            }
            Assert.IsFalse(session.CanAssign(definition, out string reason));
            Assert.IsNotEmpty(reason); Assert.IsEmpty(session.Requests);
        }

        [TestCase(null, true, 1)]
        [TestCase("", true, 1)]
        [TestCase(ItemId, true, 1)]
        [TestCase(null, false, 2)]
        [TestCase("", false, 2)]
        [TestCase(ItemId, false, 2)]
        public void Runner_RegistersOnlyEligibleAuthoredActionsAndCapsGoalsToAvailableOccurrences(
            string itemFilter, bool oncePerRide, int expectedTarget)
        {
            Board();
            var runner = RunnerWithSession();
            var objective = Objective(5, itemFilter);
            passenger.customItemInteractions = null;
            Assert.DoesNotThrow(runner.RefreshObjectiveCapabilities);
            Assert.IsFalse(session.Capabilities.Supports(objective));
            var interaction = Interaction();
            interaction.oncePerRide = oncePerRide; interaction.consumeQuantity = 2;
            passenger.customItemInteractions = new[] { null, interaction };
            runner.RefreshObjectiveCapabilities();
            Assert.IsFalse(session.Capabilities.Supports(objective), "No inventory means no available producer.");
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 5));
            interaction.requiredPassengerTags = new[] { "unlisted" };
            runner.RefreshObjectiveCapabilities();
            Assert.IsFalse(session.Capabilities.Supports(objective), "An authored but ineligible action is unavailable.");
            interaction.requiredPassengerTags = null;
            runner.RefreshObjectiveCapabilities();
            Assert.IsTrue(session.Capabilities.Supports(objective));
            Assert.IsTrue(session.CanAssign(objective, out string reason), reason);
            Assert.IsTrue(inventory.TryConsumeItem(ItemId, 5));
            Assert.IsFalse(session.CanAssign(objective, out _), "Assignment must recheck inventory after registration.");
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 5));
            Assert.IsTrue(session.GenerateRequest(objective));
            var request = session.Requests.Last();
            Assert.AreEqual(expectedTarget, request.Target);
            for (int i = 0; i < expectedTarget; i++)
                Assert.IsTrue(session.TryExecuteCustomItemInteraction(interaction, inventory, out reason), reason);
            Assert.AreEqual(TaxiRequestState.Succeeded, request.State);
            Assert.AreEqual(5 - expectedTarget * 2, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            runner.RefreshObjectiveCapabilities();
            Assert.IsFalse(session.Capabilities.Supports(objective), "Exhausted actions must unregister their producer.");
            Assert.IsFalse(session.CanAssign(objective, out _));
        }

        [Test]
        public void PresentationProfile_NullOptionalAssetsAreValid_AndInactivePhysicsChildrenAreRejected()
        {
            var profile = Asset<TruckTaxiCustomPresentationProfile>(); profile.stableId = "test.presentation";
            Assert.IsEmpty(profile.Validate());
            Assert.IsTrue(TruckTaxiCustomPresentationProfile.PrefabIsSafe(null));
            var visual = NewObject("Neutral visual");
            visual.AddComponent<MeshFilter>(); visual.AddComponent<MeshRenderer>(); visual.AddComponent<Animator>();
            profile.presentationPrefab = visual;
            Assert.IsEmpty(profile.Validate());
            var hidden = new GameObject("Inactive child"); hidden.transform.SetParent(visual.transform, false);
            hidden.AddComponent<BoxCollider>(); hidden.SetActive(false);
            Assert.IsFalse(TruckTaxiCustomPresentationProfile.PrefabIsSafe(visual));
            Assert.IsTrue(profile.Validate().Any(message => message.Contains("Invalid prefab")));
        }

        [TestCase("missing-id")]
        [TestCase("zero-duration")]
        [TestCase("nan-duration")]
        [TestCase("infinite-duration")]
        [TestCase("driver-anchor")]
        [TestCase("passenger-anchor")]
        [TestCase("legacy-driver")]
        [TestCase("legacy-passenger")]
        public void PresentationValidation_RejectsInvalidAuthoredValues(string scenario)
        {
            var profile = Asset<TruckTaxiCustomPresentationProfile>(); profile.stableId = "test.presentation";
            switch (scenario)
            {
                case "missing-id": profile.stableId = " "; break;
                case "zero-duration": profile.duration = 0; break;
                case "nan-duration": profile.duration = float.NaN; break;
                case "infinite-duration": profile.duration = float.PositiveInfinity; break;
                case "driver-anchor": profile.driverAnchorId = "NoSuchTestAnchor"; break;
                case "passenger-anchor": profile.passengerAnchorId = "NoSuchTestAnchor"; break;
                default:
                    var clip = new AnimationClip { legacy = true }; allocated.Add(clip);
                    if (scenario == "legacy-driver") profile.driverAnimation = clip; else profile.passengerAnimation = clip;
                    break;
            }
            Assert.IsNotEmpty(profile.Validate().ToArray());
        }

        [Test]
        public void Runner_NullProfilesAndEmptyPhaseAssetsDoNotRequireHostOrPlaybackAssets()
        {
            var runner = Runner();
            var phases = new List<TruckTaxiCustomPhase>();
            runner.PhaseChanged += (_, phase) => phases.Add(phase);
            Assert.DoesNotThrow(() => runner.Begin(null, null, null));
            Assert.IsFalse(runner.IsPlaying); Assert.IsEmpty(phases);
            Assert.IsFalse(runner.TryExecuteItem(null, out string reason)); Assert.IsNotEmpty(reason);
            var profile = Asset<TruckTaxiCustomInteractionProfile>(); profile.stableId = "test.empty-phases";
            profile.variants = new TruckTaxiCustomPresentationProfile[] { null };
            Assert.DoesNotThrow(() => runner.Begin(profile, null, null));
            Assert.IsTrue(runner.IsPlaying);
            runner.Beat(); runner.Complete(); runner.Complete(); runner.Cancel(); runner.Cancel();
            CollectionAssert.AreEqual(new[] { TruckTaxiCustomPhase.Started, TruckTaxiCustomPhase.Beat,
                TruckTaxiCustomPhase.Completed }, phases);
            Assert.IsFalse(runner.IsPlaying); Assert.Zero(runner.ClipsPlayed); Assert.Zero(runner.PresentationsPlayed);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AudioProfile_NullAndEmptyClipPoolsAreSafe(bool shuffle)
        {
            var runner = Runner();
            var profile = Asset<TruckTaxiCustomReactionAudioProfile>();
            profile.startClips = null; profile.loopClips = Array.Empty<AudioClip>();
            profile.accentClips = new AudioClip[] { null, null }; profile.completionClips = null;
            foreach (var phase in new[] { TruckTaxiCustomPhase.Started, TruckTaxiCustomPhase.Beat, TruckTaxiCustomPhase.Completed })
                Assert.IsNull(runner.ChooseClip(profile.Clips(phase), shuffle));
            Assert.IsNull(runner.ChooseClip(profile.Clips(TruckTaxiCustomPhase.Beat, true), shuffle));
            Assert.Zero(runner.ClipsPlayed);
        }

        private void Board()
        {
            session.StartShift();
            Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true);
            session.Tick(2, session.Pickup.StopPosition, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination, session.State);
        }

        private TruckTaxiCustomItemInteractionDefinition Interaction()
        {
            var value = Asset<TruckTaxiCustomItemInteractionDefinition>();
            value.stableId = "test.hand-over-water"; value.displayName = "Hand over water bottle";
            value.requiredItemStableId = ItemId; value.customEventId = EventId;
            value.consumeQuantity = 1; value.rewardCashCents = 125;
            value.satisfactionDelta = .5f; value.chaosDelta = 7; value.oncePerRide = true;
            return value;
        }

        private PassengerRequestDefinition Objective(int target, string itemFilter = ItemId)
        {
            var value = Asset<PassengerRequestDefinition>();
            value.requestType = TaxiRequestType.CustomItemAction; value.objectiveId = "test.water-objective";
            value.requiredCustomEventId = EventId; value.requiredItemStableId = itemFilter;
            value.description = "Hand over water bottles"; value.target = target;
            value.ratingModifier = 0; value.bonusMoneyCents = 250;
            return value;
        }

        private TaxiRequestProgress Goal(int target, TruckTaxiCustomItemInteractionDefinition interaction = null,
            string itemFilter = ItemId)
        {
            passenger.customItemInteractions = new[] { interaction ?? Interaction() };
            Assert.IsTrue(session.Capabilities.RegisterCustomItemAction(EventId, ItemId));
            Assert.IsTrue(session.GenerateRequest(Objective(target, itemFilter)));
            var request = session.Requests.Last(); Assert.AreEqual(target, request.Target);
            return request;
        }

        private void AssertEligibility(TruckTaxiCustomItemInteractionDefinition definition, bool allowed)
        {
            Assert.IsTrue(inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 2));
            Assert.AreEqual(allowed, session.CustomInteractionBlockReason(definition, inventory) == null);
            if (allowed) Assert.IsTrue(session.TryExecuteCustomItemInteraction(definition, inventory, out string reason), reason);
            else AssertRejectedWithoutMutation(definition, inventory);
        }

        private void AssertRejectedWithoutMutation(TruckTaxiCustomItemInteractionDefinition definition, TruckTaxiDriverNeedsState supplied)
        {
            int count = inventory.Count(TruckTaxiNeedsItem.WaterBottle), chaos = session.ChaosScore;
            long money = session.ShiftEarnings;
            float satisfaction = session.Satisfaction;
            int callbacks = 0;
            Action<string> lifecycle = _ => callbacks++;
            Action<TruckTaxiCustomItemSignal> signal = _ => callbacks++;
            session.PassengerCustomInteractionStarted += lifecycle;
            session.PassengerCustomInteractionCompleted += lifecycle;
            session.CustomItemInteraction += signal;
            try
            {
                Assert.IsFalse(session.TryExecuteCustomItemInteraction(definition, supplied, out string reason));
                Assert.IsNotEmpty(reason);
                Assert.AreEqual(count, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
                Assert.AreEqual(money, session.ShiftEarnings); Assert.AreEqual(satisfaction, session.Satisfaction);
                Assert.AreEqual(chaos, session.ChaosScore); Assert.Zero(callbacks);
            }
            finally
            {
                session.PassengerCustomInteractionStarted -= lifecycle;
                session.PassengerCustomInteractionCompleted -= lifecycle;
                session.CustomItemInteraction -= signal;
            }
        }

        private T Asset<T>() where T : ScriptableObject
        { var value = ScriptableObject.CreateInstance<T>(); allocated.Add(value); return value; }
        private GameObject NewObject(string name)
        { var value = new GameObject(name); allocated.Add(value); return value; }
        private TruckTaxiCustomContentRunner Runner()
        { var runner = NewObject("Custom content runner fixture").AddComponent<TruckTaxiCustomContentRunner>(); runners.Add(runner); return runner; }
        private TruckTaxiCustomContentRunner RunnerWithSession()
        {
            // Inject only fixture dependencies; never activate Bootstrap's full game startup.
            var hostObject = NewObject("Inactive custom content host"); hostObject.SetActive(false);
            var host = hostObject.AddComponent<TruckTaxiBootstrap>();
            var needs = hostObject.AddComponent<TruckTaxiDriverNeedsCoordinator>();
            typeof(TruckTaxiBootstrap).GetProperty(nameof(TruckTaxiBootstrap.Session)).SetValue(host, session);
            typeof(TruckTaxiBootstrap).GetProperty(nameof(TruckTaxiBootstrap.DriverNeeds)).SetValue(host, needs);
            typeof(TruckTaxiDriverNeedsCoordinator).GetProperty(nameof(TruckTaxiDriverNeedsCoordinator.State)).SetValue(needs, inventory);
            var runner = Runner(); runner.Initialize(host); return runner;
        }
    }
}
