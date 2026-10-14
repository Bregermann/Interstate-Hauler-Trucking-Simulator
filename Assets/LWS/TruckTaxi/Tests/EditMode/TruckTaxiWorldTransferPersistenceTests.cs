using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiWorldTransferPersistenceTests
    {
        private LwsServiceRegistry registry;
        private LwsWeatherCoordinator weather;
        private LwsRoadGraphService graph;
        private LwsRoadConditionCoordinator roads;
        private TruckTaxiEnvironmentSettings settings;

        [TestCase(-10f, false, .15f)]
        [TestCase(0f, false, 1f)]
        [TestCase(15f, false, 2f)]
        [TestCase(15f, true, 3f)]
        [TestCase(30f, true, 4.5f)]
        public void WarmRainAcceleratesGradualMelt(float temperature, bool rain, float expected) =>
            Assert.AreEqual(expected, TruckTaxiSnow.MeltRateMultiplier(temperature, rain), .0001f);

        [SetUp]
        public void SetUp()
        {
            Assert.IsFalse(Application.isPlaying);
            registry = new LwsServiceRegistry();
            weather = new LwsWeatherCoordinator();
            graph = new LwsRoadGraphService();
            roads = new LwsRoadConditionCoordinator();
            registry.Register<ILwsWeatherService>(weather);
            registry.Register<ILwsRoadGraphService>(graph);
            registry.Register<ILwsRoadConditionService>(roads, typeof(ILwsWeatherService), typeof(ILwsRoadGraphService));
            var initialized = registry.InitializeAll();
            Assert.IsTrue(initialized.Succeeded, initialized.Message);
            var validation = graph.SetActiveGraph(CreateGraph());
            Assert.IsTrue(validation.IsValid, validation.Summary);
            settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            settings.lightSnowMetersPerSecond = .01f;
            settings.heavySnowMetersPerSecond = .02f;
            settings.blizzardMetersPerSecond = .03f;
            settings.snowMeltMetersPerSecond = .005f;
        }

        [TearDown]
        public void TearDown()
        {
            try { registry?.ShutdownAll(); }
            finally { if (settings != null) Object.DestroyImmediate(settings); }
        }

        [Test]
        public void BlizzardRegistrationIsResolvableIdempotentAndKeepsTheCanonicalCoordinator()
        {
            int before = weather.Presets.Count;
            Assert.IsTrue(weather.TryGetPreset(LwsWeatherPresetCatalog.HeavySnowId, out var heavySnow));
            LwsWeatherPreset blizzard = TruckTaxiEnvironmentCoordinator.CreateBlizzardPreset();
            for (int i = 0; i < 5; i++) weather.RegisterPreset(blizzard);
            Assert.AreEqual(before + 1, weather.Presets.Count);
            Assert.IsTrue(registry.TryGet<ILwsWeatherService>(out var canonical));
            Assert.AreSame(weather, canonical);
            Assert.IsTrue(canonical.TryGetPreset(TruckTaxiSnow.BlizzardId, out var resolved));
            Assert.AreEqual(TruckTaxiSnow.BlizzardId, resolved.presetId);
            Assert.AreEqual(LwsPrecipitationType.Snow, resolved.precipitationType);
            Assert.AreEqual(1f, resolved.precipitationIntensity01);
            Assert.AreEqual(400f, resolved.visibilityMeters);
            Assert.AreEqual(18f, resolved.windSpeedMetersPerSecond);
            Assert.AreEqual(heavySnow.weatherMakerProfileName, resolved.weatherMakerProfileName);
            Assert.AreEqual(heavySnow.weatherMakerProfilePath, resolved.weatherMakerProfilePath);
            Assert.IsTrue(weather.TryGetPreset(LwsWeatherPresetCatalog.HeavySnowId, out var unchanged));
            Assert.AreEqual(heavySnow.presetId, unchanged.presetId);
            Assert.AreEqual(heavySnow.visibilityMeters, unchanged.visibilityMeters);
            Assert.AreEqual(heavySnow.precipitationIntensity01, unchanged.precipitationIntensity01);
        }

        [Test]
        public void ReRegistrationUpdatesOneCaseInsensitiveCatalogEntry()
        {
            int before = weather.Presets.Count;
            LwsWeatherPreset preset = TruckTaxiEnvironmentCoordinator.CreateBlizzardPreset();
            weather.RegisterPreset(preset);
            preset.presetId = preset.presetId.ToUpperInvariant();
            preset.visibilityMeters = 650;
            preset.windSpeedMetersPerSecond = 14;
            weather.RegisterPreset(preset);
            Assert.AreEqual(before + 1, weather.Presets.Count);
            Assert.IsTrue(weather.TryGetPreset(TruckTaxiSnow.BlizzardId, out var resolved));
            Assert.AreEqual(650f, resolved.visibilityMeters);
            Assert.AreEqual(14f, resolved.windSpeedMetersPerSecond);
            int matches = 0;
            foreach (LwsWeatherPreset entry in weather.Presets)
                if (string.Equals(entry.presetId, TruckTaxiSnow.BlizzardId, StringComparison.OrdinalIgnoreCase)) matches++;
            Assert.AreEqual(1, matches);
        }

        [Test]
        public void RegisteringBlizzardMakesTheSameCanonicalSavedWeatherRecordRestorable()
        {
            Assert.IsFalse(weather.TryGetPreset(TruckTaxiSnow.BlizzardId, out _));
            LwsWeatherPreset blizzard = TruckTaxiEnvironmentCoordinator.CreateBlizzardPreset();
            Assert.IsTrue(weather.RequestWeather(blizzard, 0, true).Succeeded);
            var participant = new LwsWeatherSaveParticipant(() => registry);
            LwsSaveParticipantState saved = participant.CaptureState();
            Assert.IsTrue(weather.RequestWeather(LwsWeatherPresetCatalog.ClearId, 0, true).Succeeded);
            Assert.IsFalse(participant.RestoreState(saved).Succeeded, "The control demonstrates the missing catalog mapping.");
            weather.RegisterPreset(blizzard);
            weather.RegisterPreset(blizzard);
            var restored = participant.RestoreState(saved);
            Assert.IsTrue(restored.Succeeded, restored.Message);
            Assert.AreEqual(TruckTaxiSnow.BlizzardId, weather.CurrentSnapshot.weatherPresetId);
            Assert.AreEqual(LwsPrecipitationType.Snow, weather.CurrentSnapshot.precipitationType);
            Assert.AreEqual(1f, weather.CurrentSnapshot.precipitationIntensity01);
            Assert.AreEqual(400f, weather.CurrentSnapshot.visibilityMeters);
            Assert.IsFalse(weather.CurrentSnapshot.transitioning);
        }

        [Test]
        public void WorldAccumulationReplacesOldRoadCacheAndKeepsLiveTargetIdentity()
        {
            SeedPreviousWorldAndSelectTarget();
            LwsRoadConditionSnapshot saved = SavedMixedAccumulation();
            roads.RestoreWorldAccumulation(saved);
            LwsRoadConditionSnapshot restored = roads.CurrentSnapshot;
            AssertTargetIdentity(restored);
            Assert.AreEqual(saved.wetness01, restored.wetness01);
            Assert.AreEqual(saved.standingWater01, restored.standingWater01);
            Assert.AreEqual(saved.snowDepth01, restored.snowDepth01);
            Assert.AreEqual(saved.packedSnow01, restored.packedSnow01);
            Assert.AreEqual(saved.ice01, restored.ice01);
            Assert.AreEqual(saved.surfaceTemperatureC, restored.surfaceTemperatureC);
            Assert.AreEqual(saved.lastWeatherPresetId, restored.lastWeatherPresetId);
            Assert.AreEqual(0f, restored.vehicleSpeedMetersPerSecond);
            Assert.IsFalse(restored.forced);
            Assert.IsFalse(roads.TryGetRoadCondition("edge.0", out _));
            Assert.IsFalse(roads.TryGetRoadCondition("road.0", out _));
            Assert.IsFalse(roads.TryGetRoadCondition("edge.1", out _));
            Assert.IsFalse(roads.TryGetRoadCondition("offroad.default", out _));
            Assert.IsFalse(roads.TryGetRoadCondition(saved.edgeId, out _));
            Assert.IsTrue(roads.TryGetRoadCondition("edge.2", out var current));
            Assert.AreEqual(restored.snowDepth01, current.snowDepth01);
            Assert.AreEqual("saved.source.edge", saved.edgeId, "The caller's saved value is not rewritten.");
            SelectRoad(0);
            Assert.AreEqual(0f, roads.CurrentSnapshot.snowDepth01, "Revisiting an old key must not resurrect its previous world's cache.");
            Assert.AreEqual(0f, roads.CurrentSnapshot.ice01);
        }

        [Test]
        public void RestoredWorldGripIsRecomputedFromConditionAndSentToPhysicsAdapter()
        {
            SelectRoad(2);
            var adapter = new RecordingPhysicsAdapter();
            Assert.IsTrue(roads.AttachPhysicsAdapter(adapter));
            int before = adapter.Calls;
            LwsRoadConditionSnapshot saved = SavedMixedAccumulation();
            saved.longitudinalGripMultiplier01 = saved.lateralGripMultiplier01 = saved.brakingGripMultiplier01 = 1.25f;
            saved.rollingResistanceMultiplier = 3;
            roads.RestoreWorldAccumulation(saved);
            LwsRoadConditionSnapshot restored = roads.CurrentSnapshot;
            LwsRoadConditionGripProfile grip = roads.Profile.GetGripProfile(LwsRoadConditionType.Mixed);
            Assert.AreEqual(LwsRoadConditionType.Mixed, restored.condition);
            Assert.AreEqual(grip.longitudinalGripMultiplier01 * .82f, restored.longitudinalGripMultiplier01, .00001f);
            Assert.AreEqual(grip.lateralGripMultiplier01 * .82f, restored.lateralGripMultiplier01, .00001f);
            Assert.AreEqual(grip.brakingGripMultiplier01 * .82f, restored.brakingGripMultiplier01, .00001f);
            Assert.AreEqual(grip.rollingResistanceMultiplier, restored.rollingResistanceMultiplier);
            Assert.AreEqual(before + 1, adapter.Calls);
            AssertTargetIdentity(adapter.Last);
            Assert.AreEqual(restored.longitudinalGripMultiplier01, adapter.Last.longitudinalGripMultiplier01);
            Assert.AreEqual(restored.snowDepth01, adapter.Last.snowDepth01);
        }

        [Test]
        public void FreshRegionDryRestoreRemovesPriorSnowIceAndGripLoss()
        {
            SeedPreviousWorldAndSelectTarget();
            roads.RestoreWorldAccumulation(SavedMixedAccumulation());
            LwsRoadConditionSnapshot fresh = LwsRoadConditionSnapshot.CreateDry(
                "unused.source.road", "unused.source.segment", "unused.source.edge",
                LwsRoadSurfaceType.Dirt, 12, roads.Profile);
            roads.RestoreWorldAccumulation(fresh);
            LwsRoadConditionSnapshot restored = roads.CurrentSnapshot;
            AssertTargetIdentity(restored);
            Assert.AreEqual(LwsRoadConditionType.Dry, restored.condition);
            Assert.AreEqual(0f, restored.wetness01);
            Assert.AreEqual(0f, restored.standingWater01);
            Assert.AreEqual(0f, restored.snowDepth01);
            Assert.AreEqual(0f, restored.packedSnow01);
            Assert.AreEqual(0f, restored.ice01);
            Assert.AreEqual(0f, restored.hydroplaningRisk01);
            LwsRoadConditionGripProfile dry = roads.Profile.GetGripProfile(LwsRoadConditionType.Dry);
            Assert.AreEqual(dry.longitudinalGripMultiplier01, restored.longitudinalGripMultiplier01);
            Assert.AreEqual(dry.lateralGripMultiplier01, restored.lateralGripMultiplier01);
            Assert.AreEqual(dry.brakingGripMultiplier01, restored.brakingGripMultiplier01);
            Assert.AreEqual(dry.rollingResistanceMultiplier, restored.rollingResistanceMultiplier);
            Assert.IsFalse(roads.TryGetRoadCondition("edge.0", out _));
            Assert.IsFalse(roads.TryGetRoadCondition("edge.1", out _));
        }

        [TestCase(LwsRoadConditionOverrideMode.AutoFromWeather, false)]
        [TestCase(LwsRoadConditionOverrideMode.ForceSnow, true)]
        public void WorldAccumulationPreservesCurrentOverrideIntent(LwsRoadConditionOverrideMode mode, bool forced)
        {
            SelectRoad(2);
            roads.ForceCondition(mode);
            LwsRoadConditionSnapshot saved = SavedMixedAccumulation();
            saved.forced = !forced;
            roads.RestoreWorldAccumulation(saved);
            Assert.AreEqual(mode, roads.Mode);
            Assert.AreEqual(forced, roads.CurrentSnapshot.forced);
            AssertTargetIdentity(roads.CurrentSnapshot);
        }

        [TestCase(TruckTaxiWeatherCondition.Snow, .3f, .1f, .2f)]
        [TestCase(TruckTaxiWeatherCondition.HeavySnow, .4f, .2f, .3f)]
        [TestCase(TruckTaxiWeatherCondition.Blizzard, .5f, .3f, .4f)]
        public void ObservedSnowAccumulatesDespiteStaleClearManualForecast(TruckTaxiWeatherCondition observed,
            float background, float clearedCell, float shallowCell)
        {
            TruckTaxiEnvironmentSnapshot environment = SnowEnvironment(TruckTaxiWeatherCondition.Clear);
            environment.forecast.automatic = true;
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings, observed);
            Assert.AreEqual(background, environment.snow.backgroundDepth, .00001f);
            AssertDepths(environment.snow, clearedCell, shallowCell, .6f, .6f);
            Assert.AreEqual(TruckTaxiWeatherCondition.Clear, environment.forecast.manualCondition);
            Assert.IsTrue(environment.forecast.automatic);
        }

        [TestCase(TruckTaxiWeatherCondition.Clear)]
        [TestCase(TruckTaxiWeatherCondition.Rain)]
        [TestCase(TruckTaxiWeatherCondition.Fog)]
        public void ObservedNonSnowMeltsDespiteStaleBlizzardManualForecast(TruckTaxiWeatherCondition observed)
        {
            TruckTaxiEnvironmentSnapshot environment = SnowEnvironment(TruckTaxiWeatherCondition.Blizzard);
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings, observed);
            Assert.AreEqual(.15f, environment.snow.backgroundDepth, .00001f);
            AssertDepths(environment.snow, 0, .05f, .45f, .55f);
            Assert.AreEqual(TruckTaxiWeatherCondition.Blizzard, environment.forecast.manualCondition);
        }

        [Test]
        public void CanonicalSavedBlizzardObservationDrivesCatchupInsteadOfManualClear()
        {
            weather.RegisterPreset(TruckTaxiEnvironmentCoordinator.CreateBlizzardPreset());
            Assert.IsTrue(weather.RequestWeather(TruckTaxiSnow.BlizzardId, 0, true).Succeeded);
            LwsSaveParticipantState saved = new LwsWeatherSaveParticipant(() => registry).CaptureState();
            var payload = JsonUtility.FromJson<LwsWeatherSavePayload>(saved.payloadJson);
            TruckTaxiWeatherCondition observed = TruckTaxiCalendarWeatherService.ConditionFor(payload.weatherPresetId);
            Assert.AreEqual(TruckTaxiWeatherCondition.Blizzard, observed);
            TruckTaxiEnvironmentSnapshot environment = SnowEnvironment(TruckTaxiWeatherCondition.Clear);
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings, observed);
            Assert.AreEqual(.5f, environment.snow.backgroundDepth, .00001f);
            AssertDepths(environment.snow, .3f, .4f, .6f, .6f);
        }

        [Test]
        public void MissingObservationRetainsManualForecastCompatibility()
        {
            TruckTaxiEnvironmentSnapshot environment = SnowEnvironment(TruckTaxiWeatherCondition.HeavySnow);
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings);
            Assert.AreEqual(.4f, environment.snow.backgroundDepth, .00001f);
            AssertDepths(environment.snow, .2f, .3f, .6f, .6f);
        }

        private void SeedPreviousWorldAndSelectTarget()
        {
            SelectRoad(0);
            roads.ForceCondition(LwsRoadConditionOverrideMode.ForceSnow);
            SelectRoad(1);
            roads.ForceCondition(LwsRoadConditionOverrideMode.ForceIce);
            Assert.IsTrue(roads.TryGetRoadCondition("edge.0", out var snow));
            Assert.Greater(snow.snowDepth01, 0);
            Assert.IsTrue(roads.TryGetRoadCondition("edge.1", out var ice));
            Assert.Greater(ice.ice01, 0);
            roads.SetAutoFromWeather();
            SelectRoad(2);
        }

        private void SelectRoad(int index)
        {
            roads.UpdatePlayerRoad(Vector3.forward * (index * 200 + 100), Vector3.forward);
            Assert.AreEqual("edge." + index, roads.CurrentRoadKey);
        }

        private void AssertTargetIdentity(LwsRoadConditionSnapshot snapshot)
        {
            Assert.AreEqual("road.2", snapshot.roadId);
            Assert.AreEqual("segment.2", snapshot.segmentId);
            Assert.AreEqual("edge.2", snapshot.edgeId);
            Assert.AreEqual("edge.2", roads.CurrentRoadKey);
            Assert.AreEqual(LwsRoadSurfaceType.Gravel, snapshot.baseSurfaceType);
        }

        private static LwsRoadConditionSnapshot SavedMixedAccumulation() => new LwsRoadConditionSnapshot
        {
            roadId = "saved.source.road", segmentId = "saved.source.segment", edgeId = "saved.source.edge",
            baseSurfaceType = LwsRoadSurfaceType.Dirt, condition = LwsRoadConditionType.Mixed,
            wetness01 = .35f, standingWater01 = .15f, snowDepth01 = .55f, packedSnow01 = .2f, ice01 = .3f,
            surfaceTemperatureC = -8, hydroplaningRisk01 = .4f, vehicleSpeedMetersPerSecond = 25,
            lastWeatherPresetId = TruckTaxiSnow.BlizzardId
        };

        private static TruckTaxiEnvironmentSnapshot SnowEnvironment(TruckTaxiWeatherCondition manual) =>
            new TruckTaxiEnvironmentSnapshot
            {
                hasSnowState = true, forecast = new TruckTaxiForecastSnapshot { manualCondition = manual },
                snow = new TruckTaxiSnowSnapshot { maximumDepth = .6f, backgroundDepth = .2f, depths = new[] { 0f, .1f, .5f, .6f } }
            };

        private static void AssertDepths(TruckTaxiSnowSnapshot snow, params float[] expected)
        {
            Assert.AreEqual(expected.Length, snow.depths.Length);
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], snow.depths[i], .00001f, "Cell " + i);
        }

        private static LwsRoadGraph CreateGraph()
        {
            var result = new LwsRoadGraph { graphId = "world-transfer.fixture" };
            for (int i = 0; i < 4; i++)
                result.nodes.Add(new LwsRoadNode { nodeId = "node." + i, position = Vector3.forward * (i * 200) });
            for (int i = 0; i < 3; i++)
            {
                var edge = new LwsRoadEdge
                {
                    roadId = "road." + i, segmentId = "segment." + i, edgeId = "edge." + i,
                    fromNodeId = "node." + i, toNodeId = "node." + (i + 1),
                    roadClass = LwsRoadClass.LocalRoad, direction = LwsRoadDirection.Northbound,
                    surfaceType = i == 2 ? LwsRoadSurfaceType.Gravel : LwsRoadSurfaceType.AsphaltInterstate,
                    oneWay = true, distanceMeters = 200, travelCost = 200, speedLimitMph = 30,
                    laneCount = 1, laneWidthMeters = 3.5f, laneCenterOffsetsMeters = new List<float> { 0 }
                };
                for (int sample = 0; sample < 3; sample++)
                    edge.samples.Add(new LwsRoadSample
                    {
                        roadId = edge.roadId, segmentId = edge.segmentId,
                        position = Vector3.forward * (i * 200 + sample * 100), forward = Vector3.forward, up = Vector3.up,
                        distanceFromStartMeters = sample * 100, direction = edge.direction,
                        roadWidthMeters = 3.5f, laneWidthMeters = 3.5f, laneCount = 1, speedLimitMph = 30
                    });
                result.edges.Add(edge);
            }
            return result;
        }

        private sealed class RecordingPhysicsAdapter : ILwsRoadConditionPhysicsAdapter
        {
            public string AdapterId => "world-transfer.fixture";
            public bool IsAvailable => true;
            public string Status => "recording only";
            public int BoundWheelCount => 0;
            public int Calls;
            public LwsRoadConditionSnapshot Last;
            public void ApplyRoadCondition(LwsRoadConditionSnapshot snapshot) { Calls++; Last = snapshot; }
            public void RestoreDryBaseline() { }
        }
    }
}
