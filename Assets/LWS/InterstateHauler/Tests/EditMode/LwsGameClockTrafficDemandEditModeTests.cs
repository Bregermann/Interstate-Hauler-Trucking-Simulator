using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsGameClockTrafficDemandEditModeTests
    {
        [Test]
        public void GameClockAdvancesAccordingToGameTimeScale()
        {
            LwsGameClockService clock = CreateClock();
            clock.SetTimeScale(20f);

            clock.Tick(3f);

            Assert.That(clock.CurrentSnapshot.timeOfDayHours, Is.EqualTo(8f + 60f / 3600f).Within(0.0005f));
            Assert.That(clock.CurrentSnapshot.totalGameSeconds, Is.EqualTo(60d).Within(0.001d));
        }

        [Test]
        public void PausedGameClockDoesNotAdvance()
        {
            LwsGameClockService clock = CreateClock();
            clock.SetTimeScale(60f);
            clock.SetPaused(true);

            clock.Tick(30f);

            Assert.That(clock.CurrentSnapshot.timeOfDayHours, Is.EqualTo(8f).Within(0.0005f));
            Assert.IsTrue(clock.CurrentSnapshot.paused);
        }

        [Test]
        public void GameClockRollsOverToNextDay()
        {
            LwsGameClockService clock = CreateClock();
            clock.SetDateTime(new LwsGameDateTime(2026, 6, 1, 23, 59, 30f));
            clock.SetTimeScale(1f);

            clock.Tick(60f);

            Assert.AreEqual(2, clock.CurrentSnapshot.day);
            Assert.AreEqual(0, clock.CurrentSnapshot.hour);
            Assert.AreEqual(0, clock.CurrentSnapshot.minute);
        }

        [Test]
        public void TimeOfDaySetWrapsWithinDay()
        {
            LwsGameClockService clock = CreateClock();

            clock.SetTimeOfDayHours(25f);

            Assert.AreEqual(1, clock.CurrentSnapshot.hour);
            Assert.AreEqual(1, clock.CurrentSnapshot.day);
        }

        [Test]
        public void TrafficDemandCurveOrdersNightDayAndPeak()
        {
            LwsTrafficDemandProfile profile = LwsTrafficDemandProfile.CreateGenericInterstate();

            float overnight = profile.EvaluateDemand01(3f);
            float daytime = profile.EvaluateDemand01(13f);
            float peak = profile.EvaluateDemand01(17f);

            Assert.Less(overnight, daytime);
            Assert.Less(daytime, peak);
        }

        [Test]
        public void TrafficDemandClampsTargetToConfiguredMaximum()
        {
            LwsTrafficDemandProfile profile = LwsTrafficDemandProfile.CreateGenericInterstate();
            profile.maximumActiveVehicles = 12;

            int target = profile.EvaluateTargetActiveVehicles(17f);

            Assert.LessOrEqual(target, 12);
            Assert.GreaterOrEqual(target, profile.minimumActiveVehicles);
        }

        [Test]
        public void EndlessSegmentIdsAndPositionsRemainStable()
        {
            string segmentId = LwsEndlessHighwayModel.FormatSegmentId(42);
            Vector3 local = LwsEndlessHighwayModel.CalculateLocalSegmentPosition(42, new LwsWorldPositionD(0d, 0d, 1000d));

            Assert.AreEqual("IH_ENDLESS_TEST_SEG_000042", segmentId);
            Assert.That(local.z, Is.EqualTo((float)(42 * LwsEndlessHighwayModel.SegmentLengthMeters - 1000d)).Within(0.01f));
        }

        [Test]
        public void RoadAheadSafetyThresholdReportsUnsafe()
        {
            float ahead = LwsEndlessHighwayModel.CalculateMetersOfRoadAhead(9000d, 2);

            Assert.IsTrue(LwsEndlessHighwayModel.IsRoadAheadUnsafe(ahead, 3000f));
        }

        [Test]
        public void EndlessGraphWindowValidatesBeyondOriginalFiveChunks()
        {
            LwsRoadGraph graph = LwsEndlessHighwayModel.BuildRoadGraph(8, 7);

            LwsRoadGraphValidationResult validation = graph.Validate();

            Assert.IsTrue(validation.IsValid, validation.Summary);
            Assert.AreEqual(14, graph.edges.Count);
            Assert.IsTrue(graph.graphId.Contains("000008"));
        }

        private static LwsGameClockService CreateClock()
        {
            var registry = new LwsServiceRegistry();
            var clock = new LwsGameClockService();
            registry.Register<ILwsGameClockService>(clock);
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            return clock;
        }
    }
}
