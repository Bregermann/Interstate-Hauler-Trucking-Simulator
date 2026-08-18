using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsFloatingOriginEditModeTests
    {
        [Test]
        public void LocalGlobalConversionsPreserveLargeOffsets()
        {
            var offset = new LwsWorldPositionD(3_000_000.25d, 0d, -1_250_000.75d);
            var local = new Vector3(125.5f, 8f, -42.25f);

            LwsWorldPositionD global = LwsWorldCoordinateUtility.LocalToGlobal(local, offset);
            Vector3 roundTrip = LwsWorldCoordinateUtility.GlobalToLocal(global, offset);

            Assert.That(global.x, Is.EqualTo(3_000_125.75d).Within(0.0001d));
            Assert.That(global.z, Is.EqualTo(-1_250_043d).Within(0.0001d));
            Assert.That(roundTrip.x, Is.EqualTo(local.x).Within(0.001f));
            Assert.That(roundTrip.y, Is.EqualTo(local.y).Within(0.001f));
            Assert.That(roundTrip.z, Is.EqualTo(local.z).Within(0.001f));
        }

        [Test]
        public void GridAlignedShiftUsesValidationXzPolicy()
        {
            LwsFloatingOriginTuning tuning = CreateTuning(600f, 500f);

            Vector3 shift = LwsWorldCoordinateUtility.CalculateGridAlignedShift(
                new Vector3(1325f, 42f, 950f),
                tuning);

            Assert.AreEqual(new Vector3(1000f, 0f, 500f), shift);
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void OriginShiftKeepsGlobalPositionStableAndIncrementsVersionOnce()
        {
            LwsFloatingOriginTuning tuning = CreateTuning(600f, 500f);
            var service = new LwsWorldOriginService();
            service.Configure(tuning);

            GameObject root = new GameObject("origin-shift-root");
            root.transform.position = new Vector3(1325f, 0f, 0f);
            LwsFloatingOriginTransformParticipant participant = root.AddComponent<LwsFloatingOriginTransformParticipant>();
            participant.Configure("test.root", LwsFloatingOriginParticipantKind.GenericSpatialRoot, false);
            service.RegisterParticipant(participant);

            LwsWorldPositionD beforeGlobal = service.LocalToGlobal(root.transform.position);
            Assert.IsTrue(service.QueueShift(new LwsOriginShiftRequest(new Vector3(1000f, 0f, 0f), "test")));
            Assert.IsTrue(service.TryExecuteQueuedShift(out LwsOriginShiftEvent shiftEvent));
            LwsWorldPositionD afterGlobal = service.LocalToGlobal(root.transform.position);

            Assert.That(root.transform.position.x, Is.EqualTo(325f).Within(0.001f));
            Assert.That(service.CurrentOriginOffset.x, Is.EqualTo(1000d).Within(0.001d));
            Assert.That(service.OriginVersion, Is.EqualTo(1));
            Assert.That(shiftEvent.ParticipantsShifted, Is.EqualTo(1));
            Assert.IsTrue(beforeGlobal.ApproximatelyEquals(afterGlobal, 0.001d));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void ThousandSimulatedShiftsAccumulateInDoublePrecision()
        {
            LwsFloatingOriginTuning tuning = CreateTuning(1f, 1f);
            tuning.minimumSecondsBetweenShifts = 0f;
            var service = new LwsWorldOriginService();
            service.Configure(tuning);

            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(service.QueueShift(new LwsOriginShiftRequest(new Vector3(500f, 0f, 0f), "simulation")));
                Assert.IsTrue(service.TryExecuteQueuedShift(out _));
            }

            Assert.That(service.CurrentOriginOffset.x, Is.EqualTo(500_000d).Within(0.001d));
            Assert.That(service.OriginVersion, Is.EqualTo(1000));
            Assert.That(service.ShiftCount, Is.EqualTo(1000));
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void ChunkCanonicalBoundsRemainGlobalWhileLocalBoundsDeriveFromOffset()
        {
            var chunk = new LwsWorldChunkDefinition
            {
                chunkId = "chunk.global",
                sceneName = "ChunkGlobal",
                scenePath = "Assets/ChunkGlobal.unity",
                boundsCenter = new Vector3(0f, 0f, 1500f),
                boundsSize = new Vector3(100f, 100f, 500f)
            };

            Bounds localBounds = chunk.GetLocalBounds(new LwsWorldPositionD(0d, 0d, 1000d));

            Assert.That(chunk.boundsCenter.z, Is.EqualTo(1500f).Within(0.001f));
            Assert.That(localBounds.center.z, Is.EqualTo(500f).Within(0.001f));
        }

        [Test]
        public void StreamingRoadIdsRemainStableAcrossOriginMath()
        {
            LwsRoadGraph graph = LwsStreamingHighwayGraphBootstrap.CreateDefaultGraph();
            var offset = new LwsWorldPositionD(0d, 0d, 2500d);
            Vector3 local = LwsWorldCoordinateUtility.GlobalToLocal(
                LwsWorldPositionD.FromVector3(graph.edges[0].samples[0].position),
                offset);
            LwsWorldPositionD global = LwsWorldCoordinateUtility.LocalToGlobal(local, offset);

            Assert.AreEqual("IH_TEST_I000_NB", graph.edges[0].roadId);
            Assert.AreEqual("IH_TEST_I000_NB_MAIN_EDGE", graph.edges[0].edgeId);
            Assert.That(global.z, Is.EqualTo(graph.edges[0].samples[0].position.z).Within(0.001d));
        }

        private static LwsFloatingOriginTuning CreateTuning(float threshold, float grid)
        {
            LwsFloatingOriginTuning tuning = ScriptableObject.CreateInstance<LwsFloatingOriginTuning>();
            tuning.shiftThresholdMeters = threshold;
            tuning.shiftGridMeters = grid;
            tuning.shiftXAxis = true;
            tuning.shiftYAxis = false;
            tuning.shiftZAxis = true;
            tuning.minimumSecondsBetweenShifts = 0f;
            return tuning;
        }
    }
}
