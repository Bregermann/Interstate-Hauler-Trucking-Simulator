using System;
using NUnit.Framework;
using UnityEngine;
public sealed class NinjaProgressTests
{
 Type type;object watch;
 [SetUp]public void Setup(){type=Type.GetType("LWS.TruckNinjaWarrior.NinjaProgressWatch, Assembly-CSharp",true);watch=Activator.CreateInstance(type);Reset(Vector3.zero);}
 void Reset(Vector3 p)=>type.GetMethod("Reset").Invoke(watch,new object[]{p});
 void Step(Vector3 p,float dt)=>type.GetMethod("Step").Invoke(watch,new object[]{p,dt});
 float Seconds=>(float)type.GetProperty("Seconds").GetValue(watch);
 [Test]public void SuspensionJitterCannotPostponeRecoveryForever(){Assert.Greater(.08f/.1f,.4f,"The old instantaneous-speed gate resets for this jitter despite no useful progress.");for(int i=0;i<65;i++)Step(new Vector3(i%2==0?.04f:-.04f,.04f,0),.1f);Assert.Greater(Seconds,6);}
 [Test]public void ActualDrivingStartsAFreshRecoveryWindow(){for(int i=0;i<50;i++)Step(Vector3.zero,.1f);Step(new Vector3(2.1f,0,0),.1f);Assert.Zero(Seconds);for(int i=0;i<20;i++)Step(new Vector3(2.1f,0,0),.1f);Assert.AreEqual(2,Seconds,.001f);}
 [Test]public void ARealVerticalJumpCountsAsMovement(){for(int i=0;i<30;i++)Step(Vector3.zero,.1f);Step(Vector3.up*3,.02f);Assert.Zero(Seconds,"A deliberate vertical jump must not be confused with chassis vibration.");}
 [Test]public void CheckpointRecoveryClearsTheOldStall(){for(int i=0;i<70;i++)Step(Vector3.zero,.1f);Reset(new Vector3(0,12.65f,120));Assert.Zero(Seconds);Step(new Vector3(0,12.65f,120),.02f);Assert.AreEqual(.02f,Seconds,.001f);}
}
