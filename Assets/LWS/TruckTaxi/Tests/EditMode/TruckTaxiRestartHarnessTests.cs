using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LWS.InterstateHauler;

namespace LWS.TruckTaxi.Tests.EditMode
{
    // Isolated tests: never initialize vendor storage, launch a player, or create a directory.
    public sealed class TruckTaxiRestartHarnessTests
    {
        private const string Run = "1234567890abcdef1234567890abcdef";
        private static string Cache => Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TaxiRestartArgumentFixture"));
        private static string[] Args(string mode = "seed", string run = Run) => new[] {
            "player.exe", TruckTaxiRestartTestArguments.ModeFlag, mode, TruckTaxiRestartTestArguments.RunFlag, run };

        [TestCase("valid")]
        [TestCase("legacy-only")]
        [TestCase("duplicate")]
        [TestCase("missing-mount")]
        [TestCase("disabled")]
        [TestCase("role")]
        [TestCase("seat-type")]
        [TestCase("non-travel")]
        [TestCase("unshared-primary")]
        [TestCase("overlap")]
        [TestCase("missing-driver")]
        [TestCase("all-shared")]
        [TestCase("two-exclusive")]
        [TestCase("private-exclusive")]
        [TestCase("exclusive-seat-type")]
        public void StarterCapacityRequiresEightActualCompatibleRegistrySlots(string fault)
        {
            var truck = new GameObject("Restart occupancy fixture");
            try
            {
                var cab = new GameObject("Cab"); cab.transform.SetParent(truck.transform, false);
                var rig = truck.AddComponent<TruckTaxiCabOccupantRig>();
                var anchors = TruckTaxiEightOccupantLayout.CreateInterstateTractorAnchors();
                anchors[4].allowedOccupants = TruckTaxiCabOccupantKind.Companion;
                switch (fault)
                {
                    case "legacy-only": anchors = new[] { anchors[4] }; break;
                    case "duplicate": anchors[3].slotId = anchors[2].slotId; break;
                    case "missing-mount": anchors[2].mountPath = "Missing"; break;
                    case "disabled": rig.enabled = false; break;
                    case "role": anchors[2].allowedOccupants = TruckTaxiCabOccupantKind.Passenger; break;
                    case "seat-type": anchors[2].allowedSeatTypes = new[] { TruckTaxiSeatType.TinyPassenger }; break;
                    case "non-travel": anchors[2].secondaryOccupantSeat = false; break;
                    case "unshared-primary": anchors[1].sharedPrimarySeat = false; break;
                    case "overlap": anchors[3].localPosition = anchors[2].localPosition; break;
                    case "missing-driver": anchors[0].id = TruckTaxiCabAnchorId.FloorPerch; break;
                    case "all-shared": anchors[4].allowedOccupants = anchors[2].allowedOccupants; break;
                    case "two-exclusive": anchors[2].allowedOccupants = TruckTaxiCabOccupantKind.Companion; break;
                    case "private-exclusive": anchors[4].privateEventEligible = true; break;
                    case "exclusive-seat-type": anchors[4].allowedSeatTypes = new[] { TruckTaxiSeatType.StandardPassenger }; break;
                }
                rig.Configure(true, anchors);
                string error = TruckTaxiRestartHarness.StarterOccupancyError(rig);
                if (fault == "valid") Assert.IsNull(error, error);
                else Assert.IsNotNull(error, "Displayed maximum must not hide fault=" + fault);
            }
            finally { UnityEngine.Object.DestroyImmediate(truck); }
        }

