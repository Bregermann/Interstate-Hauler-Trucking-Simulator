using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests.EditMode
{
    public sealed class TruckTaxiWorldLifeDamageTests
    {
        [TestCase(0.04 * TruckTaxiUsUnits.MetersPerMile, "211 ft")]
        [TestCase(0.09 * TruckTaxiUsUnits.MetersPerMile, "475 ft")]
        [TestCase(0.10 * TruckTaxiUsUnits.MetersPerMile, "0.1 mi")]
        [TestCase(1.26 * TruckTaxiUsUnits.MetersPerMile, "1.3 mi")]
        public void DistancePresentationUsesFeetUntilOneTenthMile(double meters, string expected) =>
            Assert.AreEqual(expected, TruckTaxiUsUnits.Distance(meters));

        [Test]
        public void FuelAndSpeedRemainPresentationConversions()
        {
            Assert.AreEqual(60f, TruckTaxiUsUnits.Mph(26.8224f), .01f);
            Assert.AreEqual("10.0 gal", TruckTaxiUsUnits.Fuel((float)(10 * TruckTaxiUsUnits.LitersPerUsGallon)));
            Assert.AreEqual(568, TruckTaxiUsUnits.CentsPerGallon(150));
        }

        [Test]
        public void LegacyNeedsSnapshotRestoresWithHealthyBowelAndNoToilets()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 11);
                var old = state.CapturePersistentState();
                old.UsedToiletIds = null; old.BowelPressure = 0;
                Assert.IsTrue(new TruckTaxiDriverNeedsState(settings, 12).RestorePersistentState(old, out string error), error);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void ToiletDiscoveryAndBowelNeedRoundTripThroughExistingNeedsSnapshot()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 15);
                int notifications = 0;
                state.ToiletCollectionChanged += _ => notifications++;
                state.AdvanceGameSeconds(3600);
                Assert.Greater(state.BowelPressure, 0);
                Assert.IsTrue(state.RecordToiletUse("truck-stop.west.porta"));
                Assert.IsFalse(state.RecordToiletUse("truck-stop.west.porta"));
                Assert.AreEqual(1, notifications);
                var restored = new TruckTaxiDriverNeedsState(settings, 16);
                Assert.IsTrue(restored.RestorePersistentState(state.CapturePersistentState(), out string error), error);
                Assert.AreEqual(state.BowelPressure, restored.BowelPressure, .0001f);
                Assert.IsTrue(restored.HasUsedToilet("truck-stop.west.porta"));
                Assert.AreEqual(1, restored.ToiletsDiscovered);
                restored.RelieveBowel();
                Assert.AreEqual(0, restored.BowelPressure);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void DuplicateToiletIdsFailWithoutPartialRestore()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 17);
                var snapshot = state.CapturePersistentState();
                snapshot.UsedToiletIds = new[] { "same.toilet", "same.toilet" };
                Assert.IsFalse(state.RestorePersistentState(snapshot, out _));
                Assert.AreEqual(0, state.ToiletsDiscovered);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void MotelAccessAndAmbientChoicesRoundTripWithoutChangingOldSaves()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 18);
                Assert.IsTrue(state.RentMotel("motel.validation", 1000, 12 * 3600));
                Assert.IsTrue(state.RecordAmbientChoice("motel.validation.ambient.Panhandler.gave"));
                Assert.IsFalse(state.RecordAmbientChoice("motel.validation.ambient.Panhandler.gave"));
                var restored = new TruckTaxiDriverNeedsState(settings, 19);
                Assert.IsTrue(restored.RestorePersistentState(state.CapturePersistentState(), out string error), error);
                Assert.IsTrue(restored.HasMotelAccess("motel.validation", 2000));
                Assert.IsFalse(restored.HasMotelAccess("motel.validation", 1000 + 12 * 3600));
                Assert.AreEqual(1, restored.AmbientChoiceIds.Count);
                var old = state.CapturePersistentState();
                old.RentedMotelId = null; old.MotelAccessExpiresAtGameSeconds = 0; old.AmbientChoiceIds = null;
                Assert.IsTrue(restored.RestorePersistentState(old, out error), error);
                Assert.IsNull(restored.RentedMotelId);
                Assert.AreEqual(0, restored.AmbientChoiceIds.Count);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void MotelSleepRecoversMoreThanCabNapThroughExistingFatigueState()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var cab = new TruckTaxiDriverNeedsState(settings, 20);
                var motel = new TruckTaxiDriverNeedsState(settings, 21);
                var starting = cab.CapturePersistentState(); starting.Fatigue = .95f;
                Assert.IsTrue(cab.RestorePersistentState(starting, out string error), error);
                Assert.IsTrue(motel.RestorePersistentState(starting, out error), error);
                Assert.IsTrue(cab.ApplySleep(4, .8f));
                Assert.IsTrue(motel.ApplySleep(4, 1.1f));
                Assert.Less(motel.Fatigue, cab.Fatigue);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void DamageSaveKeysAreVehicleAndStableComponentIds()
        {
            var available = new Dictionary<string, TruckTaxiDamagePart>
            {
                ["engine"] = new TruckTaxiDamagePart("engine", "Engine", TruckTaxiDamageCategory.Engine, 350000),
                ["tire.axle1.left.1"] = new TruckTaxiDamagePart("tire.axle1.left.1", "Front Left Tire", TruckTaxiDamageCategory.Tire, 65000)
            };
            var saved = new TruckTaxiDamageSnapshot { VehicleId = "truck.stable", Parts = new[]
            { new TruckTaxiDamageEntry { Id = "tire.axle1.left.1", Condition = .2f } } };
            Assert.IsTrue(TruckTaxiComponentDamage.ValidSnapshot(saved, "truck.stable", available));
            Assert.IsFalse(TruckTaxiComponentDamage.ValidSnapshot(saved, "another.truck", available));
            saved.Parts[0].Id = "tire.axle1.right.1";
            Assert.IsFalse(TruckTaxiComponentDamage.ValidSnapshot(saved, "truck.stable", available));
            Assert.IsTrue(TruckTaxiComponentDamage.ValidSnapshot(null, "truck.stable", available));
        }

    }
}
