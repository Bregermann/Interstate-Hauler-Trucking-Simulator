using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsFloatingOriginPlayModeTests
    {
        [Test]
        public void DefaultRegistryContainsOneWorldOriginService()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();
            int count = registry.Registrations.Count(r => r.ServiceType == typeof(ILwsWorldOriginService));
            Assert.AreEqual(1, count);
            Assert.IsTrue(registry.TryGet(out ILwsWorldOriginService originService));
            Assert.AreEqual("lws.world.origin", originService.ServiceId);
        }

        [UnityTest]
        public IEnumerator RegisteredRootsShiftTogetherAndUnregisteredRootsDoNotMove()
        {
            LwsFloatingOriginTuning tuning = CreateTuning();
            var service = new LwsWorldOriginService();
            service.Configure(tuning);

            GameObject first = CreateParticipant("first", new Vector3(900f, 0f, 0f), service);
            GameObject second = CreateParticipant("second", new Vector3(900f, 0f, 25f), service);
            GameObject manager = new GameObject("non-spatial-manager");
            manager.transform.position = new Vector3(900f, 0f, 50f);

            service.QueueShift(new LwsOriginShiftRequest(new Vector3(500f, 0f, 0f), "playmode"));
            service.TryExecuteQueuedShift(out LwsOriginShiftEvent shiftEvent);
            yield return null;

            Assert.That(first.transform.position.x, Is.EqualTo(400f).Within(0.001f));
            Assert.That(second.transform.position.x, Is.EqualTo(400f).Within(0.001f));
            Assert.That(manager.transform.position.x, Is.EqualTo(900f).Within(0.001f));
            Assert.That(shiftEvent.ParticipantsShifted, Is.EqualTo(2));

            Object.Destroy(first);
            Object.Destroy(second);
            Object.Destroy(manager);
            Object.Destroy(tuning);
        }

        [UnityTest]
        public IEnumerator RigidbodyParticipantPreservesVelocityAndAngularVelocity()
        {
            LwsFloatingOriginTuning tuning = CreateTuning();
            var service = new LwsWorldOriginService();
            service.Configure(tuning);

            GameObject bodyObject = new GameObject("rigidbody-participant");
            bodyObject.transform.position = new Vector3(900f, 0f, 0f);
            Rigidbody body = bodyObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearVelocity = new Vector3(12f, 0f, 2f);
            body.angularVelocity = new Vector3(0.1f, 0.2f, 0.3f);
            LwsFloatingOriginRigidbodyParticipant participant = bodyObject.AddComponent<LwsFloatingOriginRigidbodyParticipant>();
            participant.Configure("rigidbody", LwsFloatingOriginParticipantKind.PlayerTractor, false);
            service.RegisterParticipant(participant);

            service.QueueShift(new LwsOriginShiftRequest(new Vector3(500f, 0f, 0f), "velocity"));
            service.TryExecuteQueuedShift(out _);
            yield return new WaitForFixedUpdate();

            Assert.That(bodyObject.transform.position.x, Is.EqualTo(400f).Within(0.001f));
            Assert.That(body.linearVelocity.x, Is.EqualTo(12f).Within(0.001f));
            Assert.That(body.linearVelocity.z, Is.EqualTo(2f).Within(0.001f));
            Assert.That(body.angularVelocity.y, Is.EqualTo(0.2f).Within(0.001f));

            Object.Destroy(bodyObject);
            Object.Destroy(tuning);
        }

        [Test]
        public void NewParticipantCanAlignToExistingOriginOffset()
        {
            LwsFloatingOriginTuning tuning = CreateTuning();
            var service = new LwsWorldOriginService();
            service.Configure(tuning);
            service.ForceShiftNow(new Vector3(500f, 0f, 0f), "establish offset", out _);

            GameObject chunkRoot = new GameObject("chunk-root");
            chunkRoot.transform.position = Vector3.zero;
            LwsFloatingOriginTransformParticipant participant = chunkRoot.AddComponent<LwsFloatingOriginTransformParticipant>();
            participant.Configure("chunk.test", LwsFloatingOriginParticipantKind.LoadedChunkRoot, true);

            service.RegisterParticipant(participant);

            Assert.That(chunkRoot.transform.position.x, Is.EqualTo(-500f).Within(0.001f));
            Object.DestroyImmediate(chunkRoot);
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void StreamingPolicyUsesContinuousGlobalAnchor()
        {
            LwsWorldStreamingManifest manifest = ScriptableObject.CreateInstance<LwsWorldStreamingManifest>();
            LwsWorldStreamingPolicy policy = ScriptableObject.CreateInstance<LwsWorldStreamingPolicy>();
            policy.loadAheadDistanceMeters = 700f;
            policy.keepBehindDistanceMeters = 300f;
            policy.preloadMarginMeters = 0f;
            policy.minimumLoadedNeighborCount = 0;
            manifest.policy = policy;
            manifest.chunks.Add(CreateChunk("start", 0f));
            manifest.chunks.Add(CreateChunk("far", 1250f));

            var anchor = new LwsWorldStreamingAnchorState(
                new Vector3(250f, 0f, 0f),
                new LwsWorldPositionD(1250d, 0d, 0d),
                Vector3.forward,
                0f,
                false,
                Vector3.zero,
                LwsWorldPositionD.Zero);

            LwsWorldStreamingPolicyDecision decision = LwsWorldStreamingService.BuildPolicyDecision(manifest, policy, anchor);

            Assert.AreEqual("far", decision.CurrentChunkId);
            Object.DestroyImmediate(manifest);
            Object.DestroyImmediate(policy);
        }

        [Test]
        public void OriginShiftEventsFireInOrder()
        {
            LwsFloatingOriginTuning tuning = CreateTuning();
            var service = new LwsWorldOriginService();
            service.Configure(tuning);
            int order = 0;
            int startingOrder = 0;
            int completedOrder = 0;

            service.OriginShiftStarting += _ => startingOrder = ++order;
            service.OriginShiftCompleted += _ => completedOrder = ++order;
            service.QueueShift(new LwsOriginShiftRequest(new Vector3(500f, 0f, 0f), "events"));
            service.TryExecuteQueuedShift(out _);

            Assert.AreEqual(1, startingOrder);
            Assert.AreEqual(2, completedOrder);
            Assert.AreEqual(1, service.OriginVersion);
            Object.DestroyImmediate(tuning);
        }

        private static GameObject CreateParticipant(string id, Vector3 position, ILwsWorldOriginService service)
        {
            GameObject go = new GameObject(id);
            go.transform.position = position;
            LwsFloatingOriginTransformParticipant participant = go.AddComponent<LwsFloatingOriginTransformParticipant>();
            participant.Configure(id, LwsFloatingOriginParticipantKind.GenericSpatialRoot, false);
            service.RegisterParticipant(participant);
            return go;
        }

        private static LwsWorldChunkDefinition CreateChunk(string id, float centerX)
        {
            return new LwsWorldChunkDefinition
            {
                chunkId = id,
                sceneName = id,
                scenePath = $"Assets/{id}.unity",
                boundsCenter = new Vector3(centerX, 0f, 0f),
                boundsSize = new Vector3(500f, 100f, 500f)
            };
        }

        private static LwsFloatingOriginTuning CreateTuning()
        {
            LwsFloatingOriginTuning tuning = ScriptableObject.CreateInstance<LwsFloatingOriginTuning>();
            tuning.shiftThresholdMeters = 600f;
            tuning.shiftGridMeters = 500f;
            tuning.shiftXAxis = true;
            tuning.shiftYAxis = false;
            tuning.shiftZAxis = true;
            tuning.minimumSecondsBetweenShifts = 0f;
            return tuning;
        }
    }
}
