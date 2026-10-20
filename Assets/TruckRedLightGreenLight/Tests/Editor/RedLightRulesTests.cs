using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckSports.Tests
{
    public sealed class RedLightRulesTests
    {
        Type type; object rules;
        object Call(string name, params object[] args) => type.GetMethod(name).Invoke(rules, args);
        T Property<T>(string name) => (T)type.GetProperty(name).GetValue(rules);
        string Phase => type.GetProperty("Phase").GetValue(rules).ToString();
        [SetUp] public void Setup() { type = Type.GetType("LWS.TruckRedLightGreenLight.RedLightRules, Assembly-CSharp", true); rules = Activator.CreateInstance(type); Call("Reset", 12, 14); }
        void Red() { for (int i = 0; i < 2000 && Phase != "Red"; i++) Call("Advance", .01f); Assert.That(Phase, Is.EqualTo("Red")); }
        bool Observe(float x, float old, float velocity, float throttle = 0) => (bool)Call("Observe", 0, new Vector2(x, 0), new Vector2(old, 0), new Vector2(velocity, 0), velocity, throttle, true, .02f);

        [Test] public void LaughIsTheOnlyOneSecondWarning()
        {
            Call("Advance", 3f); Assert.That(Phase, Is.EqualTo("Green"));
            while (Phase == "Green") Call("Advance", .01f);
            Assert.That(Phase, Is.EqualTo("LaughWarning")); Assert.That(Property<int>("Laughs"), Is.EqualTo(1));
            float elapsed = Property<float>("PhaseTime"); Call("Advance", .998f - elapsed); Assert.That(Phase, Is.EqualTo("LaughWarning"));
            Call("Advance", .003f); Assert.That(Phase, Is.EqualTo("Red"));
            Assert.That(Enum.GetNames(type.Assembly.GetType("LWS.TruckRedLightGreenLight.RedLightPhase")), Does.Not.Contain("Yellow"));
        }
        [Test] public void SuspensionJitterDoesNotEliminate()
        { Red(); float prior = 0; for (int i = 0; i < 500; i++) { float x = i % 2 == 0 ? .002f : -.002f; Assert.That(Observe(x, prior, .2f), Is.False); prior = x; } }
        [Test] public void DeliberateSlowCreepingIsNotAnUnlimitedSpeedLoophole()
        { Red(); bool eliminated = false; for (int i = 1; i < 100; i++) eliminated |= Observe(i * .001f, (i - 1) * .001f, .05f, 1); Assert.That(eliminated, Is.True); }
        [Test] public void MeasuredSubTwoCentimetreSuspensionDriftIsNotDeliberateTravel()
        { Red(); for (int i = 1; i <= 250; i++) Assert.That(Observe(i * .00028f, (i - 1) * .00028f, .014f), Is.False); }
        [Test] public void RollingOnRedEliminatesEvenWithoutThrottle()
        { Red(); Assert.That(Observe(.1f, 0, 5), Is.True); Assert.That(Observe(.2f, .1f, 5), Is.False, "one elimination only"); }
        [Test] public void AMeasuredExternalPushDoesNotPunishStationaryVictim()
        { Red(); Call("ExternalPush", 0, new Vector2(6, 0)); Assert.That(Observe(.12f, 0, 6), Is.False); Assert.That(Observe(.24f, .12f, 5.8f), Is.False); }
        [Test] public void ACollisionDoesNotGrantImmunityToDriving()
        { Red(); Call("ExternalPush", 0, new Vector2(6, 0)); Assert.That(Observe(.12f, 0, 6, 1), Is.True); }
        [Test] public void NegativeDrivenMovementAlsoViolates()
        { Red(); Assert.That(Observe(-.1f, 0, -5, -1), Is.True); }
        [Test] public void ARecordedFinisherCannotBeEliminatedLater()
        { Call("Advance", 3f); Assert.That((bool)Call("Finish", 0, 10f), Is.True); Assert.That((bool)Call("Eliminate", 0, "LATE EVENT"), Is.False); Assert.That(Property<int>("Winner"), Is.EqualTo(0)); }
        [Test] public void SameStepFinishesUseCrossingTimeInsteadOfIterationOrder()
        { Call("Advance", 3f); Call("Finish", 0, 10.02f); Call("Finish", 1, 10.01f); Assert.That(Property<int>("Winner"), Is.EqualTo(1)); }
        [Test] public void WholeRearClearanceAndSweptCrossingAreRequired()
        {
            var crossing = type.GetMethod("WholeBodyCrossing", BindingFlags.Public | BindingFlags.Static);
            var shortArgs = new object[] { 297f, 299.9f, 300f, 0f }; Assert.That((bool)crossing.Invoke(null, shortArgs), Is.False);
            var fastArgs = new object[] { 299f, 305f, 300f, 0f }; Assert.That((bool)crossing.Invoke(null, fastArgs), Is.True); Assert.That((float)fastArgs[3], Is.EqualTo(1f / 6).Within(.0001f));
        }
        [Test] public void RematchRestoresAllStatesAndWarningClock()
        { Red(); Call("Eliminate", 0, "TEST"); Call("Reset", 12, 19); Assert.That(Phase, Is.EqualTo("Countdown")); Assert.That(Property<int>("Winner"), Is.EqualTo(-1)); Assert.That(Property<int>("Laughs"), Is.Zero); Assert.That(Observe(.01f, 0, 1), Is.False); }
    }
}
