using NUnit.Framework;
using LWS.TruckArchaeology;
public sealed class ArchaeologyRulesTests
{
    [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]public void ActualScannerClearanceLoadingAndReceptionAreAllRequiredBeforeOnceOnlyValue(int players)
    {
        var r=new ArchaeologyRules(players,false);Assert.That(r.Lift(0,0,true),Is.False);Assert.That(r.Scan(0,0,false),Is.False);Assert.That(r.Scan(0,0,true),Is.True);Assert.That(r.Expose(0,false),Is.False);Assert.That(r.Expose(0,true),Is.True);Assert.That(r.Lift(0,0,false),Is.False);Assert.That(r.Lift(0,0,true),Is.True);Assert.That(r.Load(0,0,false),Is.False);Assert.That(r.Deliver(0,0,true,100),Is.EqualTo(-1));Assert.That(r.Load(0,0,true),Is.True);Assert.That(r.Deliver(0,0,false,100),Is.EqualTo(-1));Assert.That(r.Deliver(0,0,true,100),Is.EqualTo(100));Assert.That(r.Deliver(0,0,true,100),Is.EqualTo(-1));Assert.That(r.Scores[0],Is.EqualTo(100));Assert.That(r.Delivered,Is.EqualTo(1));
    }
    [Test]public void CompetitivePlotsRejectOpponentsWhileCooperativeFindsAllowActualSharedWork()
    {
        var cup=new ArchaeologyRules(4,false);Assert.That(cup.Scan(3,0,true),Is.False);Assert.That(cup.Scan(3,1,true),Is.True);var crew=new ArchaeologyRules(4,true);Assert.That(crew.Scan(3,0,true),Is.True);Assert.That(crew.Expose(3,true),Is.True);Assert.That(crew.Lift(3,2,true),Is.True);Assert.That(crew.Load(3,0,true),Is.False);Assert.That(crew.Load(3,2,true),Is.True);Assert.That(crew.Deliver(3,2,true,140),Is.EqualTo(140));
    }
    [Test]public void AggressivePhysicalToolsReducePreservationMoreThanActualPrecisionWork()
    {
        var aggressive=new ArchaeologyRules(1,false);var careful=new ArchaeologyRules(1,false);for(int i=0;i<5;i++){aggressive.Damage(0,ArchaeologyRules.ToolLoss(ArchaeologyTool.Drill));careful.Damage(0,ArchaeologyRules.ToolLoss(ArchaeologyTool.Brush));}
        foreach(var r in new[]{aggressive,careful}){r.Scan(0,0,true);r.Expose(0,true);r.Lift(0,0,true);r.Load(0,0,true);r.Deliver(0,0,true,180);}Assert.That(careful.Scores[0],Is.GreaterThan(aggressive.Scores[0]));double locked=careful.Preservation[0];careful.Damage(0,1);Assert.That(careful.Preservation[0],Is.EqualTo(locked));
    }
    [Test]public void EveryFindIsNeededAndOriginalCargoSlotBecomesAvailableAfterDelivery()
    {
        var r=new ArchaeologyRules(1,false);for(int a=0;a<3;a++){r.Scan(a,0,true);r.Expose(a,true);r.Lift(a,0,true);r.Load(a,0,true);Assert.That(r.Cargo(0),Is.EqualTo(a));r.Deliver(a,0,true,100);Assert.That(r.Cargo(0),Is.EqualTo(-1));Assert.That(r.Complete,Is.EqualTo(a==2));}Assert.That(r.Scores[0],Is.EqualTo(300));var fresh=new ArchaeologyRules(1,false);Assert.That(fresh.Delivered,Is.Zero);Assert.That(fresh.Scores[0],Is.Zero);Assert.That(fresh.Preservation[0],Is.EqualTo(1));
    }
    [Test]public void LostActualCargoJointReturnsTheSameFindToPhysicalRecoveryWithoutPoints()
    {
        var r=new ArchaeologyRules(1,false);r.Scan(0,0,true);r.Expose(0,true);r.Lift(0,0,true);r.Load(0,0,true);
        Assert.That(r.DropCargo(0,0,false),Is.False);Assert.That(r.DropCargo(0,0,true),Is.True);Assert.That(r.Stages[0],Is.EqualTo(ArtifactStage.Exposed));Assert.That(r.Owners[0],Is.EqualTo(-1));Assert.That(r.Scores[0],Is.Zero);Assert.That(r.Deliver(0,0,true,100),Is.EqualTo(-1));Assert.That(r.Lift(0,0,true),Is.True,"The original physical find can be reacquired rather than stranded or respawned.");
    }
    [Test]public void NonfiniteDamageAndUnknownFindsCannotMutateProgress(){var r=new ArchaeologyRules(1,false);r.Damage(0,double.NaN);r.Damage(0,double.PositiveInfinity);r.Damage(-1,1);Assert.That(r.Preservation[0],Is.EqualTo(1));Assert.That(r.Scan(12,0,true),Is.False);Assert.That(r.Scan(3,0,true),Is.False);Assert.That(r.Delivered,Is.Zero);}
}
