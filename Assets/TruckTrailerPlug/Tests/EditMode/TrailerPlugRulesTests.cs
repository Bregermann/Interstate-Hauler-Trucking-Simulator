using NUnit.Framework;
using LWS.TruckTrailerPlug;
public sealed class TrailerPlugRulesTests
{
    TrailerPlugRules rules;
    [SetUp] public void Setup() { rules = new TrailerPlugRules(); rules.Start(4, 3); }
    [Test] public void HitchCanReleaseOnce() { Assert.That(rules.Release()); Assert.That(rules.Release(), Is.False); }
    [Test] public void PedestrianCanScoreOnlyOnce() { rules.Release(); Assert.That(rules.Hit(3, 4)); Assert.That(rules.Hit(3, 9), Is.False); Assert.That(rules.AttemptPoints, Is.EqualTo(100)); }
    [Test] public void ContactBeforeReleaseCannotScore() { Assert.That(rules.Hit(0, 8), Is.False); }
    [TestCase(0)][TestCase(1.99f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
    public void InsufficientOrInvalidImpactCannotScore(float speed) { rules.Release(); Assert.That(rules.Hit(0, speed), Is.False); }
    [Test] public void CompleteStableCenteredPlugAwardsSealAndPrecision() { rules.Release(); rules.Resolve(1, true, true, .1f, 1); Assert.That(rules.Sealed && rules.Perfect); Assert.That(rules.AttemptPoints, Is.EqualTo(7000)); }
    [Test] public void HoveringOverOpeningDoesNotSeal() { rules.Release(); rules.Resolve(1, false, true, 0, 1); Assert.That(rules.AttemptPoints, Is.Zero); }
    [Test] public void MovingTrailerCannotBankCoverage() { rules.Release(); rules.Resolve(1, true, false, 0, 1); Assert.That(rules.Coverage, Is.Zero); }
    [Test] public void PartialRestingPlugHasPartialPoints() { rules.Release(); rules.Resolve(.52f, true, true, 2, 1); Assert.That(rules.AttemptPoints, Is.InRange(500, 2000)); Assert.That(rules.Sealed, Is.False); }
    [Test] public void TiltedSealCannotGetPrecisionBonus() { rules.Release(); rules.Resolve(1, true, true, 0, .4f); Assert.That(rules.AttemptPoints, Is.EqualTo(5000)); }
    [Test] public void ResolveIsAtomicAndFreezesFurtherContacts() { rules.Release(); rules.Hit(1, 5); Assert.That(rules.Resolve(0, false, false, 0, 1)); Assert.That(rules.Resolve(1, true, true, 0, 1), Is.False); Assert.That(rules.Hit(2, 9), Is.False); Assert.That(rules.score[0], Is.EqualTo(100)); }
    [Test] public void FourParticipantsReceiveExactlyThreeAttemptsEach()
    {
        int turns = 0;
        do { rules.Release(); rules.Resolve(0, false, false, 0, 1); turns++; } while (rules.Next());
        Assert.That(turns, Is.EqualTo(12)); Assert.That(rules.Complete);
        for (int i = 0; i < 4; i++) { Assert.That(rules.attempts[i], Is.EqualTo(3)); Assert.That(rules.winners[i]); }
        Assert.That(rules.Release(), Is.False); Assert.That(rules.Next(), Is.False);
    }
    [Test] public void RematchClearsHitsScoresAndTurnState()
    {
        rules.Release(); rules.Hit(4, 5); rules.Resolve(1, true, true, 0, 1); rules.Next();
        rules.Start(1, 5); Assert.That(rules.Participant, Is.Zero); Assert.That(rules.Round, Is.EqualTo(1));
        Assert.That(rules.score[0], Is.Zero); rules.Release(); Assert.That(rules.Hit(4, 5));
    }
    [Test] public void WinnerUsesParticipantIdentityRatherThanNames()
    {
        do { rules.Release(); if (rules.Participant == 2) rules.Hit(1, 4); rules.Resolve(0, false, false, 0, 1); } while (rules.Next());
        Assert.That(rules.winners[2]); Assert.That(rules.winners[0], Is.False);
    }
}
