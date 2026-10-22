using System;
using NUnit.Framework;
namespace LWS.TruckDanceOff.Tests
{
    public sealed class DancePartyTimingTests
    {
        [TestCase(.04,DanceGrade.Perfect)][TestCase(-.04,DanceGrade.Perfect)]
        [TestCase(.115,DanceGrade.Great)][TestCase(-.115,DanceGrade.Great)]
        [TestCase(.19,DanceGrade.Good)][TestCase(-.19,DanceGrade.Good)]
        [TestCase(.21,DanceGrade.Good)][TestCase(-.21,DanceGrade.Good)]
        public void PartyWindowsKeepNarrowPerfectAndAllowOrdinaryEarlyLateInput(double offset,DanceGrade grade)
        {var rules=new DanceRules(new[]{new DanceNote(1,1)},.12,.21);rules.Step(1+offset,1,1);Assert.That(rules.LastGrade,Is.EqualTo(grade));Assert.That(rules.Resolved,Is.EqualTo(1));rules.Step(1.4,0,0);Assert.That(rules.Resolved,Is.EqualTo(1));}
        [Test]public void ClearlyLateAndWrongLaneStillMissWithoutFreePoints()
        {var rules=new DanceRules(new[]{new DanceNote(1,1)},.12,.21);rules.Step(1,2,2);Assert.That(rules.Score,Is.Zero);rules.Step(1.211,1,1);Assert.That(rules.Miss,Is.EqualTo(1));Assert.That(rules.Score,Is.Zero);}
        [Test]public void LegacyStrictDefaultsAndIndependentConfigurationsRemainSupported()
        {var chart=new[]{new DanceNote(1,1)};var strict=new DanceRules(chart);var party=new DanceRules(chart,.12,.21);strict.Step(1.19,1,1);party.Step(1.19,1,1);Assert.That(strict.Miss,Is.EqualTo(1));Assert.That(party.Good,Is.EqualTo(1));Assert.That(DanceRules.PerfectWindow,Is.EqualTo(.045));}
        [Test]public void InvalidWindowConfigurationFailsClearly()
        {var chart=new[]{new DanceNote(1,1)};Assert.Throws<ArgumentException>(()=>new DanceRules(chart,.02,.21));Assert.Throws<ArgumentException>(()=>new DanceRules(chart,.2,.1));Assert.Throws<ArgumentException>(()=>new DanceRules(chart,.12,double.NaN));Assert.Throws<ArgumentException>(()=>new DanceRules(chart,.12,.5));}
    }
}
