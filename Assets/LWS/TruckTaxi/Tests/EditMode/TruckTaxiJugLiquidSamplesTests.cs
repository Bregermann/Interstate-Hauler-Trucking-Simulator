using NUnit.Framework;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiJugLiquidSamplesTests
    {
        [Test] public void GeneratedLiquidLoopIsDeterministicAudibleAndBounded()
        {
            var first = TruckTaxiJugLiquidLoop.BuildSamples(22050, 3);
            var second = TruckTaxiJugLiquidLoop.BuildSamples(22050, 3);
            Assert.AreEqual(66150, first.Length);
            Assert.AreEqual(0, first[0]);
            Assert.AreEqual(0, first[first.Length - 1]);
            double energy = 0;
            for (int i = 0; i < first.Length; i++)
            {
                Assert.IsTrue(float.IsFinite(first[i]));
                Assert.That(first[i], Is.InRange(-.7f, .7f));
                Assert.AreEqual(first[i], second[i]);
                energy += first[i] * first[i];
            }
            Assert.Greater(energy / first.Length, .00001d, "Fallback must not be silent.");
        }
    }
}
