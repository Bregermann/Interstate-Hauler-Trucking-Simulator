using NUnit.Framework;
using LWS.TruckMechanicalBull;

public sealed class MechanicalBullRulesTests
{
    [TestCase(1)] [TestCase(4)]
    public void EqualPhysicalRidesRotateOwnersAndFinishExactRounds(int count)
    {
        var rules = new MechanicalBullRules(); rules.Begin(count, 3);
        for (int turn = 0; turn < count * 3; turn++)
        {
            Assert.That(rules.Owner, Is.EqualTo(turn % count)); Assert.That(rules.Round, Is.EqualTo(turn / count + 1));
            for (int step = 0; step < 500; step++) rules.Tick(.02f, true, false, 1);
            Assert.That(rules.Survival, Is.GreaterThan(9.9f));
            Assert.That(rules.Tick(.02f, false, true, .1f), Is.True);
            int scored = rules.totals[rules.Owner]; Assert.That(scored, Is.GreaterThan(90));
            rules.Tick(.02f, false, true, 0); Assert.That(rules.totals[rules.Owner], Is.EqualTo(scored), "A repeated fall cannot award twice.");
            Assert.That(rules.Next(), Is.True);
        }
        Assert.That(rules.Complete, Is.True); Assert.That(rules.Next(), Is.False);
        for (int i = 0; i < count; i++) { Assert.That(rules.totals[i], Is.EqualTo(rules.totals[0])); Assert.That(rules.Place(i), Is.EqualTo(1)); }
    }
    [Test] public void RecoveryRequiresRiskThenContinuousActualContactAndUprightHold()
    {
        var rules = new MechanicalBullRules(); rules.Begin(1, 1);
        for (int i = 0; i < 100; i++) rules.Tick(.02f, true, false, 1);
        Assert.That(rules.Recoveries, Is.Zero);
        rules.Tick(.02f, true, false, .65f);
        for (int i = 0; i < 30; i++) rules.Tick(.02f, false, false, 1);
        Assert.That(rules.Recoveries, Is.Zero, "Airborne orientation alone is not a contact recovery.");
        for (int i = 0; i < 36; i++) rules.Tick(.02f, true, false, 1);
        Assert.That(rules.Recoveries, Is.EqualTo(1));
        for (int i = 0; i < 100; i++) rules.Tick(.02f, true, false, 1);
        Assert.That(rules.Recoveries, Is.EqualTo(1), "Stable idling cannot farm recovery points.");
    }
    [Test] public void InvalidClocksDoNotAdvanceAndSixtySecondsResolvesOnlyOnce()
    {
        var rules = new MechanicalBullRules(); rules.Begin(1, 1);
        foreach (var dt in new[] { float.NaN, float.PositiveInfinity, -1, 0, 1 }) rules.Tick(dt, true, false, 1);
        Assert.That(rules.Survival, Is.Zero);
        for (int i = 0; i < 600; i++) rules.Tick(.1f, true, false, 1);
        for (int i = 0; i < 2; i++) rules.Tick(.1f, true, false, 1);
        Assert.That(rules.Resolved, Is.True); Assert.That(rules.Fell, Is.False); Assert.That(rules.Patterns, Is.EqualTo(6));
        Assert.That(rules.totals[0], Is.EqualTo(690));
    }
}
