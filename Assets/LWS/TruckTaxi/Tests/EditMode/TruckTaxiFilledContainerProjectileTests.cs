using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiFilledContainerProjectileTests
    {
        [Test]
        public void VehicleBlastHasDistinctBoundedArcadeImpulseDefault()
        {
            var container = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var projectile = container.AddComponent<TruckTaxiFilledContainerProjectile>();
                Assert.AreEqual(4500, projectile.vehicleBlastImpulse);
                Assert.Less(projectile.blastImpulse, projectile.vehicleBlastImpulse);
                Assert.AreEqual(3, projectile.vehicleBlastImpulse / 1500f, .0001f);
                Assert.AreEqual(24, projectile.blastRadius);
                Assert.AreEqual(8, projectile.vehicleBlastSpeed);
                Assert.AreEqual(28, projectile.pedestrianUpwardSpeed);
                Assert.AreEqual(80000, projectile.maximumVehicleBlastImpulse);
            }
            finally { Object.DestroyImmediate(container); }
        }

        [Test]
        public void BlastFalloffIsBoundedAndDecreasesWithDistance()
        {
            Assert.AreEqual(1, TruckTaxiFilledContainerProjectile.BlastFalloff(0, 6));
            Assert.AreEqual(.25f, TruckTaxiFilledContainerProjectile.BlastFalloff(3, 6), .0001f);
            Assert.AreEqual(0, TruckTaxiFilledContainerProjectile.BlastFalloff(6, 6));
            Assert.AreEqual(0, TruckTaxiFilledContainerProjectile.BlastFalloff(100, 6));
            Assert.AreEqual(0, TruckTaxiFilledContainerProjectile.BlastFalloff(float.NaN, 6));
            Assert.AreEqual(0, TruckTaxiFilledContainerProjectile.BlastFalloff(1, 0));
        }

        [Test]
        public void ArcadeFalloffRetainsUsefulForceAcrossTheLargerRadius()
        {
            Assert.AreEqual(.5f, TruckTaxiFilledContainerProjectile.BlastFalloff(12, 24, 1), .0001f);
            Assert.AreEqual(0, TruckTaxiFilledContainerProjectile.BlastFalloff(24, 24, 1));
            Assert.AreEqual(80000, TruckTaxiFilledContainerProjectile.BoundedVehicleImpulse(10000, 8, 4500, 80000));
            Assert.AreEqual(100000, TruckTaxiFilledContainerProjectile.BoundedVehicleImpulse(100000, 100, 4500, float.MaxValue));
        }

        [Test]
        public void PedestrianKickRemainsWithinNativeThirtyMeterPerSecondCeiling()
        {
            Vector3 current = Vector3.right * 4;
            Vector3 change = TruckTaxiFilledContainerProjectile.BoundedVelocityChange(current, Vector3.up * 28 + Vector3.forward * 10, 30);
            Assert.LessOrEqual((current + change).magnitude, 30.0001f);
            Assert.Greater(change.y, 26, "Center blast retains multiple-building-height upward launch speed.");
            Assert.AreEqual(Vector3.zero, TruckTaxiFilledContainerProjectile.BoundedVelocityChange(current, Vector3.one * float.NaN, 30));
        }

        [Test]
        public void DirectThenBlastHasOneSemanticHitAndOneIndependentRadialApplication()
        {
            var container = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var actor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var projectile = container.AddComponent<TruckTaxiFilledContainerProjectile>();
                var rigidbody = actor.AddComponent<Rigidbody>(); rigidbody.isKinematic = true;
                var target = actor.AddComponent<TruckTaxiImpactTarget>(); target.kind = TaxiImpactKind.Traffic;
                int reports = 0; projectile.HitReported += _ => reports++;
                var affect = typeof(TruckTaxiFilledContainerProjectile).GetMethod("Affect", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(affect);
                var collider = actor.GetComponent<Collider>();
                affect.Invoke(projectile, new object[] { collider, Vector3.zero, 8f, 1f, TruckTaxiFilledContainerProjectile.HitPhase.Direct });
                Assert.AreEqual(1, reports); Assert.AreEqual(0, projectile.BlastActorsAffected);
                affect.Invoke(projectile, new object[] { collider, Vector3.zero, 8f, 1f, TruckTaxiFilledContainerProjectile.HitPhase.Blast });
                affect.Invoke(projectile, new object[] { collider, Vector3.zero, 8f, 1f, TruckTaxiFilledContainerProjectile.HitPhase.Blast });
                Assert.AreEqual(1, reports); Assert.AreEqual(1, projectile.BlastActorsAffected);
                Assert.IsTrue(rigidbody.isKinematic, "Blast must not unlock another authority's body.");
            }
            finally { Object.DestroyImmediate(container); Object.DestroyImmediate(actor); }
        }

        [Test]
        public void UnadaptedKinematicPropertyCannotBypassBreakawayBudgetThroughLegacyHit()
        {
            var container = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var projectile = container.AddComponent<TruckTaxiFilledContainerProjectile>();
                var body = structure.AddComponent<Rigidbody>(); body.isKinematic = true;
                var target = structure.AddComponent<TruckTaxiImpactTarget>(); target.kind = TaxiImpactKind.Property;
                typeof(TruckTaxiFilledContainerProjectile).GetMethod("Affect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projectile,
                    new object[] { structure.GetComponent<Collider>(), Vector3.zero, 30f, 1f, TruckTaxiFilledContainerProjectile.HitPhase.Blast });
                Assert.IsTrue(body.isKinematic); Assert.IsFalse(target.Damaged);
                Assert.IsTrue(structure.GetComponent<Collider>().enabled);
            }
            finally { Object.DestroyImmediate(container); Object.DestroyImmediate(structure); }
        }

        [TestCase(1500f, 4500f)]
        [TestCase(10000f, 30000f)]
        [TestCase(100000f, 30000f)]
        public void TrafficKickScalesWithMassButNeverExceedsCeiling(float mass, float expected)
        {
            Assert.AreEqual(expected, TruckTaxiFilledContainerProjectile.BoundedVehicleImpulse(mass, 3, 4500, 30000));
            Assert.AreEqual(0, TruckTaxiFilledContainerProjectile.BoundedVehicleImpulse(float.NaN, 3, 4500, 30000));
        }

        [Test]
        public void ActorKeyCollapsesChildCollidersToOneImpactTarget()
        {
            var actor = new GameObject("Traffic actor");
            var first = new GameObject("Body collider");
            var second = new GameObject("Bumper collider");
            try
            {
                actor.AddComponent<TruckTaxiImpactTarget>().kind = TaxiImpactKind.Traffic;
                first.transform.SetParent(actor.transform);
                second.transform.SetParent(actor.transform);
                var body = first.AddComponent<BoxCollider>();
                var bumper = second.AddComponent<BoxCollider>();
                Assert.AreEqual(TruckTaxiFilledContainerProjectile.ActorKey(body),
                    TruckTaxiFilledContainerProjectile.ActorKey(bumper));
                Assert.AreNotEqual(0, TruckTaxiFilledContainerProjectile.ActorKey(body));
            }
            finally { Object.DestroyImmediate(actor); }
        }

        [Test]
        public void ActorKeyCollapsesUnmarkedCompoundRigidbodyColliders()
        {
            var actor = new GameObject("Movable prop");
            var first = new GameObject("First collider");
            var second = new GameObject("Second collider");
            try
            {
                actor.AddComponent<Rigidbody>();
                first.transform.SetParent(actor.transform);
                second.transform.SetParent(actor.transform);
                var a = first.AddComponent<BoxCollider>();
                var b = second.AddComponent<BoxCollider>();
                Assert.AreEqual(TruckTaxiFilledContainerProjectile.ActorKey(a),
                    TruckTaxiFilledContainerProjectile.ActorKey(b));
            }
            finally { Object.DestroyImmediate(actor); }
        }

        [Test]
        public void ExternalMissionHitRequiresExactAssignedVehicleAndQualifiedSpeed()
        {
            var assigned = new GameObject("Assigned vehicle");
            var replacement = new GameObject("Replacement with same ID");
            try
            {
                var observed = assigned.AddComponent<TruckTaxiImpactTarget>();
                observed.kind = TaxiImpactKind.Traffic;
                observed.targetId = "traffic.a";
                var other = replacement.AddComponent<TruckTaxiImpactTarget>();
                other.kind = TaxiImpactKind.Traffic;
                other.targetId = "traffic.a";
                Assert.IsTrue(TruckTaxiVehicleObjectives.MatchesExternalHit(
                    "traffic.a", assigned, observed, 8, 3));
                Assert.IsFalse(TruckTaxiVehicleObjectives.MatchesExternalHit(
                    "traffic.b", assigned, observed, 8, 3));
                Assert.IsFalse(TruckTaxiVehicleObjectives.MatchesExternalHit(
                    "traffic.a", assigned, other, 8, 3));
                Assert.IsFalse(TruckTaxiVehicleObjectives.MatchesExternalHit(
                    "traffic.a", assigned, observed, 2, 3));
                observed.kind = TaxiImpactKind.Property;
                Assert.IsFalse(TruckTaxiVehicleObjectives.MatchesExternalHit(
                    "traffic.a", assigned, observed, 8, 3));
            }
            finally
            {
                Object.DestroyImmediate(assigned);
                Object.DestroyImmediate(replacement);
            }
        }
    }
}
