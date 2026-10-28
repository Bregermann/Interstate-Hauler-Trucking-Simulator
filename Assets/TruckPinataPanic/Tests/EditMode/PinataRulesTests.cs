using System;
using System.Linq;
using NUnit.Framework;

namespace LWS.TruckPinataPanic.Tests
{
    public sealed class PinataRulesTests
    {
        Type type, kind; object rules;
        object Call(string name, params object[] values) => type.GetMethod(name).Invoke(rules, values);
        T Get<T>(string name) => (T)type.GetProperty(name).GetValue(rules);
        int[] Points => (int[])type.GetField("points").GetValue(rules);
        object Kind(string name) => Enum.Parse(kind, name);
        void Tick(float seconds) { for (int i = 0; i < (int)Math.Ceiling(seconds / .05f); i++) Call("Tick", .05f); }
        void Strike() { Tick(3.1f); Assert.That(Get<object>("Phase").ToString(), Is.EqualTo("Striking")); }
        bool Collect(int round, int id, int slot, out int delta) { object[] args = { round, id, slot, 0 }; bool ok = (bool)Call("Collect", args); delta = (int)args[3]; return ok; }
        [SetUp] public void Reset() { type = Type.GetType("LWS.TruckPinataPanic.PinataRules, Assembly-CSharp", true); kind = Type.GetType("LWS.TruckPinataPanic.PinataCandyKind, Assembly-CSharp", true); rules = Activator.CreateInstance(type); Call("Reset", 4, 3); }
        [Test] public void CountdownCannotAwardDamageOrCandy() { Assert.That((int)Call("BankDamage", 0, 1, 0, 300f), Is.Zero); Assert.That((bool)Call("Rupture"), Is.False); Assert.That((bool)Call("Release", 0, 0, Kind("Normal")), Is.False); Assert.That(Points, Is.All.Zero); }
        [Test] public void RealHealthLossRewardIsSmallFiniteAndOncePerHit() { Strike(); Assert.That((int)Call("BankDamage", 0, 1, 0, 160f), Is.EqualTo(2)); Assert.That((int)Call("BankDamage", 0, 1, 0, 160f), Is.Zero); Assert.That((int)Call("BankDamage", 0, 2, 0, float.NaN), Is.Zero); Assert.That((int)Call("BankDamage", 0, 3, 0, 0f), Is.Zero); Assert.That(Points[0], Is.EqualTo(2)); }
        [Test] public void OneAtomicRuptureAllowsOneReleasePerCandyLife() { Strike(); Assert.That((bool)Call("Rupture"), Is.True); Assert.That((bool)Call("Rupture"), Is.False); Assert.That(Get<int>("Ruptures"), Is.EqualTo(1)); Assert.That((bool)Call("Release", 0, 4, Kind("Golden")), Is.True); Assert.That((bool)Call("Release", 0, 4, Kind("Golden")), Is.False); Assert.That(Get<int>("RemainingCandy"), Is.EqualTo(1)); }
        [Test] public void LastImpactCanBankAfterItRupturesShell() { Strike(); Call("Rupture"); Assert.That((int)Call("BankDamage", 0, 1, 2, 80f), Is.EqualTo(1)); Assert.That(Points[2], Is.EqualTo(1)); }
        [Test] public void GoldenNormalAndExplosiveHaveActualDistinctValues() { Strike(); Call("Rupture"); Call("Release", 0, 0, Kind("Normal")); Call("Release", 0, 1, Kind("Golden")); Call("Release", 0, 2, Kind("Explosive")); Assert.That(Collect(0, 0, 1, out int a), Is.True); Assert.That(a, Is.EqualTo(10)); Assert.That(Collect(0, 1, 1, out int b), Is.True); Assert.That(b, Is.EqualTo(50)); Assert.That(Collect(0, 2, 1, out int c), Is.True); Assert.That(c, Is.EqualTo(-15)); Assert.That(Points[1], Is.EqualTo(45)); Assert.That(Points.Where((_, i) => i != 1), Is.All.Zero); }
        [Test] public void DuplicateAndWrongOwnerCollectionCannotConsumeOrScoreAgain() { Strike(); Call("Rupture"); Call("Release", 0, 7, Kind("Normal")); Assert.That(Collect(0, 7, 4, out _), Is.False); Assert.That(Collect(0, 7, 2, out _), Is.True); Assert.That(Collect(0, 7, 3, out _), Is.False); Assert.That((bool)Call("Release", 0, 7, Kind("Normal")), Is.False); Assert.That(Points[2], Is.EqualTo(10)); Assert.That(Points[3], Is.Zero); }
        [Test] public void ExplosiveNeverCreatesNegativeTotalOrCandyCount() { Strike(); Call("Rupture"); Call("Release", 0, 2, Kind("Explosive")); Assert.That(Collect(0, 2, 0, out int delta), Is.True); Assert.That(delta, Is.Zero); Assert.That(Points, Is.All.Zero); Assert.That(Get<int>("RemainingCandy"), Is.Zero); Assert.That(Collect(0, 2, 0, out _), Is.False); }
        [Test] public void IntactTimeoutNeverFabricatesRuptureOrCandy() { Strike(); Tick(55.1f); Assert.That(Get<object>("Phase").ToString(), Is.EqualTo("Intermission")); Assert.That(Get<int>("Ruptures"), Is.Zero); Assert.That((bool)Call("Release", 0, 0, Kind("Golden")), Is.False); Assert.That(Points, Is.All.Zero); }
        [Test] public void NewRoundAllowsNewCandyLifeButRejectsStaleCallbacks() { Strike(); Call("Rupture"); Call("Release", 0, 0, Kind("Golden")); Collect(0, 0, 0, out _); Tick(27.2f); Assert.That(Get<int>("Round"), Is.EqualTo(1)); Tick(3.1f); Call("Rupture"); Assert.That((bool)Call("Release", 1, 0, Kind("Normal")), Is.True); Assert.That(Collect(0, 0, 0, out _), Is.False); Assert.That(Collect(1, 0, 0, out int delta), Is.True); Assert.That(delta, Is.EqualTo(10)); Assert.That(Points[0], Is.EqualTo(60)); }
        [Test] public void CompleteFiniteMatchKeepsTiedPlacesAndRematchResetsEverything() { Call("Reset", 2, 1); Strike(); Call("Rupture"); for (int i = 0; i < 2; i++) { Call("Release", 0, i, Kind("Normal")); Collect(0, i, i, out _); } Tick(27.2f); Assert.That(Get<bool>("Complete"), Is.True); Assert.That(Get<bool>("TiedWinner"), Is.True); Assert.That((int)Call("Place", 0), Is.EqualTo(1)); Assert.That((int)Call("Place", 1), Is.EqualTo(1)); Call("Reset", 4, 3); Assert.That(Get<bool>("Complete"), Is.False); Assert.That(Get<int>("Round"), Is.Zero); Assert.That(Get<int>("Ruptures"), Is.Zero); Assert.That(Points, Is.All.Zero); }
        [Test] public void InvalidClockCannotAdvanceOrCorruptMatch() { Call("Tick", float.NaN); Call("Tick", float.PositiveInfinity); Call("Tick", -1f); Assert.That(Get<float>("Clock"), Is.Zero); Assert.That(Get<float>("PhaseSeconds"), Is.EqualTo(3)); }
    }
}
