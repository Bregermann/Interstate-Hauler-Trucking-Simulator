#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NWH.VehiclePhysics2;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    // Editor PlayMode filter: EdenboroughNwhMovementPlayModeTests.GeneratedRegionSupportsGroundedNwhMovement
    // Requires BuildRegion("R_0_0") output. This is a short tractor/collision smoke test, not road certification,
    // a trailer test, or validation of LWS controls/hardware. Never writes the scene or prefab assets.
    public sealed class EdenboroughNwhMovementPlayModeTests
    {
        private const string RegionPath = "Assets/LWS/InterstateHauler/Edenborough/Generated/R_0_0/Edenborough_R_0_0.unity";
        private const string PrefabPath = "Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab";
        private Scene runnerScene;
        private Scene regionScene;
        private AsyncOperation load;
        private GameObject ownedRoot;
        private VehicleController truck;
        private HashSet<Collider> roadColliders;

        [UnityTest, Category("EdenboroughNwhIntegration")]
        public IEnumerator GeneratedRegionSupportsGroundedNwhMovement()
        {
            runnerScene = SceneManager.GetActiveScene();
            Assert.IsFalse(SceneManager.GetSceneByPath(RegionPath).isLoaded,
                "Close the generated region first; this test only unloads a scene it owns.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(RegionPath),
                "Run EdenboroughAuthoring.BuildRegion(\"R_0_0\") before this test.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath);
            Assert.Greater(Time.timeScale, 0f, "Physics must be running; the test does not change global time.");
            Assert.AreEqual(SimulationMode.FixedUpdate, Physics.simulationMode,
                "This test uses normal NWH FixedUpdate, never manual physics simulation.");

            load = EditorSceneManager.LoadSceneAsyncInPlayMode(RegionPath,
                new LoadSceneParameters(LoadSceneMode.Additive));
            Assert.IsNotNull(load);
            double deadline = Time.realtimeSinceStartupAsDouble + 30d;
            while (!load.isDone && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.IsTrue(load.isDone, "Timed out loading the generated region additively.");
            regionScene = SceneManager.GetSceneByPath(RegionPath);
            Assert.IsTrue(regionScene.isLoaded);
            Assert.IsTrue(runnerScene.isLoaded, "The test runner scene must remain loaded.");

            var metadata = regionScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EdenboroughSourceMetadata>(true)).Single();
            Assert.AreEqual("R_0_0", metadata.regionId);
            Assert.AreEqual("EasyRoads3D Pro", metadata.roadGeometryAuthority);
            Transform spawn = metadata.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "PrototypeTruckSpawn");
            roadColliders = new HashSet<Collider>(metadata.GetComponentsInChildren<MeshCollider>(true)
                .Where(c => c.enabled && !c.isTrigger && c.gameObject.activeInHierarchy && c.sharedMesh != null &&
                    c.GetComponentsInParent<Component>(true)
                        .Any(component => component != null &&
                            component.GetType().FullName == "EasyRoads3Dv3.ERModularRoad")));
            Assert.Greater(roadColliders.Count, 0, "No generated EasyRoads collision found; ground is not accepted.");
            Assert.IsTrue(roadColliders.Any(c => c.Raycast(new Ray(spawn.position + Vector3.up * 5f,
                Vector3.down), out _, 10f)), "The authored truck spawn is not above EasyRoads collision.");

            // Inactive staging prevents OnEnable from registering this truck as the global active vehicle.
            // Keep vendor-created temporary effects in the owned additive scene, not the runner scene.
            SceneManager.SetActiveScene(regionScene);
            ownedRoot = new GameObject("Edenborough NWH movement test (temporary)");
            ownedRoot.SetActive(false);
            GameObject instance = Object.Instantiate(prefab, spawn.position, spawn.rotation, ownedRoot.transform);
            truck = instance.GetComponent<VehicleController>();
            Assert.IsNotNull(truck, "The current Taxi tractor must retain the real NWH VehicleController.");
            truck.isPlayerControllable = false;
            truck.input.autoSetInput = false;
            Assert.IsNotNull(instance.GetComponent<Rigidbody>());
            Assert.IsFalse(instance.GetComponent<Rigidbody>().isKinematic);
            foreach (var changer in instance.GetComponentsInChildren<NWH.Common.Cameras.CameraChanger>(true))
            {
                Assert.AreNotEqual(instance, changer.gameObject, "Expected the vendor's separate Cameras child.");
                changer.gameObject.SetActive(false);
            }
            foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true)) camera.gameObject.SetActive(false);
            foreach (AudioListener listener in instance.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            var lodObject = new GameObject("Edenborough test LOD reference (no rendering)");
            lodObject.transform.SetParent(instance.transform, false);
            truck.lodCamera = lodObject.AddComponent<Camera>();
            truck.lodCamera.enabled = false;
            ownedRoot.SetActive(true);

            deadline = Time.realtimeSinceStartupAsDouble + 10d;
            while (!truck.IsInitialized && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.IsTrue(truck.IsInitialized, "NWH did not initialize.");
            Assert.GreaterOrEqual(truck.powertrain.wheels.Count, 4);
            truck.powertrain.engine.StartEngine();

            float stableSince = -1f;
            deadline = Time.realtimeSinceStartupAsDouble + 12d;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                SetInput(0f, 0f, 1f, 1f);
                yield return null;
                bool settled = AllWheelsOnRoad() && truck.Speed < .15f;
                if (!settled) stableSince = -1f;
                else if (stableSince < 0f) stableSince = Time.fixedTime;
                if (stableSince >= 0f && Time.fixedTime - stableSince >= .5f) break;
            }
            Assert.IsTrue(stableSince >= 0f && Time.fixedTime - stableSince >= .5f,
                "No stable, fully grounded spawn on EasyRoads. " + Diagnostic());
            Assert.IsTrue(truck.powertrain.engine.IsRunning, "Engine failed to start. " + Diagnostic());
            Vector3 start = truck.transform.position;
            Vector3 forward = Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            truck.powertrain.transmission.ShiftInto(1);

            int samples = 0, supportedSamples = 0;
            bool sawTorque = false, sawForwardGear = false;
            float previousFixedTime = Time.fixedTime, peakSpeed = 0f, peakSteering = 0f, progress = 0f;
            deadline = Time.realtimeSinceStartupAsDouble + 15d;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                SetInput(.35f, .06f, 0f, 0f);
                yield return null;
                if (Time.fixedTime == previousFixedTime) continue;
                previousFixedTime = Time.fixedTime;
                samples++;
                if (AllWheelsOnRoad()) supportedSamples++;
                peakSpeed = Mathf.Max(peakSpeed, truck.SpeedSigned);
                sawForwardGear |= truck.powertrain.transmission.Gear > 0;
                sawTorque |= truck.powertrain.wheels.Any(w => Mathf.Abs(w.wheelUAPI.MotorTorque) > 1f);
                peakSteering = Mathf.Max(peakSteering,
                    truck.powertrain.wheels.Max(w => Mathf.Abs(w.wheelUAPI.SteerAngle)));
                Vector3 displacement = truck.transform.position - start;
                progress = Vector3.Dot(displacement, forward);
                Assert.Less(Mathf.Abs(displacement.y), 1f, "Truck fell or launched. " + Diagnostic());
                Assert.Less(Mathf.Abs(Vector3.Dot(displacement, right)), 1.5f,
                    "Truck left the bounded lane corridor. " + Diagnostic());
                Assert.Greater(Vector3.Dot(truck.transform.up, Vector3.up), .9f, "Truck tipped. " + Diagnostic());
                if (progress >= 3f && peakSpeed >= .5f && peakSteering >= .2f) break;
                Assert.Less(progress, 8f, "Exceeded the short validation corridor.");
            }
            SetInput(0f, 0f, 1f, 1f);
            Assert.GreaterOrEqual(progress, 3f, "No three-metre NWH-powered forward movement. " + Diagnostic());
            Assert.GreaterOrEqual(peakSpeed, .5f, "No measurable forward speed. " + Diagnostic());
            Assert.GreaterOrEqual(peakSteering, .2f, "NWH wheel steering did not respond to steering input.");
            Assert.IsTrue(sawTorque && sawForwardGear, "No forward drivetrain torque/gear evidence. " + Diagnostic());
            Assert.Greater(samples, 0);
            Assert.GreaterOrEqual(supportedSamples / (float)samples, .85f,
                "Fewer than 85% of observed physics samples had all wheels on EasyRoads.");
            Assert.IsTrue(AllWheelsOnRoad(), "Final wheel support is not EasyRoads collision. " + Diagnostic());
            Debug.Log($"[Edenborough NWH] PASS: {progress:F2} m forward, peak {peakSpeed:F2} m/s, " +
                $"wheel steering {peakSteering:F2} deg, road support {supportedSamples}/{samples}. " +
                "TruckTaxi_InterstateTractor; scripted NWH input only. Junctions, trailers and hardware remain unvalidated.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (truck != null && truck.IsInitialized) SetInput(0f, 0f, 1f, 1f);
            if (ownedRoot != null)
            {
                ownedRoot.SetActive(false);
                Object.Destroy(ownedRoot);
            }
            if (runnerScene.IsValid() && runnerScene.isLoaded) SceneManager.SetActiveScene(runnerScene);
            // A failed load assertion must still allow its async operation to finish before scoped unload.
            if (load == null) yield break;
            double deadline = Time.realtimeSinceStartupAsDouble + 30d;
            while (!load.isDone && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.IsTrue(load.isDone, "Region load never completed; inspect the pending load before retrying.");
            if (!regionScene.IsValid()) regionScene = SceneManager.GetSceneByPath(RegionPath);
            if (regionScene.IsValid() && regionScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(regionScene);
                deadline = Time.realtimeSinceStartupAsDouble + 20d;
                while (unload != null && !unload.isDone && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                Assert.IsTrue(unload != null && unload.isDone, "Timed out unloading only the test-owned region.");
            }
            yield return null;
            Assert.IsTrue(runnerScene.isLoaded, "The original runner scene was lost.");
        }

        private void SetInput(float throttle, float steering, float brake, float handbrake)
        {
            // NWH's documented scripted-input path; no Transform, Rigidbody, wheel torque or velocity writes.
            truck.input.Throttle = throttle;
            truck.input.Steering = steering;
            truck.input.Brakes = brake;
            truck.input.Handbrake = handbrake;
            truck.input.Clutch = 0f;
        }

        private bool AllWheelsOnRoad() => truck.IsFullyGrounded() && truck.powertrain.wheels.Count >= 4 &&
            truck.powertrain.wheels.All(w => w.wheelUAPI != null && w.wheelUAPI.IsGrounded &&
                w.wheelUAPI.HitCollider != null && roadColliders.Contains(w.wheelUAPI.HitCollider));

        private string Diagnostic() => $"initialized={truck.IsInitialized}, gear={truck.powertrain.transmission.Gear}, " +
            $"engine={truck.powertrain.engine.IsRunning}, speed={truck.SpeedSigned:F2}, " +
            $"grounded={truck.IsGrounded()}, allRoadWheels={AllWheelsOnRoad()}, lod={truck.activeLODIndex}";
    }
}
#endif
