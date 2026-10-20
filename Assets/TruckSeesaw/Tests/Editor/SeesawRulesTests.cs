using System;
using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
    public sealed class SeesawRulesTests
    {
        Type type; object rules; bool[] recovery;
        object Call(string name,params object[] args)=>type.GetMethod(name).Invoke(rules,args);
        T Read<T>(string name)=>(T)type.GetProperty(name).GetValue(rules);
        T Field<T>(string name)=>(T)type.GetField(name).GetValue(rules);
        void Step(float dt,int falls=0)=>Call("Step",dt,falls,recovery);
        [SetUp]public void Reset(){type=Type.GetType("LWS.TruckSeesaw.SeesawRules, Assembly-CSharp",true);rules=Activator.CreateInstance(type);recovery=new bool[4];Call("Reset",4,3,90f);}
        [Test]public void SurvivalRewardsOnlyElapsedActiveTime(){Step(14.9f);Assert.That(Field<int[]>("scores"),Is.All.Zero);Step(.2f);Assert.That(Field<int[]>("scores"),Is.All.EqualTo(1));}
        [Test]public void LegitimateRecentRamCreditsOneKnockoff(){Call("RecordPush",0,1,4f);Step(.02f,2);Assert.That(Field<int[]>("scores")[0],Is.EqualTo(3));Step(.02f,2);Assert.That(Field<int[]>("knockoffs")[0],Is.EqualTo(1));}
        [Test]public void GentleContactCannotClaimFall(){Call("RecordPush",0,1,.2f);Step(.02f,2);Assert.That(Field<int[]>("knockoffs")[0],Is.Zero);}
        [Test]public void ExpiredRamDoesNotClaimAnUnrelatedLaterFall(){Call("RecordPush",0,1,4f);Step(5.1f);Step(.02f,2);Assert.That(Field<int[]>("knockoffs")[0],Is.Zero);}
        [Test]public void OneSurvivorGetsOnlyOneWinAward(){Step(.02f,14);Assert.That(Read<int>("HeatWinner"),Is.Zero);Assert.That(Field<int[]>("scores")[0],Is.EqualTo(5));Step(4,14);Assert.That(Field<int[]>("scores")[0],Is.EqualTo(5));}
        [Test]public void SimultaneousFinalFallsAreADraw(){Step(.02f,15);Assert.That(Read<int>("HeatWinner"),Is.EqualTo(-1));Assert.That(Field<int[]>("heatWins"),Is.All.Zero);}
        [Test]public void SurvivingTimeoutDoesNotInventASoleSurvivor(){Step(90);Assert.That(Read<bool>("HeatComplete"),Is.True);Assert.That(Read<int>("HeatWinner"),Is.EqualTo(-1));Assert.That(Field<int[]>("scores"),Is.All.EqualTo(6));}
        [Test]public void ConfirmedBalanceRecoveryAwardsOnlyActiveTruck(){recovery[2]=true;Step(.02f);Assert.That(Field<int[]>("recoveries")[2],Is.EqualTo(1));Assert.That(Field<int[]>("scores")[2],Is.EqualTo(2));}
        [Test]public void FallCannotAlsoCollectABalanceRecovery(){recovery[2]=true;Step(.02f,4);Assert.That(Field<int[]>("recoveries")[2],Is.Zero);}
        [Test]public void RepeatedRecoveryObservationCannotFarmPoints(){recovery[2]=true;Step(.02f);Step(.02f);Assert.That(Field<int[]>("recoveries")[2],Is.EqualTo(1));recovery[2]=false;Step(.02f);recovery[2]=true;Step(.02f);Assert.That(Field<int[]>("recoveries")[2],Is.EqualTo(2));}
        [Test]public void NextHeatRestoresBodiesAndPreservesMatchTotals(){Step(.02f,14);Assert.That((bool)Call("NextHeat"),Is.True);Assert.That(Read<int>("Heat"),Is.EqualTo(1));Assert.That(Field<bool[]>("alive"),Is.All.True);Assert.That(Field<int[]>("scores")[0],Is.EqualTo(5));Assert.That(Field<float[]>("survival"),Is.All.Zero);}
        [Test]public void FinalHeatLocksMatchAndSharedPlaces(){Call("Reset",4,1,90f);Step(90);Assert.That(Read<bool>("Complete"),Is.True);Assert.That((bool)Call("NextHeat"),Is.False);for(int i=0;i<4;i++)Assert.That((int)Call("Place",i),Is.EqualTo(1));}
        [Test]public void SoloPracticeDoesNotEndImmediately(){Call("Reset",1,1,90f);Step(1);Assert.That(Read<bool>("HeatComplete"),Is.False);Step(89);Assert.That(Read<int>("HeatWinner"),Is.Zero);}
        [Test]public void RematchClearsAllPriorAwards(){Step(.02f,14);Call("Reset",4,3,90f);Assert.That(Field<int[]>("scores"),Is.All.Zero);Assert.That(Field<int[]>("heatWins"),Is.All.Zero);Assert.That(Read<bool>("HeatComplete"),Is.False);}
    }
}
