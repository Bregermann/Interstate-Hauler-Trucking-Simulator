#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    // An explicit repair of the new sport's saved apparatus, never a build or
    // normal-play migration and never a replacement for loading/launch gameplay.
    public static class CoreHitchSetupRepairs
    {
        [MenuItem("LWS/Truck Sports/Alpha 0.10/Repair Active Highwire Receiving Surfaces")]
        public static void RepairActiveHighwireReceivingSurfaces()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Repair the saved receiving platforms outside Play Mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity") throw new InvalidOperationException("This repair only applies to the saved JackknifeHighwire scene.");
            Physics.SyncTransforms();
            var colliders = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BoxCollider>(true)).ToArray();
            var roads = colliders.Where(c => c.name == "Suspended narrow physical roadway").ToArray();
            var platforms = colliders.Where(c => c.name == "Finish receiving platform").ToArray();
            if (roads.Length != 20 || platforms.Length != 4) throw new InvalidOperationException("Preserve all twenty road sections and four original receiving platforms.");
            foreach (var platform in platforms)
            {
                var joiningRoad = roads.OrderBy(r => Vector3.ProjectOnPlane(r.bounds.center - platform.bounds.center, Vector3.up).sqrMagnitude).First();
                float delta = joiningRoad.bounds.max.y - platform.bounds.max.y;
                if (Mathf.Abs(delta) > .6f) throw new InvalidOperationException("Unexpected receiving-platform height; inspect before changing saved geometry.");
                platform.transform.position += Vector3.up * delta;
                EditorUtility.SetDirty(platform.transform);
            }
            Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("HH10_HIGHWIRE_RECEIVING_SURFACES_REPAIRED original platform footprints and gates preserved; road/platform tops are physically flush.");
        }
        public static void RepairActiveHitchRunLandingGear()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before setting the receiving jacks.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/TruckHitchSports/Scenes/HitchAndRun.unity")throw new InvalidOperationException("Open the saved Hitch and Run venue.");
            var match=UnityEngine.Object.FindAnyObjectByType<HitchRunMatch>();
            foreach(var load in match.trailers)
            {
                var stand=load.Module.module.trailerStand.transform;
                Undo.RecordObject(stand,"Set native loading-jack extension");
                var position=stand.localPosition;position.y=.499f;stand.localPosition=position;EditorUtility.SetDirty(stand);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("LWS/Truck Sports/Alpha 0.10/Repair Active Trebuchet Apparatus")]
        public static void RepairActiveTrebuchetApparatus()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Repair the saved trebuchet scene outside Play Mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity") throw new InvalidOperationException("This targeted repair only applies to the saved TrailerTrebuchet scene.");
            var match = UnityEngine.Object.FindAnyObjectByType<TrebuchetMatch>();
            if (match == null || match.gameObject.scene != scene) throw new InvalidOperationException("The active scene must own the trebuchet match.");
            // Root hierarchy order is serialized. The original creator added
            // one ramp/tower per engine in this same order; never choose a
            // nearest item again after moving it (that reuses the same item).
            var sceneRoots = scene.GetRootGameObjects().OrderBy(g => g.transform.GetSiblingIndex()).ToArray();
            var ramps = sceneRoots.Where(g => g.name == "Physical siege loading ramp").Select(g => g.transform).ToArray();
            var towers = sceneRoots.Where(g => g.name == "Trebuchet pivot tower").Select(g => g.transform).ToArray();
            if (match.engines.Length != match.trucks.Length || match.targets.Length != match.engines.Length || ramps.Length != match.engines.Length || towers.Length != match.engines.Length) throw new InvalidOperationException("Each existing tractor lane must retain exactly one engine, ramp, tower and target zone.");
            var targetBlocks = match.GetComponentsInChildren<SiegeTarget>(true);
            for (int lane = 0; lane < match.engines.Length; lane++) if (!targetBlocks.Any(t => t.Match == match && t.Lane == lane)) throw new InvalidOperationException("Preserve each existing lane's physical siege targets before repairing its equipment.");
            for (int lane = 0; lane < match.engines.Length; lane++)
            {
                var engine = match.engines[lane];
                var arm = engine.Arm; var root = arm.transform;
                var cradle = engine.LoadingCradle.transform.parent;
                // Native Rigidbody.position can remain at the prefab pose in
                // stopped authoring. The actual saved tractor Transform owns
                // the lane; actors themselves are never moved by this repair.
                float laneX = match.trucks[lane].transform.position.x;
                float targetDeltaX = laneX - match.targets[lane].position.x;
                match.targets[lane].position += Vector3.right * targetDeltaX;
                EditorUtility.SetDirty(match.targets[lane]);
                foreach (var target in targetBlocks) if (target.Match == match && target.Lane == lane)
                {
                    target.transform.position += Vector3.right * targetDeltaX;
                    var targetBody = target.GetComponent<Rigidbody>();
                    if (targetBody != null) { targetBody.position = target.transform.position; EditorUtility.SetDirty(targetBody); }
                    EditorUtility.SetDirty(target.transform);
                }
                var original = root.GetComponent<MeshRenderer>();
                var visual = root.Find("Metre-scaled physical siege beam visual");
                if (visual == null)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = "Metre-scaled physical siege beam visual"; visual = cube.transform; visual.SetParent(root, false);
                    cube.GetComponent<Collider>().enabled = false;
                }
                visual.localPosition = Vector3.zero; visual.localRotation = Quaternion.identity; visual.localScale = new Vector3(1.5f, 1.3f, 26);
                visual.GetComponent<Renderer>().sharedMaterials = original.sharedMaterials; original.enabled = false;
                root.localScale = Vector3.one;
                Vector3 position = new Vector3(laneX - 8, 12, root.position.z);
                Quaternion rotation = Quaternion.Euler(50, 0, 0);
                root.SetPositionAndRotation(position, rotation); arm.position = position; arm.rotation = rotation;
                var beam = root.GetComponent<BoxCollider>(); beam.center = Vector3.zero; beam.size = new Vector3(1.5f, 1.3f, 26);
                var counterweight = root.Find("Physical trebuchet counterweight");
                if (counterweight == null) throw new InvalidOperationException("Preserve the original physical counterweight.");
                counterweight.localPosition = new Vector3(0, 0, -9); counterweight.localRotation = Quaternion.identity; counterweight.localScale = Vector3.one * 4;
                cradle.localPosition = new Vector3(8, 0, 12); cradle.localRotation = Quaternion.Inverse(root.rotation); cradle.localScale = new Vector3(7, .8f, 16);
                var volume = engine.LoadingCradle.transform;
                volume.localPosition = Vector3.zero; volume.localRotation = Quaternion.identity; volume.localScale = new Vector3(1 / 7f, 1 / .8f, 1 / 16f);
                engine.LoadingCradle.center = new Vector3(0, 3, 0); engine.LoadingCradle.size = new Vector3(8, 8, 18); engine.LoadingCradle.isTrigger = true;
                // A real crossbar carries the offset cradle from the side arm.
                // Its top meets the underside of the level loading board, so
                // neither the arm nor this support occupies the truck's roof.
                var crossbar = PhysicalBox(root, "Physical offset cradle crossbar", root.TransformPoint(new Vector3(4, 0, 12)) - Vector3.up * .8f, new Vector3(12, .8f, 2), original.sharedMaterial, beam.sharedMaterial);
                crossbar.localRotation = Quaternion.Inverse(root.rotation);
                arm.isKinematic = true; arm.angularDamping = .05f; arm.maxAngularVelocity = 2.5f; arm.maxDepenetrationVelocity = 5;
                arm.mass = 45000; arm.centerOfMass = new Vector3(0, 0, -7.5f); arm.ResetInertiaTensor(); arm.solverIterations = 32; arm.solverVelocityIterations = 12;
                var pivot = engine.Pivot; pivot.autoConfigureConnectedAnchor = false; pivot.anchor = Vector3.zero; pivot.connectedAnchor = position; pivot.axis = Vector3.right; pivot.useMotor = false; pivot.enableCollision = false;
                var spring = pivot.spring; spring.spring = spring.damper = spring.targetPosition = 0; pivot.spring = spring; pivot.useSpring = false; pivot.enablePreprocessing = false;
                // Runtime holds the measured authored loading angle. A zero-based
                // limit would rotate this apparatus away from its loading ramp.
                pivot.useLimits = false;
                var ramp = ramps[lane];
                // Connect ground to the level, physically load-bearing cradle.
                float rear = -18, front = cradle.position.z - 7.5f;
                float rearCenter = -.25f;
                float frontCenter = cradle.position.y + .4f - .3f;
                float rise = frontCenter - rearCenter;
                float span = front - rear;
                ramp.position = new Vector3(laneX, rearCenter + rise / 2, (rear + front) / 2);
                ramp.rotation = Quaternion.Euler(-Mathf.Atan2(rise, span) * Mathf.Rad2Deg, 0, 0);
                ramp.localScale = new Vector3(16, .6f, Mathf.Sqrt(span * span + rise * rise));
                // A full-size tractor must clear the cradle before firing.
                // Connect its far edge to the apron with a real descending
                // roadway rather than dropping the cab nose-first off a 3m lip.
                float unloadRear=cradle.position.z+7.5f,unloadFront=unloadRear+35;
                float unloadRise=frontCenter+.25f;
                var unloading=PhysicalBox(match.transform,"Physical siege unloading ramp "+lane,
                    new Vector3(laneX,(frontCenter-.25f)*.5f,(unloadRear+unloadFront)*.5f),
                    new Vector3(16,.6f,Mathf.Sqrt(35*35+unloadRise*unloadRise)),original.sharedMaterial,beam.sharedMaterial);
                unloading.rotation=Quaternion.Euler(Mathf.Atan2(unloadRise,35)*Mathf.Rad2Deg,0,0);
                var tower = towers[lane];
                if (tower != null)
                {
                    tower.position = new Vector3(laneX + 7, 6, position.z); tower.localScale = new Vector3(2, 12, 4);
                    // The static shaft begins beyond the rotating beam's
                    // 0.75m half-width: never overlap a world-constrained arm.
                    float axleLeft = position.x + 1, axleRight = tower.position.x + 1;
                    PhysicalBox(match.transform, "Physical elevated trebuchet axle " + lane, new Vector3((axleLeft + axleRight) * .5f, 12, position.z), new Vector3(axleRight - axleLeft, 1, 2), original.sharedMaterial, beam.sharedMaterial);
                    EditorUtility.SetDirty(tower);
                }
                EditorUtility.SetDirty(root); EditorUtility.SetDirty(arm); EditorUtility.SetDirty(beam); EditorUtility.SetDirty(pivot); EditorUtility.SetDirty(counterweight); EditorUtility.SetDirty(cradle); EditorUtility.SetDirty(volume); EditorUtility.SetDirty(engine.LoadingCradle); EditorUtility.SetDirty(ramp);
            }
            Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("HH10_TREBUCHET_APPARATUS_REPAIRED metre-scaled side arm/counterweight/hinge, actual crossbar and four distinct actor-aligned loading/target lanes; native tractors/trailers untouched.");
        }
        static Transform PhysicalBox(Transform parent, string name, Vector3 position, Vector3 size, Material material, PhysicsMaterial contact)
        {
            var existing = parent.Find(name);
            if (existing == null)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; existing = box.transform; existing.SetParent(parent, false);
            }
            existing.SetPositionAndRotation(position, Quaternion.identity); existing.localScale = size;
            existing.GetComponent<Renderer>().sharedMaterial = material;
            var collider = existing.GetComponent<BoxCollider>(); collider.enabled = true; collider.sharedMaterial = contact;
            EditorUtility.SetDirty(existing); EditorUtility.SetDirty(collider);
            return existing;
        }
    }
}
#endif
