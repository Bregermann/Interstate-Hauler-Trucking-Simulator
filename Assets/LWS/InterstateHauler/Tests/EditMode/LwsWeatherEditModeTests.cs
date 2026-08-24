using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsWeatherEditModeTests
    {
        private static readonly string[] WeatherPresetPaths =
        {
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Clear.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_PartlyCloudy.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Cloudy.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Overcast.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_LightRain.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_HeavyRain.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Thunderstorm.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_LightSnow.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_HeavySnow.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Fog.asset"
        };

        [Test]
        public void WeatherPresetAssetsValidateAndMapToInstalledProfiles()
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string path in WeatherPresetPaths)
            {
                LwsWeatherPresetDefinition definition = AssetDatabase.LoadAssetAtPath<LwsWeatherPresetDefinition>(path);

                Assert.IsNotNull(definition, path);
                Assert.IsTrue(definition.ValidateDefinition(out string message), message);
                Assert.IsTrue(ids.Add(definition.PresetId), $"Duplicate weather preset ID: {definition.PresetId}");
                Assert.IsTrue(File.Exists(definition.Preset.weatherMakerProfilePath), definition.Preset.weatherMakerProfilePath);
            }

            CollectionAssert.Contains(ids, LwsWeatherPresetCatalog.ThunderstormId);
            CollectionAssert.Contains(ids, LwsWeatherPresetCatalog.HeavySnowId);
            CollectionAssert.Contains(ids, LwsWeatherPresetCatalog.FogId);
        }

        [Test]
        public void ClearRainSnowAndStormMapToPrompt013FacingSemantics()
        {
            Assert.IsTrue(LwsWeatherPresetCatalog.TryGetBuiltInPreset(LwsWeatherPresetCatalog.ClearId, out LwsWeatherPreset clear));
            Assert.IsTrue(LwsWeatherPresetCatalog.TryGetBuiltInPreset(LwsWeatherPresetCatalog.LightRainId, out LwsWeatherPreset rain));
            Assert.IsTrue(LwsWeatherPresetCatalog.TryGetBuiltInPreset(LwsWeatherPresetCatalog.HeavySnowId, out LwsWeatherPreset snow));
            Assert.IsTrue(LwsWeatherPresetCatalog.TryGetBuiltInPreset(LwsWeatherPresetCatalog.ThunderstormId, out LwsWeatherPreset storm));

            Assert.AreEqual(LwsPrecipitationType.None, clear.precipitationType);
            Assert.AreEqual(0f, clear.precipitationIntensity01);
            Assert.AreEqual(LwsPrecipitationType.Rain, rain.precipitationType);
            Assert.Greater(rain.precipitationIntensity01, 0f);
            Assert.AreEqual(LwsPrecipitationType.Snow, snow.precipitationType);
            Assert.Greater(snow.precipitationIntensity01, 0f);
            Assert.IsTrue(storm.lightningActive);
            Assert.Greater(storm.stormIntensity01, 0.5f);
        }

        [Test]
        public void WeatherSnapshotClampsSemanticRangesAndLeavesRoadStateDry()
        {
            var snapshot = new LwsWeatherSnapshot
            {
                weatherPresetId = "",
                precipitationType = LwsPrecipitationType.Rain,
                precipitationIntensity01 = 3f,
                cloudCover01 = -2f,
                fogIntensity01 = 4f,
                stormIntensity01 = 8f,
                windSpeedMetersPerSecond = -1f,
                visibilityMeters = -5f,
                timeOfDayHours = 31f
            };

            snapshot.ClampAndRefreshDerived();

            Assert.AreEqual(LwsWeatherPresetCatalog.ClearId, snapshot.weatherPresetId);
            Assert.AreEqual(1f, snapshot.precipitationIntensity01);
            Assert.AreEqual(0f, snapshot.cloudCover01);
            Assert.AreEqual(1f, snapshot.fogIntensity01);
            Assert.AreEqual(1f, snapshot.stormIntensity01);
            Assert.AreEqual(0f, snapshot.windSpeedMetersPerSecond);
            Assert.AreEqual(20f, snapshot.visibilityMeters);
            Assert.AreEqual(7f, snapshot.timeOfDayHours);
            Assert.AreEqual(0f, snapshot.Wetness);
            Assert.AreEqual(0f, snapshot.SnowAmount);
        }

        [Test]
        public void WeatherServiceTransitionsAndPublishesEvents()
        {
            var registry = new LwsServiceRegistry();
            var weather = new LwsWeatherCoordinator();
            registry.Register<ILwsWeatherService>(weather);
            Assert.IsTrue(registry.InitializeAll().Succeeded);

            bool started = false;
            bool completed = false;
            weather.WeatherTransitionStarted += (_, _, _) => started = true;
            weather.WeatherTransitionCompleted += _ => completed = true;

            LwsServiceResult request = weather.RequestWeather(LwsWeatherPresetCatalog.LightRainId, 1f);

            Assert.IsTrue(request.Succeeded, request.Message);
            Assert.IsTrue(started);
            weather.Tick(0.5f);
            Assert.IsTrue(weather.CurrentSnapshot.transitioning);
            weather.Tick(0.6f);
            Assert.IsFalse(weather.CurrentSnapshot.transitioning);
            Assert.IsTrue(completed);
            Assert.AreEqual(LwsPrecipitationType.Rain, weather.CurrentSnapshot.precipitationType);
            registry.ShutdownAll();
        }

        [Test]
        public void DuplicateWeatherServicesAreRejected()
        {
            var firstRegistry = new LwsServiceRegistry();
            var secondRegistry = new LwsServiceRegistry();
            firstRegistry.Register<ILwsWeatherService>(new LwsWeatherCoordinator());
            secondRegistry.Register<ILwsWeatherService>(new LwsWeatherCoordinator());

            Assert.IsTrue(firstRegistry.InitializeAll().Succeeded);
            Assert.IsFalse(secondRegistry.InitializeAll().Succeeded);
            firstRegistry.ShutdownAll();
            secondRegistry.ShutdownAll();
        }

        [Test]
        public void Prompt013FacingWeatherTypesDoNotDependOnVendorClasses()
        {
            string source = File.ReadAllText("Assets/LWS/InterstateHauler/Weather/LwsWeather.cs");

            StringAssert.DoesNotContain("DigitalRuby.WeatherMaker", source);
            StringAssert.DoesNotContain("NWH", source);
            StringAssert.DoesNotContain("UTS", source);
            StringAssert.DoesNotContain("Weatherade", source);
        }

        [Test]
        public void WeatherMakerAdapterContainsGameplayPresentationStabilizationHooks()
        {
            string source = File.ReadAllText("Assets/LWS/InterstateHauler/Weather/LwsWeatherMakerAdapter.cs");

            StringAssert.Contains("ILwsCameraPresentationService", source);
            StringAssert.Contains("PrecipitationManagerTypeName", source);
            StringAssert.Contains("FullScreenCloudsTypeName", source);
            StringAssert.Contains("FullScreenFogTypeName", source);
            StringAssert.Contains("ApplySemanticWeatherToWeatherMakerRuntime", source);
            StringAssert.Contains("SetTimeScale(0f)", source);
        }

        [Test]
        public void DevelopmentWeatherTabExposesVisibleRuntimeDiagnosticsAndInstantApply()
        {
            string source = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("Requested LWS Weather", source);
            StringAssert.Contains("Actual LWS Weather", source);
            StringAssert.Contains("Weather Maker Runtime", source);
            StringAssert.Contains("Weather Maker Applied Profile", source);
            StringAssert.Contains("Precipitation", source);
            StringAssert.Contains("Cloud Cover", source);
            StringAssert.Contains("Fog", source);
            StringAssert.Contains("Game Time", source);
            StringAssert.Contains("Daylight", source);
            StringAssert.Contains("Last Apply", source);
            StringAssert.Contains("Last Error", source);
            StringAssert.Contains("INSTANT APPLY", source);
            StringAssert.Contains("HEAVY SNOW", source);
        }
    }
}
