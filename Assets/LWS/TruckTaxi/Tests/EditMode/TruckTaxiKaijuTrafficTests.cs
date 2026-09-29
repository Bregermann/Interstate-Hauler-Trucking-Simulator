using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiKaijuTrafficTests
    {
        [TestCase("Waiting")]
        [TestCase("Boarding")]
        [TestCase("Riding")]
        [TestCase("Exiting")]
        public void RivalTakeoverSynchronouslyReturnsRiderAndPoolOwnership(string stage)
        {
            var root = new GameObject("kaiju rival owner test");
            var victim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var identity = ScriptableObject.CreateInstance<PassengerProfile>();
            GameObject visual = null;
            try
            {
                var adapter = root.AddComponent<TruckTaxiTrafficAdapter>();
                adapter.enabled = false;
                var lane = new LwsTrafficLaneDefinition {
                    laneId = "rival-test", roadId = "rival-test", spawnEnabled = true, speedLimitMph = 30,
                    centerline = new[] { Vector3.zero, Vector3.right * 100, Vector3.right * 200,
                        Vector3.right * 500, Vector3.right * 1000, Vector3.right * 1200, Vector3.right * 1300 }
                };
                adapter.cityLanes = new[] { lane };
                adapter.trafficPrefabs = new[] { victim };
                var rivals = root.AddComponent<TruckTaxiRivalPopulation>();
                rivals.enabled = false;
                rivals.Initialize(adapter);
                var records = Field<System.Collections.IList>(rivals, "rivals");
                var record = records[0];
                var car = Field<TruckTaxiTrafficPopulation.Car>(record, "Car");
                var body = victim.AddComponent<Rigidbody>();
                var impact = victim.AddComponent<TruckTaxiImpactTarget>();
                var mission = victim.AddComponent<TruckTaxiMissionTarget>();
                var actor = victim.AddComponent<TruckTaxiTrafficPooledActor>();
                actor.Configure(0);
                actor.SetFullPhysics(true);
                impact.targetId = car.Id;
                car.Actor = victim; car.FullPhysics = true;
                Field<List<GameObject>>(adapter, "vehicles").Add(victim);
                Field<Dictionary<string, GameObject>>(adapter, "vehiclesById").Add(car.Id, victim);
                var dedicated = Field<HashSet<GameObject>>(adapter, "dedicatedVehicles");
                dedicated.Add(victim);
                SetField(adapter, "<FullPhysicsCount>k__BackingField", 1);
                var rider = new GameObject("rival rider");
                rider.transform.SetParent(root.transform, false);
                rider.SetActive(stage != "Riding");
                identity.passengerId = "kaiju-rider-test";
                SetField(record, "Rider", rider);
                SetField(record, "VisualIdentity", identity);
                SetField(record, "PickupQueued", true);
                var stageField = record.GetType().GetField("Stage");
                stageField.SetValue(record, System.Enum.Parse(stageField.FieldType, stage));
                var reservedIds = Field<HashSet<string>>(rivals, "usedVisualIds");
                reservedIds.Add(identity.passengerId);
                int generation = actor.Generation;
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation + 1));
                mission.Assign(car.Id, 3);
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation));
                mission.Clear();
                car.MissionReserved = true;
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation));
                car.MissionReserved = false;
                dedicated.Remove(victim);
                Assert.IsFalse(rivals.TryReleaseForKaiju(adapter, victim, generation), "Failed pool release must preserve rider state.");
                Assert.AreSame(rider, Field<GameObject>(record, "Rider"));
                Assert.AreEqual(stage, Field<object>(record, "Stage").ToString());
                Assert.IsTrue(reservedIds.Contains(identity.passengerId));
                Assert.AreSame(victim, car.Actor);
                dedicated.Add(victim);
                visual = TruckTaxiKaijuVisual.Snapshot(victim.transform);
                Assert.IsNotNull(visual);
                Assert.IsTrue(adapter.TryReleaseKaijuVictim(victim, generation));
                Assert.IsNull(car.Actor);
                Assert.IsFalse(car.FullPhysics);
                Assert.AreEqual(0, rivals.LiveCount);
                Assert.AreEqual(records.Count, rivals.LogicalCount, "The stable rival identity remains in the logical fleet.");
                Assert.IsNull(Field<GameObject>(record, "Rider"));
                Assert.IsNull(Field<PassengerProfile>(record, "VisualIdentity"));
                Assert.AreEqual("None", Field<object>(record, "Stage").ToString());
                Assert.IsFalse(Field<bool>(record, "PickupQueued"));
                Assert.IsFalse(rider.activeSelf);
                Assert.IsFalse(reservedIds.Contains(identity.passengerId));
                Assert.AreEqual(1, Field<Stack<GameObject>>(rivals, "riderPool").Count);
                Assert.GreaterOrEqual(Field<float>(record, "NextMaterialize"), Time.time + 29);
                Assert.Greater(Field<float>(record, "NextPickup"), Field<float>(record, "NextMaterialize"));
                Assert.IsFalse(victim.activeSelf);
                Assert.IsTrue(body.isKinematic);
                Assert.AreEqual(0, adapter.ActiveCount);
                Assert.AreEqual(0, adapter.FullPhysicsCount);
                Assert.AreEqual(1, adapter.PooledCount);
                Assert.IsFalse(dedicated.Contains(victim));
                Assert.IsFalse(adapter.TryResolveVehicle(car.Id, out _));
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation));
                Assert.IsFalse(rivals.TryReleaseForKaiju(adapter, victim, generation));
                Assert.AreEqual(1, Field<Stack<GameObject>>(rivals, "riderPool").Count);
                Assert.AreEqual(1, adapter.PooledCount);
                Field<List<TruckTaxiTrafficPooledActor>>(adapter, "pool").Remove(actor);
                actor.Place(null, 0, Vector3.right * 999, "other-vehicle", false);
                Assert.IsFalse(rivals.TryReleaseForKaiju(adapter, victim, actor.Generation));
                TruckTaxiKaijuVisual.Dispose(visual);
                visual = null;
                Assert.IsTrue(victim.activeSelf, "Eating the proxy cannot disable a reused pooled actor.");
                Assert.AreEqual(Vector3.right * 999, victim.transform.position);
                Assert.AreEqual("other-vehicle", impact.targetId);
            }
            finally
            {
                TruckTaxiKaijuVisual.Dispose(visual);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(victim);
                Object.DestroyImmediate(identity);
            }
        }

        [Test]
        public void VictimReleaseRejectsStaleReservedAndUnownedActorsThenClearsLogicalOwnership()
        {
            var root = new GameObject("kaiju traffic adapter test");
            var victim = new GameObject("kaiju victim test");
            try
            {
                var adapter = root.AddComponent<TruckTaxiTrafficAdapter>();
                adapter.enabled = false;
                var lane = new LwsTrafficLaneDefinition {
                    laneId = "test", roadId = "test", spawnEnabled = true, speedLimitMph = 30,
                    centerline = new[] { Vector3.zero, Vector3.right * 100, Vector3.right * 200,
                        Vector3.right * 500, Vector3.right * 1000, Vector3.right * 1200, Vector3.right * 1300 }
                };
                var population = new TruckTaxiTrafficPopulation(new[] { lane }, 1, 1, 0);
                Field<List<LwsTrafficLaneDefinition>>(adapter, "validLanes").Add(lane);
                typeof(TruckTaxiTrafficAdapter).GetField("population", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(adapter, population);
                victim.transform.position = Vector3.right * 200;
                var body = victim.AddComponent<Rigidbody>();
                victim.AddComponent<BoxCollider>();
                var impact = victim.AddComponent<TruckTaxiImpactTarget>();
                var mission = victim.AddComponent<TruckTaxiMissionTarget>();
                var actor = victim.AddComponent<TruckTaxiTrafficPooledActor>();
                actor.Configure(0);
                actor.SetFullPhysics(true);
                var car = population.Cars[0];
                impact.targetId = car.Id;
                car.Actor = victim; car.FullPhysics = true;
                int generation = actor.Generation;
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation), "Unregistered objects are never victims.");
                Field<List<GameObject>>(adapter, "vehicles").Add(victim);
                Field<Dictionary<string, GameObject>>(adapter, "vehiclesById").Add(car.Id, victim);
                typeof(TruckTaxiTrafficAdapter).GetField("<FullPhysicsCount>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(adapter, 1);
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation + 1));
                mission.Assign(car.Id, 3);
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation));
                mission.Clear();
                car.MissionReserved = true;
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation));
                car.MissionReserved = false;
                Assert.IsTrue(adapter.TryReleaseKaijuVictim(victim, generation));
                Assert.IsFalse(victim.activeSelf);
                Assert.IsTrue(body.isKinematic, "Only the existing pool owns parked physics.");
                Assert.AreEqual(generation + 1, actor.Generation);
                Assert.AreEqual(1, adapter.PooledCount);
                Assert.AreEqual(0, adapter.ActiveCount);
                Assert.AreEqual(0, adapter.FullPhysicsCount);
                Assert.IsFalse(adapter.TryResolveVehicle(car.Id, out _));
                Assert.IsNull(car.Actor);
                Assert.IsFalse(car.FullPhysics);
                Assert.Greater(Vector3.Distance(population.Position(car), victim.transform.position), adapter.visibleTrafficRadius);
                Assert.IsFalse(adapter.TryReleaseKaijuVictim(victim, generation));
                Assert.AreEqual(1, adapter.PooledCount, "Repeated cleanup cannot enqueue an actor twice.");
            }
            finally { Object.DestroyImmediate(victim); Object.DestroyImmediate(root); }
        }

        [Test]
        public void SnapshotContainsOnlyGeometryAndOwnsNoPoolState()
        {
            var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject visual = null;
            try
            {
                source.name = "UTS visual stand-in";
                source.transform.localScale = new Vector3(2, 3, 7);
                source.AddComponent<Rigidbody>();
                var pooled = source.AddComponent<TruckTaxiTrafficPooledActor>();
                pooled.Configure(0);
                var block = new MaterialPropertyBlock();
                block.SetColor("_Color", Color.green);
                source.GetComponent<Renderer>().SetPropertyBlock(block);
                visual = TruckTaxiKaijuVisual.Snapshot(source.transform);
                Assert.IsNotNull(visual);
                Assert.IsEmpty(visual.GetComponentsInChildren<MonoBehaviour>(true));
                Assert.IsEmpty(visual.GetComponentsInChildren<Rigidbody>(true));
                Assert.IsEmpty(visual.GetComponentsInChildren<Collider>(true));
                pooled.PutAway();
                pooled.Place(null, 0, Vector3.right * 999, "reused", false);
                Assert.AreEqual(Vector3.zero, visual.transform.position);
                visual.GetComponentInChildren<Renderer>().GetPropertyBlock(block);
                Assert.AreEqual(Color.green, block.GetColor("_Color"));
                Assert.AreEqual(new Vector3(2, 3, 7), TruckTaxiKaijuVisual.LocalBounds(visual).size);
                TruckTaxiKaijuVisual.Dispose(visual);
                visual = null;
                Assert.IsTrue(source.activeSelf);
                Assert.AreEqual(Vector3.right * 999, source.transform.position);
            }
            finally { TruckTaxiKaijuVisual.Dispose(visual); Object.DestroyImmediate(source); }
        }

        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(target, value);
    }
}
