using System.Collections.Generic;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiRegional")]
    public sealed class TruckTaxiTransitTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown] public void TearDown()
        {
            foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        [Test] public void RailSplineTracksBridgeAndStationsByDistance()
        {
            var route = Route();
            Assert.Greater(route.Length, 4000);
            Assert.That(route.Position(route.stationDistances[0]).z, Is.EqualTo(355).Within(3));
            Assert.That(route.Position(route.stationDistances[1]).z, Is.EqualTo(355).Within(3));
            Assert.That(route.Position(route.NearestDistance(new Vector3(2000, 8, 500))).y,
                Is.EqualTo(8).Within(1));
        }

        [Test] public void TrainDwellsAtBothStationsThenReversesWithinRoute()
        {
            var route = Route();
            var go = new GameObject("Train"); created.Add(go);
            var train = go.AddComponent<TruckTaxiRailTrain>(); train.route = route;
            train.Advance(30);
            Assert.IsTrue(train.IsDwell);
            Assert.That(train.Progress, Is.EqualTo(route.stationDistances[0]).Within(1));
            train.Advance(train.stationWait + .1f);
            train.Advance(1000);
            Assert.IsTrue(train.IsDwell);
            Assert.That(train.Progress, Is.EqualTo(route.stationDistances[1]).Within(1));
            train.Advance(train.stationWait + .1f);
            train.Advance(1000);
            Assert.AreEqual(-1, train.Direction);
            Assert.That(train.Progress, Is.EqualTo(route.Length - 65).Within(1));
        }

        [Test] public void CrossingOnlyStopsRoadTrafficApproachingTheTracks()
        {
            var go = new GameObject("Crossing"); created.Add(go);
            var crossing = go.AddComponent<TruckTaxiTransitCrossing>();
            go.transform.position = new Vector3(4000, 0, 320);
            Assert.IsTrue(crossing.IsApproachingRoad(new Vector3(3960, 0, 320), Vector3.right));
            Assert.IsTrue(crossing.IsApproachingRoad(new Vector3(4040, 0, 320), Vector3.left));
            Assert.IsFalse(crossing.IsApproachingRoad(new Vector3(3960, 0, 320), Vector3.left));
            Assert.IsFalse(crossing.IsApproachingRoad(new Vector3(3960, 0, 350), Vector3.right));
        }

        [Test] public void InstalledBigBusIsSupportedByExistingUtsAdapter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TruckTaxiTransitAuthoring.BusPrefabPath);
            Assert.IsNotNull(prefab);
            Assert.IsTrue(new LwsUtsTrafficApi().TryDescribePrefabSupport(prefab, out string message), message);
        }

        private TruckTaxiRailRoute Route()
        {
            var go = new GameObject("Route"); created.Add(go);
            var route = go.AddComponent<TruckTaxiRailRoute>();
            route.Configure(new[] {
                new Vector3(0, 0, 200), new Vector3(0, 0, 355), new Vector3(0, 0, 500),
                new Vector3(1780, 8, 500), new Vector3(2220, 8, 500), new Vector3(4000, 0, 500),
                new Vector3(4000, 0, 355), new Vector3(4000, 0, 200) });
            return route;
        }
    }
}
