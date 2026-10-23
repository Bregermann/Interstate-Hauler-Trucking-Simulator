#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class TournamentAwardAccountingTests
{
    sealed class Change { public string id; public int slot, delta, total; }
    readonly List<Change> changes = new List<Change>();
    void Capture(string id, int slot, int delta, int total) => changes.Add(new Change { id = id, slot = slot, delta = delta, total = total });
    void CaptureKind<T>(T kind, string id, int slot, int delta, int total) => Capture(id, slot, delta, total);
    object Watch(string name, bool kind)
    {
        changes.Clear(); var type = Type.GetType(name + ", Assembly-CSharp", true); var rules = Activator.CreateInstance(type); var signal = type.GetEvent("Awarded");
        var callback = GetType().GetMethod(kind ? "CaptureKind" : "Capture", BindingFlags.Instance | BindingFlags.NonPublic);
        if (kind) callback = callback.MakeGenericMethod(signal.EventHandlerType.GetGenericArguments()[0]);
        signal.AddEventHandler(rules, Delegate.CreateDelegate(signal.EventHandlerType, this, callback)); return rules;
    }
    static object Call(object rules, string method, params object[] args) => rules.GetType().GetMethod(method).Invoke(rules, args);
    static int[] Points(object rules) => (int[])rules.GetType().GetField("points").GetValue(rules);
    void Verify(object rules)
    {
        var totals = new int[Points(rules).Length]; var ids = new HashSet<string>();
        foreach (var change in changes)
        {
            Assert.That(ids.Add(change.id), Is.True); Assert.That(change.delta, Is.GreaterThan(0));
            totals[change.slot] += change.delta; Assert.That(change.total, Is.EqualTo(totals[change.slot]));
        }
        CollectionAssert.AreEqual(Points(rules), totals);
    }
    [Test]
    public void LanceHitsAndKnockdownUpgradesAccountExactlyAcrossPasses()
    {
        var rules = Watch("LWS.TruckJousting.JoustRules", true); Call(rules, "Reset", 3);
        Assert.That(Call(rules, "Hit", 0, true, 20f), Is.False); Assert.That(changes, Is.Empty);
        for (int pass = 0; pass < 3; pass++)
        {
            Call(rules, "Begin"); Assert.That(Call(rules, "Hit", 0, true, 20f), Is.True);
            Assert.That(Call(rules, "Hit", 0, true, 20f), Is.False); Assert.That(Call(rules, "Knockdown", 1), Is.True);
            Assert.That(Call(rules, "Knockdown", 1), Is.False); Assert.That(Call(rules, "Hit", 1, false, 12f), Is.True);
            Call(rules, "EndPass"); if (pass < 2) Call(rules, "NextPass");
        }
        Verify(rules); Assert.That(Points(rules), Is.EqualTo(new[] { 9, 3 })); Assert.That(changes.Count, Is.EqualTo(9));
        Assert.That(Call(rules, "Hit", 1, true, 20f), Is.False); Verify(rules);
        changes.Clear(); Call(rules, "Reset", 5); Verify(rules); Assert.That(changes, Is.Empty);
    }
    static void Run(object rules, int slot, float seconds)
    {
        Call(rules, "Tick", 3 + seconds); Assert.That(Call(rules, "Finish", slot, seconds), Is.True); Call(rules, "Tick", 2.5f);
    }
    [Test]
    public void CrossingChampionshipPublishesOnlyActualCourseWins()
    {
        var rules = Watch("LWS.TruckFrogger.FrogRules", false); Call(rules, "Reset", 1, 0, 123, 180f); Call(rules, "Begin");
        for (int course = 0; course < 5; course++) Run(rules, 0, 10f + course);
        Verify(rules); Assert.That(changes.Count, Is.EqualTo(5)); Assert.That(Points(rules)[0], Is.EqualTo(5));
        Call(rules, "Tick", 10f); Verify(rules); changes.Clear(); Call(rules, "Reset", 2, 1, 12, 180f); Verify(rules); Assert.That(changes, Is.Empty);
    }
    [Test]
    public void TiedCrossingsAndFailuresCannotPublishInventedPoints()
    {
        var rules = Watch("LWS.TruckFrogger.FrogRules", false); Call(rules, "Reset", 2, 1, 123, 180f); Call(rules, "Begin");
        Run(rules, 0, 10f); Assert.That(changes, Is.Empty); Run(rules, 1, 10f); Assert.That(changes, Is.Empty);
        Call(rules, "Tick", 4f); Assert.That(Call(rules, "Fail", 0, "ACTUAL FAILED ATTEMPT"), Is.True); Call(rules, "Tick", 2.5f);
        Assert.That(changes, Is.Empty); Run(rules, 1, 11f); Verify(rules); Assert.That(changes.Count, Is.EqualTo(1)); Assert.That(changes.Single().slot, Is.EqualTo(1));
    }
}
#endif
