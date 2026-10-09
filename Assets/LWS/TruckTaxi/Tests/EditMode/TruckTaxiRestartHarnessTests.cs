using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests.EditMode
{
    // Pure tests: never initialize vendor storage, launch Unity, or create a directory.
    public sealed class TruckTaxiRestartHarnessTests
    {
        private const string Run = "1234567890abcdef1234567890abcdef";
        private static string Cache => Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TaxiRestartArgumentFixture"));
        private static string[] Args(string mode = "seed", string run = Run) => new[] {
            "player.exe", TruckTaxiRestartTestArguments.ModeFlag, mode, TruckTaxiRestartTestArguments.RunFlag, run };

        [Test]
        public void NormalLaunchDoesNotRequestOrResolveStorage()
        {
            Assert.IsFalse(TruckTaxiRestartTestArguments.IsRequested(new[] { "player.exe", "-batchmode" }));
            Assert.IsTrue(TruckTaxiRestartTestArguments.TryParse(new[] { "player.exe" }, null, out var request, out var failure));
            Assert.IsNull(request); Assert.IsNull(failure);
        }

        [TestCase("seed")]
        [TestCase("continue")]
        [TestCase("manual")]
        [TestCase("cleanup")]
        [TestCase("soak")]
        public void ValidModeUsesOnlyFixedCacheChild(string mode)
        {
            Assert.IsTrue(TruckTaxiRestartTestArguments.TryParse(Args(mode), Cache, out var request, out var failure), failure);
            Assert.AreEqual(mode, request.Mode);
            Assert.AreEqual(Path.Combine(Cache, "TruckTaxiRestartTests", Run), request.Root);
        }

        [TestCase("../normal-saves")]
        [TestCase("C:\\normal-saves")]
        [TestCase("12345678-90ab-cdef-1234-567890abcdef")]
        [TestCase("00000000000000000000000000000000")]
        [TestCase("1234567890abcdef1234567890abcdeg")]
        [TestCase("")]
        public void RunIdentifierCannotBeAPathOrMalformedGuid(string run)
        {
            Assert.IsFalse(TruckTaxiRestartTestArguments.TryParse(Args("seed", run), Cache, out var request, out var failure));
            Assert.IsNull(request); Assert.IsNotEmpty(failure);
        }

        [Test]
        public void DuplicateMissingAndConflictingFlagsFailClosed()
        {
            var cases = new[] {
                new[] { "player.exe", TruckTaxiRestartTestArguments.RunFlag, Run },
                new[] { "player.exe", TruckTaxiRestartTestArguments.ModeFlag },
                new[] { "player.exe", TruckTaxiRestartTestArguments.ModeFlag + "=seed", TruckTaxiRestartTestArguments.RunFlag, Run },
                new[] { "player.exe", TruckTaxiRestartTestArguments.ModeFlag, "seed", TruckTaxiRestartTestArguments.RunFlag, Run, TruckTaxiRestartTestArguments.RunFlag, Run },
                new[] { "player.exe", TruckTaxiRestartTestArguments.ModeFlag, "seed", TruckTaxiRestartTestArguments.RunFlag, Run, "-truck-taxi-smoke" },
                Args("delete"), Args("SEED")
            };
            foreach (var args in cases)
            {
                Assert.IsTrue(TruckTaxiRestartTestArguments.IsRequested(args));
                Assert.IsFalse(TruckTaxiRestartTestArguments.TryParse(args, Cache, out var request, out var failure));
                Assert.IsNull(request); Assert.IsNotEmpty(failure);
            }
        }

        [Test]
        public void CacheMustBeAbsoluteAndHexIsCanonicalized()
        {
            Assert.IsFalse(TruckTaxiRestartTestArguments.TryParse(Args(), "relative", out _, out _));
            Assert.IsTrue(TruckTaxiRestartTestArguments.TryParse(Args("seed", Run.ToUpperInvariant()), Cache, out var request, out _));
            Assert.AreEqual(Run, request.RunId);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void SessionSignatureObeysExistingStatisticsAndExposureRules(int signature)
        {
            var snapshot = TruckTaxiRestartHarness.CreateSessionFixture(signature);
            Assert.IsTrue(TruckTaxiSession.ValidatePersistentState(snapshot, out var reason), reason);
            Assert.AreEqual(123400 + signature, snapshot.shiftEarningsCents);
            Assert.AreEqual(2, snapshot.statistics.completedRides);
            Assert.AreEqual(9, snapshot.statistics.totalStarsEarned);
            Assert.AreEqual(2, snapshot.passengerSelection.passengers[0].timesCompleted);
            Assert.IsFalse(snapshot.rideRequestsEnabled);
        }

        [Test]
        public void UnknownSignatureRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TruckTaxiRestartHarness.CreateSessionFixture(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => TruckTaxiRestartHarness.CreateSessionFixture(4));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DamageSignatureUsesExistingBodyPartAndValidSnapshot(int signature)
        {
            var parts = new Dictionary<string, TruckTaxiDamagePart> {
                { "engine", new TruckTaxiDamagePart("engine", "Engine", TruckTaxiDamageCategory.Engine, 100) },
                { "body.rear", new TruckTaxiDamagePart("body.rear", "Rear", TruckTaxiDamageCategory.Body, 100) },
                { "body.cab", new TruckTaxiDamagePart("body.cab", "Cab", TruckTaxiDamageCategory.Body, 100) }
            };
            string id = TruckTaxiRestartHarness.SelectBodyPart(parts);
            Assert.AreEqual("body.cab", id);
            float condition = TruckTaxiRestartHarness.DamageCondition(signature);
            Assert.That(condition, Is.GreaterThan(0).And.LessThan(1));
            var snapshot = new TruckTaxiDamageSnapshot { VehicleId = "fixture.truck", Parts = new[] {
                new TruckTaxiDamageEntry { Id = id, Condition = condition } } };
            Assert.IsTrue(TruckTaxiComponentDamage.ValidSnapshot(snapshot, "fixture.truck", parts));
            if (signature > 0) Assert.AreNotEqual(TruckTaxiRestartHarness.DamageCondition(signature - 1), condition);
        }

        [Test]
        public void MissingBodyPartFailsInsteadOfInventingDamage()
        {
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.SelectBodyPart(new Dictionary<string, TruckTaxiDamagePart>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => TruckTaxiRestartHarness.DamageCondition(4));
        }

        [TestCase(0, "alpha")]
        [TestCase(1, "beta")]
        [TestCase(2, "zeta")]
        [TestCase(3, "alpha")]
        public void AuthoredPoseSelectionIsSortedNonSpawnAndRegionBound(int signature, string expected)
        {
            var roots = new List<GameObject>();
            try
            {
                var zeta = Location(roots, "zeta", 180);
                var alpha = Location(roots, "alpha", 80);
                var beta = Location(roots, "beta", 130);
                var nearSpawn = Location(roots, "00-near", 10);
                var otherRegion = Location(roots, "00-other", 600);
                var sources = new[] { zeta, nearSpawn, otherRegion, beta, alpha };
                var selected = TruckTaxiRestartHarness.SelectPoseLocation(sources, Vector3.zero, p => p.x < 500, signature);
                Assert.AreEqual(expected, selected.locationId);
                Array.Reverse(sources);
                Assert.AreSame(selected, TruckTaxiRestartHarness.SelectPoseLocation(sources, Vector3.zero, p => p.x < 500, signature));
                Assert.GreaterOrEqual(selected.StopPosition.x, 50);
            }
            finally { foreach (var root in roots) UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void MissingOrDuplicateAuthoredPoseFailsClosed()
        {
            var roots = new List<GameObject>();
            try
            {
                var a = Location(roots, "duplicate", 80);
                var b = Location(roots, "duplicate", 100);
                Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.SelectPoseLocation(new[] { a, b }, Vector3.zero, _ => true, 0));
                Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.SelectPoseLocation(new[] { a }, Vector3.zero, _ => false, 0));
                a.truckStopPoint = null;
                Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.SelectPoseLocation(new[] { a }, Vector3.zero, _ => true, 0));
            }
            finally { foreach (var root in roots) UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void SessionFixturePreservesAcknowledgedCareerIdentity(int signature)
        {
            var identity = new TruckTaxiCareerProfileData { runProfileId = "fixture.profile",
                driverName = "Fixture Driver", firstGuideEligible = true, firstGuideClaimed = true };
            var captured = new TruckTaxiPersistentState { ownedProfile = identity };
            var fixture = TruckTaxiRestartHarness.CreateSessionFixture(signature, captured);
            Assert.AreSame(identity, fixture.ownedProfile);
            Assert.AreEqual("fixture.profile", fixture.ownedProfile.runProfileId);
            Assert.AreEqual("Fixture Driver", fixture.ownedProfile.driverName);
            Assert.IsTrue(fixture.ownedProfile.firstGuideClaimed);
        }

        private static TruckTaxiRideLocation Location(List<GameObject> roots, string id, float x)
        {
            var root = new GameObject("Authored pose fixture " + id); roots.Add(root);
            root.transform.position = new Vector3(x, 0, 0);
            var location = root.AddComponent<TruckTaxiRideLocation>();
            location.locationId = id; location.truckStopPoint = root.transform;
            return location;
        }
    }
}
