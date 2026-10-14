using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiWorldTravelTests
    {
        private TruckTaxiEnvironmentSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            settings.lightSnowMetersPerSecond = .01f;
            settings.heavySnowMetersPerSecond = .02f;
            settings.blizzardMetersPerSecond = .03f;
            settings.snowMeltMetersPerSecond = .005f;
        }

        [TearDown]
        public void TearDown()
        {
            if (settings != null) Object.DestroyImmediate(settings);
        }

        [Test]
        public void CatalogContainsOnlyTheTwoAuthoredScenesWithStableLabelsAndOppositeDestinations()
        {
            Assert.AreEqual("TruckTaxi_DemoCity", TruckTaxiWorldCatalog.Demo);
            Assert.AreEqual("TruckTaxi_Edenborough", TruckTaxiWorldCatalog.Edenborough);
            CollectionAssert.AreEquivalent(new[] { TruckTaxiWorldCatalog.Demo, TruckTaxiWorldCatalog.Edenborough },
                TruckTaxiWorldCatalog.Scenes);
            Assert.AreEqual("Demo City", TruckTaxiWorldCatalog.Label(TruckTaxiWorldCatalog.Demo));
            Assert.AreEqual("Edenborough", TruckTaxiWorldCatalog.Label(TruckTaxiWorldCatalog.Edenborough));
            Assert.AreEqual(TruckTaxiWorldCatalog.Edenborough, TruckTaxiWorldCatalog.Other(TruckTaxiWorldCatalog.Demo));
            Assert.AreEqual(TruckTaxiWorldCatalog.Demo, TruckTaxiWorldCatalog.Other(TruckTaxiWorldCatalog.Edenborough));
            foreach (string scene in TruckTaxiWorldCatalog.Scenes)
            {
                Assert.IsTrue(TruckTaxiWorldCatalog.Contains(scene));
                Assert.AreNotEqual(scene, TruckTaxiWorldCatalog.Other(scene));
                Assert.AreEqual(scene, TruckTaxiWorldCatalog.Other(TruckTaxiWorldCatalog.Other(scene)));
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Demo City")]
        [TestCase("trucktaxi_democity")]
        [TestCase("TruckTaxi_FreePlay")]
        [TestCase("TruckTaxi_Edenborough ")]
        [TestCase("../TruckTaxi_DemoCity")]
        [TestCase("Assets/LWS/TruckTaxi/Scenes/TruckTaxi_Edenborough.unity")]
        [TestCase(TruckTaxiMainMenu.SceneName)]
        public void UnknownSceneIsNotAWorldOrTravelDestination(string scene)
        {
            Assert.IsFalse(TruckTaxiWorldCatalog.Contains(scene));
            Assert.AreEqual("Unknown region", TruckTaxiWorldCatalog.Label(scene));
            Assert.IsNull(TruckTaxiWorldCatalog.Other(scene));
            Assert.IsFalse(TruckTaxiMainMenu.IsSupportedWorld(scene));
            TruckTaxiWorldVisit visit = Visit();
            visit.scene = scene;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
        }

        [Test]
        public void VisitHistoryAcceptsLegacyNullEmptyAndBothUniqueWorldsInEitherOrder()
        {
            Assert.IsTrue(TruckTaxiWorldVisit.ValidVisits(null), "Legacy payloads omit worldVisits.");
            Assert.IsTrue(TruckTaxiWorldVisit.ValidVisits(Array.Empty<TruckTaxiWorldVisit>()));
            TruckTaxiWorldVisit demo = Visit(TruckTaxiWorldCatalog.Demo);
            TruckTaxiWorldVisit edenborough = Visit(TruckTaxiWorldCatalog.Edenborough);
            Assert.IsTrue(TruckTaxiWorldVisit.ValidVisits(new[] { demo }));
            Assert.IsTrue(TruckTaxiWorldVisit.ValidVisits(new[] { demo, edenborough }));
            Assert.IsTrue(TruckTaxiWorldVisit.ValidVisits(new[] { edenborough, demo }));
        }

        [TestCase(TruckTaxiWorldCatalog.Demo)]
        [TestCase(TruckTaxiWorldCatalog.Edenborough)]
        public void VisitHistoryRejectsDuplicateWorldsEvenWhenEntriesHaveDifferentPoses(string scene)
        {
            TruckTaxiWorldVisit first = Visit(scene), second = Visit(scene);
            second.position = new Vector3(42, 3, -17);
            second.gameSeconds = 600;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { first, second }));
        }

        [Test]
        public void VisitHistoryRejectsMoreThanTwoEntriesAndNullEntries()
        {
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[]
                { Visit(), Visit(TruckTaxiWorldCatalog.Edenborough), Visit() }));
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new TruckTaxiWorldVisit[] { null }));
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { Visit(), null }));
        }

        [TestCaseSource(nameof(NonFinitePositionCases))]
        public void VisitRejectsEveryNonFinitePositionComponent(int axis, float value)
        {
            TruckTaxiWorldVisit visit = Visit();
            visit.position[axis] = value;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
        }

        [TestCaseSource(nameof(NonFiniteRotationCases))]
        public void VisitRejectsEveryNonFiniteQuaternionComponent(int axis, float value)
        {
            TruckTaxiWorldVisit visit = Visit();
            visit.rotation[axis] = value;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
        }

        [Test]
        public void VisitRejectsDegenerateRotationButAcceptsFiniteRotatedPoseAndZeroTime()
        {
            TruckTaxiWorldVisit visit = Visit();
            visit.position = new Vector3(-500, 125, 4000);
            visit.rotation = Quaternion.Euler(12, 155, -4);
            visit.clockRate = 0;
            Assert.IsTrue(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
            visit.rotation = new Quaternion(0, 0, 0, 0);
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
            visit.rotation = new Quaternion(0, 0, 0, .7f);
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
        }

        [TestCase(-.01d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void VisitRejectsInvalidGameTime(double value)
        {
            TruckTaxiWorldVisit visit = Visit();
            visit.gameSeconds = value;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
        }

        [TestCase(-.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void VisitRejectsInvalidClockRate(float value)
        {
            TruckTaxiWorldVisit visit = Visit();
            visit.clockRate = value;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
        }

        [TestCaseSource(nameof(DefinedWeatherConditions))]
        public void VisitAcceptsTheActualDefinedForecastConditions(TruckTaxiWeatherCondition condition)
        {
            TruckTaxiWorldVisit visit = Visit();
            visit.environment = Environment(condition);
            visit.environment.forecast.overrideCondition = condition;
            Assert.IsTrue(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
        }

        [TestCase(-1)]
        [TestCase(int.MaxValue)]
        public void VisitRejectsUndefinedManualOverrideAndSeverityEnums(int invalid)
        {
            TruckTaxiWorldVisit visit = Visit();
            visit.environment = Environment(TruckTaxiWeatherCondition.Clear);
            visit.environment.forecast.manualCondition = (TruckTaxiWeatherCondition)invalid;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }), "Manual condition");
            visit.environment.forecast.manualCondition = TruckTaxiWeatherCondition.Clear;
            visit.environment.forecast.overrideCondition = (TruckTaxiWeatherCondition)invalid;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }), "Override condition");
            visit.environment.forecast.overrideCondition = TruckTaxiWeatherCondition.Clear;
            visit.environment.forecast.severity = (TruckTaxiWeatherSeverity)invalid;
            Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }), "Severity");
        }

        [Test]
        public void VisitAcceptsOnlyCanonicalWeatherAndRoadParticipantIdentities()
        {
            TruckTaxiWorldVisit visit = Visit();
            visit.weather = new LwsSaveParticipantState { participantId = "lws.weather.semantic" };
            visit.roadCondition = new LwsSaveParticipantState { participantId = "lws.road-condition.semantic" };
            Assert.IsTrue(TruckTaxiWorldVisit.ValidVisits(new[] { visit }));
            foreach (string invalid in new[] { null, "", "weather", "LWS.WEATHER.SEMANTIC", "lws.road-condition.semantic" })
            {
                visit.weather.participantId = invalid;
                Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }), "Weather identity: " + invalid);
            }
            visit.weather.participantId = "lws.weather.semantic";
            foreach (string invalid in new[] { null, "", "road", "LWS.ROAD-CONDITION.SEMANTIC", "lws.weather.semantic" })
            {
                visit.roadCondition.participantId = invalid;
                Assert.IsFalse(TruckTaxiWorldVisit.ValidVisits(new[] { visit }), "Road identity: " + invalid);
            }
        }

        [TestCase(TruckTaxiWeatherCondition.Snow, .3f, .1f, .2f)]
        [TestCase(TruckTaxiWeatherCondition.HeavySnow, .4f, .2f, .3f)]
        [TestCase(TruckTaxiWeatherCondition.Blizzard, .5f, .3f, .4f)]
        public void ElapsedSnowUsesSavedClockRateAndTheActualCondition(TruckTaxiWeatherCondition condition,
            float background, float clearedCell, float shallowCell)
        {
            TruckTaxiEnvironmentSnapshot environment = Environment(condition);
            float[] depths = environment.snow.depths;
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings);
            Assert.AreEqual(background, environment.snow.backgroundDepth, .00001f);
            AssertDepths(environment.snow, clearedCell, shallowCell, .6f, .6f);
            Assert.AreSame(depths, environment.snow.depths);
            Assert.AreEqual(17, environment.snow.topology);
            Assert.AreEqual(.6f, environment.snow.maximumDepth);
        }

        [TestCase(TruckTaxiWeatherCondition.Clear)]
        [TestCase(TruckTaxiWeatherCondition.PartlyCloudy)]
        [TestCase(TruckTaxiWeatherCondition.Overcast)]
        [TestCase(TruckTaxiWeatherCondition.Rain)]
        [TestCase(TruckTaxiWeatherCondition.HeavyRain)]
        [TestCase(TruckTaxiWeatherCondition.Thunderstorm)]
        [TestCase(TruckTaxiWeatherCondition.Fog)]
        public void NonSnowWeatherMeltsGraduallyWithoutNegativeDepths(TruckTaxiWeatherCondition condition)
        {
            TruckTaxiEnvironmentSnapshot environment = Environment(condition);
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings);
            Assert.AreEqual(.15f, environment.snow.backgroundDepth, .00001f);
            AssertDepths(environment.snow, 0, .05f, .45f, .55f);
        }

        [TestCase(TruckTaxiWeatherCondition.Snow, .6f)]
        [TestCase(TruckTaxiWeatherCondition.HeavySnow, .6f)]
        [TestCase(TruckTaxiWeatherCondition.Blizzard, .6f)]
        [TestCase(TruckTaxiWeatherCondition.Clear, 0f)]
        [TestCase(TruckTaxiWeatherCondition.Rain, 0f)]
        public void LongAbsenceClampsEveryCellAndBackgroundToPhysicalBounds(TruckTaxiWeatherCondition condition, float expected)
        {
            TruckTaxiEnvironmentSnapshot environment = Environment(condition);
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600000, 60, settings);
            Assert.AreEqual(expected, environment.snow.backgroundDepth);
            AssertDepths(environment.snow, expected, expected, expected, expected);
            Assert.IsTrue(TruckTaxiSnowSnapshot.Valid(environment.snow));
        }

        [TestCase(0d)]
        [TestCase(-600d)]
        public void NoPositiveElapsedTimeLeavesSnowUnchanged(double elapsed)
        {
            TruckTaxiEnvironmentSnapshot environment = Environment(TruckTaxiWeatherCondition.Blizzard);
            TruckTaxiWorldVisit.AdvanceSnow(environment, elapsed, 60, settings);
            AssertUnchanged(environment);
        }

        [TestCase(0f)]
        [TestCase(-60f)]
        public void StoppedOrInvalidNegativeClockRateLeavesSnowUnchanged(float clockRate)
        {
            TruckTaxiEnvironmentSnapshot environment = Environment(TruckTaxiWeatherCondition.Blizzard);
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, clockRate, settings);
            AssertUnchanged(environment);
        }

        [Test]
        public void FrozenRegionDoesNotAdvanceItsSnowWhileAnotherRegionClockRuns()
        {
            TruckTaxiEnvironmentSnapshot environment = Environment(TruckTaxiWeatherCondition.Blizzard);
            environment.frozen = true;
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings);
            AssertUnchanged(environment);
        }

        [Test]
        public void ZeroAccumulationAndMeltRatesLeaveSnowUnchanged()
        {
            settings.lightSnowMetersPerSecond = settings.heavySnowMetersPerSecond = settings.blizzardMetersPerSecond = 0;
            settings.snowMeltMetersPerSecond = 0;
            foreach (TruckTaxiWeatherCondition condition in new[] { TruckTaxiWeatherCondition.Snow,
                TruckTaxiWeatherCondition.HeavySnow, TruckTaxiWeatherCondition.Blizzard, TruckTaxiWeatherCondition.Clear })
            {
                TruckTaxiEnvironmentSnapshot environment = Environment(condition);
                TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings);
                AssertUnchanged(environment);
            }
        }

        [Test]
        public void CatchupIsBoundedToOneDayOfRealEquivalentSeconds()
        {
            TruckTaxiEnvironmentSnapshot environment = Environment(TruckTaxiWeatherCondition.Snow);
            settings.lightSnowMetersPerSecond = .000001f;
            TruckTaxiWorldVisit.AdvanceSnow(environment, 10d * 86400 * 60, 60, settings);
            Assert.AreEqual(.2864f, environment.snow.backgroundDepth, .00001f);
            AssertDepths(environment.snow, .0864f, .1864f, .5864f, .6f);
        }

        [Test]
        public void MissingOptionalSnowOrSettingsIsANoOpAndMissingForecastUsesClearWeather()
        {
            Assert.DoesNotThrow(() => TruckTaxiWorldVisit.AdvanceSnow(null, 600, 60, settings));
            Assert.DoesNotThrow(() => TruckTaxiWorldVisit.AdvanceSnow(new TruckTaxiEnvironmentSnapshot(), 600, 60, settings));
            TruckTaxiEnvironmentSnapshot environment = Environment(TruckTaxiWeatherCondition.Snow);
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, null);
            AssertUnchanged(environment);
            environment.forecast = null;
            TruckTaxiWorldVisit.AdvanceSnow(environment, 600, 60, settings);
            Assert.AreEqual(.15f, environment.snow.backgroundDepth, .00001f);
            AssertDepths(environment.snow, 0, .05f, .45f, .55f);
        }

        [Test]
        public void SnowCatchupMutatesOnlyTheVisitedRegion()
        {
            TruckTaxiEnvironmentSnapshot demo = Environment(TruckTaxiWeatherCondition.Blizzard);
            TruckTaxiEnvironmentSnapshot edenborough = Environment(TruckTaxiWeatherCondition.Clear);
            TruckTaxiWorldVisit.AdvanceSnow(demo, 600, 60, settings);
            Assert.AreEqual(.5f, demo.snow.backgroundDepth, .00001f);
            AssertUnchanged(edenborough);
        }

        [TestCase(0f, 0f, 3f, true)]
        [TestCase(0f, 0f, -3f, false)]
        [TestCase(0f, 0f, 0f, false)]
        [TestCase(3f, 0f, 0f, false)]
        [TestCase(0f, 0f, .5f, false)]
        [TestCase(0f, 0f, .51f, true)]
        [TestCase(3f, 0f, 2f, true)]
        [TestCase(3f, 0f, 1f, false)]
        public void ExitRequiresActualForwardMotionAboveTheStationaryThreshold(float x, float y, float z, bool expected)
        {
            Assert.AreEqual(expected, TruckTaxiWorldExit.CorrectDirection(new Vector3(x, y, z), Vector3.forward));
        }

        [Test]
        public void ExitDirectionFollowsConnectorHeadingAndRejectsMissingHeading()
        {
            Assert.IsTrue(TruckTaxiWorldExit.CorrectDirection(Vector3.right * 3, Vector3.right * 20));
            Assert.IsFalse(TruckTaxiWorldExit.CorrectDirection(Vector3.left * 3, Vector3.right));
            Assert.IsFalse(TruckTaxiWorldExit.CorrectDirection(Vector3.forward * 3, Vector3.zero));
        }

        [Test]
        public void OldDemoSaveSceneAndFreePlayAliasRemainSupportedAlongsideEdenborough()
        {
            Assert.AreEqual("TruckTaxi_DemoCity", TruckTaxiMainMenu.VibinSceneName);
            Assert.AreEqual(TruckTaxiMainMenu.VibinSceneName, TruckTaxiMainMenu.FreePlaySceneName);
            Assert.IsTrue(TruckTaxiMainMenu.IsSupportedWorld("TruckTaxi_DemoCity"));
            Assert.IsTrue(TruckTaxiMainMenu.IsSupportedWorld(TruckTaxiEdenboroughLayout.SceneName));
            Assert.AreEqual(LwsWorldResumeCatalog.TruckTaxiStreamingWorldId, TruckTaxiWorldCatalog.WorldId(TruckTaxiWorldCatalog.Demo));
            Assert.AreEqual(TruckTaxiEdenboroughLayout.WorldId, TruckTaxiWorldCatalog.WorldId(TruckTaxiWorldCatalog.Edenborough));
        }

        [Test]
        public void LegacyDemoWorldIdentitiesDoNotResolveIntoEdenborough()
        {
            foreach (string worldId in new[] { LwsWorldResumeCatalog.TruckTaxiWorldId, LwsWorldResumeCatalog.TruckTaxiStreamingWorldId })
            {
                Assert.IsTrue(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld(worldId, TruckTaxiWorldCatalog.Demo));
                Assert.IsFalse(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld(worldId, TruckTaxiWorldCatalog.Edenborough));
            }
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld(TruckTaxiEdenboroughLayout.WorldId, TruckTaxiWorldCatalog.Edenborough));
            Assert.IsFalse(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld(TruckTaxiEdenboroughLayout.WorldId, TruckTaxiWorldCatalog.Demo));
        }

        private static TruckTaxiWorldVisit Visit(string scene = TruckTaxiWorldCatalog.Demo) =>
            new TruckTaxiWorldVisit { scene = scene, position = Vector3.zero, rotation = Quaternion.identity, gameSeconds = 0, clockRate = 60 };

        private static TruckTaxiEnvironmentSnapshot Environment(TruckTaxiWeatherCondition condition) =>
            new TruckTaxiEnvironmentSnapshot
            {
                hasSnowState = true,
                forecast = new TruckTaxiForecastSnapshot { manualCondition = condition },
                snow = new TruckTaxiSnowSnapshot { topology = 17, maximumDepth = .6f, backgroundDepth = .2f, depths = new[] { 0f, .1f, .5f, .6f } }
            };

        private static void AssertUnchanged(TruckTaxiEnvironmentSnapshot environment)
        {
            Assert.AreEqual(.2f, environment.snow.backgroundDepth);
            AssertDepths(environment.snow, 0, .1f, .5f, .6f);
        }

        private static void AssertDepths(TruckTaxiSnowSnapshot snow, params float[] expected)
        {
            Assert.AreEqual(expected.Length, snow.depths.Length);
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], snow.depths[i], .00001f, "Cell " + i);
        }

        private static IEnumerable<TestCaseData> NonFinitePositionCases => NonFiniteComponents(3);
        private static IEnumerable<TestCaseData> NonFiniteRotationCases => NonFiniteComponents(4);
        private static IEnumerable<TestCaseData> NonFiniteComponents(int axes)
        {
            for (int axis = 0; axis < axes; axis++)
                foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                    yield return new TestCaseData(axis, invalid);
        }
        private static IEnumerable<TruckTaxiWeatherCondition> DefinedWeatherConditions =>
            (TruckTaxiWeatherCondition[])Enum.GetValues(typeof(TruckTaxiWeatherCondition));
    }
}
