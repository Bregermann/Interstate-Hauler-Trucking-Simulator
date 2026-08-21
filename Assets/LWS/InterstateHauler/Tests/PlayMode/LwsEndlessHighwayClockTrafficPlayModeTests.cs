using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsEndlessHighwayClockTrafficPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LwsApplicationBootstrap.ResetForTests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (LwsApplicationBootstrap.Instance != null)
            {
                Object.Destroy(LwsApplicationBootstrap.Instance.gameObject);
            }

            Object.Destroy(GameObject.Find("IH Endless Streaming Highway Runtime"));
            LwsApplicationBootstrap.ResetForTests();
            yield return null;
        }

        [Test]
        public void DefaultRegistryContainsClockAndTrafficDemandServices()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();

            Assert.IsTrue(registry.TryGet(out ILwsGameClockService clock));
            Assert.IsTrue(registry.TryGet(out ILwsTrafficDemandService demand));
            Assert.AreEqual("lws.game.clock", clock.ServiceId);
            Assert.AreEqual("lws.traffic.demand", demand.ServiceId);
        }

        [UnityTest]
        public IEnumerator EndlessControllerRecyclesBeyondOriginalFiniteChunkCount()
        {
            GameObject bootstrapObject = new GameObject("endless-test-bootstrap");
            bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            GameObject controllerObject = new GameObject("endless-test-controller");
            LwsRoadGraphProvider provider = controllerObject.AddComponent<LwsRoadGraphProvider>();
            LwsEndlessStreamingHighwayController endless = controllerObject.AddComponent<LwsEndlessStreamingHighwayController>();
            endless.Configure(provider);

            yield return null;

            double playerZ = LwsEndlessHighwayModel.SegmentLengthMeters * 8.25d;
            endless.RefreshForPlayerGlobalZ(playerZ, true);

            Assert.GreaterOrEqual(endless.CurrentLogicalSegmentIndex, 8);
            Assert.Greater(endless.HighestSegmentGenerated, 10);
            Assert.Greater(endless.MetersOfRoadAvailableAhead, endless.EmergencyRoadAheadThresholdMeters);
            Assert.IsNotNull(endless.LastGraph);
            Assert.IsTrue(endless.LastGraph.Validate().IsValid, endless.LastGraph.Validate().Summary);

            Object.Destroy(controllerObject);
            Object.Destroy(bootstrapObject);
        }

        [UnityTest]
        public IEnumerator GameClockCoordinatorTicksWithoutChangingUnityTimeScale()
        {
            GameObject bootstrapObject = new GameObject("clock-test-bootstrap");
            bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            GameObject coordinatorObject = new GameObject("clock-test-coordinator");
            coordinatorObject.AddComponent<LwsGameClockCoordinator>();
            yield return null;

            Assert.IsTrue(LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsGameClockService clock));
            clock.SetTimeScale(60f);
            float unityTimeScale = Time.timeScale;
            float before = clock.CurrentSnapshot.timeOfDayHours;
            yield return new WaitForSecondsRealtime(0.05f);

            Assert.AreEqual(unityTimeScale, Time.timeScale);
            Assert.Greater(clock.CurrentSnapshot.timeOfDayHours, before);

            Object.Destroy(coordinatorObject);
            Object.Destroy(bootstrapObject);
        }

        [Test]
        public void TrafficDemandServiceRespondsToClockTime()
        {
            var registry = new LwsServiceRegistry();
            var clock = new LwsGameClockService();
            var demand = new LwsTrafficDemandService();
            registry.Register<ILwsGameClockService>(clock);
            registry.Register<ILwsTrafficDemandService>(demand, typeof(ILwsGameClockService));
            Assert.IsTrue(registry.InitializeAll().Succeeded);

            clock.SetTimeOfDayHours(3f);
            LwsTrafficDemandSnapshot night = demand.EvaluateDemand(clock.CurrentSnapshot, 0, 0, 0, 0, 0f, -1f, -1f);
            clock.SetTimeOfDayHours(8f);
            LwsTrafficDemandSnapshot morning = demand.EvaluateDemand(clock.CurrentSnapshot, 0, 0, 0, 0, 0f, -1f, -1f);
            clock.SetTimeOfDayHours(17f);
            LwsTrafficDemandSnapshot evening = demand.EvaluateDemand(clock.CurrentSnapshot, 0, 0, 0, 0, 0f, -1f, -1f);

            Assert.Less(night.SmoothedTargetActive, morning.SmoothedTargetActive);
            Assert.LessOrEqual(morning.SmoothedTargetActive, evening.SmoothedTargetActive);
            registry.ShutdownAll();
        }
    }
}
