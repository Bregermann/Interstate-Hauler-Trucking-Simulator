#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class FinishOutcomeReceiptTests
{
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Field(object owner, string name) => owner.GetType().GetField(name).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name).Invoke(owner, args);
    static object Player(string id)
    {
        var player = Activator.CreateInstance(Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp", true));
        player.GetType().GetField("participantId").SetValue(player, id); player.GetType().GetField("name").SetValue(player, "Same Name"); player.GetType().GetField("color").SetValue(player, Color.cyan); return player;
    }
    static bool Confirm(object journal, string id, object player, int slot, double value, out object receipt, string unit = "SEC")
    {
        object[] args = { id, player, slot, value, "FINISHED", Enum.Parse(Runtime("SportsScoreTier"), "Major"), unit, false, false, null };
        bool accepted = (bool)journal.GetType().GetMethod("TryConfirmOutcome").Invoke(journal, args); receipt = args[9]; return accepted;
    }
    [Test]
    public void FinishIsAnImmutableMeasurementWithStableIdentityAndNoPointAward()
    {
        var journal = Activator.CreateInstance(Runtime("SportsScoreJournal"), "race", "match");
        Assert.That(Confirm(journal, "finish:0", Player("crew-a"), 0, 12.345, out var first), Is.True);
        Assert.That(Confirm(journal, "finish:1", Player("crew-b"), 1, 12.345, out var second), Is.True);
        Assert.That(Field(first, "Kind").ToString(), Is.EqualTo("Outcome")); Assert.That(Field(first, "OutcomeValue"), Is.EqualTo(12.345));
        Assert.That(Field(first, "Delta"), Is.EqualTo(0d)); Assert.That(Field(first, "Total"), Is.EqualTo(0d)); Assert.That(Field(first, "Unit"), Is.EqualTo("SEC"));
        Assert.That(Field(first, "RecipientId"), Is.EqualTo("participant:crew-a")); Assert.That(Field(second, "RecipientId"), Is.EqualTo("participant:crew-b"));
        Assert.That(first.GetType().GetField("OutcomeValue").IsInitOnly, Is.True); Assert.That(first.GetType().GetField("Kind").IsInitOnly, Is.True);
        Assert.That(Confirm(journal, "finish:0", Player("crew-b"), 1, 1, out var duplicate), Is.False); Assert.That(duplicate, Is.Null);
        Call(journal, "Close"); Assert.That(Confirm(journal, "late", Player("crew-a"), 0, 13, out _), Is.False);
        var next = Activator.CreateInstance(Runtime("SportsScoreJournal"), "race", "rematch"); Assert.That(Confirm(next, "finish:0", Player("crew-a"), 0, 13, out _), Is.True);
    }
    [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)] [TestCase(-.1)]
    public void InvalidMeasurementsCannotBecomeReceiptsOrConsumeTheirIdentity(double value)
    {
        var journal = Activator.CreateInstance(Runtime("SportsScoreJournal"), "race", "match");
        Assert.That(Assert.Throws<TargetInvocationException>(() => Confirm(journal, "finish:0", Player("crew"), 0, value, out _)).InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(journal.GetType().GetProperty("Count").GetValue(journal), Is.Zero);
        Assert.That(Confirm(journal, "finish:0", Player("crew"), 0, 2, out _), Is.True);
    }
    [Test]
    public void MissingUnitsAndIdentityAreRejected()
    {
        var journal = Activator.CreateInstance(Runtime("SportsScoreJournal"), "race", "match");
        Assert.That(Assert.Throws<TargetInvocationException>(() => Confirm(journal, "finish:0", Player("crew"), 0, 2, out _, "")).InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(Assert.Throws<TargetInvocationException>(() => Confirm(journal, "finish:0", Player(""), 0, 2, out _)).InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(Assert.Throws<TargetInvocationException>(() => Confirm(journal, "finish:0", Player("crew"), -1, 2, out _)).InnerException, Is.TypeOf<ArgumentException>());
    }
    static object Watch(string name, List<(int slot, float value)> finishes)
    {
        var rules = Activator.CreateInstance(Type.GetType(name + ", Assembly-CSharp", true)); Action<int, float> callback = (slot, value) => finishes.Add((slot, value));
        rules.GetType().GetEvent("FinishConfirmed").AddEventHandler(rules, callback); return rules;
    }
    [Test]
    public void ActualRuleFinishCommitsPublishOnceAndResetSilently()
    {
        var finishes = new List<(int slot, float value)>(); var rules = Watch("LWS.TruckRedLightGreenLight.RedLightRules", finishes);
        Call(rules, "Reset", 2, 14); Assert.That(Call(rules, "Finish", 0, 1f), Is.False); Assert.That(finishes, Is.Empty);
        Call(rules, "Advance", 4f); Assert.That(Call(rules, "Finish", 0, 3.8f), Is.True); Assert.That(Call(rules, "Finish", 0, 3.8f), Is.False);
        Assert.That(Call(rules, "Finish", 1, 3.8f), Is.True); Assert.That(finishes.Count, Is.EqualTo(2));
        foreach (var finish in finishes) Assert.That(finish.value, Is.EqualTo(((float[])Field(rules, "finishTimes"))[finish.slot]));
        finishes.Clear(); Call(rules, "Reset", 2, 14); Assert.That(finishes, Is.Empty);
    }
    [Test]
    public void EscapesReportRealRecordedTimeAndPreserveTiedPlacing()
    {
        var finishes = new List<(int slot, float value)>(); var rules = Watch("LWS.TruckAvalancheEscape.AvalancheRules", finishes);
        Call(rules, "Reset", 2, 500f, 4.5f, 300f); Call(rules, "Begin"); Assert.That(Call(rules, "Finish", 0, 1f), Is.False); Call(rules, "Tick", 10f);
        Assert.That(Call(rules, "Finish", 0, 6.5f), Is.True); Assert.That(Call(rules, "Finish", 0, 6.5f), Is.False); Assert.That(Call(rules, "Finish", 1, 6.5f), Is.True);
        Assert.That(finishes.Count, Is.EqualTo(2)); foreach (var finish in finishes) Assert.That(finish.value, Is.EqualTo(((float[])Field(rules, "finishTime"))[finish.slot]));
        Assert.That(Call(rules, "Place", 0), Is.EqualTo(Call(rules, "Place", 1))); finishes.Clear(); Call(rules, "Reset", 2, 500f, 4.5f, 300f); Assert.That(finishes, Is.Empty);
    }
    [Test]
    public void BellReportsAdjustedTimeWithoutChangingPenaltyOrAttemptRules()
    {
        var finishes = new List<(int slot, float value)>(); var rules = Watch("LWS.TruckNinjaWarrior.NinjaRules", finishes);
        Call(rules, "Reset", 2, 6, 180f); Call(rules, "Begin"); Assert.That(Call(rules, "Bell", 0), Is.False); Call(rules, "Tick", 3f); Call(rules, "Tick", 10f); Call(rules, "Fall", 0);
        for (int checkpoint = 1; checkpoint <= 6; checkpoint++) Assert.That(Call(rules, "Clear", 0, checkpoint), Is.True);
        Assert.That(Call(rules, "Bell", 0), Is.True); Assert.That(Call(rules, "Bell", 0), Is.False); Assert.That(finishes.Count, Is.EqualTo(1)); Assert.That(finishes[0].value, Is.EqualTo(15f));
        Assert.That(((float[])Field(rules, "adjustedTime"))[0], Is.EqualTo(15f)); Assert.That(((int[])Field(rules, "falls"))[0], Is.EqualTo(1));
        finishes.Clear(); Call(rules, "Reset", 2, 6, 180f); Assert.That(finishes, Is.Empty);
    }
}
#endif
