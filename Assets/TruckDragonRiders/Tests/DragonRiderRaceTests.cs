using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckDragonRiders.Tests
{
    public sealed class DragonRiderRaceTests
    {
        static DragonRiderGateDefinition[] Gates()
        {
            var gates=new DragonRiderGateDefinition[8];
            for(int i=0;i<8;i++)gates[i]=new DragonRiderGateDefinition{center=new Vector3(0,100,i*100),rotation=Quaternion.identity,clearRadius=20};
            return gates;
        }
        static bool Cross(DragonRiderRace race,int slot,int gate,bool mounted=true,float offset=0)
            =>race.ObservePassage(slot,new Vector3(offset,100,gate*100-10),new Vector3(offset,100,gate*100+10),2,mounted);
        [Test] public void MissingOrUnorderedActualPassagesCannotFinishARace()
        {
            var race=new DragonRiderRace();race.Begin(1,1,Gates());race.Tick(900);
            Assert.That(Cross(race,0,7),Is.False);Assert.That(race.Finished(0),Is.False);Assert.That(race.Passed[0],Is.Zero);
            Assert.That(Cross(race,0,0,false),Is.False);Assert.That(Cross(race,0,0,true,19),Is.False);
            Assert.That(race.ObservePassage(0,new Vector3(0,100,10),new Vector3(0,100,-10),2,true),Is.False);
            Assert.That(Cross(race,0,0),Is.True);Assert.That(race.Passed[0],Is.EqualTo(1));Assert.That(race.Complete,Is.False);
        }
        [Test] public void TwoRealLapsFinishOnlyAfterEveryOrderedGate()
        {
            var race=new DragonRiderRace();race.Begin(1,2,Gates());
            for(int lap=0;lap<2;lap++)for(int gate=0;gate<8;gate++)
            {
                race.Tick(1);Assert.That(Cross(race,0,gate),Is.True);
                Assert.That(race.Finished(0),Is.EqualTo(lap==1&&gate==7));
            }
            Assert.That(race.FinishTime[0],Is.EqualTo(16));Assert.That(race.Complete,Is.True);
            Assert.That(Cross(race,0,7),Is.False,"A repeated finish contact cannot create a second result.");
        }
        [Test] public void PhysicalFinishPlusPenaltyCanLoseToALaterUnpenalizedFinish()
        {
            var race=new DragonRiderRace();race.Begin(2,1,Gates());race.AddPenalty(0,12);
            for(int gate=0;gate<8;gate++){race.Tick(1);Assert.That(Cross(race,0,gate),Is.True);}
            race.Tick(2);for(int gate=0;gate<8;gate++)Assert.That(Cross(race,1,gate),Is.True);
            Assert.That(race.FinishTime[0],Is.EqualTo(20));Assert.That(race.FinishTime[1],Is.EqualTo(10));
            Assert.That(race.Place(1,null),Is.EqualTo(1));Assert.That(race.Place(0,null),Is.EqualTo(2));
        }
        [Test] public void FailedRescuePenaltyDoesNotInventAClearedGateOrFinish()
        {
            var race=new DragonRiderRace();race.Begin(1,1,Gates());race.AddPenalty(0,12);race.ClearPassageApproach(0);
            Assert.That(race.Passed[0],Is.Zero);Assert.That(race.Finished(0),Is.False);Assert.That(race.NeedsApproach(0),Is.True);
            race.FailedRescuesExhausted(0);Assert.That(race.Complete,Is.True);Assert.That(race.FinishTime[0],Is.LessThan(0));
        }
        [Test] public void RematchClearsOutcomesPenaltiesAndGateApproaches()
        {
            var race=new DragonRiderRace();race.Begin(1,1,Gates());race.Tick(10);Cross(race,0,0);race.AddPenalty(0,4);race.FailedRescuesExhausted(0);
            race.Begin(1,2,Gates());Assert.That(race.Clock,Is.Zero);Assert.That(race.Passed[0],Is.Zero);Assert.That(race.Penalty[0],Is.Zero);
            Assert.That(race.Retired[0],Is.False);Assert.That(race.FinishTime[0],Is.LessThan(0));Assert.That(race.NeedsApproach(0),Is.True);
        }
        [Test] public void GateSourceMutationsCannotChangeAnActiveRace()
        {
            var source=Gates();var race=new DragonRiderRace();race.Begin(1,1,source);source[0].center=Vector3.one*9000;
            var copy=race.Gate(0);copy.center=Vector3.one*8000;
            Assert.That(Cross(race,0,0),Is.True);
        }
        [Test] public void WholeTruckClearanceAndFastSweptPassageAreEvaluatedAtTheActualPlane()
        {
            var race=new DragonRiderRace();race.Begin(1,1,Gates());
            Assert.That(race.ObservePassage(0,new Vector3(18.1f,100,-100),new Vector3(18.1f,100,100),2,true),Is.False);
            Assert.That(race.ObservePassage(0,new Vector3(17.9f,100,-100),new Vector3(17.9f,100,100),2,true),Is.True);
            Assert.That(race.Passed[0],Is.EqualTo(1));
        }
        [Test] public void StaminaMakesSprintingCostlyAndNormalFlightRestoresIt()
        {
            var budget=new DragonRiderFlightBudget();budget.Reset(100);Assert.That(budget.Step(2,0,true,false),Is.True);Assert.That(budget.Stamina,Is.EqualTo(62).Within(.001));
            budget.Step(2,0,false,false);Assert.That(budget.Stamina,Is.EqualTo(88).Within(.001));
            budget.Step(1,1,false,false);Assert.That(budget.Stamina,Is.EqualTo(79).Within(.001));
        }
        [Test] public void AirBrakeIsLimitedAndCannotSimultaneouslySprint()
        {
            var budget=new DragonRiderFlightBudget();budget.Reset(100);Assert.That(budget.Step(2,0,true,true),Is.False);Assert.That(budget.BrakeCharge,Is.Zero);
            budget.Step(2,0,false,false);Assert.That(budget.BrakeCharge,Is.EqualTo(.7f).Within(.001));
        }
        [Test] public void RepeatedAttacksCanDislodgeButQuietFlightRestoresStability()
        {
            var budget=new DragonRiderFlightBudget();budget.Reset(100);Assert.That(budget.ApplyAttack(40),Is.False);
            budget.Step(2,0,false,false);Assert.That(budget.Stability,Is.EqualTo(60));
            Assert.That(budget.ApplyAttack(40),Is.False);Assert.That(budget.ApplyAttack(40),Is.True);
            budget.Step(4,0,false,false);Assert.That(budget.Stability,Is.EqualTo(7).Within(.001));
        }
        [Test] public void InvalidFlightAndGateConfigurationIsRejected()
        {
            var race=new DragonRiderRace();Assert.Throws<ArgumentException>(()=>race.Begin(5,1,Gates()));
            var gates=Gates();gates[2].rotation=new Quaternion(0,0,0,0);Assert.Throws<ArgumentException>(()=>race.Begin(1,1,gates));
            var budget=new DragonRiderFlightBudget();Assert.Throws<ArgumentOutOfRangeException>(()=>budget.Reset(float.NaN));
        }
    }
}
