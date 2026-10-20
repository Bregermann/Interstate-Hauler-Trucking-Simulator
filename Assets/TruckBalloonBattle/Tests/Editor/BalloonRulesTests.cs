using System;
using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
    public sealed class BalloonRulesTests
    {
        Type type; object rules;
        object Call(string name, params object[] args) => type.GetMethod(name).Invoke(rules, args);
        T Read<T>(string name) => (T)type.GetProperty(name).GetValue(rules);
        T Field<T>(string name) => (T)type.GetField(name).GetValue(rules);
        [SetUp] public void Setup() { type = Type.GetType("LWS.TruckBalloonBattle.BalloonRules, Assembly-CSharp", true); rules = Activator.CreateInstance(type); Call("Reset", 8); }
        void Step(float dt) => Call("Step", dt);
        int Attack(int slot) { Assert.That((bool)Call("Attack", slot), Is.True); Step(.3f); return (int)Call("AttackSequence", slot); }
        bool Pop(int a, int b, int balloon, int sequence) => (bool)Call("Pop", a, b, balloon, sequence);
        [Test] public void ExactlyThreeBalloonsPerParticipant() { for (int i = 0; i < 8; i++) Assert.That((int)Call("Remaining", i), Is.EqualTo(3)); }
        [Test] public void ClockAndProximityNeverAutomaticallyPop() { Step(1000); Assert.That(Read<int>("Pops"), Is.Zero); Assert.That(Read<bool>("Complete"), Is.False); }
        [Test] public void TelegraphCannotScoreBeforeExtension() { Call("Attack", 0); Step(.1f); Assert.That(Pop(0, 1, 0, 1), Is.False); }
        [Test] public void ExpiredSpikeCannotScore() { Call("Attack", 0); Step(1); Assert.That(Pop(0, 1, 0, 1), Is.False); }
        [Test] public void OneActualTargetContactScoresExactlyOnce() { int attack = Attack(0); Assert.That(Pop(0, 1, 0, attack), Is.True); Assert.That(Pop(0, 1, 0, attack), Is.False); Assert.That(Field<int[]>("scores")[0], Is.EqualTo(1)); }
        [Test] public void OneBodyBumpCannotEraseAllThreeBalloons() { int attack = Attack(0); Assert.That(Pop(0, 1, 0, attack), Is.True); Assert.That(Pop(0, 1, 1, attack), Is.False); Assert.That(Pop(0, 1, 2, attack), Is.False); Assert.That((int)Call("Remaining", 1), Is.EqualTo(2)); }
        [Test] public void OwnBalloonsCannotAwardPoints() { int attack = Attack(0); Assert.That(Pop(0, 0, 0, attack), Is.False); }
        [Test] public void StaleOrInventedAttackSequenceCannotScore() { int attack = Attack(0); Assert.That(Pop(0, 1, 0, attack + 1), Is.False); }
        [Test] public void AttackHasRealCooldown() { Attack(0); Assert.That((bool)Call("Attack", 0), Is.False); Step(2.5f); Assert.That((bool)Call("Attack", 0), Is.True); }
        [Test] public void SeparateAttacksPopAndEliminateOnlyOnThird() { for (int i = 0; i < 3; i++) { int attack = Attack(0); Assert.That(Pop(0, 1, i, attack), Is.True); Step(2.6f); } Assert.That((bool)Call("Alive", 1), Is.False); Assert.That(Field<int[]>("scores")[0], Is.EqualTo(6)); Assert.That(Field<int[]>("eliminations")[0], Is.EqualTo(1)); }
        [Test] public void WinnerBonusIsLockedExactlyOnce() { Call("Reset", 2); for (int i = 0; i < 3; i++) { int attack = Attack(0); Pop(0, 1, i, attack); Step(2.6f); } Assert.That(Read<bool>("Complete"), Is.True); Assert.That(Read<int>("Winner"), Is.EqualTo(0)); Assert.That(Field<int[]>("scores")[0], Is.EqualTo(11)); Step(3); Assert.That(Field<int[]>("scores")[0], Is.EqualTo(11)); }
        [Test] public void SimultaneousMutualFinalPopIsDrawInsteadOfCallbackVictory()
        {
            Call("Reset", 2);
            for (int b = 0; b < 2; b++) { Call("Attack", 0); Call("Attack", 1); Step(.3f); Pop(0, 1, b, (int)Call("AttackSequence", 0)); Pop(1, 0, b, (int)Call("AttackSequence", 1)); Step(2.6f); }
            Call("Attack", 0); Call("Attack", 1); Step(.3f);
            Assert.That(Pop(0, 1, 2, (int)Call("AttackSequence", 0)), Is.True); Assert.That(Pop(1, 0, 2, (int)Call("AttackSequence", 1)), Is.True);
            Step(.02f); Assert.That(Read<bool>("Complete"), Is.True); Assert.That(Read<int>("Winner"), Is.EqualTo(-1));
        }
        [Test] public void RematchRestoresAllBalloonsAndAttackState() { int attack = Attack(0); Pop(0, 1, 0, attack); Call("Reset", 8); Assert.That(Read<int>("Pops"), Is.Zero); Assert.That(Field<int[]>("scores"), Is.All.Zero); Assert.That((int)Call("Remaining", 1), Is.EqualTo(3)); Assert.That((bool)Call("Attack", 0), Is.True); }
    }
}
