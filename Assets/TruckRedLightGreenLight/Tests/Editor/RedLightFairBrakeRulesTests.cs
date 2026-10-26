using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckSports.Tests
{
    public sealed class RedLightFairBrakeRulesTests
    {
        Type type; object rules;
        object Call(string name, params object[] args) => type.GetMethod(name).Invoke(rules, args);
        string Phase => type.GetProperty("Phase").GetValue(rules).ToString();
        [SetUp] public void Setup()
        {
            type = Type.GetType("LWS.TruckRedLightGreenLight.RedLightRules, Assembly-CSharp", true); rules = Activator.CreateInstance(type); Call("Reset", 3, 14);
            for (int i = 0; i < 2000 && Phase != "Red"; i++) Call("Advance", .01f);
            Assert.That(Phase, Is.EqualTo("Red"));
        }
        bool Observe(Vector2 position, Vector2 before, Vector2 velocity, bool accelerator, bool brake, bool grounded = true, int slot = 0)
            => (bool)Call("ObserveWithIntent", slot, position, before, velocity, accelerator, brake, grounded, .02f);

        [TestCase(0f, 0f)] [TestCase(12f, 0f)] [TestCase(-12f, 0f)] [TestCase(0f, 10f)] [TestCase(7f, -9f)]
        public void BrakeOnlyIsSafeAcrossSnowIceForwardReverseAndLateralResidualMotion(float x, float y)
        {
            var velocity = new Vector2(x, y); var before = Vector2.zero;
            for (int step = 0; step < 100; step++) { var position = before + velocity * .02f; Assert.That(Observe(position, before, velocity, false, true), Is.False); before = position; }
            Assert.That(((Array)type.GetField("outcomes").GetValue(rules)).GetValue(0).ToString(), Is.EqualTo("Racing"));
        }

        [TestCase(false, true)] [TestCase(true, true)] [TestCase(false, false)] [TestCase(true, false)]
        public void AcceleratorViolatesAtRestEvenWithBrakeOrWithoutGroundContact(bool brake, bool grounded)
        {
            Assert.That(Observe(Vector2.zero, Vector2.zero, Vector2.zero, true, brake, grounded), Is.True);
            Assert.That(Observe(Vector2.zero, Vector2.zero, Vector2.zero, true, brake, grounded), Is.False, "One elimination only.");
            Assert.That(((string[])type.GetField("reasons").GetValue(rules))[0], Is.EqualTo("DRIVING ON RED"));
        }

        [Test] public void BrakingReanchorsTheWholeResidualSlideBeforeSettledRelease()
        {
            Assert.That(Observe(new Vector2(5, -3), Vector2.zero, new Vector2(9, -8), false, true), Is.False);
            Assert.That(Observe(new Vector2(5.001f, -3), new Vector2(5, -3), Vector2.zero, false, false), Is.False);
        }

        [Test] public void ReleasingBrakeDoesNotGrantUnlimitedUnbrakedRollingImmunity()
        {
            Assert.That(Observe(new Vector2(.1f, 0), Vector2.zero, Vector2.right * 5, false, true), Is.False);
            Assert.That(Observe(new Vector2(.2f, 0), new Vector2(.1f, 0), Vector2.right * 5, false, false), Is.True);
        }

        [TestCase(5f)] [TestCase(-5f)]
        public void UnbrakedRollingStillViolatesInEitherDirection(float velocity)
            => Assert.That(Observe(new Vector2(velocity * .02f, 0), Vector2.zero, new Vector2(velocity, 0), false, false), Is.True);

        [Test] public void MeasuredExternalPushRemainsSafeWithoutDriverInput()
        {
            Call("ExternalPush", 0, Vector2.right * 6);
            Assert.That(Observe(Vector2.right * .12f, Vector2.zero, Vector2.right * 6, false, false), Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void ExternalPushCannotExcuseAcceleratorIntent(bool brake)
        {
            Call("ExternalPush", 0, Vector2.right * 6);
            Assert.That(Observe(Vector2.right * .12f, Vector2.zero, Vector2.right * 6, true, brake), Is.True);
        }

        [Test] public void OneDriversBrakeDoesNotProtectAnotherDriversRolling()
        {
            Assert.That(Observe(Vector2.right, Vector2.zero, Vector2.right * 10, false, true), Is.False);
            Assert.That(Observe(Vector2.right, Vector2.zero, Vector2.right * 10, false, false, true, 1), Is.True);
        }

        [Test] public void FinishedParticipantCannotReceiveALaterIntentViolation()
        {
            Assert.That((bool)Call("Finish", 0, 9f), Is.True);
            Assert.That(Observe(Vector2.zero, Vector2.zero, Vector2.zero, true, true), Is.False);
            Assert.That((int)type.GetProperty("Winner").GetValue(rules), Is.Zero);
        }

        [Test] public void IntentDoesNotEliminateDuringCountdownGreenOrLaughWarning()
        {
            Call("Reset", 3, 14);
            Assert.That(Observe(Vector2.zero, Vector2.zero, Vector2.zero, true, true), Is.False);
            Call("Advance", 3f); Assert.That(Phase, Is.EqualTo("Green"));
            Assert.That(Observe(Vector2.zero, Vector2.zero, Vector2.zero, true, false), Is.False);
            while (Phase == "Green") Call("Advance", .01f);
            Assert.That(Phase, Is.EqualTo("LaughWarning"));
            Assert.That(Observe(Vector2.zero, Vector2.zero, Vector2.zero, true, false), Is.False);
        }

        [Test] public void OriginalReflectionProbeObserveContractRemainsUnambiguous()
        {
            Assert.That(type.GetMethods().Count(method => method.Name == "Observe"), Is.EqualTo(1));
            Assert.That((bool)Call("Observe", 0, Vector2.right * .1f, Vector2.zero, Vector2.right * 5, 5f, 0f, true, .02f), Is.True);
        }
    }
}
