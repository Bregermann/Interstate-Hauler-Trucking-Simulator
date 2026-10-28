using System;
using NUnit.Framework;

namespace LWS.TruckTornadoRodeo.Tests
{
    public sealed class TornadoRulesTests
    {
        Type type; object rules;
        object Call(string method, params object[] args) => type.GetMethod(method).Invoke(rules, args);
        T Get<T>(string name) => (T)type.GetProperty(name).GetValue(rules);
        int[] Points => (int[])type.GetField("points").GetValue(rules);
        void Tick(float seconds) { for (int i = 0; i < Math.Ceiling(seconds / .05f); i++) Call("Tick", .05f); }
        void Begin() { Tick(3.1f); Assert.That(Get<bool>("Live"), Is.True); Assert.That((bool)Call("BeginFlight", 0, 8f, false), Is.True); }
        [SetUp] public void Reset() { type = Type.GetType("LWS.TruckTornadoRodeo.TornadoRules, Assembly-CSharp", true); rules = Activator.CreateInstance(type); Call("Reset", 4, 90f); }
        [Test] public void CountdownAndGroundSupportCannotInventAFlight() { Assert.That((bool)Call("BeginFlight", 0, 20f, false), Is.False); Tick(3.1f); Assert.That((bool)Call("BeginFlight", 0, 20f, true), Is.False); Assert.That((bool)Call("BeginFlight", 0, 5.99f, false), Is.False); Assert.That((int)Call("ObserveHeight", 0, 40f), Is.Zero); Assert.That(Points, Is.All.Zero); }
        [Test] public void ActualAltitudeTiersBankOnlyNewHeightAndOnlyTheirOwner() { Begin(); Assert.That((int)Call("ObserveHeight", 0, 29f), Is.EqualTo(10)); Assert.That((int)Call("ObserveHeight", 0, 28f), Is.Zero); Assert.That((int)Call("ObserveHeight", 0, 31f), Is.EqualTo(5)); Assert.That(Points, Is.EqualTo(new[] { 15, 0, 0, 0 })); }
        [Test] public void SignedRotationsCancelRockingAndRequireAFullTurn() { Begin(); for (int i = 0; i < 100; i++) { Call("ObserveRotation", 0, 10f, 0f, 0f); Call("ObserveRotation", 0, -10f, 0f, 0f); } Assert.That(Points[0], Is.Zero); for (int i = 0; i < 35; i++) Call("ObserveRotation", 0, 10f, 0f, 0f); Assert.That(Points[0], Is.Zero); Assert.That((int)Call("ObserveRotation", 0, 10f, 0f, 0f), Is.EqualTo(12)); }
        [Test] public void ImpossibleRotationAndNonfiniteSamplesDoNotScore() { Begin(); Assert.That((int)Call("ObserveRotation", 0, 180f, 0f, 0f), Is.Zero); Assert.That((int)Call("ObserveHeight", 0, float.PositiveInfinity), Is.Zero); Assert.That((int)Call("ObserveHeight", 0, float.NaN), Is.Zero); Assert.That(Points[0], Is.Zero); }
        [Test] public void SameRingCanBankOncePerRealFlightAndRejectStaleCallbacks() { Begin(); Assert.That((int)Call("PassRing", 0, 1, 2), Is.EqualTo(30)); Assert.That((int)Call("PassRing", 0, 1, 2), Is.Zero); Call("Land", 0, 1, true, 1f, 4f, 5f, 1f); Call("BeginFlight", 0, 8f, false); Assert.That((int)Call("PassRing", 0, 1, 2), Is.Zero); Assert.That((int)Call("PassRing", 0, 2, 2), Is.EqualTo(30)); }
        [Test] public void DebrisAvoidanceUsesActualFlightAndIsNotARepeatedIdleReward() { Assert.That((int)Call("Dodge", 0, 0, 3), Is.Zero); Begin(); Assert.That((int)Call("Dodge", 0, 1, 3), Is.EqualTo(3)); Assert.That((int)Call("Dodge", 0, 1, 3), Is.Zero); Assert.That((int)Call("Dodge", 1, 1, 3), Is.Zero); }
        [Test] public void LandingRequiresNativeSupportAndSustainedUprightControl() { Begin(); Assert.That((int)Call("Land", 0, 1, false, 1f, 3f, 4f, 1f), Is.Zero); Assert.That((int)Call("Land", 0, 1, true, 1f, 3f, 4f, .79f), Is.Zero); Assert.That((int)Call("Land", 0, 1, true, 1f, 3f, 4f, .8f), Is.EqualTo(40)); Assert.That((int)Call("Land", 0, 1, true, 1f, 3f, 4f, 1f), Is.Zero); }
        [TestCase(.69f, 4f, 5f), TestCase(1f, 12.1f, 5f), TestCase(1f, 4f, 16.1f)] public void UncontrolledNativeTouchdownNeverReceivesLandingBonus(float up, float impact, float planar) { Begin(); Assert.That((int)Call("Land", 0, 1, true, up, impact, planar, 1f), Is.Zero); Assert.That(Points[0], Is.Zero); }
        [Test] public void FiniteMatchTiesAndRematchKeepRealScoresSeparate() { Call("Reset", 2, 30f); Begin(); Call("BeginFlight", 1, 8f, false); Call("ObserveHeight", 0, 21f); Call("ObserveHeight", 1, 21f); Tick(30.1f); Assert.That(Get<bool>("Complete"), Is.True); Assert.That(Get<bool>("Tied"), Is.True); Assert.That((int)Call("Place", 0), Is.EqualTo(1)); Call("Reset", 4, 90f); Assert.That(Points, Is.All.Zero); Assert.That(Get<float>("Clock"), Is.Zero); Assert.That(Get<bool>("Complete"), Is.False); }
        [Test] public void RetiredRiderCannotCreateNewPointsAndAllRetiredEndsMatch() { Begin(); for (int i = 0; i < 4; i++) Call("Retire", i); Assert.That((int)Call("ObserveHeight", 0, 55f), Is.Zero); Tick(.1f); Assert.That(Get<bool>("Complete"), Is.True); }
    }
    public sealed class TornadoFieldTests
    {
        static float[] Sample(float x, float y, float z, float vx = 0, float vy = 0, float vz = 0, float time = 0)
        {
            var type = Type.GetType("LWS.TruckTornadoRodeo.TornadoFieldModel, Assembly-CSharp", true); object[] args = { x, y, z, vx, vy, vz, time, 0f, 0f, 0f, 0f, 0f }; type.GetMethod("Sample").Invoke(null, args); return new[] { (float)args[9], (float)args[10], (float)args[11] };
        }
        [Test] public void FieldAtActualEntryHasSuctionOrbitAndLiftAgainstGravity() { var f = Sample(20, 4, 0); Assert.That(f[0], Is.LessThan(0)); Assert.That(f[2], Is.GreaterThan(0)); Assert.That(f[1], Is.GreaterThan(9.81f)); }
        [Test] public void OutsideSpatialHazardHasNoArtificialForce() { Assert.That(Sample(69, 5, 0), Is.All.Zero); Assert.That(Sample(0, 200, 0), Is.All.Zero); Assert.That(Sample(0, -6, 0), Is.All.Zero); }
        [Test] public void EyeAndNonfiniteInputsRemainFinite() { var f = Sample(0, 8, 0); Assert.That(Array.TrueForAll(f, float.IsFinite), Is.True); Assert.That(Sample(float.NaN, 8, 0), Is.All.Zero); }
        [Test] public void GustsActuallyChangeTheSameLocalForceOverTime() { var a = Sample(25, 20, 0, time: 0); var b = Sample(25, 20, 0, time: 3); Assert.That(a, Is.Not.EqualTo(b)); }
        [Test] public void FastOrbitGetsDragInsteadOfUnlimitedTangentialAcceleration() { Assert.That(Sample(25, 25, 0, vz: 60)[2], Is.LessThan(0)); Assert.That(Sample(25, 25, 0, vz: 0)[2], Is.GreaterThan(0)); }
        [Test] public void WholeFieldGridHasBoundedAccelerationAndCeilingRestoresDownwardMotion()
        {
            for (int x = -65; x <= 65; x += 13) for (int y = 0; y <= 130; y += 13) for (int z = -65; z <= 65; z += 13) { var f = Sample(x, y, z, 40, -30, 45, 17); double m = Math.Sqrt(f[0] * f[0] + f[1] * f[1] + f[2] * f[2]); Assert.That(m, Is.LessThanOrEqualTo(28.0001)); }
            Assert.That(Sample(20, 115, 0)[1], Is.LessThan(0));
        }
    }
}
