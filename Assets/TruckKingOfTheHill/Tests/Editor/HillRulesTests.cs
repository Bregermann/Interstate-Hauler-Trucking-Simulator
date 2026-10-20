using System;
using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
    public sealed class HillRulesTests
    {
        Type type; object rules; bool[] present, eligible;
        object Call(string name, params object[] args) => type.GetMethod(name).Invoke(rules, args);
        T Read<T>(string name) => (T)type.GetProperty(name).GetValue(rules);
        float[] Scores => (float[])type.GetField("control").GetValue(rules);
        void Step(float dt) => Call("Step", dt, present, eligible);
        [SetUp] public void Reset() { type = Type.GetType("LWS.TruckKingOfTheHill.HillRules, Assembly-CSharp", true); rules = Activator.CreateInstance(type); present = new bool[8]; eligible = new bool[8]; for (int i = 0; i < 8; i++) eligible[i] = true; Call("Reset", 8, 5, 123, 60f, 25f); }
        [Test] public void OutsideNeverEarnsControl() { Step(5); Assert.That(Scores, Is.All.Zero); Assert.That(Read<int>("Controller"), Is.EqualTo(-1)); }
        [Test] public void OneActualOccupantEarnsElapsedSeconds() { present[2] = true; Step(.5f); Assert.That(Scores[2], Is.EqualTo(.5f)); Assert.That(Read<int>("Controller"), Is.EqualTo(2)); }
        [Test] public void TwoOpponentsContestAndEarnNothing() { present[2] = present[3] = true; Step(3); Assert.That(Scores, Is.All.Zero); Assert.That(Read<int>("Occupants"), Is.EqualTo(2)); }
        [Test] public void OverturnedOccupantCannotScore() { present[0] = true; eligible[0] = false; Step(1); Assert.That(Scores[0], Is.Zero); }
        [Test] public void OverturnedOpponentStillPhysicallyContests() { present[0] = present[1] = true; eligible[1] = false; Step(1); Assert.That(Scores, Is.All.Zero); }
        [Test] public void ControlResumesAfterOpponentLeaves() { present[0] = present[1] = true; Step(2); present[1] = false; Step(2); Assert.That(Scores[0], Is.EqualTo(2)); }
        [Test] public void RelocationWarningIsThreeSeconds() { Step(21.9f); Assert.That(Read<bool>("Warning"), Is.False); Step(.2f); Assert.That(Read<bool>("Warning"), Is.True); }
        [Test] public void ZoneMovesToAnnouncedDifferentAccessibleIndex() { int next = Read<int>("NextZone"); Assert.That(next, Is.Not.Zero); Step(25); Assert.That(Read<int>("Zone"), Is.EqualTo(next)); Assert.That(Read<int>("Moves"), Is.EqualTo(1)); Assert.That(Read<int>("NextZone"), Is.Not.EqualTo(next)); }
        [Test] public void OldLocationSampleCannotCreditNewLocation() { Step(24.9f); present[0] = true; Step(.2f); Assert.That(Scores[0], Is.EqualTo(.1f).Within(.0001f)); Assert.That(Read<int>("Controller"), Is.EqualTo(-1)); }
        [Test] public void ExactTargetLocksResultAndLateSamplesCannotChangeIt() { Call("Reset", 8, 5, 123, 1f, 25f); present[0] = true; Step(2); Assert.That(Read<int>("Winner"), Is.Zero); Assert.That(Read<float>("Clock"), Is.EqualTo(1)); present[0] = false; present[1] = true; Step(5); Assert.That(Scores[1], Is.Zero); }
        [Test] public void RematchRestoresScoresAndSeededLocationSequence() { int next = Read<int>("NextZone"); present[3] = true; Step(10); Call("Reset", 8, 5, 123, 60f, 25f); Assert.That(Scores, Is.All.Zero); Assert.That(Read<int>("NextZone"), Is.EqualTo(next)); Assert.That(Read<bool>("Complete"), Is.False); }
        [Test] public void LongUncontestedSampleCannotClaimSixtySecondsAcrossUnsampledMoves() { present[0] = true; Step(60); Assert.That(Scores[0], Is.EqualTo(25)); Assert.That(Read<bool>("Complete"), Is.False); Assert.That(Read<int>("Moves"), Is.EqualTo(2)); }
    }
}
