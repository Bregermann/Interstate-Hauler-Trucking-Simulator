using NUnit.Framework;
public sealed class SportsFinalRankContractTests
{
    [Test] public void ArchaeologyUsesOnlyActiveScoresAndCooperativeHasNoIndividualRank()
    {
        var rules = new LWS.TruckArchaeology.ArchaeologyRules(2, false);
        rules.Scores[0] = rules.Scores[1] = 20; rules.Scores[3] = 999;
        Assert.That(rules.Place(0), Is.EqualTo(1)); Assert.That(rules.Place(1), Is.EqualTo(1));
        rules.Scores[1] = 30; Assert.That(rules.Place(0), Is.EqualTo(2));
        Assert.That(new LWS.TruckArchaeology.ArchaeologyRules(2, true).Place(0), Is.Zero);
    }
    [Test] public void HeistFinalRankUsesBankedPointsIncludingTies()
    {
        var rules = new LWS.TruckHoardHeist.HoardHeistRules(3, 1, 20, new[] { 10 }, new[] { 1f }, new[] { 42 });
        rules.Scores[0] = 10; rules.Scores[1] = rules.Scores[2] = 20;
        Assert.That(rules.Place(0), Is.EqualTo(3)); Assert.That(rules.Place(1), Is.EqualTo(1)); Assert.That(rules.Place(2), Is.EqualTo(1));
    }
    [Test] public void GooseFinalRankUsesWholeSeriesNotLastRoundWinner()
    {
        var rules = new LWS.TruckTruckGoose.GooseRules(8, 3);
        rules.Scores[2] = 3; rules.Scores[5] = 1;
        Assert.That(rules.Winner, Is.EqualTo(-1));
        Assert.That(rules.FinalPlace(2), Is.EqualTo(1)); Assert.That(rules.FinalPlace(5), Is.EqualTo(2));
        rules.Scores[5] = 3; Assert.That(rules.FinalPlace(5), Is.EqualTo(1));
    }
}
