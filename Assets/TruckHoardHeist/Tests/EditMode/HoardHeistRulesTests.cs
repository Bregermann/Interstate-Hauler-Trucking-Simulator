using System;
using NUnit.Framework;
using LWS.TruckHoardHeist;

public sealed class HoardHeistRulesTests
{
    static HoardHeistRules Game(int players = 4) => new HoardHeistRules(players, 2, 160, new[] { 50, 90, 130, 170 }, new[] { 80f, 80f, 80f, 80f }, new[] { 101, 102, 103, 104 });
    static HoardDeliveryEvidence Proof(int body = 101, int owner = 0, bool inside = true, bool dynamic = true, bool joint = true, bool stopped = true, bool settled = true)
        => new HoardDeliveryEvidence(body, owner, inside, dynamic, joint, stopped, settled);

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void OriginalLiftAndRackMustPrecedeOnceOnlyPhysicalDelivery(int players)
    {
        var game = Game(players);
        Assert.That(game.Deliver(0, 0, Proof()), Is.EqualTo(-1));
        Assert.That(game.ReserveLift(0, 0, false), Is.False);
        Assert.That(game.ReserveLift(0, 0, true), Is.True);
        Assert.That(game.Deliver(0, 0, Proof()), Is.EqualTo(-1));
        Assert.That(game.CompleteLoad(0, 0, false), Is.False);
        Assert.That(game.CompleteLoad(0, 0, true), Is.True);
        Assert.That(game.Deliver(0, 0, Proof()), Is.EqualTo(50));
        Assert.That(game.Deliver(0, 0, Proof()), Is.EqualTo(-1));
        Assert.That(game.Scores[0], Is.EqualTo(50));
        Assert.That(game.LoadCount(0), Is.Zero);
        Assert.That(game.DeliveredCount, Is.EqualTo(1));
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void EachMissingPhysicalReceptionWitnessRejectsTheAward(int missing)
    {
        var game = Game(); game.ReserveLift(0,0,true); game.CompleteLoad(0,0,true);
        var evidence = Proof(inside: missing != 0, dynamic: missing != 1, joint: missing != 2, stopped: missing != 3, settled: missing != 4);
        Assert.That(game.Deliver(0,0,evidence), Is.EqualTo(-1));
        Assert.That(game.Scores[0], Is.Zero); Assert.That(game.States[0], Is.EqualTo(HoardCargoState.Carried));
    }
    [Test]
    public void AnotherBodyOrCrewCannotRedeemTheOriginalCargoIdentity()
    {
        var game = Game(); game.ReserveLift(0,0,true); game.CompleteLoad(0,0,true);
        Assert.That(game.Deliver(0,0,Proof(body:102)), Is.EqualTo(-1));
        Assert.That(game.Deliver(0,1,Proof(owner:1)), Is.EqualTo(-1));
        Assert.That(game.Deliver(0,0,Proof(owner:1)), Is.EqualTo(-1));
        Assert.That(game.Scores, Is.All.EqualTo(0));
    }
    [Test]
    public void PendingLoadsReserveRealCapacityAndCannotBeDoubleClaimed()
    {
        var game = Game(); Assert.That(game.ReserveLift(0,0,true), Is.True);
        Assert.That(game.ReserveLift(0,1,true), Is.False);
        Assert.That(game.ReserveLift(1,0,true), Is.False, "A crew has one physical hoist.");
        game.CompleteLoad(0,0,true); Assert.That(game.ReserveLift(1,0,true), Is.True);
        game.CompleteLoad(1,0,true); Assert.That(game.LoadKg(0), Is.EqualTo(160));
        Assert.That(game.Fits(2,0), Is.False); Assert.That(game.Scores[0], Is.Zero);
    }
    [Test]
    public void BrokenRestraintLeavesTheSameTreasureAvailableToOtherCrewsWithoutCredit()
    {
        var game = Game(); game.ReserveLift(0,0,true); game.CompleteLoad(0,0,true);
        Assert.That(game.Drop(0,0,false), Is.False); Assert.That(game.Drop(0,1,true), Is.False);
        Assert.That(game.Drop(0,0,true), Is.True); Assert.That(game.Scores[0], Is.Zero);
        Assert.That(game.ReserveLift(0,1,true), Is.True); game.CompleteLoad(0,1,true);
        Assert.That(game.Deliver(0,1,Proof(owner:1)), Is.EqualTo(50));
        Assert.That(game.Scores[0], Is.Zero); Assert.That(game.Scores[1], Is.EqualTo(50));
        Assert.That(game.Lose(0,true), Is.False); Assert.That(game.ReserveLift(0,0,true), Is.False);
    }
    [Test]
    public void FiniteOriginalInventoryConservesValueAcrossLossDeliveryAndFreshMatch()
    {
        var game = Game(1);
        for(int i=0;i<4;i++)
        {
            if(i==2) { Assert.That(game.Lose(i,true),Is.True);continue; }
            game.ReserveLift(i,0,true);game.CompleteLoad(i,0,true);
            Assert.That(game.Deliver(i,0,Proof(body:101+i)),Is.GreaterThan(0));
        }
        Assert.That(game.Exhausted,Is.True);Assert.That(game.DeliveredCount,Is.EqualTo(3));Assert.That(game.LostCount,Is.EqualTo(1));Assert.That(game.Scores[0],Is.EqualTo(310));
        var fresh=Game(1);Assert.That(fresh.Exhausted,Is.False);Assert.That(fresh.Scores[0],Is.Zero);Assert.That(fresh.States,Is.All.EqualTo(HoardCargoState.Loose));
    }
    [Test]
    public void NoiseNeedsActualRangeAndObstructionAttenuatesItWhileQuietAllowsSuspicionToFade()
    {
        var exposed=new HoardAlertness();var covered=new HoardAlertness();
        Assert.That(exposed.Hear(1,500,110,false),Is.False);Assert.That(exposed.Value,Is.Zero);
        Assert.That(exposed.Hear(2,20,110,false),Is.True);Assert.That(covered.Hear(2,20,110,true),Is.True);
        Assert.That(covered.Value,Is.LessThan(exposed.Value));
        exposed.Step(20,false);Assert.That(exposed.Value,Is.Zero);Assert.That(exposed.WakeAllowed,Is.False);
    }
    [Test]
    public void ARealAlarmCrossingLatchesUntilTheWakeSequenceAndReturnToSleepComplete()
    {
        var alert=new HoardAlertness();for(int i=0;i<4;i++)alert.Hear(4,2,110,false);
        Assert.That(alert.WakeAllowed,Is.True);Assert.That(alert.Stage,Is.EqualTo(HoardAlertStage.Awakening));
        alert.Step(.1f,false);Assert.That(alert.WakeAllowed,Is.True,"A quiet next frame must not undo an already heard alarm.");
        alert.MarkAwake();Assert.That(alert.Awakened,Is.True);alert.ReturnToSleep();Assert.That(alert.WakeAllowed,Is.False);Assert.That(alert.Awakened,Is.False);
    }
    [Test]
    public void InvalidPhysicsNoiseAndDuplicateOriginalBodyIdsCannotMutateAuthority()
    {
        var alert=new HoardAlertness();Assert.That(alert.Hear(float.NaN,0,110,false),Is.False);Assert.That(alert.Hear(1,float.PositiveInfinity,110,false),Is.False);alert.Step(float.NaN,true);Assert.That(alert.Value,Is.Zero);
        Assert.Throws<ArgumentException>(()=>new HoardHeistRules(1,2,100,new[]{50,50},new[]{20f,20f},new[]{7,7}));
    }
}
