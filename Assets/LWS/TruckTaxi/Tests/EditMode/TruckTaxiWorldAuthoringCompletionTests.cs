using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiWorldAuthoringCompletionTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        [TearDown] public void TearDown()
        { foreach (var item in objects) if (item != null) UnityEngine.Object.DestroyImmediate(item); objects.Clear(); }
        private GameObject Make(string name)
        { var go = new GameObject(name); objects.Add(go); return go; }
        private GameObject Art()
        { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(go); return go; }

        [Test]
        public void CompositionUsesReferencesAndDoesNotDuplicateComponents()
        {
            var facility = Art();
            var location = Make("Authored location").AddComponent<TruckTaxiRideLocation>();
            location.locationId = "audit.test.bay"; location.locationName = "Test bay";
            location.truckStopPoint = Make("Actual stopping anchor").transform;
            var caps = TruckTaxiServiceCapability.Refuel | TruckTaxiServiceCapability.Store | TruckTaxiServiceCapability.Restroom;
            var first = TruckTaxiServiceMapValidator.ComposeService(facility, location, caps);
            var again = TruckTaxiServiceMapValidator.ComposeService(facility, location, caps);
            Assert.AreSame(first, again);
            Assert.AreSame(location, first.location);
            Assert.AreSame(location, facility.GetComponent<TruckTaxiBathroomPoint>().location);
            Assert.AreSame(location, facility.GetComponent<TruckTaxiStorePoint>().location);
            Assert.AreSame(first, facility.GetComponent<TruckTaxiMapMarker>().service);
            Assert.AreEqual(TruckTaxiServicePresentation.MarkerType(caps), facility.GetComponent<TruckTaxiMapMarker>().markerType);
            Assert.AreSame(location.truckStopPoint, facility.GetComponent<TruckTaxiMapMarker>().source);
            Assert.AreEqual(1, facility.GetComponents<TruckTaxiGasStationPoint>().Length);
            Assert.IsFalse(first.generateValidationFacility);
        }

        [Test]
        public void MissingAnchorFailsBeforeAddingService()
        {
            var facility = Art();
            var location = Make("Missing bay").AddComponent<TruckTaxiRideLocation>(); location.locationId = "audit.missing";
            Assert.Throws<InvalidOperationException>(() => TruckTaxiServiceMapValidator.ComposeService(facility, location,
                TruckTaxiServiceCapability.Store));
            Assert.IsNull(facility.GetComponent<TruckTaxiServicePoint>());
        }

        [Test]
        public void DuplicateMapIdentityFailsBeforeAddingService()
        {
            var facility = Art();
            var location = Make("Authored bay").AddComponent<TruckTaxiRideLocation>();
            location.locationId = "audit.duplicate"; location.truckStopPoint = location.transform;
            var marker = Make("Other map owner").AddComponent<TruckTaxiMapMarker>();
            marker.stableId = "service." + location.locationId;
            Assert.Throws<InvalidOperationException>(() => TruckTaxiServiceMapValidator.ComposeService(facility, location,
                TruckTaxiServiceCapability.Store));
            Assert.IsNull(facility.GetComponent<TruckTaxiServicePoint>());
        }

        [Test]
        public void NonFiniteRoadPositionIsRejectedBeforePlanning()
        {
            var graph = Graph(); graph.nodes[0].position = new Vector3(float.NaN, 0, 0);
            var findings = TruckTaxiWorldColliderValidator.ValidateRoutesAndSeams(graph, Array.Empty<TruckTaxiRegion>(),
                Array.Empty<(string id, Vector3 position)>());
            Assert.IsTrue(findings.Any(f => f.Contains("non-finite")));
        }

        [Test]
        public void RouteAuditFindsDisconnectedNodeAndInteriorStreamingGap()
        {
            var graph = Graph();
            graph.nodes.Add(new LwsRoadNode { nodeId = "isolated", position = Vector3.forward * 100 });
            var regions = new[] {
                new TruckTaxiRegion { id = "left", bounds = new Bounds(Vector3.zero, new Vector3(20, 50, 50)) },
                new TruckTaxiRegion { id = "right", bounds = new Bounds(Vector3.right * 100, new Vector3(20, 50, 50)) } };
            var findings = TruckTaxiWorldColliderValidator.ValidateRoutesAndSeams(graph, regions,
                Array.Empty<(string id, Vector3 position)>());
            Assert.IsTrue(findings.Any(f => f.Contains("ROUTE DISCONNECTED") && f.Contains("isolated")));
            Assert.IsTrue(findings.Any(f => f.Contains("STREAM SEAM UNCOVERED")));
        }

        [Test]
        public void RoadEndpointMismatchIsReportedWithoutRepair()
        {
            var graph = Graph(); graph.edges[0].samples[0].position = Vector3.up;
            var findings = TruckTaxiWorldColliderValidator.ValidateRoutesAndSeams(graph, Array.Empty<TruckTaxiRegion>(),
                Array.Empty<(string id, Vector3 position)>());
            Assert.IsTrue(findings.Any(f => f.Contains("ROAD ENDPOINT SEAM")));
            Assert.AreEqual(Vector3.up, graph.edges[0].samples[0].position);
        }

        [Test]
        public void EmptyRendererDoesNotCountAsReferencedArt()
        {
            var facility = Make("Empty renderer"); facility.AddComponent<MeshRenderer>();
            var location = Make("Bay").AddComponent<TruckTaxiRideLocation>();
            location.locationId = "audit.noart"; location.truckStopPoint = location.transform;
            Assert.Throws<InvalidOperationException>(() => TruckTaxiServiceMapValidator.ComposeService(facility, location,
                TruckTaxiServiceCapability.Store));
            Assert.IsNull(facility.GetComponent<TruckTaxiServicePoint>());
        }

        [TestCase(2f, "FLOATING PROP CANDIDATE")]
        [TestCase(-.3f, "BURIED PROP CANDIDATE")]
        public void ScopedSupportAuditReportsWithoutMovingProp(float height, string expected)
        {
            var ground = Art(); ground.transform.position = Vector3.down * .5f;
            ground.transform.localScale = new Vector3(10, 1, 10);
            var propObject = Art(); propObject.transform.position = Vector3.up * height;
            propObject.AddComponent<TruckTaxiBreakawayProp>();
            Physics.SyncTransforms();
            var findings = TruckTaxiWorldColliderValidator.ValidatePlacement(new[] { ground, propObject });
            Assert.IsTrue(findings.Any(f => f.Contains(expected)));
            Assert.AreEqual(Vector3.up * height, propObject.transform.position);
        }

        [Test]
        public void ExplicitInteractionApproachReportsBlockingGeometry()
        {
            var desk = Make("Motel desk").AddComponent<TruckTaxiMotelDesk>();
            desk.SetStableId("audit.desk"); desk.interactionAnchor = Make("Approach anchor").transform;
            var obstruction = Art(); obstruction.transform.position = Vector3.up;
            Physics.SyncTransforms();
            var findings = TruckTaxiWorldColliderValidator.ValidatePlacement(new[] { desk.gameObject, obstruction });
            Assert.IsTrue(findings.Any(f => f.Contains("ENTRANCE CLEARANCE CANDIDATE") && f.Contains("audit.desk")));
        }

        [Test]
        public void InvalidLoadBearingClassificationIsReportOnly()
        {
            var go = Make("Load bearing prop"); var prop = go.AddComponent<TruckTaxiBreakawayProp>();
            prop.partRole = TruckTaxiPropPartRole.LoadBearing; prop.category = TruckTaxiPropPhysicsCategory.Destructible;
            var findings = TruckTaxiWorldColliderValidator.ValidatePlacement(new[] { go });
            Assert.IsTrue(findings.Any(f => f.Contains("INVALID BREAKAWAY CLASSIFICATION")));
            Assert.AreEqual(TruckTaxiPropPhysicsCategory.Destructible, prop.category);
        }

        private static LwsRoadGraph Graph() => new LwsRoadGraph { graphId = "audit.graph",
            nodes = new List<LwsRoadNode> { new LwsRoadNode { nodeId = "a" }, new LwsRoadNode { nodeId = "b", position = Vector3.right * 100 } },
            edges = new List<LwsRoadEdge> { new LwsRoadEdge { edgeId = "ab", fromNodeId = "a", toNodeId = "b", distanceMeters = 100,
                samples = new List<LwsRoadSample> { new LwsRoadSample { forward = Vector3.right },
                    new LwsRoadSample { position = Vector3.right * 100, forward = Vector3.right, distanceFromStartMeters = 100 } } } } };
    }
}
