using System;
using LWS.TruckSports;
using NUnit.Framework;
public sealed class SportsProfileStatisticsTests
{
    [Test] public void CompetitiveOutcomesAndNeutralActivitiesHaveSeparateCounters()
    {
        var s = new SportsProfileStatistics();
        Apply(s, "win", SportsMatchKind.Competitive, true, true);
        Apply(s, "loss", SportsMatchKind.Competitive, false, true);
        Apply(s, "draw", SportsMatchKind.Competitive, false, false);
        Apply(s, "practice", SportsMatchKind.Practice, true, true);
        Apply(s, "coop", SportsMatchKind.Cooperative, true, true);
        Assert.That(s.matches, Is.EqualTo(5)); Assert.That(s.competitive, Is.EqualTo(3));
        Assert.That(s.wins, Is.EqualTo(1)); Assert.That(s.losses, Is.EqualTo(1)); Assert.That(s.draws, Is.EqualTo(1));
        Assert.That(s.winnerPoints, Is.EqualTo(1)); Assert.That(s.practice, Is.EqualTo(1)); Assert.That(s.cooperative, Is.EqualTo(1));
        Assert.That(s.WinPercentage, Is.EqualTo(100d / 3).Within(.001)); Assert.That(s.playedSeconds, Is.EqualTo(50));
        Assert.That(s.Visit("night1"), Is.True); Assert.That(s.Visit("night1"), Is.False); Assert.That(s.Visit("night2"), Is.True);
        Assert.That(s.sessionIds.Count, Is.EqualTo(2));
    }
    [Test] public void RecordsHonorDirectionAndKeepDifferentVariantsSeparate()
    {
        var s = new SportsProfileStatistics();
        s.Apply("race", "easy", "a", "utc", SportsMatchKind.Competitive, true, true, 1, 10, 20, "SEC", SportsRecordPreference.Lower);
        s.Apply("race", "easy", "b", "utc", SportsMatchKind.Competitive, true, true, 1, 10, 25, "SEC", SportsRecordPreference.Lower);
        s.Apply("race", "easy", "c", "utc", SportsMatchKind.Competitive, true, true, 1, 10, 18, "SEC", SportsRecordPreference.Lower);
        s.Apply("race", "hard", "d", "utc", SportsMatchKind.Competitive, true, true, 1, 10, 40, "SEC", SportsRecordPreference.Lower);
        Assert.That(s.sports[0].records.Count, Is.EqualTo(2)); Assert.That(s.sports[0].records[0].value, Is.EqualTo(18));
        Assert.That(s.sports[0].records[0].matchId, Is.EqualTo("c")); Assert.That(s.sports[0].records[1].variant, Is.EqualTo("hard"));
    }
    [Test] public void InvalidMeasurementsCannotPartiallyIncrementCounters()
    {
        var s = new SportsProfileStatistics();
        Assert.Throws<ArgumentException>(() => s.Apply("race", "easy", "a", "utc", SportsMatchKind.Competitive, true, true, 1, 1, double.NaN, "SEC", SportsRecordPreference.Lower));
        Assert.That(s.matches, Is.Zero); Assert.That(s.sports, Is.Empty);
    }
    static void Apply(SportsProfileStatistics s, string id, SportsMatchKind kind, bool winner, bool anyWinner)
        => s.Apply("sport", "standard", id, "utc", kind, winner, anyWinner, 1, 10, null, "", SportsRecordPreference.Unspecified);
}
