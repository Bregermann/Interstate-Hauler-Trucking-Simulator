#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;

public sealed class FinalTotalResultPlacingTests
{
    static int Place(int slot, int count, int[] totals, int winner = -1)
        => (int)Type.GetType("LWS.TruckSports.SportsResultPlacings, Assembly-CSharp", true)
            .GetMethod("FromFinalTotals").Invoke(null, new object[] { slot, count, totals, winner });
    [Test] public void SharedFinalTotalsRetainCompetitionPlacingsAndNegativeValues()
    {
        var scores = new[] {70,70,10,-5};
        CollectionAssert.AreEqual(new[] {1,1,3,4}, new[] {Place(0,4,scores),Place(1,4,scores),Place(2,4,scores),Place(3,4,scores)});
        CollectionAssert.AreEqual(new[] {70,70,10,-5}, scores);
    }
    [Test] public void ExistingTiebreakWinnerPrecedesEqualTotalsWithoutChangingScores()
    {
        var scores = new[] {70,70,10};
        CollectionAssert.AreEqual(new[] {2,1,3}, new[] {Place(0,3,scores,1),Place(1,3,scores,1),Place(2,3,scores,1)});
        CollectionAssert.AreEqual(new[] {70,70,10}, scores);
    }
    [Test] public void DisplayOrderingCannotOverrideTheSportDeclaredWinner()
    {
        var scores = new[] {70,70,10,-5};
        CollectionAssert.AreEqual(new[] {2,2,4,1}, new[] {Place(0,4,scores,3),Place(1,4,scores,3),Place(2,4,scores,3),Place(3,4,scores,3)});
    }
    [Test] public void InvalidRosterReferencesAreRejected()
    {
        foreach (var invoke in new Action[] {()=>Place(0,1,null),()=>Place(0,0,new[]{1}),()=>Place(-1,1,new[]{1}),()=>Place(0,2,new[]{1}),()=>Place(0,1,new[]{1},1)})
        {
            var error = Assert.Throws<TargetInvocationException>(()=>invoke());
            Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
#endif
