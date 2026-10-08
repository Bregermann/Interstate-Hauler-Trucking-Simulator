using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests.EditMode
{
    public sealed class TruckTaxiFacilityCollisionValidationTests
    {
        private readonly List<GameObject> owned = new List<GameObject>();
        [TearDown]
        public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        [Test]
        public void AuthoringDuplicateServiceIdsIncludeInactiveObjectsAndBothPaths()
        {
            var first = Create("First service").AddComponent<TruckTaxiServicePoint>();
            var second = Create("Inactive service").AddComponent<TruckTaxiServicePoint>();
            first.stableId = second.stableId = "shared.service";
            second.gameObject.SetActive(false);
            var findings = Audit();
            Assert.That(findings, Has.Some.Contains("Duplicate service ID: shared.service"));
            Assert.IsTrue(findings.Any(f => f.Contains("Duplicate service ID") && f.Contains("First service") && f.Contains("Inactive service")));
            Assert.IsFalse(second.gameObject.activeSelf);
            Assert.That(TruckTaxiServiceMapValidation.Validate(new[] { first, second }, new TruckTaxiMapMarker[0]),
                Has.None.Contains("Duplicate service ID"), "Runtime validation retains active-only default.");
        }

        [Test]
        public void DuplicateMapIdsAndOverlappingSelectionsDoNotConfuseObjectIdentity()
        {
            var parent = Create("Parent");
            var child = Create("Child"); child.transform.SetParent(parent.transform);
            var first = parent.AddComponent<TruckTaxiMapMarker>();
            var second = child.AddComponent<TruckTaxiMapMarker>();
            first.stableId = second.stableId = "shared.marker";
            var findings = TruckTaxiServiceMapValidator.ValidateAuthoring(new[] { parent, child, parent });
            Assert.AreEqual(1, findings.Count(f => f.StartsWith("Duplicate map marker:")));
        }

        [Test]
        public void AuthoringReportsMissingDataBayAndToiletIdsIncludingInactiveFixtures()
        {
            var service = Create("Incomplete service").AddComponent<TruckTaxiServicePoint>();
            service.stableId = "incomplete"; service.radius = float.NaN;
            service.capabilities = TruckTaxiServiceCapability.Refuel | TruckTaxiServiceCapability.Store;
            service.location = service.gameObject.AddComponent<TruckTaxiRideLocation>();
            var toilet = Create("Toilet without ID").AddComponent<TruckTaxiRestroomFixture>();
            toilet.gameObject.SetActive(false);
            var findings = Audit();
            Assert.That(findings, Has.Some.Contains("Invalid authored service radius"));
            Assert.That(findings, Has.Some.Contains("no explicit parking bay"));
            Assert.That(findings, Has.Some.Contains("lacks gas station data"));
            Assert.That(findings, Has.Some.Contains("lacks store data"));
            Assert.That(findings, Has.Some.Contains("Toilet/restroom has no stable ID"));
        }

        [Test]
        public void AuthoringReportsDuplicateRestroomIds()
        {
            Create("Toilet A").AddComponent<TruckTaxiRestroomFixture>().SetStableId("restroom.shared");
            Create("Toilet B").AddComponent<TruckTaxiRestroomFixture>().SetStableId("restroom.shared");
            Assert.That(Audit(), Has.Some.Contains("Duplicate interaction ID: restroom.shared"));
        }

        [Test]
        public void HiddenAndOversizedSolidsAreReportedWithoutChangingAnyCollider()
        {
            var hidden = Cube("Hidden solid", Vector3.zero, Vector3.one);
            hidden.GetComponent<Renderer>().enabled = false;
            var oversized = Cube("Oversized solid", Vector3.right * 10, Vector3.one);
            oversized.GetComponent<BoxCollider>().size = Vector3.one * 8;
            var trigger = Cube("Invisible trigger", Vector3.right * 30, Vector3.one);
            trigger.GetComponent<Collider>().isTrigger = true;
            trigger.GetComponent<Renderer>().enabled = false;
            var findings = Audit();
            Assert.That(findings, Has.Some.Contains("DISABLED RENDERER / LIVE COLLIDER:"));
            Assert.That(findings, Has.Some.Contains("Solid bounds exceed visible geometry"));
            Assert.IsFalse(findings.Any(f => f.Contains("Invisible trigger")));
            Assert.IsTrue(hidden.GetComponent<Collider>().enabled);
            Assert.AreEqual(Vector3.one * 8, oversized.GetComponent<BoxCollider>().size);
            Assert.IsFalse(hidden.GetComponent<Renderer>().enabled);
            CollectionAssert.AreEqual(findings, Audit(), "Read-only repeated audit must produce the same findings.");
        }

        [Test]
        public void ForceHiddenRendererAndCoincidentColliderBoundsAreCandidatesOnly()
        {
            var a = Cube("Coincident A", Vector3.zero, Vector3.one * 2);
            var b = Cube("Coincident B", Vector3.zero, Vector3.one * 2);
            b.GetComponent<Renderer>().forceRenderingOff = true;
            var findings = Audit();
            Assert.That(findings, Has.Some.Contains("COINCIDENT SOLID BOUNDS CANDIDATE"));
            Assert.That(findings, Has.Some.Contains("DISABLED RENDERER / LIVE COLLIDER"));
            Assert.IsNotNull(a.GetComponent<Collider>());
            Assert.IsNotNull(b.GetComponent<Collider>());
        }

        [Test]
        public void CoplanarReviewReusesExistingRulesAndRejectsHeightSeparationAndEdgeTouch()
        {
            Cube("Surface A", Vector3.zero, new Vector3(10, .2f, 10));
            Cube("Surface B", Vector3.right * 3, new Vector3(10, .2f, 10));
            Cube("Raised surface", Vector3.up * .02f, new Vector3(10, .2f, 10));
            Cube("Edge touch only", Vector3.left * 10, new Vector3(10, .2f, 10));
            var findings = Audit().Where(f => f.Contains("COPLANAR TOP-BOUNDS") || f.Contains("DUPLICATE MESH CANDIDATE")).ToList();
            Assert.AreEqual(1, findings.Count);
            StringAssert.Contains("Surface A", findings[0]);
            StringAssert.Contains("Surface B", findings[0]);
            StringAssert.Contains("confirm triangles and co-residency", findings[0]);
        }

        [Test]
        public void ParkingClearanceFlagsWallButNotGroundBelowTheBay()
        {
            var service = Create("Parking service").AddComponent<TruckTaxiServicePoint>();
            service.stableId = "parking";
            Cube("Ground below bay", Vector3.down * .1f, new Vector3(20, .2f, 20));
            Cube("Wall in bay", new Vector3(0, 1.8f, 2), new Vector3(4, 3, .2f));
            var findings = Audit().Where(f => f.Contains("PARKING BAY CLEARANCE")).ToList();
            Assert.AreEqual(1, findings.Count);
            StringAssert.Contains("Wall in bay", findings[0]);
        }

        [Test]
        public void ImpactDescriptionContainsPathTypeLayerBoundsAndRendererState()
        {
            var parent = Create("Service building");
            var wall = Cube("Specific wall", new Vector3(1, 2, 3), new Vector3(4, 5, 6));
            wall.transform.SetParent(parent.transform);
            wall.GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();
            string report = TruckTaxiSignificantCollisionDiagnostic.DescribeCollider(wall.GetComponent<Collider>());
            StringAssert.Contains("Service building/Specific wall", report);
            StringAssert.Contains("UnityEngine.BoxCollider", report);
            StringAssert.Contains("layer=", report);
            StringAssert.Contains("boundsSize=(4.000, 5.000, 6.000)", report);
            StringAssert.Contains("renderer=MeshRenderer; enabled=False", report);
            StringAssert.Contains("duplicateStatus=UNVERIFIED", report);
            Assert.IsTrue(wall.GetComponent<Collider>().enabled);
        }

        [Test]
        public void SignificantImpactAcceptsSlowHardContactsButRejectsInvalidAndGentleContacts()
        {
            Assert.IsTrue(TruckTaxiSignificantCollisionDiagnostic.IsSignificant(.2f, 1600, .75f, 1500));
            Assert.IsTrue(TruckTaxiSignificantCollisionDiagnostic.IsSignificant(1, 100, .75f, 1500));
            Assert.IsFalse(TruckTaxiSignificantCollisionDiagnostic.IsSignificant(.2f, 100, .75f, 1500));
            Assert.IsFalse(TruckTaxiSignificantCollisionDiagnostic.IsSignificant(float.NaN, 1600, .75f, 1500));
            Assert.IsFalse(TruckTaxiSignificantCollisionDiagnostic.IsSignificant(1, float.PositiveInfinity, .75f, 1500));
        }

        [Test]
        public void ImpactThrottleLimitsBothRepeatedAndDifferentContactsWithBoundedMemory()
        {
            var gate = new TruckTaxiImpactLogThrottle();
            Assert.IsTrue(gate.TryAcquire(1, 0, 1, 5));
            Assert.IsFalse(gate.TryAcquire(2, .5f, 1, 5));
            Assert.IsTrue(gate.TryAcquire(2, 1, 1, 5));
            Assert.IsFalse(gate.TryAcquire(1, 2, 1, 5));
            Assert.IsTrue(gate.TryAcquire(1, 5, 1, 5));
            for (int i = 0; i < 1000; i++) Assert.IsTrue(gate.TryAcquire(i + 10, 10 + i, 1, 5));
            Assert.LessOrEqual(gate.TrackedCount, 64);
            gate.Clear();
            Assert.AreEqual(0, gate.TrackedCount);
            Assert.IsTrue(gate.TryAcquire(1, 0, 1, 5));
        }

        [Test]
        public void DisablingValidationArtRetiresOnlyTheTrackedInstanceAndRefreshIsIdempotent()
        {
            var generator = Create("Generator").AddComponent<TruckTaxiPhysicalFacilities>();
            var point = Create("Service").AddComponent<TruckTaxiServicePoint>();
            point.stableId = "service";
            var generated = TrackGenerated(generator, point);
            var authored = Cube(generated.name, Vector3.zero, Vector3.one);
            authored.transform.SetParent(point.transform);
            generator.Refresh();
            Assert.IsTrue(generated != null);
            point.generateValidationFacility = false;
            generator.Refresh();
            generator.Refresh();
            Assert.IsTrue(generated == null);
            Assert.AreEqual(0, generator.BuiltCount);
            Assert.IsTrue(authored != null && authored.GetComponent<Collider>().enabled);
        }

        private GameObject TrackGenerated(TruckTaxiPhysicalFacilities generator, TruckTaxiServicePoint point)
        {
            // Seed the generator's owned-instance ledger without booting the full Truck Taxi host.
            var generated = Cube("TT Validation Facility - test", Vector3.zero, Vector3.one);
            generated.transform.SetParent(point.transform);
            var ledger = (Dictionary<string, GameObject>)typeof(TruckTaxiPhysicalFacilities)
                .GetField("roots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(generator);
            ledger.Add(point.stableId, generated);
            return generated;
        }
        private List<string> Audit()
        {
            Physics.SyncTransforms();
            return TruckTaxiServiceMapValidator.ValidateAuthoring(owned);
        }
        private GameObject Create(string name)
        {
            var go = new GameObject(name); owned.Add(go); return go;
        }
        private GameObject Cube(string name, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(go);
            go.name = name; go.transform.position = position; go.transform.localScale = scale;
            return go;
        }
    }
}
