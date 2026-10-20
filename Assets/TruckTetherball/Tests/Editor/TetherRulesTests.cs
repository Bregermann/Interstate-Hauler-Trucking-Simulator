using System;
using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
    public sealed class TetherRulesTests
    {
        Type type;object rules;float angle;
        object Call(string method,params object[] args)=>type.GetMethod(method).Invoke(rules,args);
        T Read<T>(string field)=>(T)type.GetProperty(field).GetValue(rules);
        int[] Wins=>(int[])type.GetField("wins").GetValue(rules);
        [SetUp]public void Reset(){type=Type.GetType("LWS.TruckTetherball.TetherRules, Assembly-CSharp",true);rules=Activator.CreateInstance(type);Call("Reset",3,2);angle=0;}
        void Sample(float a,float radius=20,float dt=.02f)=>Call("Sample",a,radius,dt);
        void Begin(){Call("Begin");Sample(angle);}
        void Rotate(int degrees){int direction=Math.Sign(degrees);for(int i=0;i<Math.Abs(degrees)/10;i++){angle+=direction*10;Sample(angle%360);}}
        [Test]public void CountdownCannotBuildWrapProgress(){Sample(0);Sample(20);Assert.That(Read<float>("NetDegrees"),Is.Zero);}
        [Test]public void RepeatedStationaryBallContactsCannotScore(){Begin();for(int i=0;i<100;i++)Sample(0);Assert.That(Read<float>("NetDegrees"),Is.Zero);Assert.That(Wins,Is.All.Zero);}
        [Test]public void ActualPositiveOrbitBuildsOnlyAssignedDirection(){Begin();Rotate(360);Assert.That((float)Call("Progress",0),Is.EqualTo(.5f).Within(.001));Assert.That((float)Call("Progress",1),Is.Zero);}
        [Test]public void ActualNegativeOrbitBuildsOppositeDirection(){Begin();Rotate(-360);Assert.That((float)Call("Progress",1),Is.EqualTo(.5f).Within(.001));}
        [Test]public void ReversalUnwindsPriorProgress(){Begin();Rotate(360);Rotate(-180);Assert.That(Read<float>("NetDegrees"),Is.EqualTo(180).Within(.001));}
        [Test]public void CompletePhysicalWindingAwardsExactlyOneRound(){Begin();Rotate(720);Rotate(360);Assert.That(Read<int>("RoundWinner"),Is.Zero);Assert.That(Wins[0],Is.EqualTo(1));}
        [Test]public void CounterclockwiseBoundaryDoesNotLoseAFullTurn(){angle=350;Begin();Sample(0);Assert.That(Read<float>("NetDegrees"),Is.EqualTo(10));}
        [Test]public void ClockwiseBoundaryDoesNotInventAFullTurn(){angle=5;Begin();Sample(355);Assert.That(Read<float>("NetDegrees"),Is.EqualTo(-10));}
        [Test]public void ImpossibleTeleportDoesNotAwardWinding(){Begin();Sample(150);Assert.That(Read<float>("NetDegrees"),Is.Zero);}
        [Test]public void NearPoleNoiseCannotInventWraps(){Begin();Sample(15,1);Assert.That(Read<float>("NetDegrees"),Is.Zero);}
        [Test]public void NonFiniteSamplesCannotCorruptResults(){Begin();Sample(float.NaN);Sample(float.PositiveInfinity);Sample(10,20,float.NaN);Assert.That(Read<float>("NetDegrees"),Is.Zero);}
        [Test]public void NewRoundClearsBallWindingButKeepsRoundWins(){Begin();Rotate(720);Assert.That((bool)Call("NextRound"),Is.True);Assert.That(Read<float>("NetDegrees"),Is.Zero);Assert.That(Wins[0],Is.EqualTo(1));Assert.That(Read<bool>("Live"),Is.False);}
        [Test]public void BestOfThreeRequiresTwoGenuineRoundWins(){Begin();Rotate(720);Call("NextRound");Begin();Rotate(720);Assert.That(Read<bool>("Complete"),Is.True);Assert.That(Read<int>("Winner"),Is.Zero);Assert.That((bool)Call("NextRound"),Is.False);}
        [Test]public void MatchResetRestoresScoresAndPhysicsSamplingState(){Begin();Rotate(720);Call("Reset",5,3);Assert.That(Wins,Is.All.Zero);Assert.That(Read<int>("Round"),Is.Zero);Assert.That(Read<float>("NetDegrees"),Is.Zero);Assert.That(Read<bool>("Complete"),Is.False);}
    }
}
