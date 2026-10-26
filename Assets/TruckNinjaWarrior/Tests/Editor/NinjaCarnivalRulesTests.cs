using System;
using NUnit.Framework;

public sealed class NinjaCarnivalRulesTests
{
    Type type; object rules;
    object Call(string name, params object[] args) => type.GetMethod(name).Invoke(rules, args);
    T Get<T>(string name) => (T)type.GetProperty(name).GetValue(rules);
    T Field<T>(string name) => (T)type.GetField(name).GetValue(rules);
    void Go() => Call("Tick", 3f);
    void ClearAll(int actor) { for (int checkpoint = 1; checkpoint <= 6; checkpoint++) Assert.That((bool)Call("Clear", actor, checkpoint), Is.True); }
    [SetUp] public void Setup() { type = Type.GetType("LWS.TruckNinjaWarrior.NinjaRules, Assembly-CSharp", true); rules = Activator.CreateInstance(type); Call("Reset", 3, 6, 180f); Call("Begin"); }

    [Test] public void FallPreservesOtherAthletesCompletedTimeAndNextAthletesEmptyRecord()
    {
        Go(); Call("Tick", 21f); ClearAll(0); Call("Bell", 0); Call("Tick", 3f); Go();
        Call("Tick", 15f); Call("Clear", 1, 1); Assert.That((bool)Call("Fall", 1), Is.True);
        Assert.That(Get<int>("Active"), Is.EqualTo(1)); Assert.That(Get<int>("Checkpoint"), Is.Zero); Assert.That(Get<float>("Clock"), Is.Zero); Assert.That(Get<float>("Penalty"), Is.Zero);
        Assert.That(Field<float[]>("adjustedTime")[0], Is.EqualTo(21)); Assert.That(Field<bool[]>("finished")[0], Is.True); Assert.That(Field<int[]>("cleared")[0], Is.EqualTo(6));
        Assert.That(float.IsPositiveInfinity(Field<float[]>("adjustedTime")[1]), Is.True); Assert.That(Field<bool[]>("finished")[1], Is.False); Assert.That(Field<int[]>("cleared")[1], Is.Zero); Assert.That(Field<int[]>("falls")[1], Is.EqualTo(1));
        Assert.That(float.IsPositiveInfinity(Field<float[]>("adjustedTime")[2]), Is.True); Assert.That(Field<int[]>("falls")[2], Is.Zero);
    }

    [Test] public void RestartReplaysFullCountdownAndRejectsDuplicateFallAndEarlyBell()
    {
        Go(); ClearAll(0); Call("Tick", 7f); Call("Fall", 0);
        Assert.That((bool)Call("Fall", 0), Is.False); Assert.That((bool)Call("Bell", 0), Is.False); Assert.That((bool)Call("Clear", 0, 1), Is.False);
        Call("Tick", 2.99f); Assert.That(type.GetProperty("Phase").GetValue(rules).ToString(), Is.EqualTo("Countdown")); Assert.That(Get<float>("Clock"), Is.Zero);
        Call("Tick", .02f); Assert.That(type.GetProperty("Phase").GetValue(rules).ToString(), Is.EqualTo("Running")); Assert.That(Get<float>("Clock"), Is.LessThan(.02f)); Assert.That(Field<int[]>("falls")[0], Is.EqualTo(1));
    }

    [Test] public void RestartsHaveNoHiddenPenaltyOrUnrequestedLifeLimit()
    {
        for (int attempt = 0; attempt < 8; attempt++) { Go(); Call("Tick", 20f); Assert.That((bool)Call("Fall", 0), Is.True); Assert.That(Get<float>("Clock"), Is.Zero); Assert.That(Get<float>("Penalty"), Is.Zero); Assert.That(Get<int>("Active"), Is.Zero); }
        Assert.That(Field<int[]>("falls")[0], Is.EqualTo(8)); Assert.That(Get<bool>("Complete"), Is.False);
    }

    [Test] public void OnlyBellAfterCompleteNewAttemptPublishesARealFinish()
    {
        int finishes = 0; float seconds = -1;
        Action<int, float> handler = (actor, time) => { finishes++; seconds = time; };
        type.GetEvent("FinishConfirmed").AddEventHandler(rules, handler);
        Go(); Call("Tick", 30f); ClearAll(0); Call("Fall", 0); Assert.That(finishes, Is.Zero);
        Go(); Call("Tick", 14f); ClearAll(0); Assert.That((bool)Call("Bell", 0), Is.True); Assert.That((bool)Call("Bell", 0), Is.False);
        Assert.That(finishes, Is.EqualTo(1)); Assert.That(seconds, Is.EqualTo(14)); Assert.That(Field<float[]>("adjustedTime")[0], Is.EqualTo(14));
    }

    [TestCase(-1f, 4.2f)] [TestCase(0f, 4.2f)] [TestCase(.5f, 5.85f)] [TestCase(1f, 7.5f)] [TestCase(2f, 7.5f)]
    public void ChargeInterpolatesWithinExistingShortAndFullJumpCaps(float charge, float expected)
    {
        var athlete = Type.GetType("LWS.TruckNinjaWarrior.NinjaAthlete, Assembly-CSharp", true);
        float height = (float)athlete.GetMethod("ChargedHeight").Invoke(null, new object[] { 4.2f, 7.5f, charge });
        Assert.That(height, Is.EqualTo(expected).Within(.0001f));
    }
}
