using System.Collections.Generic;
using NUnit.Framework;
using LWS.TruckFareFight;
public sealed class FareFightRulesTests
{
    FareFightRules rules;
    [SetUp] public void Setup() { rules = new FareFightRules(); rules.Start(4); rules.AssignPassenger(0, 7); }
    FareFightEvent E(FareFightEventKind kind, int slot, float fraction = .5f, int victim = -1, float speed = 3) =>
        new FareFightEvent { kind = kind, participant = slot, victim = victim, normalSpeed = speed, fraction = fraction, passenger = rules.Passenger };
    void Frame(float dt, params FareFightEvent[] events) => rules.Advance(dt, new List<FareFightEvent>(events));
    void Pickup(int slot = 0) => Frame(.02f, E(FareFightEventKind.Pickup, slot));
    [Test] public void FirstPhysicalPickupWinsAtomically() { Frame(.02f, E(FareFightEventKind.Pickup, 0, .8f), E(FareFightEventKind.Pickup, 1, .2f)); Assert.That(rules.Carrier, Is.EqualTo(1)); }
    [Test] public void ExactPickupTieUsesStableParticipantId() { Frame(.02f, E(FareFightEventKind.Pickup, 2), E(FareFightEventKind.Pickup, 1)); Assert.That(rules.Carrier, Is.EqualTo(1)); }
    [Test] public void MeaningfulPhysicalContactStealsWithoutChangingDestination() { Pickup(); Frame(2); Frame(.02f, E(FareFightEventKind.Theft, 1, victim: 0)); Assert.That(rules.Carrier, Is.EqualTo(1)); Assert.That(rules.Destination, Is.EqualTo(7)); Assert.That(rules.steals[1], Is.EqualTo(1)); }
    [Test] public void CooldownPreventsImmediatePingPong() { Pickup(); Frame(.02f, E(FareFightEventKind.Theft, 1, victim: 0)); Assert.That(rules.Carrier, Is.Zero); Frame(2); Frame(.02f, E(FareFightEventKind.Theft, 1, victim: 0)); Frame(.02f, E(FareFightEventKind.Theft, 0, victim: 1)); Assert.That(rules.Carrier, Is.EqualTo(1)); }
    [TestCase(0)][TestCase(1.49f)][TestCase(float.NaN)] public void WeakOverlapCannotSteal(float speed) { Pickup(); Frame(2); Frame(.02f, E(FareFightEventKind.Theft, 1, victim: 0, speed: speed)); Assert.That(rules.Carrier, Is.Zero); }
    [Test] public void TrafficAndUnknownSlotsCannotOwnPassenger() { Pickup(); Frame(2); Frame(.02f, E(FareFightEventKind.Theft, 4, victim: 0)); Assert.That(rules.Carrier, Is.Zero); }
    [Test] public void DestroyedCarrierResetsSamePassengerAndDestination() { Pickup(); int token = rules.Passenger; Frame(.02f, E(FareFightEventKind.CarrierReset, 0)); Assert.That(rules.Carrier, Is.EqualTo(-1)); Assert.That(rules.Destination, Is.EqualTo(7)); Assert.That(rules.Passenger, Is.EqualTo(token)); }
    [Test] public void EarlierDeliveryCannotBeRevokedByLaterCollision() { Pickup(); Frame(2); Frame(.02f, E(FareFightEventKind.Theft, 1, .9f, 0), E(FareFightEventKind.Delivery, 0, .1f)); Assert.That(rules.score[0], Is.EqualTo(1)); Assert.That(rules.Carrier, Is.EqualTo(-1)); }
    [Test] public void EarlierTheftPreventsOldCarrierDelivery() { Pickup(); Frame(2); Frame(.02f, E(FareFightEventKind.Delivery, 0, .9f), E(FareFightEventKind.Theft, 1, .1f, 0)); Assert.That(rules.score[0], Is.Zero); Assert.That(rules.Carrier, Is.EqualTo(1)); }
    [Test] public void SimultaneousDestructionAndDeliveryUsesDocumentedResetPrecedence() { Pickup(); Frame(.02f, E(FareFightEventKind.Delivery, 0), E(FareFightEventKind.CarrierReset, 0)); Assert.That(rules.Deliveries, Is.Zero); Assert.That(rules.Resets, Is.EqualTo(1)); }
    [Test] public void StalePassengerEventsCannotAffectNextFare() { Pickup(); var old = E(FareFightEventKind.Pickup, 1); Frame(.02f, E(FareFightEventKind.Delivery, 0)); Frame(2); Assert.That(rules.AssignPassenger(2, 5)); Frame(.02f, old); Assert.That(rules.Carrier, Is.EqualTo(-1)); }
    [Test] public void DeliveryScoresOnceAndFreezesUntilNextPassenger() { Pickup(); Frame(.02f, E(FareFightEventKind.Delivery, 0), E(FareFightEventKind.Delivery, 0)); Assert.That(rules.score[0], Is.EqualTo(1)); Assert.That(rules.AssignPassenger(1, 4), Is.False); }
    [Test] public void FirstToFiveEndsAtFive() { for (int i = 0; i < 5; i++) { Pickup(); Frame(.02f, E(FareFightEventKind.Delivery, 0)); if (i < 4) { Frame(2); rules.AssignPassenger(0, 7); } } Assert.That(rules.Phase, Is.EqualTo(FareFightPhase.Complete)); Assert.That(rules.score[0], Is.EqualTo(5)); Assert.That(rules.winners[0]); }
    [Test] public void TimedTieEntersSuddenDeath() { rules.Start(4, 5, 10); rules.AssignPassenger(0, 7); Frame(10); Assert.That(rules.SuddenDeath); Pickup(2); Frame(.02f, E(FareFightEventKind.Delivery, 2)); Assert.That(rules.winners[2]); }
    [Test] public void TimerRejectsLateDeliveryWhenLeaderAlreadyWon() { rules.Start(2, 5, 10); rules.AssignPassenger(0, 7); Pickup(); Frame(.02f, E(FareFightEventKind.Delivery, 0)); Frame(2); rules.AssignPassenger(0, 7); Pickup(1); Frame(8); Frame(.02f, E(FareFightEventKind.Delivery, 1)); Assert.That(rules.score[1], Is.Zero); Assert.That(rules.winners[0]); }
    [Test] public void SoloPracticeDoesNotEndAfterFiveDeliveries() { rules.Start(1, 5, 0, true); for (int i = 0; i < 6; i++) { rules.AssignPassenger(0, 7); Pickup(); Frame(.02f, E(FareFightEventKind.Delivery, 0)); Frame(2); } Assert.That(rules.score[0], Is.EqualTo(6)); Assert.That(rules.Phase, Is.Not.EqualTo(FareFightPhase.Complete)); }
}
