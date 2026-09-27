using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [TestFixture, Category("TaxiRegional"), Category("TaxiMegaPass")]
    public sealed class TruckTaxiRivalPopulationTests
    {
        private readonly List<Object> created = new List<Object>();
        private bool hadPreference;
        private int previousPreference;
        [SetUp] public void SetUp()
        {
            hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            previousPreference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in created) if (item != null) Object.DestroyImmediate(item);
            created.Clear();
            if(hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,previousPreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }

        private TruckTaxiRivalPopulation Create(out TruckTaxiTrafficAdapter host)
        {
            var root = new GameObject("rival test"); created.Add(root);
            host = root.AddComponent<TruckTaxiTrafficAdapter>();
            var prefab = new GameObject("traffic prefab"); created.Add(prefab);
            host.trafficPrefabs = new[] { prefab };
            host.cityLanes = new[] {
                new LwsTrafficLaneDefinition {
                    roadId = "town", laneId = "town.east", spawnEnabled = true, speedLimitMph = 35,
                    centerline = new[] { Vector3.zero, Vector3.right * 20, Vector3.right * 40,
                        Vector3.right * 60, Vector3.right * 80, Vector3.right * 100 }
                },
                new LwsTrafficLaneDefinition {
                    roadId = "highway", laneId = "highway.east", spawnEnabled = true, speedLimitMph = 65,
                    centerline = new[] { Vector3.forward * 100, Vector3.forward * 120,
                        Vector3.forward * 140, Vector3.forward * 160,
                        Vector3.forward * 180, Vector3.forward * 200 }
                }
            };
            var rivals = root.AddComponent<TruckTaxiRivalPopulation>();
            rivals.Initialize(host);
            return rivals;
        }

        [Test]
        public void LogicalRosterAndLiveBudgetDoNotCreateFullFleet()
        {
            var rivals = Create(out var host);
            Assert.AreEqual(12, rivals.LogicalCount);
            Assert.AreEqual(0, rivals.LiveCount);
            Assert.AreEqual(3, TruckTaxiRivalPopulation.AllowedLiveCount(3, 8, 40));
            Assert.AreEqual(2, TruckTaxiRivalPopulation.AllowedLiveCount(5, 2, 40));
            Assert.AreEqual(0, TruckTaxiRivalPopulation.AllowedLiveCount(3, 8, 0));
            Assert.IsNull(rivals.DebugSpawn(Vector3.zero));
            Assert.AreEqual(0, host.ActiveCount);
        }

        [Test]
        public void DuplicatePickupAndDemandSnapshotsAreRejected()
        {
            var rivals = Create(out _);
            rivals.SetDemandZones(new[] {
                new TruckTaxiRivalDemandZone("stadium", Vector3.zero, 120, 1),
                new TruckTaxiRivalDemandZone("stadium", Vector3.one, 120, 1),
                new TruckTaxiRivalDemandZone("concert", Vector3.forward * 80, 100, .5f)
            });
            Assert.AreEqual(2, rivals.DemandZones.Count);
            Assert.AreEqual(1, rivals.DemandZones[0].At(Vector3.zero), .001f);
            Assert.IsTrue(rivals.ForcePickup("taxi.rival.0"));
            Assert.IsFalse(rivals.ForcePickup("taxi.rival.0"));
            Assert.IsFalse(rivals.ForcePickup("taxi.rival.unknown"));
        }

        [Test]
        public void RivalVisualIdentityCannotClaimCurrentPlayerOrReservedPassenger()
        {
            var player = ScriptableObject.CreateInstance<PassengerProfile>();
            var other = ScriptableObject.CreateInstance<PassengerProfile>();
            created.Add(player); created.Add(other);
            player.passengerId = "player"; other.passengerId = "other";
            var reserved = new HashSet<string> { "other" };
            Assert.IsFalse(TruckTaxiRivalPopulation.IsIdentityEligible(player, player, reserved));
            Assert.IsFalse(TruckTaxiRivalPopulation.IsIdentityEligible(other, player, reserved));
            reserved.Clear();
            Assert.IsTrue(TruckTaxiRivalPopulation.IsIdentityEligible(other, player, reserved));
            Assert.AreEqual("player", player.passengerId);
        }

        [Test]
        public void RivalPickupRequestLeavesPlayerRideAndHistoryUntouched()
        {
            var rivals = Create(out _);
            var config = ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); created.Add(config);
            var passenger = ScriptableObject.CreateInstance<PassengerProfile>(); created.Add(passenger);
            passenger.passengerId = "player.current";
            config.passengers = new[] { passenger };
            config.minimumTripDistance = 10;
            config.maximumTripDistance = 1000;
            var stops = new List<TruckTaxiRideLocation>();
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("stop " + i); created.Add(go);
                var stop = go.AddComponent<TruckTaxiRideLocation>();
                stop.locationId = "stop." + i;
                go.transform.position = Vector3.right * i * 100;
                stops.Add(stop);
            }
            var session = new TruckTaxiSession(config, stops, 17);
            session.SetWorldConditions(0, 12, TruckTaxiDemandWeather.Neutral);
            session.StartShift();
            Assert.IsTrue(session.OfferRide(passenger));
            var state = session.State;
            int historyCount = session.RideHistory.Count;
            int recentCount = session.RecentPassengerIds.Count;
            Assert.IsTrue(rivals.ForcePickup("taxi.rival.0"));
            Assert.AreEqual(state, session.State);
            Assert.AreSame(passenger, session.Passenger);
            Assert.AreEqual(historyCount, session.RideHistory.Count);
            Assert.AreEqual(recentCount, session.RecentPassengerIds.Count);
        }

        [Test]
        public void UnmaterializedRetargetPersistsPositionAndNeverMovesLiveActor()
        {
            Create(out var host);
            var logical = new TruckTaxiTrafficPopulation(host.cityLanes, 1, 1, 700000);
            var car = logical.Cars[0];
            Assert.IsTrue(logical.RetargetUnmaterialized(car, Vector3.forward * 160));
            Assert.AreEqual("highway", car.RoadId);
            Vector3 position = logical.Position(car);
            logical.Advance(car, 1);
            Assert.Greater((logical.Position(car) - position).sqrMagnitude, 0);
            var actor = new GameObject("live"); created.Add(actor);
            car.Actor = actor;
            Vector3 before = logical.Position(car);
            Assert.IsFalse(logical.RetargetUnmaterialized(car, Vector3.zero));
            Assert.AreEqual(before, logical.Position(car));
        }
    }
}
