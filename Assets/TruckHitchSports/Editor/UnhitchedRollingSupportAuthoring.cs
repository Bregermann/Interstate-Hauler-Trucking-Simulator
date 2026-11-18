#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NWH.WheelController3D;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;

namespace LWS.TruckHitchSports.Editor
{
    // Explicit sport-owned conversion of the existing native semi-trailer into
    // a rolling surf platform. NWH remains the wheel/suspension authority.
    public static class UnhitchedRollingSupportAuthoring
    {
        const string Prefix = "Surf front rolling support";
        const string GroupName = "Unpowered surf front dolly";
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Unhitched native rolling support")]
        public static void UpgradeActiveUnhitchedRollingSupport()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before converting the surf platforms.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/Unhitched.unity") throw new InvalidOperationException("This upgrade only applies to the saved Unhitched sport.");
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnhitchedMatch>(true)).Single();
            var actorPoses = match.trucks.Select(t => t.transform.localToWorldMatrix).ToArray();
            foreach (var trailer in match.trailers)
            {
                var vehicle = trailer.Vehicle;
                var stand = trailer.Module != null ? trailer.Module.module.trailerStand : null;
                if (vehicle == null || stand == null) throw new InvalidOperationException("Use the matched native trailer/landing support before upgrading a surf rig.");
                float originalMass = trailer.Body.mass; Vector3 originalCOM = trailer.Body.centerOfMass;
                Matrix4x4 originalPose = trailer.transform.localToWorldMatrix;
                var originalWheels = trailer.GetComponentsInChildren<WheelController>(true).Where(w => !w.name.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
                if (originalWheels.Length < 2) throw new InvalidOperationException("Preserve the original native rolling axle templates.");
                Func<WheelController, Vector3> local = wheel => trailer.transform.InverseTransformPoint(wheel.transform.position);
                float minimum = originalWheels.Min(w => local(w).z), maximum = originalWheels.Max(w => local(w).z);
                // A native trailer that already brackets its COM needs no
                // extra support. The stock carrier has rear-only axle stations.
                if (originalCOM.z >= minimum && originalCOM.z <= maximum)
                { stand.SetActive(false); EditorUtility.SetDirty(stand); continue; }
                float frontZ = trailer.transform.InverseTransformPoint(stand.transform.position).z;
                if (frontZ <= originalCOM.z) throw new InvalidOperationException("The existing landing-support station must lie ahead of the platform COM.");
                int lane = Array.IndexOf(match.trailers, trailer);
                float loadedMass = originalMass, weightedZ = originalMass * originalCOM.z;
                foreach (var part in match.trucks[lane].GetComponentsInChildren<Rigidbody>(true))
                {
                    // Authoring uses Transform poses, not potentially stale
                    // Rigidbody world poses from the original native prefab.
                    float z = trailer.transform.InverseTransformPoint(part.transform.TransformPoint(part.centerOfMass)).z;
                    loadedMass += part.mass; weightedZ += part.mass * z;
                }
                float rearZ = originalWheels.Average(w => local(w).z);
                float frontShare = Mathf.Clamp01((weightedZ / loadedMass - rearZ) / (frontZ - rearZ));
                float supportedMassPerWheel = loadedMass * frontShare / 2;
                var left = originalWheels.Where(w => local(w).x < 0).OrderByDescending(w => local(w).z).FirstOrDefault();
                var right = originalWheels.Where(w => local(w).x > 0).OrderByDescending(w => local(w).z).FirstOrDefault();
                if (left == null || right == null) throw new InvalidOperationException("A matched left/right native wheel pair is required.");
                int groupIndex = vehicle.powertrain.wheelGroups.FindIndex(g => g.name == GroupName);
                if (groupIndex < 0)
                {
                    groupIndex = vehicle.powertrain.wheelGroups.Count;
                    vehicle.powertrain.wheelGroups.Add(new WheelGroup { name = GroupName, vc = vehicle, steerCoefficient = 0, brakeCoefficient = 0, handbrakeCoefficient = 0, addAckerman = false, isSolid = false });
                }
                for (int side = 0; side < 2; side++)
                {
                    var source = side == 0 ? left : right;
                    string name = Prefix + (side == 0 ? " L" : " R");
                    var wheel = trailer.GetComponentsInChildren<WheelController>(true).FirstOrDefault(w => w.name == name);
                    if (wheel == null)
                    {
                        // Each native controller owns its complete Rotating and
                        // NonRotating visual containers; Instantiate remaps those
                        // internal references to the new production wheel mesh.
                        var clone = UnityEngine.Object.Instantiate(source.gameObject, trailer.transform);
                        clone.name = name; wheel = clone.GetComponent<WheelController>();
                        Undo.RegisterCreatedObjectUndo(clone, "Add native surf rolling support");
                    }
                    Vector3 station = local(source); station.z = frontZ;
                    wheel.transform.localPosition = station; wheel.transform.localRotation = source.transform.localRotation;
                    wheel.AutoSimulate = true; wheel.enabled = true;
                    // Size only the new native support suspension for its
                    // actual loaded axle share (including the rider/cab).
                    // The original carrier's rear-only spring rating can be
                    // insufficient when reused at the heavily loaded nose.
                    wheel.spring.maxForce = Mathf.Max(source.spring.maxForce, supportedMassPerWheel * Physics.gravity.magnitude * 3);
                    float stiffness = wheel.spring.maxForce / Mathf.Max(.01f, wheel.spring.maxLength);
                    float damping = 2 * Mathf.Sqrt(stiffness * supportedMassPerWheel) * .65f;
                    wheel.damper.bumpRate = Mathf.Max(source.damper.bumpRate, damping);
                    wheel.damper.reboundRate = Mathf.Max(source.damper.reboundRate, damping);
                    var component = vehicle.powertrain.wheels.FirstOrDefault(w => w.wheelUAPI == wheel);
                    if (component == null)
                    {
                        component = new WheelComponent { name = name, wheelUAPI = wheel };
                        vehicle.powertrain.wheels.Add(component);
                    }
                    component.SetWheelGroup(groupIndex);
                    EditorUtility.SetDirty(wheel); EditorUtility.SetDirty(wheel.transform);
                }
                // The support pair is deliberately unpowered: its stock NWH
                // WheelControllers simulate rolling contact without an engine
                // connection. No new solver, imposed velocity, mass or COM shift.
                stand.SetActive(false);
                if (trailer.Body.mass != originalMass || trailer.Body.centerOfMass != originalCOM || trailer.transform.localToWorldMatrix != originalPose) throw new InvalidOperationException("Do not move or rebalance the physical platform during the native support upgrade.");
                EditorUtility.SetDirty(vehicle); EditorUtility.SetDirty(stand);
            }
            for (int i = 0; i < actorPoses.Length; i++) if (match.trucks[i].transform.localToWorldMatrix != actorPoses[i]) throw new InvalidOperationException("A rider start changed during platform conversion.");
            Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("HH10_UNHITCHED_ROLLING_SUPPORT_AUTHORED native front support wheels/visuals, original rear axles, body mass/COM and all rider starts preserved; runtime acceptance pending.");
        }
    }
}
#endif
