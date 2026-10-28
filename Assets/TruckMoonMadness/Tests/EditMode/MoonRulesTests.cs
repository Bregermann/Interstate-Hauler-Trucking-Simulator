using System.Linq;
using NUnit.Framework;
using LWS.TruckMoonMadness;

public sealed class MoonRulesTests
{
    [Test] public void ContactAndIndependentOwnerAreRequiredForSingleCargo()
    {
        var r=new MoonRules(5,3);Assert.That(r.Collect(0,0,false),Is.False);Assert.That(r.Collect(0,0,true),Is.True);
        Assert.That(r.Collect(0,1,true),Is.False);Assert.That(r.Collect(1,0,true),Is.False);Assert.That(r.Collect(1,1,true),Is.True);
        Assert.That(r.Scores.Sum(),Is.Zero);Assert.That(r.Carried(0),Is.Zero);Assert.That(r.Carried(1),Is.EqualTo(1));
    }
    [Test] public void RealOwnStationOnlyScoresOnceThenRespawnIsRequired()
    {
        var r=new MoonRules(5,3);r.Collect(2,6,true);Assert.That(r.Deliver(2,5,true),Is.False);Assert.That(r.Deliver(2,6,false),Is.False);
        Assert.That(r.Deliver(2,6,true),Is.True);Assert.That(r.Deliver(2,6,true),Is.False);Assert.That(r.Collect(2,3,true),Is.False);
        Assert.That(r.Scores[6],Is.EqualTo(1));Assert.That(r.Respawn(2),Is.True);Assert.That(r.Respawn(2),Is.False);Assert.That(r.Collect(2,3,true),Is.True);
    }
    [Test] public void DroppedPhysicalCargoCanBeContestedWithoutInventingScore()
    {
        var r=new MoonRules(5,3);r.Collect(4,7,true);Assert.That(r.Drop(4),Is.True);Assert.That(r.Drop(4),Is.False);
        Assert.That(r.Scores.Sum(),Is.Zero);Assert.That(r.Collect(4,2,true),Is.True);Assert.That(r.Deliver(4,7,true),Is.False);Assert.That(r.Deliver(4,2,true),Is.True);
        Assert.That(r.Scores[2],Is.EqualTo(1));Assert.That(r.Scores[7],Is.Zero);
    }
    [Test] public void TargetLocksActualLeaderAndRejectsFurtherMutations()
    {
        var r=new MoonRules(5,3);for(int n=0;n<3;n++){r.Collect(0,3,true);Assert.That(r.Deliver(0,3,true),Is.True);if(n<2)r.Respawn(0);}
        Assert.That(r.Complete,Is.True);CollectionAssert.AreEqual(new[]{3},r.Leaders());Assert.That(r.Deliveries,Is.EqualTo(3));
        Assert.That(r.Collect(1,4,true),Is.False);Assert.That(r.Respawn(0),Is.False);Assert.That(r.Drop(1),Is.False);
    }
    [Test] public void TimeLimitKeepsActualTiesAndFreshMatchHasNoHistoricalOwnership()
    {
        var r=new MoonRules(5,5);foreach(int owner in new[]{1,5}){r.Collect(owner%5,owner,true);r.Deliver(owner%5,owner,true);}
        r.FinishAtTimeLimit();CollectionAssert.AreEquivalent(new[]{1,5},r.Leaders());var fresh=new MoonRules(5,5);Assert.That(fresh.Owners.All(i=>i==-1),Is.True);Assert.That(fresh.Scores.Sum(),Is.Zero);Assert.That(fresh.Complete,Is.False);
    }
    [TestCase(-1,0)][TestCase(5,0)][TestCase(0,-1)][TestCase(0,8)]public void InvalidPhysicalIdentityCannotClaimOrDeliver(int flag,int owner)
    {
        var r=new MoonRules(5,3);Assert.That(r.Collect(flag,owner,true),Is.False);Assert.That(r.Deliver(flag,owner,true),Is.False);Assert.That(r.Scores.Sum(),Is.Zero);
    }
}
