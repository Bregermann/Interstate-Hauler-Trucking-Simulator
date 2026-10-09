using System;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Trailer;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiFlagshipRaceTests
    {
        private LwsRoadGraph graph;
        private LwsTrafficLaneDefinition[] lanes;
        private GameObject fixture;
        private GameObject playerObject;

        [SetUp]
        public void Setup()
        {
            // Exercise the actual regional authoring output, not a race-specific invented road.
            graph = TruckTaxiDemoBuilder.CreateGraph();
            typeof(TruckTaxiRegionalAuthoring).GetMethod("ExtendGraph", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { graph });
            // ExtendGraph also discovers optional service stops in the open editor scene.
            // Exclude only those ambient spurs so this fixture cannot depend on the user's open scene.
            var ambient = new HashSet<string>();
            foreach (var node in graph.nodes)
                if (node.nodeId.StartsWith("taxi.station.", StringComparison.Ordinal) || node.nodeId == "taxi.service.river") ambient.Add(node.nodeId);
            graph.nodes.RemoveAll(node => ambient.Contains(node.nodeId));
            graph.edges.RemoveAll(edge => ambient.Contains(edge.fromNodeId) || ambient.Contains(edge.toNodeId));
            fixture = new GameObject("Flagship authoring fixture"); fixture.SetActive(false);
            var traffic = fixture.AddComponent<TruckTaxiTrafficAdapter>();
            traffic.cityLanes = Array.Empty<LwsTrafficLaneDefinition>();
            typeof(TruckTaxiRegionalAuthoring).GetMethod("ExtendTraffic", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { traffic });
            lanes = traffic.cityLanes;
        }

        [TearDown] public void Cleanup()
        {
            if (fixture != null) Object.DestroyImmediate(fixture);
            if (playerObject != null)
            {
                var vehicle = playerObject.GetComponent<VehicleController>();
                if (vehicle != null) SetField(vehicle, "_isInitialized", false);
                Object.DestroyImmediate(playerObject);
            }
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static object Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        private TruckTaxiBootstrap Host(out TruckTaxiRaceRuntime race)
        {
            var root = new GameObject("Separate authored host"); root.transform.SetParent(fixture.transform);
            var host = root.AddComponent<TruckTaxiBootstrap>();
            host.traffic = fixture.GetComponent<TruckTaxiTrafficAdapter>();
            host.roadGraph = root.AddComponent<LwsRoadGraphProvider>(); host.roadGraph.SetGraph(graph, false);
            race = root.AddComponent<TruckTaxiRaceRuntime>(); SetField(race, "host", host);
            return host;
        }

        private VehicleController Driver(TruckTaxiBootstrap host, TruckTaxiRaceRuntime race)
        {
            // Raw AddComponent runs NWH OnValidate before StateSettings can be assigned.
            // Clone the configured project tractor under the inactive fixture instead.
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab");
            Assert.IsNotNull(prefab, "Configured NWH tractor fixture is required.");
            playerObject = Object.Instantiate(prefab, fixture.transform);
            playerObject.name = "Race eligibility player";
            playerObject.SetActive(false); playerObject.transform.SetParent(null);
            var vehicle = playerObject.GetComponent<VehicleController>();
            Assert.IsNotNull(vehicle);
            Assert.IsNotNull(vehicle.stateSettings, "Fixture must satisfy NWH construction validation.");
            Assert.Greater(vehicle.stateSettings.LODs.Count, 0);
            var adapter = playerObject.GetComponent<LwsNwhVehicleAdapter>() ?? playerObject.AddComponent<LwsNwhVehicleAdapter>();
            var player = playerObject.GetComponent<LwsPlayerTruck>() ?? playerObject.AddComponent<LwsPlayerTruck>();
            SetField(adapter, "vehicleController", vehicle); SetField(player, "nwhAdapter", adapter);
            SetField(host, "<Player>k__BackingField", player);
            vehicle.vehicleRigidbody = playerObject.GetComponent<Rigidbody>();
            SetField(race, "playerBody", vehicle.vehicleRigidbody);
            playerObject.SetActive(true);
            // EditMode fixture supplies the readiness flag only; it does not simulate NWH physics.
            SetField(vehicle, "_isInitialized", true);
            return vehicle;
        }

        [Test]
        public void RuntimeAndAuthoringUseSeparateAuthoredTrafficReference()
        {
            var host = Host(out var race);
            Assert.IsNull(host.GetComponent<TruckTaxiTrafficAdapter>());
            Assert.IsTrue(race.RefreshFlagshipRoute(), race.FlagshipUnavailableReason);
            var previous = UnityEditor.Selection.activeObject;
            try
            {
                UnityEditor.Selection.activeGameObject = host.gameObject;
                Assert.DoesNotThrow(TruckTaxiRaceAuthoring.ValidateFlagship);
            }
            finally { UnityEditor.Selection.activeObject = previous; }
            host.traffic = null;
            Assert.IsFalse(race.RefreshFlagshipRoute(), "Missing authored reference must not fall back to scene search.");
        }

        [TestCase("uninitialized")]
        [TestCase("disabled")]
        [TestCase("kinematic")]
        [TestCase("mismatched")]
        [TestCase("no-input")]
        [TestCase("no-controller")]
        public void EligibilityRequiresReadyCanonicalDynamicNwhDriver(string failure)
        {
            var host = Host(out var race); var vehicle = Driver(host, race);
            Assert.IsNull(Invoke(race, "PlayerBlockReason"));
            switch (failure)
            {
                case "uninitialized": SetField(vehicle, "_isInitialized", false); break;
                case "disabled": vehicle.enabled = false; break;
                case "kinematic": vehicle.vehicleRigidbody.isKinematic = true; break;
                case "mismatched": SetField(race, "playerBody", fixture.AddComponent<Rigidbody>()); break;
                case "no-input": vehicle.input = null; break;
                case "no-controller": SetField(host.Player.NwhAdapter, "vehicleController", null); break;
            }
            Assert.IsNotNull(Invoke(race, "PlayerBlockReason"));
            Vector3 before = playerObject.transform.position;
            Assert.IsFalse((bool)Invoke(race, "PlacePlayerAt", Vector3.one * 100, true));
            Assert.AreEqual(before, playerObject.transform.position);
        }

        [TestCase("countdown")]
        [TestCase("placement")]
        [TestCase("reset")]
        [TestCase("debug")]
        public void NewCouplingCancelsBeforeAnyPlayerRelocation(string operation)
        {
            var host = Host(out var race); var vehicle = Driver(host, race);
            Assert.IsNull(Invoke(race, "PlayerBlockReason"));
            var body = vehicle.vehicleRigidbody;
            var originalConstraints = body.constraints;
            var restraint = new TruckTaxiRaceCountdownLock(body, vehicle.input);
            SetField(race, "playerCountdownLock", restraint);
            SetField(race, "<IsActive>k__BackingField", true);
            SetField(race, "countdown", operation == "countdown");
            var hitch = playerObject.GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            Assert.IsNotNull(hitch, "Configured tractor fixture must include its NWH hitch.");
            Assert.IsNotNull(hitch.module.attachmentPoint);
            hitch.module.attached = true;
            Vector3 requestedPosition = new Vector3(8, 3, 11);
            body.position = requestedPosition;
            Vector3 frozenPosition = body.position;
            // Measure only the native constraint-release effect on this configured NWH body's COM.
            // Dispose must match this control exactly; no broad position tolerance hides a race move.
            body.constraints = originalConstraints;
            Vector3 constraintOnlyPosition = body.position;
            Vector3 constraintDelta = constraintOnlyPosition - frozenPosition;
            float roundingBudget = 16 * 1.1920929e-7f * Mathf.Max(1, frozenPosition.magnitude + body.centerOfMass.magnitude);
            Assert.LessOrEqual(constraintDelta.magnitude, roundingBudget,
                "Constraint-only control exceeded a bounded single-precision roundoff budget: " + constraintDelta.ToString("G9"));
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.position = requestedPosition;
            Vector3 attachedPosition = body.position;
            Assert.AreEqual(frozenPosition, attachedPosition, "Constraint-release control must reproduce the same input pose.");
            if (operation == "countdown") Invoke(race, "HoldCountdown");
            else if (operation == "placement") Assert.IsFalse((bool)Invoke(race, "PlacePlayerAt", Vector3.one * 100, false));
            else if (operation == "reset") Assert.IsFalse(race.ResetToTrack());
            else Assert.IsFalse(race.DebugTeleportToCheckpoint(0));
            Assert.IsFalse(race.IsActive);
            Vector3 actualPosition = body.position;
            Debug.Log("Race cancellation " + operation + " COM=" + body.centerOfMass.ToString("G9") +
                " frozen=" + attachedPosition.ToString("G9") + " constraintOnly=" + constraintOnlyPosition.ToString("G9") +
                " actual=" + actualPosition.ToString("G9") + " nativeDelta=" + constraintDelta.ToString("G9") +
                " raceDelta=" + (actualPosition - constraintOnlyPosition).ToString("G9"));
            Assert.AreEqual(constraintOnlyPosition, actualPosition,
                "Cancellation may only release the native constraints, never hold or relocate the coupled tractor. " +
                "Race-only delta=" + (actualPosition - constraintOnlyPosition).ToString("G9"));
            Assert.AreEqual(originalConstraints, body.constraints);
            StringAssert.Contains("TRAILER", race.Feedback);
            hitch.module.attached = false;
        }

        [TestCase("capture")]
        [TestCase("fuel")]
        [TestCase("tow")]
        [TestCase("travel")]
        public void ExistingCaptureAndRecoveryAuthoritiesBlockRaceMovement(string authority)
        {
            var host = Host(out var race); var vehicle = Driver(host, race);
            Assert.IsNull(Invoke(race, "PlayerBlockReason"));
            if (authority == "capture")
            {
                var owner = host.gameObject.AddComponent<TruckTaxiKaijuDirector>();
                SetField(host, "<Kaiju>k__BackingField", owner); SetField(owner, "captureActive", true);
            }
            else if (authority == "fuel")
            {
                var owner = host.gameObject.AddComponent<TruckTaxiFuelController>();
                SetField(host, "<Fuel>k__BackingField", owner); SetField(owner, "<IsRescuing>k__BackingField", true);
            }
            else if (authority == "tow")
            {
                var owner = host.gameObject.AddComponent<TruckTaxiRoadsideAssistance>();
                SetField(host, "<Roadside>k__BackingField", owner); SetField(owner, "<IsRecovering>k__BackingField", true);
            }
            else
            {
                var owner = host.gameObject.AddComponent<TruckTaxiFastTravel>();
                SetField(host, "<FastTravel>k__BackingField", owner); SetField(owner, "<IsTravelling>k__BackingField", true);
            }
            SetField(race, "<IsActive>k__BackingField", true);
            Vector3 before = vehicle.vehicleRigidbody.position;
            Assert.IsFalse((bool)Invoke(race, "PlacePlayerAt", Vector3.one * 100, false));
            Assert.IsFalse(race.IsActive); Assert.AreEqual(before, vehicle.vehicleRigidbody.position);
            Assert.IsNotEmpty(race.Feedback);
            // Avoid asking the fixture's uninitialized recovery components to perform cleanup work.
            if (host.Kaiju != null) SetField(host.Kaiju, "captureActive", false);
            if (host.FastTravel != null) SetField(host.FastTravel, "<IsTravelling>k__BackingField", false);
        }

        private TruckTaxiFlagshipRoute Route()
        {
            Assert.IsTrue(TruckTaxiFlagshipRoute.TryCreate(graph, lanes, out var route, out string reason), reason);
            return route;
        }

        [Test]
        public void ActualRegionalAuthoringProducesBoundedHighwayCityHighwayCourse()
        {
            string original = graph.ToJson();
            var originalLane = (Vector3[])lanes[0].centerline.Clone();
            var route = Route();
            Assert.AreEqual(TruckTaxiFlagshipRoute.DefaultLaneId, route.SourceLaneId);
            Assert.Greater(route.LengthMeters, 7000); Assert.Less(route.LengthMeters, 10000);
            Assert.That(route.Points.Length, Is.InRange(600, 8192));
            Assert.That(route.Gates.Length, Is.InRange(60, 110));
            Assert.AreEqual(originalLane[0], route.Gates[0]);
            for (int i = 0; i < route.Points.Length; i++)
            {
                Assert.IsTrue(LwsRoadGraphQuery.TryFindNearestRoad(graph, route.Points[i], 9, out var road));
                Assert.Less(Mathf.Abs(road.NearestPosition.y - route.Points[i].y), 1.01f);
                Assert.LessOrEqual(Vector3.Distance(route.Points[i], route.Points[(i + 1) % route.Points.Length]), 10.01f);
            }
            Assert.AreEqual(original, graph.ToJson(), "Route construction must not mutate road authority.");
            CollectionAssert.AreEqual(originalLane, lanes[0].centerline);
        }

        [Test]
        public void TwentyRivalsAndPlayerGetDistinctAuthoredSingleLaneGridSlots()
        {
            var route = Route(); var slots = new List<Vector3>();
            for (int i = 0; i <= TruckTaxiFlagshipRoute.MaximumRivals; i++)
            {
                int index = route.GridIndex(i);
                Assert.That(index, Is.InRange(1, route.Points.Length - 2));
                Vector3 point = route.Points[index];
                foreach (var earlier in slots) Assert.Greater(Vector3.Distance(earlier, point), 12);
                slots.Add(point);
                Assert.Greater(route.Facing(index).sqrMagnitude, .99f);
            }
            Assert.AreEqual(21, slots.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => route.GridIndex(21));
        }

        [Test]
        public void MissingRegionalLaneFailsWithoutOvalOrStraightLineFallback()
        {
            Assert.IsFalse(TruckTaxiFlagshipRoute.TryCreate(graph, Array.Empty<LwsTrafficLaneDefinition>(), out var route, out string reason));
            Assert.IsNull(route); StringAssert.Contains("unavailable", reason);
        }

        [Test]
        public void MissingGraphAndMissingHighwayFailClearly()
        {
            Assert.IsFalse(TruckTaxiFlagshipRoute.TryCreate(null, lanes, out var absent, out string reason));
            Assert.IsNull(absent); StringAssert.Contains("graph", reason);
            graph.edges.RemoveAll(edge => edge.roadClass == LwsRoadClass.Interstate);
            Assert.IsFalse(TruckTaxiFlagshipRoute.TryCreate(graph, lanes, out var route, out reason));
            Assert.IsNull(route); Assert.IsNotEmpty(reason);
        }

        [Test]
        public void OffRoadAndSteepAuthoredLaneAreRejectedRatherThanProjectedIntoPlace()
        {
            lanes[0].centerline[15] += Vector3.forward * 80;
            Assert.IsFalse(TruckTaxiFlagshipRoute.TryCreate(graph, lanes, out _, out string reason));
            StringAssert.Contains("road graph", reason);
            lanes[0].centerline[15] -= Vector3.forward * 80;
            lanes[0].centerline[15] += Vector3.up * 20;
            Assert.IsFalse(TruckTaxiFlagshipRoute.TryCreate(graph, lanes, out _, out reason));
            StringAssert.Contains("grade", reason);
        }

        [Test]
        public void DuplicateLaneAndNonfiniteGeometryAreRejected()
        {
            Assert.IsFalse(TruckTaxiFlagshipRoute.TryCreate(graph, new[] { lanes[0], lanes[0] }, out _, out string reason));
            StringAssert.Contains("Duplicate", reason);
            lanes[0].centerline[5] = new Vector3(float.NaN, 0, 0);
            Assert.IsFalse(TruckTaxiFlagshipRoute.TryCreate(graph, lanes, out _, out reason));
            StringAssert.Contains("Nonfinite", reason);
        }

        [Test]
        public void CourseDirectionsAndRecoveryRemainOnAuthoredGeometry()
        {
            var route = Route();
            for (int i = 0; i < route.Gates.Length; i++)
            {
                Assert.AreEqual(route.Points[route.GatePointIndices[i]], route.Gates[i]);
                Assert.Greater(route.GateForwards[i].sqrMagnitude, .99f);
                int recovery = route.RecoveryIndex(i);
                Assert.AreEqual((route.GatePointIndices[i] + route.Points.Length - 2) % route.Points.Length, recovery);
                Assert.Less(Vector3.Dot(route.Points[recovery] - route.Gates[i], route.GateForwards[i]), 0);
            }
        }

        [Test]
        public void EnduranceRecoveryAndTeleportDoNotEraseOrAdvanceRequiredCheckpoint()
        {
            var gates = new[] { Vector3.zero, Vector3.right * 20, new Vector3(20, 0, 20) };
            var directions = new[] { Vector3.right, Vector3.right, Vector3.forward };
            var progress = new TruckTaxiRaceProgress(gates, new TruckTaxiRaceSettings { maximumStepMeters = 10 }, directions, true);
            progress.Begin(Vector3.left * 2, 0);
            progress.Tick(Vector3.right * 2, 1);
            Assert.AreEqual(1, progress.NextCheckpoint); Assert.AreEqual(1, progress.PassedCheckpoints);
            progress.Tick(Vector3.one * 1000, 2);
            Assert.AreEqual(1, progress.NextCheckpoint); Assert.AreEqual(1, progress.PassedCheckpoints);
            progress.Invalidate(Vector3.right * 18, 3);
            Assert.AreEqual(1, progress.NextCheckpoint);
            progress.Tick(Vector3.right * 22, 4);
            Assert.AreEqual(2, progress.NextCheckpoint); Assert.AreEqual(2, progress.PassedCheckpoints);
            Assert.AreEqual(4, progress.CurrentLapSeconds(4), "Recovery cannot restart the endurance clock.");
        }

        [Test]
        public void FlagshipRequiresOneCompleteOrderedLoopAndPreservesLegacyModeValues()
        {
            Assert.AreEqual(6, (int)TruckTaxiRaceMode.FlagshipRace);
            Assert.AreEqual(5, (int)TruckTaxiRaceMode.EventRace);
            Assert.AreEqual(1, new TruckTaxiRaceSettings().Laps(TruckTaxiRaceMode.FlagshipRace, 5));
            var route = Route();
            var progress = new TruckTaxiRaceProgress(route.Gates, new TruckTaxiRaceSettings(), route.GateForwards, true);
            progress.Begin(route.Gates[0] - route.GateForwards[0] * 2, 0);
            for (int i = 0; i < route.Gates.Length; i++)
            {
                // Reanchor represents sampled travel between gates; it must not itself grant progress.
                progress.Reanchor(route.Gates[i] - route.GateForwards[i] * 2);
                Assert.IsFalse(progress.Tick(route.Gates[i] + route.GateForwards[i] * 2, i + 1));
            }
            Assert.AreEqual(0, progress.CompletedLaps); Assert.AreEqual(0, progress.NextCheckpoint);
            progress.Reanchor(route.Gates[0] - route.GateForwards[0] * 2);
            Assert.IsTrue(progress.Tick(route.Gates[0] + route.GateForwards[0] * 2, route.Gates.Length + 1));
            Assert.AreEqual(1, progress.CompletedLaps);
        }

        [Test]
        public void UninitializedRuntimeFailsCleanlyAndHasNoRivalsToLeak()
        {
            var race = fixture.AddComponent<TruckTaxiRaceRuntime>();
            Assert.IsFalse(race.BeginFlagshipRace()); Assert.IsFalse(race.IsActive);
            Assert.AreEqual(0, race.OpponentCount);
            Assert.IsFalse(race.RefreshFlagshipRoute()); Assert.IsNotEmpty(race.FlagshipUnavailableReason);
            Assert.IsFalse(race.DebugSetRivalCount(21));
            Assert.IsFalse(race.DebugTeleportToCheckpoint(0)); Assert.IsFalse(race.DebugSpawnRivals());
            Assert.IsFalse(race.DebugForceFinish()); Assert.IsFalse(race.DebugCrashRival(0));
            race.CancelRace("Repeated cleanup"); race.CancelRace("Repeated cleanup");
            Assert.AreEqual(0, race.OpponentCount);
        }
    }
}
