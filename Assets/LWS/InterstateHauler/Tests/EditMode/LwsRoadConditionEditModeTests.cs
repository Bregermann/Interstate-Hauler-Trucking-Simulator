using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsRoadConditionEditModeTests
    {
        [Test]
        public void DefaultGripHierarchyIsNoticeable()
        {
            LwsRoadConditionProfile profile = LwsRoadConditionProfile.Default();
            Assert.IsTrue(profile.Validate(out string message), message);

            LwsRoadConditionGripProfile dry = profile.GetGripProfile(LwsRoadConditionType.Dry);
            LwsRoadConditionGripProfile wet = profile.GetGripProfile(LwsRoadConditionType.Wet);
            LwsRoadConditionGripProfile snow = profile.GetGripProfile(LwsRoadConditionType.LightSnow);
            LwsRoadConditionGripProfile ice = profile.GetGripProfile(LwsRoadConditionType.Ice);

            Assert.Less(wet.longitudinalGripMultiplier01, dry.longitudinalGripMultiplier01);
            Assert.Less(snow.longitudinalGripMultiplier01, wet.longitudinalGripMultiplier01);
            Assert.Less(ice.longitudinalGripMultiplier01, snow.longitudinalGripMultiplier01);
            Assert.Less(wet.brakingGripMultiplier01, dry.brakingGripMultiplier01);
            Assert.Less(snow.brakingGripMultiplier01, wet.brakingGripMultiplier01);
            Assert.Less(ice.brakingGripMultiplier01, snow.brakingGripMultiplier01);
        }

        [Test]
        public void RainAccumulatesGraduallyAndHeavyRainIsFaster()
        {
            LwsRoadConditionProfile profile = LwsRoadConditionProfile.Default();
            LwsRoadConditionSnapshot dry = CreateDry(profile);
            LwsWeatherSnapshot lightRain = CreateWeather(LwsPrecipitationType.Rain, 0.28f, 12f);
            LwsWeatherSnapshot heavyRain = CreateWeather(LwsPrecipitationType.Rain, 0.82f, 12f);

            LwsRoadConditionSnapshot light = LwsRoadConditionSimulation.StepAuto(dry, lightRain, profile, 90f, 0f);
            LwsRoadConditionSnapshot heavy = LwsRoadConditionSimulation.StepAuto(dry, heavyRain, profile, 90f, 0f);

            Assert.Greater(light.wetness01, dry.wetness01);
            Assert.Greater(heavy.wetness01, light.wetness01);
            Assert.Less(heavy.wetness01, 1f);
        }

        [Test]
        public void WetnessDriesWhenRainStops()
        {
            LwsRoadConditionProfile profile = LwsRoadConditionProfile.Default();
            LwsRoadConditionSnapshot wet = CreateDry(profile);
            wet.wetness01 = 0.8f;
            wet.condition = LwsRoadConditionType.Wet;

            LwsRoadConditionSnapshot dryWeather = LwsRoadConditionSimulation.StepAuto(
                wet,
                CreateWeather(LwsPrecipitationType.None, 0f, 18f),
                profile,
                300f,
                0f);

            Assert.Less(dryWeather.wetness01, wet.wetness01);
        }

        [Test]
        public void SnowAccumulatesAndMeltsWhenWarm()
        {
            LwsRoadConditionProfile profile = LwsRoadConditionProfile.Default();
            LwsRoadConditionSnapshot dry = CreateDry(profile);
            LwsRoadConditionSnapshot snowing = LwsRoadConditionSimulation.StepAuto(
                dry,
                CreateWeather(LwsPrecipitationType.Snow, 0.82f, -5f),
                profile,
                120f,
                0f);

            LwsRoadConditionSnapshot melting = LwsRoadConditionSimulation.StepAuto(
                snowing,
                CreateWeather(LwsPrecipitationType.None, 0f, 10f),
                profile,
                300f,
                0f);

            Assert.Greater(snowing.snowDepth01, dry.snowDepth01);
            Assert.Less(melting.snowDepth01 + melting.packedSnow01, snowing.snowDepth01 + snowing.packedSnow01);
        }

        [Test]
        public void IceRequiresMoistureAndFreezingConditions()
        {
            LwsRoadConditionProfile profile = LwsRoadConditionProfile.Default();
            LwsRoadConditionSnapshot coldDry = CreateDry(profile);
            coldDry.surfaceTemperatureC = -5f;

            LwsRoadConditionSnapshot afterColdDry = LwsRoadConditionSimulation.StepAuto(
                coldDry,
                CreateWeather(LwsPrecipitationType.None, 0f, -5f),
                profile,
                300f,
                0f);

            LwsRoadConditionSnapshot coldWet = coldDry;
            coldWet.wetness01 = 0.8f;
            LwsRoadConditionSnapshot afterColdWet = LwsRoadConditionSimulation.StepAuto(
                coldWet,
                CreateWeather(LwsPrecipitationType.None, 0f, -5f),
                profile,
                300f,
                0f);

            Assert.LessOrEqual(afterColdDry.ice01, 0.01f);
            Assert.Greater(afterColdWet.ice01, afterColdDry.ice01);
        }

        [Test]
        public void IceMeltsWhenWarmEnough()
        {
            LwsRoadConditionProfile profile = LwsRoadConditionProfile.Default();
            LwsRoadConditionSnapshot icy = CreateDry(profile);
            icy.ice01 = 0.8f;
            icy.condition = LwsRoadConditionType.Ice;
            icy.surfaceTemperatureC = -5f;

            LwsRoadConditionSnapshot warm = LwsRoadConditionSimulation.StepAuto(
                icy,
                CreateWeather(LwsPrecipitationType.None, 0f, 12f),
                profile,
                300f,
                0f);

            Assert.Less(warm.ice01, icy.ice01);
        }

        [Test]
        public void RoadStateUsesStableIdsAndClampsValues()
        {
            LwsRoadConditionSnapshot snapshot = LwsRoadConditionSnapshot.CreateDry(
                "IH_TEST_I000_NB",
                "IH_TEST_I000_NB_MAIN",
                "IH_TEST_I000_NB_MAIN_EDGE",
                LwsRoadSurfaceType.AsphaltInterstate,
                12f,
                LwsRoadConditionProfile.Default());

            snapshot.wetness01 = 4f;
            snapshot.ice01 = -4f;
            snapshot.Clamp();

            Assert.AreEqual("IH_TEST_I000_NB_MAIN_EDGE", snapshot.StateKey);
            Assert.AreEqual(1f, snapshot.wetness01);
            Assert.AreEqual(0f, snapshot.ice01);
        }

        [Test]
        public void RoadConditionGameplayInterfaceDoesNotLeakVendorTypes()
        {
            string source = File.ReadAllText("Assets/LWS/InterstateHauler/Roads/Conditions/LwsRoadCondition.cs");

            StringAssert.DoesNotContain("NOT_Lonely", source);
            StringAssert.DoesNotContain("DigitalRuby", source);
            StringAssert.DoesNotContain("WeatherMaker", source);
            StringAssert.DoesNotContain("NWH.", source);
        }

        private static LwsRoadConditionSnapshot CreateDry(LwsRoadConditionProfile profile)
        {
            return LwsRoadConditionSnapshot.CreateDry(
                "road.test",
                "segment.test",
                "edge.test",
                LwsRoadSurfaceType.AsphaltInterstate,
                12f,
                profile);
        }

        private static LwsWeatherSnapshot CreateWeather(LwsPrecipitationType precipitationType, float intensity, float temperatureC)
        {
            LwsWeatherSnapshot snapshot = LwsWeatherSnapshot.Clear;
            snapshot.precipitationType = precipitationType;
            snapshot.precipitationIntensity01 = intensity;
            snapshot.ambientTemperatureC = temperatureC;
            snapshot.weatherPresetId = precipitationType == LwsPrecipitationType.Snow ? LwsWeatherPresetCatalog.HeavySnowId :
                precipitationType == LwsPrecipitationType.Rain ? LwsWeatherPresetCatalog.HeavyRainId :
                LwsWeatherPresetCatalog.ClearId;
            snapshot.daylight01 = 1f;
            snapshot.windSpeedMetersPerSecond = 5f;
            snapshot.ClampAndRefreshDerived();
            return snapshot;
        }
    }
}
