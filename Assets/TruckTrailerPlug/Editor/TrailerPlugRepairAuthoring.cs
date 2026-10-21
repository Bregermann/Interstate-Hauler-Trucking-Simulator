using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NWH.VehiclePhysics2.Modules.Trailer;
using NWH.VehiclePhysics2;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckTrailerPlug.Editor
{
    // Explicit, versioned repair of the saved scene. Never called by build/Play.
    public static class TrailerPlugRepairAuthoring
    {
        public const string TractorSource = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab";
        public const string TrailerSource = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab";

        public static Bounds ModelBounds(GameObject root)
        {
            bool first = true; Bounds bounds = default;
            foreach (var mesh in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mesh.sharedMesh == null || mesh.GetComponent<Renderer>() == null) continue;
                var b = LWS.TruckSports.Editor.SportsRallyAuthoring.BoundsIn(mesh, root.transform);
                if (first) { bounds = b; first = false; } else bounds.Encapsulate(b);
            }
            if (first) throw new InvalidOperationException("Missing authored vehicle geometry: " + root.name);
            return bounds;
        }

        public static void Audit()
        {
            var text = new System.Text.StringBuilder();
            foreach (string source in new[] { TractorSource, TrailerSource })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
                if (prefab == null) throw new InvalidOperationException("Installed matched NWH vehicle missing: " + source);
                var bounds = ModelBounds(prefab);
                text.AppendLine(source + " scale=" + prefab.transform.localScale + " bounds=" + bounds.ToString("F4") + " mass=" + prefab.GetComponent<Rigidbody>().mass);
                foreach (var module in prefab.GetComponentsInChildren<TrailerHitchModuleWrapper>(true))
                    text.AppendLine("TRACTOR HITCH local=" + prefab.transform.InverseTransformPoint(module.module.attachmentPoint.position).ToString("F4"));
                foreach (var module in prefab.GetComponentsInChildren<TrailerModuleWrapper>(true))
                    text.AppendLine("TRAILER HITCH local=" + prefab.transform.InverseTransformPoint(module.module.attachmentPoint.position).ToString("F4"));
                foreach (var body in prefab.GetComponentsInChildren<Rigidbody>(true)) text.AppendLine(" BODY " + body.name + " " + body.mass);
            }
            EditorSceneManager.OpenScene(TrailerPlugAuthoring.Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<TrailerPlugMatch>();
            text.AppendLine("CURRENT tractor=" + ModelBounds(match.rig.truck.gameObject).ToString("F4") + " trailer=" + ModelBounds(match.rig.trailer.gameObject).ToString("F4") + " opening=" + match.openingRadii);
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");
            File.WriteAllText("Logs/TruckSports/PlaytestRepair/trailer-native-geometry.txt", text.ToString());
            Debug.Log(text.ToString());
        }

        [MenuItem("Truck Sports/Trailer Plug/Repair matched NWH rig and physical opening")]
        public static void Upgrade()
        {
            EditorSceneManager.OpenScene(TrailerPlugAuthoring.Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<TrailerPlugMatch>();
            const string marker = "Matched NWH hitch repair v2";
            if (match.transform.Find(marker) != null) { TrailerPlugAuthoring.Validate(); return; }
            var old = match.rig; var oldOwner = old.gameObject;
            var trailerSource = AssetDatabase.LoadAssetAtPath<GameObject>(TrailerSource);
            // Match the already-established six-metre cargo width, applying the
            // SAME uniform factor to both original models. Never stretch axes.
            float scale = 6 / ModelBounds(trailerSource).size.x;
            var tractor = MatchedVehicle(TractorSource, scale, "Matched suspended NWH tractor");
            var trailer = MatchedVehicle(TrailerSource, scale, "Matched detachable NWH cargo trailer");
            var truck = tractor.AddComponent<SportsEventTruck>();
            truck.session = match; truck.body = tractor.GetComponent<Rigidbody>(); truck.visuals = tractor.transform;
            truck.wheels = tractor.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
            truck.paint = tractor.GetComponentsInChildren<Renderer>(true).First(r => r.name == "Cab");
            var envelope = ModelBounds(tractor);
            truck.shape = tractor.AddComponent<BoxCollider>(); truck.shape.isTrigger = true;
            truck.shape.center = envelope.center; truck.shape.size = envelope.size;
            var motor = tractor.AddComponent<SportsNwhTractorMotor>(); truck.tractorMotor = motor;
            motor.vehicle = tractor.GetComponent<VehicleController>(); motor.settings = match.settings;
            motor.forceSpatialAudio = true;

            var owner = new GameObject(marker); owner.transform.SetParent(match.transform, false);
            var rig = owner.AddComponent<TrailerPlugRig>(); match.rig = rig; rig.match = match; rig.truck = truck;
            rig.trailer = trailer.GetComponent<Rigidbody>(); rig.trailerLocalBounds = ModelBounds(trailer);
            rig.tractorHitch = tractor.GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            rig.trailerModule = trailer.GetComponentInChildren<TrailerModuleWrapper>(true);
            if (rig.tractorHitch == null || rig.trailerModule == null) throw new InvalidOperationException("Matched NWH prefabs must contain their original hitch modules.");
            rig.tractorHitch.module.attachOnEnable = false; rig.tractorHitch.module.detachable = true;
            rig.tractorHitch.module.breakForce = float.PositiveInfinity;
            rig.tractorHitch.module.useHingeJoint = false;
            rig.anchor = old.anchor; rig.anchor.position = new Vector3(rig.anchor.position.x, 100, rig.anchor.position.z);
            rig.truckStart = new Vector3(-7, 96, -22);
            rig.trailerOffset = Vector3.zero; // NWH aligns the authored kingpin.
            tractor.transform.SetPositionAndRotation(rig.truckStart, Quaternion.identity);
            trailer.transform.SetPositionAndRotation(rig.truckStart, Quaternion.identity);
            // Match anchors already in the saved countdown presentation too.
            trailer.transform.position += rig.tractorHitch.module.attachmentPoint.position - rig.trailerModule.module.attachmentPoint.position;
            rig.spring = tractor.AddComponent<SpringJoint>(); rig.spring.autoConfigureConnectedAnchor = false;
            rig.spring.connectedAnchor = rig.anchor.position;
            rig.spring.anchor = new Vector3(envelope.center.x, envelope.max.y, envelope.center.z);
            rig.restLength = old.restLength; rig.spring.maxDistance = rig.restLength;
            float totalMass = tractor.GetComponentsInChildren<Rigidbody>(true).Sum(b => b.mass) + rig.trailer.mass;
            float massRatio = totalMass / (old.truck.GetComponentsInChildren<Rigidbody>(true).Sum(b => b.mass) + old.trailer.mass);
            rig.spring.spring = old.spring.spring * massRatio; rig.spring.damper = old.spring.damper * massRatio;
            rig.spring.tolerance = old.spring.tolerance;
            rig.cable = old.cable;
            trailer.AddComponent<TrailerPlugContacts>().rig = rig;
            var obstacle = trailer.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.center = rig.trailerLocalBounds.center; obstacle.size = rig.trailerLocalBounds.size + Vector3.one * .35f;
            obstacle.carving = true; obstacle.carveOnlyStationary = false;

            // Saved quarry scenery and crowd settings stay intact. Only enlarge
            // the true floor opening, its liquid and its matching navigation.
            match.openingRadii = new Vector2(rig.trailerLocalBounds.size.x * .425f, rig.trailerLocalBounds.size.z * .34f);
            EnlargeOpening(match);
            var crane = GameObject.Find("Existing modeled bungee support crane");
            if (crane != null) crane.transform.localScale = new Vector3(1, rig.anchor.position.y / 65, 1);
            foreach (var ghost in match.replay.ghosts) UnityEngine.Object.DestroyImmediate(ghost.gameObject);
            UnityEngine.Object.DestroyImmediate(match.replay);
            match.replay = SportsRallyAuthoring.Replay(match, new[] { truck.visuals, trailer.transform });
            match.replay.capacity = 1800; match.replay.rolling = true; match.replay.interval = .04f; match.replay.maximumReplayAge = 18;
            var snow = match.GetComponent<SportsSnowSurface>();
            if (snow != null)
            {
                snow.wheels = tractor.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true)
                    .Concat(trailer.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true)).ToArray();
                snow.broadBodies = new[] { rig.trailer }; EditorUtility.SetDirty(snow);
            }
            var oldTruck = old.truck.gameObject;
            UnityEngine.Object.DestroyImmediate(old.trailer.gameObject);
            UnityEngine.Object.DestroyImmediate(oldTruck);
            if (oldOwner != null && oldOwner != oldTruck) UnityEngine.Object.DestroyImmediate(oldOwner);
            foreach (var body in tractor.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            rig.trailer.isKinematic = true;
            TrailerPlugAuthoring.RebakeNavigation();
            EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
            EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
            TrailerPlugAuthoring.Validate();
            File.WriteAllLines("Logs/TruckSports/PlaytestRepair/trailer-repair-dependencies.txt", AssetDatabase.GetDependencies(TrailerPlugAuthoring.Scene, true));
            Debug.Log("TRAILER_MATCHED_REPAIR scale=" + scale + " tractor=" + envelope + " trailer=" + rig.trailerLocalBounds + " opening=" + match.openingRadii + " totalMass=" + totalMass);
        }

        static GameObject MatchedVehicle(string path, float scale, string name)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = name; root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (root.GetComponent<MeshFilter>() != null) throw new InvalidOperationException("Expected native vehicle geometry below the unit-scale physics root.");
            // NWH requires unit scales on BOTH VehicleController and each wheel
            // controller. Scale spatial coordinates, leaving their transforms
            // and original rig hierarchy intact; geometry gets an owned leaf.
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child != root.transform) child.localPosition *= scale;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var original = filter.GetComponent<MeshRenderer>(); if (original == null) continue;
                var geometry = new GameObject(filter.name); geometry.transform.SetParent(filter.transform, false); geometry.transform.localScale = Vector3.one * scale;
                geometry.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = geometry.AddComponent<MeshRenderer>(); EditorUtility.CopySerialized(original, renderer);
                UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(filter);
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider is BoxCollider box) { box.center *= scale; box.size *= scale; }
                else if (collider is SphereCollider sphere) { sphere.center *= scale; sphere.radius *= scale; }
                else if (collider is CapsuleCollider capsule) { capsule.center *= scale; capsule.radius *= scale; capsule.height *= scale; }
                else if (collider is MeshCollider mesh)
                {
                    var collision = new GameObject("Uniform native collision"); collision.transform.SetParent(mesh.transform, false);
                    collision.transform.localScale = Vector3.one * scale;
                    EditorUtility.CopySerialized(mesh, collision.AddComponent<MeshCollider>()); UnityEngine.Object.DestroyImmediate(mesh);
                }
                else throw new InvalidOperationException("Unsupported collider in matched native vehicle: " + collider.GetType().Name);
            }
            foreach (var joint in root.GetComponentsInChildren<Joint>(true))
            {
                joint.anchor *= scale;
                if (joint.connectedBody != null && joint.connectedBody.transform.IsChildOf(root.transform)) joint.connectedAnchor *= scale;
                if (joint is ConfigurableJoint configurable) { var limit = configurable.linearLimit; limit.limit *= scale; configurable.linearLimit = limit; }
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.gameObject.layer = 2;
            root.layer = 2;
            foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(listener);
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string space = behaviour.GetType().Namespace ?? "";
                if (space.StartsWith("NWH.Common.Cameras") || space.StartsWith("NWH.VehiclePhysics2.Input")) behaviour.enabled = false;
            }
            foreach (var source in root.GetComponentsInChildren<AudioSource>(true)) { source.playOnAwake = false; source.enabled = false; }
            var vehicle = root.GetComponent<VehicleController>(); vehicle.input.autoSetInput = false; vehicle.enabled = false;
            foreach (var wheel in root.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true))
            {
                wheel.wheel.radius *= scale; wheel.wheel.width *= scale; wheel.spring.maxLength *= scale;
                wheel.forceApplicationPointDistance *= scale;
                wheel.enabled = false; if (wheel.wheel.meshCollider != null) wheel.wheel.meshCollider.enabled = false;
            }
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                body.centerOfMass *= scale; body.isKinematic = true; body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.solverIterations = 24; body.solverVelocityIterations = 10; body.maxDepenetrationVelocity = 3;
                body.maxAngularVelocity = 6; body.linearDamping = .015f; body.angularDamping = .18f;
            }
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || materials[i].shader.name.StartsWith("Universal Render Pipeline/")) continue;
                    string ownedPath = "Assets/TruckTrailerPlug/Materials/Imported " + materials[i].name + ".mat";
                    var owned = AssetDatabase.LoadAssetAtPath<Material>(ownedPath);
                    if (owned == null)
                    {
                        owned = new Material(Shader.Find("Universal Render Pipeline/Lit")); owned.name = "Imported " + materials[i].name;
                        owned.mainTexture = materials[i].mainTexture;
                        if (materials[i].HasProperty("_Color")) owned.color = materials[i].color;
                        AssetDatabase.CreateAsset(owned, ownedPath);
                    }
                    materials[i] = owned;
                }
                renderer.sharedMaterials = materials;
            }
            return root;
        }

        static void EnlargeOpening(TrailerPlugMatch match)
        {
            var floor = GameObject.Find("Continuous physical floor around open hole");
            var mesh = floor.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i += 2)
            { float angle = i / 2 * Mathf.PI * 2 / (vertices.Length / 2); vertices[i] = new Vector3(Mathf.Cos(angle) * match.openingRadii.x, 0, Mathf.Sin(angle) * match.openingRadii.y); }
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            floor.GetComponent<MeshCollider>().sharedMesh = null; floor.GetComponent<MeshCollider>().sharedMesh = mesh;
            GameObject.Find("Emissive bubbling sinkhole liquid").transform.localScale = new Vector3(match.openingRadii.x * 2, .08f, match.openingRadii.y * 2);
            var pit = GameObject.Find("Deep pit catch floor");
            pit.transform.localScale = new Vector3(Mathf.Max(25, match.openingRadii.x * 2 + 8), 1, Mathf.Max(25, match.openingRadii.y * 2 + 8));
        }
    }
}
