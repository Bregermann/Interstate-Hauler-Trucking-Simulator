using System;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiDamageEnvironmentPersistenceTests
    {
        [Test]
        public void ComponentConditionsHeatAndSummaryRoundTripWithoutCosmeticAuthority()
        {
            var go = new GameObject("damage fixture");
            go.SetActive(false);
            try
            {
                var host = go.AddComponent<TruckTaxiBootstrap>();
                var truck = go.AddComponent<LwsPlayerTruck>();
                var identity = go.GetComponent<LwsVehicleIdentity>();
                identity.Configure("fixture.truck", "fixture.definition", LwsVehicleRole.PlayerTractor, "Fixture", true);
                typeof(LwsPlayerTruck).GetField("identity", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(truck, identity);
                typeof(TruckTaxiBootstrap).GetProperty("Player").SetValue(host, truck);
                var damage = go.AddComponent<TruckTaxiComponentDamage>();
                typeof(TruckTaxiComponentDamage).GetField("host", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(damage, host);
                var add = typeof(TruckTaxiComponentDamage).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
                add.Invoke(damage, new object[] { "engine", "Engine", TruckTaxiDamageCategory.Engine, 10000L });
                add.Invoke(damage, new object[] { "steering", "Steering", TruckTaxiDamageCategory.Steering, 10000L });
                Assert.IsTrue(damage.Damage("engine", .4f));
                Assert.IsTrue(damage.Damage("steering", 1));
                var saved = damage.Capture(); saved.Heat01 = .6f;
                float expected = damage.SummaryDamage01;
                damage.RepairAllAfterAssistance();
                Assert.Zero(damage.SummaryDamage01);
                Assert.IsTrue(damage.Restore(JsonUtility.FromJson<TruckTaxiDamageSnapshot>(JsonUtility.ToJson(saved))));
                Assert.AreEqual(expected, damage.SummaryDamage01, .0001f);
                Assert.AreEqual(.6f, damage.Heat01, .0001f);
                Assert.AreEqual(.6f, damage.Parts["engine"].Condition, .0001f);
                Assert.AreEqual("CRITICAL", damage.Parts["steering"].State);
                var wrongOwner = damage.Capture(); wrongOwner.VehicleId = "other.truck";
                Assert.IsFalse(damage.Restore(wrongOwner));
                saved.Parts[0].Condition = 1;
                Assert.AreEqual(.6f, damage.Parts["engine"].Condition, .0001f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [TestCase(float.NaN)] [TestCase(-.1f)] [TestCase(1.1f)]
        public void InvalidDamageHeatRejected(float heat)
        {
            Assert.IsFalse(TruckTaxiComponentDamage.ValidSnapshotData(new TruckTaxiDamageSnapshot
                { Heat01 = heat, Parts = Array.Empty<TruckTaxiDamageEntry>() }));
        }
        [Test]
        public void DuplicateDamageRejectedAndLegacyHeatDefaultsToZero()
        {
            var value = JsonUtility.FromJson<TruckTaxiDamageSnapshot>("{\"Version\":1,\"VehicleId\":\"truck\",\"Parts\":[{\"Id\":\"engine\",\"Condition\":0.5}]}");
            Assert.IsTrue(TruckTaxiComponentDamage.ValidSnapshotData(value));
            Assert.Zero(value.Heat01);
            value.Parts = new[] { value.Parts[0], value.Parts[0] };
            Assert.IsFalse(TruckTaxiComponentDamage.ValidSnapshotData(value));
            Assert.IsTrue(TruckTaxiComponentDamage.ValidSnapshotData(null));
        }
        [Test]
        public void LowSpeedLockSharperButHighwayCurveAndSmoothingUnchanged()
        {
            var oldCurve = TruckTaxiVehicleHandlingOverride.CreateSteeringCurve(65, 40, 8);
            var newCurve = TruckTaxiVehicleHandlingOverride.CreateSteeringCurve(70, 40, 8);
            Assert.Greater(newCurve.Evaluate(0) * 70, oldCurve.Evaluate(0) * 65);
            for (float mph = 35; mph <= 110; mph += 5)
            {
                Assert.AreEqual(oldCurve.Evaluate(mph / 111.8468f) * 65, newCurve.Evaluate(mph / 111.8468f) * 70, .001f);
                Assert.AreEqual(Mathf.Lerp(.045f, .15f, mph / 111.8468f),
                    TruckTaxiVehicleHandlingOverride.CreateSmoothingCurve(.025f).Evaluate(mph / 111.8468f), .0001f);
            }
            Assert.AreEqual(.92f, TruckTaxiComponentDamage.GripForCondition(0), .001f);
            Assert.AreEqual(1, TruckTaxiComponentDamage.GripForCondition(1), .001f);
            Assert.AreEqual(400, TruckTaxiVehicleHandlingOverride.ResponseForSpeed(0,400));
            Assert.AreEqual(300, TruckTaxiVehicleHandlingOverride.ResponseForSpeed(70,400));
        }

        private static TruckTaxiSnowRegion Snow(float length = 12, float offset = 0)
        {
            var region = new TruckTaxiSnowRegion(.6f);
            region.AddLane(new LwsTrafficLaneDefinition { laneId = "test", laneWidthMeters = 5,
                centerline = new[] { new Vector3(offset,0,0), new Vector3(offset,0,length) } }, 32);
            return region;
        }
        [Test]
        public void SnowPreservesPlowedDepthAndCoverageAcrossTranslatedLayout()
        {
            var source = Snow(); source.Accumulate(.5f);
            source.ClearSweep(new Vector3(0,0,1), new Vector3(0,0,5), 5, .04f);
            var saved = JsonUtility.FromJson<TruckTaxiSnowSnapshot>(JsonUtility.ToJson(source.Capture(.5f)));
            var restored = Snow(12, 100);
            Assert.IsTrue(restored.Restore(saved));
            Assert.AreEqual(.04f, restored.DepthAt(new Vector3(100,0,2)), .001f);
            Assert.AreEqual(.5f, restored.DepthAt(new Vector3(100,0,11)), .001f);
            Assert.AreEqual(.5f, saved.backgroundDepth);
            saved.depths[0] = 0;
            Assert.AreEqual(.04f, restored.Cells[0].depth, .001f);
        }
        [Test]
        public void InvalidSnowDoesNotPartiallyApply()
        {
            var region = Snow(); region.Accumulate(.2f);
            var saved = region.Capture(.2f);
            saved.depths[saved.depths.Length - 1] = float.NaN;
            Assert.IsFalse(region.Restore(saved));
            Assert.AreEqual(.2f, region.Cells[0].depth);
            Assert.IsFalse(Snow(15).CanRestore(region.Capture(.2f)));
        }
        [TestCase(true)] [TestCase(false)]
        public void ForecastRestoresOverrideAfterClockRewind(bool automatic)
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            var clock = new LwsGameClockService(); clock.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            clock.SetDateTime(new LwsGameDateTime(2026,1,5,12,0,0));
            var calendar = new TruckTaxiCalendarWeatherService();
            string applied = null;
            try
            {
                calendar.Initialize(clock, settings, id => { applied = id; return true; });
                calendar.SetAutomaticWeather(automatic);
                calendar.ForceCurrent(TruckTaxiWeatherCondition.Blizzard, 3);
                var saved = JsonUtility.FromJson<TruckTaxiForecastSnapshot>(JsonUtility.ToJson(calendar.Capture()));
                clock.AddHours(8); clock.AddHours(-7);
                Assert.IsTrue(calendar.Restore(saved));
                Assert.AreEqual(automatic, calendar.AutomaticWeather);
                Assert.AreEqual(TruckTaxiSnow.BlizzardId, applied);
                Assert.AreEqual(TruckTaxiWeatherCondition.Blizzard, calendar.CurrentCondition);
                Assert.AreEqual(new DateTime(2026,1,5,15,0,0).Ticks, calendar.Capture().overrideEndTicks);
            }
            finally { calendar.Dispose(); Object.DestroyImmediate(settings); }
        }
        [Test]
        public void EnvironmentAllowsLegacyButRejectsMalformedExtensions()
        {
            Assert.IsTrue(TruckTaxiEnvironmentSnapshot.Valid(null));
            Assert.IsFalse(TruckTaxiEnvironmentSnapshot.Valid(new TruckTaxiEnvironmentSnapshot()));
            var value = new TruckTaxiEnvironmentSnapshot { forecast = new TruckTaxiForecastSnapshot() };
            Assert.IsTrue(TruckTaxiEnvironmentSnapshot.Valid(value));
            value.forecast.manualCondition = (TruckTaxiWeatherCondition)999;
            Assert.IsFalse(TruckTaxiEnvironmentSnapshot.Valid(value));
            value.forecast = new TruckTaxiForecastSnapshot { hasOverride = true, overrideStartTicks = 20, overrideEndTicks = 10 };
            Assert.IsFalse(TruckTaxiEnvironmentSnapshot.Valid(value));
        }
        [Test]
        public void InlineNullExtensionsReadAsLegacyButDeclaredOrPartialRecordsStayInvalid()
        {
            var legacy = new TruckTaxiVibinSavePayload();
            LwsSaveParticipantState State(TruckTaxiVibinSavePayload value) => new LwsSaveParticipantState
                { participantId = TruckTaxiVibinSaveParticipant.Id, payloadVersion = 1, payloadJson = JsonUtility.ToJson(value) };
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.Read(State(legacy), out var read).Succeeded);
            Assert.IsNull(read.environment); Assert.IsNull(read.extremes); Assert.IsNull(read.damage);
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.Read(new LwsSaveParticipantState
                { participantId = TruckTaxiVibinSaveParticipant.Id, payloadVersion = 1, payloadJson = "{\"version\":1}" }, out read).Succeeded);
            Assert.IsNull(read.environment); Assert.IsNull(read.extremes); Assert.IsNull(read.damage);
            legacy.environment = new TruckTaxiEnvironmentSnapshot { version = 999 };
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.Read(State(legacy), out read).Succeeded);
            Assert.IsNotNull(read.environment);
            Assert.IsFalse(TruckTaxiEnvironmentSnapshot.Valid(read.environment));
            legacy.environment = null; legacy.hasEnvironmentState = true;
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.Read(State(legacy), out read).Succeeded);
            Assert.IsTrue(read.hasEnvironmentState);
            Assert.IsNull(read.environment);
        }
        [TestCase("damage")] [TestCase("environment")] [TestCase("extremes")]
        public void DeclaredInlineNullIsRejectedByPayloadValidation(string extension)
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var value = new TruckTaxiVibinSavePayload { regionId = "fixture.region",
                    hasDamageState = extension == "damage", hasEnvironmentState = extension == "environment",
                    hasExtremeState = extension == "extremes" };
                Assert.IsTrue(TruckTaxiVibinSaveParticipant.Read(new LwsSaveParticipantState
                    { participantId = TruckTaxiVibinSaveParticipant.Id, payloadVersion = 1,
                        payloadJson = JsonUtility.ToJson(value) }, out var read).Succeeded);
                var result = TruckTaxiVibinSaveParticipant.ValidatePayload(read, settings);
                Assert.IsFalse(result.Succeeded);
                Assert.AreEqual("A declared save extension is missing.", result.Message);
            }
            finally { Object.DestroyImmediate(settings); }
        }
        [Test]
        public void PolicyRestoreDoesNotOverwriteCanonicalVendorWeather()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            var clock = new LwsGameClockService(); clock.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            clock.SetDateTime(new LwsGameDateTime(2026,1,5,12,0,0));
            var calendar = new TruckTaxiCalendarWeatherService(); int requests = 0;
            try
            {
                calendar.Initialize(clock, settings, _ => { requests++; return true; });
                calendar.ForceCurrent(TruckTaxiWeatherCondition.HeavyRain, 3);
                var saved = calendar.Capture(); int before = requests;
                Assert.IsTrue(calendar.Restore(saved, false));
                Assert.AreEqual(before, requests);
                clock.AddHours(.1f);
                Assert.AreEqual(before, requests);
            }
            finally { calendar.Dispose(); Object.DestroyImmediate(settings); }
        }
        [Test]
        public void SavedHazardRebasesGlobalPathInsteadOfFollowingTruck()
        {
            var data = new TruckTaxiSavedExtreme { id = "dynamic:tornado:1", regionId = "town", kind = TruckTaxiExtremeKind.Tornado,
                procedural = true, radius = 125, started = 10, movingEnds = 20, ends = 30,
                path = new[] { new TruckTaxiHazardPoint { x = 1000, y = 10, z = 2000 }, new TruckTaxiHazardPoint { x = 1200, y = 10, z = 2100 } } };
            var copy = JsonUtility.FromJson<TruckTaxiSavedExtreme>(JsonUtility.ToJson(data));
            var footprint = TruckTaxiExtremeFootprint.FromSaved(copy, null, p => new Vector3((float)p.x - 900, (float)p.y, (float)p.z - 1800));
            Assert.AreEqual(new Vector3(200,10,250), footprint.PositionAt(.5f));
            Assert.AreEqual("dynamic:tornado:1", footprint.Id);
            data.path[0].x = 20000;
            Assert.AreEqual(new Vector3(100,10,200), footprint.Source);
            copy.path[0].x = double.NaN;
            Assert.IsFalse(TruckTaxiSavedExtreme.Valid(copy));
        }
        [Test]
        public void NaturalEventDryDaysAndCooldownRoundTrip()
        {
            var schedule = new TruckTaxiExtremeDaySchedule(new TruckTaxiExtremeScheduleSettings());
            var day = new DateTime(2026,1,5);
            schedule.TryDay(TruckTaxiExtremeKind.Tornado, day, true, 1, out _);
            var copy = new TruckTaxiExtremeDaySchedule(new TruckTaxiExtremeScheduleSettings());
            Assert.IsTrue(copy.Restore(JsonUtility.FromJson<TruckTaxiExtremeDayState>(JsonUtility.ToJson(schedule.Capture()))));
            Assert.AreEqual(schedule.DryDays(TruckTaxiExtremeKind.Tornado), copy.DryDays(TruckTaxiExtremeKind.Tornado));
            Assert.IsFalse(copy.TryDay(TruckTaxiExtremeKind.Tornado, day, true, 0, out var reason));
            Assert.AreEqual("ALREADY_ATTEMPTED", reason);
        }
    }
}
