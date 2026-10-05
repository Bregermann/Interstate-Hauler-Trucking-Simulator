using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiCompanionLifecycle")]
    public sealed class TruckTaxiCompanionLifecycleTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GameObject Make(string name)
        {
            var result = new GameObject(name); objects.Add(result); return result;
        }
        private GameObject Box(string name, Vector3 position, Vector3 size)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(result);
            result.name = name; result.transform.position = position; result.transform.localScale = size;
            return result;
        }
        [TearDown]
        public void Cleanup()
        {
            foreach (var item in objects) if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
        }

        [Test]
        public void LogicalMinimumsAreIdempotentAndLargeSitesHaveSixDistinctVariants()
        {
            var policy = new TruckTaxiCompanionPopulationPolicy();
            for (int i = 0; i < 100; i++) policy.RegisterBlock("block-" + i, new Bounds(Vector3.right * i * 80, Vector3.one * 50));
            policy.RegisterService("shop", Vector3.zero, 18);
            policy.RegisterService("depot", Vector3.right * 200, 30, true);
            for (int i = 0; i < 10; i++)
            {
                policy.RegisterService("shop", Vector3.zero, 18);
                policy.RegisterService("depot", Vector3.right * 200, 30, true);
            }
            Assert.AreEqual(109, policy.Count);
            Assert.AreEqual(3, policy.Records.Count(r => r.AreaId == "service:shop"));
            Assert.AreEqual(6, policy.Records.Count(r => r.AreaId == "service:depot"));
            Assert.AreEqual(6, policy.Records.Where(r => r.AreaId == "service:depot").Select(r => r.Variant).Distinct().Count());
            Assert.AreEqual(TruckTaxiCompanionPopulationPolicy.VariantCount, policy.Records.Select(r => r.Variant).Distinct().Count());
            Assert.AreEqual(policy.Count, policy.Records.Select(r => r.Id).Distinct().Count());
        }

        [Test]
        public void RegistryCreatesNoActorsAndRetainsBaselineWithoutWeatherOrTimeGates()
        {
            var loop = Make("loop").AddComponent<TruckTaxiRoadsideCompanionLoop>();
            for (int i = 0; i < 200; i++) loop.RegisterBlock("b" + i, new Bounds(Vector3.right * i * 50, Vector3.one * 30));
            loop.RegisterService("repair", Vector3.zero, 18);
            Assert.AreEqual(203, loop.LogicalPopulationCount);
            Assert.AreEqual(0, loop.MaterializedCount);
            loop.SetRegionAvailability(_ => false);
            Assert.AreEqual(203, loop.LogicalPopulationCount);
            loop.maximumMaterialized = 10000;
            Assert.AreEqual(128, loop.MaterializationBudget);
        }

        [TestCase(TaxiLocationType.Hotel, true)]
        [TestCase(TaxiLocationType.Restaurant, true)]
        [TestCase(TaxiLocationType.GasStation, true)]
        [TestCase(TaxiLocationType.RandomStreet, false)]
        public void OnlyRealServiceLocationTypesReceiveServiceBaseline(TaxiLocationType type, bool expected) =>
            Assert.AreEqual(expected, TruckTaxiCompanionPopulationPolicy.IsServiceLocation(type));

        private static TruckTaxiCabOccupantRig.Anchor Seat(TruckTaxiCabAnchorId id, Vector3 position, bool secondary = false) =>
            new TruckTaxiCabOccupantRig.Anchor { id = id, mountPath = "", localPosition = position,
                secondaryOccupantSeat = secondary, allowedOccupants = TruckTaxiCabOccupantKind.Passenger | TruckTaxiCabOccupantKind.Companion };

        [Test]
        public void SecondaryRequiresExplicitSafeCapabilityAndNeverMovesFare()
        {
            var truck = Make("truck"); var rig = truck.AddComponent<TruckTaxiCabOccupantRig>();
            var fare = Make("fare"); var companion = Make("companion");
            var front = Seat(TruckTaxiCabAnchorId.FrontPassengerSeat, new Vector3(.5f, 1, 0));
            var extra = Seat(TruckTaxiCabAnchorId.SleeperRight, new Vector3(2, 1, -3));
            rig.Configure(true, new[] { front, extra });
            Assert.IsTrue(rig.TrySeat(fare.transform, TruckTaxiCabOccupantKind.Passenger,
                front.id, TruckTaxiSeatType.StandardPassenger, Vector3.one));
            Vector3 original = fare.transform.position;
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.CompanionPassenger));
            extra.secondaryOccupantSeat = true;
            Assert.IsTrue(rig.TrySeatSecondary(companion.transform, TruckTaxiSeatType.CompanionPassenger, Vector3.one));
            Assert.AreEqual(original, fare.transform.position);
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.CompanionPassenger));
            companion.SetActive(false);
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.CompanionPassenger), "Concealment must not release seat ownership.");
            rig.Release(companion.transform);
            companion.transform.SetParent(null);
            Assert.IsTrue(rig.HasSecondarySeat(TruckTaxiSeatType.CompanionPassenger));
        }

        [Test]
        public void OverlappingOrPrimaryAnchorsCannotMasqueradeAsSecondarySeats()
        {
            var rig = Make("truck").AddComponent<TruckTaxiCabOccupantRig>();
            var front = Seat(TruckTaxiCabAnchorId.FrontPassengerSeat, Vector3.zero, true);
            var extra = Seat(TruckTaxiCabAnchorId.SleeperRight, Vector3.right * .1f, true);
            rig.Configure(true, new[] { front, extra });
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.CompanionPassenger));
        }

        [Test]
        public void WorldExitRestoresScaleParentAndTriggerOnlyAfterClearingWideTruck()
        {
            var floor = Box("sidewalk", new Vector3(0, -.1f, 0), new Vector3(80, .2f, 80));
            floor.AddComponent<TruckTaxiSurface>();
            var truck = Box("wide truck", new Vector3(0, 1.5f, 0), new Vector3(7, 3, 10));
            truck.AddComponent<Rigidbody>().isKinematic = true;
            var actorObject = Make("wide actor"); actorObject.transform.localScale = Vector3.one * 1.6f;
            var visual = Box("visual", Vector3.zero, new Vector3(.8f, 1.5f, .8f));
            visual.transform.SetParent(actorObject.transform, false); visual.transform.localPosition = Vector3.up * .75f;
            var actor = actorObject.AddComponent<TruckTaxiPassengerActor>(); actor.Bind(null);
            Vector3 scale = actor.StandingWorldScale;
            actor.transform.SetParent(truck.transform, false); actor.transform.localScale = Vector3.one * .1f;
            actor.PrepareForWorld(truck.transform);
            Assert.IsFalse(actorObject.activeSelf);
            Assert.IsNull(actor.transform.parent);
            Assert.AreEqual(scale, actor.transform.localScale);
            Physics.SyncTransforms();
            Assert.IsTrue(TruckTaxiActorWorldPlacement.TryExit(actor, truck.transform, out var exit));
            Assert.Greater(Mathf.Abs(exit.x), 3.5f + actor.StandingRadius);
            Assert.IsTrue(actor.ShowInWorld(exit, truck.transform));
            Assert.IsTrue(actorObject.activeSelf);
            Assert.IsTrue(actor.GetComponent<CapsuleCollider>().isTrigger);
            Assert.IsTrue(actor.GetComponent<CapsuleCollider>().enabled);
            Assert.AreEqual(TruckTaxiActorLifecycle.OutsideWaiting, actor.Lifecycle);
        }

        [Test]
        public void PlacementRejectsRoadRoofObstacleAndReturnThroughTruck()
        {
            var floor = Box("ground", new Vector3(0, -.1f, 0), new Vector3(60, .2f, 60));
            var surface = floor.AddComponent<TruckTaxiSurface>();
            var truck = Box("truck", Vector3.up, new Vector3(3, 2, 7));
            Physics.SyncTransforms();
            surface.isRoad = true;
            Assert.IsFalse(TruckTaxiActorWorldPlacement.TryGround(Vector3.right * 8, truck.transform, out _));
            surface.isRoad = false;
            var roof = Box("roof", new Vector3(8, 2, 0), new Vector3(4, .2f, 4));
            Physics.SyncTransforms();
            Assert.IsFalse(TruckTaxiActorWorldPlacement.TryGround(Vector3.right * 8, truck.transform, out _));
            roof.SetActive(false);
            var wall = Box("wall", new Vector3(8, 1, 0), new Vector3(1, 2, 1));
            Physics.SyncTransforms();
            Assert.IsFalse(TruckTaxiActorWorldPlacement.Clear(Vector3.right * 8, .6f, 1.8f, truck.transform));
            Assert.IsFalse(TruckTaxiActorWorldPlacement.ClearSegment(Vector3.left * 5, Vector3.right * 5, .6f, 1.8f, truck.transform));
            wall.SetActive(false);
            Physics.SyncTransforms();
            Assert.IsTrue(TruckTaxiActorWorldPlacement.ClearSegment(new Vector3(5, 0, -4), new Vector3(5, 0, 4), .6f, 1.8f, truck.transform));
        }

        [Test]
        public void CenterPivotModelUsesMeasuredFootOffsetWhenShownOutside()
        {
            Box("floor", new Vector3(0, -.1f, 0), new Vector3(40, .2f, 40)).AddComponent<TruckTaxiSurface>();
            var truck = Make("truck"); truck.transform.position = Vector3.right * 10;
            var actorObject = Box("center pivot", new Vector3(0, 4, 0), Vector3.one * 2);
            var actor = actorObject.AddComponent<TruckTaxiPassengerActor>(); actor.Bind(null);
            Assert.AreEqual(1, actor.StandingGroundOffset, .001f);
            actor.PrepareForWorld(truck.transform);
            Physics.SyncTransforms();
            Assert.IsTrue(actor.ShowInWorld(Vector3.zero, truck.transform));
            Assert.AreEqual(1, actor.transform.position.y, .001f);
            Assert.AreEqual(0, actor.GetComponent<Renderer>().bounds.min.y, .001f);
        }
    }
}