        [Test]
        public void ActualTaxiPrefabRetainsMixedStarterContractAndIdentity()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab");
            Assert.IsNotNull(prefab);
            var authored = prefab.GetComponent<TruckTaxiCabOccupantRig>();
            Assert.IsNotNull(authored);
            var sourceCab = prefab.transform.Find("Cab"); Assert.IsNotNull(sourceCab);
            var truck = new GameObject("Restart actual prefab occupancy fixture");
            try
            {
                // Copy only real occupancy/mount data; never instantiate the NWH vehicle in EditMode.
                var cab = new GameObject("Cab").transform; cab.SetParent(truck.transform, false);
                cab.localPosition = sourceCab.localPosition; cab.localRotation = sourceCab.localRotation; cab.localScale = sourceCab.localScale;
                var rig = truck.AddComponent<TruckTaxiCabOccupantRig>();
                UnityEditor.EditorJsonUtility.FromJsonOverwrite(UnityEditor.EditorJsonUtility.ToJson(authored), rig);
                Assert.AreEqual(1, rig.Anchors.Count, "Exercise the actual sparse authored fallback, not an invented eight-seat fixture.");
                var original = rig.Anchors[0];
                string originalId = original.StableSlotId;
                Assert.AreEqual(TruckTaxiCabAnchorId.SleeperRight, original.id);
                Assert.AreEqual(TruckTaxiCabOccupantKind.Companion, original.allowedOccupants);
                Assert.IsTrue(original.secondaryOccupantSeat); Assert.IsFalse(original.privateEventEligible);
                var identity = truck.AddComponent<LwsVehicleIdentity>();
                identity.Configure("player.truck.starter", TruckTaxiEightOccupantLayout.FallbackDefinitionId,
                    LwsVehicleRole.PlayerTractor, "Taxi fallback", true);
                var player = truck.AddComponent<LwsPlayerTruck>(); player.Configure(null, "player.truck.starter");
                Assert.IsTrue(TruckTaxiEightOccupantLayout.TryApplyToConfiguredStarter(player, out var reason), reason);
                Assert.IsNull(TruckTaxiRestartHarness.StarterOccupancyError(rig));
                Assert.AreSame(original, rig.Anchors[0]); Assert.AreEqual(originalId, original.StableSlotId);
                Assert.AreEqual(TruckTaxiCabOccupantKind.Companion, original.allowedOccupants);
                Assert.IsFalse(original.privateEventEligible);
                Assert.AreEqual(authored.AuthoredSleeperCapability, rig.AuthoredSleeperCapability);
                Assert.AreEqual("player.truck.starter", player.VehicleId);
                Assert.AreEqual(TruckTaxiEightOccupantLayout.FallbackDefinitionId, player.DefinitionId);
                Assert.AreEqual(TruckTaxiEightOccupantLayout.FallbackDefinitionId, identity.DefinitionId);
            }
            finally { UnityEngine.Object.DestroyImmediate(truck); }
        }

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
        [TestCase("interactive")]
        public void ValidModeUsesOnlyFixedCacheChild(string mode)
        {
            Assert.IsTrue(TruckTaxiRestartTestArguments.TryParse(Args(mode), Cache, out var request, out var failure), failure);
            Assert.AreEqual(mode, request.Mode);
            Assert.IsFalse(request.Drive, "Legacy signature/soak behavior must remain opt-in unchanged.");
            Assert.AreEqual(Path.Combine(Cache, "TruckTaxiRestartTests", Run), request.Root);
        }

        [TestCase("seed")]
        [TestCase("continue")]
        [TestCase("manual")]
        public void DriveFlagUsesSameStrictDisposableRoot(string mode)
        {
            var args = new List<string>(Args(mode)) { TruckTaxiRestartTestArguments.DriveFlag };
            Assert.IsTrue(TruckTaxiRestartTestArguments.TryParse(args.ToArray(), Cache, out var request, out var failure), failure);
            Assert.IsTrue(request.Drive);
            Assert.AreEqual(Path.Combine(Cache, "TruckTaxiRestartTests", Run), request.Root);
        }

        [TestCase("soak")]
        [TestCase("cleanup")]
        [TestCase("interactive")]
        public void DriveCannotSilentlyChangeAnotherHarnessMode(string mode)
        {
            var args = new List<string>(Args(mode)) { TruckTaxiRestartTestArguments.DriveFlag };
            Assert.IsFalse(TruckTaxiRestartTestArguments.TryParse(args.ToArray(), Cache, out _, out _));
        }

