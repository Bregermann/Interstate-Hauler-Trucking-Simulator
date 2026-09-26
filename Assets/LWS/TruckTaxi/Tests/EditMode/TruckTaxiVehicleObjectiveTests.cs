using NUnit.Framework;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiVehicleObjectiveTests
    {
        [Test]
        public void NewVehicleTypesRemainAppendedAndRequireActualCapabilities()
        {
            Assert.AreEqual(12, (int)TaxiRequestType.FollowVehicle);
            Assert.AreEqual(18, (int)TaxiRequestType.CollectDroppedObjects);
            foreach (var type in new[] { TaxiRequestType.FollowVehicle,
                TaxiRequestType.RamTargetVehicle, TaxiRequestType.BlockVehicle,
                TaxiRequestType.DestroyVehicle, TaxiRequestType.CollectDroppedObjects,
                TaxiRequestType.LoseVehicle, TaxiRequestType.ReachLocationBeforeVehicle })
            {
                var caps = new TruckTaxiObjectiveCapabilities();
                var definition = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
                definition.requestType = type;
                try
                {
                    Assert.IsFalse(caps.Supports(definition), type.ToString());
                    caps.Register(TruckTaxiObjectiveCapability.TargetVehicles, 1);
                    if (type == TaxiRequestType.DestroyVehicle)
                        caps.Register(TruckTaxiObjectiveCapability.VehicleDestruction, 1);
                    if (type == TaxiRequestType.CollectDroppedObjects)
                        caps.Register(TruckTaxiObjectiveCapability.Collectibles, 1);
                    if (type == TaxiRequestType.ReachLocationBeforeVehicle)
                        caps.Register(TruckTaxiObjectiveCapability.VehicleRaceRoute, 1);
                    Assert.AreEqual(type != TaxiRequestType.LoseVehicle,
                        caps.Supports(definition), type.ToString());
                }
                finally { Object.DestroyImmediate(definition); }
            }
        }

        [Test]
        public void RamRequiresExactAssignedTrafficImpactNotProximityOrAnotherVehicle()
        {
            Assert.IsFalse(TruckTaxiVehicleObjectives.QualifiesImpact(
                TaxiEventType.Collision, "car.a", "car.a", 8, 3));
            Assert.IsFalse(TruckTaxiVehicleObjectives.QualifiesImpact(
                TaxiEventType.TrafficRam, "car.b", "car.a", 8, 3));
            Assert.IsFalse(TruckTaxiVehicleObjectives.QualifiesImpact(
                TaxiEventType.TrafficRam, "car.a", "car.a", 2, 3));
            Assert.IsTrue(TruckTaxiVehicleObjectives.QualifiesImpact(
                TaxiEventType.TrafficRam, "car.a", "car.a", 8, 3));
        }

        [Test]
        public void FollowRequiresMovingTruckAndTargetInDistanceBand()
        {
            Assert.IsFalse(TruckTaxiVehicleObjectives.InFollowBand(20, 0, 8, 8, 35));
            Assert.IsFalse(TruckTaxiVehicleObjectives.InFollowBand(20, 8, 0, 8, 35));
            Assert.IsFalse(TruckTaxiVehicleObjectives.InFollowBand(50, 8, 8, 8, 35));
            Assert.IsTrue(TruckTaxiVehicleObjectives.InFollowBand(20, 8, 8, 8, 35));
        }

        [Test]
        public void BlockRequiresStoppedTargetAndTruckInFront()
        {
            Vector3 target = Vector3.zero, forward = Vector3.forward;
            Assert.IsFalse(TruckTaxiVehicleObjectives.IsBlocking(
                target, forward, 0, Vector3.back * 6, 0, 5));
            Assert.IsFalse(TruckTaxiVehicleObjectives.IsBlocking(
                target, forward, 4, Vector3.forward * 6, 0, 5));
            Assert.IsFalse(TruckTaxiVehicleObjectives.IsBlocking(
                target, forward, 0, Vector3.forward * 6, 0, 0));
            Assert.IsTrue(TruckTaxiVehicleObjectives.IsBlocking(
                target, forward, 0, Vector3.forward * 6, 0, 5));
        }

        [Test]
        public void MissionDurabilityRequiresMultipleQualifiedImpacts()
        {
            var go = new GameObject("Assigned car");
            try
            {
                var mission = go.AddComponent<TruckTaxiMissionTarget>();
                mission.Assign("car.a", 3);
                Assert.IsFalse(mission.ApplyQualifiedHit());
                Assert.IsFalse(mission.ApplyQualifiedHit());
                Assert.IsTrue(mission.ApplyQualifiedHit());
                Assert.IsFalse(mission.ApplyQualifiedHit());
                mission.Clear();
                Assert.IsFalse(mission.ApplyQualifiedHit());
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SpecificTargetSelectionRequiresResolverEvidence()
        {
            var config = ScriptableObject.CreateInstance<TruckTaxiConfiguration>();
            var definition = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
            try
            {
                definition.requestType = TaxiRequestType.RamTargetVehicle;
                definition.targetId = "taxi.traffic.assigned";
                var session = new TruckTaxiSession(config, Array.Empty<TruckTaxiRideLocation>(), 17);
                session.Capabilities.Register(TruckTaxiObjectiveCapability.TargetVehicles, 1);
                Assert.IsFalse(session.CanAssign(definition, out _));
                session.CanResolveSpecificTarget = d => d.targetId == "other";
                Assert.IsFalse(session.CanAssign(definition, out _));
                session.CanResolveSpecificTarget = d => d.targetId == "taxi.traffic.assigned";
                Assert.IsTrue(session.CanAssign(definition, out _));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(config);
            }
        }
    }
}
