using NUnit.Framework;
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
