using System;
using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
    public sealed class HotPotatoRulesTests
    {
        Type type; object rules;
        object Call(string name, params object[] args) => type.GetMethod(name).Invoke(rules, args);
        T Read<T>(string name) => (T)type.GetProperty(name).GetValue(rules);
        T Field<T>(string name) => (T)type.GetField(name).GetValue(rules);
        [SetUp] public void Setup() { type = Type.GetType("LWS.TruckHotPotato.HotPotatoRules, Assembly-CSharp", true); rules = Activator.CreateInstance(type); Call("Reset", 8, 42); }
        void Step(float seconds) { for (float t = 0; t < seconds; t += .02f) Call("Step", .02f); }
        void Contact(int a, int b) { Call("Contact", a, b, .02f); }
        [Test] public void NoProximityOrAutomaticTransfer() { int carrier = Read<int>("Carrier"); Step(1); Assert.That(Read<int>("Carrier"), Is.EqualTo(carrier)); Assert.That(Read<int>("Transfers"), Is.Zero); }
        [Test] public void GentleActualContactTransfersWithoutSpeedParameter() { Step(1); int a = Read<int>("Carrier"), b = (a + 1) % 8; Contact(a, b); Call("Step", .02f); Assert.That(Read<int>("Carrier"), Is.EqualTo(b)); Assert.That(Read<int>("Transfers"), Is.EqualTo(1)); }
        [Test] public void CompoundAndContinuousContactCannotPingPong() { Step(1); int a = Read<int>("Carrier"), b = (a + 1) % 8; for (int i = 0; i < 100; i++) { Contact(a, b); Contact(b, a); Call("Step", .02f); } Assert.That(Read<int>("Transfers"), Is.EqualTo(1)); }
        [Test] public void SamePairCanPassAgainAfterSeparationAndCooldown() { Step(1); int a = Read<int>("Carrier"), b = (a + 1) % 8; Contact(a, b); Call("Step", .02f); Step(1); Contact(a, b); Call("Step", .02f); Assert.That(Read<int>("Carrier"), Is.EqualTo(a)); Assert.That(Read<int>("Transfers"), Is.EqualTo(2)); }
        [Test] public void MultipleContactsResolveOneAtomicStableTransfer() { Step(1); int a = Read<int>("Carrier"); for (int i = 7; i >= 0; i--) if (i != a) Contact(a, i); Call("Step", .02f); Assert.That(Read<int>("Transfers"), Is.EqualTo(1)); Assert.That(Read<int>("Carrier"), Is.EqualTo(a == 0 ? 1 : 0)); }
        [Test] public void EverySeparatedValidTransferEarnsThreeWithinTheSameCycle() { Step(1); int a = Read<int>("Carrier"), b = (a + 1) % 8; for (int i = 0; i < 4; i++) { Contact(a, b); Call("Step", .02f); Step(1); } Assert.That(Read<int>("Transfers"), Is.EqualTo(4)); Assert.That(Field<int[]>("scores")[a], Is.EqualTo(6)); Assert.That(Field<int[]>("scores")[b], Is.EqualTo(6)); }
        [TestCase(1,1)][TestCase(3,3)][TestCase(7,7)][TestCase(-4,1)] public void TransferAwardIsTunableAndPositive(int configured,int expected) { type.GetField("transferPoints").SetValue(rules,configured); Step(1); int carrier=Read<int>("Carrier");Contact(carrier,(carrier+1)%8);Call("Step",.02f);Assert.That(Field<int[]>("scores")[carrier],Is.EqualTo(expected));Assert.That(Read<int>("TransferPoints"),Is.EqualTo(expected)); }
        [Test] public void ContinuousRestingContactDoesNotFarmTransferAwards() { Step(1);int a=Read<int>("Carrier"),b=(a+1)%8;for(int i=0;i<150;i++){Contact(a,b);Contact(b,a);Call("Step",.02f);}Assert.That(Read<int>("Transfers"),Is.EqualTo(1));Assert.That(Field<int[]>("scores")[a],Is.EqualTo(3));Assert.That(Field<int[]>("scores")[b],Is.Zero); }
        [Test] public void FuseAlwaysWithinBoundedRange() { float fuse = Read<float>("Fuse"); Assert.That(fuse, Is.InRange(12, 20)); }
        [Test] public void ExpiryEliminatesCarrierAndAwardsOnlySurvivors() { int a = Read<int>("Carrier"); float fuse = Read<float>("Fuse"); Step(fuse + .05f); Assert.That(Field<bool[]>("alive")[a], Is.False); Assert.That(Field<int[]>("scores")[a], Is.Zero); Assert.That(Read<int>("Cycle"), Is.EqualTo(2)); Assert.That(Read<int>("Carrier"), Is.Not.EqualTo(a)); }
        [Test] public void EliminatedTruckCannotReceiveBomb() { int dead = Read<int>("Carrier"); Step(Read<float>("Fuse") + .05f); Step(1); int current = Read<int>("Carrier"); Contact(current, dead); Call("Step", .02f); Assert.That(Read<int>("Carrier"), Is.EqualTo(current)); }
        [Test] public void FullMatchResolvesSoleSurvivorAndLocksScoring() { Step(170); Assert.That(Read<bool>("Complete"), Is.True); Assert.That(Read<int>("Winner"), Is.InRange(0, 7)); Assert.That((int)Call("AliveCount"), Is.EqualTo(1)); int score = Field<int[]>("scores")[Read<int>("Winner")]; Step(5); Assert.That(Field<int[]>("scores")[Read<int>("Winner")], Is.EqualTo(score)); }
        [Test] public void RematchRestoresPopulationOwnershipAndScores() { Step(170); Call("Reset", 8, 42); Assert.That(Read<bool>("Complete"), Is.False); Assert.That((int)Call("AliveCount"), Is.EqualTo(8)); Assert.That(Read<int>("Transfers"), Is.Zero); Assert.That(Field<int[]>("scores"), Is.All.Zero); }
    }
}
