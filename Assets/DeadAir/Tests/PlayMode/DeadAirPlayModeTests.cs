using System.Collections;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadAir.Tests.PlayMode
{
    public sealed class DeadAirPlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneBootstrapperCreatesRequiredRuntimeSystems()
        {
            var root = new GameObject("Dead Air Test Bootstrapper");
            try
            {
                root.AddComponent<DeadAirSceneBootstrapper>();
                yield return null;

                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirGameManager>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirStoryDirector>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirGPSDirector>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirGPSController>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirDashboardMisinformationDirector>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirTrafficHorrorDirector>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirStartRigController>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirCockpitCameraLock>());
                Assert.IsNull(Object.FindFirstObjectByType<DeadAirOffRoadFailureController>());
                Assert.IsNotNull(GameObject.Find("DEAD_AIR_UNPLACED_GAMEPLAY"));
                Assert.IsNotNull(GameObject.Find("DEAD_AIR_CONSTRUCTION_KIT"));
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirStartMarker>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirRouteFlowGizmo>());
            }
            finally
            {
                string[] createdRootNames =
                {
                    "Dead Air Test Bootstrapper",
                    "DEAD_AIR",
                    "START",
                    "SYSTEMS",
                    "STORY_TRIGGERS",
                    "CHOICE_TRIGGERS",
                    "ENVIRONMENT",
                    "UI",
                    "DEBUG",
                    "DEAD_AIR_UNPLACED_GAMEPLAY",
                    "DEAD_AIR_CONSTRUCTION_KIT"
                };

                foreach (string rootName in createdRootNames)
                {
                    GameObject go = GameObject.Find(rootName);
                    if (go != null)
                    {
                        Object.Destroy(go);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator DashboardEventStateCanBeAppliedAndCleared()
        {
            var go = new GameObject("Dashboard");
            try
            {
                DeadAirDashboardMisinformationDirector dashboard = go.AddComponent<DeadAirDashboardMisinformationDirector>();
                dashboard.Apply(new DeadAirDashboardMisinformationDirector.DashboardState
                {
                    eventKind = DeadAirDashboardEventKind.WrongSpeed,
                    active = true,
                    overrideSpeedText = "17 MPH",
                    durationSeconds = 0.01f
                });

                Assert.IsTrue(dashboard.CurrentState.active);
                yield return new WaitForSeconds(0.03f);
                Assert.IsFalse(dashboard.CurrentState.active);
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        [UnityTest]
        public IEnumerator CockpitLockPublishesCockpitModeAndSuppressesCycle()
        {
            var bootstrap = new GameObject("LWS Bootstrap");
            var truck = new GameObject("Dead Air Truck");
            var lockObject = new GameObject("Dead Air Cockpit Lock");
            try
            {
                LwsApplicationBootstrap.ResetForTests();
                bootstrap.AddComponent<LwsApplicationBootstrap>();
                DeadAirVehicleAdapter adapter = truck.AddComponent<DeadAirVehicleAdapter>();
                DeadAirCockpitCameraLock cockpitLock = lockObject.AddComponent<DeadAirCockpitCameraLock>();
                yield return null;

                Assert.IsTrue(cockpitLock.ApplyCockpitLock(adapter));
                Assert.IsTrue(adapter.CameraCycleSuppressed);
                Assert.IsTrue(LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsCameraPresentationService cameraService));
                Assert.AreEqual(LwsVehicleCameraMode.Cockpit, cameraService.CurrentMode);

                LwsVehicleCommandFrame filtered = DeadAirCockpitCameraLock.FilterDeadAirCommands(new LwsVehicleCommandFrame
                {
                    cameraCycle = LwsMomentaryIntent.Pressed
                });
                Assert.AreEqual(LwsMomentaryIntent.None, filtered.cameraCycle);
            }
            finally
            {
                Object.Destroy(lockObject);
                Object.Destroy(truck);
                Object.Destroy(bootstrap);
                LwsApplicationBootstrap.ResetForTests();
            }
        }

        [UnityTest]
        public IEnumerator StartRigRestartRestoresTrailerConnectionState()
        {
            var root = new GameObject("Dead Air Rig Runtime Test");
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
                yield return null;

                Assert.IsNotNull(adapter);
                Assert.IsTrue(rig.TrailerConsideredConnected);
                Assert.IsTrue(adapter.TrailerConsideredConnectedForDeadAir);

                rig.RestartRig();
                yield return null;

                Assert.IsTrue(rig.TrailerConsideredConnected);
                Assert.IsTrue(rig.PlayerTruck.CameraCycleSuppressed);
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(truckPrefab);
                Object.Destroy(trailerPrefab);
            }
        }

        [UnityTest]
        public IEnumerator VoidFailureRequestIsIgnoredWhileFailureMechanicIsDisabled()
        {
            var managerObject = new GameObject("Dead Air Manager");
            var truckObject = new GameObject("Dead Air Truck");
            try
            {
                DeadAirGameManager manager = managerObject.AddComponent<DeadAirGameManager>();
                managerObject.AddComponent<DeadAirEndingDirector>();
                DeadAirOffRoadFailureController offRoadFailure = managerObject.AddComponent<DeadAirOffRoadFailureController>();
                offRoadFailure.SetOffRoadVoidFailureEnabled(true);
                DeadAirVehicleAdapter adapter = truckObject.AddComponent<DeadAirVehicleAdapter>();
                truckObject.AddComponent<Rigidbody>();
                yield return null;

                manager.RequestVoidFailure();
                yield return null;

                Assert.AreEqual(DeadAirGameState.Playing, manager.State);
                Assert.IsFalse(adapter.DeadAirDrivingInputLocked);
                Assert.AreEqual(DeadAirEndingId.None, manager.EndingDirector.CurrentEnding);

                manager.RequestEnding(DeadAirEndingId.Lost);
                yield return null;

                Assert.AreEqual(DeadAirGameState.Playing, manager.State);
                Assert.IsFalse(adapter.DeadAirDrivingInputLocked);
                Assert.AreEqual(DeadAirEndingId.None, manager.EndingDirector.CurrentEnding);
            }
            finally
            {
                Object.Destroy(managerObject);
                Object.Destroy(truckObject);
            }
        }
    }
}
