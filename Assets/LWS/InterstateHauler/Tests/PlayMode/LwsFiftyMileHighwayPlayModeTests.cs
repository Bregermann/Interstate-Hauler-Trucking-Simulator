using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsFiftyMileHighwayPlayModeTests
    {
        [UnityTest]
        public IEnumerator OriginOffsetValidationMethodKeepsGlobalMileStable()
        {
            LwsFloatingOriginTuning tuning = ScriptableObject.CreateInstance<LwsFloatingOriginTuning>();
            tuning.shiftThresholdMeters = 1000f;
            tuning.shiftGridMeters = 500f;
            tuning.shiftXAxis = true;
            tuning.shiftYAxis = false;
            tuning.shiftZAxis = true;
            tuning.minimumSecondsBetweenShifts = 0f;

            var service = new LwsWorldOriginService();
            service.Configure(tuning);
            Vector3 beforeLocal = LwsFiftyMileHighwayModel.EastboundLanePosition(
                LwsFiftyMileHighwayModel.MileToMeters(25d),
                0);
            LwsWorldPositionD beforeGlobal = service.LocalToGlobal(beforeLocal);

            Assert.IsTrue(service.SetOriginOffsetForValidation(new LwsWorldPositionD(0d, 0d, 40000d), "playmode teleport", out _));
            Vector3 afterLocal = service.GlobalToLocal(beforeGlobal);
            LwsWorldPositionD afterGlobal = service.LocalToGlobal(afterLocal);

            Assert.That(afterLocal.z, Is.EqualTo(233.6f).Within(0.01f));
            Assert.That(afterGlobal.z, Is.EqualTo(beforeGlobal.z).Within(0.01d));
            Assert.That(LwsFiftyMileHighwayModel.MetersToMile(afterGlobal.z), Is.EqualTo(25d).Within(0.001d));

            Object.DestroyImmediate(tuning);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StreamingPolicyKeepsFiftyMileNeighborhoodBounded()
        {
            LwsWorldStreamingPolicy policy = ScriptableObject.CreateInstance<LwsWorldStreamingPolicy>();
            policy.loadAheadDistanceMeters = 7000f;
            policy.keepBehindDistanceMeters = 3800f;
            policy.preloadMarginMeters = 500f;
            policy.unloadDistanceMeters = 10000f;
            policy.maximumConcurrentLoads = 3;
            policy.minimumLoadedNeighborCount = 1;

            LwsWorldStreamingManifest manifest = LwsFiftyMileHighwayModel.CreateManifest(policy);
            Vector3 mile25 = LwsFiftyMileHighwayModel.EastboundLanePosition(
                LwsFiftyMileHighwayModel.MileToMeters(25d),
                0);
            var anchor = new LwsWorldStreamingAnchorState(
                mile25,
                LwsWorldPositionD.FromVector3(mile25),
                Vector3.forward,
                25f,
                false,
                mile25,
                LwsWorldPositionD.FromVector3(mile25));

            LwsWorldStreamingPolicyDecision decision = LwsWorldStreamingService.BuildPolicyDecision(manifest, policy, anchor);

            Assert.AreEqual("IH_50MI_CHUNK_012", decision.CurrentChunkId);
            Assert.That(decision.DesiredChunkIds.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(decision.DesiredChunkIds.Count, Is.LessThan(LwsFiftyMileHighwayModel.ChunkCount));
            Assert.Contains("IH_50MI_CHUNK_012", new System.Collections.Generic.List<string>(decision.DesiredChunkIds));

            Object.DestroyImmediate(manifest);
            Object.DestroyImmediate(policy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RoadGraphProviderAcceptsFiftyMileGraph()
        {
            GameObject owner = new GameObject("50-mile-road-graph-provider-test");
            LwsRoadGraphProvider provider = owner.AddComponent<LwsRoadGraphProvider>();
            LwsRoadGraph graph = LwsFiftyMileHighwayModel.CreateRoadGraph();

            provider.SetGraph(graph, true);

            Assert.AreSame(graph, provider.Graph);
            Assert.AreEqual(LwsFiftyMileHighwayModel.GraphId, provider.Graph.graphId);
            Assert.AreEqual((float)LwsFiftyMileHighwayModel.TotalLengthMeters, provider.Graph.edges[0].distanceMeters);

            Object.Destroy(owner);
            yield return null;
        }
    }
}
