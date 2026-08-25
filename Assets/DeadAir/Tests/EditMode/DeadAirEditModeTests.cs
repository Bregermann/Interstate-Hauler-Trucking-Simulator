using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace DeadAir.Tests.EditMode
{
    public sealed class DeadAirEditModeTests
    {
        [Test]
        public void DefaultGpsStateIsEnabledAndRouteVisible()
        {
            DeadAirGpsState state = DeadAirGpsState.Default();
            Assert.IsTrue(state.enabled);
            Assert.IsTrue(state.routeVisible);
            Assert.AreEqual(DeadAirGpsPresentationMode.Normal, state.presentationMode);
        }

        [Test]
        public void BeatLayoutContainsRequiredEndTrigger()
        {
            bool found = false;
            foreach (DeadAirBeatDefinition beat in DeadAirBeatLayoutUtility.CreateDefaultBeatDefinitions())
            {
                if (beat.beatId == "ENDING_TRIGGER")
                {
                    found = true;
                    Assert.AreEqual(DeadAirTriggerCategory.Ending, beat.category);
                    Assert.IsTrue(beat.critical);
                }
            }

            Assert.IsTrue(found);
        }

        [Test]
        public void ChoiceCommitDoesNotOverwriteSiblingOutcome()
        {
            var go = new GameObject("Story Director");
            try
            {
                DeadAirStoryDirector director = go.AddComponent<DeadAirStoryDirector>();
                Assert.IsTrue(director.CommitChoice("CHOICE_01", DeadAirChoiceOutcome.TrustDispatch));
                Assert.IsFalse(director.CommitChoice("CHOICE_01", DeadAirChoiceOutcome.TrustGPS));
                Assert.IsTrue(director.TryGetChoice("CHOICE_01", out DeadAirChoiceOutcome outcome));
                Assert.AreEqual(DeadAirChoiceOutcome.TrustDispatch, outcome);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AutomaticDirectionPolicyMapsStoppedReverseToThrottleAndReverseSelector()
        {
            LwsAutomaticKeyboardDirectionDecision decision = LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                false,
                true,
                LwsTransmissionMode.Automatic,
                LwsAutomaticTransmissionSelector.Drive,
                0f,
                0.35f);

            Assert.IsTrue(decision.handled);
            Assert.IsTrue(decision.requestSelectorChange);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, decision.requestedSelector);
            Assert.AreEqual(1f, decision.throttle);
            Assert.AreEqual(0f, decision.brake);
        }

        [Test]
        public void ConstructionKitCreatesReusableTemplateRoots()
        {
            try
            {
                Transform kit = DeadAirBeatLayoutUtility.EnsureConstructionKit();
                Assert.IsNotNull(kit.Find("ROAD_PIECES"));
                Assert.IsNotNull(kit.Find("CHOICE_PIECES"));
                Assert.IsNotNull(kit.Find("STORY_TRIGGERS"));
                Assert.IsNotNull(kit.Find("SIGNS"));
                Assert.IsNotNull(kit.Find("TRAFFIC_EVENTS"));
                Assert.IsNotNull(kit.Find("GPS_EVENTS"));
                Assert.IsNotNull(kit.Find("AUDIO_EVENTS"));
                Assert.IsNotNull(kit.Find("DEBUG_REFERENCE/DA_DEPOT_START_TEMPLATE"));
                Assert.IsNotNull(kit.Find("DEBUG_REFERENCE/DA_DEPOT_START_TEMPLATE/TRUCK_START_REFERENCE"));
                Assert.IsNotNull(kit.Find("DEBUG_REFERENCE/DA_DEPOT_START_TEMPLATE/TRAILER_START_REFERENCE"));
                Assert.IsNotNull(kit.Find("DEBUG_REFERENCE/DA_DEPOT_START_TEMPLATE/DA_START_000_INITIAL_TRIGGER"));
                Assert.IsNotNull(kit.Find("ROAD_PIECES/DA_VALID_ROAD_ZONE"));
                Assert.IsNotNull(kit.Find("ROAD_PIECES/DA_VALID_ROAD_DEPOT"));
                Assert.IsNotNull(kit.Find("ROAD_PIECES/DA_VALID_ROAD_HIGHWAY"));
                Assert.IsNotNull(kit.Find("ROAD_PIECES/DA_VALID_ROAD_FORK"));
                Assert.IsNotNull(kit.Find("ROAD_PIECES/DA_ROAD_FORK"));
                Assert.IsNotNull(kit.Find("CHOICE_PIECES/DA_CHOICE_TEMPLATE/CHOICE_START"));
                Assert.IsNotNull(kit.Find("CHOICE_PIECES/DA_EXIT_CHOICE_TEMPLATE/EXIT_COMMIT"));
                Assert.IsNotNull(kit.GetComponentInChildren<DeadAirRoadPiece>());
                Assert.IsNotNull(kit.GetComponentInChildren<DeadAirSign>());
            }
            finally
            {
                GameObject kit = GameObject.Find(DeadAirBeatLayoutUtility.ConstructionKitRootName);
                if (kit != null)
                {
                    Object.DestroyImmediate(kit);
                }
            }
        }

        [Test]
        public void ChoiceGroupsContainStartAndCommitLanes()
        {
            try
            {
                Transform root = DeadAirBeatLayoutUtility.EnsureUnplacedBeatLayout();
                Transform choice = root.Find("CHOICE_01");
                Assert.IsNotNull(choice);
                Assert.IsNotNull(choice.Find("CHOICE_01_START"));
                Assert.IsNotNull(choice.Find("CHOICE_01_LEFT_COMMIT"));
                Assert.IsNotNull(choice.Find("CHOICE_01_RIGHT_COMMIT"));
                Assert.IsNotNull(choice.Find("CHOICE_01_STRAIGHT_COMMIT"));
            }
            finally
            {
                GameObject unplaced = GameObject.Find(DeadAirBeatLayoutUtility.UnplacedRootName);
                if (unplaced != null)
                {
                    Object.DestroyImmediate(unplaced);
                }
            }
        }

        [Test]
        public void EndingResolverUsesCommittedChoiceBeforeFallback()
        {
            var storyObject = new GameObject("Story");
            var endingObject = new GameObject("Ending");
            try
            {
                DeadAirStoryDirector story = storyObject.AddComponent<DeadAirStoryDirector>();
                DeadAirEndingDirector ending = endingObject.AddComponent<DeadAirEndingDirector>();
                Assert.IsTrue(story.CommitChoice("CHOICE_03_EXIT17", DeadAirChoiceOutcome.Exit17));
                Assert.AreEqual(DeadAirEndingId.Exit17, ending.ResolveEndingFromChoices(story));
            }
            finally
            {
                Object.DestroyImmediate(storyObject);
                Object.DestroyImmediate(endingObject);
            }
        }

        [Test]
        public void SharedBasicAutomaticInputModeRoutesThroughTransmissionController()
        {
            var go = new GameObject("Truck");
            try
            {
                Lws18SpeedTransmissionController controller = go.AddComponent<Lws18SpeedTransmissionController>();
                LwsKeyboardGamepadTruckInputSource input = go.AddComponent<LwsKeyboardGamepadTruckInputSource>();
                input.ConfigureTransmissionController(controller);

                Assert.IsTrue(input.TrySetInputMode(LwsTruckInputMode.BasicAutomatic, out string message), message);
                Assert.AreEqual(LwsTruckInputMode.BasicAutomatic, input.InputMode);
                Assert.AreEqual(LwsTransmissionMode.Automatic, controller.DisplayState.mode);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DeadAirCameraFilterRemovesCameraCycleOnly()
        {
            LwsVehicleCommandFrame commands = new LwsVehicleCommandFrame
            {
                cameraCycle = LwsMomentaryIntent.Pressed,
                horn = LwsMomentaryIntent.Held,
                flipOffDriver = LwsMomentaryIntent.Pressed
            };

            LwsVehicleCommandFrame filtered = DeadAirCockpitCameraLock.FilterDeadAirCommands(commands);

            Assert.AreEqual(LwsMomentaryIntent.None, filtered.cameraCycle);
            Assert.AreEqual(LwsMomentaryIntent.Held, filtered.horn);
            Assert.AreEqual(LwsMomentaryIntent.Pressed, filtered.flipOffDriver);
        }

        [Test]
        public void StartMarkerProvidesTruckAndTrailerPoses()
        {
            var markerObject = new GameObject("DeadAirStartMarker");
            var truckReference = new GameObject("TRUCK_START_REFERENCE");
            var trailerReference = new GameObject("TRAILER_START_REFERENCE");
            try
            {
                DeadAirStartMarker marker = markerObject.AddComponent<DeadAirStartMarker>();
                markerObject.transform.position = new Vector3(10f, 0f, 20f);
                markerObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                truckReference.transform.SetParent(markerObject.transform, false);
                trailerReference.transform.SetParent(markerObject.transform, false);
                trailerReference.transform.localPosition = new Vector3(0f, 0f, -13.5f);
                marker.ConfigureRigReferences(truckReference.transform, trailerReference.transform);

                marker.GetTruckPose(out Vector3 truckPosition, out Quaternion truckRotation);
                marker.GetTrailerPose(out Vector3 trailerPosition, out Quaternion trailerRotation);

                Assert.AreEqual(markerObject.transform.position, truckPosition);
                Assert.AreEqual(markerObject.transform.rotation.eulerAngles.y, truckRotation.eulerAngles.y, 0.01f);
                Assert.AreEqual(13.5f, Vector3.Distance(truckPosition, trailerPosition), 0.05f);
                Assert.AreEqual(truckRotation.eulerAngles.y, trailerRotation.eulerAngles.y, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(markerObject);
            }
        }

        [Test]
        public void PreferredRuntimeStartMarkerIgnoresConstructionKitTemplate()
        {
            var kit = new GameObject(DeadAirBeatLayoutUtility.ConstructionKitRootName);
            var kitMarkerObject = new GameObject("DA_DEPOT_START_TEMPLATE");
            var blockout = new GameObject(DeadAirBeatLayoutUtility.PlayableBlockoutRootName);
            var blockoutMarkerObject = new GameObject("DA_DEPOT_START_TEMPLATE");
            try
            {
                kitMarkerObject.transform.SetParent(kit.transform, false);
                DeadAirStartMarker kitMarker = kitMarkerObject.AddComponent<DeadAirStartMarker>();
                blockoutMarkerObject.transform.SetParent(blockout.transform, false);
                DeadAirStartMarker blockoutMarker = blockoutMarkerObject.AddComponent<DeadAirStartMarker>();

                DeadAirStartMarker preferred = DeadAirBeatLayoutUtility.FindPreferredRuntimeStartMarker();

                Assert.AreSame(blockoutMarker, preferred);
                Assert.AreNotSame(kitMarker, preferred);
            }
            finally
            {
                Object.DestroyImmediate(kit);
                Object.DestroyImmediate(blockout);
            }
        }

        [Test]
        public void StartRigInitializesTruckTrailerAndPendingConnection()
        {
            var root = new GameObject("Dead Air Rig Test");
            var markerObject = new GameObject("DeadAirStartMarker");
            var truckPrefab = new GameObject("Truck Prefab");
            var trailerPrefab = new GameObject("Trailer Prefab");
            try
            {
                markerObject.transform.SetParent(root.transform, false);
                DeadAirStartMarker marker = markerObject.AddComponent<DeadAirStartMarker>();
                DeadAirStartRigController rig = root.AddComponent<DeadAirStartRigController>();
                truckPrefab.AddComponent<DeadAirVehicleAdapter>();

                rig.Configure(truckPrefab, trailerPrefab, marker);
                DeadAirVehicleAdapter adapter = rig.InitializeRig();

                Assert.IsNotNull(adapter);
                Assert.IsNotNull(rig.DeliveryTrailer);
                Assert.IsTrue(rig.TrailerConsideredConnected);
                Assert.IsTrue(adapter.TrailerConsideredConnectedForDeadAir);
                Assert.IsTrue(adapter.CameraCycleSuppressed);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(truckPrefab);
                Object.DestroyImmediate(trailerPrefab);
                foreach (DeadAirVehicleAdapter adapter in Object.FindObjectsByType<DeadAirVehicleAdapter>(FindObjectsSortMode.None))
                {
                    Object.DestroyImmediate(adapter.gameObject);
                }

                GameObject trailer = GameObject.Find("Dead Air Delivery Trailer");
                if (trailer != null)
                {
                    Object.DestroyImmediate(trailer);
                }
            }
        }

        [Test]
        public void RoadBoundaryAllowsPartialRigDeparture()
        {
            var zoneObject = new GameObject("DA_VALID_ROAD_ZONE");
            try
            {
                DeadAirValidRoadZone zone = zoneObject.AddComponent<DeadAirValidRoadZone>();
                zone.Configure("TEST_ZONE", new Vector3(20f, 8f, 20f));
                DeadAirRoadBoundaryEvaluation evaluation = DeadAirOffRoadFailureController.EvaluateRigRoadState(
                    new Vector3(99f, 0f, 0f),
                    Vector3.zero,
                    true,
                    new Vector3(99f, 0f, 99f),
                    new Vector3(99f, 0f, 100f),
                    new[] { zone });

                Assert.IsTrue(evaluation.tractorValid);
                Assert.IsFalse(evaluation.trailerValid);
                Assert.IsFalse(evaluation.entireRigOffRoad);
            }
            finally
            {
                Object.DestroyImmediate(zoneObject);
            }
        }

        [Test]
        public void RoadBoundaryAllowsTrailerKeepingRigValid()
        {
            var zoneObject = new GameObject("DA_VALID_ROAD_ZONE");
            try
            {
                DeadAirValidRoadZone zone = zoneObject.AddComponent<DeadAirValidRoadZone>();
                zone.Configure("TEST_ZONE", new Vector3(20f, 8f, 20f));
                DeadAirRoadBoundaryEvaluation evaluation = DeadAirOffRoadFailureController.EvaluateRigRoadState(
                    new Vector3(99f, 0f, 0f),
                    new Vector3(99f, 0f, 1f),
                    true,
                    Vector3.zero,
                    new Vector3(99f, 0f, 100f),
                    new[] { zone });

                Assert.IsFalse(evaluation.tractorValid);
                Assert.IsTrue(evaluation.trailerValid);
                Assert.IsFalse(evaluation.entireRigOffRoad);
            }
            finally
            {
                Object.DestroyImmediate(zoneObject);
            }
        }

        [Test]
        public void RoadBoundaryMarksEntireRigOffRoadOnlyWhenAllPointsLeave()
        {
            var zoneObject = new GameObject("DA_VALID_ROAD_ZONE");
            try
            {
                DeadAirValidRoadZone zone = zoneObject.AddComponent<DeadAirValidRoadZone>();
                zone.Configure("TEST_ZONE", new Vector3(20f, 8f, 20f));
                DeadAirRoadBoundaryEvaluation evaluation = DeadAirOffRoadFailureController.EvaluateRigRoadState(
                    new Vector3(50f, 0f, 0f),
                    new Vector3(52f, 0f, 0f),
                    true,
                    new Vector3(54f, 0f, 0f),
                    new Vector3(56f, 0f, 0f),
                    new[] { zone });

                Assert.IsFalse(evaluation.tractorValid);
                Assert.IsFalse(evaluation.trailerValid);
                Assert.IsTrue(evaluation.entireRigOffRoad);
            }
            finally
            {
                Object.DestroyImmediate(zoneObject);
            }
        }

        [Test]
        public void OffRoadGraceCancelsWhenRigReturns()
        {
            float timer = DeadAirOffRoadFailureController.UpdateGraceTimer(true, 0f, 0.75f, 1.25f, out bool triggered);
            Assert.IsFalse(triggered);
            Assert.Greater(timer, 0f);

            timer = DeadAirOffRoadFailureController.UpdateGraceTimer(false, timer, 0.1f, 1.25f, out triggered);
            Assert.IsFalse(triggered);
            Assert.AreEqual(0f, timer);
        }

        [Test]
        public void OffRoadGraceTriggersVoidFailureAfterDuration()
        {
            float timer = DeadAirOffRoadFailureController.UpdateGraceTimer(true, 0.9f, 0.4f, 1.25f, out bool triggered);
            Assert.IsTrue(triggered);
            Assert.GreaterOrEqual(timer, 1.25f);
        }
    }
}
