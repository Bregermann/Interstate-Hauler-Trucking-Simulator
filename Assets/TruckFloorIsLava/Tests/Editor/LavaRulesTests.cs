using System;
using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
    public sealed class LavaRulesTests
    {
        Type type;object rules;
        object Call(string name,params object[] args)=>type.GetMethod(name).Invoke(rules,args);
        T Read<T>(string name)=>(T)type.GetProperty(name).GetValue(rules);
        T Field<T>(string name)=>(T)type.GetField(name).GetValue(rules);
        [SetUp]public void Reset(){type=Type.GetType("LWS.TruckFloorIsLava.LavaRules, Assembly-CSharp",true);rules=Activator.CreateInstance(type);Call("Reset",8,new[]{5f,5f,5f,5f,5f,5f,5f,5f});}
        void Step(float dt,int mask=0)=>Call("Step",dt,mask);
        [Test]public void CountdownCannotActivateOrAgePlatforms(){Assert.That((bool)Call("Touch",0),Is.False);Step(10);Assert.That(Read<float>("Clock"),Is.Zero);Assert.That(Read<int>("Collapses"),Is.Zero);}
        [Test]public void UntouchedPlatformsNeverCollapseAutomatically(){Call("Begin");Step(10);Assert.That(Read<int>("Collapses"),Is.Zero);Assert.That((int)Call("Available"),Is.EqualTo(8));}
        [Test]public void GenuineTouchStartsOneFiniteWarning(){Call("Begin");Assert.That((bool)Call("Touch",0),Is.True);Step(4);Assert.That((float)Call("Remaining",0),Is.EqualTo(1));Assert.That(Read<int>("Collapses"),Is.Zero);Step(1);Assert.That(Read<int>("Collapses"),Is.EqualTo(1));}
        [Test]public void RepeatedTouchCannotResetCollapseTimer(){Call("Begin");Call("Touch",0);Step(4);Assert.That((bool)Call("Touch",0),Is.False);Step(1);Assert.That(Read<int>("Collapses"),Is.EqualTo(1));}
        [Test]public void FallingPlatformsCannotReactivateOrAwardRepeatedCollapses(){Call("Begin");Call("Touch",0);Step(5);Step(5);Assert.That((bool)Call("Touch",0),Is.False);Assert.That(Read<int>("Collapses"),Is.EqualTo(1));}
        [Test]public void SurvivalPointsRequireActualElapsedLiveTime(){Call("Begin");Step(14.9f);Assert.That(Field<int[]>("scores"),Is.All.Zero);Step(.2f);Assert.That(Field<int[]>("scores"),Is.All.EqualTo(1));}
        [Test]public void ValidRecentRamCanCreditOneActualLavaLoss(){Call("Begin");Call("RecordPush",0,1,5f);Step(.02f,2);Step(.02f,2);Assert.That(Field<int[]>("knockoffs")[0],Is.EqualTo(1));Assert.That(Field<int[]>("scores")[0],Is.EqualTo(1));}
        [Test]public void ProximityOrGentleContactCannotClaimAnElimination(){Call("Begin");Call("RecordPush",0,1,.1f);Step(.02f,2);Assert.That(Field<int[]>("knockoffs"),Is.All.Zero);}
        [Test]public void OldContactDoesNotCreditALaterUnrelatedFall(){Call("Begin");Call("RecordPush",0,1,5f);Step(4.1f);Step(.02f,2);Assert.That(Field<int[]>("knockoffs"),Is.All.Zero);}
        [Test]public void SoleSurvivorGetsOneLockedVictoryBonus(){Call("Begin");Step(.02f,254);Assert.That(Read<int>("Winner"),Is.Zero);Assert.That(Field<int[]>("scores")[0],Is.EqualTo(5));Step(30,1);Assert.That(Read<int>("Winner"),Is.Zero);Assert.That(Field<int[]>("scores")[0],Is.EqualTo(5));}
        [Test]public void SimultaneousFinalLavaContactsProduceSharedPlacing(){Call("Begin");Step(.02f,255);Assert.That(Read<int>("Winner"),Is.EqualTo(-1));Assert.That(Read<bool>("Complete"),Is.True);for(int i=0;i<8;i++)Assert.That((int)Call("Place",i),Is.EqualTo(1));}
        [Test]public void SurvivalOrderIsNotOverriddenBySmallKnockoffBonuses(){Call("Begin");Step(1,1);Step(2,2);Assert.That((int)Call("Place",0),Is.GreaterThan((int)Call("Place",1)));}
        [Test]public void SoloPracticeWaitsForItsActualLavaLoss(){Call("Reset",1,new[]{5f,5f,5f,5f,5f,5f,5f,5f});Call("Begin");Step(15);Assert.That(Read<bool>("Complete"),Is.False);Step(.02f,1);Assert.That(Read<bool>("Complete"),Is.True);}
        [Test]public void LastLivingTruckRanksAboveFinalEliminationsInSameSample(){Call("Begin");Step(.02f,254);Assert.That((int)Call("Place",0),Is.EqualTo(1));Assert.That((int)Call("Place",1),Is.EqualTo(2));}
        [Test]public void RematchRestoresFullFiniteArenaAndScores(){Call("Begin");Call("Touch",0);Step(20,254);Call("Reset",8,new[]{5f,5f,5f,5f,5f,5f,5f,5f});Assert.That(Read<int>("Collapses"),Is.Zero);Assert.That(Read<bool>("Started"),Is.False);Assert.That(Field<int[]>("scores"),Is.All.Zero);Assert.That((int)Call("Available"),Is.EqualTo(8));}
    }
}
