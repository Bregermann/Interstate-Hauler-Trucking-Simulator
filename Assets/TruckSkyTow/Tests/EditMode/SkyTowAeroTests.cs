using NUnit.Framework;using LWS.TruckSkyTow;
public sealed class SkyTowAeroTests {
 [TestCase(10)][TestCase(18)][TestCase(22)][TestCase(26.6f)][TestCase(32)][TestCase(40)]public void OrdinaryWindEnvelopeAllowsClimbAndDescent(float speed){Assert.That(SkyTowAero.Acceleration(speed,26.6f,1,9.81f,16),Is.GreaterThan(9.81f));Assert.That(SkyTowAero.Acceleration(speed,26.6f,-1,9.81f,16),Is.LessThan(9.81f));}
 [Test]public void OldFourMetreTailwindWasUnflyable(){float cruise=26.6f,air=cruise-4,oldLift=9.81f/(cruise*cruise)*air*air*1.32f;Assert.That(oldLift,Is.LessThan(9.81f));Assert.That(SkyTowAero.Acceleration(air,cruise,1,9.81f,16),Is.GreaterThan(9.81f));}
 [Test]public void NoAirflowCannotHover(){Assert.That(SkyTowAero.Acceleration(0,26.6f,1,9.81f,16),Is.Zero);}
 [Test]public void SameBoundedInputsHaveSameForce(){float lift=SkyTowAero.Acceleration(35,26.6f,1,9.81f,16);Assert.That(lift,Is.LessThanOrEqualTo(16));Assert.That(SkyTowAero.Acceleration(35,26.6f,50,9.81f,16),Is.EqualTo(lift));Assert.That(SkyTowAero.Acceleration(35,26.6f,-50,9.81f,16),Is.EqualTo(SkyTowAero.Acceleration(35,26.6f,-1,9.81f,16)));}
}
