using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsWeatherPlayModeTests
    {
        [UnityTest]
        public IEnumerator WeatherServiceInitializesAndRequestsPresets()
        {
            var registry = new LwsServiceRegistry();
            var weather = new LwsWeatherCoordinator();
            registry.Register<ILwsWeatherService>(weather);
            Assert.IsTrue(registry.InitializeAll().Succeeded);

            Assert.IsTrue(weather.RequestWeather(LwsWeatherPresetCatalog.HeavyRainId, 0f, true).Succeeded);
            yield return null;

            Assert.AreEqual(LwsWeatherCondition.HeavyRain, weather.CurrentSnapshot.condition);
            Assert.AreEqual(LwsPrecipitationType.Rain, weather.CurrentSnapshot.precipitationType);

            Assert.IsTrue(weather.RequestWeather(LwsWeatherPresetCatalog.LightSnowId, 0f, true).Succeeded);
            yield return null;

            Assert.AreEqual(LwsPrecipitationType.Snow, weather.CurrentSnapshot.precipitationType);
            registry.ShutdownAll();
        }

        [UnityTest]
        public IEnumerator TimeRequestsUpdateDayNightSnapshot()
        {
            var registry = new LwsServiceRegistry();
            var weather = new LwsWeatherCoordinator();
            registry.Register<ILwsWeatherService>(weather);
            registry.InitializeAll();

            weather.SetTimeOfDayHours(0f);
            yield return null;

            Assert.IsTrue(weather.CurrentSnapshot.IsNight);
            Assert.Less(weather.CurrentSnapshot.Daylight01, 0.15f);

            weather.SetTimeOfDayHours(12f);
            yield return null;

            Assert.IsTrue(weather.CurrentSnapshot.IsDay);
            Assert.Greater(weather.CurrentSnapshot.Daylight01, 0.9f);
            registry.ShutdownAll();
        }

        [UnityTest]
        public IEnumerator AdapterAttachmentPreventsDoubleWeatherRuntimeOwnership()
        {
            var registry = new LwsServiceRegistry();
            var weather = new LwsWeatherCoordinator();
            registry.Register<ILwsWeatherService>(weather);
            registry.InitializeAll();

            var first = new FakeWeatherAdapter();
            var second = new FakeWeatherAdapter();

            Assert.IsTrue(weather.AttachAdapter(first));
            Assert.IsFalse(weather.AttachAdapter(second));
            Assert.AreSame(first, weather.ActiveAdapter);
            weather.DetachAdapter(first);
            Assert.IsTrue(weather.AttachAdapter(second));
            registry.ShutdownAll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WeatherDebugPanelCanResolveBootstrapService()
        {
            LwsApplicationBootstrap.ResetForTests();
            var bootstrapObject = new GameObject("weather-bootstrap-test");
            bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            var panelObject = new GameObject("weather-debug-panel-test");
            panelObject.AddComponent<LwsWeatherDebugPanel>();

            yield return null;

            LwsApplicationBootstrap bootstrap = LwsApplicationBootstrap.Instance;
            Assert.IsNotNull(bootstrap);
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWeatherService weather));
            Assert.AreEqual(LwsWeatherPresetCatalog.ClearId, weather.CurrentSnapshot.weatherPresetId);

            Object.Destroy(panelObject);
            Object.Destroy(bootstrapObject);
            LwsApplicationBootstrap.ResetForTests();
        }

        private sealed class FakeWeatherAdapter : ILwsWeatherRuntimeAdapter
        {
            public bool WeatherMakerAvailable => true;
            public bool WeatherCameraBound => true;
            public string ActiveCameraName => "FakeCamera";
            public string AdapterStatus { get; private set; } = "Ready";

            public bool ApplyWeatherPreset(LwsWeatherPreset preset, float transitionSeconds, bool instant)
            {
                AdapterStatus = preset.presetId;
                return true;
            }

            public bool ApplyTimeOfDayHours(float hours)
            {
                return true;
            }

            public bool ApplyTimeScale(float timeScale)
            {
                return true;
            }

            public bool ApplyQualityTier(LwsRenderQualityTier tier)
            {
                return true;
            }
        }
    }
}
