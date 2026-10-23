using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ConfirmedScoreReceiptTests
{
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Field(object owner, string name) => owner.GetType().GetField(name).GetValue(owner);
    static object Player(string id, string name)
    {
        var player = Activator.CreateInstance(Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp", true));
        player.GetType().GetField("participantId").SetValue(player, id); player.GetType().GetField("name").SetValue(player, name); player.GetType().GetField("color").SetValue(player, Color.cyan);
        return player;
    }
    static bool Confirm(object journal, string id, object player, int slot, double delta, double total, out object receipt)
    {
        object[] args = { id, player, slot, delta, total, "RING OUT!", Enum.Parse(Runtime("SportsScoreTier"), "Major"), "PTS", null, false, false, null };
        bool accepted = (bool)journal.GetType().GetMethod("TryConfirm").Invoke(journal, args); receipt = args[11]; return accepted;
    }
    static bool Team(object journal, string id, string team, string name, double delta, double total, out object receipt)
    {
        object[] args = { id, team, name, Color.blue, delta, total, "RALLY WON", Enum.Parse(Runtime("SportsScoreTier"), "Major"), "PTS", true, false, null };
        bool accepted = (bool)journal.GetType().GetMethod("TryConfirmTeam").Invoke(journal, args); receipt = args[11]; return accepted;
    }
    [Test]
    public void TeamRecipientsNeverImpersonateAPlayerAndDuplicateNamesStaySeparate()
    {
        var journal = Activator.CreateInstance(Runtime("SportsScoreJournal"), "volley", "match");
        Assert.That(Team(journal, "rally:1", "blue", "Same Name", 1, 1, out var blue), Is.True);
        Assert.That(Team(journal, "rally:2", "orange", "Same Name", 1, 1, out var orange), Is.True);
        Assert.That(Confirm(journal, "player:event", Player("blue", "Same Name"), 0, 3, 3, out var player), Is.True);
        Assert.That(Field(blue, "RecipientId"), Is.EqualTo("team:blue")); Assert.That(Field(orange, "RecipientId"), Is.EqualTo("team:orange")); Assert.That(Field(player, "RecipientId"), Is.EqualTo("participant:blue"));
        Assert.That(Field(blue, "IsTeam"), Is.True); Assert.That(Field(player, "IsTeam"), Is.False);
        Assert.That(Field(blue, "ParticipantId"), Is.Null); Assert.That(Field(blue, "Slot"), Is.EqualTo(-1));
        Assert.That(Field(blue, "TeamId"), Is.EqualTo("blue")); Assert.That(Field(blue, "DisplayName"), Is.EqualTo("Same Name"));
        Assert.That(Team(journal, "rally:1", "orange", "Edited", 1, 2, out var duplicate), Is.False); Assert.That(duplicate, Is.Null);
        Assert.That(Field(blue, "Total"), Is.EqualTo(1d)); Assert.That(Field(blue, "RoundEnded"), Is.True);
    }
    [Test]
    public void TeamReceiptsRejectInvalidDataFreezeAtEndAndResetPerMatch()
    {
        var journal = Activator.CreateInstance(Runtime("SportsScoreJournal"), "freeze", "old");
        foreach (var bad in new[] { "", " " })
        {
            Assert.That(Assert.Throws<TargetInvocationException>(() => Team(journal, "bad", bad, "Blue", 1, 1, out _)).InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(Assert.Throws<TargetInvocationException>(() => Team(journal, "bad", "blue", bad, 1, 1, out _)).InnerException, Is.TypeOf<ArgumentException>());
        }
        Assert.That(Assert.Throws<TargetInvocationException>(() => Team(journal, "bad", "blue", "Blue", double.PositiveInfinity, 1, out _)).InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(journal.GetType().GetProperty("Count").GetValue(journal), Is.Zero);
        Assert.That(Team(journal, "round:0", "blue", "Blue", 1, 1, out _), Is.True);
        journal.GetType().GetMethod("Close").Invoke(journal, null); Assert.That(Team(journal, "round:1", "orange", "Orange", 1, 1, out _), Is.False);
        var next = Activator.CreateInstance(Runtime("SportsScoreJournal"), "freeze", "new"); Assert.That(Team(next, "round:0", "orange", "Orange", 1, 1, out var fresh), Is.True); Assert.That(Field(fresh, "MatchId"), Is.EqualTo("new"));
    }
    [Test]
    public void StableIdentitiesAndEventIdsKeepDuplicateNamesIndependent()
    {
        var journal = Activator.CreateInstance(Runtime("SportsScoreJournal"), "sumo", "match-1");
        var first = Player("crew-a", "Same Name"); var second = Player("crew-b", "Same Name");
        Assert.That(Confirm(journal, "heat/1/ring-out/2", first, 0, 1, 7, out var a), Is.True);
        Assert.That(Confirm(journal, "heat/1/ring-out/2", first, 0, 1, 8, out var duplicate), Is.False); Assert.That(duplicate, Is.Null);
        Assert.That(Confirm(journal, "heat/1/survivor", second, 1, 3, 3, out var b), Is.True);
        Assert.That(Field(a, "ParticipantId"), Is.EqualTo("crew-a")); Assert.That(Field(b, "ParticipantId"), Is.EqualTo("crew-b"));
        Assert.That(Field(a, "IsAI"), Is.True, "Default AI slot identity is carried by the real participant source");
        Assert.That(journal.GetType().GetProperty("Count").GetValue(journal), Is.EqualTo(2)); Assert.That(Field(a, "Total"), Is.EqualTo(7d));
        first.GetType().GetField("name").SetValue(first, "Edited Later"); Assert.That(Field(a, "DisplayName"), Is.EqualTo("Same Name"), "Receipt snapshots do not mutate with later crew edits");
    }
    [Test]
    public void ClosingFreezesReceiptsAndFreshMatchesHaveIndependentDeduplication()
    {
        var player = Player("crew", "Driver"); var journal = Activator.CreateInstance(Runtime("SportsScoreJournal"), "sport", "old");
        Assert.That(Confirm(journal, "penalty/1", player, 0, -100, -5, out var penalty), Is.True); Assert.That(Field(penalty, "Delta"), Is.EqualTo(-100d)); Assert.That(Field(penalty, "Total"), Is.EqualTo(-5d));
        journal.GetType().GetMethod("Close").Invoke(journal, null);
        Assert.That(Confirm(journal, "late", player, 0, 10, 5, out _), Is.False);
        var next = Activator.CreateInstance(Runtime("SportsScoreJournal"), "sport", "new"); Assert.That(Confirm(next, "penalty/1", player, 0, -100, -100, out var fresh), Is.True); Assert.That(Field(fresh, "MatchId"), Is.EqualTo("new"));
    }
    [Test]
    public void MissingIdentityAndNonFiniteScoresCannotCreateReceipts()
    {
        var journal = Activator.CreateInstance(Runtime("SportsScoreJournal"), "sport", "match"); var player = Player("", "Driver");
        Assert.That(Assert.Throws<TargetInvocationException>(() => Confirm(journal, "event", player, 0, 1, 1, out _)).InnerException, Is.TypeOf<ArgumentException>());
        player.GetType().GetField("participantId").SetValue(player, "crew");
        Assert.That(Assert.Throws<TargetInvocationException>(() => Confirm(journal, "event", player, 0, double.NaN, 1, out _)).InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(journal.GetType().GetProperty("Count").GetValue(journal), Is.Zero);
    }
}
