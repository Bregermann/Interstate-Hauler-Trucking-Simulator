using System;
using LWS.TruckDragonEggDelivery;
using NUnit.Framework;

public sealed class DragonEggDeliveryRulesTests
{
    static DragonEggDeliveryRules Rules(int count = 1, int seed = 812, int option = 2) => new DragonEggDeliveryRules(count, seed, option, new[] { "nest-0", "nest-1", "nest-2", "nest-3" });
    [Test]
    public void SameSeedPreservesDistinctCargoAndNestAssignmentsAndDifferentSeedsVaryHatching()
    {
        var a = Rules(4); var b = Rules(4); var c = Rules(4, 913);
        for (int i = 0; i < 4; i++)
        {
            Assert.That(a.Cargo[i].Id, Is.EqualTo(b.Cargo[i].Id)); Assert.That(a.Cargo[i].HatchAt, Is.EqualTo(b.Cargo[i].HatchAt));
            Assert.That(a.Cargo[i].HatchAt, Is.Not.EqualTo(c.Cargo[i].HatchAt)); Assert.That(a.Cargo[i].NestId, Is.EqualTo("nest-" + i));
        }
        Assert.Throws<ArgumentException>(() => new DragonEggDeliveryRules(2, 1, 0, new[] { "same", "same" }));
    }
    [Test]
    public void CargoCannotScoreWhenAbsentUnsupportedUnsettledOrAtWrongNestAndCannotScoreTwice()
    {
        var rules = Rules(); rules.Advance(60); rules.ObserveGentleHandling(0, 60, true);
        Assert.That(rules.Deliver(0, "nest-0", false, true, true, out _), Is.False);
        Assert.That(rules.Deliver(0, "nest-0", true, false, true, out _), Is.False);
        Assert.That(rules.Deliver(0, "nest-0", true, true, false, out _), Is.False);
        Assert.That(rules.Deliver(0, "wrong-nest", true, true, true, out _), Is.False);
        Assert.That(rules.Cargo[0].Score, Is.Zero);
        Assert.That(rules.Deliver(0, "nest-0", true, true, true, out var award), Is.True); Assert.That(award, Is.GreaterThan(700));
        Assert.That(rules.Deliver(0, "nest-0", true, true, true, out var duplicate), Is.False); Assert.That(duplicate, Is.Zero);
        Assert.That(rules.Cargo[0].Score, Is.EqualTo(award));
    }
    [Test]
    public void HatchingRequiresActualVisibleBabyAndCompletedAnimationAndPreservesTheCargoIdentity()
    {
        var rules = Rules(); string id = rules.Cargo[0].Id; rules.Advance(130);
        Assert.That(rules.Cargo[0].Stage, Is.EqualTo(DragonEggStage.Egg), "A deadline alone cannot animate or hatch the cargo.");
        Assert.That(rules.BeginCracking(0), Is.True); Assert.That(rules.BeginHatching(0), Is.True);
        Assert.That(rules.Deliver(0, "nest-0", true, true, true, out _), Is.False, "Hatching transition cannot be banked before the actual baby appears.");
        Assert.That(rules.CompleteHatch(0, false, true), Is.False); Assert.That(rules.CompleteHatch(0, true, false), Is.False);
        Assert.That(rules.CompleteHatch(0, true, true), Is.True); Assert.That(rules.Cargo[0].Id, Is.EqualTo(id));
        Assert.That(rules.Deliver(0, "nest-0", true, true, true, out var award), Is.True);
        Assert.That(rules.Cargo[0].DeliveredBaby, Is.True); Assert.That(award, Is.GreaterThan(600));
    }
    [Test]
    public void PhysicalImpactDedupDoesNotEraseDamageAndSevereDamageCannotDeliver()
    {
        var rules = Rules(); rules.Advance(2);
        Assert.That(rules.Impact(0, 10), Is.True); double condition = rules.Cargo[0].Condition;
        Assert.That(rules.Impact(0, 10), Is.False); Assert.That(rules.Cargo[0].RoughImpacts, Is.EqualTo(1));
        rules.ObserveGentleHandling(0, 100, true); Assert.That(rules.Cargo[0].Condition, Is.EqualTo(condition));
        rules.Advance(.2); Assert.That(rules.Impact(0, 25), Is.True); Assert.That(rules.Cargo[0].Stage, Is.EqualTo(DragonEggStage.Lost));
        Assert.That(rules.Deliver(0, "nest-0", true, true, true, out _), Is.False);
        Assert.That(rules.Impact(0, double.NaN), Is.False); Assert.That(rules.Cargo[0].Score, Is.Zero);
    }
    [Test]
    public void ConditionTimeAndGentleHandlingAffectValueWhileExpiryPreservesGenuineDeliveries()
    {
        var gentle = Rules(); var rough = Rules(); gentle.Advance(60); gentle.ObserveGentleHandling(0, 60, true);
        rough.Advance(300); rough.Impact(0, 10);
        Assert.That(gentle.Deliver(0, "nest-0", true, true, true, out int high), Is.True);
        Assert.That(rough.Deliver(0, "nest-0", true, true, true, out int low), Is.True); Assert.That(high, Is.GreaterThan(low));
        var two = Rules(2); two.Advance(60); Assert.That(two.Deliver(0, "nest-0", true, true, true, out int retained), Is.True);
        two.Advance(1000); two.ExpireUnfinished(); Assert.That(two.Complete, Is.True);
        Assert.That(two.Cargo[0].Score, Is.EqualTo(retained)); Assert.That(two.Cargo[1].Score, Is.Zero);
        CollectionAssert.AreEqual(new[] { 0 }, two.Winners());
    }
}
