#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class ImpossibleCompleteProbePolicyTests
{
    static Type Probe => Type.GetType("LWS.TruckSports.SportsImpossibleWindowsProbe, Assembly-CSharp", true);
    static string[] Select(params string[] args) => (string[])Probe.GetMethod("CompleteScenes").Invoke(null, new object[] { args });
    [Test] public void DefaultUsesAllTwentyOneExistingRegistryEntries()
    {
        var registry = (IEnumerable)Type.GetType("LWS.TruckSports.SportsImpossibleGames, Assembly-CSharp", true).GetProperty("All").GetValue(null);
        Assert.That(Select(), Is.EqualTo(registry.Cast<object>().Select(e => (string)e.GetType().GetField("scene").GetValue(e)).ToArray()));
        Assert.That(Select().Length, Is.EqualTo(21));
    }
    [Test] public void ExplicitSubsetPreservesRequestedOrder()
    { Assert.That(Select("-tsImpossibleCompleteScenes", "TruckSnowmanBuilding,TruckYoga"), Is.EqualTo(new[] { "TruckSnowmanBuilding", "TruckYoga" })); }
    [TestCase("TruckTaxi")]
    [TestCase("TruckYoga,TruckYoga")]
    [TestCase("")]
    [TestCase("../TruckYoga")]
    public void UnregisteredDuplicateOrEmptyScopesFail(string selection)
    { Assert.That(Assert.Throws<TargetInvocationException>(() => Select("-tsImpossibleCompleteScenes", selection)).InnerException, Is.TypeOf<ArgumentException>()); }
    [Test] public void MissingScopeValueFails()
    { Assert.That(Assert.Throws<TargetInvocationException>(() => Select("-tsImpossibleCompleteScenes")).InnerException, Is.TypeOf<ArgumentException>()); }
    [TestCase("1", 1)]
    [TestCase("4", 4)]
    public void ExplicitSupportedSimulationScaleIsRecorded(string input, int expected)
    { Assert.That(Probe.GetMethod("CompleteSimulationScale").Invoke(null, new object[] { new[] { "-tsImpossibleCompleteScale", input } }), Is.EqualTo(expected)); }
    [TestCase("0")][TestCase("-1")][TestCase("2")][TestCase("100")][TestCase("NaN")]
    public void UnsupportedSimulationScalesAreRejected(string input)
    { Assert.That(Assert.Throws<TargetInvocationException>(() => Probe.GetMethod("CompleteSimulationScale").Invoke(null, new object[] { new[] { "-tsImpossibleCompleteScale", input } })).InnerException, Is.TypeOf<ArgumentException>()); }
    [TestCase(3, 63, true, 488, 488, 488, true)]
    [TestCase(2, 63, true, 488, 488, 488, false)]
    [TestCase(3, 31, true, 488, 488, 488, false)]
    [TestCase(3, 63, false, 488, 488, 488, false)]
    [TestCase(3, 63, true, 488, 1, 488, false)]
    [TestCase(3, 63, true, 488, 488, 976, false)]
    public void FinalConstructionAwardRequiresCompletedBuildAndExactMeasuredGrade(int placed, int decorations, bool complete, int grade, double award, double total, bool accepted)
    { Assert.That(Probe.GetMethod("CompleteSnowmanEvidence").Invoke(null, new object[] { placed, decorations, complete, grade, award, total }), Is.EqualTo(accepted)); }
    [TestCase(30, 30, 24, 22200, 22200, true, true)]
    [TestCase(30, 29, 24, 22200, 22200, true, false)]
    [TestCase(30, 30, 0, 22200, 22200, true, false)]
    [TestCase(30, 30, 24, 22200, 22201, true, false)]
    [TestCase(30, 30, 24, 22200, 66600, false, true)]
    [TestCase(30, 30, 24, 22200, 10000, false, false)]
    public void RhythmAcceptanceRequiresResolvedPositiveJudgmentsAndConservedTotals(int count, int resolved, int hits, int score, int total, bool single, bool accepted)
    { Assert.That(Probe.GetMethod("CompleteRhythmEvidence").Invoke(null, new object[] { count, resolved, hits, score, total, single }), Is.EqualTo(accepted)); }
}
#endif
