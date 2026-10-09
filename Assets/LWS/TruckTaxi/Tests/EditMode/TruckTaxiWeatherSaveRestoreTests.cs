using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiWeatherSaveRestoreTests
    {
        private LwsServiceRegistry registry;
        private LwsWeatherCoordinator weather;
        private RecordingAdapter adapter;
        private LwsWeatherSaveParticipant participant;

        [SetUp]
        public void SetUp()
        {
            registry = new LwsServiceRegistry();
            weather = new LwsWeatherCoordinator();
            registry.Register<ILwsWeatherService>(weather);
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            adapter = new RecordingAdapter();
            Assert.IsTrue(weather.AttachAdapter(adapter));
            participant = new LwsWeatherSaveParticipant(() => registry);
        }

        [TearDown]
        public void TearDown() => registry?.ShutdownAll();

        [Test]
        public void RainToSavedSnowRestoresFullSemanticsAndCallsAdapter()
        {
            Assert.IsTrue(weather.TryGetPreset(LwsWeatherPresetCatalog.HeavySnowId, out var snow));
            var catalog = snow;
            snow.precipitationIntensity01 = 0.63f;
            snow.cloudCover01 = 0.81f;
            snow.fogIntensity01 = 0.57f;
            snow.stormIntensity01 = 0.73f;
            snow.lightningActive = true;
            snow.windSpeedMetersPerSecond = 17f;
            snow.windDirectionWorld = new Vector3(-3f, 0f, 4f).normalized;
            snow.ambientTemperatureC = -11f;
            snow.visibilityMeters = 725f;
            Assert.IsTrue(weather.RequestWeather(snow, 0f, true).Succeeded);
            var legacy = weather.CurrentState;
            legacy.worldTimeTicks = 638900000000000000L;
            weather.SetState(legacy);
            weather.SetTimeOfDayHours(22.25f);
            var expected = weather.CurrentSnapshot;
            var saved = participant.CaptureState();

            Assert.IsTrue(weather.RequestWeather(LwsWeatherPresetCatalog.HeavyRainId, 0f, true).Succeeded);
            weather.SetTimeOfDayHours(12f);
            int calls = adapter.WeatherCalls;
            var result = participant.RestoreState(saved);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(calls + 1, adapter.WeatherCalls);
            Assert.IsTrue(adapter.Instant);
            Assert.AreEqual(0f, adapter.Duration);
            Assert.AreEqual(catalog.weatherMakerProfileName, adapter.Preset.weatherMakerProfileName);
            Assert.AreEqual(catalog.weatherMakerProfilePath, adapter.Preset.weatherMakerProfilePath);
            var actual = weather.CurrentSnapshot;
            Assert.AreEqual(expected.weatherPresetId, actual.weatherPresetId);
            Assert.AreEqual(LwsWeatherCondition.HeavySnow, actual.condition);
            Assert.AreEqual(LwsPrecipitationType.Snow, actual.precipitationType);
            Assert.AreEqual(expected.precipitationIntensity01, actual.precipitationIntensity01);
            Assert.AreEqual(expected.cloudCover01, actual.cloudCover01);
            Assert.AreEqual(expected.fogIntensity01, actual.fogIntensity01);
            Assert.AreEqual(expected.stormIntensity01, actual.stormIntensity01);
            Assert.AreEqual(expected.lightningActive, actual.lightningActive);
            Assert.AreEqual(expected.ambientTemperatureC, actual.ambientTemperatureC);
            Assert.AreEqual(expected.visibilityMeters, actual.visibilityMeters);
            Assert.Less(Vector3.Distance(expected.windVelocity, actual.windVelocity), 0.0001f);
            Assert.AreEqual(expected.worldTimeTicks, actual.worldTimeTicks);
            Assert.AreEqual(expected.timeOfDayHours, actual.timeOfDayHours);
            Assert.AreEqual(expected.timeOfDayHours, adapter.Hours);
            Assert.IsFalse(actual.transitioning);
            var presented = adapter.Preset.ToSnapshot(actual.timeOfDayHours);
            Assert.AreEqual(actual.condition, presented.condition);
            Assert.AreEqual(actual.precipitationIntensity01, presented.precipitationIntensity01);
            Assert.AreEqual(actual.cloudCover01, presented.cloudCover01);
            Assert.AreEqual(actual.fogIntensity01, presented.fogIntensity01);
            Assert.AreEqual(actual.stormIntensity01, presented.stormIntensity01);
            Assert.AreEqual(actual.lightningActive, presented.lightningActive);
        }

        [Test]
        public void FailedVendorApplyFailsRestore()
        {
            var saved = participant.CaptureState();
            adapter.Succeeds = false;
            var result = participant.RestoreState(saved);
            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains(adapter.AdapterStatus, result.Message);
        }

        [Test]
        public void UnknownCatalogMappingFailsWithoutApplyingAnotherProfile()
        {
            var saved = participant.CaptureState();
            var payload = JsonUtility.FromJson<LwsWeatherSavePayload>(saved.payloadJson);
            payload.weatherPresetId = "missing-profile";
            saved.payloadJson = JsonUtility.ToJson(payload);
            int calls = adapter.WeatherCalls;
            Assert.IsFalse(participant.RestoreState(saved).Succeeded);
            Assert.AreEqual(calls, adapter.WeatherCalls);
        }

        private sealed class RecordingAdapter : ILwsWeatherRuntimeAdapter
        {
            public bool WeatherMakerAvailable => true;
            public bool WeatherCameraBound => true;
            public string ActiveCameraName => "fixture";
            public string AdapterStatus => "fixture apply failure";
            public bool Succeeds = true;
            public int WeatherCalls;
            public LwsWeatherPreset Preset;
            public bool Instant;
            public float Duration;
            public float Hours;
            public bool ApplyWeatherPreset(LwsWeatherPreset preset, float transitionSeconds, bool instant)
            {
                WeatherCalls++;
                Preset = preset;
                Duration = transitionSeconds;
                Instant = instant;
                return Succeeds;
            }
            public bool ApplyTimeOfDayHours(float hours) { Hours = hours; return true; }
            public bool ApplyTimeScale(float timeScale) => true;
            public bool ApplyQualityTier(LwsRenderQualityTier tier) => true;
        }
    }
}
