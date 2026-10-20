using System;
using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
    public sealed class WhackRulesTests
    {
        Type type; object rules;
        object Call(string method, params object[] args) => type.GetMethod(method).Invoke(rules, args);
        T Read<T>(string property) => (T)type.GetProperty(property).GetValue(rules);
        T Field<T>(string field) => (T)type.GetField(field).GetValue(rules);
        [SetUp] public void Reset() { type = Type.GetType("LWS.TruckWhackAMole.WhackRules, Assembly-CSharp", true); rules = Activator.CreateInstance(type); Call("Reset", 8, 36, 90210, 180f); }
        int Expose()
        {
            Call("Step", .01f); int hole = -1;
            for (int i = 0; i < 36; i++) if (Field<int[]>("generations")[i] > 0) { hole = i; break; }
            Assert.That(hole, Is.GreaterThanOrEqualTo(0)); Call("Step", .51f); Assert.That(Call("Phase", hole).ToString(), Is.EqualTo("Exposed")); return hole;
        }
        bool Hit(int slot, int hole, float speed = 2) => (bool)Call("Hit", slot, hole, Field<int[]>("generations")[hole], speed);
        void Kind(int hole, string kind) { var values = Field<Array>("kinds"); values.SetValue(Enum.Parse(values.GetType().GetElementType(), kind), hole); }
        [Test] public void InactiveHoleCannotScore() { Assert.That(Hit(0, 0), Is.False); Assert.That(Read<int>("Contacts"), Is.Zero); }
        [Test] public void EmergingMoleCannotScoreEarly() { Call("Step", .1f); int hole = Array.FindIndex(Field<int[]>("generations"), value => value > 0); Assert.That(Hit(0, hole), Is.False); }
        [Test] public void StationaryOverlapCannotFarmTargets() { int hole = Expose(); Assert.That(Hit(0, hole, .1f), Is.False); Assert.That(Hit(0, hole, 0), Is.False); Assert.That(Hit(0, hole), Is.True); }
        [Test] public void OneContactAtomicallyClaimsOneExposure() { int hole = Expose(); Kind(hole, "Normal"); Assert.That(Hit(0, hole), Is.True); Assert.That(Hit(1, hole), Is.False); Assert.That(Read<int>("Contacts"), Is.EqualTo(1)); Assert.That(Field<int[]>("scores")[0], Is.EqualTo(10)); }
        [Test] public void GoldenMoleScoresFiftyAndCorrectStatistic() { int hole = Expose(); Kind(hole, "Golden"); Hit(3, hole); Assert.That(Field<int[]>("scores")[3], Is.EqualTo(50)); Assert.That(Field<int[]>("goldenHits")[3], Is.EqualTo(1)); }
        [Test] public void DecoyCanProduceNegativeScore() { int hole = Expose(); Kind(hole, "Decoy"); Hit(4, hole); Assert.That(Field<int[]>("scores")[4], Is.EqualTo(-25)); Assert.That(Field<int[]>("decoyHits")[4], Is.EqualTo(1)); }
        [Test] public void ExpiredExposureDoesNotScore() { int hole = Expose(); Call("Step", 6f); Assert.That(Hit(0, hole), Is.False); }
        [Test] public void InvalidGenerationDoesNotStealNewExposure() { int hole = Expose(); Assert.That((bool)Call("Hit", 0, hole, Field<int[]>("generations")[hole] - 1, 2f), Is.False); }
        [Test] public void TimerFreezesLateScoring() { int hole = Expose(); Call("Step", 180f); Assert.That(Read<bool>("Complete"), Is.True); Assert.That(Hit(0, hole), Is.False); Assert.That(Read<float>("Clock"), Is.EqualTo(180)); }
        [Test] public void EqualScoresHaveSharedPlacing() { Assert.That((int)Call("Place", 0), Is.EqualTo(1)); Assert.That((int)Call("Place", 1), Is.EqualTo(1)); int hole = Expose(); Kind(hole, "Golden"); Hit(2, hole); Assert.That((int)Call("Place", 0), Is.EqualTo(2)); Assert.That((int)Call("Place", 2), Is.EqualTo(1)); }
        [Test] public void RematchRestoresCountsScoresAndSeededSchedule() { int hole = Expose(); Kind(hole, "Golden"); Hit(0, hole); Call("Reset", 8, 36, 90210, 180f); Assert.That(Field<int[]>("scores"), Is.All.Zero); Assert.That(Read<int>("Contacts"), Is.Zero); Assert.That(Expose(), Is.EqualTo(hole)); }
        [Test] public void FiniteMatchDoesNotRequireAnyContact() { for (int i = 0; i < 1801; i++) Call("Step", .1f); Assert.That(Read<bool>("Complete"), Is.True); Assert.That(Read<int>("Contacts"), Is.Zero); Assert.That((int)Call("Place", 7), Is.EqualTo(1)); }
    }
}
