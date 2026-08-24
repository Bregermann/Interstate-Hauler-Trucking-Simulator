using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [UnityTest]
        public IEnumerator WeatherRequestSnapshotAndAdapterRequestStayAligned()
        {
            var registry = new LwsServiceRegistry();
            var weather = new LwsWeatherCoordinator();
            registry.Register<ILwsWeatherService>(weather);
            Assert.IsTrue(registry.InitializeAll().Succeeded);

            var adapter = new FakeWeatherAdapter();
            Assert.IsTrue(weather.AttachAdapter(adapter));

            LwsServiceResult result = weather.RequestWeather(LwsWeatherPresetCatalog.HeavyRainId, 0f, true);
            yield return null;

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(LwsWeatherPresetCatalog.HeavyRainId, weather.CurrentSnapshot.weatherPresetId);
            Assert.AreEqual(LwsWeatherPresetCatalog.HeavyRainId, adapter.LastRequestedPresetId);
            Assert.AreEqual("WeatherMakerProfile_HeavyRain", adapter.LastRequestedProfileName);
            Assert.AreEqual(0f, adapter.LastTransitionSeconds);
            Assert.IsTrue(adapter.LastInstant);
            registry.ShutdownAll();
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator WeatherMakerAdapterCanInstantiatePrefabAndRefreshCameraWithoutDuplicates()
        {
            LwsApplicationBootstrap.ResetForTests();
            CleanupWeatherMakerRuntime();
            yield return null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LwsWeatherMakerAdapter.DefaultWeatherMakerPrefabPath);
            Assert.IsNotNull(prefab, LwsWeatherMakerAdapter.DefaultWeatherMakerPrefabPath);

            var cameraObject = new GameObject("IH Gameplay Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();

            var bootstrapObject = new GameObject("weather-bootstrap-runtime-test");
            bootstrapObject.AddComponent<LwsApplicationBootstrap>();

            var adapterObject = new GameObject("weather-maker-adapter-runtime-test");
            LwsWeatherMakerAdapter adapter = adapterObject.AddComponent<LwsWeatherMakerAdapter>();
            adapter.ConfigureWeatherMakerPrefab(prefab);

            yield return null;
            yield return null;

            Assert.IsTrue(adapter.WeatherMakerPrefabConfigured);
            Assert.IsTrue(adapter.WeatherMakerAvailable, adapter.AdapterStatus);
            Assert.IsTrue(adapter.WeatherMakerInstanceResolved, adapter.AdapterStatus);
            Assert.AreEqual(1, adapter.WeatherMakerInstanceCount, adapter.AdapterStatus);
            Assert.IsTrue(adapter.DayNightManagerAvailable, adapter.AdapterStatus);

            int instanceCount = adapter.WeatherMakerInstanceCount;
            adapter.RefreshCameraBindingForValidation();
            yield return null;

            Assert.AreEqual(instanceCount, adapter.WeatherMakerInstanceCount);
            Assert.IsTrue(adapter.WeatherCameraBound, adapter.AdapterStatus);
            Assert.IsTrue(adapter.ActiveCameraAllowed, adapter.AdapterStatus);
            Assert.AreEqual("IH Gameplay Camera", adapter.ActiveCameraName);

            LwsApplicationBootstrap bootstrap = LwsApplicationBootstrap.Instance;
            Assert.IsNotNull(bootstrap);
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsCameraPresentationService cameraPresentationService));
            cameraPresentationService.SetCameraMode(LwsVehicleCameraMode.Exterior, "IH Gameplay Camera");
            adapter.RefreshCameraBindingForValidation();
            yield return null;
            Assert.AreEqual("IH Gameplay Camera", adapter.ActiveCameraName);

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWeatherService weatherService));
            LwsServiceResult heavyRain = weatherService.RequestWeather(LwsWeatherPresetCatalog.HeavyRainId, 0f, true);
            yield return null;
            yield return null;

            Assert.IsTrue(heavyRain.Succeeded, heavyRain.Message);
            Assert.IsTrue(adapter.LastWeatherMakerApplySucceeded, adapter.AdapterStatus);
            Assert.AreEqual("WeatherMakerProfile_HeavyRain", adapter.LastResolvedWeatherMakerProfile);
            StringAssert.Contains("Heavy Rain", adapter.LastApplySummary);
            StringAssert.Contains("Rain", adapter.PrecipitationDiagnostic);

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsGameClockService gameClockService));
            gameClockService.SetTimeOfDayHours(20f);
            yield return null;
            Assert.IsTrue(adapter.GameClockSlaved, adapter.AdapterStatus);
            Assert.AreEqual(20f, adapter.WeatherMakerTimeOfDayHours, 0.25f);

            Object.Destroy(adapterObject);
            Object.Destroy(bootstrapObject);
            Object.Destroy(cameraObject);
            CleanupWeatherMakerRuntime();
            LwsApplicationBootstrap.ResetForTests();
            yield return null;
        }
#endif

        private sealed class FakeWeatherAdapter : ILwsWeatherRuntimeAdapter
        {
            public bool WeatherMakerAvailable => true;
            public bool WeatherCameraBound => true;
            public string ActiveCameraName => "FakeCamera";
            public string AdapterStatus { get; private set; } = "Ready";
            public string LastRequestedPresetId { get; private set; } = "None";
            public string LastRequestedProfileName { get; private set; } = "None";
            public float LastTransitionSeconds { get; private set; } = -1f;
            public bool LastInstant { get; private set; }

            public bool ApplyWeatherPreset(LwsWeatherPreset preset, float transitionSeconds, bool instant)
            {
                AdapterStatus = preset.presetId;
                LastRequestedPresetId = preset.presetId;
                LastRequestedProfileName = preset.weatherMakerProfileName;
                LastTransitionSeconds = transitionSeconds;
                LastInstant = instant;
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

#if UNITY_EDITOR
        private static void CleanupWeatherMakerRuntime()
        {
            GameObject runtime = GameObject.Find("IH Weather Maker Runtime");
            if (runtime != null)
            {
                Object.Destroy(runtime);
            }

            GameObject vendorNamedRuntime = GameObject.Find("WeatherMakerPrefab");
            if (vendorNamedRuntime != null)
            {
                Object.Destroy(vendorNamedRuntime);
            }
        }
#endif
    }
}