        [Test]
        public void DriveAloneDuplicateAndValueSyntaxFailClosed()
        {
            var args = new[] { TruckTaxiRestartTestArguments.DriveFlag };
            Assert.IsTrue(TruckTaxiRestartTestArguments.IsRequested(args));
            Assert.IsFalse(TruckTaxiRestartTestArguments.TryParse(args, Cache, out _, out _));
            var duplicate = new List<string>(Args()) { TruckTaxiRestartTestArguments.DriveFlag, TruckTaxiRestartTestArguments.DriveFlag };
            Assert.IsFalse(TruckTaxiRestartTestArguments.TryParse(duplicate.ToArray(), Cache, out _, out _));
            var malformed = new List<string>(Args()) { TruckTaxiRestartTestArguments.DriveFlag + "=true" };
            Assert.IsFalse(TruckTaxiRestartTestArguments.TryParse(malformed.ToArray(), Cache, out _, out _));
        }

        [Test]
        public void DriveRequiresUnpausedUnsuppressedDynamicSingleAuthority()
        {
            Assert.DoesNotThrow(() => TruckTaxiRestartHarness.ValidateDriveLifecycle(false, 1, false, true, true, false, 1, 1, 1));
            for (int fault = 0; fault < 6; fault++)
            {
                int f = fault;
                Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.ValidateDriveLifecycle(
                    f == 0, f == 1 ? 0 : 1, f == 2, f != 3, f != 4, f == 5, 1, 1, 1), "fault=" + f);
            }
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.ValidateDriveLifecycle(false, float.NaN, false, true, true, false, 1, 1, 1));
        }

        [TestCase(0)]
        [TestCase(2)]
        public void MissingOrDuplicateInputAuthoritiesNeverCountAsDrivable(int count)
        {
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.ValidateDriveLifecycle(false, 1, false, true, true, false, count, 1, 1));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.ValidateDriveLifecycle(false, 1, false, true, true, false, 1, count, 1));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.ValidateDriveLifecycle(false, 1, false, true, true, false, 1, 1, count));
        }

        [Test]
        public void ContinuousMotionAllowsFloatingOriginIndependentGlobalProgress()
        {
            Vector3 start = new Vector3(15000, 2, -14000), previous = start + Vector3.forward * 5;
            Assert.DoesNotThrow(() => TruckTaxiRestartHarness.ValidateDriveMotion(previous, previous + Vector3.forward * .2f,
                2, 2, .1f, start, Vector3.forward));
        }

        [TestCase(0, 0, 10, .02f)] // Teleport cannot prove motion.
        [TestCase(2, 0, 0, 1)] // Outside the validated lane corridor.
        [TestCase(0, 3, 0, 1)] // Airborne/drop evidence is not road driving.
        [TestCase(0, 0, -2, 1)]
        [TestCase(0, 0, 41, 1)]
        [TestCase(0, 0, 0, 0)]
        public void UnsafeOrDiscontinuousMotionFails(float x, float y, float z, float seconds)
        {
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.ValidateDriveMotion(Vector3.zero,
                new Vector3(x, y, z), 3, 3, seconds, Vector3.zero, Vector3.forward));
        }

        [Test]
        public void ContinuousMotionStillCannotLeaveTheValidatedTwentyFiveMeterCorridor()
        {
            var previous = Vector3.forward * 24;
            Assert.DoesNotThrow(() => TruckTaxiRestartHarness.ValidateDriveMotion(previous, Vector3.forward * 25,
                2, 2, 1, Vector3.zero, Vector3.forward));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.ValidateDriveMotion(previous, Vector3.forward * 26,
                2, 2, 1, Vector3.zero, Vector3.forward));
        }

        [Test]
        public void LongStartupFrameDoesNotCountAsADriveFailureOrPermitTeleportation()
        {
            Assert.DoesNotThrow(() => TruckTaxiRestartHarness.ValidateDriveMotion(Vector3.zero, Vector3.forward * .01f,
                .05f, .02f, 1.2f, Vector3.zero, Vector3.forward));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.ValidateDriveMotion(Vector3.zero, Vector3.forward * 10,
                .05f, .02f, 1.2f, Vector3.zero, Vector3.forward));
        }

        [Test]
        public void OtherRegionSeedUsesAuthoredSampleAndLaneOffset()
        {
            var edge = DriveEdge();
            Assert.IsTrue(TruckTaxiRestartHarness.TryDriveEdgeSeed(edge, out var point, out var forward));
            Assert.AreEqual(new Vector3(4030, .15f, -4.5f), point);
            Assert.AreEqual(Vector3.right, forward);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void OtherRegionSeedRejectsUnsupportedOrUnboundedGeometry(int fault)
        {
            var edge = DriveEdge();
            if (fault == 0) edge.laneCenterOffsetsMeters.Clear();
            if (fault == 1) edge.distanceMeters = 100;
            if (fault == 2) edge.samples[0].forward = Vector3.zero;
            if (fault == 3) edge.samples[0].position = new Vector3(float.NaN, 0, 0);
            if (fault == 4) edge.restrictions.Add(new LwsRoadRestriction { truckAllowed = false });
            Assert.IsFalse(TruckTaxiRestartHarness.TryDriveEdgeSeed(edge, out _, out _));
        }

        private static LwsRoadEdge DriveEdge() => new LwsRoadEdge {
            oneWay = true, roadClass = LwsRoadClass.LocalRoad, surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
            laneCount = 1, laneWidthMeters = 5, distanceMeters = 160, laneCenterOffsetsMeters = new List<float> { 4.5f },
            samples = new List<LwsRoadSample> { new LwsRoadSample { position = new Vector3(4030, .15f, 0),
                forward = Vector3.right, distanceFromStartMeters = 30 } }
        };

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
            var entries = new List<TruckTaxiDamageEntry>();
            foreach (var part in parts)
            {
                float expected = part.Key == id ? condition : .75f - signature * .02f;
                float actual = TruckTaxiRestartHarness.DamagePartCondition(signature, part.Key, id);
                Assert.AreEqual(expected, actual, .00001f, part.Key);
                entries.Add(new TruckTaxiDamageEntry { Id = part.Key, Condition = actual });
                typeof(TruckTaxiDamagePart).GetProperty(nameof(TruckTaxiDamagePart.Condition)).SetValue(part.Value, actual);
            }
            var snapshot = new TruckTaxiDamageSnapshot { VehicleId = "fixture.truck", Parts = entries.ToArray() };
            Assert.AreEqual(parts.Count, snapshot.Parts.Length);
            Assert.IsTrue(TruckTaxiComponentDamage.ValidSnapshot(snapshot, "fixture.truck", parts));
            Assert.That(TruckTaxiComponentDamage.CalculateSummary(parts.Values), Is.InRange(.2f, .5f));
            if (signature > 0) Assert.AreNotEqual(TruckTaxiRestartHarness.DamageCondition(signature - 1), condition);
        }

        [Test]
        public void MissingBodyPartFailsInsteadOfInventingDamage()
        {
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.SelectBodyPart(new Dictionary<string, TruckTaxiDamagePart>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => TruckTaxiRestartHarness.DamageCondition(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => TruckTaxiRestartHarness.DamagePartCondition(4, "engine", "body.cab"));
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
            var captured = new TruckTaxiPersistentState { ownedProfile = identity,
                lastSession = new TruckTaxiLastSessionStatistics { earningsCents = 3000000, rides = 50, totalStars = 200 } };
            var fixture = TruckTaxiRestartHarness.CreateSessionFixture(signature, captured);
            Assert.AreSame(identity, fixture.ownedProfile);
            Assert.AreEqual(0, fixture.lastSession.earningsCents, "Fixture lifetime replacement must not retain previous UI exercise totals.");
            Assert.AreEqual(0, fixture.lastSession.rides);
            Assert.AreEqual(0, fixture.lastSession.totalStars);
            Assert.AreEqual("fixture.profile", fixture.ownedProfile.runProfileId);
            Assert.AreEqual("Fixture Driver", fixture.ownedProfile.driverName);
            Assert.IsTrue(fixture.ownedProfile.firstGuideClaimed);
        }

        private static TruckTaxiSnowSnapshot SnowTopology() => new TruckTaxiSnowSnapshot {
            topology = 719, maximumDepth = .6f, backgroundDepth = .01f, depths = new float[7] };

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void EnvironmentSignatureUsesExistingContractsAndDoesNotMutateCapturedTopology(int signature)
        {
            var topology = SnowTopology();
            var fixture = TruckTaxiRestartHarness.CreateEnvironmentFixture(signature, topology);
            var preset = TruckTaxiRestartHarness.CreateWeatherFixture(signature);
            Assert.IsTrue(TruckTaxiEnvironmentSnapshot.Valid(fixture));
            Assert.IsTrue(preset.Validate(out var message), message);
            Assert.AreEqual(topology.topology, fixture.snow.topology);
            Assert.AreEqual(topology.maximumDepth, fixture.snow.maximumDepth);
            Assert.AreEqual(topology.depths.Length, fixture.snow.depths.Length);
            Assert.AreNotSame(topology.depths, fixture.snow.depths);
            CollectionAssert.AreEqual(new float[7], topology.depths);
            Assert.Greater(fixture.snow.depths[0], 0);
            Assert.AreNotEqual(fixture.snow.depths[0], fixture.snow.depths[1]);
            Assert.AreEqual(signature % 2 == 1, fixture.frozen);
            Assert.AreEqual(signature % 2 == 1, fixture.forecast.automatic);
            Assert.IsNull(TruckTaxiRestartHarness.EnvironmentSignatureError(signature, fixture, preset.ToSnapshot(14), topology));
            Assert.That(TruckTaxiRestartHarness.SignatureChestShape(signature), Is.InRange(-1f, 1f));
            if (signature > 0) Assert.AreNotEqual(TruckTaxiRestartHarness.SignatureChestShape(signature - 1), TruckTaxiRestartHarness.SignatureChestShape(signature));
        }

        [TestCase("frozen")] [TestCase("automatic")] [TestCase("override")] [TestCase("snow cell")]
        [TestCase("snow background")] [TestCase("topology")] [TestCase("missing snow")]
        [TestCase("weather")] [TestCase("intensity")] [TestCase("temperature")]
        [TestCase("wind")] [TestCase("visibility")] [TestCase("transition")]
        public void EnvironmentSignatureRejectsRestoredDefaultsAndIndividualFieldLoss(string corruption)
        {
            var topology = SnowTopology();
            var fixture = TruckTaxiRestartHarness.CreateEnvironmentFixture(3, topology);
            var weather = TruckTaxiRestartHarness.CreateWeatherFixture(3).ToSnapshot(14);
            switch (corruption)
            {
                case "frozen": fixture.frozen = false; break;
                case "automatic": fixture.forecast.automatic = false; break;
                case "override": fixture.forecast.overrideEndTicks++; break;
                case "snow cell": fixture.snow.depths[5] = 0; break;
                case "snow background": fixture.snow.backgroundDepth = 0; break;
                case "topology": fixture.snow.topology++; break;
                case "missing snow": fixture.hasSnowState = false; fixture.snow = null; break;
                case "weather": weather = LwsWeatherSnapshot.Clear; break;
                case "intensity": weather.precipitationIntensity01 = .1f; break;
                case "temperature": weather.ambientTemperatureC = 18; break;
                case "wind": weather.windVelocity = Vector3.zero; break;
                case "visibility": weather.visibilityMeters = float.NaN; break;
                case "transition": weather.transitioning = true; break;
            }
            Assert.IsNotNull(TruckTaxiRestartHarness.EnvironmentSignatureError(3, fixture, weather, topology), corruption);
        }

        [Test] public void EnvironmentFixtureRequiresRealTopologyAndKnownSignature()
        {
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.CreateEnvironmentFixture(0, null));
            var empty = SnowTopology(); empty.depths = Array.Empty<float>();
            Assert.Throws<InvalidOperationException>(() => TruckTaxiRestartHarness.CreateEnvironmentFixture(0, empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => TruckTaxiRestartHarness.CreateEnvironmentFixture(4, SnowTopology()));
            Assert.Throws<ArgumentOutOfRangeException>(() => TruckTaxiRestartHarness.CreateWeatherFixture(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => TruckTaxiRestartHarness.SignatureChestShape(4));
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
