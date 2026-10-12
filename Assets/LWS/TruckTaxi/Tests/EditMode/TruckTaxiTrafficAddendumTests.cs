using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [TestFixture, Category("TaxiTraffic")]
    public sealed class TruckTaxiTrafficAddendumTests
    {
        [Test] public void RecoveryBudgetStopsAfterThreeAttemptsAndPoolResetRearmsIt()
        {
            var root=new GameObject("bounded recovery fixture");
            try
            {
                var driver=root.AddComponent<TruckTaxiTrafficBehaviour>();
                var attempt=typeof(TruckTaxiTrafficBehaviour).GetMethod("TryCountRecoveryAttempt",
                    System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                for(int i=0;i<3;i++) Assert.IsTrue((bool)attempt.Invoke(driver,null));
                for(int i=0;i<20;i++) Assert.IsFalse((bool)attempt.Invoke(driver,null));
                Assert.AreEqual(3,driver.RecoveryAttempts);
                driver.ResetForPool(); Assert.AreEqual(0,driver.RecoveryAttempts);
                Assert.IsTrue((bool)attempt.Invoke(driver,null));
                var adapter=root.AddComponent<TruckTaxiTrafficAdapter>();
                Assert.IsFalse(adapter.TryRecycleStuckTraffic(driver),"Unregistered actors must never be retired.");
                Assert.IsFalse(adapter.TryReacquireTrafficRoute(driver),"Missing route must fail without moving the actor.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void OrdinaryAndRivalPersonalityWeightsMatchConfiguredDefaults()
        {
            var tuning = new TruckTaxiTrafficTuning();
            int[] ordinary = new int[5], rival = new int[5];
            for (uint roll = 0; roll < 100; roll++)
            {
                ordinary[(int)tuning.Choose(roll, false)]++;
                rival[(int)tuning.Choose(roll, true)]++;
            }
            CollectionAssert.AreEqual(new[] { 15, 65, 14, 5, 1 }, ordinary);
            CollectionAssert.AreEqual(new[] { 0, 35, 40, 20, 5 }, rival);
        }

        [Test]
        public void LaneChangeCooldownIsBoundedAndCautiousDriversWaitLonger()
        {
            var tuning = new TruckTaxiTrafficTuning();
            float normal = tuning.LaneChangeCooldown(TruckTaxiDriverPersonality.Normal, 50);
            Assert.GreaterOrEqual(normal, 6);
            Assert.LessOrEqual(normal, 12);
            Assert.Greater(tuning.LaneChangeCooldown(TruckTaxiDriverPersonality.Cautious, 50), normal);
            Assert.Less(tuning.LaneChangeCooldown(TruckTaxiDriverPersonality.Aggressive, 50), normal);
        }

        [Test]
        public void WeatherCautionIsClampedAtAdapterBoundary()
        {
            var root = new GameObject("weather traffic fixture");
            try
            {
                var adapter = root.AddComponent<TruckTaxiTrafficAdapter>();
                adapter.SetWeatherCaution(2);
                Assert.AreEqual(1, adapter.WeatherCaution);
                adapter.SetWeatherCaution(-1);
                Assert.AreEqual(0, adapter.WeatherCaution);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void HornPoolResetClearsClipLoopAndPitch()
        {
            var root = new GameObject("horn fixture");
            var clip = AudioClip.Create("horn fixture clip", 441, 1, 44100, false);
            try
            {
                var horn = root.AddComponent<TruckTaxiTrafficHorn>();
                horn.Configure(clip, null, null);
                var source = root.GetComponentInChildren<AudioSource>();
                Assert.IsNotNull(source);
                Assert.AreEqual(1, source.spatialBlend);
                source.clip = clip;
                source.loop = true;
                source.pitch = .7f;
                horn.ResetForPool();
                Assert.IsNull(source.clip);
                Assert.IsFalse(source.loop);
                Assert.AreEqual(1, source.pitch);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clip);
            }
        }
    }
}
