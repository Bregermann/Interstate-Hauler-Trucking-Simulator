using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests.EditMode
{
    public sealed class TruckTaxiWorldBuilderTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        [Test]
        public void ConfigureAnchor_IsIdempotentAndPreservesTransformAndAuthoredValues()
        {
            var go = New("TT_GAS_Route66");
            go.transform.position = new Vector3(31, 2, -14);
            go.transform.rotation = Quaternion.Euler(0, 25, 0);
            go.transform.localScale = new Vector3(2, 1, 3);
            Assert.IsTrue(TruckTaxiWorldBuilder.ConfigureAnchor(go.transform));
            var anchor = go.GetComponent<TruckTaxiWorldAnchor>();
            var gas = go.GetComponent<TruckTaxiGasStationPoint>();
            Assert.IsTrue(Guid.TryParse(anchor.StableId, out _));
            Assert.AreEqual(anchor.StableId, gas.stableId);
            Assert.NotNull(go.GetComponent<TruckTaxiRideLocation>());
            Assert.NotNull(go.GetComponent<TruckTaxiMapMarker>());
            gas.centsPerLiter = 219;
            gas.stoppingRadius = 11;
            var id = anchor.StableId;
            Assert.IsTrue(TruckTaxiWorldBuilder.ConfigureAnchor(go.transform));
            Assert.AreEqual(id, anchor.StableId);
            Assert.AreSame(gas, go.GetComponent<TruckTaxiGasStationPoint>());
            Assert.AreEqual(219, gas.centsPerLiter);
            Assert.AreEqual(11, gas.stoppingRadius);
            Assert.AreEqual(new Vector3(31, 2, -14), go.transform.position);
            Assert.AreEqual(Quaternion.Euler(0, 25, 0), go.transform.rotation);
            Assert.AreEqual(new Vector3(2, 1, 3), go.transform.localScale);
        }

        [Test]
        public void ConfigureAnchor_DoesNotOverwritePreExistingStoreComponent()
        {
            var go = New("TT_STORE_West_Stop");
            var store = go.AddComponent<TruckTaxiStorePoint>();
            store.displayName = "Designer label";
            store.stopRadius = 20;
            TruckTaxiWorldBuilder.ConfigureAnchor(go.transform);
            Assert.AreEqual("Designer label", store.displayName);
            Assert.AreEqual(20, store.stopRadius);
            Assert.NotNull(go.GetComponent<TruckTaxiRideLocation>());
        }

        [Test]
        public void ValidateAnchorIds_ReportsCopiedIdentity()
        {
            var first = New("TT_PARKING_A");
            var second = New("TT_PARKING_B");
            TruckTaxiWorldBuilder.ConfigureAnchor(first.transform);
            TruckTaxiWorldBuilder.ConfigureAnchor(second.transform);
            EditorUtility.CopySerialized(first.GetComponent<TruckTaxiWorldAnchor>(), second.GetComponent<TruckTaxiWorldAnchor>());
            var errors = TruckTaxiWorldBuilder.ValidateAnchorIds(new[] { first.transform, second.transform });
            Assert.IsTrue(errors.Exists(error => error.Contains("Duplicate stable ID")));
        }

        [Test]
        public void ParseRoadPoints_SortsAndRejectsGaps()
        {
            var road = New("TT_ROAD_CityStreet_Elm");
            NewChild(road.transform, "P01", new Vector3(10, 0, 0));
            NewChild(road.transform, "P00", Vector3.zero);
            var points = TruckTaxiWorldBuilderRoads.ParseRoadPoints(road.transform);
            Assert.AreEqual(Vector3.zero, points[0]);
            Assert.AreEqual(new Vector3(10, 0, 0), points[1]);
            NewChild(road.transform, "P03", new Vector3(20, 0, 0));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiWorldBuilderRoads.ParseRoadPoints(road.transform));
        }

        [Test]
        public void MergeRoad_PreservesExistingGraphAndReusesNearbyEndpoint()
        {
            var graph = LwsRoadGraph.CreateEmpty("city");
            graph.nodes.Add(new LwsRoadNode { nodeId = "city.node", position = Vector3.zero });
            string id = Guid.NewGuid().ToString("D");
            var points = new[] { new Vector3(2, 0, 0), new Vector3(20, 0, 0) };
            var first = TruckTaxiWorldBuilderRoads.MergeRoad(graph, id, "CityStreet", points);
            Assert.AreEqual(0, graph.edges.Count);
            Assert.AreEqual("city.node", first.edges[0].fromNodeId);
            Assert.IsTrue(first.Validate().IsValid);
            var updated = TruckTaxiWorldBuilderRoads.MergeRoad(first, id, "CityStreet", new[] { points[0], new Vector3(30, 0, 0) });
            Assert.AreEqual(1, updated.edges.Count);
            Assert.AreEqual(2, updated.nodes.Count);
            Assert.IsTrue(updated.Validate().IsValid);
        }

        private GameObject New(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        private void NewChild(Transform parent, string name, Vector3 position)
        {
            var go = New(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
        }
    }
}
