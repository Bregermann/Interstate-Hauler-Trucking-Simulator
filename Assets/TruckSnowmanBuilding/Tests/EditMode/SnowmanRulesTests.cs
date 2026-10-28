using System;
using NUnit.Framework;
using LWS.TruckSnowmanBuilding;
public sealed class SnowmanRulesTests
{
    [Test]public void DeliveryArrivalEnclosesActualDriveStopAndUsesMeasuredLateralTrajectory()
    {
        Assert.That(SnowmanDeliveryRoute.Arrived(1.234,0),Is.True,"Actual V2 recovery delta was inside the driver's 1.3m stop radius but outside its incompatible 1.2m phase gate.");
        Assert.That(SnowmanDeliveryRoute.Arrived(1.41,0),Is.False);
        double predicted=SnowmanDeliveryRoute.InterceptX(-11.07913,-19.04857,-.20043,2.53755);
        Assert.That(predicted,Is.EqualTo(-11.95182).Within(.0001),"Real V2 incoming trajectory must not aim at the original x=-10 lane.");
        Assert.That(SnowmanDeliveryRoute.InterceptX(-72,18,-.6,.14),Is.EqualTo(-72),"An escaped ball uses actual recovery geometry rather than backward trajectory prediction.");
    }
    [Test]public void IdleSpinSlidingAirborneAndUnavailableSnowCannotGrow(){foreach(var tuple in new[]{(0d,2d,.25d,10d),(1d,0d,.25d,10d),(1d,1d,0d,10d),(1d,1d,.25d,0d),(5d,10d,.25d,10d)}){double r=SnowmanRules.Grow(.75,tuple.Item1,tuple.Item2,tuple.Item3,tuple.Item4,3,out double used);Assert.That(r,Is.EqualTo(.75));Assert.That(used,Is.Zero);}}
    [Test]public void GenuineRollingAddsOnlyAvailableFiniteVolumeAndHonorsRadiusCap(){double r=SnowmanRules.Grow(1,1,1,.25,.1,3,out double used);Assert.That(used,Is.EqualTo(.1));Assert.That(4*Math.PI*(r*r*r-1)/3,Is.EqualTo(.1).Within(1e-10));double capped=SnowmanRules.Grow(2.99,1,1,1,100,3,out used);Assert.That(capped,Is.EqualTo(3).Within(1e-10));Assert.That(used,Is.LessThan(2));}
    [Test]public void StackMustBeOrderedAndPhysicalThenAllSixRealDecorationsAreRequired(){var r=new SnowmanRules();Assert.That(r.Place(1,true),Is.False);Assert.That(r.Place(0,false),Is.False);for(int role=0;role<3;role++)Assert.That(r.Place(role,true),Is.True);Assert.That(r.Place(2,true),Is.False);Assert.That(r.Decorate(0,false),Is.False);for(int f=0;f<5;f++)r.Decorate(f,true);Assert.That(r.Finish(true,10,2.4,1.7,1.2,1,300),Is.False);r.Decorate(5,true);Assert.That(r.Finish(false,10,2.4,1.7,1.2,1,300),Is.False);Assert.That(r.Finish(true,10,2.4,1.7,1.2,1,300),Is.True);int score=r.Score;Assert.That(r.Finish(true,10,2.4,1.7,1.2,1,10),Is.False);Assert.That(r.Score,Is.EqualTo(score));}
    [Test]public void GradeRewardsHeightProportionsStabilityAndRealCompletionTime(){int ideal=SnowmanRules.Grade(10,2.4,1.728,1.2096,1,300);Assert.That(ideal,Is.GreaterThan(SnowmanRules.Grade(8,2.4,1.728,1.2096,1,300)));Assert.That(ideal,Is.GreaterThan(SnowmanRules.Grade(10,2.4,.8,1.5,1,300)));Assert.That(ideal,Is.GreaterThan(SnowmanRules.Grade(10,2.4,1.728,1.2096,.2,300)));Assert.That(ideal,Is.GreaterThan(SnowmanRules.Grade(10,2.4,1.728,1.2096,1,600)));}
    [Test]public void InvalidSaveRuleStateFailsBeforeRestoration(){var r=new SnowmanRules();Assert.Throws<ArgumentException>(()=>r.Restore(4,63,true,100));Assert.Throws<ArgumentException>(()=>r.Restore(3,64,true,100));Assert.Throws<ArgumentException>(()=>r.Restore(2,63,true,100));Assert.That(r.Placed,Is.Zero);Assert.That(r.Complete,Is.False);}
}
