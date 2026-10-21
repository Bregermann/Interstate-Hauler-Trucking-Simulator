using System;
using NUnit.Framework;
public sealed class NinjaAthleteTests
{
 static float Delta(float current,float height)=>(float)Type.GetType("LWS.TruckNinjaWarrior.NinjaAthlete, Assembly-CSharp",true).GetMethod("TakeoffDelta").Invoke(null,new object[]{current,height,-9.81f});
 [Test]public void DescendingPlatformContactStillProducesTheConfiguredTakeoffSpeed(){float expected=(float)Math.Sqrt(2*9.81*4.2);Assert.AreEqual(expected,-9+Delta(-9,4.2f),.001f);}
 [Test]public void ExistingFasterUpwardMomentumIsPreserved(){Assert.Zero(Delta(16,7.5f));}
 [Test]public void ExtremeLandingVelocityCannotProduceAnUnboundedImpulse(){Assert.AreEqual(24,Delta(-100,7.5f));}
}
