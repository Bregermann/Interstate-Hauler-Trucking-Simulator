using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class EdenboroughTrafficDataTests
    {
        private EdenboroughTrafficData data;

        [SetUp]
        public void SetUp()
        {
            data = ScriptableObject.CreateInstance<EdenboroughTrafficData>();
            data.graphId = "EDENBOROUGH_TEST";
            data.lanes.Add(Lane("a", 0f));
            data.lanes.Add(Lane("b", 4f));
            data.turns.Add(new EdenboroughTurnBinding
            {
                edgeId = "a->b", fromLaneId = "a", toLaneId = "b",
                centerline = new[] { new Vector3(2f, 0f, 0f), new Vector3(4f, 0f, 0f) }
            });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(data);

        [Test]
        public void OrderedLegalRouteUsesExistingLwsLaneContract()
        {
            Assert.IsTrue(data.TryComposeRoute(new[] { "a", "b" }, false, out var route, out var message), message);
            Assert.IsTrue(route.Validate(out message), message);
            Assert.AreEqual(6f, route.lengthMeters, .001f);
            Assert.AreEqual(6, route.centerline.Length);
            Assert.IsFalse(data.lanes[0].spawnEnabled);
            Assert.IsTrue(route.spawnEnabled);
        }

        [Test]
        public void MissingReverseTurnDoesNotBecomeImplicitUtsLoop()
        {
            Assert.IsFalse(data.TryComposeRoute(new[] { "a", "b" }, true, out _, out var message));
            StringAssert.Contains("No legal authored transition", message);
            Assert.IsFalse(data.TryComposeRoute(new[] { "b", "a" }, false, out _, out _));
        }

        [Test]
        public void DisconnectedTurnAndEmptyLaneIdAreRejected()
        {
            data.turns[0].centerline = new[] { Vector3.zero, Vector3.one };
            Assert.IsFalse(data.TryComposeRoute(new[] { "a", "b" }, false, out _, out var message));
            StringAssert.Contains("Disconnected", message);
            Assert.IsFalse(data.TryComposeRoute(new string[] { null }, false, out _, out _));
        }

        [Test]
        public void RawWayTagsRemainAvailableToTrafficAdapter()
        {
            var way = new EdenboroughWayBinding
            {
                osmWayId = 20,
                tags = new[] { new EdenboroughOsmTag { key = "hgv", value = "destination" } }
            };
            Assert.AreEqual("destination", way.Tag("hgv"));
            Assert.AreEqual(string.Empty, way.Tag("missing"));
        }

        private static LwsTrafficLaneDefinition Lane(string id, float start) => new LwsTrafficLaneDefinition
        {
            laneId = id, roadId = id, segmentId = id, edgeId = id, laneIndex = 0,
            roadClass = LwsRoadClass.LocalRoad, direction = LwsRoadDirection.Eastbound,
            speedLimitMph = 20f, laneWidthMeters = 3.4f, lengthMeters = 2f, spawnEnabled = false,
            centerline = new[] { new Vector3(start, 0f, 0f), new Vector3(start + 1f, 0f, 0f),
                new Vector3(start + 2f, 0f, 0f) }
        };
    }
}
