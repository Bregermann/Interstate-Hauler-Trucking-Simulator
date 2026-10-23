#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class SurvivalAwardAccountingTests
{
    sealed class Change { public string id, kind; public int slot, delta, total; }
    readonly List<Change> changes = new List<Change>();
    void Capture<T>(T kind, string id, int slot, int delta, int total) => changes.Add(new Change { kind = kind.ToString(), id = id, slot = slot, delta = delta, total = total });
    object Watch(string typeName)
    {
        changes.Clear(); var type = Type.GetType(typeName + ", Assembly-CSharp", true); var rules = Activator.CreateInstance(type);
        var signal = type.GetEvent("Awarded"); var callback = GetType().GetMethod("Capture", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(signal.EventHandlerType.GetGenericArguments()[0]);
        signal.AddEventHandler(rules, Delegate.CreateDelegate(signal.EventHandlerType, this, callback)); return rules;
    }
    static object Get(object owner, string name) => owner.GetType().GetField(name) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods().Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    void ExactAccounting(object rules)
    {
        var scores = (int[])Get(rules, "scores"); var running = new int[scores.Length]; var ids = new HashSet<string>();
        foreach (var change in changes)
        {
            Assert.That(ids.Add(change.id), Is.True, "Each semantic award identity is unique within the match: " + change.id);
            Assert.That(change.delta, Is.GreaterThan(0)); running[change.slot] += change.delta;
            Assert.That(change.total, Is.EqualTo(running[change.slot]), "Callback sees the score after that exact award, including simultaneous survival/winner bonuses.");
        }
        CollectionAssert.AreEqual(scores, running, "Notifications account for all actual awards without adding or losing points.");
    }

    [Test]
    public void PotatoContactEdgesAndEveryExpiryHaveExactlyOneAccountingStream()
    {
        var rules = Watch("LWS.TruckHotPotato.HotPotatoRules"); Call(rules, "Reset", 8, 42);
        for (int i = 0; i < 50; i++) Call(rules, "Step", .02f);
        int carrier = (int)Get(rules, "Carrier"), other = (carrier + 1) % 8;
        Call(rules, "Contact", carrier, other, .02f); Call(rules, "Step", .02f);
        Assert.That(changes.Count, Is.EqualTo(1)); Assert.That(changes[0].kind, Is.EqualTo("Pass"));
        for (int i = 0; i < 100; i++) { Call(rules, "Contact", carrier, other, .02f); Call(rules, "Contact", other, carrier, .02f); Call(rules, "Step", .02f); }
        Assert.That(changes.Count, Is.EqualTo(1), "Resting compound contact cannot publish or farm another pass.");
        for (int i = 0; i < 8500 && !(bool)Get(rules, "Complete"); i++) Call(rules, "Step", .02f);
        Assert.That(Get(rules, "Complete"), Is.True); ExactAccounting(rules);
        Assert.That(changes.Count(change => change.kind == "LastStanding"), Is.EqualTo(1));
        int count = changes.Count; Call(rules, "Step", 5f); Assert.That(changes.Count, Is.EqualTo(count), "Completed rules freeze award notifications too.");
        changes.Clear(); Call(rules, "Reset", 8, 42); Assert.That(changes, Is.Empty, "Reset clears scores without pretending zeroing is a new award."); ExactAccounting(rules);
    }

    [Test]
    public void SeesawBalancesSurvivalKnockoffsAndHeatWinsPreserveExactTotals()
    {
        var rules = Watch("LWS.TruckSeesaw.SeesawRules"); Call(rules, "Reset", 4, 2, 90f);
        var recovered = new bool[4]; recovered[0] = true; Call(rules, "Step", .02f, 0, recovered);
        for (int i = 0; i < 20; i++) Call(rules, "Step", .02f, 0, recovered);
        Assert.That(changes.Count(change => change.kind == "Balance"), Is.EqualTo(1), "A held recovery observation has only one edge.");
        recovered[0] = false; Call(rules, "Step", 15f, 0, recovered);
        Call(rules, "RecordPush", 0, 1, 3f); Call(rules, "Step", .02f, 14, recovered);
        Assert.That(Get(rules, "HeatComplete"), Is.True); ExactAccounting(rules);
        Assert.That(changes.Count(change => change.kind == "Knockoff"), Is.EqualTo(1)); Assert.That(changes.Count(change => change.kind == "HeatWinner"), Is.EqualTo(1));
        int count = changes.Count; Call(rules, "Step", .5f, 14, recovered); Assert.That(changes.Count, Is.EqualTo(count));
        Assert.That(Call(rules, "NextHeat"), Is.True); Call(rules, "Step", 15f, 0, recovered); Call(rules, "Step", .02f, 14, recovered);
        Assert.That(Get(rules, "Complete"), Is.True); ExactAccounting(rules);
        changes.Clear(); Call(rules, "Reset", 4, 2, 90f); Assert.That(changes, Is.Empty); ExactAccounting(rules);
    }
}
#endif
