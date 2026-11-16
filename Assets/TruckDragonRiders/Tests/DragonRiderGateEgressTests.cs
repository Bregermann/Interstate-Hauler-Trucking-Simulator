using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckDragonRiders.Tests
{
    public sealed class DragonRiderGateEgressTests
    {
        [Test]public void ExitRemainsStraightUntilActualForwardDistanceThenReleases()
        {
            var exit=new DragonRiderGateEgress();var center=new Vector3(100,250,-80);exit.Arm(center,Vector3.right,55);
            Assert.That(exit.TryTarget(center+Vector3.right*54.99f,out var target),Is.True);Assert.That(target,Is.EqualTo(center+Vector3.right*55));Assert.That(exit.TryTarget(target,out _),Is.False);Assert.That(exit.Pending,Is.False);Assert.That(exit.TryTarget(center,out _),Is.False,"Returning behind a cleared gate must not rearm an old exit.");
        }
        [Test]public void LateralTravelAndGoingBackwardsDoNotPretendTailHasCleared()
        {
            var exit=new DragonRiderGateEgress();exit.Arm(Vector3.zero,Vector3.forward,55);Assert.That(exit.TryTarget(new Vector3(1000,1000,0),out var target),Is.True);Assert.That(target,Is.EqualTo(new Vector3(0,0,55)));Assert.That(exit.TryTarget(new Vector3(0,0,-500),out _),Is.True);
        }
        [Test]public void RematchResetAndNextRealPassageReplaceOldIntent()
        {
            var exit=new DragonRiderGateEgress();exit.Arm(Vector3.zero,Vector3.forward,55);exit.Reset();Assert.That(exit.TryTarget(Vector3.zero,out _),Is.False);exit.Arm(new Vector3(500,250,20),Vector3.left,60);Assert.That(exit.TryTarget(new Vector3(499,250,20),out var target),Is.True);Assert.That(target,Is.EqualTo(new Vector3(440,250,20)));
        }
        [Test]public void ScoredGateExitSurvivesOrderedProgressAdvancingWithoutAwardingAnotherGate()
        {
            var gates=new DragonRiderGateDefinition[12];for(int i=0;i<gates.Length;i++)gates[i]=new DragonRiderGateDefinition{center=new Vector3(i*300,250,0),rotation=Quaternion.identity,clearRadius=85};var race=new DragonRiderRace();race.Begin(1,1,gates);Assert.That(race.ObservePassage(0,new Vector3(0,250,-10),new Vector3(0,250,10),3,true),Is.True);Assert.That(race.NextGate(0),Is.EqualTo(1));
            var exit=new DragonRiderGateEgress();exit.Arm(gates[0].center,Vector3.forward,55);Assert.That(exit.TryTarget(new Vector3(0,250,20),out var target),Is.True);Assert.That(target,Is.EqualTo(new Vector3(0,250,55)));Assert.That(race.Passed[0],Is.EqualTo(1));Assert.That(race.Finished(0),Is.False);
        }
        [Test]public void InvalidPoseOrUnboundedDistanceCannotCreateAnExit()
        {
            var exit=new DragonRiderGateEgress();Assert.Throws<ArgumentException>(()=>exit.Arm(Vector3.zero,Vector3.zero,55));Assert.Throws<ArgumentException>(()=>exit.Arm(Vector3.zero,Vector3.forward,float.NaN));Assert.Throws<ArgumentException>(()=>exit.Arm(Vector3.zero,Vector3.forward,151));Assert.Throws<ArgumentException>(()=>exit.Arm(Vector3.zero,new Vector3(float.MaxValue,0,0),55));Assert.Throws<ArgumentException>(()=>exit.TryTarget(new Vector3(float.PositiveInfinity,0,0),out _));Assert.That(exit.Pending,Is.False);
        }
    }
}
